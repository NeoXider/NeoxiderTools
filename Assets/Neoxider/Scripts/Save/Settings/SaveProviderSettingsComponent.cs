using UnityEngine;

namespace Neo.Save
{
    /// <summary>
    ///     MonoBehaviour component for configuring the save provider from the Inspector.
    ///     Initializes SaveProvider with settings from a ScriptableObject without requiring an asset in Resources.
    ///     Runs its <c>Awake</c> before other scripts of the same scene (<see cref="ExecutionOrder" />), so
    ///     <see cref="SaveManager" />, <c>GameSettingsComponent</c> and other <c>Awake</c> readers load from the
    ///     configured provider.
    /// </summary>
    [NeoDoc("Save/SaveProviderSettingsComponent.md")]
    [CreateFromMenu("Neoxider/Save/SaveProviderSettingsComponent")]
    [AddComponentMenu("Neoxider/" + "Save/" + nameof(SaveProviderSettingsComponent))]
    [DefaultExecutionOrder(ExecutionOrder)]
    public class SaveProviderSettingsComponent : MonoBehaviour
    {
        /// <summary>
        ///     Script execution order of this component: earlier than every other Neoxider component, because they
        ///     may read saved data in their own <c>Awake</c>.
        /// </summary>
        public const int ExecutionOrder = -1000;

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
                    // WHY: a rejected configuration silently keeps the previous provider (usually PlayerPrefs), so
                    // saves would land in the wrong backend; report it even with debug logging off.
                    SaveProvider.LogCritical(error, this);
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
