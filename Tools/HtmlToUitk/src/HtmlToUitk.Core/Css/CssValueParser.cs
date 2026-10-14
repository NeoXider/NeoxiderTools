using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HtmlToUitk.Core.Css
{
    public static class CssValueParser
    {
        public static CssValue Parse(string text) => Parse(text, default(SourceSpan));

        public static CssValue Parse(string text, SourceSpan span)
        {
            var parser = new Parser(text ?? "");
            CssValue value = parser.ParseTop();
            value.Span = span;
            return value;
        }

        public static bool HasVarRef(CssValue value)
        {
            if (value == null) return false;
            if (value is CssVarRef) return true;
            var function = value as CssFunction;
            if (function != null)
            {
                foreach (CssValue arg in function.Args) if (HasVarRef(arg)) return true;
                return false;
            }
            var list = value as CssList;
            if (list != null)
            {
                foreach (CssValue item in list.Items) if (HasVarRef(item)) return true;
            }
            return false;
        }

        public static void CollectVarRefs(CssValue value, ICollection<CssVarRef> output)
        {
            if (value == null) return;
            var reference = value as CssVarRef;
            if (reference != null)
            {
                output.Add(reference);
                CollectVarRefs(reference.Fallback, output);
                return;
            }
            var function = value as CssFunction;
            if (function != null)
            {
                foreach (CssValue arg in function.Args) CollectVarRefs(arg, output);
                return;
            }
            var list = value as CssList;
            if (list != null)
            {
                foreach (CssValue item in list.Items) CollectVarRefs(item, output);
            }
        }

        private sealed class Parser
        {
            private const int MaxDepth = 48;
            private readonly string _t;
            private int _p;
            private int _depth;

            public Parser(string text)
            {
                _t = text;
            }

            public CssValue ParseTop()
            {
                List<CssValue> items = ParseCommaItems(false);
                if (items.Count == 0) return new CssRaw("");
                if (items.Count == 1) return items[0];
                return new CssList(items, ',');
            }

            private void SkipSpace()
            {
                while (_p < _t.Length)
                {
                    char c = _t[_p];
                    if (CssChars.IsWhitespace(c)) _p++;
                    else if (c == '/' && _p + 1 < _t.Length && _t[_p + 1] == '*')
                    {
                        int end = _t.IndexOf("*/", _p + 2, StringComparison.Ordinal);
                        _p = end < 0 ? _t.Length : end + 2;
                    }
                    else break;
                }
            }

            private List<CssValue> ParseCommaItems(bool inFunction)
            {
                var items = new List<CssValue>();
                while (true)
                {
                    SkipSpace();
                    if (_p >= _t.Length) break;
                    if (_t[_p] == ')')
                    {
                        if (inFunction) break;
                        _p++;
                        continue;
                    }
                    CssValue item = ParseSpaceList(inFunction);
                    if (item != null) items.Add(item);
                    SkipSpace();
                    if (_p < _t.Length && _t[_p] == ',')
                    {
                        _p++;
                        SkipSpace();
                        if (_p >= _t.Length || _t[_p] == ',' || _t[_p] == ')') items.Add(new CssRaw(""));
                        continue;
                    }
                    if (_p < _t.Length && _t[_p] == ')' && inFunction) break;
                    if (_p >= _t.Length) break;
                }
                return items;
            }

            private CssValue ParseSpaceList(bool inFunction)
            {
                var terms = new List<CssValue>();
                while (true)
                {
                    SkipSpace();
                    if (_p >= _t.Length) break;
                    char c = _t[_p];
                    if (c == ',' || c == ')') break;
                    CssValue term = ParseTerm();
                    if (term == null) break;
                    terms.Add(term);
                }
                if (terms.Count == 0) return null;
                if (terms.Count == 1) return terms[0];
                return new CssList(terms, ' ');
            }

            private CssValue ParseTerm()
            {
                char c = _t[_p];
                if (c == '"' || c == '\'') return new CssString(ReadString());
                if (c == '#')
                {
                    int start = _p;
                    _p++;
                    while (_p < _t.Length && CssChars.IsNameChar(_t[_p])) _p++;
                    string text = _t.Substring(start, _p - start);
                    CssColor color;
                    if (ColorParser.TryParseHex(text, out color)) return color;
                    return new CssRaw(text);
                }
                if (StartsNumber()) return ParseNumeric();
                if (CssChars.StartsIdent(_t, _p))
                {
                    int start = _p;
                    string name = CssChars.ReadIdent(_t, ref _p);
                    if (_p == start)
                    {
                        _p++;
                        return new CssRaw(_t.Substring(start, 1));
                    }
                    if (_p < _t.Length && _t[_p] == '(')
                    {
                        _p++;
                        return ParseFunction(name, start);
                    }
                    return new CssIdent(name);
                }
                switch (c)
                {
                    case '(':
                        _p++;
                        return ParseFunction("", _p - 1);
                    case '/':
                    case '*':
                        _p++;
                        return new CssIdent(c.ToString());
                    case '+':
                    case '-':
                        _p++;
                        return new CssIdent(c.ToString());
                    case '!':
                    {
                        _p++;
                        SkipSpace();
                        int start = _p;
                        while (_p < _t.Length && CssChars.IsNameChar(_t[_p])) _p++;
                        return new CssIdent("!" + _t.Substring(start, _p - start));
                    }
                    case '[':
                    {
                        int end = _t.IndexOf(']', _p);
                        end = end < 0 ? _t.Length : end + 1;
                        string text = _t.Substring(_p, end - _p);
                        _p = end;
                        return new CssRaw(text);
                    }
                    default:
                        _p++;
                        return new CssRaw(c.ToString());
                }
            }

            private bool StartsNumber()
            {
                char c = _t[_p];
                if (CssChars.IsDigit(c)) return true;
                if (c == '.') return _p + 1 < _t.Length && CssChars.IsDigit(_t[_p + 1]);
                if (c == '+' || c == '-')
                {
                    if (_p + 1 >= _t.Length) return false;
                    char d = _t[_p + 1];
                    if (CssChars.IsDigit(d)) return true;
                    return d == '.' && _p + 2 < _t.Length && CssChars.IsDigit(_t[_p + 2]);
                }
                return false;
            }

            private CssValue ParseNumeric()
            {
                int start = _p;
                if (_t[_p] == '+' || _t[_p] == '-') _p++;
                while (_p < _t.Length && CssChars.IsDigit(_t[_p])) _p++;
                if (_p < _t.Length && _t[_p] == '.' && _p + 1 < _t.Length && CssChars.IsDigit(_t[_p + 1]))
                {
                    _p++;
                    while (_p < _t.Length && CssChars.IsDigit(_t[_p])) _p++;
                }
                if (_p < _t.Length && (_t[_p] == 'e' || _t[_p] == 'E'))
                {
                    int q = _p + 1;
                    if (q < _t.Length && (_t[q] == '+' || _t[q] == '-')) q++;
                    if (q < _t.Length && CssChars.IsDigit(_t[q]))
                    {
                        while (q < _t.Length && CssChars.IsDigit(_t[q])) q++;
                        _p = q;
                    }
                }
                string numberText = _t.Substring(start, _p - start);
                double value;
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsInfinity(value) || double.IsNaN(value))
                {
                    return new CssRaw(numberText);
                }
                if (_p < _t.Length && _t[_p] == '%')
                {
                    _p++;
                    return new CssPercent(value);
                }
                if (_p < _t.Length && CssChars.StartsIdent(_t, _p) && _t[_p] != '-')
                {
                    string unit = CssChars.ReadIdent(_t, ref _p);
                    return new CssDimension(value, unit.ToLowerInvariant());
                }
                return new CssNumber(value);
            }

            private string ReadString()
            {
                char quote = _t[_p];
                _p++;
                var sb = new StringBuilder();
                while (_p < _t.Length)
                {
                    char c = _t[_p];
                    if (c == quote)
                    {
                        _p++;
                        return sb.ToString();
                    }
                    if (c == '\\')
                    {
                        _p++;
                        if (_p >= _t.Length) break;
                        if (_t[_p] == '\n')
                        {
                            _p++;
                            continue;
                        }
                        if (_t[_p] == '\r')
                        {
                            _p++;
                            if (_p < _t.Length && _t[_p] == '\n') _p++;
                            continue;
                        }
                        CssChars.ReadEscape(_t, ref _p, sb);
                        continue;
                    }
                    sb.Append(c);
                    _p++;
                }
                return sb.ToString();
            }

            private CssValue ParseFunction(string name, int start)
            {
                _depth++;
                try
                {
                    if (_depth > MaxDepth)
                    {
                        string rest = _t.Substring(start);
                        _p = _t.Length;
                        return new CssRaw(rest);
                    }
                    string lower = name.ToLowerInvariant();
                    if (lower == "url") return ParseUrl();
                    if (lower == "var") return ParseVar();
                    List<CssValue> args = ParseCommaItems(true);
                    SkipSpace();
                    if (_p < _t.Length && _t[_p] == ')') _p++;
                    var function = new CssFunction(lower == "" ? "" : name, args);
                    if (lower == "rgb" || lower == "rgba")
                    {
                        CssColor color;
                        string authored = null;
                        if (ColorParser.TryFromFunction(function, authored, out color)) return color;
                    }
                    return function;
                }
                finally
                {
                    _depth--;
                }
            }

            private CssValue ParseUrl()
            {
                SkipSpace();
                if (_p < _t.Length && (_t[_p] == '"' || _t[_p] == '\''))
                {
                    string url = ReadString();
                    SkipSpace();
                    if (_p < _t.Length && _t[_p] == ')') _p++;
                    return new CssUrl(url);
                }
                int start = _p;
                while (_p < _t.Length && _t[_p] != ')') _p++;
                string raw = _t.Substring(start, _p - start).Trim();
                if (_p < _t.Length) _p++;
                return new CssUrl(raw);
            }

            private CssValue ParseVar()
            {
                SkipSpace();
                int start = _p;
                string name;
                if (CssChars.StartsIdent(_t, _p)) name = CssChars.ReadIdent(_t, ref _p);
                else
                {
                    while (_p < _t.Length && _t[_p] != ',' && _t[_p] != ')') _p++;
                    name = _t.Substring(start, _p - start).Trim();
                }
                SkipSpace();
                CssValue fallback = null;
                if (_p < _t.Length && _t[_p] == ',')
                {
                    _p++;
                    List<CssValue> items = ParseCommaItems(true);
                    if (items.Count == 0) fallback = new CssRaw("");
                    else if (items.Count == 1) fallback = items[0];
                    else fallback = new CssList(items, ',');
                }
                SkipSpace();
                if (_p < _t.Length && _t[_p] == ')') _p++;
                return new CssVarRef(name, fallback);
            }
        }
    }
}
