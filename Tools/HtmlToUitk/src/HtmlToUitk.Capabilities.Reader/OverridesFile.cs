using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace HtmlToUitk.Capabilities.Reader;

public sealed class PropertyOverride
{
    public bool Inherited;
    public string Animatable = "none";
    public bool Documented;
    public List<string> Longhands = new List<string>();
}

public sealed class OverridesFile
{
    public Dictionary<string, string> PropertyNames = new Dictionary<string, string>(StringComparer.Ordinal);
    public Dictionary<string, PropertyOverride> Properties = new Dictionary<string, PropertyOverride>(StringComparer.Ordinal);
    public List<string> ManualFunctions = new List<string>();
    public List<string> AbsentFunctions = new List<string>();
    public List<string> ManualPseudoClasses = new List<string>();
    public List<string> ManualKeywords = new List<string>();
    public Dictionary<string, List<string>> Units = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    public Dictionary<string, string> Conditions = new Dictionary<string, string>(StringComparer.Ordinal);

    public static OverridesFile Empty()
    {
        return new OverridesFile();
    }

    public static OverridesFile Load(string path)
    {
        return Parse(File.ReadAllText(path));
    }

    public static OverridesFile Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var o = new OverridesFile();
        if (root.TryGetProperty("propertyNames", out var pn))
        {
            foreach (var p in pn.EnumerateObject())
            {
                o.PropertyNames[p.Name] = p.Value.GetString();
            }
        }

        if (root.TryGetProperty("properties", out var props))
        {
            foreach (var p in props.EnumerateObject())
            {
                var v = p.Value;
                var po = new PropertyOverride
                {
                    Inherited = v.TryGetProperty("inherited", out var i) && i.GetBoolean(),
                    Animatable = v.TryGetProperty("animatable", out var a) ? a.GetString() : "none",
                    Documented = v.TryGetProperty("documented", out var d) && d.GetBoolean(),
                };
                if (v.TryGetProperty("longhands", out var lh))
                {
                    foreach (var x in lh.EnumerateArray())
                    {
                        po.Longhands.Add(x.GetString());
                    }
                }

                o.Properties[p.Name] = po;
            }
        }

        Fill(root, "manualFunctions", o.ManualFunctions);
        Fill(root, "absentFunctions", o.AbsentFunctions);
        Fill(root, "manualPseudoClasses", o.ManualPseudoClasses);
        Fill(root, "manualKeywords", o.ManualKeywords);
        if (root.TryGetProperty("units", out var units))
        {
            foreach (var u in units.EnumerateObject())
            {
                var list = new List<string>();
                foreach (var x in u.Value.EnumerateArray())
                {
                    list.Add(x.GetString());
                }

                o.Units[u.Name] = list;
            }
        }

        if (root.TryGetProperty("conditions", out var cond))
        {
            foreach (var c in cond.EnumerateObject())
            {
                o.Conditions[c.Name] = c.Value.GetString();
            }
        }

        return o;
    }

    private static void Fill(JsonElement root, string name, List<string> target)
    {
        if (!root.TryGetProperty(name, out var arr))
        {
            return;
        }

        foreach (var x in arr.EnumerateArray())
        {
            target.Add(x.GetString());
        }
    }
}
