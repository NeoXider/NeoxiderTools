using UnityEngine;

namespace Neo
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudio))]
        public class PlayAudio : MonoBehaviour
        {
            [SerializeField] private int _clipType;

            [SerializeField] private bool _playOnAwake;

            [SerializeField] private float _volume = 1;

            private void Start()
            {
                if (_playOnAwake)
                    AudioPlay();
            }

            public void AudioPlay()
            {
                AM.I.Play(_clipType, _volume);
            }
        }
    }
}