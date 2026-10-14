using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neo.HtmlToUitk.Animation
{
    public enum AnimValueKind
    {
        Numbers,
        Color,
        Discrete
    }

    public sealed class AnimValue
    {
        private AnimValue(AnimValueKind kind, float[] numbers, string[] units, string raw)
        {
            Kind = kind;
            Numbers = numbers;
            Units = units;
            Raw = raw;
        }

        public AnimValueKind Kind { get; }

        public float[] Numbers { get; }

        public string[] Units { get; }

        public string Raw { get; }

        public float R => Numbers[0];

        public float G => Numbers[1];

        public float B => Numbers[2];

        public float A => Numbers[3];

        public static AnimValue FromNumber(float value, string unit = "")
        {
            return new AnimValue(AnimValueKind.Numbers, new[] { value }, new[] { unit }, null);
        }

        public static AnimValue FromColor(float r, float g, float b, float a)
        {
            return new AnimValue(AnimValueKind.Color, new[] { r, g, b, a }, null, null);
        }

        public static AnimValue Discrete(string raw)
        {
            return new AnimValue(AnimValueKind.Discrete, null, null, raw);
        }

        public static AnimValue Parse(string text)
        {
            string value = (text ?? string.Empty).Trim();
            if (TryParseColor(value, out AnimValue color))
                return color;
            List<string> tokens = Tokenize(value);
            if (tokens.Count == 0)
                return Discrete(value);
            float[] numbers = new float[tokens.Count];
            string[] units = new string[tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                if (!TryParseNumber(tokens[i], out numbers[i], out units[i]))
                    return Discrete(value);
            }

            return new AnimValue(AnimValueKind.Numbers, numbers, units, value);
        }

        public static AnimValue Lerp(AnimValue a, AnimValue b, float t, string property)
        {
            if (a.Kind == AnimValueKind.Color && b.Kind == AnimValueKind.Color)
                return LerpColor(a, b, t);
            if (a.Kind == AnimValueKind.Numbers && b.Kind == AnimValueKind.Numbers && TryLerpNumbers(a, b, t, property, out AnimValue result))
                return result;
            return t < 0.5f ? a : b;
        }

        public string ToText()
        {
            if (Kind == AnimValueKind.Discrete)
                return Raw;
            StringBuilder builder = new StringBuilder();
            if (Kind == AnimValueKind.Color)
            {
                builder.Append("rgba(");
                for (int i = 0; i < 3; i++)
                {
                    if (i > 0)
                        builder.Append(", ");
                    builder.Append(Math.Round(Numbers[i] * 255f).ToString("0", CultureInfo.InvariantCulture));
                }

                builder.Append(", ").Append(Numbers[3].ToString("0.###", CultureInfo.InvariantCulture)).Append(')');
                return builder.ToString();
            }

            for (int i = 0; i < Numbers.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');
                builder.Append(Numbers[i].ToString("0.####", CultureInfo.InvariantCulture)).Append(Units[i]);
            }

            return builder.ToString();
        }

        private static AnimValue LerpColor(AnimValue a, AnimValue b, float t)
        {
            float alpha = a.A + (b.A - a.A) * t;
            if (alpha <= 1e-6f)
                return FromColor(0f, 0f, 0f, 0f);
            float r = (a.R * a.A * (1f - t) + b.R * b.A * t) / alpha;
            float g = (a.G * a.A * (1f - t) + b.G * b.A * t) / alpha;
            float bl = (a.B * a.A * (1f - t) + b.B * b.A * t) / alpha;
            return FromColor(r, g, bl, alpha);
        }

        private static bool TryLerpNumbers(AnimValue a, AnimValue b, float t, string property, out AnimValue result)
        {
            result = null;
            int count = Math.Max(a.Numbers.Length, b.Numbers.Length);
            bool pad = string.Equals(property, "translate", StringComparison.Ordinal);
            float[] numbers = new float[count];
            string[] units = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (!Component(a, i, pad, out float av, out string au) || !Component(b, i, pad, out float bv, out string bu))
                    return false;
                string unit;
                if (string.Equals(au, bu, StringComparison.Ordinal))
                    unit = au;
                else if (au.Length == 0 && Math.Abs(av) < 1e-6f)
                    unit = bu;
                else if (bu.Length == 0 && Math.Abs(bv) < 1e-6f)
                    unit = au;
                else
                    return false;
                numbers[i] = av + (bv - av) * t;
                units[i] = unit;
            }

            result = new AnimValue(AnimValueKind.Numbers, numbers, units, null);
            return true;
        }

        private static bool Component(AnimValue value, int index, bool pad, out float number, out string unit)
        {
            if (index < value.Numbers.Length)
            {
                number = value.Numbers[index];
                unit = value.Units[index];
                return true;
            }

            if (pad)
            {
                number = 0f;
                unit = value.Units[value.Units.Length - 1];
                return true;
            }

            if (value.Numbers.Length == 1)
            {
                number = value.Numbers[0];
                unit = value.Units[0];
                return true;
            }

            number = 0f;
            unit = string.Empty;
            return false;
        }

        private static List<string> Tokenize(string text)
        {
            List<string> tokens = new List<string>();
            int depth = 0;
            int start = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '(')
                    depth++;
                else if (c == ')')
                    depth--;
                bool space = char.IsWhiteSpace(c) && depth == 0;
                if (space)
                {
                    if (start >= 0)
                    {
                        tokens.Add(text.Substring(start, i - start));
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = i;
                }
            }

            if (start >= 0)
                tokens.Add(text.Substring(start));
            return tokens;
        }

        private static bool TryParseNumber(string token, out float number, out string unit)
        {
            number = 0f;
            unit = string.Empty;
            int end = 0;
            while (end < token.Length)
            {
                char c = token[end];
                bool numeric = (c >= '0' && c <= '9') || c == '.' || ((c == '-' || c == '+') && end == 0);
                if (!numeric)
                    break;
                end++;
            }

            if (end == 0 || !float.TryParse(token.Substring(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                return false;
            unit = token.Substring(end);
            for (int i = 0; i < unit.Length; i++)
            {
                if (!char.IsLetter(unit[i]) && unit[i] != '%')
                    return false;
            }

            return true;
        }

        private static bool TryParseColor(string value, out AnimValue color)
        {
            color = null;
            if (value.Length == 0)
                return false;
            if (value == "transparent")
            {
                color = FromColor(0f, 0f, 0f, 0f);
                return true;
            }

            if (value == "white")
            {
                color = FromColor(1f, 1f, 1f, 1f);
                return true;
            }

            if (value == "black")
            {
                color = FromColor(0f, 0f, 0f, 1f);
                return true;
            }

            if (value[0] == '#')
                return TryParseHex(value, out color);
            bool rgba = value.StartsWith("rgba(", StringComparison.Ordinal);
            if ((rgba || value.StartsWith("rgb(", StringComparison.Ordinal)) && value.EndsWith(")", StringComparison.Ordinal))
            {
                int open = value.IndexOf('(');
                string[] parts = value.Substring(open + 1, value.Length - open - 2).Split(new[] { ',', ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3 || parts.Length > 4)
                    return false;
                float[] channels = new float[4];
                channels[3] = 1f;
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    bool percent = part.EndsWith("%", StringComparison.Ordinal);
                    if (percent)
                        part = part.Substring(0, part.Length - 1);
                    if (!float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                        return false;
                    if (i < 3)
                        channels[i] = Clamp01(percent ? parsed / 100f : parsed / 255f);
                    else
                        channels[3] = Clamp01(percent ? parsed / 100f : parsed);
                }

                color = FromColor(channels[0], channels[1], channels[2], channels[3]);
                return true;
            }

            return false;
        }

        private static bool TryParseHex(string value, out AnimValue color)
        {
            color = null;
            string hex = value.Substring(1);
            if (hex.Length == 3 || hex.Length == 4)
            {
                StringBuilder expanded = new StringBuilder();
                for (int i = 0; i < hex.Length; i++)
                    expanded.Append(hex[i]).Append(hex[i]);
                hex = expanded.ToString();
            }

            if (hex.Length != 6 && hex.Length != 8)
                return false;
            float[] channels = new float[4];
            channels[3] = 1f;
            for (int i = 0; i < hex.Length / 2; i++)
            {
                if (!int.TryParse(hex.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int byteValue))
                    return false;
                channels[i] = byteValue / 255f;
            }

            color = FromColor(channels[0], channels[1], channels[2], channels[3]);
            return true;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
