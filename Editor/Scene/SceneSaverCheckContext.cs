namespace Neo.Editor
{
    /// <summary>
    ///     One immutable snapshot of everything <see cref="SceneSaverAutoSaveScheduler" /> needs to decide
    ///     whether another backup copy is due. Collected by the caller from cheap editor properties, which
    ///     keeps the scheduler itself free of UnityEditor calls and directly testable.
    /// </summary>
    public readonly struct SceneSaverCheckContext
    {
        /// <summary>Editor time of this check, in seconds (<c>EditorApplication.timeSinceStartup</c>).</summary>
        public readonly double Now;

        /// <summary>Configured interval between two backup copies, in seconds.</summary>
        public readonly double IntervalSeconds;

        /// <summary>Asset path of the active scene; empty while the scene has never been saved.</summary>
        public readonly string ScenePath;

        /// <summary>
        ///     Monotonic token that changes whenever the scene content changes. The editor supplies
        ///     <c>Undo.GetCurrentGroup()</c>; tests supply any number they like.
        /// </summary>
        public readonly long Revision;

        /// <summary>Whether the active scene currently has unsaved changes.</summary>
        public readonly bool IsDirty;

        /// <summary>Whether the user asked for backups even while the scene has no unsaved changes.</summary>
        public readonly bool SaveEvenIfNotDirty;

        /// <summary>Creates a check snapshot.</summary>
        /// <param name="now">Editor time of this check, in seconds.</param>
        /// <param name="intervalSeconds">Configured interval between two backup copies, in seconds.</param>
        /// <param name="scenePath">Asset path of the active scene, or an empty string when unsaved.</param>
        /// <param name="revision">Token that changes when the scene content changes.</param>
        /// <param name="isDirty">Whether the active scene has unsaved changes.</param>
        /// <param name="saveEvenIfNotDirty">Whether backups are wanted for a clean scene too.</param>
        public SceneSaverCheckContext(double now, double intervalSeconds, string scenePath, long revision,
            bool isDirty, bool saveEvenIfNotDirty)
        {
            Now = now;
            IntervalSeconds = intervalSeconds;
            ScenePath = scenePath;
            Revision = revision;
            IsDirty = isDirty;
            SaveEvenIfNotDirty = saveEvenIfNotDirty;
        }
    }
}
