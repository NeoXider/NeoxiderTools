using System;

namespace Neo.Save
{
    /// <summary>
    ///     Interface for save data providers.
    ///     Exposes a unified API across different persistence backends.
    /// </summary>
    public interface ISaveProvider
    {
        /// <summary>
        ///     Provider type.
        /// </summary>
        SaveProviderType ProviderType { get; }

        /// <summary>
        ///     Raised after data is saved.
        /// </summary>
        event Action OnDataSaved;

        /// <summary>
        ///     Raised after data is loaded.
        /// </summary>
        event Action OnDataLoaded;

        /// <summary>
        ///     Raised when a key's value changes.
        /// </summary>
        /// <param name="key">Key whose value changed</param>
        event Action<string> OnKeyChanged;

        /// <summary>
        ///     Gets an integer value by key.
        /// </summary>
        /// <param name="key">Key to read</param>
        /// <param name="defaultValue">Default if the key is missing</param>
        /// <returns>Stored value or default</returns>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>
        ///     Sets an integer value by key.
        /// </summary>
        /// <param name="key">Key to write</param>
        /// <param name="value">Value to store</param>
        void SetInt(string key, int value);

        /// <summary>
        ///     Gets a floating-point value by key.
        /// </summary>
        /// <param name="key">Key to read</param>
        /// <param name="defaultValue">Default if the key is missing</param>
        /// <returns>Stored value or default</returns>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>
        ///     Sets a floating-point value by key.
        /// </summary>
        /// <param name="key">Key to write</param>
        /// <param name="value">Value to store</param>
        void SetFloat(string key, float value);

        /// <summary>
        ///     Gets a string value by key.
        /// </summary>
        /// <param name="key">Key to read</param>
        /// <param name="defaultValue">Default if the key is missing</param>
        /// <returns>Stored value or default</returns>
        string GetString(string key, string defaultValue = "");

        /// <summary>
        ///     Sets a string value by key.
        /// </summary>
        /// <param name="key">Key to write</param>
        /// <param name="value">Value to store</param>
        void SetString(string key, string value);

        /// <summary>
        ///     Gets a Boolean value by key.
        /// </summary>
        /// <param name="key">Key to read</param>
        /// <param name="defaultValue">Default if the key is missing</param>
        /// <returns>Stored value or default</returns>
        bool GetBool(string key, bool defaultValue = false);

        /// <summary>
        ///     Sets a Boolean value by key.
        /// </summary>
        /// <param name="key">Key to write</param>
        /// <param name="value">Value to store</param>
        void SetBool(string key, bool value);

        /// <summary>
        ///     Returns whether the key exists in storage.
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>true if the key exists; otherwise false</returns>
        bool HasKey(string key);

        /// <summary>
        ///     Removes the key and its value from storage.
        /// </summary>
        /// <param name="key">Key to remove</param>
        void DeleteKey(string key);

        /// <summary>
        ///     Removes all keys from storage.
        /// </summary>
        void DeleteAll();

        /// <summary>
        ///     Forces persistence to storage.
        /// </summary>
        void Save();

        /// <summary>
        ///     Forces a reload from storage.
        /// </summary>
        void Load();
    }
}
