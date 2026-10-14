using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;

namespace HtmlToUitk.Core.Style
{
    public sealed class StyleEngine : IStyleEngine
    {
        public static readonly IReadOnlyCollection<string> DefaultInherited = new[]
        {
            "-unity-editor-text-rendering-mode", "-unity-font", "-unity-font-definition", "-unity-font-style", "-unity-material",
            "-unity-paragraph-spacing", "-unity-text-align", "-unity-text-auto-size", "-unity-text-generator", "-unity-text-outline-color",
            "-unity-text-outline-width", "color", "cursor", "font-size", "letter-spacing", "text-shadow", "visibility", "white-space", "word-spacing"
        };

        public IStyleModel Build(IReadOnlyList<Stylesheet> cascade, StyleOptions options, IDiagnosticSink sink)
        {
            return new StyleModel(cascade ?? Array.Empty<Stylesheet>(), options ?? new StyleOptions(), sink);
        }
    }

    internal sealed class ReferenceComparer : IEqualityComparer<IElement>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();
        public bool Equals(IElement x, IElement y) => ReferenceEquals(x, y);
        public int GetHashCode(IElement obj) => RuntimeHelpers.GetHashCode(obj);
    }

    public sealed class StyleModel : IStyleModel
    {
        private sealed class Entry
        {
            public int SheetIndex;
            public Stylesheet Sheet;
            public StyleRule Rule;
            public Selector Selector;
            public int RuleOrder;
            public bool PseudoElement;
        }

        private struct Priority : IComparable<Priority>
        {
            public bool Important;
            public Specificity Specificity;
            public int SheetIndex;
            public int RuleOrder;
            public int DeclIndex;

            public int CompareTo(Priority other)
            {
                int c = Important.CompareTo(other.Important);
                if (c != 0) return c;
                c = Specificity.CompareTo(other.Specificity);
                if (c != 0) return c;
                c = SheetIndex.CompareTo(other.SheetIndex);
                if (c != 0) return c;
                c = RuleOrder.CompareTo(other.RuleOrder);
                if (c != 0) return c;
                return DeclIndex.CompareTo(other.DeclIndex);
            }
        }

        private sealed class Candidate
        {
            public Priority Priority;
            public string Name;
            public CssValue Value;
            public Declaration Source;
            public Entry Entry;
        }

        private readonly IReadOnlyList<Stylesheet> _cascade;
        private readonly StyleOptions _options;
        private readonly IDiagnosticSink _sink;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly HashSet<string> _inherited;
        private readonly List<StyleConflict> _conflicts = new List<StyleConflict>();
        private readonly Dictionary<IElement, Dictionary<string, CssValue>> _variableCache = new Dictionary<IElement, Dictionary<string, CssValue>>(ReferenceComparer.Instance);
        private readonly Dictionary<IElement, Dictionary<PseudoStateSet, ResolvedStyle>> _styleCache = new Dictionary<IElement, Dictionary<PseudoStateSet, ResolvedStyle>>(ReferenceComparer.Instance);

        internal StyleModel(IReadOnlyList<Stylesheet> cascade, StyleOptions options, IDiagnosticSink sink)
        {
            _cascade = cascade;
            _options = options;
            _sink = sink;
            _inherited = new HashSet<string>(options.InheritedProperties ?? StyleEngine.DefaultInherited, StringComparer.Ordinal);
            int order = 0;
            for (int s = 0; s < cascade.Count; s++)
            {
                Stylesheet sheet = cascade[s];
                foreach (StyleRule rule in sheet.Rules)
                {
                    if (rule.AtContext.Count > 0) continue;
                    foreach (Selector selector in rule.Selectors)
                    {
                        if (selector.Parts.Count == 0) continue;
                        SimpleSelector subject = selector.Parts[selector.Parts.Count - 1].Part;
                        _entries.Add(new Entry
                        {
                            SheetIndex = s,
                            Sheet = sheet,
                            Rule = rule,
                            Selector = selector,
                            RuleOrder = order,
                            PseudoElement = subject.PseudoElements.Count > 0
                        });
                    }
                    order++;
                }
            }
            CheckVariables();
            if (options.DetectConflicts) DetectConflicts();
        }

        public IReadOnlyList<Stylesheet> Cascade => _cascade;

        public IReadOnlyList<StyleConflict> Conflicts => _conflicts;

        public IReadOnlyList<IElement> Match(Selector selector, IElement root)
        {
            var result = new List<IElement>();
            if (selector == null || root == null) return result;
            Walk(root, selector, result);
            return result;
        }

        private static void Walk(IElement element, Selector selector, List<IElement> result)
        {
            if (SelectorMatcher.Matches(selector, element)) result.Add(element);
            foreach (IElement child in element.ChildElements) Walk(child, selector, result);
        }

        public IReadOnlyDictionary<string, CssValue> Variables(IElement element)
        {
            return VariablesOf(element);
        }

        public ResolvedStyle Resolve(IElement element, PseudoStateSet states)
        {
            Dictionary<PseudoStateSet, ResolvedStyle> perState;
            if (!_styleCache.TryGetValue(element, out perState))
            {
                perState = new Dictionary<PseudoStateSet, ResolvedStyle>();
                _styleCache[element] = perState;
            }
            ResolvedStyle cached;
            if (perState.TryGetValue(states, out cached)) return cached;
            ResolvedStyle style = ResolveCore(element, states, null);
            perState[states] = style;
            return style;
        }

        public ResolvedStyle ResolvePseudoElement(IElement element, string pseudoElement, PseudoStateSet states)
        {
            return ResolveCore(element, states, pseudoElement);
        }

        public IReadOnlyList<(Stylesheet Sheet, StyleRule Rule, Selector Selector)> MatchingRules(IElement element, PseudoStateSet states)
        {
            var result = new List<(Stylesheet, StyleRule, Selector)>();
            foreach (Entry entry in _entries)
            {
                if (entry.PseudoElement) continue;
                if (SelectorMatcher.Matches(entry.Selector, element, states)) result.Add((entry.Sheet, entry.Rule, entry.Selector));
            }
            return result;
        }

        private ResolvedStyle ResolveCore(IElement element, PseudoStateSet states, string pseudoElement)
        {
            Dictionary<string, CssValue> variables = VariablesOf(element);
            var winners = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            foreach (Entry entry in _entries)
            {
                SimpleSelector subject = entry.Selector.Parts[entry.Selector.Parts.Count - 1].Part;
                if (pseudoElement == null)
                {
                    if (entry.PseudoElement) continue;
                }
                else if (subject.PseudoElements.Count != 1 || subject.PseudoElements[0] != pseudoElement) continue;
                if (!SelectorMatcher.Matches(entry.Selector, element, states)) continue;
                IReadOnlyList<Declaration> declarations = entry.Rule.Declarations;
                for (int d = 0; d < declarations.Count; d++)
                {
                    Declaration declaration = declarations[d];
                    if (declaration.Name.StartsWith("--", StringComparison.Ordinal)) continue;
                    CssValue value = declaration.Value;
                    if (CssValueParser.HasVarRef(value))
                    {
                        bool ok = true;
                        value = Substitute(value, name => LookupVariable(variables, name), 0, ref ok);
                        if (!ok) continue;
                    }
                    var priority = new Priority
                    {
                        Important = declaration.Important,
                        Specificity = entry.Selector.Specificity,
                        SheetIndex = entry.SheetIndex,
                        RuleOrder = entry.RuleOrder,
                        DeclIndex = d
                    };
                    var effective = new Declaration { Name = declaration.Name, Value = value, Important = declaration.Important, Span = declaration.Span };
                    IReadOnlyList<Declaration> longhands;
                    string problem;
                    if (ShorthandExpander.IsShorthand(declaration.Name) && ShorthandExpander.TryExpand(effective, ShorthandOptions.Cascade, out longhands, out problem))
                    {
                        foreach (Declaration longhand in longhands) Offer(winners, longhand.Name, longhand.Value, declaration, entry, priority);
                    }
                    else Offer(winners, declaration.Name, value, declaration, entry, priority);
                }
            }
            IElement parent = element.Parent;
            ResolvedStyle parentStyle = parent == null ? null : Resolve(parent, PseudoStateSet.None);
            var properties = new Dictionary<string, ResolvedValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Candidate> pair in winners)
            {
                Candidate candidate = pair.Value;
                var keyword = candidate.Value as CssIdent;
                string word = keyword == null ? null : keyword.Name.ToLowerInvariant();
                if (word == "initial") continue;
                bool inherit = word == "inherit" || (word == "unset" && _inherited.Contains(pair.Key));
                if (word == "unset" && !inherit) continue;
                if (inherit)
                {
                    ResolvedValue fromParent;
                    if (parentStyle != null && parentStyle.Properties.TryGetValue(pair.Key, out fromParent))
                    {
                        properties[pair.Key] = new ResolvedValue { Value = fromParent.Value, Source = fromParent.Source, Rule = fromParent.Rule, Sheet = fromParent.Sheet, Inherited = true };
                    }
                    continue;
                }
                properties[pair.Key] = new ResolvedValue { Value = candidate.Value, Source = candidate.Source, Rule = candidate.Entry.Rule, Sheet = candidate.Entry.Sheet };
            }
            if (parentStyle != null)
            {
                foreach (KeyValuePair<string, ResolvedValue> pair in parentStyle.Properties)
                {
                    if (properties.ContainsKey(pair.Key) || winners.ContainsKey(pair.Key)) continue;
                    if (!_inherited.Contains(pair.Key)) continue;
                    properties[pair.Key] = new ResolvedValue { Value = pair.Value.Value, Source = pair.Value.Source, Rule = pair.Value.Rule, Sheet = pair.Value.Sheet, Inherited = true };
                }
            }
            return new ResolvedStyle { Properties = properties };
        }

        private static void Offer(Dictionary<string, Candidate> winners, string name, CssValue value, Declaration source, Entry entry, Priority priority)
        {
            Candidate existing;
            if (winners.TryGetValue(name, out existing) && existing.Priority.CompareTo(priority) > 0) return;
            winners[name] = new Candidate { Priority = priority, Name = name, Value = value, Source = source, Entry = entry };
        }

        private static CssValue LookupVariable(Dictionary<string, CssValue> variables, string name)
        {
            CssValue value;
            return variables.TryGetValue(name, out value) ? value : null;
        }

        private Dictionary<string, CssValue> VariablesOf(IElement element)
        {
            Dictionary<string, CssValue> cached;
            if (_variableCache.TryGetValue(element, out cached)) return cached;
            Dictionary<string, CssValue> inherited = element.Parent == null ? new Dictionary<string, CssValue>(StringComparer.Ordinal) : VariablesOf(element.Parent);
            var raw = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            foreach (Entry entry in _entries)
            {
                if (entry.PseudoElement) continue;
                bool hasCustom = false;
                foreach (Declaration d in entry.Rule.Declarations)
                {
                    if (d.Name.StartsWith("--", StringComparison.Ordinal))
                    {
                        hasCustom = true;
                        break;
                    }
                }
                if (!hasCustom) continue;
                if (!SelectorMatcher.Matches(entry.Selector, element, PseudoStateSet.None)) continue;
                for (int i = 0; i < entry.Rule.Declarations.Count; i++)
                {
                    Declaration declaration = entry.Rule.Declarations[i];
                    if (!declaration.Name.StartsWith("--", StringComparison.Ordinal)) continue;
                    var priority = new Priority { Important = declaration.Important, Specificity = entry.Selector.Specificity, SheetIndex = entry.SheetIndex, RuleOrder = entry.RuleOrder, DeclIndex = i };
                    Offer(raw, declaration.Name, declaration.Value, declaration, entry, priority);
                }
            }
            var result = new Dictionary<string, CssValue>(inherited, StringComparer.Ordinal);
            var resolved = new Dictionary<string, CssValue>(StringComparer.Ordinal);
            var invalid = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in raw.Keys)
            {
                CssValue value = ResolveCustom(name, raw, inherited, resolved, invalid, new List<string>());
                if (value != null) result[name] = value;
                else result.Remove(name);
            }
            _variableCache[element] = result;
            return result;
        }

        private CssValue ResolveCustom(string name, Dictionary<string, Candidate> raw, Dictionary<string, CssValue> inherited, Dictionary<string, CssValue> resolved, HashSet<string> invalid, List<string> stack)
        {
            CssValue done;
            if (resolved.TryGetValue(name, out done)) return done;
            if (invalid.Contains(name)) return null;
            Candidate candidate;
            if (!raw.TryGetValue(name, out candidate))
            {
                CssValue fromParent;
                return inherited.TryGetValue(name, out fromParent) ? fromParent : null;
            }
            if (stack.Contains(name))
            {
                foreach (string member in stack.GetRange(stack.IndexOf(name), stack.Count - stack.IndexOf(name))) invalid.Add(member);
                return null;
            }
            stack.Add(name);
            bool ok = true;
            CssValue value = candidate.Value;
            if (CssValueParser.HasVarRef(value))
            {
                value = Substitute(value, n => ResolveCustom(n, raw, inherited, resolved, invalid, stack), 0, ref ok);
            }
            stack.RemoveAt(stack.Count - 1);
            if (invalid.Contains(name)) return null;
            if (!ok)
            {
                invalid.Add(name);
                return null;
            }
            resolved[name] = value;
            return value;
        }

        private CssValue Substitute(CssValue value, Func<string, CssValue> lookup, int depth, ref bool ok)
        {
            if (!ok || value == null) return value;
            if (depth > _options.MaxVarDepth)
            {
                ok = false;
                return value;
            }
            var reference = value as CssVarRef;
            if (reference != null)
            {
                CssValue target = lookup(reference.Name);
                if (target != null) return target;
                if (reference.Fallback != null) return Substitute(reference.Fallback, lookup, depth + 1, ref ok);
                ok = false;
                return value;
            }
            var function = value as CssFunction;
            if (function != null && CssValueParser.HasVarRef(function))
            {
                var args = new List<CssValue>(function.Args.Count);
                foreach (CssValue arg in function.Args) args.Add(Substitute(arg, lookup, depth + 1, ref ok));
                if (!ok) return value;
                var rebuilt = new CssFunction(function.Name, args) { Span = function.Span };
                CssColor color;
                if (ColorParser.TryFromFunction(rebuilt, null, out color)) return color;
                return rebuilt;
            }
            var list = value as CssList;
            if (list != null && CssValueParser.HasVarRef(list))
            {
                var items = new List<CssValue>(list.Items.Count);
                foreach (CssValue item in list.Items)
                {
                    CssValue replaced = Substitute(item, lookup, depth + 1, ref ok);
                    var inner = replaced as CssList;
                    if (inner != null && inner.Separator == list.Separator && !ReferenceEquals(replaced, item)) items.AddRange(inner.Items);
                    else items.Add(replaced);
                }
                if (!ok) return value;
                return new CssList(items, list.Separator) { Span = list.Span };
            }
            return value;
        }

        private void CheckVariables()
        {
            if (_sink == null) return;
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (Entry entry in _entries)
            {
                foreach (Declaration d in entry.Rule.Declarations)
                {
                    if (d.Name.StartsWith("--", StringComparison.Ordinal)) declared.Add(d.Name);
                }
            }
            var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var seen = new HashSet<Declaration>();
            foreach (Entry entry in _entries)
            {
                foreach (Declaration d in entry.Rule.Declarations)
                {
                    if (!seen.Add(d)) continue;
                    var refs = new List<CssVarRef>();
                    CssValueParser.CollectVarRefs(d.Value, refs);
                    foreach (CssVarRef reference in refs)
                    {
                        if (reference.Fallback == null && !declared.Contains(reference.Name))
                        {
                            _sink.Report(new Diagnostic
                            {
                                RuleId = "C033",
                                Severity = Severity.Error,
                                Tier = Tier.Reject,
                                Span = d.Span,
                                Construct = "var(" + reference.Name + ")",
                                Message = "var() refers to the undefined custom property '" + reference.Name + "'",
                                Why = "The property would be dropped at computed-value time.",
                                Suggestion = "Define " + reference.Name + " in a :root rule or give var() a fallback value."
                            });
                        }
                        if (d.Name.StartsWith("--", StringComparison.Ordinal))
                        {
                            HashSet<string> edges;
                            if (!graph.TryGetValue(d.Name, out edges))
                            {
                                edges = new HashSet<string>(StringComparer.Ordinal);
                                graph[d.Name] = edges;
                            }
                            edges.Add(reference.Name);
                        }
                    }
                }
            }
            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (string start in graph.Keys)
            {
                if (reported.Contains(start)) continue;
                var path = new List<string>();
                if (FindCycle(start, graph, path, new HashSet<string>(StringComparer.Ordinal), start))
                {
                    foreach (string member in path) reported.Add(member);
                    Declaration site = FirstDeclaration(start);
                    _sink.Report(new Diagnostic
                    {
                        RuleId = "C033",
                        Severity = Severity.Error,
                        Tier = Tier.Reject,
                        Span = site == null ? default(SourceSpan) : site.Span,
                        Construct = string.Join(" -> ", path) + " -> " + start,
                        Message = "circular var() reference between custom properties",
                        Why = "Custom properties that reference each other are invalid at computed-value time.",
                        Suggestion = "Break the cycle by giving one of the properties a literal value."
                    });
                }
            }
        }

        private Declaration FirstDeclaration(string name)
        {
            foreach (Entry entry in _entries)
            {
                foreach (Declaration d in entry.Rule.Declarations)
                {
                    if (d.Name == name) return d;
                }
            }
            return null;
        }

        private static bool FindCycle(string node, Dictionary<string, HashSet<string>> graph, List<string> path, HashSet<string> visiting, string target)
        {
            path.Add(node);
            visiting.Add(node);
            HashSet<string> edges;
            if (graph.TryGetValue(node, out edges))
            {
                var ordered = new List<string>(edges);
                ordered.Sort(StringComparer.Ordinal);
                foreach (string next in ordered)
                {
                    if (next == target) return true;
                    if (visiting.Contains(next)) continue;
                    if (FindCycle(next, graph, path, visiting, target)) return true;
                }
            }
            path.RemoveAt(path.Count - 1);
            return false;
        }

        private void DetectConflicts()
        {
            var groups = new Dictionary<string, List<KeyValuePair<int, (Declaration Decl, Stylesheet Sheet)>>>(StringComparer.Ordinal);
            var order = new List<string>();
            for (int s = 0; s < _cascade.Count; s++)
            {
                foreach (StyleRule rule in _cascade[s].Rules)
                {
                    if (rule.AtContext.Count > 0) continue;
                    foreach (Selector selector in rule.Selectors)
                    {
                        string selectorKey = ConflictKey(selector);
                        foreach (Declaration declaration in rule.Declarations)
                        {
                            string key = selectorKey + "\u0001" + declaration.Name;
                            List<KeyValuePair<int, (Declaration, Stylesheet)>> list;
                            if (!groups.TryGetValue(key, out list))
                            {
                                list = new List<KeyValuePair<int, (Declaration, Stylesheet)>>();
                                groups[key] = list;
                                order.Add(key);
                            }
                            list.Add(new KeyValuePair<int, (Declaration, Stylesheet)>(s, (declaration, _cascade[s])));
                        }
                    }
                }
            }
            foreach (string key in order)
            {
                List<KeyValuePair<int, (Declaration Decl, Stylesheet Sheet)>> list = groups[key];
                int distinctSheets = 0;
                int lastSheet = -1;
                foreach (var item in list)
                {
                    if (item.Key != lastSheet)
                    {
                        distinctSheets++;
                        lastSheet = item.Key;
                    }
                }
                if (distinctSheets < 2) continue;
                var finals = new List<(Declaration Decl, Stylesheet Sheet)>();
                lastSheet = -1;
                foreach (var item in list)
                {
                    if (item.Key != lastSheet)
                    {
                        finals.Add(item.Value);
                        lastSheet = item.Key;
                    }
                    else finals[finals.Count - 1] = item.Value;
                }
                string selectorText = key.Substring(0, key.IndexOf('\u0001'));
                for (int i = 1; i < finals.Count; i++)
                {
                    var first = finals[i - 1];
                    var second = finals[i];
                    string firstText = first.Decl.Value.ToText() + (first.Decl.Important ? " !important" : "");
                    string secondText = second.Decl.Value.ToText() + (second.Decl.Important ? " !important" : "");
                    if (string.Equals(firstText, secondText, StringComparison.OrdinalIgnoreCase)) continue;
                    var conflict = new StyleConflict
                    {
                        SelectorText = selectorText,
                        Property = second.Decl.Name,
                        First = first.Decl.Span,
                        Second = second.Decl.Span,
                        FirstValue = firstText,
                        SecondValue = secondText,
                        FirstSheet = first.Sheet,
                        SecondSheet = second.Sheet
                    };
                    _conflicts.Add(conflict);
                    if (_sink != null)
                    {
                        _sink.Report(new Diagnostic
                        {
                            RuleId = "J001",
                            Severity = Severity.Warning,
                            Tier = Tier.Native,
                            Span = second.Decl.Span,
                            Related = new[] { first.Decl.Span, second.Decl.Span },
                            Construct = selectorText + " { " + second.Decl.Name + " }",
                            Message = "'" + selectorText + "' sets '" + second.Decl.Name + "' to '" + firstText + "' in " + (first.Sheet.Path ?? "a stylesheet") + " and to '" + secondText + "' in " + (second.Sheet.Path ?? "a stylesheet"),
                            Why = "The later stylesheet in the cascade silently wins.",
                            Suggestion = "Keep one definition, or make the intended override explicit with a more specific selector."
                        });
                    }
                }
            }
        }

        public static string ConflictKey(Selector selector)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < selector.Parts.Count; i++)
            {
                (char combinator, SimpleSelector part) = selector.Parts[i];
                if (i > 0) sb.Append(combinator == '\0' ? ' ' : combinator).Append(' ');
                var classes = new List<string>(part.Classes);
                classes.Sort(StringComparer.Ordinal);
                var attrs = new List<string>();
                foreach (AttributeSelector a in part.Attributes) attrs.Add(a.Name + (a.Operator ?? "") + (a.Value ?? "") + (a.CaseInsensitive ? "i" : ""));
                attrs.Sort(StringComparer.Ordinal);
                var pseudos = new List<string>();
                foreach (PseudoClass p in part.Pseudos) pseudos.Add(p.Name + (p.Argument == null ? "" : "(" + p.Argument + ")"));
                pseudos.Sort(StringComparer.Ordinal);
                if (part.Type != null) sb.Append(part.Type);
                if (part.Id != null) sb.Append('#').Append(part.Id);
                foreach (string c in classes) sb.Append('.').Append(c);
                foreach (string a in attrs) sb.Append('[').Append(a).Append(']');
                foreach (string p in pseudos) sb.Append(':').Append(p);
                foreach (string e in part.PseudoElements) sb.Append("::").Append(e);
                if (part.Type == null && part.Id == null && classes.Count == 0 && attrs.Count == 0 && pseudos.Count == 0 && part.PseudoElements.Count == 0) sb.Append('*');
            }
            return sb.ToString();
        }
    }
}
