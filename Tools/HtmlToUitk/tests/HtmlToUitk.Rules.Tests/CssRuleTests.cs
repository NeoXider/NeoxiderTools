using System;
using System.Linq;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class CssRuleTests
{
    [TestCase("background: url(a.png)", "C012", true)]
    [TestCase("background: red url(a.png)", "C012", true)]
    [TestCase("background-clip: padding-box", "C012", true)]
    [TestCase("background-origin: border-box", "C012", true)]
    [TestCase("background-attachment: fixed", "C012", true)]
    [TestCase("background-image: url(a.png), url(b.png)", "C012", true)]
    [TestCase("background: #ffffff", "C012", false)]
    [TestCase("background: transparent", "C012", false)]
    [TestCase("background: none", "C012", false)]
    [TestCase("background-image: none", "C012", false)]
    [TestCase("background-image: url(a.png)", "C012", false)]
    [TestCase("background-color: red", "C012", false)]
    [TestCase("pointer-events: none", "C014", true)]
    [TestCase("text-transform: uppercase", "C014", true)]
    [TestCase("line-height: 1.5", "C014", true)]
    [TestCase("mix-blend-mode: multiply", "C014", true)]
    [TestCase("-webkit-foo: 1", "C014", true)]
    [TestCase("clip-path: circle(10px)", "C014", true)]
    [TestCase("float: left", "C014", true)]
    [TestCase("content: \"x\"", "C014", true)]
    [TestCase("cursor: pointer", "C014", false)]
    [TestCase("opacity: 0.5", "C014", false)]
    [TestCase("-unity-font-style: bold", "C014", false)]
    [TestCase("font-weight: bold", "C014", false)]
    [TestCase("font-style: italic", "C014", false)]
    [TestCase("gap: 4px", "C014", false)]
    [TestCase("z-index: 2", "C014", false)]
    [TestCase("box-shadow: 0 0 2px red", "C014", false)]
    [TestCase("grid-template-columns: 1fr 1fr", "C014", false)]
    [TestCase("grid-column: span 2", "C014", false)]
    [TestCase("transform: rotate(10deg)", "C014", false)]
    [TestCase("inset: 0", "C014", false)]
    [TestCase("animation: x 1s", "C014", false)]
    [TestCase("--custom: 4px", "C014", false)]
    [TestCase("color: red !important", "C017", true)]
    [TestCase("color: red", "C017", false)]
    [TestCase("color: inherit", "C018", true)]
    [TestCase("color: unset", "C018", true)]
    [TestCase("color: revert", "C018", true)]
    [TestCase("color: initial", "C018", false)]
    [TestCase("display: block", "C019", true)]
    [TestCase("display: inline", "C019", true)]
    [TestCase("display: table", "C019", true)]
    [TestCase("display: flex", "C019", false)]
    [TestCase("display: none", "C019", false)]
    [TestCase("display: grid", "C019", false)]
    [TestCase("box-sizing: content-box", "C025", true)]
    [TestCase("box-sizing: border-box", "C025", false)]
    [TestCase("border-style: dashed", "C024", true)]
    [TestCase("border-style: dotted", "C024", true)]
    [TestCase("border-top-style: double", "C024", true)]
    [TestCase("border: 1px dashed red", "C024", true)]
    [TestCase("border-left: 2px groove red", "C024", true)]
    [TestCase("border: 1px solid red", "C024", false)]
    [TestCase("border-style: solid", "C024", false)]
    [TestCase("border-style: none", "C024", false)]
    [TestCase("text-shadow: 1px 1px red, 2px 2px blue", "C026", true)]
    [TestCase("text-shadow: 1px 1px red", "C026", false)]
    [TestCase("font-family: Georgia", "C031", true)]
    [TestCase("font-family: \"Rubik\", sans-serif", "C031", true)]
    [TestCase("text-align: center", "T001", true)]
    [TestCase("text-align: left", "T001", true)]
    [TestCase("text-align: end", "T001", true)]
    [TestCase("text-align: center; -unity-text-align: lower-right", "T001", false)]
    [TestCase("-unity-text-align: lower-right", "T001", false)]
    [TestCase("font-size: 10px", "R001", true)]
    [TestCase("font-size: 12px", "R001", true)]
    [TestCase("font-size: 13px", "R001", false)]
    [TestCase("font-size: 20px", "R001", false)]
    public void ValueRulesFireOnTheRightDeclarations(string declarations, string rule, bool expected)
    {
        Assert.That(Q.Has(Q.Decl(declarations), rule), Is.EqualTo(expected), Q.Ids(Q.Decl(declarations)));
    }

    [TestCase("width: 10", true)]
    [TestCase("width: 10px", false)]
    [TestCase("width: 0", false)]
    [TestCase("width: 50%", false)]
    [TestCase("width: auto", false)]
    [TestCase("width: abc", true)]
    [TestCase("height: 12", true)]
    [TestCase("min-width: 1.5", true)]
    [TestCase("max-width: none", false)]
    [TestCase("opacity: abc", true)]
    [TestCase("opacity: 0.5", false)]
    [TestCase("opacity: 50%", false)]
    [TestCase("opacity: 5px", true)]
    [TestCase("flex-grow: 1", false)]
    [TestCase("flex-grow: x", true)]
    [TestCase("color: hsl(120, 50%, 50%)", true)]
    [TestCase("color: hsla(120, 50%, 50%, 0.5)", true)]
    [TestCase("color: currentColor", true)]
    [TestCase("color: red", false)]
    [TestCase("color: #ff0000", false)]
    [TestCase("color: #f00", false)]
    [TestCase("color: rgb(1, 2, 3)", false)]
    [TestCase("color: rgba(1, 2, 3, 0.5)", false)]
    [TestCase("background-color: RED", false)]
    [TestCase("background-color: red blue", true)]
    [TestCase("border-color: red blue", false)]
    [TestCase("border-color: red blue green yellow", false)]
    [TestCase("border-color: red blue green yellow pink", true)]
    [TestCase("justify-content: banana", true)]
    [TestCase("justify-content: center", false)]
    [TestCase("justify-content: space-evenly", false)]
    [TestCase("align-items: stretch", false)]
    [TestCase("align-items: baseline", true)]
    [TestCase("align-content: auto", true)]
    [TestCase("flex-direction: row", false)]
    [TestCase("flex-direction: diagonal", true)]
    [TestCase("flex-wrap: wrap", false)]
    [TestCase("flex-wrap: nowrap", false)]
    [TestCase("white-space: nowrap", false)]
    [TestCase("white-space: pre-wrap", false)]
    [TestCase("white-space: break-spaces", true)]
    [TestCase("text-overflow: ellipsis", false)]
    [TestCase("text-overflow: fade", true)]
    [TestCase("visibility: hidden", false)]
    [TestCase("visibility: collapse", true)]
    [TestCase("-unity-text-align: middle-center", false)]
    [TestCase("-unity-text-align: center", true)]
    [TestCase("-unity-font-style: bold-and-italic", false)]
    [TestCase("-unity-slice-type: tiled", false)]
    [TestCase("margin: 1px 2px 3px 4px", false)]
    [TestCase("margin: 1px 2px 3px 4px 5px", true)]
    [TestCase("margin: auto", false)]
    [TestCase("padding: 4px 8px", false)]
    [TestCase("padding: 4", true)]
    [TestCase("border-width: 1px", false)]
    [TestCase("border-width: 50%", true)]
    [TestCase("border-radius: 4px", false)]
    [TestCase("border-radius: 4px 8px", false)]
    [TestCase("border-radius: 4px / 2px", true)]
    [TestCase("border-radius: auto", true)]
    [TestCase("font-size: 50%", true)]
    [TestCase("font-size: 16px", false)]
    [TestCase("letter-spacing: 2px", false)]
    [TestCase("letter-spacing: 5%", true)]
    [TestCase("rotate: 10deg", false)]
    [TestCase("rotate: 10", true)]
    [TestCase("rotate: 1turn", true)]
    [TestCase("scale: 2", false)]
    [TestCase("scale: 1.5 2", false)]
    [TestCase("scale: 2px", true)]
    [TestCase("translate: 10px 5px", false)]
    [TestCase("translate: 10 5", true)]
    [TestCase("translate: 0 0", false)]
    [TestCase("flex: auto", true)]
    [TestCase("flex: none", false)]
    [TestCase("flex: 1 1 0", false)]
    [TestCase("flex: 1 1 10px", false)]
    [TestCase("flex: banana", true)]
    [TestCase("text-align: banana", true)]
    [TestCase("font-weight: heavy", true)]
    [TestCase("font-weight: 700", false)]
    [TestCase("font-style: slanted", true)]
    [TestCase("transform: rotate(45)", true)]
    [TestCase("transform: rotate(45deg)", false)]
    [TestCase("transform: translate(10, 5)", true)]
    [TestCase("transform: scale(2px)", true)]
    [TestCase("inset: 0", false)]
    [TestCase("inset: 5", true)]
    [TestCase("flex-flow: row wrap", false)]
    [TestCase("flex-flow: row banana", true)]
    [TestCase("text-shadow: 1px 1px red", false)]
    [TestCase("text-shadow: 1 1 red", true)]
    [TestCase("background-image: banana", true)]
    [TestCase("transition: opacity 0.2s", false)]
    [TestCase("width: var(--w)", false)]
    [TestCase("width: calc(1px + 2px)", false)]
    public void InvalidValuesAreC023(string declarations, bool expected)
    {
        MiniAnalysis m = Q.Decl(declarations);
        Assert.That(Q.Has(m, "C023"), Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void FontFamilyIsAcceptedWhenMappedInTheConfig()
    {
        ConversionConfig cfg = Q.Cfg("{\"fonts\":{\"Rubik\":\"/Assets/Fonts/Rubik.asset\"}}");
        Assert.That(Q.Has(Q.Decl("font-family: \"Rubik\", sans-serif", cfg), "C031"), Is.False);
        Assert.That(Q.Has(Q.Decl("font-family: Rubik", cfg), "C031"), Is.False);
        Assert.That(Q.Has(Q.Decl("font-family: Other", cfg), "C031"), Is.True);
    }

    [Test]
    public void FontSizeMinimumComesFromTheConfig()
    {
        Assert.That(Q.Has(Q.Decl("font-size: 14px", Q.Cfg("{\"rules\":{\"minFontSize\":16}}")), "R001"), Is.True);
        Assert.That(Q.Has(Q.Decl("font-size: 14px", Q.Cfg("{\"rules\":{\"minFontSize\":12}}")), "R001"), Is.False);
    }

    [Test]
    public void ColourLiteralsAreOnlyCheckedInTokensOnlyMode()
    {
        ConversionConfig strict = Q.Cfg("{\"rules\":{\"colorLiterals\":\"tokens-only\"}}");
        Assert.That(Q.Has(Q.Decl("color: #ffffff", strict), "R004"), Is.True);
        Assert.That(Q.Has(Q.Decl("color: rgba(1, 2, 3, 0.5)", strict), "R004"), Is.True);
        Assert.That(Q.Has(Q.Decl("color: var(--c)", strict), "R004"), Is.False);
        Assert.That(Q.Has(Q.Decl("color: #ffffff"), "R004"), Is.False);
        Assert.That(Q.Has(Q.Css(":root { --c: #ffffff; }", "<div></div>", strict), "R004"), Is.False);
    }

    [Test]
    public void StrictColourModeReportsEachDeclaration()
    {
        ConversionConfig strict = Q.Cfg("{\"rules\":{\"colorLiterals\":\"tokens-only\"}}");
        Assert.That(Q.Count(Q.Decl("color: #fff; background-color: #000", strict), "R004"), Is.EqualTo(2));
    }

    [Test]
    public void ImportantIsReportedWithItsDeclaration()
    {
        MiniAnalysis m = Q.Decl("color: red !important");
        Assert.That(Q.First(m, "C017").Construct, Does.Contain("!important"));
    }

    [Test]
    public void ValueRuleDiagnosticsUseTheCatalogDefaults()
    {
        MiniAnalysis m = Q.Decl("width: 10");
        Diagnostic d = Q.First(m, "C023");
        Assert.That(d.Severity, Is.EqualTo(Severity.Error));
        Assert.That(d.Tier, Is.EqualTo(Tier.Reject));
        Assert.That(d.Why, Is.Not.Empty);
        Assert.That(d.Suggestion, Is.Not.Empty);
        Assert.That(d.Construct, Is.EqualTo("width: 10"));
    }

    [Test]
    public void C014CarriesTheAdviceForTheProperty()
    {
        Assert.That(Q.First(Q.Decl("z-index-like: 1"), "C014").Suggestion, Is.Not.Empty);
        Assert.That(Q.First(Q.Decl("text-transform: uppercase"), "C014").Suggestion, Does.Contain("string table"));
        Assert.That(Q.First(Q.Decl("-webkit-foo: 1"), "C014").Suggestion, Does.Contain("Vendor-prefixed"));
    }

    [Test]
    public void ContentIsAcceptedInsideAPseudoElementRule()
    {
        MiniAnalysis m = Q.Css(".x::before { content: \"\"; width: 4px; }");
        Assert.That(Q.Has(m, "C014"), Is.False);
    }

    [Test]
    public void DisabledRulesAreNotReported()
    {
        ConversionConfig cfg = Q.Cfg("{\"rules\":{\"disable\":[\"C023\",\"C014\"]}}");
        MiniAnalysis m = Q.Decl("width: 10; line-height: 2", cfg);
        Assert.That(Q.Has(m, "C023"), Is.False);
        Assert.That(Q.Has(m, "C014"), Is.False);
    }

    [Test]
    public void TokenRedefinitionIsC034()
    {
        var files = new System.Collections.Generic.Dictionary<string, string> { { "skin.uss", ":root { --accent: #112233; --same: #445566; }" } };
        string html = "<html><head><link rel=\"stylesheet\" href=\"skin.uss\" data-uss=\"skin.uss\"><style>:root { --accent: #ffffff; --same: #445566; }</style></head><body></body></html>";
        MiniAnalysis m = MiniAnalyzer.Analyze(new[] { ("page.html", html) }, null, null, null, null, files);
        Assert.That(Q.Count(m, "C034"), Is.EqualTo(1));
        Assert.That(Q.First(m, "C034").Construct, Does.StartWith("--accent"));
    }

    [Test]
    public void SameTokenValueWithDifferentWhitespaceIsNotC034()
    {
        var files = new System.Collections.Generic.Dictionary<string, string> { { "skin.uss", ":root { --a: rgba(1,2,3,0.5); }" } };
        string html = "<html><head><link rel=\"stylesheet\" href=\"skin.uss\" data-uss=\"skin.uss\"><style>:root { --a: rgba(1, 2, 3, 0.5); }</style></head><body></body></html>";
        Assert.That(Q.Has(MiniAnalyzer.Analyze(new[] { ("page.html", html) }, null, null, null, null, files), "C034"), Is.False);
    }

    [TestCase("url(\"https://example.com/a.png\")", true)]
    [TestCase("url(\"http://example.com/a.png\")", true)]
    [TestCase("url(\"data:image/png;base64,AAAA\")", true)]
    [TestCase("url(\"//cdn.example.com/a.png\")", true)]
    [TestCase("url(\"file:///a.png\")", true)]
    [TestCase("url(\"/Assets/a.png\")", false)]
    [TestCase("url(\"project://Assets/a.png\")", false)]
    public void ExternalImagesAreD012(string url, bool expected)
    {
        Assert.That(Q.Has(Q.Decl("background-image: " + url), "D012"), Is.EqualTo(expected));
    }

    [Test]
    public void MissingImageFilesAreD012WhenAFileSystemIsGiven()
    {
        var fs = Q.Fs(("images/ok.png", "x"));
        Assert.That(Q.Has(Q.Decl("background-image: url(\"images/ok.png\")", null, null, fs), "D012"), Is.False);
        Assert.That(Q.Has(Q.Decl("background-image: url(\"images/missing.png\")", null, null, fs), "D012"), Is.True);
        Assert.That(Q.Has(Q.Decl("background-image: url(\"images/missing.png\")"), "D012"), Is.False);
    }

    [Test]
    public void RelativeImagePathsResolveFromTheSheetFolder()
    {
        var fs = Q.Fs(("ui/images/ok.png", "x"));
        MiniAnalysis m = MiniAnalyzer.Html("<html><head><style>.x { background-image: url(\"images/ok.png\"); }</style></head><body></body></html>", "ui/page.html", null, null, fs);
        Assert.That(Q.Has(m, "D012"), Is.False);
        MiniAnalysis bad = MiniAnalyzer.Html("<html><head><style>.x { background-image: url(\"../images/ok.png\"); }</style></head><body></body></html>", "ui/page.html", null, null, fs);
        Assert.That(Q.Has(bad, "D012"), Is.True);
    }

    [Test]
    public void ConstructDiagnosticsFollowThePlan()
    {
        Assert.That(Q.Ids(Q.Decl("display: grid")), Is.EqualTo("C001:I:C"));
        Assert.That(Q.Ids(Q.Decl("position: sticky")), Is.EqualTo("C009:E:R"));
        Assert.That(Q.Ids(Q.Decl("z-index: 1")), Is.EqualTo("C021:I:C"));
        Assert.That(Q.Ids(Q.Decl("width: 10vw")), Is.EqualTo("C008:I:C"));
        Assert.That(Q.Ids(Q.Decl("color: inherit")), Is.EqualTo("C018:W:N"));
        Assert.That(Q.Ids(Q.Decl("color: revert")), Is.EqualTo("C018:E:R"));
        Assert.That(Q.Ids(Q.Decl("overflow: scroll")), Is.EqualTo("C020:I:C"));
        Assert.That(Q.Ids(Q.Decl("box-shadow: inset 0 0 1px red")), Is.EqualTo("C013:E:R"));
        Assert.That(Q.Ids(Q.Decl("box-shadow: 0 0 1px red")), Is.EqualTo("C013:W:N"));
        Assert.That(Q.Ids(Q.Decl("filter: blur(2px)")), Is.EqualTo("C010:W:N"));
    }

    [Test]
    public void GapWithWrapIsAWarning()
    {
        Assert.That(Q.Ids(Q.Decl("flex-wrap: wrap; gap: 4px")), Is.EqualTo("C002:W:C"));
        Assert.That(Q.Ids(Q.Decl("gap: 4px")), Is.EqualTo("C002:I:C"));
    }

    [Test]
    public void StrictNativeTurnsEmulationsIntoErrors()
    {
        MiniAnalysis m = Q.Decl("display: grid; width: 10vw", Q.Cfg("{\"emulation\":{\"strictNative\":true}}"));
        Assert.That(Q.Count(m, "C001"), Is.EqualTo(1));
        Assert.That(Q.First(m, "C001").Severity, Is.EqualTo(Severity.Error));
        Assert.That(Q.First(m, "C008").Severity, Is.EqualTo(Severity.Error));
        Assert.That(Q.First(m, "C001").EmulationId, Is.Null);
    }

    [Test]
    public void RejectedEmulationDiagnosticsExplainTheConfig()
    {
        MiniAnalysis m = Q.Decl("display: grid", Q.Cfg("{\"emulation\":{\"disable\":[\"grid-flatten\"]}}"));
        Assert.That(Q.First(m, "C001").Message, Does.Contain("grid-flatten"));
    }
}
