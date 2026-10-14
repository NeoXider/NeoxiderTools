using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Rules
{
    internal static class UrlChecks
    {
        public static void Check(RuleReporter reporter, IFileSystem fs, string baseDirectory, SourceSpan span, string raw)
        {
            string url = (raw ?? "").Trim();
            string low = url.ToLowerInvariant();
            string shown = url.Length <= 60 ? url : url.Substring(0, 60);
            bool projectScheme = low.StartsWith("project://", StringComparison.Ordinal) || low.StartsWith("resource(", StringComparison.Ordinal);
            bool rooted = low.StartsWith("/", StringComparison.Ordinal) && !low.StartsWith("//", StringComparison.Ordinal);
            if (projectScheme || rooted) return;
            if (low.StartsWith("http:", StringComparison.Ordinal) || low.StartsWith("https:", StringComparison.Ordinal) || low.StartsWith("data:", StringComparison.Ordinal)
                || low.StartsWith("file:", StringComparison.Ordinal) || low.StartsWith("//", StringComparison.Ordinal))
            {
                reporter.Report("D012", span, "url(" + shown + ")", "image '" + shown + "' is not inside the project");
                return;
            }
            if (fs == null || baseDirectory == null) return;
            string full = PathMath.Combine(baseDirectory, url);
            if (!fs.FileExists(full))
            {
                reporter.Report("D012", span, "url(" + shown + ")", "image file '" + shown + "' does not exist");
            }
        }
    }

    public sealed class HtmlDialectRules : IRule
    {
        private static readonly string[] HarnessScripts = { "uitk-validate.js", "uitk-prototype.js" };

        private readonly IFileSystem _fs;

        public HtmlDialectRules(IFileSystem fs = null)
        {
            _fs = fs;
        }

        public string Id => "html-dialect";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            DialectSpec dialect = DialectSpec.Embedded;
            var inline = AllowedInlineStyles(context);
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                HtmlDocument html = scope.Page.Html;
                foreach (LinkRef link in html.Links)
                {
                    if (!string.Equals(link.Rel ?? "", "stylesheet", StringComparison.OrdinalIgnoreCase)) continue;
                    if (HasAttribute(link.Attributes, "data-uitk-harness") || HasAttribute(link.Attributes, "data-uss")) continue;
                    r.Report("D010", link.Span, "<link href=\"" + (link.Href ?? "") + "\">", "stylesheet '" + (link.Href ?? "") + "' is not convertible");
                }
                foreach (DomElement script in html.Scripts)
                {
                    string src = Dom.Attr(script, "src") ?? "";
                    if (Dom.Has(script, "data-uitk-harness") || EndsWithAny(src, HarnessScripts)) continue;
                    r.Report("D009", script.Span, "<script" + (src.Length > 0 ? " src=\"" + src + "\"" : "") + ">", "script is not convertible");
                }
                foreach (DomNode child in scope.Body.Children)
                {
                    var text = child as DomText;
                    if (text != null && text.Text.Trim().Length > 0)
                    {
                        r.Report("D006", text.Span, "text \"" + Truncate(Dom.NormalizeText(text.Text), 40) + "\"", "text directly inside <body>");
                    }
                }
                foreach (DomElement element in Walk(scope.Body)) CheckElement(r, dialect, inline, scope, element);
            }
        }

        private static IEnumerable<DomElement> Walk(DomElement root)
        {
            foreach (DomElement child in Dom.ChildElements(root))
            {
                yield return child;
                string tag = Dom.Tag(child);
                if (tag == "select" || tag == "progress" || tag == "textarea") continue;
                foreach (DomElement inner in Walk(child)) yield return inner;
            }
        }

        private static bool HasAttribute(IReadOnlyList<DomAttribute> attributes, string name)
        {
            foreach (DomAttribute a in attributes)
            {
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool EndsWithAny(string text, string[] suffixes)
        {
            foreach (string suffix in suffixes)
            {
                if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string Truncate(string text, int length)
        {
            return text.Length <= length ? text : text.Substring(0, length);
        }

        private static string NormalStyle(string text)
        {
            return Regex.Replace((text ?? "").ToLowerInvariant(), @"\s+", "").TrimEnd(';');
        }

        private static HashSet<string> AllowedInlineStyles(RuleContext context)
        {
            var set = new HashSet<string>(StringComparer.Ordinal) { "display:none" };
            if (context.Config.Uxml != null && context.Config.Uxml.AllowInlineStyles != null)
            {
                foreach (string s in context.Config.Uxml.AllowInlineStyles) set.Add(NormalStyle(s));
            }
            return set;
        }

        private void CheckElement(RuleReporter r, DialectSpec dialect, HashSet<string> inlineAllowed, PageScope scope, DomElement e)
        {
            string tag = Dom.Tag(e);
            string problem;
            string utype = Dom.UxmlType(e, dialect, out problem);
            string dataUxml = Dom.Attr(e, "data-uxml");
            if (problem == "data-uxml")
            {
                r.Report("D003", e.Span, "data-uxml=\"" + dataUxml + "\"", "'" + dataUxml + "' is not a known UXML element of the dialect");
            }
            if (utype == null)
            {
                if (tag == "input")
                {
                    string itype = (Dom.Attr(e, "type") ?? "text").ToLowerInvariant();
                    r.Report("D002", e.Span, "<input type=\"" + itype + "\">", "input type '" + itype + "' has no UI Toolkit control");
                }
                else
                {
                    string advice;
                    dialect.TagAdvice.TryGetValue(tag, out advice);
                    r.Report("D001", e.Span, "<" + tag + ">", "<" + tag + "> has no UI Toolkit counterpart", advice);
                }
            }
            string effective = utype ?? "VisualElement";

            foreach (DomAttribute a in e.Attributes)
            {
                string key = (a.Name ?? "").ToLowerInvariant();
                string value = a.Value ?? "";
                if (key.StartsWith("on", StringComparison.Ordinal))
                {
                    r.Report("D005", a.Span.IsNone ? e.Span : a.Span, key + "=\"" + Truncate(value, 30) + "\"", "event handler attribute '" + key + "'");
                }
                else if (key == "href")
                {
                    r.Report("D005", a.Span.IsNone ? e.Span : a.Span, "href=\"" + Truncate(value, 30) + "\"", "link attribute 'href'");
                }
                else if (key == "style")
                {
                    if (!inlineAllowed.Contains(NormalStyle(value)))
                    {
                        r.Report("D011", a.Span.IsNone ? e.Span : a.Span, "style=\"" + Truncate(value, 60) + "\"", "inline style '" + Truncate(value, 60) + "'");
                    }
                }
                else if (key.StartsWith("data-", StringComparison.Ordinal) || dialect.SafeAttributes.Contains(key) || key == "class")
                {
                    continue;
                }
                else
                {
                    r.Report("D004", a.Span.IsNone ? e.Span : a.Span, key + "=\"" + Truncate(value, 30) + "\"", "attribute '" + key + "' is ignored by the conversion");
                }
            }
            if (Dom.Has(e, "step") && (effective == "Slider" || effective == "SliderInt" || effective == "MinMaxSlider"))
            {
                r.Report("D004", e.Span, "step=\"" + Dom.Attr(e, "step") + "\"", "attribute 'step' has no UXML slider equivalent");
            }

            bool inner = dialect.InnerTextTags.Contains(tag);
            if (!inner)
            {
                foreach (DomNode child in e.Children)
                {
                    var text = child as DomText;
                    if (text != null && text.Text.Trim().Length > 0 && tag != "option")
                    {
                        r.Report("D006", text.Span, "text \"" + Truncate(Dom.NormalizeText(text.Text), 40) + "\"", "text inside <" + tag + "> (" + effective + ")");
                        break;
                    }
                }
            }

            if (tag == "select")
            {
                foreach (DomElement child in Dom.ChildElements(e))
                {
                    if (Dom.Tag(child) != "option") r.Report("D001", child.Span, "<" + Dom.Tag(child) + ">", "<" + Dom.Tag(child) + "> inside <select> is not supported");
                }
                return;
            }
            if (effective == "Label")
            {
                foreach (DomElement child in Dom.ChildElements(e))
                {
                    r.Report("D007", child.Span, "<" + Dom.Tag(child) + "> inside <" + tag + ">", "Label <" + tag + "> contains <" + Dom.Tag(child) + ">");
                    break;
                }
            }
            if (effective == "Button")
            {
                foreach (DomElement child in Dom.ChildElements(e))
                {
                    r.Report("D008", e.Span, "<button>", "button contains <" + Dom.Tag(child) + ">");
                    break;
                }
            }
            if (effective == "Image")
            {
                string name = e.Name;
                if (string.IsNullOrEmpty(name))
                {
                    r.Report("D013", e.Span, "<img>", "<img> needs a name so its image can be bound by a #name rule");
                }
                else
                {
                    string src = Dom.Attr(e, "src");
                    if (!string.IsNullOrEmpty(src)) UrlChecks.Check(r, _fs, Dom.Directory(scope.Path), e.Span, src);
                }
            }
        }
    }

    public sealed class NamingRules : IRule
    {
        public string Id => "naming";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            DialectSpec dialect = DialectSpec.Embedded;
            var naming = context.Config.Naming;
            var prefixes = naming.Prefixes ?? Array.Empty<string>();
            var namePattern = new Regex(naming.NamePattern, RegexOptions.CultureInvariant);
            var classPattern = new Regex(naming.ClassPattern, RegexOptions.CultureInvariant);
            Regex dynamicPattern = string.IsNullOrEmpty(naming.DynamicClassPattern) ? null : new Regex(naming.DynamicClassPattern, RegexOptions.CultureInvariant);
            var dynamicClasses = new HashSet<string>(naming.DynamicClasses ?? Array.Empty<string>(), StringComparer.Ordinal);
            string undefinedMode = context.Config.Rules.UndefinedClass ?? "warn";

            foreach (PageScope scope in PageScope.All(context.Input))
            {
                var names = new Dictionary<string, int>(StringComparer.Ordinal);
                var usedClasses = new Dictionary<string, DomElement>(StringComparer.Ordinal);
                var usedOrder = new List<string>();
                foreach (DomElement e in scope.Elements())
                {
                    string tag = Dom.Tag(e);
                    string utype = Dom.UxmlTypeOrDefault(e, dialect);
                    string nameAttr = Dom.Attr(e, "name");
                    string idAttr = Dom.Attr(e, "id");
                    if (!string.IsNullOrEmpty(nameAttr) && !string.IsNullOrEmpty(idAttr) && nameAttr != idAttr)
                    {
                        r.Report("N006", e.Span, "name=\"" + nameAttr + "\" id=\"" + idAttr + "\"", "name '" + nameAttr + "' and id '" + idAttr + "' differ");
                    }
                    string name = !string.IsNullOrEmpty(nameAttr) ? nameAttr : (string.IsNullOrEmpty(idAttr) ? null : idAttr);
                    if (name != null)
                    {
                        int previous;
                        if (names.TryGetValue(name, out previous))
                        {
                            r.Report("N001", e.Span, "name=\"" + name + "\"", "name '" + name + "' is already used at line " + previous);
                        }
                        else
                        {
                            names[name] = e.Span.Start.Line;
                        }
                        if (!namePattern.IsMatch(name))
                        {
                            r.Report("N002", e.Span, "name=\"" + name + "\"", "name '" + name + "' is not kebab-case");
                        }
                        else if (prefixes.Count > 0 && !HasPrefix(name, prefixes))
                        {
                            r.Report("N005", e.Span, "name=\"" + name + "\"", "name '" + name + "' has none of the prefixes " + string.Join(", ", prefixes));
                        }
                    }
                    foreach (string cls in e.Classes)
                    {
                        if (!classPattern.IsMatch(cls))
                        {
                            r.Report("N004", e.Span, "class=\"" + cls + "\"", "class '" + cls + "' does not match " + naming.ClassPattern);
                        }
                        else if (prefixes.Count > 0 && !HasPrefix(cls, prefixes) && !cls.StartsWith("unity-", StringComparison.Ordinal))
                        {
                            r.Report("N005", e.Span, "class=\"" + cls + "\"", "class '" + cls + "' has none of the prefixes " + string.Join(", ", prefixes));
                        }
                        if (!usedClasses.ContainsKey(cls))
                        {
                            usedClasses[cls] = e;
                            usedOrder.Add(cls);
                        }
                    }
                    if (dialect.Interactive.Contains(utype) && string.IsNullOrEmpty(name))
                    {
                        r.Report("N003", e.Span, "<" + tag + ">", "interactive <" + tag + "> (" + utype + ") has no name");
                    }
                }

                if (undefinedMode == "off") continue;
                var defined = DefinedClasses(scope.Page);
                foreach (string cls in usedOrder)
                {
                    if (defined.Contains(cls) || dynamicClasses.Contains(cls) || cls.StartsWith("unity-", StringComparison.Ordinal)) continue;
                    if (dynamicPattern != null && dynamicPattern.IsMatch(cls)) continue;
                    r.Report("N007", usedClasses[cls].Span, "class=\"" + cls + "\"", "class '" + cls + "' has no style rule", null,
                        undefinedMode == "fail" ? Severity.Error : Severity.Warning);
                }
            }
        }

        public static bool HasPrefix(string text, IReadOnlyList<string> prefixes)
        {
            foreach (string prefix in prefixes)
            {
                if (text == prefix || text.StartsWith(prefix + "-", StringComparison.Ordinal) || text.StartsWith(prefix + "__", StringComparison.Ordinal)
                    || text.StartsWith(prefix + "--", StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void CollectClasses(Selector selector, HashSet<string> set)
        {
            foreach (var part in selector.Parts)
            {
                if (part.Part == null) continue;
                foreach (string cls in part.Part.Classes) set.Add(cls);
                foreach (PseudoClass pseudo in part.Part.Pseudos)
                {
                    foreach (Selector inner in pseudo.Selectors) CollectClasses(inner, set);
                }
            }
        }

        public static HashSet<string> DefinedClasses(AnalysisPage page)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            IEnumerable<Stylesheet> sheets = page.Cascade;
            foreach (Stylesheet sheet in sheets)
            {
                foreach (StyleRule rule in sheet.Rules)
                {
                    foreach (Selector selector in rule.Selectors)
                    {
                        CollectClasses(selector, set);
                    }
                }
            }
            return set;
        }
    }
}
