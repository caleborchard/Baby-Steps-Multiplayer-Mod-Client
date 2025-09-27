using BabyStepsMultiplayerClient.Networking;
using BabyStepsMultiplayerClient.UI;
using MelonLoader;

[assembly: MelonInfo(typeof(BabyStepsMultiplayerClient.Core),
    "BabyStepsMultiplayerClient",
    "1.1.0",
    "Caleb Orchard",
    "https://github.com/caleborchard/Baby-Steps-Multiplayer-Mod-Client")]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

// This will prevent the Mod from attempting to run on anything below MelonLoader v0.7.2
[assembly: VerifyLoaderVersion("0.7.2", true)]

namespace BabyStepsMultiplayerClient
{
    public class Core : MelonMod
    {
        public const string cloneText = "(Clone)";

        public static UIManager uiManager;
        public static NetworkManager networkManager;
        public static LocalPlayer localPlayer;

        public override void OnInitializeMelon() { }
        [Obsolete]
        public override void OnApplicationStart() 
        {
            uiManager = new();
            networkManager = new();
            localPlayer = new();
        }

        public override void OnGUI() 
            => uiManager.Draw();

        public override void OnUpdate()
        {
            uiManager.Update();
            networkManager.Update();
            localPlayer.Update();
        }

        public override void OnLateUpdate()
            => networkManager.LateUpdate();

        public override void OnApplicationQuit()
            => networkManager.Disconnect();
    }
}
