using UnityEngine;
using UnityEngine.Audio;

namespace Neo.Audio
{
    [NeoDoc("Audio/SettingMixer.md")]
    [CreateFromMenu("Neoxider/Audio/SettingMixer")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(SettingMixer))]
    public class SettingMixer : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Exposed parameter name in AudioMixer (e.g. MasterVolume, MusicVolume, EfxVolume).")]
        public string parameterName = "MasterVolume";

        [Header("References")] [Tooltip("AudioMixer to control.")]
        public AudioMixer audioMixer;

        public const float MaxDb = 20f;
        public const float MinDb = -80f;

        private const float MuteThreshold = 0.0001f;

        /// <summary>Sets volume in dB (−80…20). For UnityEvent and dB slider.</summary>
        public void SetVolumeDb(float volumeDb)
        {
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

            string param = string.IsNullOrEmpty(name) ? parameterName : name;
            audioMixer.SetFloat(param, Mathf.Clamp(volumeDb, MinDb, MaxDb));
        }

        /// <summary>Normalized volume 0–1. For slider and UnityEvent. Zero sets mute (−80 dB).</summary>
        /// <param name="normalizedVolume">Volume from 0 to 1.</param>
        public void SetVolume(float normalizedVolume)
        {
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
            if (audioMixer == null || string.IsNullOrEmpty(parameterName) ||
                !audioMixer.GetFloat(parameterName, out float db))
            {
                return 0f;
            }

            return db <= MinDb ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }
    }
}