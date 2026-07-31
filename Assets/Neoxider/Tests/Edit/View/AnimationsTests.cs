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
            // WHY: Disable start play to test manual triggering
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
            Assert.IsTrue(_animator.IsPlaying); // WHY: Still "playing" but paused
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

        [Test]
        public void GetAnimatedColor_PerlinNoise_RandomOffsetDesyncsInstances()
        {
            // WHY: regression - the noise-aware overload must feed randomOffset so multiple
            // animators do not flicker in lockstep (the plain overload always used zero offset).
            Color a = AnimationUtils.GetAnimatedColor(
                AnimationType.PerlinNoise, Color.black, Color.white,
                3.3f, 1.7f, false, new Vector2(0.4f, 0f), Vector2.zero, 1f);
            Color b = AnimationUtils.GetAnimatedColor(
                AnimationType.PerlinNoise, Color.black, Color.white,
                3.3f, 1.7f, false, new Vector2(123.6f, 0f), Vector2.zero, 1f);

            Assert.AreNotEqual(a.r, b.r, "Different randomOffset must yield different Perlin values.");
        }

        [Test]
        public void GetAnimatedVector3_PerlinNoise_NoiseScaleChangesResult()
        {
            // WHY: regression - noiseScale must reach the Perlin sampler through the overload.
            Vector3 low = AnimationUtils.GetAnimatedVector3(
                AnimationType.PerlinNoise, Vector3.zero, Vector3.one,
                3.3f, 1.7f, false, new Vector2(0.4f, 0f), Vector2.zero, 1f);
            Vector3 high = AnimationUtils.GetAnimatedVector3(
                AnimationType.PerlinNoise, Vector3.zero, Vector3.one,
                3.3f, 1.7f, false, new Vector2(0.4f, 0f), Vector2.zero, 3f);

            Assert.AreNotEqual(low.x, high.x, "Different noiseScale must change the sampled value.");
        }
    }
}
