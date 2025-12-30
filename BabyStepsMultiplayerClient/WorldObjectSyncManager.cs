using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BabyStepsMultiplayerClient
{
    public static class WorldObjectSyncManager
    {
        private static long serverSyncMsBase = 0;
        private static long localTimeAtSync = 0;
        private static Stopwatch localTimer = new Stopwatch();

        private static List<WaterWheel> cachedWaterWheels = new();
        private static float lastRefreshTime = 0f;
        private const float REFRESH_INTERVAL = 2f;

        public static long serverSyncMs
        {
            get
            {
                if (!localTimer.IsRunning) return serverSyncMsBase;
                return serverSyncMsBase + localTimer.ElapsedMilliseconds;
            }
        }
        public static void SetServerTime(long serverTimeMs, long estimatedRttMs = 0)
        {
            // Compensate for half the round trip time (real ~ one way latency)
            long compensatedServerTime = serverTimeMs + (estimatedRttMs / 2);

            serverSyncMsBase = compensatedServerTime;
            localTimer.Restart();

            Core.DebugMsg($"Server time synced: {serverTimeMs}ms (compensated: +{estimatedRttMs / 2}ms)");
        }
        public static void Update()
        {
            if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshWaterWheelCache();
                lastRefreshTime = Time.time;
            }
            SyncWaterWheels();
        }
        private static void RefreshWaterWheelCache()
        {
            cachedWaterWheels.RemoveAll(w => w == null); // Remove all unloaded water wheels
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

            float targetRotation = (serverSyncMs * 0.01f) % 360f;

            foreach (var wheel in cachedWaterWheels)
            {
                if (wheel == null || wheel.transform == null) continue;

                wheel.transform.localEulerAngles = new Vector3(
                    -targetRotation,
                    0f,
                    0f
                );
            }
        }

        public static void ClearCachedWheels() { cachedWaterWheels.Clear(); }
    }
}
