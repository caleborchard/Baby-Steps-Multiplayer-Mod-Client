using System.Collections.Generic;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI.Elements
{
    public class NotificationUI
    {
        private const float REFERENCE_HEIGHT = 1080f;

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

        public NotificationUI() { }

        private static float GetTime()
            => Time.unscaledTime;

        public void AddMessage(string message, float? holdDuration = null, Color? color = null)
        {
            Core.logger.Msg(message);
            float resolvedHoldDuration = holdDuration ?? Mathf.Clamp(3.5f + (message?.Length ?? 0) * 0.075f, 4f, 16f);
            messages.Add(new Message
            {
                Text = message,
                HoldDuration = resolvedHoldDuration,
                Color = color ?? Color.white
            });
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

            try
            {
                float now = GetTime();
                float yOffset = 10f * fontScale;
                float xPadding = 10f * fontScale;
                float availableWidth = Screen.width - (xPadding * 2f);

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

                    GUI.color = new Color(msg.Color.r, msg.Color.g, msg.Color.b, alpha);

                    float height = labelStyle.CalcHeight(new GUIContent(msg.Text), availableWidth);
                    GUI.Label(new Rect(xPadding, yOffset, availableWidth, height), msg.Text, labelStyle);

                    yOffset += height + (4f * fontScale);
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