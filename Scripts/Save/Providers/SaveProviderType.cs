namespace Neo.Save
{
    /// <summary>
    ///     Provider type for the save system.
    /// </summary>
    public enum SaveProviderType
    {
        /// <summary>
        ///     Persist via Unity PlayerPrefs (default).
        /// </summary>
        PlayerPrefs,

        /// <summary>
        ///     Persist to a JSON file.
        /// </summary>
        File
    }
}
