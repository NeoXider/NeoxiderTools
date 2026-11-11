namespace Neo.Tools
{
    /// <summary>
    ///     Implemented on world/dropped item behaviours to capture and restore per-instance inventory JSON.
    /// </summary>
    public interface IInventoryItemState
    {
        /// <summary>Unique key stored with the instance payload (defaults to type full name if unset).</summary>
        string InventoryStateKey { get; }

        /// <summary>Serialize current state to JSON for <see cref="InventoryItemComponentState.Json" />.</summary>
        string CaptureInventoryState();

        /// <summary>Apply JSON previously returned from <see cref="CaptureInventoryState" />.</summary>
        /// <param name="json">Payload from save or pickup capture.</param>
        void RestoreInventoryState(string json);
    }
}
