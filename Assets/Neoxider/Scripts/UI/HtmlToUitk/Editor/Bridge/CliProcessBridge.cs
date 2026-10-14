using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class CliProcessBridge : IJobBridge
    {
        private readonly string _cliPath;

        public CliProcessBridge(string cliPath)
        {
            _cliPath = cliPath;
        }

        public string Name => "cli";

        public static string ResolveExecutable(string configured, out string leadingArgument)
        {
            leadingArgument = null;
            if (string.IsNullOrWhiteSpace(configured))
                return "htmltouitk";
            string path = configured.Trim();
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                leadingArgument = path;
                return "dotnet";
            }

            return path;
        }

        public bool IsAvailable(out string reason)
        {
            string executable = ResolveExecutable(_cliPath, out string leading);
            if (leading != null)
            {
                bool exists = File.Exists(leading);
                reason = exists ? null : "CLI assembly not found: " + leading;
                if (!exists) return false;
            }
            if (FindExecutable(executable) == null)
            {
                reason = "CLI executable not found on disk/PATH: " + executable;
                return false;
            }

            reason = null;
            return true;
        }

        public static string FindExecutable(string executable)
        {
            if (string.IsNullOrWhiteSpace(executable)) return null;
            if (Path.IsPathRooted(executable) || executable.IndexOf('/') >= 0 || executable.IndexOf('\\') >= 0)
                return File.Exists(executable) ? Path.GetFullPath(executable) : null;
            string extensions = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT";
            string[] suffixes = extensions.Split(';');
            string[] directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);
            foreach (string directory in directories)
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                string candidate = Path.Combine(directory.Trim().Trim('"'), executable);
                if (File.Exists(candidate)) return candidate;
                if (Path.HasExtension(executable)) continue;
                foreach (string suffix in suffixes)
                    if (File.Exists(candidate + suffix)) return candidate + suffix;
            }
            return null;
        }

        public JobRunResult Run(JobRunRequest request)
        {
            JobRunResult result = new JobRunResult { BridgeName = Name };
            string executable = ResolveExecutable(_cliPath, out string leading);
            executable = FindExecutable(executable) ?? executable;
            List<string> args = new List<string>();
            if (leading != null)
                args.Add(leading);
            args.AddRange(CliCommandLine.BuildRunArguments(request.JobPath, request.ValidateOnly, request.Force, request.Strict));

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = CliCommandLine.Join(args),
                WorkingDirectory = string.IsNullOrEmpty(request.WorkingDirectory) ? Directory.GetCurrentDirectory() : request.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTime started = DateTime.UtcNow;
            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) => { if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data); };
                    process.ErrorDataReceived += (sender, e) => { if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data); };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    int timeoutMs = Math.Max(5, request.TimeoutSeconds) * 1000;
                    if (!process.WaitForExit(timeoutMs))
                    {
                        TryKill(process);
                        result.Faulted = true;
                        result.ExitCode = 2;
                        lock (stderr)
                            stderr.AppendLine("Timed out after " + request.TimeoutSeconds + " s");
                    }
                    else
                    {
                        process.WaitForExit();
                        result.ExitCode = process.ExitCode;
                    }
                }
            }
            catch (Exception exception)
            {
                result.Faulted = true;
                result.ExitCode = 2;
                lock (stderr)
                    stderr.AppendLine("Could not run '" + executable + "': " + exception.Message);
            }

            stopwatch.Stop();
            result.Seconds = stopwatch.Elapsed.TotalSeconds;
            lock (stdout)
                result.StdOut = stdout.ToString();
            lock (stderr)
                result.StdErr = stderr.ToString();
            result.Report = ReportReader.FromRun(result.StdOut, result.StdErr, result.ExitCode, request, started);
            result.ValidateReport();
            return result;
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Exception)
            {
            }
        }
    }
}
