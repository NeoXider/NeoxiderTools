using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoToolsDialoguePlayModeTests
    {
        private GameObject _gameObject;
        private DialogueController _controller;
        private int _presented;

        [SetUp]
        public void SetUp()
        {
            _presented = 0;
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsDialoguePlayModeTests));
            _controller = _gameObject.AddComponent<DialogueController>();
            _controller.autoStart = false;
            _controller.Typewriter.CharactersPerSecond = 0.1f;
            _controller.Typewriter.UsePunctuationPauses = false;
            _controller.dialogues = new[]
            {
                BuildDialogue("Original", "First sentence", "Second sentence"),
                BuildDialogue("Replacement", "Replacement first", "Replacement second")
            };
            _controller.OnSentenceEnd = new UnityEvent();
            _controller.OnSentenceEnd.AddListener(() => _presented++);
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DisableWhileTyping_DoesNotResurrectAutomaticPlayback()
        {
            _controller.autoNextSentence = true;
            _controller.autoNextSentenceDelay = 0f;
            _controller.StartDialogue();
            Assert.That(_controller.IsTyping, Is.True);

            _gameObject.SetActive(false);
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.CurrentDialogueId, Is.Zero);
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(_controller.IsTyping, Is.False);
            Assert.That(_controller.DialogueStarted, Is.True, "Disabling preserves the cursor for manual continuation.");
        }

        [UnityTest]
        public IEnumerator RestartWhileTyping_DoesNotAdvanceReplacementFromCancelledRun()
        {
            _controller.autoNextSentence = true;
            _controller.autoNextSentenceDelay = 0f;
            _controller.StartDialogue();

            _controller.StartDialogue(1);
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_controller.IsTyping, Is.True);
            Assert.That(_controller.Typewriter.FullText, Is.EqualTo("Replacement first"));
            Assert.That(_presented, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator CompleteTypewriter_CompletesEngineAndKeepsItsScheduledDelay()
        {
            _controller.autoNextSentence = true;
            _controller.autoNextSentenceDelay = 60f;
            int completedProgress = 0;
            _controller.OnTypewriterProgress = new UnityEvent<float>();
            _controller.OnTypewriterProgress.AddListener(progress =>
            {
                if (progress >= 1f)
                {
                    completedProgress++;
                }
            });
            _controller.StartDialogue();

            _controller.CompleteTypewriter();

            Assert.That(_controller.IsTyping, Is.False);
            Assert.That(_controller.Typewriter.Progress, Is.EqualTo(1f));
            Assert.That(_controller.Typewriter.CurrentText, Is.EqualTo("First sentence"));
            Assert.That(completedProgress, Is.EqualTo(1));
            // A stale continuation would read the new delay and advance immediately.
            _controller.autoNextSentenceDelay = 0f;
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_presented, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ZeroDelay_AutomaticTransitionsYieldAndFinishInOrder()
        {
            _controller.useTypewriterEffect = false;
            _controller.autoNextSentence = true;
            _controller.autoNextMonolog = true;
            _controller.autoNextDialogue = true;
            _controller.autoNextSentenceDelay = 0f;
            _controller.autoNextMonologDelay = 0f;
            _controller.autoNextDialogueDelay = 0f;
            _controller.dialogues[0].monologues = new[]
            {
                _controller.dialogues[0].monologues[0],
                BuildDialogue("Third", "Third sentence").monologues[0]
            };
            int allEnded = 0;
            _controller.OnAllDialoguesEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd.AddListener(() => allEnded++);

            _controller.StartDialogue();

            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_controller.CurrentDialogueId, Is.Zero);
            Assert.That(_presented, Is.EqualTo(1), "Zero-delay playback must return before the next presentation.");
            Assert.That(_controller.DialogueStarted, Is.True);
            for (int frame = 0; frame < 16 && _controller.DialogueStarted; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.DialogueStarted, Is.False);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.CurrentSentenceId, Is.EqualTo(1));
            Assert.That(_presented, Is.EqualTo(5));
            Assert.That(allEnded, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisableWithZeroDelayPending_CancelsScheduledTransition()
        {
            _controller.useTypewriterEffect = false;
            _controller.autoNextSentence = true;
            _controller.autoNextSentenceDelay = 0f;
            _controller.StartDialogue();

            _gameObject.SetActive(false);
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(_controller.DialogueStarted, Is.True);
        }

        [Test]
        public void AdvanceDuringFinalSentence_StopsTypewriterBeforeFinishing()
        {
            _controller.dialogues = new[] { BuildDialogue("Only", "Final sentence") };
            int allEnded = 0;
            _controller.OnAllDialoguesEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd.AddListener(() => allEnded++);
            _controller.StartDialogue();
            Assert.That(_controller.IsTyping, Is.True);

            _controller.Advance();

            Assert.That(_controller.DialogueStarted, Is.False);
            Assert.That(_controller.IsTyping, Is.False);
            Assert.That(allEnded, Is.EqualTo(1));
        }

        [Test]
        public void DestroyController_DetachesNotificationsFromRetainedTypewriter()
        {
            int progressNotifications = 0;
            _controller.OnTypewriterProgress = new UnityEvent<float>();
            _controller.OnTypewriterProgress.AddListener(_ => progressNotifications++);
            TypewriterEffect retainedTypewriter = _controller.Typewriter;

            Object.DestroyImmediate(_gameObject);
            retainedTypewriter.Complete();

            Assert.That(progressNotifications, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SparseDialogueDelay_StopsTraversalUntilScheduledTransition()
        {
            return VerifySparseBoundaryDelay(false);
        }

        [UnityTest]
        public IEnumerator SparseMonologDelay_StopsTraversalUntilScheduledTransition()
        {
            return VerifySparseBoundaryDelay(true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SparseBoundaryCallback_DisablesController_StopsTraversal(bool emptyMonologs)
        {
            ConfigureSparseGraph(emptyMonologs);
            UnityEvent boundary = new UnityEvent();
            int ended = 0;
            boundary.AddListener(() =>
            {
                ended++;
                _controller.enabled = false;
            });
            if (emptyMonologs)
            {
                _controller.OnMonologEnd = boundary;
            }
            else
            {
                _controller.OnDialogueEnd = boundary;
            }

            _controller.StartDialogue();

            Assert.That(_controller.enabled, Is.False);
            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.CurrentDialogueId, Is.Zero);
            Assert.That(_controller.CurrentMonologId, Is.Zero);
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(_presented, Is.Zero);
        }

        [Test]
        public void NativeTypingCompletion_RestartsController_KeepsNewRunAlive()
        {
            _controller.Typewriter.CharactersPerSecond = 5000f;
            int completed = 0;
            _controller.Typewriter.OnComplete += () =>
            {
                completed++;
                if (completed == 1)
                {
                    _controller.Typewriter.CharactersPerSecond = 0.1f;
                    _controller.StartDialogue(1);
                }
            };

            _controller.StartDialogue();

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_controller.IsTyping, Is.True);
            Assert.That(_controller.Typewriter.FullText, Is.EqualTo("Replacement first"));
            Assert.That(_controller.Typewriter.CurrentText, Is.EqualTo("R"));
            Assert.That(_presented, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SkipProgressCallback_RestartsController_EmitsNoOldCompletionOrAutoAdvance()
        {
            _controller.autoNextSentence = true;
            _controller.autoNextSentenceDelay = 0f;
            int completed = 0;
            bool restarted = false;
            _controller.Typewriter.OnComplete += () => completed++;
            _controller.OnTypewriterProgress = new UnityEvent<float>();
            _controller.OnTypewriterProgress.AddListener(progress =>
            {
                if (progress >= 1f && !restarted)
                {
                    restarted = true;
                    _controller.StartDialogue(1);
                }
            });
            _controller.StartDialogue();

            _controller.CompleteTypewriter();
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Assert.That(restarted, Is.True);
            Assert.That(completed, Is.Zero);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.CurrentSentenceId, Is.Zero);
            Assert.That(_controller.IsTyping, Is.True);
            Assert.That(_controller.Typewriter.FullText, Is.EqualTo("Replacement first"));
            Assert.That(_presented, Is.EqualTo(2));
        }

        private IEnumerator VerifySparseBoundaryDelay(bool emptyMonologs)
        {
            ConfigureSparseGraph(emptyMonologs);
            _controller.autoNextMonolog = emptyMonologs;
            _controller.autoNextDialogue = !emptyMonologs;
            _controller.autoNextMonologDelay = 0f;
            _controller.autoNextDialogueDelay = 0f;
            int ended = 0;
            UnityEvent boundary = new UnityEvent();
            boundary.AddListener(() => ended++);
            if (emptyMonologs)
            {
                _controller.OnMonologEnd = boundary;
            }
            else
            {
                _controller.OnDialogueEnd = boundary;
            }

            _controller.StartDialogue();

            Assert.That(_controller.CurrentDialogueId, Is.Zero);
            Assert.That(_controller.CurrentMonologId, Is.Zero);
            Assert.That(_presented, Is.Zero);
            Assert.That(ended, Is.EqualTo(1));
            for (int frame = 0; frame < 8 && _presented == 0; frame++)
            {
                yield return null;
            }

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(emptyMonologs ? 0 : 2));
            Assert.That(_controller.CurrentMonologId, Is.EqualTo(emptyMonologs ? 2 : 0));
            Assert.That(_controller.GetCurrentSentence().sentence, Is.EqualTo("Tail"));
            Assert.That(_presented, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(2));
        }

        private void ConfigureSparseGraph(bool emptyMonologs)
        {
            _controller.useTypewriterEffect = false;
            Dialogue tail = BuildDialogue("Tail", "Tail");
            _controller.dialogues = emptyMonologs
                ? new[]
                {
                    new Dialogue
                    {
                        monologues = new[]
                        {
                            new Monolog { sentences = new Sentence[0] },
                            new Monolog { sentences = null },
                            tail.monologues[0]
                        }
                    }
                }
                : new[]
                {
                    new Dialogue { monologues = new Monolog[0] },
                    new Dialogue { monologues = null },
                    tail
                };
        }

        private static Dialogue BuildDialogue(string speaker, params string[] text)
        {
            Sentence[] sentences = new Sentence[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                sentences[i] = new Sentence { sentence = text[i] };
            }

            return new Dialogue
            {
                monologues = new[] { new Monolog { characterName = speaker, sentences = sentences } }
            };
        }
    }
}
