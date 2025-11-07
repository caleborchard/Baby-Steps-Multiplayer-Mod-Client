using UnityEngine;
namespace BabyStepsMultiplayerClient.UI
{
    public class RuntimeFoldout
    {
        private bool isExpanded;
        private string label;

        public RuntimeFoldout(string label,
            bool defaultState = false)
        {
            this.label = label;
            this.isExpanded = defaultState;
        }

        /// <summary>
        /// Draws the foldout header and optionally its contents.
        /// Returns true if the foldout is expanded.
        /// </summary>
        public bool Draw(System.Action contents)
        {
            // Draw foldout header with arrow on left and centered text
            Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(label), StyleManager.Styles.Button);

            if (GUI.Button(buttonRect, "", StyleManager.Styles.Button))
            {
                isExpanded = !isExpanded;
            }

            // Draw arrow on the left with manual offset
            Rect arrowRect = new Rect(buttonRect.x + 10, buttonRect.y, 20, buttonRect.height);
            GUI.Label(arrowRect, isExpanded ? "▼" : "▶", StyleManager.Styles.RuntimeFoldoutButton);

            // Draw centered label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = StyleManager.Styles.Button.fontStyle,
                fontSize = StyleManager.Styles.Button.fontSize,
                normal = { textColor = StyleManager.Styles.Button.normal.textColor }
            };
            GUI.Label(buttonRect, label, labelStyle);

            // Draw contents if expanded
            if (isExpanded && contents != null)
            {
                GUILayout.BeginVertical(StyleManager.Styles.Box);
                contents.Invoke();
                GUILayout.EndVertical();
            }

            return isExpanded;
        }

        /// <summary>
        /// Optional: allow changing the label at runtime
        /// </summary>
        public void SetLabel(string newLabel)
        {
            label = newLabel;
        }

        /// <summary>
        /// Optional: force open or close
        /// </summary>
        public void SetExpanded(bool expanded)
        {
            isExpanded = expanded;
        }
    }
}