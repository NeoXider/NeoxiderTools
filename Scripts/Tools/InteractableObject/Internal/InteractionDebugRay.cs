using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Debug-ray state of one <see cref="InteractiveObject" />: the last recorded segment and colour, how long the
    ///     legacy one-shot ray stays visible, and the colour rule of the persistent per-frame ray. Plain C#, so the
    ///     rules are testable without a scene; drawing stays with the component.
    /// </summary>
    internal sealed class InteractionDebugRay
    {
        public Vector3 Start { get; private set; }

        public Vector3 End { get; private set; }

        public Color Color { get; private set; } = Color.cyan;

        /// <summary>Realtime until which a one-shot ray stays visible; infinite while recorded as persistent.</summary>
        public float VisibleUntil { get; private set; }

        /// <summary>
        ///     Records an interaction ray. A persistent recording never expires; a one-shot recording stays visible
        ///     for <paramref name="duration" /> seconds of realtime after <paramref name="now" />.
        /// </summary>
        public void Record(Vector3 start, Vector3 end, Color color, bool persistent, float now, float duration)
        {
            SetSegment(start, end, color);
            VisibleUntil = persistent ? float.PositiveInfinity : now + Mathf.Max(0f, duration);
        }

        /// <summary>Updates the drawn segment without changing how long a one-shot ray stays visible.</summary>
        public void SetSegment(Vector3 start, Vector3 end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }

        /// <summary>The persistent ray is always drawn; the one-shot ray only until it expires.</summary>
        public bool IsVisible(bool persistent, bool oneShot, float now)
        {
            return persistent || (oneShot && now <= VisibleUntil);
        }

        /// <summary>Green while interacting, yellow while hovered, cyan in range, gray otherwise.</summary>
        public static Color ResolvePersistentColor(bool interacting, bool hovered, bool inRange)
        {
            if (interacting)
            {
                return Color.green;
            }

            if (hovered)
            {
                return Color.yellow;
            }

            return inRange ? Color.cyan : Color.gray;
        }
    }
}
