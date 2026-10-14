using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HtmlToUitk.Convert.Css
{
    internal enum TrackKind
    {
        Px,
        Percent,
        Fr,
        Auto
    }

    internal readonly struct Track
    {
        public readonly TrackKind Kind;
        public readonly double Value;

        public Track(TrackKind kind, double value)
        {
            Kind = kind;
            Value = value;
        }
    }

    internal sealed class GridItem
    {
        public int Index;
        public int Row = -1;
        public int Column = -1;
        public int ColumnSpan = 1;
        public int RowSpan = 1;
        public bool ExplicitRow;
        public bool ExplicitColumn;
    }

    internal sealed class GridPlacement
    {
        public List<GridItem> Items = new List<GridItem>();
        public int RowCount;
    }

    internal static class GridLayout
    {
        private static readonly Regex IntegerRe = new Regex(@"^[+-]?\d+$", RegexOptions.CultureInvariant);

        public static bool ParseTemplate(string text, int maxTracks, out List<Track> tracks, out string problem)
        {
            tracks = new List<Track>();
            problem = null;
            if (!ParseInto(text, tracks, ref problem)) return false;
            if (tracks.Count == 0)
            {
                problem = "the template has no tracks";
                return false;
            }

            if (tracks.Count > maxTracks)
            {
                problem = "the template has " + tracks.Count + " tracks";
                return false;
            }

            return true;
        }

        private static bool ParseInto(string text, List<Track> tracks, ref string problem)
        {
            foreach (string token in CssText.SplitWs(text))
            {
                string low = token.ToLowerInvariant();
                if (low.StartsWith("[", StringComparison.Ordinal)) continue;
                if (low.StartsWith("repeat(", StringComparison.Ordinal))
                {
                    string inner = low.Substring(7, low.Length - 8);
                    List<string> parts = CssText.SplitTop(inner, ',');
                    if (parts.Count < 2)
                    {
                        problem = "repeat() needs a count and tracks";
                        return false;
                    }

                    string count = parts[0].Trim();
                    if (!IntegerRe.IsMatch(count))
                    {
                        problem = count;
                        return false;
                    }

                    int n = int.Parse(count, CultureInfo.InvariantCulture);
                    if (n < 1 || n > 1000)
                    {
                        problem = "repeat count " + n;
                        return false;
                    }

                    string body = string.Join(",", parts.GetRange(1, parts.Count - 1));
                    var single = new List<Track>();
                    if (!ParseInto(body, single, ref problem)) return false;
                    for (int i = 0; i < n; i++) tracks.AddRange(single);
                    continue;
                }

                if (low.StartsWith("minmax(", StringComparison.Ordinal) || low.StartsWith("fit-content(", StringComparison.Ordinal))
                {
                    problem = "minmax()";
                    return false;
                }

                if (low == "auto")
                {
                    tracks.Add(new Track(TrackKind.Auto, 1));
                    continue;
                }

                if (low == "min-content" || low == "max-content" || low == "masonry" || low == "subgrid" || low == "none")
                {
                    problem = low;
                    return false;
                }

                double number;
                string unit;
                if (low.EndsWith("fr", StringComparison.Ordinal) && CssText.TryNumber(low.Substring(0, low.Length - 2), out number, out unit) && unit == "")
                {
                    tracks.Add(new Track(TrackKind.Fr, number));
                    continue;
                }

                if (CssText.TryNumber(low, out number, out unit))
                {
                    if (unit == "px" || (unit == "" && number == 0))
                    {
                        tracks.Add(new Track(TrackKind.Px, number));
                        continue;
                    }

                    if (unit == "%")
                    {
                        tracks.Add(new Track(TrackKind.Percent, number));
                        continue;
                    }
                }

                problem = token;
                return false;
            }

            return true;
        }

        public static bool ParseLine(string text, out int start, out int span, out bool hasStart, out string problem)
        {
            start = -1;
            span = 1;
            hasStart = false;
            problem = null;
            if (string.IsNullOrWhiteSpace(text) || text.Trim().ToLowerInvariant() == "auto") return true;
            string[] halves = text.Split('/');
            if (halves.Length > 2)
            {
                problem = text;
                return false;
            }

            string first = halves[0].Trim().ToLowerInvariant();
            string second = halves.Length == 2 ? halves[1].Trim().ToLowerInvariant() : null;
            int firstLine = 0;
            bool firstSpan = false;
            int firstSpanValue = 1;
            if (!ParseSide(first, out firstLine, out firstSpan, out firstSpanValue))
            {
                problem = text;
                return false;
            }

            if (second == null)
            {
                if (firstSpan)
                {
                    span = firstSpanValue;
                    return true;
                }

                if (firstLine == 0) return true;
                start = firstLine - 1;
                hasStart = true;
                span = 1;
                return true;
            }

            int secondLine;
            bool secondSpan;
            int secondSpanValue;
            if (!ParseSide(second, out secondLine, out secondSpan, out secondSpanValue))
            {
                problem = text;
                return false;
            }

            if (!firstSpan && firstLine > 0)
            {
                start = firstLine - 1;
                hasStart = true;
                if (secondSpan) span = secondSpanValue;
                else if (secondLine > 0) span = Math.Max(1, secondLine - firstLine);
                return true;
            }

            if (firstSpan && !secondSpan && secondLine > 0)
            {
                start = secondLine - 1 - firstSpanValue;
                if (start < 0) start = 0;
                hasStart = true;
                span = firstSpanValue;
                return true;
            }

            problem = text;
            return false;
        }

        private static bool ParseSide(string text, out int line, out bool span, out int spanValue)
        {
            line = 0;
            span = false;
            spanValue = 1;
            if (text == "auto") return true;
            if (text.StartsWith("span", StringComparison.Ordinal))
            {
                span = true;
                string rest = text.Substring(4).Trim();
                if (rest.Length == 0) return true;
                if (!IntegerRe.IsMatch(rest)) return false;
                spanValue = Math.Max(1, int.Parse(rest, CultureInfo.InvariantCulture));
                return true;
            }

            if (!IntegerRe.IsMatch(text)) return false;
            line = int.Parse(text, CultureInfo.InvariantCulture);
            if (line < 1) return false;
            return true;
        }

        public static GridPlacement Place(IReadOnlyList<GridItem> items, int columns)
        {
            var placement = new GridPlacement();
            var occupied = new HashSet<long>();
            int cursorRow = 0;
            int cursorColumn = 0;
            int maxRow = -1;
            foreach (GridItem source in items)
            {
                var item = new GridItem
                {
                    Index = source.Index,
                    ColumnSpan = Math.Max(1, Math.Min(source.ColumnSpan, columns)),
                    RowSpan = source.RowSpan,
                    ExplicitColumn = source.ExplicitColumn,
                    ExplicitRow = source.ExplicitRow
                };
                if (source.ExplicitColumn && source.ExplicitRow)
                {
                    item.Row = source.Row;
                    item.Column = Math.Min(source.Column, columns - item.ColumnSpan);
                }
                else if (source.ExplicitColumn)
                {
                    int column = Math.Min(source.Column, columns - item.ColumnSpan);
                    int row = cursorRow;
                    if (column < cursorColumn) row++;
                    while (!Free(occupied, row, column, item.ColumnSpan)) row++;
                    item.Row = row;
                    item.Column = column;
                    cursorRow = row;
                    cursorColumn = column + item.ColumnSpan;
                }
                else if (source.ExplicitRow)
                {
                    int row = source.Row;
                    int column = 0;
                    while (!Free(occupied, row, column, item.ColumnSpan))
                    {
                        column++;
                        if (column + item.ColumnSpan > columns)
                        {
                            column = 0;
                            row++;
                        }
                    }

                    item.Row = row;
                    item.Column = column;
                }
                else
                {
                    int row = cursorRow;
                    int column = cursorColumn;
                    while (true)
                    {
                        if (column + item.ColumnSpan > columns)
                        {
                            row++;
                            column = 0;
                        }

                        if (Free(occupied, row, column, item.ColumnSpan)) break;
                        column++;
                    }

                    item.Row = row;
                    item.Column = column;
                    cursorRow = row;
                    cursorColumn = column + item.ColumnSpan;
                }

                for (int c = 0; c < item.ColumnSpan; c++) occupied.Add(Key(item.Row, item.Column + c));
                if (item.Row > maxRow) maxRow = item.Row;
                placement.Items.Add(item);
            }

            placement.RowCount = maxRow + 1;
            return placement;
        }

        private static long Key(int row, int column)
        {
            return ((long)row << 20) | (long)column;
        }

        private static bool Free(HashSet<long> occupied, int row, int column, int span)
        {
            for (int c = 0; c < span; c++)
            {
                if (occupied.Contains(Key(row, column + c))) return false;
            }

            return true;
        }

        public static string CellBlock(IReadOnlyList<Track> tracks, int column, int span, double columnGap)
        {
            double px = 0;
            double percent = 0;
            double fr = 0;
            int autos = 0;
            for (int i = column; i < column + span && i < tracks.Count; i++)
            {
                Track t = tracks[i];
                switch (t.Kind)
                {
                    case TrackKind.Px: px += t.Value; break;
                    case TrackKind.Percent: percent += t.Value; break;
                    case TrackKind.Fr: fr += t.Value; break;
                    default: autos++; break;
                }
            }

            if (fr > 0 && autos == 0 && percent == 0 && px == 0) return "flex-grow: " + CssText.FormatNumber(fr) + "; flex-basis: 0";
            if (px > 0 && fr == 0 && autos == 0 && percent == 0) return "flex-shrink: 0; width: " + CssText.FormatLength(px + columnGap * (span - 1), "px");
            if (percent > 0 && fr == 0 && autos == 0 && px == 0) return "flex-shrink: 0; width: " + CssText.FormatLength(percent, "%");
            if (fr > 0 && px > 0 && autos == 0 && percent == 0) return "flex-grow: " + CssText.FormatNumber(fr) + "; flex-basis: " + CssText.FormatLength(px, "px");
            if (px == 0 && percent == 0 && fr == 0 && autos > 0) return "flex-grow: " + CssText.FormatNumber(autos) + "; flex-basis: auto";
            return "flex-grow: " + CssText.FormatNumber(Math.Max(1, fr + autos)) + "; flex-basis: " + CssText.FormatLength(px, "px");
        }
    }
}
