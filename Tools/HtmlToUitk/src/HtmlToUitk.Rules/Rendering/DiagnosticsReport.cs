using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Rules
{
    public static class ExitCodes
    {
        public const int Pass = 0;
        public const int Fail = 1;
        public const int Warnings = 1;
        public const int Usage = 2;
    }

    public sealed class TierTally
    {
        public int Native, ConvertTime, Runtime, Reject;

        public void Add(Tier tier)
        {
            switch (tier)
            {
                case Tier.Native: Native++; break;
                case Tier.ConvertTime: ConvertTime++; break;
                case Tier.Runtime: Runtime++; break;
                default: Reject++; break;
            }
        }

        public static TierTally FromDecisions(IEnumerable<(Construct Construct, TierDecision Decision)> decisions)
        {
            var tally = new TierTally();
            foreach ((Construct Construct, TierDecision Decision) item in decisions) tally.Add(item.Decision.Tier);
            return tally;
        }

        public static TierTally FromFindings(IEnumerable<Diagnostic> findings)
        {
            var tally = new TierTally();
            foreach (Diagnostic d in findings) tally.Add(d.Tier);
            return tally;
        }
    }

    public sealed class ReportOptions
    {
        public string Version = DiagnosticsReport.ToolVersion;
        public string Source;
        public IReadOnlyList<string> NewKeys;
        public TierTally Tiers;
        public bool IncludeTiers = true;
        public bool Strict;
    }

    public sealed class DiagnosticsReport
    {
        public const string ToolName = "htmltouitk";
        public const string ToolVersion = "0.1.0";

        public Verdict Verdict;
        public int Errors, Warnings, Infos;
        public TierTally Tiers;
        public IReadOnlyList<Diagnostic> Findings = Array.Empty<Diagnostic>();
        public IReadOnlyList<string> NewKeys = Array.Empty<string>();
        public string Version = ToolVersion;
        public string Source;

        public static string VerdictText(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.Pass: return "PASS";
                case Verdict.PassWithWarnings: return "PASS WITH WARNINGS";
                default: return "FAIL";
            }
        }

        public static string SeverityText(Severity severity)
        {
            switch (severity)
            {
                case Severity.Error: return "Error";
                case Severity.Warning: return "Warning";
                default: return "Info";
            }
        }

        public static string TierText(Tier tier)
        {
            switch (tier)
            {
                case Tier.Native: return "Native";
                case Tier.ConvertTime: return "ConvertTime";
                case Tier.Runtime: return "Runtime";
                default: return "Reject";
            }
        }

        public static IReadOnlyList<Diagnostic> Distinct(IEnumerable<Diagnostic> items)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<Diagnostic>();
            foreach (Diagnostic d in items)
            {
                string key = string.Join("\u0001", d.RuleId, (int)d.Severity, (int)d.Tier, d.Span.File ?? "", d.Span.Start.Line, d.Span.Start.Column, d.Span.End.Line, d.Span.End.Column, d.Construct ?? "", d.Message ?? "");
                if (seen.Add(key)) list.Add(d);
            }
            return list;
        }

        public static DiagnosticsReport Build(IEnumerable<Diagnostic> diagnostics, ReportOptions options = null)
        {
            options = options ?? new ReportOptions();
            var bag = new DiagnosticBag();
            foreach (Diagnostic d in Distinct(diagnostics)) bag.Report(d);
            var report = new DiagnosticsReport
            {
                Findings = bag.Sorted(),
                Errors = bag.Count(Severity.Error),
                Warnings = bag.Count(Severity.Warning),
                Infos = bag.Count(Severity.Info),
                Verdict = bag.Verdict,
                Source = options.Source,
                Version = options.Version ?? ToolVersion,
                NewKeys = options.NewKeys ?? Array.Empty<string>(),
            };
            report.Tiers = options.Tiers ?? TierTally.FromFindings(report.Findings);
            return report;
        }

        public static DiagnosticsReport Build(DiagnosticBag bag, ReportOptions options = null)
        {
            return Build(bag.Items, options);
        }

        public int ExitCode(bool strict)
        {
            if (Verdict == Verdict.Fail) return ExitCodes.Fail;
            if (strict && Verdict == Verdict.PassWithWarnings) return ExitCodes.Warnings;
            return ExitCodes.Pass;
        }
    }

    public static class DiagnosticsJson
    {
        public static JsonValue ToJson(DiagnosticsReport report, bool includeTiers = true)
        {
            var root = new JsonObject();
            root.Set("tool", DiagnosticsReport.ToolName);
            root.Set("version", report.Version);
            if (!string.IsNullOrEmpty(report.Source)) root.Set("source", report.Source);
            root.Set("verdict", DiagnosticsReport.VerdictText(report.Verdict));
            root.Set("counts", new JsonObject().Set("error", report.Errors).Set("warning", report.Warnings).Set("info", report.Infos));
            if (includeTiers && report.Tiers != null)
            {
                root.Set("tiers", new JsonObject()
                    .Set("native", report.Tiers.Native)
                    .Set("convertTime", report.Tiers.ConvertTime)
                    .Set("runtime", report.Tiers.Runtime)
                    .Set("reject", report.Tiers.Reject));
            }
            var findings = new JsonArray();
            foreach (Diagnostic d in report.Findings) findings.Add(Finding(d));
            root.Set("findings", findings);
            var keys = new JsonArray();
            foreach (string key in report.NewKeys) keys.Add(key);
            root.Set("newKeys", keys);
            return root;
        }

        public static string Write(DiagnosticsReport report, bool includeTiers = true)
        {
            return JsonWriter.Write(ToJson(report, includeTiers), JsonWriteOptions.Pretty);
        }

        public static JsonValue Finding(Diagnostic d)
        {
            var o = new JsonObject();
            o.Set("rule", d.RuleId);
            o.Set("severity", DiagnosticsReport.SeverityText(d.Severity));
            o.Set("tier", DiagnosticsReport.TierText(d.Tier));
            if (!string.IsNullOrEmpty(d.EmulationId)) o.Set("emulation", d.EmulationId);
            o.Set("file", Posix(d.Span.File));
            o.Set("line", d.Span.Start.Line);
            o.Set("column", d.Span.Start.Column);
            if (d.Span.End.Line > 0)
            {
                o.Set("endLine", d.Span.End.Line);
                o.Set("endColumn", d.Span.End.Column);
            }
            if (!string.IsNullOrEmpty(d.Construct)) o.Set("construct", d.Construct);
            if (!string.IsNullOrEmpty(d.Message)) o.Set("message", d.Message);
            if (!string.IsNullOrEmpty(d.Why)) o.Set("why", d.Why);
            if (!string.IsNullOrEmpty(d.Suggestion)) o.Set("suggestion", d.Suggestion);
            var related = new JsonArray();
            foreach (SourceSpan span in d.Related)
            {
                related.Add(new JsonObject().Set("file", Posix(span.File)).Set("line", span.Start.Line).Set("column", span.Start.Column));
            }
            o.Set("related", related);
            return o;
        }

        public static string Posix(string path)
        {
            return (path ?? "").Replace('\\', '/');
        }

        public static string WriteExpected(IEnumerable<Diagnostic> diagnostics, bool withColumns)
        {
            var array = new JsonArray();
            foreach (Diagnostic d in NormalizedOrder(diagnostics))
            {
                var o = new JsonObject();
                o.Set("rule", d.RuleId);
                o.Set("severity", DiagnosticsReport.SeverityText(d.Severity));
                o.Set("tier", DiagnosticsReport.TierText(d.Tier));
                if (!string.IsNullOrEmpty(d.EmulationId)) o.Set("emulation", d.EmulationId);
                o.Set("file", Posix(d.Span.File));
                o.Set("line", d.Span.Start.Line);
                if (withColumns) o.Set("column", d.Span.Start.Column);
                array.Add(o);
            }
            return JsonWriter.Write(array, JsonWriteOptions.Pretty);
        }

        public static IReadOnlyList<Diagnostic> NormalizedOrder(IEnumerable<Diagnostic> diagnostics)
        {
            return DiagnosticsReport.Build(diagnostics, new ReportOptions { IncludeTiers = false }).Findings;
        }
    }

    public static class DiagnosticsText
    {
        public static string Render(DiagnosticsReport report, bool verbose = true)
        {
            var sb = new StringBuilder();
            foreach (Diagnostic d in report.Findings)
            {
                string file = DiagnosticsJson.Posix(d.Span.File);
                sb.Append(file.Length == 0 ? "<input>" : file).Append(':').Append(d.Span.Start.Line.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append(d.Span.Start.Column.ToString(CultureInfo.InvariantCulture)).Append(": ");
                sb.Append(SeverityWord(d.Severity)).Append(' ').Append(d.RuleId).Append(" [").Append(DiagnosticsReport.TierText(d.Tier));
                if (!string.IsNullOrEmpty(d.EmulationId)) sb.Append(" via ").Append(d.EmulationId);
                sb.Append("] ").Append(d.Message ?? "").Append('\n');
                if (verbose)
                {
                    if (!string.IsNullOrEmpty(d.Construct)) sb.Append("    construct: ").Append(d.Construct).Append('\n');
                    if (!string.IsNullOrEmpty(d.Why)) sb.Append("    why: ").Append(d.Why).Append('\n');
                    if (!string.IsNullOrEmpty(d.Suggestion)) sb.Append("    fix: ").Append(d.Suggestion).Append('\n');
                    foreach (SourceSpan related in d.Related)
                    {
                        sb.Append("    see: ").Append(DiagnosticsJson.Posix(related.File)).Append(':').Append(related.Start.Line.ToString(CultureInfo.InvariantCulture))
                            .Append(':').Append(related.Start.Column.ToString(CultureInfo.InvariantCulture)).Append('\n');
                    }
                }
            }
            sb.Append(Summary(report)).Append('\n');
            return sb.ToString();
        }

        public static string Summary(DiagnosticsReport report)
        {
            return DiagnosticsReport.VerdictText(report.Verdict) + " (" + Plural(report.Errors, "error") + ", " + Plural(report.Warnings, "warning") + ", " + Plural(report.Infos, "info") + ")";
        }

        private static string Plural(int count, string word)
        {
            return count.ToString(CultureInfo.InvariantCulture) + " " + word + (count == 1 ? "" : "s");
        }

        private static string SeverityWord(Severity severity)
        {
            switch (severity)
            {
                case Severity.Error: return "error";
                case Severity.Warning: return "warning";
                default: return "info";
            }
        }
    }
}
