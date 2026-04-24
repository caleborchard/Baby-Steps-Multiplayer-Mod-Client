using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.UI.Elements;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class UIManager
    {
        public bool showChatTab;

        private bool playerMovementSuppressedForChat;
        private bool playerMovementWasEnabledBeforeChat;

        public PlayersTabUI playersTabUI { get; private set; }
        public NotificationUI notificationsUI { get; private set; }
        public ChatTabUI chatTabUI { get; private set; }

        public UIManager()
        {
            StyleManager.Fonts.Prepare();

            notificationsUI = new NotificationUI();
            playersTabUI = new PlayersTabUI();
            chatTabUI = new ChatTabUI();
        }

        public void Draw()
        {
            StyleManager.Styles.Prepare();

            notificationsUI.DrawUI();
            playersTabUI.Draw();

            if (showChatTab)
                chatTabUI.DrawUI();
            else
                notificationsUI.ShowChatHistory = false;
        }

        public void Update()
        {
            if (Core.networkManager.client == null)
            {
                playersTabUI.IsOpen = false;
                showChatTab = false;
            }
            else
            {
                if (!showChatTab && Input.GetKeyDown(KeyCode.T))
                    showChatTab = true;

                playersTabUI.IsOpen = !showChatTab && Input.GetKey(KeyCode.Tab);
            }

            UpdateGameplayInputSuppression();
        }

        private void UpdateGameplayInputSuppression()
        {
            var localPlayer = LocalPlayer.Instance;
            var movement = localPlayer?.playerMovement;
            if (movement == null)
            {
                playerMovementSuppressedForChat = false;
                return;
            }

            if (showChatTab)
            {
                if (!playerMovementSuppressedForChat)
                {
                    playerMovementWasEnabledBeforeChat = movement.enabled;
                    if (playerMovementWasEnabledBeforeChat)
                        movement.enabled = false;

                    playerMovementSuppressedForChat = true;
                }
            }
            else if (playerMovementSuppressedForChat)
            {
                movement.enabled = playerMovementWasEnabledBeforeChat;
                playerMovementSuppressedForChat = false;
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
