using System.Collections;
using System.Reflection;
using DG.Tweening;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class ImageFillAmountAnimatorTests
    {
        private GameObject _go;
        private Tween _testTween;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Plain EditMode fixtures do not dispatch this runtime component's lifecycle.
            // Also release a tween when an assertion interrupted a lifecycle test.
            if (_testTween != null)
            {
                _testTween.Kill();
                _testTween = null;
            }

            if (_go != null)
            {
                Object.DestroyImmediate(_go);
                _go = null;
            }

            if (Application.isPlaying)
                yield return new ExitPlayMode();
        }

        [Test]
        public void SetValue_WithoutImage_DoesNotThrow()
        {
            _go = new GameObject("FillNoImage");
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();
            Assert.DoesNotThrow(() => animator.SetValue(0.5f));
            Assert.DoesNotThrow(() => animator.SetValueImmediate(0.5f));
        }

        [Test]
        public void SetValue_OnInactiveObject_AppliesImmediatelyWithoutTween()
        {
            _go = new GameObject("FillInactive");
            Image image = _go.AddComponent<Image>();
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();
            _go.SetActive(false);

            animator.SetValue(0.75f);

            Assert.AreEqual(0.75f, image.fillAmount, 0.0001f);
            Assert.IsNull(ReadTween(animator), "Inactive SetValue must not leave an orphan tween.");
        }

        [Test]
        public void SetValueImmediate_ClampsNonFiniteInputDeterministically()
        {
            _go = new GameObject("FillClamp");
            Image image = _go.AddComponent<Image>();
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();

            animator.SetValueImmediate(float.NaN);
            Assert.AreEqual(0f, image.fillAmount, 0.0001f);
            animator.SetValueImmediate(float.PositiveInfinity);
            Assert.AreEqual(0f, image.fillAmount, 0.0001f);
            animator.SetValueImmediate(float.NegativeInfinity);
            Assert.AreEqual(0f, image.fillAmount, 0.0001f);
            animator.SetValueImmediate(2f);
            Assert.AreEqual(1f, image.fillAmount, 0.0001f);
            animator.SetValueImmediate(-1f);
            Assert.AreEqual(0f, image.fillAmount, 0.0001f);
        }

        [Test]
        public void SetValueImmediate_RespectsInversion()
        {
            _go = new GameObject("FillInvert");
            Image image = _go.AddComponent<Image>();
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();
            SetInvert(animator, true);

            animator.SetValueImmediate(0.25f);

            Assert.AreEqual(0.75f, image.fillAmount, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Disable_DuringTween_StopsFurtherImageWrites()
        {
            // These are engine lifecycle assertions, not direct calls to private callbacks.
            yield return new EnterPlayMode();
            AssertLifecycleStopsTween(false);
        }

        [UnityTest]
        public IEnumerator Destroy_DuringTween_StopsFurtherImageWrites()
        {
            yield return new EnterPlayMode();
            AssertLifecycleStopsTween(true);
        }

        private void AssertLifecycleStopsTween(bool destroyComponent)
        {
            Assert.IsTrue(Application.isPlaying, "Lifecycle checks require real Play Mode dispatch.");
            _go = new GameObject("FillCleanup");
            Image image = _go.AddComponent<Image>();
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();
            animator.SetValueImmediate(0f);
            animator.SetValue(1f);
            Tween tween = ReadTween(animator) as Tween;
            _testTween = tween;
            Assert.IsNotNull(tween, "An enabled animator must create a real tween.");
            tween.SetUpdate(UpdateType.Manual);
            DOTween.ManualUpdate(0.1f, 0.1f);
            Assert.Greater(image.fillAmount, 0f, "The tween must have started before lifecycle cleanup.");
            Assert.Less(image.fillAmount, 1f, "The test must interrupt an unfinished tween.");
            float interruptedFill = image.fillAmount;

            if (destroyComponent)
                Object.DestroyImmediate(animator);
            else
                animator.enabled = false;

            Assert.IsFalse(tween.IsActive(), "Lifecycle cleanup must kill the owned tween.");
            if (!destroyComponent)
                Assert.IsNull(ReadTween(animator), "Disable must also clear the stored tween reference.");
            DOTween.ManualUpdate(1f, 1f);
            Assert.AreEqual(interruptedFill, image.fillAmount, 0.0001f,
                "The surviving Image must not be mutated after its animator is disabled or destroyed.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetValue_ConfiguresIndependentUpdateAsRequested(bool ignoreTimeScale)
        {
            _go = new GameObject("FillTimeScale");
            _go.AddComponent<Image>();
            ImageFillAmountAnimator animator = _go.AddComponent<ImageFillAmountAnimator>();
            FieldInfo setting = typeof(ImageFillAmountAnimator).GetField("_ignoreTimeScale",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(setting);
            setting.SetValue(animator, ignoreTimeScale);
            animator.SetValue(0.5f);

            Tween tween = ReadTween(animator) as Tween;
            _testTween = tween;
            Assert.IsNotNull(tween);
            FieldInfo independentUpdate = typeof(Tween).GetField("isIndependentUpdate",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(independentUpdate, "DOTween independent-update state must be inspectable.");
            Assert.AreEqual(ignoreTimeScale, independentUpdate.GetValue(tween));
        }

        private static void SetInvert(ImageFillAmountAnimator animator, bool value)
        {
            FieldInfo field = typeof(ImageFillAmountAnimator).GetField("_invertValue", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing _invertValue field.");
            field.SetValue(animator, value);
        }

        private static object ReadTween(ImageFillAmountAnimator animator)
        {
            FieldInfo field = typeof(ImageFillAmountAnimator).GetField("_anim", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Missing _anim field.");
            return field.GetValue(animator);
        }
    }
}
