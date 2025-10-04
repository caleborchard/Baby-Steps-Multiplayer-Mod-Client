using BabyStepsMultiplayerClient.Extensions;
using BabyStepsMultiplayerClient.Networking;
using Il2Cpp;
using Il2CppCinemachine;
using UnityEngine;

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

        private bool _sentInitialState = false;

        private float lastBoneSendTime = 0f;
        private const float boneSendInterval = 0.033f;
        private const int bytesPerBone = 29;
        private const int bonesPerPacket = (NetworkManager.maxPacketSize - 4) / bytesPerBone;

        public override void Initialize()
        {
            Core.DebugMsg("Starting LocalPlayer Initialize function");

            baseObject = GameObject.Find("Dudest");
            playerMovement = baseObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
                pmSuitMaterial = playerMovement.suitMat;

            cinemachineBrainObj = GameObject.Find("BigManagerPrefab/Camera");
            if (cinemachineBrainObj != null)
                cinemachineBrain = cinemachineBrainObj.GetComponent<CinemachineBrain>();

            // Base Initialization
            base.Initialize();
            SetupBonesAndMaterials();
            ApplySuitColor();

            Core.DebugMsg("Variable sets finished");
        }

        public override void Dispose()
        {
            SetSuitColor(Color.white);
            ResetSuitColor();
        }

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
            if (Core.networkManager.server == null)
                return;
            if (playerMovement == null)
                return;

            if (!_sentInitialState)
            {
                Hat hat = playerMovement.currentHat;
                if (hat != null)
                    Core.networkManager.SendDonHat(hat);

                Grabable rightItem = playerMovement.handItems[0];
                Grabable leftItem = playerMovement.handItems[1];

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
                if (jiminyRibbon == null)
                {
                    lastJiminyState = false;
                    return;
                }

                if (lastJiminyState == jiminyRibbon.active)
                    return;

                lastJiminyState = jiminyRibbon.active;
                Core.networkManager.SendJiminyRibbonState(lastJiminyState);
            }
        }

        public override void LateUpdate()
        {
            if (!_sentInitialState)
                return;
            if (Core.networkManager.server == null)
                return;
            if (playerMovement == null)
                return;
            if (boneChildren == null)
                return;

            if (Time.realtimeSinceStartup - lastBoneSendTime < boneSendInterval) return;

            lastBoneSendTime = Time.realtimeSinceStartup;

            var bonesToSend = TransformNet.ToNet(boneChildren);
            for (int i = 0; i < bonesToSend.Length; i += bonesPerPacket)
            {
                int count = Math.Min(bonesPerPacket, bonesToSend.Length - i);
                TransformNet[] chunk = new TransformNet[count];
                Array.Copy(bonesToSend, i, chunk, 0, count);
                Core.networkManager.SendBones(chunk, i);
            }
        }
    }
}
