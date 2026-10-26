using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Css;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert
{
    internal static class Assembler
    {
        private sealed class SharedUse
        {
            public PageOutput Page;
            public SheetOutput Sheet;
            public string Text;
        }

        public static TransformedModel Assemble(List<PageOutput> outputs, StageContext context, PipelineState state, TransformExtras extras)
        {
            ConversionConfig config = context.Config;
            JobSpec spec = state.Job.Spec;
            JobFolders folders = spec.Output.Folders;
            var sheetEntries = new List<SheetEntry>();
            var assets = new List<GeneratedAsset>();
            var documents = new List<UiDocument>();
            var runtime = new List<string>();
            var names = AssignNames(outputs, spec, context);
            string uxmlRoot = PathUtil.Normalize(spec.Output.Root ?? "");
            var sharedUses = new Dictionary<string, List<SharedUse>>(StringComparer.Ordinal);
            foreach (PageOutput output in outputs)
            {
                string name = names[output];
                string fileFolder = FolderFor(spec, output.Page.Input.Path);
                string uxmlFolder = fileFolder ?? folders.Uxml;
                string ussFolder = fileFolder ?? folders.Uss;
                foreach (SheetOutput sheet in output.Sheets)
                {
                    if (sheet.Role != SheetRole.Shared) continue;
                    string text = RenderSheet(sheet, null, config, folders.Shared, output);
                    List<SharedUse> list;
                    if (!sharedUses.TryGetValue(sheet.GroupId, out list))
                    {
                        list = new List<SharedUse>();
                        sharedUses[sheet.GroupId] = list;
                    }

                    list.Add(new SharedUse { Page = output, Sheet = sheet, Text = text });
                }
            }

            Dictionary<string, string> sharedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<PageOutput, Dictionary<string, string>> pageSharedPaths = new Dictionary<PageOutput, Dictionary<string, string>>();
            HashSet<string> reservedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PageOutput output in outputs)
            {
                string folder = FolderFor(spec, output.Page.Input.Path);
                reservedPaths.Add(PathUtil.Combine(folder ?? folders.Uss, names[output] + ".uss"));
            }
            Dictionary<string, string> emittedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<string> sharedGroups = new List<string>(sharedUses.Keys);
            sharedGroups.Sort(StringComparer.Ordinal);
            foreach (string group in sharedGroups)
            {
                List<SharedUse> uses = sharedUses[group];
                string first = uses[0].Text;
                bool same = true;
                foreach (SharedUse use in uses)
                {
                    if (use.Text != first) same = false;
                }

                if (same)
                {
                    string path = PathUtil.Combine(folders.Shared, PathUtil.Stem(uses[0].Sheet.SourcePath) + ".uss");
                    sharedPaths[group] = ReservePath(path, reservedPaths);
                }
            }

            foreach (PageOutput output in outputs)
            {
                string name = names[output];
                PageState page = output.Page;
                string fileFolder = FolderFor(spec, page.Input.Path);
                string uxmlFolder = fileFolder ?? folders.Uxml;
                string ussFolder = fileFolder ?? folders.Uss;
                DocumentExtras extra = output.Extras;
                extra.Name = name;
                extra.UxmlFolder = uxmlFolder;
                extra.UxmlPath = PathUtil.Combine(uxmlFolder, name + ".uxml");
                string specBase = PathUtil.Combine(uxmlFolder, name);
                string tokensPath = null;
                if (output.NewTokens.Count > 0)
                {
                    tokensPath = config.Output.TokensFile
                        .Replace("{skin}", config.Layout.Skin).Replace("{screen}", name).Replace("{screens}", config.Layout.Screens).Replace("{strings}", config.Layout.Strings);
                    tokensPath = PathUtil.Normalize(tokensPath);
                }

                extra.TokensPath = tokensPath;
                string pageUssPath = PathUtil.Combine(ussFolder, name + ".uss");
                var styles = new List<StyleRef>();
                bool tokensAdded = false;
                var pageSheetsByGroup = new Dictionary<string, SheetOutput>(StringComparer.Ordinal);
                foreach (SheetOutput s in output.Sheets) pageSheetsByGroup[s.GroupId] = s;
                string uxmlDir = uxmlFolder;
                var refGroups = new HashSet<string>(StringComparer.Ordinal);
                foreach (Stylesheet sheet in page.Cascade)
                {
                    string src = null;
                    if (sheet.Origin == StyleOrigin.Skin)
                    {
                        foreach (SkinRef skin in page.Skins)
                        {
                            if (ReferenceEquals(skin.Sheet, sheet)) src = skin.Src;
                        }
                    }
                    else if (sheet.Origin == StyleOrigin.UssPassThrough)
                    {
                        string target = sheet.Path;
                        src = PathUtil.RelativeTo(PathUtil.Combine(uxmlRoot, uxmlDir), target);
                    }
                    else
                    {
                        string groupId = sheet.Origin == StyleOrigin.Page ? "page:" + page.Input.Path : "css:" + sheet.Path;
                        if (!refGroups.Add(groupId)) continue;
                        if (!tokensAdded && tokensPath != null)
                        {
                            styles.Add(new StyleRef { Src = PathUtil.RelativeTo(uxmlDir, tokensPath) });
                            tokensAdded = true;
                        }

                        string path;
                        if (sheet.Origin == StyleOrigin.Page) path = pageUssPath;
                        else path = SharedPath(output, groupId, PathUtil.Combine(folders.Shared, PathUtil.Stem(sheet.Path) + "." + name + ".uss"), sharedPaths, pageSharedPaths, reservedPaths);
                        src = PathUtil.RelativeTo(uxmlDir, path);
                    }

                    if (src != null) styles.Add(new StyleRef { Src = src });
                }

                if (!tokensAdded && tokensPath != null) styles.Add(new StyleRef { Src = PathUtil.RelativeTo(uxmlDir, tokensPath) });
                if (!refGroups.Contains("page:" + page.Input.Path)) styles.Add(new StyleRef { Src = PathUtil.RelativeTo(uxmlDir, pageUssPath) });
                output.Document.Name = name;
                output.Document.Styles = styles;
                documents.Add(output.Document);
                foreach (SheetOutput sheet in output.Sheets)
                {
                    if (sheet.Role == SheetRole.Page)
                    {
                        string text = null;
                        var entry = new SheetEntry { Id = sheet.GroupId, RelativePath = pageUssPath, Role = SheetRole.Page, Sheet = ToStylesheet(sheet, pageUssPath, ScopedBlock(output, config)) };
                        sheetEntries.Add(entry);
                    }
                    else
                    {
                        string path;
                        path = SharedPath(output, sheet.GroupId, PathUtil.Combine(folders.Shared, PathUtil.Stem(sheet.SourcePath) + "." + name + ".uss"), sharedPaths, pageSharedPaths, reservedPaths);
                        if (emittedPaths.ContainsKey(path)) continue;
                        emittedPaths[path] = sheet.GroupId;
                        sheetEntries.Add(new SheetEntry { Id = sheet.GroupId, RelativePath = path, Role = SheetRole.Shared, Sheet = ToStylesheet(sheet, path, null) });
                    }
                }

                if (!hasPageSheet(output)) sheetEntries.Add(new SheetEntry { Id = "page:" + page.Input.Path, RelativePath = pageUssPath, Role = SheetRole.Page, Sheet = ToStylesheet(new SheetOutput { GroupId = "page:" + page.Input.Path, Role = SheetRole.Page }, pageUssPath, ScopedBlock(output, config)) });
                if (tokensPath != null)
                {
                    var tokenRule = new StyleRule
                    {
                        Selectors = new[] { SelectorFormat.Parse(":root", SourceSpan.None) },
                        Declarations = ToDeclarations(output.NewTokens)
                    };
                    var tokenSheet = new Stylesheet { Path = tokensPath, Origin = StyleOrigin.Page, Rules = new[] { tokenRule } };
                    sheetEntries.Add(new SheetEntry { Id = "tokens:" + page.Input.Path, RelativePath = tokensPath, Role = SheetRole.Tokens, Sheet = tokenSheet });
                }

                foreach (GeneratedAsset asset in output.Assets.Assets) assets.Add(asset);
                foreach (string component in output.RuntimeComponents)
                {
                    if (!runtime.Contains(component)) runtime.Add(component);
                }

                if (output.Specs.Animations.Count > 0) extra.AnimPath = specBase + ".anim.json";
                if (output.Specs.Nav != null && output.Specs.Nav.Pages.Count > 0) extra.NavPath = specBase + ".nav.json";
                if (output.Specs.Media != null) extra.MediaPath = specBase + ".mq.json";
                if (extra.Runtime.Count > 0) extra.RuntimePath = specBase + ".runtime.json";
                if (extra.NewKeys.Count > 0)
                {
                    string stub = config.Localization.KeysStub.Replace("{screen}", name);
                    extra.KeysPath = PathUtil.Combine(folders.Strings, stub);
                }
            }

            var model = new TransformedModel
            {
                Documents = documents,
                Sheets = new SheetSet { Sheets = sheetEntries },
                Assets = assets,
                RuntimeComponents = runtime,
                Specs = new Specs
                {
                    Anim = outputs.Count > 0 ? outputs[0].Specs.Animations.Count > 0 ? new AnimModel { Animations = outputs[0].Specs.Animations, Bindings = outputs[0].Specs.Bindings } : null : null,
                    Nav = outputs.Count > 0 ? outputs[0].Specs.Nav : null,
                    Media = outputs.Count > 0 ? outputs[0].Specs.Media : null
                }
            };
            foreach (PageOutput output in outputs) extras.Documents[output.Page.Input.Path] = output.Extras;
            foreach (PageOutput output in outputs) extras.SpecsByDocument[output.Page.Input.Path] = output.Specs;
            return model;
        }

        private static bool hasPageSheet(PageOutput output)
        {
            foreach (SheetOutput s in output.Sheets)
            {
                if (s.Role == SheetRole.Page) return true;
            }

            return false;
        }

        private static string FolderFor(JobSpec spec, string inputPath)
        {
            JobFileOutput file;
            if (spec.Output.PerFile != null && spec.Output.PerFile.TryGetValue(inputPath, out file) && !string.IsNullOrEmpty(file.Folder)) return PathUtil.Normalize(file.Folder);
            return null;
        }

        private static Dictionary<PageOutput, string> AssignNames(List<PageOutput> outputs, JobSpec spec, StageContext context)
        {
            Dictionary<PageOutput, string> names = new Dictionary<PageOutput, string>();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<PageOutput> ordered = new List<PageOutput>(outputs);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Page.Input.Path, b.Page.Input.Path));
            foreach (PageOutput output in ordered)
            {
                string name = output.Page.ScreenName;
                JobFileOutput file;
                if (spec.Output.PerFile != null && spec.Output.PerFile.TryGetValue(output.Page.Input.Path, out file) && !string.IsNullOrEmpty(file.Name)) name = file.Name;
                string folder = FolderFor(spec, output.Page.Input.Path);
                string uxmlFolder = folder ?? spec.Output.Folders.Uxml;
                string ussFolder = folder ?? spec.Output.Folders.Uss;
                string original = name;
                int suffix = 2;
                while (used.Contains(PathUtil.Combine(uxmlFolder, name + ".uxml")) ||
                       used.Contains(PathUtil.Combine(ussFolder, name + ".uss")))
                {
                    name = original + "-" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                }

                used.Add(PathUtil.Combine(uxmlFolder, name + ".uxml"));
                used.Add(PathUtil.Combine(ussFolder, name + ".uss"));
                names[output] = name;
            }

            return names;
        }

        private static string ReservePath(string preferred, HashSet<string> reserved)
        {
            string path = PathUtil.Normalize(preferred);
            string candidate = path;
            int dot = path.LastIndexOf('.');
            string stem = dot > path.LastIndexOf('/') ? path.Substring(0, dot) : path;
            string extension = dot > path.LastIndexOf('/') ? path.Substring(dot) : string.Empty;
            int suffix = 2;
            while (!reserved.Add(candidate))
            {
                candidate = stem + "-" + suffix.ToString(CultureInfo.InvariantCulture) + extension;
                suffix++;
            }
            return candidate;
        }

        private static string SharedPath(PageOutput page, string group, string preferred,
            Dictionary<string, string> shared, Dictionary<PageOutput, Dictionary<string, string>> variants,
            HashSet<string> reserved)
        {
            if (shared.TryGetValue(group, out string common)) return common;
            if (!variants.TryGetValue(page, out Dictionary<string, string> paths))
            {
                paths = new Dictionary<string, string>(StringComparer.Ordinal);
                variants.Add(page, paths);
            }
            if (!paths.TryGetValue(group, out string path))
            {
                path = ReservePath(preferred, reserved);
                paths.Add(group, path);
            }
            return path;
        }

        private static IReadOnlyList<Declaration> ToDeclarations(List<KeyValuePair<string, string>> pairs)
        {
            var list = new List<Declaration>();
            foreach (KeyValuePair<string, string> pair in pairs) list.Add(new Declaration { Name = pair.Key, Value = new CssRaw(pair.Value) });
            return list;
        }

        private static StyleRule ScopedBlock(PageOutput output, ConversionConfig config)
        {
            string scope = config.Uss.TokenScopeSelector;
            if (string.IsNullOrEmpty(scope)) return null;
            var scoped = new List<KeyValuePair<string, string>>();
            if (config.Uss.RedefineSkinTokens)
            {
                foreach (KeyValuePair<string, string> skin in output.SkinTokens) Upsert(scoped, skin.Key, CssText.NormaliseTokenValue(skin.Value));
            }

            foreach (KeyValuePair<string, string> token in output.PageTokens) Upsert(scoped, token.Key, CssText.NormaliseTokenValue(token.Value));
            if (scoped.Count == 0) return null;
            return new StyleRule { Selectors = new[] { SelectorFormat.Parse(scope, SourceSpan.None) }, Declarations = ToDeclarations(scoped) };
        }

        private static void Upsert(List<KeyValuePair<string, string>> list, string key, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                {
                    list[i] = new KeyValuePair<string, string>(key, value);
                    return;
                }
            }

            list.Add(new KeyValuePair<string, string>(key, value));
        }

        private static Stylesheet ToStylesheet(SheetOutput sheet, string path, StyleRule scoped)
        {
            var rules = new List<StyleRule>();
            if (scoped != null) rules.Add(scoped);
            foreach (UssRule rule in sheet.Rules) rules.Add(ToStyleRule(rule));
            foreach (UssRule rule in sheet.Tail) rules.Add(ToStyleRule(rule));
            return new Stylesheet { Path = path, Origin = StyleOrigin.Page, Rules = rules };
        }

        private static StyleRule ToStyleRule(UssRule rule)
        {
            var decls = new List<Declaration>();
            foreach (UssDecl d in rule.Decls) decls.Add(new Declaration { Name = d.Name, Value = new CssRaw(d.Value), Span = d.Span });
            return new StyleRule { Selectors = rule.Selectors, Declarations = decls, Span = rule.Span, Order = rule.Order };
        }

        private static string RenderSheet(SheetOutput sheet, StyleRule scoped, ConversionConfig config, string folder, PageOutput output)
        {
            Stylesheet stylesheet = ToStylesheet(sheet, "", scoped);
            return Writers.UssText.FormatSheet(stylesheet, config.Uxml.Indent, folder);
        }
    }
}
