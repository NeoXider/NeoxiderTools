using System;
using System.Linq;
using HtmlToUitk.Core.Css;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class CssParserTests
    {
        private static string Summary(Stylesheet sheet)
        {
            return string.Join(" | ", sheet.Rules.Select(r =>
                string.Join(",", r.Selectors.Select(s => s.Text)) + " {" + string.Join(";", r.Declarations.Select(d => d.Name + ":" + d.Value.ToText() + (d.Important ? "!" : ""))) + "}"));
        }

        [TestCase(".a{color:red}", ".a {color:red}")]
        [TestCase(".a { color : red ; }", ".a {color:red}")]
        [TestCase(".a{color:red;margin:0 auto}", ".a {color:red;margin:0 auto}")]
        [TestCase(".a,.b{color:red}", ".a,.b {color:red}")]
        [TestCase(".a\n,\n.b\n{\ncolor:red\n}", ".a,.b {color:red}")]
        [TestCase("/* c */ .a /* d */ { /* e */ color /* f */ : /* g */ red /* h */ ; }", ".a {color:red}")]
        [TestCase(".a{color:red!important}", ".a {color:red!}")]
        [TestCase(".a{color:red ! important}", ".a {color:red!}")]
        [TestCase(".a{color:red !IMPORTANT}", ".a {color:red!}")]
        [TestCase(".a{COLOR:Red}", ".a {color:Red}")]
        [TestCase(".a{--My-Token:4px}", ".a {--My-Token:4px}")]
        [TestCase(".a{--empty:;color:red}", ".a {--empty:;color:red}")]
        [TestCase(".a{background:url(data:image/png;base64,AAA=)}", ".a {background:url(\"data:image/png;base64,AAA=\")}")]
        [TestCase(".a{content:\"a;b}c\"}", ".a {content:\"a;b}c\"}")]
        [TestCase(".a{color:red;;;margin:0}", ".a {color:red;margin:0}")]
        [TestCase(".a{}", ".a {}")]
        [TestCase(".a{color:red}.b{margin:0}", ".a {color:red} | .b {margin:0}")]
        [TestCase(".a{color:red} /* tail */", ".a {color:red}")]
        [TestCase("\uFEFF.a{color:red}", ".a {color:red}")]
        [TestCase("<!-- .a{color:red} -->", ".a {color:red}")]
        [TestCase("div>p+span~em{margin:0}", "div > p + span ~ em {margin:0}")]
        [TestCase(".a{transition:opacity .2s ease-in-out}", ".a {transition:opacity 0.2s ease-in-out}")]
        [TestCase(".a{margin:calc(1px + 2px)}", ".a {margin:calc(1px + 2px)}")]
        [TestCase(".a{font-family:\"A B\",serif}", ".a {font-family:\"A B\", serif}")]
        public void ParsesRules(string css, string expected)
        {
            Assert.That(Summary(Support.Css(css)), Is.EqualTo(expected));
        }

        [Test]
        public void EmptyAndWhitespaceSheetsHaveNoRules()
        {
            Assert.That(Support.Css("").Rules, Is.Empty);
            Assert.That(Support.Css("   \n\t ").Rules, Is.Empty);
            Assert.That(Support.Css("/* only a comment */").Rules, Is.Empty);
        }

        [Test]
        public void RuleOrderFollowsSource()
        {
            Stylesheet sheet = Support.Css(".a{}.b{}.c{}");
            Assert.That(sheet.Rules.Select(r => r.Order), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void SheetMetadataIsRecorded()
        {
            Stylesheet sheet = Support.Css(".a{}", "x/y.css", StyleOrigin.Skin);
            Assert.That(sheet.Path, Is.EqualTo("x/y.css"));
            Assert.That(sheet.Origin, Is.EqualTo(StyleOrigin.Skin));
        }

        [Test]
        public void DeclarationSpansPointAtTheSource()
        {
            Stylesheet sheet = Support.Css(".a {\n    color: red;\n    margin: 4px;\n}\n.b { top: 0 }");
            Declaration margin = sheet.Rules[0].Declarations[1];
            Assert.That(margin.Span.Start.Line, Is.EqualTo(3));
            Assert.That(margin.Span.Start.Column, Is.EqualTo(5));
            Assert.That(margin.Value.Span.Start.Column, Is.EqualTo(13));
            Assert.That(sheet.Rules[1].Span.Start.Line, Is.EqualTo(5));
            Assert.That(sheet.Rules[1].Declarations[0].Span.File, Is.EqualTo("test.css"));
        }

        [Test]
        public void RuleSpanCoversSelectorToClosingBrace()
        {
            Stylesheet sheet = Support.Css("  .a { x: 1 }");
            Assert.That(sheet.Rules[0].Span.Start.Column, Is.EqualTo(3));
            Assert.That(sheet.Rules[0].Span.End.Column, Is.EqualTo(14));
        }

        [Test]
        public void CrLfAndLoneCrCountAsOneLineBreak()
        {
            Stylesheet crlf = Support.Css(".a{\r\n}\r\n.b{\r\n}");
            Assert.That(crlf.Rules[1].Span.Start.Line, Is.EqualTo(3));
            Stylesheet cr = Support.Css(".a{\r}\r.b{\r}");
            Assert.That(cr.Rules[1].Span.Start.Line, Is.EqualTo(3));
        }

        [Test]
        public void BasePositionOffsetsLinesAndColumns()
        {
            var parser = new CssParser();
            Stylesheet sheet = parser.Parse(".a { x: 1 }\n.b { y: 2 }", "page.html", StyleOrigin.Page, null, new SourcePos(100, 10, 8));
            Assert.That(sheet.Rules[0].Span.Start.Line, Is.EqualTo(10));
            Assert.That(sheet.Rules[0].Span.Start.Column, Is.EqualTo(8));
            Assert.That(sheet.Rules[0].Span.Start.Offset, Is.EqualTo(100));
            Assert.That(sheet.Rules[1].Span.Start.Line, Is.EqualTo(11));
            Assert.That(sheet.Rules[1].Span.Start.Column, Is.EqualTo(1));
        }

        [Test]
        public void ImportantFlagIsStrippedFromTheValue()
        {
            Declaration d = Support.Css(".a{color:red !important}").Rules[0].Declarations[0];
            Assert.That(d.Important, Is.True);
            Assert.That(d.Value.ToText(), Is.EqualTo("red"));
        }

        [TestCase("@import \"a.css\";", "a.css")]
        [TestCase("@import url(a.css);", "a.css")]
        [TestCase("@import url(\"dir/a.css\");", "dir/a.css")]
        [TestCase("@import url('a.css') screen;", "a.css")]
        [TestCase("@IMPORT \"a.css\" ;", "a.css")]
        [TestCase("@import \"a.css\"\n;", "a.css")]
        public void ParsesImports(string css, string target)
        {
            Stylesheet sheet = Support.Css(css);
            Assert.That(sheet.Imports.Select(i => i.Target), Is.EqualTo(new[] { target }));
            Assert.That(sheet.AtRules, Is.Empty);
        }

        [Test]
        public void ImportsKeepSourceOrderAndSpans()
        {
            Stylesheet sheet = Support.Css("@charset \"utf-8\";\n@import \"a.css\";\n.x{}\n@import \"b.css\";");
            Assert.That(sheet.Imports.Select(i => i.Target), Is.EqualTo(new[] { "a.css", "b.css" }));
            Assert.That(sheet.Imports[0].Span.Start.Line, Is.EqualTo(2));
            Assert.That(sheet.Imports[1].Span.Start.Line, Is.EqualTo(4));
        }

        [Test]
        public void ImportWithoutTargetIsAnError()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css("@import ;", "t.css", StyleOrigin.Page, bag);
            Assert.That(sheet.Imports, Is.Empty);
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void MediaRulesCarryTheirContextAndStayInTheRuleList()
        {
            Stylesheet sheet = Support.Css(".a{x:1}@media (max-width: 900px){.b{y:2}.c{z:3}}.d{w:4}");
            Assert.That(sheet.Rules.Select(r => r.Selectors[0].Text), Is.EqualTo(new[] { ".a", ".b", ".c", ".d" }));
            Assert.That(sheet.Rules[0].AtContext, Is.Empty);
            Assert.That(sheet.Rules[1].AtContext, Is.EqualTo(new[] { "@media (max-width: 900px)" }));
            Assert.That(sheet.Rules[2].AtContext, Is.EqualTo(new[] { "@media (max-width: 900px)" }));
            Assert.That(sheet.Rules[3].AtContext, Is.Empty);
            Assert.That(sheet.Rules.Select(r => r.Order), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            AtRule media = sheet.AtRules.Single();
            Assert.That(media.Name, Is.EqualTo("media"));
            Assert.That(media.Prelude, Is.EqualTo("(max-width: 900px)"));
            Assert.That(media.Rules.Count, Is.EqualTo(2));
            Assert.That(ReferenceEquals(media.Rules[0], sheet.Rules[1]), Is.True);
        }

        [Test]
        public void NestedConditionalGroupsChainTheContext()
        {
            Stylesheet sheet = Support.Css("@media screen{@supports (display:flex){.a{x:1}}}");
            Assert.That(sheet.Rules[0].AtContext, Is.EqualTo(new[] { "@media screen", "@supports (display:flex)" }));
            Assert.That(sheet.AtRules.Select(a => a.Name), Is.EquivalentTo(new[] { "media", "supports" }));
        }

        [Test]
        public void MediaPreludeWhitespaceIsCollapsed()
        {
            Stylesheet sheet = Support.Css("@media   screen\n and   (min-width:1px){.a{x:1}}");
            Assert.That(sheet.AtRules[0].Prelude, Is.EqualTo("screen and (min-width:1px)"));
        }

        [Test]
        public void KeyframesAreParsedAsBlocksOutsideTheRuleList()
        {
            Stylesheet sheet = Support.Css("@keyframes fade{from{opacity:0}50%{opacity:.5}to{opacity:1}}.a{x:1}");
            Assert.That(sheet.Rules.Count, Is.EqualTo(1));
            AtRule keyframes = sheet.AtRules.Single();
            Assert.That(keyframes.Name, Is.EqualTo("keyframes"));
            Assert.That(keyframes.Prelude, Is.EqualTo("fade"));
            Assert.That(keyframes.Rules.Select(r => r.Selectors[0].Text), Is.EqualTo(new[] { "from", "50%", "to" }));
            Assert.That(keyframes.Rules[1].Declarations[0].Value.ToText(), Is.EqualTo("0.5"));
            Assert.That(keyframes.Rules[0].Selectors[0].Parts, Is.Empty);
            Assert.That(keyframes.Rules.Select(r => r.Order), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void VendorPrefixedKeyframesAreNormalised()
        {
            Stylesheet sheet = Support.Css("@-webkit-keyframes spin{to{rotate:1turn}}");
            Assert.That(sheet.AtRules.Single().Name, Is.EqualTo("keyframes"));
        }

        [Test]
        public void KeyframeSelectorListsAreSplit()
        {
            Stylesheet sheet = Support.Css("@keyframes k{0%,100%{opacity:0}50%{opacity:1}}");
            AtRule keyframes = sheet.AtRules.Single();
            Assert.That(keyframes.Rules[0].Selectors.Select(s => s.Text), Is.EqualTo(new[] { "0%", "100%" }));
        }

        [Test]
        public void FontFaceKeepsDeclarations()
        {
            Stylesheet sheet = Support.Css("@font-face{font-family:\"X\";src:url(x.woff)}.a{x:1}");
            AtRule face = sheet.AtRules.Single();
            Assert.That(face.Name, Is.EqualTo("font-face"));
            Assert.That(face.Declarations.Select(d => d.Name), Is.EqualTo(new[] { "font-family", "src" }));
            Assert.That(sheet.Rules.Count, Is.EqualTo(1));
        }

        [Test]
        public void StatementAtRulesWithoutBlocksAreRecorded()
        {
            Stylesheet sheet = Support.Css("@namespace url(http://x);@layer a,b;.x{}");
            Assert.That(sheet.AtRules.Select(a => a.Name), Is.EqualTo(new[] { "namespace", "layer" }));
            Assert.That(sheet.Rules.Count, Is.EqualTo(1));
        }

        [Test]
        public void CharsetIsDropped()
        {
            Stylesheet sheet = Support.Css("@charset \"utf-8\";.x{}");
            Assert.That(sheet.AtRules, Is.Empty);
        }

        [Test]
        public void MissingColonIsReportedAndLaterDeclarationsSurvive()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a{oops;color:red}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a {color:red}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
            Assert.That(bag.Items[0].Severity, Is.EqualTo(Severity.Error));
            Assert.That(bag.Items[0].Span.Start.Line, Is.EqualTo(1));
        }

        [Test]
        public void EmptyValueIsReported()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a{color:;margin:0}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a {margin:0}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void InvalidPropertyNameIsReported()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a{1x:2;color:red}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a {color:red}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void NestedRulesAreRejectedWithC028()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a{color:red;&:hover{color:blue}margin:0}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a {color:red;margin:0}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C028"));
        }

        [Test]
        public void UnclosedBlockIsReportedAndKept()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a{color:red;margin:0", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a {color:red;margin:0}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void StrayClosingBraceIsReported()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css("}.a{x:1}", "t.css", StyleOrigin.Page, bag);
            Assert.That(sheet.Rules.Count, Is.EqualTo(1));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void SelectorWithoutBlockIsReported()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a; .b{x:1}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".b {x:1}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void InvalidSelectorDropsOnlyThatSelector()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css(".a, ..b, .c{x:1}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".a,.c {x:1}"));
            Assert.That(Support.Ids(bag), Is.EqualTo("C029"));
        }

        [Test]
        public void RuleWithOnlyInvalidSelectorsIsDropped()
        {
            var bag = new DiagnosticBag();
            Stylesheet sheet = Support.Css("&{x:1}.ok{y:2}", "t.css", StyleOrigin.Page, bag);
            Assert.That(Summary(sheet), Is.EqualTo(".ok {y:2}"));
            Assert.That(bag.Count(Severity.Error), Is.EqualTo(1));
        }

        [Test]
        public void MalformedInputNeverThrows()
        {
            string[] inputs =
            {
                "{", "}", "@", "@media", "@media{", "@media screen{.a{", ".a{b:c", ".a{b:c;", "/*", "/* unterminated", ".a{b:\"x", "@import", "@keyframes{", "@keyframes x{from{", ":::", ".a{;;;}", ".a{:}", ".a{a:b:c}",
                "\0", ".a{b:c}}}}", "@media screen{@media print{", "[", "(((", ")))", ".a{b:url(", "a{b:c}\u0000d{e:f}", ".\\", "#", "@\\", ".a{b:\\}"
            };
            foreach (string input in inputs)
            {
                Assert.DoesNotThrow(() => Support.Css(input, "t.css", StyleOrigin.Page, new DiagnosticBag()), input);
            }
        }

        [Test]
        public void HugeSheetsParseQuickly()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5000; i++) sb.Append(".c").Append(i).Append("{margin:").Append(i % 10).Append("px;color:#fff}\n");
            Stylesheet sheet = Support.Css(sb.ToString());
            Assert.That(sheet.Rules.Count, Is.EqualTo(5000));
            Assert.That(sheet.Rules[4999].Span.Start.Line, Is.EqualTo(5000));
        }

        [Test]
        public void CommentsInsideValuesAndSelectorsAreIgnored()
        {
            Stylesheet sheet = Support.Css(".a /* x */ .b{margin:1px /* y */ 2px}");
            Assert.That(Summary(sheet), Is.EqualTo(".a .b {margin:1px 2px}"));
        }

        [Test]
        public void SupportsAndLayerBlocksKeepRules()
        {
            Stylesheet sheet = Support.Css("@layer base{.a{x:1}}@supports (display:grid){.b{y:2}}");
            Assert.That(sheet.Rules.Select(r => string.Join(">", r.AtContext)), Is.EqualTo(new[] { "@layer base", "@supports (display:grid)" }));
        }

        [Test]
        public void UnknownAtRuleBlocksKeepDeclarations()
        {
            Stylesheet sheet = Support.Css("@page{margin:1cm}");
            Assert.That(sheet.AtRules.Single().Declarations.Single().Name, Is.EqualTo("margin"));
        }
    }
}
