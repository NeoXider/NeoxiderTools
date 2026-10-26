using System.Collections;
using Neo.Animations;
using Neo.Reactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.Animations
{
    public sealed class Audit20261023NeoAnimationsFloatPlayModeTests
    {
        private GameObject _host;
        private FloatAnimator _animator;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            _host = new GameObject(nameof(Audit20261023NeoAnimationsFloatPlayModeTests));
            _animator = _host.AddComponent<FloatAnimator>();
            _animator.playOnStart = false;
            _animator.AnimationSpeed = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Random.state = _randomState;
        }

        [UnityTest]
        public IEnumerator SmallValue_IsPublished_AndUnchangedFramesDoNotNotify()
        {
            const float target = 0.0005f;
            _animator.MinValue = target;
            _animator.MaxValue = target;
            float observed = -1f;
            int notifications = 0;
            _animator.Value.AddListener((float value) =>
            {
                observed = value;
                notifications++;
            });

            _animator.Play();
            yield return null;

            Assert.That(_animator.CurrentValue, Is.EqualTo(target));
            Assert.That(_animator.ValueFloat, Is.EqualTo(target));
            Assert.That(observed, Is.EqualTo(target));
            Assert.That(notifications, Is.EqualTo(1));

            yield return null;

            Assert.That(notifications, Is.EqualTo(1), "A constant value must not notify on every Update.");
        }

        [UnityTest]
        public IEnumerator ExternalValueWrite_IsCorrectedOnTheNextPlayingFrame()
        {
            _animator.MinValue = 4f;
            _animator.MaxValue = 4f;
            _animator.Play();
            yield return null;

            Assert.That(_animator.CurrentValue, Is.EqualTo(4f));
            _animator.Value.Value = 17f;

            yield return null;

            Assert.That(_animator.CurrentValue, Is.EqualTo(4f));
        }

        [UnityTest]
        public IEnumerator ReplacementReactiveProperty_ReceivesTheCurrentAnimationValue()
        {
            _animator.MinValue = 4f;
            _animator.MaxValue = 4f;
            _animator.Play();
            yield return null;

            Assert.That(_animator.CurrentValue, Is.EqualTo(4f));
            ReactivePropertyFloat replacement = new ReactivePropertyFloat(17f);
            float observed = -1f;
            replacement.AddListener((float value) => observed = value);
            _animator.Value = replacement;

            yield return null;

            Assert.That(_animator.Value, Is.SameAs(replacement));
            Assert.That(_animator.CurrentValue, Is.EqualTo(4f));
            Assert.That(replacement.CurrentValue, Is.EqualTo(4f));
            Assert.That(observed, Is.EqualTo(4f));
        }
    }
}
