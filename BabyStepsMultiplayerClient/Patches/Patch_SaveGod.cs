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
            // Keep compatibility across game/binding versions where the load
            // entrypoint name changed.
            return AccessTools.Method(typeof(SaveGod), "TryLoadSave")
                ?? AccessTools.Method(typeof(SaveGod), "LoadSave")
                ?? AccessTools.Method(typeof(SaveGod), "TryLoad");
        }

        [HarmonyPrefix]
        private static bool TryLoadSave_Prefix(SaveGod __instance)
        {
            //Core.DebugMsg("SaveGod TryLoadSave HarmonyPatch");

            if (Core.networkManager != null)
                Core.networkManager.Disconnect();

            // Run Original
            return true;
        }
    }
}
