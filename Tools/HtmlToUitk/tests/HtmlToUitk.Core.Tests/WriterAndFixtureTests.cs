using System;
using System.IO;
using System.Linq;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class WriterAndFixtureTests
    {
        private static string Uss(string css, UssWriterOptions options = null) => UssWriter.Write(Support.Css(css), options);

        [Test]
        public void WritesRulesInTheCanonicalLayout()
        {
            Assert.That(Uss(".a{color:red;margin:0}.b,.c{top:0}"), Is.EqualTo(".a {\n    margin: 0;\n    color: red;\n}\n\n.b,\n.c {\n    top: 0;\n}\n"));
        }

        [Test]
        public void EmptySheetsWriteNothing()
        {
            Assert.That(Uss(""), Is.EqualTo(""));
            Assert.That(Uss("/* c */"), Is.EqualTo(""));
        }

        [Test]
        public void EmptyRulesStillWriteTheirBraces()
        {
            Assert.That(Uss(".a{}"), Is.EqualTo(".a {\n}\n"));
        }

        [Test]
        public void IndentIsConfigurable()
        {
            Assert.That(Uss(".a{top:0}", new UssWriterOptions { Indent = 2 }), Is.EqualTo(".a {\n  top: 0;\n}\n"));
        }

        [Test]
        public void ImportantIsKept()
        {
            Assert.That(Uss(".a{top:0!important}"), Is.EqualTo(".a {\n    top: 0 !important;\n}\n"));
        }

        [Test]
        public void SortingCanBeDisabled()
        {
            string css = ".a{color:red;margin:0}";
            Assert.That(Uss(css, new UssWriterOptions { SortDeclarations = false }), Is.EqualTo(".a {\n    color: red;\n    margin: 0;\n}\n"));
        }

        [Test]
        public void RulesInsideAtContextsAreSkippedByDefault()
        {
            string css = ".a{top:0}@media screen{.b{top:1px}}";
            Assert.That(Uss(css), Is.EqualTo(".a {\n    top: 0;\n}\n"));
            Assert.That(Uss(css, new UssWriterOptions { IncludeAtContextRules = true }), Does.Contain(".b {"));
        }

        [Test]
        public void SelectorsAreWrittenCanonically()
        {
            Assert.That(Uss("a>b ,  .c:HOVER{top:0}"), Is.EqualTo("a > b,\n.c:hover {\n    top: 0;\n}\n"));
        }

        [TestCase("margin-top:1px;position:absolute;color:red;width:1px", "position,width,margin-top,color")]
        [TestCase("transition-duration:1s;opacity:1;transition-property:a", "opacity,transition-property,transition-duration")]
        [TestCase("--b:1;--a:2;top:0", "--b,--a,top")]
        [TestCase("zzz:1;top:0;aaa:2", "top,zzz,aaa")]
        [TestCase("font-size:1px;color:red;-unity-font-style:bold", "color,-unity-font-style,font-size")]
        [TestCase("background-color:red;border-radius:2px;height:1px;min-width:2px", "min-width,height,border-radius,background-color")]
        [TestCase("justify-content:center;align-items:center;flex-direction:row;display:flex", "display,flex-direction,align-items,justify-content")]
        [TestCase("padding-left:1px;padding-top:2px;margin-left:3px", "margin-left,padding-top,padding-left")]
        [TestCase("rotate:1deg;translate:1px;scale:2", "translate,rotate,scale")]
        public void DeclarationOrderIsCanonical(string declarations, string expectedNames)
        {
            string text = Uss(".a{" + declarations + "}");
            var names = text.Split('\n').Where(l => l.StartsWith("    ")).Select(l => l.Trim().Split(':')[0]);
            Assert.That(string.Join(",", names), Is.EqualTo(expectedNames));
        }

        [Test]
        public void MixedShorthandAndLonghandKeepsSourceOrder()
        {
            string text = Uss(".a{margin-top:1px;margin:0;color:red;top:0}");
            Assert.That(text, Is.EqualTo(".a {\n    margin-top: 1px;\n    margin: 0;\n    color: red;\n    top: 0;\n}\n"));
        }

        [Test]
        public void SortingIsStableForEqualRanks()
        {
            string text = Uss(".a{zz:1;aa:2;top:0}");
            Assert.That(text, Is.EqualTo(".a {\n    top: 0;\n    zz: 1;\n    aa: 2;\n}\n"));
        }

        [Test]
        public void UssRoundTripIsStable()
        {
            string once = Uss(".a{color:red;margin:1px 2px}\n.b:hover{opacity:.5}");
            string twice = Uss(once);
            Assert.That(twice, Is.EqualTo(once));
        }

        [Test]
        public void RankTableOrdersGroups()
        {
            Assert.That(UssPropertyOrder.RankOf("position"), Is.LessThan(UssPropertyOrder.RankOf("flex-direction")));
            Assert.That(UssPropertyOrder.RankOf("flex-direction"), Is.LessThan(UssPropertyOrder.RankOf("width")));
            Assert.That(UssPropertyOrder.RankOf("--x"), Is.LessThan(UssPropertyOrder.RankOf("position")));
            Assert.That(UssPropertyOrder.RankOf("unknown"), Is.GreaterThan(UssPropertyOrder.RankOf("transition-delay")));
        }

        private static string FixtureDir()
        {
            string root = Support.RepoRoot();
            if (root == null) return null;
            string dir = Path.Combine(root, "fixtures", "S01-minimal-page");
            return Directory.Exists(dir) ? dir : null;
        }

        [Test]
        public void FixtureS01UssIsReproducedByteForByte()
        {
            string dir = FixtureDir();
            if (dir == null) Assert.Ignore("fixtures folder not found");
            HtmlDocument page = Support.Html(File.ReadAllText(Path.Combine(dir, "input", "page.html")));
            StyleBlock block = page.StyleBlocks.Single();
            Stylesheet sheet = new CssParser().Parse(block.Text, "page.html", StyleOrigin.Page, null, block.Span.Start);
            string expected = Support.Lf(File.ReadAllText(Path.Combine(dir, "expected", "Screens", "Minimal.uss")));
            Assert.That(UssWriter.Write(sheet), Is.EqualTo(expected));
        }

        [Test]
        public void FixtureS01UxmlReadsAndWritesByteForByte()
        {
            string dir = FixtureDir();
            if (dir == null) Assert.Ignore("fixtures folder not found");
            string expected = Support.Lf(File.ReadAllText(Path.Combine(dir, "expected", "Screens", "Minimal.uxml")));
            var document = new HtmlToUitk.Core.Uxml.UxmlReader().Read(expected, "Minimal.uxml", null);
            string written = new HtmlToUitk.Core.Uxml.UxmlWriter().Write(document, new HtmlToUitk.Core.Uxml.UxmlWriterOptions());
            Assert.That(written, Is.EqualTo(expected));
        }

        [Test]
        public void FixtureS01HtmlMapsToTheExpectedElements()
        {
            string dir = FixtureDir();
            if (dir == null) Assert.Ignore("fixtures folder not found");
            var bag = new DiagnosticBag();
            HtmlDocument page = Support.Html(File.ReadAllText(Path.Combine(dir, "input", "page.html")), bag);
            Assert.That(bag.Items, Is.Empty);
            Assert.That(page.Metas.Any(m => m.GetAttribute("name") == "uitk:screen" && m.GetAttribute("content") == "Minimal"), Is.True);
            Assert.That(Support.ByName(page, "mn-title").GetAttribute("data-loc"), Is.EqualTo("minimal.title"));
            Assert.That(Support.ByName(page, "mn-action").Tag, Is.EqualTo("button"));
        }

        [Test]
        public void AllJsonFixturesInTheRepositoryParse()
        {
            string root = Support.RepoRoot();
            if (root == null) Assert.Ignore("repository root not found");
            string[] folders = { "spec", "fixtures", "examples" };
            int count = 0;
            foreach (string folder in folders)
            {
                string path = Path.Combine(root, folder);
                if (!Directory.Exists(path)) continue;
                foreach (string file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    var options = new HtmlToUitk.Core.Json.JsonReadOptions { DuplicateKeys = HtmlToUitk.Core.Json.DuplicateKeyPolicy.Error };
                    Assert.DoesNotThrow(() => HtmlToUitk.Core.Json.JsonReader.Parse(text, options), file);
                    count++;
                }
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [Test]
        public void AllCssInTheRepositoryParsesWithoutErrors()
        {
            string root = Support.RepoRoot();
            if (root == null) Assert.Ignore("repository root not found");
            string assets = Path.Combine(root, "assets");
            if (!Directory.Exists(assets)) Assert.Ignore("assets folder not found");
            int count = 0;
            foreach (string file in Directory.GetFiles(assets, "*.css"))
            {
                var bag = new DiagnosticBag();
                Stylesheet sheet = CssParser.Instance.Parse(File.ReadAllText(file), file, StyleOrigin.Linked, bag);
                Assert.That(bag.Items.Where(d => d.Severity == Severity.Error).Select(d => d.Message).ToArray(), Is.Empty, file);
                Assert.That(sheet.Rules.Count, Is.GreaterThan(0), file);
                count++;
            }
            Assert.That(count, Is.GreaterThan(0));
        }

        [Test]
        public void ExampleHtmlPagesParseCleanly()
        {
            string root = Support.RepoRoot();
            if (root == null) Assert.Ignore("repository root not found");
            string pages = Path.Combine(root, "examples", "pages");
            if (!Directory.Exists(pages)) Assert.Ignore("examples folder not found");
            foreach (string file in Directory.GetFiles(pages, "*.html"))
            {
                var bag = new DiagnosticBag();
                HtmlDocument document = new HtmlParser().Parse(File.ReadAllText(file), file, bag);
                Assert.That(bag.Items.Select(d => d.RuleId + ":" + d.Message).ToArray(), Is.Empty, file);
                Assert.That(Support.All(document.Body).Count, Is.GreaterThan(0), file);
            }
        }
    }
}
