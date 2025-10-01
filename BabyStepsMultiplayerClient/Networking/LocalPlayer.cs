using BabyStepsMultiplayerClient.Debug;
using Il2Cpp;
using Il2CppCinemachine;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Networking
{
    public class LocalPlayer
    {
        private GameObject jiminyRibbon;
        public bool lastJiminyState;

        // --- Material & Bone Info ---
        public Transform headBone;
        public (Transform, Transform) handBones;
        public Transform[] sortedBones;

        // --- Scene References ---
        public GameObject basePlayer;
        public PlayerMovement basePlayerMovement;
        public Transform baseMesh;
        public Material baseMaterial;
        public Color baseColor;
        public Transform camera;
        public CinemachineBrain cinemachineBrain;

        public Transform particleHouse;
        public ParticleParty particleParty;

        public void Update()
        {
            if (jiminyRibbon == null)
            {
                lastJiminyState = false;
                return;
            }

            if (lastJiminyState == jiminyRibbon.active) return;

            lastJiminyState = jiminyRibbon.active;
            Core.networkManager.SendJiminyRibbonState(lastJiminyState);
        }

        public void ApplyColor()
        {
            baseColor = new Color(Core.uiManager.serverConnectUI.uiColorR, Core.uiManager.serverConnectUI.uiColorG, Core.uiManager.serverConnectUI.uiColorB);
            baseMaterial.color = baseColor;
        }

        public void OnDisconnect()
        {
            baseColor = Color.white;
            baseMaterial.color = baseColor;
        }

        public void OnConnect()
        {

            BBSMMdBug.Log("Starting LocalPlayer OnConnect function");

            if (basePlayer == null) basePlayer = GameObject.Find("Dudest");
            if (basePlayerMovement == null) basePlayerMovement = basePlayer.GetComponent<PlayerMovement>();
            if (baseMesh == null) baseMesh = basePlayer.transform.Find("IKTargets/HipTarget/NathanAnimIK_October2022");
            if (camera == null) camera = basePlayer.transform.Find("GameCam");
            if (cinemachineBrain == null) cinemachineBrain = GameObject.Find("BigManagerPrefab/Camera").GetComponent<CinemachineBrain>();
            if (baseMaterial == null)
                baseMaterial = baseMesh.Find("Nathan.001").GetComponent<SkinnedMeshRenderer>().sharedMaterials.FirstOrDefault(m => m.name.Contains("NewSuit_Oct22"));
            if (jiminyRibbon == null) jiminyRibbon = basePlayerMovement.jiminyRibbon;
            if (particleHouse == null) particleHouse = basePlayer.transform.Find("ParticleHouse");
            if (particleParty == null) particleParty = particleHouse.GetComponent<ParticleParty>();

            BBSMMdBug.Log("Variable sets finished");

            if (RemotePlayer.suitTexture == null)
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

                RemotePlayer.suitTexture = t2d;
            }

            if (sortedBones == null)
            {
                sortedBones = RemotePlayer.ExtractValidBones(baseMesh.Find("Nathan.001").GetComponent<SkinnedMeshRenderer>().bones, baseMesh);
            }

            for (int i = 0; i < sortedBones.Length; i++)
            {
                if (sortedBones[i].name == "head.x") { headBone = sortedBones[i]; continue; }
                else if (sortedBones[i].name == "hand.r") { handBones.Item1 = sortedBones[i]; continue; }
                else if (sortedBones[i].name == "hand.l") { handBones.Item2 = sortedBones[i]; continue; }
            }
        }
    }
}
