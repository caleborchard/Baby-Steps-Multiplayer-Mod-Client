using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BabyStepsMultiplayerClient.UI
{
    public static class MenuInjectionLibrary
    {
        internal static readonly List<InjectedMenu> _registeredMenus = new List<InjectedMenu>();

        private static RectTransform customRoot;
        private static Button buttonTemplate;
        private static Toggle toggleTemplate;
        private static Slider sliderTemplate;
        private const float SliderHandleSize = 112f;
        private static Sprite _inputFieldSprite;
        private static Sprite _sliderHandleSprite;
        private static TMP_FontAsset _lowercaseTextFont;

        private static TMP_FontAsset GetOrCreateLowercaseTextFont()
        {
            if (_lowercaseTextFont != null) return _lowercaseTextFont;

            StyleManager.Fonts.Prepare();
            if (StyleManager.Fonts.Arial == null) return null;

            _lowercaseTextFont = TMP_FontAsset.CreateFontAsset(StyleManager.Fonts.Arial);
            return _lowercaseTextFont;
        }

        private static void ApplyLowercaseTextFont(TMP_Text text)
        {
            if (text == null) return;

            var font = GetOrCreateLowercaseTextFont();
            if (font != null)
                text.font = font;
        }

        private static Sprite GetOrCreateSliderHandleSprite()
        {
            if (_sliderHandleSprite != null) return _sliderHandleSprite;

            const int W = 64, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color32[W * H];
            float cx = (W - 1) * 0.5f;
            float cy = (H - 1) * 0.5f;
            float radius = 26f;

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(radius + 0.5f - dist) * 255f);
                pixels[y * W + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            _sliderHandleSprite = Sprite.Create(
                tex,
                new Rect(0, 0, W, H),
                new Vector2(0.5f, 0.5f),
                100f, 0,
                SpriteMeshType.FullRect);
            return _sliderHandleSprite;
        }

        private static Sprite GetOrCreateInputFieldSprite()
        {
            if (_inputFieldSprite != null) return _inputFieldSprite;

            const int W = 128, H = 64, R = 18;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx0 = R, cx1 = W - 1 - R;
            float cy0 = R, cy1 = H - 1 - R;
            var pixels = new Color32[W * H];

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x < cx0 ? cx0 - x : x > cx1 ? x - cx1 : 0f;
                float dy = y < cy0 ? cy0 - y : y > cy1 ? y - cy1 : 0f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(R + 0.5f - dist) * 255f);
                pixels[y * W + x] = new Color32(255, 255, 255, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            _inputFieldSprite = Sprite.Create(
                tex,
                new Rect(0, 0, W, H),
                new Vector2(0.5f, 0.5f),
                100f, 0,
                SpriteMeshType.FullRect,
                new Vector4(R, R, R, R));
            return _inputFieldSprite;
        }
        private static RuntimeTabMenu _currentTabMenu;
        private static int _currentTabPage = -1;
        private static InjectedMenu _currentInjectedMenu;

        private static float nextY;
        private const float Spacing = 10f;
        private const float DefaultHeight = 52f;
        internal static readonly Color TabUnselected = new Color(0.435f, 0.676f, 0.726f, 1.00f);
        internal static readonly Color TabSelected = new Color(1.000f, 1.000f, 1.000f, 0.78f);
        internal static readonly Color TabText = new Color(0.000f, 0.169f, 0.283f, 1.00f);
        internal static readonly Color TabHighlighted = new Color(0.519f, 0.804f, 0.865f, 1.00f);

        public sealed class RuntimeTabMenu
        {
            public RectTransform Root { get; internal set; }
            public RectTransform Header { get; internal set; }
            public Button[] Tabs { get; internal set; }
            public RectTransform[] Pages { get; internal set; }
            public int ActiveTab { get; private set; }
            public int TabCount => Tabs?.Length ?? 0;

            private readonly string[] _tabNames;
            private readonly List<Selectable>[] _pageSelectables;
            private int _setTabFrame = -1;

            internal MenuItemList ManagedItemList { get; set; }
            internal Color NativeHighlightedColor { get; set; } = TabHighlighted;
            private Selectable[] _fixedItems;
            internal void SetFixedMenuItems(params Selectable[] items) { _fixedItems = items; }

            internal RuntimeTabMenu(string[] tabNames)
            {
                _tabNames = tabNames;
                _pageSelectables = new List<Selectable>[tabNames.Length];
                for (int i = 0; i < tabNames.Length; i++)
                    _pageSelectables[i] = new List<Selectable>();
            }

            public RectTransform GetPage(int index)
                => Pages != null && index >= 0 && index < Pages.Length ? Pages[index] : null;

            public void OnShown() => SetActiveTab(ActiveTab);

            internal void RegisterPageSelectable(int page, Selectable s)
            {
                if (s == null || _pageSelectables == null) return;
                if (page < 0 || page >= _pageSelectables.Length) return;
                _pageSelectables[page].Add(s);
            }

            public void SetActiveTab(int index)
            {
                if (Tabs == null || Pages == null || index < 0 || index >= TabCount) return;
                int frame = UnityEngine.Time.frameCount;
                if (frame == _setTabFrame) return;
                _setTabFrame = frame;
                ActiveTab = index;

                var sels = _pageSelectables?[index];
                for (int i = 0; i < TabCount; i++)
                {
                    bool on = (i == index);

                    if (Pages[i] != null)
                    {
                        var cg = Pages[i].GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            cg.alpha = on ? 1f : 0f;
                            cg.interactable = on;
                            cg.blocksRaycasts = on;
                        }

                        if (on)
                        {
                            var allTmp = Pages[i].GetComponentsInChildren<TMP_Text>(true);
                            for (int j = 0; j < allTmp.Length; j++)
                                if (allTmp[j] != null) allTmp[j].ForceMeshUpdate();
                        }
                    }
                    if (Tabs[i] != null)
                    {
                        Color stateColor = on ? TabSelected : TabUnselected;
                        var image = Tabs[i].GetComponent<Image>();
                        if (image != null)
                        {
                            image.color = Color.white;
                            image.CrossFadeColor(stateColor, 0f, true, true);
                        }

                        var tmp = Tabs[i].GetComponentInChildren<TMP_Text>(true);
                        if (tmp != null)
                        {
                            if (_tabNames != null && i < _tabNames.Length)
                                tmp.text = _tabNames[i];
                            tmp.color = TabText;
                            tmp.ForceMeshUpdate();
                        }

                        var cb = Tabs[i].colors;
                        cb.normalColor = stateColor;
                        cb.selectedColor = NativeHighlightedColor;
                        cb.highlightedColor = NativeHighlightedColor;
                        cb.pressedColor = NativeHighlightedColor;
                        cb.disabledColor = stateColor;
                        cb.colorMultiplier = 1f;
                        Tabs[i].colors = cb;
                    }
                }
                int pageCount = sels?.Count        ?? 0;
                int fixedCount = _fixedItems?.Length ?? 0;

                var allSels = new List<Selectable>(pageCount + fixedCount);
                for (int i = 0; i < pageCount;  i++)
                    if (sels[i] != null)        allSels.Add(sels[i]);
                for (int i = 0; i < fixedCount; i++)
                    if (_fixedItems[i] != null) allSels.Add(_fixedItems[i]);
                if (ManagedItemList != null)
                {
                    var items = new GameObject[allSels.Count];
                    for (int i = 0; i < allSels.Count; i++)
                        items[i] = allSels[i].gameObject;
                    ManagedItemList.items = items;
                }
                for (int i = 0; i < allSels.Count; i++)
                {
                    var s = allSels[i];
                    var nav = s.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnUp = i > 0                 ? allSels[i - 1] : null;
                    nav.selectOnDown = i < allSels.Count - 1 ? allSels[i + 1] : null;
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;
                    s.navigation = nav;
                }
            }
        }

        public sealed class TabBuilder
        {
            internal TabBuilder() { }

            public TMP_Text AddLabel(string text)
                => MenuInjectionLibrary.AddLabel(text);

            public Button AddButton(string text, UnityAction onClick)
                => MenuInjectionLibrary.AddButton(text, onClick);

            public Toggle AddToggle(string text, bool initial, UnityAction<bool> onChange)
                => MenuInjectionLibrary.AddToggle(text, initial, onChange);

            public Slider AddSlider(string text, float min, float max, float value,
                UnityAction<float> onChange, bool wholeNumbers = false)
                => MenuInjectionLibrary.AddSlider(text, min, max, value, onChange, wholeNumbers);

            public Button AddInputField(string placeholder = "", UnityAction<string> onChange = null, string initialValue = null)
                => MenuInjectionLibrary.AddInputField(placeholder, onChange, initialValue);

            public Image AddImage(Color color, float height = DefaultHeight)
                => MenuInjectionLibrary.AddImage(color, height);
        }

        public sealed class MenuBuilder
        {
            internal readonly string _mainButtonLabel;
            internal readonly List<(string name, System.Action<TabBuilder> configure)> _tabs = new List<(string, System.Action<TabBuilder>)>();
            internal readonly List<(string label, UnityAction action)> _fixed = new List<(string, UnityAction)>();

            internal MenuBuilder(string mainButtonLabel) => _mainButtonLabel = mainButtonLabel;

            public MenuBuilder AddTab(string name, System.Action<TabBuilder> configure)
            {
                _tabs.Add((name, configure));
                return this;
            }
            public MenuBuilder AddFixedButton(string label, UnityAction action = null)
            {
                _fixed.Add((label, action));
                return this;
            }

            public InjectedMenu Build()
            {
                var menu = new InjectedMenu(this);
                if (!_registeredMenus.Contains(menu))
                    _registeredMenus.Add(menu);
                return menu;
            }
        }
        public static MenuBuilder CreateMenu(string mainButtonLabel) => new MenuBuilder(mainButtonLabel);

        internal sealed class InputFieldInfo
        {
            public string Value = "";
            public TMP_Text DisplayText;
            public UnityAction<string> OnChanged;
            public string Placeholder = "";
            public RectTransform Viewport;
            public Color ActiveColor = Color.white;
            public Color PlaceholderColor = new Color(1f, 1f, 1f, 0.45f);
            public Button OwnerButton;
            public Image CursorImage;
        }

        public sealed class InjectedMenu
        {
            private readonly string _mainButtonLabel;
            private readonly List<(string name, System.Action<TabBuilder> configure)> _tabDescs;
            private readonly List<(string label, UnityAction action)>                 _fixedDescs;
            private Menu _activeMenu;
            private RuntimeTabMenu _tabMenu;
            private bool _contentBuilt;
            private bool _hasAttemptedInitialDump;
            private GameObject _mainButtonObj;
            private Button _mainButton;
            private Transform _mainMenuRoot;
            private CanvasGroup _mainMenuCG;
            private MenuItemList _mainMenuItemList;
            private GameObject _submenuObj;
            private CanvasGroup _submenuCG;
            private MenuItemList _submenuItemList;
            private readonly List<Button> _fixedButtonInstances = new List<Button>();
            private GameObject _kbRoot;
            private CanvasGroup _kbCG;
            private Button[][] _kbCharRows;
            private string[][] _kbLowerRows;
            private string[][] _kbUpperRows;
            private Button[] _kbSpecialRow;
            private readonly List<GameObject> _kbAllButtons = new List<GameObject>();
            private bool _kbShift;
            private bool _kbUseLowercaseFontForKeyLabels;
            private Button _kbShiftBtn;
            private InputFieldInfo _kbActiveField;
            private Button _kbReturnButton;
            private bool _kbBuilt;
            private bool _kbHasEdited;
            private int _kbNavFrame = -1;
            private float _kbCursorTimer = 0f;
            private bool _kbCursorVisible = true;
            private InputFieldInfo _mouseTypingField;
            private bool _mouseTypingActive;
            internal bool IsMouseTyping => _mouseTypingActive;
            private int _mouseTypingNavFrame = -1;
            private int _mouseCursorBlinkFrame = -1;
            private bool _mouseHasEdited;
            private float _mouseCursorTimer = 0f;
            private bool _mouseCursorVisible = true;

            private bool KbVisible => _kbCG != null && _kbCG.alpha > 0.5f;
            private bool IsKbButton(GameObject go) => go != null && _kbAllButtons.Contains(go);
            private const float CanvasW = 860f;
            private const float CanvasH = 650f;
            private const float ContentH = 460f;
            private const float ContentPad = 10f;

            internal InjectedMenu(MenuBuilder b)
            {
                _mainButtonLabel = b._mainButtonLabel;
                _tabDescs = b._tabs;
                _fixedDescs = b._fixed;
            }

            public Button GetFixedButton(int index)
                => index >= 0 && index < _fixedButtonInstances.Count ? _fixedButtonInstances[index] : null;

            public void OnMenuAwake(Menu menu)
            {
                _activeMenu = menu;
                TryInject(menu, "Awake");
            }

            public void OnMenuPreUpdate(Menu menu)
            {
                _activeMenu = menu;
                if (menu == null) return;

                if (_mainButtonObj == null || _submenuObj == null)
                    TryInject(menu, "PreUpdateRetry");

                bool vis = GetSubmenuVisible();
                bool kbVis = vis && KbVisible;

                if (vis)
                {
                    if (kbVis)
                    {
                        var ev = EventSystem.current;
                        var sel = ev?.currentSelectedGameObject;
                        if ((sel == null || !IsKbButton(sel)) &&
                            _kbCharRows != null && _kbCharRows.Length > 0)
                        {
                            sel = _kbCharRows[0][0].gameObject;
                            ev?.SetSelectedGameObject(sel);
                        }
                        if (Time.frameCount != _kbNavFrame)
                        {
                            _kbNavFrame = Time.frameCount;

                            if (sel != null && IsKbButton(sel) && menu.rwPlayer != null)
                            {
                                float x  = menu.rwPlayer.GetAxis    ((int)InputActions.UIHorizontal);
                                float xp = menu.rwPlayer.GetAxisPrev((int)InputActions.UIHorizontal);
                                float y  = menu.rwPlayer.GetAxis    ((int)InputActions.UIVertical);
                                float yp = menu.rwPlayer.GetAxisPrev((int)InputActions.UIVertical);
                                var selComp = sel.GetComponent<Selectable>();
                                Selectable target = null;
                                if      (y < -0.5f && yp >= -0.5f) target = selComp?.navigation.selectOnDown;
                                else if (y >  0.5f && yp <= 0.5f)  target = selComp?.navigation.selectOnUp;
                                else if (x >  0.5f && xp <= 0.5f)  target = selComp?.navigation.selectOnRight;
                                else if (x < -0.5f && xp >= -0.5f) target = selComp?.navigation.selectOnLeft;

                                if (target != null)
                                {
                                    ev?.SetSelectedGameObject(target.gameObject);
                                    sel = target.gameObject;
                                }
                            }
                            if (menu.rwPlayer != null &&
                                (Input.GetKeyDown(KeyCode.Escape) ||
                                 menu.rwPlayer.GetButtonDown((int)InputActions.UICancel)))
                                CloseKeyboard();
                            if (_kbActiveField?.DisplayText != null)
                            {
                                _kbCursorTimer += Time.unscaledDeltaTime;
                                if (_kbCursorTimer >= 0.53f)
                                {
                                    _kbCursorTimer = 0f;
                                    _kbCursorVisible = !_kbCursorVisible;
                                    KbBlinkCursor();
                                }
                            }
                        }
                        if (_submenuItemList != null && sel != null)
                            _submenuItemList.items = new[] { sel };
                    }
                    else
                    {
                        TryHandleCancel(menu);
                        if (_mouseTypingActive && _mouseTypingField != null &&
                            Time.frameCount != _mouseTypingNavFrame)
                        {
                            _mouseTypingNavFrame = Time.frameCount;

                            bool changed = false;
                            foreach (char c in Input.inputString)
                            {
                                if (c == '\b')
                                {
                                    if (_mouseTypingField.Value.Length > 0)
                                    {
                                        _mouseTypingField.Value = _mouseTypingField.Value
                                            .Substring(0, _mouseTypingField.Value.Length - 1);
                                        changed = true;
                                    }
                                }
                                else if (c == '\r' || c == '\n')
                                {
                                    _mouseTypingField.OnChanged?.Invoke(_mouseTypingField.Value);
                                    CloseMouseTyping(restoreSelection: false);
                                    changed = false;
                                    break;
                                }
                                else
                                {
                                    _mouseTypingField.Value += c;
                                    changed = true;
                                }
                            }

                            if (Input.GetKeyDown(KeyCode.Escape) && _mouseTypingActive)
                            {
                                CloseMouseTyping(restoreSelection: true);
                                changed = false;
                            }

                            if (Input.GetMouseButtonDown(0) && _mouseTypingActive &&
                                _mouseTypingField?.OwnerButton != null &&
                                !IsPointerOverGameObject(_mouseTypingField.OwnerButton.gameObject))
                            {
                                CloseMouseTyping(restoreSelection: false);
                                changed = false;
                            }

                            if (changed && _mouseTypingField != null)
                            {
                                _mouseHasEdited = true;
                                RefreshMouseTypingDisplay();
                            }
                        }
                        if (_mouseTypingActive && _mouseTypingField?.DisplayText != null &&
                            Time.frameCount != _mouseCursorBlinkFrame)
                        {
                            _mouseCursorBlinkFrame = Time.frameCount;
                            _mouseCursorTimer += Time.unscaledDeltaTime;
                            if (_mouseCursorTimer >= 0.53f)
                            {
                                _mouseCursorTimer = 0f;
                                _mouseCursorVisible = !_mouseCursorVisible;
                                RefreshMouseTypingDisplay();
                            }
                        }
                        if (_tabMenu != null && menu.rwPlayer != null)
                        {
                            if (menu.rwPlayer.GetButtonDown((int)InputActions.UITabPrev))
                                _tabMenu.SetActiveTab((_tabMenu.ActiveTab - 1 + _tabMenu.TabCount) % _tabMenu.TabCount);
                            else if (menu.rwPlayer.GetButtonDown((int)InputActions.UITabNext))
                                _tabMenu.SetActiveTab((_tabMenu.ActiveTab + 1) % _tabMenu.TabCount);
                        }
                    }
                }
                if (kbVis)
                {
                    if (_submenuItemList != null) MenuItemList.active = _submenuItemList;
                }
                else if (vis)
                {
                    if (_submenuItemList != null) MenuItemList.active = _submenuItemList;
                    if (_mouseTypingActive && _mouseTypingField?.OwnerButton != null && _submenuItemList != null)
                        _submenuItemList.items = new[] { _mouseTypingField.OwnerButton.gameObject };
                }
                else if (menu.currentMenuScreen == Menu.MenuScreen.main)
                {
                    if (_mainMenuItemList != null) MenuItemList.active = _mainMenuItemList;
                }
            }

            public void OnMenuUpdate(Menu menu)
            {
                _activeMenu = menu;
                if (menu == null) return;

                if (Input.GetKeyDown(KeyCode.F8))
                {
                }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    Reset();
                    TryInject(menu, "Reinject_F9");
                }

                if (_mainButtonObj == null || _submenuObj == null)
                    TryInject(menu, "UpdateRetry");

                UpdateVisibility(menu);
                EnsureSelection();
            }

            private void TryInject(Menu menu, string reason)
            {
                if (menu == null) return;
                ResolveExisting(menu);

                if (!_hasAttemptedInitialDump)
                {
                    _hasAttemptedInitialDump = true;
                }

                if (menu.mainMenuCanvas == null)
                {
                    return;
                }

                var tmpl = FindTemplateButton(menu);
                if (tmpl == null)
                {
                    return;
                }

                _mainMenuRoot = tmpl.transform.parent;
                if (_mainMenuRoot != null)
                {
                    _mainMenuCG = _mainMenuRoot.GetComponent<CanvasGroup>()
                                        ?? _mainMenuRoot.gameObject.AddComponent<CanvasGroup>();
                    _mainMenuItemList = _mainMenuRoot.GetComponent<MenuItemList>();
                }

                if (_mainButtonObj == null) _mainButtonObj = BuildMainButton(menu, tmpl);
                if (_submenuObj == null) _submenuObj = BuildSubmenu(menu, tmpl);

                IntegrateIntoMainItemList();
                EnsureTabContent(menu, tmpl);
                UpdateVisibility(menu);
            }

            private void EnsureTabContent(Menu menu, Button tmpl)
            {
                if (_contentBuilt || _submenuObj == null) return;

                var contentRect = _submenuObj.transform.Find("CustomSettingsContentRoot") as RectTransform;
                if (contentRect == null) return;
                Button nativeTab = null;
                if (Menu.me?.tabs != null)
                {
                    var tabBar = Menu.me.tabs.transform;
                    for (int i = 0; i < tabBar.childCount; i++)
                    {
                        var btn = tabBar.GetChild(i)?.GetComponent<Button>();
                        if (btn != null) { nativeTab = btn; break; }
                    }
                }

                var tabNames = new string[_tabDescs.Count];
                for (int i = 0; i < _tabDescs.Count; i++) tabNames[i] = _tabDescs[i].name;

                _tabMenu = BuildNativeTabMenu(tabNames, contentRect, nativeTab, tmpl);
                _tabMenu.ManagedItemList = _submenuItemList;

                var fixedSels = new Selectable[_fixedButtonInstances.Count];
                for (int i = 0; i < _fixedButtonInstances.Count; i++) fixedSels[i] = _fixedButtonInstances[i];
                _tabMenu.SetFixedMenuItems(fixedSels);

                var togTmpl = FindToggleTemplate(menu);
                var sldTmpl = FindSliderTemplate(menu);
                var builder = new TabBuilder();
                _currentInjectedMenu = this;

                for (int i = 0; i < _tabDescs.Count; i++)
                {
                    ConfigureTabPage(_tabMenu, i, tmpl, togTmpl, sldTmpl);
                    _tabDescs[i].configure?.Invoke(builder);
                }

                _currentInjectedMenu = null;
                BuildKeyboard(menu, tmpl, true);

                _tabMenu.SetActiveTab(0);
                _contentBuilt = true;
            }

            private GameObject BuildMainButton(Menu menu, Button tmpl)
            {
                var obj = UnityEngine.Object.Instantiate(tmpl.gameObject, tmpl.transform.parent);
                obj.name = "BBSMP_MainMenuButton";
                StripLocalizationComponents(obj);
                obj.transform.SetSiblingIndex(tmpl.transform.GetSiblingIndex() + 1);

                var cr = obj.GetComponent<RectTransform>();
                var tr = tmpl.GetComponent<RectTransform>();
                if (cr != null && tr != null)
                {
                    cr.anchorMin = tr.anchorMin;
                    cr.anchorMax = tr.anchorMax;
                    cr.pivot = tr.pivot;
                    cr.sizeDelta = tr.sizeDelta;
                    cr.localScale = Vector3.one;
                    float step = Mathf.Max(45f, Mathf.Abs(tr.sizeDelta.y) + 10f);
                    cr.anchoredPosition = tr.anchoredPosition + new Vector2(0f, step);
                }

                _mainButton = obj.GetComponent<Button>();
                ClearButtonEvents(_mainButton);
                _mainButton?.onClick.AddListener((UnityAction)(() => SetSubmenuVisible(true)));
                SetButtonLabel(obj, _mainButtonLabel);
                return obj;
            }

            private GameObject BuildSubmenu(Menu menu, Button tmpl)
            {
                var root = new GameObject("BBSMP_SubmenuCanvas");
                root.transform.SetParent(menu.mainMenuCanvas.transform, false);
                root.transform.SetAsLastSibling();

                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(CanvasW, CanvasH);
                rect.anchoredPosition = new Vector2(10f, -10f);

                _submenuCG = root.AddComponent<CanvasGroup>();
                _submenuCG.alpha = 0f;
                _submenuCG.blocksRaycasts = false;
                _submenuCG.interactable = false;
                var contentObj = new GameObject("CustomSettingsContentRoot");
                contentObj.transform.SetParent(root.transform, false);
                var contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.5f, 1f);
                contentRect.anchorMax = new Vector2(0.5f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = new Vector2(800f, ContentH);
                contentRect.anchoredPosition = new Vector2(0f, -ContentPad);

                var tmplRect = tmpl.GetComponent<RectTransform>();
                float btnW = tmplRect != null ? Mathf.Max(200f, tmplRect.sizeDelta.x) : 520f;
                float btnH = tmplRect != null ? Mathf.Clamp(tmplRect.sizeDelta.y, 40f, 72f) : 52f;
                _fixedButtonInstances.Clear();
                for (int i = _fixedDescs.Count - 1; i >= 0; i--)
                {
                    var (label, action) = _fixedDescs[i];
                    var btnObj = UnityEngine.Object.Instantiate(tmpl.gameObject, root.transform);
                    btnObj.name = $"BBSMP_Fixed_{i}";
                    StripLocalizationComponents(btnObj);
                    var btn = btnObj.GetComponent<Button>();
                    ClearButtonEvents(btn);
                    SetButtonLabel(btnObj, label);

                    var btnRect = btnObj.GetComponent<RectTransform>();
                    btnRect.anchorMin = Vector2.zero;
                    btnRect.anchorMax = Vector2.zero;
                    btnRect.pivot = Vector2.zero;
                    btnRect.sizeDelta = new Vector2(btnW, btnH);

                    int slotFromBottom = (_fixedDescs.Count - 1) - i;
                    btnRect.anchoredPosition = new Vector2(10f, 10f + slotFromBottom * (btnH + 10f));

                    var capturedAction = action;
                    btn?.onClick.AddListener((UnityAction)(() =>
                    {
                        capturedAction?.Invoke();
                        SetSubmenuVisible(false);
                    }));

                    _fixedButtonInstances.Insert(0, btn);
                }

                _submenuItemList = root.AddComponent<MenuItemList>();
                _submenuItemList.items = new GameObject[0];

                return root;
            }

            private void BuildKeyboard(Menu menu, Button tmpl, bool useLowercaseFontForLetterKeys = true)
            {
                if (_kbBuilt || menu?.mainMenuCanvas == null || tmpl == null) return;
                _kbUseLowercaseFontForKeyLabels = useLowercaseFontForLetterKeys;
                _kbLowerRows = new string[][]
                {
                    new[] { "1","2","3","4","5","6","7","8","9","0","." },
                    new[] { "q","w","e","r","t","y","u","i","o","p" },
                    new[] { "a","s","d","f","g","h","j","k","l" },
                    new[] { "z","x","c","v","b","n","m" },
                };
                _kbUpperRows = new string[][]
                {
                    new[] { "1","2","3","4","5","6","7","8","9","0","." },
                    new[] { "Q","W","E","R","T","Y","U","I","O","P" },
                    new[] { "A","S","D","F","G","H","J","K","L" },
                    new[] { "Z","X","C","V","B","N","M" },
                };

                const float PanelW = 800f;
                const float KeyH = 44f;
                const float HGap = 6f;
                const float VGap = 4f;
                const float PadH = 10f;
                const float PadV = 10f;
                const float SpecFixedW = 72f;
                const float SpecGap = HGap;
                const float KeyFontSize = 20f;

                int totalRows = _kbLowerRows.Length + 1;
                float panelH = PadV * 2f + totalRows * KeyH + (totalRows - 1) * VGap - 15f;
                float innerW = PanelW - PadH * 2f;

                int maxCharCols = 0;
                for (int i = 0; i < _kbLowerRows.Length; i++)
                    if (_kbLowerRows[i].Length > maxCharCols) maxCharCols = _kbLowerRows[i].Length;

                float keyWBase = (innerW - (maxCharCols - 1) * HGap) / maxCharCols;

                _kbRoot = new GameObject("BBSMP_Keyboard");
                _kbRoot.transform.SetParent(menu.mainMenuCanvas.transform, false);
                _kbRoot.transform.SetAsLastSibling();

                var rootRect = _kbRoot.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0.5f, 0f);
                rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.sizeDelta = new Vector2(PanelW, panelH);
                rootRect.anchoredPosition = new Vector2(0f, 30f);

                var bgImg = _kbRoot.AddComponent<Image>();
                bgImg.color = new Color(0.05f, 0.11f, 0.18f, 0.97f);

                _kbCG = _kbRoot.AddComponent<CanvasGroup>();
                _kbCG.alpha = 0f;
                _kbCG.interactable = false;
                _kbCG.blocksRaycasts = false;

                _kbCharRows = new Button[_kbLowerRows.Length][];
                float y = -PadV;

                for (int row = 0; row < _kbLowerRows.Length; row++)
                {
                    int n = _kbLowerRows[row].Length;
                    float keyW = keyWBase;
                    float rowTotalW = n * keyW + (n - 1) * HGap;
                    float sx = -rowTotalW * 0.5f;

                    _kbCharRows[row] = new Button[n];

                    for (int col = 0; col < n; col++)
                    {
                        int capturedRow = row;
                        int capturedCol = col;

                        var keyObj = UnityEngine.Object.Instantiate(tmpl.gameObject, _kbRoot.transform);
                        keyObj.name = $"BBSMP_Key_{_kbUpperRows[row][col]}";
                        StripLocalizationComponents(keyObj);

                        var btn = keyObj.GetComponent<Button>();
                        ClearButtonEvents(btn);
                        if (btn != null)
                            btn.onClick.AddListener((UnityAction)(() => KbType(capturedRow, capturedCol)));

                        SetButtonLabel(keyObj, _kbLowerRows[row][col], _kbUseLowercaseFontForKeyLabels);

                        var keyTmp = keyObj.GetComponentInChildren<TMP_Text>(true);
                        if (keyTmp != null)
                            keyTmp.fontSize = Mathf.Min(keyTmp.fontSize, KeyFontSize);

                        var kr = keyObj.GetComponent<RectTransform>();
                        kr.anchorMin = new Vector2(0.5f, 1f);
                        kr.anchorMax = new Vector2(0.5f, 1f);
                        kr.pivot = new Vector2(0f, 1f);
                        kr.sizeDelta = new Vector2(keyW, KeyH);
                        kr.anchoredPosition = new Vector2(sx + col * (keyW + HGap), y);

                        _kbCharRows[row][col] = btn;
                        _kbAllButtons.Add(keyObj);
                    }

                    y -= KeyH + VGap;
                }

                float spaceW = innerW - 3f * SpecFixedW - 3f * SpecGap;
                float[] specWidths = { SpecFixedW, spaceW, SpecFixedW, SpecFixedW };
                string[] specLabels = { "Shift", "Space", "Back", "Done" };

                _kbSpecialRow = new Button[4];
                float specX = -innerW * 0.5f;

                for (int i = 0; i < 4; i++)
                {
                    var keyObj = UnityEngine.Object.Instantiate(tmpl.gameObject, _kbRoot.transform);
                    keyObj.name = $"BBSMP_Key_{specLabels[i]}";
                    StripLocalizationComponentsImmediate(keyObj);

                    var btn = keyObj.GetComponent<Button>();
                    ClearButtonEvents(btn);

                    if (btn != null)
                    {
                        switch (i)
                        {
                            case 0: btn.onClick.AddListener((UnityAction)KbToggleShift); _kbShiftBtn = btn; break;
                            case 1: btn.onClick.AddListener((UnityAction)KbSpace); break;
                            case 2: btn.onClick.AddListener((UnityAction)KbBackspace); break;
                            case 3: btn.onClick.AddListener((UnityAction)CloseKeyboard); break;
                        }
                    }

                    SetButtonLabel(keyObj, specLabels[i], false);

                    var keyTmp = keyObj.GetComponentInChildren<TMP_Text>(true);
                    if (keyTmp != null)
                        keyTmp.fontSize = Mathf.Min(keyTmp.fontSize, KeyFontSize);

                     var kr = keyObj.GetComponent<RectTransform>();
                     kr.anchorMin = new Vector2(0.5f, 1f);
                     kr.anchorMax = new Vector2(0.5f, 1f);
                     kr.pivot = new Vector2(0f, 1f);
                     kr.sizeDelta = new Vector2(specWidths[i], KeyH);
                     kr.anchoredPosition = new Vector2(specX, y);
                     specX += specWidths[i] + SpecGap;

                    _kbSpecialRow[i] = btn;
                    _kbAllButtons.Add(keyObj);
                }

                WireKeyboardNavigation();

                _kbBuilt = true;
            }

            private void WireKeyboardNavigation()
            {
                for (int row = 0; row < _kbCharRows.Length; row++)
                {
                    int n = _kbCharRows[row].Length;
                    for (int col = 0; col < n; col++)
                    {
                        var nav = new Navigation { mode = Navigation.Mode.None };
                        nav.selectOnLeft = col > 0 ? _kbCharRows[row][col - 1] : null;
                        nav.selectOnRight = col < n - 1 ? _kbCharRows[row][col + 1] : null;
                        nav.selectOnUp = row > 0
                            ? _kbCharRows[row - 1][Mathf.Min(col, _kbCharRows[row - 1].Length - 1)]
                            : null;

                        if (row < _kbCharRows.Length - 1)
                        {
                            nav.selectOnDown = _kbCharRows[row + 1][Mathf.Min(col, _kbCharRows[row + 1].Length - 1)];
                        }
                        else
                        {
                            int specIdx = Mathf.Clamp(col * _kbSpecialRow.Length / _kbCharRows[row].Length, 0, _kbSpecialRow.Length - 1);
                            nav.selectOnDown = _kbSpecialRow[specIdx];
                        }

                        _kbCharRows[row][col].navigation = nav;
                    }
                }

                int lastCharRow = _kbCharRows.Length - 1;
                int lastRowLen = _kbCharRows[lastCharRow].Length;
                for (int i = 0; i < _kbSpecialRow.Length; i++)
                {
                    var nav = new Navigation { mode = Navigation.Mode.None };
                    nav.selectOnLeft = i > 0 ? _kbSpecialRow[i - 1] : null;
                    nav.selectOnRight = i < _kbSpecialRow.Length - 1 ? _kbSpecialRow[i + 1] : null;
                    nav.selectOnDown = null;
                    int upCol = Mathf.Clamp(i * lastRowLen / _kbSpecialRow.Length, 0, lastRowLen - 1);
                    nav.selectOnUp = _kbCharRows[lastCharRow][upCol];
                    _kbSpecialRow[i].navigation = nav;
                }
            }

            private void KbType(int row, int col)
            {
                if (_kbActiveField == null) return;
                string[][] arr = _kbShift ? _kbUpperRows : _kbLowerRows;
                _kbActiveField.Value += arr[row][col];
                _kbHasEdited = true;
                KbRefreshDisplay();
                _kbActiveField.OnChanged?.Invoke(_kbActiveField.Value);
                if (_kbShift) { _kbShift = false; KbRefreshKeyLabels(); }
            }

            private void KbSpace()
            {
                if (_kbActiveField == null) return;
                _kbActiveField.Value += " ";
                _kbHasEdited = true;
                KbRefreshDisplay();
                _kbActiveField.OnChanged?.Invoke(_kbActiveField.Value);
            }

            private void KbBackspace()
            {
                if (_kbActiveField == null || _kbActiveField.Value.Length == 0) return;
                _kbActiveField.Value = _kbActiveField.Value.Substring(0, _kbActiveField.Value.Length - 1);
                _kbHasEdited = true;
                KbRefreshDisplay();
                _kbActiveField.OnChanged?.Invoke(_kbActiveField.Value);
            }

            private void KbToggleShift()
            {
                _kbShift = !_kbShift;
                KbRefreshKeyLabels();
            }

            private void KbRefreshKeyLabels()
            {
                if (_kbCharRows == null) return;
                string[][] arr = _kbShift ? _kbUpperRows : _kbLowerRows;
                for (int row = 0; row < _kbCharRows.Length; row++)
                    for (int col = 0; col < _kbCharRows[row].Length; col++)
                        SetButtonLabel(_kbCharRows[row][col].gameObject, arr[row][col], _kbUseLowercaseFontForKeyLabels);

                if (_kbShiftBtn != null)
                    SetButtonLabel(_kbShiftBtn.gameObject, _kbShift ? "SHIFT" : "Shift", false);
            }

            private void KbRefreshDisplay()
            {
                var field = _kbActiveField;
                if (field == null || field.DisplayText == null) return;
                var dt = field.DisplayText;

                _kbCursorVisible = true;
                _kbCursorTimer = 0f;

                ApplyCaretText(field, _kbCursorVisible, _kbHasEdited);
                dt.ForceMeshUpdate();

                float shift = 0f;
                var vr = dt.rectTransform;
                if (vr != null && field.Viewport != null)
                {
                    float viewW = field.Viewport.rect.width;
                    float textW = GetDisplayWidthForRenderedText(dt, dt.text);
                    shift = viewW > 1f ? Mathf.Max(0f, textW - viewW) : 0f;
                    vr.offsetMin = new Vector2(-shift, vr.offsetMin.y);
                    vr.offsetMax = new Vector2(-shift, vr.offsetMax.y);
                }

                if (field.CursorImage != null)
                {
                    field.CursorImage.color = Color.clear;
                }
            }

            private void KbBlinkCursor()
            {
                var field = _kbActiveField;
                if (field?.DisplayText == null) return;

                var dt = field.DisplayText;
                ApplyCaretText(field, _kbCursorVisible, _kbHasEdited);
                dt.ForceMeshUpdate();

                var vr = dt.rectTransform;
                if (vr != null && field.Viewport != null)
                {
                    float viewW = field.Viewport.rect.width;
                    float textW = GetDisplayWidthForRenderedText(dt, dt.text);
                    float shift = viewW > 1f ? Mathf.Max(0f, textW - viewW) : 0f;
                    vr.offsetMin = new Vector2(-shift, vr.offsetMin.y);
                    vr.offsetMax = new Vector2(-shift, vr.offsetMax.y);
                }
            }

            internal void OpenKeyboard(InputFieldInfo info, Button returnButton)
            {
                if (!_kbBuilt) return;
                _kbActiveField = info;
                _kbReturnButton = returnButton;
                _kbShift = false;
                _kbHasEdited = false;
                _kbCursorVisible = true;
                _kbCursorTimer = 0f;
                KbRefreshKeyLabels();
                KbRefreshDisplay();
                SetKeyboardVisible(true);
            }

            private void CloseKeyboard()
            {
                if (_kbActiveField?.DisplayText != null)
                {
                    var dt = _kbActiveField.DisplayText;
                    if (_kbActiveField.Value.Length > 0)
                    {
                        dt.text = _kbActiveField.Value;
                        dt.color = _kbActiveField.ActiveColor;
                    }
                    else
                    {
                        dt.text = _kbActiveField.Placeholder;
                        dt.color = _kbActiveField.PlaceholderColor;
                    }
                    dt.ForceMeshUpdate();

                    var vr = dt.rectTransform;
                    if (vr != null)
                    {
                        vr.offsetMin = new Vector2(0f, vr.offsetMin.y);
                        vr.offsetMax = new Vector2(0f, vr.offsetMax.y);
                    }

                    if (_kbActiveField.CursorImage != null)
                        _kbActiveField.CursorImage.color = Color.clear;
                }

                SetKeyboardVisible(false);
                _tabMenu?.SetActiveTab(_tabMenu.ActiveTab);
                var returnTo = _kbReturnButton;
                _kbActiveField = null;
                _kbReturnButton = null;
                if (returnTo != null)
                    EventSystem.current?.SetSelectedGameObject(returnTo.gameObject);
            }

            internal void OpenMouseTyping(InputFieldInfo info)
            {
                _mouseTypingField = info;
                _mouseTypingActive = true;
                _mouseTypingNavFrame = -1;
                _mouseCursorBlinkFrame = -1;
                _mouseHasEdited = false;
                _mouseCursorTimer = 0f;
                _mouseCursorVisible = true;
                if (info?.CursorImage != null)
                    info.CursorImage.color = Color.clear;
                EventSystem.current?.SetSelectedGameObject(null);
                RefreshMouseTypingDisplay();
            }

            private void CloseMouseTyping(bool restoreSelection = true)
            {
                _mouseTypingActive = false;
                var field = _mouseTypingField;
                _mouseTypingField = null;
                if (restoreSelection && field?.OwnerButton != null)
                    EventSystem.current?.SetSelectedGameObject(field.OwnerButton.gameObject);
                if (field.CursorImage != null)
                    field.CursorImage.color = Color.clear;
                if (field?.DisplayText == null) return;
                var dt = field.DisplayText;
                if (field.Value.Length > 0)
                {
                    dt.text = field.Value;
                    dt.color = field.ActiveColor;
                }
                else
                {
                    dt.text = field.Placeholder;
                    dt.color = field.PlaceholderColor;
                }
                dt.ForceMeshUpdate();
                var vr = dt.rectTransform;
                if (vr != null)
                {
                    vr.offsetMin = new Vector2(0f, vr.offsetMin.y);
                    vr.offsetMax = new Vector2(0f, vr.offsetMax.y);
                }
            }

            private void RefreshMouseTypingDisplay()
            {
                var field = _mouseTypingField;
                if (field?.DisplayText == null) return;
                var dt = field.DisplayText;

                ApplyCaretText(field, _mouseCursorVisible, _mouseHasEdited);
                dt.ForceMeshUpdate();
                float shift = 0f;
                var vr = dt.rectTransform;
                if (vr != null && field.Viewport != null)
                {
                    float viewW = field.Viewport.rect.width;
                    float textW = GetDisplayWidthForRenderedText(dt, dt.text);
                    shift = viewW > 1f ? Mathf.Max(0f, textW - viewW) : 0f;
                    vr.offsetMin = new Vector2(-shift, vr.offsetMin.y);
                    vr.offsetMax = new Vector2(-shift, vr.offsetMax.y);
                }
                if (field.CursorImage != null)
                {
                    field.CursorImage.color = Color.clear;
                }
            }

            private static void ApplyCaretText(InputFieldInfo field, bool showCaret, bool hasEdited)
            {
                if (field?.DisplayText == null) return;

                bool hasValue = !string.IsNullOrEmpty(field.Value);
                field.DisplayText.color = hasValue ? field.ActiveColor : field.PlaceholderColor;
                if (!hasValue)
                {
                    field.DisplayText.text = showCaret ? "|" : "<color=#FFFFFF00>|</color>";
                    return;
                }

                if (!hasEdited)
                {
                    field.DisplayText.text = showCaret
                        ? field.Value + "|"
                        : field.Value + "<color=#FFFFFF00>|</color>";
                    return;
                }
                field.DisplayText.text = showCaret
                    ? field.Value + "|"
                    : field.Value + "<color=#FFFFFF00>|</color>";
            }

            private static float GetDisplayWidthForRenderedText(TMP_Text text, string renderedText)
            {
                if (text == null) return 0f;
                if (string.IsNullOrEmpty(renderedText)) return 0f;
                const string sentinel = "M";
                float withSentinel = text.GetPreferredValues(renderedText + sentinel).x;
                float sentinelOnly = text.GetPreferredValues(sentinel).x;
                return Mathf.Max(0f, withSentinel - sentinelOnly);
            }

            private static bool IsPointerOverGameObject(GameObject target)
            {
                var ev = EventSystem.current;
                if (ev == null || target == null) return false;

                var pointerData = new PointerEventData(ev)
                {
                    position = Input.mousePosition
                };

                var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
                ev.RaycastAll(pointerData, results);
                for (int i = 0; i < results.Count; i++)
                {
                    var go = results[i].gameObject;
                    if (go == null) continue;
                    if (go == target || go.transform.IsChildOf(target.transform))
                        return true;
                }

                return false;
            }

            private void SetKeyboardVisible(bool visible)
            {
                if (_kbCG == null) return;
                _kbCG.alpha = visible ? 1f : 0f;
                _kbCG.interactable = visible;
                _kbCG.blocksRaycasts = visible;

                if (visible && _kbCharRows != null && _kbCharRows.Length > 0)
                {
                    var firstKey = _kbCharRows[0][0];
                    if (_submenuItemList != null)
                        _submenuItemList.items = new[] { firstKey.gameObject };
                    EventSystem.current?.SetSelectedGameObject(firstKey.gameObject);
                    MenuItemList.active = _submenuItemList;
                }
            }

            private void IntegrateIntoMainItemList()
            {
                if (_mainMenuItemList == null || _mainButtonObj == null) return;

                var old = _mainMenuItemList.items;
                if (old == null || old.Length == 0) return;

                for (int i = 0; i < old.Length; i++)
                    if (old[i] == _mainButtonObj) return;

                var continueBtn = _activeMenu?.ContinueGameButton;
                if (continueBtn == null) return;

                int ci = -1;
                for (int i = 0; i < old.Length; i++)
                    if (old[i] == continueBtn.gameObject) { ci = i; break; }
                if (ci < 0) return;

                var newItems = new GameObject[old.Length + 1];
                for (int i = 0, j = 0; i < newItems.Length; i++)
                {
                    if (i == ci + 1) { newItems[i] = _mainButtonObj; continue; }
                    newItems[i] = old[j++];
                }
                _mainMenuItemList.items = newItems;
            }

            private void UpdateVisibility(Menu menu)
            {
                if (menu == null) return;

                bool mainVisible = menu.mainMenuCanvas != null
                    && menu.mainMenuCanvas.activeInHierarchy
                    && menu.currentMenuScreen == Menu.MenuScreen.main;

                if (_mainButtonObj != null)
                    _mainButtonObj.SetActive(mainVisible);

                if (!mainVisible)
                {
                    if (GetSubmenuVisible()) SetSubmenuVisible(false);
                    return;
                }

                bool vis = GetSubmenuVisible();
                if (_mainMenuCG != null)
                {
                    _mainMenuCG.alpha = vis ? 0f : 1f;
                    _mainMenuCG.blocksRaycasts = !vis;
                    _mainMenuCG.interactable = !vis;
                }

                if (menu.titleGroup != null)
                    menu.titleGroup.alpha = vis ? 0f : 1f;
            }

            private void SetSubmenuVisible(bool isVisible)
            {
                if (_submenuCG == null) return;

                bool current = _submenuCG.alpha > 0.5f;
                if (current == isVisible) return;
                if (!isVisible && KbVisible) SetKeyboardVisible(false);

                _submenuCG.alpha = isVisible ? 1f : 0f;
                _submenuCG.blocksRaycasts = isVisible;
                _submenuCG.interactable = isVisible;

                for (int i = 0; i < _fixedButtonInstances.Count; i++)
                    if (_fixedButtonInstances[i] != null)
                        _fixedButtonInstances[i].interactable = isVisible;

                if (_mainButton != null)
                    _mainButton.interactable = !isVisible;

                if (isVisible)
                {
                    _tabMenu?.OnShown();
                    if (_submenuItemList != null) MenuItemList.active = _submenuItemList;
                }
                else
                {
                    if (_mainMenuItemList != null) MenuItemList.active = _mainMenuItemList;
                }

                var ev = EventSystem.current;
                if (ev != null)
                {
                    if (isVisible && _fixedButtonInstances.Count > 0 && _fixedButtonInstances[0] != null)
                        ev.SetSelectedGameObject(_fixedButtonInstances[0].gameObject);
                    else if (!isVisible && _mainButton != null)
                        ev.SetSelectedGameObject(_mainButton.gameObject);
                }

                UpdateVisibility(_activeMenu);
            }

            private void EnsureSelection()
            {
                if (KbVisible || _mouseTypingActive) return;

                var ev = EventSystem.current;
                if (ev == null) return;

                var sel = ev.currentSelectedGameObject;
                bool lost = sel == null
                    || !sel.activeInHierarchy
                    || (sel.GetComponent<Selectable>() is Selectable s && !s.interactable);

                if (!lost) return;

                if (GetSubmenuVisible())
                {
                    if (_fixedButtonInstances.Count > 0 && _fixedButtonInstances[0] != null)
                        ev.SetSelectedGameObject(_fixedButtonInstances[0].gameObject);
                }
                else if (_activeMenu?.ContinueGameButton != null)
                {
                    ev.SetSelectedGameObject(_activeMenu.ContinueGameButton.gameObject);
                }
            }

            private void TryHandleCancel(Menu menu)
            {
                if (menu?.rwPlayer == null || KbVisible || _mouseTypingActive) return;
                if (Input.GetKeyDown(KeyCode.Escape) || menu.rwPlayer.GetButtonDown((int)InputActions.UICancel))
                    SetSubmenuVisible(false);
            }

            private bool GetSubmenuVisible() => _submenuCG != null && _submenuCG.alpha > 0.5f;

            private void ResolveExisting(Menu menu)
            {
                if (menu?.mainMenuCanvas == null) return;

                var transforms = menu.mainMenuCanvas.GetComponentsInChildren<Transform>(true);
                var foundBtns = new List<GameObject>();
                var foundMenus = new List<GameObject>();
                var foundKbs = new List<GameObject>();

                for (int i = 0; i < transforms.Length; i++)
                {
                    var t = transforms[i];
                    if (t == null) continue;
                    if      (t.name == "BBSMP_MainMenuButton") foundBtns.Add(t.gameObject);
                    else if (t.name == "BBSMP_SubmenuCanvas")  foundMenus.Add(t.gameObject);
                    else if (t.name == "BBSMP_Keyboard")       foundKbs.Add(t.gameObject);
                }
                if (foundKbs.Count > 0)
                {
                    if (_kbBuilt)
                    {
                        _kbRoot = foundKbs[0];
                        _kbCG = _kbRoot?.GetComponent<CanvasGroup>();
                    }
                    else
                    {
                        for (int i = 0; i < foundKbs.Count; i++) UnityEngine.Object.Destroy(foundKbs[i]);
                        _kbRoot = null;
                        _kbCG = null;
                    }
                    for (int i = 1; i < foundKbs.Count; i++) UnityEngine.Object.Destroy(foundKbs[i]);
                }

                if (foundBtns.Count > 0)
                {
                    _mainButtonObj = foundBtns[0];
                    _mainButton = _mainButtonObj.GetComponent<Button>();
                    for (int i = 1; i < foundBtns.Count; i++) UnityEngine.Object.Destroy(foundBtns[i]);

                    _mainMenuRoot = _mainButtonObj.transform.parent;
                    _mainMenuCG = _mainMenuRoot?.GetComponent<CanvasGroup>()
                                        ?? _mainMenuRoot?.gameObject.AddComponent<CanvasGroup>();
                    _mainMenuItemList = _mainMenuRoot?.GetComponent<MenuItemList>();
                }

                if (foundMenus.Count > 0)
                {
                    _submenuObj = foundMenus[0];
                    _submenuCG = _submenuObj.GetComponent<CanvasGroup>();
                    _submenuItemList = _submenuObj.GetComponent<MenuItemList>();
                    for (int i = 1; i < foundMenus.Count; i++) UnityEngine.Object.Destroy(foundMenus[i]);

                    _fixedButtonInstances.Clear();
                    for (int i = 0; i < _fixedDescs.Count; i++)
                    {
                        var t = _submenuObj.transform.Find($"BBSMP_Fixed_{i}");
                        _fixedButtonInstances.Add(t?.GetComponent<Button>());
                    }
                }
            }

            private void Reset()
            {
                if (_mainButtonObj != null) UnityEngine.Object.Destroy(_mainButtonObj);
                if (_submenuObj    != null) UnityEngine.Object.Destroy(_submenuObj);
                if (_kbRoot        != null) UnityEngine.Object.Destroy(_kbRoot);

                _mainButtonObj = null;
                _mainButton = null;
                _submenuObj = null;
                _submenuCG = null;
                _submenuItemList = null;
                _mainMenuRoot = null;
                _mainMenuCG = null;
                _mainMenuItemList = null;
                _tabMenu = null;
                _contentBuilt = false;
                _fixedButtonInstances.Clear();

                _kbRoot = null;
                _kbCG = null;
                _kbCharRows = null;
                _kbSpecialRow = null;
                _kbLowerRows = null;
                _kbUpperRows = null;
                _kbAllButtons.Clear();
                _kbActiveField = null;
                _kbReturnButton = null;
                _kbBuilt = false;
                _kbShift = false;
                _kbShiftBtn = null;
                _kbNavFrame = -1;
                _kbCursorTimer = 0f;
                _kbCursorVisible = true;
            }

            private static Button FindTemplateButton(Menu menu)
            {
                if (menu.ContinueGameButton != null) return menu.ContinueGameButton;
                if (menu.mainMenuCanvas == null)     return null;

                var all = menu.mainMenuCanvas.GetComponentsInChildren<Button>(true);
                if (all == null || all.Length == 0) return null;

                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    var label = all[i].GetComponentInChildren<TMP_Text>(true)?.text ?? "";
                    if (label.Contains("continue", System.StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
                return all[0];
            }

            private static Toggle FindToggleTemplate(Menu menu)
            {
                if (menu?.mainMenuCanvas == null) return null;
                var all = menu.mainMenuCanvas.GetComponentsInChildren<Toggle>(true);
                return all != null && all.Length > 0 ? all[0] : null;
            }

            private static Slider FindSliderTemplate(Menu menu)
            {
                if (menu?.mainMenuCanvas == null) return null;
                var all = menu.mainMenuCanvas.GetComponentsInChildren<Slider>(true);
                return all != null && all.Length > 0 ? all[0] : null;
            }

            private static void DumpHierarchy(Menu menu, string reason)
            {
                try
                {
                    if (menu == null) return;
                    var sb = new StringBuilder();
                    sb.AppendLine("=== BBSMP Menu Hierarchy Dump ===");
                    sb.AppendLine($"Reason: {reason}");
                    sb.AppendLine($"Time: {System.DateTime.Now:O}");
                    sb.AppendLine($"Screen: {menu.currentMenuScreen}  Paused: {menu.paused}");
                    if (menu.mainMenuCanvas != null)
                    {
                        sb.AppendLine($"CanvasW: {GetPath(menu.mainMenuCanvas.transform)}");
                        AppendTransform(sb, menu.mainMenuCanvas.transform, 0, 6);
                    }
                    string dir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "UserData");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "BBSMP_MenuHierarchyDump.txt"), sb.ToString());
                }
                catch (System.Exception ex) { Core.logger?.Error($"[MenuInject] Dump failed: {ex}"); }
            }

            private static void AppendTransform(StringBuilder sb, Transform root, int depth, int maxDepth)
            {
                if (root == null || depth > maxDepth) return;
                string indent = new string(' ', depth * 2);
                var comps = root.GetComponents<Component>();
                var names = new List<string>();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] != null) names.Add(comps[i].GetType().Name);
                sb.AppendLine($"{indent}- {root.name} [{string.Join(", ", names)}]");
                for (int i = 0; i < root.childCount; i++)
                    AppendTransform(sb, root.GetChild(i), depth + 1, maxDepth);
            }

            private static string GetPath(Transform t)
            {
                if (t == null) return "<null>";
                var stack = new Stack<string>();
                while (t != null) { stack.Push(t.name); t = t.parent; }
                return string.Join("/", stack);
            }
        }

        public static void Configure(RectTransform root, Button button, Toggle toggle = null, Slider slider = null)
        {
            customRoot = root;
            buttonTemplate = button;
            toggleTemplate = toggle;
            sliderTemplate = slider;
            _currentTabMenu = null;
            _currentTabPage = -1;
            ResetLayout();
        }

        public static void ConfigureTabPage(RuntimeTabMenu tabMenu, int pageIndex,
            Button button, Toggle toggle = null, Slider slider = null)
        {
            Configure(tabMenu?.GetPage(pageIndex), button, toggle, slider);
            _currentTabMenu = tabMenu;
            _currentTabPage = pageIndex;
        }

        public static RectTransform GetCustomRoot() => customRoot;

        public static void ClearCustomContent()
        {
            if (customRoot == null) return;
            for (int i = customRoot.childCount - 1; i >= 0; i--)
            {
                var child = customRoot.GetChild(i);
                if (child != null) UnityEngine.Object.Destroy(child.gameObject);
            }
            ResetLayout();
        }

        public static TMP_Text AddLabel(string text, RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null || buttonTemplate == null) return null;

            var obj = UnityEngine.Object.Instantiate(buttonTemplate.gameObject, p);
            obj.name = "BBSMP_Label";
            StripLocalizationComponents(obj);

            var btn = obj.GetComponent<Button>();
            if (btn != null) UnityEngine.Object.Destroy(btn);

            var img = obj.GetComponent<Image>();
            if (img != null) img.enabled = false;

            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = text;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.ForceMeshUpdate();
            }

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight);
            return tmp;
        }

        public static Image AddImage(Color color, float height = DefaultHeight, RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null) return null;

            var obj = new GameObject("BBSMP_Image");
            obj.transform.SetParent(p, false);

            var rect = obj.AddComponent<RectTransform>();
            var img  = obj.AddComponent<Image>();
            img.sprite = null;
            img.color  = color;

            PlaceInLayout(rect, p, height);
            return img;
        }

        public static Button AddButton(string text, UnityAction onClick, RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null || buttonTemplate == null) return null;

            var obj = UnityEngine.Object.Instantiate(buttonTemplate.gameObject, p);
            obj.name = "BBSMP_Button";
            StripLocalizationComponents(obj);

            var button = obj.GetComponent<Button>();
            ClearButtonEvents(button);
            if (button != null && onClick != null)
                button.onClick.AddListener(onClick);

            SetButtonLabel(obj, text);
            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight);

            _currentTabMenu?.RegisterPageSelectable(_currentTabPage, button);
            return button;
        }

        public static Toggle AddToggle(string text, bool initialValue, UnityAction<bool> onValueChanged,
            RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null || toggleTemplate == null) return null;

            var obj = UnityEngine.Object.Instantiate(toggleTemplate.gameObject, p);
            obj.name = "BBSMP_Toggle";
            StripLocalizationComponents(obj);

            var toggle = obj.GetComponent<Toggle>();
            if (toggle == null) return null;

            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.isOn = initialValue;
            if (onValueChanged != null)
                toggle.onValueChanged.AddListener(onValueChanged);

            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = text; tmp.ForceMeshUpdate(); }

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight);

            _currentTabMenu?.RegisterPageSelectable(_currentTabPage, toggle);
            return toggle;
        }

        public static Slider AddSlider(string text, float min, float max, float value,
            UnityAction<float> onValueChanged, bool wholeNumbers = false, RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null || sliderTemplate == null) return null;

            var obj = UnityEngine.Object.Instantiate(sliderTemplate.gameObject, p);
            obj.name = "BBSMP_Slider";
            StripLocalizationComponents(obj);

            var slider = obj.GetComponent<Slider>();
            if (slider == null) return null;

            slider.onValueChanged = new Slider.SliderEvent();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = Mathf.Clamp(value, min, max);
            if (onValueChanged != null)
                slider.onValueChanged.AddListener(onValueChanged);

            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = text; tmp.ForceMeshUpdate(); }
            RectTransform handleRect = slider.handleRect;
            if (handleRect == null)
            {
                var allRects = obj.GetComponentsInChildren<RectTransform>(true);
                for (int hi = 0; hi < allRects.Length; hi++)
                {
                    var r = allRects[hi];
                    if (r == null) continue;
                    var hn = r.gameObject.name;
                    if (hn.IndexOf("Handle", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                        hn.IndexOf("Area",   System.StringComparison.OrdinalIgnoreCase) < 0  &&
                        hn.IndexOf("Slide",  System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        handleRect = r;
                        break;
                    }
                }
            }

            if (handleRect != null)
            {
                var handleImg = handleRect.GetComponent<Image>();
                var handleL = handleRect.GetComponent<LayoutElement>();
                var handleARF = handleRect.GetComponent<AspectRatioFitter>();
                var handleCSF = handleRect.GetComponent<ContentSizeFitter>();
                slider.handleRect = handleRect;
                handleRect.anchorMin = new Vector2(0.5f, 0.5f);
                handleRect.anchorMax = new Vector2(0.5f, 0.5f);
                handleRect.pivot = new Vector2(0.5f, 0.5f);
                handleRect.localScale = Vector3.one;

                var sd = handleRect.sizeDelta;
                float w = Mathf.Abs(sd.x);
                float h = Mathf.Abs(sd.y);
                float sz = SliderHandleSize;
                if (w > 0.01f && h > 0.01f)
                    sz = Mathf.Clamp(Mathf.Min(w, h), SliderHandleSize * 0.5f, SliderHandleSize);
                else if (w > 0.01f)
                    sz = Mathf.Clamp(w * 0.72f, SliderHandleSize * 0.5f, SliderHandleSize);
                else if (h > 0.01f)
                    sz = Mathf.Clamp(h * 0.72f, SliderHandleSize * 0.5f, SliderHandleSize);

                if (handleImg != null)
                {
                    handleImg.sprite = GetOrCreateSliderHandleSprite();
                    handleImg.type = Image.Type.Simple;
                    handleImg.preserveAspect = true;
                    handleImg.raycastTarget = true;
                }

                handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sz);
                handleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sz);

                var le = handleRect.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.minWidth = sz;
                    le.minHeight = sz;
                    le.preferredWidth = sz;
                    le.preferredHeight = sz;
                    le.flexibleWidth = 0f;
                    le.flexibleHeight = 0f;
                }
            }

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight + 12f);

            _currentTabMenu?.RegisterPageSelectable(_currentTabPage, slider);
            return slider;
        }
        public static Button AddInputField(string placeholder = "",
            UnityAction<string> onChange = null, string initialValue = null, RectTransform parent = null)
        {
            var p = parent ?? customRoot;
            if (p == null || buttonTemplate == null) return null;
            var obj = UnityEngine.Object.Instantiate(buttonTemplate.gameObject, p);
            obj.name = "BBSMP_InputField";
            StripLocalizationComponents(obj);

            var btn = obj.GetComponent<Button>();
            ClearButtonEvents(btn);
            var existingTmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (existingTmp != null)
            {
                existingTmp.text = "";
                existingTmp.raycastTarget = false;
            }
            var outerImg = obj.GetComponent<Image>();
            if (outerImg != null)
            {
                outerImg.enabled = false;
                outerImg.raycastTarget = false;
            }
            var pillObj = new GameObject("BBSMP_InputPill");
            pillObj.transform.SetParent(obj.transform, false);
            var pillRect = pillObj.AddComponent<RectTransform>();
            pillRect.anchorMin = Vector2.zero;
            pillRect.anchorMax = Vector2.one;
            pillRect.offsetMin = new Vector2( 4f,  5f);
            pillRect.offsetMax = new Vector2(-4f, -5f);

            var pillImg = pillObj.AddComponent<Image>();
            pillImg.sprite = GetOrCreateInputFieldSprite();
            pillImg.type = Image.Type.Sliced;
            pillImg.color = Color.white;
            var pillNormal = new Color(0.02f, 0.04f, 0.08f, 0.92f);
            var pillSelected = new Color(0.15f, 0.40f, 0.60f, 0.92f);

            if (sliderTemplate != null)
            {
                var sliderImages = sliderTemplate.GetComponentsInChildren<Image>(true);
                Image trackImg = null, fillImg = null;
                for (int si = 0; si < sliderImages.Length; si++)
                {
                    var img = sliderImages[si];
                    if (img == null) continue;
                    var n = img.gameObject.name;
                    if (trackImg == null && (
                            n.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("Track",      System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("Base",       System.StringComparison.OrdinalIgnoreCase) >= 0))
                        trackImg = img;
                    if (fillImg == null && (
                            n.IndexOf("Fill",       System.StringComparison.OrdinalIgnoreCase) >= 0))
                        fillImg = img;
                }
                if (trackImg != null) pillNormal = trackImg.color;
                if (fillImg  != null) pillSelected = fillImg.color;
            }

            var pillPressed = Color.Lerp(pillSelected, Color.white, 0.15f);
            var pillDisabled = new Color(pillNormal.r * 0.6f, pillNormal.g * 0.6f, pillNormal.b * 0.6f, pillNormal.a);
            if (btn != null)
            {
                btn.targetGraphic = pillImg;
                var cb = btn.colors;
                cb.normalColor = pillNormal;
                cb.highlightedColor = pillSelected;
                cb.selectedColor = pillSelected;
                cb.pressedColor = pillPressed;
                cb.disabledColor = pillDisabled;
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.1f;
                btn.colors = cb;
            }
            var viewObj = new GameObject("BBSMP_InputViewport");
            viewObj.transform.SetParent(pillObj.transform, false);
            var viewRect = viewObj.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(10f,  3f);
            viewRect.offsetMax = new Vector2(-10f, -3f);
            viewObj.AddComponent<RectMask2D>();
            TMP_Text valTmp = null;
            if (existingTmp != null)
            {
                var valObj = UnityEngine.Object.Instantiate(existingTmp.gameObject, viewObj.transform);
                valObj.name = "BBSMP_InputValue";
                StripLocalizationComponentsImmediate(valObj);
                var clonedImgs = valObj.GetComponentsInChildren<Image>(true);
                for (int ci = 0; ci < clonedImgs.Length; ci++)
                    if (clonedImgs[ci] != null) UnityEngine.Object.DestroyImmediate(clonedImgs[ci]);

                valTmp = valObj.GetComponent<TMP_Text>();
                Color templateTextColor = existingTmp.color;
                Color templatePlaceholder = new Color(
                    templateTextColor.r, templateTextColor.g, templateTextColor.b,
                    templateTextColor.a * 0.45f);
                
                Color placeholderTextColor = new Color(TabText.r, TabText.g, TabText.b, TabText.a * 0.45f);
                Color activeTextColor = new Color(TabText.r * 0.65f, TabText.g * 0.65f, TabText.b * 0.65f, 1f);

                bool hasInitial = !string.IsNullOrEmpty(initialValue);
                valTmp.text = hasInitial ? initialValue : (placeholder ?? "");
                valTmp.alignment = TextAlignmentOptions.MidlineLeft;
                valTmp.color = hasInitial ? activeTextColor : placeholderTextColor;
                valTmp.enableWordWrapping = false;
                valTmp.overflowMode = TextOverflowModes.Overflow;
                valTmp.fontSize = 19f;
                valTmp.fontStyle = FontStyles.Normal;
                ApplyLowercaseTextFont(valTmp);
                valTmp.ForceMeshUpdate();

                var vr = valObj.GetComponent<RectTransform>();
                if (vr != null)
                {
                    vr.anchorMin = Vector2.zero;
                    vr.anchorMax = Vector2.one;
                    vr.offsetMin = Vector2.zero;
                    vr.offsetMax = Vector2.zero;
                }
            }
            Image cursorImg = null;
            {
                var cursorObj = new GameObject("BBSMP_InputCursor");
                cursorObj.transform.SetParent(viewRect.transform, false);
                var cursorRect = cursorObj.AddComponent<RectTransform>();
                cursorRect.anchorMin = new Vector2(0f, 0.5f);
                cursorRect.anchorMax = new Vector2(0f, 0.5f);
                cursorRect.pivot = new Vector2(0f, 0.5f);
                cursorRect.sizeDelta = new Vector2(2f, 18f);
                cursorRect.anchoredPosition = Vector2.zero;
                cursorImg = cursorObj.AddComponent<Image>();
                cursorImg.color = Color.clear;
                cursorImg.enabled = false;
            }
            var capturedInfo = new InputFieldInfo
            {
                Value = initialValue ?? "",
                DisplayText = valTmp,
                OnChanged = onChange,
                Placeholder = placeholder ?? "",
                Viewport = viewRect,
                ActiveColor = new Color(TabText.r * 0.65f, TabText.g * 0.65f, TabText.b * 0.65f, 1f),
                PlaceholderColor = new Color(TabText.r, TabText.g, TabText.b, TabText.a * 0.45f),
             };
            var capturedBtn = btn;
            var capturedMenu = _currentInjectedMenu;
            capturedInfo.OwnerButton = btn;
            capturedInfo.CursorImage = cursorImg;

            btn?.onClick.AddListener((UnityAction)(() =>
            {
                if (capturedMenu?.IsMouseTyping == true) return;

                if (Input.GetMouseButtonUp(0))
                    capturedMenu?.OpenMouseTyping(capturedInfo);
                else
                    capturedMenu?.OpenKeyboard(capturedInfo, capturedBtn);
            }));

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight);

            _currentTabMenu?.RegisterPageSelectable(_currentTabPage, btn);
            return btn;
        }

        public static RuntimeTabMenu BuildNativeTabMenu(
            string[]      tabNames,
            RectTransform contentRoot,
            Button        nativeTabSource,
            Button        fallbackTemplate)
        {
            if (tabNames == null || tabNames.Length == 0 || contentRoot == null) return null;

            var menu = new RuntimeTabMenu(tabNames)
            {
                Root = contentRoot,
                Tabs = new Button[tabNames.Length],
                Pages = new RectTransform[tabNames.Length],
            };

            const float HeaderH = 56f;

            var headerObj = new GameObject("BBSMP_TabHeader");
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.SetParent(contentRoot, false);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.sizeDelta = new Vector2(0f, HeaderH);
            headerRect.anchoredPosition = Vector2.zero;
            menu.Header = headerRect;

            float tabW = contentRoot.sizeDelta.x > 0f
                ? contentRoot.sizeDelta.x / tabNames.Length
                : 520f / tabNames.Length;

            var tabSrc = nativeTabSource != null ? nativeTabSource.gameObject : fallbackTemplate.gameObject;
            var srcBtn = nativeTabSource ?? fallbackTemplate;
            if (srcBtn != null)
                menu.NativeHighlightedColor = srcBtn.colors.highlightedColor;

            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;

                var tabObj = UnityEngine.Object.Instantiate(tabSrc, headerRect);
                tabObj.name = $"BBSMP_Tab_{i}";
                StripLocalizationComponentsImmediate(tabObj);
                SetButtonLabel(tabObj, tabNames[i]);

                var tabRect = tabObj.GetComponent<RectTransform>();
                if (tabRect != null)
                {
                    tabRect.anchorMin = new Vector2(0f, 0f);
                    tabRect.anchorMax = new Vector2(0f, 1f);
                    tabRect.pivot = new Vector2(0f, 0.5f);
                    tabRect.sizeDelta = new Vector2(tabW - 4f, 0f);
                    tabRect.anchoredPosition = new Vector2(i * tabW, 0f);
                }

                var tabBtn = tabObj.GetComponent<Button>();
                ClearButtonEvents(tabBtn);
                if (tabBtn != null)
                    tabBtn.onClick.AddListener((UnityAction)(() => menu.SetActiveTab(idx)));
                menu.Tabs[i] = tabBtn;

                var pageObj = new GameObject($"BBSMP_Page_{i}");
                var pageRect = pageObj.AddComponent<RectTransform>();
                pageRect.SetParent(contentRoot, false);
                pageRect.anchorMin = Vector2.zero;
                pageRect.anchorMax = Vector2.one;
                pageRect.pivot = new Vector2(0.5f, 1f);
                pageRect.offsetMin = new Vector2(4f, 4f);
                pageRect.offsetMax = new Vector2(-4f, -(HeaderH + 4f));

                var pageCG = pageObj.AddComponent<CanvasGroup>();
                pageCG.alpha = 0f;
                pageCG.interactable = false;
                pageCG.blocksRaycasts = false;
                menu.Pages[i] = pageRect;
            }

            menu.SetActiveTab(0);
            return menu;
        }

        private static void ResetLayout() => nextY = 0f;

        private static void PlaceInLayout(RectTransform rect, RectTransform parent, float height)
        {
            if (rect == null || parent == null) return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);

            if (parent == customRoot)
            {
                rect.anchoredPosition = new Vector2(0f, -nextY);
                nextY += height + Spacing;
            }
        }

        internal static void SetButtonLabel(GameObject obj, string label, bool useLowercaseFont = false)
        {
            if (obj == null) return;
            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) return;

            var comps = tmp.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                if (comp == null) continue;
                if (!IsLocalizationComponent(comp.GetType().FullName)) continue;
                if (comp is UnityEngine.Behaviour b) b.enabled = false;
                UnityEngine.Object.DestroyImmediate(comp);
            }

            tmp.text = label;
            if (useLowercaseFont)
            {
                tmp.fontStyle = FontStyles.Normal;
                ApplyLowercaseTextFont(tmp);
            }
            tmp.ForceMeshUpdate();
        }

        internal static void ClearButtonEvents(Button button)
        {
            if (button != null)
                button.onClick = new Button.ButtonClickedEvent();
        }

        internal static void StripLocalizationComponents(GameObject root)
        {
            if (root == null) return;
            var comps = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                if (comp != null && IsLocalizationComponent(comp.GetType().FullName))
                    UnityEngine.Object.Destroy(comp);
            }
        }

        internal static void StripLocalizationComponentsImmediate(GameObject root)
        {
            if (root == null) return;
            var comps = root.GetComponentsInChildren<Component>(true);
            var toDestroy = new List<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                if (comp != null && IsLocalizationComponent(comp.GetType().FullName))
                    toDestroy.Add(comp);
            }
            for (int i = 0; i < toDestroy.Count; i++)
                UnityEngine.Object.DestroyImmediate(toDestroy[i]);
        }

        private static bool IsLocalizationComponent(string typeName)
            => !string.IsNullOrEmpty(typeName) && (
                typeName.Contains("I2.Loc.Localize", System.StringComparison.Ordinal) ||
                typeName.EndsWith(".Localize",        System.StringComparison.Ordinal) ||
                typeName.Contains("Localized",        System.StringComparison.Ordinal) ||
                typeName.Contains("BBLocalize",       System.StringComparison.Ordinal));
    }

    [HarmonyPatch]
    internal static class Patch_MenuHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Awake))]
        private static void Awake_Postfix(Menu __instance)
        {
            var menus = MenuInjectionLibrary._registeredMenus;
            for (int i = 0; i < menus.Count; i++)
                menus[i].OnMenuAwake(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
        private static void Update_Prefix(Menu __instance)
        {
            var menus = MenuInjectionLibrary._registeredMenus;
            for (int i = 0; i < menus.Count; i++)
                menus[i].OnMenuPreUpdate(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
        private static void Update_Postfix(Menu __instance)
        {
            var menus = MenuInjectionLibrary._registeredMenus;
            for (int i = 0; i < menus.Count; i++)
                menus[i].OnMenuUpdate(__instance);
        }
        
    }
}



