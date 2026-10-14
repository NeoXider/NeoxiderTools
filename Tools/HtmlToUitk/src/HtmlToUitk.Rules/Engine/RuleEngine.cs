using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert;
using HtmlToUitk.Convert.Config;

namespace HtmlToUitk.Rules
{
    public sealed class RuleEngine : IRuleEngine
    {
        private readonly IRuleCatalog _catalog;
        private readonly ITierPlanner _planner;
        private readonly IReadOnlyList<IConstructDetector> _detectors;
        private readonly IReadOnlyList<IRule> _rules;

        public RuleEngine(IRuleCatalog catalog, ITierPlanner planner, IReadOnlyList<IConstructDetector> detectors, IReadOnlyList<IRule> rules)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
            _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public static RuleEngine CreateDefault(IRuleCatalog catalog = null, IFileSystem fs = null, IStringTables tables = null)
        {
            return new RuleEngine(catalog ?? RuleCatalog.Embedded, new TierPlanner(), ConstructDetectors.Default(), DefaultRules.Create(fs, tables));
        }

        public IRuleCatalog Catalog => _catalog;

        public ITierPlanner Planner => _planner;

        public IReadOnlyList<Construct> Detect(AnalysisInput input)
        {
            Prepare(input);
            var constructs = new List<Construct>();
            foreach (IConstructDetector detector in _detectors) detector.Detect(input, constructs);
            return constructs;
        }

        public IReadOnlyList<(Construct Construct, TierDecision Decision)> Plan(AnalysisInput input, IReadOnlyList<Construct> constructs)
        {
            Prepare(input);
            var decisions = new List<(Construct, TierDecision)>(constructs.Count);
            foreach (Construct construct in constructs) decisions.Add((construct, _planner.Decide(construct, input.Capabilities, input.Config)));
            return decisions;
        }

        public void Run(AnalysisInput input, IDiagnosticSink sink)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            Prepare(input);
            IReadOnlyList<Construct> constructs = Detect(input);
            IReadOnlyList<(Construct Construct, TierDecision Decision)> decisions = Plan(input, constructs);
            IRuleCatalog catalog = input.Catalog ?? _catalog;
            var context = new RuleContext
            {
                Input = input,
                Constructs = constructs,
                Decisions = decisions,
                Sink = sink,
                Catalog = catalog,
                Config = input.Config,
                Capabilities = input.Capabilities,
            };
            foreach (IRule rule in _rules) rule.Evaluate(context);
        }

        private void Prepare(AnalysisInput input)
        {
            if (input.Config == null) input.Config = new ConversionConfig();
            if (input.Catalog == null) input.Catalog = _catalog;
            if (input.Capabilities == null)
            {
                CapabilityMatrix matrix;
                if (!CapabilityMatrixLoader.TryLoadEmbedded(NormalVersion(input.Config.Unity.Version), out matrix))
                {
                    throw new InvalidOperationException("AnalysisInput.Capabilities is null and no embedded capability matrix matches Unity " + input.Config.Unity.Version);
                }
                input.Capabilities = matrix;
            }
        }

        private static string NormalVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return "6000.6";
            string[] parts = version.Split('.');
            return parts.Length >= 2 ? parts[0] + "." + parts[1] : version;
        }
    }

    public static class DefaultRules
    {
        public static IReadOnlyList<IRule> Create(IFileSystem fs = null, IStringTables tables = null)
        {
            return new IRule[]
            {
                new ConstructDecisionRule(),
                new CssValueRules(fs),
                new TransitionRules(),
                new HtmlDialectRules(fs),
                new NamingRules(),
                new LocalisationRules(tables),
                new ProjectStyleRules(),
                new CrossFileRules(),
                new PageRules(),
                new AnimationRules(),
            };
        }
    }

    public sealed class ConstructDecisionRule : IRule
    {
        public string Id => "constructs";

        public void Evaluate(RuleContext context)
        {
            var reporter = new RuleReporter(context.Catalog, context.Config, context.Sink, context.Capabilities);
            foreach ((Construct Construct, TierDecision Decision) item in context.Decisions)
            {
                reporter.ReportDecision(item.Construct, item.Decision);
            }
        }
    }
}
