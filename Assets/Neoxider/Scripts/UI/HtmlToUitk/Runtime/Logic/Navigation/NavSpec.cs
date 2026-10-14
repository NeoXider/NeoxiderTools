using System;
using System.Collections.Generic;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Navigation
{
    public enum TransitionKind
    {
        None,
        Fade,
        SlideLeft,
        SlideRight,
        SlideUp,
        Scale
    }

    public enum NavAction
    {
        Navigate,
        Back,
        OpenModal,
        CloseModal
    }

    public enum NavMode
    {
        Show,
        Push
    }

    public sealed class PageDef
    {
        public PageDef(string id, string element, string focus, TransitionKind? transitionIn, TransitionKind? transitionOut, int ms)
        {
            Id = id;
            Element = element;
            Focus = focus;
            TransitionIn = transitionIn;
            TransitionOut = transitionOut;
            Ms = ms;
        }

        public string Id { get; }

        public string Element { get; }

        public string Focus { get; }

        public TransitionKind? TransitionIn { get; }

        public TransitionKind? TransitionOut { get; }

        public int Ms { get; }
    }

    public sealed class EdgeDef
    {
        public EdgeDef(string from, string trigger, NavAction action, string to, NavMode mode, TransitionKind? transition, int ms)
        {
            From = from;
            Trigger = trigger;
            Action = action;
            To = to;
            Mode = mode;
            Transition = transition;
            Ms = ms;
        }

        public string From { get; }

        public string Trigger { get; }

        public NavAction Action { get; }

        public string To { get; }

        public NavMode Mode { get; }

        public TransitionKind? Transition { get; }

        public int Ms { get; }
    }

    public sealed class ModalDef
    {
        public ModalDef(string id, string element, string ownerPage, IReadOnlyList<string> closeTriggers)
        {
            Id = id;
            Element = element;
            OwnerPage = ownerPage;
            CloseTriggers = closeTriggers;
        }

        public string Id { get; }

        public string Element { get; }

        public string OwnerPage { get; }

        public IReadOnlyList<string> CloseTriggers { get; }
    }

    public sealed class TabPair
    {
        public TabPair(string tab, string panel)
        {
            Tab = tab;
            Panel = panel;
        }

        public string Tab { get; }

        public string Panel { get; }
    }

    public sealed class TabGroupDef
    {
        public TabGroupDef(string group, string page, string defaultTab, IReadOnlyList<TabPair> tabs)
        {
            Group = group;
            Page = page;
            DefaultTab = defaultTab;
            Tabs = tabs;
        }

        public string Group { get; }

        public string Page { get; }

        public string DefaultTab { get; }

        public IReadOnlyList<TabPair> Tabs { get; }

        public string InitialTab => !string.IsNullOrEmpty(DefaultTab) ? DefaultTab : Tabs.Count > 0 ? Tabs[0].Tab : null;

        public TabPair Find(string tab)
        {
            for (int i = 0; i < Tabs.Count; i++)
            {
                if (string.Equals(Tabs[i].Tab, tab, StringComparison.Ordinal))
                    return Tabs[i];
            }

            return null;
        }
    }

    public sealed class NavSpec
    {
        private readonly Dictionary<string, PageDef> _pages = new Dictionary<string, PageDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, ModalDef> _modals = new Dictionary<string, ModalDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, TabGroupDef> _groups = new Dictionary<string, TabGroupDef>(StringComparer.Ordinal);

        public NavSpec(string document, string start, IReadOnlyList<PageDef> pages, IReadOnlyList<EdgeDef> edges, IReadOnlyList<ModalDef> modals, IReadOnlyList<TabGroupDef> tabGroups)
        {
            Document = document;
            Start = start;
            Pages = pages;
            Edges = edges;
            Modals = modals;
            TabGroups = tabGroups;
            for (int i = 0; i < pages.Count; i++)
                _pages[pages[i].Id] = pages[i];
            for (int i = 0; i < modals.Count; i++)
                _modals[modals[i].Id] = modals[i];
            for (int i = 0; i < tabGroups.Count; i++)
                _groups[tabGroups[i].Group] = tabGroups[i];
        }

        public string Document { get; }

        public string Start { get; }

        public IReadOnlyList<PageDef> Pages { get; }

        public IReadOnlyList<EdgeDef> Edges { get; }

        public IReadOnlyList<ModalDef> Modals { get; }

        public IReadOnlyList<TabGroupDef> TabGroups { get; }

        public PageDef Page(string id) => id != null && _pages.TryGetValue(id, out PageDef page) ? page : null;

        public ModalDef Modal(string id) => id != null && _modals.TryGetValue(id, out ModalDef modal) ? modal : null;

        public TabGroupDef Group(string group) => group != null && _groups.TryGetValue(group, out TabGroupDef def) ? def : null;

        public static NavSpec Parse(string json)
        {
            return FromJson(MiniJson.Parse(json));
        }

        public static NavSpec FromJson(JsonValue root)
        {
            List<PageDef> pages = new List<PageDef>();
            JsonValue pageList = root["pages"];
            for (int i = 0; i < pageList.Count; i++)
            {
                JsonValue p = pageList[i];
                JsonValue t = p["transition"];
                pages.Add(new PageDef(
                    p["id"].AsString(string.Empty),
                    p["element"].AsString(string.Empty),
                    p["focus"].AsString(),
                    ParseTransition(t["in"].AsString()),
                    ParseTransition(t["out"].AsString()),
                    t["ms"].Kind == JsonKind.Number ? t["ms"].AsInt() : -1));
            }

            List<EdgeDef> edges = new List<EdgeDef>();
            JsonValue edgeList = root["edges"];
            for (int i = 0; i < edgeList.Count; i++)
            {
                JsonValue e = edgeList[i];
                edges.Add(new EdgeDef(
                    e["from"].AsString(string.Empty),
                    e["trigger"].AsString(string.Empty),
                    ParseAction(e["action"].AsString("navigate")),
                    e["to"].AsString(),
                    string.Equals(e["mode"].AsString("push"), "show", StringComparison.Ordinal) ? NavMode.Show : NavMode.Push,
                    ParseTransition(e["transition"].AsString()),
                    e["ms"].Kind == JsonKind.Number ? e["ms"].AsInt() : -1));
            }

            List<ModalDef> modals = new List<ModalDef>();
            JsonValue modalList = root["modals"];
            for (int i = 0; i < modalList.Count; i++)
            {
                JsonValue m = modalList[i];
                modals.Add(new ModalDef(m["id"].AsString(string.Empty), m["element"].AsString(string.Empty), m["ownerPage"].AsString(), m["closeTriggers"].ToStringList()));
            }

            List<TabGroupDef> groups = new List<TabGroupDef>();
            JsonValue groupList = root["tabs"];
            for (int i = 0; i < groupList.Count; i++)
            {
                JsonValue g = groupList[i];
                List<TabPair> tabs = new List<TabPair>();
                JsonValue tabList = g["tabs"];
                for (int t = 0; t < tabList.Count; t++)
                    tabs.Add(new TabPair(tabList[t]["tab"].AsString(string.Empty), tabList[t]["panel"].AsString(string.Empty)));
                groups.Add(new TabGroupDef(g["group"].AsString(string.Empty), g["page"].AsString(string.Empty), g["default"].AsString(), tabs));
            }

            string start = root["start"].AsString();
            if (string.IsNullOrEmpty(start) && pages.Count > 0)
                start = pages[0].Id;
            return new NavSpec(root["document"].AsString(string.Empty), start, pages, edges, modals, groups);
        }

        public static TransitionKind? ParseTransition(string text)
        {
            switch (text)
            {
                case "fade": return TransitionKind.Fade;
                case "slide-left": return TransitionKind.SlideLeft;
                case "slide-right": return TransitionKind.SlideRight;
                case "slide-up": return TransitionKind.SlideUp;
                case "scale": return TransitionKind.Scale;
                case "none": return TransitionKind.None;
                default: return null;
            }
        }

        public static NavAction ParseAction(string text)
        {
            switch (text)
            {
                case "back": return NavAction.Back;
                case "openModal": return NavAction.OpenModal;
                case "closeModal": return NavAction.CloseModal;
                default: return NavAction.Navigate;
            }
        }

        public IReadOnlyList<string> Validate()
        {
            List<string> problems = new List<string>();
            if (Page(Start) == null)
                problems.Add("start page '" + Start + "' is not defined");
            for (int i = 0; i < Edges.Count; i++)
            {
                EdgeDef edge = Edges[i];
                if (Page(edge.From) == null)
                    problems.Add("edge '" + edge.Trigger + "' starts at unknown page '" + edge.From + "'");
                if (edge.Action == NavAction.Navigate && Page(edge.To) == null)
                    problems.Add("edge '" + edge.Trigger + "' navigates to unknown page '" + edge.To + "'");
                if (edge.Action == NavAction.OpenModal && Modal(edge.To) == null)
                    problems.Add("edge '" + edge.Trigger + "' opens unknown modal '" + edge.To + "'");
            }

            for (int i = 0; i < Modals.Count; i++)
            {
                if (!string.IsNullOrEmpty(Modals[i].OwnerPage) && Page(Modals[i].OwnerPage) == null)
                    problems.Add("modal '" + Modals[i].Id + "' belongs to unknown page '" + Modals[i].OwnerPage + "'");
            }

            for (int i = 0; i < TabGroups.Count; i++)
            {
                TabGroupDef group = TabGroups[i];
                if (Page(group.Page) == null)
                    problems.Add("tab group '" + group.Group + "' belongs to unknown page '" + group.Page + "'");
                if (!string.IsNullOrEmpty(group.DefaultTab) && group.Find(group.DefaultTab) == null)
                    problems.Add("tab group '" + group.Group + "' default '" + group.DefaultTab + "' is not one of its tabs");
            }

            return problems;
        }
    }
}
