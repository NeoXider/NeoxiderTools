using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Navigation
{
    public enum TransitionDirection
    {
        Forward,
        Backward
    }

    public enum NavOutcome
    {
        Done,
        Ignored,
        UnknownTarget,
        NotAllowed,
        BackOnRoot,
        NoSuchTrigger
    }

    public enum NavStepKind
    {
        HidePage,
        ShowPage,
        OpenModal,
        CloseModal,
        SelectTab
    }

    public sealed class NavStep
    {
        public NavStepKind Kind;
        public string Id;
        public NavMode Mode;
        public TransitionKind Transition;
        public TransitionDirection Direction;
        public int DurationMs;
        public string Group;
        public string Tab;
        public string Panel;
        public string PreviousTab;
        public string PreviousPanel;
    }

    public sealed class NavPlan
    {
        private static readonly IReadOnlyList<NavStep> NoSteps = new NavStep[0];

        public NavPlan(NavOutcome outcome, string message, IReadOnlyList<NavStep> steps, string pageBefore, string pageAfter, NavMode mode)
        {
            Outcome = outcome;
            Message = message;
            Steps = steps ?? NoSteps;
            PageBefore = pageBefore;
            PageAfter = pageAfter;
            Mode = mode;
        }

        public NavOutcome Outcome { get; }

        public string Message { get; }

        public IReadOnlyList<NavStep> Steps { get; }

        public string PageBefore { get; }

        public string PageAfter { get; }

        public NavMode Mode { get; }

        public bool Succeeded => Outcome == NavOutcome.Done;

        public bool ChangedPage => Succeeded && !string.Equals(PageBefore, PageAfter, StringComparison.Ordinal);
    }

    public sealed class RouterOptions
    {
        public bool ShowClearsStack = true;
        public bool ShowToExistingPopsToIt;
        public bool PushKeepsPreviousVisible;
        public bool InstantStart = true;
        public bool RestoreFocus = true;
        public TransitionKind DefaultTransition = TransitionKind.Fade;
        public int DefaultMs = 220;
        public TransitionKind ModalTransition = TransitionKind.Fade;
        public int ModalMs = 150;
        public int VisitLogLimit = 64;
        public Func<bool> ReducedMotion;
    }

    public sealed class RouterCore
    {
        private readonly NavSpec _spec;
        private readonly RouterOptions _options;
        private readonly List<string> _stack = new List<string>();
        private readonly List<string> _modals = new List<string>();
        private readonly List<string> _visits = new List<string>();
        private readonly Dictionary<string, string> _tabs = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _focus = new Dictionary<string, string>(StringComparer.Ordinal);

        public RouterCore(NavSpec spec, RouterOptions options = null)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
            _options = options ?? new RouterOptions();
            for (int i = 0; i < spec.TabGroups.Count; i++)
            {
                string initial = spec.TabGroups[i].InitialTab;
                if (initial != null)
                    _tabs[spec.TabGroups[i].Group] = initial;
            }
        }

        public NavSpec Spec => _spec;

        public RouterOptions Options => _options;

        public bool IsStarted => _stack.Count > 0;

        public string Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public int Depth => _stack.Count;

        public IReadOnlyList<string> Stack => _stack.ToArray();

        public IReadOnlyList<string> History
        {
            get
            {
                List<string> back = new List<string>();
                for (int i = 0; i < _stack.Count - 1; i++)
                    back.Add(_stack[i]);
                return back;
            }
        }

        public IReadOnlyList<string> Modals => _modals;

        public IReadOnlyList<string> VisitLog => _visits;

        public string TopModal => _modals.Count > 0 ? _modals[_modals.Count - 1] : null;

        public string SelectedTab(string group)
        {
            return group != null && _tabs.TryGetValue(group, out string tab) ? tab : null;
        }

        public bool IsModalOpen(string id)
        {
            return _modals.Contains(id);
        }

        public void RememberFocus(string key, string elementName)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(elementName))
                _focus[key] = elementName;
        }

        public string FocusTarget(string pageId)
        {
            if (_options.RestoreFocus && pageId != null && _focus.TryGetValue(pageId, out string remembered))
                return remembered;
            return _spec.Page(pageId)?.Focus;
        }

        public static string ModalFocusKey(string modalId)
        {
            return "modal:" + modalId;
        }

        public NavPlan Start()
        {
            if (_stack.Count > 0)
                return Fail(NavOutcome.Ignored, "already started");
            PageDef page = _spec.Page(_spec.Start);
            if (page == null)
                return Fail(NavOutcome.UnknownTarget, "start page '" + _spec.Start + "' is not defined");
            List<NavStep> steps = new List<NavStep>();
            for (int i = 0; i < _spec.TabGroups.Count; i++)
            {
                TabGroupDef group = _spec.TabGroups[i];
                string tab = SelectedTab(group.Group);
                if (tab != null)
                    steps.Add(new NavStep { Kind = NavStepKind.SelectTab, Id = group.Page, Group = group.Group, Tab = tab, Panel = group.Find(tab)?.Panel });
            }

            TransitionKind kind = _options.InstantStart ? TransitionKind.None : EnterKind(page, null);
            int ms = _options.InstantStart ? 0 : Duration(-1, page.Ms);
            _stack.Add(page.Id);
            AddVisit(page.Id);
            steps.Add(ShowStep(page.Id, kind, TransitionDirection.Forward, ms));
            return new NavPlan(NavOutcome.Done, null, steps, null, page.Id, NavMode.Show);
        }

        public NavPlan Navigate(string pageId, NavMode mode, TransitionKind? transition = null, int ms = -1)
        {
            PageDef page = _spec.Page(pageId);
            if (page == null)
                return Fail(NavOutcome.UnknownTarget, "unknown page '" + pageId + "'");
            TransitionKind enter = EnterKind(page, transition);
            int duration = Duration(ms, page.Ms);
            if (_stack.Count == 0)
            {
                _stack.Add(page.Id);
                AddVisit(page.Id);
                List<NavStep> first = new List<NavStep> { ShowStep(page.Id, enter, TransitionDirection.Forward, duration) };
                StampMode(first, mode);
                return new NavPlan(NavOutcome.Done, null, first, null, page.Id, mode);
            }

            string before = Current;
            if (string.Equals(before, pageId, StringComparison.Ordinal))
                return Fail(NavOutcome.Ignored, "already on '" + pageId + "'");
            bool keep = _options.PushKeepsPreviousVisible;
            if (mode == NavMode.Push && keep && LastIndexOf(pageId) >= 0)
                return Fail(NavOutcome.Ignored, "'" + pageId + "' is already on the stack");

            List<NavStep> steps = new List<NavStep>();
            CloseAllModals(steps);

            if (mode == NavMode.Push)
            {
                if (!keep)
                    steps.Add(HideStep(before, enter, TransitionDirection.Forward, duration));
                _stack.Add(page.Id);
                steps.Add(ShowStep(page.Id, enter, TransitionDirection.Forward, duration));
            }
            else
            {
                int existing = _options.ShowToExistingPopsToIt ? LastIndexOf(pageId) : -1;
                if (existing >= 0)
                {
                    for (int i = _stack.Count - 1; i > existing; i--)
                    {
                        bool top = i == _stack.Count - 1;
                        if (keep || top)
                            steps.Add(HideStep(_stack[i], top ? enter : TransitionKind.None, TransitionDirection.Forward, top ? duration : 0));
                    }

                    _stack.RemoveRange(existing + 1, _stack.Count - existing - 1);
                    if (!keep)
                        steps.Add(ShowStep(page.Id, enter, TransitionDirection.Forward, duration));
                }
                else if (_options.ShowClearsStack)
                {
                    for (int i = _stack.Count - 1; i >= 0; i--)
                    {
                        bool top = i == _stack.Count - 1;
                        if (keep || top)
                            steps.Add(HideStep(_stack[i], top ? enter : TransitionKind.None, TransitionDirection.Forward, top ? duration : 0));
                    }

                    _stack.Clear();
                    _stack.Add(page.Id);
                    steps.Add(ShowStep(page.Id, enter, TransitionDirection.Forward, duration));
                }
                else
                {
                    steps.Add(HideStep(before, enter, TransitionDirection.Forward, duration));
                    _stack[_stack.Count - 1] = page.Id;
                    steps.Add(ShowStep(page.Id, enter, TransitionDirection.Forward, duration));
                }
            }

            AddVisit(page.Id);
            StampMode(steps, mode);
            return new NavPlan(NavOutcome.Done, null, steps, before, page.Id, mode);
        }

        public NavPlan Back()
        {
            if (_modals.Count > 0)
            {
                List<NavStep> closing = new List<NavStep>();
                string current = Current;
                CloseModalAbove(_modals.Count - 1, closing);
                return new NavPlan(NavOutcome.Done, null, closing, current, current, NavMode.Show);
            }

            if (_stack.Count <= 1)
                return Fail(NavOutcome.BackOnRoot, "nothing to go back to");
            string popped = _stack[_stack.Count - 1];
            string previous = _stack[_stack.Count - 2];
            PageDef leaving = _spec.Page(popped);
            TransitionKind kind = leaving?.TransitionOut ?? leaving?.TransitionIn ?? _options.DefaultTransition;
            int duration = Duration(-1, leaving != null ? leaving.Ms : -1);
            List<NavStep> steps = new List<NavStep> { HideStep(popped, kind, TransitionDirection.Backward, duration) };
            if (!_options.PushKeepsPreviousVisible)
                steps.Add(ShowStep(previous, kind, TransitionDirection.Backward, duration));
            _stack.RemoveAt(_stack.Count - 1);
            AddVisit(previous);
            return new NavPlan(NavOutcome.Done, null, steps, popped, previous, NavMode.Show);
        }

        public NavPlan OpenModal(string modalId)
        {
            ModalDef modal = _spec.Modal(modalId);
            if (modal == null)
                return Fail(NavOutcome.UnknownTarget, "unknown modal '" + modalId + "'");
            if (_stack.Count == 0)
                return Fail(NavOutcome.NotAllowed, "router is not started");
            if (!string.IsNullOrEmpty(modal.OwnerPage) && !string.Equals(modal.OwnerPage, Current, StringComparison.Ordinal))
                return Fail(NavOutcome.NotAllowed, "modal '" + modalId + "' belongs to page '" + modal.OwnerPage + "'");
            if (_modals.Contains(modalId))
                return Fail(NavOutcome.Ignored, "modal '" + modalId + "' is already open");
            _modals.Add(modalId);
            List<NavStep> steps = new List<NavStep>
            {
                new NavStep { Kind = NavStepKind.OpenModal, Id = modalId, Transition = _options.ModalTransition, Direction = TransitionDirection.Forward, DurationMs = ModalDuration() }
            };
            return new NavPlan(NavOutcome.Done, null, steps, Current, Current, NavMode.Show);
        }

        public NavPlan CloseModal(string modalId = null)
        {
            if (_modals.Count == 0)
                return Fail(NavOutcome.Ignored, "no modal is open");
            int index = modalId == null ? _modals.Count - 1 : _modals.IndexOf(modalId);
            if (index < 0)
                return Fail(NavOutcome.Ignored, "modal '" + modalId + "' is not open");
            List<NavStep> steps = new List<NavStep>();
            CloseModalAbove(index, steps);
            return new NavPlan(NavOutcome.Done, null, steps, Current, Current, NavMode.Show);
        }

        public NavPlan SelectTab(string group, string tab)
        {
            TabGroupDef def = _spec.Group(group);
            if (def == null)
                return Fail(NavOutcome.UnknownTarget, "unknown tab group '" + group + "'");
            TabPair pair = def.Find(tab);
            if (pair == null)
                return Fail(NavOutcome.UnknownTarget, "unknown tab '" + tab + "' in group '" + group + "'");
            string previous = SelectedTab(group);
            if (string.Equals(previous, tab, StringComparison.Ordinal))
                return Fail(NavOutcome.Ignored, "tab '" + tab + "' is already selected");
            _tabs[group] = tab;
            List<NavStep> steps = new List<NavStep>
            {
                new NavStep { Kind = NavStepKind.SelectTab, Id = def.Page, Group = group, Tab = tab, Panel = pair.Panel, PreviousTab = previous, PreviousPanel = previous != null ? def.Find(previous)?.Panel : null }
            };
            return new NavPlan(NavOutcome.Done, null, steps, Current, Current, NavMode.Show);
        }

        public NavPlan ActivateTrigger(string trigger)
        {
            if (string.IsNullOrEmpty(trigger))
                return Fail(NavOutcome.NoSuchTrigger, "empty trigger");
            for (int i = _modals.Count - 1; i >= 0; i--)
            {
                ModalDef modal = _spec.Modal(_modals[i]);
                if (modal != null && Contains(modal.CloseTriggers, trigger))
                    return CloseModal(modal.Id);
            }

            for (int g = 0; g < _spec.TabGroups.Count; g++)
            {
                TabGroupDef group = _spec.TabGroups[g];
                if (LastIndexOf(group.Page) < 0)
                    continue;
                for (int t = 0; t < group.Tabs.Count; t++)
                {
                    if (string.Equals(group.Tabs[t].Tab, trigger, StringComparison.Ordinal))
                        return SelectTab(group.Group, trigger);
                }
            }

            EdgeDef edge = FindEdge(trigger);
            if (edge == null)
                return Fail(NavOutcome.NoSuchTrigger, "no edge for trigger '" + trigger + "'");
            switch (edge.Action)
            {
                case NavAction.Back:
                    return Back();
                case NavAction.OpenModal:
                    return OpenModal(edge.To);
                case NavAction.CloseModal:
                    return CloseModal(string.IsNullOrEmpty(edge.To) ? null : edge.To);
                default:
                    return Navigate(edge.To, edge.Mode, edge.Transition, edge.Ms);
            }
        }

        private TransitionKind EnterKind(PageDef page, TransitionKind? explicitKind)
        {
            return explicitKind ?? page.TransitionIn ?? _options.DefaultTransition;
        }

        private int Duration(int explicitMs, int pageMs)
        {
            if (_options.ReducedMotion != null && _options.ReducedMotion())
                return 0;
            if (explicitMs >= 0)
                return explicitMs;
            return pageMs >= 0 ? pageMs : _options.DefaultMs;
        }

        private int ModalDuration()
        {
            return _options.ReducedMotion != null && _options.ReducedMotion() ? 0 : _options.ModalMs;
        }

        private EdgeDef FindEdge(string trigger)
        {
            EdgeDef only = null;
            int count = 0;
            for (int i = 0; i < _spec.Edges.Count; i++)
            {
                EdgeDef edge = _spec.Edges[i];
                if (!string.Equals(edge.Trigger, trigger, StringComparison.Ordinal))
                    continue;
                if (string.Equals(edge.From, Current, StringComparison.Ordinal))
                    return edge;
                only = edge;
                count++;
            }

            return count == 1 ? only : null;
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void CloseAllModals(List<NavStep> steps)
        {
            if (_modals.Count > 0)
                CloseModalAbove(0, steps);
        }

        private void CloseModalAbove(int index, List<NavStep> steps)
        {
            for (int i = _modals.Count - 1; i >= index; i--)
            {
                steps.Add(new NavStep { Kind = NavStepKind.CloseModal, Id = _modals[i], Transition = _options.ModalTransition, Direction = TransitionDirection.Backward, DurationMs = ModalDuration() });
                _modals.RemoveAt(i);
            }
        }

        private int LastIndexOf(string pageId)
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_stack[i], pageId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private void AddVisit(string pageId)
        {
            _visits.Add(pageId);
            int limit = Math.Max(1, _options.VisitLogLimit);
            if (_visits.Count > limit)
                _visits.RemoveRange(0, _visits.Count - limit);
        }

        private static void StampMode(List<NavStep> steps, NavMode mode)
        {
            for (int i = 0; i < steps.Count; i++)
                steps[i].Mode = mode;
        }

        private static NavStep HideStep(string id, TransitionKind kind, TransitionDirection direction, int ms)
        {
            return new NavStep { Kind = NavStepKind.HidePage, Id = id, Transition = kind, Direction = direction, DurationMs = kind == TransitionKind.None ? 0 : ms };
        }

        private static NavStep ShowStep(string id, TransitionKind kind, TransitionDirection direction, int ms)
        {
            return new NavStep { Kind = NavStepKind.ShowPage, Id = id, Transition = kind, Direction = direction, DurationMs = kind == TransitionKind.None ? 0 : ms };
        }

        private NavPlan Fail(NavOutcome outcome, string message)
        {
            return new NavPlan(outcome, message, null, Current, Current, NavMode.Show);
        }
    }
}
