namespace Neo.Tools
{
    /// <summary>
    ///     Typed runtime contract for objects that can receive a press-style interaction.
    ///     Custom interaction sources can depend on this contract without requiring
    ///     <see cref="InteractiveObject" /> or UnityEvent wiring.
    /// </summary>
    public interface IInteractiveTarget
    {
        /// <summary>Whether the target currently accepts interaction commands.</summary>
        bool IsInteractable { get; }

        /// <summary>Begins an interaction through the target's normal dispatch path.</summary>
        void InteractDown();

        /// <summary>Ends an interaction through the target's normal dispatch path.</summary>
        void InteractUp();
    }
}
