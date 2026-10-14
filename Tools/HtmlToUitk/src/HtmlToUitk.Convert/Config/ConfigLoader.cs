using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Json;

namespace HtmlToUitk.Convert
{
    public static class ConfigLoader
    {
        public static ConversionConfig Load(string json)
        {
            return Apply(new ConversionConfig(), JsonReader.Parse(json) as JsonObject);
        }

        public static ConversionConfig Apply(ConversionConfig config, JsonObject root)
        {
            if (config == null) config = new ConversionConfig();
            ConfigExtras extras = ConfigExtras.For(config);
            if (root == null)
            {
                ConfigExtras.Set(config, extras);
                return config;
            }

            config.ProjectRoot = root.GetString("projectRoot", config.ProjectRoot);
            config.UiRoot = root.GetString("uiRoot", config.UiRoot);
            JsonObject layout = root.GetObject("layout");
            if (layout != null)
            {
                config.Layout.Screens = layout.GetString("screens", config.Layout.Screens);
                config.Layout.Skin = layout.GetString("skin", config.Layout.Skin);
                config.Layout.Strings = layout.GetString("strings", config.Layout.Strings);
                config.Layout.Shared = layout.GetString("shared", config.Layout.Shared);
                config.Layout.Generated = layout.GetString("generated", config.Layout.Generated);
            }

            JsonObject unity = root.GetObject("unity");
            if (unity != null)
            {
                config.Unity.Version = unity.GetString("version", config.Unity.Version);
                config.Unity.Capabilities = unity.GetString("capabilities", config.Unity.Capabilities);
                if (unity.Has("panelSettings")) config.Unity.PanelSettings = unity["panelSettings"].AsString();
                JsonArray resolution = unity.GetArray("referenceResolution");
                if (resolution != null && resolution.Count >= 2)
                {
                    int w = resolution[0].AsInt() ?? 1920;
                    int h = resolution[1].AsInt() ?? 1080;
                    config.Unity.ReferenceResolution = new[] { w, h };
                    extras.ReferenceWidth = w;
                    extras.ReferenceHeight = h;
                }
            }

            JsonObject loc = root.GetObject("localization");
            if (loc != null)
            {
                config.Localization.Files = loc.GetStringList("files") ?? config.Localization.Files;
                config.Localization.Format = loc.GetString("format", config.Localization.Format);
                config.Localization.Locales = loc.GetStringList("locales") ?? config.Localization.Locales;
                config.Localization.MockLocale = loc.GetString("mockLocale", config.Localization.MockLocale);
                config.Localization.KeyPattern = loc.GetString("keyPattern", config.Localization.KeyPattern);
                config.Localization.Placeholder = loc.GetString("placeholder", config.Localization.Placeholder);
                config.Localization.AllowGlyphOnly = loc.GetBool("allowGlyphOnly", config.Localization.AllowGlyphOnly);
                config.Localization.KeysStub = loc.GetString("keysStub", config.Localization.KeysStub);
            }

            JsonObject naming = root.GetObject("naming");
            if (naming != null)
            {
                config.Naming.NamePattern = naming.GetString("namePattern", config.Naming.NamePattern);
                config.Naming.ClassPattern = naming.GetString("classPattern", config.Naming.ClassPattern);
                config.Naming.Prefixes = naming.GetStringList("prefixes") ?? config.Naming.Prefixes;
                config.Naming.DynamicClassPattern = naming.GetString("dynamicClassPattern", config.Naming.DynamicClassPattern);
                config.Naming.DynamicClasses = naming.GetStringList("dynamicClasses") ?? config.Naming.DynamicClasses;
            }

            JsonObject rules = root.GetObject("rules");
            if (rules != null)
            {
                config.Rules.MinFontSize = rules.GetDouble("minFontSize", config.Rules.MinFontSize);
                config.Rules.MinTouchTarget = rules.GetDouble("minTouchTarget", config.Rules.MinTouchTarget);
                config.Rules.UndefinedClass = rules.GetString("undefinedClass", config.Rules.UndefinedClass);
                config.Rules.ColorLiterals = rules.GetString("colorLiterals", config.Rules.ColorLiterals);
                config.Rules.StrictWarnings = rules.GetBool("strictWarnings", config.Rules.StrictWarnings);
                config.Rules.Disable = rules.GetStringList("disable") ?? config.Rules.Disable;
                config.Rules.AllowLayoutAnimation = rules.GetBool("allowLayoutAnimation", config.Rules.AllowLayoutAnimation);
                config.Rules.MaxKeyframeSteps = rules.GetInt("maxKeyframeSteps", config.Rules.MaxKeyframeSteps);
            }

            JsonObject fonts = root.GetObject("fonts");
            if (fonts != null)
            {
                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, JsonValue> member in fonts.Members)
                {
                    string value = member.Value.AsString();
                    if (value != null) map[member.Key] = value;
                }

                config.Fonts = map;
            }

            JsonObject uxml = root.GetObject("uxml");
            if (uxml != null)
            {
                config.Uxml.Indent = uxml.GetInt("indent", config.Uxml.Indent);
                config.Uxml.Namespaces = uxml.GetStringList("namespaces") ?? config.Uxml.Namespaces;
                JsonObject interactive = uxml.GetObject("interactiveAttributes");
                if (interactive != null)
                {
                    var map = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, JsonValue> member in interactive.Members)
                    {
                        string value = member.Value.AsString();
                        if (value != null) map[member.Key] = value;
                    }

                    config.Uxml.InteractiveAttributes = map;
                }

                config.Uxml.AllowInlineStyles = uxml.GetStringList("allowInlineStyles") ?? config.Uxml.AllowInlineStyles;
                config.Uxml.TemplateSrcStyle = uxml.GetString("templateSrcStyle", config.Uxml.TemplateSrcStyle);
            }

            JsonObject uss = root.GetObject("uss");
            if (uss != null)
            {
                if (uss.Has("tokenScopeSelector")) config.Uss.TokenScopeSelector = uss["tokenScopeSelector"].AsString();
                config.Uss.RedefineSkinTokens = uss.GetBool("redefineSkinTokens", config.Uss.RedefineSkinTokens);
                config.Uss.PruneDeadRules = uss.GetBool("pruneDeadRules", config.Uss.PruneDeadRules);
                config.Uss.SectionOrder = uss.GetBool("sectionOrder", config.Uss.SectionOrder);
                config.Uss.UtilityPattern = uss.GetString("utilityPattern", config.Uss.UtilityPattern);
            }

            JsonObject output = root.GetObject("output");
            if (output != null) config.Output.TokensFile = output.GetString("tokensFile", config.Output.TokensFile);
            JsonObject emulation = root.GetObject("emulation");
            if (emulation != null)
            {
                config.Emulation.StrictNative = emulation.GetBool("strictNative", config.Emulation.StrictNative);
                config.Emulation.AllowRuntime = emulation.GetBool("allowRuntime", config.Emulation.AllowRuntime);
                config.Emulation.Disable = emulation.GetStringList("disable") ?? config.Emulation.Disable;
                config.Emulation.GridMaxTracks = emulation.GetInt("gridMaxTracks", config.Emulation.GridMaxTracks);
                config.Emulation.BakedTextureFolder = emulation.GetString("bakedTextureFolder", config.Emulation.BakedTextureFolder);
                extras.RootFontSize = emulation.GetDouble("rootFontSize", extras.RootFontSize);
                JsonArray resolution = emulation.GetArray("referenceResolution");
                if (resolution != null && resolution.Count >= 2)
                {
                    extras.ReferenceWidth = resolution[0].AsInt() ?? extras.ReferenceWidth;
                    extras.ReferenceHeight = resolution[1].AsInt() ?? extras.ReferenceHeight;
                }
            }

            JsonObject lint = root.GetObject("lint");
            if (lint != null)
            {
                config.Lint.Builtin = lint.GetBool("builtin", config.Lint.Builtin);
                JsonObject external = lint.GetObject("external");
                if (external != null)
                {
                    config.Lint.External = new ExternalLintConfig
                    {
                        Command = external.GetStringList("command") ?? new string[0],
                        Mirror = external.GetStringList("mirror") ?? new string[0],
                        Cwd = external.GetString("cwd", ".")
                    };
                }
            }

            JsonObject preview = root.GetObject("preview");
            if (preview != null)
            {
                config.Preview.Theme = preview.GetStringList("theme") ?? config.Preview.Theme;
                if (preview.Has("panelSettings")) config.Preview.PanelSettings = preview["panelSettings"].AsString();
                config.Preview.Viewports = preview.GetStringList("viewports") ?? config.Preview.Viewports;
                config.Preview.Backdrop = preview.GetString("backdrop", config.Preview.Backdrop);
                if (preview.Has("locale")) config.Preview.Locale = preview["locale"].AsString();
                config.Preview.DefaultTheme = preview.GetBool("defaultTheme", config.Preview.DefaultTheme);
                config.Preview.Transitions = preview.GetBool("transitions", config.Preview.Transitions);
                config.Preview.TextOverrides = StringMap(preview.GetObject("textOverrides")) ?? config.Preview.TextOverrides;
                config.Preview.Fragments = StringMap(preview.GetObject("fragments")) ?? config.Preview.Fragments;
            }

            JsonObject pages = root.GetObject("pages");
            if (pages != null)
            {
                config.Pages.Backend = pages.GetString("backend", config.Pages.Backend);
                JsonObject classes = pages.GetObject("classes");
                if (classes != null)
                {
                    config.Pages.HiddenClass = classes.GetString("hidden", config.Pages.HiddenClass);
                    config.Pages.ActiveClass = classes.GetString("active", config.Pages.ActiveClass);
                }
            }

            JsonObject animation = root.GetObject("animation");
            if (animation != null)
            {
                config.Animation.Backend = animation.GetString("backend", config.Animation.Backend);
                config.Animation.ReducedMotion = animation.GetString("reducedMotion", config.Animation.ReducedMotion);
            }

            ConfigExtras.Set(config, extras);
            return config;
        }

        private static IReadOnlyDictionary<string, string> StringMap(JsonObject obj)
        {
            if (obj == null) return null;
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> member in obj.Members)
            {
                string value = member.Value.AsString();
                if (value != null) map[member.Key] = value;
            }

            return map;
        }
    }

    public static class JobLoader
    {
        public static JobSpec Load(string json)
        {
            JsonObject root = JsonReader.Parse(json) as JsonObject;
            if (root == null) throw new FormatException("a job file must contain a JSON object");
            var spec = new JobSpec();
            spec.Schema = root.GetInt("schema", 1);
            spec.Name = root.GetString("name", null);
            spec.Config = root.GetString("config", null);
            JsonObject overrides = root.GetObject("configOverrides");
            if (overrides != null) spec.ConfigOverridesJson = JsonWriter.Write(overrides, JsonWriteOptions.Compact);
            JsonObject inputs = root.GetObject("inputs");
            if (inputs != null)
            {
                spec.Inputs.Html = inputs.GetStringList("html") ?? spec.Inputs.Html;
                spec.Inputs.Css = inputs.GetStringList("css") ?? spec.Inputs.Css;
                spec.Inputs.CssOrder = inputs.GetString("cssOrder", spec.Inputs.CssOrder);
                spec.Inputs.Uss = inputs.GetStringList("uss") ?? spec.Inputs.Uss;
                spec.Inputs.Partials = inputs.GetStringList("partials") ?? spec.Inputs.Partials;
            }

            JsonObject output = root.GetObject("output");
            if (output != null)
            {
                spec.Output.Root = output.GetString("root", spec.Output.Root);
                spec.Output.Assemble = output.GetString("assemble", spec.Output.Assemble);
                spec.Output.Partials = output.GetString("partials", spec.Output.Partials);
                JsonObject folders = output.GetObject("folders");
                if (folders != null)
                {
                    spec.Output.Folders.Uxml = folders.GetString("uxml", spec.Output.Folders.Uxml);
                    spec.Output.Folders.Uss = folders.GetString("uss", spec.Output.Folders.Uss);
                    spec.Output.Folders.Shared = folders.GetString("shared", spec.Output.Folders.Shared);
                    spec.Output.Folders.Tokens = folders.GetString("tokens", spec.Output.Folders.Tokens);
                    spec.Output.Folders.Strings = folders.GetString("strings", spec.Output.Folders.Strings);
                    spec.Output.Folders.Generated = folders.GetString("generated", spec.Output.Folders.Generated);
                    spec.Output.Folders.Components = folders.GetString("components", spec.Output.Folders.Components);
                }

                JsonObject perFile = output.GetObject("perFile");
                if (perFile != null)
                {
                    var map = new Dictionary<string, JobFileOutput>(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, JsonValue> member in perFile.Members)
                    {
                        JsonObject item = member.Value as JsonObject;
                        if (item == null) continue;
                        map[member.Key] = new JobFileOutput
                        {
                            Name = item.GetString("name", null),
                            Folder = item.GetString("folder", null),
                            Assemble = item.GetString("assemble", null)
                        };
                    }

                    spec.Output.PerFile = map;
                }
            }

            JsonObject watch = root.GetObject("watch");
            if (watch != null)
            {
                spec.Watch.Enabled = watch.GetBool("enabled", spec.Watch.Enabled);
                spec.Watch.DebounceMs = watch.GetInt("debounceMs", spec.Watch.DebounceMs);
            }

            return spec;
        }
    }
}
