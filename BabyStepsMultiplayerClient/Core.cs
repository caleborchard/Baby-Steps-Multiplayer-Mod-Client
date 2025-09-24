using BabyStepsMultiplayerClient;
using Il2Cpp;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLoader;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[assembly: MelonInfo(typeof(BabyStepsMultiplayerClient.Core), "BabyStepsMultiplayerClient", "1.0.1", "Caleb Orchard", null)]
[assembly: MelonGame("DefaultCompany", "BabySteps")]

namespace BabyStepsMultiplayerClient
{
    public class Core : MelonMod
    {
        // --- Constants ---
        private const byte OPCODE_BPU = 0x01; // Bone position update
        private const byte OPCODE_UCI = 0x02; // Update color information (used for nickname too now)
        private const byte OPCODE_GWE = 0x03; // Generic World Event
        private const byte OPCODE_AAE = 0x04; // Accessory Add(Don) Event
        private const byte OPCODE_ARE = 0x05; // Accessory Remove(Doff) Event
        private const byte OPCODE_JRE = 0x06; // Jiminy Ribbon Event
        private const byte OPCODE_CTE = 0x07; // Collision Toggle Event

        // --- Static Scene References ---
        public static Core thisInstance;
        private static GameObject basePlayer;
        public static PlayerMovement basePlayerMovement;
        private static Transform baseMesh;
        public static Material baseMaterial;
        public static Color baseColor;
        private static Transform camera;

        private static GameObject jiminyRibbon;
        private static bool lastJiminyState;

        private static Transform particleHouse;
        private static ParticleParty particleParty;

        // --- Material & Bone Info ---
        public Transform headBone;
        public (Transform, Transform) handBones;
        private Transform[] sortedBones;
        private float lastBoneSendTime = 0f;
        private const float boneSendInterval = 0.033f;
        private const int maxPacketSize = 1020;
        private const int bytesPerBone = 29;
        private const int bonesPerPacket = maxPacketSize / bytesPerBone;
        private bool sendBoneUpdates = false;

        // --- Networking ---
        public NetManager client;
        public NetPeer server;
        private ClientListener listener;
        private NetDataWriter writer = new();
        private byte uuid;
        public static ConcurrentQueue<Action> mainThreadActions = new();
        public static ConcurrentQueue<Action> AwakeRuns = new();
        private float awakeRunTimer = 0f;
        private List<byte> reusablePacketBuffer = new(1024);
        private Dictionary<byte, ushort> lastSeenSequences = new();
        private ushort localSequenceNumber = 0;
        private object boneSendCoroutineHandle;
        public static bool isRunningNetParticle = false;
        private static string cloneText = "(Clone)";

        // --- Player Tracking ---
        public Dictionary<byte, NateMP> players = new();
        private Dictionary<byte, List<byte[]>> pendingPlayerUpdatePackets = new();
        private int numClones = 1;

        // --- UI State ---
        private bool showServerPanel = false;
        private bool showPlayersTab = false;
        public ServerConnectUI serverConnectUI;
        PlayersTabUI playersTabUI;
        public IngameMessagesUI ingameMessagesUI;

        // --- Mainline (Unity Callbacks) ---
        public override void OnInitializeMelon() { }
        [Obsolete]
        public override void OnApplicationStart() 
        { 
            thisInstance = this; 
            serverConnectUI = new ServerConnectUI(this); 
            serverConnectUI.LoadConfig();
            ingameMessagesUI = new IngameMessagesUI();
            playersTabUI = new PlayersTabUI(this);
        }
        public override void OnGUI() 
        { 
            ingameMessagesUI.DrawUI(); 
            if (showServerPanel) serverConnectUI.DrawUI(); 
            if (showPlayersTab) playersTabUI.DrawUI();
        }
        public override void OnUpdate() { UpdateLoop(); }
        public override void OnLateUpdate() { HandleBoneSending(); UpdatePlayersLate(); }
        public override void OnApplicationQuit() { Disconnect(); }

        // --- Helpers ---
        private void UpdateLoop() 
        {
            if (Input.GetKeyDown(KeyCode.F2)) showServerPanel = !showServerPanel;

            if (client != null)
            {
                client.PollEvents();

                awakeRunTimer += Time.deltaTime;
                if (!AwakeRuns.IsEmpty && awakeRunTimer >= 0.1f)
                {
                    if (AwakeRuns.TryDequeue(out var action)) action.Invoke();
                    awakeRunTimer = 0f;
                }

                while (mainThreadActions.TryDequeue(out var action)) action.Invoke();

                if (Input.GetKeyDown(KeyCode.Tab)) showPlayersTab = true;

                if (lastJiminyState != jiminyRibbon.active)
                {
                    lastJiminyState = jiminyRibbon.active;
                    SendJiminyRibbonState();
                }
            }
            if (Input.GetKeyUp(KeyCode.Tab)) showPlayersTab = false; // Outside of isClientConnected check so that it won't get stuck on disconnect
        }
        private void UpdatePlayersLate()
        {
            foreach (var player in players.Values)
            {
                player.InterpolateBoneTransforms();
                player.RotateNametagTowardsCamera(camera);
                player.HideNicknameOutsideDistance(camera.position, 100f);
                player.FadeByDistance(camera);
            }
        }
        static bool IsNewer(ushort current, ushort previous) { return (ushort)(current - previous) < 32768; }
        private void ApplyAppearanceUpdate(NateMP player, byte[] data)
        {
            Color color = new Color(data[2] / 255f, data[3] / 255f, data[4] / 255f);

            bool colorDifferent = player.color != color;

            player.color = color;

            int nicknameLength = data.Length - 5;
            if (nicknameLength > 0)
            {
                string name = Encoding.UTF8.GetString(data, 5, nicknameLength);
                if (player.displayName == null) ingameMessagesUI.AddMessage($"{name} has connected");
                else
                {
                    if (player.displayName != name) ingameMessagesUI.AddMessage($"{player.displayName} has changed their nickname to {name}");
                    if (colorDifferent) ingameMessagesUI.AddMessage($"{player.displayName} has updated their color");
                }

                player.displayName = name;
            }

            player.RefreshNameAndColor();
        }
        private void ApplyAccessoryDon(NateMP player, byte[] data)
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

                        ingameMessagesUI.AddMessage($"{player.displayName} has donned the {hatName}");
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

                        ingameMessagesUI.AddMessage($"{player.displayName} has picked up the {itemName}");
                        break;
                    }
                default:
                    {
                        MelonLogger.Msg($"Unknown Accessory type: {accessoryType}");
                        break;
                    }
            }
        }
        private void ApplyJiminyRibbonStateChange(NateMP player, bool jiminyState)
        {
            player.jiminyRibbon.active = jiminyState;
        }
        private void ApplyCollisionToggle(NateMP player, bool collisionsEnabled)
        {
            if (collisionsEnabled)
            {
                player.netCollisionsEnabled = true;
                ingameMessagesUI.AddMessage($"{player.displayName} has enabled collisions");

                if (serverConnectUI.uiCollisionsEnabled) player.EnableCollision();
                else player.DisableCollision();
            }
            else
            {
                player.netCollisionsEnabled = false;
                ingameMessagesUI.AddMessage($"{player.displayName} has disabled collisions");

                player.DisableCollision();
            }
        }

        // --- Network ---
        public void connectToServer(string serverIP, int serverPort, string password)
        {
            if (basePlayer == null) basePlayer = GameObject.Find("Dudest");
            if (basePlayerMovement == null) basePlayerMovement = basePlayer.GetComponent<PlayerMovement>();
            if (baseMesh == null) baseMesh = basePlayer.transform.Find("IKTargets/HipTarget/NathanAnimIK_October2022");
            if (camera == null) camera = basePlayer.transform.Find("GameCam");
            if (baseMaterial == null)
                baseMaterial = baseMesh.Find("Nathan.001").GetComponent<SkinnedMeshRenderer>().sharedMaterials.FirstOrDefault(m => m.name.Contains("NewSuit_Oct22"));
            if (jiminyRibbon == null) jiminyRibbon = basePlayerMovement.jiminyRibbon;
            if (particleHouse == null) particleHouse = basePlayer.transform.Find("ParticleHouse");
            if (particleParty == null) particleParty = particleHouse.GetComponent<ParticleParty>();

            if (NateMP.suitTexture == null)
            {
                basePlayerMovement.CleanPlayerCompletely();

                // Copy original albedo because the mud gets projects onto the actual texture
                Texture ogAlbedo = baseMaterial.GetTexture("_AlbedoMap");
                Texture2D t2d = new Texture2D(ogAlbedo.width, ogAlbedo.height, TextureFormat.ARGB32, false);
                RenderTexture cRT = RenderTexture.active;
                RenderTexture rt = new RenderTexture(ogAlbedo.width, ogAlbedo.height, 0);
                Graphics.Blit(ogAlbedo, rt);
                RenderTexture.active = rt;
                t2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                t2d.Apply();
                RenderTexture.active = cRT;
                rt.Release();

                NateMP.suitTexture = t2d;
            }

            if (sortedBones == null)
            {
                sortedBones = NateMP.ExtractValidBones(baseMesh.Find("Nathan.001").GetComponent<SkinnedMeshRenderer>().bones, baseMesh);
            }

            for (int i = 0; i < sortedBones.Length; i++)
            {
                if (sortedBones[i].name == "head.x") { headBone = sortedBones[i]; continue; }
                else if (sortedBones[i].name == "hand.r") { handBones.Item1 = sortedBones[i]; continue; }
                else if (sortedBones[i].name == "hand.l") { handBones.Item2 = sortedBones[i]; continue; }
            }

            client?.Stop();
            client = null;

            listener = new ClientListener(this);
            client = new NetManager(listener) { AutoRecycle = true, DisconnectTimeout = 15000 };

            client.Start();
            client.Connect(serverIP, serverPort, (password == "" ? "cuzzillobochfoddy" : password));

            sendBoneUpdates = true;

            Physics.IgnoreLayerCollision(6, 6, true); // Clones (6) cannot interact with each other
            Physics.IgnoreLayerCollision(6, 11, false); // Clones CAN interact with the main player (11)
            Physics.IgnoreLayerCollision(6, 19, true); // Clones cannot interact with cutscene trigger colliders (19)
        }
        public void Disconnect()
        {
            if (client != null)
            {
                MelonLogger.Msg("Disconnected from server");
                ingameMessagesUI.AddMessage("Disconnected from server");

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

                baseColor = Color.white;
                baseMaterial.color = baseColor;

                sendBoneUpdates = false;
            }
        }
        public void SendCollisionToggle(bool state)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_CTE);
            reusablePacketBuffer.Add(Convert.ToByte(state));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }
        public void SendJiminyRibbonState()
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_JRE);
            reusablePacketBuffer.Add(Convert.ToByte(lastJiminyState));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }
        public void SendHoldGrabable(Grabable grabable, int handIndex)
        {
            Transform handBone;
            if (handIndex == 0) handBone = handBones.Item1;
            else handBone = handBones.Item2;

            Vector3 localPosition = handBone.InverseTransformPoint(grabable.transform.position);
            Quaternion localRotation = Quaternion.Inverse(handBone.rotation) * grabable.transform.rotation;

            string grabName = grabable.name;
            if (grabName.EndsWith(cloneText)) grabName = grabName.Substring(0, grabName.Length - (cloneText).Length);

            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_AAE);
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
            reusablePacketBuffer.Add(OPCODE_ARE);
            reusablePacketBuffer.Add(0x01); // Held item

            reusablePacketBuffer.Add((byte)handIndex);

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);
        }
        public void SendDonHat(Hat hat)
        {
            Vector3 localPosition = headBone.InverseTransformPoint(hat.transform.position);
            Quaternion localRotation = Quaternion.Inverse(headBone.rotation) * hat.transform.rotation;

            string hatName = hat.name;
            if (hatName.EndsWith(cloneText)) hatName = hatName.Substring(0, hatName.Length - cloneText.Length);

            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_AAE);
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
            reusablePacketBuffer.Add(OPCODE_ARE);
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
        private void HandleBoneSending()
        {
            if (!sendBoneUpdates) return;

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;

            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(sortedBones);

            for (int i = 0; i < bonesToSend.Length; i += bonesPerPacket)
            {
                int count = Math.Min(bonesPerPacket, bonesToSend.Length - i);
                TransformNet[] chunk = new TransformNet[count];
                Array.Copy(bonesToSend, i, chunk, 0, count);
                SendBones(chunk, i);
            }
        }
        private void SendBones(TransformNet[] bones, int kickoffPoint)
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_BPU);
            reusablePacketBuffer.Add((byte)kickoffPoint);
            reusablePacketBuffer.AddRange(BitConverter.GetBytes(localSequenceNumber));
            reusablePacketBuffer.AddRange(TransformNet.Serialize(bones));

            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.Unreliable);

            localSequenceNumber++;
        }
        private void Send(byte[] data, DeliveryMethod deliveryMethod)
        {
            writer.Reset();
            ushort totalLength = (ushort)(data.Length + 2);
            writer.Put(totalLength);
            writer.Put(data);
            server?.Send(writer, deliveryMethod);
        }
        public void UpdateNicknameAndColor()
        {
            reusablePacketBuffer.Clear();
            reusablePacketBuffer.Add(OPCODE_UCI);
            reusablePacketBuffer.Add((byte)(serverConnectUI.uiColorR * 255));
            reusablePacketBuffer.Add((byte)(serverConnectUI.uiColorG * 255));
            reusablePacketBuffer.Add((byte)(serverConnectUI.uiColorB * 255));
            reusablePacketBuffer.AddRange(Encoding.UTF8.GetBytes(serverConnectUI.uiNNTB));
            Send(reusablePacketBuffer.ToArray(), DeliveryMethod.ReliableOrdered);

            baseColor = new Color(serverConnectUI.uiColorR, serverConnectUI.uiColorG, serverConnectUI.uiColorB);
            baseMaterial.color = baseColor;
        }
        public void HandleServerMessage(byte[] data)
        {
            if (data.Length < 1) return;
            byte opcode = data[0];

            switch (opcode)
            {
                case 1: // UUID
                    {
                        uuid = data[1];
                        MelonLogger.Msg($"Received UUID: {uuid}");
                        break;
                    }

                case 2: // Player joined
                    {
                        byte newUUID = data[1];
                        MelonLogger.Msg($"Player {newUUID} has connected.");
                        mainThreadActions.Enqueue(() =>
                        {
                            if (NateMP.nateRecycler.Count == 0)
                            {
                                players[newUUID] = new NateMP(basePlayer, baseMesh, numClones++, mainThreadActions);
                            }
                            else
                            {
                                NateMP nate = NateMP.nateRecycler.Dequeue();
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
                        byte disconnectedUUID = data[1];
                        if (players.TryGetValue(disconnectedUUID, out var player))
                        {
                            mainThreadActions.Enqueue(() =>
                            {
                                ingameMessagesUI.AddMessage($"{player.displayName} has disconnected");
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
                                                    particleParty.OnPlant(fd);
                                                    break;
                                                }
                                            case 0x01:
                                                {
                                                    FootData fd = FootDataHelpers.DeserializeFootData(rawEventData.Skip(2).ToArray(), players[eventUUID]);
                                                    if (fd == null) return;
                                                    isRunningNetParticle = true;
                                                    particleParty.OnSlip(fd);
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

        // --- Listener ---
        private class ClientListener : INetEventListener
        {
            private readonly Core _core;
            public ClientListener(Core core) => _core = core;

            public void OnPeerConnected(NetPeer peer)
            {
                _core.server = peer;
                _core.UpdateNicknameAndColor();

                Hat hat = Core.basePlayerMovement.currentHat;
                if (hat != null) _core.SendDonHat(hat);

                Grabable rightItem = Core.basePlayerMovement.handItems[0];
                Grabable leftItem = Core.basePlayerMovement.handItems[1];

                if (rightItem != null) _core.SendHoldGrabable(rightItem, 0);
                if (leftItem != null) _core.SendHoldGrabable(leftItem, 1);

                _core.SendJiminyRibbonState();

                _core.SendCollisionToggle(_core.serverConnectUI.uiCollisionsEnabled);

                _core.ingameMessagesUI.AddMessage("Connected to server");
            }

            public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) 
            { 
                _core.Disconnect(); 
                Resources.UnloadUnusedAssets();
            }

            public void OnNetworkError(IPEndPoint ep, SocketError error) => MelonLogger.Error($"Network error: {error}");
            public void OnConnectionRequest(ConnectionRequest req) => req.AcceptIfKey("cuzzillobochfoddy");
            public void OnNetworkReceiveUnconnected(IPEndPoint ep, NetPacketReader reader, UnconnectedMessageType type) { }
            public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

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
        }
    }
}
