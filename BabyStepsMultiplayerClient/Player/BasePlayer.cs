using BabyStepsMultiplayerClient.Extensions;
using Il2Cpp;
using UnityEngine;

namespace BabyStepsMultiplayerClient.Player
{
    public class BasePlayer
    {
        public GameObject baseObject;
        public Transform baseMesh;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public GameObject jiminyRibbon;
        public MeshRenderer nateGlasses;

        public Transform particleHouse;
        public Transform particleCrushers;
        public ParticleParty particleParty;

        public Material hairMaterial;
        public Material suitMaterial;

        public Color baseColor = Color.white;
        public Color playerColor = Color.white;

        public List<Transform> boneChildren;
        public List<Transform> puppetBones;
        public Transform puppetMaster;

        public Transform headBone;
        public Transform rootBone;
        public Transform spineBone;

        public (Transform, Transform) handBones;
        public (Transform, Transform) footBones;

        public (Transform, Transform) eyeLids;
        public (Transform, Transform) eyeBalls;
        public (SkinnedMeshRenderer, SkinnedMeshRenderer) eyeBallRenderers;

        public Dictionary<string, Transform> boneMudMeshes;

        public virtual void Initialize()
        {
            if (baseObject == null)
                return;

            if (baseMesh == null)
                baseMesh = baseObject.transform.Find("IKTargets/HipTarget/NathanAnimIK_October2022");

            if (baseMesh != null)
            {
                if (skinnedMeshRenderer == null)
                    skinnedMeshRenderer = baseMesh.Find("Nathan.001").GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    var materialsArray = skinnedMeshRenderer.sharedMaterials;
                    if (hairMaterial == null)
                        hairMaterial = materialsArray.FirstOrDefault(m => m.name.Contains("Hair2_lux"));

                    materialsArray = skinnedMeshRenderer.materials;
                    if (suitMaterial == null)
                        suitMaterial = materialsArray.FirstOrDefault(m => m.name.Contains("NewSuit_Oct22"));
                    if (suitMaterial != null)
                    {
                        baseColor = suitMaterial.color;
                        playerColor = baseColor;
                    }
                }

                if (particleHouse == null)
                    particleHouse = baseObject.transform.Find("ParticleHouse");
                if (particleHouse != null)
                {
                    if (particleCrushers == null)
                        particleCrushers = particleHouse.Find("Crushers");
                    if (particleParty == null)
                        particleParty = particleHouse.GetComponent<ParticleParty>();
                }

                if (eyeLids.Item1 == null)
                    eyeLids.Item1 = baseMesh.transform.FindChild("Nathan_EyeLOuter");
                if (eyeLids.Item2 == null)
                    eyeLids.Item2 = baseMesh.transform.FindChild("Nathan_EyeROuter");

                if (eyeBalls.Item1 == null)
                    eyeBalls.Item1 = baseMesh.transform.FindChild("Nathan_EyeLInner");
                if (eyeBalls.Item1 != null)
                    eyeBallRenderers.Item1 = eyeBalls.Item1.GetComponent<SkinnedMeshRenderer>();

                if (eyeBalls.Item2 == null)
                    eyeBalls.Item2 = baseMesh.transform.FindChild("Nathan_EyeRInner");
                if (eyeBalls.Item2 != null)
                    eyeBallRenderers.Item2 = eyeBalls.Item2.GetComponent<SkinnedMeshRenderer>();

                if (jiminyRibbon == null)
                {
                    Transform ribbonTransform = baseMesh.FindChildByKeyword("JiminysCricketsRibbon");
                    if (ribbonTransform != null)
                        jiminyRibbon = ribbonTransform.gameObject;
                }
            }

            if (puppetMaster == null)
                puppetMaster = baseObject.transform.FindChild("PuppetMaster");

            if (skinnedMeshRenderer != null)
            {
                if (boneMudMeshes == null)
                    boneMudMeshes = new();

                boneChildren = ExtractValidBones(skinnedMeshRenderer.bones, baseMesh).ToList();
                if (boneChildren != null)
                {
                    if (puppetMaster != null)
                    {
                        if (puppetBones == null)
                            puppetBones = puppetMaster.FindMatchingChildren(boneChildren);
                        if (puppetBones != null)
                            foreach (var bone in puppetBones)
                            {
                                if (bone == null)
                                    continue;

                                string boneName = bone.name;
                                if (!boneMudMeshes.ContainsKey(boneName))
                                {
                                    var mudMesh = bone.FindChildByKeyword("mudMesh");
                                    if (mudMesh != null)
                                        boneMudMeshes[boneName] = mudMesh;
                                }
                            }
                    }

                    foreach (var bone in boneChildren)
                    {
                        if (bone == null)
                            continue;

                        string boneName = bone.name;
                        switch (boneName)
                        {
                            case "head.x":
                                if (headBone == null)
                                    headBone = bone;
                                goto default;

                            case "root.x":
                                if (rootBone == null)
                                    rootBone = bone;
                                goto default;

                            case "spine_02.x":
                                if (spineBone == null)
                                    spineBone = bone;
                                goto default;

                            case "hand.r":
                                if (handBones.Item1 == null)
                                    handBones.Item1 = bone;
                                goto default;

                            case "hand.l":
                                if (handBones.Item2 == null)
                                    handBones.Item2 = bone;
                                goto default;

                            case "foot.l":
                                if (footBones.Item1 == null)
                                    footBones.Item1 = bone;
                                goto default;

                            case "foot.r":
                                if (footBones.Item2 == null)
                                    footBones.Item2 = bone;
                                goto default;

                            default:
                                break;
                        }
                    }
                }
            }

            if (headBone != null
                && nateGlasses == null)
            {
                var gBone = headBone.FindChild("Nathan_Glasses");
                if (gBone != null)
                    nateGlasses = gBone.GetComponent<MeshRenderer>();
            }
        }

        public virtual void Dispose() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }

        public Texture2D CloneSuitTexture()
        {
            if (suitMaterial == null)
                return null;

            Texture ogAlbedo = suitMaterial.GetTexture("_AlbedoMap");
            Texture2D t2d = new Texture2D(ogAlbedo.width, ogAlbedo.height, TextureFormat.ARGB32, false);
            RenderTexture rt = new RenderTexture(ogAlbedo.width, ogAlbedo.height, 0);

            RenderTexture cRT = RenderTexture.active;
            Graphics.Blit(ogAlbedo, rt);
            RenderTexture.active = rt;
            t2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t2d.Apply();
            RenderTexture.active = cRT;
            rt.Release();

            return t2d;
        }

        public void SetHairHat(float hatMax, Vector4 hatUp)
        {
            if (hairMaterial == null)
                return;
            hairMaterial.SetFloat("_HatMax", hatMax);
            hairMaterial.SetVector("_HatUp", hatUp);
        }

        public Color GetSuitColor()
            => playerColor;
        public virtual void SetSuitColor(Color color)
        {
            playerColor = color;

            if ((suitMaterial != null)
                && (suitMaterial.color != playerColor))
                suitMaterial.color = playerColor;
        }
        public void ResetSuitColor()
        {
            if (suitMaterial == null)
                return;
            suitMaterial.color = baseColor;
        }

        public void FadeByDistance(float distance)
        {
            if (baseObject == null)
                return;

            float fadeStart = 1.3f;
            float fadeEnd = 0.7f;
            float alpha = Mathf.Clamp01((distance - fadeEnd) / (fadeStart - fadeEnd));

            SetOpacity(alpha);
        }

        public virtual void SetOpacity(float opacity)
        {
            if (skinnedMeshRenderer != null)
            {
                var materials = skinnedMeshRenderer.materials;
                for (int i = 0; i < materials.Count; i++)
                {
                    var mat = materials[i];
                    if (mat.HasProperty("_DitherAlpha"))
                        mat.SetFloat("_DitherAlpha", opacity);
                    else if (mat.HasProperty("_Cutoff"))
                        mat.SetFloat("_Cutoff", Mathf.Lerp(1.05f, 0.12f, opacity * opacity));
                }
            }

            if (nateGlasses != null)
                nateGlasses.materials[1].SetFloat("_DitherAlpha", opacity);

            if (eyeBallRenderers.Item1 != null
                && eyeBallRenderers.Item1.material != null)
            {
                Color lC = eyeBallRenderers.Item1.material.GetColor("_Color");
                lC.a = opacity;
                eyeBallRenderers.Item1.material.SetColor("_Color", lC);
            }

            if (eyeBallRenderers.Item2 != null
                && eyeBallRenderers.Item2.material != null)
            {
                Color lC = eyeBallRenderers.Item2.material.GetColor("_Color");
                lC.a = opacity;
                eyeBallRenderers.Item2.material.SetColor("_Color", lC);
            }
        }

        private static Transform[] ExtractValidBones(Transform[] meshBones, Transform meshRoot)
        {
            Transform[] bones = new Transform[meshBones.Length];
            HashSet<Transform> meshBoneSet = new HashSet<Transform>(meshBones);
            Queue<Transform> queue = new Queue<Transform>();

            Transform root = meshRoot.Find("root/root.x");
            queue.Enqueue(root);
            int boneIndex = 0;

            while (queue.Count > 0 && boneIndex < bones.Length)
            {
                Transform current = queue.Dequeue();

                if (!IsBoneNameValid(current.name)) continue;

                if (meshBoneSet.Contains(current))
                {
                    bones[boneIndex++] = current;
                }

                if (current.name != "ORG-face" || current.name.Contains("hand") || current.name.Contains("foot"))
                {
                    for (int i = 0; i < current.childCount; i++)
                    {
                        Transform child = current.GetChild(i);
                        if (meshBoneSet.Contains(child)) queue.Enqueue(child);
                    }
                }
            }

            if (boneIndex < bones.Length)
                Array.Resize(ref bones, boneIndex);

            return bones;
        }
        private static bool IsBoneNameValid(string name)
        {
            if (name == null) return false;

            string lowerName = name.ToLowerInvariant();
            string[] forbiddenSubstrings = { "belly", "butt", "twist", "boob", "backfat" };

            foreach (string forbidden in forbiddenSubstrings) if (lowerName.Contains(forbidden)) return false;

            return !string.Equals(name, "JiminysCricketsRibbon");
        }

        public void ResetBonesToBind()
        {
            if (skinnedMeshRenderer == null)
                return;
            if (LocalPlayer.Instance.skinnedMeshRenderer == null)
                return;

            Transform[] meshBones = skinnedMeshRenderer.bones;
            Matrix4x4[] bindPoses = LocalPlayer.Instance.skinnedMeshRenderer.sharedMesh.bindposes;
            Transform rootBone = skinnedMeshRenderer.rootBone;

            for (int i = 0; i < meshBones.Length; i++)
            {
                Transform bone = meshBones[i];
                Matrix4x4 bindPoseInverse = bindPoses[i].inverse;

                Vector3 position;
                Quaternion rotation;

                if (bone.parent == null)
                {
                    // Root bone: bind pose is already in mesh space
                    position = bindPoseInverse.GetColumn(3);
                    rotation = Quaternion.LookRotation(
                        bindPoseInverse.GetColumn(2),
                        bindPoseInverse.GetColumn(1)
                    );
                }
                else
                {
                    // Convert bind pose from mesh space to parent local space
                    Matrix4x4 localBindPose = bone.parent.worldToLocalMatrix * rootBone.localToWorldMatrix * bindPoseInverse;

                    position = localBindPose.GetColumn(3);
                    rotation = Quaternion.LookRotation(
                        localBindPose.GetColumn(2),
                        localBindPose.GetColumn(1)
                    );
                }

                bone.localPosition = position;
                bone.localRotation = rotation;
            }
        }
    }
}
