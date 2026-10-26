using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class CliProcessBridge : IJobBridge
    {
        private const int CleanupMilliseconds = 2000;
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
            int timeoutMs = (int)Math.Min(int.MaxValue, Math.Max(5L, request.TimeoutSeconds) * 1000L);
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
                    TaskCompletionSource<bool> outputClosed = new TaskCompletionSource<bool>();
                    TaskCompletionSource<bool> errorClosed = new TaskCompletionSource<bool>();
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) outputClosed.TrySetResult(true);
                        else lock (stdout) stdout.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) errorClosed.TrySetResult(true);
                        else lock (stderr) stderr.AppendLine(e.Data);
                    };
                    bool startedProcess = false;
                    bool readingOutput = false;
                    bool readingError = false;
                    try
                    {
                        startedProcess = process.Start();
                        if (!startedProcess)
                            throw new InvalidOperationException("The CLI process did not start.");
                        process.BeginOutputReadLine();
                        readingOutput = true;
                        process.BeginErrorReadLine();
                        readingError = true;
                        if (!process.WaitForExit(timeoutMs))
                        {
                            result.Faulted = true;
                            result.ExitCode = 2;
                            lock (stderr)
                                stderr.AppendLine("Timed out after " + timeoutMs / 1000.0 + " s");
                        }
                        else
                        {
                            result.ExitCode = process.ExitCode;
                        }
                    }
                    finally
                    {
                        if (startedProcess)
                        {
                            string cleanupError = FinishProcess(process, readingOutput ? outputClosed.Task : null, readingError ? errorClosed.Task : null);
                            if (cleanupError != null)
                            {
                                result.Faulted = true;
                                result.ExitCode = 2;
                                lock (stderr)
                                    stderr.AppendLine(cleanupError);
                            }
                        }
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

        private static string FinishProcess(Process process, Task outputClosed, Task errorClosed)
        {
            Stopwatch cleanup = Stopwatch.StartNew();
            List<string> errors = new List<string>();
            try
            {
                if (!process.HasExited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception exception)
                    {
                        if (!process.HasExited)
                            errors.Add("Could not terminate owned CLI process " + process.Id + ": " + exception.Message);
                    }

                    int remaining = (int)Math.Max(0L, CleanupMilliseconds - cleanup.ElapsedMilliseconds);
                    if (!process.WaitForExit(remaining))
                        errors.Add("Termination of owned CLI process " + process.Id + " was not confirmed within the cleanup deadline.");
                }
            }
            catch (Exception exception)
            {
                errors.Add("Could not confirm CLI process termination: " + exception.Message);
            }

            List<Task> streams = new List<Task>();
            if (outputClosed != null) streams.Add(outputClosed);
            if (errorClosed != null) streams.Add(errorClosed);
            int drainRemaining = (int)Math.Max(0L, CleanupMilliseconds - cleanup.ElapsedMilliseconds);
            if (streams.Count > 0 && !Task.WaitAll(streams.ToArray(), drainRemaining))
                errors.Add("CLI redirected streams did not close within the cleanup deadline.");
            CancelRead(process, outputClosed, true, errors);
            CancelRead(process, errorClosed, false, errors);
            return errors.Count == 0 ? null : string.Join("\n", errors);
        }

        private static void CancelRead(Process process, Task closed, bool output, List<string> errors)
        {
            if (closed == null || closed.IsCompleted)
                return;
            try
            {
                if (output) process.CancelOutputRead();
                else process.CancelErrorRead();
            }
            catch (Exception exception)
            {
                errors.Add("Could not cancel CLI " + (output ? "stdout" : "stderr") + " read: " + exception.Message);
            }
        }
    }
}
