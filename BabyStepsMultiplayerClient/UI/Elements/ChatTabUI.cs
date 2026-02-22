using System;
using UnityEngine;
using BabyStepsMultiplayerClient.Localization;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class ChatTabUI
    {
        private string message = "";
        private Rect textFieldRect;
        private GUIStyle textFieldStyle;

        public void DrawUI()
        {
            if (textFieldStyle == null)
                textFieldStyle = new GUIStyle(StyleManager.Styles.MiddleLeftTextField);
            textFieldStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f);

            // Show chat history when chat tab is opened
            Core.uiManager.notificationsUI.ShowChatHistory = true;

            // Do this before everything else to prevent inputs not being detected sometimes
            HandleChatInput();

            // Change size and position based on resolution
            float width = Screen.width * 0.4f;
            float height = Screen.height * 0.05f;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - height - 20;
            textFieldRect = new Rect(x, y, width, height);

            GUILayout.BeginArea(textFieldRect);
            GUI.SetNextControlName("ChatInput");

            message = GUILayout.TextField(message, 150, textFieldStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();

            // Keep focus on the text box
            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "ChatInput")
                GUI.FocusControl("ChatInput");
        }

        private void HandleChatInput()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    SendCurrentMessage();
                    Core.uiManager.showChatTab = false;
                    e.Use(); // Consume event so game doesn't see it
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Core.uiManager.showChatTab = false;
                    e.Use();
                }
            }
        }

        public void SendCurrentMessage()
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Core.networkManager.SendChatMessage(message);

            var lang = LanguageManager.GetCurrentLanguage();
            Core.uiManager.notificationsUI.AddMessage($"{lang.You}: {message}");
            message = "";
        }
    }
}