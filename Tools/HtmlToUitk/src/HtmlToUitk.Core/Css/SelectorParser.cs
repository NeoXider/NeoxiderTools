using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Css
{
    public static class SelectorParser
    {
        private static readonly HashSet<string> LegacyPseudoElements = new HashSet<string>(StringComparer.Ordinal) { "before", "after", "first-line", "first-letter" };
        private static readonly HashSet<string> SelectorListPseudos = new HashSet<string>(StringComparer.Ordinal) { "not", "is", "where", "has", "matches", "any" };
        private static readonly HashSet<string> NthPseudos = new HashSet<string>(StringComparer.Ordinal) { "nth-child", "nth-last-child", "nth-of-type", "nth-last-of-type" };

        public static IReadOnlyList<Selector> ParseList(string text, IDiagnosticSink sink)
        {
            text = text ?? "";
            return ParseList(text, 0, text.Length, new SourceMap(text, null), sink, false);
        }

        public static Selector Parse(string text, IDiagnosticSink sink = null)
        {
            IReadOnlyList<Selector> list = ParseList(text, sink);
            return list.Count == 0 ? null : list[0];
        }

        public static string Format(Selector selector)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < selector.Parts.Count; i++)
            {
                (char combinator, SimpleSelector part) = selector.Parts[i];
                if (i == 0)
                {
                    if (combinator == '>' || combinator == '+' || combinator == '~') sb.Append(combinator).Append(' ');
                }
                else if (combinator == ' ' || combinator == (char)0) sb.Append(' ');
                else sb.Append(' ').Append(combinator).Append(' ');
                sb.Append(FormatCompound(part));
            }
            return sb.ToString();
        }

        public static string FormatCompound(SimpleSelector part)
        {
            var sb = new StringBuilder();
            if (part.Type != null) sb.Append(part.Type);
            else if (part.Universal && part.Id == null && part.Classes.Count == 0 && part.Attributes.Count == 0 && part.Pseudos.Count == 0 && part.PseudoElements.Count == 0) sb.Append('*');
            if (part.Id != null) sb.Append('#').Append(CssChars.Escape(part.Id));
            foreach (string cls in part.Classes) sb.Append('.').Append(CssChars.Escape(cls));
            foreach (AttributeSelector attr in part.Attributes)
            {
                sb.Append('[').Append(attr.Name);
                if (attr.Operator != null)
                {
                    sb.Append(attr.Operator).Append(CssFormat.Quote(attr.Value ?? ""));
                    if (attr.CaseInsensitive) sb.Append(" i");
                }
                sb.Append(']');
            }
            foreach (PseudoClass pseudo in part.Pseudos) sb.Append(FormatPseudo(pseudo));
            foreach (string element in part.PseudoElements) sb.Append("::").Append(element);
            return sb.ToString();
        }

        private static string FormatPseudo(PseudoClass pseudo)
        {
            if (pseudo.HasNth) return ":" + pseudo.Name + "(" + FormatNth(pseudo.NthA, pseudo.NthB) + ")";
            if (pseudo.Selectors.Count > 0)
            {
                var parts = new List<string>();
                foreach (Selector inner in pseudo.Selectors) parts.Add(Format(inner));
                return ":" + pseudo.Name + "(" + string.Join(", ", parts) + ")";
            }
            if (pseudo.Argument != null) return ":" + pseudo.Name + "(" + pseudo.Argument + ")";
            return ":" + pseudo.Name;
        }

        public static string FormatNth(int a, int b)
        {
            if (a == 0) return b.ToString(CultureInfo.InvariantCulture);
            if (a == 2 && b == 1) return "odd";
            if (a == 2 && b == 0) return "even";
            string head = a == 1 ? "n" : a == -1 ? "-n" : a.ToString(CultureInfo.InvariantCulture) + "n";
            if (b == 0) return head;
            return head + (b > 0 ? "+" : "-") + Math.Abs(b).ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryParseNth(string text, out int a, out int b)
        {
            a = 0;
            b = 0;
            if (text == null) return false;
            int of = text.IndexOf(" of ", StringComparison.OrdinalIgnoreCase);
            if (of >= 0) text = text.Substring(0, of);
            var sb = new StringBuilder();
            foreach (char c in text) if (!CssChars.IsWhitespace(c)) sb.Append(char.ToLowerInvariant(c));
            string s = sb.ToString();
            if (s.Length == 0) return false;
            if (s == "odd")
            {
                a = 2;
                b = 1;
                return true;
            }
            if (s == "even")
            {
                a = 2;
                b = 0;
                return true;
            }
            int n = s.IndexOf('n');
            if (n < 0)
            {
                int only;
                if (!int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out only)) return false;
                b = only;
                return true;
            }
            string aPart = s.Substring(0, n);
            if (aPart == "" || aPart == "+") a = 1;
            else if (aPart == "-") a = -1;
            else if (!int.TryParse(aPart, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out a)) return false;
            string rest = s.Substring(n + 1);
            if (rest.Length == 0) return true;
            if (rest[0] != '+' && rest[0] != '-') return false;
            return int.TryParse(rest, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out b);
        }

        internal static List<Selector> ParseList(string text, int start, int end, SourceMap map, IDiagnosticSink sink, bool relative)
        {
            var result = new List<Selector>();
            int pieceStart = start;
            int depth = 0;
            char quote = '\0';
            for (int i = start; i <= end; i++)
            {
                bool atEnd = i == end;
                char c = atEnd ? ',' : text[i];
                if (!atEnd)
                {
                    if (quote != '\0')
                    {
                        if (c == '\\') i++;
                        else if (c == quote) quote = '\0';
                        continue;
                    }
                    if (c == '"' || c == '\'')
                    {
                        quote = c;
                        continue;
                    }
                    if (c == '\\')
                    {
                        i++;
                        continue;
                    }
                    if (c == '/' && i + 1 < end && text[i + 1] == '*')
                    {
                        int close = text.IndexOf("*/", i + 2, end - i - 2, StringComparison.Ordinal);
                        i = close < 0 ? end - 1 : close + 1;
                        continue;
                    }
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth = Math.Max(0, depth - 1);
                }
                if (c == ',' && (depth == 0 || atEnd))
                {
                    Selector selector = ParseOne(text, pieceStart, i, map, sink, relative);
                    if (selector != null) result.Add(selector);
                    pieceStart = i + 1;
                }
            }
            return result;
        }

        private static Selector ParseOne(string text, int start, int end, SourceMap map, IDiagnosticSink sink, bool relative)
        {
            while (start < end && CssChars.IsWhitespace(text[start])) start++;
            while (end > start && CssChars.IsWhitespace(text[end - 1])) end--;
            var parser = new OneParser(text, start, end, relative);
            Selector selector = parser.Parse();
            if (selector == null)
            {
                if (sink != null && parser.Error != null)
                {
                    sink.Report(new Diagnostic
                    {
                        RuleId = "C029",
                        Severity = Severity.Error,
                        Tier = Tier.Reject,
                        Span = map != null ? map.Span(Math.Max(start, parser.ErrorAt), Math.Min(end, Math.Max(start, parser.ErrorAt) + 1)) : default(SourceSpan),
                        Construct = text.Substring(start, end - start),
                        Message = "CSS syntax error (selector: " + parser.Error + ")",
                        Why = "The selector cannot be parsed.",
                        Suggestion = "Fix the selector syntax."
                    });
                }
                return null;
            }
            selector.Span = map != null ? map.Span(start, end) : default(SourceSpan);
            return selector;
        }

        private sealed class OneParser
        {
            private readonly string _t;
            private readonly int _start;
            private readonly int _end;
            private readonly bool _relative;
            private int _p;

            public string Error;
            public int ErrorAt;

            public OneParser(string text, int start, int end, bool relative)
            {
                _t = text;
                _start = start;
                _end = end;
                _relative = relative;
                _p = start;
            }

            private bool Fail(string message)
            {
                if (Error == null)
                {
                    Error = message;
                    ErrorAt = _p;
                }
                return false;
            }

            private bool SkipWs()
            {
                int before = _p;
                while (_p < _end)
                {
                    if (CssChars.IsWhitespace(_t[_p])) _p++;
                    else if (_t[_p] == '/' && _p + 1 < _end && _t[_p + 1] == '*')
                    {
                        int close = _t.IndexOf("*/", _p + 2, _end - _p - 2, StringComparison.Ordinal);
                        _p = close < 0 ? _end : close + 2;
                    }
                    else break;
                }
                return _p > before;
            }

            public Selector Parse()
            {
                var parts = new List<(char, SimpleSelector)>();
                char pending = '\0';
                SkipWs();
                if (_p >= _end)
                {
                    Fail("empty selector");
                    return null;
                }
                while (true)
                {
                    SkipWs();
                    if (_p >= _end) break;
                    char c = _t[_p];
                    if (c == '>' || c == '+' || c == '~')
                    {
                        if (parts.Count == 0 && !_relative)
                        {
                            Fail("a selector cannot start with a combinator");
                            return null;
                        }
                        if (pending != '\0' && pending != ' ')
                        {
                            Fail("two combinators in a row");
                            return null;
                        }
                        if (c == '>' && _p + 2 < _end && _t[_p + 1] == '>' && _t[_p + 2] == '>')
                        {
                            Fail("the '>>>' combinator is not supported");
                            return null;
                        }
                        pending = c;
                        _p++;
                        continue;
                    }
                    if (c == '/')
                    {
                        Fail("unexpected '/'");
                        return null;
                    }
                    SimpleSelector compound = ParseCompound();
                    if (compound == null) return null;
                    char comb;
                    if (parts.Count == 0) comb = _relative ? (pending == '\0' ? ' ' : pending) : '\0';
                    else comb = pending == '\0' ? ' ' : pending;
                    parts.Add((comb, compound));
                    pending = '\0';
                }
                if (pending != '\0' && pending != ' ')
                {
                    Fail("a combinator needs a selector after it");
                    return null;
                }
                if (parts.Count == 0)
                {
                    Fail("empty selector");
                    return null;
                }
                var selector = new Selector { Parts = parts };
                selector.Specificity = ComputeSpecificity(parts);
                selector.Text = Format(selector);
                return selector;
            }

            private SimpleSelector ParseCompound()
            {
                var classes = new List<string>();
                var pseudos = new List<PseudoClass>();
                var attributes = new List<AttributeSelector>();
                var elements = new List<string>();
                string type = null;
                string id = null;
                bool universal = false;
                bool any = false;
                while (_p < _end)
                {
                    char c = _t[_p];
                    if (CssChars.IsWhitespace(c) || c == '>' || c == '+' || c == '~') break;
                    if (c == '/' && _p + 1 < _end && _t[_p + 1] == '*') break;
                    if (c == '*')
                    {
                        if (any)
                        {
                            Fail("'*' must start a compound selector");
                            return null;
                        }
                        universal = true;
                        _p++;
                    }
                    else if (CssChars.StartsIdent(_t, _p))
                    {
                        if (any)
                        {
                            Fail("a type selector must start a compound selector");
                            return null;
                        }
                        type = CssChars.ReadIdent(_t, ref _p);
                    }
                    else if (c == '#')
                    {
                        _p++;
                        string name = ReadName();
                        if (name.Length == 0)
                        {
                            Fail("an id name is expected after '#'");
                            return null;
                        }
                        id = name;
                    }
                    else if (c == '.')
                    {
                        _p++;
                        string name = ReadName();
                        if (name.Length == 0)
                        {
                            Fail("a class name is expected after '.'");
                            return null;
                        }
                        classes.Add(name);
                    }
                    else if (c == '[')
                    {
                        AttributeSelector attribute = ParseAttribute();
                        if (attribute == null) return null;
                        attributes.Add(attribute);
                    }
                    else if (c == ':')
                    {
                        if (!ParsePseudo(pseudos, elements)) return null;
                    }
                    else
                    {
                        Fail("unexpected character '" + c + "'");
                        return null;
                    }
                    any = true;
                }
                if (!any)
                {
                    Fail("empty compound selector");
                    return null;
                }
                return new SimpleSelector
                {
                    Type = type,
                    Id = id,
                    Classes = classes,
                    Pseudos = pseudos,
                    Attributes = attributes,
                    PseudoElements = elements,
                    Universal = universal
                };
            }

            private string ReadName()
            {
                var sb = new StringBuilder();
                while (_p < _end)
                {
                    char c = _t[_p];
                    if (CssChars.IsNameChar(c))
                    {
                        sb.Append(c);
                        _p++;
                    }
                    else if (c == '\\' && _p + 1 < _end)
                    {
                        _p++;
                        CssChars.ReadEscape(_t, ref _p, sb);
                    }
                    else break;
                }
                return sb.ToString();
            }

            private AttributeSelector ParseAttribute()
            {
                int open = _p;
                int depthEnd = -1;
                char quote = '\0';
                for (int i = _p + 1; i < _end; i++)
                {
                    char c = _t[i];
                    if (quote != '\0')
                    {
                        if (c == '\\') i++;
                        else if (c == quote) quote = '\0';
                        continue;
                    }
                    if (c == '"' || c == '\'') quote = c;
                    else if (c == ']')
                    {
                        depthEnd = i;
                        break;
                    }
                }
                if (depthEnd < 0)
                {
                    Fail("unterminated attribute selector");
                    return null;
                }
                int innerEnd = depthEnd;
                _p = open + 1;
                while (_p < innerEnd && CssChars.IsWhitespace(_t[_p])) _p++;
                int nameStart = _p;
                while (_p < innerEnd && !CssChars.IsWhitespace(_t[_p]) && "=~|^$*".IndexOf(_t[_p]) < 0) _p++;
                string name = _t.Substring(nameStart, _p - nameStart);
                if (name.Length == 0)
                {
                    Fail("attribute name expected");
                    return null;
                }
                int checkPos = 0;
                if (!CssChars.StartsIdent(name, 0) || CssChars.ReadIdent(name, ref checkPos).Length != name.Length || checkPos != name.Length)
                {
                    Fail("invalid attribute name '" + name + "'");
                    return null;
                }
                while (_p < innerEnd && CssChars.IsWhitespace(_t[_p])) _p++;
                var attribute = new AttributeSelector { Name = name };
                if (_p < innerEnd)
                {
                    string op;
                    if (_t[_p] == '=') op = "=";
                    else if (_p + 1 < innerEnd && _t[_p + 1] == '=' && "~|^$*".IndexOf(_t[_p]) >= 0) op = _t.Substring(_p, 2);
                    else
                    {
                        Fail("invalid attribute operator");
                        return null;
                    }
                    _p += op.Length;
                    attribute.Operator = op;
                    while (_p < innerEnd && CssChars.IsWhitespace(_t[_p])) _p++;
                    if (_p >= innerEnd)
                    {
                        Fail("attribute value expected");
                        return null;
                    }
                    string value;
                    if (_t[_p] == '"' || _t[_p] == '\'')
                    {
                        char q = _t[_p];
                        _p++;
                        var sb = new StringBuilder();
                        while (_p < innerEnd && _t[_p] != q)
                        {
                            if (_t[_p] == '\\' && _p + 1 < innerEnd)
                            {
                                _p++;
                                CssChars.ReadEscape(_t, ref _p, sb);
                            }
                            else
                            {
                                sb.Append(_t[_p]);
                                _p++;
                            }
                        }
                        if (_p < innerEnd) _p++;
                        value = sb.ToString();
                    }
                    else
                    {
                        int valueStart = _p;
                        while (_p < innerEnd && !CssChars.IsWhitespace(_t[_p])) _p++;
                        value = _t.Substring(valueStart, _p - valueStart);
                    }
                    attribute.Value = value;
                    while (_p < innerEnd && CssChars.IsWhitespace(_t[_p])) _p++;
                    if (_p < innerEnd)
                    {
                        string flag = _t.Substring(_p, innerEnd - _p).Trim().ToLowerInvariant();
                        if (flag == "i") attribute.CaseInsensitive = true;
                        else if (flag != "s")
                        {
                            Fail("invalid attribute selector flag");
                            return null;
                        }
                    }
                }
                _p = depthEnd + 1;
                return attribute;
            }

            private bool ParsePseudo(List<PseudoClass> pseudos, List<string> elements)
            {
                bool doubleColon = _p + 1 < _end && _t[_p + 1] == ':';
                _p += doubleColon ? 2 : 1;
                if (!CssChars.StartsIdent(_t, _p))
                {
                    Fail("pseudo name expected");
                    return false;
                }
                string name = CssChars.ReadIdent(_t, ref _p).ToLowerInvariant();
                string argument = null;
                if (_p < _end && _t[_p] == '(')
                {
                    int close = FindClose(_p);
                    if (close < 0)
                    {
                        Fail("unterminated pseudo-class argument");
                        return false;
                    }
                    argument = _t.Substring(_p + 1, close - _p - 1);
                    int argStart = _p + 1;
                    _p = close + 1;
                    if (doubleColon || LegacyPseudoElements.Contains(name))
                    {
                        elements.Add(name);
                        return true;
                    }
                    var pseudo = new PseudoClass { Name = name, Argument = argument.Trim() };
                    if (SelectorListPseudos.Contains(name))
                    {
                        var inner = new List<Selector>();
                        bool relative = name == "has";
                        var innerSink = new CollectingSink();
                        List<Selector> parsed = ParseList(_t, argStart, close, null, innerSink, relative);
                        if (innerSink.Failed || parsed.Count == 0)
                        {
                            Fail("invalid selector inside :" + name + "()");
                            return false;
                        }
                        pseudo.Selectors = parsed;
                    }
                    else if (NthPseudos.Contains(name))
                    {
                        int a, b;
                        if (!TryParseNth(argument, out a, out b))
                        {
                            Fail("invalid An+B expression in :" + name + "()");
                            return false;
                        }
                        pseudo.HasNth = true;
                        pseudo.NthA = a;
                        pseudo.NthB = b;
                    }
                    pseudos.Add(pseudo);
                    return true;
                }
                if (doubleColon || LegacyPseudoElements.Contains(name))
                {
                    elements.Add(name);
                    return true;
                }
                pseudos.Add(new PseudoClass { Name = name });
                return true;
            }

            private int FindClose(int open)
            {
                int depth = 0;
                char quote = '\0';
                for (int i = open; i < _end; i++)
                {
                    char c = _t[i];
                    if (quote != '\0')
                    {
                        if (c == '\\') i++;
                        else if (c == quote) quote = '\0';
                        continue;
                    }
                    if (c == '"' || c == '\'') quote = c;
                    else if (c == '(') depth++;
                    else if (c == ')')
                    {
                        depth--;
                        if (depth == 0) return i;
                    }
                }
                return -1;
            }
        }

        private sealed class CollectingSink : IDiagnosticSink
        {
            public bool Failed;
            public void Report(Diagnostic diagnostic) { Failed = true; }
        }

        private static Specificity ComputeSpecificity(IReadOnlyList<(char Combinator, SimpleSelector Part)> parts)
        {
            int ids = 0, classes = 0, types = 0;
            foreach ((char _, SimpleSelector part) in parts)
            {
                if (part.Id != null) ids++;
                classes += part.Classes.Count + part.Attributes.Count;
                if (part.Type != null) types++;
                types += part.PseudoElements.Count;
                foreach (PseudoClass pseudo in part.Pseudos)
                {
                    if (pseudo.Name == "where") continue;
                    if (pseudo.Selectors.Count > 0)
                    {
                        Specificity best = Specificity.Zero;
                        foreach (Selector inner in pseudo.Selectors)
                        {
                            if (inner.Specificity.CompareTo(best) > 0) best = inner.Specificity;
                        }
                        ids += best.Ids;
                        classes += best.Classes;
                        types += best.Types;
                    }
                    else classes++;
                }
            }
            return new Specificity(ids, classes, types);
        }
    }
}
