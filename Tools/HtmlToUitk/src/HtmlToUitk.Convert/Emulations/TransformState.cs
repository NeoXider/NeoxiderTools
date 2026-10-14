using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    internal sealed class TransformState
    {
        private static readonly ConditionalWeakTable<EmulationContext, TransformState> Table = new ConditionalWeakTable<EmulationContext, TransformState>();

        private readonly Dictionary<DomElement, Dictionary<string, string>> _resolved = new Dictionary<DomElement, Dictionary<string, string>>();

        public EmulationContext Context;
        public DiagnosticBag Diagnostics;
        public DocumentExtras Doc;
        public ConfigExtras Extras;
        public IFileSystem Fs;
        public string ProjectRoot = ".";
        public string GeneratedFolder = "Generated";
        public string ScreenName;
        public AssetSink AssetSink;
        public SpecSink SpecSink;
        public ElementEditor Editor;
        public IReadOnlyList<(Construct Construct, TierDecision Decision)> Decisions = Array.Empty<(Construct, TierDecision)>();
        public Dictionary<string, string> PageSheetOf = new Dictionary<string, string>(StringComparer.Ordinal);
        public string PrimarySheetId;
        public LocTable Loc;
        public PageState Page;
        public HashSet<UxmlElement> Handled = new HashSet<UxmlElement>();

        public UiDocument Document => Context.Document;
        public SheetBuilder Sheets => Context.Sheets;
        public IStyleModel Style => Context.Style;
        public ConversionConfig Config => Context.Config;
        public CapabilityMatrix Caps => Context.Capabilities;

        public static TransformState Of(EmulationContext context)
        {
            TransformState state;
            if (!Table.TryGetValue(context, out state)) throw new InvalidOperationException("emulation context was not created by the transform stage");
            return state;
        }

        public static void Register(EmulationContext context, TransformState state)
        {
            Table.Remove(context);
            Table.Add(context, state);
        }

        public void Report(string ruleId, Severity severity, Tier tier, SourceSpan span, string construct, string message, string suggestion, string emulationId)
        {
            for (int i = 0; i < Diagnostics.Items.Count; i++)
            {
                Diagnostic d = Diagnostics.Items[i];
                if (d.RuleId == ruleId && d.Span.File == span.File && d.Span.Start.Line == span.Start.Line && d.Severity == severity) return;
            }

            Diagnostics.Report(new Diagnostic
            {
                RuleId = ruleId,
                Severity = severity,
                Tier = tier,
                Span = span,
                Construct = construct,
                Message = message,
                Suggestion = suggestion,
                EmulationId = emulationId
            });
        }


        public void AddKey(string key, string mock, string elementName, string attribute, SourceSpan span)
        {
            string previous;
            if (Doc.KeyMocks.TryGetValue(key, out previous))
            {
                if (!string.IsNullOrEmpty(mock) && string.IsNullOrEmpty(previous)) Doc.KeyMocks[key] = mock;
                return;
            }

            Doc.KeyMocks[key] = mock ?? "";
            Doc.KeyOrder.Add(key);
            if (Loc == null || !Loc.IsLoaded || !Loc.Has(key)) Doc.NewKeys.Add(key);
        }

        public static bool HasClass(UxmlElement element, string className)
        {
            IReadOnlyList<string> classes = ClassList.Split(element.GetAttribute("class"));
            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i] == className) return true;
            }

            return false;
        }

        public bool SelectorMatches(Selector selector, UxmlElement element)
        {
            return element.Source != null && SelectorMatcher.Matches(selector, element.Source);
        }

        public IEnumerable<UxmlElement> Elements()
        {
            foreach (UxmlElement root in Document.Roots)
            {
                foreach (UxmlElement element in Walk(root)) yield return element;
            }
        }

        public static IEnumerable<UxmlElement> Walk(UxmlElement element)
        {
            yield return element;
            foreach (UxmlElement child in element.Children)
            {
                foreach (UxmlElement item in Walk(child)) yield return item;
            }
        }

        public string Prop(UxmlElement element, string name)
        {
            DomElement dom = element.Source;
            if (dom == null) return null;
            Dictionary<string, string> map;
            if (!_resolved.TryGetValue(dom, out map))
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                ResolvedStyle style = Style.Resolve(dom, PseudoStateSet.None);
                foreach (KeyValuePair<string, ResolvedValue> pair in style.Properties)
                {
                    if (pair.Value.Value != null) map[pair.Key] = pair.Value.Value.ToText().Trim();
                }

                _resolved[dom] = map;
            }

            string value;
            return map.TryGetValue(name, out value) ? value : null;
        }

        public ResolvedValue PropSource(UxmlElement element, string name)
        {
            DomElement dom = element.Source;
            if (dom == null) return null;
            ResolvedValue value;
            return Style.Resolve(dom, PseudoStateSet.None).Properties.TryGetValue(name, out value) ? value : null;
        }

        public IReadOnlyList<UxmlElement> MatchUxml(Selector selector)
        {
            var result = new List<UxmlElement>();
            foreach (UxmlElement root in Document.Roots)
            {
                foreach (IElement matched in Style.Match(selector, root))
                {
                    UxmlElement uxml = matched as UxmlElement;
                    if (uxml != null && !result.Contains(uxml)) result.Add(uxml);
                }
            }

            return result;
        }

        public IReadOnlyList<UxmlElement> MatchDom(Selector selector)
        {
            var result = new List<UxmlElement>();
            DomElement domRoot = DomRoot();
            if (domRoot == null) return result;
            var byDom = new Dictionary<DomElement, UxmlElement>();
            foreach (UxmlElement element in Elements())
            {
                if (element.Source != null) byDom[element.Source] = element;
            }

            foreach (IElement matched in Style.Match(selector, domRoot))
            {
                DomElement dom = matched as DomElement;
                UxmlElement uxml;
                if (dom != null && byDom.TryGetValue(dom, out uxml) && !result.Contains(uxml)) result.Add(uxml);
            }

            return result;
        }

        public DomElement DomRoot()
        {
            foreach (UxmlElement root in Document.Roots)
            {
                DomElement dom = root.Source;
                if (dom == null) continue;
                while (dom.Parent != null) dom = dom.Parent;
                return dom;
            }

            return null;
        }
    }

    public abstract class EmulationBase : IEmulation
    {
        public abstract string Id { get; }

        public abstract Tier Tier { get; }

        public abstract IReadOnlyList<string> Handles { get; }

        public virtual IReadOnlyList<string> RuntimeComponents => Array.Empty<string>();

        public abstract int Order { get; }

        public EmulationOutcome Apply(EmulationContext context, Construct construct)
        {
            TransformState state = TransformState.Of(context);
            var outcome = new EmulationOutcome { Handled = true };
            Run(state, construct, outcome);
            return outcome;
        }

        internal abstract void Run(TransformState state, Construct construct, EmulationOutcome outcome);
    }
}
