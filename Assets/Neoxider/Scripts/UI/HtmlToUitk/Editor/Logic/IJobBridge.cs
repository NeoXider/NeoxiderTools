namespace Neo.HtmlToUitk.Tooling
{
    public sealed class JobRunRequest
    {
        public string JobPath;
        public string WorkingDirectory;
        public bool ValidateOnly;
        public bool Force;
        public bool Strict;
        public string OutputRoot;
        public int TimeoutSeconds = 120;
    }

    public sealed class JobRunResult
    {
        public DiagnosticsReport Report;
        public int ExitCode;
        public string StdOut = string.Empty;
        public string StdErr = string.Empty;
        public string BridgeName = string.Empty;
        public double Seconds;
        public bool Faulted;

        public bool Succeeded => !Faulted && ExitCode == 0 && Report != null && Report.Errors == 0
            && !string.Equals(Report.Verdict, "FAIL", System.StringComparison.OrdinalIgnoreCase);

        public void ValidateReport()
        {
            if (Report == null) Report = DiagnosticsReport.Failure("The runner returned no diagnostics report.");
            if (Report.Errors > 0 || string.Equals(Report.Verdict, "FAIL", System.StringComparison.OrdinalIgnoreCase))
            {
                Faulted = true;
                if (ExitCode == 0) ExitCode = 2;
            }
            if (Report.Errors == 0 && (ExitCode >= 2 || ExitCode < 0 || (ExitCode != 0 && string.Equals(Report.Verdict, "PASS", System.StringComparison.OrdinalIgnoreCase))))
            {
                Report = DiagnosticsReport.Failure("Runner exit code " + ExitCode + " conflicts with its PASS report.");
                Faulted = true;
            }
        }
    }

    public interface IJobBridge
    {
        string Name { get; }

        bool IsAvailable(out string reason);

        JobRunResult Run(JobRunRequest request);
    }
}
