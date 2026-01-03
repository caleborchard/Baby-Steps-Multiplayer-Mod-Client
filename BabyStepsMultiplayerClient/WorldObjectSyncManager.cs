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
        private const float WHEEL_SPEED = 9.0f; // degrees per second, magic number measured in-game from base 0.15 speed

        private static float lastRefreshTime = 0f;
        private static double currentTimeMs = 0.0;
        private static float lastUpdateTime = 0f;
        private static List<WaterWheel> cachedWaterWheels = new();
        private static Dictionary<WaterWheel, Quaternion> wheelBaseRotation = new Dictionary<WaterWheel, Quaternion>();

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
            MelonLogger.Msg(serverUptimeMs);
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
                if (wheel == null || wheel.transform == null) continue;

                Rigidbody rb = wheel.GetComponent<Rigidbody>();
                if (rb == null) continue;

                if (!wheelBaseRotation.TryGetValue(wheel, out Quaternion baseRot))
                {
                    baseRot = wheel.transform.rotation;
                    wheelBaseRotation[wheel] = baseRot;
                }

                float direction = Mathf.Sign(wheel.speed);
                float xAngle = baseAngle * direction;

                Vector3 baseEuler = baseRot.eulerAngles;
                baseEuler.x = xAngle * direction;
                Quaternion target = Quaternion.Euler(baseEuler);

                rb.MoveRotation(target);
            }
        }
        public static void ClearCachedWheels()
        {
            cachedWaterWheels.Clear();
            wheelBaseRotation.Clear();
        }
    }
}
