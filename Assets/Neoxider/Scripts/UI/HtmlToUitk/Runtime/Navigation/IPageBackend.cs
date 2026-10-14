using System;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Navigation
{
    public interface IPageTransitionHandle
    {
        bool IsDone { get; }

        void Complete();
    }

    public sealed class CompletedTransition : IPageTransitionHandle
    {
        public static readonly CompletedTransition Instance = new CompletedTransition();

        private CompletedTransition()
        {
        }

        public bool IsDone => true;

        public void Complete()
        {
        }
    }

    public interface IPageBackend
    {
        void Bind(NavSpec spec);

        VisualElement FindElement(string name);

        IPageTransitionHandle ShowPage(NavStep step, Action onDone);

        IPageTransitionHandle HidePage(NavStep step, Action onDone);

        IPageTransitionHandle OpenModal(NavStep step, Action onDone);

        IPageTransitionHandle CloseModal(NavStep step, Action onDone);

        void SelectTab(NavStep step);

        bool Focus(string elementName);

        bool FocusModal(string modalId);

        string FocusedElementName { get; }
    }

    public interface IPlanAwareBackend
    {
        bool TryExecutePlan(NavPlan plan, Action onDone, out IPageTransitionHandle handle);
    }

    public enum RouterEventKind
    {
        WillShow,
        Shown,
        WillHide,
        Hidden,
        ModalWillOpen,
        ModalOpened,
        ModalWillClose,
        ModalClosed,
        TabSelected,
        Navigated,
        BackOnRoot,
        Rejected
    }

    public readonly struct RouterEvent
    {
        public RouterEvent(RouterEventKind kind, string id, string otherId, TransitionKind transition, int durationMs, TransitionDirection direction, NavMode mode, string group, string tab, string message)
        {
            Kind = kind;
            Id = id;
            OtherId = otherId;
            Transition = transition;
            DurationMs = durationMs;
            Direction = direction;
            Mode = mode;
            Group = group;
            Tab = tab;
            Message = message;
        }

        public RouterEventKind Kind { get; }

        public string Id { get; }

        public string OtherId { get; }

        public TransitionKind Transition { get; }

        public int DurationMs { get; }

        public TransitionDirection Direction { get; }

        public NavMode Mode { get; }

        public string Group { get; }

        public string Tab { get; }

        public string Message { get; }
    }

    public interface IRouterHook
    {
        void OnRouterEvent(in RouterEvent routerEvent);
    }

    public sealed class PageClasses
    {
        public string Hidden = "is-hidden";
        public string Active = "is-active";
    }
}
