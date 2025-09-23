using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient
{
    public class PlayersTabUI
    {
        private Core _core;

        public PlayersTabUI(Core core)
        {
            _core = core;
        }
        public void DrawUI()
        {
            float panelWidth = 300f;
            float rowHeight = 25f;
            float headerHeight = 30f;
            float margin = 10f;

            int rowCount = Math.Max(1, _core.players.Count);
            float panelHeight = headerHeight + (rowCount * rowHeight) + margin;

            float x = (Screen.width - panelWidth) / 2f;
            float y = 20f;
            Rect panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.Box(panelRect, "Connected Players");

            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + headerHeight, panelRect.width - 20, panelRect.height - headerHeight - margin));

            GUIStyle centeredLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            if (_core.players.Count == 0)
            {
                GUILayout.Label("No players connected.", centeredLabel);
            }
            else
            {
                foreach (var kvp in _core.players)
                {
                    NateMP player = kvp.Value;
                    GUILayout.Label(player.displayName, centeredLabel);
                }
            }

            GUILayout.EndArea();
        }
    }
}
