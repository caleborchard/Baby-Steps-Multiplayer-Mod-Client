using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Sentis.Layers;
using UnityEngine;

namespace BabyStepsMultiplayerClient
{
    public static class WorldObjectSyncManager
    {
        private const float REFRESH_INTERVAL = 2f;
        private const float WHEEL_SPEED = 9.0f;

        private static float lastRefreshTime = 0f;
        private static double currentTimeMs = 0.0;
        private static float lastUpdateTime = 0f;
        private static List<WaterWheel> cachedWaterWheels = new();
        private static Dictionary<Rigidbody, Quaternion> wheelBaseRotation = new();
        private static Dictionary<WaterWheel, (Rigidbody wheelRb, Rigidbody axelRb)> wheelComponents = new();

        public static void Update()
        {
            float currentRealTime = Time.realtimeSinceStartup;
            float deltaTime = currentRealTime - lastUpdateTime;
            lastUpdateTime = currentRealTime;
            currentTimeMs += deltaTime * 1000.0;

            if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshWaterWheelCache();
                lastRefreshTime = Time.time;
            }

            SyncWaterWheels();
        }

        public static void OnServerTimeSample(long serverUptimeMs)
        {
            currentTimeMs = serverUptimeMs;
        }

        private static void RefreshWaterWheelCache()
        {
            cachedWaterWheels.RemoveAll(w => w == null);

            var allWaterWheels = UnityEngine.Object.FindObjectsOfType<WaterWheel>();
            foreach (var wheel in allWaterWheels)
            {
                if (!cachedWaterWheels.Contains(wheel))
                {
                    cachedWaterWheels.Add(wheel);
                    wheel.enabled = false;
                    Core.DebugMsg("Found new WaterWheel");
                }
            }
        }
        private static void SyncWaterWheels()
        {
            if (cachedWaterWheels.Count == 0) return;

            float timeSeconds = (float)(currentTimeMs / 1000.0);
            float baseAngle = (WHEEL_SPEED * timeSeconds) % 360f;

            foreach (var wheel in cachedWaterWheels)
            {
                if (wheel == null) continue;

                if (!wheelComponents.TryGetValue(wheel, out var components))
                {
                    Rigidbody wheelRb = wheel.GetComponent<Rigidbody>();
                    Rigidbody axelRb = wheel.workAxel != null ? wheel.workAxel.GetComponent<Rigidbody>() : null;
                    components = (wheelRb, axelRb);
                    wheelComponents[wheel] = components;
                }

                if (components.wheelRb == null) continue;

                if (!wheelBaseRotation.TryGetValue(components.wheelRb, out Quaternion baseRot))
                {
                    baseRot = wheel.transform.rotation;
                    wheelBaseRotation[components.wheelRb] = baseRot;
                }

                float direction = Mathf.Sign(wheel.speed);
                float xAngle = baseAngle * direction;

                Quaternion xRotation = Quaternion.AngleAxis(xAngle, baseRot * Vector3.right);
                Vector3 baseEuler = baseRot.eulerAngles;
                Quaternion baseWithoutX = Quaternion.Euler(0, baseEuler.y, baseEuler.z);

                Quaternion target = xRotation * baseWithoutX;
                components.wheelRb.MoveRotation(target);

                if (components.axelRb != null) //Goes too fast for some reason, but you never interact with it so whatever
                {
                    if (!wheelBaseRotation.TryGetValue(components.axelRb, out Quaternion axelBaseRot))
                    {
                        axelBaseRot = wheel.workAxel.transform.rotation;
                        wheelBaseRotation[components.axelRb] = axelBaseRot;
                    }

                    Vector3 axelBaseEuler = axelBaseRot.eulerAngles;
                    Quaternion axelTarget = Quaternion.Euler(xAngle, axelBaseEuler.y, axelBaseEuler.z);
                    components.axelRb.MoveRotation(axelTarget);
                }
            }
        }
        public static void ReleaseCachedWheels()
        {
            foreach (var wheel in cachedWaterWheels)
            {
                if (wheel != null)
                {
                    wheel.enabled = true;
                }
            }
        }
        public static void ClearCachedWheels()
        {
            cachedWaterWheels.Clear();
            wheelBaseRotation.Clear();
            wheelComponents.Clear();
        }
    }
}