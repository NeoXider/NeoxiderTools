using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;

namespace HtmlToUitk.Rules
{
    public sealed class RuleReporter
    {
        private readonly IRuleCatalog _catalog;
        private readonly RuleCatalog _extended;
        private readonly ConversionConfig _config;
        private readonly IDiagnosticSink _sink;
        private readonly HashSet<string> _disabled;
        private readonly CapabilityMatrix _caps;

        public RuleReporter(IRuleCatalog catalog, ConversionConfig config, IDiagnosticSink sink, CapabilityMatrix caps = null)
        {
            _caps = caps;
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _extended = catalog as RuleCatalog;
            _config = config ?? new ConversionConfig();
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _disabled = new HashSet<string>(StringComparer.Ordinal);
            if (_config.Rules != null && _config.Rules.Disable != null)
            {
                foreach (string id in _config.Rules.Disable) _disabled.Add(id);
            }
        }

        public IRuleCatalog Catalog => _catalog;

        public ConversionConfig Config => _config;

        public bool IsDisabled(string ruleId)
        {
            return ruleId != null && _disabled.Contains(ruleId);
        }

        public RuleInfo Rule(string ruleId)
        {
            RuleInfo info = _catalog.Get(ruleId);
            if (info == null) throw new InvalidOperationException("rule " + ruleId + " is not in the catalog");
            return info;
        }

        public Severity? EmulatedSeverity(string ruleId)
        {
            return _extended != null ? _extended.EmulatedSeverity(ruleId) : null;
        }

        public Diagnostic Report(string ruleId, SourceSpan span, string construct, string message, string suggestion = null,
            Severity? severity = null, Tier? tier = null, string emulation = null, IReadOnlyList<SourceSpan> related = null)
        {
            if (IsDisabled(ruleId)) return null;
            RuleInfo rule = Rule(ruleId);
            var diagnostic = new Diagnostic
            {
                RuleId = ruleId,
                Severity = severity ?? rule.DefaultSeverity,
                Tier = tier ?? rule.DefaultTier,
                Span = span,
                Related = related ?? Array.Empty<SourceSpan>(),
                Construct = construct ?? "",
                Message = string.IsNullOrEmpty(message) ? rule.Title : message,
                Why = rule.Why,
                Suggestion = suggestion ?? rule.Fix,
                EmulationId = emulation,
            };
            _sink.Report(diagnostic);
            return diagnostic;
        }

        public Diagnostic ReportDecision(Construct construct, TierDecision decision)
        {
            if (decision == null || decision.RuleId == null) return null;
            if (IsDisabled(decision.RuleId)) return null;
            RuleInfo rule = Rule(decision.RuleId);
            Severity severity;
            string suggestion;
            if (decision.Tier == Tier.Reject)
            {
                severity = rule.DefaultSeverity;
                suggestion = Cx.Get(construct, ConstructKeys.DataSuggestion) ?? rule.Fix;
            }
            else
            {
                severity = EmulatedSeverity(decision.RuleId) ?? (decision.Tier == Tier.Native ? rule.DefaultSeverity : Severity.Info);
                if (decision.Tier != Tier.Native && Cx.Is(construct, ConstructKeys.DataApproximate) && severity < Severity.Warning) severity = Severity.Warning;
                suggestion = Cx.Get(construct, ConstructKeys.DataSuggestion);
                if (decision.Tier == Tier.Native && NativeNeedsWarning(construct)) severity = Severity.Warning;
            }
            string text = Cx.Get(construct, ConstructKeys.DataText) ?? construct.Key;
            string message = Cx.Get(construct, ConstructKeys.DataMessage);
            if (message == null) message = Describe(rule, decision, text);
            return Report(decision.RuleId, construct.Span, text, message, suggestion ?? "", severity, decision.Tier, decision.EmulationId);
        }

        private bool NativeNeedsWarning(Construct construct)
        {
            if (construct.Key == ConstructKeys.ShadowOuter) return true;
            string name = Cx.Get(construct, ConstructKeys.DataName);
            if (_caps != null && name != null && construct.Key.StartsWith(ConstructKeys.FunctionPrefix, StringComparison.Ordinal))
            {
                return string.Equals(_caps.FunctionEvidence(name), "assembly-only", StringComparison.Ordinal);
            }
            return false;
        }

        private static string Describe(RuleInfo rule, TierDecision decision, string text)
        {
            switch (decision.Tier)
            {
                case Tier.Reject:
                    return text + ": " + decision.Reason;
                case Tier.Native:
                    return text + ": " + decision.Reason;
                case Tier.Runtime:
                    return text + ": " + decision.Reason + (decision.RequiredRuntime.Count > 0 ? " (runtime component " + string.Join(", ", decision.RequiredRuntime) + ")" : "");
                default:
                    return text + ": " + decision.Reason + " (" + decision.EmulationId + ")";
            }
        }
    }
}
