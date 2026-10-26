using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public class Audit20261023NeoToolsComponentsTypewriterTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                TypewriterEffectComponent component = _root.GetComponent<TypewriterEffectComponent>();
                if (component != null)
                {
                    Audit20261023NeoToolsComponentsAccess.Invoke(component, "OnDestroy");
                }

                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void StopInsideOnStart_PreventsOutputAndCompletion()
        {
            TypewriterEffect effect = CreateEffect();
            int outputs = 0;
            int completions = 0;
            effect.OnStart += effect.Stop;
            effect.OnComplete += () => completions++;

            effect.PlayAsync("old", text => outputs++).GetAwaiter().GetResult();

            Assert.That(outputs, Is.Zero);
            Assert.That(completions, Is.Zero);
            Assert.That(effect.CurrentText, Is.Empty);
            Assert.That(effect.IsTyping, Is.False);
        }

        [Test]
        public void ThrowingStartListener_ReleasesRunAndAllowsAnotherPlay()
        {
            TypewriterEffect effect = CreateEffect();
            Action fail = () => throw new InvalidOperationException("listener failure");
            effect.OnStart += fail;

            Assert.Throws<InvalidOperationException>(() => effect.PlayAsync("old", null).GetAwaiter().GetResult());
            Assert.That(effect.IsTyping, Is.False);
            effect.OnStart -= fail;
            effect.PlayAsync("next", null).GetAwaiter().GetResult();
            Assert.That(effect.CurrentText, Is.EqualTo("next"));
        }

        [Test]
        public void RestartInsideTextCallback_DoesNotEmitOldCharacterOrCompletion()
        {
            TypewriterEffect effect = CreateEffect();
            List<char> characters = new List<char>();
            int completions = 0;
            effect.OnCharacterTyped += characters.Add;
            effect.OnComplete += () => completions++;

            effect.PlayAsync("old", text =>
            {
                if (text == "o")
                {
                    effect.PlayAsync("NEW", null).GetAwaiter().GetResult();
                }
            }).GetAwaiter().GetResult();

            Assert.That(effect.CurrentText, Is.EqualTo("NEW"));
            Assert.That(characters, Is.EqualTo("NEW".ToCharArray()));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void CompleteInsideLastCharacterCallback_EmitsOneCompletion()
        {
            TypewriterEffect effect = CreateEffect();
            int completions = 0;
            effect.OnCharacterTyped += character => effect.Complete();
            effect.OnComplete += () => completions++;

            effect.PlayAsync("a", null).GetAwaiter().GetResult();

            Assert.That(effect.CurrentText, Is.EqualTo("a"));
            Assert.That(effect.Progress, Is.EqualTo(1f));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void NaturalCompletion_ReportsIdleToCompletionListener()
        {
            TypewriterEffect effect = CreateEffect();
            bool typingAtCompletion = true;
            effect.OnComplete += () => typingAtCompletion = effect.IsTyping;

            effect.PlayAsync("done", null).GetAwaiter().GetResult();

            Assert.That(typingAtCompletion, Is.False);
            Assert.That(effect.CurrentText, Is.EqualTo("done"));
        }

        [Test]
        public void Complete_WhenListenerStartsAnotherRun_ReturnsOriginalText()
        {
            TypewriterEffect effect = CreateEffect();
            effect.PlayAsync("old", null).GetAwaiter().GetResult();
            bool restarted = false;
            effect.OnComplete += () =>
            {
                if (!restarted)
                {
                    restarted = true;
                    effect.PlayAsync("NEW", null).GetAwaiter().GetResult();
                }
            };

            string completedText = effect.Complete();

            Assert.That(completedText, Is.EqualTo("old"));
            Assert.That(effect.CurrentText, Is.EqualTo("NEW"));
        }

        [Test]
        public void Component_AwakeWithManualPlayback_PreservesAuthoredText()
        {
            TypewriterEffectComponent component = CreateComponent("Authored text");

            Assert.That(component.TargetText.text, Is.EqualTo("Authored text"));
            component.Play();
            Assert.That(component.TargetText.text, Is.EqualTo("Authored text"));
        }

        [Test]
        public void Component_PlayOnEnableWithoutAutoStart_PlaysAtStartAndOnReenable()
        {
            TypewriterEffectComponent component = CreateComponent("Replay");
            Audit20261023NeoToolsComponentsAccess.Configure(component, "_playOnEnable", true);

            Audit20261023NeoToolsComponentsAccess.Invoke(component, "Start");
            Assert.That(component.TargetText.text, Is.EqualTo("Replay"));
            component.Clear();
            Assert.That(component.TargetText.text, Is.Empty);
            Audit20261023NeoToolsComponentsAccess.Invoke(component, "OnEnable");
            Assert.That(component.TargetText.text, Is.EqualTo("Replay"));
        }

        [Test]
        public void Component_Clear_ResetsEffectAndReactiveProgress()
        {
            TypewriterEffectComponent component = CreateComponent("Done");
            component.Play("Done");
            Assert.That(component.ProgressValue, Is.EqualTo(1f));

            component.Clear();

            Assert.That(component.ProgressValue, Is.Zero);
            Assert.That(component.Effect.Progress, Is.Zero);
            Assert.That(component.Effect.FullText, Is.Empty);
            Assert.That(component.TargetText.text, Is.Empty);
        }

        [Test]
        public void Component_Complete_UpdatesLabelBeforeCompletionAndKeepsRestartAlive()
        {
            TypewriterEffectComponent component = CreateComponent("Unused");
            component.Effect.CharactersPerSecond = 0.1f;
            string observedText = null;
            component.Play("original");
            component.OnComplete.AddListener(() =>
            {
                observedText = component.TargetText.text;
                component.Play("replacement");
            });
            try
            {
                component.Complete();

                Assert.That(observedText, Is.EqualTo("original"));
                Assert.That(component.Effect.FullText, Is.EqualTo("replacement"));
                Assert.That(component.IsTyping, Is.True);
                Assert.That(component.TargetText.text, Is.EqualTo("r"));
            }
            finally
            {
                component.Stop();
            }
        }

        [Test]
        public void Component_Destroy_DetachesForwardersFromExternallyRetainedEffect()
        {
            TypewriterEffectComponent component = CreateComponent("Retained");
            int forwardedCompletions = 0;
            component.OnComplete.AddListener(() => forwardedCompletions++);
            TypewriterEffect retainedEffect = component.Effect;

            Audit20261023NeoToolsComponentsAccess.Invoke(component, "OnDestroy");
            retainedEffect.PlayAsync("later", null).GetAwaiter().GetResult();

            Assert.That(retainedEffect.CurrentText, Is.EqualTo("later"));
            Assert.That(forwardedCompletions, Is.Zero);
        }

        private static TypewriterEffect CreateEffect()
        {
            // 0.2 ms rounds to zero: these callback regressions complete synchronously.
            return new TypewriterEffect(5000f) { UsePunctuationPauses = false };
        }

        private TypewriterEffectComponent CreateComponent(string text)
        {
            _root = new GameObject("Typewriter", typeof(RectTransform));
            _root.SetActive(false);
            TMP_Text target = _root.AddComponent<TextMeshProUGUI>();
            target.text = text;
            TypewriterEffectComponent component = _root.AddComponent<TypewriterEffectComponent>();
            component.TargetText = target;
            component.OnStart = new UnityEvent();
            component.OnComplete = new UnityEvent();
            Audit20261023NeoToolsComponentsAccess.Configure(component, "_autoStart", false);
            component.Effect.CharactersPerSecond = 5000f;
            component.Effect.UsePunctuationPauses = false;
            Audit20261023NeoToolsComponentsAccess.Invoke(component, "Awake");
            return component;
        }
    }
}
