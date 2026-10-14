using System;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Navigation
{
    public static class UitkTabs
    {
        public static void Select(Func<string, VisualElement> find, NavStep step, PageClasses classes)
        {
            if (find == null || step == null)
                return;
            PageClasses c = classes ?? new PageClasses();
            if (!string.IsNullOrEmpty(step.PreviousTab))
                SetTab(find(step.PreviousTab), false, c);
            if (!string.IsNullOrEmpty(step.PreviousPanel))
                SetPanel(find(step.PreviousPanel), false, c);
            if (!string.IsNullOrEmpty(step.Tab))
                SetTab(find(step.Tab), true, c);
            if (!string.IsNullOrEmpty(step.Panel))
                SetPanel(find(step.Panel), true, c);
        }

        public static void SetPanel(VisualElement panel, bool visible, PageClasses classes)
        {
            if (panel == null)
                return;
            if (visible)
            {
                panel.RemoveFromClassList(classes.Hidden);
                panel.style.display = StyleKeyword.Null;
            }
            else
            {
                panel.AddToClassList(classes.Hidden);
                panel.style.display = DisplayStyle.None;
            }
        }

        private static void SetTab(VisualElement tab, bool active, PageClasses classes)
        {
            if (tab == null)
                return;
            if (active)
                tab.AddToClassList(classes.Active);
            else
                tab.RemoveFromClassList(classes.Active);
            if (tab is Toggle toggle)
                toggle.SetValueWithoutNotify(active);
        }
    }
}
