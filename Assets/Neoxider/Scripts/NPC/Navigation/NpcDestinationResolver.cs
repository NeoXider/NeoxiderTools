using UnityEngine;
using UnityEngine.AI;

namespace Neo.NPC.Navigation
{
    /// <summary>
    ///     Resolves desired positions to nearest valid NavMesh points.
    /// </summary>
    public static class NpcDestinationResolver
    {
        /// <summary>
        ///     Try to resolve a desired position to a valid NavMesh point.
        ///     Performs a ring search around the desired position.
        /// </summary>
        public static bool TryResolve(
            Vector3 desired,
            float maxSampleDistance,
            int areaMask,
            out Vector3 resolved)
        {
            resolved = default;

            float maxDist = Mathf.Max(0.1f, maxSampleDistance);

            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, maxDist, areaMask))
            {
                resolved = hit.position;
                return true;
            }

            const int rings = 6;
            const int pointsPerRing = 10;

            float step = maxDist / rings;

            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = step * ring;
                float sampleDist = step;

                for (int i = 0; i < pointsPerRing; i++)
                {
                    float angle = i / (float)pointsPerRing * Mathf.PI * 2f;
                    Vector3 offset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    Vector3 candidate = desired + offset;

                    if (NavMesh.SamplePosition(candidate, out hit, sampleDist, areaMask))
                    {
                        resolved = hit.position;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}