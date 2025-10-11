using System;
using System.Collections;
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
        public bool autoNextMonolog = true;

        [Header("Данные диалогов")]
        public Dialogue[] dialogues;

        [Header("События")]
        public UnityEvent OnSentenceEnd;
        public UnityEvent OnMonologEnd;
        public UnityEvent OnDialogueEnd;
        public UnityEvent<string> OnCharacterChange;

        private string _lastCharacterName = string.Empty;
        private Coroutine _typewriterCoroutine;

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
            dialogueText.text = "";
            float timePerCharacter = 1f / charactersPerSecond;
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(timePerCharacter);
            }
            _typewriterCoroutine = null;
        }

        private void EndMonolog()
        {
            if(dialogueText != null) dialogueText.text = "";
            OnMonologEnd?.Invoke();

            if (autoNextMonolog) NextMonolog();
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

        public void RestartDialogue()
        {
            currentMonologId = 0;
            currentSentenceId = 0;
            _lastCharacterName = string.Empty;
            UpdateDialogueText();
        }
    }
}