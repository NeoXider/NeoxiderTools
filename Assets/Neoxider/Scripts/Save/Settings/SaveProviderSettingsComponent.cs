using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     MonoBehaviour component for configuring the save provider from the Inspector.
    ///     Initializes SaveProvider with settings from a ScriptableObject without requiring an asset in Resources.
    /// </summary>
    [NeoDoc("Save/SaveProviderSettingsComponent.md")]
    [CreateFromMenu("Neoxider/Save/SaveProviderSettingsComponent")]
    [AddComponentMenu("Neoxider/" + "Save/" + nameof(SaveProviderSettingsComponent))]
    public class SaveProviderSettingsComponent : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip(
            "Save provider settings. If unset, the default provider (PlayerPrefs) is used.")]
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
