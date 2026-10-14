using System;
using System.Linq;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Rules;
using HtmlToUitk.TestSupport;
using NUnit.Framework;

namespace HtmlToUitk.Rules.Tests;

[TestFixture]
public class DialectRuleTests
{
    [TestCase("<table name=\"a\"></table>", "D001", true)]
    [TestCase("<ul name=\"a\"></ul>", "D001", true)]
    [TestCase("<strong name=\"a\">x</strong>", "D001", true)]
    [TestCase("<canvas name=\"a\"></canvas>", "D001", true)]
    [TestCase("<div name=\"a\"></div>", "D001", false)]
    [TestCase("<section name=\"a\"></section>", "D001", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">x</span>", "D001", false)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">x</button>", "D001", false)]
    [TestCase("<input type=\"date\" name=\"a\">", "D002", true)]
    [TestCase("<input type=\"file\" name=\"a\">", "D002", true)]
    [TestCase("<input type=\"text\" name=\"a\" data-loc=\"a.b\" data-text=\"x\">", "D002", false)]
    [TestCase("<input type=\"checkbox\" name=\"a\" data-loc=\"a.b\" data-text=\"x\">", "D002", false)]
    [TestCase("<input type=\"range\" name=\"a\" data-loc=\"a.b\" data-text=\"x\">", "D002", false)]
    [TestCase("<input name=\"a\" data-loc=\"a.b\" data-text=\"x\">", "D002", false)]
    [TestCase("<div name=\"a\" data-uxml=\"Bogus\"></div>", "D003", true)]
    [TestCase("<div name=\"a\" data-uxml=\"ScrollView\"></div>", "D003", false)]
    [TestCase("<div name=\"a\" data-uxml=\"Foldout\" data-loc=\"a.b\" data-text=\"x\"></div>", "D003", false)]
    [TestCase("<div name=\"a\" foo=\"1\"></div>", "D004", true)]
    [TestCase("<div name=\"a\" role=\"button\"></div>", "D004", true)]
    [TestCase("<div name=\"a\" data-anything=\"1\"></div>", "D004", false)]
    [TestCase("<div name=\"a\" class=\"c\" id=\"a\" hidden></div>", "D004", false)]
    [TestCase("<div name=\"a\" tabindex=\"0\"></div>", "D004", false)]
    [TestCase("<div name=\"a\" onclick=\"go()\"></div>", "D005", true)]
    [TestCase("<div name=\"a\" onmouseover=\"go()\"></div>", "D005", true)]
    [TestCase("<span name=\"a\" href=\"x\" data-loc=\"a.b\">x</span>", "D005", true)]
    [TestCase("<div name=\"a\"></div>", "D005", false)]
    [TestCase("<div name=\"a\">text</div>", "D006", true)]
    [TestCase("<section name=\"a\">text</section>", "D006", true)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">text</span>", "D006", false)]
    [TestCase("<div name=\"a\">   </div>", "D006", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">t<div name=\"b\"></div></span>", "D007", true)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">t<span name=\"b\" data-loc=\"a.c\">x</span></button>", "D007", false)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">t<span name=\"b\" data-loc=\"a.c\">x</span></button>", "D008", true)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">t</button>", "D008", false)]
    [TestCase("<div name=\"a\" style=\"color: red\"></div>", "D011", true)]
    [TestCase("<div name=\"a\" style=\"display: none\"></div>", "D011", false)]
    [TestCase("<div name=\"a\" style=\"display:none;\"></div>", "D011", false)]
    [TestCase("<div name=\"a\" style=\"DISPLAY: NONE\"></div>", "D011", false)]
    [TestCase("<img src=\"a.png\">", "D013", true)]
    [TestCase("<img name=\"a\" src=\"a.png\">", "D013", false)]
    [TestCase("<img id=\"a\" src=\"a.png\">", "D013", false)]
    [TestCase("<img name=\"a\" src=\"https://example.com/a.png\">", "D012", true)]
    [TestCase("<img name=\"a\" src=\"/Assets/a.png\">", "D012", false)]
    [TestCase("text directly in body", "D006", true)]
    [TestCase("<select name=\"a\"><option data-loc=\"a.b\">x</option></select>", "D001", false)]
    [TestCase("<select name=\"a\"><div></div></select>", "D001", true)]
    public void BodyConstructsAreChecked(string body, string rule, bool expected)
    {
        MiniAnalysis m = Q.Body(body);
        Assert.That(Q.Has(m, rule), Is.EqualTo(expected), Q.Ids(m));
    }

    [TestCase("<script>go()</script>", "D009", true)]
    [TestCase("<script src=\"app.js\"></script>", "D009", true)]
    [TestCase("<script src=\"uitk-validate.js\"></script>", "D009", false)]
    [TestCase("<script src=\"../assets/uitk-prototype.js\"></script>", "D009", false)]
    [TestCase("<script src=\"x.js\" data-uitk-harness></script>", "D009", false)]
    [TestCase("<link rel=\"stylesheet\" href=\"a.css\">", "D010", true)]
    [TestCase("<link rel=\"stylesheet\" href=\"a.css\" data-uitk-harness>", "D010", false)]
    [TestCase("<link rel=\"stylesheet\" href=\"a.css\" data-uss=\"../Skin/A.uss\">", "D010", false)]
    [TestCase("<link rel=\"icon\" href=\"a.png\">", "D010", false)]
    [TestCase("<style>.a { color: red; }</style>", "D010", false)]
    public void HeadConstructsAreChecked(string head, string rule, bool expected)
    {
        MiniAnalysis m = Q.Head(head, "<div name=\"a\"></div>");
        Assert.That(Q.Has(m, rule), Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void TagAdviceIsTheSuggestion()
    {
        Assert.That(Q.First(Q.Body("<table name=\"a\"></table>"), "D001").Suggestion, Does.Contain("Nested div"));
    }

    [Test]
    public void InlineStyleAllowListComesFromTheConfig()
    {
        string body = "<div name=\"a\" style=\"opacity: 0.5\"></div>";
        Assert.That(Q.Has(Q.Body(body), "D011"), Is.True);
        Assert.That(Q.Has(Q.Body(body, "", Q.Cfg("{\"uxml\":{\"allowInlineStyles\":[\"opacity: 0.5;\"]}}")), "D011"), Is.False);
    }

    [Test]
    public void MissingImageFileIsD012WithAFileSystem()
    {
        var fs = Q.Fs(("images/ok.png", "x"));
        Assert.That(Q.Has(Q.Body("<img name=\"a\" src=\"images/ok.png\">", "", null, fs), "D012"), Is.False);
        Assert.That(Q.Has(Q.Body("<img name=\"a\" src=\"images/gone.png\">", "", null, fs), "D012"), Is.True);
    }

    [Test]
    public void EachDiagnosticPointsAtItsElement()
    {
        MiniAnalysis m = Q.Body("<div name=\"a\">\n</div>\n<table name=\"t\"></table>");
        Diagnostic d = Q.First(m, "D001");
        Assert.That(d.Span.File, Is.EqualTo("page.html"));
        Assert.That(d.Construct, Is.EqualTo("<table>"));
        Assert.That(d.Severity, Is.EqualTo(Severity.Error));
        Assert.That(d.Tier, Is.EqualTo(Tier.Reject));
    }

    [Test]
    public void WarningRulesKeepTheirSeverity()
    {
        Assert.That(Q.First(Q.Body("<div name=\"a\" foo=\"1\"></div>"), "D004").Severity, Is.EqualTo(Severity.Warning));
        Assert.That(Q.First(Q.Body("<button name=\"a\" data-loc=\"a.b\">x<span name=\"b\" data-loc=\"a.c\">y</span></button>"), "D008").Severity, Is.EqualTo(Severity.Warning));
    }
}

[TestFixture]
public class NamingRuleTests
{
    private static ConversionConfig Prefixed => Q.Cfg("{\"naming\":{\"prefixes\":[\"mn\"]}}");

    [TestCase("<div name=\"a\"></div><div name=\"a\"></div>", "N001", true)]
    [TestCase("<div name=\"a\"></div><div name=\"b\"></div>", "N001", false)]
    [TestCase("<div id=\"a\"></div><div name=\"a\"></div>", "N001", true)]
    [TestCase("<div name=\"Bad_Name\"></div>", "N002", true)]
    [TestCase("<div name=\"BadName\"></div>", "N002", true)]
    [TestCase("<div name=\"good-name\"></div>", "N002", false)]
    [TestCase("<div name=\"a1-b2\"></div>", "N002", false)]
    [TestCase("<div name=\"-a\"></div>", "N002", true)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">x</button>", "N003", false)]
    [TestCase("<button data-loc=\"a.b\">x</button>", "N003", true)]
    [TestCase("<input type=\"text\" data-loc=\"a.b\" data-text=\"x\">", "N003", true)]
    [TestCase("<div></div>", "N003", false)]
    [TestCase("<span data-loc=\"a.b\">x</span>", "N003", false)]
    [TestCase("<div name=\"a\" class=\"BadClass\"></div>", "N004", true)]
    [TestCase("<div name=\"a\" class=\"bad_class\"></div>", "N004", true)]
    [TestCase("<div name=\"a\" class=\"good-class\"></div>", "N004", false)]
    [TestCase("<div name=\"a\" class=\"block__elem\"></div>", "N004", false)]
    [TestCase("<div name=\"a\" class=\"block__elem--mod\"></div>", "N004", false)]
    [TestCase("<div name=\"a\" class=\"block__elem__deep\"></div>", "N004", true)]
    [TestCase("<div name=\"a\" id=\"b\"></div>", "N006", true)]
    [TestCase("<div name=\"a\" id=\"a\"></div>", "N006", false)]
    [TestCase("<div name=\"a\"></div>", "N006", false)]
    [TestCase("<div name=\"a\" class=\"undefined-class\"></div>", "N007", true)]
    [TestCase("<div name=\"a\" class=\"unity-label\"></div>", "N007", false)]
    [TestCase("<div name=\"a\" class=\"is-active\"></div>", "N007", false)]
    public void NamingConventionsAreChecked(string body, string rule, bool expected)
    {
        MiniAnalysis m = Q.Body(body);
        Assert.That(Q.Has(m, rule), Is.EqualTo(expected), Q.Ids(m));
    }

    [TestCase("<div name=\"mn-root\"></div>", "N005", false)]
    [TestCase("<div name=\"mn\"></div>", "N005", false)]
    [TestCase("<div name=\"other-root\"></div>", "N005", true)]
    [TestCase("<div name=\"mnx-root\"></div>", "N005", true)]
    [TestCase("<div name=\"mn-root\" class=\"mn__title\"></div>", "N005", false)]
    [TestCase("<div name=\"mn-root\" class=\"mn--dark\"></div>", "N005", false)]
    [TestCase("<div name=\"mn-root\" class=\"other\"></div>", "N005", true)]
    [TestCase("<div name=\"mn-root\" class=\"unity-label\"></div>", "N005", false)]
    public void PrefixesComeFromTheConfig(string body, string rule, bool expected)
    {
        MiniAnalysis m = Q.Body(body, "", Prefixed);
        Assert.That(Q.Has(m, rule), Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void NoPrefixesMeansNoN005()
    {
        Assert.That(Q.Has(Q.Body("<div name=\"anything\" class=\"whatever\"></div>"), "N005"), Is.False);
    }

    [Test]
    public void DefinedClassesSilenceN007()
    {
        Assert.That(Q.Has(Q.Body("<div name=\"a\" class=\"c\"></div>", ".c { color: red; }"), "N007"), Is.False);
        Assert.That(Q.Has(Q.Body("<div name=\"a\" class=\"c\"></div>", ".other { color: red; }"), "N007"), Is.True);
        Assert.That(Q.Has(Q.Body("<div name=\"a\" class=\"c\"></div>", "div.c { color: red; }"), "N007"), Is.False);
        Assert.That(Q.Has(Q.Body("<div name=\"a\" class=\"c\"></div>", ".x:not(.c) { color: red; }"), "N007"), Is.False);
    }

    [Test]
    public void UndefinedClassModeComesFromTheConfig()
    {
        string body = "<div name=\"a\" class=\"c\"></div>";
        Assert.That(Q.Has(Q.Body(body, "", Q.Cfg("{\"rules\":{\"undefinedClass\":\"off\"}}")), "N007"), Is.False);
        Assert.That(Q.First(Q.Body(body, "", Q.Cfg("{\"rules\":{\"undefinedClass\":\"fail\"}}")), "N007").Severity, Is.EqualTo(Severity.Error));
        Assert.That(Q.First(Q.Body(body), "N007").Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void DynamicClassesAreExempt()
    {
        string body = "<div name=\"a\" class=\"state-open\"></div>";
        Assert.That(Q.Has(Q.Body(body), "N007"), Is.True);
        Assert.That(Q.Has(Q.Body(body, "", Q.Cfg("{\"naming\":{\"dynamicClasses\":[\"state-open\"]}}")), "N007"), Is.False);
        Assert.That(Q.Has(Q.Body(body, "", Q.Cfg("{\"naming\":{\"dynamicClassPattern\":\"^state-\"}}")), "N007"), Is.False);
    }

    [Test]
    public void N007IsReportedOncePerClass()
    {
        Assert.That(Q.Count(Q.Body("<div name=\"a\" class=\"c\"></div><div name=\"b\" class=\"c\"></div>"), "N007"), Is.EqualTo(1));
    }

    [Test]
    public void DuplicateNamesNameTheFirstLine()
    {
        MiniAnalysis m = Q.Body("<div name=\"a\"></div>\n<div name=\"a\"></div>");
        Assert.That(Q.First(m, "N001").Message, Does.Contain("already used at line"));
    }

    [Test]
    public void CustomNamePatternIsHonoured()
    {
        ConversionConfig cfg = Q.Cfg("{\"naming\":{\"namePattern\":\"^[a-z]+$\"}}");
        Assert.That(Q.Has(Q.Body("<div name=\"abc\"></div>", "", cfg), "N002"), Is.False);
        Assert.That(Q.Has(Q.Body("<div name=\"ab-c\"></div>", "", cfg), "N002"), Is.True);
    }

    [Test]
    public void NamesAreScopedPerPage()
    {
        MiniAnalysis m = MiniAnalyzer.Analyze(new[]
        {
            ("a.html", "<html><body><div name=\"x\"></div></body></html>"),
            ("b.html", "<html><body><div name=\"x\"></div></body></html>"),
        }, null, null, null, null, null);
        Assert.That(Q.Has(m, "N001"), Is.False);
    }
}

[TestFixture]
public class LocalisationRuleTests
{
    [TestCase("<span name=\"a\">Hello</span>", "L001", true)]
    [TestCase("<button name=\"a\">Start</button>", "L001", true)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">Hello</span>", "L001", false)]
    [TestCase("<span name=\"a\">★</span>", "L001", false)]
    [TestCase("<span name=\"a\">→</span>", "L001", false)]
    [TestCase("<span name=\"a\">  </span>", "L001", false)]
    [TestCase("<span name=\"a\">Привет</span>", "L001", true)]
    [TestCase("<div name=\"a\" title=\"Tip\"></div>", "L001", true)]
    [TestCase("<div name=\"a\" data-loc-tooltip=\"a.tip\"></div>", "L001", false)]
    [TestCase("<input type=\"text\" name=\"a\" placeholder=\"Name\" data-loc=\"a.b\" data-text=\"x\">", "L001", true)]
    [TestCase("<input type=\"text\" name=\"a\" data-loc-placeholder=\"a.ph\" data-loc=\"a.b\" data-text=\"x\">", "L001", false)]
    [TestCase("<select name=\"a\"><option>Raw</option></select>", "L001", true)]
    [TestCase("<select name=\"a\"><option data-loc=\"a.o\">Raw</option></select>", "L001", false)]
    [TestCase("<span name=\"a\" data-loc=\"BAD\">x</span>", "L002", true)]
    [TestCase("<span name=\"a\" data-loc=\"nodot\">x</span>", "L002", true)]
    [TestCase("<span name=\"a\" data-loc=\"menu.main.play\">x</span>", "L002", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b-c_d\">x</span>", "L002", false)]
    [TestCase("<div name=\"a\" data-loc-tooltip=\"BAD\"></div>", "L002", true)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">One</span><span name=\"b\" data-loc=\"a.b\">Two</span>", "L005", true)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">One</span><span name=\"b\" data-loc=\"a.b\">One</span>", "L005", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">One</span><span name=\"b\" data-loc=\"a.b\">  One </span>", "L005", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\"></span><span name=\"b\" data-loc=\"a.b\">Two</span>", "L005", false)]
    [TestCase("<div name=\"a\" data-loc=\"a.b\"></div>", "L006", true)]
    [TestCase("<img name=\"a\" data-loc=\"a.b\">", "L006", true)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">x</span>", "L006", false)]
    [TestCase("<input type=\"checkbox\" name=\"a\" data-loc=\"a.b\" data-text=\"x\">", "L006", false)]
    [TestCase("<input type=\"text\" name=\"a\" data-loc=\"a.b\">", "L007", true)]
    [TestCase("<input type=\"text\" name=\"a\" data-loc=\"a.b\" data-text=\"Name\">", "L007", false)]
    [TestCase("<span name=\"a\" data-loc=\"a.b\">x</span>", "L007", false)]
    [TestCase("<button name=\"a\" data-loc=\"a.b\">x</button>", "L007", false)]
    public void KeysAreChecked(string body, string rule, bool expected)
    {
        MiniAnalysis m = Q.Body(body);
        Assert.That(Q.Has(m, rule), Is.EqualTo(expected), Q.Ids(m));
    }

    [Test]
    public void GlyphOnlyTextCanBeForbidden()
    {
        string body = "<span name=\"a\">★</span>";
        Assert.That(Q.Has(Q.Body(body, "", Q.Cfg("{\"localization\":{\"allowGlyphOnly\":false}}")), "L001"), Is.True);
    }

    [Test]
    public void CustomKeyPatternIsHonoured()
    {
        ConversionConfig cfg = Q.Cfg("{\"localization\":{\"keyPattern\":\"^k_[0-9]+$\"}}");
        Assert.That(Q.Has(Q.Body("<span name=\"a\" data-loc=\"k_12\">x</span>", "", cfg), "L002"), Is.False);
        Assert.That(Q.Has(Q.Body("<span name=\"a\" data-loc=\"a.b\">x</span>", "", cfg), "L002"), Is.True);
    }

    [Test]
    public void L005NamesBothTexts()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.b\">One</span><span name=\"b\" data-loc=\"a.b\">Two</span>");
        Assert.That(Q.First(m, "L005").Message, Does.Contain("One").And.Contain("Two"));
    }

    private static IStringTables Tables(string json)
    {
        var fs = new MemoryFileSystem().AddFile("strings.json", json);
        return new ConfigStringTables(fs, "");
    }

    private static ConversionConfig WithTable => Q.Cfg("{\"localization\":{\"files\":[\"strings.json\"],\"mockLocale\":\"en\"}}");

    [Test]
    public void UnknownKeysAreL003WhenATableExists()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.new\">x</span>", "", WithTable, null, Tables("{\"en\":{\"a.old\":\"Old\"}}"));
        Assert.That(Q.Has(m, "L003"), Is.True);
        Assert.That(Q.First(m, "L003").Severity, Is.EqualTo(Severity.Info));
    }

    [Test]
    public void KnownKeysAreSilent()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.old\">Old</span>", "", WithTable, null, Tables("{\"en\":{\"a.old\":\"Old\"}}"));
        Assert.That(Q.Has(m, "L003"), Is.False);
        Assert.That(Q.Has(m, "L004"), Is.False);
    }

    [Test]
    public void MockTextThatDiffersFromTheTableIsL004()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.old\">Different</span>", "", WithTable, null, Tables("{\"en\":{\"a.old\":\"Old\"}}"));
        Assert.That(Q.Has(m, "L004"), Is.True);
        Assert.That(Q.First(m, "L004").Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void WhitespaceDifferencesAreIgnoredByL004()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.old\">  Old   text </span>", "", WithTable, null, Tables("{\"en\":{\"a.old\":\"Old text\"}}"));
        Assert.That(Q.Has(m, "L004"), Is.False);
    }

    [Test]
    public void NoTableMeansNoL003()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.new\">x</span>", "", Q.Cfg("{\"localization\":{\"files\":[\"none.json\"]}}"), null, Tables("{\"en\":{}}"));
        Assert.That(Q.Has(m, "L003"), Is.False);
    }

    [Test]
    public void MockLocaleIsUsedForTheComparison()
    {
        string table = "{\"en\":{\"a.k\":\"Hello\"},\"ru\":{\"a.k\":\"Privet\"}}";
        ConversionConfig ru = Q.Cfg("{\"localization\":{\"files\":[\"strings.json\"],\"mockLocale\":\"ru\"}}");
        Assert.That(Q.Has(Q.Body("<span name=\"a\" data-loc=\"a.k\">Privet</span>", "", ru, null, Tables(table)), "L004"), Is.False);
        Assert.That(Q.Has(Q.Body("<span name=\"a\" data-loc=\"a.k\">Hello</span>", "", ru, null, Tables(table)), "L004"), Is.True);
    }

    [Test]
    public void RepeatedKeysReportL003Once()
    {
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.new\">x</span><span name=\"b\" data-loc=\"a.new\">x</span>", "", WithTable, null, Tables("{\"en\":{}}"));
        Assert.That(Q.Count(m, "L003"), Is.EqualTo(1));
    }

    [Test]
    public void TableLookupsSupportTheFlatFormat()
    {
        var fs = new MemoryFileSystem().AddFile("en.json", "{\"a.k\":\"Hello\"}");
        ConversionConfig cfg = Q.Cfg("{\"localization\":{\"files\":[\"en.json\"],\"format\":\"flat\",\"mockLocale\":\"en\"}}");
        MiniAnalysis m = Q.Body("<span name=\"a\" data-loc=\"a.k\">Hello</span>", "", cfg, null, new ConfigStringTables(fs, ""));
        Assert.That(Q.Has(m, "L003"), Is.False);
        Assert.That(Q.Has(m, "L004"), Is.False);
    }

    [Test]
    public void StringTableApi()
    {
        var table = StringTable.Load(new MemoryFileSystem().AddFile("s.json", "{\"version\":1,\"en\":{\"a\":\"A\",\"b\":\"B\"},\"ru\":{\"a\":\"AA\"}}"), new[] { "s.json", "missing.json" }, "locale-map");
        Assert.That(table.HasSources, Is.True);
        Assert.That(table.Has("a"), Is.True);
        Assert.That(table.Has("zzz"), Is.False);
        Assert.That(table.Get("a", "ru"), Is.EqualTo("AA"));
        Assert.That(table.Get("b", "ru"), Is.EqualTo("B"));
        Assert.That(table.Get("zzz", "en"), Is.Null);
        Assert.That(table.Keys(), Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void EmptyTableHasNoSources()
    {
        Assert.That(StringTable.Load(new MemoryFileSystem(), new[] { "none.json" }, "locale-map").HasSources, Is.False);
    }
}
