using System.Globalization;
using System.Text;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Tools.Dialogue</b> demo (Dialogue.unity). The dialogue box, the
    ///     playback buttons and the bark buttons call <see cref="DialogueController" /> and
    ///     <see cref="DialogueLinePlayer" /> directly through persistent UnityEvents; their events call the
    ///     public listeners below, which mirror the live state into the authored labels and the log feed. The
    ///     toggles and the speed slider go through this component because the matching controller settings are
    ///     fields, not methods. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Dialogue Demo")]
    public sealed class DialogueDemoController : MonoBehaviour
    {
        [Header("Modules")]
        [Tooltip("Linear conversation driven by the dialogue box and the playback buttons.")]
        [SerializeField] private DialogueController _dialogue;

        [Tooltip("Pooled bark lines driven by the bark buttons.")]
        [SerializeField] private DialogueLinePlayer _barks;

        [Header("Controls synced on start")]
        [SerializeField] private Toggle _typewriterToggle;
        [SerializeField] private Toggle _autoAdvanceToggle;
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private Toggle _autoIdleToggle;

        [Header("Live values")]
        [SerializeField] private TMP_Text _speakerValue;
        [SerializeField] private TMP_Text _cursorValue;
        [SerializeField] private TMP_Text _stateValue;
        [SerializeField] private Image _typingFill;
        [SerializeField] private TMP_Text _barkStatusValue;

        [Header("Barks")]
        [Tooltip("Seconds between idle barks while the Auto idle toggle is on.")]
        [SerializeField] [Min(0.5f)] private float _autoIdleInterval = 4f;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private readonly StringBuilder _line = new(128);
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _dialogue != null && _barks != null && _log != null && _speakerValue != null &&
                                    _cursorValue != null && _stateValue != null && _barkStatusValue != null;

        public DialogueController Dialogue => _dialogue;
        public DialogueLinePlayer Barks => _barks;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            SyncControls();
            SetText(_barkStatusValue, "silent");
            bool autoStart = _dialogue != null && _dialogue.autoStart;
            // WHY: Start order between components is undefined; an auto-started conversation may already have
            // reported its state through the events, which must not be overwritten here.
            if (_dialogue == null || !_dialogue.DialogueStarted)
            {
                SetTypingFill(0f);
                SetText(_stateValue, autoStart ? "starting" : "press Skip or next");
            }

            Log(autoStart
                ? "DialogueController.autoStart -> StartDialogue()"
                : "Press Skip or next to start the conversation");
        }

        /// <summary>Toggle target: switches the typewriter for the next sentence.</summary>
        public void SetTypewriter(bool enabled)
        {
            if (_dialogue == null)
            {
                WarnUnbuilt();
                return;
            }

            _dialogue.useTypewriterEffect = enabled;
            Log("useTypewriterEffect = " + (enabled ? "true" : "false") + " (from the next sentence)");
        }

        /// <summary>Toggle target: advances to the next sentence by itself after typing.</summary>
        public void SetAutoAdvance(bool enabled)
        {
            if (_dialogue == null)
            {
                WarnUnbuilt();
                return;
            }

            _dialogue.autoNextSentence = enabled;
            Log("autoNextSentence = " + (enabled ? "true" : "false"));
        }

        /// <summary>Slider target: typing speed of <see cref="DialogueController.Typewriter" />.</summary>
        public void SetCharactersPerSecond(float charactersPerSecond)
        {
            if (_dialogue != null && _dialogue.Typewriter != null)
            {
                _dialogue.Typewriter.CharactersPerSecond = Mathf.Max(1f, charactersPerSecond);
            }
        }

        /// <summary>Toggle target: lets the bark player schedule idle lines by itself.</summary>
        public void SetAutoIdle(bool enabled)
        {
            if (_barks == null)
            {
                WarnUnbuilt();
                return;
            }

            _barks.IdleInterval = enabled ? _autoIdleInterval : 0f;
            Log("DialogueLinePlayer.IdleInterval = " +
                _barks.IdleInterval.ToString("0.#", CultureInfo.InvariantCulture) + " s");
        }

        /// <summary>DialogueController.OnCharacterChange listener.</summary>
        public void OnSpeakerChanged(string speaker)
        {
            SetText(_speakerValue, string.IsNullOrEmpty(speaker) ? "-" : speaker);
            Log("OnCharacterChange -> " + speaker);
        }

        /// <summary>DialogueController.OnSentenceEnd listener (raised when a sentence is presented).</summary>
        public void OnSentenceShown()
        {
            if (_dialogue == null)
            {
                return;
            }

            if (_cursorValue != null)
            {
                _line.Clear();
                _line.Append("dialogue ").Append(_dialogue.CurrentDialogueId).Append(" · monolog ")
                    .Append(_dialogue.CurrentMonologId).Append(" · sentence ").Append(_dialogue.CurrentSentenceId);
                _cursorValue.SetText(_line);
            }

            bool typing = _dialogue.useTypewriterEffect && _dialogue.IsTyping;
            SetTypingFill(typing ? 0f : 1f);
            SetText(_stateValue, typing ? "typing" : "waiting for Skip or next");
        }

        /// <summary>DialogueController.OnTypewriterProgress listener (0..1).</summary>
        public void OnTypingProgress(float progress)
        {
            SetTypingFill(progress);
            if (progress >= 1f)
            {
                SetText(_stateValue, "waiting for Skip or next");
            }
        }

        /// <summary>DialogueController.OnMonologEnd listener.</summary>
        public void OnMonologEnded()
        {
            Log("OnMonologEnd");
        }

        /// <summary>DialogueController.OnAllDialoguesEnd listener.</summary>
        public void OnAllDialoguesEnded()
        {
            SetText(_stateValue, "finished - press Restart");
            Log("OnAllDialoguesEnd - press Restart to play it again");
        }

        /// <summary>DialogueLinePlayer.OnLineStarted listener.</summary>
        public void OnBarkStarted(string text)
        {
            PooledDialogueLine current = _barks != null ? _barks.Pool.Current : null;
            _line.Clear();
            if (current != null)
            {
                _line.Append("playing '").Append(current.Tag).Append("' · priority ").Append(_barks.Pool.CurrentPriority);
            }
            else
            {
                _line.Append("playing");
            }

            if (_barkStatusValue != null)
            {
                _barkStatusValue.SetText(_line);
            }

            Log("OnLineStarted · " + (current != null ? current.Tag : "?") + ": " + text);
        }

        /// <summary>DialogueLinePlayer.OnLineFinished listener.</summary>
        public void OnBarkFinished()
        {
            SetText(_barkStatusValue, "silent");
        }

        private void SyncControls()
        {
            if (_dialogue != null)
            {
                if (_typewriterToggle != null)
                {
                    _typewriterToggle.SetIsOnWithoutNotify(_dialogue.useTypewriterEffect);
                }

                if (_autoAdvanceToggle != null)
                {
                    _autoAdvanceToggle.SetIsOnWithoutNotify(_dialogue.autoNextSentence);
                }

                if (_speedSlider != null && _dialogue.Typewriter != null)
                {
                    _speedSlider.SetValueWithoutNotify(_dialogue.Typewriter.CharactersPerSecond);
                }
            }

            if (_autoIdleToggle != null && _barks != null)
            {
                _autoIdleToggle.SetIsOnWithoutNotify(_barks.IdleInterval > 0f);
            }
        }

        private void SetTypingFill(float progress)
        {
            if (_typingFill != null)
            {
                RectTransform fill = _typingFill.rectTransform;
                fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[DialogueDemoController] The scene UI is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
