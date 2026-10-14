using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Navigation
{
    public sealed class UiDocumentPageBackend : IPageBackend
    {
        private readonly VisualElement _root;
        private readonly PageClasses _classes;
        private readonly Dictionary<string, VisualElement> _pages = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly Dictionary<string, VisualElement> _modals = new Dictionary<string, VisualElement>(StringComparer.Ordinal);
        private readonly List<string> _missing = new List<string>();

        public UiDocumentPageBackend(VisualElement root, PageClasses classes = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _classes = classes ?? new PageClasses();
        }

        public IReadOnlyList<string> MissingElements => _missing;

        public string FocusedElementName => ElementFocus.FocusedName(_root);

        public void Bind(NavSpec spec)
        {
            _pages.Clear();
            _modals.Clear();
            _missing.Clear();
            for (int i = 0; i < spec.Pages.Count; i++)
            {
                VisualElement element = _root.Q<VisualElement>(spec.Pages[i].Element);
                if (element == null)
                {
                    _missing.Add(spec.Pages[i].Element);
                    continue;
                }

                _pages[spec.Pages[i].Id] = element;
                SetVisible(element, false);
            }

            for (int i = 0; i < spec.Modals.Count; i++)
            {
                VisualElement element = _root.Q<VisualElement>(spec.Modals[i].Element);
                if (element == null)
                {
                    _missing.Add(spec.Modals[i].Element);
                    continue;
                }

                _modals[spec.Modals[i].Id] = element;
                SetVisible(element, false);
            }

            for (int g = 0; g < spec.TabGroups.Count; g++)
            {
                TabGroupDef group = spec.TabGroups[g];
                for (int t = 0; t < group.Tabs.Count; t++)
                    UitkTabs.SetPanel(_root.Q<VisualElement>(group.Tabs[t].Panel), false, _classes);
            }
        }

        public VisualElement FindElement(string name)
        {
            return string.IsNullOrEmpty(name) ? null : _root.Q<VisualElement>(name);
        }

        public VisualElement PageElement(string pageId)
        {
            return _pages.TryGetValue(pageId, out VisualElement element) ? element : null;
        }

        public IPageTransitionHandle ShowPage(NavStep step, Action onDone)
        {
            return Show(PageElement(step.Id), step, onDone);
        }

        public IPageTransitionHandle HidePage(NavStep step, Action onDone)
        {
            return Hide(PageElement(step.Id), step, onDone);
        }

        public IPageTransitionHandle OpenModal(NavStep step, Action onDone)
        {
            return Show(_modals.TryGetValue(step.Id, out VisualElement element) ? element : null, step, onDone);
        }

        public IPageTransitionHandle CloseModal(NavStep step, Action onDone)
        {
            return Hide(_modals.TryGetValue(step.Id, out VisualElement element) ? element : null, step, onDone);
        }

        public void SelectTab(NavStep step)
        {
            UitkTabs.Select(FindElement, step, _classes);
        }

        public bool Focus(string elementName)
        {
            return ElementFocus.FocusByName(_root, elementName);
        }

        public bool FocusModal(string modalId)
        {
            return _modals.TryGetValue(modalId, out VisualElement element) && ElementFocus.FocusFirstFocusable(_root, element);
        }

        private IPageTransitionHandle Show(VisualElement element, NavStep step, Action onDone)
        {
            if (element == null)
            {
                onDone?.Invoke();
                return CompletedTransition.Instance;
            }

            SetVisible(element, true);
            element.pickingMode = PickingMode.Position;
            return ElementTransition.Start(element, step.Transition, true, step.Direction, step.DurationMs, onDone);
        }

        private IPageTransitionHandle Hide(VisualElement element, NavStep step, Action onDone)
        {
            if (element == null)
            {
                onDone?.Invoke();
                return CompletedTransition.Instance;
            }

            bool animated = step.Transition != TransitionKind.None && step.DurationMs > 0;
            StyleEnum<Position> position = element.style.position;
            StyleLength left = element.style.left;
            StyleLength top = element.style.top;
            StyleLength right = element.style.right;
            StyleLength bottom = element.style.bottom;
            PickingMode picking = element.pickingMode;
            if (animated)
            {
                element.pickingMode = PickingMode.Ignore;
                element.style.position = Position.Absolute;
                element.style.left = 0;
                element.style.top = 0;
                element.style.right = 0;
                element.style.bottom = 0;
            }

            return ElementTransition.Start(element, step.Transition, false, step.Direction, step.DurationMs, () =>
            {
                if (animated)
                {
                    element.style.position = position;
                    element.style.left = left;
                    element.style.top = top;
                    element.style.right = right;
                    element.style.bottom = bottom;
                    element.pickingMode = picking;
                }

                SetVisible(element, false);
                onDone?.Invoke();
            });
        }

        private void SetVisible(VisualElement element, bool visible)
        {
            if (visible)
            {
                element.RemoveFromClassList(_classes.Hidden);
                element.AddToClassList(_classes.Active);
                element.style.display = StyleKeyword.Null;
            }
            else
            {
                element.AddToClassList(_classes.Hidden);
                element.RemoveFromClassList(_classes.Active);
                element.style.display = DisplayStyle.None;
            }
        }

    }
}
