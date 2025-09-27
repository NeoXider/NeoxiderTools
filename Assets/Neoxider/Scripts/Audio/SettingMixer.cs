using UnityEngine;
using UnityEngine.Audio;

namespace Neo
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(SettingMixer))]
        public class SettingMixer : MonoBehaviour
        {
            public string nameMixer = "Master";
            public AudioMixer audioMixer;
            public readonly float Max = 20;

            public readonly float Min = -80;

            public void SetVolume(string name = "", float volume = 0)
            {
                name = string.IsNullOrEmpty(name) ? nameMixer : name;
                audioMixer.SetFloat(name, volume);
            }
        }
    }
}