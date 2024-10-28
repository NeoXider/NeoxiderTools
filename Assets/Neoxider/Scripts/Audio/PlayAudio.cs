using UnityEngine;

namespace Neoxider
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudio))]
        public class PlayAudio : MonoBehaviour
        {
            [SerializeField]
            private ClipType _clipType;

            [SerializeField]
            private bool _playOnAwake = true;

            [SerializeField]
            private float _volume = 1;

            private void Start()
            {
                if (_playOnAwake)
                    AudioPlay();
            }

            public void AudioPlay()
            {
                AudioManager.PlaySound(_clipType);
            }

            private void OnValidate()
            {
                name = nameof(PlayAudio) + " " + _clipType.ToString();
            }
        }
    }
}
