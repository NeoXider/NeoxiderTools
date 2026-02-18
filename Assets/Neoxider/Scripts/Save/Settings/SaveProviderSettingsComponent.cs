using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     MonoBehaviour компонент для настройки провайдера сохранения через Inspector.
    ///     Позволяет инициализировать SaveProvider с настройками из ScriptableObject без необходимости размещения в Resources.
    /// </summary>
    [NeoDoc("Save/SaveProviderSettingsComponent.md")]
    [AddComponentMenu("Neoxider/" + "Save/" + nameof(SaveProviderSettingsComponent))]
    public class SaveProviderSettingsComponent : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip(
            "Настройки провайдера сохранения. Если не указаны, будет использован провайдер по умолчанию (PlayerPrefs).")]
        [SerializeField]
        private SaveProviderSettings _settings;

        private void Awake()
        {
            InitializeProvider();
        }

        private void InitializeProvider()
        {
            if (_settings != null)
            {
                ISaveProvider provider = _settings.CreateProvider();
                SaveProvider.SetProvider(provider);
            }
        }
    }
}
