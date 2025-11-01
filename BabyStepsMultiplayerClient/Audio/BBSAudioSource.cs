using Il2CppFMOD;
using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using Concentus.Structs;
using FMOD = Il2CppFMOD;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSAudioSource
    {
        private Transform transform;
        private FMOD.System fmodSystem;
        private Sound sound;
        private Channel channel;

        private const int SAMPLE_RATE = 48000;
        private const int CHANNELS = 2; // Stereo
        private const int FRAME_SIZE = 960;

        private const int BUFFER_SIZE = SAMPLE_RATE * CHANNELS * 2 * 200 / 1000;

        private int writePosition = 0;
        private bool isInitialized = false;

        // Opus decoder
        private OpusDecoder opusDecoder;
        private short[] decodedSamples; // Mono samples from decoder
        private byte[] stereoBuffer; // Converted to stereo PCM16

        // Jitter buffer for Opus-encoded network packets
        private Queue<byte[]> jitterBuffer = new Queue<byte[]>();
        private const int MIN_JITTER_FRAMES = 2; // 40ms minimum buffer
        private const int MAX_JITTER_FRAMES = 6; // 120ms maximum buffer
        private bool isPlaying = false;

        // Stats
        public int UnderrunCount { get; private set; }
        public int OverrunCount { get; private set; }
        public int DecodeErrors { get; private set; }
        public float CurrentLatencyMs { get; private set; }

        public BBSAudioSource(Transform transform)
        {
            this.transform = transform;
        }

        public void Initialize()
        {
            try
            {
                fmodSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;

                opusDecoder = new OpusDecoder(SAMPLE_RATE, 1);
                decodedSamples = new short[FRAME_SIZE]; // Mono samples
                stereoBuffer = new byte[FRAME_SIZE * 2 * 2]; // Stereo bytes

                CREATESOUNDEXINFO exinfo = new CREATESOUNDEXINFO();
                exinfo.cbsize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
                exinfo.length = BUFFER_SIZE;
                exinfo.numchannels = CHANNELS;
                exinfo.defaultfrequency = SAMPLE_RATE;
                exinfo.format = SOUND_FORMAT.PCM16;
                exinfo.decodebuffersize = FRAME_SIZE;

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

                sound.set3DMinMaxDistance(1f, 100f);

                result = fmodSystem.getMasterChannelGroup(out var masterChannelGroup);
                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to get master channel group: {result}");
                    return;
                }

                // Wait for jitter buffer to fill, start paused
                result = fmodSystem.playSound(sound, masterChannelGroup, true, out channel);

                if (result != RESULT.OK)
                {
                    MelonLogger.Error($"Failed to play FMOD sound: {result}");
                    return;
                }

                channel.setMode(MODE._3D);

                isInitialized = true;
                MelonLogger.Msg("BBSAudioSource initialized successfully with Opus decoder");
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
                Vector3 pos = transform.position;
                Vector3 vel = Vector3.zero;

                VECTOR fmodPos = new VECTOR { x = pos.x, y = pos.y, z = pos.z };
                VECTOR fmodVel = new VECTOR { x = vel.x, y = vel.y, z = vel.z };

                channel.set3DAttributes(ref fmodPos, ref fmodVel);

                ProcessJitterBuffer();
                UpdateLatencyStats();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error in BBSAudioSource Update: {e}");
            }
        }

        private void ProcessJitterBuffer()
        {
            // Wait for minimum buffer to start playing
            if (!isPlaying && jitterBuffer.Count >= MIN_JITTER_FRAMES)
            {
                channel.setPaused(false);
                isPlaying = true;
                //MelonLogger.Msg($"Started playback with {jitterBuffer.Count} frames buffered");
            }

            // Stop if buffer is empty to prevent clicking sounds, "underrun"
            if (isPlaying && jitterBuffer.Count == 0)
            {
                channel.setPaused(true);
                isPlaying = false;
                UnderrunCount++;
                //MelonLogger.Warning("Audio underrun - pausing playback");
            }

            // Write available frames to FMOD
            while (jitterBuffer.Count > 0 && CanWriteFrame())
            {
                byte[] opusPacket = jitterBuffer.Dequeue();
                DecodeAndWriteFrame(opusPacket);
            }

            // Drop frames if buffer is too full, very unlikely
            if (jitterBuffer.Count > MAX_JITTER_FRAMES)
            {
                int framesToDrop = jitterBuffer.Count - MAX_JITTER_FRAMES;
                for (int i = 0; i < framesToDrop; i++)
                {
                    jitterBuffer.Dequeue();
                }
                OverrunCount++;
                //MelonLogger.Warning($"Dropped {framesToDrop} frames due to overrun");
            }
        }

        private void DecodeAndWriteFrame(byte[] opusPacket)
        {
            if (opusPacket.Length < 2) return;

            try
            {
                int decodedLength = opusDecoder.Decode(opusPacket, 0, opusPacket.Length, decodedSamples, 0, FRAME_SIZE);

                if (decodedLength != FRAME_SIZE)
                {
                    MelonLogger.Warning($"Unexpected decoded length: {decodedLength} (expected {FRAME_SIZE})");
                }

                ConvertMonoToStereo(decodedSamples, stereoBuffer, decodedLength);
                WriteAudioDataInternal(stereoBuffer);
            }
            catch (Exception e)
            {
                DecodeErrors++;
                MelonLogger.Error($"Error decoding Opus frame: {e}");

                // Write silence on decode error to maintain timing
                Array.Clear(stereoBuffer, 0, stereoBuffer.Length);
                WriteAudioDataInternal(stereoBuffer);
            }
        }

        private void ConvertMonoToStereo(short[] monoSamples, byte[] stereoBytes, int sampleCount)
        {
            int byteIndex = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = monoSamples[i];
                byte low = (byte)(sample & 0xFF);
                byte high = (byte)((sample >> 8) & 0xFF);

                // Left
                stereoBytes[byteIndex++] = low;
                stereoBytes[byteIndex++] = high;

                // Right (duplicate)
                stereoBytes[byteIndex++] = low;
                stereoBytes[byteIndex++] = high;
            }
        }

        private bool CanWriteFrame()
        {
            if (!channel.hasHandle() || !sound.hasHandle()) return false;

            try
            {
                uint playbackPos = 0;
                channel.getPosition(out playbackPos, TIMEUNIT.PCMBYTES);

                int distance = writePosition - (int)playbackPos;
                if (distance < 0) distance += BUFFER_SIZE;

                // Only write if we have space (keep buffer under 120ms)
                int maxBufferBytes = SAMPLE_RATE * CHANNELS * 2 * 120 / 1000;
                return distance < maxBufferBytes;
            }
            catch
            {
                return false;
            }
        }

        private void UpdateLatencyStats()
        {
            try
            {
                if (!channel.hasHandle() || !sound.hasHandle()) return;

                uint playbackPos = 0;
                channel.getPosition(out playbackPos, TIMEUNIT.PCMBYTES);

                int distance = writePosition - (int)playbackPos;
                if (distance < 0) distance += BUFFER_SIZE;

                // Milliseconds
                int samples = distance / (CHANNELS * 2);
                CurrentLatencyMs = (float)samples / SAMPLE_RATE * 1000f;
            }
            catch { }
        }

        public void QueueOpusPacket(byte[] opusPacket) // Method to be called on data receive
        {
            if (!isInitialized || opusPacket == null || opusPacket.Length == 0) return;
            jitterBuffer.Enqueue(opusPacket);
        }

        private void WriteAudioDataInternal(byte[] data)
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
                    if (len1 > 0) System.Runtime.InteropServices.Marshal.Copy(data, 0, ptr1, (int)len1);

                    if (len2 > 0) System.Runtime.InteropServices.Marshal.Copy(data, (int)len1, ptr2, (int)len2);

                    sound.unlock(ptr1, ptr2, len1, len2);

                    writePosition = (writePosition + data.Length) % (int)length;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error writing audio data: {e}");
            }
        }

        public void ClearBuffer()
        {
            jitterBuffer.Clear();
            if (isPlaying)
            {
                channel.setPaused(true);
                isPlaying = false;
            }
        }

        public void Dispose()
        {
            try
            {
                jitterBuffer.Clear();

                if (channel.hasHandle()) channel.stop();
                if (sound.hasHandle()) sound.release();

                opusDecoder = null;

                isInitialized = false;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Error disposing BBSAudioSource: {e}");
            }
        }
    }
}