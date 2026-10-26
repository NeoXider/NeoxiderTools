using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Main dialogue system controller (UniTask).
    /// </summary>
    [NeoDoc("Tools/Dialogue/DialogueController.md")]
    [CreateFromMenu("Neoxider/Tools/Dialogue/DialogueController")]
    [AddComponentMenu("Neoxider/" + "Tools/Dialogue/" + nameof(DialogueController))]
    public class DialogueController : MonoBehaviour
    {
        [Header("Components")] [Tooltip("UI component. If not set, searched on this object")] [SerializeField]
        private DialogueUI _dialogueUI;

        [Header("Typewriter Settings")] public bool useTypewriterEffect = true;

        [SerializeField] private TypewriterEffect _typewriter = new();

        [Header("Auto Start")] [Tooltip("Automatically start first dialogue on Start")]
        public bool autoStart;

        [Header("Auto Advance")] [Tooltip("Advance to next sentence after typewriter finishes")]
        public bool autoNextSentence;

        [Tooltip("Advance to next monolog when current ends")]
        public bool autoNextMonolog;

        [Tooltip("Advance to next dialogue when current ends")]
        public bool autoNextDialogue;

        [Tooltip("Allow restarting current dialogue via RestartDialogue()")]
        public bool allowRestart;

        [Header("Auto Advance Delays")] [Min(0f)]
        public float autoNextSentenceDelay = 3f;

        [Min(0f)] public float autoNextMonologDelay = 3f;
        [Min(0f)] public float autoNextDialogueDelay = 3f;

        [Header("Dialogue Data")] public Dialogue[] dialogues;

        public UnityEvent OnSentenceEnd;

        public UnityEvent OnMonologEnd;
        public UnityEvent OnDialogueEnd;
        public UnityEvent OnAllDialoguesEnd;
        public UnityEvent<string> OnCharacterChange;
        public UnityEvent<char> OnCharacterTyped;
        public UnityEvent<float> OnTypewriterProgress;
        private CancellationTokenSource _autoDelayCts;
        private string _currentSentenceCached = string.Empty;
        private int _playbackVersion;

        private CancellationTokenSource _typewriterCts;

        public TypewriterEffect Typewriter => _typewriter;
        public int CurrentDialogueId { get; private set; }
        public int CurrentMonologId { get; private set; }
        public int CurrentSentenceId { get; private set; }
        public bool IsTyping => _typewriter?.IsTyping ?? false;
        public bool DialogueStarted { get; private set; }

        private void Awake()
        {
            if (_dialogueUI == null)
            {
                _dialogueUI = GetComponent<DialogueUI>();
            }

            if (_typewriter == null)
            {
                _typewriter = new TypewriterEffect();
            }

            _typewriter.RebuildPauseMap();

            _typewriter.OnCharacterTyped += ForwardCharacterTyped;
            _typewriter.OnProgressChanged += ForwardTypewriterProgress;
        }

        private void Start()
        {
            if (autoStart && dialogues != null && dialogues.Length > 0)
            {
                StartDialogue();
            }
        }

        private void OnDisable()
        {
            CancelAll();
        }

        private void OnDestroy()
        {
            CancelAll();
            if (_typewriter != null)
            {
                _typewriter.OnCharacterTyped -= ForwardCharacterTyped;
                _typewriter.OnProgressChanged -= ForwardTypewriterProgress;
            }
        }

        private void ForwardCharacterTyped(char character)
        {
            OnCharacterTyped?.Invoke(character);
        }

        private void ForwardTypewriterProgress(float progress)
        {
            OnTypewriterProgress?.Invoke(progress);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_typewriter != null && _typewriter.CharactersPerSecond <= 0f)
            {
                _typewriter.CharactersPerSecond = 0.1f;
            }
        }
#endif

        /// <summary>
        ///     Starts dialogue at the given indices.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void StartDialogue(int dialogueIndex = 0, int monologIndex = 0, int sentenceIndex = 0)
        {
            CancelAll();
            DialogueStarted = false;

            if (dialogues == null || dialogues.Length == 0)
            {
                NeoDiagnostics.LogWarning("[DialogueController] No dialogues configured.", this);
                return;
            }

            CurrentDialogueId = Mathf.Clamp(dialogueIndex, 0, dialogues.Length - 1);
            CurrentMonologId = Mathf.Max(0, monologIndex);
            CurrentSentenceId = Mathf.Max(0, sentenceIndex);

            DialogueStarted = true;
            _dialogueUI?.Reset();
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Advances to the next sentence.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void NextSentence()
        {
            CancelAll();
            CurrentSentenceId++;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Advances to the next monolog.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void NextMonolog()
        {
            CancelAll();
            CurrentMonologId++;
            CurrentSentenceId = 0;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Advances to the next dialogue.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void NextDialogue()
        {
            CancelAll();
            CurrentDialogueId++;
            CurrentMonologId = 0;
            CurrentSentenceId = 0;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Skips typewriter or advances. If dialogue has not started, starts it.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void SkipOrNext()
        {
            if (!DialogueStarted)
            {
                if (dialogues != null && dialogues.Length > 0)
                {
                    StartDialogue();
                }

                return;
            }

            if (IsTyping)
            {
                CompleteTypewriter();
                return;
            }

            Advance();
        }

        /// <summary>
        ///     Advance to next sentence, monolog, or dialogue.
        /// </summary>
        public void Advance()
        {
            CancelAll();

            if (!DialogueStarted || dialogues == null || CurrentDialogueId >= dialogues.Length)
            {
                return;
            }

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue?.monologues == null || CurrentMonologId >= currentDialogue.monologues.Length)
            {
                if (TryMoveToNextDialogue())
                {
                    ShowCurrentSentence();
                }

                return;
            }

            Monolog currentMonolog = currentDialogue.monologues[CurrentMonologId];
            if (currentMonolog?.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length - 1)
            {
                if (TryMoveToNextMonolog())
                {
                    ShowCurrentSentence();
                }

                return;
            }

            NextSentence();
        }

        // True means the cursor moved immediately and the caller should present it.
        private bool TryMoveToNextMonolog()
        {
            int playbackVersion = _playbackVersion;
            OnMonologEnd?.Invoke();
            if (playbackVersion != _playbackVersion)
            {
                return false;
            }

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue?.monologues != null && CurrentMonologId < currentDialogue.monologues.Length - 1)
            {
                if (autoNextMonolog)
                {
                    ScheduleAutoDelay(autoNextMonologDelay, NextMonolog);
                    return false;
                }

                CancelAll();
                CurrentMonologId++;
                CurrentSentenceId = 0;
                return true;
            }

            return TryMoveToNextDialogue();
        }

        private bool TryMoveToNextDialogue()
        {
            int playbackVersion = _playbackVersion;
            OnDialogueEnd?.Invoke();
            if (playbackVersion != _playbackVersion)
            {
                return false;
            }

            if (CurrentDialogueId < dialogues.Length - 1)
            {
                if (autoNextDialogue)
                {
                    ScheduleAutoDelay(autoNextDialogueDelay, NextDialogue);
                    return false;
                }

                CancelAll();
                CurrentDialogueId++;
                CurrentMonologId = 0;
                CurrentSentenceId = 0;
                return true;
            }

            DialogueStarted = false;
            OnAllDialoguesEnd?.Invoke();
            return false;
        }

        /// <summary>
        ///     Completes typewriter and shows full text.
        /// </summary>
        public void CompleteTypewriter()
        {
            if (!IsTyping)
            {
                return;
            }

            int playbackVersion = _playbackVersion;
            CancelTypewriter();
            string completedText = _typewriter.Complete();
            if (playbackVersion != _playbackVersion)
            {
                return;
            }

            _dialogueUI?.SetDialogueText(completedText);

            if (autoNextSentence)
            {
                ScheduleAutoDelay(autoNextSentenceDelay, Advance);
            }
        }

        /// <summary>
        ///     Restarts the current dialogue.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void RestartDialogue()
        {
            if (!allowRestart)
            {
                return;
            }

            StartDialogue(CurrentDialogueId);
        }

        /// <summary>
        ///     Restarts all dialogues from the beginning.
        /// </summary>
        [Button(PlayModeOnly = true)]
        public void RestartAll()
        {
            StartDialogue();
        }

#if UNITY_EDITOR
        [Button("Open Dialogue Editor", 180)]
        private void OpenDialogueEditor()
        {
            Type windowType = Type.GetType("Neo.Tools.Editor.DialogueEditorWindow, Neo.Editor");
            if (windowType == null)
            {
                NeoDiagnostics.LogError(
                    "[DialogueController] DialogueEditorWindow not found. Ensure Neo.Editor assembly is compiled.",
                    this);
                return;
            }

            MethodInfo showForMethod = windowType.GetMethod(
                "ShowFor",
                BindingFlags.Public | BindingFlags.Static);

            if (showForMethod == null)
            {
                NeoDiagnostics.LogError("[DialogueController] DialogueEditorWindow.ShowFor method not found.", this);
                return;
            }

            showForMethod.Invoke(null, new object[] { this });
        }
#endif

        /// <summary>
        ///     Current dialogue by CurrentDialogueId, or null.
        /// </summary>
        public Dialogue GetCurrentDialogue()
        {
            if (dialogues == null || CurrentDialogueId < 0 || CurrentDialogueId >= dialogues.Length)
            {
                return null;
            }

            return dialogues[CurrentDialogueId];
        }

        /// <summary>
        ///     Current monolog by CurrentMonologId, or null.
        /// </summary>
        public Monolog GetCurrentMonolog()
        {
            Dialogue d = GetCurrentDialogue();
            if (d?.monologues == null || CurrentMonologId < 0 || CurrentMonologId >= d.monologues.Length)
            {
                return null;
            }

            return d.monologues[CurrentMonologId];
        }

        /// <summary>
        ///     Current sentence by CurrentSentenceId, or null.
        /// </summary>
        public Sentence GetCurrentSentence()
        {
            Monolog m = GetCurrentMonolog();
            if (m?.sentences == null || CurrentSentenceId < 0 || CurrentSentenceId >= m.sentences.Length)
            {
                return null;
            }

            return m.sentences[CurrentSentenceId];
        }

        private void ShowCurrentSentence()
        {
            // Empty containers advance in this loop, rather than recursively re-entering presentation.
            while (true)
            {
                int playbackVersion = _playbackVersion;
                if (dialogues == null || dialogues.Length == 0)
                {
                    return;
                }

                if (CurrentDialogueId >= dialogues.Length)
                {
                    DialogueStarted = false;
                    OnAllDialoguesEnd?.Invoke();
                    return;
                }

                Dialogue currentDialogue = GetCurrentDialogue();
                if (currentDialogue == null)
                {
                    NeoDiagnostics.LogWarning($"[DialogueController] Dialogue [{CurrentDialogueId}] is null.", this);
                    return;
                }

                currentDialogue.OnChangeDialog?.Invoke(CurrentDialogueId);
                if (playbackVersion != _playbackVersion)
                {
                    return;
                }

                if (currentDialogue.monologues == null || CurrentMonologId >= currentDialogue.monologues.Length)
                {
                    if (TryMoveToNextDialogue())
                    {
                        continue;
                    }

                    return;
                }

                Monolog currentMonolog = GetCurrentMonolog();
                if (currentMonolog == null)
                {
                    NeoDiagnostics.LogWarning(
                        $"[DialogueController] Monolog [{CurrentDialogueId}][{CurrentMonologId}] is null.",
                        this);
                    return;
                }

                currentMonolog.OnChangeMonolog?.Invoke(CurrentMonologId);
                if (playbackVersion != _playbackVersion)
                {
                    return;
                }

                if (currentMonolog.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length)
                {
                    if (TryMoveToNextMonolog())
                    {
                        continue;
                    }

                    return;
                }

                Sentence sentence = GetCurrentSentence();
                if (sentence == null)
                {
                    NeoDiagnostics.LogWarning(
                        $"[DialogueController] Sentence [{CurrentDialogueId}][{CurrentMonologId}][{CurrentSentenceId}] is null.",
                        this);
                    return;
                }

                sentence.OnChangeSentence?.Invoke();
                if (playbackVersion != _playbackVersion)
                {
                    return;
                }

                _dialogueUI?.SetCharacterName(currentMonolog.characterName);
                if (playbackVersion != _playbackVersion)
                {
                    return;
                }

                _dialogueUI?.SetCharacterSprite(sentence.sprite);
                OnCharacterChange?.Invoke(currentMonolog.characterName);
                if (playbackVersion != _playbackVersion)
                {
                    return;
                }

                _currentSentenceCached = sentence.sentence ?? string.Empty;

                if (useTypewriterEffect && !string.IsNullOrEmpty(_currentSentenceCached))
                {
                    StartTypewriterAsync(_currentSentenceCached).Forget();
                }
                else
                {
                    _dialogueUI?.SetDialogueText(_currentSentenceCached);
                    if (autoNextSentence)
                    {
                        ScheduleAutoDelay(autoNextSentenceDelay, Advance);
                    }
                }

                if (playbackVersion == _playbackVersion)
                {
                    OnSentenceEnd?.Invoke();
                }

                return;
            }
        }

        private async UniTaskVoid StartTypewriterAsync(string text)
        {
            CancelTypewriter();
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            _typewriterCts = cancellationSource;
            CancellationToken token = cancellationSource.Token;

            try
            {
                await _typewriter.PlayAsync(text, t => _dialogueUI?.SetDialogueText(t), token);
                // PlayAsync handles cancellation internally, so a normal return can belong to an old run.
                token.ThrowIfCancellationRequested();

                if (autoNextSentence)
                {
                    ScheduleAutoDelay(autoNextSentenceDelay, Advance);
                }
            }
            catch (OperationCanceledException)
            {
                // WHY: Cancellation is expected
            }
            finally
            {
                if (_typewriterCts == cancellationSource)
                {
                    _typewriterCts = null;
                    cancellationSource.Dispose();
                }
            }
        }

        private void ScheduleAutoDelay(float delay, Action action)
        {
            CancelAutoDelay();
            _autoDelayCts = new CancellationTokenSource();
            AutoDelayAsync(delay, action, _autoDelayCts.Token).Forget();
        }

        private async UniTaskVoid AutoDelayAsync(float delay, Action action, CancellationToken token)
        {
            try
            {
                if (delay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                }
                else
                {
                    // Even zero-delay automatic transitions must yield to avoid recursive playback.
                    await UniTask.NextFrame(cancellationToken: token);
                }

                token.ThrowIfCancellationRequested();
                action?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // WHY: Cancellation is expected
            }
        }

        private void CancelTypewriter()
        {
            _typewriter?.Stop();
            if (_typewriterCts != null)
            {
                _typewriterCts.Cancel();
                _typewriterCts.Dispose();
                _typewriterCts = null;
            }
        }

        private void CancelAutoDelay()
        {
            if (_autoDelayCts != null)
            {
                _autoDelayCts.Cancel();
                _autoDelayCts.Dispose();
                _autoDelayCts = null;
            }
        }

        private void CancelAll()
        {
            _playbackVersion++;
            CancelTypewriter();
            CancelAutoDelay();
        }
    }
}
