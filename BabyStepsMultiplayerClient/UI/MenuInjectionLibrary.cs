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
        // All InjectedMenus built via MenuBuilder.Build() are registered here.
        // Patch_MenuHooks (below) forwards the game's Menu events to every entry.
        internal static readonly List<InjectedMenu> _registeredMenus = new List<InjectedMenu>();

        private static RectTransform customRoot;
        private static Button        buttonTemplate;
        private static Toggle        toggleTemplate;
        private static Slider        sliderTemplate;
        private static bool          _hasWrittenSliderDiagnosticsHeader;

        // Current tab-page context — set by ConfigureTabPage so Add* methods auto-register.
        private static RuntimeTabMenu _currentTabMenu;
        private static int            _currentTabPage = -1;

        private static float nextY;
        private const  float Spacing       = 10f;
        private const  float DefaultHeight = 52f;

        // Colors matching the native Options-menu tabs
        internal static readonly Color TabUnselected  = new Color(0.435f, 0.676f, 0.726f, 1.00f);
        internal static readonly Color TabSelected    = new Color(1.000f, 1.000f, 1.000f, 0.78f);
        internal static readonly Color TabText        = new Color(0.000f, 0.169f, 0.283f, 1.00f);
        internal static readonly Color TabHighlighted = new Color(0.519f, 0.804f, 0.865f, 1.00f);

        // -----------------------------------------------------------------------
        // RuntimeTabMenu — owns tab state, page visibility, item-list management,
        // and Unity navigation wiring
        // -----------------------------------------------------------------------

        public sealed class RuntimeTabMenu
        {
            public RectTransform   Root   { get; internal set; }
            public RectTransform   Header { get; internal set; }
            public Button[]        Tabs   { get; internal set; }
            public RectTransform[] Pages  { get; internal set; }
            public int ActiveTab { get; private set; }
            public int TabCount  => Tabs?.Length ?? 0;

            private readonly string[]           _tabNames;
            private readonly List<Selectable>[] _pageSelectables;

            internal MenuItemList ManagedItemList { get; set; }

            // Highlight color sampled from the native tab button source so our tabs
            // match the game's exact look when focused by the cursor/controller.
            internal Color NativeHighlightedColor { get; set; } = TabHighlighted;

            // Stored as Selectable so we can wire Unity Navigation between them.
            private Selectable[] _fixedItems;
            internal void SetFixedMenuItems(params Selectable[] items) { _fixedItems = items; }

            internal RuntimeTabMenu(string[] tabNames)
            {
                _tabNames        = tabNames;
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
                ActiveTab = index;

                var sels = _pageSelectables?[index];

                // ── Page visibility & TMP refresh ──────────────────────────────
                for (int i = 0; i < TabCount; i++)
                {
                    bool on = (i == index);

                    if (Pages[i] != null)
                    {
                        var cg = Pages[i].GetComponent<CanvasGroup>();
                        if (cg != null)
                        {
                            cg.alpha          = on ? 1f : 0f;
                            cg.interactable   = on;
                            cg.blocksRaycasts = on;
                        }

                        if (on)
                        {
                            var allTmp = Pages[i].GetComponentsInChildren<TMP_Text>(true);
                            for (int j = 0; j < allTmp.Length; j++)
                                if (allTmp[j] != null) allTmp[j].ForceMeshUpdate();
                        }
                    }

                    // ── Tab button styling (color only — never touch interactable) ──
                    // Setting interactable=false causes cursor loss when navigation
                    // lands on that button, so we signal "active" via color only.
                    if (Tabs[i] != null)
                    {
                        // Unity renders: Graphic.color × CanvasRenderer.color.
                        // If Graphic.color (image.color) is anything other than white,
                        // it tints every ColorBlock state — including highlighted — so
                        // even a pure-white NativeHighlightedColor would still look blue.
                        // Fix: lock image.color to white and let the ColorBlock carry all
                        // appearance data. CrossFadeColor(duration=0) snaps CanvasRenderer
                        // to the correct resting color immediately without a tween.
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
                        cb.normalColor      = stateColor;
                        cb.selectedColor    = NativeHighlightedColor;
                        cb.highlightedColor = NativeHighlightedColor;
                        cb.pressedColor     = NativeHighlightedColor;
                        cb.disabledColor    = stateColor;
                        cb.colorMultiplier  = 1f;
                        Tabs[i].colors = cb;
                    }
                }

                // ── Build unified navigation list ──────────────────────────────
                // [page selectables...] then [fixed selectables...], strict linear
                // chain with null at both ends — no wrapping.
                int pageCount  = sels?.Count        ?? 0;
                int fixedCount = _fixedItems?.Length ?? 0;

                var allSels = new List<Selectable>(pageCount + fixedCount);
                for (int i = 0; i < pageCount;  i++)
                    if (sels[i] != null)        allSels.Add(sels[i]);
                for (int i = 0; i < fixedCount; i++)
                    if (_fixedItems[i] != null) allSels.Add(_fixedItems[i]);

                // ── MenuItemList update ────────────────────────────────────────
                if (ManagedItemList != null)
                {
                    var items = new GameObject[allSels.Count];
                    for (int i = 0; i < allSels.Count; i++)
                        items[i] = allSels[i].gameObject;
                    ManagedItemList.items = items;
                }

                // ── Unity Navigation wiring ────────────────────────────────────
                for (int i = 0; i < allSels.Count; i++)
                {
                    var s   = allSels[i];
                    var nav = s.navigation;
                    nav.mode          = Navigation.Mode.Explicit;
                    nav.selectOnUp    = i > 0                 ? allSels[i - 1] : null;
                    nav.selectOnDown  = i < allSels.Count - 1 ? allSels[i + 1] : null;
                    nav.selectOnLeft  = null;
                    nav.selectOnRight = null;
                    s.navigation = nav;
                }
            }
        }

        // -----------------------------------------------------------------------
        // TabBuilder — thin wrapper passed to tab-content configure callbacks.
        // Context is already set by ConfigureTabPage before each callback fires,
        // so these methods are pure pass-throughs to the static Add* API.
        // -----------------------------------------------------------------------

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
        }

        // -----------------------------------------------------------------------
        // MenuBuilder — fluent descriptor for an injected menu.
        // No Unity objects are created here; all building happens in InjectedMenu.
        // -----------------------------------------------------------------------

        public sealed class MenuBuilder
        {
            internal readonly string _mainButtonLabel;
            internal readonly List<(string name, System.Action<TabBuilder> configure)> _tabs   = new List<(string, System.Action<TabBuilder>)>();
            internal readonly List<(string label, UnityAction action)>                 _fixed  = new List<(string, UnityAction)>();

            internal MenuBuilder(string mainButtonLabel) => _mainButtonLabel = mainButtonLabel;

            public MenuBuilder AddTab(string name, System.Action<TabBuilder> configure)
            {
                _tabs.Add((name, configure));
                return this;
            }

            // action == null → the button simply closes the submenu (use for "Back").
            // A non-null action runs first, then the submenu closes automatically.
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

        // Factory
        public static MenuBuilder CreateMenu(string mainButtonLabel) => new MenuBuilder(mainButtonLabel);

        // -----------------------------------------------------------------------
        // InjectedMenu — owns all live state and all game-loop logic.
        // Created once via MenuBuilder.Build(); call OnMenu* from your hooks.
        // -----------------------------------------------------------------------

        public sealed class InjectedMenu
        {
            // ── Descriptor ────────────────────────────────────────────────────
            private readonly string _mainButtonLabel;
            private readonly List<(string name, System.Action<TabBuilder> configure)> _tabDescs;
            private readonly List<(string label, UnityAction action)>                 _fixedDescs;

            // ── Live state ────────────────────────────────────────────────────
            private Menu        _activeMenu;
            private RuntimeTabMenu _tabMenu;
            private bool        _contentBuilt;
            private bool        _hasAttemptedInitialDump;

            // Main-menu integration
            private GameObject   _mainButtonObj;
            private Button       _mainButton;
            private Transform    _mainMenuRoot;
            private CanvasGroup  _mainMenuCG;
            private MenuItemList _mainMenuItemList;

            // Submenu
            private GameObject   _submenuObj;
            private CanvasGroup  _submenuCG;
            private MenuItemList _submenuItemList;
            private readonly List<Button> _fixedButtonInstances = new List<Button>();

            // Layout constants
            private const float CanvasW    = 560f;
            private const float CanvasH    = 650f;
            private const float ContentH   = 460f;
            private const float ContentPad = 10f;

            internal InjectedMenu(MenuBuilder b)
            {
                _mainButtonLabel = b._mainButtonLabel;
                _tabDescs        = b._tabs;
                _fixedDescs      = b._fixed;
            }

            // ── Game-loop hooks ────────────────────────────────────────────────

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
                if (vis) TryHandleCancel(menu);

                // Shoulder-button tab switching
                if (vis && _tabMenu != null && menu.rwPlayer != null)
                {
                    if (menu.rwPlayer.GetButtonDown((int)InputActions.UITabPrev))
                        _tabMenu.SetActiveTab((_tabMenu.ActiveTab + 1) % _tabMenu.TabCount);
                    else if (menu.rwPlayer.GetButtonDown((int)InputActions.UITabNext))
                        _tabMenu.SetActiveTab((_tabMenu.ActiveTab - 1 + _tabMenu.TabCount) % _tabMenu.TabCount);
                }

                if (vis)
                {
                    if (_submenuItemList != null) MenuItemList.active = _submenuItemList;
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
                    DumpHierarchy(menu, "ManualDump_F8");

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    DumpHierarchy(menu, "PreReinject_F9");
                    Reset();
                    TryInject(menu, "Reinject_F9");
                }

                if (_mainButtonObj == null || _submenuObj == null)
                    TryInject(menu, "UpdateRetry");

                UpdateVisibility(menu);
                EnsureSelection();
            }

            // ── Injection ──────────────────────────────────────────────────────

            private void TryInject(Menu menu, string reason)
            {
                if (menu == null) return;
                ResolveExisting(menu);

                if (!_hasAttemptedInitialDump)
                {
                    _hasAttemptedInitialDump = true;
                    DumpHierarchy(menu, $"Initial_{reason}");
                }

                if (menu.mainMenuCanvas == null)
                {
                    Core.logger?.Warning($"[MenuInject] Canvas null ({reason})");
                    return;
                }

                var tmpl = FindTemplateButton(menu);
                if (tmpl == null)
                {
                    Core.logger?.Warning($"[MenuInject] No template button ({reason})");
                    return;
                }

                _mainMenuRoot = tmpl.transform.parent;
                if (_mainMenuRoot != null)
                {
                    _mainMenuCG       = _mainMenuRoot.GetComponent<CanvasGroup>()
                                        ?? _mainMenuRoot.gameObject.AddComponent<CanvasGroup>();
                    _mainMenuItemList = _mainMenuRoot.GetComponent<MenuItemList>();
                }

                if (_mainButtonObj == null) _mainButtonObj = BuildMainButton(menu, tmpl);
                if (_submenuObj    == null) _submenuObj    = BuildSubmenu(menu, tmpl);

                IntegrateIntoMainItemList();
                EnsureTabContent(menu, tmpl);
                UpdateVisibility(menu);
                Core.logger?.Msg($"[MenuInject] Injection complete ({reason})");
            }

            private void EnsureTabContent(Menu menu, Button tmpl)
            {
                if (_contentBuilt || _submenuObj == null) return;

                var contentRect = _submenuObj.transform.Find("CustomSettingsContentRoot") as RectTransform;
                if (contentRect == null) return;

                // Find native tab button for visual cloning
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

                for (int i = 0; i < _tabDescs.Count; i++)
                {
                    ConfigureTabPage(_tabMenu, i, tmpl, togTmpl, sldTmpl);
                    _tabDescs[i].configure?.Invoke(builder);
                }

                _tabMenu.SetActiveTab(0);
                _contentBuilt = true;
                Core.logger?.Msg("[MenuInject] Tab content built");
            }

            // ── Builders ───────────────────────────────────────────────────────

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
                    cr.anchorMin        = tr.anchorMin;
                    cr.anchorMax        = tr.anchorMax;
                    cr.pivot            = tr.pivot;
                    cr.sizeDelta        = tr.sizeDelta;
                    cr.localScale       = Vector3.one;
                    float step          = Mathf.Max(45f, Mathf.Abs(tr.sizeDelta.y) + 10f);
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
                rect.anchorMin        = new Vector2(0f, 1f);
                rect.anchorMax        = new Vector2(0f, 1f);
                rect.pivot            = new Vector2(0f, 1f);
                rect.sizeDelta        = new Vector2(CanvasW, CanvasH);
                rect.anchoredPosition = new Vector2(10f, -10f);

                _submenuCG                = root.AddComponent<CanvasGroup>();
                _submenuCG.alpha          = 0f;
                _submenuCG.blocksRaycasts = false;
                _submenuCG.interactable   = false;

                // Tab content root
                var contentObj  = new GameObject("CustomSettingsContentRoot");
                contentObj.transform.SetParent(root.transform, false);
                var contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin        = new Vector2(0.5f, 1f);
                contentRect.anchorMax        = new Vector2(0.5f, 1f);
                contentRect.pivot            = new Vector2(0.5f, 1f);
                contentRect.sizeDelta        = new Vector2(520f, ContentH);
                contentRect.anchoredPosition = new Vector2(0f, -ContentPad);

                var tmplRect = tmpl.GetComponent<RectTransform>();
                float btnW = tmplRect != null ? Mathf.Max(200f, tmplRect.sizeDelta.x) : 520f;
                float btnH = tmplRect != null ? Mathf.Clamp(tmplRect.sizeDelta.y, 40f, 72f) : 52f;

                // Fixed buttons — stacked upward from the bottom of the submenu canvas.
                // The last descriptor goes at the bottom (y=10), earlier ones stack above.
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
                    btnRect.pivot     = Vector2.zero;
                    btnRect.sizeDelta = new Vector2(btnW, btnH);

                    int slotFromBottom = (_fixedDescs.Count - 1) - i;
                    btnRect.anchoredPosition = new Vector2(10f, 10f + slotFromBottom * (btnH + 10f));

                    var capturedAction = action;
                    btn?.onClick.AddListener((UnityAction)(() =>
                    {
                        capturedAction?.Invoke();
                        SetSubmenuVisible(false);
                    }));

                    _fixedButtonInstances.Insert(0, btn); // maintain descriptor order
                }

                _submenuItemList       = root.AddComponent<MenuItemList>();
                _submenuItemList.items = new GameObject[0];

                return root;
            }

            // ── Visibility & selection ─────────────────────────────────────────

            private void IntegrateIntoMainItemList()
            {
                if (_mainMenuItemList == null || _mainButtonObj == null) return;

                var old = _mainMenuItemList.items;
                if (old == null || old.Length == 0) return;

                for (int i = 0; i < old.Length; i++)
                    if (old[i] == _mainButtonObj) return; // already integrated

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
                    _mainMenuCG.alpha          = vis ? 0f : 1f;
                    _mainMenuCG.blocksRaycasts = !vis;
                    _mainMenuCG.interactable   = !vis;
                }

                if (menu.titleGroup != null)
                    menu.titleGroup.alpha = vis ? 0f : 1f;
            }

            private void SetSubmenuVisible(bool isVisible)
            {
                if (_submenuCG == null) return;

                bool current = _submenuCG.alpha > 0.5f;
                if (current == isVisible) return;

                _submenuCG.alpha          = isVisible ? 1f : 0f;
                _submenuCG.blocksRaycasts = isVisible;
                _submenuCG.interactable   = isVisible;

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
                if (menu?.rwPlayer == null) return;
                if (Input.GetKeyDown(KeyCode.Escape) || menu.rwPlayer.GetButtonDown((int)InputActions.UICancel))
                    SetSubmenuVisible(false);
            }

            private bool GetSubmenuVisible() => _submenuCG != null && _submenuCG.alpha > 0.5f;

            // ── Lifecycle ─────────────────────────────────────────────────────

            private void ResolveExisting(Menu menu)
            {
                if (menu?.mainMenuCanvas == null) return;

                var transforms = menu.mainMenuCanvas.GetComponentsInChildren<Transform>(true);
                var foundBtns  = new List<GameObject>();
                var foundMenus = new List<GameObject>();

                for (int i = 0; i < transforms.Length; i++)
                {
                    var t = transforms[i];
                    if (t == null) continue;
                    if      (t.name == "BBSMP_MainMenuButton") foundBtns.Add(t.gameObject);
                    else if (t.name == "BBSMP_SubmenuCanvas")  foundMenus.Add(t.gameObject);
                }

                if (foundBtns.Count > 0)
                {
                    _mainButtonObj = foundBtns[0];
                    _mainButton    = _mainButtonObj.GetComponent<Button>();
                    for (int i = 1; i < foundBtns.Count; i++) UnityEngine.Object.Destroy(foundBtns[i]);

                    _mainMenuRoot     = _mainButtonObj.transform.parent;
                    _mainMenuCG       = _mainMenuRoot?.GetComponent<CanvasGroup>()
                                        ?? _mainMenuRoot?.gameObject.AddComponent<CanvasGroup>();
                    _mainMenuItemList = _mainMenuRoot?.GetComponent<MenuItemList>();
                }

                if (foundMenus.Count > 0)
                {
                    _submenuObj      = foundMenus[0];
                    _submenuCG       = _submenuObj.GetComponent<CanvasGroup>();
                    _submenuItemList  = _submenuObj.GetComponent<MenuItemList>();
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

                _mainButtonObj    = null;
                _mainButton       = null;
                _submenuObj       = null;
                _submenuCG        = null;
                _submenuItemList  = null;
                _mainMenuRoot     = null;
                _mainMenuCG       = null;
                _mainMenuItemList = null;
                _tabMenu          = null;
                _contentBuilt     = false;
                _fixedButtonInstances.Clear();
            }

            // ── Template discovery ─────────────────────────────────────────────

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

            // ── Diagnostics ────────────────────────────────────────────────────

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
                        sb.AppendLine($"Canvas: {GetPath(menu.mainMenuCanvas.transform)}");
                        AppendTransform(sb, menu.mainMenuCanvas.transform, 0, 6);
                    }
                    string dir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "UserData");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "BBSMP_MenuHierarchyDump.txt"), sb.ToString());
                    Core.logger?.Msg("[MenuInject] Hierarchy dump written");
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

        // -----------------------------------------------------------------------
        // Configuration — called by EnsureTabContent / consumers
        // -----------------------------------------------------------------------

        public static void Configure(RectTransform root, Button button, Toggle toggle = null, Slider slider = null)
        {
            customRoot      = root;
            buttonTemplate  = button;
            toggleTemplate  = toggle;
            sliderTemplate  = slider;
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

        // -----------------------------------------------------------------------
        // Element builders
        // -----------------------------------------------------------------------

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
                tmp.text      = text;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.ForceMeshUpdate();
            }

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, DefaultHeight);
            return tmp;
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
            toggle.isOn           = initialValue;
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
            obj.transform.localScale = Vector3.one;

            var slider = obj.GetComponent<Slider>();
            if (slider == null) return null;

            slider.onValueChanged = new Slider.SliderEvent();
            slider.minValue       = min;
            slider.maxValue       = max;
            slider.wholeNumbers   = wholeNumbers;
            slider.value          = Mathf.Clamp(value, min, max);
            if (onValueChanged != null)
                slider.onValueChanged.AddListener(onValueChanged);

            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = text; tmp.ForceMeshUpdate(); }

            float sliderHeight = DefaultHeight;
            var templateRect = sliderTemplate.GetComponent<RectTransform>();
            if (templateRect != null && templateRect.sizeDelta.y > 0f)
                sliderHeight = templateRect.sizeDelta.y;

            PlaceInLayout(obj.GetComponent<RectTransform>(), p, sliderHeight);

            LogSliderComparison(sliderTemplate.gameObject, obj, text);

            _currentTabMenu?.RegisterPageSelectable(_currentTabPage, slider);
            return slider;
        }

        // -----------------------------------------------------------------------
        // Tab menu factory
        // -----------------------------------------------------------------------

        public static RuntimeTabMenu BuildNativeTabMenu(
            string[]      tabNames,
            RectTransform contentRoot,
            Button        nativeTabSource,
            Button        fallbackTemplate)
        {
            if (tabNames == null || tabNames.Length == 0 || contentRoot == null) return null;

            var menu = new RuntimeTabMenu(tabNames)
            {
                Root  = contentRoot,
                Tabs  = new Button[tabNames.Length],
                Pages = new RectTransform[tabNames.Length],
            };

            const float HeaderH = 56f;

            var headerObj  = new GameObject("BBSMP_TabHeader");
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.SetParent(contentRoot, false);
            headerRect.anchorMin        = new Vector2(0f, 1f);
            headerRect.anchorMax        = new Vector2(1f, 1f);
            headerRect.pivot            = new Vector2(0f, 1f);
            headerRect.sizeDelta        = new Vector2(0f, HeaderH);
            headerRect.anchoredPosition = Vector2.zero;
            menu.Header = headerRect;

            float tabW = contentRoot.sizeDelta.x > 0f
                ? contentRoot.sizeDelta.x / tabNames.Length
                : 520f / tabNames.Length;

            var tabSrc = nativeTabSource != null ? nativeTabSource.gameObject : fallbackTemplate.gameObject;

            // Sample the highlight color from the native source so our tabs match the
            // game's look exactly when the cursor hovers over them.
            var srcBtn = nativeTabSource ?? fallbackTemplate;
            if (srcBtn != null)
                menu.NativeHighlightedColor = srcBtn.colors.highlightedColor;

            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;

                var tabObj  = UnityEngine.Object.Instantiate(tabSrc, headerRect);
                tabObj.name = $"BBSMP_Tab_{i}";
                StripLocalizationComponentsImmediate(tabObj);
                SetButtonLabel(tabObj, tabNames[i]);

                var tabRect = tabObj.GetComponent<RectTransform>();
                if (tabRect != null)
                {
                    tabRect.anchorMin        = new Vector2(0f, 0f);
                    tabRect.anchorMax        = new Vector2(0f, 1f);
                    tabRect.pivot            = new Vector2(0f, 0.5f);
                    tabRect.sizeDelta        = new Vector2(tabW - 4f, 0f);
                    tabRect.anchoredPosition = new Vector2(i * tabW, 0f);
                }

                var tabBtn = tabObj.GetComponent<Button>();
                ClearButtonEvents(tabBtn);
                if (tabBtn != null)
                    tabBtn.onClick.AddListener((UnityAction)(() => menu.SetActiveTab(idx)));
                menu.Tabs[i] = tabBtn;

                var pageObj  = new GameObject($"BBSMP_Page_{i}");
                var pageRect = pageObj.AddComponent<RectTransform>();
                pageRect.SetParent(contentRoot, false);
                pageRect.anchorMin = Vector2.zero;
                pageRect.anchorMax = Vector2.one;
                pageRect.pivot     = new Vector2(0.5f, 1f);
                pageRect.offsetMin = new Vector2(4f, 4f);
                pageRect.offsetMax = new Vector2(-4f, -(HeaderH + 4f));

                var pageCG = pageObj.AddComponent<CanvasGroup>();
                pageCG.alpha          = 0f;
                pageCG.interactable   = false;
                pageCG.blocksRaycasts = false;
                menu.Pages[i] = pageRect;
            }

            menu.SetActiveTab(0);
            return menu;
        }

        // -----------------------------------------------------------------------
        // Internal helpers
        // -----------------------------------------------------------------------

        private static void ResetLayout() => nextY = 0f;

        private static void PlaceInLayout(RectTransform rect, RectTransform parent, float height)
        {
            if (rect == null || parent == null) return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);

            if (parent == customRoot)
            {
                rect.anchoredPosition = new Vector2(0f, -nextY);
                nextY += height + Spacing;
            }
        }

        internal static void SetButtonLabel(GameObject obj, string label)
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
            var comps     = root.GetComponentsInChildren<Component>(true);
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

        private static void LogSliderComparison(GameObject nativeSlider, GameObject injectedSlider, string label)
        {
            try
            {
                string dir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "UserData");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir, "BBSMP_SliderDiagnostics.txt");

                var sb = new StringBuilder();
                if (!_hasWrittenSliderDiagnosticsHeader)
                {
                    _hasWrittenSliderDiagnosticsHeader = true;
                    sb.AppendLine("=== BBSMP Slider Diagnostics ===");
                    sb.AppendLine("Compares native slider template vs injected slider instance.");
                    sb.AppendLine();
                }

                sb.AppendLine($"Time: {System.DateTime.Now:O}");
                sb.AppendLine($"Label: {label}");
                sb.AppendLine("-- Native Template --");
                AppendSliderTree(sb, nativeSlider?.transform, 0, 4);
                sb.AppendLine("-- Injected Instance --");
                AppendSliderTree(sb, injectedSlider?.transform, 0, 4);
                sb.AppendLine();

                System.IO.File.AppendAllText(file, sb.ToString());
                Core.logger?.Msg("[MenuInject] Slider diagnostics appended: UserData/BBSMP_SliderDiagnostics.txt");
            }
            catch (System.Exception ex)
            {
                Core.logger?.Warning($"[MenuInject] Slider diagnostics failed: {ex.Message}");
            }
        }

        private static void AppendSliderTree(StringBuilder sb, Transform root, int depth, int maxDepth)
        {
            if (root == null || depth > maxDepth) return;

            string indent = new string(' ', depth * 2);
            var rt = root as RectTransform;

            if (rt != null)
            {
                sb.AppendLine($"{indent}- {root.name} [RectTransform] localScale={root.localScale} lossyScale={root.lossyScale} size={rt.sizeDelta} anchorMin={rt.anchorMin} anchorMax={rt.anchorMax} pivot={rt.pivot} anchoredPos={rt.anchoredPosition}");
            }
            else
            {
                sb.AppendLine($"{indent}- {root.name} [Transform] localScale={root.localScale} lossyScale={root.lossyScale}");
            }

            var image = root.GetComponent<Image>();
            if (image != null)
                sb.AppendLine($"{indent}  Image: enabled={image.enabled} type={image.type} preserveAspect={image.preserveAspect} sprite={(image.sprite != null ? image.sprite.name : "<null>")}");

            for (int i = 0; i < root.childCount; i++)
                AppendSliderTree(sb, root.GetChild(i), depth + 1, maxDepth);
        }
    }

    // -----------------------------------------------------------------------
    // Generic Harmony patch — forwards Menu events to every registered
    // InjectedMenu. Consumer files never need to touch this; just call
    // MenuInjectionLibrary.CreateMenu(...).Build() and it hooks in automatically.
    // -----------------------------------------------------------------------

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
