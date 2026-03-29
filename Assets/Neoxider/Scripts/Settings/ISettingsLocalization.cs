namespace Neo.Settings
{
    /// <summary>Optional localization for Settings UI keys. Implemented outside Neo.Settings.</summary>
    public interface ISettingsLocalization
    {
        /// <summary>Returns a display string for a settings label key, or the key itself if unknown.</summary>
        string Get(string key);
    }
}
