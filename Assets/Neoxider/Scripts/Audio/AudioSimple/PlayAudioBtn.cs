using Neo.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Neo
{
    namespace Audio
    {
        /// <summary>Plays sound on button click. Supports specific clip by ID or random clip from list.</summary>
        [NeoDoc("Audio/PlayAudioBtn.md")]
        [CreateFromMenu("Neoxider/Audio/PlayAudioBtn")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudioBtn))]
        public class PlayAudioBtn : MonoBehaviour
        {
            [Header("Legacy Mode (by ID)")] [SerializeField]
            private int _idClip;

            [Header("New Mode (by Clip)")] [SerializeField]
            private AudioClip[] _clips;

            [SerializeField] private bool _useRandomClip;
            [SerializeField] private float _volume = 1f;

            [SerializeField] [GetComponent] private Button _button;

            private void OnEnable()
            {
                if (_button != null)
                {
                    _button.onClick.AddListener(AudioPlay);
                }
            }

            private void OnDisable()
            {
                if (_button != null)
                {
                    _button.onClick.RemoveListener(AudioPlay);
                }
            }

            /// <summary>Plays the sound. If clips are set and useRandomClip is true, picks random; otherwise uses legacy ID mode.</summary>
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
                        Debug.LogWarning("[PlayAudioBtn] Выбранный клип равен null.");
                    }
                }
                else
                {
                    AM.I?.Play(_idClip);
                }
            }
        }
    }
}
