using System;
using UnityEngine;
using BabyStepsMultiplayerClient.Localization;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class ChatTabUI
    {
        public float FakeCursorHeightMultiplier { get; set; } = 0.7f;

        private string message = "";
        private Rect textFieldRect;
        private GUIStyle textFieldStyle;
        private Texture2D backgroundTexture;

        public void DrawUI()
        {
            if (textFieldStyle == null)
                textFieldStyle = new GUIStyle(StyleManager.Styles.MiddleLeftTextField);
            textFieldStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f);
            textFieldStyle.wordWrap = false;

            if (backgroundTexture == null)
            {
                backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
                backgroundTexture.Apply();
            }

            Core.uiManager.notificationsUI.ShowChatHistory = true;

            HandleChatInput();

            float width = Screen.width * 0.4f;
            float height = Screen.height * 0.05f;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - height - 20;
            textFieldRect = new Rect(x, y, width, height);

            float overlayStartY = y - 20;
            float overlayHeight = Screen.height - overlayStartY;
            GUI.DrawTexture(new Rect(0, overlayStartY, Screen.width, overlayHeight), backgroundTexture);

            float innerWidth = textFieldRect.width - textFieldStyle.padding.horizontal;
            float cursorWidth = Mathf.Max(1f, textFieldStyle.fontSize * 0.08f);
            string visibleMessage = GetVisibleTail(message, Mathf.Max(4f, innerWidth - cursorWidth - 2f));

            GUI.TextField(textFieldRect, visibleMessage, 150, textFieldStyle);
            DrawFakeCursor(visibleMessage, cursorWidth);

            if (Event.current.type == EventType.Repaint)
                GUI.FocusControl(string.Empty);
        }

        private void HandleChatInput()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.keyCode == KeyCode.A)
                {
                    e.Use();
                    return;
                }

                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    SendCurrentMessage();
                    Core.uiManager.showChatTab = false;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Core.uiManager.showChatTab = false;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Backspace)
                {
                    if (message.Length > 0)
                        message = message.Substring(0, message.Length - 1);
                    e.Use();
                }
                else if (!char.IsControl(e.character) && message.Length < 150)
                {
                    message += e.character;
                    e.Use();
                }
            }
        }

        private string GetVisibleTail(string fullText, float maxWidth)
        {
            if (string.IsNullOrEmpty(fullText))
                return string.Empty;

            int startIndex = fullText.Length - 1;
            for (int i = fullText.Length - 1; i >= 0; i--)
            {
                string candidate = fullText.Substring(i);
                float width = textFieldStyle.CalcSize(new GUIContent(candidate)).x;
                if (width <= maxWidth)
                    startIndex = i;
                else
                    break;
            }

            return fullText.Substring(startIndex);
        }

        private void DrawFakeCursor(string visibleMessage, float cursorWidth)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (Mathf.Repeat(Time.unscaledTime, 1f) >= 0.5f)
                return;

            float availableHeight = textFieldRect.height - textFieldStyle.padding.vertical;
            float cursorHeight = Mathf.Clamp(availableHeight * FakeCursorHeightMultiplier, 4f, availableHeight);

            float innerLeft = textFieldRect.x + textFieldStyle.padding.left;
            float innerRight = textFieldRect.xMax - textFieldStyle.padding.right;
            float innerTop = textFieldRect.y + textFieldStyle.padding.top;

            float visibleTextWidth = textFieldStyle.CalcSize(new GUIContent(visibleMessage)).x;
            float cursorX = Mathf.Min(innerLeft + visibleTextWidth - 5f, innerRight - cursorWidth);
            float cursorY = innerTop + ((availableHeight - cursorHeight) * 0.5f);

            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cursorX, cursorY, cursorWidth, cursorHeight), Texture2D.whiteTexture);
            GUI.color = prev;
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