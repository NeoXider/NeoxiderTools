using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class RunOptions
    {
        public bool ValidateOnly;
        public bool Force;
        public bool Strict;
        public IReadOnlyList<string> OnlyFiles;
    }

    public static class HtmlToUitkRunner
    {
        private static readonly object Gate = new object();
        private static volatile bool _running;

        public static bool IsRunning => _running;

        public static IJobBridge BridgeOverride { get; set; }

        public static IJobBridge SelectBridge(out string note)
        {
            if (BridgeOverride != null)
            {
                note = null;
                return BridgeOverride;
            }

            InProcessBridge inProcess = new InProcessBridge();
            CliProcessBridge cli = new CliProcessBridge(HtmlToUitkSettings.EffectiveCliPath);
            bool inProcessAvailable = inProcess.IsAvailable(out string inProcessReason);
            bool cliAvailable = cli.IsAvailable(out string cliReason);
            note = null;
            if (inProcessAvailable && (HtmlToUitkSettings.PreferInProcess || !cliAvailable))
                return inProcess;
            if (cliAvailable)
            {
                note = HtmlToUitkSettings.PreferInProcess && !inProcessAvailable ? "In-process core unavailable (" + inProcessReason + "); using the CLI." : null;
                return cli;
            }

            note = "No runner available. In-process: " + inProcessReason + ". CLI: " + cliReason;
            return null;
        }

        public static Task<JobRunResult> RunAsync(JobDocument document, string jobPath, RunOptions options)
        {
            string cliPath = HtmlToUitkSettings.EffectiveCliPath;
            int timeout = HtmlToUitkSettings.CliTimeoutSeconds;
            IJobBridge bridge = SelectBridge(out string note);
            return Task.Run(() => Execute(document, jobPath, options, bridge, note, cliPath, timeout));
        }

        public static JobRunResult Run(JobDocument document, string jobPath, RunOptions options)
        {
            IJobBridge bridge = SelectBridge(out string note);
            return Execute(document, jobPath, options, bridge, note, HtmlToUitkSettings.EffectiveCliPath, HtmlToUitkSettings.CliTimeoutSeconds);
        }

        public static string OutputRootAbsolute(JobDocument document, string jobPath)
        {
            string root = string.IsNullOrEmpty(document.OutputRoot) ? "." : document.OutputRoot;
            return InputExpander.Combine(HtmlToUitkPaths.DirectoryOf(jobPath), root);
        }

        public static void RefreshAssets(JobDocument document, string jobPath)
        {
            string output = OutputRootAbsolute(document, jobPath);
            string relative = HtmlToUitkPaths.ToProjectRelative(output);
            if (relative != null && AssetDatabase.IsValidFolder(relative))
                AssetDatabase.ImportAsset(relative, ImportAssetOptions.ImportRecursive);
            else if (relative != null)
                AssetDatabase.Refresh();
        }

        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string job = ArgumentValue(args, "-htujob");
            if (string.IsNullOrEmpty(job))
            {
                Debug.LogError("[HtmlToUitk] -htujob <path> is required");
                EditorApplication.Exit(2);
                return;
            }

            string absolute = HtmlToUitkPaths.ToAbsolute(job);
            JobDocument document;
            try
            {
                document = JobDocument.Parse(File.ReadAllText(absolute));
            }
            catch (Exception exception)
            {
                Debug.LogError("[HtmlToUitk] cannot read job '" + absolute + "': " + exception.Message);
                EditorApplication.Exit(2);
                return;
            }

            RunOptions options = new RunOptions { ValidateOnly = Array.IndexOf(args, "-htuValidateOnly") >= 0, Force = Array.IndexOf(args, "-htuForce") >= 0, Strict = Array.IndexOf(args, "-htuStrict") >= 0 };
            JobRunResult result = Run(document, absolute, options);
            Debug.Log("[HtmlToUitk] " + Summary(result));
            if (!options.ValidateOnly && result.Succeeded)
                RefreshAssets(document, absolute);
            EditorApplication.Exit(result.ExitCode);
        }

        public static string Summary(JobRunResult result)
        {
            DiagnosticsReport report = result.Report;
            return report.Verdict + ": " + report.Errors + " errors, " + report.Warnings + " warnings, " + report.Infos + " info (" + result.BridgeName + ", " + result.Seconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s)";
        }

        private static JobRunResult Execute(JobDocument document, string jobPath, RunOptions options, IJobBridge bridge, string note, string cliPath, int timeoutSeconds)
        {
            lock (Gate)
            {
                _running = true;
                try
                {
                    if (bridge == null)
                        return new JobRunResult { Faulted = true, ExitCode = 2, Report = DiagnosticsReport.Failure(note ?? "No runner available") };
                    string directory = HtmlToUitkPaths.DirectoryOf(jobPath);
                    string temporary = directory + "/.htu-run-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".htujob.json";
                    JobDocument derived = JobDocument.Parse(document.ToJson());
                    if (options.OnlyFiles != null && options.OnlyFiles.Count > 0)
                        derived.Html = new List<string>(options.OnlyFiles);
                    File.WriteAllText(temporary, derived.ToJson(), new UTF8Encoding(false));
                    try
                    {
                        JobRunRequest request = new JobRunRequest
                        {
                            JobPath = temporary,
                            WorkingDirectory = directory,
                            ValidateOnly = options.ValidateOnly,
                            Force = options.Force,
                            Strict = options.Strict,
                            OutputRoot = OutputRootAbsolute(derived, temporary),
                            TimeoutSeconds = timeoutSeconds
                        };
                        JobRunResult result = bridge.Run(request);
                        if (result.Faulted && bridge is InProcessBridge && new CliProcessBridge(cliPath).IsAvailable(out string _))
                            result = new CliProcessBridge(cliPath).Run(request);
                        result.ValidateReport();
                        if (note != null)
                            result.StdErr = note + "\n" + result.StdErr;
                        return result;
                    }
                    finally
                    {
                        TryDelete(temporary);
                    }
                }
                finally
                {
                    _running = false;
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string ArgumentValue(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }
    }
}
