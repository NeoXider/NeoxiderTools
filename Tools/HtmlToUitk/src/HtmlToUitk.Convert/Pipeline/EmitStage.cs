using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Bake;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Emulations;
using HtmlToUitk.Convert.Writers;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert
{
    public sealed class EmitStage : IPipelineStage<TransformedModel, OutputSet>
    {
        private readonly PipelineState _state;

        internal EmitStage(PipelineState state)
        {
            _state = state;
        }

        public string Name => "emit";

        private static OutputFile Text(string path, string text, FileKind kind)
        {
            return new OutputFile { RelativePath = PathUtil.Normalize(path), Content = new UTF8Encoding(false).GetBytes(text), Kind = kind };
        }

        public OutputSet Run(TransformedModel model, StageContext context)
        {
            ConversionConfig config = context.Config;
            TransformExtras extras = TransformExtras.For(model);
            List<OutputFile> files = new List<OutputFile>();
            OutputFile assetManifest = null;
            UxmlWriterOptions options = new UxmlWriterOptions { Indent = config.Uxml.Indent, Namespaces = config.Uxml.Namespaces };
            foreach (UiDocument document in model.Documents)
            {
                DocumentExtras extra = extras.Get(document.SourcePath);
                files.Add(Text(extra.UxmlPath, _state.Services.UxmlWriter.Write(document, options), FileKind.Uxml));
                if (!string.IsNullOrEmpty(extra.KeysPath)) files.Add(Text(extra.KeysPath, SpecWriters.KeysStub(extra, config), FileKind.Json));
                SpecSink specs;
                if (!extras.SpecsByDocument.TryGetValue(document.SourcePath, out specs)) continue;
                if (extra.AnimPath != null) files.Add(Text(extra.AnimPath, SpecWriters.Anim(new AnimModel { Animations = specs.Animations, Bindings = specs.Bindings }), FileKind.Json));
                if (extra.NavPath != null) files.Add(Text(extra.NavPath, SpecWriters.Nav(specs.Nav), FileKind.Json));
                if (extra.MediaPath != null) files.Add(Text(extra.MediaPath, SpecWriters.Media(specs.Media), FileKind.Json));
                if (extra.RuntimePath != null) files.Add(Text(extra.RuntimePath, SpecWriters.Runtime(extra.Runtime), FileKind.Json));
            }

            foreach (SheetEntry entry in model.Sheets.Sheets)
            {
                if (entry.Role == SheetRole.Skin || entry.Role == SheetRole.PassThrough) continue;
                string folder = PathUtil.Directory(entry.RelativePath);
                files.Add(Text(entry.RelativePath, UssText.FormatSheet(entry.Sheet, config.Uxml.Indent, folder), FileKind.Uss));
            }

            AssetSink generated = new AssetSink();
            foreach (GeneratedAsset asset in model.Assets) generated.Add(asset);
            foreach (GeneratedAsset asset in generated.Assets)
            {
                files.Add(new OutputFile { RelativePath = PathUtil.Normalize(asset.RelativePath), Content = asset.Content, Kind = FileKind.Png });
            }

            if (generated.Assets.Count > 0)
            {
                JsonObject manifest = new JsonObject();
                manifest.Set("schema", 1);
                JsonArray entries = new JsonArray();
                List<GeneratedAsset> sortedAssets = new List<GeneratedAsset>(generated.Assets);
                sortedAssets.Sort((left, right) => string.CompareOrdinal(left.RelativePath, right.RelativePath));
                HashSet<string> listed = new HashSet<string>(StringComparer.Ordinal);
                foreach (GeneratedAsset asset in sortedAssets)
                {
                    if (!listed.Add(asset.RelativePath)) continue;
                    RgbaImage image = PngDecoder.Decode(asset.Content);
                    JsonObject entry = new JsonObject();
                    entry.Set("path", PathUtil.Normalize(asset.RelativePath));
                    using (SHA256 hash = SHA256.Create()) entry.Set("sha256", BitConverter.ToString(hash.ComputeHash(asset.Content)).Replace("-", "").ToLowerInvariant());
                    entry.Set("width", image.Width); entry.Set("height", image.Height);
                    entry.Set("kind", "gradient"); entry.Set("sRGB", true); entry.Set("wrap", "clamp"); entry.Set("mipmaps", false);
                    entries.Add(entry);
                }
                manifest.Set("assets", entries);
                assetManifest = Text("generated-assets.json", JsonWriter.Write(manifest, JsonWriteOptions.Pretty), FileKind.Json);
                files.Add(assetManifest);
            }
            files.Sort((a, b) => string.CompareOrdinal(a.RelativePath, b.RelativePath));
            List<OutputFile> unique = new List<OutputFile>();
            Dictionary<string, OutputFile> seen = new Dictionary<string, OutputFile>(StringComparer.OrdinalIgnoreCase);
            foreach (OutputFile file in files)
            {
                if (string.Equals(file.RelativePath, "diagnostics.json", StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(file.RelativePath, "generated-assets.json", StringComparison.OrdinalIgnoreCase) &&
                     !ReferenceEquals(file, assetManifest)))
                    throw new FormatException("Generated output targets a reserved artifact: " + file.RelativePath);
                if (!seen.TryGetValue(file.RelativePath, out OutputFile previous))
                {
                    seen.Add(file.RelativePath, file);
                    unique.Add(file);
                }
                else if (previous.Kind != file.Kind || !SameBytes(previous.Content, file.Content))
                {
                    throw new FormatException("Distinct generated outputs target the same path: " + file.RelativePath);
                }
            }

            return new OutputSet { Files = unique };
        }

        private static bool SameBytes(byte[] first, byte[] second)
        {
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (first[i] != second[i]) return false;
            return true;
        }
    }
}
