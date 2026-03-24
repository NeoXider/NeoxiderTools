using Neo.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo
{
    namespace Audio
    {
        /// <summary>Plays sound on button click. Supports specific clip by ID or random clip from list.</summary>
        [NeoDoc("Audio/PlayAudioBtn.md")]
        [CreateFromMenu("Neoxider/Audio/PlayAudioBtn")]
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudioBtn))]
        public class PlayAudioBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
            IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
        {
            public enum TriggerMode
            {
                PointerClick,
                PointerEnter,
                PointerExit,
                PointerDown,
                PointerUp,
                Select,
                Deselect,
                Submit,
                Manual
            }

            [Header("Legacy Mode (by ID)")] [SerializeField]
            private int _idClip;

            [Header("New Mode (by Clip)")] [SerializeField]
            private AudioClip[] _clips;

            [Header("Trigger")] [SerializeField] private TriggerMode _triggerMode = TriggerMode.PointerClick;
            [SerializeField] private bool _useRandomClip;
            [SerializeField] private float _volume = 1f;

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

            public void OnPointerEnter(PointerEventData eventData)
            {
                TryTrigger(TriggerMode.PointerEnter);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                TryTrigger(TriggerMode.PointerExit);
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                TryTrigger(TriggerMode.PointerDown);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                TryTrigger(TriggerMode.PointerUp);
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                {
                    return;
                }

                TryTrigger(TriggerMode.PointerClick);
            }

            public void OnSelect(BaseEventData eventData)
            {
                TryTrigger(TriggerMode.Select);
            }

            public void OnDeselect(BaseEventData eventData)
            {
                TryTrigger(TriggerMode.Deselect);
            }

            public void OnSubmit(BaseEventData eventData)
            {
                TryTrigger(TriggerMode.Submit);
            }

            private void TryTrigger(TriggerMode triggerMode)
            {
                if (_triggerMode != triggerMode)
                {
                    return;
                }

                AudioPlay();
            }
        }
    }
}
