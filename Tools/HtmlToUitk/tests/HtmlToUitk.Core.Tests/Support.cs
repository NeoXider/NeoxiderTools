using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;

namespace HtmlToUitk.Core.Tests
{
    internal static class Support
    {
        public static Stylesheet Css(string text, string path = "test.css", StyleOrigin origin = StyleOrigin.Page, IDiagnosticSink sink = null)
        {
            return CssParser.Instance.Parse(text, path, origin, sink);
        }

        public static HtmlDocument Html(string text, IDiagnosticSink sink = null)
        {
            return new HtmlParser().Parse(text, "test.html", sink);
        }

        public static string Ids(DiagnosticBag bag) => string.Join(",", bag.Items.Select(d => d.RuleId));

        public static string Dump(DomNode node)
        {
            var text = node as DomText;
            if (text != null) return "\"" + text.Text.Replace("\n", "\\n") + "\"";
            var element = (DomElement)node;
            var sb = new StringBuilder(element.Tag);
            if (element.Attributes.Count > 0)
            {
                sb.Append('(').Append(string.Join(",", element.Attributes.Select(a => a.Name + "=" + a.Value))).Append(')');
            }
            if (element.Children.Count > 0)
            {
                sb.Append('[').Append(string.Join(" ", element.Children.Select(Dump))).Append(']');
            }
            return sb.ToString();
        }

        public static string DumpBody(string html, IDiagnosticSink sink = null)
        {
            HtmlDocument document = Html(html, sink);
            return string.Join(" ", document.Body.Children.Select(Dump));
        }

        public static List<DomElement> All(DomElement root)
        {
            var list = new List<DomElement>();
            foreach (DomNode node in root.Children)
            {
                var element = node as DomElement;
                if (element == null) continue;
                list.Add(element);
                list.AddRange(All(element));
            }
            return list;
        }

        public static DomElement ByName(HtmlDocument document, string name)
        {
            return All(document.Body).FirstOrDefault(e => e.Name == name);
        }

        public static string Query(string html, string selectorText, PseudoStateSet states = PseudoStateSet.None)
        {
            HtmlDocument document = Html(html);
            Selector selector = SelectorParser.Parse(selectorText);
            if (selector == null) return "<invalid>";
            var names = new List<string>();
            foreach (DomElement element in All(document.Body))
            {
                if (SelectorMatcher.Matches(selector, element, states)) names.Add(element.Name ?? element.Tag);
            }
            return string.Join(" ", names);
        }

        public static Declaration Decl(string declarationText)
        {
            Stylesheet sheet = Css(".x{" + declarationText + "}");
            return sheet.Rules[0].Declarations[0];
        }

        public static string Expand(string declarationText, bool reset = false)
        {
            IReadOnlyList<Declaration> result = ShorthandExpander.Expand(Decl(declarationText), reset ? ShorthandOptions.Cascade : ShorthandOptions.Default);
            return string.Join("; ", result.Select(d => d.Name + ": " + d.Value.ToText() + (d.Important ? " !important" : "")));
        }

        public static IStyleModel Model(string[] css, DiagnosticBag bag = null, StyleOptions options = null)
        {
            var sheets = new List<Stylesheet>();
            for (int i = 0; i < css.Length; i++) sheets.Add(Css(css[i], "sheet" + (i + 1) + ".css", i == 0 ? StyleOrigin.Page : StyleOrigin.Linked, bag));
            return new StyleEngine().Build(sheets, options ?? new StyleOptions(), bag);
        }

        public static string Resolve(string html, string elementName, PseudoStateSet states, params string[] css)
        {
            HtmlDocument document = Html(html);
            IStyleModel model = Model(css);
            DomElement element = ByName(document, elementName);
            if (element == null) throw new InvalidOperationException("no element named " + elementName);
            ResolvedStyle style = model.Resolve(element, states);
            return string.Join("; ", style.Properties.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ": " + p.Value.Value.ToText() + (p.Value.Inherited ? " (inherited)" : "")));
        }

        public static string RepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "fixtures")) && Directory.Exists(Path.Combine(dir, "src"))) return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        public static string Lf(string text) => text.Replace("\r\n", "\n");
    }
}
