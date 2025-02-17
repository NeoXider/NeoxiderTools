using UnityEngine;
using UnityEngine.Audio;

namespace Neo
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(SettingMixer))]
        public class SettingMixer : MonoBehaviour
        {
            public AudioMixer audioMixer;

            public void SetVolume(float volume)
            {
                audioMixer.SetFloat("Master", volume);
            }
        }
    }
}