using BabyStepsMultiplayerClient.Player;
using UnityEngine;
using BabyStepsMultiplayerClient.Localization;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class PlayersTabUI : RuntimeWindow
    {
        private static Vector2 defaultSize = new Vector2(300, 10);

        public PlayersTabUI()
            : base(LanguageManager.GetCurrentLanguage().ConnectedPlayers, 2,
                  new((Screen.width / 2f) - (defaultSize.x / 2), 20),
                  defaultSize, false)
        {
            IsDraggable = false;
            ShouldDrawScrollBar = false;
            ShouldAutoResizeHeight = true;
            MinResizeHeight = defaultSize.y;
            MaxResizeHeight = Screen.height - 20;
        }

        internal override void DrawContent()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            float scale = Screen.height / 1080f;
            Vector2 newPos = Position;
            newPos.x = ((Screen.width / scale) / 2f) - (Size.x / 2);
            Position = newPos;
            MaxResizeHeight = ((Screen.height - newPos.y) - 20);

            int localHeight = 0;
            if ((LocalPlayer.Instance != null)
                && (LocalPlayer.Instance.headBone != null))
                localHeight = (int)(LocalPlayer.Instance.headBone.position.y - 120);

            Label = $"{lang.ConnectedPlayers} [Y:{localHeight}]";

            GUILayout.Space(1);

            if (Core.networkManager.players.Count == 0)
                GUILayout.Label(lang.NoPlayersConnected, StyleManager.Styles.MiddleCenterLabel);
            else
                foreach (var kvp in Core.networkManager.players)
                {
                    RemotePlayer player = kvp.Value;
                    if (player == null)
                        continue;

                    int height = 0;
                    if (player.rootBone != null)
                        height = (int)player.rootBone.position.y - 120;

                    GUILayout.Label($"[Y:{height}] {player.displayName}", StyleManager.Styles.MiddleCenterLabel);
                }

            GUILayout.Space(2);
        }
    }
}