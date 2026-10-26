using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Neo.HtmlToUitk.Animation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkRuntimeAnimationTests
    {
        private sealed class HostWindow : EditorWindow { }

        private static AnimSpec Spec(double iterations, AnimDirection direction, bool missingEnd = false)
        {
            List<AnimStep> steps = new List<AnimStep>
            {
                new AnimStep(0f, null, new[] { new KeyValuePair<string, string>("opacity", missingEnd ? "0.2" : "0") })
            };
            if (!missingEnd)
                steps.Add(new AnimStep(1f, null, new[] { new KeyValuePair<string, string>("opacity", "1") }));
            AnimDef definition = new AnimDef("fade", null, 10000, 0, iterations, direction, AnimFill.Forwards, "linear", steps);
            return new AnimSpec(new[] { definition }, Array.Empty<AnimBinding>());
        }

        [TestCase(0.25, AnimDirection.Normal, 0.25f)]
        [TestCase(0.25, AnimDirection.Reverse, 0.75f)]
        [TestCase(1.25, AnimDirection.Alternate, 0.75f)]
        [TestCase(1.25, AnimDirection.AlternateReverse, 0.25f)]
        public void ReducedMotionSkipUsesTheFractionalTerminalSample(double iterations, AnimDirection direction, float expected)
        {
            VisualElement element = new VisualElement();
            using (UitkAnimator animator = new UitkAnimator(element, Spec(iterations, direction)))
            {
                animator.ReducedMotion = true;
                Assert.That(animator.PlayManual("fade", element, ReducedMotionMode.Skip), Is.False);
                Assert.That(element.style.opacity.value, Is.EqualTo(expected).Within(0.0001f));
                Assert.That(animator.ActiveCount, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator StopJumpToEndUsesTheFractionalTerminalSample()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            try
            {
                window.Show();
                VisualElement element = window.rootVisualElement;
                yield return null;
                AnimDirection[] directions = { AnimDirection.Normal, AnimDirection.Reverse, AnimDirection.Alternate, AnimDirection.AlternateReverse };
                float[] expected = { 0.25f, 0.75f, 0.75f, 0.25f };
                for (int i = 0; i < directions.Length; i++)
                {
                    using (UitkAnimator animator = new UitkAnimator(element, Spec(i < 2 ? 0.25 : 1.25, directions[i])))
                    {
                        Assert.That(animator.PlayManual("fade", element, ReducedMotionMode.Off), Is.True);
                        Assert.That(animator.ActiveCount, Is.EqualTo(1));
                        animator.Stop(element, true);
                        Assert.That(element.style.opacity.value, Is.EqualTo(expected[i]).Within(0.0001f), directions[i].ToString());
                        Assert.That(animator.ActiveCount, Is.Zero);
                    }
                }
            }
            finally { window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator MissingEndpointReturnsToTheUnderlyingValueForStopAndReducedMotion()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkAnimator animator = null;
            try
            {
                window.Show();
                VisualElement element = window.rootVisualElement;
                element.style.opacity = 0.8f;
                yield return null;
                Assert.That(element.resolvedStyle.opacity, Is.EqualTo(0.8f).Within(0.0001f));
                animator = new UitkAnimator(element, Spec(1, AnimDirection.Normal, true));
                animator.PlayManual("fade", element, ReducedMotionMode.Off);
                yield return null;
                Assert.That(element.style.opacity.value, Is.LessThan(0.3f));
                animator.Stop(element, true);
                Assert.That(element.style.opacity.value, Is.EqualTo(0.8f).Within(0.0001f), "The animation must not use its own previous sample as the underlying value.");
                yield return null;
                animator.ReducedMotion = true;
                animator.PlayManual("fade", element, ReducedMotionMode.Skip);
                Assert.That(element.style.opacity.value, Is.EqualTo(0.8f).Within(0.0001f), "Skip must also resolve an omitted terminal keyframe.");
            }
            finally { animator?.Dispose(); window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator CancellationCallbacksCanStopOtherRunsAndDisposeAReplacement()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkAnimator animator = null;
            try
            {
                window.Show();
                VisualElement first = new VisualElement();
                VisualElement second = new VisualElement();
                first.style.opacity = 0.9f;
                second.style.opacity = 0.7f;
                window.rootVisualElement.Add(first);
                window.rootVisualElement.Add(second);
                yield return null;
                animator = new UitkAnimator(window.rootVisualElement, Spec(1, AnimDirection.Normal));
                animator.PlayManual("fade", first, ReducedMotionMode.Off);
                animator.PlayManual("fade", second, ReducedMotionMode.Off);
                Assert.That(animator.ActiveCount, Is.EqualTo(2));
                int cancellations = 0;
                Action<UitkAnimationEvent> stopOthers = evt => { cancellations++; animator.StopAll(false); };
                animator.Cancelled += stopOthers;
                animator.StopAll(false);
                Assert.That(cancellations, Is.EqualTo(2));
                Assert.That(animator.ActiveCount, Is.Zero);
                Assert.That(first.style.opacity.value, Is.EqualTo(0.9f));
                Assert.That(second.style.opacity.value, Is.EqualTo(0.7f));
                animator.Cancelled -= stopOthers;
                animator.PlayManual("fade", first, ReducedMotionMode.Off);
                animator.Cancelled += evt => animator.Dispose();
                Assert.That(animator.PlayManual("fade", first, ReducedMotionMode.Off), Is.False);
                Assert.That(animator.ActiveCount, Is.Zero);
                Assert.That(first.style.opacity.value, Is.EqualTo(0.9f));
            }
            finally { animator?.Dispose(); window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator CancellationRestartSupersedesThePendingSameKeyReplacement()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkAnimator animator = null;
            try
            {
                window.Show();
                VisualElement element = new VisualElement { name = "target" };
                element.style.opacity = 0.9f;
                window.rootVisualElement.Add(element);
                yield return null;
                Assert.That(element.panel, Is.Not.Null);
                animator = new UitkAnimator(window.rootVisualElement, Spec(1, AnimDirection.Normal));
                List<string> events = new List<string>();
                animator.Started += evt => events.Add("started:" + evt.AnimationId);
                animator.Cancelled += evt => events.Add("cancelled:" + evt.AnimationId);
                animator.Finished += evt => events.Add("finished:" + evt.AnimationId);
                Assert.That(animator.PlayManual("fade", element, ReducedMotionMode.Off), Is.True);

                bool restarted = false;
                Action<UitkAnimationEvent> restart = null;
                restart = evt =>
                {
                    animator.Cancelled -= restart;
                    restarted = animator.PlayManual("fade", element, ReducedMotionMode.Off);
                };
                animator.Cancelled += restart;
                Assert.That(animator.PlayManual("fade", element, ReducedMotionMode.Off), Is.False,
                    "The pending replacement must yield to the newer same-key callback request.");
                Assert.That(restarted, Is.True);
                Assert.That(animator.ActiveCount, Is.EqualTo(1));
                Assert.That(events, Is.EqualTo(new[] { "started:fade", "cancelled:fade", "started:fade" }));

                animator.Stop(element);
                animator.Stop(element);
                Assert.That(animator.ActiveCount, Is.Zero);
                Assert.That(element.style.opacity.value, Is.EqualTo(0.9f));
                Assert.That(events, Is.EqualTo(new[] { "started:fade", "cancelled:fade", "started:fade", "cancelled:fade" }));
                yield return null;
                Assert.That(element.style.opacity.value, Is.EqualTo(0.9f));
                Assert.That(events.Count, Is.EqualTo(4), "Stopped work must not publish a late completion.");
            }
            finally { animator?.Dispose(); window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator CancellationStartsForOtherKeysDoNotSupersedeThePendingReplacement()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkAnimator animator = null;
            try
            {
                window.Show();
                VisualElement first = new VisualElement { name = "first" };
                VisualElement second = new VisualElement { name = "second" };
                first.style.opacity = 0.9f;
                second.style.opacity = 0.7f;
                first.style.scale = new Scale(new Vector3(1.2f, 0.8f, 1f));
                StyleScale authoredScale = first.style.scale;
                window.rootVisualElement.Add(first);
                window.rootVisualElement.Add(second);
                yield return null;
                Assert.That(first.panel, Is.Not.Null);
                Assert.That(second.panel, Is.Not.Null);
                AnimSpec fade = Spec(1, AnimDirection.Normal);
                AnimStep[] scaleSteps =
                {
                    new AnimStep(0f, null, new[] { new KeyValuePair<string, string>("scale", "0.5") }),
                    new AnimStep(1f, null, new[] { new KeyValuePair<string, string>("scale", "1") })
                };
                AnimDef scale = new AnimDef("scale", null, 10000, 0, 1, AnimDirection.Normal, AnimFill.Forwards, "linear", scaleSteps);
                animator = new UitkAnimator(window.rootVisualElement,
                    new AnimSpec(new[] { fade.Animations[0], scale }, Array.Empty<AnimBinding>()));
                List<string> events = new List<string>();
                animator.Started += evt => events.Add("started:" + evt.Element.name + ":" + evt.AnimationId);
                animator.Cancelled += evt => events.Add("cancelled:" + evt.Element.name + ":" + evt.AnimationId);
                Assert.That(animator.PlayManual("fade", first, ReducedMotionMode.Off), Is.True);
                bool startedOtherId = false;
                bool startedOtherElement = false;
                Action<UitkAnimationEvent> startOthers = null;
                startOthers = evt =>
                {
                    animator.Cancelled -= startOthers;
                    startedOtherId = animator.PlayManual("scale", first, ReducedMotionMode.Off);
                    startedOtherElement = animator.PlayManual("fade", second, ReducedMotionMode.Off);
                };
                animator.Cancelled += startOthers;
                Assert.That(animator.PlayManual("fade", first, ReducedMotionMode.Off), Is.True);
                Assert.That(startedOtherId, Is.True);
                Assert.That(startedOtherElement, Is.True);
                Assert.That(animator.ActiveCount, Is.EqualTo(3));
                Assert.That(events, Is.EqualTo(new[]
                {
                    "started:first:fade", "cancelled:first:fade", "started:first:scale",
                    "started:second:fade", "started:first:fade"
                }));

                animator.Stop(first);
                Assert.That(animator.ActiveCount, Is.EqualTo(1));
                Assert.That(first.style.opacity.value, Is.EqualTo(0.9f));
                Assert.That(first.style.scale, Is.EqualTo(authoredScale));
                animator.Stop(second);
                Assert.That(animator.ActiveCount, Is.Zero);
                Assert.That(second.style.opacity.value, Is.EqualTo(0.7f));
                Assert.That(events, Is.EqualTo(new[]
                {
                    "started:first:fade", "cancelled:first:fade", "started:first:scale",
                    "started:second:fade", "started:first:fade", "cancelled:first:fade",
                    "cancelled:first:scale", "cancelled:second:fade"
                }));
            }
            finally { animator?.Dispose(); window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }

        [UnityTest]
        public IEnumerator StopAllDoesNotRevisitAReplacementStartedByItsCancellationCallback()
        {
            HostWindow window = ScriptableObject.CreateInstance<HostWindow>();
            UitkAnimator animator = null;
            try
            {
                window.Show();
                VisualElement element = new VisualElement { name = "target" };
                element.style.opacity = 0.9f;
                window.rootVisualElement.Add(element);
                yield return null;
                Assert.That(element.panel, Is.Not.Null);
                animator = new UitkAnimator(window.rootVisualElement, Spec(1, AnimDirection.Normal));
                List<string> events = new List<string>();
                animator.Started += evt => events.Add("started:" + evt.AnimationId);
                animator.Cancelled += evt => events.Add("cancelled:" + evt.AnimationId);
                Assert.That(animator.PlayManual("fade", element, ReducedMotionMode.Off), Is.True);
                bool restarted = false;
                Action<UitkAnimationEvent> restart = null;
                restart = evt =>
                {
                    animator.Cancelled -= restart;
                    restarted = animator.PlayManual("fade", element, ReducedMotionMode.Off);
                };
                animator.Cancelled += restart;
                animator.StopAll(false);
                Assert.That(restarted, Is.True);
                Assert.That(animator.ActiveCount, Is.EqualTo(1));
                Assert.That(events, Is.EqualTo(new[] { "started:fade", "cancelled:fade", "started:fade" }));
                animator.StopAll(false);
                Assert.That(animator.ActiveCount, Is.Zero);
                Assert.That(element.style.opacity.value, Is.EqualTo(0.9f));
                Assert.That(events, Is.EqualTo(new[] { "started:fade", "cancelled:fade", "started:fade", "cancelled:fade" }));
            }
            finally { animator?.Dispose(); window.Close(); UnityEngine.Object.DestroyImmediate(window); }
        }
    }
}
