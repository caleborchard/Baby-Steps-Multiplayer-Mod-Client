using BabyStepsMultiplayerClient.Components;
using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(BabyStepsMultiplayerClient.Core),
    "BabyStepsMultiplayerClient",
    "1.2.2",
    "Caleb Orchard",
    "https://github.com/caleborchard/Baby-Steps-Multiplayer-Mod-Client")]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

namespace BabyStepsMultiplayerClient
{
    public class Core : MelonMod
    {
        public const string SERVER_VERSION = "106";
        public static string CLIENT_VERSION;

        public const string cloneText = "(Clone)";

        public static MelonLogger.Instance logger;

        public static UIManager uiManager;
        public static NetworkManager networkManager;

        private static bool _firstOpen = false;

        public override void OnLateInitializeMelon()
        {
            CLIENT_VERSION = Info.Version;

            logger = LoggerInstance;

            ManagedEnumerator.Register();

            uiManager = new();
            networkManager = new();

            logger.Msg("Initialized!");

            VersionCheck.CheckForUpdate();
        }

        public override void OnGUI()
            => uiManager.Draw();

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (!_firstOpen 
                && sceneName.Contains("Title"))
            {
                _firstOpen = true;
                uiManager.serverConnectUI.IsOpen = true;
            }
        }

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

        public override void OnLateUpdate()
        {
            if (LocalPlayer.Instance != null)
                LocalPlayer.Instance.LateUpdate();

            networkManager.LateUpdate();
        }

        public override void OnApplicationQuit()
            => networkManager.Disconnect();

        public static bool HasLoadedGame()
        {
            if (Menu.me == null)
                return false;
            return Menu.me.gameInProgress;
        }

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
