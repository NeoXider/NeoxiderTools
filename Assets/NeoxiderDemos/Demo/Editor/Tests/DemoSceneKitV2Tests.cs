using System;
using System.Collections.Generic;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Contracts of the demo kit v2 API: marker-less roots, matchable kept subtrees, ctx.Retire, owned listeners
    ///     on hand-authored events and their validation, the NeoxiderPages sample check, value / bar row parts, the
    ///     authored collapsed info card, the input-module upgrade, multi-argument wiring and the Game view capture.
    /// </summary>
    public sealed class DemoSceneKitV2Tests
    {
        private Scene _scene;
        private Scene _previousActive;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
            _scene = DemoTestScenes.OpenEmpty("__NeoDemoKitV2Tests", out _folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }
        }

        [Test]
        public void MarkerlessRoots_AreFoundByNameCountAsGeneratedAndStaleOnesAreRemoved()
        {
            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, ctx =>
            {
                ctx.Child(ctx.Root("Overlay", true).transform, "Label");
                ctx.Root("Old");
            }, false);
            Assert.That(first.Success, Is.True, first.Summary());
            GameObject overlay = FindRoot("[Demo] Overlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.GetComponent<DemoGeneratedRoot>(), Is.Null, "Marker-less roots carry no sample component.");
            Assert.That(DemoSceneBuildContext.IsGenerated(overlay.transform.GetChild(0).gameObject), Is.True);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene,
                ctx => ctx.Child(ctx.Root("Overlay", true).transform, "Label"), false);
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(FindRoot("[Demo] Overlay"), Is.SameAs(overlay));
            Assert.That(FindRoot("[Demo] Old"), Is.Null, "A marker-less root the builder no longer makes is stale.");
        }

        [Test]
        public void MarkerlessScenes_RejectComponentsOfTheDemoSampleAssembly()
        {
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene,
                ctx => ctx.Ensure<DemoLogFeed>(ctx.Root("Overlay", true)), false);

            Assert.That(result.Success, Is.False);
            Assert.That(string.Join("\n", result.Errors), Does.Contain("Demo sample assembly"));
        }

        [Test]
        public void KeptSubtrees_StayMatchableSoChildrenUnderAPrimitiveAreReused()
        {
            Action<DemoSceneBuildContext> build = ctx =>
            {
                GameObject body = ctx.Primitive(ctx.Root("World").transform, "Body", PrimitiveType.Cube);
                ctx.Child(body.transform, "Marker");
            };

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, build);
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, build);

            Assert.That(first.Success && second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(FindRoot("[Demo] World").transform.Find("Body").childCount, Is.EqualTo(1));
        }

        [Test]
        public void Retire_DeletesHandAuthoredObjectsCountsThemAndRefusesGeneratedOnes()
        {
            GameObject legacy = new("Legacy Canvas");
            new GameObject("Child").transform.SetParent(legacy.transform, false);
            bool refused = false;

            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, ctx =>
            {
                GameObject generated = ctx.Root("Frame");
                try
                {
                    ctx.Retire(generated, "not allowed");
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }

                Assert.That(ctx.Retire(ctx.FindHandAuthoredRoot("Legacy Canvas"), "pre-kit layout"), Is.True);
            });

            Assert.That(result.Success, Is.True, result.Summary());
            Assert.That(refused, Is.True, "Generated objects are pruned by the context, never retired.");
            Assert.That(FindRoot("Legacy Canvas"), Is.Null);
            Assert.That(result.Removed, Is.EqualTo(2));
            Assert.That(string.Join("\n", result.Notes), Does.Contain("Retired hand-authored 'Legacy Canvas' (pre-kit layout)"));
        }

        [Test]
        public void OwnListeners_KeepsTheAuthorsCallsAndRebuildsIdempotently()
        {
            GameObject author = new("Author", typeof(RectTransform));
            Button button = author.AddComponent<Button>();
            DemoLogFeed authorFeed = author.AddComponent<DemoLogFeed>();
            UnityEventTools.AddStringPersistentListener(button.onClick, authorFeed.Log, "author");
            GameObject gone = new("Gone");
            DemoLogFeed goneFeed = gone.AddComponent<DemoLogFeed>();
            UnityEventTools.AddStringPersistentListener(button.onClick, goneFeed.Log, "stale");
            Object.DestroyImmediate(gone);

            Action<DemoSceneBuildContext> build = ctx =>
            {
                DemoLogFeed feed = ctx.Ensure<DemoLogFeed>(ctx.Child(ctx.Root("Services").transform, "Feed"));
                ctx.OwnListeners(button, button.onClick);
                DemoWiring.AddString(button.onClick, feed.Log, "generated");
            };

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, build);
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, build);

            Assert.That(first.Success && second.Success, Is.True, first.Summary() + "\n" + second.Summary());
            Assert.That(DemoWiring.Count(button.onClick), Is.EqualTo(2), DemoWiring.Describe(button.onClick));
            Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(authorFeed));
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
        }

        [Test]
        public void TrackedListeners_WithAMissingTargetFailValidation()
        {
            GameObject author = new("Author", typeof(RectTransform));
            Button button = author.AddComponent<Button>();

            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, ctx =>
            {
                GameObject temporary = new("Temporary");
                DemoLogFeed feed = temporary.AddComponent<DemoLogFeed>();
                UnityEventTools.AddStringPersistentListener(button.onClick, feed.Log, "lost");
                Object.DestroyImmediate(temporary);
                ctx.TrackListeners(button, button.onClick);
            });

            Assert.That(result.Success, Is.False);
            Assert.That(string.Join("\n", result.Errors), Does.Contain("without a target"));
        }

        [Test]
        public void PruneOwned_RemovesMissingGeneratedAndOwnerTargetsOnly()
        {
            GameObject host = new("Host", typeof(RectTransform));
            Button button = host.AddComponent<Button>();
            DemoLogFeed keep = host.AddComponent<DemoLogFeed>();
            GameObject ownerObject = new("Owner");
            DemoValueReadout owner = ownerObject.AddComponent<DemoValueReadout>();
            UnityEventTools.AddStringPersistentListener(button.onClick, keep.Log, "keep");
            UnityEventTools.AddStringPersistentListener(button.onClick, owner.SetText, "owned");

            int removed = DemoWiring.PruneOwned(button.onClick, owner);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(DemoWiring.Describe(button.onClick), Is.EqualTo("DemoLogFeed.Log"));
            Assert.That(DemoWiring.PruneOwned(null), Is.Zero);
        }

        [Test]
        public void AddDynamic_WiresTwoArgumentEventsOnce()
        {
            GameObject host = new("Animated");
            Animator animator = host.AddComponent<Animator>();
            UnityEvent<string, float> unityEvent = new();

            DemoWiring.AddDynamic<string, float>(unityEvent, animator.SetFloat);
            DemoWiring.AddDynamic<string, float>(unityEvent, animator.SetFloat);

            Assert.That(DemoWiring.Count(unityEvent), Is.EqualTo(1));
            Assert.That(DemoWiring.Describe(unityEvent), Is.EqualTo("Animator.SetFloat"));
        }

        [Test]
        public void Setter_WiresAPropertySetterLikeTheInspector()
        {
            GameObject host = new("Toggle", typeof(RectTransform));
            Toggle toggle = host.AddComponent<Toggle>();
            Image image = host.AddComponent<Image>();

            DemoWiring.AddSetter(toggle.onValueChanged, image, nameof(Behaviour.enabled));

            Assert.That(DemoWiring.Describe(toggle.onValueChanged), Is.EqualTo("Image.set_enabled"));
            Assert.Throws<ArgumentException>(() => DemoWiring.Setter<float>(image, "noSuchProperty"));
        }

        [Test]
        public void PanelRows_ReturnEveryPartAndFilledBarsUseAFilledImage()
        {
            DemoValueRow value = default;
            DemoBarRow bar = default;
            DemoBarRow filled = default;
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, ctx =>
            {
                DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Rows", DemoPalette.Cyan)
                {
                    HeaderWidth = 1200f,
                    LogWidth = 900f
                });
                value = frame.Content.ValueRow("Score", "0", true);
                bar = frame.Content.BarRow("HP", DemoPalette.Good, 0.5f);
                filled = frame.Content.BarRow("Mana", DemoPalette.Cyan, 0.25f, true);
                frame.Content.LogRow(3, "ready");
            });

            Assert.That(result.Success, Is.True, result.Summary());
            Assert.That(value.Caption.text, Is.EqualTo("Score"));
            Assert.That(value.Value.enableAutoSizing, Is.True);
            Assert.That(bar.Caption, Is.Not.Null);
            Assert.That(bar.Fill.rectTransform.anchorMax.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(filled.Fill.type, Is.EqualTo(Image.Type.Filled));
            Assert.That(filled.Fill.fillAmount, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(filled.Fill.sprite, Is.Not.Null);
            Assert.That(((RectTransform)FindRoot("[Demo] Frame").transform.Find("Header")).sizeDelta.x,
                Is.EqualTo(1200f).Within(0.01f));
            Assert.That(((RectTransform)FindRoot("[Demo] Frame").transform.Find("LogFeed")).sizeDelta.x,
                Is.EqualTo(900f).Within(0.01f));
        }

        [Test]
        public void InfoCard_IsAuthoredInItsStartCollapsedState()
        {
            ModuleDemoSceneInfo info = null;
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, ctx =>
            {
                info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Info");
                DemoSceneKit.InfoCardCanvas(ctx, info, new DemoInfoContent
                {
                    Title = "Collapsed",
                    Purpose = "Starts collapsed",
                    StartCollapsed = true
                }, "Info Card", DemoPalette.Warm);
            });

            Assert.That(result.Success, Is.True, result.Summary());
            Assert.That(info.IsCollapsed, Is.True);
            Assert.That(info.OverlayRoot.transform.Find("Body").gameObject.activeSelf, Is.False);
            Assert.That(info.AccentColor, Is.EqualTo(DemoPalette.Warm));
        }

        [Test]
        public void UpgradeLegacyInputModule_ReplacesStandaloneInputModuleOnHandAuthoredEventSystems()
        {
            Type preferred = DemoSceneKit.ResolveInputModule(DemoInputModule.Auto);
            Assume.That(preferred, Is.Not.EqualTo(typeof(StandaloneInputModule)), "Needs the Input System package.");
            GameObject host = new("EventSystem");
            host.AddComponent<EventSystem>();
            host.AddComponent<StandaloneInputModule>();

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene,
                ctx => DemoSceneKit.Frame(ctx, new DemoFrameOptions("Upgrade", DemoPalette.Accent)));
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene,
                ctx => DemoSceneKit.Frame(ctx, new DemoFrameOptions("Upgrade", DemoPalette.Accent)));

            Assert.That(first.Success && second.Success, Is.True, first.Summary());
            Assert.That(host.GetComponent<StandaloneInputModule>(), Is.Null);
            Assert.That(host.GetComponent(preferred), Is.Not.Null);
            Assert.That(string.Join("\n", first.Notes), Does.Contain("Replaced StandaloneInputModule"));
            Assert.That(string.Join("\n", second.Notes), Does.Not.Contain("Replaced StandaloneInputModule"));
        }

        [Test]
        public void ShotSize_ParsesWidthByHeight()
        {
            Assert.That(DemoSceneBatch.TryParseSize("1280x720", out int width, out int height), Is.True);
            Assert.That(width, Is.EqualTo(1280));
            Assert.That(height, Is.EqualTo(720));
            Assert.That(DemoSceneBatch.TryParseSize(null, out width, out height), Is.True);
            Assert.That(width, Is.EqualTo(DemoSceneCapture.DefaultWidth));
            Assert.That(DemoSceneBatch.TryParseSize("wide", out _, out _), Is.False);
            Assert.That(DemoSceneBatch.TryParseSize("8x8", out _, out _), Is.False);
        }

        [Test]
        public void Capture_DrawsTheCameraAndOverlayCanvasAndRestoresTheCanvas()
        {
            Assume.That(DemoSceneCapture.HasGraphicsDevice, Is.True, "Captures need a graphics device (no -nographics).");
            GameObject cameraHost = new("Main Camera");
            Camera camera = cameraHost.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.red;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject canvasHost = new("Canvas", typeof(RectTransform));
            Canvas canvas = canvasHost.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasHost.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            GameObject panelHost = new("Panel", typeof(RectTransform));
            panelHost.transform.SetParent(canvasHost.transform, false);
            Image panel = panelHost.AddComponent<Image>();
            panel.color = Color.green;
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.25f, 0.25f);
            rect.anchorMax = new Vector2(0.75f, 0.75f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            List<string> warnings = new();
            Texture2D shot = DemoSceneCapture.Render(_scene, 320, 180, warnings);
            try
            {
                Assert.That(shot.width, Is.EqualTo(320));
                Assert.That(shot.height, Is.EqualTo(180));
                Color corner = shot.GetPixel(8, 8);
                Color center = shot.GetPixel(160, 90);
                Assert.That(corner.r, Is.GreaterThan(corner.g), "The camera's red clear color fills the corners.");
                Assert.That(center.g, Is.GreaterThan(center.r), "The overlay canvas is drawn on top of the camera.");
            }
            finally
            {
                Object.DestroyImmediate(shot);
            }

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), "The canvas is restored.");
            Assert.That(canvas.worldCamera, Is.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
        }

        private GameObject FindRoot(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
