using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Emulations;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Rules;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class ContractsTests
    {
        private static Diagnostic D(string rule, Severity severity, string file = "a.html", int line = 1, int column = 1)
        {
            var pos = new SourcePos(0, line, column);
            return new Diagnostic { RuleId = rule, Severity = severity, Span = new SourceSpan(file, pos, pos) };
        }

        [Test]
        public void EmptyBagPasses()
        {
            var bag = new DiagnosticBag();
            Assert.That(bag.Verdict, Is.EqualTo(Verdict.Pass));
            Assert.That(bag.Count(Severity.Error), Is.EqualTo(0));
            Assert.That(bag.Sorted(), Is.Empty);
        }

        [TestCase(new[] { Severity.Info }, Verdict.Pass)]
        [TestCase(new[] { Severity.Info, Severity.Warning }, Verdict.PassWithWarnings)]
        [TestCase(new[] { Severity.Warning, Severity.Warning }, Verdict.PassWithWarnings)]
        [TestCase(new[] { Severity.Warning, Severity.Error }, Verdict.Fail)]
        [TestCase(new[] { Severity.Error }, Verdict.Fail)]
        [TestCase(new[] { Severity.Info, Severity.Info, Severity.Info }, Verdict.Pass)]
        public void VerdictFollowsTheWorstSeverity(Severity[] severities, Verdict expected)
        {
            var bag = new DiagnosticBag();
            foreach (Severity s in severities) bag.Report(D("X001", s));
            Assert.That(bag.Verdict, Is.EqualTo(expected));
        }

        [Test]
        public void CountsPerSeverity()
        {
            var bag = new DiagnosticBag();
            bag.Report(D("A", Severity.Error));
            bag.Report(D("B", Severity.Warning));
            bag.Report(D("C", Severity.Warning));
            bag.Report(D("D", Severity.Info));
            Assert.That(new[] { bag.Count(Severity.Error), bag.Count(Severity.Warning), bag.Count(Severity.Info) }, Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(bag.Items.Count, Is.EqualTo(4));
        }

        [Test]
        public void SortedOrdersBySeverityFileLineColumnRule()
        {
            var bag = new DiagnosticBag();
            bag.Report(D("I1", Severity.Info, "a.html", 1, 1));
            bag.Report(D("W2", Severity.Warning, "b.html", 1, 1));
            bag.Report(D("E3", Severity.Error, "b.html", 2, 1));
            bag.Report(D("E1", Severity.Error, "a.html", 5, 9));
            bag.Report(D("E2", Severity.Error, "a.html", 5, 2));
            bag.Report(D("E0", Severity.Error, "a.html", 5, 2));
            bag.Report(D("W1", Severity.Warning, "a.html", 3, 1));
            Assert.That(bag.Sorted().Select(d => d.RuleId), Is.EqualTo(new[] { "E0", "E2", "E1", "E3", "W1", "W2", "I1" }));
        }

        [Test]
        public void SortedIsStableForIdenticalKeys()
        {
            var bag = new DiagnosticBag();
            for (int i = 0; i < 20; i++)
            {
                Diagnostic d = D("R", Severity.Warning);
                d.Message = i.ToString();
                bag.Report(d);
            }
            Assert.That(bag.Sorted().Select(d => d.Message), Is.EqualTo(Enumerable.Range(0, 20).Select(i => i.ToString())));
        }

        [Test]
        public void ReportRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new DiagnosticBag().Report(null));
        }

        [Test]
        public void SourcePosAndSpanHaveValueEquality()
        {
            var a = new SourcePos(1, 2, 3);
            Assert.That(a, Is.EqualTo(new SourcePos(1, 2, 3)));
            Assert.That(a == new SourcePos(1, 2, 4), Is.False);
            Assert.That(a != new SourcePos(1, 2, 4), Is.True);
            Assert.That(a.ToString(), Is.EqualTo("2:3"));
            var span = new SourceSpan("f", a, a);
            Assert.That(span, Is.EqualTo(new SourceSpan("f", a, a)));
            Assert.That(span == new SourceSpan("g", a, a), Is.False);
            Assert.That(span.ToString(), Is.EqualTo("f:2:3"));
            Assert.That(SourceSpan.None.IsNone, Is.True);
            Assert.That(span.IsNone, Is.False);
            Assert.That(span.GetHashCode(), Is.EqualTo(new SourceSpan("f", a, a).GetHashCode()));
        }

        [Test]
        public void DiagnosticDefaultsAreSafe()
        {
            var d = new Diagnostic();
            Assert.That(d.Related, Is.Empty);
            Assert.That(d.EmulationId, Is.Null);
        }

        [Test]
        public void ModelObjectsDefaultToEmptyCollections()
        {
            Assert.That(new HtmlDocument().Links, Is.Empty);
            Assert.That(new DomElement().Children, Is.Empty);
            Assert.That(new StyleRule().Selectors, Is.Empty);
            Assert.That(new Stylesheet().Imports, Is.Empty);
            Assert.That(new HtmlToUitk.Core.Uxml.UiDocument().Roots, Is.Empty);
            Assert.That(new InputSet().Pages, Is.Empty);
            Assert.That(new TransformedModel().Documents, Is.Empty);
            Assert.That(new Construct().Data, Is.Empty);
            Assert.That(new SimpleSelector().Classes, Is.Empty);
        }

        [Test]
        public void ConfigDefaultsMirrorTheSchema()
        {
            var config = new ConversionConfig();
            Assert.That(config.UiRoot, Is.EqualTo("UI"));
            Assert.That(config.Layout.Screens, Is.EqualTo("Screens"));
            Assert.That(config.Unity.ReferenceResolution, Is.EqualTo(new[] { 1920, 1080 }));
            Assert.That(config.Localization.Locales, Is.EqualTo(new[] { "en" }));
            Assert.That(config.Rules.MinFontSize, Is.EqualTo(13));
            Assert.That(config.Rules.MinTouchTarget, Is.EqualTo(40));
            Assert.That(config.Rules.MaxKeyframeSteps, Is.EqualTo(8));
            Assert.That(config.Uxml.Indent, Is.EqualTo(4));
            Assert.That(config.Uxml.Namespaces, Is.EqualTo(new[] { "ui", "uie" }));
            Assert.That(config.Uxml.AllowInlineStyles, Is.EqualTo(new[] { "display: none;" }));
            Assert.That(config.Uss.SectionOrder, Is.True);
            Assert.That(config.Emulation.AllowRuntime, Is.True);
            Assert.That(config.Emulation.GridMaxTracks, Is.EqualTo(24));
            Assert.That(config.Lint.Builtin, Is.True);
            Assert.That(config.Preview.Viewports, Is.EqualTo(new[] { "1920x1080" }));
            Assert.That(config.Pages.HiddenClass, Is.EqualTo("is-hidden"));
            Assert.That(config.Animation.ReducedMotion, Is.EqualTo("simplify"));
            Assert.That(config.Fonts, Is.Empty);
            var job = new JobSpec();
            Assert.That(job.Output.Assemble, Is.EqualTo("perFile"));
            Assert.That(job.Inputs.CssOrder, Is.EqualTo("before"));
            Assert.That(job.Watch.DebounceMs, Is.EqualTo(400));
        }

        [Test]
        public void ConfigSectionsAreNotShared()
        {
            var a = new ConversionConfig();
            var b = new ConversionConfig();
            Assert.That(ReferenceEquals(a.Layout, b.Layout), Is.False);
            Assert.That(ReferenceEquals(a.Rules, b.Rules), Is.False);
        }

        private static CapabilityMatrix Matrix()
        {
            return new CapabilityMatrix(new CapabilityData
            {
                UnityVersion = "6000.6.0f1",
                Properties = new Dictionary<string, PropertyCapability>
                {
                    { "color", new PropertyCapability { Name = "color", Id = "Color", Inherited = true, Animatable = Animatable.Full } },
                    { "justify-content", new PropertyCapability { Name = "justify-content", Id = "JustifyContent", Values = new[] { "center", "space-evenly" } } }
                },
                Combinators = new[] { " ", ">" },
                PseudoClasses = new[] { "hover", "focus" },
                Functions = new Dictionary<string, bool> { { "var", true }, { "calc", false }, { "linear-gradient", true } },
                FunctionEvidence = new Dictionary<string, string> { { "linear-gradient", "assembly-only" } },
                Keywords = new[] { "inherit", "initial" },
                Filters = new[] { "blur" },
                Easings = new[] { "ease", "linear" },
                LengthUnits = new[] { "px", "%" },
                AngleUnits = new[] { "deg" },
                TimeUnits = new[] { "s", "ms" },
                Conditions = new Dictionary<string, string> { { "backdrop-filter", "panel.renderMode == ScreenSpaceOverlay" } }
            });
        }

        [TestCase("css:color", true)]
        [TestCase("css:gap", false)]
        [TestCase("property:justify-content", true)]
        [TestCase("uss:linear-gradient", true)]
        [TestCase("uss:calc", false)]
        [TestCase("uss:inherit", true)]
        [TestCase("uss:hover", true)]
        [TestCase("function:var", true)]
        [TestCase("function:calc", false)]
        [TestCase("keyword:initial", true)]
        [TestCase("keyword:unset", false)]
        [TestCase("filter:blur", true)]
        [TestCase("filter:sepia", false)]
        [TestCase("easing:ease", true)]
        [TestCase("easing:cubic-bezier", false)]
        [TestCase("unit:px", true)]
        [TestCase("unit:deg", true)]
        [TestCase("unit:ms", true)]
        [TestCase("unit:vh", false)]
        [TestCase("pseudo:hover", true)]
        [TestCase("pseudo:visited", false)]
        [TestCase("combinator:>", true)]
        [TestCase("combinator:+", false)]
        [TestCase("nocolon", false)]
        [TestCase("weird:thing", false)]
        [TestCase(":x", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void SupportsKeys(string key, bool expected)
        {
            Assert.That(Matrix().Supports(key), Is.EqualTo(expected));
        }

        [Test]
        public void MatrixQueries()
        {
            CapabilityMatrix matrix = Matrix();
            Assert.That(matrix.UnityVersion, Is.EqualTo("6000.6.0f1"));
            Assert.That(matrix.HasProperty("color"), Is.True);
            Assert.That(matrix.HasProperty(null), Is.False);
            Assert.That(matrix.Property("color").Inherited, Is.True);
            Assert.That(matrix.Property("justify-content").Values, Does.Contain("space-evenly"));
            Assert.That(matrix.Property("nope"), Is.Null);
            Assert.That(matrix.HasFunction("var"), Is.True);
            Assert.That(matrix.HasFunction("calc"), Is.False);
            Assert.That(matrix.FunctionEvidence("linear-gradient"), Is.EqualTo("assembly-only"));
            Assert.That(matrix.FunctionEvidence("var"), Is.Null);
            Assert.That(matrix.HasCombinator('>'), Is.True);
            Assert.That(matrix.HasCombinator('+'), Is.False);
            Assert.That(matrix.Condition("backdrop-filter"), Does.Contain("ScreenSpaceOverlay"));
            Assert.That(matrix.Condition("other"), Is.Null);
            Assert.Throws<ArgumentNullException>(() => new CapabilityMatrix(null));
        }

        private sealed class FakeEmulation : IEmulation
        {
            public FakeEmulation(string id, int order, params string[] handles)
            {
                Id = id;
                Order = order;
                Handles = handles;
            }

            public string Id { get; }
            public Tier Tier => Tier.ConvertTime;
            public IReadOnlyList<string> Handles { get; }
            public IReadOnlyList<string> RuntimeComponents => Array.Empty<string>();
            public int Order { get; }
            public EmulationOutcome Apply(EmulationContext context, Construct construct) => new EmulationOutcome { Handled = true };
        }

        [Test]
        public void RegistryOrdersByOrderThenIdThenRegistration()
        {
            var registry = new EmulationRegistry();
            registry.Register(new FakeEmulation("zeta", 2, "prop:z"));
            registry.Register(new FakeEmulation("beta", 1, "prop:b"));
            registry.Register(new FakeEmulation("alpha", 1, "prop:a"));
            registry.Register(new FakeEmulation("first", 0, "prop:f"));
            Assert.That(registry.Ordered.Select(e => e.Id), Is.EqualTo(new[] { "first", "alpha", "beta", "zeta" }));
            registry.Register(new FakeEmulation("late", -1, "prop:l"));
            Assert.That(registry.Ordered[0].Id, Is.EqualTo("late"));
        }

        [Test]
        public void RegistryLooksUpByConstructKey()
        {
            var registry = new EmulationRegistry();
            registry.Register(new FakeEmulation("grid", 1, "prop:display=grid", "prop:grid-template-columns"));
            Assert.That(registry.For("prop:display=grid").Id, Is.EqualTo("grid"));
            Assert.That(registry.For("prop:grid-template-columns").Id, Is.EqualTo("grid"));
            Assert.That(registry.For("prop:other"), Is.Null);
            Assert.That(registry.For(null), Is.Null);
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void SheetBuilderGroupsRulesPerSheetInInsertionOrder()
        {
            var builder = new SheetBuilder();
            var r1 = new StyleRule();
            var r2 = new StyleRule();
            var r3 = new StyleRule();
            builder.AddRule("page", r1);
            builder.AddRule("shared", r2);
            builder.AddRule("page", r3);
            Assert.That(builder.SheetIds, Is.EqualTo(new[] { "page", "shared" }));
            Assert.That(builder.Rules("page"), Is.EqualTo(new[] { r1, r3 }));
            Assert.That(builder.Rules("none"), Is.Empty);
            builder.ReplaceRules("page", new[] { r2 });
            Assert.That(builder.Rules("page"), Is.EqualTo(new[] { r2 }));
            builder.ReplaceRules("fresh", null);
            Assert.That(builder.SheetIds.Last(), Is.EqualTo("fresh"));
            Assert.Throws<ArgumentNullException>(() => builder.AddRule(null, r1));
            Assert.Throws<ArgumentNullException>(() => builder.AddRule("x", null));
        }

        [TestCase("a b  c", new[] { "a", "b", "c" })]
        [TestCase("  ", new string[0])]
        [TestCase("", new string[0])]
        [TestCase(null, new string[0])]
        [TestCase("a\tb\nc\rd\fe", new[] { "a", "b", "c", "d", "e" })]
        public void ClassListSplits(string value, string[] expected)
        {
            Assert.That(ClassList.Split(value), Is.EqualTo(expected));
        }

        [Test]
        public void CssValuesCompareByText()
        {
            Assert.That(new CssDimension(0, "px").ToText(), Is.EqualTo("0"));
            Assert.That(new CssDimension(0, "s").ToText(), Is.EqualTo("0s"));
            Assert.That(new CssList(new CssValue[] { new CssNumber(1), new CssIdent("a") }, ' ').ToText(), Is.EqualTo("1 a"));
            Assert.That(new CssList(new CssValue[] { new CssNumber(1), new CssIdent("a") }, ',').ToText(), Is.EqualTo("1, a"));
            Assert.That(new CssFunction("f", null).ToText(), Is.EqualTo("f()"));
            Assert.That(new CssVarRef("--x", new CssNumber(1)).ToText(), Is.EqualTo("var(--x, 1)"));
            Assert.That(new CssUrl("a b.png").ToText(), Is.EqualTo("url(\"a b.png\")"));
            Assert.That(new CssPercent(12.5).ToText(), Is.EqualTo("12.5%"));
            Assert.That(new CssRaw("x").ToString(), Is.EqualTo("x"));
        }

        [Test]
        public void SpecificityHasValueSemantics()
        {
            Assert.That(new Specificity(1, 2, 3), Is.EqualTo(new Specificity(1, 2, 3)));
            Assert.That(new Specificity(1, 2, 3).GetHashCode(), Is.EqualTo(new Specificity(1, 2, 3).GetHashCode()));
            Assert.That(new Specificity(1, 0, 0).CompareTo(null), Is.GreaterThan(0));
            Assert.That(Specificity.Zero.ToString(), Is.EqualTo("(0,0,0)"));
        }
    }
}
