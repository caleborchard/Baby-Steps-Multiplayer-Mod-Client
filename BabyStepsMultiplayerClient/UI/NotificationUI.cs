using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class NotificationUI
    {
        private const float fadeDuration = 1f; // Seconds to fade out
        private const float holdDuration = 3f; // Seconds to stay visible before fade out

        private class Message
        {
            public string Text;
            public float TimeAdded;
        }

        private readonly List<Message> messages = new List<Message>();
        private readonly List<Message> messagesToRemove = new List<Message>();

        public NotificationUI() { }

        private static float GetTime()
            => Time.unscaledTime;

        public void AddMessage(string message)
        {
            Core.logger.Msg(message);
            messages.Add(new Message
            {
                Text = message,
            });
        }

        public void DrawUI()
        {
            float now = GetTime();

            int yOffset = 10;
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                var msg = messages[i];

                if (msg.TimeAdded <= 0f)
                    msg.TimeAdded = now;

                float age = now - msg.TimeAdded;
                if (FadeMessage(age, out float alpha))
                {
                    messagesToRemove.Add(msg);
                    continue;
                }

                Color oldColor = GUI.color;
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);

                GUI.Label(new Rect(10, yOffset, Screen.width, 25), msg.Text, Core.uiManager.labelStyle);

                yOffset += 20;
            }
            GUI.color = Color.white;

            if (messagesToRemove.Count > 0)
            {
                messages.RemoveAll(messagesToRemove.Contains);
                messagesToRemove.Clear();
            }
        }

        private static bool FadeMessage(float age, out float alpha)
        {
            if (age < holdDuration)
                alpha = 1f;
            else
            {
                float fadeOutAge = (age - fadeDuration) - holdDuration;
                alpha = Mathf.Clamp01(1f - (fadeOutAge / fadeDuration));
                if (alpha <= 0f)
                    return true;
            }
            return false;
        }
    }
}