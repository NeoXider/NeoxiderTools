namespace Neo.NoCode
{
    /// <summary>
    ///     How often the binding refreshes the target UI.
    /// </summary>
    public enum NoCodeFloatUpdateMode
    {
        /// <summary>Resolve and apply once when enabled.</summary>
        Once = 0,

        /// <summary>
        ///     Subscribe when the member holds <see cref="Neo.Reactive.ReactivePropertyFloat"/>,
        ///     <see cref="Neo.Reactive.ReactivePropertyInt"/>, or <see cref="Neo.Reactive.ReactivePropertyBool"/>.
        ///     Non-reactive numeric members fall back to polling with the component poll interval.
        /// </summary>
        Reactive = 1,

        /// <summary>Refresh in <see cref="UnityEngine.MonoBehaviour.LateUpdate"/> using the component poll interval.</summary>
        Poll = 2
    }
}
