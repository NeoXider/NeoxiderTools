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
        [Tooltip("Имя параметра экспозиции в AudioMixer (например MasterVolume, MusicVolume, EfxVolume).")]
        public string parameterName = "MasterVolume";

        [Header("References")]
        public AudioMixer audioMixer;

        public const float MaxDb = 20f;
        public const float MinDb = -80f;

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

        /// <summary>
        ///     Возвращает нормализованную громкость (0–1) текущего параметра микшера.
        /// </summary>
        public float GetVolume()
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName) || !audioMixer.GetFloat(parameterName, out float db))
            {
                return 0f;
            }

            return db <= MinDb ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }

        /// <summary>
        ///     Устанавливает громкость по нормализованному значению 0–1. Один параметр — задаётся в parameterName.
        /// </summary>
        public void SetVolume(float normalizedVolume)
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            float db = normalizedVolume > 0f ? Mathf.Log10(Mathf.Clamp01(normalizedVolume)) * 20f : MinDb;
            audioMixer.SetFloat(parameterName, Mathf.Clamp(db, MinDb, MaxDb));
        }
    }
}