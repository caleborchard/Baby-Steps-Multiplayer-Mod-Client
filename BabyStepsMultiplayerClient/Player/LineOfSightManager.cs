using UnityEngine;
using MelonLoader;

namespace BabyStepsMultiplayerClient.Player
{
    public class LineOfSightManager
    {
        // Cone parameters
        private float coneAngle = 60f; // Half-angle in degrees
        private float coneDistance = 50f; // Maximum distance to check
        private Transform playerHeadBone;

        // Debug visualization
        private bool debugVisualization = false;

        public LineOfSightManager(Transform headBone)
        {
            playerHeadBone = headBone;
        }

        public bool IsInLineOfSight(Vector3 targetPosition)
        {
            if (playerHeadBone == null) return false;

            Vector3 headForward = playerHeadBone.forward;
            Vector3 directionToTarget = (targetPosition - playerHeadBone.position).normalized;

            float distance = Vector3.Distance(playerHeadBone.position, targetPosition);
            if (distance > coneDistance) return false;
            float angle = Vector3.Angle(headForward, directionToTarget);

            return angle <= coneAngle;
        }

        public bool HasClearLineOfSight(Vector3 targetPosition)
        {
            if (playerHeadBone == null) return false;

            Vector3 rayStart = playerHeadBone.position;
            Vector3 rayDirection = (targetPosition - rayStart).normalized;
            float distance = Vector3.Distance(rayStart, targetPosition);

            // Cast a ray and check if we hit the target before hitting other objects
            if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, distance))
            {
                // Allow a small margin for error
                return hit.distance >= distance * 0.95f;
            }

            return true;
        }

        public bool CanSee(Vector3 targetPosition)
        {
            return IsInLineOfSight(targetPosition) && HasClearLineOfSight(targetPosition);
        }

        public void SetConeAngle(float angle)
        {
            coneAngle = Mathf.Clamp(angle, 0f, 180f);
        }

        public void SetConeDistance(float distance)
        {
            coneDistance = Mathf.Max(0f, distance);
        }

        public float GetConeAngle() => coneAngle;
        public float GetConeDistance() => coneDistance;
        public bool IsDebugVisualizationEnabled() => debugVisualization;
    }
}
