using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlToUitk.Convert;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Includes;
using HtmlToUitk.Core.Text;
using NUnit.Framework;

namespace HtmlToUitk.Core.Tests
{
    public class IncludeAndTextTests
    {
        [TestCase("a/b/../c", "a/c")]
        [TestCase("a//b", "a/b")]
        [TestCase("./a", "a")]
        [TestCase("a/./b/", "a/b")]
        [TestCase("../a", "../a")]
        [TestCase("a/../../b", "../b")]
        [TestCase("a\\b\\c", "a/b/c")]
        [TestCase("/a/../b", "/b")]
        [TestCase("/../a", "/a")]
        [TestCase("C:\\x\\..\\y", "C:/y")]
        [TestCase("C:/../y", "C:/y")]
        [TestCase("", "")]
        [TestCase(".", "")]
        [TestCase("a/b", "a/b")]
        public void NormalizesPaths(string input, string expected)
        {
            Assert.That(PathUtil.Normalize(input), Is.EqualTo(expected));
        }

        [TestCase("a/b", "c.css", "a/b/c.css")]
        [TestCase("a/b", "../c.css", "a/c.css")]
        [TestCase("a/b", "/root/c.css", "/root/c.css")]
        [TestCase("", "c.css", "c.css")]
        [TestCase("a", "", "a")]
        [TestCase("a", "C:/x.css", "C:/x.css")]
        [TestCase("a", "..\\..\\x.css", "../x.css")]
        public void CombinesPaths(string directory, string relative, string expected)
        {
            Assert.That(PathUtil.Combine(directory, relative), Is.EqualTo(expected));
        }

        [TestCase("a/b/c.css", "a/b", "c.css", ".css", "c")]
        [TestCase("c.tar.gz", "", "c.tar.gz", ".gz", "c.tar")]
        [TestCase(".hidden", "", ".hidden", "", ".hidden")]
        [TestCase("a\\b.html", "a", "b.html", ".html", "b")]
        [TestCase("/x.css", "/", "x.css", ".css", "x")]
        public void SplitsPaths(string path, string directory, string fileName, string extension, string stem)
        {
            Assert.That(PathUtil.Directory(path), Is.EqualTo(directory));
            Assert.That(PathUtil.FileName(path), Is.EqualTo(fileName));
            Assert.That(PathUtil.Extension(path), Is.EqualTo(extension));
            Assert.That(PathUtil.Stem(path), Is.EqualTo(stem));
        }

        [TestCase("UI/Screens", "UI/Skin/Skin.uss", "../Skin/Skin.uss")]
        [TestCase("UI/Screens", "UI/Screens/A.uss", "A.uss")]
        [TestCase("A", "B/C.uss", "../B/C.uss")]
        [TestCase("", "C.uss", "C.uss")]
        public void ComputesRelativePaths(string from, string target, string expected)
        {
            Assert.That(PathUtil.RelativeTo(from, target), Is.EqualTo(expected));
        }

        [TestCase("/a", true)]
        [TestCase("\\a", true)]
        [TestCase("C:/a", true)]
        [TestCase("c:\\a", true)]
        [TestCase("a/b", false)]
        [TestCase("", false)]
        public void DetectsRootedPaths(string path, bool expected)
        {
            Assert.That(PathUtil.IsRooted(path), Is.EqualTo(expected));
        }

        [Test]
        public void SourceMapMapsOffsets()
        {
            var map = new SourceMap("ab\ncd\r\nef\rgh", "f");
            Assert.That(map.Pos(0).ToString(), Is.EqualTo("1:1"));
            Assert.That(map.Pos(1).ToString(), Is.EqualTo("1:2"));
            Assert.That(map.Pos(3).ToString(), Is.EqualTo("2:1"));
            Assert.That(map.Pos(7).ToString(), Is.EqualTo("3:1"));
            Assert.That(map.Pos(10).ToString(), Is.EqualTo("4:1"));
            Assert.That(map.Pos(100).Offset, Is.EqualTo(12));
            Assert.That(map.Pos(-5).Offset, Is.EqualTo(0));
            Assert.That(map.Span(0, 2).File, Is.EqualTo("f"));
            Assert.That(map.Span(5, 3).End.Offset, Is.EqualTo(5));
        }

        [Test]
        public void SourceMapOriginShiftsFirstLineColumns()
        {
            var map = new SourceMap("ab\ncd", "f", new SourcePos(50, 7, 10));
            Assert.That(map.Pos(1).ToString(), Is.EqualTo("7:11"));
            Assert.That(map.Pos(1).Offset, Is.EqualTo(51));
            Assert.That(map.Pos(3).ToString(), Is.EqualTo("8:1"));
        }

        [Test]
        public void SourceMapHandlesEmptyText()
        {
            var map = new SourceMap("", "f");
            Assert.That(map.Pos(0).ToString(), Is.EqualTo("1:1"));
            Assert.That(new SourceMap(null, "f").Pos(3).Line, Is.EqualTo(1));
        }

        [TestCase("", 2166136261u)]
        [TestCase("a", 3826002220u)]
        [TestCase("foobar", 3214735720u)]
        public void FnvMatchesTheReferenceVectors(string text, uint expected)
        {
            Assert.That(Fnv.Hash32(text), Is.EqualTo(expected));
            Assert.That(Fnv.Hex8(text), Is.EqualTo(expected.ToString("x8")));
        }

        private static MemoryFileSystem Files(params string[] pairs)
        {
            var fs = new MemoryFileSystem();
            for (int i = 0; i + 1 < pairs.Length; i += 2) fs.AddFile(pairs[i], pairs[i + 1]);
            return fs;
        }

        [Test]
        public void ExpandsImportsDepthFirstInSourceOrder()
        {
            MemoryFileSystem fs = Files(
                "css/a.css", "@import \"b.css\";\n.a{x:1}",
                "css/b.css", ".b{x:1}",
                "css/c.css", ".c{x:1}");
            ImportExpansion result = ImportExpander.ExpandText("@import \"a.css\";\n@import \"c.css\";\n.root{x:1}", "css/main.css", StyleOrigin.Linked, fs, null);
            Assert.That(result.Sheets.Select(s => s.Path), Is.EqualTo(new[] { "css/b.css", "css/a.css", "css/c.css", "css/main.css" }));
            Assert.That(result.Sheets.Select(s => s.Origin), Is.EqualTo(new[] { StyleOrigin.Imported, StyleOrigin.Imported, StyleOrigin.Imported, StyleOrigin.Linked }));
            Assert.That(result.Graph.Edges.Select(e => e.From + ">" + e.To), Is.EqualTo(new[] { "css/main.css>css/a.css", "css/a.css>css/b.css", "css/main.css>css/c.css" }));
            Assert.That(result.Graph.Edges.All(e => e.Kind == "import"), Is.True);
        }

        [Test]
        public void ImportsResolveRelativeToTheImportingFile()
        {
            MemoryFileSystem fs = Files("shared/base.css", ".base{x:1}", "pages/p/page.css", "@import \"../../shared/base.css\";");
            ImportExpansion result = ImportExpander.ExpandText("@import \"p/page.css\";", "pages/main.css", StyleOrigin.Page, fs, null);
            Assert.That(result.Sheets.Select(s => s.Path), Is.EqualTo(new[] { "shared/base.css", "pages/p/page.css", "pages/main.css" }));
        }

        [Test]
        public void MissingImportsAreJ002()
        {
            var bag = new DiagnosticBag();
            ImportExpansion result = ImportExpander.ExpandText("@import \"nope.css\";\n.a{x:1}", "main.css", StyleOrigin.Page, Files(), bag);
            Assert.That(Support.Ids(bag), Is.EqualTo("J002"));
            Assert.That(bag.Items[0].Severity, Is.EqualTo(Severity.Error));
            Assert.That(bag.Items[0].Span.Start.Line, Is.EqualTo(1));
            Assert.That(bag.Items[0].Span.File, Is.EqualTo("main.css"));
            Assert.That(result.Sheets.Select(s => s.Path), Is.EqualTo(new[] { "main.css" }));
        }

        [TestCase("http://x.example/a.css")]
        [TestCase("https://x.example/a.css")]
        [TestCase("//cdn.example/a.css")]
        [TestCase("data:text/css,a{}")]
        public void ExternalImportsAreJ002(string target)
        {
            var bag = new DiagnosticBag();
            ImportExpander.ExpandText("@import url(\"" + target + "\");", "main.css", StyleOrigin.Page, Files(), bag);
            Assert.That(Support.Ids(bag), Is.EqualTo("J002"));
            Assert.That(bag.Items[0].Message, Does.Contain("External"));
            Assert.That(ImportExpander.IsExternal(target), Is.True);
        }

        [Test]
        public void ImportCyclesAreJ003WithTheChain()
        {
            var bag = new DiagnosticBag();
            MemoryFileSystem fs = Files("a.css", "@import \"b.css\";", "b.css", "@import \"c.css\";", "c.css", "@import \"a.css\";");
            ImportExpansion result = ImportExpander.ExpandText(fs.ReadAllText("a.css"), "a.css", StyleOrigin.Page, fs, bag);
            Assert.That(Support.Ids(bag), Is.EqualTo("J003"));
            Assert.That(bag.Items[0].Message, Does.Contain("a.css -> b.css -> c.css -> a.css"));
            Assert.That(bag.Items[0].Span.File, Is.EqualTo("c.css"));
            Assert.That(result.Sheets.Select(s => s.Path), Is.EqualTo(new[] { "c.css", "b.css", "a.css" }));
        }

        [Test]
        public void SelfImportIsACycle()
        {
            var bag = new DiagnosticBag();
            MemoryFileSystem fs = Files("a.css", "@import \"a.css\";");
            ImportExpander.ExpandText(fs.ReadAllText("a.css"), "a.css", StyleOrigin.Page, fs, bag);
            Assert.That(Support.Ids(bag), Is.EqualTo("J003"));
        }

        [Test]
        public void DiamondImportsAreIncludedOnce()
        {
            MemoryFileSystem fs = Files("b.css", "@import \"d.css\";", "c.css", "@import \"d.css\";", "d.css", ".d{x:1}");
            var bag = new DiagnosticBag();
            ImportExpansion result = ImportExpander.ExpandText("@import \"b.css\";@import \"c.css\";", "a.css", StyleOrigin.Page, fs, bag);
            Assert.That(result.Sheets.Select(s => s.Path), Is.EqualTo(new[] { "d.css", "b.css", "c.css", "a.css" }));
            Assert.That(bag.Items, Is.Empty);
        }

        [Test]
        public void ImportedSyntaxErrorsCarryTheImportedFile()
        {
            var bag = new DiagnosticBag();
            MemoryFileSystem fs = Files("bad.css", ".a{oops}");
            ImportExpander.ExpandText("@import \"bad.css\";", "main.css", StyleOrigin.Page, fs, bag);
            Assert.That(bag.Items.Single().Span.File, Is.EqualTo("bad.css"));
        }

        [Test]
        public void ImportCascadeFeedsTheStyleEngine()
        {
            MemoryFileSystem fs = Files("base.css", ".x{color:red;margin-top:1px}");
            ImportExpansion result = ImportExpander.ExpandText("@import \"base.css\";\n.x{color:blue}", "page.css", StyleOrigin.Page, fs, null);
            var model = new HtmlToUitk.Core.Style.StyleEngine().Build(result.Sheets, new HtmlToUitk.Core.Style.StyleOptions(), null);
            DomElement element = Support.ByName(Support.Html("<div name=a class=x></div>"), "a");
            var style = model.Resolve(element, HtmlToUitk.Core.Style.PseudoStateSet.None);
            Assert.That(style.Properties["color"].Value.ToText(), Is.EqualTo("blue"));
            Assert.That(style.Properties["margin-top"].Value.ToText(), Is.EqualTo("1px"));
            Assert.That(model.Conflicts.Count, Is.EqualTo(1));
        }

        [TestCase("partials/card.html#card", "pages/home.html", "pages/partials/card.html", "card")]
        [TestCase("../shared.html#x", "pages/home.html", "shared.html", "x")]
        [TestCase("#local", "pages/home.html", "pages/home.html", "local")]
        [TestCase("card.html", "pages/home.html", "pages/card.html", null)]
        [TestCase("  card.html#c  ", "home.html", "card.html", "c")]
        public void ParsesIncludeReferences(string text, string from, string file, string id)
        {
            IncludeRef reference = IncludeRef.Parse(text, from);
            Assert.That(reference.File, Is.EqualTo(file));
            Assert.That(reference.Id, Is.EqualTo(id));
            Assert.That(reference.Key, Is.EqualTo(file + (id == null ? "" : "#" + id)));
        }

        [Test]
        public void EmptyIncludeReferencesAreNull()
        {
            Assert.That(IncludeRef.Parse("", "a.html"), Is.Null);
            Assert.That(IncludeRef.Parse("  ", "a.html"), Is.Null);
            Assert.That(IncludeRef.Parse(null, "a.html"), Is.Null);
        }

        private static IncludeGraph Graph(params string[] edges)
        {
            var nodes = new List<string>();
            var list = new List<IncludeEdge>();
            foreach (string edge in edges)
            {
                string[] parts = edge.Split('>');
                list.Add(new IncludeEdge { From = parts[0], To = parts[1], Kind = "include" });
                if (!nodes.Contains(parts[0])) nodes.Add(parts[0]);
                if (!nodes.Contains(parts[1])) nodes.Add(parts[1]);
            }
            return new IncludeGraph { Nodes = nodes, Edges = list };
        }

        [TestCase("a>b,b>c", null)]
        [TestCase("a>b,b>a", "a>b>a")]
        [TestCase("a>a", "a>a")]
        [TestCase("a>b,b>c,c>a", "a>b>c>a")]
        [TestCase("a>b,a>c,b>d,c>d", null)]
        [TestCase("x>a,a>b,b>c,c>b", "b>c>b")]
        [TestCase("", null)]
        public void FindsIncludeCycles(string edges, string expected)
        {
            IncludeGraph graph = Graph(edges.Length == 0 ? new string[0] : edges.Split(','));
            IReadOnlyList<string> cycle = IncludeGraphTools.FindCycle(graph);
            Assert.That(cycle == null ? null : string.Join(">", cycle), Is.EqualTo(expected));
        }

        [Test]
        public void PartialIndexFindsTemplatesByFileAndId()
        {
            MemoryFileSystem fs = Files("partials/card.html", "<template id=card><div class=card>c</div></template><template id=badge><b>b</b></template><template><i>no id</i></template>");
            PartialIndex index = PartialIndex.Load(fs, new[] { "partials/card.html", "partials/missing.html" }, null);
            Assert.That(index.Keys.OrderBy(k => k), Is.EqualTo(new[] { "partials/card.html#badge", "partials/card.html#card" }));
            Assert.That(index.Find(IncludeRef.Parse("partials/card.html#card", "pages/x.html")), Is.Null);
            Assert.That(index.Find(IncludeRef.Parse("card.html#card", "partials/x.html")).Tag, Is.EqualTo("template"));
            Assert.That(index.Find(null), Is.Null);
            Assert.That(index.Find(IncludeRef.Parse("card.html#zzz", "partials/x.html")), Is.Null);
        }

        [Test]
        public void CloneCopiesTheSubtreeWithFreshParents()
        {
            HtmlDocument document = Support.Html("<div class=a><span>x</span><br></div>");
            var original = (DomElement)document.Body.Children[0];
            DomElement copy = DomClone.Clone(original);
            Assert.That(Support.Dump(copy), Is.EqualTo(Support.Dump(original)));
            Assert.That(ReferenceEquals(copy, original), Is.False);
            Assert.That(ReferenceEquals(copy.Children[0], original.Children[0]), Is.False);
            Assert.That(ReferenceEquals(((DomElement)copy.Children[0]).Parent, copy), Is.True);
            Assert.That(ReferenceEquals(((DomText)((DomElement)copy.Children[0]).Children[0]).Parent, copy.Children[0]), Is.True);
            Assert.That(copy.Attributes[0], Is.Not.SameAs(original.Attributes[0]));
        }

        [Test]
        public void MemoryFileSystemBehaves()
        {
            MemoryFileSystem fs = Files("a/b.txt", "x", "a/c/d.txt", "y", "e.txt", "z");
            Assert.That(fs.FileExists("a\\b.txt"), Is.True);
            Assert.That(fs.FileExists("a/zzz"), Is.False);
            Assert.That(fs.DirectoryExists("a"), Is.True);
            Assert.That(fs.DirectoryExists("a/c"), Is.True);
            Assert.That(fs.DirectoryExists("q"), Is.False);
            Assert.That(fs.EnumerateFiles("a", false), Is.EqualTo(new[] { "a/b.txt" }));
            Assert.That(fs.EnumerateFiles("a", true), Is.EqualTo(new[] { "a/b.txt", "a/c/d.txt" }));
            Assert.That(fs.EnumerateFiles("", true).Count, Is.EqualTo(3));
            Assert.Throws<FileNotFoundException>(() => fs.ReadAllText("nope"));
            fs.WriteAllBytes("out/x.bin", new byte[] { 1, 2 });
            Assert.That(fs.ReadAllBytes("out/x.bin"), Is.EqualTo(new byte[] { 1, 2 }));
            fs.AddFile("bom.txt", new byte[] { 0xEF, 0xBB, 0xBF, 0x41 });
            Assert.That(fs.ReadAllText("bom.txt"), Is.EqualTo("A"));
        }

        [Test]
        public void DiskFileSystemRoundTrips()
        {
            string root = Path.Combine(Path.GetTempPath(), "htu-" + Guid.NewGuid().ToString("N"));
            try
            {
                var fs = new DiskFileSystem(root);
                fs.WriteAllBytes("a/b/c.txt", new byte[] { 0x68, 0x69 });
                fs.WriteAllBytes("a/d.txt", new byte[] { 0x21 });
                Assert.That(fs.FileExists("a/b/c.txt"), Is.True);
                Assert.That(fs.DirectoryExists("a/b"), Is.True);
                Assert.That(fs.ReadAllText("a/b/c.txt"), Is.EqualTo("hi"));
                Assert.That(fs.EnumerateFiles("a", true), Is.EqualTo(new[] { "a/b/c.txt", "a/d.txt" }));
                Assert.That(fs.EnumerateFiles("a", false), Is.EqualTo(new[] { "a/d.txt" }));
                Assert.That(fs.EnumerateFiles("missing", true), Is.Empty);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
