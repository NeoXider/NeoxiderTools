using System;
using System.Collections.Generic;
using NUnit.Framework;
using Neo.HtmlToUitk.Animation;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class AnimationTests
    {
        private const string SampleJson = @"{
  ""schema"": 1,
  ""animations"": [
    { ""id"": ""fade-in-up"", ""keyframes"": ""fade-in-up"", ""duration"": 0.4, ""delay"": 0, ""iterations"": 1, ""direction"": ""normal"", ""fill"": ""forwards"", ""easing"": ""linear"",
      ""steps"": [
        { ""offset"": 0, ""set"": { ""opacity"": ""0"", ""translate"": ""0 12px"" } },
        { ""offset"": 1, ""set"": { ""opacity"": ""1"", ""translate"": ""0 0"" } } ] },
    { ""id"": ""pulse"", ""duration"": 1, ""iterations"": ""infinite"", ""direction"": ""alternate"", ""easing"": ""linear"",
      ""steps"": [
        { ""offset"": 0, ""set"": { ""scale"": ""1"" } },
        { ""offset"": 0.5, ""set"": { ""scale"": ""1.2"" } },
        { ""offset"": 1, ""set"": { ""scale"": ""1"" } } ] }
  ],
  ""bindings"": [
    { ""selector"": "".lg__card"", ""animation"": ""fade-in-up"", ""trigger"": ""onShow"", ""stagger"": 0.05, ""reducedMotion"": ""skip"" }
  ]
}";

        [Test]
        public void ParsesSampleSpec()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            Assert.That(spec.Animations.Count, Is.EqualTo(2));
            AnimDef fade = spec.Find("fade-in-up");
            Assert.That(fade.Duration, Is.EqualTo(0.4).Within(1e-9));
            Assert.That(fade.Fill, Is.EqualTo(AnimFill.Forwards));
            Assert.That(spec.Find("pulse").IsInfinite, Is.True);
            Assert.That(spec.Find("pulse").Direction, Is.EqualTo(AnimDirection.Alternate));
            Assert.That(spec.Bindings[0].Trigger, Is.EqualTo(AnimTrigger.OnShow));
            Assert.That(spec.Bindings[0].Stagger, Is.EqualTo(0.05));
            Assert.That(spec.Bindings[0].ReducedMotion, Is.EqualTo(ReducedMotionMode.Skip));
        }

        [Test]
        public void TimelineBeforeActiveAfter()
        {
            Assert.That(AnimTimeline.Evaluate(0.1, 1.0, 0.5, 1, AnimDirection.Normal).Phase, Is.EqualTo(AnimPhase.Before));
            TimelineSample mid = AnimTimeline.Evaluate(1.0, 1.0, 0.5, 1, AnimDirection.Normal);
            Assert.That(mid.Phase, Is.EqualTo(AnimPhase.Active));
            Assert.That(mid.Progress, Is.EqualTo(0.5).Within(1e-9));
            TimelineSample done = AnimTimeline.Evaluate(1.5, 1.0, 0.5, 1, AnimDirection.Normal);
            Assert.That(done.Phase, Is.EqualTo(AnimPhase.After));
            Assert.That(done.Progress, Is.EqualTo(1.0));
        }

        [TestCase(AnimDirection.Normal, 0, 0.25, 0.25)]
        [TestCase(AnimDirection.Reverse, 0, 0.25, 0.75)]
        [TestCase(AnimDirection.Alternate, 0, 0.25, 0.25)]
        [TestCase(AnimDirection.Alternate, 1, 0.25, 0.75)]
        [TestCase(AnimDirection.AlternateReverse, 0, 0.25, 0.75)]
        [TestCase(AnimDirection.AlternateReverse, 1, 0.25, 0.25)]
        public void DirectionMapsLocalProgress(AnimDirection direction, int iteration, double local, double expected)
        {
            Assert.That(AnimTimeline.DirectedProgress(direction, iteration, local), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void IterationsAdvanceAndFinalProgressFollowsDirection()
        {
            TimelineSample second = AnimTimeline.Evaluate(1.25, 1.0, 0.0, 3, AnimDirection.Alternate);
            Assert.That(second.Iteration, Is.EqualTo(1));
            Assert.That(second.Progress, Is.EqualTo(0.75).Within(1e-9));
            TimelineSample end = AnimTimeline.Evaluate(5.0, 1.0, 0.0, 3, AnimDirection.Alternate);
            Assert.That(end.Phase, Is.EqualTo(AnimPhase.After));
            Assert.That(end.Progress, Is.EqualTo(1.0));
            TimelineSample even = AnimTimeline.Evaluate(5.0, 1.0, 0.0, 2, AnimDirection.Alternate);
            Assert.That(even.Progress, Is.EqualTo(0.0));
        }

        [Test]
        public void InfiniteNeverFinishes()
        {
            Assert.That(double.IsPositiveInfinity(AnimTimeline.TotalDuration(1.0, 0.0, double.PositiveInfinity)));
            Assert.That(AnimTimeline.Evaluate(1000.5, 1.0, 0.0, double.PositiveInfinity, AnimDirection.Normal).Phase, Is.EqualTo(AnimPhase.Active));
        }

        [TestCase(AnimPhase.Before, AnimFill.None, false)]
        [TestCase(AnimPhase.Before, AnimFill.Backwards, true)]
        [TestCase(AnimPhase.Before, AnimFill.Forwards, false)]
        [TestCase(AnimPhase.After, AnimFill.Forwards, true)]
        [TestCase(AnimPhase.After, AnimFill.Both, true)]
        [TestCase(AnimPhase.After, AnimFill.None, false)]
        [TestCase(AnimPhase.Active, AnimFill.None, true)]
        public void FillRules(AnimPhase phase, AnimFill fill, bool applies)
        {
            Assert.That(AnimTimeline.FillApplies(phase, fill), Is.EqualTo(applies));
        }

        [Test]
        public void StaggerDelayIsLinearInIndex()
        {
            Assert.That(AnimTimeline.StaggerDelay(0, 0.05), Is.EqualTo(0.0));
            Assert.That(AnimTimeline.StaggerDelay(4, 0.05), Is.EqualTo(0.2).Within(1e-9));
            Assert.That(AnimTimeline.StaggerDelay(3, 0.0), Is.EqualTo(0.0));
        }

        [Test]
        public void InstanceReportsStartIterationAndFinish()
        {
            AnimDef def = new AnimDef("a", null, 1.0, 0.0, 2, AnimDirection.Normal, AnimFill.None, "linear", new List<AnimStep>());
            AnimInstance instance = new AnimInstance(def, 0.5);
            AnimFrame wait = instance.Advance(0.25);
            Assert.That(wait.JustStarted, Is.False);
            Assert.That(wait.ApplyValues, Is.False);
            AnimFrame start = instance.Advance(0.5);
            Assert.That(start.JustStarted, Is.True);
            Assert.That(start.ApplyValues, Is.True);
            AnimFrame second = instance.Advance(1.0);
            Assert.That(second.IterationCompleted, Is.True);
            AnimFrame last = instance.Advance(5.0);
            Assert.That(last.JustFinished, Is.True);
            Assert.That(last.Revert, Is.True);
            Assert.That(instance.Finished, Is.True);
            Assert.That(instance.Advance(1.0).JustFinished, Is.False);
        }

        [Test]
        public void ForwardsFillKeepsFinalValuesAndDoesNotRevert()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            AnimInstance instance = new AnimInstance(spec.Find("fade-in-up"), 0.0);
            AnimFrame last = instance.Advance(2.0);
            Assert.That(last.JustFinished, Is.True);
            Assert.That(last.ApplyValues, Is.True);
            Assert.That(last.Revert, Is.False);
        }

        [Test]
        public void SamplerInterpolatesOpacityAndTranslate()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            PreparedAnim prepared = PreparedAnim.Prepare(spec.Find("fade-in-up"));
            Assert.That(prepared.PropertyCount, Is.EqualTo(2));
            int opacity = IndexOf(prepared, "opacity");
            int translate = IndexOf(prepared, "translate");
            AnimValue half = prepared.SampleProperty(opacity, 0.5, null);
            Assert.That(half.Numbers[0], Is.EqualTo(0.5f).Within(1e-5));
            AnimValue move = prepared.SampleProperty(translate, 0.5, null);
            Assert.That(move.Numbers.Length, Is.EqualTo(2));
            Assert.That(move.Numbers[1], Is.EqualTo(6f).Within(1e-4));
            Assert.That(move.Units[1], Is.EqualTo("px"));
        }

        [Test]
        public void SamplerUsesSegmentsBetweenMiddleKeyframes()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            PreparedAnim prepared = PreparedAnim.Prepare(spec.Find("pulse"));
            Assert.That(prepared.SampleProperty(0, 0.25, null).Numbers[0], Is.EqualTo(1.1f).Within(1e-4));
            Assert.That(prepared.SampleProperty(0, 0.5, null).Numbers[0], Is.EqualTo(1.2f).Within(1e-4));
            Assert.That(prepared.SampleProperty(0, 0.75, null).Numbers[0], Is.EqualTo(1.1f).Within(1e-4));
            Assert.That(prepared.SampleProperty(0, 1.0, null).Numbers[0], Is.EqualTo(1f).Within(1e-5));
        }

        [Test]
        public void MissingEndKeyframeBlendsTowardsUnderlyingValue()
        {
            List<AnimStep> steps = new List<AnimStep>
            {
                new AnimStep(0f, null, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("opacity", "0") }),
                new AnimStep(0.5f, null, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("opacity", "0.5") })
            };
            AnimDef def = new AnimDef("x", null, 1.0, 0.0, 1, AnimDirection.Normal, AnimFill.None, "linear", steps);
            PreparedAnim prepared = PreparedAnim.Prepare(def);
            AnimValue end = prepared.SampleProperty(0, 1.0, name => AnimValue.FromNumber(1f));
            Assert.That(end.Numbers[0], Is.EqualTo(1f).Within(1e-5));
            AnimValue late = prepared.SampleProperty(0, 0.75, name => AnimValue.FromNumber(1f));
            Assert.That(late.Numbers[0], Is.EqualTo(0.75f).Within(1e-4));
        }

        [Test]
        public void StepEasingOverridesAnimationEasingForItsSegment()
        {
            List<AnimStep> steps = new List<AnimStep>
            {
                new AnimStep(0f, "ease-in", new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("opacity", "0") }),
                new AnimStep(1f, null, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("opacity", "1") })
            };
            AnimDef def = new AnimDef("x", null, 1.0, 0.0, 1, AnimDirection.Normal, AnimFill.None, "linear", steps);
            float eased = PreparedAnim.Prepare(def).SampleProperty(0, 0.5, null).Numbers[0];
            Assert.That(eased, Is.LessThan(0.45f));
        }

        [Test]
        public void ReducedMotionSkipDropsLoopsAndJumpsFiniteForwardsFill()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            MotionPlan loop = ReducedMotionPolicy.Apply(spec.Find("pulse"), ReducedMotionMode.Skip, true);
            Assert.That(loop.Skip, Is.True);
            Assert.That(loop.JumpToEnd, Is.False);
            MotionPlan once = ReducedMotionPolicy.Apply(spec.Find("fade-in-up"), ReducedMotionMode.Skip, true);
            Assert.That(once.Skip, Is.True);
            Assert.That(once.JumpToEnd, Is.True);
        }

        [Test]
        public void ReducedMotionSimplifyKeepsOpacityOnlyAndShortens()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            MotionPlan plan = ReducedMotionPolicy.Apply(spec.Find("fade-in-up"), ReducedMotionMode.Simplify, true);
            Assert.That(plan.Skip, Is.False);
            Assert.That(plan.Definition.Duration, Is.LessThanOrEqualTo(ReducedMotionPolicy.SimplifiedDuration));
            Assert.That(plan.Definition.Properties(), Is.EqualTo(new[] { "opacity" }));
            MotionPlan pulse = ReducedMotionPolicy.Apply(spec.Find("pulse"), ReducedMotionMode.Simplify, true);
            Assert.That(pulse.Skip, Is.True);
        }

        [Test]
        public void ReducedMotionIsInactiveWhenSystemDoesNotRequestIt()
        {
            AnimSpec spec = AnimSpec.Parse(SampleJson);
            MotionPlan plan = ReducedMotionPolicy.Apply(spec.Find("pulse"), ReducedMotionMode.Skip, false);
            Assert.That(plan.Skip, Is.False);
            Assert.That(plan.Definition, Is.SameAs(spec.Find("pulse")));
        }

        private static int IndexOf(PreparedAnim prepared, string property)
        {
            for (int i = 0; i < prepared.PropertyCount; i++)
            {
                if (prepared.PropertyAt(i) == property)
                    return i;
            }

            return -1;
        }
    }

    public class EasingAndValueTests
    {
        [TestCase("linear")]
        [TestCase("ease")]
        [TestCase("ease-in")]
        [TestCase("ease-out")]
        [TestCase("ease-in-out")]
        [TestCase("ease-in-sine")]
        [TestCase("ease-out-cubic")]
        [TestCase("ease-in-out-circ")]
        [TestCase("ease-in-elastic")]
        [TestCase("ease-out-back")]
        [TestCase("ease-in-out-bounce")]
        [TestCase("cubic-bezier(0.1, 0.7, 1.0, 0.1)")]
        public void EndpointsAreExactAndNamesAreKnown(string name)
        {
            Assert.That(Easing.IsKnown(name), Is.True);
            Assert.That(Easing.Evaluate(name, 0.0), Is.EqualTo(0.0));
            Assert.That(Easing.Evaluate(name, 1.0), Is.EqualTo(1.0));
        }

        [TestCase("ease-in-sine")]
        [TestCase("ease-in-cubic")]
        [TestCase("ease-out-cubic")]
        [TestCase("ease-in-out-cubic")]
        [TestCase("ease-in-circ")]
        [TestCase("ease")]
        [TestCase("ease-in-out")]
        public void NonOvershootingCurvesAreMonotonic(string name)
        {
            double previous = 0.0;
            for (int i = 1; i <= 100; i++)
            {
                double value = Easing.Evaluate(name, i / 100.0);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous - 1e-9), name + " at " + i);
                previous = value;
            }
        }

        [Test]
        public void KnownReferenceValues()
        {
            Assert.That(Easing.Evaluate("ease-in-cubic", 0.5), Is.EqualTo(0.125).Within(1e-9));
            Assert.That(Easing.Evaluate("ease-out-cubic", 0.5), Is.EqualTo(0.875).Within(1e-9));
            Assert.That(Easing.Evaluate("ease-in-out-cubic", 0.5), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(Easing.Evaluate("ease-in-sine", 0.5), Is.EqualTo(1.0 - Math.Cos(Math.PI / 4)).Within(1e-9));
            Assert.That(Easing.Evaluate("ease-in-out", 0.5), Is.EqualTo(0.5).Within(1e-4));
            Assert.That(Easing.Evaluate("ease-out-bounce", 0.5), Is.EqualTo(0.765625).Within(1e-9));
            Assert.That(Easing.Evaluate("cubic-bezier(0, 0, 1, 1)", 0.3), Is.EqualTo(0.3).Within(1e-5));
        }

        [Test]
        public void BackAndElasticOvershoot()
        {
            Assert.That(Easing.Evaluate("ease-out-back", 0.7), Is.GreaterThan(1.0));
            Assert.That(Easing.Evaluate("ease-in-back", 0.2), Is.LessThan(0.0));
        }

        [Test]
        public void UnknownNamesFallBackToLinear()
        {
            Assert.That(Easing.IsKnown("ease-in-wobble"), Is.False);
            Assert.That(Easing.Evaluate("ease-in-wobble", 0.3), Is.EqualTo(0.3));
        }

        [Test]
        public void ParsesNumbersWithUnits()
        {
            AnimValue value = AnimValue.Parse("0 12px");
            Assert.That(value.Kind, Is.EqualTo(AnimValueKind.Numbers));
            Assert.That(value.Numbers, Is.EqualTo(new[] { 0f, 12f }));
            Assert.That(value.Units, Is.EqualTo(new[] { string.Empty, "px" }));
            Assert.That(AnimValue.Parse("45deg").Units[0], Is.EqualTo("deg"));
            Assert.That(AnimValue.Parse("-50%").Numbers[0], Is.EqualTo(-50f));
        }

        [TestCase("#fff", 1f, 1f, 1f, 1f)]
        [TestCase("#ff000080", 1f, 0f, 0f, 0.502f)]
        [TestCase("rgb(0, 255, 0)", 0f, 1f, 0f, 1f)]
        [TestCase("rgba(0, 0, 255, 0.5)", 0f, 0f, 1f, 0.5f)]
        [TestCase("transparent", 0f, 0f, 0f, 0f)]
        public void ParsesColors(string text, float r, float g, float b, float a)
        {
            AnimValue value = AnimValue.Parse(text);
            Assert.That(value.Kind, Is.EqualTo(AnimValueKind.Color));
            Assert.That(value.R, Is.EqualTo(r).Within(0.01f));
            Assert.That(value.G, Is.EqualTo(g).Within(0.01f));
            Assert.That(value.B, Is.EqualTo(b).Within(0.01f));
            Assert.That(value.A, Is.EqualTo(a).Within(0.01f));
        }

        [Test]
        public void LerpAdoptsUnitFromUnitlessZeroAndBroadcastsScalars()
        {
            AnimValue move = AnimValue.Lerp(AnimValue.Parse("0"), AnimValue.Parse("10px 20px"), 0.5f, "translate");
            Assert.That(move.Numbers, Is.EqualTo(new[] { 5f, 10f }));
            Assert.That(move.Units, Is.EqualTo(new[] { "px", "px" }));
            AnimValue scale = AnimValue.Lerp(AnimValue.Parse("1"), AnimValue.Parse("2 4"), 0.5f, "scale");
            Assert.That(scale.Numbers, Is.EqualTo(new[] { 1.5f, 2.5f }));
        }

        [Test]
        public void IncompatibleUnitsFlipAtHalf()
        {
            AnimValue early = AnimValue.Lerp(AnimValue.Parse("10px"), AnimValue.Parse("50%"), 0.25f, "left");
            AnimValue late = AnimValue.Lerp(AnimValue.Parse("10px"), AnimValue.Parse("50%"), 0.75f, "left");
            Assert.That(early.Units[0], Is.EqualTo("px"));
            Assert.That(late.Units[0], Is.EqualTo("%"));
            AnimValue word = AnimValue.Lerp(AnimValue.Parse("none"), AnimValue.Parse("auto"), 0.75f, "display");
            Assert.That(word.Raw, Is.EqualTo("auto"));
        }

        [Test]
        public void ColorLerpUsesPremultipliedAlpha()
        {
            AnimValue mid = AnimValue.Lerp(AnimValue.Parse("transparent"), AnimValue.Parse("#ff0000"), 0.5f, "background-color");
            Assert.That(mid.A, Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(mid.R, Is.EqualTo(1f).Within(1e-5));
            Assert.That(mid.G, Is.EqualTo(0f).Within(1e-5));
        }
    }
}
