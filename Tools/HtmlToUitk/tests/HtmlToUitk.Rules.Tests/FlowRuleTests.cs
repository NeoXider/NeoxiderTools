using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class PageRuleTests
{
    private const string Btn = "<button name=\"{0}\" data-loc=\"k.{0}\" data-text=\"x\"{1}>x</button>";

    private static string Page(string id, string attrs, string inner)
    {
        return "<section name=\"p-" + id + "\" data-page=\"" + id + "\" " + attrs + ">" + inner + "</section>";
    }

    private static string B(string name, string attrs = "")
    {
        return string.Format(Btn, name, attrs.Length == 0 ? "" : " " + attrs);
    }

    [Test]
    public void ValidFlowHasOnlyTheRuntimeNote()
    {
        MiniAnalysis m = Q.Body(Page("home", "data-start", B("go", "data-nav=\"two\"")) + Page("two", "", B("back", "data-back")));
        Assert.That(Q.Ids(m).Split(' ').Where(i => i.StartsWith("P")), Is.EqualTo(new[] { "P011:I:R" }));
        Assert.That(Q.First(m, "P011").EmulationId, Is.EqualTo("page-router"));
    }

    [Test]
    public void P011IsReportedOnceAtTheFirstPage()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "", B("y", "data-back")));
        Assert.That(Q.Count(m, "P011"), Is.EqualTo(1));
        Assert.That(Q.First(m, "P011").Construct, Does.Contain("a"));
    }

    [Test]
    public void NoPagesMeansNoPageRules()
    {
        Assert.That(Q.Ids(Q.Body(B("x"))).Split(' ').Any(i => i.StartsWith("P")), Is.False);
    }

    [Test]
    public void MissingStartIsP001AtTheFirstPage()
    {
        MiniAnalysis m = Q.Body(Page("a", "", B("x")) + Page("b", "", B("y")));
        Assert.That(Q.Has(m, "P001"), Is.True);
        Assert.That(Q.First(m, "P001").Construct, Does.Contain("\"a\""));
    }

    [Test]
    public void TwoStartPagesAreP002()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "data-start", B("y")));
        Assert.That(Q.Count(m, "P002"), Is.EqualTo(1));
        Assert.That(Q.Has(m, "P001"), Is.False);
    }

    [Test]
    public void UnknownNavigationTargetIsP003()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-nav=\"nowhere\"")));
        Assert.That(Q.Has(m, "P003"), Is.True);
        Assert.That(Q.First(m, "P003").Construct, Does.Contain("nowhere"));
        Assert.That(Q.Has(m, "P011"), Is.False);
    }

    [Test]
    public void KnownNavigationTargetIsFine()
    {
        Assert.That(Q.Has(Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "", B("y"))), "P003"), Is.False);
    }

    [Test]
    public void OrphanPageIsP004()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x")) + Page("lost", "", B("y")));
        Assert.That(Q.Has(m, "P004"), Is.True);
        Assert.That(Q.First(m, "P004").Severity, Is.EqualTo(Severity.Warning));
        Assert.That(Q.Has(m, "P005"), Is.False);
    }

    [Test]
    public void StartPageIsNeverAnOrphan()
    {
        Assert.That(Q.Has(Q.Body(Page("a", "data-start", B("x"))), "P004"), Is.False);
    }

    [Test]
    public void UnreachablePageIsP005()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "", B("y")) + Page("c", "", B("z", "data-nav=\"b\"")));
        Assert.That(Q.Has(m, "P005"), Is.True);
        Assert.That(Q.First(m, "P005").Construct, Does.Contain("\"c\""));
        Assert.That(Q.Count(m, "P005"), Is.EqualTo(1));
    }

    [Test]
    public void ReachabilityFollowsChains()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "", B("y", "data-nav=\"c\"")) + Page("c", "", B("z", "data-back")));
        Assert.That(Q.Has(m, "P005"), Is.False);
        Assert.That(Q.Has(m, "P004"), Is.False);
    }

    [Test]
    public void BackOnTheStartPageIsP006()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-back")));
        Assert.That(Q.Has(m, "P006"), Is.True);
        Assert.That(Q.First(m, "P006").Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void BackOnOtherPagesIsFine()
    {
        Assert.That(Q.Has(Q.Body(Page("a", "data-start", B("x", "data-nav=\"b\"")) + Page("b", "", B("y", "data-back"))), "P006"), Is.False);
    }

    [Test]
    public void DuplicateModalIdIsP007()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x") + "<div name=\"m1\" data-modal=\"m\"></div><div name=\"m2\" data-modal=\"m\"></div>"));
        Assert.That(Q.Count(m, "P007"), Is.EqualTo(1));
    }

    [Test]
    public void DistinctModalIdsAreFine()
    {
        Assert.That(Q.Has(Q.Body(Page("a", "data-start", B("x") + "<div name=\"m1\" data-modal=\"m\"></div><div name=\"m2\" data-modal=\"n\"></div>")), "P007"), Is.False);
    }

    [Test]
    public void TabWithoutPanelIsP008()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", "<div name=\"tabs\" data-tabs=\"g\">" + B("t", "data-tab=\"missing\"") + "</div>"));
        Assert.That(Q.Has(m, "P008"), Is.True);
    }

    [Test]
    public void TabWithPanelIsFine()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", "<div name=\"tabs\" data-tabs=\"g\">" + B("t", "data-tab=\"panel\"") + "</div><div name=\"panel\"></div>"));
        Assert.That(Q.Has(m, "P008"), Is.False);
    }

    [Test]
    public void TabOutsideAGroupIsP008()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("t", "data-tab=\"panel\"") + "<div name=\"panel\"></div>"));
        Assert.That(Q.Has(m, "P008"), Is.True);
    }

    [Test]
    public void EmptyTabGroupIsP008()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", "<div name=\"tabs\" data-tabs=\"g\"></div>" + B("x")));
        Assert.That(Q.Has(m, "P008"), Is.True);
    }

    [TestCase("fade", false)]
    [TestCase("slide-left", false)]
    [TestCase("slide-right", false)]
    [TestCase("slide-up", false)]
    [TestCase("scale", false)]
    [TestCase("none", false)]
    [TestCase("spin", true)]
    [TestCase("Fade", true)]
    public void UnknownTransitionIsP009(string transition, bool expected)
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start data-transition=\"" + transition + "\"", B("x")));
        Assert.That(Q.Has(m, "P009"), Is.EqualTo(expected));
    }

    [Test]
    public void PageWithoutFocusableElementIsP010()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", "<span name=\"t\" data-loc=\"k.t\">x</span>"));
        Assert.That(Q.Has(m, "P010"), Is.True);
        Assert.That(Q.First(m, "P010").Severity, Is.EqualTo(Severity.Warning));
    }

    [TestCase("<span name=\"t\" data-loc=\"k.t\" data-focus>x</span>")]
    [TestCase("<div name=\"t\" tabindex=\"0\"></div>")]
    [TestCase("<input type=\"text\" name=\"t\" data-loc=\"k.t\" data-text=\"x\">")]
    [TestCase("<input type=\"checkbox\" name=\"t\" data-loc=\"k.t\" data-text=\"x\">")]
    public void FocusableMarkersSatisfyP010(string inner)
    {
        Assert.That(Q.Has(Q.Body(Page("a", "data-start", inner)), "P010"), Is.False);
    }

    [Test]
    public void ModalTargetsAreJ008()
    {
        MiniAnalysis m = Q.Body(Page("a", "data-start", B("x", "data-open-modal=\"nope\"")));
        Assert.That(Q.Has(m, "J008"), Is.True);
        MiniAnalysis ok = Q.Body(Page("a", "data-start", B("x", "data-open-modal=\"yes\"") + "<div name=\"m\" data-modal=\"yes\"></div>"));
        Assert.That(Q.Has(ok, "J008"), Is.False);
    }

    [Test]
    public void FlowIsCheckedAcrossFiles()
    {
        MiniAnalysis m = MiniAnalyzer.Analyze(new[]
        {
            ("a.html", "<html><body>" + Page("a", "data-start", B("x", "data-nav=\"b\"")) + "</body></html>"),
            ("b.html", "<html><body>" + Page("b", "", B("y", "data-back")) + "</body></html>"),
        }, null, null, null, null, null);
        Assert.That(Q.Has(m, "P003"), Is.False);
        Assert.That(Q.Has(m, "P004"), Is.False);
        Assert.That(Q.Has(m, "P001"), Is.False);
    }

    [Test]
    public void DiagnosticsPointAtTheTriggerElement()
    {
        MiniAnalysis m = Q.Body("<section name=\"p\" data-page=\"a\" data-start>\n<button name=\"x\" data-loc=\"k.x\" data-text=\"x\" data-nav=\"zzz\">x</button>\n</section>");
        Assert.That(Q.First(m, "P003").Span.Start.Line, Is.EqualTo(4));
    }
}

[TestFixture]
public class ProjectStyleRuleTests
{
    private static string Btn(string cls) => "<button name=\"b\" class=\"" + cls + "\" data-loc=\"k.b\">x</button>";

    [TestCase(".t { height: 30px; }", true)]
    [TestCase(".t { height: 44px; }", false)]
    [TestCase(".t { height: 40px; }", false)]
    [TestCase(".t { min-height: 20px; }", true)]
    [TestCase(".t { width: 20px; }", true)]
    [TestCase(".t { width: 100px; }", false)]
    [TestCase(".t { height: 50px; width: 10px; }", true)]
    [TestCase(".t { height: 50px; width: 60px; }", false)]
    [TestCase(".t { color: red; }", false)]
    public void TouchTargetsAreChecked(string css, bool expected)
    {
        MiniAnalysis m = Q.Body(Btn("t"), css);
        Assert.That(Q.Has(m, "R002"), Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void TouchTargetMinimumComesFromTheConfig()
    {
        string css = ".t { height: 44px; }";
        Assert.That(Q.Has(Q.Body(Btn("t"), css, Q.Cfg("{\"rules\":{\"minTouchTarget\":48}}")), "R002"), Is.True);
        Assert.That(Q.Has(Q.Body(Btn("t"), css, Q.Cfg("{\"rules\":{\"minTouchTarget\":0}}")), "R002"), Is.False);
    }

    [Test]
    public void NonInteractiveElementsAreExempt()
    {
        Assert.That(Q.Has(Q.Body("<div name=\"d\" class=\"t\"></div>", ".t { height: 5px; }"), "R002"), Is.False);
    }

    private static string Label(string cls) => "<span name=\"s\" class=\"" + cls + "\" data-loc=\"k.s\">text</span>";

    [TestCase(".t { white-space: nowrap; }", "R003")]
    [TestCase(".t { white-space: nowrap; overflow: hidden; }", "R003")]
    [TestCase(".t { white-space: nowrap; text-overflow: ellipsis; }", "C030 R003")]
    [TestCase(".t { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }", "")]
    [TestCase(".t { text-overflow: ellipsis; }", "C030")]
    [TestCase(".t { text-overflow: ellipsis; overflow: hidden; }", "C030")]
    [TestCase(".t { text-overflow: ellipsis; white-space: nowrap; }", "C030 R003")]
    [TestCase(".t { color: red; }", "")]
    [TestCase(".t { white-space: normal; }", "")]
    public void SingleLineTextIsChecked(string css, string expected)
    {
        MiniAnalysis m = Q.Body(Label("t"), css);
        string found = string.Join(" ", m.Bag.Items.Select(d => d.RuleId).Where(id => id == "R003" || id == "C030").Distinct().OrderBy(x => x, StringComparer.Ordinal));
        Assert.That(found, Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void ElementsWithoutTextAreExempt()
    {
        Assert.That(Q.Has(Q.Body("<span name=\"s\" class=\"t\"></span>", ".t { white-space: nowrap; }"), "R003"), Is.False);
    }

    [Test]
    public void NowrapIsInheritedFromTheParent()
    {
        MiniAnalysis m = Q.Body("<div name=\"p\" class=\"p\"><span name=\"s\" data-loc=\"k.s\">text</span></div>", ".p { white-space: nowrap; }");
        Assert.That(Q.Has(m, "R003"), Is.True);
    }

    [Test]
    public void ElementDiagnosticsPointAtTheElement()
    {
        MiniAnalysis m = Q.Body("\n<span name=\"s\" class=\"t\" data-loc=\"k.s\">text</span>", ".t { white-space: nowrap; }");
        Diagnostic d = Q.First(m, "R003");
        Assert.That(d.Span.File, Is.EqualTo("page.html"));
        Assert.That(d.Construct, Does.Contain("name=\"s\""));
    }
}

[TestFixture]
public class CrossFileRuleTests
{
    private static MiniAnalysis Job(string css, params string[] pageBodies)
    {
        var pages = new List<(string, string)>();
        for (int i = 0; i < pageBodies.Length; i++) pages.Add(("p" + i + ".html", "<html><head><link rel=\"stylesheet\" href=\"shared.css\"></head><body>" + pageBodies[i] + "</body></html>"));
        return MiniAnalyzer.Analyze(pages, null, null, null, null, new Dictionary<string, string> { { "shared.css", css } });
    }

    [Test]
    public void UnusedSharedRuleIsJ005()
    {
        MiniAnalysis m = Job(".used { color: red; }\n.unused { color: blue; }", "<div name=\"a\" class=\"used\"></div>");
        Assert.That(Q.Count(m, "J005"), Is.EqualTo(1));
        Assert.That(Q.First(m, "J005").Span.Start.Line, Is.EqualTo(2));
        Assert.That(Q.First(m, "J005").Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void RuleUsedByOnePageIsFine()
    {
        MiniAnalysis m = Job(".used { color: red; }", "<div name=\"a\"></div>", "<div name=\"b\" class=\"used\"></div>");
        Assert.That(Q.Has(m, "J005"), Is.False);
    }

    [TestCase(":root { --c: red; }")]
    [TestCase(".x:hover { color: red; }")]
    [TestCase("#a { color: red; }")]
    [TestCase("div.x { color: red; }")]
    public void SelectorsThatMatchOrAreExemptAreNotReported(string css)
    {
        MiniAnalysis m = Job(css.Replace(".x", ".x"), "<div name=\"a\" class=\"x\"></div>");
        Assert.That(Q.Has(m, "J005"), Is.False, Q.Ids(m));
    }

    [Test]
    public void IdSelectorsMatchByName()
    {
        Assert.That(Q.Has(Job("#nope { color: red; }", "<div name=\"a\"></div>"), "J005"), Is.True);
        Assert.That(Q.Has(Job("#a { color: red; }", "<div name=\"a\"></div>"), "J005"), Is.False);
    }

    [Test]
    public void DynamicClassesAreExempt()
    {
        string css = ".state-open { color: red; }";
        Assert.That(Q.Has(Job(css, "<div name=\"a\"></div>"), "J005"), Is.True);
        var pages = new[] { ("p.html", "<html><head><link rel=\"stylesheet\" href=\"shared.css\"></head><body><div name=\"a\"></div></body></html>") };
        MiniAnalysis exempt = MiniAnalyzer.Analyze(pages, Q.Cfg("{\"naming\":{\"dynamicClasses\":[\"state-open\"]}}"), null, null, null, new Dictionary<string, string> { { "shared.css", css } });
        Assert.That(Q.Has(exempt, "J005"), Is.False);
        MiniAnalysis pattern = MiniAnalyzer.Analyze(pages, null, null, null, null, new Dictionary<string, string> { { "shared.css", css } });
        Assert.That(Q.Has(pattern, "J005"), Is.True);
        MiniAnalysis isPrefix = MiniAnalyzer.Analyze(pages, null, null, null, null, new Dictionary<string, string> { { "shared.css", ".is-open { color: red; }" } });
        Assert.That(Q.Has(isPrefix, "J005"), Is.False, "the default pattern ^is- exempts is-* classes");
    }

    [Test]
    public void PageStyleBlocksAreNotChecked()
    {
        Assert.That(Q.Has(Q.Css(".unused { color: red; }"), "J005"), Is.False);
    }

    [Test]
    public void RulesInTemplatesCountAsUsed()
    {
        string html = "<html><head><link rel=\"stylesheet\" href=\"shared.css\"></head><body><div name=\"a\"></div><template id=\"t\"><div class=\"card\"></div></template></body></html>";
        MiniAnalysis m = MiniAnalyzer.Analyze(new[] { ("p.html", html) }, null, null, null, null, new Dictionary<string, string> { { "shared.css", ".card { color: red; }" } });
        Assert.That(Q.Has(m, "J005"), Is.False);
    }
}
