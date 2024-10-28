using UnityEngine;
using UnityEngine.Audio;

namespace Neoxider
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(AudioSetting))]
        public class AudioSetting : MonoBehaviour
        {
            public AudioMixer audioMixer;

            public void SetVolume(float volume)
            {
                audioMixer.SetFloat("Master", volume);
            }
        }
    }
}