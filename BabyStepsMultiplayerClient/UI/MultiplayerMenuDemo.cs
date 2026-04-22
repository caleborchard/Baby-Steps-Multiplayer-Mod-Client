using Il2Cpp;
using UnityEngine.Events;

namespace BabyStepsMultiplayerClient.UI
{
    public static class MultiplayerMenuDemo
    {
        private static MenuInjectionLibrary.InjectedMenu _menu;

        // Call once from Core — registers with the library's generic patch automatically.
        public static void Initialize()
        {
            if (_menu != null) return;
            _menu = MenuInjectionLibrary.CreateMenu("Multiplayer")
                .AddTab("General", ConfigureTab)
                .AddTab("Audio",   ConfigureTab)
                .AddTab("Debug",   ConfigureTab)
                .AddFixedButton("Open Multiplayer Panel", (UnityAction)(() =>
                {
                    if (Core.uiManager?.serverConnectUI != null)
                        Core.uiManager.serverConnectUI.IsOpen = true;
                }))
                .AddFixedButton("Back")
                .Build();
        }

        private static void ConfigureTab(MenuInjectionLibrary.TabBuilder tab)
        {
            tab.AddLabel("UI Element Showcase");
            tab.AddButton("Example Button", (UnityAction)(()       => Core.logger?.Msg("[Demo] Button clicked")));
            tab.AddToggle("Example Toggle", false, (UnityAction<bool>)(val  => Core.logger?.Msg($"[Demo] Toggle: {val}")));
            tab.AddSlider("Example Slider", 0f, 1f, 0.5f, (UnityAction<float>)(val => Core.logger?.Msg($"[Demo] Slider: {val:F2}")));
        }
    }
}