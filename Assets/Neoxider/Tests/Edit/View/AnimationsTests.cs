using Neo.Animations;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class AnimationsTests
    {
        private GameObject _testObj;
        private FloatAnimator _animator;

        [SetUp]
        public void SetUp()
        {
            _testObj = new GameObject("TestAnimator");
            _animator = _testObj.AddComponent<FloatAnimator>();
            // Disable start play to test manual triggering
            _animator.playOnStart = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObj != null)
            {
                Object.DestroyImmediate(_testObj);
            }
        }

        [Test]
        public void Play_SetsIsPlayingTrue()
        {
            _animator.Play();
            Assert.IsTrue(_animator.IsPlaying);
            Assert.IsFalse(_animator.IsPaused);
        }

        [Test]
        public void Stop_SetsIsPlayingFalse()
        {
            _animator.Play();
            _animator.Stop();
            Assert.IsFalse(_animator.IsPlaying);
        }

        [Test]
        public void Pause_SetsIsPausedTrue()
        {
            _animator.Play();
            _animator.Pause();
            Assert.IsTrue(_animator.IsPaused);
            Assert.IsTrue(_animator.IsPlaying); // Still "playing" but paused
        }

        [Test]
        public void Resume_RestoresPlayingState()
        {
            _animator.Play();
            _animator.Pause();
            _animator.Resume();
            Assert.IsFalse(_animator.IsPaused);
        }

        [Test]
        public void AnimationType_SetterGetsSet()
        {
            _animator.AnimationType = AnimationType.SinWave;
            Assert.AreEqual(AnimationType.SinWave, _animator.AnimationType);
        }
    }
}
