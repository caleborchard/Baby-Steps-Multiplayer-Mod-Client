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
            internal static GUIStyleState WhiteTextColorState { get; private set; }

            internal static GUIStyle Label { get; private set; }
            internal static GUIStyle MiddleCenterLabel { get; private set; }

            internal static GUIStyle TextField { get; private set; }
            internal static GUIStyle MiddleLeftTextField { get; private set; }

            internal static GUIStyle Box { get; private set; }
            internal static GUIStyle Button { get; private set; }

            internal static GUIStyle HorizontalSlider { get; private set; }
            internal static GUIStyle HorizontalSliderThumb { get; private set; }

            internal static void Prepare()
            {
                if (WhiteTextColorState == null)
                    WhiteTextColorState = new()
                    {
                        textColor = Color.white,
                    };

                if (Label == null)
                    Label = new GUIStyle(GUI.skin.label)
                    {
                        font = Fonts.Arial,
                        normal = WhiteTextColorState
                    };

                if (MiddleCenterLabel == null)
                    MiddleCenterLabel = new GUIStyle(Label)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };

                if (Box == null)
                    Box = new GUIStyle(GUI.skin.box)
                    {
                        font = Fonts.Arial,
                        //normal = WhiteTextColorState
                    };

                if (Button == null)
                    Button = new GUIStyle(GUI.skin.button)
                    {
                        font = Fonts.Arial,
                        //normal = WhiteTextColorState
                    };

                if (HorizontalSlider == null)
                    HorizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
                    {
                        font = Fonts.Arial,
                        //normal = WhiteTextColorState,
                        fixedHeight = 20
                    };
                
                if (HorizontalSliderThumb == null)
                    HorizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
                    {
                        font = Fonts.Arial,
                        //normal = WhiteTextColorState,
                        fixedHeight = 20,
                        fixedWidth = 20
                    };

                if (TextField == null)
                    TextField = new GUIStyle(GUI.skin.textField)
                    {
                        font = Fonts.Arial,
                        normal = WhiteTextColorState
                    };

                if (MiddleLeftTextField == null)
                    MiddleLeftTextField = new GUIStyle(TextField)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
            }
        }
    }
}
