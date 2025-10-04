using BabyStepsMultiplayerClient.Extensions;
using BabyStepsMultiplayerClient.Player;
using HarmonyLib;
using Il2Cpp;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_PlayerMovement
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.DropHandItem))]
        private static void DropHandItem_Postfix(PlayerMovement __instance,
            int __0) // hat
        {
            Core.DebugMsg("PlayerMovement DropHandItem HarmonyPatch");

            if (Core.networkManager.client == null)
                return;
            if (LocalPlayer.Instance == null)
                return;

            Grabable heldItem = LocalPlayer.Instance.playerMovement.handItems[__0];
            if (heldItem != null)
                Core.networkManager.SendDropGrabable(__0);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.KnockOffHat))]
        private static void KnockOffHat_Postfix(PlayerMovement __instance)
        {
            Core.DebugMsg("PlayerMovement KnockOffHat HarmonyPatch");

            if (Core.networkManager.client == null)
                return;
            Core.networkManager.SendDoffHat();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.WearHat))]
        private static void WearHat_Postfix(PlayerMovement __instance, 
            Hat __0) // hat
        {
            Core.DebugMsg("PlayerMovement WearHat HarmonyPatch");

            if (Core.networkManager.client == null)
                return;

            //Core.networkManager.SendDonHat(__0);
            __instance.StartCoroutine(DelayedSendHat(__0));

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
            // 30 is arbitrary, not too long to be super noticeable but enough to solve most issues
            for (int i = 0; i < 30; i++) yield return null;

            Core.networkManager.SendDonHat(hat);
        }
    }
}
