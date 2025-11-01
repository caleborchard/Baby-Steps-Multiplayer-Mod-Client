using UnityEngine;
using System.Text.RegularExpressions;
using BabyStepsMultiplayerClient.Player;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class ServerConnectUI : RuntimeWindow
    {
        public RuntimeFoldout serverInfoFoldout = new RuntimeFoldout("Server Information", false);
        public RuntimeFoldout playerCustomizationFoldout = new RuntimeFoldout("Player Customization", true);

        public ServerConnectUI()
            : base($"Server Join Panel v{Core.CLIENT_VERSION}", 0, new(30, 30), new(250, 400), false)
        {
            ShouldDrawContentBacker = false;
        }

        internal override void DrawContent()
        {
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
            GUILayout.Space(10);
            GUI.enabled = true;

            serverInfoFoldout.Draw(HandleServerInfo);
            GUILayout.Space(10);
            playerCustomizationFoldout.Draw(HandlePlayerCustomization);
        }

        private void HandleServerInfo()
        {
            GUILayout.Label("Server IP:", StyleManager.Styles.Label);
            ModSettings.connection.Address.Value = GUILayout.TextField(ModSettings.connection.Address.Value, 32, StyleManager.Styles.TextField);
            GUILayout.Label("Server Port:", StyleManager.Styles.Label);

            string newPort = GUILayout.TextField(ModSettings.connection.Port.Value.ToString(), 5, StyleManager.Styles.TextField);
            if (int.TryParse(newPort, out int customPort))
                ModSettings.connection.Port.Value = customPort;

            GUILayout.Label("Password (Optional):", StyleManager.Styles.Label);
            ModSettings.connection.Password.Value = GUILayout.PasswordField(ModSettings.connection.Password.Value, '*', 32, StyleManager.Styles.TextField);

            GUILayout.Space(5);
        }

        private void HandlePlayerCustomization()
        {
            GUI.enabled = !(Core.networkManager.client == null);
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
            GUILayout.Space(5);
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
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
            GUILayout.Space(5);
            GUI.enabled = true;

            GUILayout.Label("Nickname:", StyleManager.Styles.Label);
            ModSettings.player.Nickname.Value = FilterKeyboardCharacters(GUILayout.TextField(ModSettings.player.Nickname.Value, 20, StyleManager.Styles.TextField));

            var currentColor = ModSettings.player.SuitColor.Value;
            if (currentColor.a != 1f)
                currentColor.a = 1f;

            GUILayout.Space(5);

            // Row: "Suit Tint:" and color preview block
            GUILayout.BeginHorizontal();
            GUILayout.Label("Suit Tint:", StyleManager.Styles.Label, GUILayout.Width(80));

            // Draw the color block on the same line
            GUI.color = currentColor;
            GUILayout.Label("████████", StyleManager.Styles.MiddleCenterLabel, GUILayout.Width(80));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            GUILayout.Label($"Red: {(int)(currentColor.r * 255)}", StyleManager.Styles.Label);
            currentColor.r = GUILayout.HorizontalSlider(currentColor.r, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);

            GUILayout.Label($"Green: {(int)(currentColor.g * 255)}", StyleManager.Styles.Label);
            currentColor.g = GUILayout.HorizontalSlider(currentColor.g, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);

            GUILayout.Label($"Blue: {(int)(currentColor.b * 255)}", StyleManager.Styles.Label);
            currentColor.b = GUILayout.HorizontalSlider(currentColor.b, 0f, 1f, StyleManager.Styles.HorizontalSlider, StyleManager.Styles.HorizontalSliderThumb);

            GUILayout.Space(5);
            ModSettings.player.SuitColor.Value = currentColor;

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
