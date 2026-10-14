using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Json;
using HtmlToUitk.Core.Text;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Jobs
{
    public sealed class JobRunOptions
    {
        public bool ValidateOnly;
        public bool Force;
        public bool Strict;
        public string OutputOverride;
        public string CapabilitiesPath;
        public bool WriteReport = true;
    }

    public sealed class JobRunResult
    {
        public ConversionResult Conversion;
        public DiagnosticsReport Report;
        public int ExitCode;
        public readonly List<string> Written = new List<string>();
        public readonly List<string> Unchanged = new List<string>();
        public string OutputDirectory;
        public string Error;
    }

    public static class JobRunner
    {
        public const int ExitPass = 0;
        public const int ExitFail = 1;
        public const int ExitUsage = 2;

        public static CapabilityMatrix LoadCapabilities(ConversionConfig config, IFileSystem fs, string configDirectory, string explicitPath)
        {
            string path = explicitPath ?? config.Unity.Capabilities;
            if (!string.IsNullOrEmpty(path))
            {
                string candidate = PathUtil.IsRooted(path) ? path : PathUtil.Combine(configDirectory, path);
                if (fs.FileExists(candidate)) return CapabilityMatrixLoader.Load(fs.ReadAllText(candidate));
                if (explicitPath != null && File.Exists(explicitPath)) return CapabilityMatrixLoader.Load(File.ReadAllText(explicitPath));
            }

            CapabilityMatrix embedded;
            if (CapabilityMatrixLoader.TryLoadEmbedded(config.Unity.Version, out embedded)) return embedded;
            throw new FileNotFoundException("no capability matrix for Unity " + config.Unity.Version + " (" + path + ")");
        }

        public static ConversionConfig LoadConfig(JobSpec spec, IFileSystem fs, out string configDirectory)
        {
            configDirectory = "";
            var config = new ConversionConfig();
            if (!string.IsNullOrEmpty(spec.Config))
            {
                string configPath = PathUtil.Normalize(spec.Config);
                configDirectory = PathUtil.Directory(configPath);
                if (!fs.FileExists(configPath)) throw new FileNotFoundException("config file not found: " + spec.Config);
                config = ConfigLoader.Load(fs.ReadAllText(configPath));
            }

            if (!string.IsNullOrEmpty(spec.ConfigOverridesJson))
            {
                config = ConfigLoader.Apply(config, JsonReader.Parse(spec.ConfigOverridesJson) as JsonObject);
            }

            return config;
        }

        public static JobRunResult RunFile(string jobPath, JobRunOptions options, ConvertServices services = null)
        {
            options = options ?? new JobRunOptions();
            string fullJob = Path.GetFullPath(jobPath);
            string jobDirectory = Path.GetDirectoryName(fullJob);
            var fs = new DiskFileSystem(jobDirectory);
            JobSpec spec = JobLoader.Load(File.ReadAllText(fullJob));
            return RunSpec(spec, fs, jobDirectory, options, services);
        }

        public static JobRunResult RunSpec(JobSpec spec, IFileSystem fs, string jobDirectory, JobRunOptions options, ConvertServices services = null)
        {
            options = options ?? new JobRunOptions();
            var result = new JobRunResult();
            string configDirectory;
            ConversionConfig config = LoadConfig(spec, fs, out configDirectory);
            CapabilityMatrix caps = LoadCapabilities(config, fs, configDirectory, options.CapabilitiesPath);
            var job = new ConversionJob { Spec = spec, Config = config, Capabilities = caps };
            var runOptions = new RunOptions { Force = options.Force, ValidateOnly = options.ValidateOnly };
            ConversionResult conversion = ConversionPipeline.Run(job, fs, services, runOptions, configDirectory);
            result.Conversion = conversion;
            var reportOptions = new ReportOptions { Source = spec.Name, NewKeys = conversion.NewKeys };
            result.Report = DiagnosticsReport.Build(conversion.Diagnostics, reportOptions);
            result.ExitCode = result.Report.ExitCode(options.Strict) == 0 ? ExitPass : ExitFail;
            string outputRoot = options.OutputOverride ?? spec.Output.Root ?? "";
            string outputDirectory = PathUtil.IsRooted(outputRoot) || jobDirectory == null ? PathUtil.Normalize(outputRoot) : PathUtil.Combine(PathUtil.Normalize(jobDirectory), outputRoot);
            result.OutputDirectory = outputDirectory;
            if (options.ValidateOnly || conversion.Refused) return result;
            var outFs = new DiskFileSystem(outputDirectory);
            Commit(conversion.Output, outFs, result);
            if (options.WriteReport)
            {
                byte[] report = new UTF8Encoding(false).GetBytes(DiagnosticsJson.Write(result.Report));
                WriteIfChanged(outFs, "diagnostics.json", report, result);
            }

            return result;
        }

        public static void Commit(OutputSet output, IFileSystem fs, JobRunResult result)
        {
            foreach (OutputFile file in output.Files) WriteIfChanged(fs, file.RelativePath, file.Content, result);
        }

        private static void WriteIfChanged(IFileSystem fs, string path, byte[] content, JobRunResult result)
        {
            if (fs.FileExists(path))
            {
                byte[] existing = fs.ReadAllBytes(path);
                if (SameBytes(existing, content))
                {
                    result.Unchanged.Add(path);
                    return;
                }
            }

            fs.WriteAllBytes(path, content);
            result.Written.Add(path);
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
    }
}
