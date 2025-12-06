using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    /// Настройки для системы сохранения данных.
    /// ScriptableObject для конфигурации провайдера сохранения.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveProviderSettings", menuName = "Neo/Save/Save Provider Settings", order = 1)]
    public class SaveProviderSettings : ScriptableObject
    {
        [Header("Provider Type")]
        [Tooltip("Тип провайдера для сохранения данных")]
        [SerializeField] private SaveProviderType _providerType = SaveProviderType.PlayerPrefs;
        
        [Header("File Settings")]
        [Tooltip("Имя файла для сохранения (используется только для File провайдера). По умолчанию: save.json")]
        [SerializeField] private string _fileName = "save.json";
        
        /// <summary>
        /// Тип провайдера.
        /// </summary>
        public SaveProviderType ProviderType => _providerType;
        
        /// <summary>
        /// Имя файла для сохранения (используется только для File провайдера).
        /// </summary>
        public string FileName => _fileName;
        
        /// <summary>
        /// Создает и возвращает провайдер на основе настроек.
        /// </summary>
        /// <returns>Экземпляр ISaveProvider в соответствии с настройками</returns>
        public ISaveProvider CreateProvider()
        {
            switch (_providerType)
            {
                case SaveProviderType.PlayerPrefs:
                    return new PlayerPrefsSaveProvider();
                    
                case SaveProviderType.File:
                    return new FileSaveProvider(string.IsNullOrEmpty(_fileName) ? "save.json" : _fileName);
                    
                default:
                    Debug.LogWarning($"[SaveProviderSettings] Unknown provider type: {_providerType}. Using PlayerPrefs as default.");
                    return new PlayerPrefsSaveProvider();
            }
        }
    }
}

