using System;
using System.Collections.Generic;
using NUnit.Framework;
using Neo.HtmlToUitk.Effects;
using Neo.HtmlToUitk.Responsive;
using Neo.HtmlToUitk.Selectors;

namespace Neo.HtmlToUitk.PureLogicTests
{
    public class BreakpointTests
    {
        private const string Sample = @"{ ""schema"": 1, ""root"": ""app-root"", ""classes"": [
            { ""class"": ""mq-w-max-900"", ""query"": { ""maxWidth"": 900 } },
            { ""class"": ""mq-portrait"", ""query"": { ""orientation"": ""portrait"" } },
            { ""class"": ""mq-wide"", ""query"": { ""minWidth"": 1200, ""minAspect"": 1.7 } },
            { ""class"": ""mq-short"", ""query"": { ""maxHeight"": 500, ""minWidth"": 600 } } ] }";

        private static List<string> Active(double width, double height)
        {
            List<string> list = new List<string>();
            BreakpointSet.Parse(Sample).Evaluate(width, height, list);
            return list;
        }

        [Test]
        public void ParsesRootAndRules()
        {
            BreakpointSet set = BreakpointSet.Parse(Sample);
            Assert.That(set.Root, Is.EqualTo("app-root"));
            Assert.That(set.Rules.Count, Is.EqualTo(4));
        }

        [Test]
        public void SelectionAtTypicalResolutions()
        {
            Assert.That(Active(1920, 1080), Is.EqualTo(new[] { "mq-wide" }));
            Assert.That(Active(800, 1280), Is.EqualTo(new[] { "mq-w-max-900", "mq-portrait" }));
            Assert.That(Active(1000, 400), Is.EqualTo(new[] { "mq-short" }));
            Assert.That(Active(900, 700), Is.EqualTo(new[] { "mq-w-max-900" }));
        }

        [Test]
        public void BoundsAreInclusive()
        {
            Assert.That(Active(900, 1000), Contains.Item("mq-w-max-900"));
            Assert.That(Active(901, 1000), Does.Not.Contain("mq-w-max-900"));
            Assert.That(Active(1200, 700), Contains.Item("mq-wide"));
            Assert.That(Active(1199, 700), Does.Not.Contain("mq-wide"));
        }

        [Test]
        public void SquareIsPortraitAndNotLandscape()
        {
            BreakpointQuery query = new BreakpointQuery { Orientation = BreakpointOrientation.Portrait };
            Assert.That(query.Matches(500, 500), Is.True);
            Assert.That(new BreakpointQuery { Orientation = BreakpointOrientation.Landscape }.Matches(500, 500), Is.False);
        }

        [Test]
        public void ZeroHeightNeverMatchesAspectRules()
        {
            Assert.That(new BreakpointQuery { MinAspect = 1.0 }.Matches(100, 0), Is.False);
        }

        [Test]
        public void SameSetComparesOrderedContents()
        {
            Assert.That(BreakpointSet.SameSet(new[] { "a", "b" }, new[] { "a", "b" }), Is.True);
            Assert.That(BreakpointSet.SameSet(new[] { "a", "b" }, new[] { "b", "a" }), Is.False);
            Assert.That(BreakpointSet.SameSet(new[] { "a" }, new[] { "a", "b" }), Is.False);
        }
    }

    public class SelectorTests
    {
        private sealed class Node : ISelectNode
        {
            private readonly HashSet<string> _classes;

            public Node(string type, string name, string classes, Node parent)
            {
                TypeName = type;
                Name = name;
                Parent = parent;
                _classes = new HashSet<string>(classes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            }

            public string TypeName { get; }

            public string Name { get; }

            public ISelectNode Parent { get; }

            public bool HasClass(string className) => _classes.Contains(className);
        }

        private static readonly Node Root = new Node("VisualElement", "app-root", "", null);
        private static readonly Node Menu = new Node("VisualElement", "menu", "lg__menu", Root);
        private static readonly Node Card = new Node("Button", "card-1", "lg__card is-active", Menu);

        [TestCase(".lg__card", true)]
        [TestCase(".lg__card.is-active", true)]
        [TestCase(".lg__card.is-gone", false)]
        [TestCase("Button", true)]
        [TestCase("button", true)]
        [TestCase("Label", false)]
        [TestCase("#card-1", true)]
        [TestCase("Button#card-1.lg__card", true)]
        [TestCase("*", true)]
        [TestCase(".lg__menu .lg__card", true)]
        [TestCase(".lg__menu > .lg__card", true)]
        [TestCase("#app-root .lg__card", true)]
        [TestCase("#app-root > .lg__card", false)]
        [TestCase(".lg__card, .other", true)]
        [TestCase(".other, .nothing", false)]
        [TestCase("#app-root > .lg__menu > Button", true)]
        public void MatchesCard(string selector, bool expected)
        {
            SelectorList list = SelectorList.Parse(selector);
            Assert.That(list.IsValid, Is.True, list.Error);
            Assert.That(list.Matches(Card), Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("> .a")]
        [TestCase("a:hover")]
        [TestCase(".")]
        [TestCase("a[b]")]
        public void RejectsUnsupportedSyntax(string selector)
        {
            SelectorList list = SelectorList.Parse(selector);
            Assert.That(list.IsValid, Is.False);
            Assert.That(list.Matches(Card), Is.False);
        }
    }

    public class BlurPlanTests
    {
        [TestCase(0f, 0)]
        [TestCase(1f, 1)]
        [TestCase(8f, 4)]
        [TestCase(64f, 7)]
        public void IterationsGrowLogarithmicallyAndAreCapped(float radius, int expected)
        {
            int result = BlurPlan.IterationsForRadius(radius, 1);
            Assert.That(result, Is.EqualTo(Math.Min(expected, BlurPlan.MaxIterations)));
        }

        [Test]
        public void LevelsHalveFromDownsampledStartAndStopAtMinimum()
        {
            IReadOnlyList<BlurLevel> levels = BlurPlan.Levels(1920, 1080, 4, 3);
            Assert.That(levels.Count, Is.EqualTo(4));
            Assert.That(levels[0].Width, Is.EqualTo(480));
            Assert.That(levels[0].Height, Is.EqualTo(270));
            Assert.That(levels[3].Width, Is.EqualTo(60));
            Assert.That(levels[3].Height, Is.EqualTo(33));
            IReadOnlyList<BlurLevel> tiny = BlurPlan.Levels(32, 32, 1, 6);
            Assert.That(tiny[tiny.Count - 1].Width, Is.EqualTo(BlurPlan.MinSize));
            Assert.That(tiny.Count, Is.LessThan(8));
        }
    }
}
