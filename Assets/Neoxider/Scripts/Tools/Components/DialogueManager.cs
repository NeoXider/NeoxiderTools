using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Tools
{
    [Serializable]
    public class Dialogue
    {
        public UnityEvent<int> OnChangeDialog;
        public Monolog[] monologues;
    }

    [Serializable]
    public class Monolog
    {
        public UnityEvent<int> OnChangeMonolog;
        public string characterName;
        public Sentence[] sentences;
    }

    [Serializable]
    public class Sentence
    {
        public UnityEvent OnChangeSentence;
        public Sprite sprite;
        [TextArea(3, 7)] public string sentence;
    }

    public class DialogueManager : MonoBehaviour
    {
        [Header("UI Элементы")]
        public Image characterImage;
        public TMP_Text characterNameText;
        public TMP_Text dialogueText;
        public bool setNativeSize = true;

        [Header("Настройки эффектов")]
        public bool useTypewriterEffect = true;
        public float charactersPerSecond = 50f;

        [Header("Поведение")]
        public bool autoNextSentence = false;
        public bool autoNextMonolog = false;
        public bool autoNextDialogue = false;
        public bool allowRestart = false;

        [Header("Задержки автопереходов (сек)")]
        [Min(0f)] public float autoNextSentenceDelay = 3f;
        [Min(0f)] public float autoNextMonologDelay = 3f;
        [Min(0f)] public float autoNextDialogueDelay = 3f;

        [Header("Данные диалогов")]
        public Dialogue[] dialogues;

        [Header("События")]
        public UnityEvent OnSentenceEnd;
        public UnityEvent OnMonologEnd;
        public UnityEvent OnDialogueEnd;
        public UnityEvent<string> OnCharacterChange;

        private string _lastCharacterName = string.Empty;
        private Coroutine _typewriterCoroutine;
        private Coroutine _autoDelayCoroutine;
        private string _currentSentenceCached = string.Empty;

        public int currentDialogueId { get; private set; }
        public int currentMonologId { get; private set; }
        public int currentSentenceId { get; private set; }

        public void StartDialogue(int index = 0, int monolog = 0, int sentence = 0)
        {
            currentDialogueId = index;
            currentMonologId = monolog;
            currentSentenceId = sentence;
            _lastCharacterName = string.Empty;
            UpdateDialogueText();
        }

        public void StartDialogue(int index)
        {
            StartDialogue(index, 0);
        }

        private void UpdateDialogueText()
        {
            if (currentDialogueId >= dialogues.Length) return;

            var currentDialogue = dialogues[currentDialogueId];
            currentDialogue.OnChangeDialog?.Invoke(currentDialogueId);

            if (currentMonologId >= currentDialogue.monologues.Length)
            {
                OnDialogueEnd?.Invoke();
                if (autoNextDialogue)
                {
                    StartCoroutine(DelayedNextDialogue());
                }
                return;
            }

            var currentMonolog = currentDialogue.monologues[currentMonologId];
            currentMonolog.OnChangeMonolog?.Invoke(currentMonologId);

            if (currentSentenceId >= currentMonolog.sentences.Length)
            {
                EndMonolog();
                return;
            }

            var sentence = currentMonolog.sentences[currentSentenceId];
            sentence.OnChangeSentence?.Invoke();

            UpdateCharacter(currentMonolog);
            UpdateContent(currentMonolog);
            OnSentenceEnd?.Invoke();

            if (!useTypewriterEffect && autoNextSentence)
            {
                StartCoroutine(DelayedNextSentenceOrEndMonolog());
            }
        }

        private void UpdateCharacter(Monolog currentMonolog)
        {
            var characterName = currentMonolog.characterName;
            if (characterName != _lastCharacterName)
            {
                if(characterNameText != null) characterNameText.text = characterName;
                OnCharacterChange?.Invoke(characterName);
                _lastCharacterName = characterName;
            }
        }

        private void UpdateContent(Monolog currentMonolog)
        {
            var sentence = currentMonolog.sentences[currentSentenceId];
            _currentSentenceCached = sentence.sentence;

            if (useTypewriterEffect)
            {
                if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = StartCoroutine(Typewriter(sentence.sentence));
            }
            else
            {
                if(dialogueText != null) dialogueText.text = sentence.sentence;
            }

            if (characterImage != null && sentence.sprite != null)
            {
                characterImage.sprite = sentence.sprite;
                if (setNativeSize) characterImage.SetNativeSize();
            }
        }

        private IEnumerator Typewriter(string text)
        {
            if(dialogueText == null) yield break;
            var sb = new StringBuilder(text.Length);
            dialogueText.text = "";
            float timePerCharacter = 1f / charactersPerSecond;
            foreach (char c in text)
            {
                sb.Append(c);
                dialogueText.text = sb.ToString();
                yield return new WaitForSeconds(timePerCharacter);
            }
            _typewriterCoroutine = null;

            if (autoNextSentence)
            {
                ScheduleAutoDelay(DelayedNextSentenceOrEndMonolog());
            }
        }

        private void TryNextSentenceOrEndMonolog()
        {
            var currentDialogue = dialogues.Length > currentDialogueId ? dialogues[currentDialogueId] : null;
            if (currentDialogue == null) return;

            var currentMonolog = currentDialogue.monologues.Length > currentMonologId ? currentDialogue.monologues[currentMonologId] : null;
            if (currentMonolog == null) return;

            if (currentSentenceId + 1 < currentMonolog.sentences.Length)
            {
                NextSentence();
            }
            else
            {
                EndMonolog();
            }
        }

        private IEnumerator DelayedNextSentenceOrEndMonolog()
        {
            if (autoNextSentenceDelay > 0f)
            {
                yield return new WaitForSeconds(autoNextSentenceDelay);
            }
            TryNextSentenceOrEndMonolog();
        }

        private void EndMonolog()
        {
            if(dialogueText != null) dialogueText.text = "";
            OnMonologEnd?.Invoke();

            if (autoNextMonolog)
            {
                ScheduleAutoDelay(DelayedNextMonolog());
            }
        }

        private IEnumerator DelayedNextMonolog()
        {
            if (autoNextMonologDelay > 0f)
            {
                yield return new WaitForSeconds(autoNextMonologDelay);
            }
            NextMonolog();
        }

        public void NextSentence()
        {
            currentSentenceId++;
            UpdateDialogueText();
        }

        public void NextMonolog()
        {
            currentMonologId++;
            currentSentenceId = 0;
            UpdateDialogueText();
        }

        public void NextDialogue()
        {
            currentDialogueId++;
            currentMonologId = 0;
            currentSentenceId = 0;
            UpdateDialogueText();
        }

        private IEnumerator DelayedNextDialogue()
        {
            if (autoNextDialogueDelay > 0f)
            {
                yield return new WaitForSeconds(autoNextDialogueDelay);
            }
            NextDialogue();
        }

        private void ScheduleAutoDelay(IEnumerator routine)
        {
            if (_autoDelayCoroutine != null)
            {
                StopCoroutine(_autoDelayCoroutine);
                _autoDelayCoroutine = null;
            }
            _autoDelayCoroutine = StartCoroutine(routine);
        }

        private void CancelAutoDelay()
        {
            if (_autoDelayCoroutine != null)
            {
                StopCoroutine(_autoDelayCoroutine);
                _autoDelayCoroutine = null;
            }
        }

        [Button]
        public void SkipOrNext()
        {
            // Если печать активна — остановить и показать всю фразу
            if (useTypewriterEffect && _typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
                if (dialogueText != null)
                {
                    dialogueText.text = _currentSentenceCached;
                }
                CancelAutoDelay();
                return;
            }

            // Иначе перейти дальше сразу
            CancelAutoDelay();
            TryNextSentenceOrEndMonolog();
        }

        public void RestartDialogue()
        {
            if (!allowRestart) return;
            currentMonologId = 0;
            currentSentenceId = 0;
            _lastCharacterName = string.Empty;
            UpdateDialogueText();
        }

        private void OnValidate()
        {
            if (charactersPerSecond <= 0f)
            {
                charactersPerSecond = 0.01f;
            }
            if (autoNextSentenceDelay < 0f) autoNextSentenceDelay = 0f;
            if (autoNextMonologDelay < 0f) autoNextMonologDelay = 0f;
            if (autoNextDialogueDelay < 0f) autoNextDialogueDelay = 0f;
        }
    }
}