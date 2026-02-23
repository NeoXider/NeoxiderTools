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

        private const float MuteThreshold = 0.0001f;

        /// <summary>
        ///     Громкость в дБ (−80…20). Для UnityEvent и слайдера в дБ.
        /// </summary>
        public void SetVolumeDb(float volumeDb)
        {
            if (audioMixer == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }
            audioMixer.SetFloat(parameterName, Mathf.Clamp(volumeDb, MinDb, MaxDb));
        }

        /// <summary>
        ///     Громкость в дБ (−80…20) для указанного параметра. Если name пустой — используется parameterName.
        /// </summary>
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
        ///     Нормализованная громкость 0–1. Для слайдера и UnityEvent. Ноль гарантированно ставит mute (−80 дБ).
        /// </summary>
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

        /// <summary>
        ///     Вкл/выкл по флагу: true — полная громкость (1), false — mute (0).
        /// </summary>
        public void SetVolumeEnabled(bool enabled)
        {
            SetVolume(enabled ? 1f : 0f);
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
    }
}