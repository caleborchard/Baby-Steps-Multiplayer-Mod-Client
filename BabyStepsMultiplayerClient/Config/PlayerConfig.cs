using MelonLoader;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Config
{
    public class PlayerConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<string> Nickname;
        internal MelonPreferences_Entry<Color> SuitColor;
        internal MelonPreferences_Entry<bool> Collisions;
        internal MelonPreferences_Entry<bool> CutscenePlayerVisibility;
        internal MelonPreferences_Entry<bool> ShowNametags;
        internal MelonPreferences_Entry<string> Language;

        public override string ID
            => "Player";

        public override void CreatePreferences()
        {
            Nickname = CreatePref("Nickname",
                "Nickname",
                "User Nickname",
                "Nate");

            SuitColor = CreatePref("SuitColor",
                "Suit Color",
                "Suit Color",
                new Color(1, 1, 1, 1));

            Collisions = CreatePref("PlayerCollisions",
                "Player Collisions",
                "Collisions with Other Players",
                true);

            CutscenePlayerVisibility = CreatePref("CutscenePlayerVisibility",
                "Cutscene Player Visibility",
                "Show Other Players During Cutscenes",
                false);

            ShowNametags = CreatePref("ShowNametags",
                "Show Nametags",
                "Show Nametags Above Other Players",
                true);

            Language = CreatePref("Language",
                "Language",
                "UI Language",
                "English");
        }
    }
}
