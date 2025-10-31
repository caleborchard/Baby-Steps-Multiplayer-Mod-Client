using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI.Elements;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class UIManager
    {
        public bool showServerPanel { get; private set; }
        public bool showPlayersTab { get; private set; }
        public bool showChatTab;

        public PlayersTabUI playersTabUI { get; private set; }
        public ServerConnectUI serverConnectUI { get; private set; }
        public NotificationUI notificationsUI { get; private set; }
        public ChatTabUI chatTabUI { get; private set; }

        public UIManager()
        {
            serverConnectUI = new ServerConnectUI();
            serverConnectUI.LoadConfig();

            notificationsUI = new NotificationUI();
            playersTabUI = new PlayersTabUI();
            chatTabUI = new ChatTabUI();
        }

        public void Draw()
        {
            StyleManager.Fonts.Prepare();
            StyleManager.Styles.Prepare();

            notificationsUI.DrawUI();

            if (showServerPanel)
                serverConnectUI.DrawUI();

            if (showPlayersTab)
                playersTabUI.DrawUI();

            if (showChatTab)
                chatTabUI.DrawUI();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
                showServerPanel = !showServerPanel;

            if (Core.networkManager.client == null)
                showPlayersTab = false;
            else
                showPlayersTab = !showServerPanel && Input.GetKey(KeyCode.Tab);

            if (Input.GetKeyDown(KeyCode.T) && Core.networkManager.client != null)
                showChatTab = true;
        }

        public void ApplyCollisionToggle(RemotePlayer player, bool collisionsEnabled)
        {
            if (collisionsEnabled)
            {
                player.netCollisionsEnabled = true;
                notificationsUI.AddMessage($"{player.displayName} has enabled collisions");

                if (ModSettings.player.Collisions.Value) player.EnableCollision();
                else player.DisableCollision();
            }
            else
            {
                player.netCollisionsEnabled = false;
                notificationsUI.AddMessage($"{player.displayName} has disabled collisions");

                player.DisableCollision();
            }
        }
    }
}
