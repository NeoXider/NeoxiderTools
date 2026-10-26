using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests.Edit
{
    public sealed class Audit20261023NeoToolsDialogueEditModeTests
    {
        private GameObject _gameObject;
        private DialogueController _controller;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(Audit20261023NeoToolsDialogueEditModeTests));
            _controller = _gameObject.AddComponent<DialogueController>();
            _controller.useTypewriterEffect = false;
            _controller.dialogues = new[] { BuildDialogue("Original"), BuildDialogue("Replacement") };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [TestCase("dialogue")]
        [TestCase("monolog")]
        [TestCase("sentence")]
        [TestCase("character")]
        public void PresentationCallback_StartsReplacement_AbandonsOldPresentation(string callback)
        {
            int presented = 0;
            _controller.OnSentenceEnd = new UnityEvent();
            _controller.OnSentenceEnd.AddListener(() => presented++);
            UnityAction replace = () => _controller.StartDialogue(1);

            switch (callback)
            {
                case "dialogue":
                    _controller.dialogues[0].OnChangeDialog = new UnityEvent<int>();
                    _controller.dialogues[0].OnChangeDialog.AddListener(_ => replace());
                    break;
                case "monolog":
                    _controller.dialogues[0].monologues[0].OnChangeMonolog = new UnityEvent<int>();
                    _controller.dialogues[0].monologues[0].OnChangeMonolog.AddListener(_ => replace());
                    break;
                case "sentence":
                    _controller.dialogues[0].monologues[0].sentences[0].OnChangeSentence = new UnityEvent();
                    _controller.dialogues[0].monologues[0].sentences[0].OnChangeSentence.AddListener(replace);
                    break;
                case "character":
                    _controller.OnCharacterChange = new UnityEvent<string>();
                    _controller.OnCharacterChange.AddListener(name =>
                    {
                        if (name == "Original")
                        {
                            replace();
                        }
                    });
                    break;
            }

            _controller.StartDialogue();

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.GetCurrentSentence().sentence, Is.EqualTo("Replacement"));
            Assert.That(presented, Is.EqualTo(1), "The interrupted call must not present a second, stale sentence.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EndCallback_StartsReplacement_DoesNotFinishReplacement(bool monologEnd)
        {
            int allEnded = 0;
            _controller.OnAllDialoguesEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd.AddListener(() => allEnded++);
            UnityEvent endEvent = new UnityEvent();
            endEvent.AddListener(() => _controller.StartDialogue(1));
            if (monologEnd)
            {
                _controller.OnMonologEnd = endEvent;
            }
            else
            {
                _controller.OnDialogueEnd = endEvent;
            }

            _controller.StartDialogue();
            _controller.Advance();

            Assert.That(_controller.DialogueStarted, Is.True);
            Assert.That(_controller.CurrentDialogueId, Is.EqualTo(1));
            Assert.That(_controller.GetCurrentSentence().sentence, Is.EqualTo("Replacement"));
            Assert.That(allEnded, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AllDialoguesEnd_RestartsPlayback_RetainsStartedState(bool directNextDialogue)
        {
            _controller.dialogues = new[] { BuildDialogue("Loop") };
            bool startedDuringEnd = true;
            int ended = 0;
            _controller.OnAllDialoguesEnd = new UnityEvent();
            _controller.OnAllDialoguesEnd.AddListener(() =>
            {
                ended++;
                startedDuringEnd = _controller.DialogueStarted;
                _controller.StartDialogue();
            });

            _controller.StartDialogue();
            if (directNextDialogue)
            {
                _controller.NextDialogue();
            }
            else
            {
                _controller.Advance();
            }

            Assert.That(startedDuringEnd, Is.False, "Completion state must be visible before notifying listeners.");
            Assert.That(_controller.DialogueStarted, Is.True, "The listener's restart must survive the old call.");
            Assert.That(_controller.CurrentDialogueId, Is.Zero);
            Assert.That(_controller.GetCurrentSentence().sentence, Is.EqualTo("Loop"));
            Assert.That(ended, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StartDialogue_WithMissingData_ClearsPreviousStartedState(bool nullArray)
        {
            _controller.StartDialogue();
            Assert.That(_controller.DialogueStarted, Is.True);
            _controller.dialogues = nullArray ? null : new Dialogue[0];

            _controller.StartDialogue();

            Assert.That(_controller.DialogueStarted, Is.False);
            Assert.That(_controller.GetCurrentDialogue(), Is.Null);
        }

        [TestCase("", 0)]
        [TestCase("Ann", 1)]
        public void SetCharacterName_WithCachedName_RefreshesLabelWithoutDuplicateEvent(string characterName,
            int expectedChanges)
        {
            DialogueUI ui = _gameObject.AddComponent<DialogueUI>();
            TextMeshProUGUI label = _gameObject.AddComponent<TextMeshProUGUI>();
            Assert.That(label.font, Is.Not.Null, "The project's TMP default font is required by this UI fixture.");
            Assert.That(label.font.material, Is.Not.Null);
            ui.characterNameText = label;
            int changes = 0;
            ui.OnCharacterChange = new UnityEvent<string>();
            ui.OnCharacterChange.AddListener(_ => changes++);
            ui.SetCharacterName(characterName);
            label.text = "Existing label";

            ui.SetCharacterName(characterName);

            Assert.That(label.text, Is.EqualTo(characterName));
            Assert.That(changes, Is.EqualTo(expectedChanges));
        }

        private static Dialogue BuildDialogue(string speaker)
        {
            return new Dialogue
            {
                monologues = new[]
                {
                    new Monolog
                    {
                        characterName = speaker,
                        sentences = new[] { new Sentence { sentence = speaker } }
                    }
                }
            };
        }
    }
}
