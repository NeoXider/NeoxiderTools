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

        [Header("File encryption (AES + Base64)")]
        [Tooltip(
            "When using File provider: encrypt JSON on disk (AES-CBC). OFF by default. If enabled with empty key/IV below, built-in defaults from SaveFileEncryption are used (replace for production).")]
        [SerializeField]
        private bool _encryptFileSave;

        [Tooltip(
            "AES key (UTF-8): 16, 24, or 32 bytes. Leave empty together with IV to use built-in default key when encryption is on.")]
        [SerializeField]
        private string _fileEncryptionKey = "";

        [Tooltip(
            "AES IV (UTF-8): 16 bytes. Leave empty together with key to use built-in default IV when encryption is on.")]
        [SerializeField]
        private string _fileEncryptionIv = "";

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
                {
                    var options = new FileSaveProviderOptions();
                    if (_encryptFileSave)
                    {
                        if (FileSaveEncryptionConfig.TryCreate(true, _fileEncryptionKey, _fileEncryptionIv,
                                out FileSaveEncryptionConfig enc, out string err))
                        {
                            options.Encryption = enc;
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveProviderSettings] File encryption disabled: {err}");
                        }
                    }

                    return new FileSaveProvider(string.IsNullOrEmpty(_fileName) ? "save.json" : _fileName, options);
                }

                default:
                    Debug.LogWarning(
                        $"[SaveProviderSettings] Unknown provider type: {_providerType}. Using PlayerPrefs as default.");
                    return new PlayerPrefsSaveProvider();
            }
        }
    }
}
