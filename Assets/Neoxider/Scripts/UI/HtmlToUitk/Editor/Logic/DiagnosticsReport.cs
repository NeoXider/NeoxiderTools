using System;
using System.Collections.Generic;
using Neo.HtmlToUitk.Json;

namespace Neo.HtmlToUitk.Tooling
{
    public enum FindingSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum FileStatus
    {
        NotRun,
        Pass,
        Warn,
        Fail
    }

    public sealed class FindingLocation
    {
        public FindingLocation(string file, int line, int column)
        {
            File = file;
            Line = line;
            Column = column;
        }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }
    }

    public sealed class Finding
    {
        public string Rule;
        public FindingSeverity Severity;
        public string Tier;
        public string Emulation;
        public string File;
        public int Line;
        public int Column;
        public int EndLine;
        public int EndColumn;
        public string Construct;
        public string Message;
        public string Why;
        public string Suggestion;
        public List<FindingLocation> Related = new List<FindingLocation>();
    }

    public sealed class DiagnosticsReport
    {
        private readonly Dictionary<string, List<Finding>> _byFile = new Dictionary<string, List<Finding>>(StringComparer.OrdinalIgnoreCase);

        public string Tool { get; private set; }

        public string Version { get; private set; }

        public string Source { get; private set; }

        public string Verdict { get; private set; }

        public int Errors { get; private set; }

        public int Warnings { get; private set; }

        public int Infos { get; private set; }

        public IReadOnlyList<KeyValuePair<string, int>> Tiers { get; private set; } = new List<KeyValuePair<string, int>>();

        public List<Finding> Findings { get; } = new List<Finding>();

        public List<string> NewKeys { get; } = new List<string>();

        public static DiagnosticsReport Parse(string json)
        {
            JsonValue root = MiniJson.Parse(json);
            string verdict = root["verdict"].AsString();
            if (root.Kind != JsonKind.Object || (verdict != "PASS" && verdict != "PASS WITH WARNINGS" && verdict != "FAIL"))
                throw new JsonParseException("Diagnostics require a recognized verdict", 1, 1);
            DiagnosticsReport report = new DiagnosticsReport
            {
                Tool = root["tool"].AsString("htmltouitk"),
                Version = root["version"].AsString(string.Empty),
                Source = root["source"].AsString(),
                Verdict = verdict
            };
            JsonValue counts = root["counts"];
            report.Errors = counts["error"].AsInt();
            report.Warnings = counts["warning"].AsInt();
            report.Infos = counts["info"].AsInt();
            List<KeyValuePair<string, int>> tiers = new List<KeyValuePair<string, int>>();
            JsonValue tierObject = root["tiers"];
            for (int i = 0; i < tierObject.Members.Count; i++)
                tiers.Add(new KeyValuePair<string, int>(tierObject.Members[i].Key, tierObject.Members[i].Value.AsInt()));
            report.Tiers = tiers;
            JsonValue list = root["findings"];
            for (int i = 0; i < list.Count; i++)
                report.Add(ParseFinding(list[i]));
            report.NewKeys.AddRange(root["newKeys"].ToStringList());
            if (counts.Kind != JsonKind.Object)
                report.RecountFromFindings();
            return report;
        }

        public static DiagnosticsReport Failure(string message)
        {
            DiagnosticsReport report = new DiagnosticsReport { Tool = "htmltouitk", Version = string.Empty, Verdict = "FAIL" };
            report.Add(new Finding { Rule = "E000", Severity = FindingSeverity.Error, Tier = "Reject", File = string.Empty, Message = message });
            report.RecountFromFindings();
            return report;
        }

        public void Add(Finding finding)
        {
            Findings.Add(finding);
            string key = Key(finding.File);
            if (!_byFile.TryGetValue(key, out List<Finding> list))
            {
                list = new List<Finding>();
                _byFile[key] = list;
            }

            list.Add(finding);
        }

        public IReadOnlyList<Finding> For(string file)
        {
            return _byFile.TryGetValue(Key(file), out List<Finding> list) ? list : (IReadOnlyList<Finding>)Array.Empty<Finding>();
        }

        public IEnumerable<string> Files()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Findings.Count; i++)
            {
                if (seen.Add(Key(Findings[i].File)))
                    yield return Findings[i].File;
            }
        }

        public FileStatus StatusOf(string file)
        {
            IReadOnlyList<Finding> findings = For(file);
            FileStatus status = FileStatus.Pass;
            for (int i = 0; i < findings.Count; i++)
            {
                if (findings[i].Severity == FindingSeverity.Error)
                    return FileStatus.Fail;
                if (findings[i].Severity == FindingSeverity.Warning)
                    status = FileStatus.Warn;
            }

            return status;
        }

        public static string Key(string file)
        {
            return GlobMatcher.Normalize(file ?? string.Empty);
        }

        private void RecountFromFindings()
        {
            Errors = 0;
            Warnings = 0;
            Infos = 0;
            for (int i = 0; i < Findings.Count; i++)
            {
                switch (Findings[i].Severity)
                {
                    case FindingSeverity.Error: Errors++; break;
                    case FindingSeverity.Warning: Warnings++; break;
                    default: Infos++; break;
                }
            }
        }

        private static Finding ParseFinding(JsonValue f)
        {
            Finding finding = new Finding
            {
                Rule = f["rule"].AsString(string.Empty),
                Severity = ParseSeverity(f["severity"].AsString("Info")),
                Tier = f["tier"].AsString(string.Empty),
                Emulation = f["emulation"].AsString(),
                File = f["file"].AsString(string.Empty),
                Line = f["line"].AsInt(),
                Column = f["column"].AsInt(),
                EndLine = f["endLine"].AsInt(),
                EndColumn = f["endColumn"].AsInt(),
                Construct = f["construct"].AsString(),
                Message = f["message"].AsString(),
                Why = f["why"].AsString(),
                Suggestion = f["suggestion"].AsString()
            };
            JsonValue related = f["related"];
            for (int i = 0; i < related.Count; i++)
                finding.Related.Add(new FindingLocation(related[i]["file"].AsString(string.Empty), related[i]["line"].AsInt(), related[i]["column"].AsInt()));
            return finding;
        }

        private static FindingSeverity ParseSeverity(string text)
        {
            if (string.Equals(text, "Error", StringComparison.OrdinalIgnoreCase))
                return FindingSeverity.Error;
            if (string.Equals(text, "Warning", StringComparison.OrdinalIgnoreCase))
                return FindingSeverity.Warning;
            return FindingSeverity.Info;
        }
    }
}
