using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Jobs;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Core.Text;

namespace HtmlToUitk.Convert.Tests
{
    internal static class Repo
    {
        private static string _root;

        public static string Root
        {
            get
            {
                if (_root != null) return _root;
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "spec", "rules.json")) && Directory.Exists(Path.Combine(dir.FullName, "src")))
                    {
                        _root = dir.FullName;
                        return _root;
                    }

                    dir = dir.Parent;
                }

                throw new DirectoryNotFoundException("repository root not found");
            }
        }

        public static string Fixtures => Path.Combine(Root, "fixtures");

        public static IEnumerable<string> FixtureIds()
        {
            if (!Directory.Exists(Fixtures)) yield break;
            var dirs = new List<string>(Directory.GetDirectories(Fixtures));
            dirs.Sort(StringComparer.Ordinal);
            foreach (string dir in dirs) yield return Path.GetFileName(dir);
        }
    }

    internal sealed class FixtureRun
    {
        public string Directory;
        public JsonObject Meta;
        public ConversionResult Result;
        public string Mode;
        public bool Force;
        public readonly List<string> Mismatches = new List<string>();
        public readonly List<string> Extra = new List<string>();

        public static CapabilityMatrix Capabilities(JsonObject meta)
        {
            string baseJson = File.ReadAllText(Path.Combine(Repo.Root, "spec", "capabilities.6000.6.json"));
            JsonValue patch = meta["capabilityPatch"];
            if (patch != null && !patch.IsNull) baseJson = CapabilityPatcher.Apply(baseJson, patch);
            return CapabilityMatrixLoader.Load(baseJson);
        }

        public static FixtureRun Execute(string id, ConvertServices services = null)
        {
            string dir = Path.Combine(Repo.Fixtures, id);
            var run = new FixtureRun { Directory = dir };
            run.Meta = (JsonObject)JsonReader.Parse(File.ReadAllText(Path.Combine(dir, "fixture.json")));
            run.Mode = run.Meta.GetString("mode", "convert");
            run.Force = run.Meta.GetBool("force", false);
            var fs = new DiskFileSystem(dir);
            var spec = new JobSpec { Name = id };
            spec.Config = run.Meta.GetString("config", null);
            IReadOnlyList<string> inputs = run.Meta.GetStringList("inputs");
            spec.Inputs.Html = inputs ?? new string[0];
            string configDirectory;
            ConversionConfig config = JobRunner.LoadConfig(spec, fs, out configDirectory);
            var job = new ConversionJob { Spec = spec, Config = config, Capabilities = Capabilities(run.Meta) };
            var options = new RunOptions { Force = run.Force, ValidateOnly = run.Mode == "validate" };
            run.Result = ConversionPipeline.Run(job, fs, services, options, configDirectory);
            return run;
        }

        public void Compare()
        {
            string expectedRoot = Path.Combine(Directory, "expected");
            if (!System.IO.Directory.Exists(expectedRoot)) return;
            var outputs = new Dictionary<string, OutputFile>(StringComparer.Ordinal);
            foreach (OutputFile file in Result.Output.Files) outputs[file.RelativePath] = file;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in System.IO.Directory.GetFiles(expectedRoot, "*", SearchOption.AllDirectories))
            {
                string relative = PathUtil.Normalize(file.Substring(expectedRoot.Length + 1));
                if (relative == "diagnostics.json") continue;
                if (relative.EndsWith(".png.expect.json", StringComparison.Ordinal))
                {
                    string pngPath = relative.Substring(0, relative.Length - ".expect.json".Length);
                    seen.Add(pngPath);
                    ComparePng(pngPath, file, outputs);
                    continue;
                }

                seen.Add(relative);
                OutputFile actual;
                if (!outputs.TryGetValue(relative, out actual))
                {
                    Mismatches.Add("missing output " + relative);
                    continue;
                }

                string expectedText = Normalize(File.ReadAllText(file));
                string actualText = Normalize(new UTF8Encoding(false).GetString(actual.Content));
                if (relative.EndsWith(".json", StringComparison.Ordinal))
                {
                    expectedText = Canonical(expectedText);
                    actualText = Canonical(actualText);
                }

                if (expectedText != actualText) Mismatches.Add("different " + relative + "\n--- expected\n" + expectedText + "\n--- actual\n" + actualText);
            }

            foreach (string path in outputs.Keys)
            {
                if (!seen.Contains(path)) Extra.Add(path);
            }
        }

        private void ComparePng(string pngPath, string expectFile, Dictionary<string, OutputFile> outputs)
        {
            OutputFile actual;
            if (!outputs.TryGetValue(pngPath, out actual))
            {
                Mismatches.Add("missing output " + pngPath);
                return;
            }

            var expect = (JsonObject)JsonReader.Parse(File.ReadAllText(expectFile));
            Bake.RgbaImage image = Bake.PngDecoder.Decode(actual.Content);
            int width = expect.GetInt("width", 0);
            int height = expect.GetInt("height", 0);
            int tolerance = expect.GetInt("tolerance", 0);
            if (image.Width != width || image.Height != height) Mismatches.Add(pngPath + " size " + image.Width + "x" + image.Height + " expected " + width + "x" + height);
            JsonArray samples = expect.GetArray("samples");
            if (samples == null) return;
            for (int i = 0; i < samples.Count; i++)
            {
                var sample = (JsonObject)samples[i];
                int x = sample.GetInt("x", 0);
                int y = sample.GetInt("y", 0);
                JsonArray rgba = sample.GetArray("rgba");
                Bake.Rgba8 pixel = image.Get(x, y);
                int[] expectedValues = { rgba[0].AsInt() ?? 0, rgba[1].AsInt() ?? 0, rgba[2].AsInt() ?? 0, rgba[3].AsInt() ?? 0 };
                int[] actualValues = { pixel.R, pixel.G, pixel.B, pixel.A };
                for (int c = 0; c < 4; c++)
                {
                    if (Math.Abs(expectedValues[c] - actualValues[c]) > tolerance) Mismatches.Add(pngPath + " sample (" + x + "," + y + ") channel " + c + ": " + actualValues[c] + " expected " + expectedValues[c]);
                }
            }
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";
        }

        private static string Canonical(string text)
        {
            try
            {
                return JsonWriter.Write(JsonObject.Canonicalize(JsonReader.Parse(text)), JsonWriteOptions.Canonical);
            }
            catch (JsonParseException)
            {
                return text;
            }
        }
    }
}
