using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkEditorWatcherTests
    {
        [UnityTest]
        public IEnumerator RenameAwayFromASupportedExtensionReportsTheRemovedInput()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Audit20261023NeoHtmlToUitkEditor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string oldPath = Path.Combine(directory, "menu.html");
            string newPath = Path.Combine(directory, "menu.txt");
            File.WriteAllText(oldPath, "<button>Menu</button>");
            List<string> changed = new List<string>();
            using (HtmlToUitkWatcher watcher = new HtmlToUitkWatcher(0, new[] { ".html" }, paths => changed.AddRange(paths)))
            {
                try
                {
                    watcher.Watch(new[] { directory });
                    File.Move(oldPath, newPath);
                    double deadline = EditorApplication.timeSinceStartup + 5;
                    while (!changed.Contains(GlobMatcher.Normalize(oldPath)) && EditorApplication.timeSinceStartup < deadline)
                        yield return null;

                    Assert.That(changed, Does.Contain(GlobMatcher.Normalize(oldPath)));
                    Assert.That(changed, Does.Not.Contain(GlobMatcher.Normalize(newPath)));
                    Assert.That(File.Exists(oldPath), Is.False);
                    Assert.That(File.Exists(newPath), Is.True);
                }
                finally
                {
                    watcher.Dispose();
                    if (File.Exists(oldPath)) File.Delete(oldPath);
                    if (File.Exists(newPath)) File.Delete(newPath);
                    Directory.Delete(directory);
                }
            }
        }

        [Test]
        public void DisposedWatcherCannotAllocateNewDirectoryHandles()
        {
            HtmlToUitkWatcher watcher = new HtmlToUitkWatcher(0, Array.Empty<string>(), null);
            watcher.Dispose();

            Assert.Throws<ObjectDisposedException>(() => watcher.Watch(new[] { Path.GetTempPath() }));
            Assert.That(watcher.WatchedDirectories, Is.Zero);
        }
    }
}
