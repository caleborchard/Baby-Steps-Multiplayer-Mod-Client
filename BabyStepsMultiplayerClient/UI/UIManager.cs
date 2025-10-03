using BabyStepsMultiplayerClient.Player;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class UIManager
    {
        public bool showServerPanel { get; private set; }
        public bool showPlayersTab { get; private set; }

        public PlayersTabUI playersTabUI { get; private set; }
        public ServerConnectUI serverConnectUI { get; private set; }
        public NotificationUI notificationsUI { get; private set; }

        public Font arialFont;
        public GUIStyle labelStyle;
        public GUIStyle centeredLabelStyle;

        public GUIStyle boxStyle;
        public GUIStyle buttonStyle;

        public UIManager()
        {
            arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            CreateLabelStyles();

            serverConnectUI = new ServerConnectUI();
            serverConnectUI.LoadConfig();

            notificationsUI = new NotificationUI();
            playersTabUI = new PlayersTabUI();
        }

        private void CreateLabelStyles()
        {
            labelStyle = new GUIStyle()
            {
                font = arialFont,
                normal = new()
                {
                    textColor = Color.white,
                },
            };

            centeredLabelStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void CloneDefaultStyles()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.font = arialFont;

                if (buttonStyle.normal == null)
                    buttonStyle.normal = new();
                buttonStyle.normal.textColor = Color.white;
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.font = arialFont;

                if (boxStyle.normal == null)
                    boxStyle.normal = new();
                boxStyle.normal.textColor = Color.white;
            }
        }

        public void Draw()
        {
            CloneDefaultStyles();

            notificationsUI.DrawUI();

            if (showServerPanel)
                serverConnectUI.DrawUI();

            if (showPlayersTab)
                playersTabUI.DrawUI();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
                showServerPanel = !showServerPanel;

            if (Core.networkManager.client == null)
                showPlayersTab = false;
            else
                showPlayersTab = !showServerPanel && Input.GetKey(KeyCode.Tab);
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
