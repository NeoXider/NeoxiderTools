using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Jobs
{
    public static class JobEntry
    {
        public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
        {
            try
            {
                return RunCore(args ?? new string[0], stdout ?? TextWriter.Null, stderr ?? TextWriter.Null);
            }
            catch (FileNotFoundException exception)
            {
                (stderr ?? TextWriter.Null).WriteLine("error: " + exception.Message);
                return JobRunner.ExitUsage;
            }
            catch (IOException exception)
            {
                (stderr ?? TextWriter.Null).WriteLine("error: " + exception.Message);
                return JobRunner.ExitUsage;
            }
            catch (UnauthorizedAccessException exception)
            {
                (stderr ?? TextWriter.Null).WriteLine("error: " + exception.Message);
                return JobRunner.ExitUsage;
            }
            catch (FormatException exception)
            {
                (stderr ?? TextWriter.Null).WriteLine("error: " + exception.Message);
                return JobRunner.ExitUsage;
            }
        }

        public static string Usage()
        {
            return "usage:\n"
                + "  run <job.json> [--validate-only] [--format text|json] [--force] [--strict] [--out <dir>] [--capabilities <file>]\n"
                + "  validate <html|dir|glob>... [--config <file>] [--format text|json] [--strict]\n"
                + "  convert <html|dir|glob>... --out <dir> [--config <file>] [--format text|json] [--force] [--strict]\n"
                + "exit codes: 0 pass (warnings only pass unless --strict), 1 fail, 2 usage or I/O error\n";
        }

        private static int RunCore(string[] args, TextWriter stdout, TextWriter stderr)
        {
            if (args.Length == 0)
            {
                stderr.Write(Usage());
                return JobRunner.ExitUsage;
            }

            string command = args[0];
            var positional = new List<string>();
            var options = new JobRunOptions();
            string format = "text";
            string config = null;
            string outDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--validate-only": options.ValidateOnly = true; break;
                    case "--force": options.Force = true; break;
                    case "--strict": options.Strict = true; break;
                    case "--format":
                        if (i + 1 >= args.Length) return Usage(stderr, "--format needs a value");
                        format = args[++i];
                        break;
                    case "--out":
                        if (i + 1 >= args.Length) return Usage(stderr, "--out needs a value");
                        outDir = args[++i];
                        break;
                    case "--config":
                        if (i + 1 >= args.Length) return Usage(stderr, "--config needs a value");
                        config = args[++i];
                        break;
                    case "--capabilities":
                        if (i + 1 >= args.Length) return Usage(stderr, "--capabilities needs a value");
                        options.CapabilitiesPath = args[++i];
                        break;
                    default:
                        if (arg.StartsWith("--", StringComparison.Ordinal)) return Usage(stderr, "unknown option " + arg);
                        positional.Add(arg);
                        break;
                }
            }

            if (format != "text" && format != "json") return Usage(stderr, "--format must be text or json");
            JobRunResult result;
            if (command == "run")
            {
                if (positional.Count != 1) return Usage(stderr, "run needs exactly one job file");
                if (!File.Exists(positional[0]))
                {
                    stderr.WriteLine("error: job file not found: " + positional[0]);
                    return JobRunner.ExitUsage;
                }

                options.OutputOverride = outDir;
                result = JobRunner.RunFile(positional[0], options);
            }
            else if (command == "validate" || command == "convert")
            {
                if (positional.Count == 0) return Usage(stderr, command + " needs at least one input");
                if (command == "convert" && outDir == null) return Usage(stderr, "convert needs --out");
                options.ValidateOnly = command == "validate";
                var spec = new JobSpec { Name = command, Config = config };
                spec.Inputs.Html = positional;
                spec.Output.Root = outDir ?? "";
                string cwd = Directory.GetCurrentDirectory();
                result = JobRunner.RunSpec(spec, new HtmlToUitk.Core.Includes.DiskFileSystem(cwd), cwd, options);
            }
            else
            {
                return Usage(stderr, "unknown command " + command);
            }

            if (format == "json") stdout.Write(DiagnosticsJson.Write(result.Report));
            else
            {
                stdout.Write(DiagnosticsText.Render(result.Report));
                if (result.Conversion.Refused) stdout.WriteLine("conversion refused: the verdict is FAIL (use --force to write the valid rest)");
                else if (!options.ValidateOnly)
                {
                    stdout.WriteLine("output: " + result.OutputDirectory + " (" + result.Written.Count + " written, " + result.Unchanged.Count + " unchanged)");
                    foreach (string path in result.Written) stdout.WriteLine("  wrote " + path);
                }
            }

            return result.ExitCode;
        }

        private static int Usage(TextWriter stderr, string message)
        {
            stderr.WriteLine("error: " + message);
            stderr.Write(Usage());
            return JobRunner.ExitUsage;
        }
    }
}
