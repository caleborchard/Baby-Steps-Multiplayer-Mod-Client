using BabyStepsMenuLib;
using BabyStepsMultiplayerClient.Localization;
using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.Networking.Steam;
using BabyStepsMultiplayerClient.Player;
using BabyStepsNetworking.ServerBrowser;
using Il2CppTMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyStepsMultiplayerClient.UI
{
    public static class MultiplayerMenu
    {
        private static MenuInjectionLibrary.InjectedMenu _menu;

        private static Toggle _collisionToggle;
        private static Toggle _cutsceneToggle;
        private static Toggle _nametagToggle;
        private static Button _pttKeyBtn;
        private static Button _tabMenuKeyBtn;
        private static Button _chatMenuKeyBtn;
        private static Button _audioDeviceBtn;
        private static float  _lastAudioDeviceLabelRefresh = -10f;
        private static Image  _colorPreviewImage;

        private static KeybindCaptureTarget _keybindCaptureTarget;
        private static bool _waitingForRelease;
        private static BaseInputModule _cachedInputModule;
        private static bool _playerMovementSuppressedForCapture;
        private static bool _playerMovementWasEnabledBeforeCapture;

        private enum KeybindCaptureTarget
        {
            None,
            PushToTalk,
            TabMenu,
            ChatMenu,
        }

        private static Button _connectTabBtn;

        // Host buttons now live in the Steam tab (not fixed buttons).
        private static Button _hostLanTabBtn;
        private static Button _hostSteamTabBtn;

        // Language-change refresh
        private static string _lastKnownLanguage;
        private static TMP_Text _lobbyBrowserLabel;
        private static TMP_Text _audioDeviceStaticLabel;
        private static TMP_Text _gainLabel;

        // Tab button refs (cached on first menu open for direct text updates)
        private static Button _tabBtn0, _tabBtn1, _tabBtn2, _tabBtn3, _tabBtn4;

        // Audio tab toggle refs
        private static Toggle _microphoneToggle;
        private static Toggle _deafenToggle;
        private static Toggle _pttToggle;

        // Player tab
        private static Button _updateAppearanceBtn;

        // LAN tab label refs
        private static TMP_Text _serverIpLabel;
        private static TMP_Text _serverPortLabel;
        private static TMP_Text _passwordOptLabel;

        // LAN tab input field info refs (for placeholder refresh)
        private static MenuInjectionLibrary.InputFieldInfo _serverIpInputInfo;
        private static MenuInjectionLibrary.InputFieldInfo _serverPortInputInfo;
        private static MenuInjectionLibrary.InputFieldInfo _lanPasswordInputInfo;

        // Lobby password input info ref
        private static MenuInjectionLibrary.InputFieldInfo _hostingPasswordInputInfo;

        // Password popup button text refs
        private static TMP_Text _pwEnterTmp;
        private static TMP_Text _pwBackTmp;
        public static bool IsCapturingKeybind => _keybindCaptureTarget != KeybindCaptureTarget.None;

        // Public main dedicated server
        private static string PublicMainAddress
        {
            get
            {
                var ov = ModSettings.connection.PublicMainIpOverride?.Value;
                return !string.IsNullOrEmpty(ov) ? ov : "bbsmm.mooo.com";
            }
        }
        private const int    PublicMainPort       = 7777;
        private const int    PublicMainStatusPort = PublicMainPort + 1; // raw UDP status listener, no LiteNetLib
        private const string PublicMainName       = "Public Main";

        // Server browser state
        private static SteamLobbyBrowser _lobbyBrowser;
        private static readonly List<ServerInfo> _foundLobbies  = new(); // raw Steam results
        private static readonly List<ServerInfo> _displayLobbies = new(); // merged, sorted display list
        private static readonly List<Button> _lobbyButtons = new();
        private static Button _refreshBtn;
        private const int PageSize = 5;
        private static int   _lobbyPage = 0;
        private static Button    _prevPageBtn;
        private static TMP_Text  _pageLabel;
        private static Button    _nextPageBtn;

        // Dedicated server status query — tuple so no cross-assembly type is needed
        private static (int PlayerCount, int MaxPlayers, bool IsLocked)? _publicMainStatus;
        private static Task<(int PlayerCount, int MaxPlayers, bool IsLocked)?>? _publicMainQueryTask;

        // Tracks which server the local player is currently connected to (for real-time count)
        private static ServerInfo _connectedServerInfo;
        private static int        _lastDisplayedPlayerCount = -1;

        // Set once after the menu tabs are lazily built — used to trigger the first populate
        private static bool _buttonsWereReady = false;

        // (Invite UI removed — use Steam's built-in friend invite via the overlay instead.)

        // Password popup state
        private static ServerInfo _pendingLockedLobby;
        private static string     _pendingJoinPassword = string.Empty;

        // Password popup canvas overlay
        private static UnityEngine.GameObject                          _pwCanvas;
        private static Il2CppTMPro.TMP_Text                           _pwTitle;
        private static Button                                          _pwInputBtn; // the popup's input field button
        private static BabyStepsMenuLib.MenuInjectionLibrary.InputFieldInfo _pwInputInfo;
        private static float                                           _pwEnterDebounce = -10f;

        // Lock icons — one child Image per lobby button slot
        private static UnityEngine.Sprite             _lockSprite;
        private static readonly UnityEngine.GameObject[] _lobbyLockIcons = new UnityEngine.GameObject[PageSize];

        // Popup button references (needed to drive controller focus)
        private static Button _pwEnterBtn;
        private static Button _pwBackBtn;

        public static void Initialize()
        {
            if (_menu != null) return;

            MenuInjectionLibrary.Logger = Core.logger;
            MenuInjectionLibrary.IsCapturingKeybindProvider = () => IsCapturingKeybind;

            // Block menu navigation while the password popup is open without affecting
            // keyboard overlay navigation (IsOverlayBlockingMenuProvider skips the
            // HandleKeyboardInput guard that IsCapturingKeybindProvider would hit).
            MenuInjectionLibrary.IsOverlayBlockingMenuProvider = () =>
                _pwCanvas != null && _pwCanvas.activeSelf;
            MenuInjectionLibrary.OnChatKeyboardCancelled += () => Core.uiManager.showChatTab = false;

            ModSettings.Load();

            _lobbyBrowser = new SteamLobbyBrowser();
            _lobbyBrowser.ServersFound += OnLobbiesFound;

            var initLang = LanguageManager.GetCurrentLanguage();
            _lastKnownLanguage = LanguageManager.CurrentLanguage;
            _menu = MenuInjectionLibrary.CreateMenu("Multiplayer")
                .AddTab(initLang.TabLobby,   ConfigureServersTab)
                .AddTab(initLang.TabPlayer,  ConfigurePlayerTab)
                .AddTab(initLang.TabGeneral, ConfigureGeneralTab)
                .AddTab(initLang.TabAudio,   ConfigureAudioTab)
                .AddTab(initLang.TabLAN,     ConfigureConnectionTab)
                .AddFixedButton(initLang.ButtonBack)
                .WithMargin(136f, 125f)
                .Build();

            Core.OnConnectionStateChanged += RefreshConnectionState;
            Core.OnConnectionStateChanged += RefreshHostState;
            Core.OnConnectionStateChanged += RefreshHostSteamState;

            // Populate the lobby list immediately on first open.
            // RebuildLobbyUI shows Public Main with "?" count while the query is in flight.
            _publicMainQueryTask = DedicatedServerQuery.QueryAsync(PublicMainAddress, PublicMainStatusPort);
            _lobbyBrowser?.Refresh();
            RebuildLobbyUI();
        }

        private static float _lastAutoLobbyRefresh = -30f; // negative so first refresh fires immediately

        public static void Update()
        {
            // Refresh UI text when the game language changes.
            var currentLang = LanguageManager.CurrentLanguage;
            if (currentLang != _lastKnownLanguage)
            {
                _lastKnownLanguage = currentLang;
                RefreshLocalizableText();
            }

            // Tick the lobby browser so async results arrive.
            _lobbyBrowser?.Poll();

            // One-shot: the moment the menu tabs finish their lazy build (game canvas ready),
            // populate the lobby list using whatever cached data is already available.
            // _displayLobbies.Count == 0 is NOT a safe guard — RebuildLobbyUI() is called from
            // Initialize() before buttons exist, which populates _displayLobbies but updates no
            // visible buttons, so the count is already > 0 when the real buttons appear.
            if (!_buttonsWereReady && _lobbyButtons.Count > 0)
            {
                _buttonsWereReady = true;

                // Cache tab button refs now that the menu content is built.
                _tabBtn0 = _menu?.GetTab(0);
                _tabBtn1 = _menu?.GetTab(1);
                _tabBtn2 = _menu?.GetTab(2);
                _tabBtn3 = _menu?.GetTab(3);
                _tabBtn4 = _menu?.GetTab(4);

                // Apply the current language to any UI elements whose refs now exist.
                // This handles the case where language was changed before the menu was
                // ever opened (so the RefreshLocalizableText() fired during the language-change
                // event found null refs and couldn't update them).
                RefreshLocalizableText();

                // Collapse the three pagination elements into one horizontal row.
                RepositionPaginationRow();

                // Nudge the Back fixed button a bit lower.
                var backBtn = _menu?.GetFixedButton(0);
                if (backBtn != null)
                {
                    var rt = backBtn.GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y - 135f);
                }

                RebuildLobbyUI(); // uses _foundLobbies + _publicMainStatus already cached from init queries
                // Re-query the dedicated server in case the init query result has expired or timed out
                if (_publicMainQueryTask == null || _publicMainQueryTask.IsCompleted)
                    _publicMainQueryTask = DedicatedServerQuery.QueryAsync(PublicMainAddress, PublicMainStatusPort);
            }

            // When the public main query finishes, update the display.
            if (_publicMainQueryTask != null && _publicMainQueryTask.IsCompleted)
            {
                _publicMainStatus    = _publicMainQueryTask.Result;
                _publicMainQueryTask = null;
                RebuildLobbyUI();
            }

            // Keep the connected server's player count in sync with the live tab menu.
            if (Core.networkManager.IsConnected && _connectedServerInfo != null)
            {
                int live = Core.networkManager.players.Values.Count(p => p.firstAppearanceApplication) + 1;
                if (live != _lastDisplayedPlayerCount)
                {
                    _lastDisplayedPlayerCount = live;
                    RebuildLobbyUI();
                }
            }
            else if (!Core.networkManager.IsConnected && _connectedServerInfo != null)
            {
                _connectedServerInfo          = null;
                _lastDisplayedPlayerCount     = -1;
                RebuildLobbyUI();
            }

            // Auto-refresh the lobby list every 30 s when idle.
            float now2 = UnityEngine.Time.realtimeSinceStartup;
            if (!Core.networkManager.IsConnected
                && !(Core.inGameHost?.IsRunning ?? false)
                && now2 - _lastAutoLobbyRefresh >= 30f)
            {
                _lastAutoLobbyRefresh = now2;
                _lobbyBrowser?.Refresh();
                // Also silently re-query the dedicated server for a fresh player count.
                if (_publicMainQueryTask == null || _publicMainQueryTask.IsCompleted)
                    _publicMainQueryTask = DedicatedServerQuery.QueryAsync(PublicMainAddress, PublicMainStatusPort);
            }


            // Refresh audio device label every few seconds (handles cold start and device changes).
            if (_audioDeviceBtn != null)
            {
                float now3 = UnityEngine.Time.realtimeSinceStartup;
                if (now3 - _lastAudioDeviceLabelRefresh >= 3f)
                {
                    _lastAudioDeviceLabelRefresh = now3;
                    SetBtnText(_audioDeviceBtn, GetAudioDeviceLabel());
                }
            }

            if (_keybindCaptureTarget == KeybindCaptureTarget.None) return;

            UpdateGameplayInputSuppressionForCapture();

            if (_waitingForRelease)
            {
                // Wait until all input is released before listening for a new press.
                if (InputBindingHelper.IsInputHeldForCapture())
                    return;
                _waitingForRelease = false;
                return;
            }

            if (InputBindingHelper.TryCapturePressedBinding(out string binding, out string displayName))
            {
                ApplyCapturedKeybind(binding, displayName);
            }
        }

        // ── Tab builders ─────────────────────────────────────────────────────

        private static void ConfigurePlayerTab(MenuInjectionLibrary.TabBuilder tab)
        {
            var lang  = LanguageManager.GetCurrentLanguage();
            var color = ModSettings.player.SuitColor.Value;

            _updateAppearanceBtn = tab.AddButton(lang.UpdateNameAndAppearance, (UnityAction)OnUpdateAppearanceClicked);
            tab.AddInputField(lang.Nickname,
                (UnityAction<string>)(v => ModSettings.player.Nickname.Value = v),
                ModSettings.player.Nickname.Value);

            Slider rSlider = null, gSlider = null, bSlider = null;

            rSlider = tab.AddSlider("R:", 0f, 1f, color.r,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.r = val;
                    ModSettings.player.SuitColor.Value = c;
                    RefreshColorPreview();
                }));

            gSlider = tab.AddSlider("G:", 0f, 1f, color.g,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.g = val;
                    ModSettings.player.SuitColor.Value = c;
                    RefreshColorPreview();
                }));

            bSlider = tab.AddSlider("B:", 0f, 1f, color.b,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.b = val;
                    ModSettings.player.SuitColor.Value = c;
                    RefreshColorPreview();
                }));

            _colorPreviewImage = tab.AddImage(color, 40f);
        }

        private static void ConfigureConnectionTab(MenuInjectionLibrary.TabBuilder tab)
        {
            var lang = LanguageManager.GetCurrentLanguage();

            _serverIpLabel = tab.AddLabel(lang.ServerIP);
            tab.AddInputField(lang.ServerIP,
                (UnityAction<string>)(v => ModSettings.connection.Address.Value = v),
                ModSettings.connection.Address.Value);
            _serverIpInputInfo = MenuInjectionLibrary.LastCreatedInputFieldInfo;

            _serverPortLabel = tab.AddLabel(lang.ServerPort);
            tab.AddInputField(lang.ServerPort,
                (UnityAction<string>)(v => { if (int.TryParse(v, out int p)) ModSettings.connection.Port.Value = p; }),
                ModSettings.connection.Port.Value.ToString());
            _serverPortInputInfo = MenuInjectionLibrary.LastCreatedInputFieldInfo;

            _passwordOptLabel = tab.AddLabel(lang.PasswordOptional);
            tab.AddInputField(lang.PasswordOptional,
                (UnityAction<string>)(v => ModSettings.connection.Password.Value = v),
                ModSettings.connection.Password.Value);
            _lanPasswordInputInfo = MenuInjectionLibrary.LastCreatedInputFieldInfo;

            _connectTabBtn = tab.AddButton(GetConnectLabel(), (UnityAction)OnConnectClicked);
            _hostLanTabBtn = tab.AddButton(GetHostLabel(), (UnityAction)OnHostClicked);
        }

        private static void ConfigureGeneralTab(MenuInjectionLibrary.TabBuilder tab)
        {
            _collisionToggle = tab.AddToggle(GetCollisionToggleLabel(), ModSettings.player.Collisions.Value,
                (UnityAction<bool>)OnCollisionToggled);
            _cutsceneToggle  = tab.AddToggle(GetCutsceneToggleLabel(), ModSettings.player.CutscenePlayerVisibility.Value,
                (UnityAction<bool>)OnCutsceneToggled);
            _nametagToggle   = tab.AddToggle(GetNametagToggleLabel(), ModSettings.player.ShowNametags.Value,
                (UnityAction<bool>)OnNametagToggled);

            _tabMenuKeyBtn = tab.AddButton(GetTabMenuKeyLabel(), (UnityAction)OnTabMenuKeyClicked);
            _chatMenuKeyBtn = tab.AddButton(GetChatMenuKeyLabel(), (UnityAction)OnChatMenuKeyClicked);

        }

        private static void ConfigureAudioTab(MenuInjectionLibrary.TabBuilder tab)
        {
            _microphoneToggle = tab.AddToggle(GetMicrophoneToggleLabel(), ModSettings.audio.MicrophoneEnabled.Value,
                (UnityAction<bool>)OnMicrophoneToggled);
            _deafenToggle = tab.AddToggle(GetDeafenToggleLabel(), ModSettings.audio.Deafened.Value,
                (UnityAction<bool>)OnDeafenToggled);
            _pttToggle = tab.AddToggle(GetPushToTalkToggleLabel(), ModSettings.audio.PushToTalk.Value,
                (UnityAction<bool>)OnPushToTalkToggled);
            _pttKeyBtn = tab.AddButton(GetPttKeyLabel(), (UnityAction)OnPttKeyClicked);
            _pttKeyBtn.interactable = ModSettings.audio.PushToTalk.Value;

            Slider gainSlider = null;
            _gainLabel  = tab.AddLabel(GetGainLabel());
            gainSlider = tab.AddSlider("", 0f, 3f, ModSettings.audio.MicrophoneGain.Value,
                (UnityAction<float>)(val =>
                {
                    ModSettings.audio.MicrophoneGain.Value = val;
                    LocalPlayer.Instance?.mic.SetGain(val);
                    if (_gainLabel != null) { _gainLabel.text = GetGainLabel(); _gainLabel.ForceMeshUpdate(); }
                }));

            _audioDeviceStaticLabel = tab.AddLabel(LanguageManager.GetCurrentLanguage().AudioDevice);
            _audioDeviceBtn = tab.AddButton(GetAudioDeviceLabel(), (UnityAction)OnAudioDeviceCycled);
        }

        // Steam join-by-ID field value
        private static string _steamJoinId = string.Empty;

        // Captured once during tab build — used as template for the popup input field.
        private static Button _hostingPasswordBtn;

        private static void ConfigureServersTab(MenuInjectionLibrary.TabBuilder tab)
        {
            // ── Hosting / disconnect ──────────────────────────────────────────────
            tab.AddImage(Color.clear, 10f);
            var lang = LanguageManager.GetCurrentLanguage();
            _hostSteamTabBtn = tab.AddButton(GetHostSteamLabel(), (UnityAction)OnHostSteamClicked);
            _hostingPasswordBtn = tab.AddInputField(lang.LobbyPassword,
                (UnityAction<string>)(v => ModSettings.connection.HostPassword.Value = v),
                ModSettings.connection.HostPassword.Value);
            _hostingPasswordInputInfo = MenuInjectionLibrary.LastCreatedInputFieldInfo;

            // ── [DEBUG] Password popup test ───────────────────────────────────────
            // tab.AddButton("[DEBUG] Open Password Popup", (UnityAction)(() =>
            // {
            //     var dummy = new BabyStepsNetworking.ServerBrowser.ServerInfo
            //     {
            //         Name               = "🔒 Test Session",
            //         Address            = "0",
            //         IsPasswordProtected = true,
            //     };
            //     ShowPasswordPrompt(dummy);
            // }));

            tab.AddImage(Color.clear, 24f);

            // ── Lobby browser ─────────────────────────────────────────────────────
            _lobbyBrowserLabel = tab.AddLabel(lang.LobbyBrowser);
            _refreshBtn = tab.AddButton(lang.Refresh, (UnityAction)OnRefreshLobbies);

            // Three elements laid out vertically here; RepositionPaginationRow() collapses
            // them into a single horizontal row once the game canvas is ready.
            _prevPageBtn = tab.AddButton("<", (UnityAction)OnPrevPage);
            _pageLabel   = tab.AddLabel("1/1");
            _nextPageBtn = tab.AddButton(">", (UnityAction)OnNextPage);

            for (int i = 0; i < PageSize; i++)
            {
                int captured = i;
                var btn = tab.AddButton("---", (UnityAction)(() => OnLobbyButtonClicked(captured)));
                btn.interactable = false;
                var cb = btn.colors;
                cb.disabledColor = new Color(cb.disabledColor.r, cb.disabledColor.g, cb.disabledColor.b, 0.8f);
                btn.colors = cb;
                _lobbyButtons.Add(btn);
            }

            // Build the password popup HERE — _currentInjectedMenu is still set during Build,
            // so AddInputField captures the correct menu reference for the keyboard/mouse trigger.
            BuildPasswordPopup();
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private static void OnConnectClicked()
        {
            if (Core.networkManager.IsConnected)
            {
                Core.networkManager.Disconnect();
            }
            else
            {
                // Mouse input fires this handler twice per click (same double-fire issue as
                // OnHostSteamClicked). Guard the connect path so only the first call goes
                // through; duplicates within 0.5 s are silently dropped.
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (now - _lastConnectClickTime < 0.5f) return;
                _lastConnectClickTime = now;

                ModSettings.Save();
                Core.networkManager.Connect(
                    ModSettings.connection.Address.Value,
                    ModSettings.connection.Port.Value,
                    ModSettings.connection.Password.Value);
            }
        }

        private static void OnHostSteamClicked()
        {
            // Mouse input fires this handler twice per click (down + up or similar).
            // Both calls land sequentially in the same frame so a flag-based guard is
            // cleared before the second call checks it.  A time-based debounce works:
            // ignore any call that arrives within 500 ms of the previous one.
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _lastHostSteamClickTime < 0.5f) return;
            _lastHostSteamClickTime = now;

            {
                bool isHosting = Core.inGameHost?.IsRunning ?? false;

                if (!isHosting && Core.networkManager.IsConnected)
                {
                    Core.networkManager.Disconnect();
                    return;
                }

                if (isHosting)
                {
                    // Null first so RefreshHostSteamState reads correct state after Disconnect fires.
                    Core.inGameHost?.Stop();
                    Core.inGameHost = null;
                    _ownLobbyId = 0;
                    Core.networkManager.Disconnect();
                    Core.uiManager.notificationsUI.AddMessage(LanguageManager.GetCurrentLanguage().StoppedHosting);
                    RefreshHostSteamState();
                    // Clear the lobby from the browser now that we've stopped.
                    OnRefreshLobbies();
                }
                else
                {
                    ModSettings.Save();
                    string password = ModSettings.connection.HostPassword.Value;

                    var host = new Networking.InGameHost();
                    host.PlayerJoined += name => Core.uiManager.notificationsUI.AddMessage(string.Format(LanguageManager.GetCurrentLanguage().PlayerJoinedSession, name));
                    host.PlayerLeft  += name => Core.uiManager.notificationsUI.AddMessage(string.Format(LanguageManager.GetCurrentLanguage().PlayerLeftSession, name));

                    // StartSteam returns the loopback client transport for our own connection.
                    var loopback = host.StartSteam(password);

                    host.LobbyCreated += lobbyIdStr =>
                    {
                        ulong.TryParse(lobbyIdStr, out _ownLobbyId);
                        RefreshHostSteamState();
                        Core.networkManager.ConnectLoopback(loopback);
                        // Show our own lobby in the browser immediately after it's ready.
                        OnRefreshLobbies();
                    };

                    Core.inGameHost = host;
                    Core.uiManager.notificationsUI.AddMessage(LanguageManager.GetCurrentLanguage().CreatingLobby);
                }
            }
        }

        private static void OnHostClicked()
        {
            bool isHosting = Core.inGameHost?.IsRunning ?? false;

            if (isHosting)
            {
                // Null first so RefreshHostState reads correct state after Disconnect fires.
                Core.inGameHost?.Stop();
                Core.inGameHost = null;
                Core.networkManager.Disconnect();
                Core.uiManager.notificationsUI.AddMessage(LanguageManager.GetCurrentLanguage().StoppedHosting);
                RefreshHostState();
            }
            else
            {
                ModSettings.Save();
                int port = ModSettings.connection.Port.Value;
                string password = ModSettings.connection.Password.Value;

                var host = new BabyStepsMultiplayerClient.Networking.InGameHost();
                host.Log += msg => Core.logger.Msg($"[Host] {msg}");
                host.PlayerJoined += name => Core.uiManager.notificationsUI.AddMessage(string.Format(LanguageManager.GetCurrentLanguage().PlayerJoinedSession, name));
                host.PlayerLeft  += name => Core.uiManager.notificationsUI.AddMessage(string.Format(LanguageManager.GetCurrentLanguage().PlayerLeftSession, name));
                host.Start(port, password);
                Core.inGameHost = host;

                Core.networkManager.Connect("127.0.0.1", port, password);
                Core.uiManager.notificationsUI.AddMessage(string.Format(LanguageManager.GetCurrentLanguage().HostingOnPort, port));
            }
        }

        private static bool _steamJoinInProgress = false;
        private static void OnJoinBySteamId()
        {
            // Guard against IMGUI double-fire or re-entrant calls.
            if (_steamJoinInProgress) return;

            if (string.IsNullOrWhiteSpace(_steamJoinId))
            {
                Core.uiManager.notificationsUI.AddMessage("Enter the host's SteamID64 first.");
                return;
            }
            if (Core.networkManager.IsConnected)
            {
                Core.uiManager.notificationsUI.AddMessage("Disconnect first.");
                return;
            }
            _steamJoinInProgress = true;
            try
            {
                ModSettings.Save();
                Core.networkManager.ConnectSteam(_steamJoinId.Trim(), ModSettings.connection.Password.Value);
            }
            finally
            {
                _steamJoinInProgress = false;
            }
        }

        private static void OnP2PTestListenToggle()
        {
            if (Networking.Steam.SteamP2PTest.IsListening)
                Networking.Steam.SteamP2PTest.StopListening();
            else
                Networking.Steam.SteamP2PTest.StartListening();
        }

        private static void OnP2PTestPing()
        {
            if (!ulong.TryParse(_steamJoinId?.Trim(), out ulong targetId) || targetId == 0)
            {
                Core.uiManager.notificationsUI.AddMessage("Enter host SteamID64 in the field above first.");
                return;
            }
            Networking.Steam.SteamP2PTest.SendPing(targetId);
        }

        private static void OnPrevPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(_displayLobbies.Count / (float)PageSize));
            _lobbyPage = (_lobbyPage - 1 + pages) % pages;
            RebuildLobbyUI();
        }

        private static void OnNextPage()
        {
            int pages = Mathf.Max(1, Mathf.CeilToInt(_displayLobbies.Count / (float)PageSize));
            _lobbyPage = (_lobbyPage + 1) % pages;
            RebuildLobbyUI();
        }

        /// <summary>
        /// Called once after the game canvas is ready. Collapses the three vertically-stacked
        /// pagination elements (<, label, >) into a single horizontal row and shifts the lobby
        /// buttons up to close the gap.
        /// </summary>
        private static void RepositionPaginationRow()
        {
            if (_prevPageBtn == null || _pageLabel == null || _nextPageBtn == null) return;

            var prevRT  = _prevPageBtn.GetComponent<RectTransform>();
            var labelRT = _pageLabel.GetComponent<RectTransform>();
            var nextRT  = _nextPageBtn.GetComponent<RectTransform>();
            if (prevRT == null || labelRT == null || nextRT == null) return;

            // Measure the parent page to get exact pixel width.
            var parentRT = prevRT.parent as RectTransform;
            float pageW  = (parentRT != null && parentRT.rect.width > 10f) ? parentRT.rect.width : 800f;

            float rowY       = prevRT.anchoredPosition.y;
            const float rowH  = 52f;
            const float arrowW = 90f;
            float labelW     = pageW - arrowW * 2f;
            const float shift = 2f * (rowH + 10f); // two rows collapsed → shift lobby buttons up

            // Pivot (0,1) + anchorMin==anchorMax==(0,1) gives left-edge pixel positioning.
            void Place(RectTransform rt, float x, float w)
            {
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.pivot            = new Vector2(0f, 1f);
                rt.sizeDelta        = new Vector2(w, rowH);
                rt.anchoredPosition = new Vector2(x, rowY);
            }

            Place(prevRT,  0f,              arrowW);
            Place(labelRT, arrowW,          labelW);
            Place(nextRT,  arrowW + labelW, arrowW);

            // Bold < > text and shrink page label.
            var prevTmp = _prevPageBtn.GetComponentInChildren<TMP_Text>(true);
            if (prevTmp != null) { prevTmp.fontStyle = FontStyles.Bold; prevTmp.ForceMeshUpdate(); }

            var nextTmp = _nextPageBtn.GetComponentInChildren<TMP_Text>(true);
            if (nextTmp != null) { nextTmp.fontStyle = FontStyles.Bold; nextTmp.ForceMeshUpdate(); }

            _pageLabel.alignment = Il2CppTMPro.TextAlignmentOptions.Center;
            _pageLabel.fontSize  = 20f;
            _pageLabel.ForceMeshUpdate();

            // Close the vertical gap left by collapsing 3 rows into 1.
            foreach (var btn in _lobbyButtons)
            {
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition += new Vector2(0f, shift);
            }
        }

        private static void OnRefreshLobbies()
        {
            SetBtnText(_refreshBtn, LanguageManager.GetCurrentLanguage().Refreshing);
            foreach (var btn in _lobbyButtons) { SetBtnText(btn, "---"); btn.interactable = false; }

            // Reset auto-refresh timer so a manual refresh pushes the next auto-refresh out.
            _lastAutoLobbyRefresh = UnityEngine.Time.realtimeSinceStartup;

            // Query the dedicated server for a fresh player count.
            _publicMainQueryTask = DedicatedServerQuery.QueryAsync(PublicMainAddress, PublicMainStatusPort);

            // Refresh Steam lobby list (own lobby is injected by RebuildLobbyUI, not here).
            _lobbyBrowser?.Refresh();
        }

        private static ulong _ownLobbyId;
        private static float _lastHostSteamClickTime = -10f;
        private static float _lastConnectClickTime   = -10f;
        private static float _lastLobbyClickTime     = -10f;

        /// <summary>
        /// Rebuilds <see cref="_displayLobbies"/> and refreshes all lobby buttons.
        /// Order: own Steam lobby (if hosting) → Public Main → other Steam lobbies by player count.
        /// </summary>
        private static void RebuildLobbyUI()
        {
            _displayLobbies.Clear();

            // 1. Own Steam lobby (if hosting and lobby ID is known)
            if (Core.inGameHost?.IsRunning == true && _ownLobbyId != 0)
            {
                string persona  = NativeSteamAPI.GetPersonaName();
                bool   ownLocked = !string.IsNullOrEmpty(ModSettings.connection.HostPassword.Value);
                string name = (ownLocked ? "🔒 " : "")
                    + (string.IsNullOrEmpty(persona)
                        ? $"{NativeSteamAPI.GetLocalSteamId()}  (this session)"
                        : $"{persona}'s Session  (this session)");
                _displayLobbies.Add(new ServerInfo
                {
                    Name                = name,
                    IsPasswordProtected = ownLocked,
                    Address             = NativeSteamAPI.GetLocalSteamId().ToString(),
                    SessionId           = _ownLobbyId.ToString(),
                    PlayerCount         = Math.Max(1, Core.inGameHost.PlayerCount),
                    MaxPlayers          = 16,
                    Type                = ServerType.SteamP2P,
                });
            }

            // 2. Public Main dedicated server (always shown)
            bool publicLocked = _publicMainStatus?.IsLocked ?? false;
            _displayLobbies.Add(new ServerInfo
            {
                Name                = (publicLocked ? "🔒 " : "") + PublicMainName,
                Address             = PublicMainAddress,
                Port                = PublicMainPort,
                PlayerCount         = _publicMainStatus?.PlayerCount ?? -1, // -1 = unknown (not yet queried)
                MaxPlayers          = _publicMainStatus?.MaxPlayers  ?? 16,
                IsPasswordProtected = publicLocked,
                Type                = ServerType.LiteNetLib,
            });

            // 3. Other Steam lobbies sorted by player count descending
            string? ownAddress = (Core.inGameHost?.IsRunning == true && _ownLobbyId != 0)
                ? NativeSteamAPI.GetLocalSteamId().ToString()
                : null;

            var others = _foundLobbies
                .Where(s => s.Type != ServerType.SteamP2P || s.Address != ownAddress)
                .OrderByDescending(s => s.PlayerCount)
                .ToList();
            _displayLobbies.AddRange(others);

            // Pagination
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(_displayLobbies.Count / (float)PageSize));
            _lobbyPage = Mathf.Clamp(_lobbyPage, 0, pageCount - 1);
            int startIdx = _lobbyPage * PageSize;

            if (_pageLabel != null)
            {
                _pageLabel.text = $"{_lobbyPage + 1}/{pageCount}";
                _pageLabel.ForceMeshUpdate();
            }

            // Refresh button display
            bool canJoin = !Core.networkManager.IsConnected;
            // Only count players who have sent PlayerInfo — ghosts have firstAppearanceApplication=false
            int liveCount = Core.networkManager.IsConnected
                ? (Core.networkManager.players.Values.Count(p => p.firstAppearanceApplication) + 1)
                : -2;

            for (int i = 0; i < _lobbyButtons.Count; i++)
            {
                int lobbyIdx = startIdx + i;
                if (lobbyIdx < _displayLobbies.Count)
                {
                    var s = _displayLobbies[lobbyIdx];

                    // If connected to this server, substitute the live tab-menu count.
                    bool isThisServer = _connectedServerInfo != null
                        && _connectedServerInfo.Address == s.Address
                        && (_connectedServerInfo.Type != ServerType.LiteNetLib || _connectedServerInfo.Port == s.Port);
                    int  displayCount = (isThisServer && liveCount >= 0) ? liveCount : s.PlayerCount;

                    string disp  = s.Name.Replace("🔒 ", string.Empty);
                    string count = displayCount < 0 ? "?" : displayCount.ToString();
                    SetBtnText(_lobbyButtons[i], $"{disp}  ({count}/{s.MaxPlayers})");
                    _lobbyButtons[i].interactable = canJoin;
                    SetLobbyLockIcon(i, s.IsPasswordProtected);
                }
                else
                {
                    SetBtnText(_lobbyButtons[i], "---");
                    _lobbyButtons[i].interactable = false;
                    SetLobbyLockIcon(i, false);
                }
            }
        }

        /// <summary>Connects to a server from the display list, dispatching by server type.</summary>
        private static void JoinServer(ServerInfo info, string password)
        {
            _connectedServerInfo      = info;
            _lastDisplayedPlayerCount = -1;
            ModSettings.Save();
            string cleanName = info.Name.Replace("🔒 ", string.Empty);
            string joiningMsg = string.Format(LanguageManager.GetCurrentLanguage().JoiningServer, cleanName);
            if (info.Type == ServerType.LiteNetLib)
            {
                Core.networkManager.Connect(info.Address, info.Port, password);
                Core.uiManager.notificationsUI.AddMessage(joiningMsg);
            }
            else
            {
                Core.networkManager.ConnectSteam(info.Address, password);
                Core.uiManager.notificationsUI.AddMessage(joiningMsg);
            }
        }

        private static void OnLobbyButtonClicked(int slot)
        {
            // Same double-fire guard as OnConnectClicked — mouse events can fire twice per
            // click, and a duplicate JoinServer call would create a second LiteNetLib
            // connection that the server sees as an anonymous ghost peer.
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _lastLobbyClickTime < 0.5f) return;
            _lastLobbyClickTime = now;

            int index = _lobbyPage * PageSize + slot;
            if (index >= _displayLobbies.Count) return;
            var info = _displayLobbies[index];

            if (info.IsPasswordProtected)
            {
                ShowPasswordPrompt(info);
                return;
            }

            JoinServer(info, string.Empty);
        }

        // ── Password popup ────────────────────────────────────────────────────────

        private static void ShowPasswordPrompt(ServerInfo lobby)
        {
            _pendingLockedLobby  = lobby;
            _pendingJoinPassword = string.Empty;

            if (_pwTitle != null)
            {
                string name = lobby.Name.Replace("🔒 ", string.Empty).Trim();
                _pwTitle.text = string.Format(LanguageManager.GetCurrentLanguage().PasswordForSession, name);
                _pwTitle.ForceMeshUpdate();
            }

            // Reset the input field to blank/placeholder so it doesn't carry over a previous value.
            if (_pwInputInfo != null)
            {
                _pwInputInfo.Value = string.Empty;
                if (_pwInputInfo.DisplayText != null)
                {
                    _pwInputInfo.DisplayText.text  = _pwInputInfo.Placeholder;
                    _pwInputInfo.DisplayText.color = _pwInputInfo.PlaceholderColor;
                    _pwInputInfo.DisplayText.ForceMeshUpdate();
                }
            }

            _pwCanvas?.SetActive(true);
            // Show the dim inside the library's keyboard overlay canvas (sortingOrder 10000)
            // so it is guaranteed to render above our popup panel canvas (9999).
            MenuInjectionLibrary.ShowPopupDim(new Color(0f, 0f, 0f, 0.78f));

            // Tell the library which items the controller can navigate while popup is open.
            MenuInjectionLibrary.OverlayNavigableItems = new[]
            {
                _pwInputBtn  != null ? _pwInputBtn.gameObject  : null,
                _pwEnterBtn  != null ? _pwEnterBtn.gameObject  : null,
                _pwBackBtn   != null ? _pwBackBtn.gameObject   : null,
            }.Where(g => g != null).ToArray();

            // Grab controller focus on the Enter button.
            UnityEngine.EventSystems.EventSystem.current
                ?.SetSelectedGameObject(_pwEnterBtn != null ? _pwEnterBtn.gameObject : null);
        }

        /// <summary>
        /// Builds the password popup canvas. Called from ConfigureServersTab during Build so
        /// _currentInjectedMenu is set — that's the only time AddInputField captures the correct
        /// menu reference to trigger mouse-typing and the on-screen keyboard.
        /// </summary>
        private static void BuildPasswordPopup()
        {
            if (_pwCanvas != null) return;

            // Popup panel canvas at 9999 — no dim here; the dim lives inside the library's
            // keyboard overlay canvas (10000) via ShowPopupDim so it's always above this canvas.
            _pwCanvas = new UnityEngine.GameObject("BBSPasswordPopup");
            UnityEngine.Object.DontDestroyOnLoad(_pwCanvas);
            var cv = _pwCanvas.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 9999;
            _pwCanvas.AddComponent<CanvasScaler>();
            _pwCanvas.AddComponent<GraphicRaycaster>();

            // Panel
            var panelGO = new UnityEngine.GameObject("Panel");
            panelGO.transform.SetParent(_pwCanvas.transform, false);
            panelGO.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.97f);
            var panelRT = panelGO.GetComponent<RectTransform>();
            SetRTCenter(panelRT, new Vector2(400, 260), Vector2.zero);

            // Title — clone the TMP_Text from the Steam tab button (already created above,
            // correct font/style, never null at this point in ConfigureServersTab).
            var titleTmplGO = _hostSteamTabBtn?.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true)?.gameObject;
            if (titleTmplGO != null)
            {
                var tc = UnityEngine.Object.Instantiate(titleTmplGO, panelGO.transform);
                tc.SetActive(true);
                _pwTitle = tc.GetComponent<Il2CppTMPro.TMP_Text>();
                if (_pwTitle != null)
                {
                    _pwTitle.text             = "Enter password:";
                    _pwTitle.alignment        = Il2CppTMPro.TextAlignmentOptions.Center;
                    _pwTitle.enableAutoSizing = false;
                    _pwTitle.fontSize         = 24f;
                    _pwTitle.ForceMeshUpdate();
                }
                SetRTCenter(tc.GetComponent<RectTransform>(), new Vector2(360, 70), new Vector2(0, 90));
            }

            // Input field — created now while _currentInjectedMenu is set so the Button's
            // onClick correctly captures the menu for mouse-typing and on-screen keyboard.
            _pwInputBtn = MenuInjectionLibrary.AddInputField(
                "type password...",
                (UnityAction<string>)(v => _pendingJoinPassword = v),
                string.Empty,
                panelRT);
            _pwInputInfo = MenuInjectionLibrary.LastCreatedInputFieldInfo;
            if (_pwInputBtn != null)
                SetRTCenter(_pwInputBtn.GetComponent<RectTransform>(), new Vector2(340, 52), new Vector2(0, 15));

            // Enter / Back buttons
            if (_refreshBtn != null)
            {
                var ec = UnityEngine.Object.Instantiate(_refreshBtn.gameObject, panelGO.transform);
                ec.SetActive(true);
                _pwEnterBtn = ec.GetComponent<Button>();
                if (_pwEnterBtn != null) { _pwEnterBtn.interactable = true; _pwEnterBtn.onClick = new Button.ButtonClickedEvent(); _pwEnterBtn.onClick.AddListener((UnityAction)OnPopupEnterClicked); }
                _pwEnterTmp = ec.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                if (_pwEnterTmp != null) { _pwEnterTmp.text = "Enter"; _pwEnterTmp.ForceMeshUpdate(); }
                SetRTCenter(ec.GetComponent<RectTransform>(), new Vector2(140, 52), new Vector2(-85, -72));

                var bc = UnityEngine.Object.Instantiate(_refreshBtn.gameObject, panelGO.transform);
                bc.SetActive(true);
                _pwBackBtn = bc.GetComponent<Button>();
                if (_pwBackBtn != null) { _pwBackBtn.interactable = true; _pwBackBtn.onClick = new Button.ButtonClickedEvent(); _pwBackBtn.onClick.AddListener((UnityAction)HidePasswordPopup); }
                _pwBackTmp = bc.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
                if (_pwBackTmp != null) { _pwBackTmp.text = "Back"; _pwBackTmp.ForceMeshUpdate(); }
                SetRTCenter(bc.GetComponent<RectTransform>(), new Vector2(140, 52), new Vector2(85, -72));

                // Trap controller navigation inside the popup: each button only navigates
                // to the other popup button so the controller can't escape to items behind.
                if (_pwEnterBtn != null && _pwBackBtn != null)
                {
                    SetExplicitNav(_pwEnterBtn, _pwBackBtn, _pwBackBtn);
                    SetExplicitNav(_pwBackBtn, _pwEnterBtn, _pwEnterBtn);
                }
            }

            _pwCanvas.SetActive(false);
        }

        private static void SetExplicitNav(Button btn, Button left, Button right)
        {
            var nav = btn.navigation;
            nav.mode        = Navigation.Mode.Explicit;
            nav.selectOnLeft  = left;
            nav.selectOnRight = right;
            nav.selectOnUp    = null;
            nav.selectOnDown  = null;
            btn.navigation  = nav;
        }

        private static void SetRTFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        }

        private static void SetRTCenter(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        private static void HidePasswordPopup()
        {
            _pendingLockedLobby  = null;
            _pendingJoinPassword = string.Empty;

            // Close any active mouse-typing session before deactivating the canvas.
            // If _pwInputBtn is still the OwnerButton of a live typing session and we
            // deactivate it first, the Rewired navigation will crash trying to reach
            // an inactive GameObject next frame.
            MenuInjectionLibrary.ForceCloseMouseTyping();
            MenuInjectionLibrary.OverlayNavigableItems = null;
            MenuInjectionLibrary.HidePopupDim();

            _pwCanvas?.SetActive(false);

            // Release popup focus so the menu regains normal controller navigation.
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        private static void HidePasswordPrompt() => HidePasswordPopup();

        private static void OnPopupEnterClicked()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _pwEnterDebounce < 0.5f) return;
            _pwEnterDebounce = now;

            if (_pendingLockedLobby == null) return;
            string pw   = _pendingJoinPassword;
            var lobby   = _pendingLockedLobby;
            HidePasswordPopup();
            JoinServer(lobby, pw);
        }

        // ── Lock icon ─────────────────────────────────────────────────────────────

        private static UnityEngine.Sprite GetLockSprite()
        {
            if (_lockSprite != null) return _lockSprite;

            const int S = 24;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color32[S * S];
            var fill  = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        if (x >= 0 && x < S && y >= 0 && y < S)
                            px[y * S + x] = c;
            }

            // y=0 is bottom in Texture2D. Body at bottom, shackle U at top.
            Rect(2, 1, 21, 10, fill);   // body
            Rect(5, 8, 8, 19, fill);    // shackle left post
            Rect(15, 8, 18, 19, fill);  // shackle right post
            Rect(5, 17, 18, 20, fill);  // shackle top bar
            Rect(9, 10, 14, 17, clear); // hollow inside shackle (makes U-shape)

            tex.SetPixels32(px);
            tex.Apply();
            _lockSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
            return _lockSprite;
        }

        private static void SetLobbyLockIcon(int slot, bool show)
        {
            if (slot < 0 || slot >= _lobbyButtons.Count) return;
            var btn = _lobbyButtons[slot];
            if (btn == null) return;

            if (_lobbyLockIcons[slot] == null)
            {
                var go = new UnityEngine.GameObject("LockIcon");
                go.transform.SetParent(btn.transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = GetLockSprite();
                img.preserveAspect = true;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 0.72f);
                rt.anchorMax        = new Vector2(0f, 0.72f);
                rt.pivot            = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(4f, 0f);
                rt.sizeDelta        = new Vector2(20f, 20f);
                _lobbyLockIcons[slot] = go;
            }

            _lobbyLockIcons[slot].SetActive(show);

            // Add/remove left margin on the button's label so the icon has clear space.
            var tmp = btn.GetComponentInChildren<Il2CppTMPro.TMP_Text>(true);
            if (tmp != null)
            {
                var m = tmp.margin;
                m.x = show ? 30f : 0f;
                tmp.margin = m;
            }
        }

        private static void OnLobbiesFound(IReadOnlyList<ServerInfo> servers)
        {
            _foundLobbies.Clear();
            _foundLobbies.AddRange(servers);
            SetBtnText(_refreshBtn, LanguageManager.GetCurrentLanguage().Refresh);

            // Polling-based invite detection: check whether any lobby has stamped
            // an invite marker for our SteamID.  Works regardless of Steam callback mode.
            if (!Core.networkManager.IsConnected && !(Core.inGameHost?.IsRunning ?? false))
            {
                string myId      = NativeSteamAPI.GetLocalSteamId().ToString();
                string inviteKey = "bbs_invite_" + myId;
                foreach (var s in servers)
                {
                    if (string.IsNullOrEmpty(s.SessionId)) continue;
                    if (!ulong.TryParse(s.SessionId, out ulong lobbyId) || lobbyId == 0) continue;
                    if (NativeSteamAPI.GetLobbyData(lobbyId, inviteKey) != "1") continue;

                    Core.logger.Msg($"[Steam] Invite detected in lobby {lobbyId} — joining {s.Name}");
                    Core.uiManager.notificationsUI.AddMessage($"Auto-joining {s.Name.Replace("🔒 ", "")}…");
                    Core.networkManager.ConnectSteam(s.Address, string.Empty);
                    break;
                }
            }

            RebuildLobbyUI();
        }

        private static void OnMicrophoneToggled(bool enabled)
        {
            ModSettings.audio.MicrophoneEnabled.Value = enabled;
            LocalPlayer.Instance?.SetMicrophoneEnabled(enabled);
        }

        private static void OnDeafenToggled(bool enabled)
        {
            ModSettings.audio.Deafened.Value = enabled;
        }

        private static void OnPushToTalkToggled(bool enabled)
        {
            ModSettings.audio.PushToTalk.Value = enabled;
            LocalPlayer.Instance?.SetPushToTalkEnabled(enabled);
            if (_pttKeyBtn != null) _pttKeyBtn.interactable = enabled;
        }

        private static void OnAudioDeviceCycled()
        {
            var devices = Audio.BBSMicrophoneCapture.GetAvailableDevicesStatic();
            if (devices == null || devices.Length == 0) return;

            int next = (ModSettings.audio.SelectedMicrophoneIndex.Value + 1) % devices.Length;
            ModSettings.audio.SelectedMicrophoneIndex.Value = next;
            LocalPlayer.Instance?.SetMicrophoneDevice(next);
            ModSettings.Save();

            SetBtnText(_audioDeviceBtn, GetAudioDeviceLabel());
        }

        private static void OnPttKeyClicked()
        {
            BeginCapture(KeybindCaptureTarget.PushToTalk);
            SetBtnText(_pttKeyBtn, LanguageManager.GetCurrentLanguage().PressAnyKey);
        }

        private static void OnTabMenuKeyClicked()
        {
            BeginCapture(KeybindCaptureTarget.TabMenu);
            SetBtnText(_tabMenuKeyBtn, LanguageManager.GetCurrentLanguage().PressAnyKey);
        }

        private static void OnChatMenuKeyClicked()
        {
            BeginCapture(KeybindCaptureTarget.ChatMenu);
            SetBtnText(_chatMenuKeyBtn, LanguageManager.GetCurrentLanguage().PressAnyKey);
        }

        private static void OnCollisionToggled(bool enabled)
        {
            if (!Core.networkManager.IsConnected)
            {
                if (_collisionToggle != null && _collisionToggle.isOn != ModSettings.player.Collisions.Value)
                    _collisionToggle.isOn = ModSettings.player.Collisions.Value;
                return;
            }

            if (ModSettings.player.Collisions.Value == enabled) return;

            ModSettings.player.Collisions.Value = enabled;
            Core.networkManager.SendCollisionToggle(enabled);
            foreach (var player in Core.networkManager.players)
            {
                bool shouldEnable = ModSettings.player.Collisions.Value && player.Value.netCollisionsEnabled;
                if (shouldEnable) player.Value.EnableCollision();
                else              player.Value.DisableCollision();
            }
        }

        private static void OnCutsceneToggled(bool enabled)
        {
            if (!Core.networkManager.IsConnected)
            {
                if (_cutsceneToggle != null && _cutsceneToggle.isOn != ModSettings.player.CutscenePlayerVisibility.Value)
                    _cutsceneToggle.isOn = ModSettings.player.CutscenePlayerVisibility.Value;
                return;
            }

            ModSettings.player.CutscenePlayerVisibility.Value = enabled;
        }

        private static void OnNametagToggled(bool enabled)
        {
            if (!Core.networkManager.IsConnected)
            {
                if (_nametagToggle != null && _nametagToggle.isOn != ModSettings.player.ShowNametags.Value)
                    _nametagToggle.isOn = ModSettings.player.ShowNametags.Value;
                return;
            }

            ModSettings.player.ShowNametags.Value = enabled;
        }

        private static void OnUpdateAppearanceClicked()
        {
            if (!Core.networkManager.IsConnected) return;
            ModSettings.Save();
            Core.networkManager.SendPlayerInformation();
            LocalPlayer.Instance?.ApplySuitColor();
            Core.uiManager.notificationsUI.AddMessage(LanguageManager.GetCurrentLanguage().AppearanceUpdated);
        }

        private static void RefreshConnectionState()
        {
            SetBtnText(_connectTabBtn, GetConnectLabel());
            RefreshHostSteamState();
        }

        private static void RefreshHostState()
        {
            SetBtnText(_hostLanTabBtn, GetHostLabel());
            // Disable Host (LAN) if connected to something else (not self-hosting LAN)
            if (_hostLanTabBtn != null)
                _hostLanTabBtn.interactable = !(Core.networkManager.IsConnected && !(Core.inGameHost?.IsRunning ?? false));
        }

        private static void RefreshHostSteamState()
        {
            SetBtnText(_hostSteamTabBtn, GetHostSteamLabel());
            RebuildLobbyUI();
        }

        private static void RefreshColorPreview()
        {
            if (_colorPreviewImage != null)
                _colorPreviewImage.color = ModSettings.player.SuitColor.Value;
        }

        // ── Label getters ─────────────────────────────────────────────────────

        private static string GetConnectLabel()
            => Core.networkManager?.IsConnected == true
                ? LanguageManager.GetCurrentLanguage().Disconnect
                : LanguageManager.GetCurrentLanguage().Connect;

        private static string GetHostLabel()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            return (Core.inGameHost?.IsRunning ?? false) ? lang.StopHosting : lang.HostLAN;
        }

        private static string GetHostSteamLabel()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            if (Core.inGameHost?.IsRunning ?? false) return lang.StopHosting;
            if (Core.networkManager?.IsConnected ?? false) return lang.Disconnect;
            return lang.HostLobby;
        }

        private static string GetMicrophoneToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().EnableMicrophone,
                LanguageManager.GetCurrentLanguage().DisableMicrophone);

        private static string GetDeafenToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().Deafen,
                LanguageManager.GetCurrentLanguage().Undeafen);

        private static string GetPushToTalkToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().EnablePushToTalk,
                LanguageManager.GetCurrentLanguage().DisablePushToTalk);

        private static string GetCollisionToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().EnableCollisions,
                LanguageManager.GetCurrentLanguage().DisableCollisions);

        private static string GetCutsceneToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().EnablePlayerCutsceneVisibility,
                LanguageManager.GetCurrentLanguage().DisablePlayerCutsceneVisibility);

        private static string GetNametagToggleLabel()
            => GetNeutralToggleLabel(
                LanguageManager.GetCurrentLanguage().EnableNametags,
                LanguageManager.GetCurrentLanguage().DisableNametags);
        
        private static string GetPttKeyLabel()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            return $"{lang.PushToTalkKey} {InputBindingHelper.GetDisplayName(ModSettings.audio.PushToTalkKey.Value)}";
        }

        private static string GetTabMenuKeyLabel()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            return $"{lang.PlayerListKeyLabel} {InputBindingHelper.GetDisplayName(ModSettings.player.TabMenuKey.Value)}";
        }

        private static string GetChatMenuKeyLabel()
        {
            var lang = LanguageManager.GetCurrentLanguage();
            return $"{lang.ChatMenuKeyLabel} {InputBindingHelper.GetDisplayName(ModSettings.player.ChatMenuKey.Value)}";
        }

        private static string GetGainLabel()
            => $"{LanguageManager.GetCurrentLanguage().MicrophoneGain} {ModSettings.audio.MicrophoneGain.Value:F2}x";

        private static string GetAudioDeviceLabel()
        {
            var devices = Audio.BBSMicrophoneCapture.GetAvailableDevicesStatic();
            if (devices == null || devices.Length == 0) return LanguageManager.GetCurrentLanguage().NoDevicesFound;
            int idx = UnityEngine.Mathf.Clamp(ModSettings.audio.SelectedMicrophoneIndex.Value, 0, devices.Length - 1);
            return devices[idx];
        }

        private static string GetNeutralToggleLabel(string enabledText, string disabledText)
        {
            if (string.IsNullOrEmpty(enabledText)) return disabledText ?? string.Empty;
            if (string.IsNullOrEmpty(disabledText)) return enabledText;

            if (disabledText.EndsWith(enabledText, System.StringComparison.OrdinalIgnoreCase))
                return enabledText;
            if (enabledText.EndsWith(disabledText, System.StringComparison.OrdinalIgnoreCase))
                return disabledText;

            var enabledWords = enabledText.Split(' ');
            var disabledWords = disabledText.Split(' ');
            int i = enabledWords.Length - 1;
            int j = disabledWords.Length - 1;
            int commonWords = 0;
            while (i >= 0 && j >= 0 &&
                   string.Equals(enabledWords[i], disabledWords[j], System.StringComparison.OrdinalIgnoreCase))
            {
                commonWords++;
                i--;
                j--;
            }

            if (commonWords > 0)
            {
                int start = enabledWords.Length - commonWords;
                var neutral = string.Join(" ", enabledWords, start, commonWords).Trim();
                if (!string.IsNullOrEmpty(neutral))
                    return neutral;
            }

            return enabledText;
        }

        private static void RefreshLocalizableText()
        {
            var lang = LanguageManager.GetCurrentLanguage();

            // Main "Multiplayer" button
            _menu?.SetMainButtonLabel(lang.MultiplayerTitle);

            // Tab names — use both SetTabName (persists into _currentTabNames so a menu
            // rebuild picks up the right language) AND direct SetBtnText on the cached
            // Button refs (same code path that works for every other button).
            _menu?.SetTabName(0, lang.TabLobby);
            _menu?.SetTabName(1, lang.TabPlayer);
            _menu?.SetTabName(2, lang.TabGeneral);
            _menu?.SetTabName(3, lang.TabAudio);
            _menu?.SetTabName(4, lang.TabLAN);
            SetBtnText(_tabBtn0, lang.TabLobby);
            SetBtnText(_tabBtn1, lang.TabPlayer);
            SetBtnText(_tabBtn2, lang.TabGeneral);
            SetBtnText(_tabBtn3, lang.TabAudio);
            SetBtnText(_tabBtn4, lang.TabLAN);

            // Back fixed button
            SetBtnText(_menu?.GetFixedButton(0), lang.ButtonBack);

            // Lobby tab buttons
            SetBtnText(_connectTabBtn,   GetConnectLabel());
            SetBtnText(_hostLanTabBtn,   GetHostLabel());
            SetBtnText(_hostSteamTabBtn, GetHostSteamLabel());
            SetBtnText(_refreshBtn,      lang.Refresh);

            // Hosting password placeholder
            RefreshInputFieldPlaceholder(_hostingPasswordInputInfo, lang.LobbyPassword);

            // Toggle font — copy from the game's toggle template (which still has BBLocalize
            // and gets its font updated by I2Loc).  Without this, Asian-language characters
            // render as boxes on toggle labels because BBLocalize was stripped by MenuLib.
            var toggleFont = MenuInjectionLibrary.GetCurrentToggleFont();

            // General tab toggles & keybind buttons
            SetToggleText(_collisionToggle, GetCollisionToggleLabel(), toggleFont);
            SetToggleText(_cutsceneToggle,  GetCutsceneToggleLabel(),  toggleFont);
            SetToggleText(_nametagToggle,   GetNametagToggleLabel(),   toggleFont);
            SetBtnText(_tabMenuKeyBtn,  GetTabMenuKeyLabel());
            SetBtnText(_chatMenuKeyBtn, GetChatMenuKeyLabel());

            // Audio tab toggles & buttons
            SetToggleText(_microphoneToggle, GetMicrophoneToggleLabel(), toggleFont);
            SetToggleText(_deafenToggle,     GetDeafenToggleLabel(),     toggleFont);
            SetToggleText(_pttToggle,        GetPushToTalkToggleLabel(), toggleFont);
            SetBtnText(_pttKeyBtn,       GetPttKeyLabel());
            SetBtnText(_audioDeviceBtn,  GetAudioDeviceLabel());

            if (_audioDeviceStaticLabel != null)
            {
                _audioDeviceStaticLabel.text = lang.AudioDevice;
                _audioDeviceStaticLabel.ForceMeshUpdate();
            }
            if (_gainLabel != null)
            {
                _gainLabel.text = GetGainLabel();
                _gainLabel.ForceMeshUpdate();
            }

            // Lobby browser label
            if (_lobbyBrowserLabel != null)
            {
                _lobbyBrowserLabel.text = lang.LobbyBrowser;
                _lobbyBrowserLabel.ForceMeshUpdate();
            }

            // Player tab
            SetBtnText(_updateAppearanceBtn, lang.UpdateNameAndAppearance);

            // LAN tab labels
            SetLabelText(_serverIpLabel,    lang.ServerIP);
            SetLabelText(_serverPortLabel,  lang.ServerPort);
            SetLabelText(_passwordOptLabel, lang.PasswordOptional);

            // LAN tab input field placeholders
            RefreshInputFieldPlaceholder(_serverIpInputInfo,    lang.ServerIP);
            RefreshInputFieldPlaceholder(_serverPortInputInfo,  lang.ServerPort);
            RefreshInputFieldPlaceholder(_lanPasswordInputInfo, lang.PasswordOptional);

            // Password popup buttons
            if (_pwEnterTmp != null) { _pwEnterTmp.text = lang.Enter;      _pwEnterTmp.ForceMeshUpdate(); }
            if (_pwBackTmp  != null) { _pwBackTmp.text  = lang.ButtonBack; _pwBackTmp.ForceMeshUpdate();  }
        }

        private static void SetLabelText(TMP_Text label, string text)
        {
            if (label == null) return;
            label.text = text;
            label.ForceMeshUpdate();
        }

        private static void RefreshInputFieldPlaceholder(MenuInjectionLibrary.InputFieldInfo info, string placeholder)
        {
            if (info == null) return;
            info.Placeholder = placeholder;
            if (string.IsNullOrEmpty(info.Value) && info.DisplayText != null)
            {
                info.DisplayText.text = placeholder;
                info.DisplayText.ForceMeshUpdate();
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static void SetBtnText(Button btn, string text)
        {
            if (btn == null) return;
            var tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) return;
            tmp.text = text;
            tmp.ForceMeshUpdate();
        }

        private static void SetToggleText(Toggle toggle, string text, Il2CppTMPro.TMP_FontAsset font = null)
        {
            if (toggle == null) return;
            var tmp = toggle.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) return;
            tmp.text = text;
            if (font != null) tmp.font = font;
            tmp.ForceMeshUpdate();
        }

        private static void ApplyCapturedKeybind(string binding, string displayName)
        {
            if (string.Equals(binding, KeyCode.Escape.ToString(), System.StringComparison.OrdinalIgnoreCase))
            {
                CancelCapture();
                return;
            }

            switch (_keybindCaptureTarget)
            {
                case KeybindCaptureTarget.PushToTalk:
                    ModSettings.audio.PushToTalkKey.Value = binding;
                    SetBtnText(_pttKeyBtn, $"Bind Key: {displayName}");
                    break;
                case KeybindCaptureTarget.TabMenu:
                    ModSettings.player.TabMenuKey.Value = binding;
                    SetBtnText(_tabMenuKeyBtn, GetTabMenuKeyLabel());
                    break;
                case KeybindCaptureTarget.ChatMenu:
                    ModSettings.player.ChatMenuKey.Value = binding;
                    SetBtnText(_chatMenuKeyBtn, GetChatMenuKeyLabel());
                    break;
            }

            _keybindCaptureTarget = KeybindCaptureTarget.None;
            ModSettings.Save();
            EndCapture();
        }

        private static void BeginCapture(KeybindCaptureTarget target)
        {
            if (_keybindCaptureTarget != KeybindCaptureTarget.None)
            {
                return;
            }

            _keybindCaptureTarget = target;
            _waitingForRelease = true;
            UpdateGameplayInputSuppressionForCapture();

            if (EventSystem.current != null)
            {
                _cachedInputModule = EventSystem.current.currentInputModule;
                if (_cachedInputModule != null)
                    _cachedInputModule.enabled = false;
            }
        }

        private static void CancelCapture()
        {
            switch (_keybindCaptureTarget)
            {
                case KeybindCaptureTarget.PushToTalk:
                    SetBtnText(_pttKeyBtn, GetPttKeyLabel());
                    break;
                case KeybindCaptureTarget.TabMenu:
                    SetBtnText(_tabMenuKeyBtn, GetTabMenuKeyLabel());
                    break;
                case KeybindCaptureTarget.ChatMenu:
                    SetBtnText(_chatMenuKeyBtn, GetChatMenuKeyLabel());
                    break;
            }

            EndCapture();
        }

        private static void UpdateGameplayInputSuppressionForCapture()
        {
            var localPlayer = LocalPlayer.Instance;
            var movement = localPlayer?.playerMovement;
            if (movement == null)
            {
                _playerMovementSuppressedForCapture = false;
                return;
            }

            if (IsCapturingKeybind)
            {
                if (!_playerMovementSuppressedForCapture)
                {
                    _playerMovementWasEnabledBeforeCapture = movement.enabled;
                    if (_playerMovementWasEnabledBeforeCapture)
                        movement.enabled = false;

                    _playerMovementSuppressedForCapture = true;
                }
            }
            else if (_playerMovementSuppressedForCapture)
            {
                movement.enabled = _playerMovementWasEnabledBeforeCapture;
                _playerMovementSuppressedForCapture = false;
            }
        }

        private static void EndCapture()
        {
            if (_cachedInputModule != null)
                _cachedInputModule.enabled = true;

            _keybindCaptureTarget = KeybindCaptureTarget.None;
            _waitingForRelease = false;
            UpdateGameplayInputSuppressionForCapture();
        }
    }
}
