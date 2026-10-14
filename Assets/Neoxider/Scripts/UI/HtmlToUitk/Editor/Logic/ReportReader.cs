using System;
using System.IO;

namespace Neo.HtmlToUitk.Tooling
{
    public static class ReportReader
    {
        public const string ReportFileName = "diagnostics.json";
        public static readonly TimeSpan FreshnessSlack = TimeSpan.FromSeconds(2);

        public static DiagnosticsReport FromRun(string stdout, string stderr, int exitCode, JobRunRequest request, DateTime runStartUtc)
        {
            DiagnosticsReport fromFile = TryReadFile(request, runStartUtc, out string fileError);
            if (fromFile != null)
                return fromFile;
            string json = CliCommandLine.ExtractJsonObject(stdout);
            if (json != null)
            {
                try
                {
                    return DiagnosticsReport.Parse(json);
                }
                catch (Neo.HtmlToUitk.Json.JsonParseException exception)
                {
                    return DiagnosticsReport.Failure("The tool printed a malformed report: " + exception.Message + Tail(stderr));
                }
            }

            string detail = string.IsNullOrWhiteSpace(stderr) ? Tail(stdout) : Tail(stderr);
            string reason = fileError != null ? " " + fileError : string.Empty;
            return DiagnosticsReport.Failure("The tool produced no " + ReportFileName + " (exit code " + exitCode + ")." + reason + detail);
        }

        public static string ReportPath(JobRunRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OutputRoot))
                return null;
            return request.OutputRoot.Replace('\\', '/').TrimEnd('/') + "/" + ReportFileName;
        }

        private static DiagnosticsReport TryReadFile(JobRunRequest request, DateTime runStartUtc, out string error)
        {
            error = null;
            string path = ReportPath(request);
            if (path == null || !File.Exists(path))
                return null;
            try
            {
                if (File.GetLastWriteTimeUtc(path) < runStartUtc - FreshnessSlack)
                {
                    error = "The report file in the output folder is older than this run.";
                    return null;
                }

                return DiagnosticsReport.Parse(File.ReadAllText(path));
            }
            catch (Neo.HtmlToUitk.Json.JsonParseException exception)
            {
                error = "The report file is malformed: " + exception.Message;
                return null;
            }
            catch (IOException exception)
            {
                error = "The report file cannot be read: " + exception.Message;
                return null;
            }
        }

        private static string Tail(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            string trimmed = text.Trim();
            if (trimmed.Length > 600)
                trimmed = trimmed.Substring(trimmed.Length - 600);
            return " " + trimmed;
        }
    }
}
