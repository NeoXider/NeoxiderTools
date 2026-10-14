using System;
using System.Collections.Generic;

namespace HtmlToUitk.Capabilities.Model
{
    public enum Animatable { None, Discrete, Full }

    public sealed class PropertyCapability
    {
        public string Name, Id;
        public bool Inherited;
        public Animatable Animatable;
        public IReadOnlyList<string> Values = Array.Empty<string>();
        public IReadOnlyList<string> Longhands = Array.Empty<string>();
        public string Evidence;
    }

    public sealed class CapabilityData
    {
        public string UnityVersion;
        public IReadOnlyDictionary<string, PropertyCapability> Properties = new Dictionary<string, PropertyCapability>(0);
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Shorthands = new Dictionary<string, IReadOnlyList<string>>(0);
        public IReadOnlyList<string> Combinators = Array.Empty<string>();
        public IReadOnlyList<string> SelectorTypes = Array.Empty<string>();
        public IReadOnlyList<string> PseudoClasses = Array.Empty<string>();
        public IReadOnlyDictionary<string, bool> Functions = new Dictionary<string, bool>(0);
        public IReadOnlyDictionary<string, string> FunctionEvidence = new Dictionary<string, string>(0);
        public IReadOnlyList<string> Keywords = Array.Empty<string>();
        public IReadOnlyList<string> Filters = Array.Empty<string>();
        public IReadOnlyList<string> Easings = Array.Empty<string>();
        public IReadOnlyList<string> LengthUnits = Array.Empty<string>();
        public IReadOnlyList<string> AngleUnits = Array.Empty<string>();
        public IReadOnlyList<string> TimeUnits = Array.Empty<string>();
        public IReadOnlyDictionary<string, string> Conditions = new Dictionary<string, string>(0);
        public IReadOnlyList<string> Warnings = Array.Empty<string>();
    }

    public sealed class CapabilityMatrix
    {
        private readonly CapabilityData _data;
        private readonly HashSet<string> _pseudo;
        private readonly HashSet<string> _keywords;
        private readonly HashSet<string> _filters;
        private readonly HashSet<string> _easings;
        private readonly HashSet<string> _units;
        private readonly HashSet<string> _combinators;

        public CapabilityMatrix(CapabilityData data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _pseudo = new HashSet<string>(data.PseudoClasses, StringComparer.Ordinal);
            _keywords = new HashSet<string>(data.Keywords, StringComparer.Ordinal);
            _filters = new HashSet<string>(data.Filters, StringComparer.Ordinal);
            _easings = new HashSet<string>(data.Easings, StringComparer.Ordinal);
            _combinators = new HashSet<string>(data.Combinators, StringComparer.Ordinal);
            _units = new HashSet<string>(StringComparer.Ordinal);
            foreach (string unit in data.LengthUnits) _units.Add(unit);
            foreach (string unit in data.AngleUnits) _units.Add(unit);
            foreach (string unit in data.TimeUnits) _units.Add(unit);
        }

        public CapabilityData Data => _data;

        public string UnityVersion => _data.UnityVersion;

        public bool HasProperty(string name) => name != null && _data.Properties.ContainsKey(name);

        public PropertyCapability Property(string name)
        {
            PropertyCapability capability;
            return name != null && _data.Properties.TryGetValue(name, out capability) ? capability : null;
        }

        public bool HasPseudoClass(string name) => name != null && _pseudo.Contains(name);

        public bool HasCombinator(char c) => _combinators.Contains(c.ToString());

        public bool HasFunction(string name)
        {
            bool supported;
            return name != null && _data.Functions.TryGetValue(name, out supported) && supported;
        }

        public bool HasKeyword(string name) => name != null && _keywords.Contains(name);

        public bool HasFilter(string name) => name != null && _filters.Contains(name);

        public bool HasEasing(string name) => name != null && _easings.Contains(name);

        public bool HasUnit(string unit) => unit != null && _units.Contains(unit);

        public string FunctionEvidence(string name)
        {
            string evidence;
            return name != null && _data.FunctionEvidence.TryGetValue(name, out evidence) ? evidence : null;
        }

        public bool Supports(string capabilityKey)
        {
            if (capabilityKey == null) return false;
            int colon = capabilityKey.IndexOf(':');
            if (colon <= 0) return false;
            string kind = capabilityKey.Substring(0, colon);
            string name = capabilityKey.Substring(colon + 1);
            switch (kind)
            {
                case "css":
                    return HasProperty(name);
                case "uss":
                    return HasProperty(name) || HasFunction(name) || HasKeyword(name) || HasPseudoClass(name);
                case "property":
                    return HasProperty(name);
                case "function":
                    return HasFunction(name);
                case "keyword":
                    return HasKeyword(name);
                case "filter":
                    return HasFilter(name);
                case "easing":
                    return HasEasing(name);
                case "unit":
                    return HasUnit(name);
                case "pseudo":
                    return HasPseudoClass(name);
                case "combinator":
                    return name.Length == 1 && HasCombinator(name[0]);
                default:
                    return false;
            }
        }

        public string Condition(string key)
        {
            string condition;
            return key != null && _data.Conditions.TryGetValue(key, out condition) ? condition : null;
        }
    }
}
