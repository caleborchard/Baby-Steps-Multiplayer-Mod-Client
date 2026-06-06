using HarmonyLib;
using Il2Cpp;
using System.Reflection;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_SaveGod
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(SaveGod), "TryLoadSave")
                ?? AccessTools.Method(typeof(SaveGod), "LoadSave")
                ?? AccessTools.Method(typeof(SaveGod), "TryLoad");
        }

        [HarmonyPrefix]
        private static bool TryLoadSave_Prefix(SaveGod __instance)
        {
            return true;
        }

        [HarmonyPostfix]
        private static void TryLoadSave_Postfix()
        {
            // After a save loads, clear remote players' stale visual state (hats, held items from the previous save) and request fresh state from the server.
            if (Core.networkManager?.IsConnected == true)
            {
                Core.logger.Msg("[SaveGod] Save loaded while connected — clearing remote state and requesting resync");
                Core.networkManager.RequestStateResync();
            }
        }
    }
}
