using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class RuntimeWindow
    {
        private const float REFERENCE_HEIGHT = 1080f;

        public float ScrollbarWidth = 16f;

        public bool IsOpen;
        public bool IsDraggable = true;

        public bool ShouldDrawBox = true;
        public bool ShouldDrawScrollBar = true;
        public bool ShouldDrawContentBacker = true;

        public bool ShouldAutoResizeHeight;

        public float MinResizeHeight = 10;
        public float MaxResizeHeight;

        public string Label;

        private float _contentHeight;

        private Rect _windowRect = new();
        private Vector2 _scrollPos;

        // Layout Rects
        private Rect _windowHeaderRect = new();
        private Rect _windowContentRect = new();
        private Rect _windowScrollViewRect = new();
        private Rect _windowScrollRect = new();
        private Rect _scrollBarRect = new();

        private bool _hasClicked;
        private bool _isDragging;

        private Vector2 _dragOffset = new();
        private Vector2 _posCache = new();
        private Vector2 _sizeCache = new();

        // Helper to get the current scale multiplier
        private float ScaleFactor => Screen.height / REFERENCE_HEIGHT;

        public Vector2 Position
        {
            get => _posCache;
            set
            {
                _posCache.x = value.x;
                _posCache.y = value.y;
                _windowRect.position = _posCache;
            }
        }

        public Vector2 Size
        {
            get => _sizeCache;
            set
            {
                _sizeCache.x = value.x;
                _sizeCache.y = value.y;
                _windowRect.size = _sizeCache;
            }
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
            this._contentHeight = defaultSize.y;
            this.IsOpen = defaultState;
        }

        /// <summary>
        /// Draws the window and optionally its contents.
        /// Returns true if the window is open.
        /// </summary>
        public bool Draw()
        {
            if (IsOpen)
            {
                Matrix4x4 originalMatrix = GUI.matrix;

                float scale = ScaleFactor;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

                try
                {
                    RecalculateBounds();

                    if (Label == null) Label = string.Empty;
                    if (ShouldDrawBox)
                        GUI.Box(_windowRect, Label, StyleManager.Styles.Box);

                    GUI.BeginGroup(_windowContentRect);
                    {
                        // ScrollBar
                        if (ShouldDrawScrollBar && (_contentHeight > _windowContentRect.height))
                        {
                            _scrollPos.y = GUI.VerticalScrollbar(_scrollBarRect, _scrollPos.y, _scrollBarRect.height, 0, _contentHeight, StyleManager.Styles.VerticalScrollBar);
                        }
                        else _scrollPos.y = 0;

                        // ScrollView Container
                        if (ShouldDrawContentBacker)
                            GUI.BeginGroup(_windowScrollRect, StyleManager.Styles.Box);
                        else
                            GUI.BeginGroup(_windowScrollRect);

                        // Actual Content Area
                        GUILayout.BeginArea(_windowScrollViewRect);
                        {
                            DrawContent();

                            // Handle Content Resizing
                            if (Event.current.type == EventType.Repaint)
                            {
                                Rect lastRect = GUILayoutUtility.GetLastRect();
                                _contentHeight = lastRect.y + lastRect.height;
                            }
                        }
                        GUILayout.EndArea();
                        GUI.EndGroup(); // End ScrollView Group
                    }
                    GUI.EndGroup(); // End Content Group

                    // Auto Resize Logic
                    if (ShouldAutoResizeHeight)
                    {
                        Vector2 newSize = Size;
                        float newHeight = _windowHeaderRect.height + _windowScrollRect.height + 10;
                        if ((MaxResizeHeight > 0) && (newHeight > MaxResizeHeight))
                            newHeight = MaxResizeHeight;
                        if (newHeight < MinResizeHeight)
                            newHeight = MinResizeHeight;
                        newSize.y = newHeight;
                        Size = newSize;
                    }
                }
                finally
                {
                    GUI.matrix = originalMatrix;
                }
            }

            HandleDrag();

            return IsOpen;
        }

        internal virtual void DrawContent() { }

        private void RecalculateBounds()
        {
            Vector2 currentPos = Position;
            Vector2 currentSize = Size;

            // Header
            _windowHeaderRect.x = currentPos.x;
            _windowHeaderRect.y = currentPos.y;
            _windowHeaderRect.width = currentSize.x + 8;
            _windowHeaderRect.height = 30f;

            // Content Frame
            _windowContentRect.x = _windowHeaderRect.x + 10;
            _windowContentRect.y = _windowHeaderRect.y + _windowHeaderRect.height;
            _windowContentRect.width = _windowHeaderRect.width - 28;
            _windowContentRect.height = currentSize.y - (_windowHeaderRect.height + 10);

            if (ShouldDrawScrollBar && (_contentHeight > _windowContentRect.height))
                _windowContentRect.width += 5;

            // Scroll View inner rect
            _windowScrollViewRect.width = _windowContentRect.width;
            _windowScrollViewRect.height = _contentHeight;

            if (ShouldDrawScrollBar && (_contentHeight > _windowContentRect.height))
                _windowScrollViewRect.width -= ScrollbarWidth + 5;

            // Scroll Offset Group
            _windowScrollRect.x = _windowScrollViewRect.x;
            _windowScrollRect.y = -_scrollPos.y;
            _windowScrollRect.width = _windowScrollViewRect.width;
            _windowScrollRect.height = _contentHeight;

            // Scrollbar Visuals
            _scrollBarRect.x = (_windowScrollViewRect.width + 5);
            _scrollBarRect.width = ScrollbarWidth;
            _scrollBarRect.height = _windowContentRect.height;
        }

        private void HandleDrag()
        {
            if (!IsOpen || !IsDraggable)
            {
                _isDragging = false;
                return;
            }

            float scale = ScaleFactor;

            // Convert Mouse Position to "Reference Resolution" Space
            // Input.mousePosition is (0,0) at bottom left. GUI is (0,0) at top left.
            Vector2 rawMousePos = Input.mousePosition;
            Vector2 guiMousePos = new Vector2(rawMousePos.x, Screen.height - rawMousePos.y) / scale;

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

                    // Check if mouse is inside the Header Rect (using scaled coordinates)
                    if (_windowHeaderRect.Contains(guiMousePos))
                    {
                        _isDragging = true;
                        _dragOffset.x = guiMousePos.x - Position.x;
                        _dragOffset.y = guiMousePos.y - Position.y;
                    }
                }
            }

            if (_isDragging)
            {
                Vector2 newPos = Position;
                newPos.x = guiMousePos.x - _dragOffset.x;
                newPos.y = guiMousePos.y - _dragOffset.y;
                Position = newPos;
            }
        }
    }
}