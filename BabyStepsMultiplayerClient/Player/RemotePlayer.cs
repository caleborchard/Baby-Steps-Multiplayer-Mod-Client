using BabyStepsMultiplayerClient.Components;
using BabyStepsMultiplayerClient.Extensions;
using BabyStepsMultiplayerClient.Networking;
using Il2Cpp;
using Il2CppNWH.DWP2.WaterObjects;
using MelonLoader;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using BabyStepsMultiplayerClient.Player.UI;
using BabyStepsMultiplayerClient.Player.Audio;

namespace BabyStepsMultiplayerClient.Player
{
    public class RemotePlayer : BasePlayer
    {
        public static ConcurrentBag<RemotePlayer> GlobalPool = new();

        private static Dictionary<string, GameObject> savablePrefabs = new Dictionary<string, GameObject>();

        public static Texture2D suitTexture;
        public NameTagUI nameTag;
        public Gazable gazable;

        public Dictionary<string, (Rigidbody, CapsuleCollider)> boneColliders;
        public Dictionary<Transform, Transform> boneCrushers;

        public Hat hat;
        public Collider[] hatColliders;

        public BBSAudioSource audioSource;

        public (Grabable, Grabable) heldItems;
        public (Collider[], Collider[]) heldItemColliders;

        public (FootData, FootData) feetData;

        public string displayName = "Nate";

        private BoneSnapshot currentBoneGroup;
        private ConcurrentQueue<BoneSnapshot> snapshotBuffer = new();
        private const double INTERPDELAY = 0.1; // 100ms

        public bool netCollisionsEnabled;
        private bool firstBoneInterpRan;
        private object collisionCoroutineToken;
        public float distanceFromPlayer;
        public bool firstAppearanceApplication;

        public static readonly Dictionary<string,
            (Vector3 center,
            float radius,
            float height,
            int direction)> colliderTemplate = new()
            {
                // Spine
                { "head.x", (new Vector3(0, 0.1f, 0.05f), 0.13f, 0.35f, 1) },
                { "spine_02.x", (new Vector3(0, 0.21f, 0), 0.15f, 0.42f, 0) },
                { "spine_01.x", (new Vector3(0, 0.1f, 0), 0.1636f, 0.45f, 0) },
                { "root.x", (new Vector3(0, 0, -0.025f), 0.175f, 0.5f, 0) },

                // Left Arm
                { "arm_stretch.l", (new Vector3(0, 0.13f, 0), 0.07f, 0.38f, 1) }, // Shoulder
                { "forearm_stretch.l", (new Vector3(0, 0.15f, 0), 0.05f, 0.35f, 1) },
                { "hand.l", (new Vector3(0, 0.07f, 0), 0.05f, 0.2f, 1) },

                // Right Arm
                { "arm_stretch.r", (new Vector3(0, 0.13f, 0), 0.07f, 0.38f, 1) }, // Shoulder
                { "forearm_stretch.r", (new Vector3(0, 0.15f, 0), 0.05f, 0.35f, 1) },
                { "hand.r", (new Vector3(0, 0.07f, 0), 0.05f, 0.2f, 1) },

                // Left Leg
                { "thigh_stretch.l", (new Vector3(0.01f, 0.15f, 0), 0.121f, 0.6f, 1) },
                { "leg_stretch.l", (new Vector3(0f, 0.15f, 0), 0.075f, 0.455f, 1) },
                { "foot.l", (new Vector3(0, 0.1f, -0.02f), 0.06f, 0.3f, 1) },

                // Right Leg
                { "thigh_stretch.r", (new Vector3(0.01f, 0.15f, 0), 0.121f, 0.6f, 1) },
                { "leg_stretch.r", (new Vector3(0f, 0.15f, 0), 0.075f, 0.455f, 1) },
                { "foot.r", (new Vector3(0, 0.1f, -0.02f), 0.06f, 0.3f, 1) },
            };

        public void Initialize(int numClones)
        {
            if (LocalPlayer.Instance == null)
                return;

            if (suitTexture == null)
            {
                LocalPlayer.Instance.CleanCompletely();
                suitTexture = LocalPlayer.Instance.CloneSuitTexture();
            }

            baseObject = new GameObject($"NateClone{numClones}");

            nameTag = new(baseObject.transform);

            if (LocalPlayer.Instance.baseMesh != null)
            {
                baseMesh = UnityEngine.Object.Instantiate(LocalPlayer.Instance.baseMesh);
                baseMesh.name = "NateMesh";
                baseMesh.parent = baseObject.transform;
                baseMesh.transform.position = Vector3.zero;
                MelonCoroutines.Start(DelayedComponentStrip(baseMesh));
            }

            if (LocalPlayer.Instance.particleCrushers != null)
            {
                particleCrushers = UnityEngine.Object.Instantiate(LocalPlayer.Instance.particleCrushers);
                particleCrushers.parent = baseObject.transform;
            }

            // Base Initialization
            Initialize();

            if (skinnedMeshRenderer != null) 
                meshMaterials = skinnedMeshRenderer.materials;

            SetupBonesAndMaterials();

            if (jiminyRibbon != null)
                jiminyRibbon.active = false;

            if (meshMaterials != null)
            {
                for (int i = 0; i < meshMaterials.Length; i++)
                {
                    Material mat = meshMaterials[i];
                    if (mat == null)
                        continue;

                    if (!mat.HasProperty("_DitherAlpha")
                        || mat.shader.name == "Better Lit/DuderSuit")
                        continue;

                    MaterialKeywordHelper(mat);
                }
            }

            if (suitMaterial != null)
            {
                if (suitTexture != null)
                    suitMaterial.SetTexture("_AlbedoMap", suitTexture);
                suitMaterial.SetTexture("_WetnessTexture", null);
                suitMaterial.SetTexture("_RainDropTexture", null);
            }

            SetHairHat(1, new Vector4(0, 1, 1, 0));

            if (eyeBalls.Item1 != null)
                eyeBalls.Item1.localRotation = Quaternion.identity;
            if (eyeBalls.Item2 != null)
                eyeBalls.Item2.localRotation = Quaternion.identity;

            if (eyeBallRenderers.Item1 != null)
                StdMatSetup(eyeBallRenderers.Item1.material);
            if (eyeBallRenderers.Item2 != null)
                StdMatSetup(eyeBallRenderers.Item2.material);

            if (eyeLids.Item1 != null)
                eyeLids.Item1.gameObject.SetActive(false);
            if (eyeLids.Item2 != null)
                eyeLids.Item2.gameObject.SetActive(false);

            if (headBone != null
                && nameTag != null)
                nameTag.SetParent(headBone);

            if (headBone != null)
            {
                gazable = headBone.gameObject.AddComponent<Gazable>();
                MelonCoroutines.Start(DelayedGazableFillin(gazable));
            }

            if (nateGlasses != null)
                MaterialKeywordHelper(nateGlasses.material);

            ResetPosition();
            ResetBonesToBind();

            SetupBones();
            CreateParticleCrushers();
            CreateFootData();

#if DEBUG
            audioSource = new BBSAudioSource(headBone);
            audioSource.Initialize();
#endif
        }

        private static System.Collections.IEnumerator DelayedGazableFillin(Gazable _gazable)
        {
            yield return null;
            _gazable.wontMove = false;
            _gazable.sqrRad = 5f; // Maybe position the Gazable object slightly behind the head?
                                  // The problem is that when someone is walking behind you your player looks down to try to look backwards at them.
        }

        public override void Dispose()
        {
            RemoveHat();

            DropItem(0);
            DropItem(1);

            ResetBonesToBind();

            DisableCollision();
            SetActive(false);
            SetOpacity(0f);
            ResetPosition();

            ResetSuitColor();
            SetDisplayName("Nate");

#if DEBUG
            if (audioSource != null)
                audioSource.Dispose();
            audioSource = null;
#endif

            distanceFromPlayer = 0f;
            firstBoneInterpRan = false;
            netCollisionsEnabled = false;
            firstAppearanceApplication = false;
            snapshotBuffer.Clear();

            GlobalPool.Add(this);
        }

        public void ResetPosition()
        {
            if (rootBone == null)
                return;

            rootBone.transform.position = Vector3.zero;
        }

        public override void LateUpdate()
        {
            if (nameTag != null) nameTag.LateUpdate();

            InterpolateBoneTransforms();

            if ((rootBone != null) && (LocalPlayer.Instance != null))
            {
                if ((LocalPlayer.Instance.rootBone != null) && firstBoneInterpRan)
                    distanceFromPlayer = Vector3.Distance(LocalPlayer.Instance.rootBone.position, rootBone.transform.position);

                GameObject camera = LocalPlayer.Instance.GetCameraObject();
                if (camera != null)
                {
                    float distance = Vector3.Distance(camera.transform.position, rootBone.transform.position);
                    FadeByDistance(distance);
                }
            }

#if DEBUG
            if (audioSource != null)
            {
                audioSource.Update();

                // Test Mic Capture
                if (LocalPlayer.Instance != null)
                    LocalPlayer.Instance.WriteMicrophoneToAudioSource(audioSource);
            }
#endif
        }

        public void SetDisplayName(string name)
        {
            if (nameTag == null) return;
            displayName = name;
            nameTag.SetText(name);
        }

        public override void SetOpacity(float opacity)
        {
            base.SetOpacity(opacity);

            if (nameTag != null)
            {
                Color color = nameTag.GetColor();
                color.a = opacity;
                nameTag.SetColor(color);
            }
        }

        public void SetActive(bool active)
        {
            if (baseObject == null) return;
            baseObject.active = active;
        }

        private static System.Collections.IEnumerator DelayedComponentStrip(Transform mesh)
        {
            // Delay
            yield return null;

            if (mesh != null)
                foreach (var component in mesh.GetComponents<Component>())
                    if (component is not Transform)
                        UnityEngine.Object.Destroy(component);
        }

        public void EnableCollision()
        {
            if (collisionCoroutineToken != null) MelonCoroutines.Stop(collisionCoroutineToken);
            collisionCoroutineToken = MelonCoroutines.Start(EnableCollisionCoroutine());
        }

        private System.Collections.IEnumerator EnableCollisionCoroutine()
        {
            while (distanceFromPlayer <= 0.6f) yield return null;

            if (boneColliders != null)
                foreach ((Rigidbody, CapsuleCollider) val in boneColliders.Values)
                {
                    if (val.Item2 == null) continue;
                    val.Item2.enabled = true;
                }

            if (hatColliders != null)
                foreach (Collider val in hatColliders)
                {
                    if (val == null) continue;
                    val.enabled = true;
                }

             if (heldItemColliders.Item1 != null)
                foreach (Collider val in heldItemColliders.Item1)
                {
                    if (val == null) continue;
                    val.enabled = true;
                }

            if (heldItemColliders.Item2 != null)
                foreach (Collider val in heldItemColliders.Item2)
                {
                    if (val == null) continue;
                    val.enabled = true;
                }

            collisionCoroutineToken = null;
        }

        public void DisableCollision()
        {

            if (collisionCoroutineToken != null)
            {
                MelonCoroutines.Stop(collisionCoroutineToken);
                collisionCoroutineToken = null;
            }

            if (boneColliders != null)
                foreach ((Rigidbody, CapsuleCollider) val in boneColliders.Values)
                {
                    if (val.Item2 == null) continue;
                    val.Item2.enabled = false;
                }

            if (hat != null)
            {
                var allHatColliders = hat.GetComponentsInChildren<Collider>();
                foreach (Collider val in allHatColliders)
                {
                    if (val == null) continue;
                    val.enabled = false;
                }
            }

            if (heldItemColliders.Item1 != null)
                foreach (Collider val in heldItemColliders.Item1)
                {
                    if (val == null) continue;
                    val.enabled = false;
                }

            if (heldItemColliders.Item2 != null)
                foreach (Collider val in heldItemColliders.Item2)
                {
                    if (val == null) continue;
                    val.enabled = false;
                }
        }

        private void StdMatSetup(Material mat)
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void MaterialKeywordHelper(Material mat)
        {
            if (mat == null) return;

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

        private void SetupBones()
        {
            if (boneChildren == null) return;

            if (boneColliders == null) boneColliders = new();

            foreach (var bone in boneChildren)
            {
                if (bone == null) continue;

                //bone.gameObject.layer = 6;
                bone.gameObject.layer = 23;

                CreateBoneCollider(bone);
                CreateBoneMudMesh(bone);
            }
        }

        private void CreateBoneCollider(Transform bone)
        {
            if (bone == null) return;

            string boneName = bone.name;
            if (!colliderTemplate.TryGetValue(bone.name, out var template)) return;

            // Temporary exception to test out player pushing. Maybe specify layer in the colliderTemplate Dictionary
            if (boneName.Contains("spine_01") || boneName.Contains("root")) bone.gameObject.layer = 6;

            Rigidbody rb = bone.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = bone.gameObject.AddComponent<Rigidbody>();

                rb.mass = 0.5f;
            }
            rb.useGravity = false;
            rb.isKinematic = true;

            CapsuleCollider col = bone.gameObject.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = bone.gameObject.AddComponent<CapsuleCollider>();
                col.center = template.center;
                col.radius = template.radius * 0.7f;
                col.height = template.height;
                col.direction = template.direction;
                col.isTrigger = false;
                col.material = new PhysicMaterial("PlayerSticky")
                {
                    bounciness = 0f,
                    bounceCombine = PhysicMaterialCombine.Minimum,
                    frictionCombine = PhysicMaterialCombine.Maximum,
                    dynamicFriction = 1f,
                    staticFriction = 1f
                };
            }
            col.enabled = false;

            boneColliders.Add(boneName, (rb, col));
        }

        private void CreateBoneMudMesh(Transform bone)
        {
            if (bone == null) return;
            if (LocalPlayer.Instance == null) return;
            if (LocalPlayer.Instance.boneMudMeshes == null) return;

            string boneName = bone.name;
            if (!LocalPlayer.Instance.boneMudMeshes.TryGetValue(boneName, out Transform mesh)) return;

            if (boneMudMeshes == null)
                boneMudMeshes = new();

            GameObject mudMesh = UnityEngine.Object.Instantiate(mesh.gameObject);
            mudMesh.transform.SetParent(bone);
            mudMesh.transform.localPosition = mesh.transform.localPosition;
            mudMesh.transform.localRotation = mesh.transform.localRotation;

            WaterObject wO = mudMesh.GetComponent<WaterObject>();
            if (wO != null) UnityEngine.Object.Destroy(wO);

            boneMudMeshes[boneName] = mudMesh.transform;
        }

        private void CreateParticleCrushers()
        {
            if (particleCrushers == null) return;

            if (boneCrushers == null) boneCrushers = new();

            if (rootBone != null)
            {
                Transform crusher = null;
                if (particleCrushers != null) crusher = particleCrushers.GetChild(0);
                boneCrushers.Add(rootBone, crusher);
            }

            if (spineBone != null)
            {
                Transform crusher = null;
                if (particleCrushers != null) crusher = particleCrushers.GetChild(1);
                boneCrushers.Add(spineBone, crusher);
            }

            if (headBone != null)
            {
                Transform crusher = null;
                if (particleCrushers != null) crusher = particleCrushers.GetChild(2);
                boneCrushers.Add(headBone, crusher);
            }
        }

        private void CreateFootData()
        {
            feetData.Item1 = new();
            if (footBones.Item1 != null)
            {
                feetData.Item1.bone = footBones.Item1;

                var boneName = footBones.Item1.name;
                if (boneColliders.TryGetValue(boneName, out (Rigidbody, CapsuleCollider) cc)) feetData.Item1.rb = cc.Item1;
            }

            feetData.Item2 = new();
            if (footBones.Item1 != null)
            {
                feetData.Item2.bone = footBones.Item1;

                var boneName = footBones.Item1.name;
                if (boneColliders.TryGetValue(boneName, out (Rigidbody, CapsuleCollider) cc)) feetData.Item2.rb = cc.Item1;
            }
        }

        public void WearHat(string hatName,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            if (hat != null) RemoveHat();

            if (headBone == null) return;

            if (!savablePrefabs.TryGetValue(hatName, out GameObject prefab))
            {
                GameObject loaderLocations = GameObject.Find("BigManagerPrefab/GlobalObjectParent/Savables");
                Transform hatLoader = loaderLocations.transform.FindChildByKeyword(hatName);
                GlobalObjectLoader gOL = hatLoader.GetComponent<GlobalObjectLoader>();
                AssetReference assetRef = gOL.loadee;

                prefab = assetRef.LoadAssetAsync<GameObject>().WaitForCompletion();
                savablePrefabs[hatName] = prefab;
            }

            GameObject hatGO = UnityEngine.Object.Instantiate(prefab, headBone);
            Hat hHat = hatGO.transform.GetComponentInChildren<Hat>();

            Transform fruitShine = hatGO.transform.FindChildByKeyword("FruitShine");
            if (fruitShine != null) UnityEngine.Object.Destroy(fruitShine.gameObject);

            WaterObject wO = hatGO.GetComponentInChildren<WaterObject>();
            if (wO != null) UnityEngine.Object.Destroy(wO);

            hHat.grabable = false;
            UnityEngine.Object.Destroy(hHat.rb);

            hatGO.transform.localPosition = localPosition;
            hatGO.transform.localRotation = localRotation;

            SetHairHat(hHat.hairAmt, hHat.hairlineUpVec);

            hat = hHat;
            hatColliders = hat.GetComponentsInChildren<Collider>();
            foreach (Collider val in hatColliders)
            {
                if (val == null) continue;
                val.enabled = false;
            }
        }
        public void RemoveHat()
        {
            if (hat != null)
            {
                UnityEngine.Object.Destroy(hat.gameObject);
                hat = null;
                SetHairHat(1, new Vector4(0, 1, 1, 0));
                hatColliders = null;
            }
        }


        public void HoldItem(string grabableName, int handIndex, Vector3 localPosition, Quaternion localRotation)
        {
            Grabable item;
            Transform hand;
            if (handIndex == 0)
            {
                item = heldItems.Item1;
                hand = handBones.Item1;
            }
            else
            {
                item = heldItems.Item2;
                hand = handBones.Item2;
            }
            if (hand == null) return;

            if (item != null) DropItem(handIndex);

            if (!savablePrefabs.TryGetValue(grabableName, out GameObject prefab))
            {
                GameObject loaderLocations = GameObject.Find("BigManagerPrefab/GlobalObjectParent/Savables");

                Transform savableLoader = loaderLocations.transform.FindChildByKeyword(grabableName);

                GlobalObjectLoader gOL = savableLoader.GetComponent<GlobalObjectLoader>();

                AssetReference assetRef = gOL.loadee;

                prefab = assetRef.LoadAssetAsync<GameObject>().WaitForCompletion();
                savablePrefabs[grabableName] = prefab;
            }

            GameObject itemGO = UnityEngine.Object.Instantiate(prefab, hand);
            Grabable grabable = itemGO.transform.GetComponentInChildren<Grabable>();

            grabable.grabable = false;
            UnityEngine.Object.Destroy(grabable.rb);

            var pedometer = grabable.GetComponent<Pedometer>();
            if (pedometer != null) GameObject.Destroy(pedometer);

            grabable.transform.localPosition = localPosition;
            grabable.transform.localRotation = localRotation;

            Collider[] colliderArr = null;
            if (handIndex == 0)
            {
                heldItems.Item1 = grabable;
                heldItemColliders.Item1 = grabable.GetComponentsInChildren<Collider>();
                colliderArr = heldItemColliders.Item1;
            }
            else
            {
                heldItems.Item2 = grabable;
                heldItemColliders.Item2 = grabable.GetComponentsInChildren<Collider>();
                colliderArr = heldItemColliders.Item2;
            }

            foreach (Collider val in colliderArr)
            {
                if (val == null) continue;
                val.enabled = false;
            }

            Transform fruitShine = itemGO.transform.FindChildByKeyword("FruitShine");
            if (fruitShine != null) UnityEngine.Object.Destroy(fruitShine.gameObject);

            WaterObject wO = itemGO.GetComponentInChildren<WaterObject>();
            if (wO != null) UnityEngine.Object.Destroy(wO);
        }
        public void DropItem(int handIndex)
        {
            if (handIndex == 0 && heldItems.Item1 != null)
            {
                UnityEngine.Object.Destroy(heldItems.Item1.gameObject);
                heldItems.Item1 = null;
                heldItemColliders.Item1 = null;
            }
            else if (handIndex == 1 && heldItems.Item2 != null)
            {
                UnityEngine.Object.Destroy(heldItems.Item2.gameObject);
                heldItems.Item2 = null;
                heldItemColliders.Item2 = null;
            }
        }

        private void ApplySnapshotInstant(BoneSnapshot latest)
        {
            for (int i = 0; i < latest.transformNets.Length; i++)
            {
                var net = latest.transformNets[i];
                if (net == null) continue;

                int hIdx = net.heirarchyIndex;
                if (hIdx < 0 || hIdx >= boneChildren.Count) continue;

                var bone = boneChildren[hIdx];
                if (bone == null) continue;

                var npos = net.position;
                Vector3 pos = new Vector3(npos.X, npos.Y, npos.Z);

                var nrot = net.rotation;
                Quaternion rot = new Quaternion(nrot.X, nrot.Y, nrot.Z, nrot.W);

                bone.position = pos;
                bone.rotation = rot;
            }
        }
        public void UpdateBones(TransformNet[] bonesToUpdate, int kickoffPoint)
        {
            if (boneChildren == null) return;
            if (bonesToUpdate == null) return;

            BoneSnapshot snapshot;
            if (kickoffPoint != 0) snapshot = currentBoneGroup;
            else
            {
                if (currentBoneGroup != null)
                {
                    snapshotBuffer.Enqueue(currentBoneGroup);
                    while (snapshotBuffer.Count > 10) snapshotBuffer.TryDequeue(out _); // Prevent infinite growth
                }

                snapshot = new BoneSnapshot(boneChildren.Count);
                snapshot.time = Time.timeAsDouble;
            }

            for (int i = kickoffPoint; i < bonesToUpdate.Length; i++)
            {
                var bone = bonesToUpdate[i];
                if (boneChildren[bone.heirarchyIndex] == null) continue;

                snapshot.transformNets[bone.heirarchyIndex] = bone;
            }

            currentBoneGroup = snapshot;
        }
        private void InterpolateBoneTransforms()
        {
            if (boneChildren == null) return;
            if (boneColliders == null) return;

            if (!firstBoneInterpRan && snapshotBuffer.Count > 0)
            {
                // Get latest bone snapshot and set position and rotation to that immediately instead of lerping
                BoneSnapshot latest = null;
                while (snapshotBuffer.Count > 0) snapshotBuffer.TryDequeue(out latest);

                ApplySnapshotInstant(latest);

                bool shouldEnableColliders = ModSettings.player.Collisions.Value && netCollisionsEnabled;
                Core.networkManager.ApplyCollisionToggle(this, shouldEnableColliders);

                firstBoneInterpRan = true;
                return;
            }

            if (snapshotBuffer.Count < 2)
            {
                if (skinnedMeshRenderer != null) skinnedMeshRenderer.gameObject.active = false;
                if (nameTag != null) nameTag.SetActive(false);
                return;
            }

            if (skinnedMeshRenderer != null && !skinnedMeshRenderer.gameObject.active)
            {
                skinnedMeshRenderer.gameObject.active = true;
                if (nameTag != null) nameTag.SetActive(true);
            }

            double renderTime = Time.timeAsDouble - INTERPDELAY;

            BoneSnapshot prev = null, next = null;
            foreach (var snap in snapshotBuffer)
            {
                if (snap.time <= renderTime) prev = snap;
                if (snap.time > renderTime) { next = snap; break; }
            }

            //if (prev == null || next == null) return; // Not enough data to interpolate
            if (prev == null) return;
            if (next == null)
            {
                ApplySnapshotInstant(prev);
                return;
            }

            double t = (renderTime - prev.time) / (next.time - prev.time);
            float tf = Mathf.Clamp01((float)t);

            for (int i = 0; i < prev.transformNets.Length; i++)
            {
                var prevNet = prev.transformNets[i];
                var nextNet = next.transformNets[i];

                if (prevNet == null || nextNet == null) continue;

                int hIdx = prevNet.heirarchyIndex;
                if (hIdx < 0 || hIdx >= boneChildren.Count) continue;

                var bone = boneChildren[hIdx];
                if (bone == null) continue;

                var ppos = prev.transformNets[i].position;
                Vector3 prevPos = new Vector3(ppos.X, ppos.Y, ppos.Z);
                var npos = next.transformNets[i].position;
                Vector3 nextPos = new Vector3(npos.X, npos.Y, npos.Z);

                var prot = prev.transformNets[i].rotation;
                Quaternion prevRot = new Quaternion(prot.X, prot.Y, prot.Z, prot.W);
                var nrot = next.transformNets[i].rotation;
                Quaternion nextRot = new Quaternion(nrot.X, nrot.Y, nrot.Z, nrot.W);

                bone.position = Vector3.Lerp(prevPos, nextPos, tf);
                bone.rotation = Quaternion.Slerp(prevRot, nextRot, tf);
            }

            foreach (var b2c in boneCrushers)
            {
                if (b2c.Key == null) continue;
                if (b2c.Value == null) continue;
                b2c.Value.position = b2c.Key.position;
            }
        }
    }
}
