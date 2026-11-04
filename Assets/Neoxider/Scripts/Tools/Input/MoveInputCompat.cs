using System;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Two-axis movement on either input backend, so gameplay code needs no backend switch. The legacy Input
    ///     Manager reads its named raw axes (<c>Input.GetAxisRaw</c>); the Input System path reads WASD, the arrow
    ///     keys and the gamepad left stick through <see cref="OptionalInputSystemAdapter.ReadMove" />, which does not
    ///     allocate once warm. Uses the shared legacy-first selection of <see cref="InputBackendCompat" /> and samples
    ///     at the moment of the call.
    /// </summary>
    public static class MoveInputCompat
    {
        /// <summary>The Input Manager's default horizontal axis.</summary>
        public const string DefaultHorizontalAxis = "Horizontal";

        /// <summary>The Input Manager's default vertical axis.</summary>
        public const string DefaultVerticalAxis = "Vertical";

        /// <summary>
        ///     Raw movement from the default <see cref="DefaultHorizontalAxis" /> and <see cref="DefaultVerticalAxis" />
        ///     axes. See <see cref="ReadMove(string, string)" />.
        /// </summary>
        public static Vector2 ReadMove()
        {
            return ReadMove(DefaultHorizontalAxis, DefaultVerticalAxis);
        }

        /// <summary>
        ///     Raw movement this frame: x is right, y is up (forward). Each component stays in -1..1 and the
        ///     diagonal is not normalized, so normalize it yourself when diagonal speed must not grow. The axis
        ///     names apply only to the legacy Input Manager; like <c>Input.GetAxisRaw</c>, a name that is not set up
        ///     there throws <see cref="ArgumentException" />. The Input System path ignores them and returns zero
        ///     without a keyboard or gamepad.
        /// </summary>
        public static Vector2 ReadMove(string horizontalAxis, string verticalAxis)
        {
            if (InputBackendCompat.UsesLegacyInput)
            {
                try
                {
                    return new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis));
                }
                catch (InvalidOperationException)
                {
                    InputBackendCompat.MarkLegacyUnavailable();
                }
            }

            return OptionalInputSystemAdapter.ReadMove();
        }
    }
}
