using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class RuntimeWindow
    {
        public float ScrollbarWidth = 16f;

        public bool IsOpen;
        public bool IsDraggable = true;

        public bool ShouldDrawBox = true;
        public bool ShouldDrawScrollBar = true;
        public bool ShouldDrawContentBacker = true;

        public (bool, bool) ShouldAutoExpandSize = (false, false);

        public string Label;

        private float _contentHeight;

        private Rect _windowRect = new();
        private Vector2 _scrollPos;

        private bool _hasClicked;
        private bool _isDragging;
        private Vector2 _dragOffset;

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

                // Window ScrollBar
                if (ShouldDrawScrollBar
                    && (_contentHeight > windowContentRect.height))
                    _scrollPos.y = GUI.VerticalScrollbar(scrollBarRect, _scrollPos.y, scrollBarRect.height, 0, _contentHeight, StyleManager.Styles.VerticalScrollBar);
                else
                    _scrollPos.y = 0;

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

                // End Window
                GUI.EndGroup();

                // Do Window Drag
                if (IsDraggable)
                    HandleDrag(windowHeaderRect);
            }

            return IsOpen;
        }

        internal virtual void DrawContent() { }

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
                _windowRect.width + 8,
                30);

            // Window Content Area
            windowContentRect = new Rect(
                windowHeaderRect.x + 10,
                windowHeaderRect.y + windowHeaderRect.height,
                windowHeaderRect.width - 28,
                _windowRect.height - (windowHeaderRect.height + 10));
            if (ShouldDrawScrollBar && (_contentHeight > windowContentRect.height))
                windowContentRect.width += 5;

            // Window ScrollView Area
            windowScrollViewRect = new Rect(windowContentRect);
            windowScrollViewRect.position = Vector2.zero;
            windowScrollViewRect.height = _contentHeight;
            if (ShouldDrawScrollBar && (_contentHeight > windowContentRect.height))
                windowScrollViewRect.width -= ScrollbarWidth + 5;

            // Window ScrollView Scroll
            windowScrollRect = new Rect(windowScrollViewRect);
            windowScrollRect.y = -_scrollPos.y;
            windowScrollRect.height = _contentHeight;

            // ScrollBar
            scrollBarRect = new Rect(
                (windowScrollViewRect.width + 5),
                0,
                ScrollbarWidth,
                windowContentRect.height);
        }

        private void HandleDrag(Rect windowHeaderRect)
        {
            Vector2 mousePos = Event.current.mousePosition;
            if (_hasClicked)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    _hasClicked = false;
                    if (_isDragging)
                        Event.current.Use();
                    _isDragging = false;
                }
                if (_isDragging && (Event.current.type == EventType.MouseDown))
                    Event.current.Use();
            }
            else
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    _hasClicked = true;
                    if (windowHeaderRect.Contains(Event.current.mousePosition))
                    {
                        _isDragging = true;
                        _dragOffset = mousePos - Position;
                        Event.current.Use();
                    }
                }
            }

            if (_isDragging && (Event.current.type == EventType.MouseDrag))
            {
                Position = mousePos - _dragOffset;
                Event.current.Use();
            }
        }
    }
}
