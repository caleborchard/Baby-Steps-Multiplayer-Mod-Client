using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.Localization;

namespace BabyStepsMultiplayerClient.UI
{
    public static class MultiplayerMenu
    {
        private static MenuInjectionLibrary.InjectedMenu _menu;

        private static Toggle _collisionToggle;
        private static Toggle _cutsceneToggle;
        private static Toggle _nametagToggle;
        private static Button _pttKeyBtn;
        private static Image  _colorPreviewImage;

        private static bool _isWaitingForKey;

        private static Button ConnectFixedBtn => _menu?.GetFixedButton(0);

        public static void Initialize()
        {
            if (_menu != null) return;

            ModSettings.Load();

            _menu = MenuInjectionLibrary.CreateMenu("Multiplayer")
                .AddTab("Player",     ConfigurePlayerTab)
                .AddTab("Connection", ConfigureConnectionTab)
                .AddTab("General",    ConfigureGeneralTab)
                .AddTab("Audio",      ConfigureAudioTab)
                .AddFixedButton(GetConnectLabel(), (UnityAction)OnConnectClicked)
                .AddFixedButton("Back")
                .Build();

            Core.OnConnectionStateChanged += RefreshConnectionState;
        }

        public static void Update()
        {
            if (!_isWaitingForKey) return;
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode) && keyCode != KeyCode.None && keyCode != KeyCode.Mouse0)
                {
                    ModSettings.audio.PushToTalkKey.Value = keyCode.ToString();
                    if (LocalPlayer.Instance != null)
                        LocalPlayer.Instance.SetPushToTalkKey(keyCode);
                    _isWaitingForKey = false;
                    SetBtnText(_pttKeyBtn, $"Bind Key: {keyCode}");
                    break;
                }
            }
        }

        // ── Tab builders ─────────────────────────────────────────────────────

        private static void ConfigurePlayerTab(MenuInjectionLibrary.TabBuilder tab)
        {
            var lang  = LanguageManager.GetCurrentLanguage();
            var color = ModSettings.player.SuitColor.Value;

            tab.AddButton(lang.UpdateNameAndAppearance, (UnityAction)OnUpdateAppearanceClicked);
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

            tab.AddLabel(lang.ServerIP);
            tab.AddInputField(lang.ServerIP,
                (UnityAction<string>)(v => ModSettings.connection.Address.Value = v),
                ModSettings.connection.Address.Value);

            tab.AddLabel(lang.ServerPort);
            tab.AddInputField(lang.ServerPort,
                (UnityAction<string>)(v => { if (int.TryParse(v, out int p)) ModSettings.connection.Port.Value = p; }),
                ModSettings.connection.Port.Value.ToString());

            tab.AddLabel(lang.PasswordOptional);
            tab.AddInputField(lang.PasswordOptional,
                (UnityAction<string>)(v => ModSettings.connection.Password.Value = v),
                ModSettings.connection.Password.Value);
        }

        private static void ConfigureGeneralTab(MenuInjectionLibrary.TabBuilder tab)
        {
            _collisionToggle = tab.AddToggle(GetCollisionToggleLabel(), ModSettings.player.Collisions.Value,
                (UnityAction<bool>)OnCollisionToggled);
            _cutsceneToggle  = tab.AddToggle(GetCutsceneToggleLabel(), ModSettings.player.CutscenePlayerVisibility.Value,
                (UnityAction<bool>)OnCutsceneToggled);
            _nametagToggle   = tab.AddToggle(GetNametagToggleLabel(), ModSettings.player.ShowNametags.Value,
                (UnityAction<bool>)OnNametagToggled);
        }

        private static void ConfigureAudioTab(MenuInjectionLibrary.TabBuilder tab)
        {
            tab.AddToggle(GetMicrophoneToggleLabel(), ModSettings.audio.MicrophoneEnabled.Value,
                (UnityAction<bool>)OnMicrophoneToggled);
            tab.AddToggle(GetDeafenToggleLabel(), ModSettings.audio.Deafened.Value,
                (UnityAction<bool>)OnDeafenToggled);
            tab.AddToggle(GetPushToTalkToggleLabel(), ModSettings.audio.PushToTalk.Value,
                (UnityAction<bool>)OnPushToTalkToggled);
            _pttKeyBtn = tab.AddButton($"Bind Key: {ModSettings.audio.PushToTalkKey.Value}", (UnityAction)OnPttKeyClicked);
            _pttKeyBtn.interactable = ModSettings.audio.PushToTalk.Value;

            TMP_Text gainLabel = null;
            Slider   gainSlider = null;
            gainLabel  = tab.AddLabel(GetGainLabel());
            gainSlider = tab.AddSlider("", 0f, 3f, ModSettings.audio.MicrophoneGain.Value,
                (UnityAction<float>)(val =>
                {
                    ModSettings.audio.MicrophoneGain.Value = val;
                    LocalPlayer.Instance?.mic.SetGain(val);
                    if (gainLabel != null) { gainLabel.text = GetGainLabel(); gainLabel.ForceMeshUpdate(); }
                }));
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private static void OnConnectClicked()
        {
            if (Core.networkManager.client != null)
            {
                Core.networkManager.Disconnect();
            }
            else
            {
                ModSettings.Save();
                Core.networkManager.Connect(
                    ModSettings.connection.Address.Value,
                    ModSettings.connection.Port.Value,
                    ModSettings.connection.Password.Value);
            }
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

        private static void OnPttKeyClicked()
        {
            _isWaitingForKey = true;
            SetBtnText(_pttKeyBtn, LanguageManager.GetCurrentLanguage().PressAnyKey);
        }

        private static void OnCollisionToggled(bool enabled)
        {
            if (Core.networkManager.client == null)
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
            if (Core.networkManager.client == null)
            {
                if (_cutsceneToggle != null && _cutsceneToggle.isOn != ModSettings.player.CutscenePlayerVisibility.Value)
                    _cutsceneToggle.isOn = ModSettings.player.CutscenePlayerVisibility.Value;
                return;
            }

            ModSettings.player.CutscenePlayerVisibility.Value = enabled;
        }

        private static void OnNametagToggled(bool enabled)
        {
            if (Core.networkManager.client == null)
            {
                if (_nametagToggle != null && _nametagToggle.isOn != ModSettings.player.ShowNametags.Value)
                    _nametagToggle.isOn = ModSettings.player.ShowNametags.Value;
                return;
            }

            ModSettings.player.ShowNametags.Value = enabled;
        }

        private static void OnUpdateAppearanceClicked()
        {
            if (Core.networkManager.client == null) return;
            ModSettings.Save();
            Core.networkManager.mainThreadActions.Enqueue(() =>
            {
                Core.networkManager.SendPlayerInformation();
                LocalPlayer.Instance?.ApplySuitColor();
            });
            Core.uiManager.notificationsUI.AddMessage(LanguageManager.GetCurrentLanguage().AppearanceUpdated);
        }

        private static void RefreshConnectionState()
            => SetBtnText(ConnectFixedBtn, GetConnectLabel());

        private static void RefreshColorPreview()
        {
            if (_colorPreviewImage != null)
                _colorPreviewImage.color = ModSettings.player.SuitColor.Value;
        }

        // ── Label getters ─────────────────────────────────────────────────────

        private static string GetConnectLabel()
            => Core.networkManager?.client != null
                ? LanguageManager.GetCurrentLanguage().Disconnect
                : LanguageManager.GetCurrentLanguage().Connect;

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

        private static string GetGainLabel()
            => $"{LanguageManager.GetCurrentLanguage().MicrophoneGain} {ModSettings.audio.MicrophoneGain.Value:F2}x";

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

        // ── UI helpers ────────────────────────────────────────────────────────

        private static void SetBtnText(Button btn, string text)
        {
            if (btn == null) return;
            var tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) return;
            tmp.text = text;
            tmp.ForceMeshUpdate();
        }

        private static void UpdateSliderLabel(Slider slider, string text)
        {
            if (slider == null) return;
            var tmp = slider.GetComponentInChildren<TMP_Text>(true);
            if (tmp == null) return;
            tmp.text = text;
            tmp.ForceMeshUpdate();
        }
    }
}
