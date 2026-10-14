using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class StyleEngineTests
    {
        private const string H1 = "<div name='a' class='x'><span name='b' class='y'>t</span></div>";
        private const string H3 = "<div name='a' class='x'><div name='m'><span name='b'>t</span></div></div>";

        private static string A(PseudoStateSet states, params string[] css) => Support.Resolve(H1, "a", states, css);

        private static string A(params string[] css) => Support.Resolve(H1, "a", PseudoStateSet.None, css);

        private static string B(params string[] css) => Support.Resolve(H1, "b", PseudoStateSet.None, css);

        [TestCase(".x{color:red} div{color:blue}", "color: red")]
        [TestCase("div{color:blue} .x{color:red}", "color: red")]
        [TestCase(".x{color:red} .x{color:blue}", "color: blue")]
        [TestCase("#a{color:green} .x{color:red}", "color: green")]
        [TestCase(".x{color:red} #a{color:green}", "color: green")]
        [TestCase("div.x{color:red} .x{color:blue}", "color: red")]
        [TestCase("*{color:red} div{color:blue}", "color: blue")]
        [TestCase(".x{color:red!important} #a{color:blue}", "color: red")]
        [TestCase(".x{color:red!important} .x{color:blue!important}", "color: blue")]
        [TestCase(".x{color:red} .x{color:blue!important} #a{color:green}", "color: blue")]
        [TestCase(".x{color:red;margin-top:1px} .x{color:blue}", "color: blue; margin-top: 1px")]
        [TestCase("div{color:red} div{margin-top:1px} div{color:blue}", "color: blue; margin-top: 1px")]
        [TestCase(".x.x{color:red} #a{color:blue}", "color: blue")]
        [TestCase("body div{color:red} div{color:blue}", "color: red")]
        [TestCase("div:not(.zzz){color:red} .x{color:blue}", "color: red")]
        [TestCase(":where(.x){color:red} div{color:blue}", "color: blue")]
        [TestCase(".nomatch{color:red}", "")]
        public void CascadeOrdersBySpecificityThenOrder(string css, string expected)
        {
            Assert.That(A(css), Is.EqualTo(expected));
        }

        [TestCase(".x{color:red}", ".x{color:blue}", "color: blue")]
        [TestCase(".x{color:blue}", ".x{color:red}", "color: red")]
        [TestCase("div.x{color:red}", ".x{color:blue}", "color: red")]
        [TestCase(".x{color:red}", "div.x{color:blue}", "color: blue")]
        [TestCase(".x{color:red!important}", ".x{color:blue}", "color: red")]
        [TestCase(".x{margin:1px}", ".x{margin-left:2px}", "margin-bottom: 1px; margin-left: 2px; margin-right: 1px; margin-top: 1px")]
        public void SheetOrderDecidesBetweenFiles(string first, string second, string expected)
        {
            Assert.That(A(first, second), Is.EqualTo(expected));
        }

        [Test]
        public void ThreeSheetsApplyInOrder()
        {
            Assert.That(A(".x{color:red}", ".x{color:green}", ".x{color:blue}"), Is.EqualTo("color: blue"));
            Assert.That(A(".x{color:red}", ".x{margin-top:1px}", ".x{color:blue}"), Is.EqualTo("color: blue; margin-top: 1px"));
        }

        [TestCase(".x{color:red;margin:4px;font-size:12px}", "color: red (inherited); font-size: 12px (inherited)")]
        [TestCase(".x{background-color:red;opacity:.5;width:10px}", "")]
        [TestCase(".x{visibility:hidden;white-space:nowrap}", "visibility: hidden (inherited); white-space: nowrap (inherited)")]
        [TestCase(".x{-unity-font-style:bold;-unity-text-align:middle-center}", "-unity-font-style: bold (inherited); -unity-text-align: middle-center (inherited)")]
        [TestCase(".x{letter-spacing:2px;word-spacing:1px;text-shadow:1px 1px red}", "letter-spacing: 2px (inherited); text-shadow: 1px 1px red (inherited); word-spacing: 1px (inherited)")]
        [TestCase(".x{cursor:pointer}", "cursor: pointer (inherited)")]
        [TestCase(".x{flex-direction:row;justify-content:center;overflow:hidden}", "")]
        public void OnlyUiToolkitInheritedPropertiesFlowDown(string css, string expected)
        {
            Assert.That(B(css), Is.EqualTo(expected));
        }

        [Test]
        public void InheritanceReachesDeepDescendantsAndIsOverridable()
        {
            Assert.That(Support.Resolve(H3, "b", PseudoStateSet.None, ".x{color:red}"), Is.EqualTo("color: red (inherited)"));
            Assert.That(Support.Resolve(H3, "b", PseudoStateSet.None, ".x{color:red} div div{color:blue}"), Is.EqualTo("color: blue (inherited)"));
            Assert.That(Support.Resolve(H3, "b", PseudoStateSet.None, ".x{color:red} span{color:green}"), Is.EqualTo("color: green"));
        }

        [TestCase(".y{background-color:inherit} .x{background-color:red}", "background-color: red (inherited)")]
        [TestCase(".x{color:red} .y{color:initial}", "")]
        [TestCase(".x{color:red} .y{color:unset}", "color: red (inherited)")]
        [TestCase(".x{margin-top:1px} .y{margin-top:unset}", "")]
        [TestCase(".x{margin-top:1px} .y{margin-top:inherit}", "margin-top: 1px (inherited)")]
        [TestCase(".y{margin-top:inherit}", "")]
        [TestCase(".x{color:red} .y{color:blue}", "color: blue")]
        public void CssWideKeywordsInTheCascade(string css, string expected)
        {
            Assert.That(B(css), Is.EqualTo(expected));
        }

        [TestCase(".x{margin:4px} .x{margin-left:8px}", "margin-bottom: 4px; margin-left: 8px; margin-right: 4px; margin-top: 4px")]
        [TestCase(".x{margin-left:8px} .x{margin:4px}", "margin-bottom: 4px; margin-left: 4px; margin-right: 4px; margin-top: 4px")]
        [TestCase("#a{margin-left:1px} .x{margin:4px}", "margin-bottom: 4px; margin-left: 1px; margin-right: 4px; margin-top: 4px")]
        [TestCase(".x{margin:1px 2px}", "margin-bottom: 1px; margin-left: 2px; margin-right: 2px; margin-top: 1px")]
        [TestCase(".x{border:1px solid red}", "border-bottom-color: red; border-bottom-style: solid; border-bottom-width: 1px; border-left-color: red; border-left-style: solid; border-left-width: 1px; border-right-color: red; border-right-style: solid; border-right-width: 1px; border-top-color: red; border-top-style: solid; border-top-width: 1px")]
        [TestCase(".x{border:1px solid red} .x{border-top-color:blue}", "border-bottom-color: red; border-bottom-style: solid; border-bottom-width: 1px; border-left-color: red; border-left-style: solid; border-left-width: 1px; border-right-color: red; border-right-style: solid; border-right-width: 1px; border-top-color: blue; border-top-style: solid; border-top-width: 1px")]
        [TestCase(".x{flex:1}", "flex-basis: 0%; flex-grow: 1; flex-shrink: 1")]
        [TestCase(".x{padding:2px!important} #a{padding-top:9px}", "padding-bottom: 2px; padding-left: 2px; padding-right: 2px; padding-top: 2px")]
        public void ShorthandsExpandBeforeTheCascade(string css, string expected)
        {
            Assert.That(A(css), Is.EqualTo(expected));
        }

        [TestCase(":root{--c:red} .x{color:var(--c)}", "color: red")]
        [TestCase(".x{color:var(--missing, blue)}", "color: blue")]
        [TestCase(".x{color:red} .x{color:var(--none)}", "color: red")]
        [TestCase(":root{--a:var(--b);--b:4px} .x{margin-top:var(--a)}", "margin-top: 4px")]
        [TestCase(":root{--a:var(--b);--b:var(--a)} .x{margin-top:var(--a, 1px)}", "margin-top: 1px")]
        [TestCase(":root{--pad:4px 8px} .x{padding:var(--pad)}", "padding-bottom: 4px; padding-left: 8px; padding-right: 8px; padding-top: 4px")]
        [TestCase(":root{--a:4px} .x{margin:1px var(--a)}", "margin-bottom: 1px; margin-left: 4px; margin-right: 4px; margin-top: 1px")]
        [TestCase(":root{--r:10} .x{color:rgb(var(--r),0,0)}", "color: rgb(10, 0, 0)")]
        [TestCase(":root{--c:red} .x{--c:blue;color:var(--c)}", "color: blue")]
        [TestCase(".x{--c:blue} .x{color:var(--c)}", "color: blue")]
        [TestCase(":root{--c:red} .x{color:var(--c, green)}", "color: red")]
        [TestCase(".x{color:var(--a, var(--b, purple))}", "color: purple")]
        [TestCase(":root{--b:teal} .x{color:var(--a, var(--b, purple))}", "color: teal")]
        [TestCase(":root{--w:2px} .x{border:var(--w) solid red}", "border-bottom-color: red; border-bottom-style: solid; border-bottom-width: 2px; border-left-color: red; border-left-style: solid; border-left-width: 2px; border-right-color: red; border-right-style: solid; border-right-width: 2px; border-top-color: red; border-top-style: solid; border-top-width: 2px")]
        public void VariablesAreSubstitutedBeforeExpansion(string css, string expected)
        {
            Assert.That(A(css), Is.EqualTo(expected));
        }

        [Test]
        public void ElementLevelVariablesInheritDownTheTree()
        {
            Assert.That(B(":root{--c:red} .x{--c:blue} .y{color:var(--c)}"), Is.EqualTo("color: blue"));
            Assert.That(B(":root{--c:red} .y{color:var(--c)}"), Is.EqualTo("color: red"));
        }

        [Test]
        public void VariablesApiReturnsResolvedValues()
        {
            HtmlDocument document = Support.Html(H1);
            IStyleModel model = Support.Model(new[] { ":root{--a:1px} .x{--b:var(--a);--c:var(--zzz)} .y{--a:9px}" });
            IReadOnlyDictionary<string, CssValue> onA = model.Variables(Support.ByName(document, "a"));
            Assert.That(onA["--a"].ToText(), Is.EqualTo("1px"));
            Assert.That(onA["--b"].ToText(), Is.EqualTo("1px"));
            Assert.That(onA.ContainsKey("--c"), Is.False);
            IReadOnlyDictionary<string, CssValue> onB = model.Variables(Support.ByName(document, "b"));
            Assert.That(onB["--a"].ToText(), Is.EqualTo("9px"));
            Assert.That(onB["--b"].ToText(), Is.EqualTo("1px"));
        }

        [Test]
        public void CyclicCustomPropertiesAreInvalid()
        {
            HtmlDocument document = Support.Html(H1);
            IStyleModel model = Support.Model(new[] { ":root{--a:var(--b);--b:var(--a);--ok:1px}" });
            IReadOnlyDictionary<string, CssValue> vars = model.Variables(Support.ByName(document, "a"));
            Assert.That(vars.ContainsKey("--a"), Is.False);
            Assert.That(vars.ContainsKey("--b"), Is.False);
            Assert.That(vars["--ok"].ToText(), Is.EqualTo("1px"));
        }

        [Test]
        public void SelfReferenceIsACycle()
        {
            HtmlDocument document = Support.Html(H1);
            IStyleModel model = Support.Model(new[] { ":root{--a:var(--a)}" });
            Assert.That(model.Variables(Support.ByName(document, "a")).ContainsKey("--a"), Is.False);
        }

        [Test]
        public void VarDepthLimitInvalidatesLongChains()
        {
            var css = new System.Text.StringBuilder(":root{--v0:1px;");
            for (int i = 1; i <= 40; i++) css.Append("--v").Append(i).Append(":var(--v").Append(i - 1).Append(");");
            css.Append("}");
            HtmlDocument document = Support.Html(H1);
            IStyleModel model = Support.Model(new[] { css.ToString() }, null, new StyleOptions { MaxVarDepth = 100 });
            Assert.That(model.Variables(Support.ByName(document, "a"))["--v40"].ToText(), Is.EqualTo("1px"));
        }

        [TestCase(PseudoStateSet.None, "color: red")]
        [TestCase(PseudoStateSet.Hover, "color: blue")]
        [TestCase(PseudoStateSet.Active, "color: red")]
        [TestCase(PseudoStateSet.Hover | PseudoStateSet.Active, "color: blue")]
        public void StatesSelectPseudoClassRules(PseudoStateSet states, string expected)
        {
            Assert.That(A(states, ".x{color:red} .x:hover{color:blue}"), Is.EqualTo(expected));
        }

        [Test]
        public void ActiveBeatsHoverByOrder()
        {
            string css = ".x:hover{color:blue} .x:active{color:green}";
            Assert.That(A(PseudoStateSet.Hover | PseudoStateSet.Active, css), Is.EqualTo("color: green"));
            Assert.That(A(PseudoStateSet.Hover, css), Is.EqualTo("color: blue"));
        }

        [TestCase("@media (max-width:1px){.x{color:red}}")]
        [TestCase(".x::before{color:red}")]
        [TestCase(".x::after{color:red}")]
        public void MediaAndPseudoElementRulesDoNotStyleTheElement(string css)
        {
            Assert.That(A(css), Is.EqualTo(""));
        }

        [Test]
        public void PseudoElementsResolveSeparately()
        {
            var model = (StyleModel)Support.Model(new[] { ".x::before{color:red;content:\"x\"} .x{margin-top:1px}" });
            DomElement element = Support.ByName(Support.Html(H1), "a");
            ResolvedStyle before = model.ResolvePseudoElement(element, "before", PseudoStateSet.None);
            Assert.That(before.Properties.Keys.OrderBy(k => k), Is.EqualTo(new[] { "color", "content" }));
            Assert.That(model.ResolvePseudoElement(element, "after", PseudoStateSet.None).Properties, Is.Empty);
        }

        [Test]
        public void ResolvedValuesKnowTheirOrigin()
        {
            var bag = new DiagnosticBag();
            IStyleModel model = Support.Model(new[] { ".x{color:red}", ".x{color:blue}" }, bag);
            DomElement element = Support.ByName(Support.Html(H1), "a");
            ResolvedValue value = model.Resolve(element, PseudoStateSet.None).Properties["color"];
            Assert.That(value.Sheet.Path, Is.EqualTo("sheet2.css"));
            Assert.That(value.Source.Name, Is.EqualTo("color"));
            Assert.That(value.Rule.Selectors[0].Text, Is.EqualTo(".x"));
            Assert.That(value.Inherited, Is.False);
        }

        [Test]
        public void ShorthandSourceIsTheOriginalDeclaration()
        {
            IStyleModel model = Support.Model(new[] { ".x{margin:1px 2px}" });
            DomElement element = Support.ByName(Support.Html(H1), "a");
            ResolvedValue value = model.Resolve(element, PseudoStateSet.None).Properties["margin-left"];
            Assert.That(value.Source.Name, Is.EqualTo("margin"));
            Assert.That(value.Value.ToText(), Is.EqualTo("2px"));
        }

        [Test]
        public void ResolveIsCachedPerElementAndState()
        {
            IStyleModel model = Support.Model(new[] { ".x{color:red}" });
            DomElement element = Support.ByName(Support.Html(H1), "a");
            Assert.That(ReferenceEquals(model.Resolve(element, PseudoStateSet.None), model.Resolve(element, PseudoStateSet.None)), Is.True);
            Assert.That(ReferenceEquals(model.Resolve(element, PseudoStateSet.None), model.Resolve(element, PseudoStateSet.Hover)), Is.False);
        }

        [Test]
        public void CascadePropertyExposesTheSheetsInOrder()
        {
            IStyleModel model = Support.Model(new[] { ".a{}", ".b{}", ".c{}" });
            Assert.That(model.Cascade.Select(s => s.Path), Is.EqualTo(new[] { "sheet1.css", "sheet2.css", "sheet3.css" }));
        }

        [Test]
        public void MatchReturnsDocumentOrder()
        {
            HtmlDocument document = Support.Html("<div name='r'><p name='p1'></p><div name='d'><p name='p2'></p></div></div>");
            IStyleModel model = Support.Model(new string[0]);
            IReadOnlyList<IElement> matches = model.Match(SelectorParser.Parse("p"), document.Body);
            Assert.That(matches.Select(m => m.Name), Is.EqualTo(new[] { "p1", "p2" }));
            Assert.That(model.Match(SelectorParser.Parse("div > p"), document.Body).Select(m => m.Name), Is.EqualTo(new[] { "p1", "p2" }));
            Assert.That(model.Match(SelectorParser.Parse("div div p"), document.Body).Select(m => m.Name), Is.EqualTo(new[] { "p2" }));
        }

        [Test]
        public void CustomInheritedSetIsHonoured()
        {
            var options = new StyleOptions { InheritedProperties = new[] { "margin-left" } };
            IStyleModel model = Support.Model(new[] { ".x{margin-left:3px;color:red}" }, null, options);
            DomElement child = Support.ByName(Support.Html(H1), "b");
            ResolvedStyle style = model.Resolve(child, PseudoStateSet.None);
            Assert.That(style.Properties.Keys.ToArray(), Is.EqualTo(new[] { "margin-left" }));
        }

        [Test]
        public void DefaultInheritedListMatchesTheUitkSpecification()
        {
            Assert.That(StyleEngine.DefaultInherited, Does.Contain("color"));
            Assert.That(StyleEngine.DefaultInherited, Does.Contain("-unity-font-definition"));
            Assert.That(StyleEngine.DefaultInherited, Does.Not.Contain("margin-top"));
            Assert.That(StyleEngine.DefaultInherited, Does.Not.Contain("background-color"));
        }

        [Test]
        public void ConflictsAreReportedAcrossSheetsWithBothSpans()
        {
            var bag = new DiagnosticBag();
            IStyleModel model = Support.Model(new[] { ".btn{color:red;margin:1px}", ".btn{color:blue;margin:1px}" }, bag);
            Assert.That(model.Conflicts.Count, Is.EqualTo(1));
            StyleConflict conflict = model.Conflicts[0];
            Assert.That(conflict.SelectorText, Is.EqualTo(".btn"));
            Assert.That(conflict.Property, Is.EqualTo("color"));
            Assert.That(conflict.FirstValue, Is.EqualTo("red"));
            Assert.That(conflict.SecondValue, Is.EqualTo("blue"));
            Assert.That(conflict.First.File, Is.EqualTo("sheet1.css"));
            Assert.That(conflict.Second.File, Is.EqualTo("sheet2.css"));
            Assert.That(conflict.FirstSheet.Path, Is.EqualTo("sheet1.css"));
            Diagnostic diagnostic = bag.Items.Single();
            Assert.That(diagnostic.RuleId, Is.EqualTo("J001"));
            Assert.That(diagnostic.Severity, Is.EqualTo(Severity.Warning));
            Assert.That(diagnostic.Related.Count, Is.EqualTo(2));
            Assert.That(diagnostic.Related[0].File, Is.EqualTo("sheet1.css"));
            Assert.That(diagnostic.Related[1].File, Is.EqualTo("sheet2.css"));
            Assert.That(bag.Verdict, Is.EqualTo(Verdict.PassWithWarnings));
        }

        [TestCase(".a{color:red}", ".a{color:red}", 0)]
        [TestCase(".a{color:red}", ".a{color:RED}", 0)]
        [TestCase(".a{color:red}", ".b{color:blue}", 0)]
        [TestCase(".a{color:red}", ".a{margin:0}", 0)]
        [TestCase(".a.b{color:red}", ".b.a{color:blue}", 1)]
        [TestCase("a  >  b{color:red}", "a>b{color:blue}", 1)]
        [TestCase(".a{color:red}.a{color:blue}", ".c{x:1}", 0)]
        [TestCase(":root{--x:1}", ":root{--x:2}", 1)]
        [TestCase(":root{--x:1}", ":root{--y:2}", 0)]
        [TestCase(".a{color:red}", ".a{color:blue!important}", 1)]
        [TestCase("@media screen{.a{color:red}}", ".a{color:blue}", 0)]
        public void ConflictDetectionTable(string first, string second, int expected)
        {
            IStyleModel model = Support.Model(new[] { first, second });
            Assert.That(model.Conflicts.Count, Is.EqualTo(expected));
        }

        [Test]
        public void ConflictsRequireDifferentSheets()
        {
            IStyleModel model = Support.Model(new[] { ".a{color:red} .a{color:blue}" });
            Assert.That(model.Conflicts, Is.Empty);
        }

        [Test]
        public void ThreeSheetsChainConflicts()
        {
            IStyleModel model = Support.Model(new[] { ".a{color:red}", ".a{color:green}", ".a{color:green}" });
            Assert.That(model.Conflicts.Count, Is.EqualTo(1));
            model = Support.Model(new[] { ".a{color:red}", ".a{color:green}", ".a{color:blue}" });
            Assert.That(model.Conflicts.Count, Is.EqualTo(2));
        }

        [Test]
        public void ConflictDetectionCanBeDisabled()
        {
            var bag = new DiagnosticBag();
            IStyleModel model = Support.Model(new[] { ".a{color:red}", ".a{color:blue}" }, bag, new StyleOptions { DetectConflicts = false });
            Assert.That(model.Conflicts, Is.Empty);
            Assert.That(bag.Items, Is.Empty);
        }

        [Test]
        public void UndefinedVariablesAreReportedOncePerReference()
        {
            var bag = new DiagnosticBag();
            Support.Model(new[] { ".a{color:var(--nope)} .b{margin:var(--nope) var(--other, 1px)}" }, bag);
            Assert.That(Support.Ids(bag), Is.EqualTo("C033,C033"));
            Assert.That(bag.Items.All(d => d.Severity == Severity.Error), Is.True);
            Assert.That(bag.Items[0].Construct, Is.EqualTo("var(--nope)"));
            Assert.That(bag.Verdict, Is.EqualTo(Verdict.Fail));
        }

        [Test]
        public void DefinedVariablesAndFallbacksAreSilent()
        {
            var bag = new DiagnosticBag();
            Support.Model(new[] { ":root{--a:1px} .a{margin:var(--a) var(--b, 2px)}" }, bag);
            Assert.That(bag.Items, Is.Empty);
        }

        [Test]
        public void VariableCyclesAreReported()
        {
            var bag = new DiagnosticBag();
            Support.Model(new[] { ":root{--a:var(--b);--b:var(--c);--c:var(--a)}" }, bag);
            Assert.That(bag.Items.Count(d => d.Message.Contains("circular")), Is.EqualTo(1));
            Assert.That(bag.Items.First(d => d.Message.Contains("circular")).Construct, Does.Contain("--a"));
        }

        [Test]
        public void VariablesDefinedInLaterSheetsCount()
        {
            var bag = new DiagnosticBag();
            Support.Model(new[] { ".a{color:var(--c)}", ":root{--c:red}" }, bag);
            Assert.That(bag.Items, Is.Empty);
        }

        [Test]
        public void EmptyCascadeResolvesToNothing()
        {
            Assert.That(A(), Is.EqualTo(""));
        }

        [Test]
        public void StatesPropagateToDescendantRules()
        {
            string result = Support.Resolve(H1, "b", PseudoStateSet.Hover, ".x:hover .y{color:red}");
            Assert.That(result, Is.EqualTo("color: red"));
            Assert.That(Support.Resolve(H1, "b", PseudoStateSet.None, ".x:hover .y{color:red}"), Is.EqualTo(""));
        }

        [Test]
        public void MatchingRulesListsTheCandidates()
        {
            var model = (StyleModel)Support.Model(new[] { ".x{a:1} div{b:2} .zzz{c:3}" });
            DomElement element = Support.ByName(Support.Html(H1), "a");
            var rules = model.MatchingRules(element, PseudoStateSet.None);
            Assert.That(rules.Select(r => r.Selector.Text), Is.EqualTo(new[] { ".x", "div" }));
        }
    }
}
