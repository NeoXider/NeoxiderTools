using System;
using System.Collections.Generic;
using System.Text;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Core.Html
{
    public sealed class HtmlParser : IHtmlParser
    {
        public static readonly IReadOnlyCollection<string> VoidTags = new[]
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr", "param", "keygen"
        };

        public HtmlDocument Parse(string text, string path, IDiagnosticSink sink)
        {
            var run = new Run(text ?? "", path, sink);
            return run.Execute();
        }

        private sealed class Run
        {
            private static readonly HashSet<string> Void = new HashSet<string>(VoidTags, StringComparer.Ordinal);
            private static readonly HashSet<string> ParagraphClosers = new HashSet<string>(StringComparer.Ordinal)
            {
                "address", "article", "aside", "blockquote", "center", "details", "dialog", "dir", "div", "dl", "fieldset", "figcaption", "figure",
                "footer", "header", "hgroup", "hr", "main", "menu", "nav", "ol", "p", "pre", "section", "summary", "table", "ul", "h1", "h2", "h3",
                "h4", "h5", "h6", "form", "li", "dd", "dt", "listing", "xmp"
            };
            private static readonly HashSet<string> OptionalEnd = new HashSet<string>(StringComparer.Ordinal)
            {
                "p", "li", "dt", "dd", "tr", "td", "th", "thead", "tbody", "tfoot", "option", "optgroup", "colgroup", "caption", "html", "head", "body"
            };
            private static readonly HashSet<string> Headings = new HashSet<string>(StringComparer.Ordinal) { "h1", "h2", "h3", "h4", "h5", "h6" };

            private readonly string _t;
            private readonly string _path;
            private readonly IDiagnosticSink _sink;
            private readonly SourceMap _map;
            private int _p;

            private readonly DomElement _body;
            private readonly List<DomElement> _stack = new List<DomElement>();
            private readonly List<StyleBlock> _styleBlocks = new List<StyleBlock>();
            private readonly List<LinkRef> _links = new List<LinkRef>();
            private readonly List<DomElement> _metas = new List<DomElement>();
            private readonly List<DomElement> _scripts = new List<DomElement>();
            private readonly List<DomElement> _templates = new List<DomElement>();
            private readonly List<StyleSourceRef> _styleOrder = new List<StyleSourceRef>();
            private readonly List<DomAttribute> _htmlAttributes = new List<DomAttribute>();
            private readonly List<DomAttribute> _bodyAttributes = new List<DomAttribute>();
            private const int MaxDepth = 512;
            private readonly Dictionary<DomElement, List<DomNode>> _children = new Dictionary<DomElement, List<DomNode>>();
            private string _title;
            private bool _bodyStarted;

            public Run(string text, string path, IDiagnosticSink sink)
            {
                _t = text;
                _path = path;
                _sink = sink;
                _map = new SourceMap(text, path);
                _body = new DomElement { Tag = "body", Span = _map.Span(0, text.Length) };
                _children[_body] = new List<DomNode>();
            }

            private void Report(string ruleId, Severity severity, int start, int end, string construct, string message, string suggestion)
            {
                if (_sink == null) return;
                string shown = construct;
                if (shown != null && shown.Length > 80) shown = shown.Substring(0, 80);
                _sink.Report(new Diagnostic
                {
                    RuleId = ruleId,
                    Severity = severity,
                    Tier = Tier.Native,
                    Span = _map.Span(start, end),
                    Construct = shown,
                    Message = message,
                    Why = "The HTML parser had to guess how to recover; browsers and the converter may disagree.",
                    Suggestion = suggestion
                });
            }

            private DomElement Current => _stack.Count == 0 ? _body : _stack[_stack.Count - 1];

            public HtmlDocument Execute()
            {
                while (_p < _t.Length)
                {
                    if (_t[_p] != '<')
                    {
                        ReadText();
                        continue;
                    }
                    char next = _p + 1 < _t.Length ? _t[_p + 1] : '\0';
                    if (next == '!')
                    {
                        SkipMarkupDeclaration();
                    }
                    else if (next == '?')
                    {
                        SkipBogus();
                    }
                    else if (next == '/')
                    {
                        char letter = _p + 2 < _t.Length ? _t[_p + 2] : '\0';
                        if (IsAsciiLetter(letter)) ReadEndTag();
                        else if (letter == '>') _p += 3;
                        else if (_p + 2 >= _t.Length)
                        {
                            AddText(_p, _t.Length);
                            _p = _t.Length;
                        }
                        else SkipBogus();
                    }
                    else if (IsAsciiLetter(next)) ReadStartTag();
                    else
                    {
                        int start = _p;
                        _p++;
                        int stop = _t.IndexOf('<', _p);
                        if (stop < 0) stop = _t.Length;
                        AddText(start, stop);
                        _p = stop;
                    }
                }
                while (_stack.Count > 0) CloseTop(_t.Length, true, null);
                return Finish();
            }

            private HtmlDocument Finish()
            {
                Seal(_body);
                foreach (DomElement template in _templates) Seal(template);
                _body.Attributes = _bodyAttributes;
                return new HtmlDocument
                {
                    Path = _path,
                    Body = _body,
                    HtmlAttributes = _htmlAttributes,
                    Title = _title,
                    StyleBlocks = _styleBlocks,
                    Links = _links,
                    Metas = _metas,
                    Scripts = _scripts,
                    Templates = _templates,
                    StyleOrder = _styleOrder
                };
            }

            private void Seal(DomElement element)
            {
                List<DomNode> list;
                if (_children.TryGetValue(element, out list))
                {
                    element.Children = list;
                    foreach (DomNode node in list)
                    {
                        var child = node as DomElement;
                        if (child != null) Seal(child);
                    }
                }
            }

            private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

            private static bool IsSpace(char c) => c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f';

            private void ReadText()
            {
                int start = _p;
                int stop = _t.IndexOf('<', _p);
                while (stop >= 0)
                {
                    char next = stop + 1 < _t.Length ? _t[stop + 1] : '\0';
                    if (next == '!' || next == '?' || next == '/' || IsAsciiLetter(next)) break;
                    stop = _t.IndexOf('<', stop + 1);
                }
                if (stop < 0) stop = _t.Length;
                AddText(start, stop);
                _p = stop;
            }

            private bool IsBlank(int start, int end)
            {
                for (int i = start; i < end; i++) if (!IsSpace(_t[i])) return false;
                return true;
            }

            private void AddText(int start, int end)
            {
                if (end <= start) return;
                if (!_bodyStarted)
                {
                    if (IsBlank(start, end)) return;
                    _bodyStarted = true;
                }
                string decoded = HtmlEntities.Decode(_t.Substring(start, end - start), false);
                AppendText(Current, decoded, start, end);
            }

            private void AppendText(DomElement parent, string text, int start, int end)
            {
                List<DomNode> list = ChildrenOf(parent);
                if (list.Count > 0)
                {
                    var last = list[list.Count - 1] as DomText;
                    if (last != null)
                    {
                        last.Text += text;
                        last.Span = new SourceSpan(_path, last.Span.Start, _map.Pos(end));
                        return;
                    }
                }
                list.Add(new DomText { Text = text, Span = _map.Span(start, end), Parent = parent });
            }

            private List<DomNode> ChildrenOf(DomElement element)
            {
                List<DomNode> list;
                if (!_children.TryGetValue(element, out list))
                {
                    list = new List<DomNode>();
                    _children[element] = list;
                }
                return list;
            }

            private void SkipMarkupDeclaration()
            {
                if (string.CompareOrdinal(_t, _p, "<!--", 0, 4) == 0)
                {
                    int close = _t.IndexOf("-->", _p + 4, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        Report("H003", Severity.Warning, _p, _t.Length, "<!--", "Unterminated comment", "Close the comment with -->.");
                        _p = _t.Length;
                    }
                    else _p = close + 3;
                    return;
                }
                SkipBogus();
            }

            private void SkipBogus()
            {
                int close = _t.IndexOf('>', _p);
                _p = close < 0 ? _t.Length : close + 1;
            }

            private void ReadStartTag()
            {
                int tagStart = _p;
                int i = _p + 1;
                int nameStart = i;
                while (i < _t.Length && !IsSpace(_t[i]) && _t[i] != '/' && _t[i] != '>') i++;
                string name = _t.Substring(nameStart, i - nameStart).ToLowerInvariant();
                var attributes = new List<DomAttribute>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                bool selfClosing = false;
                bool closed = false;
                while (i < _t.Length)
                {
                    while (i < _t.Length && IsSpace(_t[i])) i++;
                    if (i >= _t.Length) break;
                    char c = _t[i];
                    if (c == '>')
                    {
                        i++;
                        closed = true;
                        break;
                    }
                    if (c == '/')
                    {
                        if (i + 1 < _t.Length && _t[i + 1] == '>')
                        {
                            selfClosing = true;
                            i += 2;
                            closed = true;
                            break;
                        }
                        i++;
                        continue;
                    }
                    int attrStart = i;
                    i++;
                    while (i < _t.Length && !IsSpace(_t[i]) && _t[i] != '=' && _t[i] != '>' && !(_t[i] == '/' && i + 1 < _t.Length && _t[i + 1] == '>')) i++;
                    string attrName = _t.Substring(attrStart, i - attrStart).ToLowerInvariant();
                    int afterName = i;
                    while (i < _t.Length && IsSpace(_t[i])) i++;
                    string value = "";
                    int attrEnd = afterName;
                    if (i < _t.Length && _t[i] == '=')
                    {
                        i++;
                        while (i < _t.Length && IsSpace(_t[i])) i++;
                        if (i < _t.Length && (_t[i] == '"' || _t[i] == '\''))
                        {
                            char quote = _t[i];
                            int valueStart = i + 1;
                            int end = _t.IndexOf(quote, valueStart);
                            if (end < 0)
                            {
                                Report("H003", Severity.Warning, attrStart, _t.Length, attrName, "Unterminated attribute value", "Close the attribute value with " + quote + ".");
                                value = HtmlEntities.Decode(_t.Substring(valueStart), true);
                                i = _t.Length;
                                attrEnd = _t.Length;
                            }
                            else
                            {
                                value = HtmlEntities.Decode(_t.Substring(valueStart, end - valueStart), true);
                                i = end + 1;
                                attrEnd = i;
                            }
                        }
                        else
                        {
                            int valueStart = i;
                            while (i < _t.Length && !IsSpace(_t[i]) && _t[i] != '>') i++;
                            value = HtmlEntities.Decode(_t.Substring(valueStart, i - valueStart), true);
                            attrEnd = i;
                        }
                    }
                    else i = afterName;
                    if (attrName.Length == 0) continue;
                    if (seen.Add(attrName))
                    {
                        attributes.Add(new DomAttribute { Name = attrName, Value = value, Span = _map.Span(attrStart, attrEnd) });
                    }
                }
                if (!closed)
                {
                    Report("H003", Severity.Warning, tagStart, _t.Length, "<" + name, "Unterminated start tag", "Close the tag with >.");
                    _p = _t.Length;
                    return;
                }
                _p = i;
                HandleStartTag(name, attributes, selfClosing, tagStart, i);
            }

            private void HandleStartTag(string name, List<DomAttribute> attributes, bool selfClosing, int tagStart, int tagEnd)
            {
                switch (name)
                {
                    case "html":
                        MergeAttributes(_htmlAttributes, attributes);
                        return;
                    case "head":
                        return;
                    case "body":
                        MergeAttributes(_bodyAttributes, attributes);
                        _bodyStarted = true;
                        return;
                    case "base":
                        return;
                    case "meta":
                    {
                        var meta = new DomElement { Tag = "meta", Attributes = attributes, Span = _map.Span(tagStart, tagEnd) };
                        _metas.Add(meta);
                        return;
                    }
                    case "link":
                    {
                        string rel = null;
                        string href = null;
                        foreach (DomAttribute a in attributes)
                        {
                            if (a.Name == "rel") rel = a.Value.Trim().ToLowerInvariant();
                            else if (a.Name == "href") href = a.Value;
                        }
                        _links.Add(new LinkRef { Rel = rel, Href = href, Attributes = attributes, Span = _map.Span(tagStart, tagEnd) });
                        if (rel != null && Array.IndexOf(rel.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), "stylesheet") >= 0)
                        {
                            _styleOrder.Add(new StyleSourceRef { Kind = StyleSourceKind.Link, Index = _links.Count - 1, Span = _map.Span(tagStart, tagEnd) });
                        }
                        return;
                    }
                    case "title":
                    {
                        string raw = ReadRawText("title", out int rawStart, out int rawEnd);
                        string decoded = HtmlEntities.Decode(raw, false);
                        if (_title == null) _title = CollapseSpaces(decoded);
                        return;
                    }
                    case "style":
                    {
                        string raw = ReadRawText("style", out int rawStart, out int rawEnd);
                        _styleBlocks.Add(new StyleBlock { Text = raw, Attributes = attributes, Span = _map.Span(rawStart, rawEnd) });
                        _styleOrder.Add(new StyleSourceRef { Kind = StyleSourceKind.Style, Index = _styleBlocks.Count - 1, Span = _map.Span(tagStart, rawEnd) });
                        return;
                    }
                    case "script":
                    {
                        string raw = ReadRawText("script", out int rawStart, out int rawEnd);
                        var script = new DomElement { Tag = "script", Attributes = attributes, Span = _map.Span(tagStart, rawEnd) };
                        if (raw.Length > 0) script.Children = new DomNode[] { new DomText { Text = raw, Span = _map.Span(rawStart, rawEnd), Parent = script } };
                        _scripts.Add(script);
                        return;
                    }
                }
                _bodyStarted = true;
                CloseImplied(name);
                var element = new DomElement { Tag = name, Attributes = attributes, Span = _map.Span(tagStart, tagEnd) };
                bool isTemplate = name == "template";
                DomElement parent = Current;
                element.Parent = parent;
                if (!isTemplate) ChildrenOf(parent).Add(element);
                bool isVoid = Void.Contains(name);
                if (isVoid)
                {
                    return;
                }
                if (_stack.Count >= MaxDepth)
                {
                    Report("H005", Severity.Error, tagStart, tagEnd, "<" + name + ">", "Elements are nested deeper than " + MaxDepth, "Flatten the markup.");
                    return;
                }
                if (selfClosing)
                {
                    Report("H004", Severity.Warning, tagStart, tagEnd, "<" + name + " />", "The self-closing syntax is ignored on non-void HTML elements", "Write <" + name + "></" + name + "> explicitly.");
                }
                if (name == "textarea")
                {
                    if (_p < _t.Length && _t[_p] == '\r') _p++;
                    if (_p < _t.Length && _t[_p] == '\n') _p++;
                    string raw = ReadRawText("textarea", out int rawStart, out int rawEnd);
                    if (raw.Length > 0) AppendText(element, HtmlEntities.Decode(raw, false), rawStart, rawEnd);
                    element.Span = _map.Span(tagStart, _p);
                    return;
                }
                if (name == "pre" || name == "listing")
                {
                    if (_p < _t.Length && _t[_p] == '\r') _p++;
                    if (_p < _t.Length && _t[_p] == '\n') _p++;
                }
                _stack.Add(element);
                if (isTemplate) _templates.Add(element);
            }

            private static string CollapseSpaces(string text)
            {
                var sb = new StringBuilder();
                bool pending = false;
                foreach (char c in text)
                {
                    if (IsSpace(c))
                    {
                        pending = sb.Length > 0;
                        continue;
                    }
                    if (pending)
                    {
                        sb.Append(' ');
                        pending = false;
                    }
                    sb.Append(c);
                }
                return sb.ToString();
            }

            private static void MergeAttributes(List<DomAttribute> target, List<DomAttribute> source)
            {
                foreach (DomAttribute a in source)
                {
                    bool exists = false;
                    foreach (DomAttribute t in target)
                    {
                        if (t.Name == a.Name)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) target.Add(a);
                }
            }

            private string ReadRawText(string tag, out int rawStart, out int rawEnd)
            {
                rawStart = _p;
                int search = _p;
                while (true)
                {
                    int close = _t.IndexOf("</", search, StringComparison.Ordinal);
                    if (close < 0)
                    {
                        Report("H003", Severity.Warning, _p, _t.Length, "<" + tag, "Unterminated <" + tag + "> element", "Add </" + tag + ">.");
                        rawEnd = _t.Length;
                        string rest = _t.Substring(rawStart);
                        _p = _t.Length;
                        return rest;
                    }
                    if (string.Compare(_t, close + 2, tag, 0, tag.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        int after = close + 2 + tag.Length;
                        if (after >= _t.Length || IsSpace(_t[after]) || _t[after] == '>' || _t[after] == '/')
                        {
                            rawEnd = close;
                            int gt = _t.IndexOf('>', after);
                            _p = gt < 0 ? _t.Length : gt + 1;
                            return _t.Substring(rawStart, rawEnd - rawStart);
                        }
                    }
                    search = close + 2;
                }
            }

            private void ReadEndTag()
            {
                int tagStart = _p;
                int i = _p + 2;
                int nameStart = i;
                while (i < _t.Length && !IsSpace(_t[i]) && _t[i] != '>' && _t[i] != '/') i++;
                string name = _t.Substring(nameStart, i - nameStart).ToLowerInvariant();
                int gt = _t.IndexOf('>', i);
                int tagEnd = gt < 0 ? _t.Length : gt + 1;
                if (gt < 0) Report("H003", Severity.Warning, tagStart, _t.Length, "</" + name, "Unterminated end tag", "Close the tag with >.");
                _p = tagEnd;
                switch (name)
                {
                    case "html":
                    case "head":
                    case "body":
                    case "title":
                    case "style":
                    case "script":
                        return;
                    case "br":
                        HandleStartTag("br", new List<DomAttribute>(), false, tagStart, tagEnd);
                        return;
                }
                for (int index = _stack.Count - 1; index >= 0; index--)
                {
                    if (_stack[index].Tag == name)
                    {
                        while (_stack.Count - 1 > index) CloseTop(tagStart, true, name);
                        CloseTop(tagEnd, false, null);
                        return;
                    }
                }
                Report("H001", Severity.Warning, tagStart, tagEnd, "</" + name + ">", "End tag without a matching start tag", "Remove </" + name + "> or add the missing <" + name + ">.");
            }

            private void CloseTop(int endOffset, bool implicitClose, string closedBy)
            {
                DomElement element = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                element.Span = new SourceSpan(_path, element.Span.Start, _map.Pos(endOffset));
                if (implicitClose && !OptionalEnd.Contains(element.Tag))
                {
                    Report("H002", Severity.Warning, element.Span.Start.Offset, Math.Min(_t.Length, element.Span.Start.Offset + element.Tag.Length + 1), "<" + element.Tag + ">",
                        "<" + element.Tag + "> is not closed" + (closedBy != null ? " before </" + closedBy + ">" : ""), "Add </" + element.Tag + ">.");
                }
            }

            private int FindOpen(Func<string, bool> match, Func<string, bool> boundary)
            {
                for (int i = _stack.Count - 1; i >= 0; i--)
                {
                    string tag = _stack[i].Tag;
                    if (match(tag)) return i;
                    if (boundary(tag)) return -1;
                }
                return -1;
            }

            private void PopTo(int index, int offset)
            {
                while (_stack.Count > index) CloseTop(offset, true, null);
            }

            private void CloseImplied(string name)
            {
                if (ParagraphClosers.Contains(name))
                {
                    int p = FindOpen(t => t == "p", t => t == "table" || t == "td" || t == "th" || t == "template" || t == "button" || t == "caption" || t == "object");
                    if (p >= 0) PopTo(p, ImpliedOffset());
                }
                switch (name)
                {
                    case "li":
                    {
                        int li = FindOpen(t => t == "li", t => t == "ul" || t == "ol" || t == "menu" || t == "table" || t == "template");
                        if (li >= 0) PopTo(li, ImpliedOffset());
                        break;
                    }
                    case "dd":
                    case "dt":
                    {
                        int d = FindOpen(t => t == "dd" || t == "dt", t => t == "dl" || t == "table" || t == "template");
                        if (d >= 0) PopTo(d, ImpliedOffset());
                        break;
                    }
                    case "option":
                        if (Current.Tag == "option") PopTo(_stack.Count - 1, ImpliedOffset());
                        break;
                    case "optgroup":
                        if (Current.Tag == "option") PopTo(_stack.Count - 1, ImpliedOffset());
                        if (Current.Tag == "optgroup") PopTo(_stack.Count - 1, ImpliedOffset());
                        break;
                    case "tr":
                    {
                        int tr = FindOpen(t => t == "tr", t => t == "table" || t == "template");
                        if (tr >= 0) PopTo(tr, ImpliedOffset());
                        break;
                    }
                    case "td":
                    case "th":
                    {
                        int cell = FindOpen(t => t == "td" || t == "th", t => t == "tr" || t == "table" || t == "template");
                        if (cell >= 0) PopTo(cell, ImpliedOffset());
                        break;
                    }
                    case "thead":
                    case "tbody":
                    case "tfoot":
                    {
                        int section = FindOpen(t => t == "thead" || t == "tbody" || t == "tfoot", t => t == "table" || t == "template");
                        if (section >= 0) PopTo(section, ImpliedOffset());
                        break;
                    }
                    case "button":
                    {
                        int button = FindOpen(t => t == "button", t => t == "template");
                        if (button >= 0) PopTo(button, ImpliedOffset());
                        break;
                    }
                }
                if (Headings.Contains(name) && Headings.Contains(Current.Tag)) PopTo(_stack.Count - 1, ImpliedOffset());
            }

            private int ImpliedOffset()
            {
                int lt = _t.LastIndexOf('<', Math.Max(0, _p - 1));
                return lt < 0 ? _p : lt;
            }
        }
    }
}
