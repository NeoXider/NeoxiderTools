using System.Collections.Generic;
using Neo.Editor;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Release-hygiene guard: every file listed in <see cref="PackageHealthCheck.VersionMentions" />
    ///     must advertise the version from package.json.
    ///     <para>
    ///         This lives in the EditMode suite — which CI runs on every push — and not only behind the
    ///         <c>Neoxider/Health Check</c> menu item, because that menu item is exactly what nobody
    ///         clicked while the repo-root README advertised <c>10.1.0</c> through the v10.3.0 and
    ///         v10.4.0 releases.
    ///     </para>
    /// </summary>
    public sealed class PackageVersionParityTests
    {
        [Test]
        public void EveryVersionBearingFile_AdvertisesThePackageVersion()
        {
            List<string> problems = PackageHealthCheck.FindVersionParityProblems();

            Assert.That(problems, Is.Empty,
                "Package version drift — bump every file in PackageHealthCheck.VersionMentions:\n  " +
                string.Join("\n  ", problems));
        }

        // WHY: a list that quietly shrank to nothing would make the test above pass on a broken repo.
        [Test]
        public void VersionMentionList_CoversEveryKnownVersionBearingFile()
        {
            Assert.That(PackageHealthCheck.VersionMentions.Length, Is.GreaterThanOrEqualTo(7),
                "Files were removed from PackageHealthCheck.VersionMentions — the guard got weaker, not the repo cleaner.");
        }
    }
}
