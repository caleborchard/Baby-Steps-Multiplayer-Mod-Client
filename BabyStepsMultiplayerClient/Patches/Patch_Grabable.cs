using HarmonyLib;
using Il2Cpp;
//using UnityEngine;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_Grabable
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grabable), nameof(Grabable.PlaceInHand))]
        private static void PlaceInHand_Postfix(Grabable __instance, 
            //Transform __0, // hand
            int __1) // handIndex
        {
            if (Core.thisInstance.client == null) 
                return;

            Core.thisInstance.SendHoldGrabable(__instance, __1);
        }
    }
}
