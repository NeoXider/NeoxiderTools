using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HtmlToUitk.Capabilities.Reader;

public sealed class ReadResult
{
    public string Json;
    public string UnityVersion;
    public IReadOnlyList<string> Warnings;
}

public static class CapabilityReader
{
    public const int SchemaVersion = 1;

    private sealed class Resolved
    {
        public IReadOnlyList<string> Members;
    }

    public static ReadResult Read(string managedDir, string unityVersion, ReaderConfig config, OverridesFile overrides)
    {
        var warnings = new List<string>();
        var assemblies = new List<MetadataAssembly>();
        try
        {
            foreach (var name in config.Assemblies)
            {
                var path = Path.Combine(managedDir, name);
                if (File.Exists(path))
                {
                    assemblies.Add(MetadataAssembly.Open(path));
                }
                else if (string.Equals(name, config.Primary, StringComparison.Ordinal))
                {
                    throw new FileNotFoundException("primary assembly not found: " + path);
                }
                else
                {
                    warnings.Add("assembly not found: " + name);
                }
            }

            var primary = assemblies.First(a => string.Equals(a.FileName, config.Primary, StringComparison.Ordinal));
            var used = new HashSet<string>(StringComparer.Ordinal) { primary.FileName };

            Resolved Resolve(SetSpec spec)
            {
                foreach (var asm in assemblies)
                {
                    var matches = asm.FindTypes(spec.Type);
                    if (matches.Count == 0)
                    {
                        continue;
                    }

                    var pick = matches
                        .OrderBy(m => m.FullName.StartsWith("UnityEngine.UIElements", StringComparison.Ordinal) ? 0 : 1)
                        .ThenBy(m => m.FullName, StringComparer.Ordinal)
                        .First();
                    if (matches.Count > 1)
                    {
                        warnings.Add("type " + spec.Type + " is ambiguous in " + asm.FileName + "; using " + pick.FullName);
                    }

                    used.Add(asm.FileName);
                    return new Resolved { Members = pick.Members };
                }

                warnings.Add("type not found: " + spec.Type);
                return null;
            }

            List<KeyValuePair<string, string>> Names(SetSpec spec, Resolved r, Func<string, string> derive)
            {
                var list = new List<KeyValuePair<string, string>>();
                if (r == null)
                {
                    return list;
                }

                foreach (var m in r.Members)
                {
                    if (spec.Skip.Contains(m))
                    {
                        continue;
                    }

                    if (spec.Only && !spec.Names.ContainsKey(m))
                    {
                        continue;
                    }

                    list.Add(new KeyValuePair<string, string>(m, spec.Names.TryGetValue(m, out var n) ? n : derive(m)));
                }

                return list;
            }

            var sets = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.Ordinal);
            foreach (var kv in config.Sets)
            {
                var r = Resolve(kv.Value);
                Func<string, string> derive = kv.Key == "properties"
                    ? (m => overrides.PropertyNames.TryGetValue(m, out var o) ? o : Naming.PropertyName(m))
                    : Naming.Kebab;
                sets[kv.Key] = Names(kv.Value, r, derive);
            }

            var values = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var ve in config.ValueEnums)
            {
                var r = Resolve(ve.Set);
                var list = Names(ve.Set, r, Naming.Kebab).Select(p => p.Value).ToList();
                list.AddRange(ve.Set.Add);
                values[ve.Property] = list;
            }

            var properties = new Dictionary<string, JObj>(StringComparer.Ordinal);
            if (sets.TryGetValue("properties", out var propNames))
            {
                foreach (var pair in propNames)
                {
                    var name = pair.Value;
                    if (!primary.HasUserString(name))
                    {
                        warnings.Add("property " + name + " (member " + pair.Key + ") is not in the user-string heap");
                    }

                    if (!overrides.Properties.TryGetValue(name, out var po))
                    {
                        warnings.Add("property " + name + " has no entry in the overrides file");
                        po = new PropertyOverride();
                    }

                    properties[name] = BuildProperty(name, pair.Key, po, values, po.Documented ? "assembly+manual" : "assembly-only");
                }
            }

            foreach (var kv in overrides.Properties)
            {
                if (properties.ContainsKey(kv.Key))
                {
                    continue;
                }

                warnings.Add("property " + kv.Key + " is in the overrides file but not in the assembly");
                properties[kv.Key] = BuildProperty(kv.Key, Naming.PascalCase(kv.Key), kv.Value, values, "manual");
            }

            foreach (var name in values.Keys)
            {
                if (!properties.ContainsKey(name))
                {
                    warnings.Add("value enum for unknown property " + name);
                }
            }

            var propertiesObj = new JObj();
            var shorthands = new JObj();
            foreach (var name in properties.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                propertiesObj.Add(name, properties[name]);
                if (overrides.Properties.TryGetValue(name, out var po) && po.Longhands.Count > 0)
                {
                    shorthands.Add(name, po.Longhands.Cast<object>().ToList());
                }
            }

            var selectors = new JObj()
                .Add("combinators", Values(sets, "combinators").OrderBy(x => x, StringComparer.Ordinal).Cast<object>().ToList())
                .Add("types", Values(sets, "selectorTypes").Cast<object>().ToList())
                .Add("pseudoClasses", Values(sets, "pseudoClasses").Concat(overrides.ManualPseudoClasses).Distinct().Cast<object>().ToList());

            var functionMap = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var f in Values(sets, "functions"))
            {
                var evidence = overrides.ManualFunctions.Contains(f) ? "assembly+manual" : "assembly-only";
                functionMap[f] = new JObj().Add("evidence", evidence).Inlined();
            }

            foreach (var f in overrides.ManualFunctions)
            {
                if (!functionMap.ContainsKey(f))
                {
                    functionMap[f] = new JObj().Add("evidence", "manual-only").Inlined();
                }
            }

            foreach (var f in overrides.AbsentFunctions)
            {
                if (!functionMap.ContainsKey(f))
                {
                    functionMap[f] = false;
                }
            }

            var functions = new JObj();
            foreach (var f in functionMap.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                functions.Add(f, functionMap[f]);
            }

            var easings = Values(sets, "easings");
            foreach (var setName in new[] { "easings", "filters", "functions", "pseudoClasses", "keywords" })
            {
                foreach (var e in Values(sets, setName))
                {
                    if (!primary.HasUserString(e))
                    {
                        warnings.Add(setName + " name " + e + " is not in the user-string heap");
                    }
                }
            }

            var keywords = Values(sets, "keywords")
                .Concat(overrides.ManualKeywords)
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            var units = new JObj();
            foreach (var kv in overrides.Units.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                units.Add(kv.Key, kv.Value.Cast<object>().ToList());
            }

            var conditions = new JObj();
            foreach (var kv in overrides.Conditions.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                conditions.Add(kv.Key, kv.Value);
            }

            var generatedFrom = new List<object>();
            foreach (var asm in assemblies)
            {
                if (used.Contains(asm.FileName))
                {
                    generatedFrom.Add(new JObj().Add("assembly", asm.FileName).Add("sha256", asm.Sha256).Inlined());
                }
            }

            var doc = new JObj()
                .Add("schema", SchemaVersion)
                .Add("unityVersion", unityVersion)
                .Add("generatedFrom", generatedFrom)
                .Add("properties", propertiesObj)
                .Add("shorthands", shorthands)
                .Add("selectors", selectors)
                .Add("functions", functions)
                .Add("keywords", keywords.Cast<object>().ToList())
                .Add("filters", Values(sets, "filters").Cast<object>().ToList())
                .Add("easings", easings.Cast<object>().ToList())
                .Add("units", units)
                .Add("conditions", conditions)
                .Add("warnings", warnings.Cast<object>().ToList());

            return new ReadResult { Json = JsonText.Write(doc), UnityVersion = unityVersion, Warnings = warnings };
        }
        finally
        {
            foreach (var a in assemblies)
            {
                a.Dispose();
            }
        }
    }

    private static List<string> Values(Dictionary<string, List<KeyValuePair<string, string>>> sets, string key)
    {
        return sets.TryGetValue(key, out var list) ? list.Select(p => p.Value).ToList() : new List<string>();
    }

    private static JObj BuildProperty(string name, string id, PropertyOverride po, Dictionary<string, List<string>> values, string evidence)
    {
        var obj = new JObj().Inlined()
            .Add("id", id)
            .Add("inherited", po.Inherited)
            .Add("animatable", po.Animatable);
        if (values.TryGetValue(name, out var list))
        {
            obj.Add("values", list.Cast<object>().ToList());
        }

        obj.Add("evidence", evidence);
        return obj;
    }
}
