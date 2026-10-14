using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HtmlToUitk.TestSupport;

public static class MiniJsonSchema
{
    public static IReadOnlyList<string> Validate(string schemaJson, string instanceJson)
    {
        using var schema = JsonDocument.Parse(schemaJson);
        using var instance = JsonDocument.Parse(instanceJson);
        var errors = new List<string>();
        Check(schema.RootElement, schema.RootElement, instance.RootElement, "$", errors);
        return errors;
    }

    private static JsonElement Resolve(JsonElement root, JsonElement schema)
    {
        while (schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("$ref", out var r))
        {
            var path = r.GetString();
            if (path == null || !path.StartsWith("#/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("unsupported $ref " + path);
            }

            var node = root;
            foreach (var part in path.Substring(2).Split('/'))
            {
                node = node.GetProperty(part);
            }

            schema = node;
        }

        return schema;
    }

    private static void Check(JsonElement root, JsonElement schemaIn, JsonElement value, string path, List<string> errors)
    {
        var schema = Resolve(root, schemaIn);
        if (schema.ValueKind == JsonValueKind.True)
        {
            return;
        }

        if (schema.ValueKind == JsonValueKind.False)
        {
            errors.Add(path + ": not allowed");
            return;
        }

        if (schema.TryGetProperty("anyOf", out var anyOf))
        {
            var ok = false;
            foreach (var option in anyOf.EnumerateArray())
            {
                var sub = new List<string>();
                Check(root, option, value, path, sub);
                if (sub.Count == 0)
                {
                    ok = true;
                    break;
                }
            }

            if (!ok)
            {
                errors.Add(path + ": matches no anyOf option");
            }
        }

        if (schema.TryGetProperty("oneOf", out var oneOf))
        {
            var matches = 0;
            foreach (var option in oneOf.EnumerateArray())
            {
                var sub = new List<string>();
                Check(root, option, value, path, sub);
                if (sub.Count == 0)
                {
                    matches++;
                }
            }

            if (matches != 1)
            {
                errors.Add(path + ": matches " + matches + " oneOf options");
            }
        }

        if (schema.TryGetProperty("const", out var c) && !Same(c, value))
        {
            errors.Add(path + ": expected const " + c.GetRawText());
        }

        if (schema.TryGetProperty("enum", out var en) && !en.EnumerateArray().Any(e => Same(e, value)))
        {
            errors.Add(path + ": value " + value.GetRawText() + " not in enum");
        }

        if (schema.TryGetProperty("type", out var type))
        {
            var types = type.ValueKind == JsonValueKind.Array ? type.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string> { type.GetString() };
            if (!types.Any(t => TypeMatches(t, value)))
            {
                errors.Add(path + ": expected type " + string.Join("|", types) + " but found " + value.ValueKind);
                return;
            }
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            if (schema.TryGetProperty("pattern", out var pattern) && !Regex.IsMatch(value.GetString(), pattern.GetString()))
            {
                errors.Add(path + ": string does not match " + pattern.GetString());
            }

            if (schema.TryGetProperty("minLength", out var ml) && value.GetString().Length < ml.GetInt32())
            {
                errors.Add(path + ": string shorter than " + ml.GetInt32());
            }
        }

        if (value.ValueKind == JsonValueKind.Number && schema.TryGetProperty("minimum", out var min) && value.GetDouble() < min.GetDouble())
        {
            errors.Add(path + ": number below minimum");
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            var items = value.EnumerateArray().ToList();
            if (schema.TryGetProperty("minItems", out var mi) && items.Count < mi.GetInt32())
            {
                errors.Add(path + ": fewer than " + mi.GetInt32() + " items");
            }

            if (schema.TryGetProperty("items", out var itemSchema))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    Check(root, itemSchema, items[i], path + "[" + i + "]", errors);
                }
            }
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var present = value.EnumerateObject().Select(p => p.Name).ToList();
            if (schema.TryGetProperty("minProperties", out var mp) && present.Count < mp.GetInt32())
            {
                errors.Add(path + ": fewer than " + mp.GetInt32() + " properties");
            }

            if (schema.TryGetProperty("required", out var req))
            {
                foreach (var r in req.EnumerateArray())
                {
                    if (!present.Contains(r.GetString()))
                    {
                        errors.Add(path + ": missing required " + r.GetString());
                    }
                }
            }

            var declared = schema.TryGetProperty("properties", out var props) ? props : default;
            foreach (var p in value.EnumerateObject())
            {
                if (declared.ValueKind == JsonValueKind.Object && declared.TryGetProperty(p.Name, out var ps))
                {
                    Check(root, ps, p.Value, path + "." + p.Name, errors);
                }
                else if (schema.TryGetProperty("additionalProperties", out var ap))
                {
                    if (ap.ValueKind == JsonValueKind.False)
                    {
                        errors.Add(path + ": unexpected property " + p.Name);
                    }
                    else if (ap.ValueKind == JsonValueKind.Object)
                    {
                        Check(root, ap, p.Value, path + "." + p.Name, errors);
                    }
                }
            }
        }
    }

    private static bool TypeMatches(string type, JsonElement v)
    {
        switch (type)
        {
            case "object": return v.ValueKind == JsonValueKind.Object;
            case "array": return v.ValueKind == JsonValueKind.Array;
            case "string": return v.ValueKind == JsonValueKind.String;
            case "boolean": return v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False;
            case "null": return v.ValueKind == JsonValueKind.Null;
            case "number": return v.ValueKind == JsonValueKind.Number;
            case "integer": return v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out _);
            default: throw new InvalidOperationException("unsupported type " + type);
        }
    }

    private static bool Same(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
        {
            return false;
        }

        switch (a.ValueKind)
        {
            case JsonValueKind.String: return a.GetString() == b.GetString();
            case JsonValueKind.Number: return a.GetDouble() == b.GetDouble();
            default: return a.GetRawText() == b.GetRawText();
        }
    }
}
