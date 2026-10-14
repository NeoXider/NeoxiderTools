using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlToUitk.Convert.Css
{
    internal readonly struct ColorValue
    {
        public readonly int R;
        public readonly int G;
        public readonly int B;
        public readonly double A;

        public ColorValue(int r, int g, int b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    internal static class CssText
    {
        private static readonly Regex LengthRe = new Regex(
            @"^([+-]?(?:\d+\.?\d*|\.\d+))(px|%|deg|s|ms|em|rem|vh|vw|vmin|vmax|pt|ch|ex|fr|cm|mm|in|pc|rad|turn|grad)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex HexRe = new Regex("^#([0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant);
        private static readonly Regex RgbRe = new Regex(@"^rgba?\((.*)\)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex VarRe = new Regex(@"var\(\s*(--[A-Za-z0-9_-]+)\s*(,)?", RegexOptions.CultureInvariant);

        private static readonly Dictionary<string, ColorValue> Named = new Dictionary<string, ColorValue>(StringComparer.Ordinal)
        {
            { "transparent", new ColorValue(0, 0, 0, 0.0) },
            { "black", new ColorValue(0, 0, 0, 1.0) },
            { "white", new ColorValue(255, 255, 255, 1.0) },
            { "red", new ColorValue(255, 0, 0, 1.0) },
            { "green", new ColorValue(0, 128, 0, 1.0) },
            { "blue", new ColorValue(0, 0, 255, 1.0) },
            { "yellow", new ColorValue(255, 255, 0, 1.0) },
            { "cyan", new ColorValue(0, 255, 255, 1.0) },
            { "magenta", new ColorValue(255, 0, 255, 1.0) },
            { "gray", new ColorValue(128, 128, 128, 1.0) },
            { "grey", new ColorValue(128, 128, 128, 1.0) },
            { "orange", new ColorValue(255, 165, 0, 1.0) },
            { "purple", new ColorValue(128, 0, 128, 1.0) },
            { "pink", new ColorValue(255, 192, 203, 1.0) },
            { "brown", new ColorValue(165, 42, 42, 1.0) },
            { "gold", new ColorValue(255, 215, 0, 1.0) },
            { "silver", new ColorValue(192, 192, 192, 1.0) }
        };

        public static List<string> SplitTop(string text, char separator)
        {
            var parts = new List<string>();
            int depth = 0;
            char quote = '\0';
            int start = 0;
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (quote != '\0')
                {
                    if (ch == '\\')
                    {
                        i += 2;
                        continue;
                    }

                    if (ch == quote) quote = '\0';
                }
                else if (ch == '"' || ch == '\'') quote = ch;
                else if (ch == '(' || ch == '[') depth++;
                else if (ch == ')' || ch == ']') depth = Math.Max(0, depth - 1);
                else if (ch == separator && depth == 0)
                {
                    parts.Add(text.Substring(start, i - start));
                    start = i + 1;
                }

                i++;
            }

            parts.Add(text.Substring(start));
            return parts;
        }

        public static List<string> SplitWs(string text)
        {
            var parts = new List<string>();
            int depth = 0;
            char quote = '\0';
            var current = new StringBuilder();
            string trimmed = text.Trim();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char ch = trimmed[i];
                if (quote != '\0')
                {
                    current.Append(ch);
                    if (ch == quote) quote = '\0';
                    continue;
                }

                if (ch == '"' || ch == '\'')
                {
                    quote = ch;
                    current.Append(ch);
                }
                else if (ch == '(' || ch == '[')
                {
                    depth++;
                    current.Append(ch);
                }
                else if (ch == ')' || ch == ']')
                {
                    depth = Math.Max(0, depth - 1);
                    current.Append(ch);
                }
                else if (char.IsWhiteSpace(ch) && depth == 0)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                }
                else current.Append(ch);
            }

            if (current.Length > 0) parts.Add(current.ToString());
            return parts;
        }

        public static bool TryNumber(string token, out double number, out string unit)
        {
            number = 0;
            unit = "";
            Match match = LengthRe.Match(token.Trim());
            if (!match.Success) return false;
            number = double.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            unit = match.Groups[2].Success ? match.Groups[2].Value.ToLowerInvariant() : "";
            return true;
        }

        public static bool TryColor(string token, out ColorValue color)
        {
            color = default(ColorValue);
            string text = token.Trim();
            string low = text.ToLowerInvariant();
            if (Named.TryGetValue(low, out color)) return true;
            Match hex = HexRe.Match(text);
            if (hex.Success)
            {
                string digits = hex.Groups[1].Value;
                if (digits.Length == 3 || digits.Length == 4)
                {
                    var sb = new StringBuilder();
                    foreach (char c in digits) sb.Append(c).Append(c);
                    digits = sb.ToString();
                }

                int r = Hex(digits, 0);
                int g = Hex(digits, 2);
                int b = Hex(digits, 4);
                double a = digits.Length == 8 ? Hex(digits, 6) / 255.0 : 1.0;
                color = new ColorValue(r, g, b, a);
                return true;
            }

            Match rgb = RgbRe.Match(low);
            if (rgb.Success)
            {
                string body = rgb.Groups[1].Value.Replace('/', ' ').Replace(',', ' ');
                string[] parts = body.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3 && parts.Length != 4) return false;
                var values = new double[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool percent = part.EndsWith("%", StringComparison.Ordinal);
                    double value;
                    if (!double.TryParse(percent ? part.Substring(0, part.Length - 1) : part, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
                    if (i < 3) values[i] = percent ? Math.Round(value * 2.55) : Math.Round(value);
                    else values[i] = percent ? value / 100.0 : value;
                }

                double alpha = parts.Length == 4 ? values[3] : 1.0;
                color = new ColorValue(Clamp255(values[0]), Clamp255(values[1]), Clamp255(values[2]), Math.Max(0.0, Math.Min(1.0, alpha)));
                return true;
            }

            return false;
        }

        private static int Hex(string digits, int index)
        {
            return int.Parse(digits.Substring(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static int Clamp255(double value)
        {
            return (int)Math.Max(0, Math.Min(255, value));
        }

        public static string FormatColor(ColorValue color, string authored = null)
        {
            if (authored != null)
            {
                string trimmed = authored.Trim();
                if (HexRe.IsMatch(trimmed) && (trimmed.Length == 4 || trimmed.Length == 7)) return trimmed;
            }

            if (color.A >= 0.9999) return "rgb(" + color.R + ", " + color.G + ", " + color.B + ")";
            return "rgba(" + color.R + ", " + color.G + ", " + color.B + ", " + Fixed(color.A, 3) + ")";
        }

        public static string Fixed(double value, int decimals)
        {
            decimal rounded = Math.Round((decimal)value, decimals, MidpointRounding.ToEven);
            string text = rounded.ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (text.IndexOf('.') >= 0) text = text.TrimEnd('0').TrimEnd('.');
            if (text == "" || text == "-0") return "0";
            return text;
        }

        public static string FormatLength(double number, string unit)
        {
            string text = Fixed(number, 4);
            if (text == "0" && (unit == "px" || unit == "")) return "0";
            return text + unit;
        }

        public static string FormatNumber(double number)
        {
            return Fixed(number, 4);
        }

        public static bool IsHexColor(string token)
        {
            return HexRe.IsMatch(token.Trim());
        }

        public static bool IsColorLiteral(string token)
        {
            string text = token.Trim();
            if (HexRe.IsMatch(text)) return true;
            string low = text.ToLowerInvariant();
            return low.StartsWith("rgb(", StringComparison.Ordinal) || low.StartsWith("rgba(", StringComparison.Ordinal)
                || low.StartsWith("hsl(", StringComparison.Ordinal) || low.StartsWith("hsla(", StringComparison.Ordinal);
        }

        public static List<KeyValuePair<string, bool>> VarRefs(string value)
        {
            var list = new List<KeyValuePair<string, bool>>();
            foreach (Match m in VarRe.Matches(value)) list.Add(new KeyValuePair<string, bool>(m.Groups[1].Value, m.Groups[2].Success));
            return list;
        }

        public static string Collapse(string text)
        {
            var sb = new StringBuilder();
            bool space = false;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    space = true;
                    continue;
                }

                if (space && sb.Length > 0) sb.Append(' ');
                space = false;
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string NormaliseTokenValue(string value)
        {
            string text = value.Trim();
            string low = text.ToLowerInvariant();
            ColorValue color;
            if ((low.StartsWith("rgb(", StringComparison.Ordinal) || low.StartsWith("rgba(", StringComparison.Ordinal)) && TryColor(text, out color))
            {
                return FormatColor(color);
            }

            return Collapse(text);
        }
    }
}
