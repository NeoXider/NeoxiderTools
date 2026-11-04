using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class TypewriterRevealTests
    {
        private const int UnlimitedVisibleCharacters = 99999;

        private GameObject _root;
        private TypewriterEffectComponent _component;

        [TearDown]
        public void TearDown()
        {
            if (_component != null)
            {
                Invoke(_component, "OnDestroy");
                _component = null;
            }

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        [Test]
        public void PlayAsync_RichTextTags_AreAppendedAsOneStep()
        {
            TypewriterEffect effect = CreateInstantEffect();
            List<string> outputs = new();

            effect.PlayAsync("<b>Hi</b>", outputs.Add).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "", "<b>", "<b>H", "<b>Hi", "<b>Hi</b>" }, outputs);
            Assert.That(effect.CurrentText, Is.EqualTo("<b>Hi</b>"));
        }

        [Test]
        public void PlayRevealAsync_ReportsVisibleCharactersOnly()
        {
            TypewriterEffect effect = CreateInstantEffect();
            List<int> counts = new();
            List<char> typed = new();
            effect.OnCharacterTyped += typed.Add;

            effect.PlayRevealAsync("<b>Hi</b>!", counts.Add).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, counts);
            CollectionAssert.AreEqual(new[] { 'H', 'i', '!' }, typed);
            Assert.That(effect.Progress, Is.EqualTo(1f));
            Assert.That(effect.FullText, Is.EqualTo("<b>Hi</b>!"));
            Assert.That(effect.IsTyping, Is.False);
        }

        [Test]
        public void PlayRevealAsync_NeverBuildsThePrefixString()
        {
            TypewriterEffect effect = CreateInstantEffect();
            int last = 0;

            effect.PlayRevealAsync("<i>" + new string('b', 400) + "</i>", count => last = count).GetAwaiter()
                .GetResult();

            // WHY: the prefix builder is the only per-character string source; a reveal run must leave it untouched.
            Assert.That(GetBuilder(effect).Length, Is.Zero);
            Assert.That(last, Is.EqualTo(400));
        }

        [Test]
        public void PlayRevealAsync_GcAllocCallsDoNotGrowWithTextLength()
        {
            TypewriterEffect effect = CreateInstantEffect();
            int last = 0;
            Action<int> sink = count => last = count;
            string shortText = new('a', 10);
            string longText = new('b', 400);
            // WHY: warm UniTask pools before measuring.
            effect.PlayRevealAsync(longText, sink).GetAwaiter().GetResult();
            effect.PlayRevealAsync(shortText, sink).GetAwaiter().GetResult();

            long shortCalls;
            long longCalls;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                shortCalls = MeasureRun(counter, effect, shortText, sink);
                longCalls = MeasureRun(counter, effect, longText, sink);
            }

            Assert.That(last, Is.EqualTo(400));
            Assert.That(longCalls, Is.EqualTo(shortCalls),
                "Revealing must not allocate per character (no intermediate strings).");
        }

        [Test]
        public void Component_MaxVisibleCharactersMode_AssignsTextOnceAndRevealsIt()
        {
            _root = new GameObject("TypewriterReveal", typeof(RectTransform), typeof(TextMeshProUGUI));
            _root.SetActive(false);
            TextMeshProUGUI label = _root.GetComponent<TextMeshProUGUI>();
            _component = _root.AddComponent<TypewriterEffectComponent>();
            _component.OnComplete = new UnityEvent();
            _component.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            _component.Effect.CharactersPerSecond = 100000f;
            _component.Effect.ClearPunctuationPauses();
            Invoke(_component, "Awake");

            List<int> revealed = new();
            List<string> texts = new();
            _component.Effect.OnCharacterTyped += _ =>
            {
                revealed.Add(label.maxVisibleCharacters);
                texts.Add(label.text);
            };
            int completed = 0;
            _component.OnComplete.AddListener(() => completed++);

            _component.Play("<b>Hey</b>");

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, revealed);
            CollectionAssert.AreEqual(new[] { "<b>Hey</b>", "<b>Hey</b>", "<b>Hey</b>" }, texts);
            Assert.That(label.text, Is.EqualTo("<b>Hey</b>"));
            Assert.That(label.maxVisibleCharacters, Is.EqualTo(UnlimitedVisibleCharacters),
                "Completion restores TMP's unlimited default.");
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_component.ProgressValue, Is.EqualTo(1f));
        }

        [Test]
        public void Component_StoppedReveal_ThenRebuildTextRun_RestoresTheVisibleLimit()
        {
            TextMeshProUGUI label = CreateComponent(0.1f);
            _component.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            try
            {
                _component.Play("Hello");
                Assert.That(label.maxVisibleCharacters, Is.EqualTo(1), "The slow reveal waits after one character.");

                _component.Stop();
                Assert.That(label.maxVisibleCharacters, Is.EqualTo(1), "Stop keeps the current reveal.");

                _component.RevealMode = TypewriterRevealMode.RebuildText;
                _component.Play("World");

                Assert.That(label.maxVisibleCharacters, Is.EqualTo(UnlimitedVisibleCharacters),
                    "A text run must not inherit the stopped reveal's limit.");
                Assert.That(label.text, Is.EqualTo("W"));
            }
            finally
            {
                _component.Stop();
            }
        }

        [Test]
        public void Component_ModeSwitchedDuringReveal_CompleteAndClearStillRestoreTheLimit()
        {
            TextMeshProUGUI label = CreateComponent(0.1f);
            _component.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
            try
            {
                _component.Play("Hello");
                _component.RevealMode = TypewriterRevealMode.RebuildText;

                _component.Complete();

                Assert.That(label.maxVisibleCharacters, Is.EqualTo(UnlimitedVisibleCharacters),
                    "Restoring follows the mode the run started with, not the current one.");
                Assert.That(label.text, Is.EqualTo("Hello"));

                _component.RevealMode = TypewriterRevealMode.MaxVisibleCharacters;
                _component.Play("Again");
                _component.RevealMode = TypewriterRevealMode.RebuildText;
                _component.Clear();

                Assert.That(label.maxVisibleCharacters, Is.EqualTo(UnlimitedVisibleCharacters));
                Assert.That(label.text, Is.Empty);
            }
            finally
            {
                _component.Stop();
            }
        }

        private TextMeshProUGUI CreateComponent(float charactersPerSecond)
        {
            _root = new GameObject("TypewriterRevealRestore", typeof(RectTransform), typeof(TextMeshProUGUI));
            _root.SetActive(false);
            TextMeshProUGUI label = _root.GetComponent<TextMeshProUGUI>();
            _component = _root.AddComponent<TypewriterEffectComponent>();
            _component.OnStart = new UnityEvent();
            _component.OnComplete = new UnityEvent();
            _component.Effect.CharactersPerSecond = charactersPerSecond;
            _component.Effect.ClearPunctuationPauses();
            Invoke(_component, "Awake");
            return label;
        }

        private static long MeasureRun(GcAllocCounter counter, TypewriterEffect effect, string text,
            Action<int> sink)
        {
            counter.Begin();
            effect.PlayRevealAsync(text, sink).GetAwaiter().GetResult();
            return counter.End();
        }

        private static StringBuilder GetBuilder(TypewriterEffect effect)
        {
            FieldInfo field = typeof(TypewriterEffect).GetField("_builder",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (StringBuilder)field.GetValue(effect);
        }

        private static TypewriterEffect CreateInstantEffect()
        {
            // WHY: a per-character delay below 0.5 ms rounds to zero, so the run completes synchronously.
            TypewriterEffect effect = new(100000f);
            effect.ClearPunctuationPauses();
            return effect;
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
