using Il2CppFMOD;
using FMOD = Il2CppFMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace BabyStepsMultiplayerClient.Audio
{
    public class BBSAudioSource
    {
        public static FMOD.System CoreSystem;

        public Transform TargetTransform { get; private set; }
        public FMOD.Sound Sound { get; private set; }
        public FMOD.Channel Channel { get; private set; }
        public FMOD.ChannelGroup ChannelGroup { get; private set; }

        private bool isPlaying;
        private bool initialized;
        private FMOD.VECTOR fmodPos;
        private FMOD.VECTOR fmodVel = new FMOD.VECTOR();
        private FMOD.VECTOR fmodForward = new FMOD.VECTOR();
        private FMOD.VECTOR fmodUp = new FMOD.VECTOR();

        // Placeholder streaming data for testing
        private byte[] placeholderPCMData;
        private const int sampleRate = 44100;
        private const float toneFrequency = 440f;
        private const float durationSeconds = 2f;

        public BBSAudioSource(Transform targetTransform)
        {
            if (!CoreSystem.hasHandle()) CoreSystem = Il2CppBabySteps.Core.Audio.Services.Player.system;

            this.TargetTransform = targetTransform;
        }
        public RESULT Initialize(byte[] initialBuffer = null)
        {
            if (!CoreSystem.hasHandle()) return RESULT.ERR_UNINITIALIZED;

            ChannelGroup tempGroup = new ChannelGroup();
            RESULT result = CoreSystem.getMasterChannelGroup(out tempGroup);
            if (result != RESULT.OK) return result;
            ChannelGroup = tempGroup;

            // Generate placeholder audio data
            placeholderPCMData = GenerateSineWave(toneFrequency, durationSeconds);

            for (int i = 0; i < 15; i++) MelonLogger.Msg(placeholderPCMData[i]);

            var exInfo = new FMOD.CREATESOUNDEXINFO();
            exInfo.cbsize = (int)System.Runtime.InteropServices.Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
            exInfo.length = (uint)placeholderPCMData.Length;
            exInfo.numchannels = 1;
            exInfo.defaultfrequency = sampleRate;
            exInfo.format = SOUND_FORMAT.PCM16;

            // Placeholder 3D sound object (to be replaced with a PCM callback version)
            result = CoreSystem.createSound(
                placeholderPCMData,
                MODE.OPENMEMORY | MODE.OPENRAW | MODE.LOOP_NORMAL | MODE._3D,
                ref exInfo,
                out var tempSound
            );
            if (result != RESULT.OK) return result;

            Sound = tempSound;
            Sound.set3DMinMaxDistance(0.5f, 50f);

            result = CoreSystem.playSound(Sound, ChannelGroup, true, out var tempChannel);
            if (result != RESULT.OK) return result;

            Channel = tempChannel;
            Channel.setVolume(1.0f);
            Channel.setPaused(false);

            initialized = true;
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
        }
        public RESULT FeedAudioData(byte[] pcmData)
        {
            if (!initialized || Sound.hasHandle()) return RESULT.ERR_UNINITIALIZED;

            // TODO: PCM streaming integration. Can use FMOD.Sound.lock/unlock if writing directly to stream buffer apparently

            return RESULT.OK;
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

        // Placeholder helper function to generate solid tone dummy data
        private byte[] GenerateSineWave(float frequency, float duration)
        {
            int sampleCount = (int)(sampleRate * duration);
            short[] samples = new short[sampleCount];

            double increment = (Math.PI * 2.0 * frequency) / sampleRate;
            double phase = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (short)(Math.Sin(phase) * short.MaxValue);
                phase += increment;
            }

            byte[] pcmBytes = new byte[sampleCount * 2];
            Buffer.BlockCopy(samples, 0, pcmBytes, 0, pcmBytes.Length);
            return pcmBytes;
        }
    }
}
