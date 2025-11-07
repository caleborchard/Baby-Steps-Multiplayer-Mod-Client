using MelonLoader;
using UnityEngine;
namespace BabyStepsMultiplayerClient.Config
{
    public class AudioConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<bool> MicrophoneEnabled;
        internal MelonPreferences_Entry<float> MicrophoneGain;
        internal MelonPreferences_Entry<int> SelectedMicrophoneIndex;
        internal MelonPreferences_Entry<bool> PushToTalk;
        internal MelonPreferences_Entry<string> PushToTalkKey;
        internal MelonPreferences_Entry<bool> Deafened;

        public override string ID
            => "Audio";

        public override void CreatePreferences()
        {
            MicrophoneEnabled = CreatePref("MicrophoneEnabled",
                "Microphone Enabled",
                "Enable or disable microphone input",
                false);

            MicrophoneGain = CreatePref("MicrophoneGain",
                "Microphone Gain",
                "Microphone input gain/volume multiplier",
                1.0f);

            SelectedMicrophoneIndex = CreatePref("SelectedMicrophoneIndex",
                "Selected Microphone Index",
                "Index of the currently selected microphone device",
                0);

            PushToTalk = CreatePref("PushToTalk",
                "Push to Talk",
                "Enable push-to-talk mode for microphone",
                false);

            PushToTalkKey = CreatePref("PushToTalkKey",
                "Push to Talk Key",
                "Key to press for push-to-talk",
                "V");

            Deafened = CreatePref("Deafened",
                "Deafened",
                "Mute all incoming voice chat",
                false);
        }
    }
}