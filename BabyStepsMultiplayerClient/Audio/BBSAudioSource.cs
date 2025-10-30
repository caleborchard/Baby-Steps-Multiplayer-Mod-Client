using System;
using MelonLoader;
using UnityEngine;
using Il2CppFMOD;
using FMOD = Il2CppFMOD;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSAudioSource
    {
        private Transform transform;
        private FMOD.System fmodSystem;
        private FMOD.Sound sound;
        private FMOD.Channel channel;

        private const int SAMPLE_RATE = 48000;
        private const int CHANNELS = 2;
        private const int BUFFER_SIZE = 48000 * 2; // 1 second buffer

        private byte[] audioBuffer;
        private int writePosition = 0;
        private bool isInitialized = false;
        private bool needsResync = false;

        // Placeholder variables for test tone
        private float testPhase = 0f;
        private float testFrequency = 440f;
        private const float TEST_FREQ_MIN = 220f;
        private const float TEST_FREQ_MAX = 880f;
        private float testFreqDirection = 1f;

        public BBSAudioSource(Transform transform)
        {
            this.transform = transform;
            audioBuffer = new byte[BUFFER_SIZE];
        }

        public void Initialize()
        {
            try
            {
                // Get FMOD system instance
                fmodSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;

                // Create sound info for streaming PCM
                CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
                exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
                exinfo.length = (uint)BUFFER_SIZE;
                exinfo.numchannels = CHANNELS;
                exinfo.defaultfrequency = SAMPLE_RATE;
                exinfo.format = SOUND_FORMAT.PCM16;
                exinfo.decodebuffersize = (uint)(SAMPLE_RATE / 10); // 100ms decode buffer

                // Create the sound
                RESULT result = fmodSystem.createSound(
                    string.Empty,
                    MODE.OPENUSER | MODE._3D | MODE.LOOP_NORMAL,
                    ref exinfo,
                    out sound
                );

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to create FMOD sound: {result}");
                    return;
                }

                // Set 3D settings
                sound.set3DMinMaxDistance(1f, 100f);

                // Get CoreSystem master channel group
                result = fmodSystem.getMasterChannelGroup(out var masterChannelGroup);

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to get master channel group: {result}");
                    return;
                }

                // Play the sound
                result = fmodSystem.playSound(sound, masterChannelGroup, false, out channel);

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to play FMOD sound: {result}");
                    return;
                }

                // Configure channel for 3D
                channel.setMode(MODE._3D);

                isInitialized = true;
                MelonLogger.Msg("BBSAudioSource initialized successfully");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error initializing BBSAudioSource: {e}");
            }
        }

        public void Update()
        {
            if (!isInitialized || channel.hasHandle() == false) return;

            try
            {
                // Update 3D position
                Vector3 pos = transform.position;
                Vector3 vel = Vector3.zero; // You can calculate velocity if needed

                VECTOR fmodPos = new VECTOR
                {
                    x = pos.x,
                    y = pos.y,
                    z = pos.z
                };

                VECTOR fmodVel = new VECTOR
                {
                    x = vel.x,
                    y = vel.y,
                    z = vel.z
                };

                channel.set3DAttributes(ref fmodPos, ref fmodVel);

                // Check if we need to resync the buffer
                SyncBufferIfNeeded();

                // Placeholder: Generate test tone with changing pitch
                //GenerateTestTone();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in BBSAudioSource Update: {e}");
            }
        }

        // Placeholder function: Generates a sine wave test tone with changing pitch
        private void GenerateTestTone()
        {
            try
            {
                // Modulate frequency over time
                testFrequency += testFreqDirection * 100f * Time.deltaTime;
                if (testFrequency >= TEST_FREQ_MAX)
                {
                    testFrequency = TEST_FREQ_MAX;
                    testFreqDirection = -1f;
                }
                else if (testFrequency <= TEST_FREQ_MIN)
                {
                    testFrequency = TEST_FREQ_MIN;
                    testFreqDirection = 1f;
                }

                // Generate samples for this frame
                int samplesToGenerate = (int)(SAMPLE_RATE * Time.deltaTime);
                byte[] frameBuffer = new byte[samplesToGenerate * CHANNELS * 2]; // 2 bytes per sample (16-bit)

                for (int i = 0; i < samplesToGenerate; i++)
                {
                    // Generate sine wave sample
                    short sample = (short)(Mathf.Sin(testPhase) * 0.3f * short.MaxValue);
                    testPhase += 2f * Mathf.PI * testFrequency / SAMPLE_RATE;

                    if (testPhase > 2f * Mathf.PI) testPhase -= 2f * Mathf.PI;

                    // Write to both channels (stereo)
                    for (int c = 0; c < CHANNELS; c++)
                    {
                        int idx = (i * CHANNELS + c) * 2;
                        frameBuffer[idx] = (byte)(sample & 0xFF);
                        frameBuffer[idx + 1] = (byte)((sample >> 8) & 0xFF);
                    }
                }

                // Write to FMOD sound
                WriteAudioData(frameBuffer);
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error generating test tone: {e}");
            }
        }

        // Sync buffer to prevent static when coming back into range
        private void SyncBufferIfNeeded()
        {
            try
            {
                if (!channel.hasHandle() || !sound.hasHandle()) return;

                // Get current playback position
                uint playbackPos = 0;
                channel.getPosition(out playbackPos, TIMEUNIT.PCMBYTES);

                // Calculate the distance between write and playback positions
                int distance = writePosition - (int)playbackPos;
                if (distance < 0) distance += BUFFER_SIZE;

                // If we're too close (less than 200ms ahead) or too far (more than 800ms ahead), resync
                int minBuffer = SAMPLE_RATE * CHANNELS * 2 / 5; // 200ms
                int maxBuffer = SAMPLE_RATE * CHANNELS * 2 * 4 / 5; // 800ms

                if (distance < minBuffer || distance > maxBuffer)
                {
                    // Resync: position write head 300ms ahead of playback
                    int targetBuffer = SAMPLE_RATE * CHANNELS * 2 * 3 / 10; // 300ms
                    writePosition = ((int)playbackPos + targetBuffer) % BUFFER_SIZE;

                    // Clear the area we're about to write to prevent old data from playing
                    ClearBuffer(writePosition, targetBuffer / 2);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error syncing buffer: {e}");
            }
        }

        // Clear a section of the buffer
        private void ClearBuffer(int startPos, int length)
        {
            try
            {
                if (!sound.hasHandle()) return;

                IntPtr ptr1, ptr2;
                uint len1, len2;

                RESULT result = sound.@lock(
                    (uint)startPos,
                    (uint)length,
                    out ptr1, out ptr2,
                    out len1, out len2
                );

                if (result == RESULT.OK)
                {
                    // Zero out the buffer sections
                    if (len1 > 0)
                    {
                        byte[] zeros = new byte[len1];
                        System.Runtime.InteropServices.Marshal.Copy(zeros, 0, ptr1, (int)len1);
                    }

                    if (len2 > 0)
                    {
                        byte[] zeros = new byte[len2];
                        System.Runtime.InteropServices.Marshal.Copy(zeros, 0, ptr2, (int)len2);
                    }

                    sound.unlock(ptr1, ptr2, len1, len2);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error clearing buffer: {e}");
            }
        }

        // Public method to write PCM audio data
        public void WriteAudioData(byte[] data)
        {
            if (!isInitialized || sound.hasHandle() == false) return;

            try
            {
                uint length = 0;
                sound.getLength(out length, TIMEUNIT.PCMBYTES);

                IntPtr ptr1, ptr2;
                uint len1, len2;

                RESULT result = sound.@lock(
                    (uint)writePosition,
                    (uint)data.Length,
                    out ptr1, out ptr2,
                    out len1, out len2
                );

                if (result == RESULT.OK)
                {
                    // Copy data to FMOD buffer
                    if (len1 > 0)
                        System.Runtime.InteropServices.Marshal.Copy(data, 0, ptr1, (int)len1);

                    if (len2 > 0)
                        System.Runtime.InteropServices.Marshal.Copy(data, (int)len1, ptr2, (int)len2);

                    sound.unlock(ptr1, ptr2, len1, len2);

                    // Update write position
                    writePosition = (writePosition + data.Length) % (int)length;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error writing audio data: {e}");
            }
        }

        // Cleanup
        public void Dispose()
        {
            try
            {
                if (channel.hasHandle()) channel.stop();

                if (sound.hasHandle()) sound.release();

                isInitialized = false;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error disposing BBSAudioSource: {e}");
            }
        }
    }
}