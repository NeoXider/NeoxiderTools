using UnityEngine;
using UnityEngine.Audio;

namespace Neo.Audio
{
    [NeoDoc("Audio/SettingMixer.md")]
    [CreateFromMenu("Neoxider/Audio/SettingMixer")]
    [AddComponentMenu("Neoxider/" + "Audio/" + nameof(SettingMixer))]
    public class SettingMixer : MonoBehaviour
    {
        public const int GroupMaster = 0;
        public const int GroupMusic = 1;
        public const int GroupSfx = 2;

        [Header("Settings")]
        public string nameMixer = "Master";

        [Tooltip("Parameter names: [0]=Master, [1]=Music, [2]=Sfx. Used by GetVolumeByGroup/SetVolumeByGroup.")]
        [SerializeField]
        private string[] _groupParameterNames = { "MasterVolume", "MusicVolume", "EfxVolume" };

        [Header("References")]
        public AudioMixer audioMixer;

        public const float MaxDb = 20f;
        public const float MinDb = -80f;

        public void SetVolume(string name = "", float volumeDb = 0f)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning($"[SettingMixer] AudioMixer не установлен! Нельзя установить громкость для '{nameMixer}'.");
                return;
            }

            name = string.IsNullOrEmpty(name) ? nameMixer : name;
            audioMixer.SetFloat(name, Mathf.Clamp(volumeDb, MinDb, MaxDb));
        }

        /// <summary>
        ///     Возвращает нормализованную громкость (0–1) группы по индексу. 0=Master, 1=Music, 2=Sfx.
        ///     Для NeoCondition: Property = GetVolumeByGroup, Argument = 0/1/2.
        /// </summary>
        public float GetVolumeByGroup(int groupIndex)
        {
            if (audioMixer == null || _groupParameterNames == null || groupIndex < 0 || groupIndex >= _groupParameterNames.Length)
            {
                return 0f;
            }

            string param = _groupParameterNames[groupIndex];
            if (string.IsNullOrEmpty(param) || !audioMixer.GetFloat(param, out float db))
            {
                return 0f;
            }

            return db <= MinDb ? 0f : Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
        }

        /// <summary>
        ///     Устанавливает громкость группы по индексу (0=Master, 1=Music, 2=Sfx). Нормализованное значение 0–1.
        /// </summary>
        public void SetVolumeByGroup(int groupIndex, float normalizedVolume)
        {
            if (audioMixer == null || _groupParameterNames == null || groupIndex < 0 || groupIndex >= _groupParameterNames.Length)
            {
                return;
            }

            string param = _groupParameterNames[groupIndex];
            if (string.IsNullOrEmpty(param))
            {
                return;
            }

            float db = normalizedVolume > 0f ? Mathf.Log10(Mathf.Clamp01(normalizedVolume)) * 20f : MinDb;
            audioMixer.SetFloat(param, Mathf.Clamp(db, MinDb, MaxDb));
        }
    }
}