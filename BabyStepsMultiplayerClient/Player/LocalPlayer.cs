using BabyStepsMultiplayerClient.Audio;
using BabyStepsMultiplayerClient.Extensions;
using BabyStepsMultiplayerClient.Networking;
using Il2Cpp;
using Il2CppCinemachine;
using MelonLoader;
using UnityEngine;
using static Il2CppTMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace BabyStepsMultiplayerClient.Player
{
    public class LocalPlayer : BasePlayer
    {
        public static LocalPlayer Instance;

        public PlayerMovement playerMovement;
        public GameObject cinemachineBrainObj;
        public CinemachineBrain cinemachineBrain;
        public Material pmSuitMaterial;

        public bool lastJiminyState;
        public Grabable lastLeftHandItem;
        public Grabable lastRightHandItem;
        public Hat lastHat;

        private bool _sentInitialState = false;

        private float lastBoneSendTime = 0f;
        private const float boneSendInterval = 0.033f;
        private const int bytesPerBone = 29;
        private const int bonesPerPacket = (NetworkManager.maxPacketSize - 4) / bytesPerBone;

        public BBSMicrophoneCapture mic;
        private bool micEnabled = false;
        private int micDevice = 0;
        public byte[] latestAudioFrame = null;
        private float _currentJawTarget = 0f;
        private bool _isSpeaking = false;
        private Camera.CameraCallback _onPreCullDelegate;
        private bool pushToTalkEnabled = false;
        private KeyCode pushToTalkKey = KeyCode.V;

        private LineOfSightManager lineOfSightManager;
        private float lastGazableUpdateTime = 0f;
        private const float gazableUpdateInterval = 0.1f; // Update every 100ms

        public override void Initialize()
        {
            Core.DebugMsg("Starting LocalPlayer Initialize function");

            baseObject = GameObject.Find("Dudest");
            if (baseObject == null) Core.DebugMsg("Failed to find Dudest gO!");
            playerMovement = baseObject.GetComponent<PlayerMovement>();
            if (playerMovement == null) Core.DebugMsg("Failed to find PlayerMovement component!");
            else pmSuitMaterial = playerMovement.suitMat;

            cinemachineBrainObj = GameObject.Find("BigManagerPrefab/Camera");
            if (cinemachineBrainObj == null) Core.DebugMsg("Failed to find cinemachineBrainObj!");
            else cinemachineBrain = cinemachineBrainObj.GetComponent<CinemachineBrain>();
            if (cinemachineBrain == null) Core.DebugMsg("Failed to find CinemachineBrain Component!");

            // Base Initialization
            base.Initialize();
            SetupBonesAndMaterials();
            ApplySuitColor();

            latestAudioFrame = new byte[0];
            mic = new BBSMicrophoneCapture();
            mic.Initialize(micDevice);
            if (micEnabled) mic.StartRecording();

            SetPushToTalkEnabled(ModSettings.audio.PushToTalk.Value);

            _onPreCullDelegate = new Action<Camera>(OnCameraPreCull);
            Camera.onPreCull += _onPreCullDelegate;

            if (headBone != null)
            {
                lineOfSightManager = new LineOfSightManager(headBone);
            }

            Core.DebugMsg("LocalPlayer Initialized");
        }

        public override void Dispose()
        {
            if (_onPreCullDelegate != null)
            {
                Camera.onPreCull -= _onPreCullDelegate;
                _onPreCullDelegate = null;
            }

            SetSuitColor(Color.white);
            ResetSuitColor();

            if (mic != null) mic.Dispose();
            mic = null;

            lineOfSightManager = null;
        }

        private void OnCameraPreCull(Camera cam)
        {
            if (_isSpeaking)
            {
                // Force the jaw to the calculated position
                SetMouthOpen(_currentJawTarget);
            }
            // Doesn't force close here if not speaking, allowing the FaceActor to blink normally.
        }

        private void UpdateMicrophone()
        {
            if (mic == null || !micEnabled) return;

            // Only Record when Connected to Server
            if ((Core.networkManager == null) || Core.networkManager.server == null)
            {
                if (mic.IsRecording()) mic.StopRecording();
            }
            else
            {
                if (!mic.IsRecording()) mic.StartRecording();
            }
        }
        public void SetMicrophoneEnabled(bool state)
        {
            micEnabled = state;
            if (state)
            {
                if (!mic.IsInitialized()) { mic.Initialize(micDevice); mic.StartRecording(); }
                else mic.StartRecording();
            }
            else
            {
                if (mic.IsInitialized()) mic.StopRecording(); 
            }
        }
        public bool IsMicrophoneEnabled() { return micEnabled; }
        public void SetPushToTalkEnabled(bool state) { pushToTalkEnabled = state; }
        public bool IsPushToTalkEnabled() { return pushToTalkEnabled; }
        public void SetPushToTalkKey(KeyCode key) { pushToTalkKey = key; }
        public KeyCode GetPushToTalkKey() { return pushToTalkKey; }
        public void SetMicrophoneDevice(int deviceIndex)
        {
            micDevice = deviceIndex;
            if (mic != null && mic.IsInitialized())
            {
                bool wasRecording = mic.IsRecording();
                mic.Dispose();
                mic = new BBSMicrophoneCapture();
                mic.Initialize(micDevice);
                if (wasRecording) mic.StartRecording();
            }
        }
        public int GetMicrophoneDevice() { return micDevice; }
        public GameObject GetCameraObject()
        {
            if (cinemachineBrain == null) return null;

            var virtualCam = cinemachineBrain.ActiveVirtualCamera;
            if (virtualCam == null) return null;

            var virtualCamObj = virtualCam.VirtualCameraGameObject;
            if (virtualCamObj == null) return null;

            return virtualCamObj;
        }

        public void ApplySuitColor()
        {
            Color newColor = ModSettings.player.SuitColor.Value;
            if (newColor.a != 1f) newColor.a = 1f;
            SetSuitColor(newColor);
        }
        public override void SetSuitColor(Color color)
        {
            base.SetSuitColor(color);

            if ((pmSuitMaterial != null)
                && (pmSuitMaterial.color != playerColor))
                pmSuitMaterial.color = playerColor;
        }

        public void CleanCompletely()
        {
            if (playerMovement == null) return;
            playerMovement.CleanPlayerCompletely();
        }

        public override void Update()
        {
            UpdateMicrophone();

            if (Core.networkManager.server == null) return;
            if (playerMovement == null) return;

            if (!_sentInitialState)
            {
                Hat hat = playerMovement.currentHat;
                if (hat != null) Core.networkManager.SendDonHat(hat);
                lastHat = hat;

                Grabable rightItem = playerMovement.handItems[0];
                Grabable leftItem = playerMovement.handItems[1];
                lastLeftHandItem = leftItem; lastRightHandItem = rightItem;

                if (rightItem != null)
                    Core.networkManager.SendHoldGrabable(rightItem, 0);
                if (leftItem != null)
                    Core.networkManager.SendHoldGrabable(leftItem, 1);

                Core.networkManager.SendJiminyRibbonState(lastJiminyState);

                Core.networkManager.SendCollisionToggle(ModSettings.player.Collisions.Value);

                _sentInitialState = true;
            }
            else
            {
                if (jiminyRibbon == null) { lastJiminyState = false; }
                else if (lastJiminyState != jiminyRibbon.active)
                {
                    lastJiminyState = jiminyRibbon.active;
                    Core.networkManager.SendJiminyRibbonState(lastJiminyState);
                }

                var leftItem = playerMovement.handItems[1];
                var rightItem = playerMovement.handItems[0];
                if (lastLeftHandItem != leftItem)
                {
                    lastLeftHandItem = leftItem;
                    if (leftItem != null)
                    {
                        Core.DebugMsg("Left Hand Pickup!");
                        Core.networkManager.SendHoldGrabable(leftItem, 1);
                    }
                    else
                    {
                        Core.DebugMsg("Left Hand Drop!");
                        Core.networkManager.SendDropGrabable(1);
                    }
                }
                if (lastRightHandItem != rightItem)
                {
                    lastRightHandItem = rightItem;
                    if (rightItem != null)
                    {
                        Core.DebugMsg("Right Hand Pickup!");
                        Core.networkManager.SendHoldGrabable(rightItem, 0);
                    }
                    else
                    {
                        Core.DebugMsg("Right Hand Drop!");
                        Core.networkManager.SendDropGrabable(0);
                    }
                }

                var hat = playerMovement.currentHat;
                if (lastHat != hat)
                {
                    lastHat = hat;
                    if (hat != null)
                    {
                        Core.DebugMsg("Hat Don!");
                        Core.networkManager.SendDonHat(hat);
                    }
                    else
                    {
                        Core.DebugMsg("Hat Doff!");
                        Core.networkManager.SendDoffHat();
                    }
                }
            }
        }

        public override void LateUpdate()
        {
            if (!_sentInitialState) return;
            if (Core.networkManager.server == null) return;
            if (playerMovement == null) return;
            if (boneChildren == null) return;

            if (IsMicrophoneEnabled() != ModSettings.audio.MicrophoneEnabled.Value)
                SetMicrophoneEnabled(ModSettings.audio.MicrophoneEnabled.Value);

            if (GetMicrophoneDevice() != ModSettings.audio.SelectedMicrophoneIndex.Value)
                SetMicrophoneDevice(ModSettings.audio.SelectedMicrophoneIndex.Value);

            if (micEnabled && mic != null && mic.IsRecording())
            {
                bool shouldTransmit = true;
                if (pushToTalkEnabled) shouldTransmit = Input.GetKey(pushToTalkKey);

                if (shouldTransmit)
                {
                    latestAudioFrame = mic.GetOpusPacket();
                    if (latestAudioFrame != null && latestAudioFrame.Length > 0)
                    {
                        Core.networkManager.SendAudioFrame(latestAudioFrame);

                        _currentJawTarget = mic.GetAmplitude();
                        _isSpeaking = true;

                        SetMouthOpen(_currentJawTarget);
                    }
                    else _isSpeaking = false;
                }
                else _isSpeaking = false;
            }
            else _isSpeaking = false;

            // Update line of sight for Gazable components
            UpdateLineOfSight();

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;
            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(boneChildren);
            Core.networkManager.SendBones(bonesToSend, 0);
        }

        private void UpdateLineOfSight()
        {
            if (lineOfSightManager == null)
                return;

            // Throttle updates to avoid excessive checks
            if (Time.realtimeSinceStartup - lastGazableUpdateTime < gazableUpdateInterval)
                return;

            lastGazableUpdateTime = Time.realtimeSinceStartup;

            foreach (var remotePlayer in RemotePlayer.GlobalPool)
            {
                if (remotePlayer == null || remotePlayer.headBone == null || remotePlayer.gazable == null)
                    continue;

                Vector3 targetPosition = remotePlayer.headBone.position;
                bool isVisible = lineOfSightManager.CanSee(targetPosition);

                if (remotePlayer.gazable.enabled != isVisible)
                {
                    remotePlayer.gazable.enabled = isVisible;
                }
            }
        }

        public LineOfSightManager GetLineOfSightManager() => lineOfSightManager;
    }
}
