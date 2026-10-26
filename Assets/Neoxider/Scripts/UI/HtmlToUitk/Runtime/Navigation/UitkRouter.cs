using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Navigation
{
    public sealed class UitkRouter : IDisposable
    {
        private sealed class PlanRun
        {
            public NavPlan Plan;
            public int Pending;
            public bool Finished;
            public int Generation;
        }

        private readonly RouterCore _core;
        private readonly IPageBackend _backend;
        private readonly List<IRouterHook> _hooks = new List<IRouterHook>();
        private readonly List<Action> _unwire = new List<Action>();
        private readonly List<IPageTransitionHandle> _inFlight = new List<IPageTransitionHandle>();
        private readonly List<string> _missingTriggers = new List<string>();
        private bool _disposed;
        private int _generation;

        public UitkRouter(NavSpec spec, IPageBackend backend, RouterOptions options = null)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _core = new RouterCore(spec, options);
            _backend.Bind(spec);
        }

        public event Action<string> OnWillShow;

        public event Action<string> OnShown;

        public event Action<string> OnWillHide;

        public event Action<string> OnHidden;

        public event Action<string> OnModalOpened;

        public event Action<string> OnModalClosed;

        public event Action<string, string> OnTabSelected;

        public event Action OnBackOnRoot;

        public event Action<RouterEvent> OnEvent;

        public NavSpec Spec => _core.Spec;

        public RouterOptions Options => _core.Options;

        public string Current => _core.Current;

        public IReadOnlyList<string> Stack => _core.Stack;

        public IReadOnlyList<string> Modals => _core.Modals;

        public IReadOnlyList<string> History => _core.History;

        public IReadOnlyList<string> VisitLog => _core.VisitLog;

        public IReadOnlyList<string> MissingTriggers => _missingTriggers;

        public bool IsTransitioning
        {
            get
            {
                for (int i = 0; i < _inFlight.Count; i++)
                {
                    if (!_inFlight[i].IsDone)
                        return true;
                }

                return false;
            }
        }

        public string SelectedTab(string group) => _core.SelectedTab(group);

        public void AddHook(IRouterHook hook)
        {
            if (hook != null && !_hooks.Contains(hook))
                _hooks.Add(hook);
        }

        public void RemoveHook(IRouterHook hook)
        {
            _hooks.Remove(hook);
        }

        public bool Start()
        {
            return !_disposed && Execute(_core.Start());
        }

        public bool Navigate(string pageId, NavMode mode = NavMode.Push, TransitionKind? transition = null, int ms = -1)
        {
            return !_disposed && Execute(_core.Navigate(pageId, mode, transition, ms));
        }

        public bool Back()
        {
            return !_disposed && Execute(_core.Back());
        }

        public bool OpenModal(string modalId)
        {
            return !_disposed && Execute(_core.OpenModal(modalId));
        }

        public bool CloseModal(string modalId = null)
        {
            return !_disposed && Execute(_core.CloseModal(modalId));
        }

        public bool SelectTab(string group, string tab)
        {
            return !_disposed && Execute(_core.SelectTab(group, tab));
        }

        public bool Activate(string trigger)
        {
            return !_disposed && Execute(_core.ActivateTrigger(trigger));
        }

        public void WireTriggers()
        {
            if (_disposed) return;
            UnwireTriggers();
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            NavSpec spec = _core.Spec;
            for (int i = 0; i < spec.Edges.Count; i++)
                names.Add(spec.Edges[i].Trigger);
            for (int i = 0; i < spec.Modals.Count; i++)
            {
                for (int c = 0; c < spec.Modals[i].CloseTriggers.Count; c++)
                    names.Add(spec.Modals[i].CloseTriggers[c]);
            }

            for (int g = 0; g < spec.TabGroups.Count; g++)
            {
                for (int t = 0; t < spec.TabGroups[g].Tabs.Count; t++)
                    names.Add(spec.TabGroups[g].Tabs[t].Tab);
            }

            List<string> ordered = new List<string>(names);
            ordered.Sort(StringComparer.Ordinal);
            for (int i = 0; i < ordered.Count; i++)
                WireTrigger(ordered[i]);
        }

        public void UnwireTriggers()
        {
            for (int i = 0; i < _unwire.Count; i++)
                _unwire[i]();
            _unwire.Clear();
            _missingTriggers.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _generation++;
            UnwireTriggers();
            CompleteInFlight();
            _hooks.Clear();
        }

        private void WireTrigger(string name)
        {
            VisualElement element = _backend.FindElement(name);
            if (element == null)
            {
                _missingTriggers.Add(name);
                return;
            }

            string trigger = name;
            Action handler = () => { if (!_disposed && element.enabledInHierarchy) Activate(trigger); };
            if (element is Button button)
            {
                button.clicked += handler;
                _unwire.Add(() => button.clicked -= handler);
                return;
            }

            EventCallback<ClickEvent> onClick = evt => handler();
            EventCallback<NavigationSubmitEvent> onSubmit = evt => handler();
            element.RegisterCallback(onClick);
            element.RegisterCallback(onSubmit);
            _unwire.Add(() =>
            {
                element.UnregisterCallback(onClick);
                element.UnregisterCallback(onSubmit);
            });
        }

        private bool Execute(NavPlan plan)
        {
            if (_disposed)
                return false;
            if (!plan.Succeeded)
            {
                if (plan.Outcome == NavOutcome.BackOnRoot)
                {
                    Raise(new RouterEvent(RouterEventKind.BackOnRoot, _core.Current, null, TransitionKind.None, 0, TransitionDirection.Backward, NavMode.Show, null, null, plan.Message));
                    OnBackOnRoot?.Invoke();
                }
                else if (plan.Outcome != NavOutcome.Ignored)
                {
                    Raise(new RouterEvent(RouterEventKind.Rejected, _core.Current, null, TransitionKind.None, 0, TransitionDirection.Forward, plan.Mode, null, null, plan.Message));
                }

                return false;
            }

            int generation = ++_generation;
            CompleteInFlight();
            if (_disposed || generation != _generation) return false;
            RememberFocus(plan);
            if (plan.ChangedPage)
            {
                TransitionKind kind = TransitionKind.None;
                int ms = 0;
                TransitionDirection direction = TransitionDirection.Forward;
                for (int i = 0; i < plan.Steps.Count; i++)
                {
                    if (plan.Steps[i].Kind == NavStepKind.ShowPage)
                    {
                        kind = plan.Steps[i].Transition;
                        ms = plan.Steps[i].DurationMs;
                        direction = plan.Steps[i].Direction;
                    }
                }

                Raise(new RouterEvent(RouterEventKind.Navigated, plan.PageAfter, plan.PageBefore, kind, ms, direction, plan.Mode, null, null, null));
            }

            for (int i = 0; i < plan.Steps.Count; i++)
            {
                if (_disposed || generation != _generation) return false;
                RaiseStep(plan.Steps[i], true);
                if (_disposed || generation != _generation) return false;
            }

            PlanRun run = new PlanRun { Plan = plan, Pending = 1, Generation = generation };
            Action done = () =>
            {
                run.Pending--;
                if (run.Pending == 0)
                    Finish(run);
            };

            bool planHandled = false;
            if (_backend is IPlanAwareBackend aware)
            {
                run.Pending++;
                bool handled = aware.TryExecutePlan(plan, done, out IPageTransitionHandle planHandle);
                if (_disposed || generation != _generation)
                {
                    planHandle?.Complete();
                    return false;
                }
                if (handled)
                {
                    planHandled = true;
                    if (planHandle != null && !planHandle.IsDone)
                        _inFlight.Add(planHandle);
                }
                else
                {
                    run.Pending--;
                }
            }

            for (int i = 0; i < plan.Steps.Count; i++)
            {
                if (_disposed || generation != _generation) return false;
                NavStep step = plan.Steps[i];
                if (planHandled && step.Kind != NavStepKind.SelectTab)
                    continue;
                if (step.Kind == NavStepKind.SelectTab)
                {
                    _backend.SelectTab(step);
                    if (_disposed || generation != _generation) return false;
                    Raise(new RouterEvent(RouterEventKind.TabSelected, step.Id, null, TransitionKind.None, 0, TransitionDirection.Forward, NavMode.Show, step.Group, step.Tab, null));
                    if (_disposed || generation != _generation) return false;
                    continue;
                }

                run.Pending++;
                IPageTransitionHandle handle = Dispatch(step, done);
                if (_disposed || generation != _generation)
                {
                    handle?.Complete();
                    return false;
                }
                if (handle != null && !handle.IsDone)
                    _inFlight.Add(handle);
            }

            done();
            return true;
        }

        private IPageTransitionHandle Dispatch(NavStep step, Action done)
        {
            switch (step.Kind)
            {
                case NavStepKind.ShowPage: return _backend.ShowPage(step, done);
                case NavStepKind.HidePage: return _backend.HidePage(step, done);
                case NavStepKind.OpenModal: return _backend.OpenModal(step, done);
                default: return _backend.CloseModal(step, done);
            }
        }

        private void Finish(PlanRun run)
        {
            if (run.Finished || _disposed || run.Generation != _generation)
                return;
            run.Finished = true;
            NavPlan plan = run.Plan;
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                RaiseStep(plan.Steps[i], false);
                if (_disposed || run.Generation != _generation) return;
            }
            ApplyFocus(plan);
        }

        private void CompleteInFlight()
        {
            if (_inFlight.Count == 0)
                return;
            List<IPageTransitionHandle> snapshot = new List<IPageTransitionHandle>(_inFlight);
            _inFlight.Clear();
            for (int i = 0; i < snapshot.Count; i++)
                snapshot[i].Complete();
        }

        private void RememberFocus(NavPlan plan)
        {
            string focused = _backend.FocusedElementName;
            if (string.IsNullOrEmpty(focused))
                return;
            bool pageRemembered = false;
            bool modalRemembered = false;
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                NavStep step = plan.Steps[i];
                if (step.Kind == NavStepKind.HidePage && !pageRemembered && !modalRemembered)
                {
                    _core.RememberFocus(step.Id, focused);
                    pageRemembered = true;
                }
                else if (step.Kind == NavStepKind.OpenModal)
                {
                    string previousModal = _core.Modals.Count > 1 ? _core.Modals[_core.Modals.Count - 2] : null;
                    _core.RememberFocus(previousModal != null ? RouterCore.ModalFocusKey(previousModal) : plan.PageBefore, focused);
                }
                else if (step.Kind == NavStepKind.CloseModal && !modalRemembered)
                {
                    _core.RememberFocus(RouterCore.ModalFocusKey(step.Id), focused);
                    modalRemembered = true;
                }
            }
        }

        private void ApplyFocus(NavPlan plan)
        {
            string pageToFocus = null;
            string modalToFocus = null;
            bool closedModal = false;
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                NavStep step = plan.Steps[i];
                if (step.Kind == NavStepKind.ShowPage)
                    pageToFocus = step.Id;
                else if (step.Kind == NavStepKind.OpenModal)
                    modalToFocus = step.Id;
                else if (step.Kind == NavStepKind.CloseModal)
                    closedModal = true;
            }

            if (modalToFocus == null && closedModal)
                modalToFocus = _core.TopModal;
            if (modalToFocus != null)
            {
                string remembered = _core.FocusTarget(RouterCore.ModalFocusKey(modalToFocus));
                if (string.IsNullOrEmpty(remembered) || !_backend.Focus(remembered))
                    _backend.FocusModal(modalToFocus);
                return;
            }

            if (pageToFocus == null && closedModal)
                pageToFocus = _core.Current;
            if (pageToFocus == null)
                return;
            string target = _core.FocusTarget(pageToFocus);
            if (!string.IsNullOrEmpty(target))
                _backend.Focus(target);
        }

        private void RaiseStep(NavStep step, bool will)
        {
            RouterEventKind kind;
            switch (step.Kind)
            {
                case NavStepKind.ShowPage:
                    kind = will ? RouterEventKind.WillShow : RouterEventKind.Shown;
                    break;
                case NavStepKind.HidePage:
                    kind = will ? RouterEventKind.WillHide : RouterEventKind.Hidden;
                    break;
                case NavStepKind.OpenModal:
                    kind = will ? RouterEventKind.ModalWillOpen : RouterEventKind.ModalOpened;
                    break;
                case NavStepKind.CloseModal:
                    kind = will ? RouterEventKind.ModalWillClose : RouterEventKind.ModalClosed;
                    break;
                default:
                    return;
            }

            Raise(new RouterEvent(kind, step.Id, null, step.Transition, step.DurationMs, step.Direction, step.Mode, null, null, null));
        }

        private void Raise(RouterEvent routerEvent)
        {
            if (_disposed) return;
            int generation = _generation;
            for (int i = 0; i < _hooks.Count; i++)
            {
                _hooks[i].OnRouterEvent(in routerEvent);
                if (_disposed || generation != _generation) return;
            }
            OnEvent?.Invoke(routerEvent);
            if (_disposed || generation != _generation) return;
            switch (routerEvent.Kind)
            {
                case RouterEventKind.WillShow: OnWillShow?.Invoke(routerEvent.Id); break;
                case RouterEventKind.Shown: OnShown?.Invoke(routerEvent.Id); break;
                case RouterEventKind.WillHide: OnWillHide?.Invoke(routerEvent.Id); break;
                case RouterEventKind.Hidden: OnHidden?.Invoke(routerEvent.Id); break;
                case RouterEventKind.ModalOpened: OnModalOpened?.Invoke(routerEvent.Id); break;
                case RouterEventKind.ModalClosed: OnModalClosed?.Invoke(routerEvent.Id); break;
                case RouterEventKind.TabSelected: OnTabSelected?.Invoke(routerEvent.Group, routerEvent.Tab); break;
            }
        }
    }
}
