namespace Neo.Settings
{
    /// <summary>How a mutation is written to SaveProvider.</summary>
    public enum SettingsPersistMode
    {
        /// <summary>Queue save according to group rules (may flush soon).</summary>
        Immediate = 0,

        /// <summary>Delay save until debounce elapses (e.g. mouse sensitivity slider).</summary>
        Deferred = 1,

        /// <summary>Do not schedule save until flush (see <see cref="GameSettings.FlushPendingSettingsSave"/>).</summary>
        SkipUntilFlush = 2
    }
}
