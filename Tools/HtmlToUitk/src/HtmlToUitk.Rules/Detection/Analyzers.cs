using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlToUitk.Core.Css;

namespace HtmlToUitk.Rules
{
    public static class CalcAnalyzer
    {
        public static bool CanFold(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return false;
            var parser = new Parser(expression);
            try
            {
                Quantity q = parser.ParseExpression();
                return parser.AtEnd && q.Ok;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string UnitClass(string unit)
        {
            switch (unit)
            {
                case "":
                case "%":
                case "deg":
                case "s":
                case "ms":
                    return unit;
                default:
                    return "px";
            }
        }

        private struct Quantity
        {
            public double Value;
            public string Unit;
            public bool Ok;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _p;

            public Parser(string s)
            {
                _s = s;
            }

            public bool AtEnd
            {
                get
                {
                    SkipSpace();
                    return _p >= _s.Length;
                }
            }

            private void SkipSpace()
            {
                while (_p < _s.Length && char.IsWhiteSpace(_s[_p])) _p++;
            }

            public Quantity ParseExpression()
            {
                SkipSpace();
                int nameStart = _p;
                while (_p < _s.Length && (char.IsLetter(_s[_p]) || _s[_p] == '-')) _p++;
                string name = _s.Substring(nameStart, _p - nameStart).ToLowerInvariant();
                SkipSpace();
                if (_p >= _s.Length || _s[_p] != '(') throw new FormatException("expected function");
                _p++;
                Quantity result = ParseFunctionBody(name);
                SkipSpace();
                if (_p >= _s.Length || _s[_p] != ')') throw new FormatException("expected )");
                _p++;
                return result;
            }

            private Quantity ParseFunctionBody(string name)
            {
                if (name == "calc" || name == "") return ParseSum();
                if (name != "min" && name != "max" && name != "clamp") throw new FormatException("unsupported function " + name);
                var args = new List<Quantity>();
                while (true)
                {
                    args.Add(ParseSum());
                    SkipSpace();
                    if (_p < _s.Length && _s[_p] == ',') { _p++; continue; }
                    break;
                }
                if (name == "clamp" && args.Count != 3) throw new FormatException("clamp takes three arguments");
                if (args.Count < 1) throw new FormatException("no arguments");
                string unit = args[0].Unit;
                double value = args[0].Value;
                foreach (Quantity q in args)
                {
                    if (q.Unit != unit) return new Quantity { Ok = false };
                }
                if (name == "min") foreach (Quantity q in args) value = Math.Min(value, q.Value);
                else if (name == "max") foreach (Quantity q in args) value = Math.Max(value, q.Value);
                else value = Math.Max(args[0].Value, Math.Min(args[1].Value, args[2].Value));
                return new Quantity { Value = value, Unit = unit, Ok = true };
            }

            private Quantity ParseSum()
            {
                Quantity left = ParseProduct();
                while (true)
                {
                    SkipSpace();
                    if (_p < _s.Length && (_s[_p] == '+' || _s[_p] == '-'))
                    {
                        char op = _s[_p++];
                        Quantity right = ParseProduct();
                        if (!left.Ok || !right.Ok || left.Unit != right.Unit) return new Quantity { Ok = false };
                        left = new Quantity { Value = op == '+' ? left.Value + right.Value : left.Value - right.Value, Unit = left.Unit, Ok = true };
                    }
                    else break;
                }
                return left;
            }

            private Quantity ParseProduct()
            {
                Quantity left = ParseTerm();
                while (true)
                {
                    SkipSpace();
                    if (_p < _s.Length && (_s[_p] == '*' || _s[_p] == '/'))
                    {
                        char op = _s[_p++];
                        Quantity right = ParseTerm();
                        if (!left.Ok || !right.Ok) return new Quantity { Ok = false };
                        if (op == '*')
                        {
                            if (left.Unit.Length > 0 && right.Unit.Length > 0) return new Quantity { Ok = false };
                            left = new Quantity { Value = left.Value * right.Value, Unit = left.Unit.Length > 0 ? left.Unit : right.Unit, Ok = true };
                        }
                        else
                        {
                            if (right.Unit.Length > 0 || right.Value == 0) return new Quantity { Ok = false };
                            left = new Quantity { Value = left.Value / right.Value, Unit = left.Unit, Ok = true };
                        }
                    }
                    else break;
                }
                return left;
            }

            private Quantity ParseTerm()
            {
                SkipSpace();
                if (_p >= _s.Length) throw new FormatException("unexpected end");
                char c = _s[_p];
                if (c == '(')
                {
                    _p++;
                    Quantity inner = ParseSum();
                    SkipSpace();
                    if (_p >= _s.Length || _s[_p] != ')') throw new FormatException("expected )");
                    _p++;
                    return inner;
                }
                if (char.IsLetter(c) || c == '-' && _p + 1 < _s.Length && char.IsLetter(_s[_p + 1]))
                {
                    int save = _p;
                    int start = _p;
                    while (_p < _s.Length && (char.IsLetter(_s[_p]) || _s[_p] == '-')) _p++;
                    string name = _s.Substring(start, _p - start).ToLowerInvariant();
                    if (_p < _s.Length && _s[_p] == '(')
                    {
                        if (name == "var" || name == "env" || name == "attr") throw new FormatException("non-constant");
                        _p = save;
                        return ParseExpression();
                    }
                    throw new FormatException("identifier in expression");
                }
                int numStart = _p;
                if (_s[_p] == '+' || _s[_p] == '-') _p++;
                while (_p < _s.Length && (char.IsDigit(_s[_p]) || _s[_p] == '.')) _p++;
                if (_p == numStart || (_p == numStart + 1 && (_s[numStart] == '+' || _s[numStart] == '-'))) throw new FormatException("number expected");
                string number = _s.Substring(numStart, _p - numStart);
                double value;
                if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) throw new FormatException("bad number");
                int unitStart = _p;
                if (_p < _s.Length && _s[_p] == '%') _p++;
                else while (_p < _s.Length && char.IsLetter(_s[_p])) _p++;
                return new Quantity { Value = value, Unit = UnitClass(_s.Substring(unitStart, _p - unitStart).ToLowerInvariant()), Ok = true };
            }
        }
    }

    public static class GradientSubset
    {
        private static readonly HashSet<string> Sides = new HashSet<string>(StringComparer.Ordinal) { "top", "bottom", "left", "right" };

        public static bool IsSupported(CssFunction function)
        {
            if (function == null || function.Args.Count < 2) return false;
            int first = 0;
            CssValue head = function.Args[0];
            if (IsDirection(head)) first = 1;
            if (function.Args.Count - first < 2) return false;
            for (int i = first; i < function.Args.Count; i++)
            {
                if (!IsStop(function.Args[i])) return false;
            }
            return true;
        }

        private static bool IsDirection(CssValue value)
        {
            double n;
            string unit;
            if (CssValues.TryDimension(value, out n, out unit)) return unit == "deg";
            IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
            if (tokens.Count < 2 || CssValues.Keyword(tokens[0]) != "to") return false;
            for (int i = 1; i < tokens.Count; i++)
            {
                string k = CssValues.Keyword(tokens[i]);
                if (k == null || !Sides.Contains(k)) return false;
            }
            return true;
        }

        private static bool IsStop(CssValue value)
        {
            IReadOnlyList<CssValue> tokens = CssValues.SpaceTokens(value);
            if (tokens.Count < 1 || tokens.Count > 2) return false;
            if (!IsColor(tokens[0])) return false;
            if (tokens.Count == 1) return true;
            double n;
            string unit;
            return CssValues.TryDimension(tokens[1], out n, out unit) && (unit == "%" || unit == "px");
        }

        private static bool IsColor(CssValue value)
        {
            if (value is CssColor) return true;
            string fn = CssValues.FunctionName(value);
            if (fn == "rgb" || fn == "rgba") return true;
            string k = CssValues.Keyword(value);
            return k != null && k != "to" && k != "in" && k != "currentcolor" && k != "inherit";
        }
    }
}
