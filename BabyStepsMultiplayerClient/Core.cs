using BabyStepsMultiplayerClient.Components;
using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using FMOD = Il2CppFMOD;

[assembly: MelonInfo(typeof(BabyStepsMultiplayerClient.Core),
    "BabyStepsMultiplayerClient",
    "1.1.5",
    "Caleb Orchard",
    "https://github.com/caleborchard/Baby-Steps-Multiplayer-Mod-Client")]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

namespace BabyStepsMultiplayerClient
{
    public class Core : MelonMod
    {
        public const string SERVER_VERSION = "104";

        public const string cloneText = "(Clone)";

        public static MelonLogger.Instance logger;

        public static UIManager uiManager;
        public static NetworkManager networkManager;
        public static Il2CppFMOD.System CoreSystem;

        public override void OnInitializeMelon() { }
        [Obsolete]
        public override void OnApplicationStart() 
        {
            logger = LoggerInstance;

            ManagedEnumerator.Register();

            uiManager = new();
            networkManager = new();

            logger.Msg("Initialized!");

            VersionCheck.CheckForUpdateAsync();
        }

        public override void OnGUI()
            => uiManager.Draw();

        Il2CppFMOD.Sound sound;
        Il2CppFMOD.Channel channel;
        Il2CppFMOD.ChannelGroup masterGroup;

        public override void OnUpdate()
        {
            uiManager.Update();
            networkManager.Update();

            if (LocalPlayer.Instance != null)
                LocalPlayer.Instance.Update();

            if (MelonDebug.IsEnabled())
            {
                if (Input.GetKeyDown(KeyCode.F3))
                    networkManager.Connect(ModSettings.connection.Address.Value,
                        ModSettings.connection.Port.Value,
                        ModSettings.connection.Password.Value);

                if (Input.GetKeyDown(KeyCode.F4))
                    networkManager.Disconnect();
            }
        }

        private static FMOD.VECTOR UnityToFMOD(Vector3 v)
        {
            // FMOD uses a left-handed coordinate system: +X right, +Y up, +Z forward
            return new FMOD.VECTOR { x = v.x, y = v.y, z = v.z };
        }

        public override void OnLateUpdate()
        {
            if (LocalPlayer.Instance != null)
                LocalPlayer.Instance.LateUpdate();

            networkManager.LateUpdate();
        }

        public override void OnApplicationQuit()
            => networkManager.Disconnect();

        public static void DebugMsg(string msg)
        {
            if (!MelonDebug.IsEnabled())
                return;
            logger.Msg(msg);
        }

        public static bool RegisterComponent<T>(params Type[] interfaces)
            where T : class
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<T>(new()
                {
                    LogSuccess = true,
                    Interfaces = interfaces
                });
            }
            catch (Exception e)
            {
                logger.Error($"Exception while attempting to Register {typeof(T).Name}: {e}");
                return false;
            }
            return true;
        }
    }
}
