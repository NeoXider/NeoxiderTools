using System;
using System.Linq;
using System.Text;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Core.Uxml;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class UnitAndNameTests
    {
        [TestCase("px", UnitKind.Length)]
        [TestCase("PX", UnitKind.Length)]
        [TestCase("em", UnitKind.Length)]
        [TestCase("rem", UnitKind.Length)]
        [TestCase("vw", UnitKind.Length)]
        [TestCase("vmin", UnitKind.Length)]
        [TestCase("pt", UnitKind.Length)]
        [TestCase("ch", UnitKind.Length)]
        [TestCase("%", UnitKind.Percent)]
        [TestCase("deg", UnitKind.Angle)]
        [TestCase("rad", UnitKind.Angle)]
        [TestCase("turn", UnitKind.Angle)]
        [TestCase("s", UnitKind.Time)]
        [TestCase("ms", UnitKind.Time)]
        [TestCase("hz", UnitKind.Frequency)]
        [TestCase("dppx", UnitKind.Resolution)]
        [TestCase("fr", UnitKind.Flex)]
        [TestCase("", UnitKind.None)]
        [TestCase(null, UnitKind.None)]
        [TestCase("furlong", UnitKind.Unknown)]
        public void ClassifiesUnits(string unit, UnitKind expected)
        {
            Assert.That(CssUnits.Classify(unit), Is.EqualTo(expected));
        }

        [TestCase("px", true, false, false)]
        [TestCase("pt", true, false, false)]
        [TestCase("em", false, true, false)]
        [TestCase("rem", false, true, false)]
        [TestCase("vh", false, true, true)]
        [TestCase("dvh", false, true, true)]
        [TestCase("%", false, false, false)]
        [TestCase("s", false, false, false)]
        public void UnitFlags(string unit, bool absolute, bool relative, bool viewport)
        {
            Assert.That(CssUnits.IsAbsoluteLength(unit), Is.EqualTo(absolute));
            Assert.That(CssUnits.IsRelativeLength(unit), Is.EqualTo(relative));
            Assert.That(CssUnits.IsViewportUnit(unit), Is.EqualTo(viewport));
        }

        [TestCase("10px", 10.0)]
        [TestCase("12pt", 16.0)]
        [TestCase("1pc", 16.0)]
        [TestCase("1in", 96.0)]
        [TestCase("2.54cm", 96.0)]
        [TestCase("25.4mm", 96.0)]
        [TestCase("0", 0.0)]
        [TestCase("-5px", -5.0)]
        public void ConvertsAbsoluteLengthsToPixels(string text, double expected)
        {
            double pixels;
            Assert.That(CssUnits.TryToPixels(CssValueParser.Parse(text), out pixels), Is.True);
            Assert.That(pixels, Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase("1em")]
        [TestCase("50%")]
        [TestCase("1s")]
        [TestCase("red")]
        [TestCase("5")]
        public void RelativeValuesDoNotConvertToPixels(string text)
        {
            double pixels;
            Assert.That(CssUnits.TryToPixels(CssValueParser.Parse(text), out pixels), Is.False);
        }

        [TestCase("90deg", 90.0)]
        [TestCase("100grad", 90.0)]
        [TestCase("0.5turn", 180.0)]
        [TestCase("3.141592653589793rad", 180.0)]
        [TestCase("0", 0.0)]
        public void ConvertsAnglesToDegrees(string text, double expected)
        {
            double degrees;
            Assert.That(CssUnits.TryToDegrees(CssValueParser.Parse(text), out degrees), Is.True);
            Assert.That(degrees, Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase("1s", 1.0, 1000.0)]
        [TestCase("250ms", 0.25, 250.0)]
        [TestCase("0.5s", 0.5, 500.0)]
        public void ConvertsTimes(string text, double seconds, double milliseconds)
        {
            double s, ms;
            Assert.That(CssUnits.TryToSeconds(CssValueParser.Parse(text), out s), Is.True);
            Assert.That(CssUnits.TryToMilliseconds(CssValueParser.Parse(text), out ms), Is.True);
            Assert.That(s, Is.EqualTo(seconds).Within(1e-9));
            Assert.That(ms, Is.EqualTo(milliseconds).Within(1e-9));
            Assert.That(CssUnits.TryToSeconds(CssValueParser.Parse("1px"), out s), Is.False);
            Assert.That(CssUnits.TryToMilliseconds(CssValueParser.Parse("1"), out ms), Is.False);
        }

        [TestCase("0", true)]
        [TestCase("0px", true)]
        [TestCase("0%", true)]
        [TestCase("0s", true)]
        [TestCase("1px", false)]
        [TestCase("red", false)]
        public void DetectsZero(string text, bool expected)
        {
            Assert.That(CssUnits.IsZero(CssValueParser.Parse(text)), Is.EqualTo(expected));
        }

        [TestCase("login-page", "LoginPage")]
        [TestCase("login_page", "LoginPage")]
        [TestCase("login page", "LoginPage")]
        [TestCase("LoginPage", "LoginPage")]
        [TestCase("HTMLParser", "HtmlParser")]
        [TestCase("main-menu-2", "MainMenu2")]
        [TestCase("a", "A")]
        [TestCase("", "")]
        [TestCase("---", "")]
        [TestCase("screen.v2", "ScreenV2")]
        public void ConvertsToPascalCase(string input, string expected)
        {
            Assert.That(NameCase.ToPascal(input), Is.EqualTo(expected));
        }

        [TestCase("LoginPage", "login-page")]
        [TestCase("login_page", "login-page")]
        [TestCase("HTMLParser", "html-parser")]
        [TestCase("Main Menu", "main-menu")]
        [TestCase("already-kebab", "already-kebab")]
        [TestCase("Version2Screen", "version2-screen")]
        [TestCase("", "")]
        public void ConvertsToKebabCase(string input, string expected)
        {
            Assert.That(NameCase.ToKebab(input), Is.EqualTo(expected));
        }

        [TestCase("a", true)]
        [TestCase("login-page", true)]
        [TestCase("a1-b2", true)]
        [TestCase("1a", false)]
        [TestCase("-a", false)]
        [TestCase("a-", false)]
        [TestCase("a--b", false)]
        [TestCase("A", false)]
        [TestCase("a_b", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ValidatesKebabCase(string input, bool expected)
        {
            Assert.That(NameCase.IsKebab(input), Is.EqualTo(expected));
        }
    }

    public class FuzzTests
    {
        private static string Garbage(Random random, string alphabet, int length)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++) sb.Append(alphabet[random.Next(alphabet.Length)]);
            return sb.ToString();
        }

        private const string HtmlAlphabet = "<>/=\"' !-?&#;abcdefghij:\n\t[]{}()*.,@";
        private const string CssAlphabet = "{}();:,.#>+~*[]=\"'/ \n-!@%abcdefghij0123456789\\";
        private const string JsonAlphabet = "{}[]\",:0123456789.-+eE tfnrulsa\\/ \n";
        private const string XmlAlphabet = "<>/=\"' &;:-!?[]abcdefghij\n";

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void HtmlParserSurvivesRandomMarkup(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 300; i++)
            {
                string text = Garbage(random, HtmlAlphabet, random.Next(0, 120));
                Assert.DoesNotThrow(() => new HtmlParser().Parse(text, "f.html", new DiagnosticBag()), text);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        public void CssParserSurvivesRandomStylesheets(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 300; i++)
            {
                string text = Garbage(random, CssAlphabet, random.Next(0, 120));
                Assert.DoesNotThrow(() =>
                {
                    Stylesheet sheet = CssParser.Instance.Parse(text, "f.css", StyleOrigin.Page, new DiagnosticBag());
                    UssWriter.Write(sheet);
                }, text);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void SelectorAndValueParsersSurviveRandomText(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 400; i++)
            {
                string text = Garbage(random, CssAlphabet, random.Next(0, 60));
                Assert.DoesNotThrow(() =>
                {
                    SelectorParser.ParseList(text, new DiagnosticBag());
                    CssValueParser.Parse(text).ToText();
                    ShorthandExpander.Expand(new Declaration { Name = "margin", Value = CssValueParser.Parse(text) });
                    ShorthandExpander.Expand(new Declaration { Name = "background", Value = CssValueParser.Parse(text) });
                    ShorthandExpander.Expand(new Declaration { Name = "font", Value = CssValueParser.Parse(text) });
                    ShorthandExpander.Expand(new Declaration { Name = "transition", Value = CssValueParser.Parse(text) });
                    ShorthandExpander.Expand(new Declaration { Name = "animation", Value = CssValueParser.Parse(text) });
                }, text);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void JsonReaderOnlyThrowsParseExceptions(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 500; i++)
            {
                string text = Garbage(random, JsonAlphabet, random.Next(0, 60));
                try
                {
                    JsonValue value = JsonReader.Parse(text);
                    string written = JsonWriter.Write(value, JsonWriteOptions.Compact);
                    Assert.That(JsonWriter.Write(JsonReader.Parse(written), JsonWriteOptions.Compact), Is.EqualTo(written), text);
                }
                catch (JsonParseException)
                {
                }
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void UxmlReaderSurvivesRandomXml(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 300; i++)
            {
                string text = Garbage(random, XmlAlphabet, random.Next(0, 80));
                Assert.DoesNotThrow(() => new UxmlReader().Read(text, "f.uxml", new DiagnosticBag()), text);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StyleEngineSurvivesRandomRulesOnRandomDocuments(int seed)
        {
            var random = new Random(seed);
            for (int i = 0; i < 60; i++)
            {
                string html = Garbage(random, "<div class=a b>/ pspan\"='", random.Next(0, 80));
                string css = Garbage(random, ".#a b>+~{}:;-0123 var()--,", random.Next(0, 100));
                Assert.DoesNotThrow(() =>
                {
                    HtmlDocument document = new HtmlParser().Parse(html, "f.html", null);
                    Stylesheet sheet = CssParser.Instance.Parse(css, "f.css", StyleOrigin.Page, null);
                    var model = new HtmlToUitk.Core.Style.StyleEngine().Build(new[] { sheet, sheet }, new HtmlToUitk.Core.Style.StyleOptions(), new DiagnosticBag());
                    foreach (DomElement element in Support.All(document.Body))
                    {
                        model.Resolve(element, HtmlToUitk.Core.Style.PseudoStateSet.Hover);
                        model.Variables(element);
                    }
                }, html + " | " + css);
            }
        }

        [TestCase("\\")]
        [TestCase("a \\")]
        [TestCase("-\\")]
        [TestCase("1px \\")]
        [TestCase("f(\\")]
        [TestCase("var(\\")]
        public void TrailingBackslashDoesNotHangTheValueParser(string text)
        {
            Assert.That(Task_Run(() => CssValueParser.Parse(text).ToText()), Is.True);
        }

        [TestCase("\\")]
        [TestCase(".a\\")]
        [TestCase("a \\")]
        [TestCase("#\\")]
        [TestCase(":\\")]
        public void TrailingBackslashDoesNotHangTheSelectorParser(string text)
        {
            Assert.That(Task_Run(() => SelectorParser.ParseList(text, new DiagnosticBag()).Count), Is.True);
        }

        private static bool Task_Run<T>(Func<T> action)
        {
            var task = System.Threading.Tasks.Task.Run(action);
            return task.Wait(TimeSpan.FromSeconds(5));
        }

        [Test]
        public void ParsingTheSameInputTwiceGivesIdenticalOutput()
        {
            string css = ".a{margin:1px 2px;color:RED;background:url(a.png) no-repeat}\n.b:hover > .c{opacity:.5;transition:opacity .2s}\n@media (max-width:900px){.d{top:0}}";
            string first = UssWriter.Write(CssParser.Instance.Parse(css, "a.css", StyleOrigin.Page, null), new UssWriterOptions { IncludeAtContextRules = true });
            string second = UssWriter.Write(CssParser.Instance.Parse(css, "a.css", StyleOrigin.Page, null), new UssWriterOptions { IncludeAtContextRules = true });
            Assert.That(second, Is.EqualTo(first));
        }
    }
}
