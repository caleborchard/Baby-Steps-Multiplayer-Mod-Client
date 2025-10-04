using MelonLoader;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Config
{
    public class PlayerConfig : ConfigCategory
    {
        internal MelonPreferences_Entry<string> Nickname;
        internal MelonPreferences_Entry<Color> SuitColor;
        internal MelonPreferences_Entry<bool> Collisions;

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
        }
    }
}
