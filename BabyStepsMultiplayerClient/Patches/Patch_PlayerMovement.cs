using HarmonyLib;
using Il2Cpp;

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
            if (Core.thisInstance.client == null)
                return true;

            Grabable heldItem = Core.basePlayerMovement.handItems[__0];
            if (heldItem != null)
                Core.thisInstance.SendDropGrabable(__0);

            // Run Original
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.KnockOffHat))]
        private static bool KnockOffHat_Prefix(PlayerMovement __instance)
        {
            if (Core.thisInstance.client == null)
                return true;

            Core.thisInstance.SendDoffHat();

            // Run Original
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.WearHat))]
        private static void WearHat_Postfix(PlayerMovement __instance, 
            Hat __0) // hat
        {
            if (Core.thisInstance.client == null)
                return;

            Core.thisInstance.SendDonHat(__0);

            for (int i = 0; i < __instance.handItems.Length; i++)
            {
                Grabable item = __instance.handItems[i];
                if ((item != null)
                    && item.name.Contains(__0.name))
                    Core.thisInstance.SendDropGrabable(i);
            }
        }
    }
}
