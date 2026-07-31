using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class DialoguePlayModeTests
    {
        private DialogueController _controller;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject(nameof(DialoguePlayModeTests));
            _controller = _go.AddComponent<DialogueController>();
            _controller.useTypewriterEffect = false;
            _controller.autoStart = false;
            _controller.dialogues = new[]
            {
                BuildDialogue("Ann", "a1", "a2"),
                BuildDialogue("Bob", "b1")
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private static Dialogue BuildDialogue(string speaker, params string[] sentences)
        {
            var monolog = new Monolog
            {
                characterName = speaker,
                sentences = new Sentence[sentences.Length]
            };

            for (int i = 0; i < sentences.Length; i++)
            {
                monolog.sentences[i] = new Sentence { sentence = sentences[i] };
            }

            return new Dialogue { monologues = new[] { monolog } };
        }

        [UnityTest]
        public IEnumerator StartDialogue_SetsIndicesAndMarksStarted()
        {
            _controller.StartDialogue();
            yield return null;

            Assert.IsTrue(_controller.DialogueStarted);
            Assert.AreEqual(0, _controller.CurrentDialogueId);
            Assert.AreEqual(0, _controller.CurrentMonologId);
            Assert.AreEqual(0, _controller.CurrentSentenceId);
        }

        [UnityTest]
        public IEnumerator NextSentence_AdvancesWithinMonolog()
        {
            _controller.StartDialogue();
            yield return null;

            _controller.NextSentence();
            yield return null;

            Assert.AreEqual(1, _controller.CurrentSentenceId);
            Assert.AreEqual(0, _controller.CurrentDialogueId);
        }

        [UnityTest]
        public IEnumerator NextDialogue_MovesToSecondDialogueAndResetsSentence()
        {
            _controller.StartDialogue();
            yield return null;

            _controller.NextSentence();
            yield return null;
            _controller.NextDialogue();
            yield return null;

            Assert.AreEqual(1, _controller.CurrentDialogueId);
            Assert.AreEqual(0, _controller.CurrentSentenceId);
        }

        [UnityTest]
        public IEnumerator RunningPastTheLastSentence_RaisesEndEvents()
        {
            // WHY: AddComponent does not run Unity's serialization pass, so every UnityEvent field on a
            // component created in code is null until something assigns it.
            _controller.OnDialogueEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd = new UnityEvent();

            int dialogueEnded = 0;
            int allEnded = 0;
            _controller.OnDialogueEnd.AddListener(() => dialogueEnded++);
            _controller.OnAllDialoguesEnd.AddListener(() => allEnded++);

            _controller.StartDialogue(1);
            yield return null;

            // WHY: dialogue 1 holds a single sentence, so one advance runs past its end.
            _controller.NextSentence();
            yield return null;

            Assert.GreaterOrEqual(dialogueEnded, 1, "Finishing the last sentence must raise OnDialogueEnd.");
            Assert.GreaterOrEqual(allEnded, 1, "Finishing the last dialogue must raise OnAllDialoguesEnd.");
        }

        [UnityTest]
        public IEnumerator DisablingMidDialogue_DoesNotThrowAndKeepsState()
        {
            _controller.StartDialogue();
            yield return null;

            _go.SetActive(false);
            yield return null;
            _go.SetActive(true);
            yield return null;

            Assert.IsTrue(_controller.DialogueStarted);
        }
    }
}
