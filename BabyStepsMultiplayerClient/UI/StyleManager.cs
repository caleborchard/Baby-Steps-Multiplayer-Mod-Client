using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    internal static class StyleManager
    {
        internal static class Fonts
        {
            internal static Font Arial { get; private set; }

            internal static void Prepare()
            {
                if (Arial == null)
                    Arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        internal static class Styles
        {
            internal static GUIStyle Label { get; private set; }
            internal static GUIStyle MiddleCenterLabel { get; private set; }

            internal static GUIStyle TextField { get; private set; }
            internal static GUIStyle MiddleLeftTextField { get; private set; }

            internal static GUIStyle Box { get; private set; }
            internal static GUIStyle Button { get; private set; }
            internal static GUIStyle VerticalScrollBar { get; private set; }

            internal static GUIStyle HorizontalSlider { get; private set; }
            internal static GUIStyle HorizontalSliderThumb { get; private set; }

            internal static GUIStyle RuntimeFoldoutButton { get; private set; }


            internal static void Prepare()
            {
                if (Label == null)
                    Label = new GUIStyle(GUI.skin.label)
                    {
                        font = Fonts.Arial,
                        normal = new()
                        {
                            textColor = Color.white
                        }
                    };

                if (MiddleCenterLabel == null)
                    MiddleCenterLabel = new GUIStyle(Label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };

                if (Box == null)
                    Box = new GUIStyle(GUI.skin.box)
                    {
                        font = Fonts.Arial
                    };

                if (Button == null)
                    Button = new GUIStyle(GUI.skin.button)
                    {
                        font = Fonts.Arial
                    };

                if (HorizontalSlider == null)
                    HorizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
                    {
                        font = Fonts.Arial,
                        fixedHeight = 20
                    };
                
                if (HorizontalSliderThumb == null)
                    HorizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
                    {
                        font = Fonts.Arial,
                        fixedHeight = 20,
                        fixedWidth = 20
                    };

                if (VerticalScrollBar == null)
                    VerticalScrollBar = new GUIStyle(GUI.skin.verticalScrollbar)
                    {
                        font = Fonts.Arial
                    };

                if (TextField == null)
                    TextField = new GUIStyle(GUI.skin.textField)
                    {
                        font = Fonts.Arial,
                    };

                if (MiddleLeftTextField == null)
                    MiddleLeftTextField = new GUIStyle(TextField)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };

                if (RuntimeFoldoutButton == null)
                    RuntimeFoldoutButton = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
            }
        }
    }
}
