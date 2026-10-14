using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Spec;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert.Dialect
{
    internal static class NavCollector
    {
        private sealed class Context
        {
            public readonly List<NavPage> Pages = new List<NavPage>();
            public readonly List<NavEdge> Edges = new List<NavEdge>();
            public readonly List<NavModal> Modals = new List<NavModal>();
            public readonly List<NavTabGroup> Tabs = new List<NavTabGroup>();
            public string Start;
            public readonly Dictionary<string, List<string>> CloseTriggers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            public readonly List<string> ModalOrder = new List<string>();
        }

        public static NavModel Collect(PageState page, ConversionConfig config)
        {
            var context = new Context();
            Walk(page.Html.Body, null, null, context);
            var modals = new List<NavModal>();
            foreach (NavModal modal in context.Modals)
            {
                List<string> triggers;
                context.CloseTriggers.TryGetValue(modal.Id + "\u0001" + modal.Element, out triggers);
                modals.Add(new NavModal
                {
                    Id = modal.Id,
                    Element = modal.Element,
                    OwnerPage = modal.OwnerPage,
                    CloseTriggers = triggers ?? new List<string>()
                });
            }

            return new NavModel
            {
                Document = page.ScreenName,
                Start = context.Start,
                Pages = context.Pages,
                Edges = context.Edges,
                Modals = modals,
                Tabs = context.Tabs
            };
        }

        public static IReadOnlyList<PageDef> PageDefs(NavModel nav, PageState page)
        {
            var list = new List<PageDef>();
            if (nav == null) return list;
            foreach (NavPage p in nav.Pages)
            {
                list.Add(new PageDef { Id = p.Id, ElementName = p.Element, IsStart = p.Id == nav.Start, Span = SpanOf(page, p.Element) });
            }

            return list;
        }

        private static SourceSpan SpanOf(PageState page, string name)
        {
            foreach (DomNode node in Descendants(page.Html.Body))
            {
                DomElement element = node as DomElement;
                if (element != null && element.Name == name) return element.Span;
            }

            return SourceSpan.None;
        }

        private static IEnumerable<DomNode> Descendants(DomElement element)
        {
            foreach (DomNode child in element.Children)
            {
                yield return child;
                DomElement e = child as DomElement;
                if (e != null)
                {
                    foreach (DomNode d in Descendants(e)) yield return d;
                }
            }
        }

        private static void Walk(DomElement element, string pageId, string modalKey, Context context)
        {
            foreach (DomNode node in element.Children)
            {
                DomElement child = node as DomElement;
                if (child == null) continue;
                Visit(child, pageId, modalKey, context);
            }
        }

        private static void Visit(DomElement el, string pageId, string modalKey, Context context)
        {
            string name = el.Name;
            string dataPage = UxmlMapper.Attr(el, "data-page");
            if (dataPage != null)
            {
                string id = dataPage.Trim();
                string transition = UxmlMapper.Attr(el, "data-transition");
                string ms = UxmlMapper.Attr(el, "data-transition-ms");
                var navPage = new NavPage
                {
                    Id = id,
                    Element = name,
                    Focus = UxmlMapper.Attr(el, "data-focus") ?? FirstInteractive(el),
                    TransitionIn = transition,
                    TransitionOut = transition,
                    TransitionMs = ParseMs(ms)
                };
                context.Pages.Add(navPage);
                if (UxmlMapper.HasAttr(el, "data-start") && context.Start == null) context.Start = id;
                pageId = id;
            }

            string modal = UxmlMapper.Attr(el, "data-modal");
            if (modal != null)
            {
                var m = new NavModal { Id = modal.Trim(), Element = name, OwnerPage = pageId };
                context.Modals.Add(m);
                modalKey = m.Id + "\u0001" + m.Element;
            }

            string nav = UxmlMapper.Attr(el, "data-nav");
            if (nav != null && pageId != null && name != null)
            {
                context.Edges.Add(new NavEdge
                {
                    From = pageId,
                    Trigger = name,
                    Action = "navigate",
                    To = nav.Trim(),
                    Mode = UxmlMapper.Attr(el, "data-nav-mode"),
                    Transition = UxmlMapper.Attr(el, "data-transition"),
                    Ms = ParseMs(UxmlMapper.Attr(el, "data-transition-ms"))
                });
            }

            if (UxmlMapper.HasAttr(el, "data-back") && pageId != null && name != null)
            {
                context.Edges.Add(new NavEdge { From = pageId, Trigger = name, Action = "back" });
            }

            string openModal = UxmlMapper.Attr(el, "data-open-modal");
            if (openModal != null && pageId != null && name != null)
            {
                context.Edges.Add(new NavEdge { From = pageId, Trigger = name, Action = "openModal", To = openModal.Trim() });
            }

            if (UxmlMapper.HasAttr(el, "data-close") && modalKey != null && name != null)
            {
                List<string> list;
                if (!context.CloseTriggers.TryGetValue(modalKey, out list))
                {
                    list = new List<string>();
                    context.CloseTriggers[modalKey] = list;
                }

                list.Add(name);
            }

            string tabs = UxmlMapper.Attr(el, "data-tabs");
            if (tabs != null)
            {
                var group = new NavTabGroup { Group = tabs.Trim(), Page = pageId };
                var items = new List<NavTab>();
                string defaultTab = null;
                foreach (DomNode node in Descendants(el))
                {
                    DomElement tab = node as DomElement;
                    if (tab == null) continue;
                    string panel = UxmlMapper.Attr(tab, "data-tab");
                    if (panel == null) continue;
                    items.Add(new NavTab { Tab = tab.Name, Panel = panel.Trim() });
                    if (defaultTab == null) defaultTab = tab.Name;
                    if (UxmlMapper.HasAttr(tab, "data-tab-default")) defaultTab = tab.Name;
                }

                group.Tabs = items;
                group.Default = defaultTab;
                context.Tabs.Add(group);
            }

            Walk(el, pageId, modalKey, context);
        }

        private static int ParseMs(string text)
        {
            int value;
            if (text != null && int.TryParse(text.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0) return value;
            return -1;
        }

        private static string FirstInteractive(DomElement root)
        {
            DialectSpec dialect = DialectSpec.Instance;
            foreach (DomNode node in Descendants(root))
            {
                DomElement el = node as DomElement;
                if (el == null || el.Name == null) continue;
                string tag = el.Tag.ToLowerInvariant();
                string uxml = null;
                string dataUxml = UxmlMapper.Attr(el, "data-uxml");
                if (!string.IsNullOrEmpty(dataUxml)) uxml = dataUxml;
                else if (tag == "input")
                {
                    string type = (UxmlMapper.Attr(el, "type") ?? "text").ToLowerInvariant();
                    dialect.InputTypes.TryGetValue(type, out uxml);
                }
                else dialect.TagToUxml.TryGetValue(tag, out uxml);
                if (uxml != null && dialect.FocusableByDefault.Contains(uxml)) return el.Name;
            }

            return null;
        }
    }
}
