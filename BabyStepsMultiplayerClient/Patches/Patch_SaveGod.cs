using HarmonyLib;
using Il2Cpp;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_SaveGod
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveGod), nameof(SaveGod.LoadSave))]
        private static bool LoadSave_Prefix(SaveGod __instance)
        {
            Core.networkManager.Disconnect();

            // Run Original
            return true;
        }
    }
}
