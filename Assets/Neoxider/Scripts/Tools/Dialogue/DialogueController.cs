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

            _typewriter.OnCharacterTyped += c => OnCharacterTyped?.Invoke(c);
            _typewriter.OnProgressChanged += p => OnTypewriterProgress?.Invoke(p);
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
        [Button]
        public void StartDialogue(int dialogueIndex = 0, int monologIndex = 0, int sentenceIndex = 0)
        {
            CancelAll();

            if (dialogues == null || dialogues.Length == 0)
            {
                Debug.LogWarning("[DialogueController] No dialogues configured.", this);
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
        [Button]
        public void NextSentence()
        {
            CancelAll();
            CurrentSentenceId++;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Advances to the next monolog.
        /// </summary>
        [Button]
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
        [Button]
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
        [Button]
        public void SkipOrNext()
        {
            // Start if not started yet
            if (!DialogueStarted)
            {
                if (dialogues != null && dialogues.Length > 0)
                {
                    StartDialogue();
                }

                return;
            }

            // If typing, show full text
            if (IsTyping)
            {
                CompleteTypewriter();
                return;
            }

            // Advance
            Advance();
        }

        /// <summary>
        ///     Advance to next sentence, monolog, or dialogue.
        /// </summary>
        public void Advance()
        {
            CancelAutoDelay();

            if (!DialogueStarted || dialogues == null || CurrentDialogueId >= dialogues.Length)
            {
                return;
            }

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue?.monologues == null || CurrentMonologId >= currentDialogue.monologues.Length)
            {
                // End of dialogue — go to next dialogue
                GoToNextDialogue();
                return;
            }

            Monolog currentMonolog = currentDialogue.monologues[CurrentMonologId];
            if (currentMonolog?.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length - 1)
            {
                // End of monolog — go to next monolog
                GoToNextMonolog();
                return;
            }

            // Next sentence
            NextSentence();
        }

        private void GoToNextMonolog()
        {
            OnMonologEnd?.Invoke();

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue?.monologues != null && CurrentMonologId < currentDialogue.monologues.Length - 1)
            {
                // More monologs in this dialogue
                if (autoNextMonolog)
                {
                    ScheduleAutoDelay(autoNextMonologDelay, NextMonolog);
                }
                else
                {
                    NextMonolog();
                }
            }
            else
            {
                // End of dialogue
                GoToNextDialogue();
            }
        }

        private void GoToNextDialogue()
        {
            OnDialogueEnd?.Invoke();

            if (CurrentDialogueId < dialogues.Length - 1)
            {
                // More dialogues
                if (autoNextDialogue)
                {
                    ScheduleAutoDelay(autoNextDialogueDelay, NextDialogue);
                }
                else
                {
                    NextDialogue();
                }
            }
            else
            {
                // All dialogues finished
                OnAllDialoguesEnd?.Invoke();
                DialogueStarted = false;
            }
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

            CancelTypewriter();
            _dialogueUI?.SetDialogueText(_currentSentenceCached);

            OnTypewriterProgress?.Invoke(1f);

            if (autoNextSentence)
            {
                ScheduleAutoDelay(autoNextSentenceDelay, Advance);
            }
        }

        /// <summary>
        ///     Restarts the current dialogue.
        /// </summary>
        [Button]
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
        [Button]
        public void RestartAll()
        {
            StartDialogue();
        }

#if UNITY_EDITOR
        [Button("Open Dialogue Editor", 180)]
        private void OpenDialogueEditor()
        {
            var windowType = Type.GetType("Neo.Tools.Editor.DialogueEditorWindow, Neo.Editor");
            if (windowType == null)
            {
                Debug.LogError(
                    "[DialogueController] DialogueEditorWindow not found. Ensure Neo.Editor assembly is compiled.",
                    this);
                return;
            }

            MethodInfo showForMethod = windowType.GetMethod(
                "ShowFor",
                BindingFlags.Public | BindingFlags.Static);

            if (showForMethod == null)
            {
                Debug.LogError("[DialogueController] DialogueEditorWindow.ShowFor method not found.", this);
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
            if (dialogues == null || dialogues.Length == 0)
            {
                return;
            }

            if (CurrentDialogueId >= dialogues.Length)
            {
                OnAllDialoguesEnd?.Invoke();
                DialogueStarted = false;
                return;
            }

            Dialogue currentDialogue = GetCurrentDialogue();
            if (currentDialogue == null)
            {
                Debug.LogWarning($"[DialogueController] Dialogue [{CurrentDialogueId}] is null.", this);
                return;
            }

            currentDialogue.OnChangeDialog?.Invoke(CurrentDialogueId);

            if (currentDialogue.monologues == null || CurrentMonologId >= currentDialogue.monologues.Length)
            {
                GoToNextDialogue();
                return;
            }

            Monolog currentMonolog = GetCurrentMonolog();
            if (currentMonolog == null)
            {
                Debug.LogWarning($"[DialogueController] Monolog [{CurrentDialogueId}][{CurrentMonologId}] is null.",
                    this);
                return;
            }

            currentMonolog.OnChangeMonolog?.Invoke(CurrentMonologId);

            if (currentMonolog.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length)
            {
                GoToNextMonolog();
                return;
            }

            Sentence sentence = GetCurrentSentence();
            if (sentence == null)
            {
                Debug.LogWarning(
                    $"[DialogueController] Sentence [{CurrentDialogueId}][{CurrentMonologId}][{CurrentSentenceId}] is null.",
                    this);
                return;
            }

            sentence.OnChangeSentence?.Invoke();

            _dialogueUI?.SetCharacterName(currentMonolog.characterName);
            _dialogueUI?.SetCharacterSprite(sentence.sprite);
            OnCharacterChange?.Invoke(currentMonolog.characterName);

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

            OnSentenceEnd?.Invoke();
        }

        private async UniTaskVoid StartTypewriterAsync(string text)
        {
            CancelTypewriter();
            _typewriterCts = new CancellationTokenSource();

            try
            {
                await _typewriter.PlayAsync(text, t => _dialogueUI?.SetDialogueText(t), _typewriterCts.Token);

                // Typewriter finished
                if (autoNextSentence)
                {
                    ScheduleAutoDelay(autoNextSentenceDelay, Advance);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected
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

                action?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected
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
            CancelTypewriter();
            CancelAutoDelay();
        }
    }
}
