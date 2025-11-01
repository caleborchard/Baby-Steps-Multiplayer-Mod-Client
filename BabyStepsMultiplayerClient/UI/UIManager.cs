using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI.Elements;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class UIManager
    {
        public bool showChatTab;

        public PlayersTabUI playersTabUI { get; private set; }
        public ServerConnectUI serverConnectUI { get; private set; }
        public NotificationUI notificationsUI { get; private set; }
        public ChatTabUI chatTabUI { get; private set; }

        public UIManager()
        {
            StyleManager.Fonts.Prepare();

            serverConnectUI = new ServerConnectUI();
            serverConnectUI.LoadConfig();

            notificationsUI = new NotificationUI();
            playersTabUI = new PlayersTabUI();
            chatTabUI = new ChatTabUI();
        }

        public void Draw()
        {
            StyleManager.Styles.Prepare();

            notificationsUI.DrawUI();

            serverConnectUI.Draw();
            playersTabUI.Draw();

            if (showChatTab)
                chatTabUI.DrawUI();
        }

        public void Update()
        {
            // Toggle the Menu but only when Chat Input is Disabled
            if (!showChatTab && Input.GetKeyDown(KeyCode.F2))
                serverConnectUI.IsOpen = !serverConnectUI.IsOpen;

            // Only use while Connected and the Menu is Closed
            if (serverConnectUI.IsOpen || (Core.networkManager.client == null))
            {
                playersTabUI.IsOpen = false;
                showChatTab = false;
            }
            else
            {
                // Toggle the Chat Input when not already Active
                if (!showChatTab && Input.GetKeyDown(KeyCode.T))
                    showChatTab = true;

                // Toggle the Scoreboard only when Chat Input is Disabled
                playersTabUI.IsOpen = !showChatTab && Input.GetKey(KeyCode.Tab);
            }
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
