using System;
using System.Linq;
using HtmlToUitk.Core.Html;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class HtmlParserTests
    {
        [TestCase("<div>hi</div>", "div[\"hi\"]")]
        [TestCase("<div class=\"a b\" name=x>t</div>", "div(class=a b,name=x)[\"t\"]")]
        [TestCase("<br>", "br")]
        [TestCase("<br/>", "br")]
        [TestCase("<br />", "br")]
        [TestCase("a<br>b", "\"a\" br \"b\"")]
        [TestCase("<img src=x.png alt=\"\">", "img(src=x.png,alt=)")]
        [TestCase("<input type=text><input>", "input(type=text) input")]
        [TestCase("<a href=foo>x</a>", "a(href=foo)[\"x\"]")]
        [TestCase("<div title='a \"b\"'></div>", "div(title=a \"b\")")]
        [TestCase("<button disabled>x</button>", "button(disabled=)[\"x\"]")]
        [TestCase("<DIV CLASS=A>x</DIV>", "div(class=A)[\"x\"]")]
        [TestCase("<SPAN>x</SPAN>", "span[\"x\"]")]
        [TestCase("<div a=1 a=2></div>", "div(a=1)")]
        [TestCase("<a href=\"x\"class=\"y\">z</a>", "a(href=x,class=y)[\"z\"]")]
        [TestCase("<a href = \"x\" >z</a>", "a(href=x)[\"z\"]")]
        [TestCase("<div data-x=1 :y=2 @z=3></div>", "div(data-x=1,:y=2,@z=3)")]
        [TestCase("<p>a &amp; b &lt;c&gt; &copy; &#65; &#x42;</p>", "p[\"a & b <c> \u00a9 A B\"]")]
        [TestCase("<a title=\"&quot;x&quot; &amp; y\"></a>", "a(title=\"x\" & y)")]
        [TestCase("<p>&zzzz; &amp</p>", "p[\"&zzzz; &\"]")]
        [TestCase("<p>a&nbsp;b</p>", "p[\"a\u00a0b\"]")]
        [TestCase("a<!-- c -->b", "\"ab\"")]
        [TestCase("<!DOCTYPE html><div></div>", "div")]
        [TestCase("<!doctype HTML PUBLIC \"x\"><div></div>", "div")]
        [TestCase("<?xml version=\"1.0\"?><div></div>", "div")]
        [TestCase("<![CDATA[x]]><div></div>", "div")]
        [TestCase("<p>a<p>b", "p[\"a\"] p[\"b\"]")]
        [TestCase("<p>a<div>b</div>", "p[\"a\"] div[\"b\"]")]
        [TestCase("<p>a<ul><li>b</ul>", "p[\"a\"] ul[li[\"b\"]]")]
        [TestCase("<ul><li>a<li>b</ul>", "ul[li[\"a\"] li[\"b\"]]")]
        [TestCase("<ul><li>a<ul><li>b</li></ul><li>c</ul>", "ul[li[\"a\" ul[li[\"b\"]]] li[\"c\"]]")]
        [TestCase("<dl><dt>a<dd>b<dt>c</dl>", "dl[dt[\"a\"] dd[\"b\"] dt[\"c\"]]")]
        [TestCase("<table><tr><td>a<td>b<tr><td>c</table>", "table[tr[td[\"a\"] td[\"b\"]] tr[td[\"c\"]]]")]
        [TestCase("<table><thead><tr><th>h<tbody><tr><td>d</table>", "table[thead[tr[th[\"h\"]]] tbody[tr[td[\"d\"]]]]")]
        [TestCase("<select><option>a<option>b</select>", "select[option[\"a\"] option[\"b\"]]")]
        [TestCase("<select><optgroup><option>a<optgroup><option>b</select>", "select[optgroup[option[\"a\"]] optgroup[option[\"b\"]]]")]
        [TestCase("<h1>a<h2>b", "h1[\"a\"] h2[\"b\"]")]
        [TestCase("<button>a<button>b</button>", "button[\"a\"] button[\"b\"]")]
        [TestCase("<div><span>x</div>y", "div[span[\"x\"]] \"y\"")]
        [TestCase("a</div>b", "\"ab\"")]
        [TestCase("a</>b", "\"ab\"")]
        [TestCase("</br>", "br")]
        [TestCase("a < b", "\"a < b\"")]
        [TestCase("1<2", "\"1<2\"")]
        [TestCase("<3 hearts", "\"<3 hearts\"")]
        [TestCase("a<", "\"a<\"")]
        [TestCase("<div>a<", "div[\"a<\"]")]
        [TestCase("<textarea>a<b>c</textarea>", "textarea[\"a<b>c\"]")]
        [TestCase("<textarea>\nx</textarea>", "textarea[\"x\"]")]
        [TestCase("<textarea>&lt;&amp;</textarea>", "textarea[\"<&\"]")]
        [TestCase("<pre>\nabc</pre>", "pre[\"abc\"]")]
        [TestCase("<pre>\n\nabc</pre>", "pre[\"\\nabc\"]")]
        [TestCase("<div>\n  <span>x</span>\n</div>", "div[\"\\n  \" span[\"x\"] \"\\n\"]")]
        [TestCase("  \n <div>x</div>\n", "div[\"x\"] \"\\n\"")]
        [TestCase("<span>a</span> <span>b</span>", "span[\"a\"] \" \" span[\"b\"]")]
        [TestCase("<b><i>x</b>y</i>", "b[i[\"x\"]] \"y\"")]
        [TestCase("<div></div></div>", "div")]
        [TestCase("<svg><path d=\"M0 0\"/></svg>", "svg[path(d=M0 0)]")]
        [TestCase("<custom-element foo=bar>x</custom-element>", "custom-element(foo=bar)[\"x\"]")]
        public void BuildsTheExpectedTree(string html, string expected)
        {
            Assert.That(Support.DumpBody(html), Is.EqualTo(expected));
        }

        [TestCase("a</div>b", "H001")]
        [TestCase("<div><span>x", "H002,H002")]
        [TestCase("<b><i>x</b>y</i>", "H002,H001")]
        [TestCase("<div/>x", "H004,H002")]
        [TestCase("a<!-- b", "H003")]
        [TestCase("a<div class=\"x", "H003,H003")]
        [TestCase("<div", "H003")]
        [TestCase("<p>a<p>b", "")]
        [TestCase("<ul><li>a<li>b</ul>", "")]
        [TestCase("<table><tr><td>a<td>b</table>", "")]
        [TestCase("<div></div>", "")]
        [TestCase("<br/><img src=x/>", "")]
        [TestCase("<script>unterminated", "H003")]
        [TestCase("<style>unterminated", "H003")]
        [TestCase("<title>x", "H003")]
        public void RecoveryIsReported(string html, string expectedIds)
        {
            var bag = new DiagnosticBag();
            Support.Html(html, bag);
            Assert.That(Support.Ids(bag), Is.EqualTo(expectedIds));
            Assert.That(bag.Items.All(d => d.Severity == Severity.Warning), Is.True);
        }

        [Test]
        public void ScriptContentIsRawAndCollected()
        {
            HtmlDocument document = Support.Html("<script>if (a<b && c>d) {}</script><div>x</div><script src=a.js></script>");
            Assert.That(document.Scripts.Count, Is.EqualTo(2));
            Assert.That(((DomText)document.Scripts[0].Children[0]).Text, Is.EqualTo("if (a<b && c>d) {}"));
            Assert.That(document.Scripts[1].GetAttribute("src"), Is.EqualTo("a.js"));
            Assert.That(document.Body.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void ScriptEndTagIsCaseInsensitiveAndIgnoresLookalikes()
        {
            HtmlDocument document = Support.Html("<script>var s='</scripty>';</SCRIPT ><div></div>");
            Assert.That(((DomText)document.Scripts[0].Children[0]).Text, Is.EqualTo("var s='</scripty>';"));
            Assert.That(document.Body.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void StyleBlocksKeepRawTextAndPosition()
        {
            HtmlDocument document = Support.Html("<html>\n<head>\n<style media=screen>\n.a>b{x:1}\n</style>\n</head></html>");
            StyleBlock block = document.StyleBlocks.Single();
            Assert.That(block.Text, Is.EqualTo("\n.a>b{x:1}\n"));
            Assert.That(block.Span.Start.Line, Is.EqualTo(3));
            Assert.That(block.Span.Start.Column, Is.EqualTo(21));
            Assert.That(block.Span.End.Line, Is.EqualTo(5));
            Assert.That(block.Attributes.Single().Name, Is.EqualTo("media"));
            Assert.That(document.Body.Children, Is.Empty);
        }

        [Test]
        public void StyleBlockPositionsMapIntoTheCssParser()
        {
            HtmlDocument document = Support.Html("<html>\n<style>\n.a { x: 1 }\n.b { y: 2 }\n</style></html>");
            StyleBlock block = document.StyleBlocks.Single();
            HtmlToUitk.Core.Css.Stylesheet sheet = new HtmlToUitk.Core.Css.CssParser().Parse(block.Text, "t.html", HtmlToUitk.Core.Css.StyleOrigin.Page, null, block.Span.Start);
            Assert.That(sheet.Rules[0].Span.Start.Line, Is.EqualTo(3));
            Assert.That(sheet.Rules[1].Span.Start.Line, Is.EqualTo(4));
            Assert.That(sheet.Rules[1].Declarations[0].Span.Start.Column, Is.EqualTo(6));
        }

        [Test]
        public void TitleIsDecodedAndCollapsed()
        {
            HtmlDocument document = Support.Html("<title>  My  &amp; \n Title </title><div></div>");
            Assert.That(document.Title, Is.EqualTo("My & Title"));
            Assert.That(document.Body.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void MetaLinkAndStyleOrderAreCollected()
        {
            HtmlDocument document = Support.Html(
                "<meta charset=utf-8><meta name=uitk:screen content=Main>" +
                "<link rel=stylesheet href=a.css><style>.a{}</style><link rel=\"icon\" href=x.png><link rel=\"Stylesheet preload\" href=b.css>");
            Assert.That(document.Metas.Count, Is.EqualTo(2));
            Assert.That(document.Metas[1].GetAttribute("content"), Is.EqualTo("Main"));
            Assert.That(document.Links.Select(l => l.Href), Is.EqualTo(new[] { "a.css", "x.png", "b.css" }));
            Assert.That(document.Links[1].Rel, Is.EqualTo("icon"));
            Assert.That(document.StyleOrder.Select(s => s.Kind + ":" + s.Index), Is.EqualTo(new[] { "Link:0", "Style:0", "Link:2" }));
        }

        [Test]
        public void LinksInTheBodyStillCount()
        {
            HtmlDocument document = Support.Html("<div><link rel=stylesheet href=late.css></div>");
            Assert.That(document.Links.Count, Is.EqualTo(1));
            Assert.That(document.StyleOrder.Count, Is.EqualTo(1));
            Assert.That(Support.Dump(document.Body.Children[0]), Is.EqualTo("div"));
        }

        [Test]
        public void HtmlHeadAndBodyTagsAreFolded()
        {
            HtmlDocument document = Support.Html("<!DOCTYPE html><html lang=en data-x=1><head><title>t</title></head><body class=main id=b><div>x</div></body></html>");
            Assert.That(Support.Dump(document.Body.Children.Single()), Is.EqualTo("div[\"x\"]"));
            Assert.That(document.Body.Attributes.Select(a => a.Name + "=" + a.Value), Is.EqualTo(new[] { "class=main", "id=b" }));
            Assert.That(document.HtmlAttributes.Select(a => a.Name + "=" + a.Value), Is.EqualTo(new[] { "lang=en", "data-x=1" }));
            Assert.That(document.Body.Parent, Is.Null);
            Assert.That(document.Body.Tag, Is.EqualTo("body"));
        }

        [Test]
        public void RepeatedBodyTagsMergeAttributes()
        {
            HtmlDocument document = Support.Html("<body a=1><div></div><body a=2 b=3>");
            Assert.That(document.Body.Attributes.Select(a => a.Name + "=" + a.Value), Is.EqualTo(new[] { "a=1", "b=3" }));
        }

        [Test]
        public void TemplatesAreDetachedFromTheTree()
        {
            HtmlDocument document = Support.Html("<template id=t><div>x</div><span>y</span></template><div>z</div>");
            Assert.That(document.Templates.Count, Is.EqualTo(1));
            Assert.That(Support.Dump(document.Templates[0]), Is.EqualTo("template(id=t)[div[\"x\"] span[\"y\"]]"));
            Assert.That(document.Body.Children.Count, Is.EqualTo(1));
            Assert.That(document.Templates[0].Children.OfType<DomElement>().All(c => ReferenceEquals(c.Parent, document.Templates[0])), Is.True);
        }

        [Test]
        public void NestedTemplatesAreBothCollected()
        {
            HtmlDocument document = Support.Html("<template id=outer><template id=inner><b>x</b></template></template>");
            Assert.That(document.Templates.Select(t => t.GetAttribute("id")), Is.EqualTo(new[] { "outer", "inner" }));
        }

        [Test]
        public void ElementSpansCoverTheirMarkup()
        {
            HtmlDocument document = Support.Html("<div>\n  <span a=1>x</span>\n</div>");
            DomElement div = (DomElement)document.Body.Children[0];
            DomElement span = (DomElement)div.Children[1];
            Assert.That(div.Span.Start.Line, Is.EqualTo(1));
            Assert.That(div.Span.Start.Column, Is.EqualTo(1));
            Assert.That(div.Span.End.Line, Is.EqualTo(3));
            Assert.That(div.Span.End.Column, Is.EqualTo(7));
            Assert.That(span.Span.Start.Line, Is.EqualTo(2));
            Assert.That(span.Span.Start.Column, Is.EqualTo(3));
            Assert.That(span.Span.End.Column, Is.EqualTo(21));
            Assert.That(span.Attributes[0].Span.Start.Column, Is.EqualTo(9));
            Assert.That(span.Attributes[0].Span.End.Column, Is.EqualTo(12));
            Assert.That(span.Span.File, Is.EqualTo("test.html"));
        }

        [Test]
        public void VoidElementSpansEndAtTheTag()
        {
            HtmlDocument document = Support.Html("<br><img src=x>");
            Assert.That(((DomElement)document.Body.Children[1]).Span.End.Column, Is.EqualTo(16));
        }

        [Test]
        public void TextSpansPointAtTheSourceText()
        {
            HtmlDocument document = Support.Html("<p>one\ntwo</p>");
            var text = (DomText)((DomElement)document.Body.Children[0]).Children[0];
            Assert.That(text.Text, Is.EqualTo("one\ntwo"));
            Assert.That(text.Span.Start.Column, Is.EqualTo(4));
            Assert.That(text.Span.End.Line, Is.EqualTo(2));
        }

        [Test]
        public void NamePrefersNameThenId()
        {
            HtmlDocument document = Support.Html("<div id=i name=n></div><div id=only></div><div></div>");
            var elements = Support.All(document.Body);
            Assert.That(elements.Select(e => e.Name), Is.EqualTo(new[] { "n", "only", null }));
        }

        [Test]
        public void ClassesSplitOnAnyWhitespace()
        {
            HtmlDocument document = Support.Html("<div class=\"  a\t b\n c  \"></div>");
            Assert.That(((DomElement)document.Body.Children[0]).Classes, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void ClassesFollowAttributeChanges()
        {
            HtmlDocument document = Support.Html("<div class=a></div>");
            var element = (DomElement)document.Body.Children[0];
            Assert.That(element.Classes, Is.EqualTo(new[] { "a" }));
            element.Attributes = new[] { new DomAttribute { Name = "class", Value = "b c" } };
            Assert.That(element.Classes, Is.EqualTo(new[] { "b", "c" }));
        }

        [Test]
        public void ParentAndChildElementViewsAreConsistent()
        {
            HtmlDocument document = Support.Html("<div name=a>t<span name=b></span>u<em name=c></em></div>");
            var a = (DomElement)document.Body.Children[0];
            IElement element = a;
            Assert.That(element.ChildElements.Select(c => c.Name), Is.EqualTo(new[] { "b", "c" }));
            Assert.That(ReferenceEquals(element.ChildElements[0].Parent, a), Is.True);
            Assert.That(ReferenceEquals(element.Parent, document.Body), Is.True);
            Assert.That(element.TypeName, Is.EqualTo("div"));
            Assert.That(element.GetAttribute("name"), Is.EqualTo("a"));
            Assert.That(element.GetAttribute("zzz"), Is.Null);
        }

        [Test]
        public void DeepNestingIsCappedWithoutOverflow()
        {
            var bag = new DiagnosticBag();
            string html = string.Concat(Enumerable.Repeat("<div>", 5000)) + "x";
            HtmlDocument document = Support.Html(html, bag);
            Assert.That(bag.Items.Any(d => d.RuleId == "H005"), Is.True);
            int depth = 0;
            DomElement cursor = document.Body;
            while (cursor.Children.OfType<DomElement>().Any())
            {
                cursor = cursor.Children.OfType<DomElement>().First();
                depth++;
            }
            Assert.That(depth, Is.LessThanOrEqualTo(513));
        }

        [Test]
        public void ManySiblingsParse()
        {
            string html = string.Concat(Enumerable.Repeat("<li>a</li>", 20000));
            HtmlDocument document = Support.Html("<ul>" + html + "</ul>");
            Assert.That(((DomElement)document.Body.Children[0]).Children.Count, Is.EqualTo(20000));
        }

        [TestCase("")]
        [TestCase("<")]
        [TestCase("<<<<")]
        [TestCase("</")]
        [TestCase("<!")]
        [TestCase("<!-")]
        [TestCase("<!--")]
        [TestCase("<div <div <div")]
        [TestCase("<a b='c")]
        [TestCase("<a b=\"c")]
        [TestCase("&")]
        [TestCase("&#")]
        [TestCase("&#x")]
        [TestCase("&#99999999999999999999;")]
        [TestCase("<div>&amp;&amp;&amp;&amp;")]
        [TestCase("\0")]
        [TestCase("<\0>")]
        [TestCase("<a\0b>")]
        [TestCase("</div")]
        [TestCase("<template><template><template>")]
        [TestCase("<style><style><style>")]
        [TestCase("<textarea><textarea>")]
        [TestCase("<table><tr><td><table><tr><td>")]
        public void HostileInputNeverThrows(string html)
        {
            Assert.DoesNotThrow(() => Support.Html(html, new DiagnosticBag()), html);
        }

        [Test]
        public void NullTextIsTreatedAsEmpty()
        {
            HtmlDocument document = new HtmlParser().Parse(null, "x.html", null);
            Assert.That(document.Body.Children, Is.Empty);
            Assert.That(document.Path, Is.EqualTo("x.html"));
        }

        [Test]
        public void RealisticPageParsesCompletely()
        {
            string html =
                "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"uitk:screen\" content=\"Minimal\">\n" +
                "<link rel=\"stylesheet\" href=\"../base.css\" data-uitk-harness>\n<style>\n.mn { top: 0; }\n</style>\n</head>\n<body>\n" +
                "<div name=\"mn-root\" class=\"mn\">\n  <span name=\"mn-title\" class=\"mn__title\" data-loc=\"minimal.title\">Welcome</span>\n" +
                "  <button name=\"mn-action\" class=\"mn__action\" data-loc=\"minimal.action\">Start</button>\n</div>\n</body>\n</html>\n";
            var bag = new DiagnosticBag();
            HtmlDocument document = Support.Html(html, bag);
            Assert.That(bag.Items, Is.Empty);
            Assert.That(Support.All(document.Body).Select(e => e.Tag), Is.EqualTo(new[] { "div", "span", "button" }));
            Assert.That(document.Metas.Count, Is.EqualTo(2));
            Assert.That(document.Links[0].Attributes.Any(a => a.Name == "data-uitk-harness"), Is.True);
            Assert.That(document.StyleOrder.Select(s => s.Kind), Is.EqualTo(new[] { StyleSourceKind.Link, StyleSourceKind.Style }));
            DomElement button = Support.ByName(document, "mn-action");
            Assert.That(button.GetAttribute("data-loc"), Is.EqualTo("minimal.action"));
            Assert.That(button.Span.Start.Line, Is.EqualTo(14));
        }
    }
}
