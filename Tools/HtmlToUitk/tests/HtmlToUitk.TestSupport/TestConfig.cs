using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.TestSupport;

public static class TestConfig
{
    public static ConversionConfig Load(string json)
    {
        JsonObject root = JsonReader.Parse(json).AsObject();
        var c = new ConversionConfig();
        if (root == null) return c;
        c.ProjectRoot = root.GetString("projectRoot", c.ProjectRoot);
        c.UiRoot = root.GetString("uiRoot", c.UiRoot);
        JsonObject loc = root.GetObject("localization");
        if (loc != null)
        {
            var l = c.Localization;
            c.Localization = new LocalizationConfig
            {
                Files = loc.GetStringList("files") ?? l.Files,
                Format = loc.GetString("format", l.Format),
                Locales = loc.GetStringList("locales") ?? l.Locales,
                MockLocale = loc.GetString("mockLocale", l.MockLocale),
                KeyPattern = loc.GetString("keyPattern", l.KeyPattern),
                Placeholder = loc.GetString("placeholder", l.Placeholder),
                AllowGlyphOnly = loc.GetBool("allowGlyphOnly", l.AllowGlyphOnly),
                KeysStub = loc.GetString("keysStub", l.KeysStub),
            };
        }
        JsonObject naming = root.GetObject("naming");
        if (naming != null)
        {
            var n = c.Naming;
            c.Naming = new NamingConfig
            {
                NamePattern = naming.GetString("namePattern", n.NamePattern),
                ClassPattern = naming.GetString("classPattern", n.ClassPattern),
                Prefixes = naming.GetStringList("prefixes") ?? n.Prefixes,
                DynamicClassPattern = naming.GetString("dynamicClassPattern", n.DynamicClassPattern),
                DynamicClasses = naming.GetStringList("dynamicClasses") ?? n.DynamicClasses,
            };
        }
        JsonObject rules = root.GetObject("rules");
        if (rules != null)
        {
            var r = c.Rules;
            c.Rules = new RulesConfig
            {
                MinFontSize = rules.GetDouble("minFontSize", r.MinFontSize),
                MinTouchTarget = rules.GetDouble("minTouchTarget", r.MinTouchTarget),
                UndefinedClass = rules.GetString("undefinedClass", r.UndefinedClass),
                ColorLiterals = rules.GetString("colorLiterals", r.ColorLiterals),
                StrictWarnings = rules.GetBool("strictWarnings", r.StrictWarnings),
                Disable = rules.GetStringList("disable") ?? r.Disable,
                AllowLayoutAnimation = rules.GetBool("allowLayoutAnimation", r.AllowLayoutAnimation),
                MaxKeyframeSteps = rules.GetInt("maxKeyframeSteps", r.MaxKeyframeSteps),
            };
        }
        JsonObject fonts = root.GetObject("fonts");
        if (fonts != null)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> member in fonts.Members) map[member.Key] = member.Value.AsString() ?? "";
            c.Fonts = map;
        }
        JsonObject uxml = root.GetObject("uxml");
        if (uxml != null)
        {
            var u = c.Uxml;
            c.Uxml = new UxmlConfig
            {
                Indent = uxml.GetInt("indent", u.Indent),
                Namespaces = uxml.GetStringList("namespaces") ?? u.Namespaces,
                InteractiveAttributes = u.InteractiveAttributes,
                AllowInlineStyles = uxml.GetStringList("allowInlineStyles") ?? u.AllowInlineStyles,
                TemplateSrcStyle = uxml.GetString("templateSrcStyle", u.TemplateSrcStyle),
            };
        }
        JsonObject emulation = root.GetObject("emulation");
        if (emulation != null)
        {
            var e = c.Emulation;
            c.Emulation = new EmulationConfig
            {
                StrictNative = emulation.GetBool("strictNative", e.StrictNative),
                AllowRuntime = emulation.GetBool("allowRuntime", e.AllowRuntime),
                Disable = emulation.GetStringList("disable") ?? e.Disable,
                GridMaxTracks = emulation.GetInt("gridMaxTracks", e.GridMaxTracks),
                BakedTextureFolder = emulation.GetString("bakedTextureFolder", e.BakedTextureFolder),
            };
        }
        return c;
    }
}
