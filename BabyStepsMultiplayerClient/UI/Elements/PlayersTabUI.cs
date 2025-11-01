using BabyStepsMultiplayerClient.Player;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class PlayersTabUI : RuntimeWindow
    {
        private static Vector2 defaultSize = new Vector2(300, 100);

        public PlayersTabUI()
            : base("Connected Players", 2, 
                  new(((Screen.width / 2f) - defaultSize.x), 20), 
                  defaultSize, false)
        {
            ShouldDrawScrollBar = false;
            ShouldAutoResizeHeight = true;
            MaxResizeHeight = Screen.height - 20;
        }

        internal override void DrawContent()
        {
            Vector2 newPos = Position;
            newPos.x = (Screen.width / 2f) - Size.x;
            Position = newPos;

            MaxResizeHeight = ((Screen.height - newPos.y) - 20);

            int localHeight = 0;
            if ((LocalPlayer.Instance != null)
                && (LocalPlayer.Instance.headBone != null))
                localHeight = (int)(LocalPlayer.Instance.headBone.position.y - 120);
            Label = $"Connected Players [Y:{localHeight}]";

            GUILayout.Space(1);
            if (Core.networkManager.players.Count == 0)
                GUILayout.Label("No players connected.", StyleManager.Styles.MiddleCenterLabel);
            else
                foreach (var kvp in Core.networkManager.players)
                {
                    RemotePlayer player = kvp.Value;
                    if (player == null)
                        continue;

                    int height = 0;
                    if (player.rootBone != null)
                        height = (int)player.rootBone.position.y - 120;
                    GUILayout.Label($"[Y:{height}] { player.displayName }", StyleManager.Styles.MiddleCenterLabel);
                }
            GUILayout.Space(2);
        }
    }
}
