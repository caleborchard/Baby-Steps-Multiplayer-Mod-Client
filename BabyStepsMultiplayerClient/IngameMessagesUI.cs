using System;
using System.Collections.Generic;
using UnityEngine;

namespace BabyStepsMultiplayerClient
{
    public class IngameMessagesUI
    {
        private class Message
        {
            public string Text;
            public float TimeAdded;
        }

        private readonly List<Message> messages = new List<Message>();
        private readonly float fadeDuration = 3f;

        public IngameMessagesUI() { }

        public void AddMessage(string message)
        {
            messages.Add(new Message
            {
                Text = message,
                TimeAdded = Time.time
            });
        }

        public void DrawUI()
        {
            float now = Time.time;
            int yOffset = 10;

            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var msg = messages[i];
                float age = now - msg.TimeAdded;

                float alpha = Mathf.Clamp01(1f - (age / fadeDuration));

                if (alpha <= 0f)
                {
                    messages.RemoveAt(i);
                    continue;
                }

                Color oldColor = GUI.color;
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);

                GUI.Label(new Rect(10, yOffset, Screen.width, 25), msg.Text);

                yOffset += 20;
            }

            GUI.color = Color.white;
        }
    }
}