using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Physics-independent description of a candidate hit used by interaction queries.
    /// </summary>
    public readonly struct InteractionRayHit
    {
        public InteractionRayHit(float distance, Vector3 point, bool isTarget, bool blocksInteraction)
        {
            Distance = distance;
            Point = point;
            IsTarget = isTarget;
            BlocksInteraction = blocksInteraction;
        }

        public float Distance { get; }

        public Vector3 Point { get; }

        public bool IsTarget { get; }

        public bool BlocksInteraction { get; }
    }
}
