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

        private BBSMicrophoneCapture mic;
        private bool micEnabled = false;
        private float micVolume = 1f;
        public int micDevice = 0;

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

            mic = new BBSMicrophoneCapture();
            mic.Initialize(micDevice);
            mic.SetVolume(micVolume);
            if (micEnabled) mic.StartRecording();

            Core.DebugMsg("LocalPlayer Initialized");
        }

        public override void Dispose()
        {
            SetSuitColor(Color.white);
            ResetSuitColor();

            if (mic != null) mic.Dispose();
            mic = null;
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
                if (!mic.IsInitialized()) { mic.Initialize(micDevice); mic.SetVolume(micVolume); mic.StartRecording(); }
                else mic.StartRecording();
            }
            else
            {
                if (mic.IsInitialized()) mic.StopRecording(); 
            }
        }
        public void SetMicrophoneVolume(float volume)
        {
            micVolume = volume;
            if (mic != null) mic.SetVolume(volume);
        }
        public bool IsMicrophoneEnabled() { return micEnabled; }
        public float GetMicrophoneVolume() { return micVolume; }

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
        private System.Collections.IEnumerator RestoreHatDelayed(Hat hat)
        {
            // Delay
            yield return null;

            if (hat != null
                && playerMovement != null)
            {
                playerMovement.KnockOffHat();
                playerMovement.WearHat(hat);
            }
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

            if (micEnabled && mic != null && mic.IsRecording())
            {
                byte[] audioFrame = mic.GetOpusPacket();
                if (audioFrame != null && audioFrame.Length > 0)
                {
                    Core.networkManager.SendAudioFrame(audioFrame);
                }
            }

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;
            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(boneChildren);
            Core.networkManager.SendBones(bonesToSend, 0);
        }
    }
}
