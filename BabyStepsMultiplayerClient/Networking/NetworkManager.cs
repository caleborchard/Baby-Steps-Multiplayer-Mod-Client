using BabyStepsMultiplayerClient.Debug;
using BabyStepsMultiplayerClient.Networking;
using Il2Cpp;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLoader;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

namespace BabyStepsMultiplayerClient.UI
{
    public class NetworkManager
    {
        // --- Networking ---
        public NetManager client { get; private set; }
        public NetPeer server;
        private ClientListener listener;
        private NetDataWriter writer = new();
        private byte uuid;
        public ConcurrentQueue<Action> mainThreadActions = new();
        public ConcurrentQueue<Action> AwakeRuns = new();
        private float awakeRunTimer = 0f;
        private List<byte> reusablePacketBuffer = new(1024);
        private Dictionary<byte, ushort> lastSeenSequences = new();
        private ushort localSequenceNumber = 0;
        private object boneSendCoroutineHandle;
        public bool isRunningNetParticle = false;

        // --- Material & Bone Info ---
        private float lastBoneSendTime = 0f;
        private const float boneSendInterval = 0.033f;
        private const int maxPacketSize = 1020;
        private const int bytesPerBone = 29;
        private const int bonesPerPacket = maxPacketSize / bytesPerBone;
        private bool sendBoneUpdates = false;

        // --- Player Tracking ---
        public Dictionary<byte, RemotePlayer> players = new();
        private Dictionary<byte, List<byte[]>> pendingPlayerUpdatePackets = new();
        private int numClones = 1;

        private bool IsNewer(ushort current, ushort previous)
            => (ushort)(current - previous) < 32768;

        private void ApplyCollisionLayers()
        {
            Physics.IgnoreLayerCollision(6, 6, true); // Clones (6) cannot interact with each other
            Physics.IgnoreLayerCollision(6, 11, false); // Clones CAN interact with the main player (11)
            Physics.IgnoreLayerCollision(6, 19, true); // Clones cannot interact with cutscene trigger colliders (19)
        }

        public void Connect(string serverIP, int serverPort, string password)
        {
            Core.localPlayer.OnConnect();

            client?.Stop();
            client = null;

            listener = new ClientListener();
            client = new NetManager(listener) { AutoRecycle = true, DisconnectTimeout = 15000 };

            client.Start();
            client.Connect(serverIP, serverPort, (password == "" ? "cuzzillobochfoddy" : password));

            sendBoneUpdates = true;

            ApplyCollisionLayers();
        }


        public void Disconnect()
        {
            if (client == null)
                return;

            MelonLogger.Msg("Disconnected from server");
            Core.uiManager.ingameMessagesUI.AddMessage("Disconnected from server");

            client?.Stop();
            client = null;
            server = null;

            if (boneSendCoroutineHandle != null)
            {
                MelonCoroutines.Stop(boneSendCoroutineHandle);
                boneSendCoroutineHandle = null;
            }

            foreach (var player in players)
            {
                player.Value.Destroy();
                players.Remove(player.Key);
            }

            Core.localPlayer.OnDisconnect();

            sendBoneUpdates = false;
        }

        public void Update()
        {
            if (client == null)
                return;

            client.PollEvents();

            awakeRunTimer += Time.deltaTime;
            if (!AwakeRuns.IsEmpty && awakeRunTimer >= 0.1f)
            {
                if (AwakeRuns.TryDequeue(out var action)) action.Invoke();
                awakeRunTimer = 0f;
            }

            while (mainThreadActions.TryDequeue(out var action)) action.Invoke();
        }

        public void LateUpdate()
        {
            UpdateBones();
            UpdatePlayers();
        }


        private void ApplyJiminyRibbonStateChange(RemotePlayer player, bool jiminyState)
            => player.jiminyRibbon.active = jiminyState;

        private void ApplyCollisionToggle(RemotePlayer player, bool collisionsEnabled)
        {
            if (collisionsEnabled)
            {
                player.netCollisionsEnabled = true;
                Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has enabled collisions");

                if (Core.uiManager.serverConnectUI.uiCollisionsEnabled) player.EnableCollision();
                else player.DisableCollision();
            }
            else
            {
                player.netCollisionsEnabled = false;
                Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has disabled collisions");

                player.DisableCollision();
            }
        }

        private void ApplyAppearanceUpdate(RemotePlayer player, byte[] data)
        {
            Color color = new Color(data[2] / 255f, data[3] / 255f, data[4] / 255f);

            bool colorDifferent = player.color != color;

            player.color = color;

            int nicknameLength = data.Length - 5;
            if (nicknameLength > 0)
            {
                string name = Encoding.UTF8.GetString(data, 5, nicknameLength);
                if (player.displayName == null) Core.uiManager.ingameMessagesUI.AddMessage($"{name} has connected");
                else
                {
                    if (player.displayName != name) Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has changed their nickname to {name}");
                    if (colorDifferent) Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has updated their color");
                }

                player.displayName = name;
            }

            player.RefreshNameAndColor();
        }
        private void ApplyAccessoryDon(RemotePlayer player, byte[] data)
        {
            var accessoryType = data[2];
            int offset = 3;

            switch (accessoryType)
            {
                case 0x00: // Hat
                    {
                        float posX = BitConverter.ToSingle(data, offset); offset += 4;
                        float posY = BitConverter.ToSingle(data, offset); offset += 4;
                        float posZ = BitConverter.ToSingle(data, offset); offset += 4;
                        Vector3 localPosition = new Vector3(posX, posY, posZ);

                        float rotX = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotY = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotW = BitConverter.ToSingle(data, offset); offset += 4;
                        Quaternion localRotation = new Quaternion(rotX, rotY, rotZ, rotW);

                        string hatName = Encoding.UTF8.GetString(data, offset, data.Length - offset);

                        player.WearHat(hatName, localPosition, localRotation);

                        Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has donned the {hatName}");
                        break;
                    }
                case 0x01: // Held item
                    {
                        int handIndex = data[offset]; offset++;

                        float posX = BitConverter.ToSingle(data, offset); offset += 4;
                        float posY = BitConverter.ToSingle(data, offset); offset += 4;
                        float posZ = BitConverter.ToSingle(data, offset); offset += 4;
                        Vector3 localPosition = new Vector3(posX, posY, posZ);

                        float rotX = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotY = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotZ = BitConverter.ToSingle(data, offset); offset += 4;
                        float rotW = BitConverter.ToSingle(data, offset); offset += 4;
                        Quaternion localRotation = new Quaternion(rotX, rotY, rotZ, rotW);

                        string itemName = Encoding.UTF8.GetString(data, offset, data.Length - offset);

                        player.HoldItem(itemName, handIndex, localPosition, localRotation);

                        Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has picked up the {itemName}");
                        break;
                    }
                default:
                    {
                        MelonLogger.Msg($"Unknown Accessory type: {accessoryType}");
                        break;
                    }
            }
        }

        private void UpdatePlayers()
        {
            foreach (var player in players.Values)
            {
                player.InterpolateBoneTransforms();
                player.RotateNametagTowardsCamera(Core.localPlayer.cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.transform.position);
                player.HideNicknameOutsideDistance(Core.localPlayer.camera.position, 100f);
                player.FadeByDistance(Core.localPlayer.camera);
            }
        }

        private void UpdateBones()
        {
            if (!sendBoneUpdates) return;

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;

            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(Core.localPlayer.sortedBones);

            for (int i = 0; i < bonesToSend.Length; i += bonesPerPacket)
            {
                int count = Math.Min(bonesPerPacket, bonesToSend.Length - i);
                TransformNet[] chunk = new TransformNet[count];
                Array.Copy(bonesToSend, i, chunk, 0, count);
                SendBones(chunk, i);
            }
        }

        private void Send(byte[] data, DeliveryMethod deliveryMethod)
        {
            writer.Reset();
            ushort totalLength = (ushort)(data.Length + 2);
            writer.Put(totalLength);
            writer.Put(data);
            server?.Send(writer, deliveryMethod);
        }

        public void SendCollisionToggle(bool state)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.ToggleCollisions);
            reusablePacketBuffer.Add(Convert.ToByte(state));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }

        public void SendJiminyRibbonState(bool state)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.JiminyRibbon);
            reusablePacketBuffer.Add(Convert.ToByte(state));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }

        public void SendBones(TransformNet[] bones, int kickoffPoint)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.BonePositionUpdate);
            reusablePacketBuffer.Add((byte)kickoffPoint);
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localSequenceNumber));
            reusablePacketBuffer.AddRange(TransformNet.Serialize(bones));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.Unreliable);

            localSequenceNumber++;
        }

        public void SendHoldGrabable(Grabable grabable, int handIndex)
        {
            Transform handBone;
            if (handIndex == 0) handBone = Core.localPlayer.handBones.Item1;
            else handBone = Core.localPlayer.handBones.Item2;

            Vector3 localPosition = handBone.InverseTransformPoint(grabable.transform.position);
            Quaternion localRotation = Quaternion.Inverse(handBone.rotation) * grabable.transform.rotation;

            string grabName = grabable.name;
            if (grabName.EndsWith(Core.cloneText)) grabName = grabName.Substring(0, grabName.Length - (Core.cloneText).Length);

            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.AddAccessory);
            reusablePacketBuffer.Add(0x01); // Held item

            reusablePacketBuffer.Add((byte)handIndex);

            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.x));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.y));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.z));

            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.x));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.y));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.z));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.w));

            reusablePacketBuffer.AddRange(Encoding.UTF8.GetBytes(grabName));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }

        public void SendDropGrabable(int handIndex)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.RemoveAccessory);
            reusablePacketBuffer.Add(0x01); // Held item

            reusablePacketBuffer.Add((byte)handIndex);

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }

        public void SendDonHat(Hat hat)
        {
            Vector3 localPosition = Core.localPlayer.headBone.InverseTransformPoint(hat.transform.position);
            Quaternion localRotation = Quaternion.Inverse(Core.localPlayer.headBone.rotation) * hat.transform.rotation;

            string hatName = hat.name;
            if (hatName.EndsWith(Core.cloneText)) hatName = hatName.Substring(0, hatName.Length - Core.cloneText.Length);

            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.AddAccessory);
            reusablePacketBuffer.Add(0x00); // Hat

            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.x));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.y));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localPosition.z));

            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.x));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.y));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.z));
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localRotation.w));

            reusablePacketBuffer.AddRange(Encoding.UTF8.GetBytes(hatName));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }
        public void SendDoffHat()
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.RemoveAccessory);
            reusablePacketBuffer.Add(0x00); // Hat

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }
        /*
        public void SendParticle(byte[] raw)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_GWE);
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localSequenceNumber));
            reusablePacketBuffer.Add(0x00); // 0x00 Particle, 0x01 Sound
            reusablePacketBuffer.AddRange(raw);

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.Unreliable);

            localSequenceNumber++;
        }
        */

        public void SendPlayerInformation()
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add((byte)eOpCode.PlayerInformation);
            reusablePacketBuffer.Add((byte)(Core.uiManager.serverConnectUI.uiColorR * 255));
            reusablePacketBuffer.Add((byte)(Core.uiManager.serverConnectUI.uiColorG * 255));
            reusablePacketBuffer.Add((byte)(Core.uiManager.serverConnectUI.uiColorB * 255));
            reusablePacketBuffer.AddRange(Encoding.UTF8.GetBytes(Core.uiManager.serverConnectUI.uiNNTB));
            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);

            Core.localPlayer.ApplyColor();
        }

        public void HandleServerMessage(byte[] data)
        {
            if (data.Length < 1) return;
            byte opcode = data[0];

            switch (opcode)
            {
                case 1: // UUID
                    {
                        BBSMMdBug.Log("Personal UUID packet received");

                        uuid = data[1];
                        MelonLogger.Msg($"Received UUID: {uuid}");
                        break;
                    }

                case 2: // Player joined
                    {
                        BBSMMdBug.Log("Player join packet received");

                        byte newUUID = data[1];
                        MelonLogger.Msg($"Player {newUUID} has connected.");
                        mainThreadActions.Enqueue(() =>
                        {
                            if (RemotePlayer.nateRecycler.Count == 0)
                            {
                                players[newUUID] = new RemotePlayer(numClones++, mainThreadActions);
                            }
                            else
                            {
                                RemotePlayer nate = RemotePlayer.nateRecycler.Dequeue();
                                nate.baseObj.active = true;
                                nate.displayName = null;
                                nate.RemoveHat(); // Redundancy, already called when player is first added to recycler
                                nate.ResetBonesToBind();
                                players[newUUID] = nate;
                            }

                            if (pendingPlayerUpdatePackets.TryGetValue(newUUID, out List<byte[]> pendingPackets))
                            {
                                foreach (byte[] packet in pendingPackets)
                                {
                                    HandleServerMessage(packet);
                                }
                            }
                        });
                        break;
                    }

                case 3: // Player disconnected
                    {
                        BBSMMdBug.Log("Player disconnect packet received");

                        byte disconnectedUUID = data[1];
                        if (players.TryGetValue(disconnectedUUID, out var player))
                        {
                            mainThreadActions.Enqueue(() =>
                            {
                                Core.uiManager.ingameMessagesUI.AddMessage($"{player.displayName} has disconnected");
                                player.Destroy();
                                players.Remove(disconnectedUUID);
                                lastSeenSequences.Remove(disconnectedUUID);
                            });
                            MelonLogger.Msg($"Player {disconnectedUUID} disconnected.");
                        }
                        else MelonLogger.Warning($"No such player {disconnectedUUID} to disconnect.");
                        break;
                    }

                case 4: // Appearance update
                    {
                        BBSMMdBug.Log("Player appearance packet received");

                        byte playerUUID = data[1];
                        if (players.TryGetValue(playerUUID, out var targetPlayer)) ApplyAppearanceUpdate(targetPlayer, data);
                        else
                        {
                            if (!pendingPlayerUpdatePackets.TryGetValue(playerUUID, out var list)) pendingPlayerUpdatePackets[playerUUID] = list = new List<byte[]>();
                            list.Add(data);
                        }

                        break;
                    }

                case 5: // Bone update
                    {
                        byte boneUUID = data[1];
                        if (!players.TryGetValue(boneUUID, out var bonePlayer)) break;

                        byte kickoffPoint = data[2];
                        ushort seq = BitConverter.ToUInt16(data, 3);
                        byte[] rawBoneData = data.Skip(5).ToArray();

                        if (!lastSeenSequences.TryGetValue(boneUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                        {
                            lastSeenSequences[boneUUID] = seq;

                            var bones = TransformNet.Deserialize(rawBoneData);
                            bonePlayer.UpdateBones(bones, kickoffPoint);
                        }

                        break;
                    }

                case 6: // Generic World Event
                    {
                        BBSMMdBug.Log("GWE packet received");

                        byte eventUUID = data[1];
                        ushort seq = BitConverter.ToUInt16(data, 2);
                        byte[] rawEventData = data.Skip(4).ToArray();

                        if (!lastSeenSequences.TryGetValue(eventUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                        {
                            byte eID = rawEventData[0];
                            switch (eID)
                            {
                                case 0x00: // Particle GWE
                                    {
                                        switch (rawEventData[1])
                                        {
                                            case 0x00:
                                                {
                                                    FootData fd = FootDataHelpers.DeserializeFootData(rawEventData.Skip(2).ToArray(), players[eventUUID]);
                                                    if (fd == null) return;
                                                    isRunningNetParticle = true;
                                                    Core.localPlayer.particleParty.OnPlant(fd);
                                                    break;
                                                }
                                            case 0x01:
                                                {
                                                    FootData fd = FootDataHelpers.DeserializeFootData(rawEventData.Skip(2).ToArray(), players[eventUUID]);
                                                    if (fd == null) return;
                                                    isRunningNetParticle = true;
                                                    Core.localPlayer.particleParty.OnSlip(fd);
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case 0x01: // Sound GWE
                                    {
                                        break;
                                    }
                                default:
                                    {
                                        MelonLogger.Msg($"Unknown GWE eID: {eID}");
                                        break;
                                    }
                            }
                        }
                        break;
                    }

                case 7: // Don Accessory
                    {
                        BBSMMdBug.Log("Accessory don packet received");

                        byte playerUUID = data[1];
                        if (players.TryGetValue(playerUUID, out var player)) ApplyAccessoryDon(player, data);
                        else
                        {
                            if (!pendingPlayerUpdatePackets.TryGetValue(playerUUID, out var list)) pendingPlayerUpdatePackets[playerUUID] = list = new List<byte[]>();
                            list.Add(data);
                        }

                        break;
                    }

                case 8: // Doff Accessory
                    {
                        BBSMMdBug.Log("Accessory doff packet received");

                        byte playerUUID = data[1];
                        if (!players.TryGetValue(playerUUID, out var player)) break;
                        var accessoryType = data[2];

                        switch (accessoryType)
                        {
                            case 0x00: // Hat
                                {
                                    player.RemoveHat();
                                    break;
                                }
                            case 0x01: // Held item
                                {
                                    player.DropItem(data[3]);
                                    break;
                                }
                            default:
                                {
                                    MelonLogger.Msg($"Unknown Accessory type: {accessoryType}");
                                    break;
                                }
                        }
                        break;
                    }

                case 9: // Jiminy Ribbon State
                    {
                        BBSMMdBug.Log("Jiminy Ribbon packet received");

                        byte playerUUID = data[1];
                        bool jiminyState = Convert.ToBoolean(data[2]);
                        if (players.TryGetValue(playerUUID, out var player)) ApplyJiminyRibbonStateChange(player, jiminyState);
                        else
                        {
                            if (!pendingPlayerUpdatePackets.TryGetValue(playerUUID, out var list)) pendingPlayerUpdatePackets[playerUUID] = list = new List<byte[]>();
                            list.Add(data);
                        }

                        break;
                    }
                case 10: // Collision Toggle Event
                    {
                        BBSMMdBug.Log("Collision toggle packet received");

                        byte playerUUID = data[1];
                        bool collisionsEnabled = Convert.ToBoolean(data[2]);
                        if (players.TryGetValue(playerUUID, out var player)) ApplyCollisionToggle(player, collisionsEnabled);
                        else
                        {
                            if (!pendingPlayerUpdatePackets.TryGetValue(playerUUID, out var list)) pendingPlayerUpdatePackets[playerUUID] = list = new List<byte[]>();
                            list.Add(data);
                        }

                        break;
                    }

                default:
                    {
                        MelonLogger.Msg($"Unknown opcode from server: {opcode}");
                        break;
                    }
            }
        }
    }
}
