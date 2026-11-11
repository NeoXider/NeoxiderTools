namespace Neo.Cards
{
    /// <summary>
    ///     Built-in card animation kinds for <see cref="CardViewAnimationTemplates" /> and
    ///     <see cref="ICardViewAnimations" />.
    /// </summary>
    public enum CardViewAnimationType
    {
        /// <summary>Bouncy scale (DOPunchScale).</summary>
        Bounce,

        /// <summary>Single scale pulse cycle.</summary>
        Pulse,

        /// <summary>Looped scale pulse.</summary>
        PulseLooped,

        /// <summary>Position shake (DOShakePosition).</summary>
        Shake,

        /// <summary>Brief highlight (color/alpha).</summary>
        Highlight,

        /// <summary>Fly-in with appear (move + scale/alpha).</summary>
        FlyIn,

        /// <summary>Subtle idle sway loop.</summary>
        Idle
    }
}
