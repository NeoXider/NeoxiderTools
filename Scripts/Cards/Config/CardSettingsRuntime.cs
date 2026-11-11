using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Global runtime settings for the card system.
    /// </summary>
    public static class CardSettingsRuntime
    {
        /// <summary>
        ///     Global animation config when a component has no local source.
        ///     Cleared automatically at the start of every play session.
        /// </summary>
        public static CardAnimationConfig GlobalAnimationConfig { get; set; }

        // WHY: with domain reload disabled, statics survive between play sessions; a config published by a
        // deck of the previous session would silently drive boards and views of the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            GlobalAnimationConfig = null;
        }
    }
}
