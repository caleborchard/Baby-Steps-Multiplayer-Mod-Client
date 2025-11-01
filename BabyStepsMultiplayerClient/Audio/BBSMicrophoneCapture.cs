﻿using System;
using MelonLoader;
using UnityEngine;
using Il2CppFMOD;
using Concentus.Structs;
using Concentus.Enums;
using FMOD = Il2CppFMOD;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSMicrophoneCapture
    {
        private FMOD.System fmodSystem;
        private Sound recordSound;
        private int selectedDeviceIndex = 0;
        private bool isRecording = false;
        private bool isInitialized = false;

        private const int SAMPLE_RATE = 48000;
        private const int CHANNELS = 1;
        private const int FRAME_SIZE = 960; // Optimal for Opus

        private const int BUFFER_SIZE = SAMPLE_RATE * CHANNELS * 2 * 100 / 1000;

        private uint lastRecordPos = 0;
        private float volume = 1.0f;

        private OpusEncoder opusEncoder;
        private short[] sampleBuffer;
        private byte[] opusPacketBuffer;

        private byte[] frameBuffer; // Pre-allocated for GC

        // Voice Activity Detection
        private const float VAD_THRESHOLD = 0.01f; // Adjust based on testing
        private bool voiceDetected = false;

        // Stats
        public int DroppedFrames { get; private set; }
        public int CapturedFrames { get; private set; }
        public int EncodeErrors { get; private set; }
        public bool VoiceDetected => voiceDetected;

        public BBSMicrophoneCapture()
        {
            frameBuffer = new byte[FRAME_SIZE * CHANNELS * 2];
            sampleBuffer = new short[FRAME_SIZE];
            opusPacketBuffer = new byte[4000]; // Max Opus packet size
        }

        public bool Initialize(int deviceIndex = 0)
        {
            try
            {
                fmodSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;

                if (!fmodSystem.hasHandle())
                {
                    MelonLogger.Error("FMOD system not available");
                    return false;
                }

                int numDrivers, numConnected;
                RESULT result = fmodSystem.getRecordNumDrivers(out numDrivers, out numConnected);

                if (result != RESULT.OK || numConnected == 0)
                {
                    MelonLogger.Error($"No recording devices found: {result}");
                    return false;
                }

                Core.DebugMsg($"Found {numConnected} recording device(s)");
                for (int i = 0; i < numConnected; i++)
                {
                    string name;
                    Il2CppSystem.Guid guid;
                    int systemRate;
                    SPEAKERMODE speakerMode;
                    int speakerModeChannels;
                    DRIVER_STATE state;

                    fmodSystem.getRecordDriverInfo(i, out name, 256, out guid, out systemRate, out speakerMode, out speakerModeChannels, out state);

                    Core.DebugMsg($"  Device {i}: {name} ({systemRate}Hz)");
                }

                if (deviceIndex < 0 || deviceIndex >= numConnected)
                {
                    MelonLogger.Warning($"Invalid device index {deviceIndex}, using device 0");
                    deviceIndex = 0;
                }

                selectedDeviceIndex = deviceIndex;

                opusEncoder = new OpusEncoder(SAMPLE_RATE, 1, OpusApplication.OPUS_APPLICATION_VOIP);

                // Set encoder parameters for low latency
                opusEncoder.Bitrate = 24000; // 24kbps is good enough quality for voice
                opusEncoder.Complexity = 5; // 0-10, 5 is okay
                opusEncoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
                opusEncoder.UseVBR = true; // Variable bitrate
                opusEncoder.UseDTX = true; // Discontinuous transmission (shut up transmission on silence). Super nice that this is built in

                CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
                exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
                exinfo.length = BUFFER_SIZE;
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
                MelonLogger.Msg($"BBSMicrophoneCapture initialized on device {deviceIndex} with Opus encoder");
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
                EncodeErrors = 0;

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

        public float GetVolume() => volume;

        public byte[] GetOpusPacket() // Main method to be called each frame
        {
            byte[] monoFrame = GetAudioFrameInternal();
            if (monoFrame == null) return null;

            try
            {
                ConvertBytesToSamples(monoFrame, sampleBuffer);

                int encodedLength = opusEncoder.Encode(sampleBuffer, 0, FRAME_SIZE,opusPacketBuffer, 0, opusPacketBuffer.Length);

                if (encodedLength < 0)
                {
                    EncodeErrors++;
                    MelonLogger.Error($"Opus encoding failed with error code: {encodedLength}");
                    return null;
                }

                byte[] opusPacket = new byte[encodedLength];
                Array.Copy(opusPacketBuffer, 0, opusPacket, 0, encodedLength);

                return opusPacket;
            }
            catch (Exception e)
            {
                EncodeErrors++;
                MelonLogger.Error($"Error encoding audio: {e}");
                return null;
            }
        }

        private byte[] GetAudioFrameInternal() // Raw PCM data
        {
            if (!isRecording || !recordSound.hasHandle())
                return null;

            try
            {
                uint recordPos = 0;
                RESULT result = fmodSystem.getRecordPosition(selectedDeviceIndex, out recordPos);

                if (result != RESULT.OK)
                    return null;

                uint recordPosBytes = recordPos * CHANNELS * 2;
                uint lastRecordPosBytes = lastRecordPos * CHANNELS * 2;

                int availableBytes;
                if (recordPosBytes >= lastRecordPosBytes)
                {
                    availableBytes = (int)(recordPosBytes - lastRecordPosBytes);
                }
                else
                {
                    availableBytes = (int)(BUFFER_SIZE - lastRecordPosBytes + recordPosBytes);
                }

                int frameSizeBytes = FRAME_SIZE * CHANNELS * 2;

                // Not enough data yet
                if (availableBytes < frameSizeBytes)
                {
                    return null;
                }

                // Check for dropped frames (buffer overrun)
                if (availableBytes > frameSizeBytes * 2)
                {
                    DroppedFrames++;
                    // Skip to most recent complete frame
                    int framesToSkip = (availableBytes / frameSizeBytes) - 1;
                    lastRecordPosBytes = (lastRecordPosBytes + (uint)(framesToSkip * frameSizeBytes)) % BUFFER_SIZE;
                    availableBytes = frameSizeBytes;
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

                if (len1 > 0)
                    System.Runtime.InteropServices.Marshal.Copy(ptr1, frameBuffer, 0, (int)len1);

                if (len2 > 0)
                    System.Runtime.InteropServices.Marshal.Copy(ptr2, frameBuffer, (int)len1, (int)len2);

                recordSound.unlock(ptr1, ptr2, len1, len2);

                if (volume != 1.0f)
                {
                    ApplyVolume(frameBuffer);
                }

                // Simple VAD check
                voiceDetected = DetectVoiceActivity(frameBuffer);

                // Update position
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

        private void ConvertBytesToSamples(byte[] bytes, short[] samples)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
            }
        }

        private void ApplyVolume(byte[] data)
        {
            for (int i = 0; i < data.Length; i += 2)
            {
                short sample = (short)(data[i] | (data[i + 1] << 8));
                sample = (short)(sample * volume);
                data[i] = (byte)(sample & 0xFF);
                data[i + 1] = (byte)((sample >> 8) & 0xFF);
            }
        }

        private bool DetectVoiceActivity(byte[] data)
        {
            if (data == null || data.Length == 0) return false;

            float energy = 0f;
            int sampleCount = data.Length / 2;

            for (int i = 0; i < data.Length; i += 2)
            {
                short sample = (short)(data[i] | (data[i + 1] << 8));
                float normalized = sample / 32768f;
                energy += normalized * normalized;
            }

            energy /= sampleCount;
            return energy > VAD_THRESHOLD;
        }

        public bool IsRecording() => isRecording;
        public bool IsInitialized() => isInitialized;
        public int GetSelectedDeviceIndex() => selectedDeviceIndex;

        public string[] GetAvailableDevices()
        {
            if (!fmodSystem.hasHandle()) return new string[0];

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

                    fmodSystem.getRecordDriverInfo(i, out name, 256, out guid, out systemRate,
                    out speakerMode, out speakerModeChannels, out state);

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

                opusEncoder = null;

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