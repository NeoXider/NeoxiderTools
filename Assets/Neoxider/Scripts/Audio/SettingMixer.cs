using UnityEngine;
using UnityEngine.Audio;

namespace Neo
{
    namespace Audio
    {
        [NeoDoc("Audio/SettingMixer.md")]
        [AddComponentMenu("Neo/" + "Audio/" + nameof(SettingMixer))]
        public class SettingMixer : MonoBehaviour
        {
            [Header("Settings")] public string nameMixer = "Master";

            [Header("References")] public AudioMixer audioMixer;
            public readonly float Max = 20;

            public readonly float Min = -80;

            public void SetVolume(string name = "", float volume = 0)
            {
                if (audioMixer == null)
                {
                    Debug.LogWarning(
                        $"[SettingMixer] AudioMixer не установлен! Нельзя установить громкость для '{nameMixer}'.");
                    return;
                }

                name = string.IsNullOrEmpty(name) ? nameMixer : name;
                audioMixer.SetFloat(name, volume);
            }
        }
    }
}