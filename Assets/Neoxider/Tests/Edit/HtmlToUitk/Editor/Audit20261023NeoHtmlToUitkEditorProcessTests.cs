using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorProcessTests
    {
        [TestCase(int.MaxValue)]
        [TestCase(-1)]
        public void ExtremeTimeoutsAllowAFastProcessAndDrainItsRealDiagnostics(int timeout)
        {
            RunProbe("pass", timeout, (result, tag) =>
            {
                Assert.That(result.Succeeded, Is.True, result.StdErr);
                Assert.That(result.ExitCode, Is.Zero);
                Assert.That(result.Report.Verdict, Is.EqualTo("PASS"));
                Assert.That(result.StdErr, Does.Contain("owned process probe stderr"));
                Assert.That(IsOwnedProcessAlive(tag), Is.False);
            });
        }

        [Test]
        public void TimeoutReturnsAFailureAndConfirmsTheOwnedProcessExited()
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            RunProbe("wait", 0, (result, tag) =>
            {
                Assert.That(result.Faulted, Is.True);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ExitCode, Is.Not.EqualTo(0));
                Assert.That(result.StdErr, Does.Contain("Timed out"));
                Assert.That(IsOwnedProcessAlive(tag), Is.False);
                Assert.That(elapsed.Elapsed.TotalSeconds, Is.LessThan(15));
            });
        }

        private static void RunProbe(string mode, int timeout, Action<JobRunResult, string> inspect)
        {
            string probe = Environment.GetEnvironmentVariable("NEO_HTMLTOUITK_PROCESS_PROBE");
            if (string.IsNullOrEmpty(probe))
                Assert.Ignore("After source freeze, build the task-owned net9.0 probe and set NEO_HTMLTOUITK_PROCESS_PROBE to its executable or DLL.");
            Assert.That(File.Exists(probe), Is.True, "The explicitly configured probe must exist.");
            string directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-process-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string job = Path.Combine(directory, "probe.htujob.json");
            string pid = job + ".pid";
            try
            {
                File.WriteAllText(job, mode);
                JobRunResult result = new CliProcessBridge(probe).Run(new JobRunRequest { JobPath = job, WorkingDirectory = directory, TimeoutSeconds = timeout });
                Assert.That(File.Exists(pid), Is.True, result.StdErr);
                inspect(result, File.ReadAllText(pid));
            }
            finally
            {
                if (File.Exists(pid))
                {
                    StopOwnedProcess(File.ReadAllText(pid));
                    File.Delete(pid);
                }
                File.Delete(job);
                Directory.Delete(directory);
            }
        }

        private static Process FindOwnedProcess(string tag, bool requireUnambiguousIdentity = false)
        {
            string[] parts = tag.Split('|');
            int id = int.Parse(parts[0], CultureInfo.InvariantCulture);
            long startTicks = long.Parse(parts[1], CultureInfo.InvariantCulture);
            Process process = null;
            try
            {
                process = Process.GetProcessById(id);
                if (!process.HasExited)
                {
                    if (process.StartTime.ToUniversalTime().Ticks == startTicks)
                        return process;
                    if (requireUnambiguousIdentity)
                        throw new InvalidDataException("The probe PID is alive with a different start identity; termination cannot be claimed: " + tag);
                }
                process.Dispose();
                return null;
            }
            catch (ArgumentException)
            {
                process?.Dispose();
                return null;
            }
            catch (InvalidOperationException)
            {
                process?.Dispose();
                return null;
            }
            catch
            {
                process?.Dispose();
                throw;
            }
        }

        private static bool IsOwnedProcessAlive(string tag)
        {
            using (Process process = FindOwnedProcess(tag, true))
                return process != null;
        }

        private static void StopOwnedProcess(string tag)
        {
            using (Process process = FindOwnedProcess(tag))
            {
                if (process == null) return;
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    if (!process.HasExited) throw;
                }
                Assert.That(process.WaitForExit(2000), Is.True, "The task-owned probe process could not be cleaned up: " + tag);
            }
        }
    }
}
