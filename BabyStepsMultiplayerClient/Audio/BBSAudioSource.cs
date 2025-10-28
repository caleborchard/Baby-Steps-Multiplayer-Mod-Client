using Concentus.Enums;
using Concentus.Structs;
using Il2CppFMOD;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FMOD = Il2CppFMOD;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSAudioSource : IDisposable // Experimenting with IDisposable, need to do more research into properly using it.
    {
        public static FMOD.System CoreSystem;

        public Transform TargetTransform { get; private set; }
        public FMOD.Sound Sound { get; private set; }
        public FMOD.Channel Channel { get; private set; }
        public FMOD.ChannelGroup ChannelGroup { get; private set; }

        // PCM streaming buffer (mono, PCM16)
        private readonly CircularBuffer<byte> pcmBuffer = new CircularBuffer<byte>(48000 * 2 * 5); // 5 sec buffer at 48000 mono 16 bit
        private readonly object pcmLock = new object();

        private bool isPlaying;
        private bool initialized;
        private FMOD.VECTOR fmodPos;
        private FMOD.VECTOR fmodVel = new FMOD.VECTOR();
        private FMOD.VECTOR fmodForward = new FMOD.VECTOR();
        private FMOD.VECTOR fmodUp = new FMOD.VECTOR();

        // Audio format
        private const int sampleRate = 48000; // Use 48000 for Opus compat
        private const int channels = 1;

        // Circular audio streaming
        private uint writeOffset = 0;
        private uint soundBufferLength;

        public BBSAudioSource(Transform targetTransform)
        {
            if (!CoreSystem.hasHandle()) CoreSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;
            this.TargetTransform = targetTransform;
        }
        public RESULT Initialize()
        {
            if (!CoreSystem.hasHandle()) return RESULT.ERR_UNINITIALIZED;

            RESULT result = CoreSystem.getMasterChannelGroup(out var tempGroup);
            if (result != RESULT.OK) return result;
            ChannelGroup = tempGroup;

            var exInfo = new FMOD.CREATESOUNDEXINFO();
            exInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            exInfo.numchannels = channels;
            exInfo.defaultfrequency = sampleRate;
            exInfo.format = SOUND_FORMAT.PCM16;
            exInfo.decodebuffersize = sampleRate / 4;
            soundBufferLength = (uint)(sampleRate * channels * 2 * 5); // 5 seconds at 48khz mono 16 bit
            exInfo.length = soundBufferLength;

            result = CoreSystem.createSound(
                IntPtr.Zero,
                MODE.OPENUSER | MODE.LOOP_NORMAL | MODE._3D | MODE._3D_WORLDRELATIVE,
                ref exInfo,
                out var tempSound
            );
            if (result != RESULT.OK) return result;

            Sound = tempSound;
            Sound.set3DMinMaxDistance(0.1f, 50f);

            result = CoreSystem.playSound(Sound, ChannelGroup, true, out var tempChannel);
            if (result != RESULT.OK) return result;

            Channel = tempChannel;
            Channel.setVolume(1.0f);
            Channel.setPaused(false);

            initialized = true;
            isPlaying = true;
            return RESULT.OK;
        }
        public void Update()
        {
            if (!initialized || TargetTransform == null || !Channel.hasHandle()) return;

            Vector3 pos = TargetTransform.position;
            Vector3 fwd = TargetTransform.forward;
            Vector3 up = TargetTransform.up;

            fmodPos.x = pos.x;
            fmodPos.y = pos.y;
            fmodPos.z = pos.z;

            fmodForward.x = fwd.x;
            fmodForward.y = fwd.y;
            fmodForward.z = fwd.z;

            fmodUp.x = up.x;
            fmodUp.y = up.y;
            fmodUp.z = up.z;

            Channel.set3DAttributes(ref fmodPos, ref fmodVel);
            CoreSystem.update();

            FeedFakeSineWave();

            StreamAudio();
        }

        private float testPhase, testTime = 0f;
        private void FeedFakeSineWave()
        {
            const int frameSamples = 2400;
            short[] pcm = new short[frameSamples];
            float freq = 220f + Mathf.Sin(testTime * 0.5f) * 200f;
            float phaseInc = (2f * Mathf.PI * freq) / sampleRate;

            for (int i = 0; i < frameSamples; i++)
            {
                pcm[i] = (short)(Mathf.Sin(testPhase) * short.MaxValue * 0.2f);
                testPhase += phaseInc;
                if (testPhase > 2f * Mathf.PI) testPhase -= 2f * Mathf.PI;
            }

            byte[] pcmBytes = new byte[pcm.Length * 2];
            Buffer.BlockCopy(pcm, 0, pcmBytes, 0, pcmBytes.Length);
            FeedAudioData(pcmBytes);

            testTime += frameSamples / (float)sampleRate;
        }

        public RESULT FeedAudioData(byte[] pcmData)
        {
            if (!initialized) return RESULT.ERR_UNINITIALIZED;
            if (pcmData == null || pcmData.Length == 0) return RESULT.OK;

            lock (pcmLock) pcmBuffer.Write(pcmData, 0, pcmData.Length);
            return RESULT.OK;
        }
        private void StreamAudio()
        {
            if (!Sound.hasHandle()) return;

            const int chunkSize = 480 * 2;
            int availableBytes;
            lock (pcmLock) availableBytes = pcmBuffer.Length;

            if (availableBytes < chunkSize) return; // Not enough data to stream yet

            // Read from circular PCM buffer
            byte[] temp = new byte[chunkSize];
            int bytesRead;
            lock (pcmLock) bytesRead = pcmBuffer.Read(temp, 0, chunkSize);
            if (bytesRead == 0) return;

            // Lock the section of the FMOD sound
            RESULT result = Sound.@lock(writeOffset, (uint)bytesRead, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            if (result != RESULT.OK) return;

            if (len1 > 0) Marshal.Copy(temp, 0, ptr1, (int)len1);

            if (len2 > 0 && len1 < bytesRead) Marshal.Copy(temp, (int)len1, ptr2, (int)len2);

            Sound.unlock(ptr1, ptr2, len1, len2);

            // Advance write cursor circularly
            writeOffset = (writeOffset + (uint)bytesRead) % soundBufferLength;
        }
        public void Dispose()
        {
            if (ChannelGroup.hasHandle()) ChannelGroup.clearHandle();
            if (Channel.hasHandle())
            {
                Channel.stop();
                Channel.clearHandle();
            }
            if (Sound.hasHandle())
            {
                Sound.release();
                Sound.clearHandle();
            }
            initialized = false;
        }

        private class CircularBuffer<T>
        {
            private readonly T[] buffer;
            private int head = 0;
            private int tail = 0;
            private int length = 0;
            private readonly object locker = new object();

            public int Capacity => buffer.Length;
            public int Length { get { lock (locker) { return length; } } }

            public CircularBuffer(int capacity)
            {
                buffer = new T[capacity];
            }
            public int Write(T[] src, int srcIdx, int count)
            {
                lock (locker)
                {
                    int written = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (length >= buffer.Length) break; // full
                        buffer[tail] = src[srcIdx + i];
                        tail = (tail + 1) % buffer.Length;
                        length++; written++;
                    }
                    return written;
                }
            }
            public int Read(T[] dst, int dstIdx, int count)
            {
                lock (locker)
                {
                    int read = 0;
                    for (int i = 0; i < count; i++)
                    {
                        if (length == 0) break; // empty
                        dst[dstIdx + i] = buffer[head];
                        head = (head + 1) % buffer.Length;
                        length--; read++;
                    }
                    return read;
                }
            }
        }
    }
}
