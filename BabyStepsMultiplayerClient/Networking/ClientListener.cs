using BabyStepsMultiplayerClient.Player;
using LiteNetLib;
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
            Core.uiManager.notificationsUI.AddMessage("Connected to server");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            Core.networkManager.server = null;
            Core.networkManager.Disconnect();
            Resources.UnloadUnusedAssets();
        }

        public void OnConnectionRequest(ConnectionRequest req) 
            => req.AcceptIfKey("cuzzillobochfoddy");
        public void OnNetworkError(IPEndPoint ep, SocketError error)
            => Core.logger.Error($"Network error: {error}");

        public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader reader, UnconnectedMessageType type)
            => ReadPacket(reader);
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
            => ReadPacket(reader);

        private void ReadPacket(NetPacketReader reader)
        {
            byte[] fullData = reader.GetRemainingBytes();
            reader.Recycle();

            if (fullData.Length < 2)
            {
                Core.logger.Warning("Received packet too short to contain length header.");
                return;
            }

            ushort declaredLength = BitConverter.ToUInt16(fullData, 0);

            if (declaredLength != fullData.Length)
            {
                Core.logger.Warning($"Packet length mismatch: expected {declaredLength}, got {fullData.Length}");
                return;
            }

            byte[] data = new byte[fullData.Length - 2];
            Buffer.BlockCopy(fullData, 2, data, 0, data.Length);

            Core.networkManager.HandleServerMessage(data);
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }
}
