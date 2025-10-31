using MelonLoader;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class RuntimeWindow
    {
        public float ScrollbarWidth = 16f;

        public bool IsOpen;
        public bool IsDraggable;

        public bool ShouldDrawOptions;
        public bool ShouldDrawScrollBar;

        public bool ShouldAlwaysDrawScrollBar;

        public bool ShouldDrawBox = true;
        public bool ShouldDrawContentBacker;
        public bool ShouldDrawOptionsBacker;

        public string Label;

        private float _contentHeight;
        private float _optionsHeight;

        private Rect _windowRect = new();
        private Vector2 _scrollPos;

        public Vector2 Position
        {
            get => _windowRect.position;
            set => _windowRect.position = value;
        }

        public Vector2 Size
        {
            get => _windowRect.size;
            set => _windowRect.size = value;
        }

        public RuntimeWindow(string label,
            int id,
            Vector2 defaultPos = new(),
            Vector2 defaultSize = new(),
            bool defaultState = false)
        {
            this.Label = label;
            this.Position = defaultPos;
            this.Size = defaultSize;
            this._contentHeight = this.Size.y;
            this.IsOpen = defaultState;
        }

        /// <summary>
        /// Draws the window and optionally its contents.
        /// Returns true if the window is open.
        /// </summary>
        public bool Draw()
        {
            // Draw contents if shown
            if (IsOpen)
            {
                // Get Render Rects
                GetRects(
                    out Rect windowHeaderRect,
                    out Rect windowContentRect,
                    out Rect windowScrollViewRect,
                    out Rect windowScrollRect,
                    out Rect scrollBarRect);

                // Window
                if (Label == null)
                    Label = string.Empty;
                if (ShouldDrawBox)
                    GUI.Box(_windowRect, Label, StyleManager.Styles.Box);
                GUI.BeginGroup(windowContentRect);

                // Window ScrollView
                if (ShouldDrawContentBacker)
                    GUI.BeginGroup(windowScrollRect, StyleManager.Styles.Box);
                else
                    GUI.BeginGroup(windowScrollRect);
                GUILayout.BeginArea(windowScrollViewRect);

                // Draw Window Content
                DrawContent();

                // Resize ScrollView
                if (Event.current.type == EventType.Repaint)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    _contentHeight = lastRect.y + lastRect.height;
                }

                // End ScrollView
                GUILayout.EndArea();
                GUI.EndGroup();

                // Window Options
                if (ShouldDrawOptions)
                {

                }

                // End Window
                GUI.EndGroup();

                // Window ScrollBar
                if (ShouldDrawScrollBar
                    && (ShouldAlwaysDrawScrollBar || (_contentHeight > windowContentRect.height)))
                    _scrollPos.y = GUI.VerticalScrollbar(scrollBarRect, _scrollPos.y, scrollBarRect.height, 0, _contentHeight, StyleManager.Styles.VerticalScrollBar);
            }
            return IsOpen;
        }

        internal virtual void DrawContent() { }
        internal virtual void DrawOptions() { }

        private void GetRects(
            out Rect windowHeaderRect,
            out Rect windowContentRect,
            out Rect windowScrollViewRect,
            out Rect windowScrollRect,
            out Rect scrollBarRect)
        {
            // Window Header Area
            windowHeaderRect = new Rect(
                _windowRect.x,
                _windowRect.y,
                _windowRect.width,
                25);

            // Window Content Area
            windowContentRect = new Rect(
                windowHeaderRect.x + 10,
                windowHeaderRect.y + windowHeaderRect.height,
                windowHeaderRect.width - 20,
                _windowRect.height - (windowHeaderRect.height + 10));
            if (ShouldDrawOptions)
                windowContentRect.height -= _optionsHeight;

            // Window ScrollView Area
            windowScrollViewRect = new Rect(windowContentRect);
            windowScrollViewRect.position = Vector2.zero;
            windowScrollViewRect.height = _contentHeight;
            if (ShouldDrawScrollBar && (ShouldAlwaysDrawScrollBar || (_contentHeight > windowContentRect.height)))
                windowScrollViewRect.width -= ScrollbarWidth;

            // Window ScrollView Scroll
            windowScrollRect = new Rect(windowScrollViewRect);
            windowScrollRect.y = -_scrollPos.y;
            windowScrollRect.height = _contentHeight;

            // ScrollBar
            scrollBarRect = new Rect(
                ((_windowRect.x + _windowRect.width) - 5) - ScrollbarWidth,
                windowContentRect.y,
                ScrollbarWidth,
                windowContentRect.height);
        }
    }
}
