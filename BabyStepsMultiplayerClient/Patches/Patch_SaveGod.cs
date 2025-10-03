using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace BabyStepsMultiplayerClient.Patches
{
    [HarmonyPatch]
    internal class Patch_SaveGod
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveGod), nameof(SaveGod.LoadSave))]
        private static bool LoadSave_Prefix(SaveGod __instance)
        {
            /*
            Core.networkManager.Disconnect();

            // Run Original
            return true;
            */
            MelonLogger.Msg("SaveGod LoadSave");
            return true;
        }

        [HarmonyPatch(typeof(SaveGod), nameof(SaveGod.EmptySave))]
        private static bool EmptySave_Prefix(SaveGod __instance)
        {
            MelonLogger.Msg("SaveGod EmptySave");
            return true;
        }
    }
}
