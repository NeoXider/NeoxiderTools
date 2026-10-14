using System;
using System.Collections.Generic;

namespace Neo.HtmlToUitk.Navigation
{
    public enum UiKitAction
    {
        Show,
        Push,
        Pop,
        OpenPopup,
        ClosePopup
    }

    public sealed class UiKitPagePlan
    {
        public string PageId;
        public bool IsStart;
        public string ShowPreset;
        public string HidePreset;
    }

    public sealed class UiKitNavigationPlan
    {
        public string ButtonPath;
        public UiKitAction Action;
        public string TargetId;
    }

    public sealed class UiKitConfigPlan
    {
        public List<UiKitPagePlan> Pages = new List<UiKitPagePlan>();
        public List<UiKitNavigationPlan> Navigation = new List<UiKitNavigationPlan>();
        public List<string> Warnings = new List<string>();
    }

    public static class UiKitMapping
    {
        public static string PresetName(TransitionKind? kind)
        {
            switch (kind)
            {
                case TransitionKind.Fade: return "fade";
                case TransitionKind.SlideLeft: return "slide-left";
                case TransitionKind.SlideRight: return "slide-right";
                case TransitionKind.SlideUp: return "slide-up";
                case TransitionKind.Scale: return "scale";
                case TransitionKind.None: return "none";
                default: return null;
            }
        }

        public static string PopupPath(NavSpec spec, ModalDef modal, string fallbackPage, Func<ModalDef, string> popupName)
        {
            string owner = !string.IsNullOrEmpty(modal.OwnerPage) ? modal.OwnerPage : fallbackPage;
            string popup = popupName != null ? popupName(modal) : modal.Element;
            return owner + "/" + popup;
        }

        public static UiKitConfigPlan Map(NavSpec spec, Func<ModalDef, string> popupName = null)
        {
            UiKitConfigPlan plan = new UiKitConfigPlan();
            for (int i = 0; i < spec.Pages.Count; i++)
            {
                PageDef page = spec.Pages[i];
                plan.Pages.Add(new UiKitPagePlan
                {
                    PageId = page.Id,
                    IsStart = string.Equals(page.Id, spec.Start, StringComparison.Ordinal),
                    ShowPreset = PresetName(page.TransitionIn),
                    HidePreset = PresetName(page.TransitionOut)
                });
                if (page.Ms >= 0)
                    plan.Warnings.Add("page '" + page.Id + "': transition ms is ignored, durations come from uikit.uss");
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < spec.Edges.Count; i++)
            {
                EdgeDef edge = spec.Edges[i];
                string path = edge.From + "/" + edge.Trigger;
                UiKitNavigationPlan entry = MapEdge(spec, edge, path, popupName, plan);
                if (entry == null)
                    continue;
                if (!seen.Add(path))
                {
                    plan.Warnings.Add("button '" + path + "' has more than one edge; the first one is kept");
                    continue;
                }

                plan.Navigation.Add(entry);
                if (edge.Transition.HasValue || edge.Ms >= 0)
                    plan.Warnings.Add("edge '" + path + "': per-edge transition and ms are ignored, UiKit transitions are page presets");
            }

            for (int i = 0; i < spec.Modals.Count; i++)
            {
                ModalDef modal = spec.Modals[i];
                string popupPath = PopupPath(spec, modal, null, popupName);
                for (int c = 0; c < modal.CloseTriggers.Count; c++)
                {
                    string path = popupPath + "/" + modal.CloseTriggers[c];
                    if (!seen.Add(path))
                        continue;
                    plan.Navigation.Add(new UiKitNavigationPlan { ButtonPath = path, Action = UiKitAction.ClosePopup, TargetId = string.Empty });
                }
            }

            if (spec.TabGroups.Count > 0)
                plan.Warnings.Add("tabs have no UiKitConfig equivalent; drive them through UitkRouter with the NeoxiderUiKit backend");
            return plan;
        }

        private static UiKitNavigationPlan MapEdge(NavSpec spec, EdgeDef edge, string path, Func<ModalDef, string> popupName, UiKitConfigPlan plan)
        {
            switch (edge.Action)
            {
                case NavAction.Back:
                    return new UiKitNavigationPlan { ButtonPath = path, Action = UiKitAction.Pop, TargetId = string.Empty };
                case NavAction.OpenModal:
                    {
                        ModalDef modal = spec.Modal(edge.To);
                        if (modal == null)
                        {
                            plan.Warnings.Add("edge '" + path + "' opens unknown modal '" + edge.To + "'");
                            return null;
                        }

                        return new UiKitNavigationPlan { ButtonPath = path, Action = UiKitAction.OpenPopup, TargetId = PopupPath(spec, modal, edge.From, popupName) };
                    }
                case NavAction.CloseModal:
                    {
                        ModalDef modal = string.IsNullOrEmpty(edge.To) ? null : spec.Modal(edge.To);
                        string target = modal != null ? PopupPath(spec, modal, edge.From, popupName) : string.Empty;
                        return new UiKitNavigationPlan { ButtonPath = path, Action = UiKitAction.ClosePopup, TargetId = target };
                    }
                default:
                    if (spec.Page(edge.To) == null)
                    {
                        plan.Warnings.Add("edge '" + path + "' navigates to unknown page '" + edge.To + "'");
                        return null;
                    }

                    return new UiKitNavigationPlan { ButtonPath = path, Action = edge.Mode == NavMode.Push ? UiKitAction.Push : UiKitAction.Show, TargetId = edge.To };
            }
        }
    }
}
