using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class RuntimeFoldout
    {
        private bool isExpanded;
        private string label;

        public RuntimeFoldout(string label, bool defaultState = false)
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
            // Draw foldout header with arrow
            string headerLabel = (isExpanded ? "▼ " : "▶ ") + label;
            if (GUILayout.Button(headerLabel, StyleManager.Styles.Button))
            {
                isExpanded = !isExpanded;
            }

            // Draw contents if expanded
            if (isExpanded && contents != null)
            {
                GUILayout.BeginVertical("box");
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
