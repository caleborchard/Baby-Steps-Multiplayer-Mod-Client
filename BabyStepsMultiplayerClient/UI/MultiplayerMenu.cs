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

        private static Button _micBtn;
        private static Button _deafenBtn;
        private static Button _pttBtn;
        private static Button _pttKeyBtn;
        private static Button _collisionBtn;
        private static Button _cutsceneBtn;
        private static Button _nametagBtn;
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

            rSlider = tab.AddSlider(GetChannelLabel(lang.Red, color.r), 0f, 1f, color.r,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.r = val;
                    ModSettings.player.SuitColor.Value = c;
                    UpdateSliderLabel(rSlider, GetChannelLabel(LanguageManager.GetCurrentLanguage().Red, val));
                    RefreshColorPreview();
                }));

            gSlider = tab.AddSlider(GetChannelLabel(lang.Green, color.g), 0f, 1f, color.g,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.g = val;
                    ModSettings.player.SuitColor.Value = c;
                    UpdateSliderLabel(gSlider, GetChannelLabel(LanguageManager.GetCurrentLanguage().Green, val));
                    RefreshColorPreview();
                }));

            bSlider = tab.AddSlider(GetChannelLabel(lang.Blue, color.b), 0f, 1f, color.b,
                (UnityAction<float>)(val =>
                {
                    var c = ModSettings.player.SuitColor.Value; c.b = val;
                    ModSettings.player.SuitColor.Value = c;
                    UpdateSliderLabel(bSlider, GetChannelLabel(LanguageManager.GetCurrentLanguage().Blue, val));
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
            _collisionBtn = tab.AddButton(GetCollisionLabel(), (UnityAction)OnCollisionClicked);
            _cutsceneBtn  = tab.AddButton(GetCutsceneLabel(),  (UnityAction)OnCutsceneClicked);
            _nametagBtn   = tab.AddButton(GetNametagLabel(),   (UnityAction)OnNametagClicked);
        }

        private static void ConfigureAudioTab(MenuInjectionLibrary.TabBuilder tab)
        {
            _micBtn    = tab.AddButton(GetMicLabel(),    (UnityAction)OnMicClicked);
            _deafenBtn = tab.AddButton(GetDeafenLabel(), (UnityAction)OnDeafenClicked);
            _pttBtn    = tab.AddButton(GetPttLabel(),    (UnityAction)OnPttClicked);
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

        private static void OnMicClicked()
        {
            ModSettings.audio.MicrophoneEnabled.Value = !ModSettings.audio.MicrophoneEnabled.Value;
            LocalPlayer.Instance?.SetMicrophoneEnabled(ModSettings.audio.MicrophoneEnabled.Value);
            SetBtnText(_micBtn, GetMicLabel());
        }

        private static void OnDeafenClicked()
        {
            ModSettings.audio.Deafened.Value = !ModSettings.audio.Deafened.Value;
            SetBtnText(_deafenBtn, GetDeafenLabel());
        }

        private static void OnPttClicked()
        {
            ModSettings.audio.PushToTalk.Value = !ModSettings.audio.PushToTalk.Value;
            LocalPlayer.Instance?.SetPushToTalkEnabled(ModSettings.audio.PushToTalk.Value);
            SetBtnText(_pttBtn, GetPttLabel());
            if (_pttKeyBtn != null) _pttKeyBtn.interactable = ModSettings.audio.PushToTalk.Value;
        }

        private static void OnPttKeyClicked()
        {
            _isWaitingForKey = true;
            SetBtnText(_pttKeyBtn, LanguageManager.GetCurrentLanguage().PressAnyKey);
        }

        private static void OnCollisionClicked()
        {
            if (Core.networkManager.client == null) return;
            ModSettings.player.Collisions.Value = !ModSettings.player.Collisions.Value;
            Core.networkManager.SendCollisionToggle(ModSettings.player.Collisions.Value);
            foreach (var player in Core.networkManager.players)
            {
                bool shouldEnable = ModSettings.player.Collisions.Value && player.Value.netCollisionsEnabled;
                if (shouldEnable) player.Value.EnableCollision();
                else              player.Value.DisableCollision();
            }
            SetBtnText(_collisionBtn, GetCollisionLabel());
        }

        private static void OnCutsceneClicked()
        {
            if (Core.networkManager.client == null) return;
            ModSettings.player.CutscenePlayerVisibility.Value = !ModSettings.player.CutscenePlayerVisibility.Value;
            SetBtnText(_cutsceneBtn, GetCutsceneLabel());
        }

        private static void OnNametagClicked()
        {
            if (Core.networkManager.client == null) return;
            ModSettings.player.ShowNametags.Value = !ModSettings.player.ShowNametags.Value;
            SetBtnText(_nametagBtn, GetNametagLabel());
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

        private static string GetMicLabel()
            => ModSettings.audio.MicrophoneEnabled.Value
                ? LanguageManager.GetCurrentLanguage().DisableMicrophone
                : LanguageManager.GetCurrentLanguage().EnableMicrophone;

        private static string GetDeafenLabel()
            => ModSettings.audio.Deafened.Value
                ? LanguageManager.GetCurrentLanguage().Undeafen
                : LanguageManager.GetCurrentLanguage().Deafen;

        private static string GetPttLabel()
            => ModSettings.audio.PushToTalk.Value
                ? LanguageManager.GetCurrentLanguage().DisablePushToTalk
                : LanguageManager.GetCurrentLanguage().EnablePushToTalk;

        private static string GetCollisionLabel()
            => ModSettings.player.Collisions.Value
                ? LanguageManager.GetCurrentLanguage().DisableCollisions
                : LanguageManager.GetCurrentLanguage().EnableCollisions;

        private static string GetCutsceneLabel()
            => ModSettings.player.CutscenePlayerVisibility.Value
                ? LanguageManager.GetCurrentLanguage().DisablePlayerCutsceneVisibility
                : LanguageManager.GetCurrentLanguage().EnablePlayerCutsceneVisibility;

        private static string GetNametagLabel()
            => ModSettings.player.ShowNametags.Value
                ? LanguageManager.GetCurrentLanguage().DisableNametags
                : LanguageManager.GetCurrentLanguage().EnableNametags;

        private static string GetGainLabel()
            => $"{LanguageManager.GetCurrentLanguage().MicrophoneGain} {ModSettings.audio.MicrophoneGain.Value:F2}x";

        private static string GetChannelLabel(string channel, float value)
            => $"{channel}: {(int)(value * 255)}";

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
