using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo.Tools
{
    /// <summary>
    ///     Основной контроллер диалоговой системы (на UniTask).
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/Dialogue/" + nameof(DialogueController))]
    public class DialogueController : MonoBehaviour
    {
        [Header("Components")] [Tooltip("UI компонент. Если не указан, ищется на этом объекте")] [SerializeField]
        private DialogueUI _dialogueUI;

        [Header("Typewriter Settings")] public bool useTypewriterEffect = true;

        [SerializeField] private TypewriterEffect _typewriter = new();

        [Header("Auto Start")] [Tooltip("Автоматически запустить первый диалог при Start")]
        public bool autoStart;

        [Header("Auto Advance")] public bool autoNextSentence;

        public bool autoNextMonolog;
        public bool autoNextDialogue;
        public bool allowRestart;

        [Header("Auto Advance Delays")] [Min(0f)]
        public float autoNextSentenceDelay = 3f;

        [Min(0f)] public float autoNextMonologDelay = 3f;
        [Min(0f)] public float autoNextDialogueDelay = 3f;

        [Header("Dialogue Data")] public Dialogue[] dialogues;

        [Header("Events")] public UnityEvent OnSentenceEnd;

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
        ///     Запускает диалог с указанными индексами.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
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
        ///     Переходит к следующему предложению.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void NextSentence()
        {
            CancelAll();
            CurrentSentenceId++;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Переходит к следующему монологу.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void NextMonolog()
        {
            CancelAll();
            CurrentMonologId++;
            CurrentSentenceId = 0;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Переходит к следующему диалогу.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void NextDialogue()
        {
            CancelAll();
            CurrentDialogueId++;
            CurrentMonologId = 0;
            CurrentSentenceId = 0;
            ShowCurrentSentence();
        }

        /// <summary>
        ///     Пропускает печать или переходит к следующему.
        ///     Если диалог не начат — начинает его.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void SkipOrNext()
        {
            // Если диалог не начат — начинаем
            if (!DialogueStarted)
            {
                if (dialogues != null && dialogues.Length > 0)
                {
                    StartDialogue();
                }

                return;
            }

            // Если печатаем — показываем полный текст
            if (IsTyping)
            {
                CompleteTypewriter();
                return;
            }

            // Переходим к следующему
            Advance();
        }

        /// <summary>
        ///     Переход к следующему предложению/монологу/диалогу.
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
                // Конец диалога — переходим к следующему диалогу
                GoToNextDialogue();
                return;
            }

            Monolog currentMonolog = currentDialogue.monologues[CurrentMonologId];
            if (currentMonolog?.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length - 1)
            {
                // Конец монолога — переходим к следующему монологу
                GoToNextMonolog();
                return;
            }

            // Следующее предложение
            NextSentence();
        }

        private void GoToNextMonolog()
        {
            OnMonologEnd?.Invoke();

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue?.monologues != null && CurrentMonologId < currentDialogue.monologues.Length - 1)
            {
                // Есть ещё монологи
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
                // Конец диалога
                GoToNextDialogue();
            }
        }

        private void GoToNextDialogue()
        {
            OnDialogueEnd?.Invoke();

            if (CurrentDialogueId < dialogues.Length - 1)
            {
                // Есть ещё диалоги
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
                // Все диалоги закончились
                OnAllDialoguesEnd?.Invoke();
                DialogueStarted = false;
            }
        }

        /// <summary>
        ///     Завершает печать и показывает полный текст.
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
        ///     Перезапускает текущий диалог.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void RestartDialogue()
        {
            if (!allowRestart)
            {
                return;
            }

            StartDialogue(CurrentDialogueId);
        }

        /// <summary>
        ///     Перезапускает все диалоги сначала.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#else
        [ButtonAttribute]
#endif
        public void RestartAll()
        {
            StartDialogue();
        }

        private void ShowCurrentSentence()
        {
            if (dialogues == null || dialogues.Length == 0)
            {
                return;
            }

            // Проверяем границы диалога
            if (CurrentDialogueId >= dialogues.Length)
            {
                OnAllDialoguesEnd?.Invoke();
                DialogueStarted = false;
                return;
            }

            Dialogue currentDialogue = dialogues[CurrentDialogueId];
            if (currentDialogue == null)
            {
                Debug.LogWarning($"[DialogueController] Dialogue [{CurrentDialogueId}] is null.", this);
                return;
            }

            currentDialogue.OnChangeDialog?.Invoke(CurrentDialogueId);

            // Проверяем границы монолога
            if (currentDialogue.monologues == null || CurrentMonologId >= currentDialogue.monologues.Length)
            {
                GoToNextDialogue();
                return;
            }

            Monolog currentMonolog = currentDialogue.monologues[CurrentMonologId];
            if (currentMonolog == null)
            {
                Debug.LogWarning($"[DialogueController] Monolog [{CurrentDialogueId}][{CurrentMonologId}] is null.",
                    this);
                return;
            }

            currentMonolog.OnChangeMonolog?.Invoke(CurrentMonologId);

            // Проверяем границы предложения
            if (currentMonolog.sentences == null || CurrentSentenceId >= currentMonolog.sentences.Length)
            {
                GoToNextMonolog();
                return;
            }

            Sentence sentence = currentMonolog.sentences[CurrentSentenceId];
            if (sentence == null)
            {
                Debug.LogWarning(
                    $"[DialogueController] Sentence [{CurrentDialogueId}][{CurrentMonologId}][{CurrentSentenceId}] is null.",
                    this);
                return;
            }

            sentence.OnChangeSentence?.Invoke();

            // Обновляем UI
            _dialogueUI?.SetCharacterName(currentMonolog.characterName);
            _dialogueUI?.SetCharacterSprite(sentence.sprite);
            OnCharacterChange?.Invoke(currentMonolog.characterName);

            // Показываем текст
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

                // Печать завершена успешно
                if (autoNextSentence)
                {
                    ScheduleAutoDelay(autoNextSentenceDelay, Advance);
                }
            }
            catch (OperationCanceledException)
            {
                // Отмена — нормальное поведение
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
                // Отмена — нормальное поведение
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