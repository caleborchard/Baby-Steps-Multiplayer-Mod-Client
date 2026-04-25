using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BabyStepsMultiplayerClient.Patches
{
    // This is to fix the bug found natively in the game where toggle buttons do not work when clicked.
    [HarmonyPatch]
    internal static class Patch_UIClickDedup
    {
        private static readonly Dictionary<int, int> _toggleClickFrame = new();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Button), nameof(Button.OnSubmit))]
        private static bool Button_OnSubmit(BaseEventData eventData)
            => !Input.GetMouseButtonDown(0);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
        private static bool Toggle_OnPointerClick(Toggle __instance, PointerEventData eventData)
        {
            // Block non-Left events — original body ignores them anyway.
            if (eventData.button != PointerEventData.InputButton.Left) return false;

            int frame = Time.frameCount;
            int id = __instance.GetInstanceID();
            bool isDup = _toggleClickFrame.TryGetValue(id, out int last) && last == frame;

            if (isDup) return false;
            _toggleClickFrame[id] = frame;

            // Call Set() directly through the managed interop layer which uses the real (noninlined) method.
            if (__instance.IsActive() && __instance.IsInteractable())
                __instance.Set(!__instance.isOn, true);

            return false; // Always block, handled above.
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
        private static void Toggle_OnPointerClick_Post(Toggle __instance, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
        }
    }
}
