using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsViewImageFillTests
    {
        private const float NativeDeadlineSeconds = 5f;
        private readonly List<GameObject> _objects = new();
        private readonly List<Tween> _tweens = new();
        private readonly List<string> _targetWarnings = new();

        private enum TargetChange
        {
            DestroyImage,
            DestroyExternalObject,
            ReassignLiveImage
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            int warningCount = 0;
            string warnings = string.Empty;
            try
            {
                foreach (Tween tween in _tweens)
                {
                    if (tween.IsActive())
                    {
                        tween.Kill();
                    }
                }

                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }

                yield return null;
                warningCount = _targetWarnings.Count;
                warnings = string.Join("\n", _targetWarnings);
            }
            finally
            {
                Application.logMessageReceived -= CaptureTargetWarning;
                _objects.Clear();
                _tweens.Clear();
                _targetWarnings.Clear();
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            Assert.That(warningCount, Is.Zero, warnings);
        }

        [UnityTest]
        public IEnumerator FillAmount_DestroyOnlyImage_RetiresOwnTweenAndUsesReplacement()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.FillAmount, TargetChange.DestroyImage);
        }

        [UnityTest]
        public IEnumerator SlicedWidth_DestroyOnlyImage_RetiresOwnTweenAndUsesReplacement()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.SlicedWidth, TargetChange.DestroyImage);
        }

        [UnityTest]
        public IEnumerator FillAmount_DestroyExternalTargetObject_LeavesOwnerAndUnrelatedTweenAlive()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.FillAmount,
                TargetChange.DestroyExternalObject);
        }

        [UnityTest]
        public IEnumerator SlicedWidth_DestroyExternalTargetObject_LeavesOwnerAndUnrelatedTweenAlive()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.SlicedWidth,
                TargetChange.DestroyExternalObject);
        }

        [UnityTest]
        public IEnumerator FillAmount_ReassignLiveImage_RetiresOldHandleWithoutStaleWrites()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.FillAmount, TargetChange.ReassignLiveImage);
        }

        [UnityTest]
        public IEnumerator SlicedWidth_ReassignLiveImage_RetiresOldHandleWithoutStaleWrites()
        {
            yield return new EnterPlayMode();
            yield return ExerciseTargetChange(ImageFillAmountAnimator.FillMode.SlicedWidth, TargetChange.ReassignLiveImage);
        }

        [UnityTest]
        public IEnumerator SlicedWidth_DirtyCallbackImmediateZero_WinsOverRetiredTweenVisibility()
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            Application.logMessageReceived += CaptureTargetWarning;
            Image image = CreateImage(CreateTrack("DirtyCallbackTrack"), "DirtyCallbackFill", 0.1f, 0.9f);
            ImageFillAmountAnimator owner = AddAnimator(image.gameObject, image,
                ImageFillAmountAnimator.FillMode.SlicedWidth);
            owner.SetValueImmediate(0.1f);
            Tween retired = StartTween(owner, 0.9f);
            int killCount = 0;
            int completionCount = 0;
            retired.OnKill(() => killCount++);
            retired.OnComplete(() => completionCount++);

            Image unrelated = CreateImage(CreateTrack("DirtyCallbackControl"), "UnrelatedFill", 0f, 1f);
            ImageFillAmountAnimator unrelatedOwner = AddAnimator(unrelated.gameObject, unrelated,
                ImageFillAmountAnimator.FillMode.FillAmount);
            unrelatedOwner.SetValueImmediate(0.1f);
            Tween unrelatedTween = StartTween(unrelatedOwner, 0.9f);
            yield return WaitForProgress(owner, image, retired, ImageFillAmountAnimator.FillMode.SlicedWidth,
                0.1f, 0.9f, 0.1f, 0.9f);
            yield return WaitForProgress(unrelatedOwner, unrelated, unrelatedTween,
                ImageFillAmountAnimator.FillMode.FillAmount, 0.1f, 0.9f, 0f, 1f);

            float armedAnchor = image.rectTransform.anchorMax.x;
            int callbackCount = 0;
            bool callbackSawOwnedTween = false;
            bool retiredInsideCallback = false;
            bool ownerClearedInsideCallback = false;
            float anchorInsideCallback = float.NaN;
            float displayedInsideCallback = float.NaN;
            bool visibleInsideCallback = true;
            UnityAction dirtyCallback = null;
            dirtyCallback = () =>
            {
                // Unsubscribe before the immediate write dirties the same rect again.
                image.UnregisterDirtyVerticesCallback(dirtyCallback);
                callbackCount++;
                callbackSawOwnedTween = ReferenceEquals(ReadTween(owner), retired) && retired.IsActive() &&
                    owner.DisplayedValue > 0f && image.rectTransform.anchorMax.x > armedAnchor;
                owner.SetValueImmediate(0f);
                retiredInsideCallback = !retired.IsActive();
                ownerClearedInsideCallback = ReadTween(owner) == null;
                anchorInsideCallback = image.rectTransform.anchorMax.x;
                displayedInsideCallback = owner.DisplayedValue;
                visibleInsideCallback = image.enabled;
            };

            try
            {
                float unrelatedBefore = unrelated.fillAmount;
                int armedAtFrame = Time.frameCount;
                double deadline = Time.realtimeSinceStartupAsDouble + NativeDeadlineSeconds;
                image.RegisterDirtyVerticesCallback(dirtyCallback);
                do
                {
                    yield return null;
                    Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                        "The native SlicedWidth rect update must invoke the registered public callback.");
                } while (callbackCount == 0 || Time.frameCount <= armedAtFrame);

                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(callbackSawOwnedTween, Is.True, "Arm only after real unfinished tween progress.");
                Assert.That(retiredInsideCallback, Is.True);
                Assert.That(ownerClearedInsideCallback, Is.True);
                Assert.That(anchorInsideCallback, Is.EqualTo(0.1f));
                Assert.That(displayedInsideCallback, Is.Zero);
                Assert.That(visibleInsideCallback, Is.False);

                int retiredAtFrame = Time.frameCount;
                deadline = Time.realtimeSinceStartupAsDouble + NativeDeadlineSeconds;
                // Killing from inside an SDK update retires immediately; OnKill follows native despawn.
                do
                {
                    Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.1f));
                    Assert.That(image.enabled, Is.False);
                    Assert.That(owner.DisplayedValue, Is.Zero);
                    Assert.That(owner.isActiveAndEnabled, Is.True);
                    Assert.That(retired.IsActive(), Is.False);
                    Assert.That(ReadTween(owner), Is.Null);
                    Assert.That(completionCount, Is.Zero);
                    Assert.That(callbackCount, Is.EqualTo(1));
                    Assert.That(unrelatedOwner.isActiveAndEnabled, Is.True);
                    Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
                    Assert.That(unrelatedTween.IsActive() && unrelatedTween.IsPlaying(), Is.True);
                    AssertNoTargetWarnings();
                    yield return null;
                    Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                        "Native SDK cleanup and stability share the original deadline.");
                } while (killCount == 0 || Time.frameCount <= retiredAtFrame);

                Assert.That(killCount, Is.EqualTo(1));
                int settledAtFrame = Time.frameCount;
                do
                {
                    Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.1f));
                    Assert.That(image.enabled, Is.False,
                        "The old positive setter tail must not re-enable the immediate zero image.");
                    Assert.That(owner.DisplayedValue, Is.Zero);
                    Assert.That(owner.isActiveAndEnabled, Is.True);
                    Assert.That(retired.IsActive(), Is.False);
                    Assert.That(ReadTween(owner), Is.Null);
                    Assert.That(killCount, Is.EqualTo(1));
                    Assert.That(completionCount, Is.Zero);
                    Assert.That(callbackCount, Is.EqualTo(1));
                    Assert.That(unrelatedOwner.isActiveAndEnabled, Is.True);
                    Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
                    Assert.That(unrelatedTween.IsActive() && unrelatedTween.IsPlaying(), Is.True);
                    Assert.That(unrelated.fillAmount, Is.GreaterThan(unrelatedBefore));
                    AssertNoTargetWarnings();
                    if (Time.frameCount >= settledAtFrame + 3)
                    {
                        break;
                    }

                    yield return null;
                    Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Native stability frames.");
                } while (true);

                Tween fresh = StartTween(owner, 0.8f);
                Assert.That(fresh, Is.Not.SameAs(retired));
                yield return WaitForProgress(owner, image, fresh, ImageFillAmountAnimator.FillMode.SlicedWidth,
                    0f, 0.8f, 0.1f, 0.9f);
                Assert.That(image.enabled, Is.True, "A later valid positive value must make the image visible again.");
                Assert.That(ReadTween(owner), Is.SameAs(fresh));
                Assert.That(killCount, Is.EqualTo(1));
                Assert.That(completionCount, Is.Zero);
                Assert.That(callbackCount, Is.EqualTo(1));
                Assert.That(unrelatedTween.IsActive() && unrelatedTween.IsPlaying(), Is.True);
                Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
                AssertNoTargetWarnings();
            }
            finally
            {
                if (image != null)
                {
                    image.UnregisterDirtyVerticesCallback(dirtyCallback);
                }
            }
        }

        private IEnumerator ExerciseTargetChange(ImageFillAmountAnimator.FillMode mode, TargetChange change)
        {
            Assert.That(Application.isPlaying, Is.True, "Target lifetime requires native Play Mode dispatch.");
            Application.logMessageReceived += CaptureTargetWarning;
            RectTransform track = CreateTrack("Track");
            Image original = CreateImage(track, "Original", 0.1f, 0.9f);
            GameObject originalObject = original.gameObject;
            GameObject ownerObject = change == TargetChange.DestroyExternalObject
                ? CreateObject("ExternalOwner")
                : originalObject;
            ImageFillAmountAnimator owner = AddAnimator(ownerObject, original, mode);
            owner.SetValueImmediate(0.1f);
            Tween retired = StartTween(owner, 0.9f);
            int killCount = 0;
            int completionCount = 0;
            retired.OnKill(() => killCount++);
            retired.OnComplete(() => completionCount++);

            Image unrelated = CreateImage(CreateTrack("UnrelatedTrack"), "Unrelated", 0f, 1f);
            ImageFillAmountAnimator unrelatedOwner = AddAnimator(unrelated.gameObject, unrelated,
                ImageFillAmountAnimator.FillMode.FillAmount);
            unrelatedOwner.SetValueImmediate(0.1f);
            Tween unrelatedTween = StartTween(unrelatedOwner, 0.9f);
            yield return WaitForProgress(owner, original, retired, mode, 0.1f, 0.9f, 0.1f, 0.9f);
            yield return WaitForProgress(unrelatedOwner, unrelated, unrelatedTween,
                ImageFillAmountAnimator.FillMode.FillAmount, 0.1f, 0.9f, 0f, 1f);

            Assert.That(owner.isActiveAndEnabled, Is.True);
            float oldNativeValue = ReadNativeValue(original, mode, 0.1f, 0.9f);
            float unrelatedBefore = unrelated.fillAmount;
            Image replacement = null;
            float untouchedReplacement = 0f;
            if (change == TargetChange.ReassignLiveImage)
            {
                replacement = CreateImage(track, "Replacement", 0.3f, 0.8f);
                replacement.fillAmount = 0.35f;
                untouchedReplacement = ReadNativeValue(replacement, mode, 0.3f, 0.8f);
                SetField(owner, "_image", replacement);
            }
            else if (change == TargetChange.DestroyExternalObject)
            {
                Object.Destroy(originalObject);
            }
            else
            {
                // Destroy just the Image: the owner receives neither OnDisable nor OnDestroy.
                Object.Destroy(original);
            }

            int changedAtFrame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + NativeDeadlineSeconds;
            do
            {
                yield return null;
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "The captured tween must retire on a native update, not by reaching its endpoint.");
            } while (retired.IsActive() || Time.frameCount <= changedAtFrame);

            int retiredAtFrame = Time.frameCount;
            do
            {
                Assert.That(retired.IsActive(), Is.False);
                Assert.That(completionCount, Is.Zero);
                Assert.That(ReadTween(owner), Is.Null);
                Assert.That(owner != null && owner.isActiveAndEnabled, Is.True);
                Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
                Assert.That(unrelatedTween.IsActive() && unrelatedTween.IsPlaying(), Is.True);
                AssertNoTargetWarnings();
                yield return null;
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "Retirement and deferred native OnKill share the original deadline.");
            } while (killCount == 0 || Time.frameCount <= retiredAtFrame);

            Assert.That(Time.frameCount, Is.GreaterThan(retiredAtFrame));
            Assert.That(killCount, Is.EqualTo(1), "Only the captured owned handle must be killed, exactly once.");
            Assert.That(completionCount, Is.Zero, "An unavailable target must retire without forced completion.");
            Assert.That(ReadTween(owner), Is.Null);
            Assert.That(owner != null && owner.isActiveAndEnabled, Is.True);
            Assert.That(owner.gameObject, Is.SameAs(ownerObject));
            Assert.That(unrelatedOwner.isActiveAndEnabled, Is.True);
            Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
            Assert.That(unrelatedTween.IsActive() && unrelatedTween.IsPlaying(), Is.True);
            Assert.That(unrelated.fillAmount, Is.GreaterThan(unrelatedBefore),
                "Retiring one target must leave the unrelated native tween advancing.");
            AssertNoTargetWarnings();

            if (change == TargetChange.ReassignLiveImage)
            {
                Assert.That(original != null && originalObject.activeInHierarchy, Is.True);
                Assert.That(ReadNativeValue(original, mode, 0.1f, 0.9f), Is.EqualTo(oldNativeValue),
                    "The old captured Image must receive no write after reassignment.");
                Assert.That(ReadNativeValue(replacement, mode, 0.3f, 0.8f), Is.EqualTo(untouchedReplacement),
                    "The retired setter must not redirect its value to the replacement.");
            }
            else
            {
                Assert.That(original == null, Is.True);
                if (change == TargetChange.DestroyImage)
                {
                    Assert.That(originalObject != null && originalObject.activeInHierarchy, Is.True);
                    Assert.That(originalObject.GetComponent<Image>(), Is.Null);
                    replacement = originalObject.AddComponent<Image>();
                    ConfigureRect(replacement, 0.3f, 0.8f);
                    // Leave the destroyed serialized reference in place: the local GetComponent fallback must rebind.
                }
                else
                {
                    Assert.That(originalObject == null, Is.True);
                    replacement = CreateImage(track, "Replacement", 0.3f, 0.8f);
                    SetField(owner, "_image", replacement);
                }
            }

            replacement.type = mode == ImageFillAmountAnimator.FillMode.FillAmount ? Image.Type.Filled : Image.Type.Sliced;
            owner.SetValueImmediate(0.2f);
            Assert.That(ReadNativeValue(replacement, mode, 0.3f, 0.8f), Is.EqualTo(0.2f).Within(0.0001f));
            Tween fresh = StartTween(owner, 0.8f);
            Assert.That(fresh, Is.Not.SameAs(retired), "Owned handles remain non-recyclable.");
            yield return WaitForProgress(owner, replacement, fresh, mode, 0.2f, 0.8f, 0.3f, 0.8f);
            Assert.That(ReadTween(owner), Is.SameAs(fresh));
            Assert.That(killCount, Is.EqualTo(1), "A retired callback cannot kill the later owned tween.");
            if (change == TargetChange.ReassignLiveImage)
            {
                Assert.That(ReadNativeValue(original, mode, 0.1f, 0.9f), Is.EqualTo(oldNativeValue));
            }

            owner.enabled = false;
            Assert.That(fresh.IsActive(), Is.False);
            Assert.That(ReadTween(owner), Is.Null);
            Assert.That(ReadNativeValue(replacement, mode, 0.3f, 0.8f), Is.EqualTo(0.8f).Within(0.0001f),
                "Disabling still snaps the valid bound target to the last request.");
            AssertDisabledModeControls(owner, replacement);
            yield return null;
            Assert.That(replacement.fillAmount, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(replacement.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(unrelatedTween.IsActive(), Is.True);
            Assert.That(ReadTween(unrelatedOwner), Is.SameAs(unrelatedTween));
            Assert.That(killCount, Is.EqualTo(1));
            if (change == TargetChange.ReassignLiveImage)
            {
                Assert.That(ReadNativeValue(original, mode, 0.1f, 0.9f), Is.EqualTo(oldNativeValue),
                    "Later mode restoration must leave the previously assigned Image untouched.");
            }

            AssertNoTargetWarnings();
        }

        private static void AssertDisabledModeControls(ImageFillAmountAnimator owner, Image image)
        {
            image.type = Image.Type.Sliced;
            owner.Mode = ImageFillAmountAnimator.FillMode.SlicedWidth;
            owner.SetValueImmediate(0f);
            Assert.That(image.enabled, Is.False);
            Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.3f).Within(0.0001f));

            owner.Mode = ImageFillAmountAnimator.FillMode.FillAmount;
            image.type = Image.Type.Filled;
            Assert.That(image.enabled, Is.True);
            Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.0001f),
                "Mode restoration must use the replacement's authored full anchor.");
            owner.SetBool01(0.49f);
            Assert.That(image.fillAmount, Is.Zero);
            owner.SetBool(true);
            Assert.That(image.fillAmount, Is.EqualTo(1f));
            owner.SetValue(0.4f);
            Assert.That(ReadTween(owner), Is.Null, "A disabled owner still applies new input immediately.");
            Assert.That(image.fillAmount, Is.EqualTo(0.4f).Within(0.0001f));

            image.type = Image.Type.Sliced;
            owner.Mode = ImageFillAmountAnimator.FillMode.SlicedWidth;
            owner.SetValueImmediate(0.5f);
            Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(image.fillAmount, Is.EqualTo(0.4f).Within(0.0001f), "SlicedWidth must not cut the fill.");
            owner.Mode = ImageFillAmountAnimator.FillMode.FillAmount;
            Assert.That(image.rectTransform.anchorMin.x, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(image.rectTransform.anchorMax.x, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(image.rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(image.rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(image.enabled, Is.True);
        }

        private Tween StartTween(ImageFillAmountAnimator owner, float target)
        {
            owner.SetValue(target);
            Tween tween = ReadTween(owner);
            Assert.That(tween, Is.Not.Null);
            Assert.That(tween.IsActive() && tween.IsPlaying(), Is.True);
            _tweens.Add(tween);
            return tween;
        }

        private static IEnumerator WaitForProgress(ImageFillAmountAnimator owner, Image image, Tween tween,
            ImageFillAmountAnimator.FillMode mode, float start, float target, float anchorMin, float anchorMax)
        {
            int initialFrame = Time.frameCount;
            double deadline = Time.realtimeSinceStartupAsDouble + NativeDeadlineSeconds;
            float displayed;
            do
            {
                yield return null;
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Native progress deadline.");
                Assert.That(image != null && owner.isActiveAndEnabled, Is.True);
                Assert.That(tween.IsActive() && tween.IsPlaying(), Is.True);
                Assert.That(ReadTween(owner), Is.SameAs(tween));
                displayed = ReadNativeValue(image, mode, anchorMin, anchorMax);
            } while (Time.frameCount <= initialFrame || displayed <= start);

            Assert.That(displayed, Is.GreaterThan(start));
            Assert.That(displayed, Is.LessThan(target), "Interrupt a genuinely advancing unfinished tween.");
            Assert.That(owner.DisplayedValue, Is.EqualTo(displayed).Within(0.0001f));
        }

        private ImageFillAmountAnimator AddAnimator(GameObject host, Image image, ImageFillAmountAnimator.FillMode mode)
        {
            ImageFillAmountAnimator owner = host.AddComponent<ImageFillAmountAnimator>();
            SetField(owner, "_image", image);
            SetField(owner, "_duration", 10f);
            SetField(owner, "_ease", Ease.Linear);
            SetField(owner, "_ignoreTimeScale", true);
            image.type = mode == ImageFillAmountAnimator.FillMode.FillAmount ? Image.Type.Filled : Image.Type.Sliced;
            owner.Mode = mode;
            return owner;
        }

        private RectTransform CreateTrack(string name)
        {
            RectTransform track = (RectTransform)CreateObject(name, typeof(RectTransform)).transform;
            track.sizeDelta = new Vector2(1000f, 40f);
            return track;
        }

        private Image CreateImage(RectTransform parent, string name, float anchorMin, float anchorMax)
        {
            GameObject host = CreateObject(name, typeof(RectTransform), typeof(Image));
            host.transform.SetParent(parent, false);
            Image image = host.GetComponent<Image>();
            ConfigureRect(image, anchorMin, anchorMax);
            return image;
        }

        private static void ConfigureRect(Image image, float anchorMin, float anchorMax)
        {
            image.rectTransform.anchorMin = new Vector2(anchorMin, 0f);
            image.rectTransform.anchorMax = new Vector2(anchorMax, 1f);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            GameObject host = new("Audit20261023NeoToolsViewImageFill." + name, components);
            _objects.Add(host);
            return host;
        }

        private static float ReadNativeValue(Image image, ImageFillAmountAnimator.FillMode mode,
            float anchorMin, float anchorMax)
        {
            return mode == ImageFillAmountAnimator.FillMode.FillAmount
                ? image.fillAmount
                : Mathf.InverseLerp(anchorMin, anchorMax, image.rectTransform.anchorMax.x);
        }

        private static Tween ReadTween(ImageFillAmountAnimator owner)
        {
            return (Tween)typeof(ImageFillAmountAnimator).GetField("_anim",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        }

        private static void SetField(ImageFillAmountAnimator owner, string name, object value)
        {
            FieldInfo field = typeof(ImageFillAmountAnimator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(owner, value);
        }

        private void CaptureTargetWarning(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            bool destroyed = condition.IndexOf("MissingReferenceException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                condition.IndexOf("has been destroyed", StringComparison.OrdinalIgnoreCase) >= 0;
            bool dotween = condition.IndexOf("DOTWEEN", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (condition.IndexOf("missing/null", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 condition.IndexOf("missing target", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 condition.IndexOf("SAFE MODE", StringComparison.OrdinalIgnoreCase) >= 0);
            if (destroyed || dotween)
            {
                _targetWarnings.Add(condition + "\n" + stackTrace);
            }
        }

        private void AssertNoTargetWarnings()
        {
            Assert.That(_targetWarnings, Is.Empty, string.Join("\n", _targetWarnings));
        }
    }
}
