using System;
using System.Collections.Generic;
using System.Linq;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class DetectorTests
{
    [TestCase(".a { color: red; }", "")]
    [TestCase("div { color: red; }", "selector:type")]
    [TestCase(".a { display: grid; }", "display:grid")]
    [TestCase(".a { display: inline-grid; }", "display:grid")]
    [TestCase(".a { display: flex; }", "")]
    [TestCase(".a { display: none; }", "")]
    [TestCase(".a { gap: 4px; }", "prop:gap")]
    [TestCase(".a { row-gap: 4px; }", "prop:gap")]
    [TestCase(".a { column-gap: 4px; }", "prop:gap")]
    [TestCase(".a { grid-gap: 4px; }", "prop:gap")]
    [TestCase(".a { display: grid; gap: 4px; }", "display:grid")]
    [TestCase(".a:not(.b) { color: red; }", "pseudo:not")]
    [TestCase(".a:is(.b, .c) { color: red; }", "pseudo:is")]
    [TestCase(".a:where(.b) { color: red; }", "pseudo:where")]
    [TestCase(".a:has(.b) { color: red; }", "pseudo:has")]
    [TestCase(".a:hover { color: red; }", "")]
    [TestCase(".a:active { color: red; }", "")]
    [TestCase(".a:focus { color: red; }", "")]
    [TestCase(".a:disabled { color: red; }", "")]
    [TestCase(".a:checked { color: red; }", "")]
    [TestCase(":root { --c: red; }", "")]
    [TestCase(".a:first-child { color: red; }", "pseudo:first-child")]
    [TestCase(".a:last-child { color: red; }", "pseudo:last-child")]
    [TestCase(".a:only-child { color: red; }", "pseudo:only-child")]
    [TestCase(".a:nth-child(2n+1) { color: red; }", "pseudo:nth-child")]
    [TestCase(".a:nth-of-type(2) { color: red; }", "pseudo:nth-of-type")]
    [TestCase(".a:empty { color: red; }", "pseudo:empty")]
    [TestCase(".a:focus-visible { color: red; }", "pseudo:focus-visible")]
    [TestCase(".a:visited { color: red; }", "pseudo:visited")]
    [TestCase(".a::before { color: red; }", "pseudo-element:before")]
    [TestCase(".a::after { color: red; }", "pseudo-element:after")]
    [TestCase(".a:before { color: red; }", "pseudo-element:before")]
    [TestCase(".a::placeholder { color: red; }", "pseudo-element:placeholder")]
    [TestCase(".a + .b { color: red; }", "selector:sibling")]
    [TestCase(".a ~ .b { color: red; }", "selector:sibling")]
    [TestCase(".a > .b { color: red; }", "")]
    [TestCase(".a .b { color: red; }", "")]
    [TestCase(".a[type=\"text\"] { color: red; }", "selector:attribute")]
    [TestCase("* { color: red; }", "")]
    [TestCase("#a { color: red; }", "")]
    public void SelectorAndDisplayConstructs(string css, string keys)
    {
        Assert.That(Q.Keys(Q.Css(css)), Is.EqualTo(keys));
    }

    [TestCase(".a { width: 10px; }", "")]
    [TestCase(".a { width: 10vw; }", "unit:vw")]
    [TestCase(".a { width: 10vh; }", "unit:vh")]
    [TestCase(".a { width: 10vmin; }", "unit:vmin")]
    [TestCase(".a { width: 10vmax; }", "unit:vmax")]
    [TestCase(".a { width: 1em; }", "unit:em")]
    [TestCase(".a { width: 1rem; }", "unit:rem")]
    [TestCase(".a { width: 1ch; }", "unit:ch")]
    [TestCase(".a { width: 1pt; }", "unit:pt")]
    [TestCase(".a { width: 1fr; }", "unit:fr")]
    [TestCase(".a { width: 50%; }", "")]
    [TestCase(".a { rotate: 10deg; }", "")]
    [TestCase(".a { transition-duration: 200ms; }", "")]
    [TestCase(".a { margin: 1em 2rem; }", "unit:em unit:rem")]
    [TestCase(".a { --gap: 10vw; }", "")]
    [TestCase(".a { width: calc(1px + 2px); }", "fn:calc")]
    [TestCase(".a { width: calc(100% - 4px); }", "fn:calc")]
    [TestCase(".a { width: min(10px, 20px); }", "fn:min")]
    [TestCase(".a { width: max(10px, 20px); }", "fn:max")]
    [TestCase(".a { width: clamp(1px, 2px, 3px); }", "fn:clamp")]
    [TestCase(".a { padding-left: calc(2rem + 8px); }", "fn:calc")]
    [TestCase(".a { width: var(--w); }", "")]
    public void UnitAndMathConstructs(string css, string keys)
    {
        Assert.That(Q.Keys(Q.Css(css)), Is.EqualTo(keys));
    }

    [TestCase("calc(1px + 2px)", "true")]
    [TestCase("calc(100% - 4px)", "false")]
    [TestCase("calc(2 * 8px)", "true")]
    [TestCase("calc(10px / 2)", "true")]
    [TestCase("calc(10px * 2px)", "false")]
    [TestCase("calc(2rem + 8px)", "true")]
    [TestCase("calc(50% + 50%)", "true")]
    [TestCase("min(10px, 20px)", "true")]
    [TestCase("max(10px, 5%)", "false")]
    [TestCase("clamp(1px, 2px, 3px)", "true")]
    [TestCase("clamp(1px, 2px)", "false")]
    [TestCase("calc(var(--a) + 1px)", "false")]
    [TestCase("calc((1px + 2px) * 2)", "true")]
    [TestCase("calc(1px + (2px + 3px))", "true")]
    public void CalcFoldabilityFollowsUnits(string expression, string foldable)
    {
        Assert.That(CalcAnalyzer.CanFold(expression), Is.EqualTo(foldable == "true"));
    }

    [Test]
    public void CalcWithoutParenthesesIsNotFoldable()
    {
        Assert.That(CalcAnalyzer.CanFold(""), Is.False);
        Assert.That(CalcAnalyzer.CanFold("1px + 2px"), Is.False);
        Assert.That(CalcAnalyzer.CanFold("calc(1px +"), Is.False);
    }

    [TestCase(".a { background-image: linear-gradient(to right, #000000, #ffffff); }", "fn:linear-gradient", "true")]
    [TestCase(".a { background-image: linear-gradient(90deg, #000000 10%, #ffffff 90%); }", "fn:linear-gradient", "true")]
    [TestCase(".a { background-image: linear-gradient(#ff0000, blue); }", "fn:linear-gradient", "true")]
    [TestCase(".a { background-image: linear-gradient(in oklch, red, blue); }", "fn:linear-gradient", "false")]
    [TestCase(".a { background-image: linear-gradient(red); }", "fn:linear-gradient", "false")]
    [TestCase(".a { background-image: linear-gradient(1turn, red, blue); }", "fn:linear-gradient", "false")]
    [TestCase(".a { background-image: radial-gradient(circle, #000000, #ffffff); }", "fn:radial-gradient", "false")]
    [TestCase(".a { background-image: conic-gradient(red, blue); }", "fn:conic-gradient", "false")]
    [TestCase(".a { background-image: repeating-linear-gradient(red, blue 10px); }", "fn:repeating-linear-gradient", "false")]
    public void GradientConstructsCarryTheSubsetFlag(string css, string key, string supported)
    {
        MiniAnalysis m = Q.Css(css);
        Assert.That(Q.Keys(m), Is.EqualTo(key));
        Assert.That(m.Constructs[0].Data["supported"], Is.EqualTo(supported));
    }

    [TestCase(".a { box-shadow: 0 0 4px #000000; }", "shadow:outer")]
    [TestCase(".a { box-shadow: inset 0 0 4px #000000; }", "shadow:inset")]
    [TestCase(".a { box-shadow: none; }", "")]
    [TestCase(".a { box-shadow: 0 0 4px #000000, inset 0 0 2px #ffffff; }", "shadow:inset shadow:outer")]
    [TestCase(".a { filter: blur(4px); }", "filter:blur")]
    [TestCase(".a { backdrop-filter: blur(4px); }", "filter:blur")]
    [TestCase(".a { filter: none; }", "")]
    [TestCase(".a { filter: blur(2px) brightness(1.2); }", "filter:blur filter:brightness")]
    [TestCase(".a { filter: url(x.svg#f); }", "filter:url")]
    [TestCase(".a { overflow: auto; }", "overflow:auto")]
    [TestCase(".a { overflow: scroll; }", "overflow:scroll")]
    [TestCase(".a { overflow: hidden; }", "")]
    [TestCase(".a { overflow: visible; }", "")]
    [TestCase(".a { overflow-x: auto; }", "overflow:axis")]
    [TestCase(".a { overflow-x: auto; overflow-y: scroll; }", "overflow:axis")]
    [TestCase(".a { position: fixed; }", "position:fixed")]
    [TestCase(".a { position: sticky; }", "position:sticky")]
    [TestCase(".a { position: static; }", "position:static")]
    [TestCase(".a { position: absolute; }", "")]
    [TestCase(".a { position: relative; }", "")]
    [TestCase(".a { z-index: 3; }", "prop:z-index")]
    public void EffectConstructs(string css, string keys)
    {
        Assert.That(Q.Keys(Q.Css(css)), Is.EqualTo(keys));
    }

    [TestCase(".a { transform: rotate(10deg); }", "transform:single")]
    [TestCase(".a { transform: translateX(4px); }", "transform:single")]
    [TestCase(".a { transform: scale(1.1); }", "transform:single")]
    [TestCase(".a { transform: translate(4px, 2px) rotate(3deg); }", "transform:multi")]
    [TestCase(".a { transform: translate(4px, 2px) rotate(3deg) scale(2); }", "transform:multi")]
    [TestCase(".a { transform: matrix(1, 0, 0, 1, 0, 0); }", "transform:unsupported")]
    [TestCase(".a { transform: skewX(4deg); }", "transform:unsupported")]
    [TestCase(".a { transform: rotate3d(1, 1, 1, 4deg); }", "transform:unsupported")]
    [TestCase(".a { transform: none; }", "")]
    [TestCase(".a { color: inherit; }", "keyword:inherit")]
    [TestCase(".a { color: unset; }", "keyword:unset")]
    [TestCase(".a { color: revert; }", "keyword:revert")]
    [TestCase(".a { color: initial; }", "")]
    [TestCase(".a { inset: 0; }", "shorthand:inset")]
    [TestCase(".a { flex-flow: row wrap; }", "shorthand:flex-flow")]
    [TestCase(".a { border: 1px solid red; }", "shorthand:border")]
    [TestCase(".a { border-top: 1px solid red; }", "shorthand:border-top")]
    [TestCase(".a { animation: x 1s; }", "anim:declaration")]
    [TestCase(".a { animation-name: x; }", "anim:declaration")]
    [TestCase(".a { animation-duration: 1s; }", "anim:declaration")]
    public void TransformKeywordAndShorthandConstructs(string css, string keys)
    {
        Assert.That(Q.Keys(Q.Css(css)), Is.EqualTo(keys));
    }

    [TestCase("@media (max-width: 900px) { .a { color: red; } }", "at:media", "")]
    [TestCase("@media screen and (min-width: 600px) and (max-width: 900px) { .a { color: red; } }", "at:media", "")]
    [TestCase("@media (orientation: portrait) { .a { color: red; } }", "at:media", "")]
    [TestCase("@media (min-aspect-ratio: 16/9) { .a { color: red; } }", "at:media", "")]
    [TestCase("@media (width >= 600px) { .a { color: red; } }", "at:media", "")]
    [TestCase("@media print { .a { color: red; } }", "at:media", "print")]
    [TestCase("@media (hover: none) { .a { color: red; } }", "at:media", "(hover: none)")]
    [TestCase("@media (prefers-color-scheme: dark) { .a { color: red; } }", "at:media", "(prefers-color-scheme: dark)")]
    [TestCase("@media (min-orientation: portrait) { .a { color: red; } }", "at:media", "(min-orientation: portrait)")]
    public void MediaQueriesRecordTheirFirstUnsupportedFeature(string css, string keys, string unsupported)
    {
        MiniAnalysis m = Q.Css(css);
        Assert.That(Q.Keys(m), Is.EqualTo(keys));
        Construct media = m.Constructs.First(c => c.Key == "at:media");
        string actual;
        media.Data.TryGetValue("feature", out actual);
        Assert.That(actual ?? "", Is.EqualTo(unsupported));
    }

    [TestCase("@font-face { font-family: X; src: url(x.ttf); }", "at:font-face")]
    [TestCase("@supports (display: grid) { .a { color: red; } }", "at:supports")]
    [TestCase("@layer base { .a { color: red; } }", "at:layer")]
    [TestCase("@container (min-width: 10px) { .a { color: red; } }", "at:container")]
    [TestCase("@page { margin: 1cm; }", "at:page")]
    [TestCase("@charset \"utf-8\";", "")]
    public void OtherAtRules(string css, string keys)
    {
        Assert.That(Q.Keys(Q.Css(css)), Does.StartWith(keys));
    }

    [Test]
    public void RulesInsideAtRulesAreScannedToo()
    {
        Assert.That(Q.Keys(Q.Css("@media (max-width: 900px) { .a:not(.b) { gap: 4px; } }")), Is.EqualTo("at:media prop:gap pseudo:not"));
    }

    [TestCase("@keyframes k { from { opacity: 0; } to { opacity: 1; } }", "false")]
    [TestCase("@keyframes k { from { translate: 0 0; } to { translate: 0 8px; } }", "false")]
    [TestCase("@keyframes k { from { width: 1px; } to { width: 2px; } }", "true")]
    [TestCase("@keyframes k { from { box-shadow: none; } to { box-shadow: 0 0 1px red; } }", "true")]
    [TestCase("@keyframes k { from { display: none; } to { display: flex; } }", "true")]
    [TestCase("@keyframes k { from { margin-left: 0; } to { margin-left: 4px; } }", "true")]
    public void KeyframesAreMarkedInvalidWhenTheyAnimateLayoutOrUnknownProperties(string css, string invalid)
    {
        MiniAnalysis m = Q.Css(css);
        Construct keyframes = m.Constructs.First(c => c.Key == "at:keyframes");
        string value;
        keyframes.Data.TryGetValue("invalid", out value);
        Assert.That(value == "true", Is.EqualTo(invalid == "true"));
    }

    [Test]
    public void LayoutAnimationCanBeAllowedByConfig()
    {
        MiniAnalysis m = Q.Css("@keyframes k { from { width: 1px; } to { width: 2px; } }", "<div name=\"a\"></div>", Q.Cfg("{\"rules\":{\"allowLayoutAnimation\":true}}"));
        Assert.That(m.Constructs.First(c => c.Key == "at:keyframes").Data.ContainsKey("invalid"), Is.False);
    }

    [TestCase(".a { display: grid; grid-template-columns: repeat(auto-fill, minmax(10px, 1fr)); }", "auto-fill")]
    [TestCase(".a { display: grid; grid-template-columns: repeat(auto-fit, 1fr); }", "auto-fit")]
    [TestCase(".a { display: grid; grid-template-columns: minmax(10px, 1fr) 1fr; }", "minmax")]
    [TestCase(".a { display: grid; grid-template-columns: subgrid; }", "subgrid")]
    [TestCase(".a { display: grid; grid-auto-flow: row dense; }", "dense")]
    public void GridConstructsRecordUnresolvableTemplates(string css, string feature)
    {
        Assert.That(Q.Css(css).Constructs.First().Data["unresolvable"], Is.EqualTo(feature));
    }

    [Test]
    public void GridConstructsRecordTheTrackCount()
    {
        Assert.That(Q.Css(".a { display: grid; grid-template-columns: repeat(30, 1fr); }").Constructs.First().Data["tracks"], Is.EqualTo("30"));
        Assert.That(Q.Css(".a { display: grid; grid-template-columns: 1fr 1fr; }").Constructs.First().Data.ContainsKey("tracks"), Is.False);
    }

    [Test]
    public void GapInAWrapRuleIsApproximate()
    {
        Assert.That(Q.Css(".a { flex-wrap: wrap; gap: 4px; }").Constructs.First().Data["approximate"], Is.EqualTo("true"));
        Assert.That(Q.Css(".a { flex-wrap: nowrap; gap: 4px; }").Constructs.First().Data.ContainsKey("approximate"), Is.False);
    }

    [Test]
    public void TransformOrderProblemsAreRecorded()
    {
        Assert.That(Q.Css(".a { transform: rotate(1deg) translate(1px, 2px); }").Constructs.First().Data.ContainsKey("unresolvable"), Is.True);
        Assert.That(Q.Css(".a { transform: scale(2) rotate(1deg); }").Constructs.First().Data.ContainsKey("unresolvable"), Is.True);
        Assert.That(Q.Css(".a { transform: rotate(1deg) rotate(2deg); }").Constructs.First().Data["unresolvable"], Does.Contain("repeated"));
        Assert.That(Q.Css(".a { transform: translate(1px, 2px) scale(2); }").Constructs.First().Data.ContainsKey("unresolvable"), Is.False);
    }

    [TestCase(".a::before { content: \"\"; }", "empty")]
    [TestCase(".a::before { content: \"→\"; }", "glyph")]
    [TestCase(".a::after { content: \"NEW\"; }", "text")]
    [TestCase(".a::after { content: attr(data-text-after); }", "attr")]
    [TestCase(".a::after { color: red; }", "none")]
    [TestCase(".a::after { content: counter(x); }", "other")]
    public void PseudoElementContentIsClassified(string css, string kind)
    {
        Assert.That(Q.Css(css).Constructs.First(c => c.Key.StartsWith("pseudo-element:")).Data["content"], Is.EqualTo(kind));
    }

    [Test]
    public void GlyphOnlyCanBeSwitchedOff()
    {
        MiniAnalysis m = Q.Css(".a::before { content: \"→\"; }", "<div></div>", Q.Cfg("{\"localization\":{\"allowGlyphOnly\":false}}"));
        Assert.That(m.Constructs.First().Data["content"], Is.EqualTo("text"));
    }

    [Test]
    public void ConstructsCarrySpansAndText()
    {
        MiniAnalysis m = Q.Css(".a {\n  display: grid;\n}");
        Construct c = m.Constructs.Single();
        Assert.That(c.Span.Start.Line, Is.EqualTo(3));
        Assert.That(c.Data["text"], Is.EqualTo("display: grid"));
        Assert.That(c.Kind, Is.EqualTo(ConstructKind.Value));
    }

    [Test]
    public void SelectorConstructsUseTheSelectorSpan()
    {
        MiniAnalysis m = Q.Css("\n\n.a:not(.b) { color: red; }");
        Assert.That(m.Constructs.Single().Span.Start.Line, Is.EqualTo(4));
        Assert.That(m.Constructs.Single().Kind, Is.EqualTo(ConstructKind.Selector));
    }

    [Test]
    public void EachSelectorOfAListIsDetectedSeparately()
    {
        Assert.That(Q.Keys(Q.Css(".a:first-child, .b:last-child { color: red; }")), Is.EqualTo("pseudo:first-child pseudo:last-child"));
    }

    [Test]
    public void SkinSheetsAreNotScanned()
    {
        var files = new Dictionary<string, string> { { "skin.uss", ".a { display: grid; gap: 2px; }" } };
        MiniAnalysis m = MiniAnalyzer.Analyze(new[] { ("page.html", "<html><head><link rel=\"stylesheet\" href=\"skin.uss\" data-uss=\"x\"></head><body></body></html>") }, null, null, null, null, files);
        Assert.That(Q.Keys(m), Is.EqualTo(""));
    }

    [Test]
    public void LinkedSheetsAreScanned()
    {
        var files = new Dictionary<string, string> { { "a.css", ".a { display: grid; }" } };
        MiniAnalysis m = MiniAnalyzer.Analyze(new[] { ("page.html", "<html><head><link rel=\"stylesheet\" href=\"a.css\"></head><body></body></html>") }, null, null, null, null, files);
        Assert.That(Q.Keys(m), Is.EqualTo("display:grid"));
    }

    [Test]
    public void DefaultDetectorListHasThreeDetectors()
    {
        Assert.That(ConstructDetectors.Default().Count, Is.EqualTo(3));
    }

    [Test]
    public void DetectAllUsesTheGivenDetectors()
    {
        MiniAnalysis m = Q.Css(".a:not(.b) { display: grid; }");
        Assert.That(ConstructDetectors.DetectAll(m.Input, new IConstructDetector[] { new SelectorConstructDetector() }).Count, Is.EqualTo(1));
        Assert.That(ConstructDetectors.DetectAll(m.Input).Count, Is.EqualTo(2));
    }
}
