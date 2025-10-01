using Il2Cpp;
using LiteNetLib;
using MelonLoader;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    internal class ClientListener : INetEventListener
    {
        public void OnPeerConnected(NetPeer peer)
        {
            Core.networkManager.server = peer;
            Core.networkManager.SendPlayerInformation();

            Hat hat = Core.localPlayer.basePlayerMovement.currentHat;
            if (hat != null)
                Core.networkManager.SendDonHat(hat);

            Grabable rightItem = Core.localPlayer.basePlayerMovement.handItems[0];
            Grabable leftItem = Core.localPlayer.basePlayerMovement.handItems[1];

            if (rightItem != null)
                Core.networkManager.SendHoldGrabable(rightItem, 0);
            if (leftItem != null)
                Core.networkManager.SendHoldGrabable(leftItem, 1);

            Core.networkManager.SendJiminyRibbonState(Core.localPlayer.lastJiminyState);

            Core.networkManager.SendCollisionToggle(Core.uiManager.serverConnectUI.uiCollisionsEnabled);

            Core.uiManager.ingameMessagesUI.AddMessage("Connected to server");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            Core.networkManager.Disconnect();
            Resources.UnloadUnusedAssets();
        }

        public void OnConnectionRequest(ConnectionRequest req) 
            => req.AcceptIfKey("cuzzillobochfoddy");

        public void OnNetworkError(IPEndPoint ep, SocketError error)
            => MelonLogger.Error($"Network error: {error}");

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            byte[] fullData = reader.GetRemainingBytes();
            reader.Recycle();

            if (fullData.Length < 2)
            {
                MelonLogger.Warning("Received packet too short to contain length header.");
                return;
            }

            ushort declaredLength = BitConverter.ToUInt16(fullData, 0);

            if (declaredLength != fullData.Length)
            {
                MelonLogger.Warning($"Packet length mismatch: expected {declaredLength}, got {fullData.Length}");
                return;
            }

            byte[] data = new byte[fullData.Length - 2];
            Buffer.BlockCopy(fullData, 2, data, 0, data.Length);

            Core.networkManager.HandleServerMessage(data);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader reader, UnconnectedMessageType type) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }
}
