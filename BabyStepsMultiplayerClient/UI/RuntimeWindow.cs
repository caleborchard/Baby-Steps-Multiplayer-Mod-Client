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

        public bool ShouldAutoResizeHeight;
        public float MaxResizeHeight;

        public string Label;

        private float _contentHeight;

        private Rect _windowRect = new();
        private Vector2 _scrollPos;

        private Rect _windowHeaderRect = new();
        private Rect _windowContentRect = new();
        private Rect _windowScrollViewRect = new();
        private Rect _windowScrollRect = new();
        private Rect _scrollBarRect = new();

        private bool _hasClicked;
        private bool _isDragging;
        private float _dragOffset_x;
        private float _dragOffset_y;

        public float Pos_X
        {
            get => _windowRect.x;
            set => _windowRect.x = value;
        }

        public float Pos_Y
        {
            get => _windowRect.y;
            set => _windowRect.y = value;
        }

        public float Width
        {
            get => _windowRect.width;
            set => _windowRect.width = value;
        }

        public float Height
        {
            get => _windowRect.height;
            set => _windowRect.height = value;
        }

        public RuntimeWindow(string label,
            int id,
            Vector2 defaultPos = new(),
            Vector2 defaultSize = new(),
            bool defaultState = false)
        {
            this.Label = label;
            this.Pos_X = defaultPos.x;
            this.Pos_Y = defaultPos.y;
            this.Width = defaultSize.x;
            this.Height = defaultSize.y;
            this._contentHeight = defaultSize.y;
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
                // Calculate Render Bounds
                RecalculateBounds();

                // Window
                if (Label == null)
                    Label = string.Empty;
                if (ShouldDrawBox)
                    GUI.Box(_windowRect, Label, StyleManager.Styles.Box);

                // Window Inner Box Group
                GUI.BeginGroup(_windowContentRect);

                // Window ScrollBar
                if (ShouldDrawScrollBar
                    && (_contentHeight > _windowContentRect.height))
                    _scrollPos.y = GUI.VerticalScrollbar(_scrollBarRect, _scrollPos.y, _scrollBarRect.height, 0, _contentHeight);
                else
                    _scrollPos.y = 0;

                // Window ScrollView
                if (ShouldDrawContentBacker)
                    GUI.BeginGroup(_windowScrollRect, StyleManager.Styles.Box);
                else
                    GUI.BeginGroup(_windowScrollRect);
                GUILayout.BeginArea(_windowScrollViewRect);

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

                // Do Window Resize
                if (ShouldAutoResizeHeight)
                {
                    Height = Height + _contentHeight + 10;
                    if (Height > MaxResizeHeight)
                        Height = MaxResizeHeight;
                }
            }

            // Do Window Drag
            HandleDrag();

            return IsOpen;
        }

        internal virtual void DrawContent() { }

        private void RecalculateBounds()
        {
            // Window Header Area
            _windowHeaderRect.x = _windowRect.x;
            _windowHeaderRect.y = _windowRect.y;
            _windowHeaderRect.width = _windowRect.width + 8;
            _windowHeaderRect.height = 30f;

            // Window Content Area
            _windowContentRect.x = _windowHeaderRect.x + 10;
            _windowContentRect.y = _windowHeaderRect.y + _windowHeaderRect.height;
            _windowContentRect.width = _windowHeaderRect.width - 28;
            _windowContentRect.height = _windowRect.height - (_windowHeaderRect.height + 10);
            if (ShouldDrawScrollBar && (_contentHeight > _windowContentRect.height))
                _windowContentRect.width += 5;

            // Window ScrollView Area
            _windowScrollViewRect.width = _windowContentRect.width;
            _windowScrollViewRect.height = _contentHeight;
            if (ShouldDrawScrollBar && (_contentHeight > _windowContentRect.height))
                _windowScrollViewRect.width -= ScrollbarWidth + 5;

            // Window ScrollView Scroll
            _windowScrollRect.x = _windowScrollViewRect.x;
            _windowScrollRect.y = -_scrollPos.y;
            _windowScrollRect.width = _windowScrollViewRect.width;
            _windowScrollRect.height = _contentHeight;

            // ScrollBar
            _scrollBarRect.x = (_windowScrollViewRect.width + 5);
            _scrollBarRect.width = ScrollbarWidth;
            _scrollBarRect.height = _windowContentRect.height;
        }

        private void HandleDrag()
        {
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = -mousePos.y;

            if (!IsOpen || !IsDraggable)
                _isDragging = false;

            if (_hasClicked)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    _hasClicked = false;
                    _isDragging = false;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _hasClicked = true;

                    if (IsOpen
                        && IsDraggable
                        && _windowHeaderRect.Contains(Event.current.mousePosition))
                    {
                        _isDragging = true;
                        _dragOffset_x = mousePos.x - Pos_X;
                        _dragOffset_y = mousePos.y - Pos_Y;
                    }
                }
            }

            if (_isDragging)
            {
                Pos_X = mousePos.x - _dragOffset_x;
                Pos_Y = mousePos.y - _dragOffset_y;
            }
        }
    }
}
