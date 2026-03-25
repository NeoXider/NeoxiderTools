using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     Save system settings.
    ///     ScriptableObject that configures the active save provider.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveProviderSettings", menuName = "Neoxider/Save/Save Provider Settings", order = 1)]
    public class SaveProviderSettings : ScriptableObject
    {
        [Header("Provider Type")] [Tooltip("Provider type for saving data")] [SerializeField]
        private SaveProviderType _providerType = SaveProviderType.PlayerPrefs;

        [Header("File Settings")]
        [Tooltip("Save file name (used only for File provider). Default: save.json")]
        [SerializeField]
        private string _fileName = "save.json";

        /// <summary>
        ///     Provider type.
        /// </summary>
        public SaveProviderType ProviderType => _providerType;

        /// <summary>
        ///     Save file name (used only for the File provider).
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        ///     Creates a provider instance from these settings.
        /// </summary>
        /// <returns>ISaveProvider matching the configured type</returns>
        public ISaveProvider CreateProvider()
        {
            switch (_providerType)
            {
                case SaveProviderType.PlayerPrefs:
                    return new PlayerPrefsSaveProvider();

                case SaveProviderType.File:
                    return new FileSaveProvider(string.IsNullOrEmpty(_fileName) ? "save.json" : _fileName);

                default:
                    Debug.LogWarning(
                        $"[SaveProviderSettings] Unknown provider type: {_providerType}. Using PlayerPrefs as default.");
                    return new PlayerPrefsSaveProvider();
            }
        }
    }
}
