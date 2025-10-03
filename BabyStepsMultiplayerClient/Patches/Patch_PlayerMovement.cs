using BabyStepsMultiplayerClient.Debug;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_PlayerMovement
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.DropHandItem))]
        private static bool DropHandItem_Prefix(PlayerMovement __instance,
            int __0) // hat
        {

            BBSMMdBug.Log("PlayerMovement DropHandItem HarmonyPatch");

            if (Core.networkManager.client == null) return true;

            Grabable heldItem = Core.localPlayer.basePlayerMovement.handItems[__0];
            if (heldItem != null) Core.networkManager.SendDropGrabable(__0);

            // Run Original
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.KnockOffHat))]
        private static bool KnockOffHat_Prefix(PlayerMovement __instance)
        {

            BBSMMdBug.Log("PlayerMovement KnockOffHat HarmonyPatch");

            if (Core.networkManager.client == null) return true;

            Core.networkManager.SendDoffHat();

            // Run Original
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.WearHat))]
        private static void WearHat_Postfix(PlayerMovement __instance, 
            Hat __0) // hat
        {

            BBSMMdBug.Log("PlayerMovement WearHat HarmonyPatch");

            if (Core.networkManager.client == null) return;

            //Core.networkManager.SendDonHat(__0);
            MelonCoroutines.Start(DelayedSendHat(__0)); // Switch to HerpDerp StartCoroutine Extension on merge

            for (int i = 0; i < __instance.handItems.Length; i++)
            {
                Grabable item = __instance.handItems[i];
                if ((item != null)
                    && item.name.Contains(__0.name))
                    Core.networkManager.SendDropGrabable(i);
            }
        }

        private static System.Collections.IEnumerator DelayedSendHat(Hat hat)
        {
            // Wait arbitrary amount of time to mitigate hat not being properly positioned on head yet
            for (int i = 0; i < 30; i++) yield return null;

            Core.networkManager.SendDonHat(hat);
        }
    }
}
