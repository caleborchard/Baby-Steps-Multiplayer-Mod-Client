using BabyStepsNetworking.Transport;
using BabyStepsNetworking.Transport.Steam;
using System.Text;

namespace BabyStepsMultiplayerClient.Networking.Steam
{
    public sealed class BBSSteamClientTransport : SteamClientTransport
    {
        private const int  Channel  = 113; // matches SteamServerTransport
        private const byte AuthByte = 0xFD;

        private ulong _hostId      = 0;
        private bool  _connected   = false;
        private long  _connectTimeMs = 0; // used to detect stale 0xFE from previous sessions

        public override event Action?          Connected;
        public override event Action<string>?  Disconnected;
        public override event Action<byte[]>?  PacketReceived;

        public override bool IsConnected => _connected;

        public override void Connect(string address, int port, string connectionKey)
        {
            if (!ulong.TryParse(address, out _hostId) || _hostId == 0)
            {
                Core.logger.Error($"[Steam Client] Invalid or zero host SteamID64: '{address}'");
                return;
            }

            NativeSteamAPI.AcceptP2PSession(_hostId);

            NativeSteamAPI.SendP2PPacket(_hostId, new byte[] { 0xFF /* ping */ }, 0 /* unreliable */, Channel);

            // Send AUTH packet so the host can validate before accepting the P2P session.
            byte[] keyBytes = Encoding.UTF8.GetBytes(connectionKey);
            byte[] auth     = new byte[1 + keyBytes.Length];
            auth[0] = AuthByte;
            System.Buffer.BlockCopy(keyBytes, 0, auth, 1, keyBytes.Length);
            NativeSteamAPI.SendP2PPacket(_hostId, auth, 2 /* k_EP2PSendReliable */, Channel);

            _connectTimeMs = Environment.TickCount64;
            _connected = true;
            Connected?.Invoke();
        }

        public override void Disconnect()
        {
            if (!_connected) return;
            // Notify the host immediately so it can clean up without waiting for timeout.
            NativeSteamAPI.SendP2PPacket(_hostId, new byte[] { 0xFE }, 2 /* reliable */, Channel);
            _connected           = false;
            _hostId              = 0;
            _receivedFirstPacket = false;
            Disconnected?.Invoke("Disconnected");
        }

        private bool _receivedFirstPacket = false;

        public override void Poll()
        {
            if (!_connected) return;

            while (NativeSteamAPI.IsP2PPacketAvailable(out uint size, Channel))
            {
                var buf = new byte[size];
                if (NativeSteamAPI.ReadP2PPacket(buf, out _, out ulong from, Channel))
                {
                    if (from != _hostId)
                    {
                        Core.logger.Msg($"[Steam Client] Dropping packet from unexpected SteamID={from} (expected {_hostId})");
                        continue;
                    }
                    if (!_receivedFirstPacket) _receivedFirstPacket = true;
                    // Accept the P2P session so Steam delivers subsequent packets from the host.
                    NativeSteamAPI.AcceptP2PSession(_hostId);

                    // 0xFE = graceful server shutdown disconnect immediately
                    if (buf.Length == 1 && buf[0] == 0xFE)
                    {
                        long elapsed = Environment.TickCount64 - _connectTimeMs;
                        if (elapsed < 500)
                        {
                            Core.logger.Msg($"[Steam Client] Discarding stale 0xFE ({elapsed}ms after connect — previous session residue)");
                            continue;
                        }
                        Core.logger.Msg("[Steam Client] Server sent graceful disconnect");
                        _connected           = false;
                        _hostId              = 0;
                        _receivedFirstPacket = false;
                        Disconnected?.Invoke("Server disconnected");
                        return;
                    }

                    // 0xFC = rejected (wrong password or version mismatch).
                    if (buf.Length == 1 && buf[0] == 0xFC)
                    {
                        long elapsed = Environment.TickCount64 - _connectTimeMs;
                        Core.logger.Msg($"[Steam Client] Connection rejected by server ({elapsed}ms after connect)");
                        _connected           = false;
                        _hostId              = 0;
                        _receivedFirstPacket = false;
                        Disconnected?.Invoke("Wrong password");
                        return;
                    }

                    PacketReceived?.Invoke(buf);
                }
            }
        }

        public override void Send(byte[] payload, PacketDelivery delivery)
        {
            if (!_connected) return;
            NativeSteamAPI.SendP2PPacket(_hostId, payload, ToSendType(delivery), Channel);
        }

        private static int ToSendType(PacketDelivery d) =>
            d == PacketDelivery.Unreliable ? 0 /* k_EP2PSendUnreliable */ : 2 /* k_EP2PSendReliable */;
    }
}
