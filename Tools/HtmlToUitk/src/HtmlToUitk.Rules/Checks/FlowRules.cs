using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;

namespace HtmlToUitk.Rules
{
    public sealed class ProjectStyleRules : IRule
    {
        public string Id => "project-style";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            DialectSpec dialect = DialectSpec.Embedded;
            double minTouch = context.Config.Rules.MinTouchTarget;
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                IStyleModel model = scope.Page.Style;
                if (model == null) continue;
                foreach (DomElement e in scope.Elements())
                {
                    string utype = Dom.UxmlTypeOrDefault(e, dialect);
                    string tag = Dom.Tag(e);
                    string label = "<" + tag + " name=\"" + (e.Name ?? "") + "\">";
                    ResolvedStyle style = model.Resolve(e, PseudoStateSet.None);
                    if (dialect.Interactive.Contains(utype) && minTouch > 0)
                    {
                        double? height = Px(style, "height") ?? Px(style, "min-height");
                        double? width = Px(style, "width");
                        if (height != null && height.Value < minTouch)
                        {
                            r.Report("R002", e.Span, label, "height " + CssFormat.Number(height.Value) + "px is below the minimum " + CssFormat.Number(minTouch) + "px");
                        }
                        else if (width != null && width.Value < minTouch)
                        {
                            r.Report("R002", e.Span, label, "width " + CssFormat.Number(width.Value) + "px is below the minimum " + CssFormat.Number(minTouch) + "px");
                        }
                    }
                    bool inner = dialect.InnerTextTags.Contains(tag);
                    string mock = inner ? Dom.OwnText(e) : (Dom.Attr(e, "data-text") ?? "");
                    bool hasText = mock.Length > 0 || Dom.Has(e, "data-loc");
                    if (dialect.TextElements.Contains(utype) && hasText)
                    {
                        bool nowrap = Keyword(style, "white-space") == "nowrap";
                        bool ellipsis = Keyword(style, "text-overflow") == "ellipsis";
                        bool clipped = Keyword(style, "overflow") == "hidden" || (Keyword(style, "overflow-x") == "hidden" && Keyword(style, "overflow-y") == "hidden");
                        if (nowrap && !(ellipsis && clipped)) r.Report("R003", e.Span, label, "nowrap text without ellipsis and overflow: hidden");
                        if (ellipsis && !(nowrap && clipped)) r.Report("C030", e.Span, label, "text-overflow: ellipsis needs overflow: hidden and white-space: nowrap");
                    }
                }
            }
        }

        private static double? Px(ResolvedStyle style, string property)
        {
            ResolvedValue value;
            if (!style.Properties.TryGetValue(property, out value) || value.Value == null) return null;
            double n;
            string unit;
            if (!CssValues.TryDimension(value.Value, out n, out unit)) return null;
            if (unit == "px" || (unit == "" && n == 0)) return n;
            return null;
        }

        private static string Keyword(ResolvedStyle style, string property)
        {
            ResolvedValue value;
            if (!style.Properties.TryGetValue(property, out value) || value.Value == null) return null;
            return CssValues.Keyword(value.Value);
        }
    }

    public sealed class CrossFileRules : IRule
    {
        public string Id => "cross-file";

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            CheckUnusedRules(context, r);
            CheckModalTargets(context, r);
        }

        private static void CheckUnusedRules(RuleContext context, RuleReporter r)
        {
            var elements = new List<DomElement>();
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                elements.AddRange(scope.Elements());
                foreach (DomElement template in scope.Page.Html.Templates)
                {
                    elements.Add(template);
                    elements.AddRange(Dom.Descendants(template));
                }
            }
            if (elements.Count == 0) return;
            var naming = context.Config.Naming;
            Regex dynamicPattern = string.IsNullOrEmpty(naming.DynamicClassPattern) ? null : new Regex(naming.DynamicClassPattern, RegexOptions.CultureInvariant);
            var dynamicClasses = new HashSet<string>(naming.DynamicClasses ?? Array.Empty<string>(), StringComparer.Ordinal);
            foreach (Stylesheet sheet in SheetScope.Convertible(context.Input))
            {
                if (sheet.Origin == StyleOrigin.Page) continue;
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.Selectors.Count == 0 || OnlyCustomProperties(rule)) continue;
                    bool used = false;
                    foreach (Selector selector in rule.Selectors)
                    {
                        if (IsRoot(selector) || SubjectExempt(selector, dynamicClasses, dynamicPattern) || SubjectMatchesAny(selector, elements)) { used = true; break; }
                    }
                    if (!used)
                    {
                        Selector first = rule.Selectors[0];
                        r.Report("J005", rule.Span.IsNone ? first.Span : rule.Span, first.Text ?? "", "no element of any page matches " + (first.Text ?? ""));
                    }
                }
            }
        }

        private static bool OnlyCustomProperties(StyleRule rule)
        {
            if (rule.Declarations.Count == 0) return true;
            foreach (Declaration d in rule.Declarations)
            {
                if (!(d.Name ?? "").StartsWith("--", StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static bool IsRoot(Selector selector)
        {
            return (selector.Text ?? "").Trim() == ":root";
        }

        private static SimpleSelector Subject(Selector selector)
        {
            return selector.Parts.Count == 0 ? null : selector.Parts[selector.Parts.Count - 1].Part;
        }

        private static bool SubjectExempt(Selector selector, HashSet<string> dynamicClasses, Regex dynamicPattern)
        {
            SimpleSelector subject = Subject(selector);
            if (subject == null) return true;
            foreach (string cls in subject.Classes)
            {
                if (dynamicClasses.Contains(cls) || (dynamicPattern != null && dynamicPattern.IsMatch(cls))) return true;
            }
            return false;
        }

        private static bool SubjectMatchesAny(Selector selector, List<DomElement> elements)
        {
            SimpleSelector subject = Subject(selector);
            if (subject == null) return true;
            foreach (DomElement e in elements)
            {
                if (SubjectMatches(subject, e)) return true;
            }
            return false;
        }

        public static bool SubjectMatches(SimpleSelector part, DomElement e)
        {
            if (!string.IsNullOrEmpty(part.Id) && !string.Equals(part.Id, e.Name, StringComparison.Ordinal)) return false;
            if (!string.IsNullOrEmpty(part.Type) && !part.Universal && !string.Equals(part.Type, e.Tag, StringComparison.OrdinalIgnoreCase)) return false;
            IReadOnlyList<string> classes = e.Classes;
            foreach (string cls in part.Classes)
            {
                bool found = false;
                foreach (string c in classes)
                {
                    if (c == cls) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        private static void CheckModalTargets(RuleContext context, RuleReporter r)
        {
            var modals = new HashSet<string>(StringComparer.Ordinal);
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                foreach (DomElement e in scope.Elements())
                {
                    string id = Dom.Attr(e, "data-modal");
                    if (!string.IsNullOrEmpty(id)) modals.Add(id);
                }
            }
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                foreach (DomElement e in scope.Elements())
                {
                    string target = Dom.Attr(e, "data-open-modal");
                    if (string.IsNullOrEmpty(target) || modals.Contains(target)) continue;
                    r.Report("J008", e.Span, "data-open-modal=\"" + target + "\"", "no modal with the id '" + target + "' exists in the input set");
                }
            }
        }
    }

    public sealed class PageRules : IRule
    {
        private static readonly HashSet<string> Transitions = new HashSet<string>(StringComparer.Ordinal)
        {
            "fade", "slide-left", "slide-right", "slide-up", "scale", "none",
        };

        public string Id => "pages";

        private sealed class Page
        {
            public string Id;
            public DomElement Element;
            public bool Start;
            public bool Incoming;
            public bool Outgoing;
            public readonly List<string> Targets = new List<string>();
        }

        public void Evaluate(RuleContext context)
        {
            var r = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            DialectSpec dialect = DialectSpec.Embedded;
            var pages = new List<Page>();
            var byId = new Dictionary<string, Page>(StringComparer.Ordinal);
            var byElement = new Dictionary<DomElement, Page>();
            var all = new List<DomElement>();
            foreach (PageScope scope in PageScope.All(context.Input))
            {
                foreach (DomElement e in scope.Elements())
                {
                    all.Add(e);
                    string id = Dom.Attr(e, "data-page");
                    if (string.IsNullOrEmpty(id)) continue;
                    var page = new Page { Id = id, Element = e, Start = Dom.Has(e, "data-start") };
                    pages.Add(page);
                    byElement[e] = page;
                    if (!byId.ContainsKey(id)) byId[id] = page;
                }
            }
            if (pages.Count == 0) return;

            int errors = 0;
            Action<string, SourceSpan, string, string> error = (rule, span, construct, message) =>
            {
                if (r.Report(rule, span, construct, message) != null) errors++;
            };

            var starts = pages.FindAll(p => p.Start);
            if (starts.Count == 0) error("P001", pages[0].Element.Span, "data-page=\"" + pages[0].Id + "\"", "the flow has no data-start page");
            for (int i = 1; i < starts.Count; i++) error("P002", starts[i].Element.Span, "data-start", "more than one start page: '" + starts[0].Id + "' and '" + starts[i].Id + "'");

            var modalIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DomElement e in all)
            {
                Page owner = OwnerPage(e, byElement);
                string nav = Dom.Attr(e, "data-nav");
                if (nav != null)
                {
                    if (owner != null) { owner.Outgoing = true; owner.Targets.Add(nav); }
                    Page target;
                    if (byId.TryGetValue(nav, out target)) target.Incoming = true;
                    else error("P003", e.Span, "data-nav=\"" + nav + "\"", "data-nav points to the page '" + nav + "', which is not defined");
                }
                if (Dom.Has(e, "data-back"))
                {
                    if (owner != null)
                    {
                        owner.Outgoing = true;
                        if (owner.Start) r.Report("P006", e.Span, "data-back", "data-back on the start page '" + owner.Id + "' has nothing to return to");
                    }
                }
                if (Dom.Has(e, "data-open-modal") && owner != null) owner.Outgoing = true;
                string transition = Dom.Attr(e, "data-transition");
                if (transition != null && !Transitions.Contains(transition))
                {
                    error("P009", e.Span, "data-transition=\"" + transition + "\"", "'" + transition + "' is not a known page transition");
                }
                string modal = Dom.Attr(e, "data-modal");
                if (!string.IsNullOrEmpty(modal) && !modalIds.Add(modal))
                {
                    error("P007", e.Span, "data-modal=\"" + modal + "\"", "the modal id '" + modal + "' is used more than once");
                }
            }

            CheckTabs(all, byElement, error);

            foreach (Page page in pages)
            {
                if (!HasFocusable(page.Element, dialect))
                {
                    r.Report("P010", page.Element.Span, "data-page=\"" + page.Id + "\"", "page '" + page.Id + "' has no focusable element");
                }
            }

            var reachable = new HashSet<string>(StringComparer.Ordinal);
            if (starts.Count == 1)
            {
                var queue = new Queue<string>();
                queue.Enqueue(starts[0].Id);
                reachable.Add(starts[0].Id);
                while (queue.Count > 0)
                {
                    Page current;
                    if (!byId.TryGetValue(queue.Dequeue(), out current)) continue;
                    foreach (string next in current.Targets)
                    {
                        if (byId.ContainsKey(next) && reachable.Add(next)) queue.Enqueue(next);
                    }
                }
            }
            foreach (Page page in pages)
            {
                if (page.Start) continue;
                if (!page.Incoming && !page.Outgoing)
                {
                    r.Report("P004", page.Element.Span, "data-page=\"" + page.Id + "\"", "no navigation leads to or from the page '" + page.Id + "'");
                }
                else if (starts.Count == 1 && !reachable.Contains(page.Id))
                {
                    r.Report("P005", page.Element.Span, "data-page=\"" + page.Id + "\"", "page '" + page.Id + "' cannot be reached from the start page '" + starts[0].Id + "'");
                }
            }

            if (errors == 0)
            {
                r.Report("P011", pages[0].Element.Span, "data-page=\"" + pages[0].Id + "\"", "pages, modals and tabs are switched by the router component", null, null, null,
                    r.Rule("P011").Emulation);
            }
        }

        private static Page OwnerPage(DomElement e, Dictionary<DomElement, Page> byElement)
        {
            DomElement current = e;
            while (current != null)
            {
                Page page;
                if (byElement.TryGetValue(current, out page)) return page;
                current = current.Parent;
            }
            return null;
        }

        private static bool HasFocusable(DomElement pageElement, DialectSpec dialect)
        {
            foreach (DomElement e in Dom.Descendants(pageElement))
            {
                if (Dom.Has(e, "data-focus") || Dom.Has(e, "tabindex")) return true;
                string utype = Dom.UxmlTypeOrDefault(e, dialect);
                if (dialect.FocusableByDefault.Contains(utype)) return true;
            }
            return false;
        }

        private static void CheckTabs(List<DomElement> all, Dictionary<DomElement, Page> byElement, Action<string, SourceSpan, string, string> error)
        {
            foreach (DomElement e in all)
            {
                string group = Dom.Attr(e, "data-tabs");
                if (group != null)
                {
                    bool any = false;
                    foreach (DomElement inner in Dom.Descendants(e))
                    {
                        if (Dom.Has(inner, "data-tab")) { any = true; break; }
                    }
                    if (!any) error("P008", e.Span, "data-tabs=\"" + group + "\"", "the tab group '" + group + "' has no data-tab element");
                }
                string panel = Dom.Attr(e, "data-tab");
                if (panel == null) continue;
                bool inGroup = false;
                for (DomElement p = e.Parent; p != null; p = p.Parent)
                {
                    if (Dom.Has(p, "data-tabs")) { inGroup = true; break; }
                }
                if (!inGroup)
                {
                    error("P008", e.Span, "data-tab=\"" + panel + "\"", "data-tab is used outside of a data-tabs group");
                    continue;
                }
                Page owner = OwnerPage(e, byElement);
                DomElement scopeRoot = owner != null ? owner.Element : RootOf(e);
                bool found = false;
                foreach (DomElement candidate in Dom.Descendants(scopeRoot))
                {
                    if (string.Equals(candidate.Name, panel, StringComparison.Ordinal)) { found = true; break; }
                }
                if (!found) error("P008", e.Span, "data-tab=\"" + panel + "\"", "the tab panel '" + panel + "' does not exist");
            }
        }

        private static DomElement RootOf(DomElement e)
        {
            DomElement current = e;
            while (current.Parent != null) current = current.Parent;
            return current;
        }
    }
}
