using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    internal static class EmulationHelpers
    {
        public static string MatchingSelectorText(StyleRule rule, UxmlElement element)
        {
            if (rule == null || rule.Selectors.Count == 0) return "";
            if (element != null && element.Source != null)
            {
                foreach (Selector selector in rule.Selectors)
                {
                    if (SelectorMatcher.Matches(selector, element.Source)) return SelectorFormat.Text(selector);
                }
            }

            return SelectorFormat.Text(rule.Selectors[0]);
        }

        public static bool TryPx(string text, out double px)
        {
            px = 0;
            if (string.IsNullOrEmpty(text)) return true;
            double number;
            string unit;
            if (!CssText.TryNumber(text, out number, out unit)) return false;
            if (unit == "px" || (unit == "" && number == 0))
            {
                px = number;
                return true;
            }

            return false;
        }

        public static List<Declaration> ParseBlock(string block, SourceSpan span)
        {
            var list = new List<Declaration>();
            foreach (string piece in block.Split(';'))
            {
                string trimmed = piece.Trim();
                if (trimmed.Length == 0) continue;
                int colon = trimmed.IndexOf(':');
                list.Add(RuleOps.Decl(trimmed.Substring(0, colon).Trim(), trimmed.Substring(colon + 1).Trim(), span));
            }

            return list;
        }

        public static StyleRule ClassRule(string className, string block, SourceSpan span, int order)
        {
            return RuleOps.Rule(new[] { SelectorFormat.ForClass(className, span) }, ParseBlock(block, span), span, order);
        }
    }

    public sealed class GridFlattenEmulation : EmulationBase
    {
        private static readonly Regex AreaRowRe = new Regex("\"([^\"]*)\"|'([^']*)'", RegexOptions.CultureInvariant);

        public override string Id => "grid-flatten";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "display:grid" };

        public override int Order => 70;

        private sealed class Area
        {
            public int Row;
            public int Column;
            public int RowSpan;
            public int ColumnSpan;
        }

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var names = new NameGenerator();
            var blocks = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var emitted = new HashSet<string>(StringComparer.Ordinal);
            var containers = new List<UxmlElement>();
            foreach (UxmlElement element in state.Elements())
            {
                if ((state.Prop(element, "display") ?? "").ToLowerInvariant() == "grid") containers.Add(element);
            }

            foreach (UxmlElement container in containers) Flatten(state, names, container, blocks, emitted);
        }

        private static void Fail(TransformState state, UxmlElement container, string reason)
        {
            ResolvedValue source = state.PropSource(container, "display");
            SourceSpan span = source != null && source.Source != null ? source.Source.Span : SourceSpan.None;
            state.Report("E001", Severity.Error, Tier.Reject, span, "display: grid", "grid-flatten cannot resolve the grid: " + reason, "Use a fixed template or nested flex containers.", "grid-flatten");
        }

        private static Dictionary<string, Area> ParseAreas(string text, out int columns, out int rows)
        {
            var areas = new Dictionary<string, Area>(StringComparer.Ordinal);
            columns = 0;
            rows = 0;
            if (string.IsNullOrEmpty(text) || text.Trim().ToLowerInvariant() == "none") return areas;
            int row = 0;
            foreach (Match m in AreaRowRe.Matches(text))
            {
                string line = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                string[] cells = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                columns = Math.Max(columns, cells.Length);
                for (int c = 0; c < cells.Length; c++)
                {
                    string name = cells[c];
                    if (name == ".") continue;
                    Area area;
                    if (!areas.TryGetValue(name, out area))
                    {
                        areas[name] = new Area { Row = row, Column = c, RowSpan = 1, ColumnSpan = 1 };
                    }
                    else
                    {
                        area.ColumnSpan = Math.Max(area.ColumnSpan, c - area.Column + 1);
                        area.RowSpan = Math.Max(area.RowSpan, row - area.Row + 1);
                    }
                }

                row++;
            }

            rows = row;
            return areas;
        }

        private static void Flatten(TransformState state, NameGenerator names, UxmlElement container, Dictionary<string, List<string>> blocks, HashSet<string> emitted)
        {
            string template = state.Prop(container, "grid-template-columns");
            Dictionary<string, Area> areas = null;
            int areaColumns = 0;
            int areaRows = 0;
            string areasText = state.Prop(container, "grid-template-areas");
            if (!string.IsNullOrEmpty(areasText)) areas = ParseAreas(areasText, out areaColumns, out areaRows);
            List<Track> tracks;
            string problem;
            if (template == null || template.ToLowerInvariant() == "none")
            {
                if (areas != null && areaColumns > 0)
                {
                    tracks = new List<Track>();
                    for (int i = 0; i < areaColumns; i++) tracks.Add(new Track(TrackKind.Fr, 1));
                }
                else
                {
                    Fail(state, container, "no grid-template-columns");
                    return;
                }
            }
            else if (!GridLayout.ParseTemplate(template, state.Config.Emulation.GridMaxTracks, out tracks, out problem))
            {
                Fail(state, container, problem);
                return;
            }

            string flow = (state.Prop(container, "grid-auto-flow") ?? "row").ToLowerInvariant();
            if (flow.Contains("column") || flow.Contains("dense"))
            {
                Fail(state, container, "grid-auto-flow: " + flow);
                return;
            }

            double rowGap;
            double columnGap;
            if (!EmulationHelpers.TryPx(state.Prop(container, "row-gap"), out rowGap) || !EmulationHelpers.TryPx(state.Prop(container, "column-gap"), out columnGap))
            {
                Fail(state, container, "the gap is not a pixel length");
                return;
            }

            var items = new List<GridItem>();
            for (int i = 0; i < container.Children.Count; i++)
            {
                UxmlElement child = container.Children[i];
                var item = new GridItem { Index = i };
                string area = state.Prop(child, "grid-area");
                Area named;
                if (!string.IsNullOrEmpty(area) && areas != null && areas.TryGetValue(area.Trim(), out named))
                {
                    item.Column = named.Column;
                    item.ColumnSpan = named.ColumnSpan;
                    item.Row = named.Row;
                    item.RowSpan = named.RowSpan;
                    item.ExplicitColumn = true;
                    item.ExplicitRow = true;
                }
                else if (!string.IsNullOrEmpty(area))
                {
                    Fail(state, container, "grid-area: " + area);
                    return;
                }
                else
                {
                    string column = state.Prop(child, "grid-column") ?? Join(state.Prop(child, "grid-column-start"), state.Prop(child, "grid-column-end"));
                    string row = state.Prop(child, "grid-row") ?? Join(state.Prop(child, "grid-row-start"), state.Prop(child, "grid-row-end"));
                    int start, span;
                    bool hasStart;
                    if (!GridLayout.ParseLine(column, out start, out span, out hasStart, out problem))
                    {
                        Fail(state, container, "grid-column: " + problem);
                        return;
                    }

                    item.ColumnSpan = span;
                    if (hasStart)
                    {
                        item.Column = start;
                        item.ExplicitColumn = true;
                    }

                    if (!GridLayout.ParseLine(row, out start, out span, out hasStart, out problem))
                    {
                        Fail(state, container, "grid-row: " + problem);
                        return;
                    }

                    if (span > 1)
                    {
                        Fail(state, container, "a row span cannot be expressed with nested flex rows");
                        return;
                    }

                    if (hasStart)
                    {
                        item.Row = start;
                        item.ExplicitRow = true;
                    }
                }

                items.Add(item);
            }

            GridPlacement placement = GridLayout.Place(items, tracks.Count);
            ResolvedValue declaring = state.PropSource(container, "display");
            string key = EmulationHelpers.MatchingSelectorText(declaring == null ? null : declaring.Rule, container);
            string hash = names.Generate("grid", key).Substring("htu-grid-".Length);
            string rowClass = "htu-grid-row-" + hash;
            string rowGapClass = "htu-grid-rowgap-" + hash;
            string colGapClass = "htu-grid-colgap-" + hash;
            List<string> distinct;
            if (!blocks.TryGetValue(hash, out distinct))
            {
                distinct = new List<string>();
                blocks[hash] = distinct;
            }

            var rows = new List<UxmlElement>();
            for (int r = 0; r < placement.RowCount; r++)
            {
                var cells = new SortedDictionary<int, GridItem>();
                foreach (GridItem item in placement.Items)
                {
                    if (item.Row == r) cells[item.Column] = item;
                }

                var rowElements = new List<UxmlElement>();
                var rowEntries = new List<KeyValuePair<UxmlElement, string>>();
                int c = 0;
                while (c < tracks.Count)
                {
                    GridItem item;
                    int span = 1;
                    UxmlElement element = null;
                    if (cells.TryGetValue(c, out item))
                    {
                        span = item.ColumnSpan;
                        element = container.Children[item.Index];
                    }

                    string block = GridLayout.CellBlock(tracks, c, span, columnGap);
                    int number = distinct.IndexOf(block) + 1;
                    if (number == 0)
                    {
                        distinct.Add(block);
                        number = distinct.Count;
                    }

                    rowEntries.Add(new KeyValuePair<UxmlElement, string>(element, "htu-grid-cell-" + hash + "-" + number));
                    c += span;
                }

                for (int e = 0; e < rowEntries.Count; e++)
                {
                    UxmlElement element = rowEntries[e].Key;
                    string cellClass = rowEntries[e].Value;
                    bool last = e == rowEntries.Count - 1;
                    if (element == null)
                    {
                        string classes = cellClass + (!last && columnGap > 0 ? " " + colGapClass : "");
                        element = ElementEditor.Create("VisualElement", "class", classes, "picking-mode", "Ignore");
                    }
                    else
                    {
                        state.Editor.AddClass(element, cellClass);
                        if (!last && columnGap > 0) state.Editor.AddClass(element, colGapClass);
                    }

                    rowElements.Add(element);
                }

                string rowClasses = rowClass + (r > 0 && rowGap > 0 ? " " + rowGapClass : "");
                UxmlElement wrapper = ElementEditor.Create("VisualElement", "class", rowClasses);
                wrapper.Children = rowElements;
                foreach (UxmlElement element in rowElements) element.Parent = wrapper;
                wrapper.Parent = container;
                rows.Add(wrapper);
            }

            container.Children = rows;
            state.Handled.Add(container);
            if (declaring != null && emitted.Add(hash))
            {
                SourceSpan span = declaring.Source != null ? declaring.Source.Span : SourceSpan.None;
                int order = declaring.Rule.Order;
                var generated = new List<StyleRule>
                {
                    EmulationHelpers.ClassRule(rowClass, "flex-direction: row", span, order)
                };
                if (rowGap > 0) generated.Add(EmulationHelpers.ClassRule(rowGapClass, "margin-top: " + CssText.FormatLength(rowGap, "px"), span, order));
                if (columnGap > 0) generated.Add(EmulationHelpers.ClassRule(colGapClass, "margin-right: " + CssText.FormatLength(columnGap, "px"), span, order));
                for (int i = 0; i < distinct.Count; i++) generated.Add(EmulationHelpers.ClassRule("htu-grid-cell-" + hash + "-" + (i + 1), distinct[i], span, order));
                RuleOps.InsertAfter(state.Sheets, declaring.Rule, generated, state.PrimarySheetId);
            }
            else if (declaring != null)
            {
                SourceSpan span = declaring.Source != null ? declaring.Source.Span : SourceSpan.None;
                int order = declaring.Rule.Order;
                var missing = new List<StyleRule>();
                for (int i = 0; i < distinct.Count; i++)
                {
                    missing.Add(EmulationHelpers.ClassRule("htu-grid-cell-" + hash + "-" + (i + 1), distinct[i], span, order));
                }

                RemoveDuplicateCellRules(state, hash, missing);
            }

            foreach (string gapName in new[] { "row-gap", "column-gap" })
            {
                ResolvedValue gap = state.PropSource(container, gapName);
                if (gap != null && gap.Rule != null) RuleOps.StripDeclarations(state.Sheets, gap.Rule, d => d.Name == "gap" || d.Name == "row-gap" || d.Name == "column-gap");
            }
        }

        private static void RemoveDuplicateCellRules(TransformState state, string hash, List<StyleRule> wanted)
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, StyleRule> pair in RuleOps.All(state.Sheets))
            {
                existing.Add(RuleOps.SelectorsText(pair.Value));
            }

            var add = new List<StyleRule>();
            foreach (StyleRule rule in wanted)
            {
                if (!existing.Contains(RuleOps.SelectorsText(rule))) add.Add(rule);
            }

            if (add.Count == 0) return;
            string target = state.PrimarySheetId;
            var list = new List<StyleRule>(state.Sheets.Rules(target));
            list.AddRange(add);
            state.Sheets.ReplaceRules(target, list);
        }

        private static string Join(string start, string end)
        {
            if (start == null && end == null) return null;
            if (end == null) return start;
            return (start ?? "auto") + " / " + end;
        }
    }

    public sealed class GapMarginsEmulation : EmulationBase
    {
        public override string Id => "gap-margins";

        public override Tier Tier => Tier.ConvertTime;

        public override IReadOnlyList<string> Handles => new[] { "prop:gap", "prop:row-gap", "prop:column-gap" };

        public override int Order => 80;

        internal override void Run(TransformState state, Construct construct, EmulationOutcome outcome)
        {
            var names = new NameGenerator();
            var emitted = new HashSet<string>(StringComparer.Ordinal);
            var elements = new List<UxmlElement>(state.Elements());
            foreach (UxmlElement element in elements)
            {
                string rowText = state.Prop(element, "row-gap");
                string colText = state.Prop(element, "column-gap");
                if (rowText == null && colText == null) continue;
                if (state.Handled.Contains(element)) continue;
                if (element.Children.Count == 0) continue;
                double row, column;
                if (!EmulationHelpers.TryPx(rowText, out row) || !EmulationHelpers.TryPx(colText, out column))
                {
                    ResolvedValue bad = state.PropSource(element, rowText != null ? "row-gap" : "column-gap");
                    state.Report("E001", Severity.Error, Tier.Reject, bad != null && bad.Source != null ? bad.Source.Span : SourceSpan.None, "gap", "gap-margins needs pixel gaps", "Use px values for gap.", "gap-margins");
                    continue;
                }

                if (row == 0 && column == 0) continue;
                string direction = (state.Prop(element, "flex-direction") ?? "column").ToLowerInvariant();
                string wrap = (state.Prop(element, "flex-wrap") ?? "nowrap").ToLowerInvariant();
                ResolvedValue declaring = state.PropSource(element, rowText != null ? "row-gap" : "column-gap");
                string key = EmulationHelpers.MatchingSelectorText(declaring == null ? null : declaring.Rule, element);
                string hash = names.Generate("gap", key).Substring("htu-gap-".Length);
                string childClass = "htu-gap-" + hash;
                SourceSpan span = declaring != null && declaring.Source != null ? declaring.Source.Span : SourceSpan.None;
                int order = declaring != null && declaring.Rule != null ? declaring.Rule.Order : 0;
                var generated = new List<StyleRule>();
                if (wrap != "nowrap")
                {
                    string containerClass = "htu-gapwrap-" + hash;
                    string negative = Sides(-column, -row);
                    string positive = Sides(column, row);
                    state.Editor.AddClass(element, containerClass);
                    foreach (UxmlElement child in element.Children) state.Editor.AddClass(child, childClass);
                    if (emitted.Add(containerClass))
                    {
                        generated.Add(EmulationHelpers.ClassRule(containerClass, negative, span, order));
                        generated.Add(EmulationHelpers.ClassRule(childClass, positive, span, order));
                    }

                    state.Report("C002", Severity.Warning, Tier.ConvertTime, span, "gap", "gap with flex-wrap is approximated with margins and a negative container margin", "The container box grows by the gap.", "gap-margins");
                }
                else
                {
                    bool horizontal = direction.StartsWith("row", StringComparison.Ordinal);
                    bool reverse = direction.EndsWith("reverse", StringComparison.Ordinal);
                    string side = horizontal ? (reverse ? "margin-left" : "margin-right") : (reverse ? "margin-top" : "margin-bottom");
                    double amount = horizontal ? column : row;
                    if (amount <= 0) continue;
                    for (int i = 0; i < element.Children.Count - 1; i++) state.Editor.AddClass(element.Children[i], childClass);
                    if (emitted.Add(childClass)) generated.Add(EmulationHelpers.ClassRule(childClass, side + ": " + CssText.FormatLength(amount, "px"), span, order));
                }

                if (generated.Count > 0 && declaring != null && declaring.Rule != null) RuleOps.InsertAfter(state.Sheets, declaring.Rule, generated, state.PrimarySheetId);
            }
        }

        private static string Sides(double right, double bottom)
        {
            var parts = new List<string>();
            if (right != 0) parts.Add("margin-right: " + CssText.FormatLength(right, "px"));
            if (bottom != 0) parts.Add("margin-bottom: " + CssText.FormatLength(bottom, "px"));
            return string.Join("; ", parts);
        }
    }
}
