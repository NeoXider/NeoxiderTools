using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Convert.Writers
{
    public static class SpecWriters
    {
        private static string Text(JsonValue value)
        {
            return JsonWriter.Write(value, JsonWriteOptions.Pretty);
        }

        public static string KeysStub(DocumentExtras extras, ConversionConfig config)
        {
            JsonObject root = new JsonObject();
            root.Set("version", 1);
            string mockLocale = config.Localization.MockLocale;
            foreach (string locale in config.Localization.Locales)
            {
                JsonObject table = new JsonObject();
                foreach (string key in extras.NewKeys)
                {
                    string mock;
                    extras.KeyMocks.TryGetValue(key, out mock);
                    mock = mock ?? "";
                    table.Set(key, locale == mockLocale ? mock : config.Localization.Placeholder + mock);
                }

                root.Set(locale, table);
            }

            return Text(root);
        }

        public static string Anim(AnimModel model)
        {
            JsonObject root = new JsonObject();
            root.Set("schema", 1);
            JsonArray animations = new JsonArray();
            foreach (AnimDef a in model.Animations)
            {
                JsonObject obj = new JsonObject();
                obj.Set("id", a.Id);
                obj.Set("keyframes", a.Keyframes ?? a.Id);
                obj.Set("duration", JsonValue.From(a.Duration));
                obj.Set("delay", JsonValue.From(a.Delay));
                if (a.Iterations < 0) obj.Set("iterations", "infinite");
                else obj.Set("iterations", a.Iterations);
                obj.Set("direction", a.Direction ?? "normal");
                obj.Set("fill", a.Fill ?? "none");
                obj.Set("easing", a.Easing ?? "ease");
                JsonArray steps = new JsonArray();
                foreach (AnimStep step in a.Steps)
                {
                    JsonObject s = new JsonObject();
                    s.Set("offset", JsonValue.From(step.Offset));
                    if (!string.IsNullOrEmpty(step.Easing)) s.Set("easing", step.Easing);
                    JsonObject set = new JsonObject();
                    List<string> names = new List<string>(step.Set.Keys);
                    names.Sort(StringComparer.Ordinal);
                    foreach (string name in names) set.Set(name, step.Set[name]);
                    s.Set("set", set);
                    steps.Add(s);
                }

                obj.Set("steps", steps);
                animations.Add(obj);
            }

            root.Set("animations", animations);
            if (model.Bindings.Count > 0)
            {
                JsonArray bindings = new JsonArray();
                foreach (AnimBinding b in model.Bindings)
                {
                    JsonObject obj = new JsonObject();
                    obj.Set("selector", b.Selector);
                    obj.Set("animation", b.Animation);
                    obj.Set("trigger", b.Trigger ?? "onShow");
                    if (b.Stagger > 0) obj.Set("stagger", JsonValue.From(b.Stagger));
                    if (!string.IsNullOrEmpty(b.ReducedMotion)) obj.Set("reducedMotion", b.ReducedMotion);
                    bindings.Add(obj);
                }

                root.Set("bindings", bindings);
            }

            return Text(root);
        }

        public static string Nav(NavModel model)
        {
            JsonObject root = new JsonObject();
            root.Set("schema", 1);
            root.Set("document", model.Document ?? "");
            root.Set("start", model.Start ?? "");
            JsonArray pages = new JsonArray();
            foreach (NavPage p in model.Pages)
            {
                JsonObject obj = new JsonObject();
                obj.Set("id", p.Id);
                obj.Set("element", p.Element ?? "");
                if (!string.IsNullOrEmpty(p.Focus)) obj.Set("focus", p.Focus);
                if (p.TransitionIn != null || p.TransitionOut != null || p.TransitionMs >= 0)
                {
                    JsonObject t = new JsonObject();
                    if (p.TransitionIn != null) t.Set("in", p.TransitionIn);
                    if (p.TransitionOut != null) t.Set("out", p.TransitionOut);
                    if (p.TransitionMs >= 0) t.Set("ms", p.TransitionMs);
                    obj.Set("transition", t);
                }

                pages.Add(obj);
            }

            root.Set("pages", pages);
            if (model.Edges.Count > 0)
            {
                JsonArray edges = new JsonArray();
                foreach (NavEdge e in model.Edges)
                {
                    JsonObject obj = new JsonObject();
                    obj.Set("from", e.From);
                    obj.Set("trigger", e.Trigger);
                    obj.Set("action", e.Action);
                    if (!string.IsNullOrEmpty(e.To)) obj.Set("to", e.To);
                    if (!string.IsNullOrEmpty(e.Mode)) obj.Set("mode", e.Mode);
                    if (!string.IsNullOrEmpty(e.Transition)) obj.Set("transition", e.Transition);
                    if (e.Ms >= 0) obj.Set("ms", e.Ms);
                    edges.Add(obj);
                }

                root.Set("edges", edges);
            }

            if (model.Modals.Count > 0)
            {
                JsonArray modals = new JsonArray();
                foreach (NavModal m in model.Modals)
                {
                    JsonObject obj = new JsonObject();
                    obj.Set("id", m.Id);
                    obj.Set("element", m.Element ?? "");
                    obj.Set("ownerPage", m.OwnerPage ?? "");
                    JsonArray close = new JsonArray();
                    foreach (string trigger in m.CloseTriggers) close.Add(trigger);
                    if (close.Count > 0) obj.Set("closeTriggers", close);
                    modals.Add(obj);
                }

                root.Set("modals", modals);
            }

            if (model.Tabs.Count > 0)
            {
                JsonArray tabs = new JsonArray();
                foreach (NavTabGroup g in model.Tabs)
                {
                    JsonObject obj = new JsonObject();
                    obj.Set("group", g.Group);
                    obj.Set("page", g.Page ?? "");
                    if (!string.IsNullOrEmpty(g.Default)) obj.Set("default", g.Default);
                    JsonArray items = new JsonArray();
                    foreach (NavTab tab in g.Tabs)
                    {
                        JsonObject t = new JsonObject();
                        t.Set("tab", tab.Tab ?? "");
                        t.Set("panel", tab.Panel);
                        items.Add(t);
                    }

                    obj.Set("tabs", items);
                    tabs.Add(obj);
                }

                root.Set("tabs", tabs);
            }

            return Text(root);
        }

        public static string Media(MediaQuerySpec spec)
        {
            JsonObject root = new JsonObject();
            root.Set("schema", 1);
            root.Set("root", spec.Root ?? "");
            JsonArray classes = new JsonArray();
            foreach (MediaClass c in spec.Classes)
            {
                JsonObject obj = new JsonObject();
                obj.Set("class", c.Class);
                if (!string.IsNullOrEmpty(c.Selector)) obj.Set("selector", c.Selector);
                JsonObject q = new JsonObject();
                MediaQuery query = c.Query;
                if (query.MinWidth.HasValue) q.Set("minWidth", JsonValue.From(query.MinWidth.Value));
                if (query.MaxWidth.HasValue) q.Set("maxWidth", JsonValue.From(query.MaxWidth.Value));
                if (query.MinHeight.HasValue) q.Set("minHeight", JsonValue.From(query.MinHeight.Value));
                if (query.MaxHeight.HasValue) q.Set("maxHeight", JsonValue.From(query.MaxHeight.Value));
                if (query.MinAspect.HasValue) q.Set("minAspect", JsonValue.From(query.MinAspect.Value));
                if (query.MaxAspect.HasValue) q.Set("maxAspect", JsonValue.From(query.MaxAspect.Value));
                if (!string.IsNullOrEmpty(query.Orientation)) q.Set("orientation", query.Orientation);
                obj.Set("query", q);
                classes.Add(obj);
            }

            root.Set("classes", classes);
            return Text(root);
        }

        public static string Runtime(IReadOnlyList<RuntimeEntry> entries)
        {
            JsonObject root = new JsonObject();
            root.Set("schema", 1);
            JsonArray components = new JsonArray();
            List<RuntimeEntry> sorted = new List<RuntimeEntry>(entries);
            sorted.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.Id, b.Id);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Selector ?? "", b.Selector ?? "");
                return c != 0 ? c : string.CompareOrdinal(a.Uses ?? "", b.Uses ?? "");
            });
            foreach (RuntimeEntry e in sorted)
            {
                JsonObject obj = new JsonObject();
                obj.Set("id", e.Id);
                if (!string.IsNullOrEmpty(e.Uses)) obj.Set("uses", e.Uses);
                if (!string.IsNullOrEmpty(e.Selector)) obj.Set("selector", e.Selector);
                if (e.Params != null && e.Params.Count > 0)
                {
                    JsonObject p = new JsonObject();
                    List<string> names = new List<string>(e.Params.Keys);
                    names.Sort(StringComparer.Ordinal);
                    foreach (string name in names) p.Set(name, e.Params[name]);
                    obj.Set("params", p);
                }

                components.Add(obj);
            }

            root.Set("components", components);
            return Text(root);
        }

        public static string Report(DiagnosticBag bag, string version)
        {
            JsonObject root = new JsonObject();
            root.Set("tool", "htmltouitk");
            root.Set("version", version);
            Verdict verdict = bag.Verdict;
            root.Set("verdict", verdict == Verdict.Fail ? "FAIL" : verdict == Verdict.PassWithWarnings ? "PASS WITH WARNINGS" : "PASS");
            JsonObject counts = new JsonObject();
            counts.Set("error", bag.Count(Severity.Error));
            counts.Set("warning", bag.Count(Severity.Warning));
            counts.Set("info", bag.Count(Severity.Info));
            root.Set("counts", counts);
            int native = 0, convert = 0, runtime = 0, reject = 0;
            JsonArray findings = new JsonArray();
            foreach (Diagnostic d in bag.Sorted())
            {
                switch (d.Tier)
                {
                    case Tier.Native: native++; break;
                    case Tier.ConvertTime: convert++; break;
                    case Tier.Runtime: runtime++; break;
                    default: reject++; break;
                }

                JsonObject f = new JsonObject();
                f.Set("rule", d.RuleId ?? "");
                f.Set("severity", d.Severity.ToString());
                f.Set("tier", d.Tier.ToString());
                if (!string.IsNullOrEmpty(d.EmulationId)) f.Set("emulation", d.EmulationId);
                f.Set("file", d.Span.File ?? "");
                f.Set("line", d.Span.Start.Line);
                f.Set("column", d.Span.Start.Column);
                f.Set("endLine", d.Span.End.Line);
                f.Set("endColumn", d.Span.End.Column);
                f.Set("construct", d.Construct ?? "");
                f.Set("message", d.Message ?? "");
                f.Set("why", d.Why ?? "");
                f.Set("suggestion", d.Suggestion ?? "");
                JsonArray related = new JsonArray();
                foreach (SourceSpan span in d.Related)
                {
                    JsonObject r = new JsonObject();
                    r.Set("file", span.File ?? "");
                    r.Set("line", span.Start.Line);
                    r.Set("column", span.Start.Column);
                    related.Add(r);
                }

                f.Set("related", related);
                findings.Add(f);
            }

            JsonObject tiers = new JsonObject();
            tiers.Set("native", native);
            tiers.Set("convertTime", convert);
            tiers.Set("runtime", runtime);
            tiers.Set("reject", reject);
            root.Set("tiers", tiers);
            root.Set("findings", findings);
            return Text(root);
        }
    }
}
