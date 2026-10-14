using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert
{
    public sealed class ConversionResult
    {
        public OutputSet Output = new OutputSet();
        public DiagnosticBag Diagnostics = new DiagnosticBag();
        public TransformedModel Model;
        public AnalysisResult Analysis;
        public ConversionPlan Plan;
        public bool Refused;
        public bool Validated;
        public IReadOnlyList<string> NewKeys = Array.Empty<string>();
    }

    public sealed class PlanStage : IPipelineStage<AnalysisResult, ConversionPlan>
    {
        private readonly PipelineState _state;

        internal PlanStage(PipelineState state)
        {
            _state = state;
        }

        public string Name => "plan";

        public ConversionPlan Run(AnalysisResult input, StageContext context)
        {
            var actions = new List<(Construct, TierDecision)>();
            ITierPlanner planner = _state.Services.Planner;
            if (planner != null)
            {
                foreach (Construct construct in input.Constructs) actions.Add((construct, planner.Decide(construct, context.Capabilities, context.Config)));
            }

            var plan = new ConversionPlan { Actions = actions };
            _state.Plan = plan;
            return plan;
        }
    }

    public static class ConversionPipeline
    {
        public static ConvertServices DefaultServices(IFileSystem fs, string configDirectory)
        {
            var services = new ConvertServices();
            RuleCatalog catalog = RuleCatalog.Embedded;
            services.Catalog = catalog;
            RuleEngine engine = RuleEngine.CreateDefault(catalog, fs, new ConfigStringTables(fs, configDirectory));
            services.RuleEngine = engine;
            services.Planner = engine.Planner;
            services.Detectors = ConstructDetectors.Default();
            return services;
        }

        public static ConversionResult Run(ConversionJob job, IFileSystem fs, ConvertServices services, RunOptions options, string configDirectory = "")
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            options = options ?? new RunOptions();
            configDirectory = PathUtil.Normalize(configDirectory ?? "");
            services = services ?? DefaultServices(fs, configDirectory);
            var state = new PipelineState
            {
                Job = job,
                Fs = fs,
                Services = services,
                Options = options,
                ConfigDirectory = configDirectory,
                ProjectRoot = PathUtil.Combine(configDirectory, job.Config.ProjectRoot ?? "")
            };
            state.UiRootPath = PathUtil.Combine(state.ProjectRoot, job.Config.UiRoot ?? "");
            var context = new StageContext { Config = job.Config, Capabilities = job.Capabilities, Diagnostics = new DiagnosticBag(), Fs = fs };
            var result = new ConversionResult { Diagnostics = context.Diagnostics };
            InputSet inputs = new LoadStage(state).Run(job, context);
            AnalysisResult analysis = new AnalyseStage(state).Run(inputs, context);
            result.Analysis = analysis;
            ConversionPlan plan = new PlanStage(state).Run(analysis, context);
            result.Plan = plan;
            var newKeys = new List<string>();
            foreach (PageState page in state.Pages)
            {
                foreach (string key in page.Map.NewKeys)
                {
                    if (!newKeys.Contains(key)) newKeys.Add(key);
                }
            }

            result.NewKeys = newKeys;
            if (options.ValidateOnly)
            {
                result.Validated = true;
                return result;
            }

            if (context.Diagnostics.Verdict == Verdict.Fail && !options.Force)
            {
                result.Refused = true;
                return result;
            }

            TransformedModel model = new TransformStage(state).Run((analysis, plan), context);
            result.Model = model;
            result.Output = new EmitStage(state).Run(model, context);
            return result;
        }
    }
}
