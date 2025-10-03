using UnityEngine;
using System.Text.RegularExpressions;
using BabyStepsMultiplayerClient.Player;

namespace BabyStepsMultiplayerClient.UI
{
    public class ServerConnectUI
    {
        // --- Draw Parameters ---
        public static Rect windowDimensions = new(30, 30, 250, 515); //25 is one label //530
        public static bool dragging = false;
        public static Vector2 dragOffset;
        public static GUIStyle sliderStyle;
        public static GUIStyle thumbStyle;

        // --- Mainline ---
        public void DrawUI()
        {
            if (sliderStyle == null)
            {
                sliderStyle = new(GUI.skin.horizontalSlider);
                sliderStyle.fixedHeight = 20;
            }
            if (thumbStyle == null)
            {
                thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
                thumbStyle.fixedHeight = 20;
                thumbStyle.fixedWidth = 20;
            }

            GUI.Box(windowDimensions, "Server Join Panel", Core.uiManager.boxStyle);

            GUILayout.BeginArea(new Rect(windowDimensions.x + 10, windowDimensions.y + 25, windowDimensions.width - 20, windowDimensions.height - 35));

            GUILayout.Label("Server IP:", Core.uiManager.labelStyle);
            ModSettings.connection.Address.Value = GUILayout.TextField(ModSettings.connection.Address.Value, 32);
            GUILayout.Label("Server Port:", Core.uiManager.labelStyle);

            string newPort = GUILayout.TextField(ModSettings.connection.Port.Value.ToString(), 5);
            if (int.TryParse(newPort, out int customPort))
                ModSettings.connection.Port.Value = customPort;

            GUILayout.Label("Password:", Core.uiManager.labelStyle);
            //uiPassword = GUILayout.TextField(uiPassword, 32);
            ModSettings.connection.Password.Value = GUILayout.PasswordField(ModSettings.connection.Password.Value, '*', 32);

            GUILayout.Space(10);
            GUILayout.Label("Nickname:", Core.uiManager.labelStyle);
            ModSettings.player.Nickname.Value = FilterKeyboardCharacters(GUILayout.TextField(ModSettings.player.Nickname.Value, 20));

            GUI.enabled = !(Core.networkManager.client == null);
            GUILayout.Space(5);
            if (GUILayout.Button((ModSettings.player.Collisions.Value ? "Disable" : "Enable") + " Collisions", Core.uiManager.buttonStyle))
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

            var currentColor = ModSettings.player.SuitColor.Value;
            if (currentColor.a != 1f)
                currentColor.a = 1f;

            GUILayout.Space(5);
            GUILayout.Label("Suit Tint:", Core.uiManager.labelStyle);
            GUILayout.Space(2);
            GUILayout.Label($"Red: {(int)(currentColor.r * 255)}", Core.uiManager.labelStyle);
            currentColor.r = GUILayout.HorizontalSlider(currentColor.r, 0f, 1f, sliderStyle, thumbStyle);
            GUILayout.Label($"Green: {(int)(currentColor.g * 255)}", Core.uiManager.labelStyle);
            currentColor.g = GUILayout.HorizontalSlider(currentColor.g, 0f, 1f, sliderStyle, thumbStyle);
            GUILayout.Label($"Blue: {(int)(currentColor.b * 255)}", Core.uiManager.labelStyle);
            currentColor.b = GUILayout.HorizontalSlider(currentColor.b, 0f, 1f, sliderStyle, thumbStyle);

            GUILayout.Space(5);
            GUI.color = currentColor;
            GUILayout.Label("████████████", Core.uiManager.centeredLabelStyle);
            GUI.color = Color.white;
            ModSettings.player.SuitColor.Value = currentColor;

            GUI.enabled = !(Core.networkManager.client == null);
            GUILayout.Space(5);
            if (GUILayout.Button("Update Appearance and Nickname", Core.uiManager.buttonStyle) && Core.networkManager.client != null)
            {
                SaveConfig();
                Core.networkManager.mainThreadActions.Enqueue(() => {
                    Core.networkManager.SendPlayerInformation();
                    if (LocalPlayer.Instance != null)
                        LocalPlayer.Instance.ApplySuitColor();
                });

            }
            GUI.enabled = true;

            GUILayout.Space(10);
            GUI.enabled = Core.networkManager.client == null;
            if (GUILayout.Button("Connect", Core.uiManager.buttonStyle) && Core.networkManager.client == null)
            {
                Core.logger.Msg($"{ModSettings.player.Nickname.Value}, {ModSettings.connection.Address.Value}:{ModSettings.connection.Port.Value}");
                SaveConfig();
                Core.networkManager.Connect(ModSettings.connection.Address.Value,
                    ModSettings.connection.Port.Value, 
                    ModSettings.connection.Password.Value);
            }
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
            if (GUILayout.Button("Disconnect", Core.uiManager.buttonStyle) && Core.networkManager.client != null)
            {
                Core.networkManager.Disconnect();
            }
            GUI.enabled = true;

            GUILayout.EndArea();

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
