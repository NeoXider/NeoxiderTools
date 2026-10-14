using System;
using System.Linq;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class TransitionRuleTests
{
    [TestCase("transition: opacity 0.2s", "")]
    [TestCase("transition: opacity 0.2s ease-out", "")]
    [TestCase("transition: opacity 200ms ease-in-out-cubic 50ms", "")]
    [TestCase("transition: all .3s", "")]
    [TestCase("transition: none", "")]
    [TestCase("transition: scale 120ms ease-in, opacity 0.2s", "")]
    [TestCase("transition: background-color 0.2s linear", "")]
    [TestCase("transition: opacity 0.2s cubic-bezier(0.4, 0, 0.2, 1)", "R007")]
    [TestCase("transition: opacity 0.2s steps(4)", "R007")]
    [TestCase("transition: opacity 0.2s step-end", "R007")]
    [TestCase("transition: boxy 0.2s", "C023")]
    [TestCase("transition: opacity 200 ease", "R009")]
    [TestCase("transition: opacity 1s 2s 3s", "R009")]
    [TestCase("transition: opacity foo 1s", "R009")]
    [TestCase("transition: width 0.2s", "R008")]
    [TestCase("transition: height 0.2s", "R008")]
    [TestCase("transition: margin-left 0.2s", "R008")]
    [TestCase("transition: left 0.2s", "R008")]
    [TestCase("transition: background-image 0.2s", "R006")]
    [TestCase("transition: display 0.2s", "R006")]
    [TestCase("transition: position 0.2s", "R006")]
    [TestCase("transition: visibility 0.2s", "R006")]
    [TestCase("transition: transform 0.2s", "")]
    [TestCase("transition-property: opacity", "")]
    [TestCase("transition-property: boxy", "C023")]
    [TestCase("transition-property: width", "R008")]
    [TestCase("transition-property: background-image", "R006")]
    [TestCase("transition-duration: 0.2s", "")]
    [TestCase("transition-duration: 200", "R009")]
    [TestCase("transition-delay: 50ms", "")]
    [TestCase("transition-delay: soon", "R009")]
    [TestCase("transition-timing-function: ease-out", "")]
    [TestCase("transition-timing-function: cubic-bezier(0, 0, 1, 1)", "R007")]
    [TestCase("transition-timing-function: wobble", "R007")]
    [TestCase("transition: opacity var(--t)", "")]
    public void TransitionDeclarationsAreChecked(string declaration, string expectedRules)
    {
        MiniAnalysis m = Q.Decl(declaration);
        var found = m.Bag.Items.Select(d => d.RuleId).Where(id => id == "R006" || id == "R007" || id == "R008" || id == "R009" || id == "C023").Distinct().OrderBy(x => x, StringComparer.Ordinal);
        Assert.That(string.Join(" ", found), Is.EqualTo(expectedRules), Q.Ids(m));
    }

    [Test]
    public void WidthAndBackgroundImageReportBothRules()
    {
        MiniAnalysis m = Q.Decl("transition: width 0.2s linear 50ms, background-image 0.2s");
        Assert.That(Q.Has(m, "R008"), Is.True);
        Assert.That(Q.Has(m, "R006"), Is.True);
    }

    [Test]
    public void EasingAdviceNamesTheNearestKeyword()
    {
        Assert.That(Q.First(Q.Decl("transition: opacity 1s cubic-bezier(0.4,0,0.2,1)"), "R007").Suggestion, Does.Contain("ease-out-cubic"));
        Assert.That(Q.First(Q.Decl("transition: opacity 1s steps(4)"), "R007").Suggestion, Does.Contain("stepped"));
    }

    [Test]
    public void TransitionDiagnosticsAreAttachedToTheDeclarationLine()
    {
        MiniAnalysis m = Q.Css(".x {\n  color: red;\n  transition: opacity 1s cubic-bezier(0,0,1,1);\n}");
        Assert.That(Q.First(m, "R007").Span.Start.Line, Is.EqualTo(4));
    }

    [Test]
    public void RuleDisableSilencesTransitionRules()
    {
        Assert.That(Q.Has(Q.Decl("transition: width 1s", Q.Cfg("{\"rules\":{\"disable\":[\"R008\"]}}")), "R008"), Is.False);
    }
}

[TestFixture]
public class AnimationRuleTests
{
    private const string Pop = "@keyframes pop { from { opacity: 0; } to { opacity: 1; } }\n";

    [TestCase("animation: pop 1s", "")]
    [TestCase("animation: pop 1s ease-out forwards", "")]
    [TestCase("animation: pop 300ms 100ms 3 alternate", "")]
    [TestCase("animation: missing 1s", "A001")]
    [TestCase("animation: pop 1s cubic-bezier(0, 0, 1, 1)", "R007")]
    [TestCase("animation: pop 1s 2s 3s", "A006")]
    [TestCase("animation: pop 1s foo bar", "A006")]
    [TestCase("animation: pop -1s", "A006")]
    [TestCase("animation: pop 1s -2", "A006")]
    [TestCase("animation: pop 1s infinite", "A005")]
    [TestCase("animation: pop 1s infinite; --anim-reduced-motion: skip", "")]
    [TestCase("animation: pop 1s infinite; --anim-reduced-motion: simplify", "")]
    [TestCase("animation: pop 1s infinite; --anim-reduced-motion: bogus", "A005")]
    [TestCase("animation-name: pop", "")]
    [TestCase("animation-name: missing", "A001")]
    [TestCase("animation-name: none", "")]
    [TestCase("animation-duration: 1s", "")]
    [TestCase("animation-duration: 1", "A006")]
    [TestCase("animation-duration: -1s", "A006")]
    [TestCase("animation-delay: 100ms", "")]
    [TestCase("animation-delay: now", "A006")]
    [TestCase("animation-iteration-count: 3", "")]
    [TestCase("animation-iteration-count: infinite", "A005")]
    [TestCase("animation-iteration-count: 1.5", "A006")]
    [TestCase("animation-iteration-count: -1", "A006")]
    [TestCase("animation-timing-function: ease-out", "")]
    [TestCase("animation-timing-function: steps(3)", "R007")]
    public void AnimationDeclarationsAreChecked(string declaration, string expectedRules)
    {
        MiniAnalysis m = Q.Css(Pop + ".x { " + declaration + " }");
        var found = m.Bag.Items.Select(d => d.RuleId).Where(id => id.StartsWith("A") || id == "R007").Distinct().OrderBy(x => x, StringComparer.Ordinal);
        Assert.That(string.Join(" ", found), Is.EqualTo(expectedRules), Q.Ids(m));
    }

    [TestCase("@keyframes k { from { width: 1px; } to { width: 2px; } }", "A003")]
    [TestCase("@keyframes k { from { height: 1px; } to { height: 2px; } }", "A003")]
    [TestCase("@keyframes k { from { margin-left: 1px; } to { margin-left: 2px; } }", "A003")]
    [TestCase("@keyframes k { from { box-shadow: none; } to { box-shadow: 0 0 1px red; } }", "A002")]
    [TestCase("@keyframes k { from { display: none; } to { display: flex; } }", "A002")]
    [TestCase("@keyframes k { from { transform: none; } to { transform: rotate(1deg); } }", "A002")]
    [TestCase("@keyframes k { from { opacity: 0; } to { opacity: 1; } }", "")]
    [TestCase("@keyframes k { from { translate: 0 0; } to { translate: 0 8px; } }", "")]
    [TestCase("@keyframes k { from { scale: 1; } to { scale: 2; } }", "")]
    [TestCase("@keyframes k { from { rotate: 0deg; } to { rotate: 90deg; } }", "")]
    [TestCase("@keyframes k { from { background-color: red; } to { background-color: blue; } }", "")]
    [TestCase("@keyframes k { from { visibility: hidden; } to { visibility: visible; } }", "")]
    [TestCase("@keyframes k { from { opacity: 0; --x: 1; } to { opacity: 1; } }", "")]
    public void KeyframePropertiesAreChecked(string css, string expectedRules)
    {
        MiniAnalysis m = Q.Css(css);
        var found = m.Bag.Items.Select(d => d.RuleId).Where(id => id.StartsWith("A")).Distinct().OrderBy(x => x, StringComparer.Ordinal);
        Assert.That(string.Join(" ", found), Is.EqualTo(expectedRules), Q.Ids(m));
    }

    [Test]
    public void LayoutPropertyIsReportedOncePerKeyframesAtItsFirstDeclaration()
    {
        MiniAnalysis m = Q.Css("@keyframes k {\n from { width: 1px; }\n 50% { width: 5px; }\n to { width: 9px; }\n}");
        Assert.That(Q.Count(m, "A003"), Is.EqualTo(1));
        Assert.That(Q.First(m, "A003").Span.Start.Line, Is.EqualTo(3));
    }

    [Test]
    public void LayoutAnimationCanBeAllowed()
    {
        MiniAnalysis m = Q.Css("@keyframes k { from { width: 1px; } to { width: 2px; } }", "<div></div>", Q.Cfg("{\"rules\":{\"allowLayoutAnimation\":true}}"));
        Assert.That(Q.Has(m, "A003"), Is.False);
    }

    [Test]
    public void InvalidKeyframesDoNotReportC007()
    {
        Assert.That(Q.Has(Q.Css("@keyframes k { from { width: 1px; } to { width: 2px; } }"), "C007"), Is.False);
        Assert.That(Q.Has(Q.Css("@keyframes k { from { opacity: 0; } to { opacity: 1; } }"), "C007"), Is.True);
    }

    [Test]
    public void ValidKeyframesReportAWarningWithTheRuntimeComponent()
    {
        Diagnostic d = Q.First(Q.Css("@keyframes k { from { opacity: 0; } to { opacity: 1; } }"), "C007");
        Assert.That(d.Severity, Is.EqualTo(Severity.Warning));
        Assert.That(d.Tier, Is.EqualTo(Tier.Runtime));
        Assert.That(d.EmulationId, Is.EqualTo("keyframes-animator"));
    }

    [TestCase(8, false)]
    [TestCase(9, true)]
    public void TooManyStepsIsA004(int steps, bool expected)
    {
        string blocks = string.Join(" ", Enumerable.Range(0, steps).Select(i => (i * 100 / Math.Max(1, steps - 1)) + "% { opacity: 0.5; }"));
        Assert.That(Q.Has(Q.Css("@keyframes k { " + blocks + " }"), "A004"), Is.EqualTo(expected));
    }

    [Test]
    public void StepLimitComesFromTheConfig()
    {
        string css = "@keyframes k { 0% { opacity: 0; } 50% { opacity: 1; } 100% { opacity: 0; } }";
        Assert.That(Q.Has(Q.Css(css, "<div></div>", Q.Cfg("{\"rules\":{\"maxKeyframeSteps\":2}}")), "A004"), Is.True);
        Assert.That(Q.Has(Q.Css(css), "A004"), Is.False);
    }

    [Test]
    public void DeclarationsWithoutAnimationNamesAreIgnored()
    {
        Assert.That(Q.Has(Q.Decl("animation: 1s"), "A001"), Is.False);
    }

    [Test]
    public void ReducedMotionAppliesPerRule()
    {
        MiniAnalysis m = Q.Css(Pop + ".a { animation: pop 1s infinite; }\n.b { animation: pop 1s infinite; --anim-reduced-motion: skip; }");
        Assert.That(Q.Count(m, "A005"), Is.EqualTo(1));
        Assert.That(Q.First(m, "A005").Span.Start.Line, Is.EqualTo(3));
    }
}
