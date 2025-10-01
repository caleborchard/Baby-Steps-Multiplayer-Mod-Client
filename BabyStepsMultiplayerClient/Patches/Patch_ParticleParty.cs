using BabyStepsMultiplayerClient.Networking;
using HarmonyLib;
using Il2Cpp;

namespace BabyStepsMultiplayerClient.Patches
{
    //[HarmonyPatch]
    internal class Patch_ParticleParty
    {
        /*
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ParticleParty), nameof(ParticleParty.OnPlant))]
        private static bool OnPlant_Prefix(ParticleParty __instance,
            FootData __0) // foot
        {
            if (Core.networkManager.client == null) 
                return true;

            if (Core.networkManager.isRunningNetParticle) 
            {
                Core.networkManager.isRunningNetParticle = false; 
                return true; 
            }

            byte[] serialized = FootDataHelpers.SerializeFootData(__0);

            List<byte> packet = new();

            packet.Add(0x00);
            packet.AddRange(serialized);

            //Core.thisInstance.SendParticle(packet.ToArray());

            // Run Original
            return true;
        }
        
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ParticleParty), nameof(ParticleParty.OnSlip))]
        private static bool OnSlip_Prefix(ParticleParty __instance,
            FootData __0) // foot
        {
            if(Core.networkManager.client == null) 
                return true;

            if (Core.networkManager.isRunningNetParticle)
            {
                Core.networkManager.isRunningNetParticle = false;
                return true;
            }

            byte[] serialized = FootDataHelpers.SerializeFootData(__0);
            List<byte> packet = new();

            packet.Add(0x01);
            packet.AddRange(serialized);

            //Core.thisInstance.SendParticle(packet.ToArray());

            // Run Original
            return true;
        }
        */
    }
}
