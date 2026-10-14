using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Neo.HtmlToUitk.Tooling
{
    public sealed class InProcessBridge : IJobBridge
    {
        public const string EntryTypeName = "HtmlToUitk.Convert.Jobs.JobEntry";

        private static MethodInfo _entry;
        private static bool _searched;

        public string Name => "in-process";

        public static void ResetDiscovery()
        {
            _entry = null;
            _searched = false;
        }

        public bool IsAvailable(out string reason)
        {
            if (FindEntry() != null)
            {
                reason = null;
                return true;
            }

            reason = "HtmlToUitk.Convert.Jobs.JobEntry.Run(string[], TextWriter, TextWriter) was not found in the Editor domain (copy the core plugin DLLs into Editor/Plugins)";
            return false;
        }

        public JobRunResult Run(JobRunRequest request)
        {
            JobRunResult result = new JobRunResult { BridgeName = Name };
            MethodInfo entry = FindEntry();
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTime started = DateTime.UtcNow;
            if (entry == null)
            {
                result.Faulted = true;
                result.Report = DiagnosticsReport.Failure("In-process core is not available");
                return result;
            }

            StringWriter stdout = new StringWriter();
            StringWriter stderr = new StringWriter();
            try
            {
                List<string> args = CliCommandLine.BuildRunArguments(request.JobPath, request.ValidateOnly, request.Force, request.Strict);
                result.ExitCode = (int)entry.Invoke(null, new object[] { args.ToArray(), stdout, stderr });
            }
            catch (TargetInvocationException exception)
            {
                result.Faulted = true;
                result.ExitCode = 2;
                result.StdErr = (exception.InnerException ?? exception).ToString();
            }
            catch (Exception exception)
            {
                result.Faulted = true;
                result.ExitCode = 2;
                result.StdErr = exception.ToString();
            }

            result.StdOut = stdout.ToString();
            if (string.IsNullOrEmpty(result.StdErr))
                result.StdErr = stderr.ToString();
            stopwatch.Stop();
            result.Seconds = stopwatch.Elapsed.TotalSeconds;
            result.Report = ReportReader.FromRun(result.StdOut, result.StdErr, result.ExitCode, request, started);
            return result;
        }

        private static MethodInfo FindEntry()
        {
            if (_searched && _entry != null)
                return _entry;
            _searched = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (!name.StartsWith("HtmlToUitk.", StringComparison.Ordinal))
                    continue;
                Type type = assembly.GetType(EntryTypeName, false);
                MethodInfo method = type?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string[]), typeof(TextWriter), typeof(TextWriter) }, null);
                if (method != null && method.ReturnType == typeof(int))
                {
                    _entry = method;
                    return _entry;
                }
            }

            return null;
        }
    }
}
