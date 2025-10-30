using System;
using MelonLoader;
using UnityEngine;
using Il2CppFMOD;
using FMOD = Il2CppFMOD;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSMicrophoneCapture
    {
        private FMOD.System fmodSystem;
        private FMOD.Sound recordSound;
        private int selectedDeviceIndex = 0;
        private bool isRecording = false;
        private bool isInitialized = false;

        private const int SAMPLE_RATE = 48000;
        private const int CHANNELS = 1; // Mono for voice - saves bandwidth
        private const int FRAME_SIZE = 960; // 20ms at 48kHz - standard for Opus
        private const int BUFFER_SIZE = SAMPLE_RATE * CHANNELS * 2; // 1 second circular buffer

        private uint lastRecordPos = 0;
        private float volume = 1.0f;

        // Output buffer for one frame (ready for networking)
        private byte[] frameBuffer;

        // Stats for monitoring
        public int DroppedFrames { get; private set; }
        public int CapturedFrames { get; private set; }

        public BBSMicrophoneCapture()
        {
            frameBuffer = new byte[FRAME_SIZE * CHANNELS * 2]; // 16-bit PCM
        }

        public bool Initialize(int deviceIndex = 0)
        {
            try
            {
                // Get FMOD system instance
                fmodSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;

                if (!fmodSystem.hasHandle())
                {
                    MelonLogger.Error("FMOD system not available");
                    return false;
                }

                // Get number of recording devices
                int numDrivers, numConnected;
                RESULT result = fmodSystem.getRecordNumDrivers(out numDrivers, out numConnected);

                if (result != RESULT.OK || numConnected == 0)
                {
                    MelonLogger.Error($"No recording devices found: {result}");
                    return false;
                }

                // Log available devices
                MelonLogger.Msg($"Found {numConnected} recording device(s)");
                for (int i = 0; i < numConnected; i++)
                {
                    string name;
                    Il2CppSystem.Guid guid;
                    int systemRate;
                    SPEAKERMODE speakerMode;
                    int speakerModeChannels;
                    DRIVER_STATE state;

                    fmodSystem.getRecordDriverInfo(
                        i,
                        out name,
                        256,
                        out guid,
                        out systemRate,
                        out speakerMode,
                        out speakerModeChannels,
                        out state
                    );

                    MelonLogger.Msg($"  Device {i}: {name} ({systemRate}Hz)");
                }

                // Validate device index
                if (deviceIndex < 0 || deviceIndex >= numConnected)
                {
                    MelonLogger.Warning($"Invalid device index {deviceIndex}, using device 0");
                    deviceIndex = 0;
                }

                selectedDeviceIndex = deviceIndex;

                // Create sound for recording
                CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
                exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
                exinfo.length = (uint)BUFFER_SIZE;
                exinfo.numchannels = CHANNELS;
                exinfo.defaultfrequency = SAMPLE_RATE;
                exinfo.format = SOUND_FORMAT.PCM16;

                result = fmodSystem.createSound(
                    string.Empty,
                    MODE.LOOP_NORMAL | MODE.OPENUSER,
                    ref exinfo,
                    out recordSound
                );

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to create recording sound: {result}");
                    return false;
                }

                isInitialized = true;
                MelonLogger.Msg($"BBSMicrophoneCapture initialized on device {deviceIndex}");
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error initializing BBSMicrophoneCapture: {e}");
                return false;
            }
        }

        public bool StartRecording()
        {
            if (!isInitialized)
            {
                MelonLogger.Error("Cannot start recording - not initialized");
                return false;
            }

            if (isRecording)
            {
                MelonLogger.Warning("Already recording");
                return true;
            }

            try
            {
                RESULT result = fmodSystem.recordStart(selectedDeviceIndex, recordSound, true);

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to start recording: {result}");
                    return false;
                }

                isRecording = true;
                lastRecordPos = 0;
                DroppedFrames = 0;
                CapturedFrames = 0;

                MelonLogger.Msg("Recording started");
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error starting recording: {e}");
                return false;
            }
        }

        public void StopRecording()
        {
            if (!isRecording) return;

            try
            {
                fmodSystem.recordStop(selectedDeviceIndex);
                isRecording = false;
                MelonLogger.Msg("Recording stopped");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error stopping recording: {e}");
            }
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
        }

        public float GetVolume()
        {
            return volume;
        }

        // Call this in LateUpdate to get the latest audio frame
        // Returns null if no new frame is available
        public byte[] GetAudioFrame()
        {
            if (!isRecording || !recordSound.hasHandle())
                return null;

            try
            {
                // Get current recording position
                uint recordPos = 0;
                RESULT result = fmodSystem.getRecordPosition(selectedDeviceIndex, out recordPos);

                if (result != RESULT.OK)
                    return null;

                // Convert to bytes
                uint recordPosBytes = recordPos * CHANNELS * 2; // 2 bytes per sample (16-bit)
                uint lastRecordPosBytes = lastRecordPos * CHANNELS * 2;

                // Calculate available samples
                int availableBytes;
                if (recordPosBytes >= lastRecordPosBytes)
                {
                    availableBytes = (int)(recordPosBytes - lastRecordPosBytes);
                }
                else
                {
                    // Wrapped around
                    availableBytes = (int)(BUFFER_SIZE - lastRecordPosBytes + recordPosBytes);
                }

                // Check if we have at least one frame available
                int frameSizeBytes = FRAME_SIZE * CHANNELS * 2;
                if (availableBytes < frameSizeBytes)
                {
                    return null; // Not enough data yet
                }

                // Check for buffer overrun (dropped frames)
                if (availableBytes > frameSizeBytes * 3)
                {
                    DroppedFrames++;
                    // Skip ahead to most recent data
                    int framesToSkip = (availableBytes / frameSizeBytes) - 1;
                    lastRecordPosBytes = (lastRecordPosBytes + (uint)(framesToSkip * frameSizeBytes)) % BUFFER_SIZE;
                }

                // Lock and read one frame
                IntPtr ptr1, ptr2;
                uint len1, len2;

                result = recordSound.@lock(
                    lastRecordPosBytes,
                    (uint)frameSizeBytes,
                    out ptr1, out ptr2,
                    out len1, out len2
                );

                if (result != RESULT.OK)
                    return null;

                // Copy data from FMOD buffer
                if (len1 > 0)
                    System.Runtime.InteropServices.Marshal.Copy(ptr1, frameBuffer, 0, (int)len1);

                if (len2 > 0)
                    System.Runtime.InteropServices.Marshal.Copy(ptr2, frameBuffer, (int)len1, (int)len2);

                recordSound.unlock(ptr1, ptr2, len1, len2);

                // Apply volume
                if (volume != 1.0f)
                {
                    ApplyVolume(frameBuffer);
                }

                // Update position for next frame
                lastRecordPos = (uint)((lastRecordPosBytes + frameSizeBytes) / (CHANNELS * 2));
                lastRecordPos %= (uint)(BUFFER_SIZE / (CHANNELS * 2));

                CapturedFrames++;

                return frameBuffer;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error capturing audio frame: {e}");
                return null;
            }
        }

        // Convert mono to stereo for playback through BBSAudioSource
        public byte[] ConvertToStereo(byte[] monoData)
        {
            if (monoData == null) return null;

            byte[] stereoData = new byte[monoData.Length * 2];

            for (int i = 0; i < monoData.Length; i += 2)
            {
                // Copy same sample to both left and right channels
                stereoData[i * 2] = monoData[i];         // Left low byte
                stereoData[i * 2 + 1] = monoData[i + 1]; // Left high byte
                stereoData[i * 2 + 2] = monoData[i];     // Right low byte
                stereoData[i * 2 + 3] = monoData[i + 1]; // Right high byte
            }

            return stereoData;
        }

        private void ApplyVolume(byte[] data)
        {
            for (int i = 0; i < data.Length; i += 2)
            {
                // Reconstruct 16-bit sample
                short sample = (short)(data[i] | (data[i + 1] << 8));

                // Apply volume
                sample = (short)(sample * volume);

                // Write back
                data[i] = (byte)(sample & 0xFF);
                data[i + 1] = (byte)((sample >> 8) & 0xFF);
            }
        }

        public bool IsRecording()
        {
            return isRecording;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public int GetSelectedDeviceIndex()
        {
            return selectedDeviceIndex;
        }

        // Get list of available recording devices
        public string[] GetAvailableDevices()
        {
            if (!fmodSystem.hasHandle())
                return new string[0];

            try
            {
                int numDrivers, numConnected;
                fmodSystem.getRecordNumDrivers(out numDrivers, out numConnected);

                string[] devices = new string[numConnected];

                for (int i = 0; i < numConnected; i++)
                {
                    string name;
                    Il2CppSystem.Guid guid;
                    int systemRate;
                    SPEAKERMODE speakerMode;
                    int speakerModeChannels;
                    DRIVER_STATE state;

                    fmodSystem.getRecordDriverInfo(
                        i,
                        out name,
                        256,
                        out guid,
                        out systemRate,
                        out speakerMode,
                        out speakerModeChannels,
                        out state
                    );

                    devices[i] = name;
                }

                return devices;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error getting device list: {e}");
                return new string[0];
            }
        }

        public void Dispose()
        {
            try
            {
                StopRecording();

                if (recordSound.hasHandle())
                {
                    recordSound.release();
                }

                isInitialized = false;
                MelonLogger.Msg("BBSMicrophoneCapture disposed");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error disposing BBSMicrophoneCapture: {e}");
            }
        }
    }
}