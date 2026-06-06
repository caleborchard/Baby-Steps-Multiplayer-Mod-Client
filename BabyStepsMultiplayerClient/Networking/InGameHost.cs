using BabyStepsMultiplayerClient.Networking.Steam;
using BabyStepsNetworking.Host;
using BabyStepsNetworking.Packets;
using BabyStepsNetworking.Shared;
using BabyStepsNetworking.Transport;
using BabyStepsNetworking.Transport.LiteNetLib;
using BabyStepsNetworking.Transport.LocalLoopback;
using System.Text;

namespace BabyStepsMultiplayerClient.Networking
{
    /// <summary>
    /// Hosts a multiplayer session from within the game client.
    /// After calling Start(), connect your own NetworkManager to "127.0.0.1" on the same port.
    ///
    /// For Steam P2P hosting, replace the LiteNetLibServerTransport with a
    /// SteamServerTransport implementation (uses Il2CppFacepunch.Steamworks).
    ///
    /// Usage:
    ///   var host = new InGameHost();
    ///   host.Start(7777, "mypassword");
    ///   Core.networkManager.Connect("127.0.0.1", 7777, "mypassword");
    ///   // each Update(): host.Tick(deltaMs);
    ///   // on stop: host.Stop();
    /// </summary>
    public class InGameHost
    {
        private NetworkHost _host;

        public bool IsRunning => _host?.IsRunning ?? false;
        public int PlayerCount => _host?.Clients.Count ?? 0;

        public event Action<string>? Log;
        public event Action<string>? PlayerJoined;
        public event Action<string>? PlayerLeft;

        public void Start(int port = 7777, string password = "")
        {
            var settings = new HostSettings
            {
                Port = port,
                Password = string.IsNullOrEmpty(password) ? "cuzzillobochfoddy" : password,
                ServerVersion = Core.SERVER_VERSION,
                VoiceChatEnabled = true,
                MaxBandwidthKbps = 512f,
            };

            var transport = new LiteNetLibServerTransport();
            _host = new NetworkHost(transport, settings);
            _host.Log += msg => Log?.Invoke(msg);
            _host.ClientConnected += OnClientConnected;
            _host.ClientDisconnected += OnClientDisconnected;

            RegisterHandlers();
            _host.Start();
        }

        /// <summary>
        /// Start a Steam P2P hosted session.
        /// The host's OWN client connects via an in-process loopback transport (not Steam P2P)
        /// to avoid the reliable-retransmit loop that occurs when connecting to your own SteamID.
        /// Returns the LocalLoopbackClientTransport to pass to NetworkManager.ConnectLoopback().
        /// Fires LobbyCreated(lobbyIdString) once the Steam lobby is ready.
        /// </summary>
        public LocalLoopbackClientTransport StartSteam(string password = "")
        {
            var steamTransport = new BBSSteamServerTransport();
            steamTransport.IsPasswordProtected = !string.IsNullOrEmpty(password);
            steamTransport.LobbyCreated += lobbyId =>
            {
                Log?.Invoke($"Steam lobby ready: {lobbyId}");
                LobbyCreated?.Invoke(lobbyId);
            };
            return StartSteam(steamTransport, password);
        }

        /// <summary>Fires with the Steam lobby ID string once StartSteam() has created the lobby.</summary>
        public event Action<string>? LobbyCreated;

        /// <summary>
        /// Start a Steam P2P hosted session with a pre-constructed transport.
        /// Remote clients use Steam P2P; the local client uses an in-process loopback.
        /// </summary>
        public LocalLoopbackClientTransport StartSteam(
            BabyStepsNetworking.Transport.Steam.SteamServerTransport steamTransport, string password = "")
        {
            // Build the loopback pair for the local (self-hosted) client.
            var (loopbackServer, loopbackClient) = LocalLoopbackTransportPair.Create();

            // Start the Steam P2P transport so remote clients can connect.
            string key = Core.SERVER_VERSION + (string.IsNullOrEmpty(password) ? "cuzzillobochfoddy" : password);
            steamTransport.StartListening(0, key);
            loopbackServer.StartListening(0, key);

            // Combine both into one composite transport so NetworkHost sees all peers.
            // Steam clients: peer IDs 0–99 999.  Loopback local client: peer IDs 100 000+.
            var composite = new CompositeServerTransport(steamTransport, loopbackServer);

            var settings = new HostSettings
            {
                Password = string.IsNullOrEmpty(password) ? "cuzzillobochfoddy" : password,
                ServerVersion = Core.SERVER_VERSION,
                VoiceChatEnabled = true,
                MaxBandwidthKbps = 512f,
            };

            _host = new NetworkHost(composite, settings);
            _host.Log += msg => Log?.Invoke(msg);
            _host.ClientConnected += OnClientConnected;
            _host.ClientDisconnected += OnClientDisconnected;

            // Give the Steam transport a real total-player-count source so bbs_players
            // reflects everyone including the host's own loopback connection.
            if (steamTransport is Networking.Steam.BBSSteamServerTransport bbsSteam)
                bbsSteam.TotalPlayerCountProvider = () => _host?.Clients.Count ?? 0;

            RegisterHandlers();
            // Don't call _host.Start() — transports are already started above.
            // We still need the uptime stopwatch running so call it through a shim.
            _host.StartWithoutListening();

            return loopbackClient;
        }

        public void Stop()
        {
            _host?.Stop();
            _host = null;
        }

        public void Tick(float deltaMs) => _host?.Tick(deltaMs);

        /// <summary>Expose the host to NetworkClient.RegisterExtension() for mod channel support.</summary>
        public NetworkHost? Host => _host;

        private void RegisterHandlers()
        {
            _host.RegisterHandler(CoreClientToServerOpcode.BoneUpdate,      HandleBoneUpdate);
            _host.RegisterHandler(CoreClientToServerOpcode.PlayerInfo,      HandlePlayerInfo);
            _host.RegisterHandler(CoreClientToServerOpcode.WorldEvent,      HandleWorldEvent);
            _host.RegisterHandler(CoreClientToServerOpcode.AccessoryAdd,    HandleAccessoryAdd);
            _host.RegisterHandler(CoreClientToServerOpcode.AccessoryRemove, HandleAccessoryRemove);
            _host.RegisterHandler(CoreClientToServerOpcode.JiminyRibbon,    HandleJiminyUpdate);
            _host.RegisterHandler(CoreClientToServerOpcode.CollisionToggle, HandleCollisionToggle);
            _host.RegisterHandler(CoreClientToServerOpcode.ChatMessage,     HandleChatMessage);
            _host.RegisterHandler(CoreClientToServerOpcode.AudioFrame,      HandleAudioFrame);
            _host.RegisterHandler(CoreClientToServerOpcode.RequestState,    HandleRequestState);
        }

        private void OnClientConnected(ConnectedClient client)
        {
            // Display name arrives with PlayerInfo — nothing to do here yet
        }

        private void OnClientDisconnected(ConnectedClient client)
        {
            PlayerLeft?.Invoke(client.DisplayName ?? "Unknown");
        }

        private void HandleBoneUpdate(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [kickoff][seq_lo][seq_hi][raw_bone...]
            if (!sender.IsInitialized || data.Length < 4) return;
            byte kickoff = data[0];
            ushort seq = BitConverter.ToUInt16(data, 1);
            byte[] raw = data[3..];
            if (!host.IsNewerSequence(sender.Uuid, seq)) return;

            sender.LastBoneKickoffPoint = kickoff;
            sender.LatestRawBoneData = raw;

            var packet = PacketBuilder.Build(CoreServerToClientOpcode.BoneUpdate, bytes =>
            {
                bytes.Add(sender.Uuid); bytes.Add(kickoff);
                bytes.AddRange(BitConverter.GetBytes(seq));
                bytes.AddRange(raw);
                bytes.AddRange(BitConverter.GetBytes(host.UptimeMs));
            });
            host.EnqueueBoneUpdate(sender.PeerId, packet);
        }

        private void HandlePlayerInfo(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [R][G][B][name_utf8...]   name may be empty
            if (data.Length < 3) return;
            bool first = !sender.IsInitialized;
            sender.Color = new RGBColor(data[0], data[1], data[2]);
            string name = Encoding.UTF8.GetString(data, 3, data.Length - 3);

            if (sender.DisplayName == null)
            {
                sender.DisplayName = name;
                PlayerJoined?.Invoke(name);
            }
            else if (sender.DisplayName != name)
                sender.DisplayName = name;

            var infoPacket = PacketBuilder.Build(CoreServerToClientOpcode.PlayerInfoUpdate, bytes =>
            {
                bytes.Add(sender.Uuid); bytes.Add(sender.Color.Value.R);
                bytes.Add(sender.Color.Value.G); bytes.Add(sender.Color.Value.B);
                bytes.AddRange(Encoding.UTF8.GetBytes(sender.DisplayName!));
            });
            sender.InfoPacket = infoPacket;

            if (first)
                host.Broadcast(PacketBuilder.Build(CoreServerToClientOpcode.PlayerJoined, new[] { sender.Uuid }),
                    PacketDelivery.ReliableOrdered, sender.PeerId);

            host.Broadcast(infoPacket, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleWorldEvent(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [seq_lo][seq_hi][event_data...]
            if (data.Length < 2) return;
            ushort seq = BitConverter.ToUInt16(data, 0);
            if (!host.IsNewerSequence(sender.Uuid, seq)) return;
            var pkt = PacketBuilder.Build(CoreServerToClientOpcode.WorldEvent, b => { b.Add(sender.Uuid); b.AddRange(data); });
            host.EnqueueHighPriority(sender.PeerId, pkt);
        }

        private void HandleAccessoryAdd(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [type][...accessory_data]
            if (data.Length < 1) return;
            byte type = data[0];
            var pkt = PacketBuilder.Build(CoreServerToClientOpcode.AccessoryAdd, b => { b.Add(sender.Uuid); b.AddRange(data); });
            sender.SavedPackets[type] = pkt;
            host.Broadcast(pkt, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleAccessoryRemove(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [type][...optional_data]
            if (data.Length < 1) return;
            byte type = data[0];
            var pkt = PacketBuilder.Build(CoreServerToClientOpcode.AccessoryRemove, b => { b.Add(sender.Uuid); b.AddRange(data); });
            sender.SavedPackets[type] = null;
            host.Broadcast(pkt, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleJiminyUpdate(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [bool_state]
            if (data.Length < 1) return;
            sender.JiminyState = data[0] != 0;
            var pkt = new[] { (byte)CoreServerToClientOpcode.JiminyRibbon, sender.Uuid, Convert.ToByte(sender.JiminyState) };
            sender.SavedPackets[0x02] = pkt;
            host.Broadcast(pkt, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleCollisionToggle(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [bool_state]
            if (data.Length < 1) return;
            sender.CollisionsEnabled = data[0] != 0;
            var pkt = new[] { (byte)CoreServerToClientOpcode.CollisionToggle, sender.Uuid, Convert.ToByte(sender.CollisionsEnabled) };
            sender.SavedPackets[0x03] = pkt;
            host.Broadcast(pkt, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleChatMessage(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [message_utf8...]
            if (data.Length < 1) return;
            var pkt = PacketBuilder.Build(CoreServerToClientOpcode.ChatMessage, b => { b.Add(sender.Uuid); b.AddRange(data); });
            host.Broadcast(pkt, PacketDelivery.ReliableOrdered, sender.PeerId);
        }

        private void HandleAudioFrame(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // data (opcode stripped): [seq_lo][seq_hi][encoded_audio...]
            var pkt = PacketBuilder.Build(CoreServerToClientOpcode.AudioFrame, b => { b.Add(sender.Uuid); b.AddRange(data); });
            host.EnqueueAudio(sender.PeerId, pkt);
        }

        private void HandleRequestState(ConnectedClient sender, byte[] data, NetworkHost host)
        {
            // Client switched saves and needs a fresh copy of everyone's visual state
            // (accessories, collision flags, jiminy ribbon).  Re-send all SavedPackets
            // for every other initialized player.
            int resent = 0;
            foreach (var existing in host.Clients.Values)
            {
                if (existing.PeerId == sender.PeerId) continue;
                if (existing.InfoPacket == null) continue; // not yet initialized
                foreach (var saved in existing.SavedPackets.Values)
                {
                    if (saved != null)
                    {
                        host.Send(sender.PeerId, saved, PacketDelivery.ReliableOrdered);
                        resent++;
                    }
                }
            }
            Core.logger.Msg($"[Host] RequestState from peer {sender.PeerId}: re-sent {resent} state packet(s)");
        }
    }
}
