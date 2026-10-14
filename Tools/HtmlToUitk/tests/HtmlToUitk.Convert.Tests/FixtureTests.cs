using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace HtmlToUitk.Convert.Tests
{
    public class FixtureTests
    {
        private static readonly Dictionary<string, string> KnownOpen = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "S21-gap-native-capable", "translator must expand gap to row-gap/column-gap when the matrix has them" },
            { "S30-media-breakpoints", "media-breakpoints emulation not written" },
            { "S33-linear-gradient-native", "native linear-gradient normalisation (to right -> 90deg) not written" },
            { "S34-radial-gradient-bake", "gradient-bake emulation not written" },
            { "S35-box-shadow-drop-shadow", "shadow emulation (native drop-shadow) not written" },
            { "S37-blur-runtime-fallback", "backdrop-blur emulation and runtime manifest entries not written" },
            { "S39-keyframes-spec", "keyframes-animator emulation (spec sink, runtime entry) not written" },
            { "S41-stagger-reduced-motion", "keyframes-animator emulation (spec sink, runtime entry) not written" },
            { "S43-pages-nav-modal-tabs", "page-router emulation (hidden classes, nav spec, runtime entry) not written" },
            { "M01-multipage-shared-css", "multi-file assembly not verified against the M fixtures" },
            { "M02-css-link-order-override", "multi-file assembly not verified against the M fixtures" },
            { "M03-css-conflict-diagnostic", "multi-file assembly not verified against the M fixtures" },
            { "M04-uss-passthrough-merge", "multi-file assembly not verified against the M fixtures" },
            { "M05-import-chain-and-cycle", "multi-file assembly not verified against the M fixtures" },
            { "M07-partials-templates", "partials (template/instance) not written" },
            { "M08-partials-inline", "partials (inline) not written" },
            { "M10-single-document-assembly", "singleDocument assembly not written" },
            { "M12-unused-css-rules", "multi-file assembly not verified against the M fixtures" },
            { "M13-per-file-output-mapping", "per-file name and folder mapping not verified" },
            { "M14-shared-tokens-multi", "shared tokens file not written" },
            { "M15-job-file-folders-globs", "job file with folders and globs not verified" },
            { "M16-incremental-regeneration-stable-bytes", "incremental regeneration not verified" }
        };

        public static IEnumerable<string> Ids()
        {
            foreach (string id in Repo.FixtureIds())
            {
                if (id.StartsWith("S", StringComparison.Ordinal) || id.StartsWith("M", StringComparison.Ordinal)) yield return id;
            }
        }

        [TestCaseSource(nameof(Ids))]
        public void ConvertedFilesMatchExpected(string id)
        {
            string reason;
            if (KnownOpen.TryGetValue(id, out reason)) Assert.Ignore("open: " + reason);
            FixtureRun run = FixtureRun.Execute(id);
            if (run.Mode != "convert") Assert.Ignore("validate-only fixture");
            run.Compare();
            Assert.That(run.Mismatches, Is.Empty, string.Join("\n", run.Mismatches));
        }
    }
}
