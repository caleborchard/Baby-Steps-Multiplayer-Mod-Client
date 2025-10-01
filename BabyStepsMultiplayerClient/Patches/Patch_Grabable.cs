using BabyStepsMultiplayerClient.Debug;
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

            BBSMMdBug.Log("Grabable PlaceInHand HarmonyPatch");

            if (Core.networkManager.client == null) 
                return;

            Core.networkManager.SendHoldGrabable(__instance, __1);
        }
    }
}
