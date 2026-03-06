using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Neo.Audio
{
    [NeoDoc("Audio/SettingMixer.md")]
    [CreateFromMenu("Neoxider/Audio/SettingMixer")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(SettingMixer))]
    public class SettingMixer : MonoBehaviour
    {
        public enum MixerParameterType
        {
            Master,
            Music,
            Efx,
            Custom
        }

        [Header("Settings")]
        [Tooltip("Preset exposed parameter type or Custom.")]
        public MixerParameterType parameterType = MixerParameterType.Master;

        [Tooltip("Used only when parameterType = Custom.")]
        [FormerlySerializedAs("parameterName")]
        public string customParameterName = "MasterVolume";

        [Header("References")] [Tooltip("AudioMixer to control.")]
        public AudioMixer audioMixer;

        public const float MaxDb = 20f;
        public const float MinDb = -80f;

        private const float MuteThreshold = 0.0001f;
        [SerializeField]
        private bool migratedLegacyParameter;

        private string ParameterName => GetParameterName();

        private void Awake()
        {
            TryMigrateLegacyParameter();
        }

        private void OnValidate()
        {
            TryMigrateLegacyParameter();
        }

        /// <summary>Sets volume in dB (−80…20). For UnityEvent and dB slider.</summary>
        public void SetVolumeDb(float volumeDb)
        {
            string parameterName = ParameterName;
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            audioMixer.SetFloat(parameterName, Mathf.Clamp(volumeDb, MinDb, MaxDb));
        }

        /// <summary>Sets volume in dB (−80…20) for the given parameter. If name is empty, parameterName is used.</summary>
        /// <param name="name">Mixer parameter name, or empty to use parameterName.</param>
        /// <param name="volumeDb">Volume in dB.</param>
        public void SetVolumeDb(string name, float volumeDb)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("[SettingMixer] AudioMixer не установлен.");
                return;
            }

            string param = string.IsNullOrEmpty(name) ? ParameterName : name;
            if (string.IsNullOrEmpty(param))
            {
                return;
            }

            audioMixer.SetFloat(param, Mathf.Clamp(volumeDb, MinDb, MaxDb));
        }

        /// <summary>Normalized volume 0–1. For slider and UnityEvent. Zero sets mute (−80 dB).</summary>
        /// <param name="normalizedVolume">Volume from 0 to 1.</param>
        public void SetVolume(float normalizedVolume)
        {
            string parameterName = ParameterName;
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedVolume);
            float db = clamped <= MuteThreshold ? MinDb : Mathf.Log10(clamped) * 20f;
            audioMixer.SetFloat(parameterName, Mathf.Clamp(db, MinDb, MaxDb));
        }

        /// <summary>Enables or disables volume: true = full volume (1), false = mute (0).</summary>
        public void SetVolumeEnabled(bool enabled)
        {
            SetVolume(enabled ? 1f : 0f);
        }

        /// <summary>Returns normalized volume (0–1) of the current mixer parameter.</summary>
        /// <returns>Volume from 0 to 1.</returns>
        public float GetVolume()
        {
            string parameterName = ParameterName;
            if (audioMixer == null || string.IsNullOrEmpty(parameterName) ||
                !audioMixer.GetFloat(parameterName, out float db))
            {
                return 0f;
            }

            return db <= MinDb ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }

        /// <summary>Sets current parameter by normalized 0..1 value.</summary>
        public void Set(float normalizedVolume)
        {
            SetVolume(normalizedVolume);
        }

        /// <summary>Sets current parameter by enabled state (true=1, false=0).</summary>
        public void Set(bool enabled)
        {
            SetVolumeEnabled(enabled);
        }

        /// <summary>Sets specific preset parameter by normalized value.</summary>
        public void Set(MixerParameterType targetType, float normalizedVolume)
        {
            SetVolumeInternal(GetParameterName(targetType), normalizedVolume);
        }

        /// <summary>Sets specific preset parameter by enabled state.</summary>
        public void Set(MixerParameterType targetType, bool enabled)
        {
            SetVolumeInternal(GetParameterName(targetType), enabled ? 1f : 0f);
        }

        /// <summary>Sets custom parameter by normalized value.</summary>
        public void SetCustom(string parameterName, float normalizedVolume)
        {
            SetVolumeInternal(parameterName, normalizedVolume);
        }

        /// <summary>Sets custom parameter by enabled state.</summary>
        public void SetCustom(string parameterName, bool enabled)
        {
            SetVolumeInternal(parameterName, enabled ? 1f : 0f);
        }

        private void SetVolumeInternal(string parameterName, float normalizedVolume)
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedVolume);
            float db = clamped <= MuteThreshold ? MinDb : Mathf.Log10(clamped) * 20f;
            audioMixer.SetFloat(parameterName, Mathf.Clamp(db, MinDb, MaxDb));
        }

        private string GetParameterName()
        {
            return GetParameterName(parameterType);
        }

        private string GetParameterName(MixerParameterType targetType)
        {
            switch (targetType)
            {
                case MixerParameterType.Master:
                    return "MasterVolume";
                case MixerParameterType.Music:
                    return "MusicVolume";
                case MixerParameterType.Efx:
                    return "EfxVolume";
                case MixerParameterType.Custom:
                    return customParameterName;
                default:
                    return customParameterName;
            }
        }

        private void TryMigrateLegacyParameter()
        {
            if (migratedLegacyParameter || string.IsNullOrEmpty(customParameterName))
            {
                return;
            }

            switch (customParameterName)
            {
                case "MasterVolume":
                    parameterType = MixerParameterType.Master;
                    break;
                case "MusicVolume":
                    parameterType = MixerParameterType.Music;
                    break;
                case "EfxVolume":
                    parameterType = MixerParameterType.Efx;
                    break;
                default:
                    parameterType = MixerParameterType.Custom;
                    break;
            }

            migratedLegacyParameter = true;
        }
    }
}