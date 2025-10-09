using System;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class ChatTabUI
    {
        private string message = "";
        private bool isTyping = false;
        private Rect textFieldRect;

        public void DrawUI()
        {
            // Only draw when chat tab is open
            if (!Core.uiManager.showChatTab)
            {
                if (isTyping)
                {
                    // restore game control state
                    isTyping = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    // optional: re-enable player input if you disabled it elsewhere
                }
                return;
            }

            // When chat opens
            if (!isTyping)
            {
                isTyping = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Responsive layout
            float width = Screen.width * 0.4f;
            float height = Screen.height * 0.05f;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - height - 20;
            textFieldRect = new Rect(x, y, width, height);

            // Draw TextField
            GUILayout.BeginArea(textFieldRect);
            GUI.SetNextControlName("ChatInput");

            GUIStyle style = new GUIStyle(GUI.skin.textField)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.02f),
                alignment = TextAnchor.MiddleLeft
            };

            message = GUILayout.TextField(message, style, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();

            // Keep focus on the text box
            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "ChatInput")
                GUI.FocusControl("ChatInput");

            HandleChatInput();
        }

        private void HandleChatInput()
        {
            // Suppress game input while typing
            // (depends on how your mod handles player input)
            // Typically you'd set a global "isChatTyping" flag that your movement scripts check

            // Send message with Enter
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                SendCurrentMessage();
                Core.uiManager.showChatTab = false;
                Event.current.Use(); // consume event so game doesn’t see it
            }

            // Close chat with Escape
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Core.uiManager.showChatTab = false;
                Event.current.Use();
            }
        }

        public void SendCurrentMessage()
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Core.networkManager.SendChatMessage(message);
            message = "";
        }
    }
}