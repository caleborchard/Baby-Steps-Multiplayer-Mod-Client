using Il2Cpp;
using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppInterop.Runtime;
using Il2CppNWH.DWP2.WaterData;
using Il2CppNWH.DWP2.WaterObjects;
using Il2CppSystem.Linq;
using Il2CppTMPro;
using MelonLoader;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BabyStepsMultiplayerClient
{
    public class RbMap
    {
        public Rigidbody mainRb;
        public WaterObject waterObject;
        public Vector3 previousPosition;
        public Quaternion previousRotation;

        public RbMap(Rigidbody _mainRb, WaterObject _waterObject)
        { mainRb = _mainRb; waterObject = _waterObject; }
    }

    public class NateMP
    {
        // --- Mainline ---
        public static Queue<NateMP> nateRecycler = new();

        public GameObject baseObj;
        public Transform mesh;
        public Transform crushers;
        public Color color;

        private Transform[] bones;
        private Dictionary<Transform, RbMap> boneToRbMap;
        private bool[] bonesUpdated;
        public FootData[] feet;
        private (Transform, Transform) hands;
        private List<CapsuleCollider> capsuleColliders;
        private List<(Transform, Transform)> boneToCrusherList;
        private bool collidersHaveBeenEnabled = false;

        private Vector3[] previousBonePositions;
        private Quaternion[] previousBoneRotations;
        private Vector3[] targetBonePositions;
        private Quaternion[] targetBoneRotations;
        private float boneLerpTimer = 0f;
        public float boneLerpDuration = 0.033f;
        public float lastBonePacketTime = 0f;

        public string displayName;
        public GameObject textObj;
        private TextMeshPro textMesh;
        private Material suitMaterial;
        private Material hairMaterial;
        private SkinnedMeshRenderer skinnedMeshRenderer;

        public static Texture2D suitTexture;

        private SkinnedMeshRenderer lEyeball;
        private SkinnedMeshRenderer rEyeball;
        private MeshRenderer glasses;

        public Hat hat;
        public (Grabable, Grabable) heldItems;

        private static Dictionary<string, GameObject> savablePrefabs = new Dictionary<string, GameObject>();

        public NateMP(GameObject basePlayer, Transform baseMesh, int numClones, ConcurrentQueue<Action> mainThreadActions)
        {
            PlayerMovement basePlayerMovement = basePlayer.GetComponent<PlayerMovement>();

            baseObj = new GameObject($"NateClone{numClones}");

            mesh = GameObject.Instantiate(baseMesh);
            mesh.name = "NateMesh";
            mesh.parent = baseObj.transform;
            MelonCoroutines.Start(DelayedComponentStrip(mesh));

            SetupMaterials();
            SetupEyes();
            SetupCrushers(basePlayer);
            SetupBonesAndColliders(basePlayer, baseMesh);
        }

        // --- Runtime Update (Mainline) ---
        public void UpdateBones(TransformNet[] bonesToUpdate, int kickoffPoint)
        {
            for (int i = kickoffPoint; i < bonesToUpdate.Length; i++)
            {
                var bone = bonesToUpdate[i];
                var currentCBone = bones[i];
                if (currentCBone == null) break;

                previousBonePositions[i] = currentCBone.position;
                previousBoneRotations[i] = currentCBone.rotation;

                Vector3 targetPos = new Vector3(bone.position.X, bone.position.Y, bone.position.Z);
                Quaternion targetRot = new Quaternion(bone.rotation.X, bone.rotation.Y, bone.rotation.Z, bone.rotation.W);
                targetBonePositions[i] = targetPos;
                targetBoneRotations[i] = targetRot;

                bonesUpdated[i] = true;
            }

            boneLerpTimer = 0f;

            if (!collidersHaveBeenEnabled) 
            { 
                foreach (CapsuleCollider cc in capsuleColliders) { cc.enabled = true; } collidersHaveBeenEnabled = true; 

            }
        }

        public void InterpolateBoneTransforms(float deltaTime)
        {
            if (bones == null) return;

            boneLerpTimer += deltaTime;
            float t = Mathf.Clamp01(boneLerpTimer / boneLerpDuration);

            t = t * t * (3f - 2f * t); // Cubic Hermite Smoothstep easing

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                if (!bonesUpdated[i]) continue;

                Vector3 interpolatedPos = Vector3.Lerp(previousBonePositions[i], targetBonePositions[i], t);
                Quaternion interpolatedRot = Quaternion.Slerp(previousBoneRotations[i], targetBoneRotations[i], t);

                bones[i].position = interpolatedPos;
                bones[i].rotation = interpolatedRot;

                if (boneToRbMap.TryGetValue(bones[i], out var rbMap))
                {
                    Rigidbody rb = rbMap.mainRb;
                    Transform tr = rb.transform;

                    rb.isKinematic = false;

                    Vector3 delta = rbMap.mainRb.transform.position - rbMap.previousPosition;
                    if (delta.sqrMagnitude > Mathf.Epsilon)
                    {
                        Vector3 velocity = (delta / deltaTime);
                        rb.velocity = velocity;
                    }
                    rbMap.previousPosition = tr.position;

                    Quaternion deltaRotation = tr.rotation * Quaternion.Inverse(rbMap.previousRotation);
                    deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 axis);
                    if (angleInDegrees > 180f) angleInDegrees -= 360f;
                    if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
                    {
                        Vector3 angularVelocity = axis * angleInDegrees * Mathf.Deg2Rad / deltaTime;
                        rb.angularVelocity = angularVelocity * 0.2f;
                    }
                    rbMap.previousRotation = tr.rotation;

                    rbMap.waterObject.FixedUpdate();

                    rb.isKinematic = true;
                }
            }

            foreach ((Transform, Transform) b2c in boneToCrusherList)
            {
                b2c.Item2.position = b2c.Item1.position;
            }
        }

        public void RefreshNameAndColor()
        {
            suitMaterial.color = color;
            textMesh.text = displayName;
        }

        public void RotateNametagTowardsCamera(Transform camera)
        {
            textObj.transform.LookAt(camera);
            textObj.transform.Rotate(0, 180, 0);
        }

        public void HideNicknameOutsideDistance(Vector3 basePos, float distance)
        {
            float calcDist = Vector3.Distance(basePos, textObj.transform.position);
            if (calcDist > distance && textObj.gameObject.active) textObj.gameObject.active = false;
            else if (calcDist < distance && !textObj.gameObject.active) textObj.gameObject.active = true;
        }

        public void FadeByDistance(Transform cam)
        {
            float distance = Vector3.Distance(cam.position, textObj.transform.position);
            float fadeStart = 1.3f;
            float fadeEnd = 0.7f;
            float alpha = Mathf.Clamp01((distance - fadeEnd) / (fadeStart - fadeEnd));

            SetOpacity(alpha);
        }

        public void Destroy()
        {
            RemoveHat();
            DropItem(0); DropItem(1);

            baseObj.active = false;
            foreach (var b in bones)
            {
                b.position = Vector3.zero;
            }

            nateRecycler.Enqueue(this);
        }

        // --- Network ---
        public void HoldItem(string grabableName, int handIndex, Vector3 localPosition, Quaternion localRotation)
        {
            Grabable item;
            Transform hand;

            if (handIndex == 0) { item = heldItems.Item1; hand = hands.Item1; }
            else { item = heldItems.Item2; hand = hands.Item2; }

            if (item != null) DropItem(handIndex);

            if (!savablePrefabs.TryGetValue(grabableName, out GameObject prefab))
            {
                GameObject loaderLocations = GameObject.Find("BigManagerPrefab/GlobalObjectParent/Savables");

                Transform savableLoader = FindChildByKeyword(loaderLocations.transform, grabableName);

                GlobalObjectLoader gOL = savableLoader.GetComponent<GlobalObjectLoader>();

                AssetReference assetRef = gOL.loadee;

                prefab = assetRef.LoadAssetAsync<GameObject>().WaitForCompletion();
                savablePrefabs[grabableName] = prefab;
            }

            GameObject itemGO = GameObject.Instantiate(prefab, hand);
            Grabable grabable = itemGO.transform.GetComponentInChildren<Grabable>();

            grabable.grabable = false;
            grabable.rb.Destroy();

            grabable.transform.localPosition = localPosition;
            grabable.transform.localRotation = localRotation;

            if (handIndex == 0) { heldItems.Item1 = grabable; }
            else { heldItems.Item2 = grabable; }
        }

        public void DropItem(int handIndex)
        {
            if (handIndex == 0 && heldItems.Item1 != null)
            {
                heldItems.Item1.gameObject.Destroy();
                heldItems.Item1 = null;
            }
            else if (handIndex == 1 && heldItems.Item2 != null)
            {
                heldItems.Item2.gameObject.Destroy();
                heldItems.Item2 = null;
            }
        }

        public void WearHat(string hatName, Vector3 localPosition, Quaternion localRotation)
        {
            if (heldItems.Item1 != null && heldItems.Item1.name.Contains(hatName)) DropItem(0);
            if (heldItems.Item2 != null && heldItems.Item2.name.Contains(hatName)) DropItem(1);

            if (hat != null) RemoveHat();

            GameObject head = textObj.transform.parent.gameObject;

            if (!savablePrefabs.TryGetValue(hatName, out GameObject prefab))
            {
                GameObject loaderLocations = GameObject.Find("BigManagerPrefab/GlobalObjectParent/Savables");
                Transform hatLoader = FindChildByKeyword(loaderLocations.transform, hatName);
                GlobalObjectLoader gOL = hatLoader.GetComponent<GlobalObjectLoader>();
                AssetReference assetRef = gOL.loadee;

                prefab = assetRef.LoadAssetAsync<GameObject>().WaitForCompletion();
                savablePrefabs[hatName] = prefab;
            }

            GameObject hatGO = GameObject.Instantiate(prefab, head.transform);
            Hat hHat = hatGO.transform.GetComponentInChildren<Hat>();

            hHat.grabable = false;
            hHat.rb.Destroy();

            hatGO.transform.localPosition = localPosition;
            hatGO.transform.localRotation = localRotation;

            HairHatSetter(hHat.hairAmt, hHat.hairlineUpVec);

            hat = hHat;
        }

        public void RemoveHat()
        {
            if (hat != null)
            {
                hat.gameObject.Destroy();
                hat = null;

                HairHatSetter(1, new Vector4(0, 1, 1, 0));
            }
        }

        // --- Helpers ---
        private void HairHatSetter(float hatMax, Vector4 hatUp)
        {
            hairMaterial.SetFloat("_HatMax", hatMax);
            hairMaterial.SetVector("_HatUp", hatUp);
        }

        private void SetOpacity(float opacity)
        {
            var materials = skinnedMeshRenderer.materials;
            for (int i = 0; i < materials.Count; i++)
            {
                var mat = materials[i];
                if (mat.HasProperty("_DitherAlpha"))
                {
                    mat.SetFloat("_DitherAlpha", opacity);
                }
                else if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", Mathf.Lerp(1.05f, 0.12f, opacity * opacity));
            }

            glasses.materials[1].SetFloat("_DitherAlpha", opacity);

            Color lC = lEyeball.material.GetColor("_Color");
            lC.a = opacity;
            lEyeball.material.SetColor("_Color", lC);

            Color rC = rEyeball.material.GetColor("_Color");
            rC.a = opacity;
            rEyeball.material.SetColor("_Color", rC);
        }

        private void StdMatSetup(Material mat)
        {
            //mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // Makes the eyes look creepy!
        }

        private void MaterialKeywordHelper(Material mat)
        {
            mat.EnableKeyword("_DITHERCONSTANT");
            mat.EnableKeyword("_SPECULAR");
            mat.EnableKeyword("_CHEAPSSS");
            mat.EnableKeyword("_CHEAPSSSTEXTURE");
            mat.DisableKeyword("_ALPHACUT");
            mat.EnableKeyword("_NORMALMAP");

            mat.EnableKeyword("_MASKMAP");

            mat.EnableKeyword("_EMISSION");
            mat.EnableKeyword("_METALLICGLOSSMAP");
        }

        Transform FindChildByKeyword(Transform root, string keyword)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.name.Contains(keyword)) return current;

                for (int i = 0; i < current.childCount; i++) queue.Enqueue(current.GetChild(i));
            }

            return null;
        }

        List<Transform> FindMatchingChildren(Transform puppetMaster, List<Transform> bones)
        {
            var matches = new List<Transform>();
            var queue = new Queue<Transform>();
            var boneNames = new HashSet<string>();

            foreach (var bone in bones)
            {
                if (bone != null) boneNames.Add(bone.name);
            }

            queue.Enqueue(puppetMaster);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (boneNames.Contains(current.name)) matches.Add(current);

                for (int i = 0; i < current.childCount; i++)
                {
                    queue.Enqueue(current.GetChild(i));
                }
            }

            return matches;
        }

        public static Transform[] ExtractValidBones(Transform[] meshBones, Transform meshRoot)
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

        private System.Collections.IEnumerator DelayedComponentStrip(Transform mesh)
        {
            yield return null; // Delay

            foreach (var component in mesh.GetComponents<Component>())
            {
                if (!(component is Transform)) UnityEngine.Object.Destroy(component);
            }
        }

        private System.Collections.IEnumerator RestoreHat(Hat hat, PlayerMovement basePlayerMovement)
        {
            yield return null;

            if (hat != null)
            {
                basePlayerMovement.KnockOffHat();
                basePlayerMovement.WearHat(hat);
            }
        }

        // --- Constructor Helpers ---
        private void SetupMaterials()
        {
            skinnedMeshRenderer = mesh.FindChild("Nathan.001").GetComponent<SkinnedMeshRenderer>();
            var materialsArray = skinnedMeshRenderer.materials;

            for (int i = 0; i < materialsArray.Length; i++)
            {
                Material originalMat = materialsArray[i];
                if (originalMat.HasProperty("_DitherAlpha") && originalMat.shader.name != "Better Lit/DuderSuit") MaterialKeywordHelper(originalMat);
                else if (originalMat.name.Contains("NewSuit_Oct22"))
                {
                    originalMat.SetTexture("_AlbedoMap", suitTexture);
                    originalMat.SetTexture("_WetnessTexture", null);
                    originalMat.SetTexture("_RainDropTexture", null);

                    originalMat.color = color;
                    materialsArray[i] = originalMat;

                    suitMaterial = materialsArray[i];
                    break;
                }
                else if (originalMat.name.Contains("Hair2_lux")) // Set hair material hat values to default
                {
                    hairMaterial = originalMat;

                    HairHatSetter(1, new Vector4(0, 1, 1, 0));
                }
            }
            skinnedMeshRenderer.materials = materialsArray;
        }

        private void SetupEyes()
        {
            // Disable the shininess of the eyeballs that's leftover after fading
            mesh.transform.FindChild("Nathan_EyeLOuter")?.gameObject.SetActive(false);
            mesh.transform.FindChild("Nathan_EyeROuter")?.gameObject.SetActive(false);

            // Setup transparency materials for actual eyeball materials
            lEyeball = mesh.transform.FindChild("Nathan_EyeLInner")?.GetComponent<SkinnedMeshRenderer>();
            rEyeball = mesh.transform.FindChild("Nathan_EyeRInner")?.GetComponent<SkinnedMeshRenderer>();

            StdMatSetup(lEyeball.material);
            StdMatSetup(rEyeball.material);
        }

        private void SetupCrushers(GameObject basePlayer)
        {
            crushers = GameObject.Instantiate(basePlayer.transform.Find("ParticleHouse/Crushers"));
            crushers.parent = baseObj.transform;
        }

        private void SetupBonesAndColliders(GameObject basePlayer, Transform baseMesh)
        {
            Transform[] meshBones = skinnedMeshRenderer.bones;
            feet = new FootData[2];
            bones = ExtractValidBones(meshBones, mesh);
            int boneCount = bones.Length;
            previousBonePositions = new Vector3[boneCount];
            previousBoneRotations = new Quaternion[boneCount];
            targetBonePositions = new Vector3[boneCount];
            targetBoneRotations = new Quaternion[boneCount];
            bonesUpdated = new bool[bones.Length];

            // Set all clone bone local positions to model bind poses to reset facial expression and finger poses
            Matrix4x4[] bindPoses = skinnedMeshRenderer.sharedMesh.bindposes;
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
                    Matrix4x4 localBindPose = (bone.parent.worldToLocalMatrix * rootBone.localToWorldMatrix) * bindPoseInverse;

                    position = localBindPose.GetColumn(3);
                    rotation = Quaternion.LookRotation(
                        localBindPose.GetColumn(2),
                        localBindPose.GetColumn(1)
                    );
                }

                bone.localPosition = position;
                bone.localRotation = rotation;
            }

            var basePlayerBones = FindMatchingChildren(basePlayer.transform.FindChild("PuppetMaster"), bones.ToList());

            // Custom bone sizes because it gets weird if you copy them from PuppetMaster
            capsuleColliders = new List<CapsuleCollider>();
            var colliderData = new Dictionary<string, (Vector3 center, float radius, float height, int direction)>();

            colliderData.Add("head.x", (new Vector3(0, 0.1f, 0.05f), 0.13f, 0.35f, 1));
            colliderData.Add("spine_02.x", (new Vector3(0, 0.21f, 0), 0.15f, 0.42f, 0));
            colliderData.Add("spine_01.x", (new Vector3(0, 0.1f, 0), 0.1636f, 0.45f, 0));
            colliderData.Add("root.x", (new Vector3(0, 0, -0.025f), 0.175f, 0.5f, 0));

            colliderData.Add("arm_stretch.l", (new Vector3(0, 0.13f, 0), 0.07f, 0.38f, 1)); // Shoulder
            colliderData.Add("forearm_stretch.l", (new Vector3(0, 0.15f, 0), 0.05f, 0.35f, 1));
            colliderData.Add("hand.l", (new Vector3(0, 0.07f, 0), 0.05f, 0.2f, 1));
            colliderData.Add("arm_stretch.r", (new Vector3(0, 0.13f, 0), 0.07f, 0.38f, 1)); // Shoulder
            colliderData.Add("forearm_stretch.r", (new Vector3(0, 0.15f, 0), 0.05f, 0.35f, 1));
            colliderData.Add("hand.r", (new Vector3(0, 0.07f, 0), 0.05f, 0.2f, 1));

            colliderData.Add("thigh_stretch.l", (new Vector3(0.01f, 0.15f, 0), 0.121f, 0.6f, 1));
            colliderData.Add("leg_stretch.l", (new Vector3(0f, 0.15f, 0), 0.075f, 0.455f, 1));
            colliderData.Add("foot.l", (new Vector3(0, 0.1f, -0.02f), 0.06f, 0.3f, 1));
            colliderData.Add("thigh_stretch.r", (new Vector3(0.01f, 0.15f, 0), 0.121f, 0.6f, 1));
            colliderData.Add("leg_stretch.r", (new Vector3(0f, 0.15f, 0), 0.075f, 0.455f, 1));//0.463
            colliderData.Add("foot.r", (new Vector3(0, 0.1f, -0.02f), 0.06f, 0.3f, 1));

            boneToCrusherList = new();
            boneToRbMap = new();

            foreach (var bone in bones)
            {
                if (bone == null) continue;

                bone.gameObject.layer = 6;

                if (colliderData.TryGetValue(bone.name, out var cData))
                {
                    // Main actual collider
                    Rigidbody rb = bone.gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = true;
                    rb.mass = 0.5f;

                    CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
                    col.center = cData.center;
                    col.radius = cData.radius * 0.7f;
                    col.height = cData.height;
                    col.direction = cData.direction;
                    col.isTrigger = false;
                    col.material = new PhysicMaterial("PlayerSticky")
                    {
                        bounciness = 0f,
                        bounceCombine = PhysicMaterialCombine.Minimum,
                        frictionCombine = PhysicMaterialCombine.Maximum,
                        dynamicFriction = 1f,
                        staticFriction = 1f
                    };
                    col.enabled = false;
                    capsuleColliders.Add(col);

                    var matchingOGBone = basePlayerBones.FirstOrDefault(b => b != null && b.name == bone.name);
                    if (matchingOGBone != null)
                    {
                        var OGmudMeshChild = FindChildByKeyword(matchingOGBone, "mudMesh");
                        if (OGmudMeshChild != null)
                        {
                            GameObject mudMesh = GameObject.Instantiate(OGmudMeshChild.gameObject);
                            mudMesh.transform.SetParent(bone);
                            mudMesh.transform.localPosition = OGmudMeshChild.transform.localPosition;
                            mudMesh.transform.localRotation = OGmudMeshChild.transform.localRotation;

                            // WaterObjects temporarily disabled due to issues with more than 2 clones being active.
                            /*
                            WaterObject ogWO = OGmudMeshChild.GetComponent<WaterObject>();
                            WaterObject wO = mudMesh.GetComponent<WaterObject>();

                            if (wO.myBodyPart == BBSBodyPart.Head) wO.myBodyPart = BBSBodyPart.Chest; // To prevent drowning sound effects
                            //wO.isPlayer = false; // If this is true, the main player's particles will sometimes spawn for the clone too
                            //wO.simulationMesh = ogWO.SimulationMesh;
                            //wO.CurrentWaterDataProvider = ogWO.CurrentWaterDataProvider;
                            //wO.targetRigidbody = rb;
                            //wO._meshFilter = mudMesh.GetComponent<MeshFilter>();
                            boneToRbMap.Add(bone, new RbMap(rb, wO));
                            Core.AwakeRuns.Enqueue(() => { wO.Awake(); });
                            */
                        }
                    }

                    // Continue after these because the feet should not have a sensor or cBC
                    if (bone.name == "hand.r")
                    {
                        hands.Item1 = bone;
                    }
                    else if (bone.name == "hand.l")
                    {
                        hands.Item2 = bone;
                    }

                    if (bone.name == "foot.l")
                    {
                        feet[0] = new FootData();
                        feet[0].bone = bone;
                        feet[0].rb = rb;
                        continue;
                    }
                    else if (bone.name == "foot.r")
                    {
                        feet[1] = new FootData();
                        feet[1].bone = bone;
                        feet[1].rb = rb;
                        continue;
                    }

                    if (bone.name == "root.x")
                    {
                        boneToCrusherList.Add((bone, crushers.GetChild(0)));
                    }
                    else if (bone.name == "spine_02.x")
                    {
                        boneToCrusherList.Add((bone, crushers.GetChild(1)));
                    }
                    else if (bone.name == "head.x")
                    {
                        boneToCrusherList.Add((bone, crushers.GetChild(2)));

                        var gBone = bone.FindChild("Nathan_Glasses");
                        if (gBone != null)
                        {
                            glasses = gBone.GetComponent<MeshRenderer>();
                            MaterialKeywordHelper(glasses.materials[1]);

                            // Maybe remove lens material here eventually
                        }

                        textObj = new GameObject("Nametag");
                        textObj.transform.SetParent(bone);
                        textObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                        textObj.transform.rotation = Quaternion.identity;
                        textObj.transform.localRotation = Quaternion.identity;

                        Component meshComp = textObj.AddComponent(Il2CppType.Of<TextMeshPro>());
                        textMesh = meshComp.Cast<TextMeshPro>();
                        textMesh.alignment = TextAlignmentOptions.Center;
                        textMesh.text = displayName;
                        textMesh.fontSize = 1.5f;
                        textMesh.color = Color.white;

                        Shader overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
                        if (overlayShader != null)
                        {
                            // Create a new material with the overlay shader
                            Material overlayMat = new Material(overlayShader);
                            overlayMat.mainTexture = textMesh.font.material.mainTexture; // Keep glyph atlas
                            textMesh.fontMaterial = overlayMat;
                        }
                        else
                        {
                            MelonLogger.Warning("Could not find TMP Distance Field Overlay shader!");
                        }
                    }
                }
            }
        }
    }
}