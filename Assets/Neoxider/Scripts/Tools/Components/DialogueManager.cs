using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo
{
    namespace Tools
    {
        [System.Serializable]
        public class Dialogue
        {
            public UnityEvent<int> OnChangeDialog;
            public Monolog[] monologues;
        }

        [System.Serializable]
        public class Monolog
        {
            public UnityEvent<int> OnChangeMonolog;
            public string characterName;
            public DialogueContent[] contents;
        }

        [System.Serializable]
        public class DialogueContent
        {
            public UnityEvent OnChangeContent;
            public Sprite sprite;
            [TextArea(3, 7)]
            public string sentence;
        }

        public class DialogueManager : MonoBehaviour
        {
            public Dialogue[] dialogues;

            [Space]
            public TMP_Text _characterText;
            public TMP_Text _dialogueText;
            public Image _imageCharacter;
            public bool _setNativeSize = true;

            public int currentDialogueId => _currentDialogueIndex;
            public int currentMonologId => _currentMonologIndex;
            public int currentSentenceId => _currentSentenceIndex;

            private int _currentDialogueIndex = 0;
            private int _currentMonologIndex = 0;
            private int _currentSentenceIndex = 0;
            private string _lastCharacterName = string.Empty;

            public UnityEvent OnSentenceEnd;
            public UnityEvent OnMonologEnd;
            public UnityEvent OnDialogueEnd;
            public UnityEvent<string> OnCharacterChange;

            public bool autoNextMonolog = true;

            public void StartDialogue(int index = 0, int monolog = 0, int sentence = 0)
            {
                _currentDialogueIndex = index;
                _currentMonologIndex = monolog;
                _currentSentenceIndex = sentence;
                _lastCharacterName = string.Empty;
                UpdateDialogueText();
            }

            public void StartDialogue(int index)
            {
                StartDialogue(index, 0, 0);
            }

            private void UpdateDialogueText()
            {
                if (_currentDialogueIndex < dialogues.Length)
                {
                    var currentDialogue = dialogues[_currentDialogueIndex];
                    currentDialogue.OnChangeDialog.Invoke(_currentDialogueIndex);

                    if (_currentMonologIndex < currentDialogue.monologues.Length)
                    {
                        var currentMonolog = currentDialogue.monologues[_currentMonologIndex];
                        currentMonolog.OnChangeMonolog.Invoke(_currentMonologIndex);

                        if (_currentSentenceIndex < currentMonolog.contents.Length)
                        {
                            var content = currentMonolog.contents[_currentSentenceIndex];
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
                string characterName = currentMonolog.characterName;
                if (characterName != _lastCharacterName)
                {
                    _characterText.text = characterName;
                    OnCharacterChange.Invoke(characterName);
                    _lastCharacterName = characterName;
                }
            }

            private void UpdateContent(Monolog currentMonolog)
            {
                var content = currentMonolog.contents[_currentSentenceIndex];
                _dialogueText.text = content.sentence;

                if (_imageCharacter != null && content.sprite != null)
                {
                    _imageCharacter.sprite = content.sprite;
                    if (_setNativeSize)
                    {
                        _imageCharacter.SetNativeSize();
                    }
                }
            }

            private void EndMonolog()
            {
                _dialogueText.text = "";
                OnMonologEnd.Invoke();

                if (autoNextMonolog)
                {
                    NextMonolog();
                }
            }

            public void NextSentence()
            {
                _currentSentenceIndex++;
                UpdateDialogueText();
            }

            public void NextMonolog()
            {
                _currentMonologIndex++;
                _currentSentenceIndex = 0;
                UpdateDialogueText();
            }

            public void NextDialogue()
            {
                _currentDialogueIndex++;
                _currentMonologIndex = 0;
                _currentSentenceIndex = 0;
                UpdateDialogueText();
            }

            public void RestartDialogue()
            {
                _currentMonologIndex = 0;
                _currentSentenceIndex = 0;
                _lastCharacterName = string.Empty;
                UpdateDialogueText();
            }
        }
    }
}