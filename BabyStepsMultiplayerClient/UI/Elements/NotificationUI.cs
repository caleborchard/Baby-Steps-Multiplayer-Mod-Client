using System.Collections.Generic;
using UnityEngine;
using BabyStepsMultiplayerClient.Localization;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class NotificationUI
    {
        private const float REFERENCE_HEIGHT = 1080f;
        private const float BACKDROP_ALPHA = 0.5f; // Transparency of the backdrop

        private const float fadeDuration = 1f; // Seconds to fade out

        private class Message
        {
            public string Text;
            public float TimeAdded;
            public Color Color;
            public float HoldDuration; // Seconds to stay visible before fade out
        }

        private readonly List<Message> messages = new List<Message>();
        private readonly List<Message> messagesToRemove = new List<Message>();
        private readonly List<Message> chatHistory = new List<Message>();
        
        public bool ShowChatHistory { get; set; }
        
        private GUIStyle backdropStyle;

        public NotificationUI() { }

        private static float GetTime()
            => Time.unscaledTime;

        public void AddMessage(string message, float? holdDuration = null, Color? color = null)
        {
            Core.logger.Msg(message);
            float resolvedHoldDuration = holdDuration ?? Mathf.Clamp(3.5f + (message?.Length ?? 0) * 0.075f, 4f, 16f);
            var msg = new Message
            {
                Text = message,
                HoldDuration = resolvedHoldDuration,
                Color = color ?? Color.white,
                TimeAdded = 0f
            };
            messages.Add(msg);
            chatHistory.Add(msg);
        }

        public void DrawUI()
        {
            float scale = Screen.height / REFERENCE_HEIGHT;
            float fontScale = Mathf.Max(scale, 1f);
            int fontSize = Mathf.RoundToInt(16f * fontScale);

            var labelStyle = new GUIStyle(StyleManager.Styles.Label)
            {
                fontSize = fontSize,
                wordWrap = true
            };

            if (backdropStyle == null)
            {
                backdropStyle = new GUIStyle(GUI.skin.box);
                backdropStyle.normal.background = Texture2D.whiteTexture;
            }

            try
            {
                float now = GetTime();
                float yOffset = 10f * fontScale;
                float xPadding = 10f * fontScale;
                float availableWidth = Screen.width - (xPadding * 2f);

                // Draw active notifications
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    var msg = messages[i];

                    if (msg.TimeAdded <= 0f)
                        msg.TimeAdded = now;

                    float age = now - msg.TimeAdded;
                    if (FadeMessage(msg.HoldDuration, age, out float alpha))
                    {
                        messagesToRemove.Add(msg);
                        continue;
                    }

                    float height = labelStyle.CalcHeight(new GUIContent(msg.Text), availableWidth);
                    float width = labelStyle.CalcSize(new GUIContent(msg.Text)).x;
                    float padding = 4f * fontScale;

                    // Draw backdrop sized to text
                    GUI.color = new Color(0.2f, 0.2f, 0.2f, BACKDROP_ALPHA * alpha);
                    GUI.Box(new Rect(xPadding - padding, yOffset - padding, width + (padding * 2), height + (padding * 2)), "", backdropStyle);

                    // Draw text
                    GUI.color = new Color(msg.Color.r, msg.Color.g, msg.Color.b, alpha);
                    GUI.Label(new Rect(xPadding, yOffset, availableWidth, height), msg.Text, labelStyle);

                    yOffset += height + padding + (4f * fontScale);
                }

                // Draw chat history if enabled
                if (ShowChatHistory && chatHistory.Count > 0)
                {
                    // Add some spacing
                    yOffset += 20f * fontScale;
                    
                    float maxHeight = Screen.height - yOffset - 50f; // Leave room at bottom
                    float currentHeight = 0f;
                    int startIndex = chatHistory.Count - 1;

                    // Find where to start drawing from bottom up
                    for (int i = chatHistory.Count - 1; i >= 0; i--)
                    {
                        float height = labelStyle.CalcHeight(new GUIContent(chatHistory[i].Text), availableWidth);
                        if (currentHeight + height + (4f * fontScale) > maxHeight)
                        {
                            startIndex = i + 1;
                            break;
                        }
                        currentHeight += height + (4f * fontScale);
                        if (i == 0)
                            startIndex = 0;
                    }

                    // Draw history messages with full opacity
                    for (int i = startIndex; i < chatHistory.Count; i++)
                    {
                        float height = labelStyle.CalcHeight(new GUIContent(chatHistory[i].Text), availableWidth);
                        float width = labelStyle.CalcSize(new GUIContent(chatHistory[i].Text)).x;
                        float padding = 4f * fontScale;

                        // Draw backdrop sized to text
                        GUI.color = new Color(0.2f, 0.2f, 0.2f, BACKDROP_ALPHA);
                        GUI.Box(new Rect(xPadding - padding, yOffset - padding, width + (padding * 2), height + (padding * 2)), "", backdropStyle);

                        // Draw text
                        GUI.color = Color.white;
                        GUI.Label(new Rect(xPadding, yOffset, availableWidth, height), chatHistory[i].Text, labelStyle);

                        yOffset += height + padding + (4f * fontScale);
                    }
                }
            }
            finally
            {
                GUI.color = Color.white;
            }

            if (messagesToRemove.Count > 0)
            {
                messages.RemoveAll(messagesToRemove.Contains);
                messagesToRemove.Clear();
            }
        }

        private static bool FadeMessage(float HoldDuration, float age, out float alpha)
        {
            if (age < HoldDuration)
                alpha = 1f;
            else
            {
                float timeIntoFade = age - HoldDuration;
                alpha = Mathf.Clamp01(1f - (timeIntoFade / fadeDuration));

                if (alpha <= 0f)
                    return true;
            }
            return false;
        }
    }
}