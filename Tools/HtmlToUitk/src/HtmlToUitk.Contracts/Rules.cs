using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Rules
{
    public enum ConstructKind { Property, Value, Selector, AtRule, Element, Attribute, Page }

    public sealed class Construct
    {
        public ConstructKind Kind;
        public string Key;
        public SourceSpan Span;
        public object Node;
        public IReadOnlyDictionary<string, string> Data = Empty.Strings;
    }

    public sealed class AnalysisPage
    {
        public string Path;
        public HtmlDocument Html;
        public UiDocument Document;
        public IReadOnlyList<Stylesheet> Cascade = Array.Empty<Stylesheet>();
        public IStyleModel Style;
    }

    public sealed class AnalysisInput
    {
        public IReadOnlyList<AnalysisPage> Pages = Array.Empty<AnalysisPage>();
        public IReadOnlyList<Stylesheet> Stylesheets = Array.Empty<Stylesheet>();
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
        public IRuleCatalog Catalog;
        public TokenTable Tokens;
        public KeyTable Keys;
        public NavModel Nav;
        public AnimModel Anim;
        public IncludeGraph Includes;
        public IReadOnlyCollection<string> KnownKeys = Array.Empty<string>();
    }

    public interface IConstructDetector
    {
        void Detect(AnalysisInput input, ICollection<Construct> output);
    }

    public sealed class TierDecision
    {
        public Tier Tier;
        public string EmulationId;
        public string RuleId;
        public string Reason;
        public IReadOnlyList<string> RequiredRuntime = Array.Empty<string>();
    }

    public interface ITierPlanner
    {
        TierDecision Decide(Construct construct, CapabilityMatrix caps, ConversionConfig config);
    }

    public sealed class RuleContext
    {
        public AnalysisInput Input;
        public IReadOnlyList<Construct> Constructs = Array.Empty<Construct>();
        public IReadOnlyList<(Construct Construct, TierDecision Decision)> Decisions = Array.Empty<(Construct, TierDecision)>();
        public IDiagnosticSink Sink;
        public IRuleCatalog Catalog;
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
    }

    public interface IRule
    {
        string Id { get; }
        void Evaluate(RuleContext context);
    }

    public interface IRuleEngine
    {
        void Run(AnalysisInput input, IDiagnosticSink sink);
    }
}
