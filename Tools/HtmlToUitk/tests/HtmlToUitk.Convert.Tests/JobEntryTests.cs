using System;
using System.IO;
using HtmlToUitk.Convert.Jobs;
using NUnit.Framework;

namespace HtmlToUitk.Convert.Tests
{
    public class JobEntryTests
    {
        private static string CopyFixture(string id)
        {
            string source = Path.Combine(Repo.Fixtures, id);
            string target = Path.Combine(Path.GetTempPath(), "htu-job-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            foreach (string file in Directory.GetFiles(Path.Combine(source, "input"), "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length + 1);
                string destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }

            File.Copy(Path.Combine(source, "config.json"), Path.Combine(target, "config.json"));
            return target;
        }

        [Test]
        public void RunWritesOutputAndReturnsZero()
        {
            string dir = CopyFixture("S01-minimal-page");
            try
            {
                File.WriteAllText(Path.Combine(dir, "main.htujob.json"), "{ \"schema\": 1, \"name\": \"t\", \"config\": \"config.json\", \"inputs\": { \"html\": [\"input/page.html\"] }, \"output\": { \"root\": \"out\" } }");
                var stdout = new StringWriter();
                var stderr = new StringWriter();
                int code = JobEntry.Run(new[] { "run", Path.Combine(dir, "main.htujob.json") }, stdout, stderr);
                Assert.That(code, Is.EqualTo(0), stdout + stderr.ToString());
                Assert.That(File.Exists(Path.Combine(dir, "out", "Screens", "Minimal.uxml")), Is.True);
                Assert.That(File.Exists(Path.Combine(dir, "out", "Screens", "Minimal.uss")), Is.True);
                Assert.That(File.Exists(Path.Combine(dir, "out", "diagnostics.json")), Is.True);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void MissingJobFileIsAUsageError()
        {
            var stderr = new StringWriter();
            int code = JobEntry.Run(new[] { "run", Path.Combine(Path.GetTempPath(), "htu-none.json") }, new StringWriter(), stderr);
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void UnknownCommandIsAUsageError()
        {
            int code = JobEntry.Run(new[] { "bogus" }, new StringWriter(), new StringWriter());
            Assert.That(code, Is.EqualTo(2));
        }

        [Test]
        public void NoArgumentsIsAUsageError()
        {
            int code = JobEntry.Run(new string[0], new StringWriter(), new StringWriter());
            Assert.That(code, Is.EqualTo(2));
        }
    }
}
