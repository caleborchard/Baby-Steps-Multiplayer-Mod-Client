using BabyStepsMultiplayerClient.Audio;
using BabyStepsMultiplayerClient.Player;
using Il2Cpp;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLoader;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using static Il2CppRootMotion.FinalIK.FBBIKHeadEffector;

namespace BabyStepsMultiplayerClient.Networking
{
    public class NetworkManager
    {
        // --- Networking ---
        public NetManager client { get; private set; }
        public NetPeer server;
        private ClientListener listener;
        private byte uuid;
        public ConcurrentQueue<Action> mainThreadActions = new();

        private PlayerPacketQueue<byte, byte[]> pendingPlayerJoins = new();
        private PlayerPacketQueue<byte, byte[]> pendingPlayerUpdates = new();

        private ConcurrentDictionary<byte, ushort> lastSeenBoneSequences = new();
        private ConcurrentDictionary<byte, ushort> lastSeenAudioFrameSequences = new();
        private ushort localBoneSequenceNumber = 0;
        private ushort localAudioFrameSequenceNumber = 0;
        public bool isRunningNetParticle = false;

        // --- Material & Bone Info ---
        public const int maxPacketSize = 1024;

        // --- Player Tracking ---
        public ConcurrentDictionary<byte, RemotePlayer> players = new();
        private int numClones = 1;

        private bool IsNewer(ushort current, ushort previous)
            => (ushort)(current - previous) < 32768;

        public NetworkManager()
        {
            Physics.IgnoreLayerCollision(6, 6, true); // Clones (6) cannot interact with each other
            Physics.IgnoreLayerCollision(6, 11, false); // Clones CAN interact with the main player (11)
            Physics.IgnoreLayerCollision(6, 19, true); // Clones cannot interact with cutscene trigger colliders (19)

            Physics.IgnoreLayerCollision(23, 23, true);
            Physics.IgnoreLayerCollision(23, 11, false);
            Physics.IgnoreLayerCollision(23, 19, true);
        }

        public void Connect(string serverIP, int serverPort, string password)
        {
            Disconnect();

            Core.uiManager.notificationsUI.AddMessage("Connecting to server...");

            listener = new ClientListener();
            client = new NetManager(listener)
            {
                AutoRecycle = true,
                DisconnectTimeout = 15000,
                UseNativeSockets = true,
            };

            client.Start();

            string effectivePassword = Core.SERVER_VERSION + (password == "" ? "cuzzillobochfoddy" : password);
            client.Connect(serverIP, serverPort, effectivePassword);
        }

        public void Disconnect()
        {
            if (client == null) return;

            client?.Stop();
            client = null;
            server = null;
            localBoneSequenceNumber = 0;
            localAudioFrameSequenceNumber = 0;
            uuid = 0;
            numClones = 0;

            pendingPlayerJoins.Clear();
            pendingPlayerUpdates.Clear();

            mainThreadActions.Clear();
            lastSeenBoneSequences.Clear();
            lastSeenAudioFrameSequences.Clear();

            foreach (var player in players) player.Value.Dispose();
            players.Clear();

            if (LocalPlayer.Instance != null) LocalPlayer.Instance.Dispose();
            LocalPlayer.Instance = null;

            Core.uiManager.notificationsUI.AddMessage("Disconnected from server");
        }

        public void Update()
        {
            if (client == null) return;

            if (Core.HasLoadedGame())
            {
                if (LocalPlayer.Instance == null)
                {
                    LocalPlayer.Instance = new();
                    LocalPlayer.Instance.Initialize();
                    return;
                }

                pendingPlayerJoins.Process(HandleServerMessage);
            }
            
            while (mainThreadActions.TryDequeue(out var action))
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Core.logger.Error(ex.ToString());
                }

            try
            {
                if (!client.UnsyncedEvents) 
                    client.PollEvents();
            }
            catch (Exception ex)
            {
                Core.logger.Error(ex.ToString());
            }
        }

        public void LateUpdate()
        {
            if (client == null) return;
            if (players == null) return;

            foreach (var player in players.Values)
            {
                if (player == null) continue;

                player.LateUpdate();
            }
        }

        private void ApplyJiminyRibbonStateChange(RemotePlayer player, bool jiminyState)
        {
            if (player.jiminyRibbon == null)
                return;
            player.jiminyRibbon.active = jiminyState;
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
            Color color = new Color(data[2] / 255f, data[3] / 255f, data[4] / 255f);

            bool colorDifferent = player.GetSuitColor() != color;
            if (colorDifferent) player.SetSuitColor(color);

            int nicknameLength = data.Length - 5;
            if (nicknameLength > 0)
            {
                string name = Encoding.UTF8.GetString(data, 5, nicknameLength);
                if (!player.firstAppearanceApplication)
                {
                    Core.uiManager.notificationsUI.AddMessage($"{name} has connected");
                    player.firstAppearanceApplication = true;
                }
                else
                {
                    if (player.displayName != name)
                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has changed their nickname to {name}");
                    if (colorDifferent)
                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has updated their color");
                }

                player.SetDisplayName(name);
            }
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

                        //player.WearHat(hatName, localPosition, localRotation);
                        player.WearHat(hatName, new Vector3(0f, 0.21f, -0.02f), new Quaternion(-0.21517f, 0.0000689f, -0.0003178f, 0.97627f));

                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has donned the {hatName}");
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

                        Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has picked up the {itemName}");
                        break;
                    }
                default:
                    {
                        Core.logger.Msg($"Unknown Accessory type: {accessoryType}");
                        break;
                    }
            }
        }

        private void Send(List<byte> data, DeliveryMethod deliveryMethod)
            => Send(data.ToArray(), deliveryMethod);
        private void Send(byte[] data, DeliveryMethod deliveryMethod)
        {
            NetDataWriter writer = new();
            ushort totalLength = (ushort)(data.Length + 2);
            writer.Put(totalLength);
            writer.Put(data);
            server?.Send(writer, deliveryMethod);
        }

        public void SendCollisionToggle(bool state)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.ToggleCollisions,
                Convert.ToByte(state)
            };
            Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendJiminyRibbonState(bool state)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.JiminyRibbon,
                Convert.ToByte(state)
            };
            Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendBones(TransformNet[] bones, int kickoffPoint)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.BonePositionUpdate,
                (byte)kickoffPoint
            };
            writer.AddRange(BitConverter.GetBytes(localBoneSequenceNumber++));
            writer.AddRange(TransformNet.Serialize(bones));

            Send(writer, DeliveryMethod.Unreliable);
        }

        private void SendAddAccessory(byte itemType,
            string itemName,
            Vector3 localPosition,
            Quaternion localRotation,
            int handIndex = 0)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.AddAccessory,
                itemType
            };

            if (itemType == 0x01) // Held item
                writer.Add((byte)handIndex);

            writer.AddRange(BitConverter.GetBytes(localPosition.x));
            writer.AddRange(BitConverter.GetBytes(localPosition.y));
            writer.AddRange(BitConverter.GetBytes(localPosition.z));

            writer.AddRange(BitConverter.GetBytes(localRotation.x));
            writer.AddRange(BitConverter.GetBytes(localRotation.y));
            writer.AddRange(BitConverter.GetBytes(localRotation.z));
            writer.AddRange(BitConverter.GetBytes(localRotation.w));

            writer.AddRange(Encoding.UTF8.GetBytes(itemName));

            Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void SendRemoveAccessory(byte itemType, int handIndex = 0)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.RemoveAccessory,
                itemType
            };

            if (itemType == 0x01) // Held item
                writer.Add((byte)handIndex);

            Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendHoldGrabable(Grabable grabable, int handIndex)
        {
            if (LocalPlayer.Instance == null)
                return;

            Transform handBone;
            if (handIndex == 0) handBone = LocalPlayer.Instance.handBones.Item1;
            else handBone = LocalPlayer.Instance.handBones.Item2;

            if (handBone == null)
                return;

            Vector3 localPosition = handBone.InverseTransformPoint(grabable.transform.position);
            Quaternion localRotation = Quaternion.Inverse(handBone.rotation) * grabable.transform.rotation;

            string grabName = grabable.name;
            if (grabName.EndsWith(Core.cloneText))
                grabName = grabName.Substring(0, grabName.Length - (Core.cloneText).Length);

            SendAddAccessory(0x01, grabName, localPosition, localRotation, handIndex); // Held item
        }
        public void SendDropGrabable(int handIndex)
            => SendRemoveAccessory(0x01, handIndex); // Held item

        public void SendDonHat(Hat hat)
        {
            if (LocalPlayer.Instance == null)
                return;
            if (LocalPlayer.Instance.headBone == null)
                return;

            Vector3 localPosition = LocalPlayer.Instance.headBone.InverseTransformPoint(hat.transform.position);
            Quaternion localRotation = Quaternion.Inverse(LocalPlayer.Instance.headBone.rotation) * hat.transform.rotation;

            string hatName = hat.name;
            if (hatName.EndsWith(Core.cloneText)) hatName = hatName.Substring(0, hatName.Length - Core.cloneText.Length);

            SendAddAccessory(0, hatName, localPosition, localRotation); // Hat
        }
        public void SendDoffHat()
            => SendRemoveAccessory(0); // Hat

        public void SendChatMessage(string message)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.ChatMessage
            };

            writer.AddRange(Encoding.UTF8.GetBytes(message));

            Send(writer, DeliveryMethod.ReliableOrdered);
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
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.PlayerInformation,
                (byte)(ModSettings.player.SuitColor.Value.r * 255),
                (byte)(ModSettings.player.SuitColor.Value.g * 255),
                (byte)(ModSettings.player.SuitColor.Value.b * 255)
            };
            writer.AddRange(Encoding.UTF8.GetBytes(ModSettings.player.Nickname.Value));

            Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendAudioFrame(byte[] encodedData)
        {
            List<byte> writer = new(maxPacketSize)
            {
                (byte)eOpCode.AudioFrame
            };

            writer.AddRange(BitConverter.GetBytes(localAudioFrameSequenceNumber++));

            writer.AddRange(encodedData);

            Send(writer, DeliveryMethod.Unreliable);
        }

        public void HandleServerMessage(byte[] data)
        {
            try
            {
                if (data.Length < 1) return;
                byte opcode = data[0];

                switch (opcode)
                {
                    case 1: // UUID
                        {
                            Core.DebugMsg("Personal UUID packet received");

                            uuid = data[1];
                            Core.logger.Msg($"Received UUID: {uuid}");
                            break;
                        }

                    case 2: // Player joined
                        {
                            Core.DebugMsg("Player join packet received");

                            byte newUUID = data[1];

                            if (players.ContainsKey(newUUID))
                                return;

                            if (!Core.HasLoadedGame()
                                || (LocalPlayer.Instance == null))
                            {
                                pendingPlayerJoins.Enqueue(newUUID, data);
                                return;
                            }

                            RemotePlayer nate = null;
                            if (!RemotePlayer.GlobalPool.TryTake(out nate))
                            {
                                nate = new();
                                nate.Initialize(numClones++);
                            }

                            nate.SetActive(true);
                            nate.RemoveHat(); // Redundancy, already called when player is first added to recycler
                            nate.ResetBonesToBind();

                            nate.InitializeAudioSource();

                            players[newUUID] = nate;

                            pendingPlayerUpdates.Process(newUUID, HandleServerMessage);

                            break;
                        }

                    case 3: // Player disconnected
                        {
                            Core.DebugMsg("Player disconnect packet received");
                            byte disconnectedUUID = data[1];
                            if (players.TryGetValue(disconnectedUUID, out var player))
                            {
                                players.Remove(disconnectedUUID, out _);
                                lastSeenBoneSequences.Remove(disconnectedUUID, out _);
                                lastSeenAudioFrameSequences.Remove(disconnectedUUID, out _);
                                Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has disconnected");
                                player.Dispose();
                            }
                            //else
                            //    Core.logger.Warning($"No such player {disconnectedUUID} to disconnect.");

                            pendingPlayerJoins.Clear(disconnectedUUID);
                            pendingPlayerUpdates.Clear(disconnectedUUID);

                            break;
                        }

                    case 4: // Appearance update
                        {
                            Core.DebugMsg("Player appearance packet received");

                            byte playerUUID = data[1];
                            if (players.TryGetValue(playerUUID, out var targetPlayer))
                                ApplyAppearanceUpdate(targetPlayer, data);
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }

                    case 5: // Bone update
                        {
                            byte boneUUID = data[1];
                            if (players.TryGetValue(boneUUID, out var bonePlayer))
                            {
                                byte kickoffPoint = data[2];
                                ushort seq = BitConverter.ToUInt16(data, 3);
                                byte[] rawBoneData = data.Skip(5).ToArray();

                                if (!lastSeenBoneSequences.TryGetValue(boneUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                                {
                                    lastSeenBoneSequences[boneUUID] = seq;

                                    byte[] boneData = rawBoneData;
                                    var bones = TransformNet.Deserialize(boneData);
                                    bonePlayer.UpdateBones(bones, kickoffPoint);
                                }
                            }
                            else
                                pendingPlayerUpdates.Enqueue(boneUUID, data);

                            break;
                        }

                    case 6: // Generic World Event. Old and unused, redo this before enabling again
                        {
                            Core.DebugMsg("GWE packet received");

                            byte eventUUID = data[1];
                            ushort seq = BitConverter.ToUInt16(data, 2);
                            byte[] rawEventData = data.Skip(4).ToArray();

                            if (!lastSeenBoneSequences.TryGetValue(eventUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
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
                                                        if ((LocalPlayer.Instance != null)
                                                            && (LocalPlayer.Instance.particleParty != null))
                                                        {
                                                            FootData fd = FootDataHelpers.DeserializeFootData(rawEventData.Skip(2).ToArray(), players[eventUUID]);
                                                            if (fd == null) return;
                                                            isRunningNetParticle = true;
                                                            LocalPlayer.Instance.particleParty.OnPlant(fd);
                                                        }
                                                        break;
                                                    }
                                                case 0x01:
                                                    {
                                                        if ((LocalPlayer.Instance != null)
                                                            && (LocalPlayer.Instance.particleParty != null))
                                                        {
                                                            FootData fd = FootDataHelpers.DeserializeFootData(rawEventData.Skip(2).ToArray(), players[eventUUID]);
                                                            if (fd == null) return;
                                                            isRunningNetParticle = true;
                                                            LocalPlayer.Instance.particleParty.OnSlip(fd);
                                                        }
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
                                            Core.logger.Msg($"Unknown GWE eID: {eID}");
                                            break;
                                        }
                                }
                            }
                            break;
                        }

                    case 7: // Don Accessory
                        {
                            Core.DebugMsg("Accessory don packet received");

                            byte playerUUID = data[1];
                            if (players.TryGetValue(playerUUID, out var player))
                            {
                                ApplyAccessoryDon(player, data);
                                ApplyCollisionToggle(player, player.netCollisionsEnabled);
                            }
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }

                    case 8: // Doff Accessory
                        {
                            Core.DebugMsg("Accessory doff packet received");

                            byte playerUUID = data[1];
                            if (players.TryGetValue(playerUUID, out var player))
                            {
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
                                            Core.logger.Msg($"Unknown Accessory type: {accessoryType}");
                                            break;
                                        }
                                }
                            }
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }

                    case 9: // Jiminy Ribbon State
                        {
                            Core.DebugMsg("Jiminy Ribbon packet received");

                            byte playerUUID = data[1];
                            bool jiminyState = Convert.ToBoolean(data[2]);
                            if (players.TryGetValue(playerUUID, out var player)) 
                                ApplyJiminyRibbonStateChange(player, jiminyState);
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }

                    case 10: // Collision Toggle Event
                        {
                            Core.DebugMsg("Collision toggle packet received");

                            byte playerUUID = data[1];
                            bool collisionsEnabled = Convert.ToBoolean(data[2]);
                            if (players.TryGetValue(playerUUID, out var player))
                            {
                                ApplyCollisionToggle(player, collisionsEnabled);
                                Core.uiManager.notificationsUI.AddMessage($"{player.displayName} has {(collisionsEnabled ? "enabled" : "disabled")} collisions");
                            }
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }
                    case 11: // Text Chat Messages
                        {
                            Core.DebugMsg("Chat message received");

                            byte playerUUID = data[1];
                            string message = Encoding.UTF8.GetString(data.Skip(2).ToArray());
                            if (players.TryGetValue(playerUUID, out var player))
                                Core.uiManager.notificationsUI.AddMessage($"{player.displayName}: {message}");
                            else
                                pendingPlayerUpdates.Enqueue(playerUUID, data);

                            break;
                        }
                    case 12: // Proximity Chat Audio Frames
                        {
                            byte playerUUID = data[1];

                            if(players.TryGetValue(playerUUID, out var player))
                            {
                                ushort seq = BitConverter.ToUInt16(data, 2);
                                if (!lastSeenAudioFrameSequences.TryGetValue(playerUUID, out ushort lastSeq) || IsNewer(seq, lastSeq))
                                {
                                    lastSeenAudioFrameSequences[playerUUID] = seq;

                                    byte[] audioFrame = data[4..];
                                    player.audioSource.QueueOpusPacket(audioFrame);
                                }
                            }

                            break;
                        }

                    default:
                        {
                            Core.logger.Msg($"Unknown opcode from server: {opcode}");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Core.logger.Error(ex);
            }
        }
    }
}
