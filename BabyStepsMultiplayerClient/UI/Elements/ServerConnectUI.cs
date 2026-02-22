using UnityEngine;
using System.Text.RegularExpressions;
using BabyStepsMultiplayerClient.Player;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class ServerConnectUI : RuntimeWindow
    {
        public RuntimeFoldout serverInfoFoldout = new RuntimeFoldout("Server Information", false);
        public RuntimeFoldout audioSettingsFoldout = new RuntimeFoldout("Audio Settings", false);
        public RuntimeFoldout microphoneDevicesFoldout = new RuntimeFoldout("Microphone Devices", false);
        public RuntimeFoldout generalSettingsFoldout = new RuntimeFoldout("General Settings", false);
        public RuntimeFoldout playerCustomizationFoldout = new RuntimeFoldout("Player Customization", true);

        private string[] availableDevices = new string[0];
        private bool isWaitingForKey = false;

        // Peak meter variables
        private float currentPeak = 0f;
        private float peakDecayRate = 0.95f;

        public ServerConnectUI()
            : base($"Server Join Panel v{Core.CLIENT_VERSION}", 0, new(30, 30), new(250, 400), false)
        {
            ShouldDrawContentBacker = false;
        }

        internal override void DrawContent()
        {
            // Handle key input for push-to-talk keybind (must be before GUI rendering)
            if (isWaitingForKey)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode) && keyCode != KeyCode.None && keyCode != KeyCode.Mouse0)
                    {
                        ModSettings.audio.PushToTalkKey.Value = keyCode.ToString();
                        if (LocalPlayer.Instance != null)
                        {
                            LocalPlayer.Instance.SetPushToTalkKey(keyCode);
                        }
                        isWaitingForKey = false;
                        break;
                    }
                }
            }

            bool isConnected = Core.networkManager.client != null;
            string buttonText = isConnected ? "Disconnect" : "Connect";

            if (GUILayout.Button(buttonText, StyleManager.Styles.Button))
            {
                if (isConnected)
                {
                    Core.networkManager.Disconnect();
                }
                else
                {
                    Core.logger.Msg($"{ModSettings.player.Nickname.Value}, {ModSettings.connection.Address.Value}:{ModSettings.connection.Port.Value}");
                    SaveConfig();
                    Core.networkManager.Connect(
                        ModSettings.connection.Address.Value,
                        ModSettings.connection.Port.Value,
                        ModSettings.connection.Password.Value
                    );
                }
            }

            GUILayout.Space(10);

            serverInfoFoldout.Draw(HandleServerInfo);
            GUILayout.Space(10);
            audioSettingsFoldout.Draw(HandleAudioSettings);
            GUILayout.Space(10);
            generalSettingsFoldout.Draw(HandleGeneralSettings);
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

        private void HandleAudioSettings()
        {
            GUI.enabled = LocalPlayer.Instance != null;

            string buttonText = (ModSettings.audio.MicrophoneEnabled.Value ? "Disable" : "Enable") + " Microphone";

            if (GUILayout.Button(buttonText, StyleManager.Styles.Button))
            {
                ModSettings.audio.MicrophoneEnabled.Value = !ModSettings.audio.MicrophoneEnabled.Value;
                LocalPlayer.Instance?.SetMicrophoneEnabled(ModSettings.audio.MicrophoneEnabled.Value);
            }

            GUILayout.Space(5);

            // Deafen Toggle
            if (GUILayout.Button((ModSettings.audio.Deafened.Value ? "Undeafen" : "Deafen"), StyleManager.Styles.Button))
            {
                ModSettings.audio.Deafened.Value = !ModSettings.audio.Deafened.Value;
            }

            GUILayout.Space(5);

            // Push to Talk Toggle
            if (GUILayout.Button((ModSettings.audio.PushToTalk.Value ? "Disable" : "Enable") + " Push to Talk", StyleManager.Styles.Button))
            {
                ModSettings.audio.PushToTalk.Value = !ModSettings.audio.PushToTalk.Value;
                LocalPlayer.Instance?.SetPushToTalkEnabled(ModSettings.audio.PushToTalk.Value);
            }

            GUILayout.Space(5);

            // Push to Talk Key Binding
            GUI.enabled = LocalPlayer.Instance != null && ModSettings.audio.PushToTalk.Value;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Push to Talk Key:", StyleManager.Styles.Label);

            string keyButtonText = isWaitingForKey ? "Press any key..." : ModSettings.audio.PushToTalkKey.Value;
            if (GUILayout.Button(keyButtonText, StyleManager.Styles.Button, GUILayout.Width(120)))
            {
                isWaitingForKey = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUI.enabled = LocalPlayer.Instance != null;

            // Microphone Gain Slider
            GUILayout.Label($"Microphone Gain: {ModSettings.audio.MicrophoneGain.Value:F2}x", StyleManager.Styles.Label);
            float newGain = GUILayout.HorizontalSlider(
                ModSettings.audio.MicrophoneGain.Value,
                0f,
                3f,
                StyleManager.Styles.HorizontalSlider,
                StyleManager.Styles.HorizontalSliderThumb
            );

            if (newGain != ModSettings.audio.MicrophoneGain.Value)
            {
                ModSettings.audio.MicrophoneGain.Value = newGain;
                LocalPlayer.Instance?.mic.SetGain(newGain);
            }

            GUILayout.Space(5);

            DrawPeakMeter();

            GUILayout.Space(5);

            microphoneDevicesFoldout.Draw(HandleMicrophoneDevices);

            GUI.enabled = true;
        }

        private void DrawPeakMeter()
        {
            bool hasActiveRecording = LocalPlayer.Instance != null &&
                                     LocalPlayer.Instance.mic != null &&
                                     LocalPlayer.Instance.mic.IsRecording();

            if (hasActiveRecording) UpdatePeakLevel();
            else currentPeak = 0f;

            // Meter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Level:", StyleManager.Styles.Label, GUILayout.Width(45));

            Rect meterRect = GUILayoutUtility.GetRect(180, 20, GUILayout.ExpandWidth(false));

            // Background
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.DrawTexture(meterRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            float peakDB = 20f * Mathf.Log10(Mathf.Max(currentPeak, 0.0001f));
            float normalizedPeak = Mathf.Clamp01((peakDB + 60f) / 60f);

            if (normalizedPeak > 0.01f)
            {
                Rect fillRect = new Rect(meterRect.x, meterRect.y, meterRect.width * normalizedPeak, meterRect.height);

                Color meterColor;
                if (normalizedPeak < 0.666f) // Green zone (-60 to -20 dB)
                {
                    meterColor = Color.green;
                }
                else if (normalizedPeak < 0.85f) // Yellow zone (-20 to -9 dB)
                {
                    float yellowBlend = (normalizedPeak - 0.666f) / (0.85f - 0.666f);
                    meterColor = Color.Lerp(Color.green, Color.yellow, yellowBlend);
                }
                else // Red zone (-9 to 0 dB)
                {
                    float redBlend = (normalizedPeak - 0.85f) / (1.0f - 0.85f);
                    meterColor = Color.Lerp(Color.yellow, Color.red, redBlend);
                }

                GUI.color = meterColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            DrawTickMark(meterRect, 0.666f);
            DrawTickMark(meterRect, 0.85f);

            GUILayout.EndHorizontal();

            string dbText = peakDB > -60f ? $"{peakDB:F1} dB" : "-inf dB";
            GUILayout.Label(dbText, StyleManager.Styles.Label);
        }

        private void DrawTickMark(Rect meterRect, float position)
        {
            float xPos = meterRect.x + meterRect.width * position;
            Rect tickRect = new Rect(xPos - 1, meterRect.y, 2, meterRect.height);
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            GUI.DrawTexture(tickRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void UpdatePeakLevel()
        {
            currentPeak *= peakDecayRate;

            var mic = LocalPlayer.Instance?.mic;
            if (mic == null || !mic.IsRecording()) return;

            float newPeak = mic.GetLastFramePeak();

            if (newPeak > currentPeak) currentPeak = newPeak;
        }

        private void HandleMicrophoneDevices()
        {
            if (LocalPlayer.Instance != null && LocalPlayer.Instance.mic != null)
            {
                if (availableDevices.Length < 1) availableDevices = LocalPlayer.Instance.mic.GetAvailableDevices();

                if (availableDevices.Length > 0)
                {
                    ModSettings.audio.SelectedMicrophoneIndex.Value = Mathf.Clamp(
                        ModSettings.audio.SelectedMicrophoneIndex.Value,
                        0,
                        availableDevices.Length - 1
                    );

                    int newIndex = GUILayout.SelectionGrid(
                        ModSettings.audio.SelectedMicrophoneIndex.Value,
                        availableDevices,
                        1,
                        StyleManager.Styles.ButtonLeftCenteredText
                    );

                    if (newIndex != ModSettings.audio.SelectedMicrophoneIndex.Value)
                    {
                        ModSettings.audio.SelectedMicrophoneIndex.Value = newIndex;
                        LocalPlayer.Instance.SetMicrophoneDevice(ModSettings.audio.SelectedMicrophoneIndex.Value);
                    }
                }
            }
        }

        private void HandleGeneralSettings()
        {
            GUI.enabled = !(Core.networkManager.client == null);
            if (GUILayout.Button((ModSettings.player.Collisions.Value ? "Disable" : "Enable") + " Collisions", StyleManager.Styles.Button))
            {
                ModSettings.player.Collisions.Value = !ModSettings.player.Collisions.Value;
                Core.networkManager.SendCollisionToggle(ModSettings.player.Collisions.Value);

                foreach (var player in Core.networkManager.players)
                {
                    bool shouldEnableColliders = ModSettings.player.Collisions.Value && player.Value.netCollisionsEnabled;

                    if (shouldEnableColliders) player.Value.EnableCollision();
                    else player.Value.DisableCollision();
                }
            }
            GUILayout.Space(5);
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
            if (GUILayout.Button((ModSettings.player.CutscenePlayerVisibility.Value ? "Enable" : "Disable") + " Player Cutscene Visibility", StyleManager.Styles.Button))
            {
                ModSettings.player.CutscenePlayerVisibility.Value = !ModSettings.player.CutscenePlayerVisibility.Value;
            }
            GUILayout.Space(5);
            GUI.enabled = true;

            GUI.enabled = !(Core.networkManager.client == null);
            if (GUILayout.Button((ModSettings.player.ShowNametags.Value ? "Disable" : "Enable") + " Nametag Visibility", StyleManager.Styles.Button))
            {
                ModSettings.player.ShowNametags.Value = !ModSettings.player.ShowNametags.Value;
            }
            GUILayout.Space(5);
            GUI.enabled = true;
        }

        private void HandlePlayerCustomization()
        {
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