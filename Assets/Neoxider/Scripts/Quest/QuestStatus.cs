namespace Neo.Quest
{
    /// <summary>
    ///     Quest status at runtime.
    /// </summary>
    public enum QuestStatus
    {
        /// <summary>Quest not started.</summary>
        NotStarted,

        /// <summary>Quest active; objectives in progress.</summary>
        Active,

        /// <summary>All objectives done; quest completed.</summary>
        Completed,

        /// <summary>Quest failed (optional).</summary>
        Failed
    }
}
