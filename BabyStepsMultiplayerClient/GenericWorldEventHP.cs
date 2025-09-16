using HarmonyLib;
using Il2Cpp;
using Il2CppNWH.DWP2.WaterObjects;
using MelonLoader;
using Microsoft.VisualBasic;
using System.Text;
using UnityEngine;
using static Il2CppTechnie.PhysicsCreator.SphereUtils;

namespace BabyStepsMultiplayerClient
{
    [HarmonyPatch(typeof(ParticleParty), "OnPlant")]
    public static class PP_OnPlant_Patch
    {
        static void Prefix(ParticleParty __instance, object[] __args)
        {
            if (Core.thisInstance.client == null) return;

            if (Core.isRunningNetParticle) { Core.isRunningNetParticle = false; return; }

            FootData foot = (FootData)__args[0];
            byte[] serialized = FootDataHelpers.SerializeFootData(foot);

            List<byte> packet = new();

            packet.Add(0x00);
            packet.AddRange(serialized);

            Core.thisInstance.SendParticle(packet.ToArray());
        }
    }

    [HarmonyPatch(typeof(ParticleParty), "OnSlip")]
    public static class PP_OnSlip_Patch
    {
        static void Prefix(ParticleParty __instance, object[] __args)
        {
            if (Core.thisInstance.client == null) return;

            if (Core.isRunningNetParticle) { Core.isRunningNetParticle = false; return; }

            FootData foot = (FootData)__args[0];
            byte[] serialized = FootDataHelpers.SerializeFootData(foot);
            List<byte> packet = new();

            packet.Add(0x01);
            packet.AddRange(serialized);

            Core.thisInstance.SendParticle(packet.ToArray());
        }
    }
    [HarmonyPatch(typeof(PlayerMovement), "WearHat")]
    public static class PM_WearHat_Patch
    {
        static void Postfix(PlayerMovement __instance, Hat hat)
        {
            if (Core.thisInstance.client == null) return;

            Core.thisInstance.SendDonHat(hat);
        }
    }

    [HarmonyPatch(typeof(PlayerMovement), "KnockOffHat")]
    public static class PM_KnockOffHat_Patch
    {
        static void Prefix(PlayerMovement __instance)
        {
            if (Core.thisInstance.client == null) return;

            Core.thisInstance.SendDoffHat();
        }
    }

    [HarmonyPatch(typeof(Grabable), "PlaceInHand")]
    public static class G_PlaceInHand_Patch
    {
        static void Postfix(Grabable __instance, Transform hand, int handIndex)
        {
            if (Core.thisInstance.client == null) return;

            Core.thisInstance.SendHoldGrabable(__instance, handIndex);
        }
    }

    [HarmonyPatch(typeof(PlayerMovement), "DropHandItem")]
    public static class PM_DropHandItem_Patch
    {
        static void Prefix(PlayerMovement __instance, int hand)
        {
            if (Core.thisInstance.client == null) return;

            Grabable heldItem = Core.basePlayerMovement.handItems[hand];
            if(heldItem != null) Core.thisInstance.SendDropGrabable(hand);
        }
    }

    [HarmonyPatch(typeof(SaveGod), "LoadSave")]
    public static class SG_LoadSave_Patch
    {
        static void Prefix(SaveGod __instance)
        {
            Core _core = Core.thisInstance;
            if (_core.client != null)
            {
                _core.Disconnect();
            }
        }
    }
}