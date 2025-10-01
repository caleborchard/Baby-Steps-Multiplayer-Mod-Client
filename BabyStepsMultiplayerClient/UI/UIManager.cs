using BabyStepsMultiplayerClient.Networking;
using MelonLoader;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class UIManager
    {
        public bool showServerPanel { get; private set; }
        public bool showPlayersTab { get; private set; }

        public PlayersTabUI playersTabUI { get; private set; }
        public ServerConnectUI serverConnectUI { get; private set; }
        public IngameMessagesUI ingameMessagesUI { get; private set; }

        public UIManager()
        {
            serverConnectUI = new ServerConnectUI();
            serverConnectUI.LoadConfig();

            ingameMessagesUI = new IngameMessagesUI();
            playersTabUI = new PlayersTabUI();
        }

        public void Draw()
        {
            ingameMessagesUI.DrawUI();
            if (showServerPanel) serverConnectUI.DrawUI();
            if (showPlayersTab) playersTabUI.DrawUI();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2)) showServerPanel = !showServerPanel;

            if (MelonDebug.IsEnabled() && Input.GetKeyDown(KeyCode.F3))
            {
                Core.networkManager.Connect(serverConnectUI.uiIP, int.Parse(serverConnectUI.uiPORT), serverConnectUI.uiPassword);
            }

            if (Core.networkManager.client == null) showPlayersTab = false;
            else showPlayersTab = Input.GetKey(KeyCode.Tab);
        }

        public void ApplyCollisionToggle(RemotePlayer player, bool collisionsEnabled)
        {
            if (collisionsEnabled)
            {
                player.netCollisionsEnabled = true;
                ingameMessagesUI.AddMessage($"{player.displayName} has enabled collisions");

                if (serverConnectUI.uiCollisionsEnabled) player.EnableCollision();
                else player.DisableCollision();
            }
            else
            {
                player.netCollisionsEnabled = false;
                ingameMessagesUI.AddMessage($"{player.displayName} has disabled collisions");

                player.DisableCollision();
            }
        }
    }
}
