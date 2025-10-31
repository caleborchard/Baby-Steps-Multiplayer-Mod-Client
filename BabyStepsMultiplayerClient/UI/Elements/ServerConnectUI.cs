using UnityEngine;
using System.Text.RegularExpressions;
using BabyStepsMultiplayerClient.Player;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class ServerConnectUI
    {
        // --- Draw Parameters ---
        public static Rect windowDimensions = new(30, 30, 250, 400); //25 is one label //515
        public static bool dragging = false;
        public static Vector2 dragOffset;
        public static bool draggingScrollbar;
        public static Vector2 scrollPos;

        public static RuntimeFoldout serverInfoFoldout = new RuntimeFoldout("Server Information", false);

        public void DrawUI()
        {
            GUI.Box(windowDimensions, "Server Join Panel v" + Core.CLIENT_VERSION, StyleManager.Styles.Box);

            float contentHeight = 515f;

            Rect innerAreaRect = new Rect(windowDimensions.x + 10, windowDimensions.y + 25, windowDimensions.width - 20, windowDimensions.height - 35);
            Rect contentRect = new Rect(0, 0, innerAreaRect.width - 20, contentHeight); // approximate content height

            GUI.BeginGroup(innerAreaRect);

            float scrollbarWidth = 16f;
            float viewHeight = innerAreaRect.height;
            float scrollMax = Mathf.Max(0, contentHeight - viewHeight);

            // --- Detect if mouse is over scrollbar ---
            Rect scrollbarRect = new Rect(innerAreaRect.width - scrollbarWidth, 0, scrollbarWidth, viewHeight);
            bool mouseOverScrollbar = scrollbarRect.Contains(Event.current.mousePosition);

            // Only capture mouse when inside scrollbar area
            if (mouseOverScrollbar || draggingScrollbar)
            {
                float oldScroll = scrollPos.y;
                float newScroll = GUI.VerticalScrollbar(scrollbarRect, oldScroll, viewHeight, 0, contentHeight, StyleManager.Styles.VerticalScrollBar);
                if (Math.Abs(newScroll - oldScroll) > 0.01f)
                    draggingScrollbar = true; // actively dragging

                scrollPos.y = newScroll;

                if (Event.current.type == EventType.MouseUp)
                    draggingScrollbar = false;
            }
            else
            {
                GUI.VerticalScrollbar(scrollbarRect, scrollPos.y, viewHeight, 0, contentHeight, StyleManager.Styles.VerticalScrollBar);
            }

            // --- Scrollable content ---
            GUI.BeginGroup(new Rect(0, -scrollPos.y, contentRect.width, contentHeight));
            GUILayout.BeginArea(new Rect(0, 0, contentRect.width - 5, contentHeight));

            serverInfoFoldout.Draw(() =>
            {
                GUILayout.Label("Server IP:", StyleManager.Styles.Label);
                ModSettings.connection.Address.Value = GUILayout.TextField(ModSettings.connection.Address.Value, 32, StyleManager.Styles.TextField);
                GUILayout.Label("Server Port:", StyleManager.Styles.Label);

                string newPort = GUILayout.TextField(ModSettings.connection.Port.Value.ToString(), 5, StyleManager.Styles.TextField);
                if (int.TryParse(newPort, out int customPort))
                    ModSettings.connection.Port.Value = customPort;

                GUILayout.Label("Password (Optional):", StyleManager.Styles.Label);
                ModSettings.connection.Password.Value = GUILayout.PasswordField(ModSettings.connection.Password.Value, '*', 32, StyleManager.Styles.TextField);
            });

            GUILayout.Space(10);
            GUILayout.Label("Nickname:", StyleManager.Styles.Label);
            ModSettings.player.Nickname.Value = FilterKeyboardCharacters(GUILayout.TextField(ModSettings.player.Nickname.Value, 20, StyleManager.Styles.TextField));

            var currentColor = ModSettings.player.SuitColor.Value;
            if (currentColor.a != 1f)
                currentColor.a = 1f;

            GUILayout.Space(5);
            GUILayout.Label("Suit Tint:", StyleManager.Styles.Label);
            GUILayout.Space(2);
            GUILayout.Label($"Red: {(int)(currentColor.r * 255)}", StyleManager.Styles.Label);
            currentColor.r = GUILayout.HorizontalSlider(currentColor.r, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);
            GUILayout.Label($"Green: {(int)(currentColor.g * 255)}", StyleManager.Styles.Label);
            currentColor.g = GUILayout.HorizontalSlider(currentColor.g, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);
            GUILayout.Label($"Blue: {(int)(currentColor.b * 255)}", StyleManager.Styles.Label);
            currentColor.b = GUILayout.HorizontalSlider(currentColor.b, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);

            GUILayout.Space(5);
            GUI.color = currentColor;
            GUILayout.Label("████████████", StyleManager.Styles.MiddleCenterLabel);
            GUI.color = Color.white;
            ModSettings.player.SuitColor.Value = currentColor;

            GUI.enabled = !(Core.networkManager.client == null);
            GUILayout.Space(5);
            if (GUILayout.Button("Update Name & Appearance", StyleManager.Styles.Button) && Core.networkManager.client != null)
            {
                SaveConfig();
                Core.networkManager.mainThreadActions.Enqueue(() => {
                    Core.networkManager.SendPlayerInformation();
                    if (LocalPlayer.Instance != null)
                        LocalPlayer.Instance.ApplySuitColor();
                });
                Core.uiManager.notificationsUI.AddMessage("Your appearance has been updated");
            }
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
            GUILayout.Space(5);
            if (GUILayout.Button((ModSettings.player.Collisions.Value ? "Disable" : "Enable") + " Collisions", StyleManager.Styles.Button))
            {
                ModSettings.player.Collisions.Value = !ModSettings.player.Collisions.Value;
                Core.networkManager.SendCollisionToggle(ModSettings.player.Collisions.Value);

                foreach (var player in Core.networkManager.players)
                {
                    if (ModSettings.player.Collisions.Value && player.Value.netCollisionsEnabled) player.Value.EnableCollision();
                    else player.Value.DisableCollision();
                }
            }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUI.enabled = Core.networkManager.client == null;
            if (GUILayout.Button("Connect", StyleManager.Styles.Button) && Core.networkManager.client == null)
            {
                Core.logger.Msg($"{ModSettings.player.Nickname.Value}, {ModSettings.connection.Address.Value}:{ModSettings.connection.Port.Value}");
                SaveConfig();
                Core.networkManager.Connect(ModSettings.connection.Address.Value,
                    ModSettings.connection.Port.Value,
                    ModSettings.connection.Password.Value);
            }
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
            if (GUILayout.Button("Disconnect", StyleManager.Styles.Button) && Core.networkManager.client != null)
            {
                Core.networkManager.Disconnect();
            }
            GUI.enabled = true;

            GUILayout.EndArea();
            GUI.EndGroup();
            GUI.EndGroup();

            // --- Only handle drag if not over or dragging the scrollbar ---
            if (!mouseOverScrollbar && !draggingScrollbar)
                HandleDrag();
        }

        private void HandleDrag()
        {
            var mousePos = Event.current.mousePosition;

            if (Event.current.type == EventType.MouseDown &&
                new Rect(windowDimensions.x, windowDimensions.y, windowDimensions.width, 40).Contains(mousePos))
            {
                dragging = true;
                dragOffset = mousePos - windowDimensions.position;
                Event.current.Use();
            }

            if (dragging && Event.current.type == EventType.MouseDrag)
            {
                windowDimensions.position = mousePos - dragOffset;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
                dragging = false;
        }

        // --- Helpers ---
        public void SaveConfig()
        {
            ModSettings.Save();
        }
        public void LoadConfig()
        {
            ModSettings.Load();
        }
        private string FilterKeyboardCharacters(string input)
        {
            return Regex.Replace(input, @"[^\p{L}\p{N}!@#\$%\^&\*\(\)_\+\-=\[\]{};:'"",<.>/?\\|`~ ]", "");
        }
    }
}
