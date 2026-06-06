using BabyStepsNetworking.Transport;
using BabyStepsNetworking.Transport.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BabyStepsMultiplayerClient.Networking.Steam
{
    /// <summary>
    /// Steam P2P server transport via direct P/Invoke into steam_api64.dll.
    ///
    /// Auth handshake: every new peer's first packet must be [0xFD][key_utf8].
    /// Peers with a wrong key are silently dropped.
    ///
    /// Lobby metadata:
    ///   "bbs_game"    → "1"   (filter for server browser)
    ///   "bbs_players" → N     (updated on connect / disconnect)
    ///   "bbs_name"    → name  (set at StartListening time)
    /// </summary>
    public sealed class BBSSteamServerTransport : SteamServerTransport
    {
        // Use a high channel number to avoid conflicts with the game's own P2P channels.
        private const int Channel  = 113;
        private const byte AuthByte = 0xFD;

        private readonly Dictionary<ulong, int>  _steamToId = new();
        private readonly Dictionary<int, ulong>  _idToSteam = new();
        private readonly HashSet<int>            _authed    = new();
        private readonly Dictionary<int, double> _lastHeard = new();
        private const double TimeoutMs = 15_000;
        private int  _nextId = 0;
        private bool _facepunchReady = false;
        private string _key       = string.Empty;
        private string _inviteKey = string.Empty; // random token stored in lobby; invited players use this to bypass password
        private bool   _running = false;
        private ulong  _lobbyId = 0;
        private ulong  _pendingLobbyCall = 0; // SteamAPICall_t for lobby creation
        private ulong  _localSteamId = 0;     // cached so we can drop self-loop echo packets

        public Func<int>? TotalPlayerCountProvider { get; set; }
        public bool IsPasswordProtected { get; set; }

        public override event Action<int>?         PeerConnected;
        public override event Action<int, string>? PeerDisconnected;
        public override event Action<int, byte[]>? PacketReceived;

        public override IEnumerable<int> ConnectedPeerIds => _authed;
        public override bool             IsRunning         => _running;

        public override void StartListening(int port, string connectionKey)
        {
            _key          = connectionKey;
            _running      = true;
            _localSteamId = NativeSteamAPI.GetLocalSteamId();

            try
            {
                Steamworks.SteamClient.Init(0, asyncCallbacks: false);
                _facepunchReady = true;
                Core.logger.Msg("[Steam Server] Facepunch full init OK");
            }
            catch (Exception ex) when (ex.Message.Contains("SteamAPI_SteamInput") ||
                                        ex.Message.Contains("entry point") ||
                                        ex.Message.Contains("SteamInput"))
            {
                _facepunchReady = true;
                Core.logger.Msg($"[Steam Server] Facepunch partial init (SteamInput unavailable — benign): {ex.Message}");
            }
            catch (Exception ex) when (ex.Message.Contains("already initialized"))
            {
                _facepunchReady = true;
                Core.logger.Msg("[Steam Server] Facepunch already initialized — reusing existing instance");
            }
            catch (Exception ex)
            {
                Core.logger.Warning($"[Steam Server] Facepunch init failed: {ex.Message}");
            }

            int friendsAccepted = NativeSteamAPI.PreAcceptFriends();
            Core.logger.Msg($"[Steam Server] Pre-accepted P2P sessions for {friendsAccepted} Steam friends");

            NativeSteamAPI.RegisterP2PSessionRequestCallback(sid =>
            {
                Core.logger.Msg($"[Steam Server] P2PSessionRequest from {sid} (native) — accepting");
                NativeSteamAPI.AcceptP2PSession(sid);
            });
            Core.logger.Msg("[Steam Server] P2PSessionRequest native callback registered");

            if (_facepunchReady)
            {
                try
                {
                    Steamworks.SteamNetworking.OnP2PSessionRequest += sid =>
                    {
                        Core.logger.Msg($"[Steam Server] P2PSessionRequest from {sid.Value} (Facepunch) — accepting");
                        NativeSteamAPI.AcceptP2PSession(sid.Value);
                    };
                    Core.logger.Msg("[Steam Server] OnP2PSessionRequest hooked via Facepunch");
                }
                catch (Exception ex)
                {
                    Core.logger.Msg($"[Steam Server] Facepunch OnP2PSessionRequest hook skipped: {ex.Message}");
                }

                try
                {
                    Steamworks.SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) =>
                    {
                        if (lobby.Id != _lobbyId) return;
                        ulong memberId = member.Id.Value;
                        if (memberId == _localSteamId) return; // ignore self
                        Core.logger.Msg($"[Steam Server] Lobby member joined: {memberId} — pre-accepting P2P session");
                        NativeSteamAPI.AcceptP2PSession(memberId);
                    };
                    Core.logger.Msg("[Steam Server] OnLobbyMemberJoined hooked");
                }
                catch (Exception ex)
                {
                    Core.logger.Msg($"[Steam Server] Facepunch OnLobbyMemberJoined hook skipped: {ex.Message}");
                }
            }

            _pendingLobbyCall = NativeSteamAPI.CreateLobby(2 /*Public*/, 16);
        }

        public override void Stop()
        {
            if (!_running) return;
            _running = false;

            NativeSteamAPI.UnregisterP2PSessionRequestCallback();

            // Notify all connected peers so they disconnect immediately rather than
            // waiting for the 15-second inactivity timeout.
            var goodbye = new byte[] { 0xFE };
            foreach (var (id, steamId) in _idToSteam)
                if (_authed.Contains(id))
                    NativeSteamAPI.SendP2PPacket(steamId, goodbye, 2 /* reliable */, Channel);

            if (_lobbyId != 0) NativeSteamAPI.LeaveLobby(_lobbyId);
            _lobbyId           = 0;
            _pendingLobbyCall  = 0;
            _inviteKey         = string.Empty;

            _steamToId.Clear();
            _idToSteam.Clear();
            _authed.Clear();
        }

        private long _lastPlayerCountTick = 0;

        public override void Poll()
        {
            if (!_running) return;

            long nowMs = System.Environment.TickCount64;

            // Check if the lobby creation result is back.
            if (_pendingLobbyCall != 0 && _lobbyId == 0)
            {
                if (NativeSteamAPI.IsAPICallCompleted(_pendingLobbyCall, out bool failed))
                {
                    Core.logger.Msg($"[Steam] Lobby creation call completed. failed={failed}");
                    if (!failed && NativeSteamAPI.GetAPICallResult<NativeSteamAPI.LobbyCreated_t>(
                            _pendingLobbyCall, NativeSteamAPI.k_LobbyCreated, out var result))
                    {
                        Core.logger.Msg($"[Steam] LobbyCreated_t: eResult={result.m_eResult} lobbyId={result.m_ulSteamIDLobby}");
                        if (result.m_eResult == 1 /* k_EResultOK */)
                        {
                            _lobbyId = result.m_ulSteamIDLobby;
                            string personaName = NativeSteamAPI.GetPersonaName();
                            string lobbyName   = string.IsNullOrEmpty(personaName)
                                ? $"Game {_lobbyId}"
                                : $"{personaName}'s Session";

                            // Generate a per-session invite key.  Players who accept a Steam invite
                            // read this from lobby metadata and use it as an alternate auth token,
                            // letting them bypass a password they don't know.
                            _inviteKey = Guid.NewGuid().ToString("N")[..8];

                            bool j  = NativeSteamAPI.SetLobbyJoinable(_lobbyId, true);
                            bool d1 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_game",       "1");
                            bool d2 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_players",    "1");
                            bool d3 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_name",       lobbyName);
                            bool d4 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_host",       _localSteamId.ToString());
                            bool d5 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_locked",     IsPasswordProtected ? "1" : "0");
                            bool d6 = NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_invite_key", _inviteKey);
                            Core.logger.Msg($"[Steam] Lobby ready: '{lobbyName}' locked={IsPasswordProtected}");

                            string check = NativeSteamAPI.GetLobbyData(_lobbyId, "bbs_game");
                            Core.logger.Msg($"[Steam] Verify GetLobbyData(bbs_game)='{check}'");
                            OnLobbyCreated(_lobbyId.ToString());
                        }
                    }
                    else if (!failed)
                        Core.logger.Msg($"[Steam] GetAPICallResult returned false for LobbyCreated");
                    _pendingLobbyCall = 0;
                }
            }

            if (_lobbyId != 0 && nowMs - _lastPlayerCountTick >= 2000)
            {
                _lastPlayerCountTick = nowMs;
                UpdateLobbyPlayerCount();
            }

            if (_facepunchReady)
            {
                try { Steamworks.SteamClient.RunCallbacks(); } catch { }
            }

            DrainChannel(Channel);
            for (int ch = 0; ch <= 10; ch++)
                if (ch != Channel) DrainChannel(ch);

            // Timeout check cause Steam P2P has no reliable disconnect notification.
            double now = Environment.TickCount64;
            foreach (var (pid, last) in _lastHeard.ToArray())
            {
                if (_authed.Contains(pid) && (now - last) >= TimeoutMs)
                {
                    Core.logger.Msg($"[Steam Server] Peer {pid} timed out, disconnecting");
                    Kick(pid, "Timeout");
                    _lastHeard.Remove(pid);
                }
            }
        }

        private void DrainChannel(int ch)
        {
            while (NativeSteamAPI.IsP2PPacketAvailable(out uint size, ch))
            {
                var buf = new byte[size];
                if (NativeSteamAPI.ReadP2PPacket(buf, out _, out ulong from, ch))
                {
                    if (ch != Channel)
                        Core.logger.Msg($"[Steam Server] Packet on alternate channel {ch} from {from} len={size} — expected channel {Channel}");
                    HandleIncoming(from, buf);
                }
            }
        }

        public override void Send(int peerId, byte[] payload, PacketDelivery delivery)
        {
            if (!_idToSteam.TryGetValue(peerId, out ulong sid)) return;
            NativeSteamAPI.SendP2PPacket(sid, payload, ToSendType(delivery), Channel);
        }

        public override void Broadcast(byte[] payload, PacketDelivery delivery, int excludePeerId = -1)
        {
            int st = ToSendType(delivery);
            foreach (var (pid, sid) in _idToSteam)
            {
                if (pid == excludePeerId || !_authed.Contains(pid)) continue;
                NativeSteamAPI.SendP2PPacket(sid, payload, st, Channel);
            }
        }

        public override void Kick(int peerId, string reason = "")
        {
            if (!_idToSteam.TryGetValue(peerId, out ulong sid)) return;
            _idToSteam.Remove(peerId);
            _steamToId.Remove(sid);
            _authed.Remove(peerId);
            _lastHeard.Remove(peerId);
            PeerDisconnected?.Invoke(peerId, reason);
            UpdateLobbyPlayerCount();
        }

        private void HandleIncoming(ulong from, byte[] data)
        {
            if (from == _localSteamId) return;

            // Always accept the session so Steam's relay infrastructure doesn't drop us.
            NativeSteamAPI.AcceptP2PSession(from);

            if (!_steamToId.TryGetValue(from, out int id))
            {
                id = _nextId++;
                _steamToId[from] = id;
                _idToSteam[id]   = from;
                Core.logger.Msg($"[Steam Server] New peer SteamID={from} assigned localId={id}");
            }

            if (!_authed.Contains(id))
            {
                // First packet must be AUTH: [0xFD][key_utf8]
                if (data.Length < 2 || data[0] != AuthByte) return;
                string key = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                bool inviteAuth = !string.IsNullOrEmpty(_inviteKey) && key == Core.SERVER_VERSION + _inviteKey;
                Core.logger.Msg($"[Steam Server] Auth from {from}: keyMatchesPassword={key == _key} inviteAuth={inviteAuth} keyLen={key.Length} expectedLen={_key.Length}");
                if (key != _key && !inviteAuth)
                {
                    Core.logger.Msg($"[Steam Server] Auth FAILED (wrong password) from SteamID={from}");
                    NativeSteamAPI.SendP2PPacket(from, new byte[] { 0xFC }, 2 /* reliable */, Channel);
                    _steamToId.Remove(from);
                    _idToSteam.Remove(id);
                    return;
                }
                Core.logger.Msg($"[Steam Server] Auth OK for localId={id} (invite={inviteAuth}), firing PeerConnected");
                _authed.Add(id);
                _lastHeard[id] = Environment.TickCount64;
                // Clear the lobby invite marker for this player so they don't auto-reconnect.
                if (_lobbyId != 0)
                    NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_invite_" + from.ToString(), "0");
                PeerConnected?.Invoke(id);
                UpdateLobbyPlayerCount();
                return;
            }

            _lastHeard[id] = Environment.TickCount64;

            if (data.Length == 1 && data[0] == 0xFE) // graceful disconnect
            {
                Core.logger.Msg($"[Steam Server] Peer {id} sent graceful disconnect");
                Kick(id, "Client disconnected");
                _lastHeard.Remove(id);
                return;
            }

            if (data.Length >= 2 && data[0] == AuthByte)
            {
                Core.logger.Msg($"[Steam Server] Peer {id} reconnecting — resetting auth state");
                _authed.Remove(id);
                PeerDisconnected?.Invoke(id, "Reconnect");
                string rKey = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                bool rInviteAuth = !string.IsNullOrEmpty(_inviteKey) && rKey == Core.SERVER_VERSION + _inviteKey;
                if (rKey != _key && !rInviteAuth) return;
                _authed.Add(id);
                _lastHeard[id] = Environment.TickCount64;
                PeerConnected?.Invoke(id);
                UpdateLobbyPlayerCount();
                return;
            }

            PacketReceived?.Invoke(id, data);
        }

        private void UpdateLobbyPlayerCount()
        {
            if (_lobbyId == 0) return;
            // Use the provider (includes loopback host) if available; else count Steam peers.
            int count = TotalPlayerCountProvider != null
                ? TotalPlayerCountProvider()
                : _authed.Count;
            // The host is always present when a lobby is live, so clamp to at least 1.
            // This also handles the startup race where the loopback client hasn't been
            // added to NetworkHost.Clients yet when the first update fires.
            count = Math.Max(1, count);
            NativeSteamAPI.SetLobbyData(_lobbyId, "bbs_players", count.ToString());
        }

        private static int ToSendType(PacketDelivery d) =>
            d == PacketDelivery.Unreliable ? 0 /* k_EP2PSendUnreliable */ : 2 /* k_EP2PSendReliable */;
    }
}
