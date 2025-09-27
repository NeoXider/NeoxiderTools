using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo
{
    namespace Tools
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
            public DialogueContent[] contents;
        }

        [Serializable]
        public class DialogueContent
        {
            public UnityEvent OnChangeContent;
            public Sprite sprite;
            [TextArea(3, 7)] public string sentence;
        }

        public class DialogueManager : MonoBehaviour
        {
            public Dialogue[] dialogues;
            public Image _imageCharacter;
            public bool _setNativeSize = true;

            public UnityEvent OnSentenceEnd;
            public UnityEvent OnMonologEnd;
            public UnityEvent OnDialogueEnd;
            public UnityEvent<string> OnCharacterChange;

            public bool autoNextMonolog = true;

            [Space] public TMP_Text _characterText;

            public TMP_Text _dialogueText;
            private string _lastCharacterName = string.Empty;

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
                if (currentDialogueId < dialogues.Length)
                {
                    var currentDialogue = dialogues[currentDialogueId];
                    currentDialogue.OnChangeDialog.Invoke(currentDialogueId);

                    if (currentMonologId < currentDialogue.monologues.Length)
                    {
                        var currentMonolog = currentDialogue.monologues[currentMonologId];
                        currentMonolog.OnChangeMonolog.Invoke(currentMonologId);

                        if (currentSentenceId < currentMonolog.contents.Length)
                        {
                            var content = currentMonolog.contents[currentSentenceId];
                            content.OnChangeContent.Invoke();

                            UpdateCharacter(currentMonolog);
                            UpdateContent(currentMonolog);
                            OnSentenceEnd.Invoke();
                        }
                        else
                        {
                            EndMonolog();
                        }
                    }
                    else
                    {
                        OnDialogueEnd.Invoke();
                    }
                }
            }

            private void UpdateCharacter(Monolog currentMonolog)
            {
                var characterName = currentMonolog.characterName;
                if (characterName != _lastCharacterName)
                {
                    _characterText.text = characterName;
                    OnCharacterChange.Invoke(characterName);
                    _lastCharacterName = characterName;
                }
            }

            private void UpdateContent(Monolog currentMonolog)
            {
                var content = currentMonolog.contents[currentSentenceId];
                _dialogueText.text = content.sentence;

                if (_imageCharacter != null && content.sprite != null)
                {
                    _imageCharacter.sprite = content.sprite;
                    if (_setNativeSize) _imageCharacter.SetNativeSize();
                }
            }

            private void EndMonolog()
            {
                _dialogueText.text = "";
                OnMonologEnd.Invoke();

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
}