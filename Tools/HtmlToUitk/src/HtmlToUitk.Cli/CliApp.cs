using System;
using System.IO;
using HtmlToUitk.Capabilities.Reader;
using HtmlToUitk.Convert.Jobs;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Cli
{
    public static class CliApp
    {
        public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
        {
            if (args == null || args.Length == 0)
            {
                stderr.WriteLine("usage: htmltouitk <command> ...");
                stderr.WriteLine("commands: validate, convert, run, preview, compare, capabilities, rules");
                return ExitCodes.Usage;
            }
            string cmd = args[0];
            try
            {
                switch (cmd)
                {
                    case "validate":
                    case "convert":
                    case "run":
                        return JobEntry.Run(args, stdout, stderr);
                    case "capabilities":
                        return ReaderCli.Run(args.Length > 1 ? args[1..] : new string[0], stdout, stderr);
                    case "rules":
                        Rules(stdout);
                        return ExitCodes.Pass;
                    case "preview":
                        stderr.WriteLine("preview unsupported: use python -m htmltouitk preview with PYTHONPATH=oracle/python; see docs/Production.md");
                        return ExitCodes.Usage;
                    case "compare":
                        stderr.WriteLine("compare unsupported: use tools/compare_shots.py with independently captured images; see docs/Production.md");
                        return ExitCodes.Usage;
                    default:
                        stderr.WriteLine("unknown command: " + cmd);
                        stderr.WriteLine("commands: validate, convert, run, preview, compare, capabilities, rules");
                        return ExitCodes.Usage;
                }
            }
            catch (InvalidOperationException ex)
            {
                stderr.WriteLine("FAIL unsupported conversion: " + ex.Message);
                return ExitCodes.Fail;
            }
            catch (Exception ex)
            {
                stderr.WriteLine("error: " + ex.Message);
                return ExitCodes.Usage;
            }
        }

        private static void Rules(TextWriter stdout)
        {
            stdout.WriteLine("Rule catalogue (embedded): " + RuleCatalog.Embedded.All.Count + " rules");
            foreach (RuleInfo info in RuleCatalog.Embedded.All)
            {
                stdout.WriteLine(info.Id + " " + info.DefaultSeverity + " / " + info.DefaultTier + " | " + info.Title);
            }
        }
    }
}
