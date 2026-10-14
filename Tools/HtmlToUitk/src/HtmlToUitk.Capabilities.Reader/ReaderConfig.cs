using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace HtmlToUitk.Capabilities.Reader;

public sealed class SetSpec
{
    public string Type;
    public bool Only;
    public List<string> Skip = new List<string>();
    public List<string> Add = new List<string>();
    public Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class ValueEnumSpec
{
    public string Property;
    public SetSpec Set;
}

public sealed class ReaderConfig
{
    public string Primary;
    public List<string> Assemblies = new List<string>();
    public Dictionary<string, SetSpec> Sets = new Dictionary<string, SetSpec>(StringComparer.Ordinal);
    public List<ValueEnumSpec> ValueEnums = new List<ValueEnumSpec>();

    public static ReaderConfig Default()
    {
        var asm = typeof(ReaderConfig).Assembly;
        using var stream = asm.GetManifestResourceStream("reader-config.json");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static ReaderConfig Load(string path)
    {
        return Parse(File.ReadAllText(path));
    }

    public static ReaderConfig Parse(string json)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow });
        var root = doc.RootElement;
        var cfg = new ReaderConfig { Primary = root.GetProperty("primary").GetString() };
        foreach (var a in root.GetProperty("assemblies").EnumerateArray())
        {
            cfg.Assemblies.Add(a.GetString());
        }

        foreach (var p in root.GetProperty("sets").EnumerateObject())
        {
            cfg.Sets[p.Name] = ParseSet(p.Value);
        }

        if (root.TryGetProperty("valueEnums", out var ve))
        {
            foreach (var e in ve.EnumerateArray())
            {
                cfg.ValueEnums.Add(new ValueEnumSpec { Property = e.GetProperty("property").GetString(), Set = ParseSet(e) });
            }
        }

        return cfg;
    }

    private static SetSpec ParseSet(JsonElement e)
    {
        var s = new SetSpec { Type = e.GetProperty("type").GetString() };
        if (e.TryGetProperty("only", out var only))
        {
            s.Only = only.GetBoolean();
        }

        if (e.TryGetProperty("skip", out var skip))
        {
            foreach (var x in skip.EnumerateArray())
            {
                s.Skip.Add(x.GetString());
            }
        }

        if (e.TryGetProperty("add", out var add))
        {
            foreach (var x in add.EnumerateArray())
            {
                s.Add.Add(x.GetString());
            }
        }

        if (e.TryGetProperty("names", out var names))
        {
            foreach (var n in names.EnumerateObject())
            {
                s.Names[n.Name] = n.Value.GetString();
            }
        }

        return s;
    }
}
