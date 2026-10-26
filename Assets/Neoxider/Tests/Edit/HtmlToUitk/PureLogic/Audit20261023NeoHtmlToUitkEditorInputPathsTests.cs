using System;
using System.Collections.Generic;
using System.IO;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorInputPathsTests
    {
        private sealed class Directories : IFileView
        {
            private readonly HashSet<string> _paths;

            public Directories(params string[] paths)
            {
                _paths = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            }

            public bool DirectoryExists(string path) => _paths.Contains(GlobMatcher.Normalize(path));
            public bool FileExists(string path) => false;
            public IEnumerable<string> EnumerateFiles(string directory, bool recursive) => Array.Empty<string>();
        }

        private static readonly string Root = GlobMatcher.Normalize(Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-inputs"));

        [Test]
        public void EditingExternalDependenciesReplacesTheWatchedRootAndInputPolicy()
        {
            JobDocument document = new JobDocument { OutputRoot = "Generated" };
            document.Css.Add("../old/main.css");
            Directories files = new Directories(Root, Root + "/old", Root + "/new");
            IReadOnlyList<string> before = JobInputPaths.GetWatchDirectories(document, Root + "/job", files);
            document.Css[0] = "../new/main.css";
            IReadOnlyList<string> after = JobInputPaths.GetWatchDirectories(document, Root + "/job", files);

            Assert.That(before, Is.EqualTo(new[] { Root + "/old" }));
            Assert.That(after, Is.EqualTo(new[] { Root + "/new" }));
            Assert.That(JobInputPaths.IsInput(document, Root + "/job", Root + "/new/main.css"), Is.True);
            Assert.That(JobInputPaths.IsInput(document, Root + "/job", Root + "/old/main.css"), Is.False);
        }

        [TestCase("pages/menu.html", true)]
        [TestCase("pages/sub/menu.htm", true)]
        [TestCase("style/main.css", true)]
        [TestCase("skin/theme.uss", true)]
        [TestCase("partials/title.html", true)]
        [TestCase("unrelated/menu.html", false)]
        [TestCase("style/readme.txt", false)]
        [TestCase("Generated/theme.uss", false)]
        public void OnlyAuthoredDependenciesMatchAndGeneratedOutputStaysExcluded(string path, bool expected)
        {
            JobDocument document = new JobDocument { OutputRoot = "./Generated" };
            document.Html.Add("pages");
            document.Css.Add("style/**/*.css");
            document.Uss.Add("**/*.uss");
            document.Partials.Add("partials/title.html");
            Assert.That(JobInputPaths.IsInput(document, Root, Root + "/" + path), Is.EqualTo(expected));
        }

        [Test]
        public void ParentSegmentsInGlobsAndOutputsShareCanonicalBoundaries()
        {
            JobDocument document = new JobDocument { OutputRoot = "../Generated" };
            document.Uss.Add("../**/*.uss");
            Assert.That(JobInputPaths.IsInput(document, Root + "/job", Root + "/skin/theme.uss"), Is.True);
            Assert.That(JobInputPaths.IsInput(document, Root + "/job", Root + "/job/../Generated/theme.uss"), Is.False);
            Assert.That(JobInputPaths.IsInput(document, Root + "/job", Root + "/GeneratedSibling/theme.uss"), Is.True);
        }

        [Test]
        public void MissingInputDirectoriesUseAnExistingAncestorAndSharedRootsAreDeduplicated()
        {
            JobDocument document = new JobDocument { OutputRoot = "Generated" };
            document.Html.Add("missing/pages/**/*.html");
            document.Css.Add("missing/style/main.css");
            document.Uss.Add("theme.uss");
            Assert.That(JobInputPaths.GetWatchDirectories(document, Root, new Directories(Root)), Is.EqualTo(new[] { Root }));
        }
    }
}
