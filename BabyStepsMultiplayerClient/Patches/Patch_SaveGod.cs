using HarmonyLib;
using Il2Cpp;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_SaveGod
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveGod), nameof(SaveGod.TryLoadSave))]
        private static bool TryLoadSave_Prefix(SaveGod __instance)
        {
            Core.DebugMsg("SaveGod TryLoadSave HarmonyPatch");

            if (Core.networkManager != null)
                Core.networkManager.Disconnect();

            // Run Original
            return true;
        }
    }
}
