using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlToUitk.Convert.Css
{
    internal static class CalcFold
    {
        private static readonly Regex FunctionStart = new Regex(@"(?<![A-Za-z0-9_-])(calc|min|max|clamp)\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private struct Quantity
        {
            public double Value;
            public string Unit;

            public Quantity(double value, string unit)
            {
                Value = value;
                Unit = unit;
            }
        }

        private sealed class Parser
        {
            private readonly string _text;
            private int _pos;

            public bool Failed;

            public Parser(string text)
            {
                _text = text;
            }

            public bool AtEnd
            {
                get
                {
                    SkipSpace();
                    return _pos >= _text.Length;
                }
            }

            private void SkipSpace()
            {
                while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
            }

            public Quantity ParseSum()
            {
                Quantity left = ParseProduct();
                while (!Failed)
                {
                    SkipSpace();
                    if (_pos >= _text.Length) break;
                    char c = _text[_pos];
                    if (c != '+' && c != '-') break;
                    _pos++;
                    Quantity right = ParseProduct();
                    left = Combine(left, right, c == '+' ? 1 : -1);
                }

                return left;
            }

            private Quantity Combine(Quantity a, Quantity b, int sign)
            {
                if (Failed) return a;
                if (a.Unit == b.Unit) return new Quantity(a.Value + sign * b.Value, a.Unit);
                if (b.Unit == "" && b.Value == 0) return a;
                if (a.Unit == "" && a.Value == 0) return new Quantity(sign * b.Value, b.Unit);
                Failed = true;
                return a;
            }

            private Quantity ParseProduct()
            {
                Quantity left = ParseUnary();
                while (!Failed)
                {
                    SkipSpace();
                    if (_pos >= _text.Length) break;
                    char c = _text[_pos];
                    if (c != '*' && c != '/') break;
                    _pos++;
                    Quantity right = ParseUnary();
                    if (Failed) break;
                    if (c == '*')
                    {
                        if (left.Unit == "") left = new Quantity(left.Value * right.Value, right.Unit);
                        else if (right.Unit == "") left = new Quantity(left.Value * right.Value, left.Unit);
                        else Failed = true;
                    }
                    else
                    {
                        if (right.Value == 0) Failed = true;
                        else if (right.Unit == "") left = new Quantity(left.Value / right.Value, left.Unit);
                        else if (right.Unit == left.Unit) left = new Quantity(left.Value / right.Value, "");
                        else Failed = true;
                    }
                }

                return left;
            }

            private Quantity ParseUnary()
            {
                SkipSpace();
                if (_pos >= _text.Length)
                {
                    Failed = true;
                    return new Quantity(0, "");
                }

                char c = _text[_pos];
                if (c == '(')
                {
                    _pos++;
                    Quantity inner = ParseSum();
                    SkipSpace();
                    if (_pos < _text.Length && _text[_pos] == ')') _pos++;
                    else Failed = true;
                    return inner;
                }

                if (c == '+' || c == '-')
                {
                    int save = _pos;
                    _pos++;
                    SkipSpace();
                    if (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
                    {
                        _pos = save;
                        return ParseNumber();
                    }

                    Quantity operand = ParseUnary();
                    return c == '-' ? new Quantity(-operand.Value, operand.Unit) : operand;
                }

                if (char.IsLetter(c))
                {
                    int start = _pos;
                    while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '-')) _pos++;
                    string name = _text.Substring(start, _pos - start).ToLowerInvariant();
                    SkipSpace();
                    if (_pos < _text.Length && _text[_pos] == '(' && (name == "calc" || name == "min" || name == "max" || name == "clamp"))
                    {
                        _pos++;
                        var args = new List<Quantity>();
                        while (!Failed)
                        {
                            args.Add(ParseSum());
                            SkipSpace();
                            if (_pos < _text.Length && _text[_pos] == ',')
                            {
                                _pos++;
                                continue;
                            }

                            break;
                        }

                        SkipSpace();
                        if (_pos < _text.Length && _text[_pos] == ')') _pos++;
                        else Failed = true;
                        if (Failed) return new Quantity(0, "");
                        return Apply(name, args);
                    }

                    Failed = true;
                    return new Quantity(0, "");
                }

                return ParseNumber();
            }

            private Quantity Apply(string name, List<Quantity> args)
            {
                if (name == "calc")
                {
                    if (args.Count != 1) Failed = true;
                    return args.Count > 0 ? args[0] : new Quantity(0, "");
                }

                string unit = args.Count > 0 ? args[0].Unit : "";
                foreach (Quantity q in args)
                {
                    if (q.Unit != unit)
                    {
                        Failed = true;
                        return new Quantity(0, "");
                    }
                }

                if (name == "min")
                {
                    double best = args[0].Value;
                    foreach (Quantity q in args) best = Math.Min(best, q.Value);
                    return new Quantity(best, unit);
                }

                if (name == "max")
                {
                    double best = args[0].Value;
                    foreach (Quantity q in args) best = Math.Max(best, q.Value);
                    return new Quantity(best, unit);
                }

                if (args.Count != 3)
                {
                    Failed = true;
                    return new Quantity(0, "");
                }

                return new Quantity(Math.Max(args[0].Value, Math.Min(args[1].Value, args[2].Value)), unit);
            }

            private Quantity ParseNumber()
            {
                SkipSpace();
                int start = _pos;
                if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) _pos++;
                while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.')) _pos++;
                if (_pos == start || (_pos == start + 1 && !char.IsDigit(_text[start])))
                {
                    Failed = true;
                    return new Quantity(0, "");
                }

                double value;
                if (!double.TryParse(_text.Substring(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    Failed = true;
                    return new Quantity(0, "");
                }

                int unitStart = _pos;
                if (_pos < _text.Length && _text[_pos] == '%') _pos++;
                else
                {
                    while (_pos < _text.Length && char.IsLetter(_text[_pos])) _pos++;
                }

                return new Quantity(value, _text.Substring(unitStart, _pos - unitStart).ToLowerInvariant());
            }
        }

        public static bool ContainsMath(string text)
        {
            return FunctionStart.IsMatch(text);
        }

        public static string Fold(string text, out bool folded, out bool failed)
        {
            folded = false;
            failed = false;
            var sb = new StringBuilder();
            int pos = 0;
            while (pos < text.Length)
            {
                Match m = FunctionStart.Match(text, pos);
                if (!m.Success)
                {
                    sb.Append(text, pos, text.Length - pos);
                    break;
                }

                sb.Append(text, pos, m.Index - pos);
                int end = MatchingParen(text, m.Index + m.Length - 1);
                if (end < 0)
                {
                    failed = true;
                    sb.Append(text, m.Index, text.Length - m.Index);
                    break;
                }

                string expression = text.Substring(m.Index, end - m.Index + 1);
                var parser = new Parser(expression);
                Quantity result = parser.ParseSum();
                if (parser.Failed || !parser.AtEnd)
                {
                    failed = true;
                    sb.Append(expression);
                }
                else
                {
                    folded = true;
                    sb.Append(Format(result));
                }

                pos = end + 1;
            }

            return sb.ToString();
        }

        private static string Format(Quantity q)
        {
            if (q.Unit == "") return CssText.FormatNumber(q.Value);
            return CssText.FormatLength(q.Value, q.Unit);
        }

        private static int MatchingParen(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }
    }
}
