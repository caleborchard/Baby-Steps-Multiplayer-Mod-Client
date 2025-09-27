using Il2Cpp;
using LiteNetLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    internal class ClientListener : INetEventListener
    {
        private readonly Core _core;

        public ClientListener(Core core) 
            => _core = core;

        public void OnPeerConnected(NetPeer peer)
        {
            _core.server = peer;
            _core.UpdateNicknameAndColor();

            Hat hat = Core.basePlayerMovement.currentHat;
            if (hat != null) 
                _core.SendDonHat(hat);

            Grabable rightItem = Core.basePlayerMovement.handItems[0];
            Grabable leftItem = Core.basePlayerMovement.handItems[1];

            if (rightItem != null) 
                _core.SendHoldGrabable(rightItem, 0);
            if (leftItem != null) 
                _core.SendHoldGrabable(leftItem, 1);

            _core.SendJiminyRibbonState();

            _core.SendCollisionToggle(_core.serverConnectUI.uiCollisionsEnabled);

            _core.ingameMessagesUI.AddMessage("Connected to server");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            _core.Disconnect();
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

            _core.HandleServerMessage(data);
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader reader, UnconnectedMessageType type) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    }
}
