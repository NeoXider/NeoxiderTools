using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Navigation
{
    public static class ElementFocus
    {
        public const long FocusDelayMs = 32;

        public static string FocusedName(VisualElement root)
        {
            if (root == null || root.panel == null)
                return null;
            VisualElement focused = root.panel.focusController?.focusedElement as VisualElement;
            return focused != null && !string.IsNullOrEmpty(focused.name) && root.Contains(focused) ? focused.name : null;
        }

        public static bool FocusByName(VisualElement root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return false;
            VisualElement target = root.Q<VisualElement>(name);
            if (target == null)
                return false;
            Schedule(root, () => FocusElementOrFirstFocusable(target));
            return true;
        }

        public static bool FocusFirstFocusable(VisualElement root, VisualElement container)
        {
            if (root == null || container == null)
                return false;
            Schedule(root, () => FocusFirstWithin(container));
            return true;
        }

        private static void Schedule(VisualElement root, Action action)
        {
            if (root.panel == null)
            {
                action();
                return;
            }

            root.schedule.Execute(action).ExecuteLater(FocusDelayMs);
        }

        private static void FocusElementOrFirstFocusable(VisualElement element)
        {
            if (element.focusable && element.enabledInHierarchy)
            {
                element.Focus();
                return;
            }

            FocusFirstWithin(element);
        }

        private static void FocusFirstWithin(VisualElement container)
        {
            Stack<VisualElement> stack = new Stack<VisualElement>();
            for (int i = container.childCount - 1; i >= 0; i--)
                stack.Push(container[i]);
            while (stack.Count > 0)
            {
                VisualElement element = stack.Pop();
                if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.visibility == Visibility.Hidden)
                    continue;
                if (element.focusable && element.enabledInHierarchy)
                {
                    element.Focus();
                    return;
                }

                for (int i = element.childCount - 1; i >= 0; i--)
                    stack.Push(element[i]);
            }
        }
    }
}
