using MelonLoader;
using UnityEngine;
using System.Text.RegularExpressions;

namespace BabyStepsMultiplayerClient
{
    public class ServerConnectUI
    {
        // --- Draw Parameters ---
        public static readonly string configPath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "BabyStepsClientConfig.cfg");
        public static Rect windowDimensions = new(100, 100, 250, 550); //25 is one label //530
        public static bool dragging = false;
        public static Vector2 dragOffset;
        public static GUIStyle centeredLabel;
        public static GUIStyle sliderStyle;
        public static GUIStyle thumbStyle;

        // --- UI Inputs ---
        public string uiNNTB;
        public string uiIP;
        public string uiPORT;
        public string uiPassword;
        public float uiColorR, uiColorG, uiColorB;
        public bool uiCollisionsEnabled = true;

        private Core _core;

        // --- Mainline ---
        public ServerConnectUI(Core core)
        {
            uiNNTB = "Nate";
            uiIP = "127.0.0.1";
            uiPORT = "7777";
            uiPassword = "";
            uiColorR = 1f; uiColorG = 1f; uiColorB = 1f;
            _core = core;
        }
        public void DrawUI()
        {
            if (centeredLabel == null) centeredLabel = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            if (sliderStyle == null) sliderStyle = new(GUI.skin.horizontalSlider);
            if (thumbStyle == null) thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);

            sliderStyle.fixedHeight = 20;
            thumbStyle.fixedHeight = 20;
            thumbStyle.fixedWidth = 20;

            GUI.Box(windowDimensions, "Server Join Panel");

            GUILayout.BeginArea(new Rect(windowDimensions.x + 10, windowDimensions.y + 25, windowDimensions.width - 20, windowDimensions.height - 35));

            GUILayout.Label("Server IP:");
            uiIP = GUILayout.TextField(uiIP, 32);
            GUILayout.Label("Server Port:");
            uiPORT = GUILayout.TextField(uiPORT, 5);
            GUILayout.Label("Password:");
            uiPassword = GUILayout.TextField(uiPassword, 5);

            GUILayout.Space(10);
            GUILayout.Label("Nickname:");
            uiNNTB = FilterKeyboardCharacters(GUILayout.TextField(uiNNTB, 20));

            GUI.enabled = !(_core.client == null);
            if (GUILayout.Button((uiCollisionsEnabled ? "Disable" : "Enable") + " Collisions"))
            {
                uiCollisionsEnabled = !uiCollisionsEnabled;
                _core.SendCollisionToggle(uiCollisionsEnabled);

                foreach (var player in _core.players)
                {
                    if (uiCollisionsEnabled && player.Value.netCollisionsEnabled) player.Value.EnableCollision();
                    else player.Value.DisableCollision();
                }
            }
            GUI.enabled = true;

            GUILayout.Label("Suit Tint:");
            GUILayout.Label($"Red: {(int)(uiColorR * 255)}");
            uiColorR = GUILayout.HorizontalSlider(uiColorR, 0f, 1f, sliderStyle, thumbStyle);
            GUILayout.Label($"Green: {(int)(uiColorG * 255)}");
            uiColorG = GUILayout.HorizontalSlider(uiColorG, 0f, 1f, sliderStyle, thumbStyle);
            GUILayout.Label($"Blue: {(int)(uiColorB * 255)}");
            uiColorB = GUILayout.HorizontalSlider(uiColorB, 0f, 1f, sliderStyle, thumbStyle);

            GUI.color = new Color(uiColorR, uiColorG, uiColorB);
            GUILayout.Label("████████████", centeredLabel);
            GUI.color = Color.white;

            GUILayout.Space(10);
            GUI.enabled = (_core.client == null);
            if (GUILayout.Button("Connect"))
            {
                if (_core.client == null)
                {
                    MelonLogger.Msg($"{uiNNTB}, {uiIP}:{uiPORT}");
                    Core.baseColor = new Color(uiColorR, uiColorG, uiColorB);
                    SaveConfig();
                    _core.connectToServer(uiIP, int.Parse(uiPORT), uiPassword);
                }
            }

            GUI.enabled = !(_core.client == null);
            if (GUILayout.Button("Update Appearance and Nickname") && _core.client != null)
            {
                SaveConfig();
                Core.mainThreadActions.Enqueue(_core.UpdateNicknameAndColor);
            }
            GUI.enabled = true;

            GUI.enabled = !(_core.client == null);
            if (GUILayout.Button("Disconnect") && _core.client != null)
            {
                _core.Disconnect();
            }
            GUI.enabled = true;

            GUILayout.EndArea();
            HandleDrag();
        }
        private void HandleDrag()
        {
            var mousePos = Event.current.mousePosition;

            if (Event.current.type == EventType.MouseDown &&
                new Rect(windowDimensions.x, windowDimensions.y, windowDimensions.width, 20).Contains(mousePos))
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
            try
            {
                string[] lines = {
                    uiIP,
                    uiPORT,
                    uiNNTB,
                    uiColorR.ToString(),
                    uiColorG.ToString(),
                    uiColorB.ToString()
                };
                File.WriteAllLines(configPath, lines);
                MelonLogger.Msg("Saved config.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to save config: {ex.Message}");
            }
        }
        public void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) return;

                string[] lines = File.ReadAllLines(configPath);
                if (lines.Length >= 6)
                {
                    uiIP = lines[0];
                    uiPORT = lines[1];
                    uiNNTB = lines[2];
                    float.TryParse(lines[3], out uiColorR);
                    float.TryParse(lines[4], out uiColorG);
                    float.TryParse(lines[5], out uiColorB);
                    MelonLogger.Msg("Loaded config.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to load config: {ex.Message}");
            }
        }
        private string FilterKeyboardCharacters(string input)
        {
            return Regex.Replace(input, @"[^\p{L}\p{N}!@#\$%\^&\*\(\)_\+\-=\[\]{};:'"",<.>/?\\|`~ ]", "");
        }
    }
}
