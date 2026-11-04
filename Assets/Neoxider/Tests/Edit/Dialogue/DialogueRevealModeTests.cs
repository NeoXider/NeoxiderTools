using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     The MaxVisibleCharacters typewriter mode of <see cref="DialogueController" /> and the visible-character API
    ///     of <see cref="DialogueUI" />. Only synchronous runs are checked here: either the first step before the
    ///     typewriter's first delay, which EditMode does not advance, or a whole run whose per-character delay rounds
    ///     to zero milliseconds.
    /// </summary>
    public sealed class DialogueRevealModeTests
    {
        private const string SentenceText = "Hello <b>brave</b> traveller";
        private const int Unlimited = 99999;

        private GameObject _gameObject;
        private DialogueController _controller;
        private DialogueUI _ui;
        private TextMeshProUGUI _label;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(DialogueRevealModeTests), typeof(RectTransform));
            GameObject labelObject = new("Body", typeof(RectTransform));
            labelObject.transform.SetParent(_gameObject.transform, false);
            labelObject.SetActive(false);
            _label = labelObject.AddComponent<TextMeshProUGUI>();
            if (_label.font == null || _label.font.material == null)
            {
                Assert.Ignore("No TMP default font asset with a material is available in this project.");
            }

            _ui = _gameObject.AddComponent<DialogueUI>();
            _ui.dialogueText = _label;
            _controller = _gameObject.AddComponent<DialogueController>();
            _controller.dialogues = new[] { BuildDialogue() };
            // WHY: one character per second keeps every run waiting on its first delay while the test asserts.
            _controller.Typewriter.CharactersPerSecond = 1f;
            SetPrivateField(_controller, "_dialogueUI", _ui);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
            {
                _controller.RevealMode = TypewriterRevealMode.RebuildText;
                _controller.useTypewriterEffect = false;
                _controller.StartDialogue();
            }

            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void RevealMode_DefaultsToRebuildText()
        {
            Assert.That(_controller.RevealMode, Is.EqualTo(TypewriterRevealMode.RebuildText));
        }

        [Test]
        public void MaxVisibleCharacters_AssignsTheWholeSentenceOnceAndRevealsIt()
        {
            _controller.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;

            _controller.StartDialogue();

            Assert.That(_controller.IsTyping, Is.True, "positive control: the run waits on its first delay");
            Assert.That(_label.text, Is.EqualTo(SentenceText), "the full sentence is assigned up front");
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(1), "the first step revealed one character");

            _controller.CompleteTypewriter();

            Assert.That(_controller.IsTyping, Is.False);
            Assert.That(_label.text, Is.EqualTo(SentenceText));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited), "completion lifts the limit");
        }

        [TestCase("A<br>B")]
        [TestCase("x < y > z")]
        [TestCase("Coin <sprite=0> earned")]
        public void MaxVisibleCharacters_NaturalCompletion_ShowsTheWholeSentence(string sentence)
        {
            _controller.dialogues = new[] { BuildDialogue(sentence) };
            _controller.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            _controller.Typewriter.UsePunctuationPauses = false;
            // WHY: 1 / 10000 s rounds to a 0 ms delay, so the typewriter never awaits and the run finishes inside
            // StartDialogue.
            _controller.Typewriter.CharactersPerSecond = 10000f;
            bool completed = false;
            int firstStepLimit = -1;
            _controller.Typewriter.OnComplete += () => completed = true;
            _controller.Typewriter.OnCharacterTyped += _ =>
            {
                if (firstStepLimit < 0)
                {
                    firstStepLimit = _label.maxVisibleCharacters;
                }
            };

            _controller.StartDialogue();

            Assert.That(completed, Is.True, "positive control: the run completed on its own, not via CompleteTypewriter");
            Assert.That(firstStepLimit, Is.EqualTo(1), "positive control: the reveal path lowered the limit");
            Assert.That(_controller.IsTyping, Is.False);
            Assert.That(_label.text, Is.EqualTo(sentence));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited),
                "the typewriter counts every '<...>' as a tag while TMP counts <br>, sprites and plain '<' text as " +
                "characters, so a finished sentence must lift the limit instead of keeping the run's final count");
        }

        [Test]
        public void MaxVisibleCharacters_NaturalCompletion_RestoresTheLabelsOwnLimit()
        {
            _label.maxVisibleCharacters = 40;
            _controller.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            _controller.Typewriter.UsePunctuationPauses = false;
            _controller.Typewriter.CharactersPerSecond = 10000f;
            bool completed = false;
            int firstStepLimit = -1;
            _controller.Typewriter.OnComplete += () => completed = true;
            _controller.Typewriter.OnCharacterTyped += _ =>
            {
                if (firstStepLimit < 0)
                {
                    firstStepLimit = _label.maxVisibleCharacters;
                }
            };

            _controller.StartDialogue();

            Assert.That(completed && !_controller.IsTyping, Is.True, "positive control: the run finished synchronously");
            Assert.That(firstStepLimit, Is.EqualTo(1), "positive control: the reveal path lowered the authored limit");
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(40), "the label's authored limit comes back");
        }

        [Test]
        public void RebuildText_KeepsTheLegacyGrowingPrefix()
        {
            _controller.StartDialogue();

            Assert.That(_controller.IsTyping, Is.True);
            Assert.That(_label.text, Is.EqualTo("H"));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited));
        }

        [Test]
        public void SwitchingBackToRebuildText_DoesNotInheritTheRevealLimit()
        {
            _controller.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            _controller.StartDialogue();
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(1));

            _controller.RevealMode = TypewriterRevealMode.RebuildText;
            _controller.StartDialogue();

            Assert.That(_label.text, Is.EqualTo("H"));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited));
        }

        [Test]
        public void DialogueUI_OnlyLiftsALimitItSetItself()
        {
            _label.maxVisibleCharacters = 4;
            _ui.SetDialogueText("Authored limit");
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(4), "a label's own limit is not the UI's to change");

            _ui.SetDialogueTextRevealed("Revealed", 2);
            Assert.That(_label.text, Is.EqualTo("Revealed"));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(2));
            _ui.SetVisibleCharacters(-3);
            Assert.That(_label.maxVisibleCharacters, Is.Zero, "negative counts clamp to zero");

            _ui.ClearDialogueText();
            Assert.That(_label.text, Is.Empty);
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(4),
                "lifting the reveal limit puts back the label's own limit, not TMP's unlimited default");

            _ui.ClearDialogueText();
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(4), "nothing to lift once the limit is gone");
        }

        [Test]
        public void DialogueUI_ShowAllCharacters_LiftsTheLimitWithoutTouchingTheText()
        {
            _ui.ShowAllCharacters();
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited), "no limit set: nothing changes");

            _ui.SetDialogueTextRevealed("Partly shown", 3);
            _ui.SetVisibleCharacters(7);
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(7));

            _ui.ShowAllCharacters();

            Assert.That(_label.text, Is.EqualTo("Partly shown"));
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited),
                "the value captured before the first lowering is restored, not an intermediate reveal step");
        }

        [Test]
        public void DialogueUI_RevealSteps_DoNotAllocate()
        {
            string line = new string('a', 600);
            _ui.SetDialogueTextRevealed(line, 0);
            for (int count = 0; count < 50; count++)
            {
                _ui.SetVisibleCharacters(count);
            }

            long allocationCalls = GcAlloc.TryCount(() =>
            {
                for (int count = 1; count <= line.Length; count++)
                {
                    _ui.SetVisibleCharacters(count);
                }
            }, out string notAdmitted);

            Assert.That(_label.text, Is.EqualTo(line), "positive control: the whole line stays assigned");
            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(line.Length), "positive control: the reveal advanced");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "a reveal step only raises maxVisibleCharacters; it must not build a string per character");
        }

        [Test]
        public void DialogueUI_RebindingTheLabel_RestoresTheLabelThatWasLimited()
        {
            GameObject otherObject = new("OtherBody", typeof(RectTransform));
            otherObject.transform.SetParent(_gameObject.transform, false);
            otherObject.SetActive(false);
            TextMeshProUGUI other = otherObject.AddComponent<TextMeshProUGUI>();
            _ui.SetDialogueTextRevealed("First", 1);

            _ui.dialogueText = other;
            _ui.SetDialogueTextRevealed("Second", 2);

            Assert.That(_label.maxVisibleCharacters, Is.EqualTo(Unlimited), "the previous label is released");
            Assert.That(other.maxVisibleCharacters, Is.EqualTo(2));

            _ui.ShowAllCharacters();
            Assert.That(other.maxVisibleCharacters, Is.EqualTo(Unlimited));
        }

        private static Dialogue BuildDialogue(string sentence = SentenceText)
        {
            return new Dialogue
            {
                monologues = new[]
                {
                    new Monolog
                    {
                        characterName = "Guide",
                        sentences = new[] { new Sentence { sentence = sentence } }
                    }
                }
            };
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
