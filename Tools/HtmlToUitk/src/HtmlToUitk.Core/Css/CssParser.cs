using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Css
{
    public sealed class CssParser : ICssParser
    {
        public static readonly CssParser Instance = new CssParser();

        public Stylesheet Parse(string text, string path, StyleOrigin origin, IDiagnosticSink sink)
        {
            return Parse(text, path, origin, sink, default(SourcePos));
        }

        public Stylesheet Parse(string text, string path, StyleOrigin origin, IDiagnosticSink sink, SourcePos basePosition)
        {
            text = text ?? "";
            if (text.Length > 0 && text[0] == (char)0xFEFF && basePosition.Line == 0)
            {
                text = text.Substring(1);
                basePosition = new SourcePos(1, 1, 1);
            }
            var run = new Run(text, path, sink, new SourceMap(text, path, basePosition));
            run.ParseBlockContents(0, text.Length, new List<string>(), false);
            return new Stylesheet
            {
                Path = path,
                Origin = origin,
                Rules = run.Rules,
                AtRules = run.AtRules,
                Imports = run.Imports
            };
        }

        private sealed class Run
        {
            private static readonly HashSet<string> ConditionalGroups = new HashSet<string>(StringComparer.Ordinal) { "media", "supports", "layer", "container", "document", "scope", "starting-style" };

            private readonly string _t;
            private readonly string _path;
            private readonly IDiagnosticSink _sink;
            private readonly SourceMap _map;

            public readonly List<StyleRule> Rules = new List<StyleRule>();
            public readonly List<AtRule> AtRules = new List<AtRule>();
            public readonly List<ImportRef> Imports = new List<ImportRef>();

            public Run(string text, string path, IDiagnosticSink sink, SourceMap map)
            {
                _t = text;
                _path = path;
                _sink = sink;
                _map = map;
            }

            private void Report(string ruleId, int start, int end, string construct, string message, string suggestion)
            {
                if (_sink == null) return;
                string shown = construct == null ? null : CssChars.CollapseWhitespace(construct);
                if (shown != null && shown.Length > 80) shown = shown.Substring(0, 80);
                _sink.Report(new Diagnostic
                {
                    RuleId = ruleId,
                    Severity = Severity.Error,
                    Tier = Tier.Reject,
                    Span = _map.Span(start, Math.Max(start, end)),
                    Construct = shown,
                    Message = message,
                    Why = ruleId == "C028" ? "USS has no nested rules." : "The stylesheet cannot be parsed reliably.",
                    Suggestion = suggestion
                });
            }

            private int SkipTrivia(int pos, int end)
            {
                while (pos < end)
                {
                    char c = _t[pos];
                    if (CssChars.IsWhitespace(c)) pos++;
                    else if (c == '/' && pos + 1 < end && _t[pos + 1] == '*')
                    {
                        int close = _t.IndexOf("*/", pos + 2, end - pos - 2, StringComparison.Ordinal);
                        pos = close < 0 ? end : close + 2;
                    }
                    else if (c == '<' && pos + 3 < end && string.CompareOrdinal(_t, pos, "<!--", 0, 4) == 0) pos += 4;
                    else if (c == '-' && pos + 2 < end && string.CompareOrdinal(_t, pos, "-->", 0, 3) == 0) pos += 3;
                    else break;
                }
                return pos;
            }

            private int SkipString(int pos, int end)
            {
                char quote = _t[pos];
                pos++;
                while (pos < end)
                {
                    char c = _t[pos];
                    if (c == '\\') pos += 2;
                    else if (c == quote) return pos + 1;
                    else if (c == '\n') return pos;
                    else pos++;
                }
                return end;
            }

            private int FindStop(int pos, int end, bool stopAtBrace, bool stopAtSemicolon, bool stopAtCloseBrace)
            {
                int depth = 0;
                while (pos < end)
                {
                    char c = _t[pos];
                    if (c == '"' || c == '\'')
                    {
                        pos = SkipString(pos, end);
                        continue;
                    }
                    if (c == '\\')
                    {
                        pos += 2;
                        continue;
                    }
                    if (c == '/' && pos + 1 < end && _t[pos + 1] == '*')
                    {
                        int close = _t.IndexOf("*/", pos + 2, end - pos - 2, StringComparison.Ordinal);
                        pos = close < 0 ? end : close + 2;
                        continue;
                    }
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth = Math.Max(0, depth - 1);
                    else if (depth == 0)
                    {
                        if (c == '{' && stopAtBrace) return pos;
                        if (c == ';' && stopAtSemicolon) return pos;
                        if (c == '}' && stopAtCloseBrace) return pos;
                    }
                    pos++;
                }
                return end;
            }

            private int FindBlockEnd(int open, int end)
            {
                int depth = 0;
                int pos = open;
                while (pos < end)
                {
                    char c = _t[pos];
                    if (c == '"' || c == '\'')
                    {
                        pos = SkipString(pos, end);
                        continue;
                    }
                    if (c == '\\')
                    {
                        pos += 2;
                        continue;
                    }
                    if (c == '/' && pos + 1 < end && _t[pos + 1] == '*')
                    {
                        int close = _t.IndexOf("*/", pos + 2, end - pos - 2, StringComparison.Ordinal);
                        pos = close < 0 ? end : close + 2;
                        continue;
                    }
                    if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0) return pos;
                    }
                    pos++;
                }
                return -1;
            }

            public void ParseBlockContents(int start, int end, List<string> atContext, bool inKeyframes)
            {
                int pos = start;
                while (true)
                {
                    pos = SkipTrivia(pos, end);
                    if (pos >= end) return;
                    char c = _t[pos];
                    if (c == '}')
                    {
                        Report("C029", pos, pos + 1, "}", "CSS syntax error (unmatched '}')", "Remove the stray closing brace.");
                        pos++;
                        continue;
                    }
                    if (c == '@' && !inKeyframes)
                    {
                        pos = ParseAtRule(pos, end, atContext);
                        continue;
                    }
                    pos = ParseQualifiedRule(pos, end, atContext, inKeyframes);
                }
            }

            private int ParseQualifiedRule(int pos, int end, List<string> atContext, bool inKeyframes)
            {
                int preludeStart = pos;
                int stop = FindStop(pos, end, true, true, false);
                if (stop >= end || _t[stop] == ';')
                {
                    Report("C029", preludeStart, Math.Min(end, stop), _t.Substring(preludeStart, Math.Min(end, stop) - preludeStart), "CSS syntax error (selector without a declaration block)", "Add a { ... } block or remove the text.");
                    return Math.Min(end, stop + 1);
                }
                int close = FindBlockEnd(stop, end);
                int bodyStart = stop + 1;
                int bodyEnd;
                int next;
                if (close < 0)
                {
                    Report("C029", stop, stop + 1, "{", "CSS syntax error (unclosed block)", "Add the missing closing brace.");
                    bodyEnd = end;
                    next = end;
                }
                else
                {
                    bodyEnd = close;
                    next = close + 1;
                }
                int preludeEnd = stop;
                while (preludeEnd > preludeStart && CssChars.IsWhitespace(_t[preludeEnd - 1])) preludeEnd--;
                List<Selector> selectors;
                if (inKeyframes) selectors = ParseKeyframeSelectors(preludeStart, preludeEnd);
                else selectors = SelectorParser.ParseList(_t, preludeStart, preludeEnd, _map, _sink, false);
                var declarations = new List<Declaration>();
                ParseDeclarations(bodyStart, bodyEnd, declarations);
                if (selectors.Count == 0 && !inKeyframes) return next;
                var rule = new StyleRule
                {
                    Selectors = selectors,
                    Declarations = declarations,
                    AtContext = atContext.Count == 0 ? (IReadOnlyList<string>)Array.Empty<string>() : atContext.ToArray(),
                    Span = _map.Span(preludeStart, next),
                    Order = Rules.Count
                };
                if (inKeyframes) KeyframeRules.Add(rule);
                else Rules.Add(rule);
                return next;
            }

            private List<StyleRule> KeyframeRules = new List<StyleRule>();

            private List<Selector> ParseKeyframeSelectors(int start, int end)
            {
                var result = new List<Selector>();
                int pieceStart = start;
                for (int i = start; i <= end; i++)
                {
                    if (i == end || _t[i] == ',')
                    {
                        string text = CssChars.CollapseWhitespace(_t.Substring(pieceStart, i - pieceStart));
                        if (text.Length > 0)
                        {
                            result.Add(new Selector { Text = text.ToLowerInvariant(), Span = _map.Span(pieceStart, i) });
                        }
                        pieceStart = i + 1;
                    }
                }
                return result;
            }

            private void ParseDeclarations(int start, int end, List<Declaration> output)
            {
                int pos = start;
                while (true)
                {
                    pos = SkipTrivia(pos, end);
                    while (pos < end && _t[pos] == ';') pos = SkipTrivia(pos + 1, end);
                    if (pos >= end) return;
                    int itemStart = pos;
                    int stop = FindStop(pos, end, true, true, false);
                    if (stop < end && _t[stop] == '{')
                    {
                        int close = FindBlockEnd(stop, end);
                        int blockEnd = close < 0 ? end : close + 1;
                        Report("C028", itemStart, stop, _t.Substring(itemStart, stop - itemStart), "Nested CSS rule is not supported by USS", "Write the nested rule as a separate top-level rule.");
                        pos = blockEnd;
                        continue;
                    }
                    int itemEnd = stop;
                    pos = stop < end ? stop + 1 : end;
                    ParseDeclaration(itemStart, itemEnd, output);
                }
            }

            private void ParseDeclaration(int start, int end, List<Declaration> output)
            {
                int colon = -1;
                int depth = 0;
                for (int i = start; i < end; i++)
                {
                    char c = _t[i];
                    if (c == '"' || c == '\'')
                    {
                        i = SkipString(i, end) - 1;
                        continue;
                    }
                    if (c == '/' && i + 1 < end && _t[i + 1] == '*')
                    {
                        int close = _t.IndexOf("*/", i + 2, end - i - 2, StringComparison.Ordinal);
                        i = close < 0 ? end : close + 1;
                        continue;
                    }
                    if (c == '(' || c == '[') depth++;
                    else if (c == ')' || c == ']') depth = Math.Max(0, depth - 1);
                    else if (c == ':' && depth == 0)
                    {
                        colon = i;
                        break;
                    }
                }
                if (colon < 0)
                {
                    Report("C029", start, end, _t.Substring(start, end - start), "CSS syntax error (declaration without ':')", "Write the declaration as 'property: value;'.");
                    return;
                }
                string name = StripComments(_t.Substring(start, colon - start)).Trim();
                if (!IsValidPropertyName(name))
                {
                    Report("C029", start, colon, _t.Substring(start, end - start), "CSS syntax error (invalid property name '" + name + "')", "Use a valid property name.");
                    return;
                }
                if (!name.StartsWith("--", StringComparison.Ordinal)) name = name.ToLowerInvariant();
                int valueStart = colon + 1;
                int valueEnd = end;
                while (valueStart < valueEnd && CssChars.IsWhitespace(_t[valueStart])) valueStart++;
                while (valueEnd > valueStart && CssChars.IsWhitespace(_t[valueEnd - 1])) valueEnd--;
                bool important = false;
                string valueText = StripComments(_t.Substring(valueStart, valueEnd - valueStart)).Trim();
                int bang = valueText.LastIndexOf('!');
                if (bang >= 0)
                {
                    string tail = valueText.Substring(bang + 1).Trim();
                    if (string.Equals(tail, "important", StringComparison.OrdinalIgnoreCase))
                    {
                        important = true;
                        valueText = valueText.Substring(0, bang).TrimEnd();
                    }
                }
                if (valueText.Length == 0 && !name.StartsWith("--", StringComparison.Ordinal))
                {
                    Report("C029", start, end, _t.Substring(start, end - start), "CSS syntax error (empty value for '" + name + "')", "Give the property a value or remove it.");
                    return;
                }
                SourceSpan valueSpan = _map.Span(valueStart, valueEnd);
                CssValue value = name.StartsWith("--", StringComparison.Ordinal) && valueText.Length == 0 ? new CssRaw("") : CssValueParser.Parse(valueText, valueSpan);
                value.Span = valueSpan;
                output.Add(new Declaration
                {
                    Name = name,
                    Value = value,
                    Important = important,
                    Span = _map.Span(start, valueEnd)
                });
            }

            private static bool IsValidPropertyName(string name)
            {
                if (name.Length == 0) return false;
                if (name.StartsWith("--", StringComparison.Ordinal)) return name.Length > 2 && name.IndexOfAny(new[] { ' ', '\t', '\n', '\r', '{', '}', ';', ':' }) < 0;
                int pos = 0;
                if (!CssChars.StartsIdent(name, 0)) return false;
                string ident = CssChars.ReadIdent(name, ref pos);
                return pos == name.Length && ident.Length > 0;
            }

            private static string StripComments(string text)
            {
                if (text.IndexOf("/*", StringComparison.Ordinal) < 0) return text;
                var sb = new System.Text.StringBuilder(text.Length);
                int i = 0;
                while (i < text.Length)
                {
                    if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
                    {
                        int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                        i = close < 0 ? text.Length : close + 2;
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append(text[i]);
                        i++;
                    }
                }
                return sb.ToString();
            }

            private int ParseAtRule(int pos, int end, List<string> atContext)
            {
                int atStart = pos;
                pos++;
                int nameStart = pos;
                string name = CssChars.StartsIdent(_t, pos) ? CssChars.ReadIdent(_t, ref pos) : "";
                name = name.ToLowerInvariant();
                string canonical = StripVendor(name);
                int preludeStart = pos;
                int stop = FindStop(pos, end, true, true, true);
                int preludeEnd = stop;
                string prelude = CssChars.CollapseWhitespace(StripComments(_t.Substring(preludeStart, preludeEnd - preludeStart))).Trim();
                if (stop >= end || _t[stop] == ';' || _t[stop] == '}')
                {
                    int next = stop < end && _t[stop] == ';' ? stop + 1 : stop;
                    SourceSpan span = _map.Span(atStart, Math.Min(end, next));
                    if (canonical == "import")
                    {
                        string target = ExtractImportTarget(prelude);
                        if (target != null) Imports.Add(new ImportRef { Target = target, Span = span });
                        else Report("C029", atStart, next, "@import " + prelude, "CSS syntax error (@import without a target)", "Use @import url(\"file.css\");");
                    }
                    else if (canonical != "charset")
                    {
                        AtRules.Add(new AtRule { Name = canonical, Prelude = prelude, Span = span });
                    }
                    return next;
                }
                int close = FindBlockEnd(stop, end);
                int bodyStart = stop + 1;
                int bodyEnd = close < 0 ? end : close;
                int after = close < 0 ? end : close + 1;
                if (close < 0) Report("C029", stop, stop + 1, "{", "CSS syntax error (unclosed @" + canonical + " block)", "Add the missing closing brace.");
                var at = new AtRule { Name = canonical, Prelude = prelude, Span = _map.Span(atStart, after) };
                if (ConditionalGroups.Contains(canonical))
                {
                    var context = new List<string>(atContext) { "@" + canonical + (prelude.Length > 0 ? " " + prelude : "") };
                    int before = Rules.Count;
                    ParseBlockContents(bodyStart, bodyEnd, context, false);
                    at.Rules = Rules.GetRange(before, Rules.Count - before);
                }
                else if (canonical == "keyframes")
                {
                    var saved = KeyframeRules;
                    KeyframeRules = new List<StyleRule>();
                    var context = new List<string> { "@keyframes " + prelude };
                    ParseBlockContents(bodyStart, bodyEnd, context, true);
                    at.Rules = KeyframeRules;
                    KeyframeRules = saved;
                    for (int i = 0; i < at.Rules.Count; i++) ((StyleRule)at.Rules[i]).Order = i;
                }
                else
                {
                    var declarations = new List<Declaration>();
                    ParseDeclarations(bodyStart, bodyEnd, declarations);
                    at.Declarations = declarations;
                }
                AtRules.Add(at);
                return after;
            }

            private static string StripVendor(string name)
            {
                foreach (string prefix in new[] { "-webkit-", "-moz-", "-ms-", "-o-" })
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal)) return name.Substring(prefix.Length);
                }
                return name;
            }

            private static string ExtractImportTarget(string prelude)
            {
                if (prelude.Length == 0) return null;
                CssValue value = CssValueParser.Parse(prelude);
                var list = value as CssList;
                CssValue first = list != null && list.Items.Count > 0 ? list.Items[0] : value;
                var url = first as CssUrl;
                if (url != null) return url.Url;
                var str = first as CssString;
                if (str != null) return str.Value;
                return null;
            }
        }
    }
}
