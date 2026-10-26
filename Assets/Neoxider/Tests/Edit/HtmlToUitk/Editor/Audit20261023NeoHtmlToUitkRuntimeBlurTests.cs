using System;
using NUnit.Framework;
using Neo.HtmlToUitk.Effects;
using Neo.HtmlToUitk.Responsive;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class Audit20261023NeoHtmlToUitkRuntimeBlurTests
    {
        private sealed class Host : IDisposable
        {
            private readonly GameObject _object = new GameObject("HtmlToUitk blur audit");
            private readonly PanelSettings _settings = ScriptableObject.CreateInstance<PanelSettings>();
            public readonly Texture2D Texture = new Texture2D(2, 2);
            public readonly VisualElement First = new VisualElement { name = "first" };
            public readonly VisualElement Second = new VisualElement { name = "second" };
            public UIDocument Document { get; }
            public BackdropBlurPass Pass { get; }

            public Host()
            {
                try
                {
                    _object.SetActive(false);
                    Document = _object.AddComponent<UIDocument>();
                    Document.panelSettings = _settings;
                    Pass = _object.AddComponent<BackdropBlurPass>();
                    _object.SetActive(true);
                    First.style.backgroundImage = Background.FromTexture2D(Texture);
                    First.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    Second.style.backgroundImage = Background.FromTexture2D(Texture);
                    Second.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    Document.rootVisualElement.Add(First);
                    Document.rootVisualElement.Add(Second);
                }
                catch { Dispose(); throw; }
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(_object);
                UnityEngine.Object.DestroyImmediate(_settings);
                UnityEngine.Object.DestroyImmediate(Texture);
            }
        }

        [Test]
        public void ReleaseWithoutAFallbackSourcePreservesTheAuthoredBackground()
        {
            using (Host host = new Host())
            {
                StyleBackground image = host.First.style.backgroundImage;
                StyleBackgroundSize size = host.First.style.backgroundSize;
                host.Pass.Configure(host.Document, "first", null, null);
                host.Pass.Refresh();
                host.Pass.Release();
                host.Pass.Release();
                Assert.That(host.First.style.backgroundImage, Is.EqualTo(image));
                Assert.That(host.First.style.backgroundSize, Is.EqualTo(size));
                Assert.That(host.Pass.Output, Is.Null);
                Assert.That(host.Pass.IsUsingNative, Is.False);
            }
        }

        [Test]
        public void FallbackRetargetAndReleaseRestoreBackgroundsAndReleaseTheOutput()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("RenderTexture/Blit ownership regression requires a graphics device.");
            using (Host host = new Host())
            {
                using (SerializedObject serialized = new SerializedObject(host.Pass))
                {
                    serialized.FindProperty("preferNative").boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                StyleBackground firstImage = host.First.style.backgroundImage;
                StyleBackgroundSize firstSize = host.First.style.backgroundSize;
                StyleBackground secondImage = host.Second.style.backgroundImage;
                StyleBackgroundSize secondSize = host.Second.style.backgroundSize;
                host.Pass.Configure(host.Document, "first", null, host.Texture);
                RenderTexture previous = RenderTexture.active;
                RenderTexture sentinel = RenderTexture.GetTemporary(4, 4, 0);
                try
                {
                    RenderTexture.active = sentinel;
                    host.Pass.Refresh();
                    Assert.That(RenderTexture.active, Is.SameAs(sentinel));
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(sentinel);
                }
                RenderTexture firstOutput = host.Pass.Output;
                Assert.That(firstOutput, Is.Not.Null);
                Background nativeBackground = Background.FromRenderTexture(firstOutput);
                Assert.That(nativeBackground.renderTexture, Is.SameAs(firstOutput));
                Assert.That(nativeBackground.texture, Is.Null);
                // Unity 6.3's inline getter omits RenderTexture; resolvedStyle reads the applied background.
                Assert.That(host.First.resolvedStyle.backgroundImage.renderTexture, Is.SameAs(firstOutput));
                Assert.That(host.Pass.IsUsingNative, Is.False);
                host.Pass.Refresh();
                Assert.That(host.Pass.Output, Is.SameAs(firstOutput));
                Assert.That(host.First.resolvedStyle.backgroundImage.renderTexture, Is.SameAs(firstOutput));
                Assert.That(host.Second.style.backgroundImage, Is.EqualTo(secondImage));
                Assert.That(host.Second.style.backgroundSize, Is.EqualTo(secondSize));
                host.Pass.Configure(host.Document, "second", null, host.Texture);
                Assert.That(host.First.style.backgroundImage, Is.EqualTo(firstImage));
                Assert.That(host.First.style.backgroundSize, Is.EqualTo(firstSize));
                Assert.That(firstOutput == null || !firstOutput.IsCreated(), Is.True);
                host.Pass.Refresh();
                RenderTexture secondOutput = host.Pass.Output;
                Assert.That(secondOutput, Is.Not.Null);
                Assert.That(secondOutput, Is.Not.SameAs(firstOutput));
                Assert.That(host.Second.resolvedStyle.backgroundImage.renderTexture, Is.SameAs(secondOutput));
                Assert.That(host.First.style.backgroundImage, Is.EqualTo(firstImage));
                Assert.That(host.First.style.backgroundSize, Is.EqualTo(firstSize));
                host.Pass.Release();
                Assert.That(host.Second.style.backgroundImage, Is.EqualTo(secondImage));
                Assert.That(host.Second.style.backgroundSize, Is.EqualTo(secondSize));
                Assert.That(secondOutput == null || !secondOutput.IsCreated(), Is.True);
                Assert.That(host.Pass.Output, Is.Null);
                Assert.That(host.Pass.IsUsingNative, Is.False);
                host.Pass.Release();
                Assert.That(host.Second.style.backgroundImage, Is.EqualTo(secondImage));
                Assert.That(host.Second.style.backgroundSize, Is.EqualTo(secondSize));
                Assert.That(secondOutput == null || !secondOutput.IsCreated(), Is.True);
                Assert.That(host.Pass.Output, Is.Null);
                Assert.That(host.Pass.IsUsingNative, Is.False);
            }
        }

        [Test]
        public void IncompleteMediaSelectorDoesNotAcquireResponsiveOwnership()
        {
            VisualElement root = new VisualElement();
            BreakpointSet invalid = new BreakpointSet(null, new[] { new BreakpointRule("wide", new BreakpointQuery(), ".card >") });
            Assert.Throws<FormatException>(() => new BreakpointController(root, invalid));
            using (BreakpointController controller = new BreakpointController(root,
                       new BreakpointSet(null, new[] { new BreakpointRule("wide", new BreakpointQuery()) })))
            {
                controller.ApplyViewport(1280, 720);
                Assert.That(root.ClassListContains("wide"), Is.True);
            }
            Assert.That(root.ClassListContains("wide"), Is.False);
        }
    }
}
