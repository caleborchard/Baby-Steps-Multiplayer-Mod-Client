﻿using BabyStepsMultiplayerClient.Extensions;
using BabyStepsMultiplayerClient.Networking;
using Il2Cpp;
using Il2CppCinemachine;
using UnityEngine;
using BabyStepsMultiplayerClient.Audio;

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

#if DEBUG
        private BBSMicrophoneCapture mic;
#endif

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

#if DEBUG
            mic = new BBSMicrophoneCapture();
            mic.Initialize(0);
            mic.SetVolume(1f);
            mic.StartRecording();
#endif

            Core.DebugMsg("LocalPlayer Initialized");
        }

        public override void Dispose()
        {
            SetSuitColor(Color.white);
            ResetSuitColor();

#if DEBUG
            if (mic != null)
                mic.Dispose();
            mic = null;
#endif
        }

#if DEBUG
        private void UpdateMicrophone()
        {
            if (mic == null)
                return;

            // Only Record when Connected to Server
            if ((Core.networkManager == null)
                || Core.networkManager.server == null)
            {
                if (mic.IsRecording())
                    mic.StopRecording();
            }
            else
            {
                if (!mic.IsRecording())
                    mic.StopRecording();
            }
        }

        public void WriteMicrophoneToAudioSource(BBSAudioSource audioSource)
        {
            if ((audioSource == null)
                || (mic == null)
                || !mic.IsInitialized()
                || !mic.IsRecording()) 
                return;

            byte[] frame = mic.GetAudioFrame();
            if (frame != null)
            {
                byte[] stereo = mic.ConvertToStereo(frame);
                audioSource.WriteAudioData(stereo);
            }
        }
#endif

        public GameObject GetCameraObject()
        {
            if (cinemachineBrain == null)
                return null;

            var virtualCam = cinemachineBrain.ActiveVirtualCamera;
            if (virtualCam == null) 
                return null;

            var virtualCamObj = virtualCam.VirtualCameraGameObject;
            if (virtualCamObj == null)
                return null;

            return virtualCamObj;
        }

        public void ApplySuitColor()
        {
            Color newColor = ModSettings.player.SuitColor.Value;
            if (newColor.a != 1f)
                newColor.a = 1f;
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
            if (playerMovement == null)
                return;
            playerMovement.CleanPlayerCompletely();
        }

        public void RestoreHat(Hat hat)
        {
            if (hat == null)
                return;
            if (playerMovement == null)
                return;
            playerMovement.StartCoroutine(RestoreHatDelayed(hat));
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
#if DEBUG
            UpdateMicrophone();
#endif

            if (Core.networkManager.server == null)
                return;
            if (playerMovement == null)
                return;

            if (!_sentInitialState)
            {
                Hat hat = playerMovement.currentHat;
                if (hat != null)
                    Core.networkManager.SendDonHat(hat);
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

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;

            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(boneChildren);
            Core.networkManager.SendBones(bonesToSend, 0);
            /*
            for (int i = 0; i < bonesToSend.Length; i += bonesPerPacket)
            {
                int count = Math.Min(bonesPerPacket, bonesToSend.Length - i);
                TransformNet[] chunk = new TransformNet[count];
                Array.Copy(bonesToSend, i, chunk, 0, count);
                Core.networkManager.SendBones(chunk, i);
            }
            */
        }
    }
}
