using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Neo.HtmlToUitk.Navigation;
using Neo.HtmlToUitk.Tooling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    /// <summary>Opt-in real CLI/import/render integration; no substituted converter or page backend.</summary>
    public sealed class Release11HtmlToUitkEndToEndTests
    {
        private bool _enteredPlayMode;

        [UnityTest]
        public IEnumerator PublishedCliImportsRendersNavigatesAndRebindsActualDocument()
        {
            string cli = Environment.GetEnvironmentVariable("NEO_HTMLTOUITK_E2E_CLI");
            string fixture = Environment.GetEnvironmentVariable("NEO_HTMLTOUITK_E2E_FIXTURE");
            string artifacts = Environment.GetEnvironmentVariable("NEO_HTMLTOUITK_E2E_ARTIFACTS");
            if (string.IsNullOrEmpty(cli) || !File.Exists(cli) || string.IsNullOrEmpty(fixture) || !Directory.Exists(fixture))
                Assert.Ignore("Configure the published CLI and immutable production-neutral fixture for real end-to-end verification.");
            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("This integration requires a visible Editor/Game View and a real graphics device.");

            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            string nonce = Guid.NewGuid().ToString("N");
            string inputRoot = Path.Combine(Path.GetTempPath(), "NeoHtmlToUitkRelease11-" + nonce);
            string assetRoot = "Assets/__NeoHtmlToUitkRelease11_" + nonce;
            string outputRoot = Path.Combine(HtmlToUitkPaths.ProjectRoot, assetRoot, "Output");
            IJobBridge previousBridge = HtmlToUitkRunner.BridgeOverride;
            bool previousManagedImport = HtmlToUitkSettings.ManageTextureImport;
            bool hadManagedImportPreference = EditorPrefs.HasKey("Neoxider.HtmlToUitk.textures");
            GameObject host = null;
            PanelSettings panel = null;
            RenderTexture target = null;
            IDisposable action = null;
            IDisposable saveAction = null;
            try
            {
                CopyFixture(fixture, inputRoot);
                Directory.CreateDirectory(outputRoot);
                string jobPath = Path.Combine(inputRoot, "neutral.htujob.json");
                byte[] originalJob = File.ReadAllBytes(jobPath);
                JobDocument job = JobDocument.Parse(File.ReadAllText(jobPath));
                job.OutputRoot = outputRoot.Replace('\\', '/');
                HtmlToUitkRunner.BridgeOverride = new CliProcessBridge(cli);
                HtmlToUitkSettings.ManageTextureImport = true;
                JobRunResult converted = HtmlToUitkRunner.Run(job, jobPath, new RunOptions());
                Assert.That(converted.Succeeded, Is.True, converted.StdErr + "\n" + converted.StdOut);
                Assert.That(converted.BridgeName, Is.EqualTo("cli"));
                Dictionary<string, string> firstOutput = HashFiles(outputRoot);
                Assert.That(firstOutput.ContainsKey("Screens/Neutral.uxml"), Is.True);
                Assert.That(firstOutput.ContainsKey("Screens/Profile.uxml"), Is.True);
                Assert.That(firstOutput.ContainsKey("generated-assets.json"), Is.True);
                JobRunResult repeated = HtmlToUitkRunner.Run(job, jobPath, new RunOptions());
                Assert.That(repeated.Succeeded, Is.True, repeated.StdErr);
                CollectionAssert.AreEquivalent(firstOutput, HashFiles(outputRoot), "Repeated conversion must preserve every output byte.");
                JobRunResult validated = HtmlToUitkRunner.Run(job, jobPath, new RunOptions { ValidateOnly = true });
                Assert.That(validated.Succeeded, Is.True, validated.StdErr);
                CollectionAssert.AreEquivalent(firstOutput, HashFiles(outputRoot), "Validate Only must not replace generated files.");
                CollectionAssert.AreEqual(originalJob, File.ReadAllBytes(jobPath));
                Assert.That(Directory.GetFiles(inputRoot, ".htu-run-*.htujob.json"), Is.Empty);

                string themePath = assetRoot + "/RuntimeTheme.tss";
                File.WriteAllText(Path.Combine(HtmlToUitkPaths.ProjectRoot, themePath), "@import url(\"unity-theme://default\");\n");
                HtmlToUitkRunner.RefreshAssets(job, jobPath);
                AssetDatabase.ImportAsset(themePath, ImportAssetOptions.ForceSynchronousImport);
                string screenRoot = assetRoot + "/Output/Screens/";
                VisualTreeAsset neutral = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(screenRoot + "Neutral.uxml");
                VisualTreeAsset profile = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(screenRoot + "Profile.uxml");
                TextAsset nav = AssetDatabase.LoadAssetAtPath<TextAsset>(screenRoot + "Neutral.nav.json");
                TextAsset anim = AssetDatabase.LoadAssetAtPath<TextAsset>(screenRoot + "Neutral.anim.json");
                TextAsset media = AssetDatabase.LoadAssetAtPath<TextAsset>(screenRoot + "Neutral.mq.json");
                ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                Assert.That(neutral, Is.Not.Null); Assert.That(profile, Is.Not.Null);
                Assert.That(nav, Is.Not.Null); Assert.That(anim, Is.Not.Null); Assert.That(media, Is.Not.Null); Assert.That(theme, Is.Not.Null);
                string[] pngFiles = Directory.GetFiles(outputRoot, "*.png", SearchOption.AllDirectories);
                Assert.That(pngFiles.Length, Is.EqualTo(7), "The immutable neutral fixture produces seven distinct gradients.");
                foreach (string png in pngFiles)
                {
                    string relative = HtmlToUitkPaths.ToProjectRelative(png);
                    TextureImporter importer = AssetImporter.GetAtPath(relative) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, relative);
                    Assert.That(importer.sRGBTexture, Is.True, relative);
                    Assert.That(importer.mipmapEnabled, Is.False, relative);
                    Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), relative);
                }

                target = new RenderTexture(1280, 720, 0); target.Create();
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
                panel.themeStyleSheet = theme;
                panel.targetTexture = target;
                host = new GameObject("NeoHtmlToUitkRelease11-" + nonce); host.SetActive(false);
                UIDocument document = host.AddComponent<UIDocument>();
                document.panelSettings = panel;
                document.visualTreeAsset = neutral;
                UitkScreen screen = host.AddComponent<UitkScreen>();
                int actionCalls = 0;
                int saveCalls = 0;
                action = screen.RegisterAction("app-home-settings", () => actionCalls++);
                saveAction = screen.RegisterAction("app-save", () => saveCalls++);
                screen.Configure(document, nav, anim, media);
                host.SetActive(true);
                yield return WaitForDocument(screen, document, "app-root");
                Assert.That(screen.LastError, Is.Null);
                Assert.That(screen.Root.panel, Is.Not.Null);
                Assert.That(screen.Router, Is.Not.Null);
                Assert.That(screen.Router.Current, Is.EqualTo("home"));
                Assert.That(screen.MissingElements, Is.Empty);
                yield return new WaitForEndOfFrame();
                CaptureAndAssertGradient(target, screen.Root, artifacts);

                Submit(RequiredButton(screen.Root, "app-home-settings"));
                yield return WaitForTransition(screen);
                Assert.That(screen.Router.Current, Is.EqualTo("settings"));
                Assert.That(actionCalls, Is.EqualTo(1));
                Submit(RequiredButton(screen.Root, "app-tab-video"));
                Assert.That(screen.Router.SelectedTab("settings-tabs"), Is.EqualTo("app-tab-video"));
                Assert.That(screen.Root.Q<VisualElement>("app-panel-video").resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
                Assert.That(screen.Root.Q<VisualElement>("app-panel-audio").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Submit(RequiredButton(screen.Root, "app-settings-back"));
                yield return WaitForTransition(screen);
                Assert.That(screen.Router.Current, Is.EqualTo("home"));
                Submit(RequiredButton(screen.Root, "app-home-quit"));
                yield return WaitForTransition(screen);
                Assert.That(screen.Router.Modals, Does.Contain("confirm"));
                Submit(RequiredButton(screen.Root, "app-confirm-cancel"));
                yield return WaitForTransition(screen);
                Assert.That(screen.Router.Modals, Is.Empty);

                screen.enabled = false;
                Submit(RequiredButton(document.rootVisualElement, "app-home-settings"));
                Assert.That(actionCalls, Is.EqualTo(1), "A disabled screen must release native callbacks.");
                screen.enabled = true;
                yield return WaitForDocument(screen, document, "app-root");
                Submit(RequiredButton(screen.Root, "app-home-settings"));
                yield return WaitForTransition(screen);
                Assert.That(actionCalls, Is.EqualTo(2), "Re-enabling must bind one callback, not accumulate old registrations.");

                screen.Configure(document, null);
                document.visualTreeAsset = profile;
                yield return WaitForDocument(screen, document, "app-profile");
                Assert.That(screen.Root, Is.SameAs(document.rootVisualElement));
                Assert.That(screen.Root.Q<VisualElement>("app-profile"), Is.Not.Null);
                Submit(RequiredButton(screen.Root, "app-save"));
                Assert.That(saveCalls, Is.EqualTo(1));
            }
            finally
            {
                action?.Dispose(); saveAction?.Dispose();
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                HtmlToUitkRunner.BridgeOverride = previousBridge;
                if (hadManagedImportPreference) HtmlToUitkSettings.ManageTextureImport = previousManagedImport;
                else EditorPrefs.DeleteKey("Neoxider.HtmlToUitk.textures");
                if (AssetDatabase.IsValidFolder(assetRoot)) AssetDatabase.DeleteAsset(assetRoot);
                string physicalAssetRoot = Path.GetFullPath(Path.Combine(HtmlToUitkPaths.ProjectRoot, assetRoot));
                string expectedParent = Path.GetFullPath(Application.dataPath);
                if (!string.Equals(Path.GetDirectoryName(physicalAssetRoot), expectedParent, StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(physicalAssetRoot) != "__NeoHtmlToUitkRelease11_" + nonce)
                    throw new InvalidOperationException("Refusing cleanup outside the exact fixture-owned Assets directory.");
                if (Directory.Exists(physicalAssetRoot)) Directory.Delete(physicalAssetRoot, true);
                if (File.Exists(physicalAssetRoot + ".meta")) File.Delete(physicalAssetRoot + ".meta");
                if (Directory.Exists(inputRoot)) Directory.Delete(inputRoot, true);
            }
        }

        [UnityTearDown]
        public IEnumerator ExitOwnedPlayMode()
        {
            if (!_enteredPlayMode) yield break;
            _enteredPlayMode = false;
            yield return new ExitPlayMode();
        }

        private static Button RequiredButton(VisualElement root, string name)
        {
            Button button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            return button;
        }

        private static void Submit(Button button)
        {
            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        private static IEnumerator WaitForTransition(UitkScreen screen)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (screen.Router != null && screen.Router.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(screen.Router, Is.Not.Null);
            Assert.That(screen.Router.IsTransitioning, Is.False, "Native transition did not settle within five seconds.");
        }

        private static IEnumerator WaitForDocument(UitkScreen screen, UIDocument document, string elementName)
        {
            // Observe an actual Update boundary before accepting a replacement root/tree.
            yield return null;
            float deadline = Time.realtimeSinceStartup + 5;
            while ((screen.Root == null || screen.Root != document.rootVisualElement || screen.Root.panel == null
                || screen.Root.Q<VisualElement>(elementName) == null) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(screen.Root, Is.Not.Null);
            Assert.That(document.rootVisualElement, Is.Not.Null);
            Assert.That(screen.Root, Is.SameAs(document.rootVisualElement));
            Assert.That(screen.Root.panel, Is.Not.Null);
            Assert.That(screen.Root.Q<VisualElement>(elementName), Is.Not.Null);
        }

        private static void CopyFixture(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith("out/", StringComparison.Ordinal) || relative.StartsWith("out\\", StringComparison.Ordinal)) continue;
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target);
            }
        }

        private static Dictionary<string, string> HashFiles(string directory)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                using (SHA256 algorithm = SHA256.Create())
                using (FileStream input = File.OpenRead(file))
                    result.Add(file.Substring(directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/'), BitConverter.ToString(algorithm.ComputeHash(input)));
            }
            return result;
        }

        private static void CaptureAndAssertGradient(RenderTexture target, VisualElement root, string artifacts)
        {
            VisualElement gradient = root.Q<VisualElement>("app-linear");
            Assert.That(gradient, Is.Not.Null);
            Rect bounds = gradient.worldBound;
            Assert.That(float.IsNaN(bounds.xMin) || float.IsNaN(bounds.yMin) || float.IsNaN(bounds.xMax) || float.IsNaN(bounds.yMax)
                || float.IsInfinity(bounds.xMin) || float.IsInfinity(bounds.yMin) || float.IsInfinity(bounds.xMax) || float.IsInfinity(bounds.yMax), Is.False);
            Assert.That(bounds.width, Is.GreaterThan(100));
            Assert.That(bounds.height, Is.GreaterThan(30));
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(target.width)); Assert.That(bounds.yMax, Is.LessThanOrEqualTo(target.height));
            Background background = gradient.resolvedStyle.backgroundImage;
            Texture2D nativeTexture = background.texture != null ? background.texture : background.sprite != null ? background.sprite.texture : null;
            Assert.That(nativeTexture, Is.Not.Null, "The generated PNG must resolve to a native Texture2D or Sprite texture.");
            string importedPath = AssetDatabase.GetAssetPath(nativeTexture).Replace('\\', '/');
            Assert.That(importedPath, Does.Contain("/Output/Generated/gradient-"));
            Assert.That(importedPath, Does.EndWith(".png"));
            RenderTexture previous = RenderTexture.active;
            Texture2D capture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); capture.Apply();
                if (!string.IsNullOrEmpty(artifacts))
                {
                    Directory.CreateDirectory(artifacts);
                    File.WriteAllBytes(Path.Combine(artifacts, "HtmlToUitk-native-" + Application.unityVersion + ".png"), capture.EncodeToPNG());
                }
                int y = target.height - 1 - Mathf.FloorToInt(bounds.center.y);
                int left = Mathf.FloorToInt(bounds.xMin + 8);
                int right = Mathf.FloorToInt(bounds.xMax - 8);
                Color a = capture.GetPixel(left, y); Color b = capture.GetPixel(right, y);
                Assert.That(a.r - a.b, Is.GreaterThan(0.3f), "The authored linear gradient starts red on the left.");
                Assert.That(b.b - b.r, Is.GreaterThan(0.1f), "The authored linear gradient ends blue on the right.");
                Assert.That(Mathf.Abs(a.r - b.r) + Mathf.Abs(a.b - b.b), Is.GreaterThan(0.2f), "Actual native panel pixels must show the generated gradient, not a flat/empty target.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }
    }
}
