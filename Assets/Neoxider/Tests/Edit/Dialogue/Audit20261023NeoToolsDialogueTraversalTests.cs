using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoToolsDialogueTraversalTests
    {
        private const int EmptyCount = 10000;
        private GameObject _gameObject;
        private DialogueController _controller;
        private Sentence _tail;
        private int _presented;
        private int _monologEnded;
        private int _dialogueEnded;
        private int _allEnded;

        [SetUp]
        public void SetUp()
        {
            _presented = 0;
            _monologEnded = 0;
            _dialogueEnded = 0;
            _allEnded = 0;
            _tail = new Sentence { sentence = "Tail" };
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsDialogueTraversalTests));
            _controller = _gameObject.AddComponent<DialogueController>();
            _controller.useTypewriterEffect = false;
            _controller.OnSentenceEnd = new UnityEvent();
            _controller.OnSentenceEnd.AddListener(() => _presented++);
            _controller.OnMonologEnd = new UnityEvent();
            _controller.OnMonologEnd.AddListener(() => _monologEnded++);
            _controller.OnDialogueEnd = new UnityEvent();
            _controller.OnDialogueEnd.AddListener(() => _dialogueEnded++);
            _controller.OnAllDialoguesEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd.AddListener(() => _allEnded++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartDialogue_SkipsTenThousandEmptyContainersAndPresentsTailSynchronously(bool emptyMonologs)
        {
            _controller.dialogues = CreateSparseGraph(emptyMonologs, EmptyCount, true);

            _controller.StartDialogue();

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(emptyMonologs ? 0 : EmptyCount));
            Assert.That(_controller.CurrentMonologId, Is.EqualTo(emptyMonologs ? EmptyCount : 0));
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_controller.GetCurrentSentence(), Is.SameAs(_tail));
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(_monologEnded, Is.EqualTo(emptyMonologs ? EmptyCount : 0));
            Assert.That(_dialogueEnded, Is.EqualTo(emptyMonologs ? 0 : EmptyCount));
            Assert.That(_allEnded, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartDialogue_WithTenThousandEmptyContainers_CompletesOnce(bool emptyMonologs)
        {
            _controller.dialogues = CreateSparseGraph(emptyMonologs, EmptyCount, false);

            _controller.StartDialogue();
            _controller.Advance();

            Assert.That(_controller.DialogueStarted, Is.False);
            Assert.That(_controller.GetCurrentSentence(), Is.Null);
            Assert.That(_presented, Is.Zero);
            Assert.That(_monologEnded, Is.EqualTo(emptyMonologs ? EmptyCount : 0));
            Assert.That(_dialogueEnded, Is.EqualTo(emptyMonologs ? 1 : EmptyCount));
            Assert.That(_allEnded, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BoundaryCallback_StartsTail_AbandonsOldSparseTraversal(bool emptyMonologs)
        {
            _controller.dialogues = CreateSparseGraph(emptyMonologs, EmptyCount, true);
            UnityEvent boundary = emptyMonologs ? _controller.OnMonologEnd : _controller.OnDialogueEnd;
            boundary.AddListener(() =>
            {
                int ended = emptyMonologs ? _monologEnded : _dialogueEnded;
                if (ended == 25)
                {
                    _controller.StartDialogue(emptyMonologs ? 0 : EmptyCount, emptyMonologs ? EmptyCount : 0);
                }
            });

            _controller.StartDialogue();

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.GetCurrentSentence(), Is.SameAs(_tail));
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(emptyMonologs ? _monologEnded : _dialogueEnded, Is.EqualTo(25));
            Assert.That(_allEnded, Is.Zero);
        }

        [Test]
        public void SparseTraversal_PreservesDataBoundaryAndPresentationEventOrder()
        {
            List<string> order = new List<string>();
            Dialogue emptyDialogue = new Dialogue { monologues = new Monolog[0], OnChangeDialog = new UnityEvent<int>() };
            Monolog emptyMonolog = new Monolog { sentences = new Sentence[0], OnChangeMonolog = new UnityEvent<int>() };
            Monolog tailMonolog = CreateTailMonolog();
            tailMonolog.OnChangeMonolog = new UnityEvent<int>();
            _tail.OnChangeSentence = new UnityEvent();
            Dialogue tailDialogue = new Dialogue
            {
                monologues = new[] { emptyMonolog, tailMonolog },
                OnChangeDialog = new UnityEvent<int>()
            };
            emptyDialogue.OnChangeDialog.AddListener(index => order.Add("dialogue:" + index));
            tailDialogue.OnChangeDialog.AddListener(index => order.Add("dialogue:" + index));
            emptyMonolog.OnChangeMonolog.AddListener(index => order.Add("monolog:" + index));
            tailMonolog.OnChangeMonolog.AddListener(index => order.Add("monolog:" + index));
            _tail.OnChangeSentence.AddListener(() => order.Add("sentence"));
            _controller.OnMonologEnd.AddListener(() => order.Add("monologEnd:" + _controller.CurrentMonologId));
            _controller.OnDialogueEnd.AddListener(() => order.Add("dialogueEnd:" + _controller.CurrentDialogueId));
            _controller.OnAllDialoguesEnd.AddListener(() => order.Add("allEnd"));
            _controller.OnCharacterChange = new UnityEvent<string>();
            _controller.OnCharacterChange.AddListener(name => order.Add("character:" + name));
            _controller.OnSentenceEnd.AddListener(() => order.Add("presented"));
            _controller.dialogues = new[] { emptyDialogue, tailDialogue };

            _controller.StartDialogue();
            _controller.Advance();

            CollectionAssert.AreEqual(new[]
            {
                "dialogue:0", "dialogueEnd:0", "dialogue:1", "monolog:0", "monologEnd:0",
                "dialogue:1", "monolog:1", "sentence", "character:Tail", "presented",
                "monologEnd:1", "dialogueEnd:1", "allEnd"
            }, order);
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(_allEnded, Is.EqualTo(1));
            Assert.That(_controller.DialogueStarted, Is.False);
        }

        [TestCase("dialogue")]
        [TestCase("monolog")]
        [TestCase("sentence")]
        public void SparseTraversal_StopsAtNullEntryInsteadOfSkippingIt(string nullLevel)
        {
            if (nullLevel == "dialogue")
            {
                _controller.dialogues = CreateSparseGraph(false, 64, true);
                _controller.dialogues[63] = null;
            }
            else
            {
                _controller.dialogues = CreateSparseGraph(true, 64, true);
                Monolog[] monologues = _controller.dialogues[0].monologues;
                if (nullLevel == "monolog")
                {
                    monologues[63] = null;
                }
                else
                {
                    monologues[63] = new Monolog { sentences = new[] { null, _tail } };
                }
            }

            _controller.StartDialogue();

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.GetCurrentSentence(), Is.Null);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(nullLevel == "dialogue" ? 63 : 0));
            Assert.That(_controller.CurrentMonologId, Is.EqualTo(nullLevel == "dialogue" ? 0 : 63));
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_presented, Is.Zero);
            Assert.That(_allEnded, Is.Zero);
        }

        private Dialogue[] CreateSparseGraph(bool emptyMonologs, int count, bool includeTail)
        {
            int length = count + (includeTail ? 1 : 0);
            if (emptyMonologs)
            {
                Monolog[] monologues = new Monolog[length];
                for (int i = 0; i < count; i++)
                {
                    monologues[i] = new Monolog { sentences = i % 2 == 0 ? null : new Sentence[0] };
                }

                if (includeTail)
                {
                    monologues[count] = CreateTailMonolog();
                }

                return new[] { new Dialogue { monologues = monologues } };
            }

            Dialogue[] dialogues = new Dialogue[length];
            for (int i = 0; i < count; i++)
            {
                dialogues[i] = new Dialogue { monologues = i % 2 == 0 ? null : new Monolog[0] };
            }

            if (includeTail)
            {
                dialogues[count] = new Dialogue { monologues = new[] { CreateTailMonolog() } };
            }

            return dialogues;
        }

        private Monolog CreateTailMonolog()
        {
            return new Monolog { characterName = "Tail", sentences = new[] { _tail } };
        }
    }
}
