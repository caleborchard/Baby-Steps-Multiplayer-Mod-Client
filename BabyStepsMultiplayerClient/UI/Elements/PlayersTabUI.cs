using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class PlayersTabUI
    {
        public void DrawUI()
        {
            float panelWidth = 300f;
            float rowHeight = 25f;
            float headerHeight = 30f;
            float margin = 10f;

            int rowCount = Math.Max(1, Core.networkManager.players.Count);
            float panelHeight = headerHeight + rowCount * rowHeight + margin;

            float x = (Screen.width - panelWidth) / 2f;
            float y = 20f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            int localHeight = (int)(LocalPlayer.Instance.headBone.position.y - 120);
            GUI.Box(panelRect, $"Connected Players [Y:{localHeight}]", StyleManager.Styles.Box);

            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + headerHeight, panelRect.width - 20, panelRect.height - headerHeight - margin));

            if (Core.networkManager.players.Count == 0)
            {
                GUILayout.Label("No players connected.", StyleManager.Styles.MiddleCenterLabel);
            }
            else
            {
                foreach (var kvp in Core.networkManager.players)
                {
                    RemotePlayer player = kvp.Value;
                    if (player == null)
                        continue;
                    if (player.rootBone == null)
                        continue;

                    int height = (int)player.rootBone.position.y - 120;
                    GUILayout.Label($"[Y:{height}] { player.displayName }", StyleManager.Styles.MiddleCenterLabel);
                }
            }

            GUILayout.EndArea();
        }
    }
}
