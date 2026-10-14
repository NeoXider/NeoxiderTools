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

            foreach (GeneratedAsset asset in model.Assets)
            {
                files.Add(new OutputFile { RelativePath = PathUtil.Normalize(asset.RelativePath), Content = asset.Content, Kind = FileKind.Png });
            }

            if (model.Assets.Count > 0)
            {
                JsonObject manifest = new JsonObject();
                manifest.Set("schema", 1);
                JsonArray entries = new JsonArray();
                List<GeneratedAsset> sortedAssets = new List<GeneratedAsset>(model.Assets);
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
                files.Add(Text("generated-assets.json", JsonWriter.Write(manifest, JsonWriteOptions.Pretty), FileKind.Json));
            }
            files.Sort((a, b) => string.CompareOrdinal(a.RelativePath, b.RelativePath));
            List<OutputFile> unique = new List<OutputFile>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (OutputFile file in files)
            {
                if (seen.Add(file.RelativePath)) unique.Add(file);
            }

            return new OutputSet { Files = unique };
        }
    }
}
