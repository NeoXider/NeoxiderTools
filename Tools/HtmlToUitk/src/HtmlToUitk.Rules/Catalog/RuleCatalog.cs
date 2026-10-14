using System;
using System.Collections.Generic;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Rules
{
    public sealed class RuleCatalog : IRuleCatalog
    {
        private static RuleCatalog _embedded;
        private readonly Dictionary<string, RuleInfo> _byId = new Dictionary<string, RuleInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, Severity> _emulated = new Dictionary<string, Severity>(StringComparer.Ordinal);
        private readonly List<RuleInfo> _all = new List<RuleInfo>();

        public static RuleCatalog Embedded => _embedded ?? (_embedded = Load(EmbeddedSpec.ReadText("rules.json")));

        public IReadOnlyList<RuleInfo> All => _all;

        public RuleInfo Get(string id)
        {
            RuleInfo info;
            return id != null && _byId.TryGetValue(id, out info) ? info : null;
        }

        public bool Has(string id)
        {
            return id != null && _byId.ContainsKey(id);
        }

        public Severity? EmulatedSeverity(string id)
        {
            Severity severity;
            return id != null && _emulated.TryGetValue(id, out severity) ? severity : (Severity?)null;
        }

        public static RuleCatalog Load(string json)
        {
            JsonObject root = JsonReader.Parse(json).AsObject();
            if (root == null) throw new FormatException("rules.json must be an object");
            var severityMap = new Dictionary<string, Severity>(StringComparer.Ordinal)
            {
                { "FAIL", Severity.Error }, { "WARN", Severity.Warning }, { "INFO", Severity.Info },
                { "Error", Severity.Error }, { "Warning", Severity.Warning }, { "Info", Severity.Info },
            };
            JsonArray rules = root.GetArray("rules");
            if (rules == null) throw new FormatException("rules.json has no rules array");
            var catalog = new RuleCatalog();
            foreach (JsonValue item in rules.Items)
            {
                JsonObject o = item.AsObject();
                if (o == null) throw new FormatException("rules.json rules must be objects");
                string id = o.GetString("id");
                if (string.IsNullOrEmpty(id)) throw new FormatException("rule without id");
                if (catalog._byId.ContainsKey(id)) throw new FormatException("duplicate rule id " + id);
                Severity severity = ParseSeverity(o.GetString("severity"), severityMap, id);
                Tier tier = ParseTier(o.GetString("tier", "Native"), id);
                var info = new RuleInfo
                {
                    Id = id,
                    DefaultSeverity = severity,
                    DefaultTier = tier,
                    Title = o.GetString("title", ""),
                    Why = o.GetString("why", ""),
                    Fix = o.GetString("fix", ""),
                    Emulation = o.GetString("emulation"),
                };
                catalog._byId[id] = info;
                catalog._all.Add(info);
                string emulated = o.GetString("emulatedSeverity");
                if (emulated != null) catalog._emulated[id] = ParseSeverity(emulated, severityMap, id);
            }
            catalog._all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return catalog;
        }

        private static Severity ParseSeverity(string text, Dictionary<string, Severity> map, string id)
        {
            Severity severity;
            if (text == null || !map.TryGetValue(text, out severity)) throw new FormatException("rule " + id + " has unknown severity '" + text + "'");
            return severity;
        }

        private static Tier ParseTier(string text, string id)
        {
            switch (text)
            {
                case "Native": return Tier.Native;
                case "ConvertTime": return Tier.ConvertTime;
                case "Runtime": return Tier.Runtime;
                case "Reject": return Tier.Reject;
                default: throw new FormatException("rule " + id + " has unknown tier '" + text + "'");
            }
        }
    }
}
