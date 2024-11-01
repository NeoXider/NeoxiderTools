using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Tools
    {
        [System.Serializable]
        public class Dialogue
        {
            public string characterName;
            public string[] sentences;
        }

        public class DialogueManager : MonoBehaviour
        {
            public Dialogue[] dialogues;
            public TMP_Text _dialogueText;

            private int currentDialogueIndex = 0;
            private int currentSentenceIndex = 0;

            public UnityEvent OnSentenceEnd;
            public UnityEvent OnDialogueEnd;

            public bool autoNextDialogue = true;

            public void StartDialogue(int index)
            {
                currentDialogueIndex = index;
                currentSentenceIndex = 0;
                UpdateDialogueText();
            }

            private void UpdateDialogueText()
            {
                if (currentDialogueIndex < dialogues.Length)
                {
                    if (currentSentenceIndex < dialogues[currentDialogueIndex].sentences.Length)
                    {
                        _dialogueText.text = dialogues[currentDialogueIndex].sentences[currentSentenceIndex];
                        OnSentenceEnd.Invoke();
                    }
                    else
                    {
                        _dialogueText.text = "";
                        OnDialogueEnd.Invoke();

                        if (autoNextDialogue)
                        {
                            NextDialogue();
                        }
                    }
                }
            }

            public void NextSentence()
            {
                currentSentenceIndex++;

                UpdateDialogueText();
            }

            public void NextDialogue()
            {
                currentDialogueIndex++;
                currentSentenceIndex = 0;

                UpdateDialogueText();
            }

            public void RestartDialogue()
            {
                currentSentenceIndex = 0;

                UpdateDialogueText();
            }
        }
    }
}