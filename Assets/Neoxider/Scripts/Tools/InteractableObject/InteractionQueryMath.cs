using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Pure range and hit-order rules shared by interaction targets and custom query sources.
    /// </summary>
    public static class InteractionQueryMath
    {
        /// <summary>
        ///     Returns whether two points satisfy a maximum distance. A non-positive maximum is unlimited.
        /// </summary>
        public static bool IsWithinRange(Vector3 source, Vector3 target, float maximumDistance)
        {
            if (maximumDistance <= 0f)
            {
                return true;
            }

            float distanceSquared = (target - source).sqrMagnitude;
            return distanceSquared <= maximumDistance * maximumDistance;
        }

        /// <summary>
        ///     Returns the ray distance used to stop an obstacle query just before its target.
        /// </summary>
        public static float GetObstacleCheckDistance(float sourceToTargetDistance, float targetPadding = 0.1f)
        {
            return Mathf.Max(0f, sourceToTargetDistance - Mathf.Max(0f, targetPadding));
        }

        /// <summary>Finds the nearest candidate, independently of the input order.</summary>
        public static bool TryGetNearestHit(InteractionRayHit[] hits, int count, out InteractionRayHit nearestHit)
        {
            nearestHit = default;
            if (hits == null || count <= 0)
            {
                return false;
            }

            int validCount = Mathf.Min(count, hits.Length);
            float nearestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < validCount; i++)
            {
                InteractionRayHit hit = hits[i];
                if (!float.IsFinite(hit.Distance) || hit.Distance < 0f || hit.Distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.Distance;
                nearestHit = hit;
                found = true;
            }

            return found;
        }

        /// <summary>
        ///     Selects the nearest target. When a clear path is required, the target is accepted only
        ///     when it is no farther than the nearest blocking non-target hit.
        /// </summary>
        public static bool TrySelectTarget(InteractionRayHit[] hits, int count, bool requireClearPath,
            out InteractionRayHit targetHit)
        {
            targetHit = default;
            if (hits == null || count <= 0)
            {
                return false;
            }

            int validCount = Mathf.Min(count, hits.Length);
            float nearestTargetDistance = float.MaxValue;
            float nearestBlockingDistance = float.MaxValue;
            bool hasTarget = false;

            for (int i = 0; i < validCount; i++)
            {
                InteractionRayHit hit = hits[i];
                if (!float.IsFinite(hit.Distance) || hit.Distance < 0f)
                {
                    continue;
                }

                if (hit.IsTarget && hit.Distance < nearestTargetDistance)
                {
                    nearestTargetDistance = hit.Distance;
                    targetHit = hit;
                    hasTarget = true;
                }
                else if (!hit.IsTarget && hit.BlocksInteraction && hit.Distance < nearestBlockingDistance)
                {
                    nearestBlockingDistance = hit.Distance;
                }
            }

            return hasTarget && (!requireClearPath || nearestTargetDistance <= nearestBlockingDistance);
        }
    }
}
