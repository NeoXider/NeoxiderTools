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
            "Save provider settings. If unset, keeps the current provider or uses normal Resources/default initialization.")]
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
                if (!_settings.TryCreateProvider(out ISaveProvider provider, out string error))
                {
                    SaveProvider.LogError(error, this);
                    return;
                }

                SaveProvider.SetProvider(provider);
                return;
            }

            _ = SaveProvider.CurrentProvider;
            SaveProvider.Log(
                "SaveProviderSettingsComponent has no settings assigned; using the current or lazily initialized provider.",
                this);
        }
    }
}
