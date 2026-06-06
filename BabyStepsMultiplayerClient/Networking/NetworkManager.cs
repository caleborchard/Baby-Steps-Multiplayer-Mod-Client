using BabyStepsMultiplayerClient.Audio;
using BabyStepsMultiplayerClient.Player;
using BabyStepsMultiplayerClient.Localization;
using BabyStepsNetworking.Client;
using BabyStepsNetworking.Packets;
using BabyStepsNetworking.Transport;
using BabyStepsNetworking.Transport.LiteNetLib;
using Il2Cpp;
using MelonLoader;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class NetworkManager
    {
        private NetworkClient _networkClient;
        public bool IsConnected => _networkClient?.IsConnected ?? false;
        public bool HasReceivedUUID => _uuidReceived;

        private PlayerPacketQueue<byte, byte[]> pendingPlayerJoins = new();
        private PlayerPacketQueue<byte, byte[]> pendingPlayerUpdates = new();

        private ConcurrentDictionary<byte, ushort> lastSeenBoneSequences = new();
        private ConcurrentDictionary<byte, ushort> lastSeenAudioFrameSequences = new();
        private ushort localBoneSequenceNumber = 0;

        private HashSet<byte> _playersWithFirstBoneUpdate = new();
        private Dictionary<byte, bool> _pendingCollisionState = new();
        private ushort localAudioFrameSequenceNumber = 0;
        public bool isRunningNetParticle = false;

        public const int maxPacketSize = 1024;

        public ConcurrentDictionary<byte, RemotePlayer> players = new();
        private int numClones = 1;

        private byte uuid;

        private const string EasterEggMarker = "⁣";
        private IPAddress connectedServerAddress;
        public bool IsEasterEggActive { get; private set; }

        private bool IsNewer(ushort current, ushort previous)
            => (ushort)(current - previous) < 32768;

        public NetworkManager()
        {
            Physics.IgnoreLayerCollision(6, 6, true);
            Physics.IgnoreLayerCollision(6, 11, false);
            Physics.IgnoreLayerCollision(6, 19, true);
            Physics.IgnoreLayerCollision(23, 23, true);
            Physics.IgnoreLayerCollision(23, 11, false);
            Physics.IgnoreLayerCollision(23, 19, true);
        }

        private string _disconnectReason;

        private void OnDisconnected(string reason)
        {
            // Only capture non-default reasons so generic disconnects still show "Disconnected from server".
            if (!string.IsNullOrEmpty(reason)
                && reason != "Disconnected"
                && reason != "Server disconnected"
                && reason != "Client disconnected")
                _disconnectReason = reason;
            Disconnect();
        }

        public void ConnectLoopback(BabyStepsNetworking.Transport.LocalLoopback.LocalLoopbackClientTransport loopback)
        {
            Disconnect();
            _connectedViaLoopback = true;
            Core.uiManager.notificationsUI.AddMessage("Connecting to local session…");

            _networkClient = new NetworkClient(loopback);
            _networkClient.Connected += OnConnected;
            _networkClient.Disconnected += OnDisconnected;

            RegisterHandlers();
            loopback.Connect("loopback", 0, string.Empty); // key ignored by loopback
        }

        public void ConnectSteam(string hostSteamId64, string password)
        {
            // You can't join your own hosted stuff
            if (ulong.TryParse(hostSteamId64, out ulong hostId) &&
                hostId == Networking.Steam.NativeSteamAPI.GetLocalSteamId())
            {
                Core.uiManager.notificationsUI.AddMessage(
                    IsConnected
                        ? "You are already connected to this session."
                        : "Cannot join your own session from the same account.");
                return;
            }

            Disconnect();
            Core.uiManager.notificationsUI.AddMessage("Connecting via Steam P2P…");

            var transport = new Steam.BBSSteamClientTransport();
            _networkClient = new NetworkClient(transport);
            _networkClient.Connected += OnConnected;
            _networkClient.Disconnected += OnDisconnected;

            RegisterHandlers();

            string key = Core.SERVER_VERSION + (string.IsNullOrEmpty(password) ? "cuzzillobochfoddy" : password);
            _networkClient.Connect(hostSteamId64, 0, key);
        }

        public void Connect(string serverIP, int serverPort, string password)
        {
            Disconnect();

            Core.uiManager.notificationsUI.AddMessage("Connecting to server...");

            var transport = new LiteNetLibClientTransport();
            _networkClient = new NetworkClient(transport);
            _networkClient.Connected += OnConnected;
            _networkClient.Disconnected += OnDisconnected;

            RegisterHandlers();

            string key = Core.SERVER_VERSION + (string.IsNullOrEmpty(password) ? "cuzzillobochfoddy" : password);
            _networkClient.Connect(serverIP, serverPort, key);
        }

        private void RegisterHandlers()
        {
            _networkClient.RegisterHandler(CoreServerToClientOpcode.AssignUUID,       HandleAssignUUID);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.PlayerJoined,     HandlePlayerJoined);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.PlayerLeft,       HandlePlayerLeft);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.PlayerInfoUpdate, HandlePlayerInfoUpdate);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.BoneUpdate,       HandleBoneUpdate);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.WorldEvent,       HandleWorldEvent);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.AccessoryAdd,     HandleAccessoryAdd);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.AccessoryRemove,  HandleAccessoryRemove);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.JiminyRibbon,     HandleJiminyRibbon);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.CollisionToggle,  HandleCollisionToggle);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.ChatMessage,      HandleChatMessage);
            _networkClient.RegisterHandler(CoreServerToClientOpcode.AudioFrame,       HandleAudioFrame);
        }

        private void OnConnected()
        {

        }

        public void Disconnect()
        {
            if (_networkClient == null) return;

            _networkClient.Disconnect();
            _networkClient = null;
            connectedServerAddress = null;
            IsEasterEggActive = false;
            localBoneSequenceNumber = 0;
            localAudioFrameSequenceNumber = 0;
            uuid = 0;
            numClones = 0;
            _uuidReceived = false;
            _connectedViaLoopback = false;

            pendingPlayerJoins.Clear();
            pendingPlayerUpdates.Clear();
            lastSeenBoneSequences.Clear();
            lastSeenAudioFrameSequences.Clear();
            _playersWithFirstBoneUpdate.Clear();
            _pendingCollisionState.Clear();

            foreach (var player in players) player.Value.Dispose();
            players.Clear();

            if (LocalPlayer.Instance != null) LocalPlayer.Instance.Dispose();
            LocalPlayer.Instance = null;

            WorldObjectSyncManager.ReleaseCachedWheels();
            WorldObjectSyncManager.ClearCachedWheels();

            string dcMsg = _disconnectReason ?? "Disconnected from server";
            _disconnectReason = null;
            Core.uiManager.notificationsUI.AddMessage(dcMsg);
            Core.OnConnectionStateChanged?.Invoke();
        }

        public void Update()
        {
            if (_networkClient == null) return;

            if (Core.HasLoadedGame())
            {
                if (LocalPlayer.Instance == null)
                {
                    LocalPlayer.Instance = new();
                    LocalPlayer.Instance.Initialize();
                    return;
                }

                pendingPlayerJoins.Process(pktData =>
                {
                    if (pktData.Length >= 1) SpawnRemotePlayer(pktData[0]);
                });
            }

            _networkClient.Tick();
            if (_networkClient == null) return;

            WorldObjectSyncManager.Update();
        }

        public void LateUpdate()
        {
            if (_networkClient == null || players == null) return;
            foreach (var player in players.Values)
                if (player != null) player.LateUpdate();
        }

        // Opcode handlers
        // In the new architecture, the opcode byte is stripped BEFORE the handler is called, so data[0] is always the first non-opcode byte.

        private bool _uuidReceived = false;
        private void HandleAssignUUID(byte[] data, NetworkClient _)
        {
            // data: [uuid][uptime_8bytes]
            if (data.Length < 9) return;
            uuid = data[0];
            long uptimeMs = BitConverter.ToInt64(data, 1);
            WorldObjectSyncManager.OnServerTimeSample(uptimeMs);

            if (_uuidReceived) return;
            _uuidReceived = true;

            SendPlayerInformation();

            var lang = LanguageManager.GetCurrentLanguage();
            Core.uiManager.notificationsUI.AddMessage(lang.ConnectedToServer);
            Core.uiManager.notificationsUI.AddMessage("Press the 'T' key to send a message");
            Core.OnConnectionStateChanged?.Invoke();
        }

        private void HandlePlayerJoined(byte[] data, NetworkClient _)
        {
            if (data.Length < 1) return;
            byte newUUID = data[0];
            if (players.ContainsKey(newUUID)) return;

            if (!Core.HasLoadedGame() || LocalPlayer.Instance == null)
            {
                pendingPlayerJoins.Enqueue(newUUID, new[] { newUUID });
                return;
            }

            SpawnRemotePlayer(newUUID);
        }

        private void SpawnRemotePlayer(byte newUUID)
        {
            RemotePlayer nate = null;
            if (!RemotePlayer.GlobalPool.TryTake(out nate))
            {
                nate = new();
                nate.Initialize(numClones++);
            }
            nate.SetActive(true);
            nate.SetOpacity(1f);
            nate.RemoveHat();
            nate.ResetBonesToBind();
            _playersWithFirstBoneUpdate.Remove(newUUID);
            _pendingCollisionState.Remove(newUUID);
            // Disable colliders to stop flinging
            ApplyCollisionToggle(nate, false);
            nate.InitializeAudioSource();
            players[newUUID] = nate;
            // Drain updates queued before this player was spawned
            pendingPlayerUpdates.Process(newUUID, DispatchPlayerUpdate);
        }

        private void HandlePlayerLeft(byte[] data, NetworkClient _)
        {
            if (data.Length < 1) return;
            byte disconnectedUUID = data[0];
            if (players.TryGetValue(disconnectedUUID, out var player))
            {
                var lang = LanguageManager.GetCurrentLanguage();
                players.TryRemove(disconnectedUUID, out RemotePlayer _rp);
                lastSeenBoneSequences.TryRemove(disconnectedUUID, out ushort _s1);
                lastSeenAudioFrameSequences.TryRemove(disconnectedUUID, out ushort _s2);
                if (player.firstAppearanceApplication)
                    Core.uiManager.notificationsUI.AddMessage(string.Format(lang.HasDisconnected, player.displayName));
                player.Dispose();
            }
            pendingPlayerJoins.Clear(disconnectedUUID);
            pendingPlayerUpdates.Clear(disconnectedUUID);
            _playersWithFirstBoneUpdate.Remove(disconnectedUUID);
            _pendingCollisionState.Remove(disconnectedUUID);
        }

        private void HandlePlayerInfoUpdate(byte[] data, NetworkClient _)
        {
            if (data.Length < 5) return;
            byte playerUUID = data[0];
            if (players.TryGetValue(playerUUID, out var target))
                ApplyAppearanceUpdate(target, data);
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.PlayerInfoUpdate, data));
        }

        private void HandleBoneUpdate(byte[] data, NetworkClient _)
        {
            if (data.Length < 5) return;
            byte boneUUID = data[0];

            if (players.TryGetValue(boneUUID, out var bonePlayer))
            {
                byte kickoff = data[1];
                ushort seq = BitConverter.ToUInt16(data, 2);
                byte[] rawBoneAndUptime = data.Skip(4).ToArray();

                long uptimeMs = BitConverter.ToInt64(rawBoneAndUptime, rawBoneAndUptime.Length - 8);
                WorldObjectSyncManager.OnServerTimeSample(uptimeMs);
                byte[] rawBone = rawBoneAndUptime.Take(rawBoneAndUptime.Length - 8).ToArray();

                if (!lastSeenBoneSequences.TryGetValue(boneUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                {
                    lastSeenBoneSequences[boneUUID] = seq;
                    var bones = TransformNet.Deserialize(rawBone);
                    if (bones != null && bones.Length > 0)
                        bonePlayer.UpdateBones(bones, kickoff);
                }

                if (!_playersWithFirstBoneUpdate.Contains(boneUUID))
                {
                    _playersWithFirstBoneUpdate.Add(boneUUID);
                    if (_pendingCollisionState.TryGetValue(boneUUID, out bool deferred))
                    {
                        _pendingCollisionState.Remove(boneUUID);
                        ApplyCollisionToggle(bonePlayer, deferred);
                        Core.logger.Msg($"[Net] Applied deferred collision={deferred} for UUID {boneUUID} after first bone update");
                    }
                }
            }
            else
                pendingPlayerUpdates.Enqueue(boneUUID, WithOpcode(CoreServerToClientOpcode.BoneUpdate, data));
        }

        private void HandleWorldEvent(byte[] data, NetworkClient _)
        {
            if (data.Length < 3) return;
            byte eventUUID = data[0];
            ushort seq = BitConverter.ToUInt16(data, 1);
            byte[] rawEvent = data.Skip(3).ToArray();

            if (!lastSeenBoneSequences.TryGetValue(eventUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
            {
                if (rawEvent.Length < 2) return;
                byte eID = rawEvent[0];
                if (eID == 0x00 && LocalPlayer.Instance?.particleParty != null)
                {
                    var fd = FootDataHelpers.DeserializeFootData(rawEvent.Skip(2).ToArray(), players.ContainsKey(eventUUID) ? players[eventUUID] : null);
                    if (fd != null)
                    {
                        isRunningNetParticle = true;
                        if (rawEvent[1] == 0x00) LocalPlayer.Instance.particleParty.OnPlant(fd);
                        else if (rawEvent[1] == 0x01) LocalPlayer.Instance.particleParty.OnSlip(fd);
                    }
                }
            }
        }

        private void HandleAccessoryAdd(byte[] data, NetworkClient _)
        {
            if (data.Length < 2) return;
            byte playerUUID = data[0];
            if (players.TryGetValue(playerUUID, out var player))
            {
                ApplyAccessoryDon(player, data);
                ApplyCollisionToggle(player, player.netCollisionsEnabled);
            }
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.AccessoryAdd, data));
        }

        private void HandleAccessoryRemove(byte[] data, NetworkClient _)
        {
            if (data.Length < 2) return;
            byte playerUUID = data[0];
            byte accessoryType = data[1];
            if (players.TryGetValue(playerUUID, out var player))
            {
                switch (accessoryType)
                {
                    case 0x00: player.RemoveHat(); break;
                    case 0x01: if (data.Length >= 3) player.DropItem(data[2]); break;
                    default: Core.logger.Msg($"Unknown Accessory type: {accessoryType}"); break;
                }
            }
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.AccessoryRemove, data));
        }

        private void HandleJiminyRibbon(byte[] data, NetworkClient _)
        {
            if (data.Length < 2) return;
            byte playerUUID = data[0];
            bool jiminy = data[1] != 0;
            if (players.TryGetValue(playerUUID, out var player))
                ApplyJiminyRibbonStateChange(player, jiminy);
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.JiminyRibbon, data));
        }

        private void HandleCollisionToggle(byte[] data, NetworkClient _)
        {
            if (data.Length < 2) return;
            byte playerUUID = data[0];
            bool collisions = data[1] != 0;
            if (players.TryGetValue(playerUUID, out var player))
            {
                if (collisions && !_playersWithFirstBoneUpdate.Contains(playerUUID))
                {
                    _pendingCollisionState[playerUUID] = true;
                    Core.logger.Msg($"[Net] Deferring collision enable for UUID {playerUUID} (no bone update yet)");
                }
                else
                {
                    _pendingCollisionState.Remove(playerUUID);
                    ApplyCollisionToggle(player, collisions);
                    if (_playersWithFirstBoneUpdate.Contains(playerUUID))
                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has {(collisions ? "enabled" : "disabled")} collisions");
                }
            }
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.CollisionToggle, data));
        }

        private void HandleChatMessage(byte[] data, NetworkClient _)
        {
            if (data.Length < 2) return;
            byte playerUUID = data[0];
            string message = Encoding.UTF8.GetString(data, 1, data.Length - 1);
            if (players.TryGetValue(playerUUID, out var player))
            {
                int words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                float dur = Mathf.Max(3f, 3f + words - 5) * 0.5f;
                Core.uiManager.notificationsUI.AddMessage($"{player.displayName}: {message}", dur);
            }
            else
                pendingPlayerUpdates.Enqueue(playerUUID, WithOpcode(CoreServerToClientOpcode.ChatMessage, data));
        }

        private void HandleAudioFrame(byte[] data, NetworkClient _)
        {
            if (ModSettings.audio.Deafened.Value || data.Length < 4) return;
            byte playerUUID = data[0];
            if (players.TryGetValue(playerUUID, out var player))
            {
                ushort seq = BitConverter.ToUInt16(data, 1);
                if (!lastSeenAudioFrameSequences.TryGetValue(playerUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                {
                    lastSeenAudioFrameSequences[playerUUID] = seq;
                    player.audioSource.QueueOpusPacket(data[3..]);
                }
            }
        }

        private void DispatchPlayerUpdate(byte[] pktData)
        {
            if (pktData.Length < 1) return;
            byte opcode = pktData[0];
            byte[] data = pktData.Length > 1 ? pktData[1..] : Array.Empty<byte>();
            switch (opcode)
            {
                case (byte)CoreServerToClientOpcode.PlayerInfoUpdate:  HandlePlayerInfoUpdate(data, null); break;
                case (byte)CoreServerToClientOpcode.BoneUpdate:        HandleBoneUpdate(data, null); break;
                case (byte)CoreServerToClientOpcode.AccessoryAdd:      HandleAccessoryAdd(data, null); break;
                case (byte)CoreServerToClientOpcode.AccessoryRemove:   HandleAccessoryRemove(data, null); break;
                case (byte)CoreServerToClientOpcode.JiminyRibbon:      HandleJiminyRibbon(data, null); break;
                case (byte)CoreServerToClientOpcode.CollisionToggle:   HandleCollisionToggle(data, null); break;
                case (byte)CoreServerToClientOpcode.ChatMessage:       HandleChatMessage(data, null); break;
            }
        }

        private static byte[] WithOpcode(CoreServerToClientOpcode opcode, byte[] data)
        {
            var result = new byte[1 + data.Length];
            result[0] = (byte)opcode;
            Buffer.BlockCopy(data, 0, result, 1, data.Length);
            return result;
        }

        private void ApplyJiminyRibbonStateChange(RemotePlayer player, bool jiminyState)
        {
            if (player.jiminyRibbon != null) player.jiminyRibbon.active = jiminyState;
        }

        internal void ApplyCollisionToggle(RemotePlayer player, bool collisionsEnabled)
        {
            if (collisionsEnabled)
            {
                player.netCollisionsEnabled = true;
                if (ModSettings.player.Collisions.Value) player.EnableCollision();
                else player.DisableCollision();
            }
            else
            {
                player.netCollisionsEnabled = false;
                player.DisableCollision();
            }
        }

        private void ApplyAppearanceUpdate(RemotePlayer player, byte[] data)
        {
            var lang = LanguageManager.GetCurrentLanguage();
            Color color = new Color(data[1] / 255f, data[2] / 255f, data[3] / 255f);
            bool colorDifferent = player.BaseSuitColor != color;
            player.UpdateBaseSuitColor(color);

            bool easter = data.Length > 4 && data[4] != 0;
            int len = data.Length > 5 ? data.Length - 5 : 0;
            string name = len > 0 ? Encoding.UTF8.GetString(data, 5, len) : string.Empty;
            player.SetRainbowSuitActive(easter);

            if (!player.firstAppearanceApplication)
            {
                if (!string.IsNullOrEmpty(name))
                    Core.uiManager.notificationsUI.AddMessage(string.Format(lang.HasConnected, name));
                player.firstAppearanceApplication = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(name) && player.displayName != name)
                    Core.uiManager.notificationsUI.AddMessage(string.Format(lang.HasChangedNicknameTo, player.displayName, name));
                if (colorDifferent)
                    Core.uiManager.notificationsUI.AddMessage(string.Format(lang.HasUpdatedTheirColor, player.displayName));
            }

            player.SetDisplayName(name);
        }

        // data: [uuid][type][...accessory_data]
        private void ApplyAccessoryDon(RemotePlayer player, byte[] data)
        {
            if (data.Length < 2) return;
            byte accessoryType = data[1];
            int offset = 2;
            // Minimum bytes needed after offset: 7 floats (28 bytes) for pos+rot, +1 for name
            const int FloatBytes = 7 * 4; // 28
            switch (accessoryType)
            {
                case 0x00: // Hat — needs uuid(1)+type(1)+pos(12)+rot(16)+name(1+) = 31+
                    {
                        if (data.Length < offset + FloatBytes) return;
                        float pX = BitConverter.ToSingle(data, offset); offset += 4;
                        float pY = BitConverter.ToSingle(data, offset); offset += 4;
                        float pZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rX = BitConverter.ToSingle(data, offset); offset += 4;
                        float rY = BitConverter.ToSingle(data, offset); offset += 4;
                        float rZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rW = BitConverter.ToSingle(data, offset); offset += 4;
                        string hatName = Encoding.UTF8.GetString(data, offset, data.Length - offset);
                        player.WearHat(hatName, new Vector3(0f, 0.21f, -0.02f), new Quaternion(-0.21517f, 0.0000689f, -0.0003178f, 0.97627f));
                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has donned the {hatName}");
                        break;
                    }
                case 0x01: // Held item — needs uuid(1)+type(1)+hand(1)+pos(12)+rot(16)+name(1+) = 32+
                    {
                        if (data.Length < offset + 1 + FloatBytes) return;
                        int hand = data[offset]; offset++;
                        float pX = BitConverter.ToSingle(data, offset); offset += 4;
                        float pY = BitConverter.ToSingle(data, offset); offset += 4;
                        float pZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rX = BitConverter.ToSingle(data, offset); offset += 4;
                        float rY = BitConverter.ToSingle(data, offset); offset += 4;
                        float rZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rW = BitConverter.ToSingle(data, offset); offset += 4;
                        string itemName = Encoding.UTF8.GetString(data, offset, data.Length - offset);
                        player.HoldItem(itemName, hand, new Vector3(pX, pY, pZ), new Quaternion(rX, rY, rZ, rW));
                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has picked up the {itemName}");
                        break;
                    }
                default:
                    Core.logger.Msg($"Unknown Accessory type: {accessoryType}");
                    break;
            }
        }

        public static bool HasEasterEggMarker(string name)
            => !string.IsNullOrEmpty(name) && name.Contains(EasterEggMarker, StringComparison.Ordinal);

        public static string StripEasterEggMarker(string name)
            => string.IsNullOrEmpty(name) ? name : name.Replace(EasterEggMarker, string.Empty, StringComparison.Ordinal);

        private bool _connectedViaLoopback = false;

        private void UpdateEasterEggState()
        {
            bool active = false;
            string nickname = ModSettings.player.Nickname.Value;
            if (!string.IsNullOrWhiteSpace(nickname) && string.Equals(nickname, "Caleb", StringComparison.Ordinal))
            {
                if (_connectedViaLoopback)
                {
                    active = true;
                }
                else
                {
                    var resolved = ResolveServerAddress(ModSettings.connection.Address.Value);
                    if (resolved != null) connectedServerAddress = resolved;
                    if (connectedServerAddress != null)
                        active = IsLocalAddress(connectedServerAddress);
                }
            }
            IsEasterEggActive = active;
        }

        // ─── Send methods ──────────────────────────────────────────────────────────

        public void SendBones(TransformNet[] bones, int kickoffPoint)
        {
            var payload = new List<byte>(maxPacketSize)
            {
                (byte)CoreClientToServerOpcode.BoneUpdate,
                (byte)kickoffPoint
            };
            payload.AddRange(BitConverter.GetBytes(localBoneSequenceNumber++));
            payload.AddRange(TransformNet.Serialize(bones));
            _networkClient?.Send(payload.ToArray(), PacketDelivery.Unreliable);
        }

        public void SendPlayerInformation()
        {
            UpdateEasterEggState();
            string name = StripEasterEggMarker(ModSettings.player.Nickname.Value ?? string.Empty);
            var payload = new List<byte>
            {
                (byte)CoreClientToServerOpcode.PlayerInfo,
                (byte)(ModSettings.player.SuitColor.Value.r * 255),
                (byte)(ModSettings.player.SuitColor.Value.g * 255),
                (byte)(ModSettings.player.SuitColor.Value.b * 255),
                (byte)(IsEasterEggActive ? 1 : 0)
            };
            payload.AddRange(Encoding.UTF8.GetBytes(name));
            _networkClient?.Send(payload.ToArray(), PacketDelivery.ReliableOrdered);
        }

        public void SendAudioFrame(byte[] encodedData)
        {
            var payload = new List<byte> { (byte)CoreClientToServerOpcode.AudioFrame };
            payload.AddRange(BitConverter.GetBytes(localAudioFrameSequenceNumber++));
            payload.AddRange(encodedData);
            _networkClient?.Send(payload.ToArray(), PacketDelivery.Unreliable);
        }

        public void SendChatMessage(string message)
        {
            var payload = new List<byte> { (byte)CoreClientToServerOpcode.ChatMessage };
            payload.AddRange(Encoding.UTF8.GetBytes(message));
            _networkClient?.Send(payload.ToArray(), PacketDelivery.ReliableOrdered);
        }

        public void SendCollisionToggle(bool state)
            => _networkClient?.Send(new[] { (byte)CoreClientToServerOpcode.CollisionToggle, Convert.ToByte(state) }, PacketDelivery.ReliableOrdered);

        public void RequestStateResync()
        {
            if (!IsConnected) return;

            SendDoffHat();
            SendDropGrabable(0);
            SendDropGrabable(1);

            foreach (var player in players.Values)
            {
                player.RemoveHat();
                player.DropItem(0);
                player.DropItem(1);
            }

            _networkClient?.Send(
                new[] { (byte)CoreClientToServerOpcode.RequestState },
                PacketDelivery.ReliableOrdered);

            if (LocalPlayer.Instance != null)
            {
                try { LocalPlayer.Instance.Dispose(); } catch { }
                LocalPlayer.Instance = null;
            }

            Core.logger.Msg("[Net] State resync requested after save load");
        }

        public void SendJiminyRibbonState(bool state)
            => _networkClient?.Send(new[] { (byte)CoreClientToServerOpcode.JiminyRibbon, Convert.ToByte(state) }, PacketDelivery.ReliableOrdered);

        public void SendHoldGrabable(Grabable grabable, int handIndex)
        {
            if (LocalPlayer.Instance == null) return;
            Transform handBone = handIndex == 0 ? LocalPlayer.Instance.handBones.Item1 : LocalPlayer.Instance.handBones.Item2;
            if (handBone == null) return;
            Vector3 localPos = handBone.InverseTransformPoint(grabable.transform.position);
            Quaternion localRot = Quaternion.Inverse(handBone.rotation) * grabable.transform.rotation;
            string name = grabable.name;
            if (name.EndsWith(Core.cloneText)) name = name[..^Core.cloneText.Length];
            SendAddAccessory(0x01, name, localPos, localRot, handIndex);
        }

        public void SendDropGrabable(int handIndex) => SendRemoveAccessory(0x01, handIndex);

        public void SendDonHat(Hat hat)
        {
            if (LocalPlayer.Instance?.headBone == null) return;
            Vector3 localPos = LocalPlayer.Instance.headBone.InverseTransformPoint(hat.transform.position);
            Quaternion localRot = Quaternion.Inverse(LocalPlayer.Instance.headBone.rotation) * hat.transform.rotation;
            string name = hat.name;
            if (name.EndsWith(Core.cloneText)) name = name[..^Core.cloneText.Length];
            SendAddAccessory(0x00, name, localPos, localRot);
        }

        public void SendDoffHat() => SendRemoveAccessory(0x00);

        private void SendAddAccessory(byte itemType, string itemName, Vector3 pos, Quaternion rot, int handIndex = 0)
        {
            var payload = new List<byte> { (byte)CoreClientToServerOpcode.AccessoryAdd, itemType };
            if (itemType == 0x01) payload.Add((byte)handIndex);
            payload.AddRange(BitConverter.GetBytes(pos.x));
            payload.AddRange(BitConverter.GetBytes(pos.y));
            payload.AddRange(BitConverter.GetBytes(pos.z));
            payload.AddRange(BitConverter.GetBytes(rot.x));
            payload.AddRange(BitConverter.GetBytes(rot.y));
            payload.AddRange(BitConverter.GetBytes(rot.z));
            payload.AddRange(BitConverter.GetBytes(rot.w));
            payload.AddRange(Encoding.UTF8.GetBytes(itemName));
            _networkClient?.Send(payload.ToArray(), PacketDelivery.ReliableOrdered);
        }

        private void SendRemoveAccessory(byte itemType, int handIndex = 0)
        {
            var payload = new List<byte> { (byte)CoreClientToServerOpcode.AccessoryRemove, itemType };
            if (itemType == 0x01) payload.Add((byte)handIndex);
            _networkClient?.Send(payload.ToArray(), PacketDelivery.ReliableOrdered);
        }

        private static IPAddress ResolveServerAddress(string addr)
        {
            if (string.IsNullOrWhiteSpace(addr)) return null;
            if (string.Equals(addr, "localhost", StringComparison.OrdinalIgnoreCase)) return IPAddress.Loopback;
            if (IPAddress.TryParse(addr, out var ip)) return ip;
            try { return Dns.GetHostAddresses(addr).FirstOrDefault(); } catch { return null; }
        }

        private static bool IsLocalAddress(IPAddress address)
        {
            if (address == null) return false;
            if (IPAddress.IsLoopback(address)) return true;
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address == null) continue;
                        if (ua.Address.Equals(address)) return true;
                        if (ua.IPv4Mask != null
                            && ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                            && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                            && IsInSameSubnet(address, ua.Address, ua.IPv4Mask))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool IsInSameSubnet(IPAddress a, IPAddress subnet, IPAddress mask)
        {
            var ab = a.GetAddressBytes(); var sb = subnet.GetAddressBytes(); var mb = mask.GetAddressBytes();
            if (ab.Length != sb.Length || ab.Length != mb.Length) return false;
            for (int i = 0; i < ab.Length; i++)
                if ((ab[i] & mb[i]) != (sb[i] & mb[i])) return false;
            return true;
        }
    }
}
