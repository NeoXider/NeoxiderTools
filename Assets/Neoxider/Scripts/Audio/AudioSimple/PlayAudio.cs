using Neo.Extensions;
using UnityEngine;

namespace Neo
{
    namespace Audio
    {
        /// <summary>
        ///     Компонент для воспроизведения звукового эффекта из AM.
        ///     Поддерживает воспроизведение конкретного клипа по ID или случайного клипа из списка.
        /// </summary>
        [NeoDoc("Audio/PlayAudio.md")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudio))]
        public class PlayAudio : MonoBehaviour
        {
            [Header("Legacy Mode (by ID)")] [SerializeField]
            private int _clipType;

            [Header("New Mode (by Clip)")] [SerializeField]
            private AudioClip[] _clips;

            [SerializeField] private bool _useRandomClip;

            [SerializeField] private bool _playOnAwake;
            [SerializeField] private float _volume = 1;

            private void Start()
            {
                if (_playOnAwake)
                {
                    AudioPlay();
                }
            }

            /// <summary>
            ///     Воспроизводит звук. Если заданы клипы и useRandomClip=true - выбирает случайный из списка.
            ///     Иначе использует режим по ID (legacy).
            /// </summary>
            public void AudioPlay()
            {
                if (_clips != null && _clips.Length > 0)
                {
                    AudioClip clipToPlay;
                    if (_useRandomClip && _clips.Length > 1)
                    {
                        clipToPlay = _clips.GetRandomElement();
                    }
                    else
                    {
                        clipToPlay = _clips[0];
                    }

                    if (clipToPlay != null)
                    {
                        AM.I?.Play(clipToPlay, _volume);
                    }
                    else
                    {
                        Debug.LogWarning("[PlayAudio] Выбранный клип равен null.");
                    }
                }
                else
                {
                    AM.I?.Play(_clipType, _volume);
                }
            }
        }
    }
}
