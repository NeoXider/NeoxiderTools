using Neo.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Audio
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

            [Header("By Sound Id (recommended)")]
            [Tooltip("Sound entry on AM, chosen by id. Survives reordering the list, unlike the index below.")]
            [AudioId(AudioIdKind.Sound)]
            [SerializeField]
            private string _soundId = string.Empty;

            [Header("Legacy Mode (by index)")] [SerializeField]
            private int _idClip;

            [Header("New Mode (by Clip)")] [SerializeField]
            private AudioClip[] _clips;

            [Header("Trigger")] [SerializeField] private TriggerMode _triggerMode = TriggerMode.PointerClick;
            [SerializeField] private bool _useRandomClip;

            // WHY the default is negative and not 1: this value REPLACES the entry's own volume multiplier,
            // so a fresh component pointed at an entry authored at 0.5 used to play it at full with nothing
            // to explain it. Negative means "whatever the entry says". Components serialized before this
            // carry an explicit 1 and go on overriding, exactly as they did.
            [Tooltip("Volume for this play, replacing the AM entry's own volume multiplier. " +
                     "Negative = use the entry's volume. Still multiplied by the effects channel.")]
            [Range(-1f, AudioEntry.MaxVolume)]
            [SerializeField]
            private float _volume = -1f;

            /// <summary>True when this component overrides the entry's own volume rather than deferring to it.</summary>
            private bool HasVolumeOverride => _volume >= 0f;

            /// <summary>
            ///     Plays the sound. Explicit clips win, then the sound id, then the legacy index - so an
            ///     existing component that only has an index set behaves exactly as it always did.
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
                        // A loose clip has no entry to defer to, so "no override" is simply full volume.
                        AM.I?.Play(clipToPlay, HasVolumeOverride ? _volume : 1f);
                    }
                    else
                    {
                        global::NeoDiagnostics.LogWarning("[PlayAudioBtn] Selected clip is null.");
                    }
                }
                else if (!string.IsNullOrEmpty(_soundId))
                {
                    if (HasVolumeOverride)
                    {
                        AM.I?.Play(_soundId, _volume);
                    }
                    else
                    {
                        AM.I?.Play(_soundId);
                    }
                }
                else
                {
                    // WHY this branch ignores _volume: it always has. The legacy index path reads the volume
                    // stored on the record itself, and passing 1 here would override exactly the value the
                    // record exists to carry.
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
