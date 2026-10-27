using System;
using System.Collections;
using System.Collections.Generic;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Localization;
using Neo.HtmlToUitk.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VeloraShowcase.QA
{
    /// <summary>
    /// Exercises the delivered Resources tree and public adapter via native UI Toolkit submit events.
    /// An isolated real panel is used; these tests do not claim mouse hit testing or visual parity.
    /// </summary>
    public sealed class VeloraShowcaseInteractionTests
    {
        private GameObject _host;
        private UIDocument _document;
        private UitkScreen _screen;
        private VeloraShowcaseController _controller;
        private PanelSettings _panel;
        private RenderTexture _target;
        private VisualTreeAsset _tree;
        private Func<bool> _previousReducedMotion;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousReducedMotion = UitkAnimator.SystemReducedMotion;
            if (!Application.isPlaying)
                Assert.Ignore("Run this assembly in Unity Play Mode.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("An attached native UI Toolkit panel requires a real graphics device.");

            UitkAnimator.SystemReducedMotion = null;
            _tree = Resources.Load<VisualTreeAsset>("Velora/Screens/Velora");
            Assert.That(_tree, Is.Not.Null, "Integrate the delivered Resources/Velora folder before running QA.");
            ThemeStyleSheet theme = FindTheme();
            Assert.That(theme, Is.Not.Null, "The delivered RuntimeTheme.tss must be imported.");

            _target = new RenderTexture(1280, 900, 0);
            _target.Create();
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            _panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panel.scale = 1f;
            _panel.themeStyleSheet = theme;
            _panel.targetTexture = _target;
            _host = new GameObject("VeloraShowcase-QA-" + Guid.NewGuid().ToString("N"));
            _host.SetActive(false);
            _document = _host.AddComponent<UIDocument>();
            _document.panelSettings = _panel;
            _document.visualTreeAsset = _tree;
            _screen = _host.AddComponent<UitkScreen>();
            _controller = _host.AddComponent<VeloraShowcaseController>();
            _host.SetActive(true);
            yield return WaitForReady();
            yield return WaitForIdle();
            Assert.That(_screen.LastError, Is.Null);
            Assert.That(_screen.MissingElements, Is.Empty);
            Assert.That(_screen.Router.Current, Is.EqualTo("home"));
            Assert.That(_controller.CurrentPage, Is.EqualTo("home"));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) UnityEngine.Object.Destroy(_host);
            yield return null;
            if (_panel != null) UnityEngine.Object.Destroy(_panel);
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }
            UitkAnimator.SystemReducedMotion = _previousReducedMotion;
        }

        [UnityTest]
        public IEnumerator NativeSubmit_VisitsFourPages_AndClosesGeneratedModal()
        {
            Submit("vl-nav-pricing");
            yield return WaitForIdle();
            AssertCurrentPage("pricing");
            Assert.That(Required<Button>("vl-nav-pricing").ClassListContains("vl-nav-active"), Is.True);

            Submit("vl-footer-about-pricing");
            yield return WaitForIdle();
            AssertCurrentPage("about");
            Submit("vl-footer-contact-about");
            yield return WaitForIdle();
            AssertCurrentPage("contact");
            Submit("vl-brand-home");
            yield return WaitForIdle();
            AssertCurrentPage("home");

            Submit("vl-home-start");
            yield return WaitForIdle();
            CollectionAssert.Contains(_screen.Router.Modals, "get-started");
            Assert.That(Required<VisualElement>("vl-get-started-modal").resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
            Submit("vl-start-close");
            yield return WaitForIdle();
            Assert.That(_screen.Router.Modals, Is.Empty);
            Assert.That(Required<VisualElement>("vl-get-started-modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator NativeSubmit_ThemeBindsOnce_AfterInitializeAndComponentEnable()
        {
            _controller.Initialize();
            _controller.Initialize();
            yield return WaitForReady();
            yield return WaitForIdle();
            int before = _controller.InteractionCount;
            Submit("vl-theme-toggle");
            Assert.That(_controller.InteractionCount, Is.EqualTo(before + 1), "Repeated Initialize must leave one typed callback.");
            Assert.That(_controller.DarkTheme, Is.True);
            Assert.That(Required<VisualElement>("vl-velora-root").ClassListContains("vl-dark"), Is.True);
            Submit("vl-theme-toggle");
            Assert.That(_controller.DarkTheme, Is.False);

            _controller.enabled = false;
            before = _controller.InteractionCount;
            Submit("vl-theme-toggle");
            Assert.That(_controller.InteractionCount, Is.EqualTo(before), "Disabled adapter must release its callbacks.");
            _controller.enabled = true;
            yield return WaitForReady();
            yield return WaitForIdle();
            before = _controller.InteractionCount;
            Submit("vl-theme-toggle");
            Assert.That(_controller.InteractionCount, Is.EqualTo(before + 1));
            Assert.That(_controller.DarkTheme, Is.True);
            Assert.That(Required<VisualElement>("vl-velora-root").ClassListContains("vl-dark"), Is.True);
        }

        [UnityTest]
        public IEnumerator NativeSubmit_SearchFilters_AndReturnsFromPricingToDashboard()
        {
            Submit("vl-nav-pricing");
            yield return WaitForIdle();
            Submit("vl-search-open");
            yield return WaitForIdle();
            CollectionAssert.Contains(_screen.Router.Modals, "search");
            Required<TextField>("vl-search-input").value = "chart";
            yield return null;
            Assert.That(Required<Button>("vl-search-chart").ClassListContains("vl-hidden"), Is.False);
            Assert.That(Required<Button>("vl-search-aurora").ClassListContains("vl-hidden"), Is.True);
            Assert.That(Required<Button>("vl-search-text").ClassListContains("vl-hidden"), Is.True);
            Submit("vl-search-chart");
            yield return WaitForIdle();
            yield return new WaitForSecondsRealtime(0.2f);
            AssertCurrentPage("home");
            Assert.That(_screen.Router.Modals, Is.Empty);
            Assert.That(Required<ScrollView>("vl-page-home").scrollOffset.y, Is.GreaterThan(0f), "Result must scroll to the actual dashboard.");
        }

        [UnityTest]
        public IEnumerator NativeSubmit_ContactValidates_ShowsSuccess_AndReopens()
        {
            _controller.Navigate("contact");
            yield return WaitForIdle();
            Submit("vl-contact-submit");
            Assert.That(_controller.Status, Is.EqualTo("Contact validation failed"));
            Assert.That(Required<VisualElement>("vl-contact-fields").ClassListContains("vl-hidden"), Is.False);
            Required<TextField>("vl-contact-name").value = "Ada Lovelace";
            Required<TextField>("vl-contact-email").value = "ada@example.com";
            Required<TextField>("vl-contact-message").value = "This is a locally validated QA message.";
            Submit("vl-contact-submit");
            Assert.That(Required<VisualElement>("vl-contact-fields").ClassListContains("vl-hidden"), Is.True);
            Assert.That(Required<VisualElement>("vl-contact-success").ClassListContains("vl-hidden"), Is.False);
            Assert.That(_controller.Status, Does.Contain("no data transmitted"));
            Submit("vl-contact-again");
            Assert.That(Required<VisualElement>("vl-contact-fields").ClassListContains("vl-hidden"), Is.False);
            Assert.That(Required<VisualElement>("vl-contact-success").ClassListContains("vl-hidden"), Is.True);
        }

        [UnityTest]
        public IEnumerator NativeSubmit_ContactAndWaitlist_RejectMultipleAtSigns()
        {
            _controller.Navigate("contact");
            yield return WaitForIdle();
            Required<TextField>("vl-contact-name").value = "Ada Lovelace";
            Required<TextField>("vl-contact-email").value = "ada@@example.com";
            Required<TextField>("vl-contact-message").value = "This message must not bypass invalid email validation.";
            Submit("vl-contact-submit");
            Assert.That(_controller.Status, Is.EqualTo("Contact validation failed"));
            Assert.That(Required<VisualElement>("vl-contact-fields").ClassListContains("vl-hidden"), Is.False);
            Assert.That(Required<VisualElement>("vl-contact-success").ClassListContains("vl-hidden"), Is.True);

            _controller.Navigate("pricing");
            yield return WaitForIdle();
            Required<TextField>("vl-waitlist-email").value = "ada@@example.com";
            Submit("vl-waitlist-submit");
            Assert.That(_controller.Status, Is.EqualTo("Waitlist validation failed"));
            Assert.That(Required<Button>("vl-waitlist-submit").enabledInHierarchy, Is.True);
            Required<TextField>("vl-waitlist-email").value = "ada@example.com";
            Submit("vl-waitlist-submit");
            Assert.That(_controller.Status, Is.EqualTo("Waitlist preview completed locally"));
            Assert.That(Required<Button>("vl-waitlist-submit").enabledInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator Breakpoint640_HidesDesktopNavigation_AndCanSubmitMobileRoute()
        {
            yield return ResizeViewport(640, 844);
            Assert.That(Required<VisualElement>("vl-desktop-nav").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Required<Button>("vl-menu-toggle").resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
            Submit("vl-menu-toggle");
            Assert.That(Required<VisualElement>("vl-mobile-nav").ClassListContains("vl-hidden"), Is.False);
            Submit("vl-mobile-contact");
            yield return WaitForIdle();
            AssertCurrentPage("contact");
            Assert.That(Required<VisualElement>("vl-mobile-nav").ClassListContains("vl-hidden"), Is.True);

            yield return ResizeViewport(1280, 900);
            Assert.That(Required<VisualElement>("vl-desktop-nav").resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
            Assert.That(Required<Button>("vl-menu-toggle").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Breakpoint390_OpensSearchFromMobileMenu_AndClosesIt()
        {
            yield return ResizeViewport(390, 844);
            AssertHittable(Required<Button>("vl-menu-toggle"));
            Submit("vl-menu-toggle");
            yield return null;
            Button mobileSearch = Required<Button>("vl-mobile-search");
            AssertHittable(mobileSearch);
            Submit("vl-mobile-search");
            yield return WaitForIdle();
            CollectionAssert.Contains(_screen.Router.Modals, "search");
            Assert.That(Required<VisualElement>("vl-mobile-nav").ClassListContains("vl-hidden"), Is.True);
            AssertHittable(Required<Button>("vl-search-close"));
            Submit("vl-search-close");
            yield return WaitForIdle();
            Assert.That(_screen.Router.Modals, Is.Empty);
            Assert.That(Required<VisualElement>("vl-search-modal").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Breakpoint1000_MotionControlRemainsVisibleAndOperable()
        {
            yield return ResizeViewport(1000, 844);
            AssertHittable(Required<Button>("vl-motion-toggle"));
            Submit("vl-motion-toggle");
            Assert.That(_controller.ReducedMotion, Is.True);
            Assert.That(_screen.Animator.IsReducedMotion, Is.True);
            Assert.That(_screen.Animator.ActiveCount, Is.Zero);
            Submit("vl-motion-toggle");
            Assert.That(_controller.ReducedMotion, Is.False);
            Assert.That(_screen.Animator.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator NativeKeyframes_AnimateAurora_AndGradientResolvesToTexture()
        {
            VisualElement aurora = _screen.Root.Q<VisualElement>(className: "vl-aurora-one");
            Assert.That(aurora, Is.Not.Null);
            Background gradient = aurora.resolvedStyle.backgroundImage;
            Texture2D texture = gradient.texture != null ? gradient.texture : gradient.sprite != null ? gradient.sprite.texture : null;
            Assert.That(texture, Is.Not.Null, "The converted radial gradient must resolve to its generated native texture.");
            Assert.That(texture.width, Is.GreaterThan(1));
            Assert.That(texture.height, Is.GreaterThan(1));
            Assert.That(_screen.Animator.Spec.Find("aurora-one").IsInfinite, Is.True);
            Assert.That(_screen.Animator.PlayManual("aurora-one", aurora), Is.True);
            float firstX = aurora.style.translate.value.x.value;
            yield return new WaitForSecondsRealtime(0.65f);
            float nextX = aurora.style.translate.value.x.value;
            Assert.That(nextX, Is.GreaterThan(firstX + 0.02f), "Native animation scheduler must actually advance the converted translate channel.");

            Submit("vl-motion-toggle");
            Assert.That(_controller.ReducedMotion, Is.True);
            Assert.That(_screen.Animator.IsReducedMotion, Is.True);
            Assert.That(_screen.Animator.ActiveCount, Is.Zero);
            Assert.That(Required<TextElement>("vl-hero-word").text, Is.EqualTo("alive."));
            Submit("vl-motion-toggle");
            Assert.That(_controller.ReducedMotion, Is.False);
            Assert.That(_screen.Animator.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Localization_ResolvesAllAuthoredTextKeys()
        {
            List<string> unresolved = new List<string>();
            _screen.Root.Query<TextElement>().ForEach(element =>
            {
                if (UitkLocalization.LooksLikeKey(element.text) && element.text.StartsWith("velora.", StringComparison.Ordinal))
                    unresolved.Add(element.name + ": " + element.text);
            });
            Assert.That(unresolved, Is.Empty, string.Join("\n", unresolved));
            Assert.That(Required<Button>("vl-nav-pricing").text, Is.EqualTo("Pricing"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PublicReloadRoot_RebindsThemeAndHeaderNavigationToReplacementTree()
        {
            VisualElement oldRoot = _screen.Root;
            VisualElement replacement = _tree.CloneTree();
            replacement.style.position = Position.Absolute;
            replacement.style.left = 0;
            replacement.style.top = 0;
            replacement.style.right = 0;
            replacement.style.bottom = 0;
            oldRoot.Clear();
            oldRoot.Add(replacement);
            _screen.ReloadRoot(replacement);
            yield return WaitForReady();
            yield return WaitForIdle();
            Assert.That(_screen.Root, Is.SameAs(replacement));
            Submit("vl-theme-toggle");
            Assert.That(_controller.DarkTheme, Is.True);
            Assert.That(Required<VisualElement>("vl-velora-root").ClassListContains("vl-dark"), Is.True,
                "After the public screen reload, the adapter must apply theme to the live app, not the discarded app.");
            Submit("vl-nav-pricing");
            yield return WaitForIdle();
            AssertCurrentPage("pricing");
        }

        [UnityTest]
        public IEnumerator ScreenComponentEnable_RebindsHeaderNavigationToCurrentRouter()
        {
            _screen.enabled = false;
            yield return null;
            _screen.enabled = true;
            yield return WaitForReady();
            yield return WaitForIdle();
            Submit("vl-nav-pricing");
            yield return WaitForIdle();
            AssertCurrentPage("pricing");
        }

        [UnityTest]
        public IEnumerator DesktopHeroCta_IsPaintedReadableAndPointerOperable()
        {
            yield return AssertHeroCtaAtViewport(1280, 900);
        }

        [UnityTest]
        public IEnumerator MobileHeroCta_IsPaintedReadableAndPointerOperable()
        {
            yield return AssertHeroCtaAtViewport(390, 844);
        }

        [UnityTest]
        public IEnumerator MobilePricing_CardsFitTheirNaturalContentWithoutOverlap()
        {
            yield return ResizeViewport(390, 844);
            _controller.Navigate("pricing");
            yield return WaitForIdle();
            yield return new WaitForSecondsRealtime(1.05f);
            VisualElement free = Required<TextElement>("vl-free-name").parent;
            VisualElement pro = Required<TextElement>("vl-pro-name").parent;
            Assert.That(free.ClassListContains("vl-plan"), Is.True);
            Assert.That(pro.ClassListContains("vl-plan"), Is.True);
            AssertPlanNaturalHeight(free);
            AssertPlanNaturalHeight(pro);
            AssertPlanContentOrder(free, new[]
            {
                "vl-free-name", "vl-free-description", "vl-free-price", "vl-free-duration",
                "vl-free-feature-0", "vl-free-feature-1", "vl-free-feature-2", "vl-free-feature-3",
                "vl-free-feature-4", "vl-free-feature-5", "vl-free-feature-6", "vl-free-browse"
            });
            AssertPlanContentOrder(pro, new[]
            {
                "vl-pro-name", "vl-pro-description", "vl-pro-price", "vl-pro-duration",
                "vl-pro-feature-0", "vl-pro-feature-1", "vl-pro-feature-2", "vl-pro-feature-3",
                "vl-pro-feature-4", "vl-pro-feature-5", "vl-pro-feature-6",
                "vl-waitlist-email", "vl-waitlist-submit", "vl-waitlist-feedback"
            });
            Assert.That(free.worldBound.yMax, Is.LessThanOrEqualTo(pro.worldBound.yMin + 1.5f),
                "Free and Pro must occupy separate vertical space on mobile.");
            Assert.That(pro.worldBound.yMax, Is.LessThanOrEqualTo(Required<TextElement>("vl-pricing-compare-title").worldBound.yMin + 1.5f),
                "Compare plans must follow the full Pro card instead of overlapping its overflowing content.");
        }

        [UnityTest]
        public IEnumerator StyledNativeScroller_WheelAndThumbDragReachFooterAtDesktopAndMobile()
        {
            int[] widths = { 1280, 390 };
            int[] heights = { 900, 844 };
            for (int view = 0; view < widths.Length; view++)
            {
                yield return ResizeViewport(widths[view], heights[view]);
                _controller.Navigate("home");
                yield return WaitForIdle();
                yield return new WaitForSecondsRealtime(0.2f);
                ScrollView scroll = Required<ScrollView>("vl-page-home");
                Scroller scroller = scroll.verticalScroller;
                float range = Mathf.Abs(scroller.highValue - scroller.lowValue);
                Assert.That(range, Is.GreaterThan(100f), "The real home page must overflow before testing its native scroller.");
                Assert.That(scroller.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(0f).Within(1f));

                SendWheelAtViewport(scroll, 2f);
                yield return null;
                yield return null;
                Assert.That(scroll.scrollOffset.y, Is.GreaterThan(5f),
                    "A wheel event at the real picked content must move the ScrollView after native chrome styling.");
                for (int step = 0; step < 40 && scroll.scrollOffset.y < Mathf.Abs(scroller.highValue - scroller.lowValue) - 2f; step++)
                {
                    SendWheelAtViewport(scroll, 12f);
                    yield return null;
                }
                yield return null;
                range = Mathf.Abs(scroller.highValue - scroller.lowValue);
                Assert.That(scroll.scrollOffset.y, Is.GreaterThanOrEqualTo(range - 2f),
                    "Repeated wheel input must reach the bottom of the authored home content.");
                AssertHittable(Required<Button>("vl-footer-contact-home"));
                TestContext.Progress.WriteLine("Native wheel " + widths[view] + "x" + heights[view]
                    + ": offset=" + scroll.scrollOffset.y + ", max=" + range);

                // Reset via the public app action, then prove a separate native pointer path.
                _controller.Navigate("home");
                yield return WaitForIdle();
                Assert.That(scroll.scrollOffset.y, Is.EqualTo(0f).Within(1f));
                Slider slider = scroller.slider;
                VisualElement container = slider.Q<VisualElement>(className: BaseSlider<float>.dragContainerUssClassName);
                VisualElement thumb = slider.Q<VisualElement>(className: BaseSlider<float>.draggerUssClassName);
                Assert.That(container, Is.Not.Null, "The styled native slider must retain its drag container.");
                Assert.That(thumb, Is.Not.Null, "The styled native slider must retain its thumb.");
                Assert.That(container.enabledInHierarchy, Is.True);
                Assert.That(thumb.worldBound.width, Is.GreaterThan(0f));
                Assert.That(thumb.worldBound.height, Is.GreaterThan(0f));
                Assert.That(container.worldBound.height, Is.GreaterThan(thumb.worldBound.height + 20f));
                Vector2 start = thumb.worldBound.center;
                Vector2 end = new Vector2(start.x, container.worldBound.yMax - thumb.worldBound.height * 0.5f);
                VisualElement picked = thumb.panel.Pick(start);
                Assert.That(ReferenceEquals(picked, thumb) || ReferenceEquals(picked, container)
                    || (picked != null && container.Contains(picked)), Is.True,
                    "The visible styled thumb must still be picked at its centre.");
                yield return PointerDrag(picked, container, start, end);
                yield return null;
                yield return null;
                range = Mathf.Abs(scroller.highValue - scroller.lowValue);
                Assert.That(scroll.scrollOffset.y, Is.GreaterThanOrEqualTo(range * 0.98f),
                    "Pointer down/move/up on the actual native thumb must move close to the bottom.");
                AssertHittable(Required<Button>("vl-footer-contact-home"));
                TestContext.Progress.WriteLine("Native thumb drag " + widths[view] + "x" + heights[view]
                    + ": offset=" + scroll.scrollOffset.y + ", max=" + range);
            }
        }

        private IEnumerator AssertHeroCtaAtViewport(int width, int height)
        {
            yield return ResizeViewport(width, height);
            _controller.Navigate("home");
            yield return WaitForIdle();
            // Let the authored reveal finish; do not force opacity or inject a replacement style.
            yield return new WaitForSecondsRealtime(1.1f);
            Button button = Required<Button>("vl-home-start");
            AssertHittable(button);
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(button.text, Does.Contain("Get started"), "The authored localized CTA text must remain present.");
            Assert.That(button.resolvedStyle.fontSize, Is.GreaterThanOrEqualTo(12f));
            float inheritedOpacity = 1f;
            for (VisualElement ancestor = button; ancestor != null; ancestor = ancestor.parent)
                inheritedOpacity *= ancestor.resolvedStyle.opacity;
            Assert.That(inheritedOpacity, Is.GreaterThan(0.95f), "A hit-testable CTA can still be visually transparent.");
            Color background = button.resolvedStyle.backgroundColor;
            Color foreground = button.resolvedStyle.color;
            Assert.That(background.a, Is.GreaterThan(0.9f), "The main CTA must paint an opaque background.");
            Assert.That(foreground.a, Is.GreaterThan(0.9f), "The CTA label must not be transparent.");
            Assert.That(ContrastRatio(background, foreground), Is.GreaterThanOrEqualTo(4.5f),
                "The main CTA label must remain readable against its resolved background.");
            _screen.Root.MarkDirtyRepaint();
            yield return null;
            yield return null;
            yield return null;
            AssertCtaPaintedPixels(button, background, foreground);
            yield return PointerClick(button);
            yield return WaitForIdle();
            CollectionAssert.Contains(_screen.Router.Modals, "get-started");
        }

        private void AssertCtaPaintedPixels(Button button, Color background, Color foreground)
        {
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = _target;
                image.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
                image.Apply(false, false);
                Rect bounds = button.worldBound;
                Rect viewport = button.panel.visualTree.worldBound;
                float pixelsPerUnitX = _target.width / viewport.width;
                float pixelsPerUnitY = _target.height / viewport.height;
                int left = Mathf.CeilToInt((bounds.xMin + 12f - viewport.xMin) * pixelsPerUnitX);
                int right = Mathf.FloorToInt((bounds.xMax - 12f - viewport.xMin) * pixelsPerUnitX);
                int top = Mathf.CeilToInt((bounds.yMin + 7f - viewport.yMin) * pixelsPerUnitY);
                int bottom = Mathf.FloorToInt((bounds.yMax - 7f - viewport.yMin) * pixelsPerUnitY);
                Assert.That(right, Is.GreaterThan(left));
                Assert.That(bottom, Is.GreaterThan(top));
                Color32[] pixels = image.GetPixels32();
                int total = 0;
                int paintedBackground = 0;
                int paintedForeground = 0;
                int linearBackground = 0;
                int gammaBackground = 0;
                int mirroredBackground = 0;
                Vector2 textSize = button.MeasureTextSize(button.text, 0f, VisualElement.MeasureMode.Undefined,
                    0f, VisualElement.MeasureMode.Undefined);
                float textTop = (bounds.center.y - textSize.y * 0.5f - 2f - viewport.yMin) * pixelsPerUnitY;
                float textBottom = (bounds.center.y + textSize.y * 0.5f + 2f - viewport.yMin) * pixelsPerUnitY;
                for (int panelY = top; panelY < bottom; panelY++)
                {
                    int renderY = _target.height - 1 - panelY;
                    for (int x = left; x < right; x++)
                    {
                        Color pixel = pixels[renderY * _target.width + x];
                        total++;
                        if (ColorDistance(pixel, background) < 0.22f) paintedBackground++;
                        if (ColorDistance(pixel, background.linear) < 0.22f) linearBackground++;
                        if (ColorDistance(pixel, background.gamma) < 0.22f) gammaBackground++;
                        Color mirrored = pixels[panelY * _target.width + x];
                        if (ColorDistance(mirrored, background) < 0.22f) mirroredBackground++;
                        if (panelY >= textTop && panelY <= textBottom && ColorDistance(pixel, foreground) < 0.25f
                            && ContrastRatio(pixel, background) >= 3f)
                            paintedForeground++;
                    }
                }
                int sampleX = Mathf.Clamp(Mathf.RoundToInt((bounds.center.x - viewport.xMin) * pixelsPerUnitX), 0, _target.width - 1);
                int sampleY = Mathf.Clamp(Mathf.RoundToInt((bounds.yMax - 12f - viewport.yMin) * pixelsPerUnitY), 0, _target.height - 1);
                Color sampleFlipped = pixels[(_target.height - 1 - sampleY) * _target.width + sampleX];
                Color sampleUnflipped = pixels[sampleY * _target.width + sampleX];
                int opaqueSamples = 0;
                int variedSamples = 0;
                int sampleCount = 0;
                int stride = Math.Max(1, pixels.Length / 256);
                Color first = pixels[0];
                for (int i = 0; i < pixels.Length; i += stride)
                {
                    Color sample = pixels[i];
                    sampleCount++;
                    if (sample.a > 0.9f) opaqueSamples++;
                    if (ColorDistance(sample, first) > 0.04f) variedSamples++;
                }
                string diagnostic = "CTA readback: Unity=" + Application.unityVersion
                    + ", colorSpace=" + QualitySettings.activeColorSpace + ", graphics=" + SystemInfo.graphicsDeviceType
                    + ", target=" + _target.graphicsFormat + ", sRGB=" + _target.sRGB
                    + ", readback=" + image.graphicsFormat + ", size=" + _target.width + "x" + _target.height
                    + ", bounds=" + bounds + ", panel=" + viewport + ", crop=" + left + "," + top + ".." + right + "," + bottom
                    + ", bg=#" + ColorUtility.ToHtmlStringRGBA(background)
                    + ", bgLinear=#" + ColorUtility.ToHtmlStringRGBA(background.linear)
                    + ", bgGamma=#" + ColorUtility.ToHtmlStringRGBA(background.gamma)
                    + ", sampleXY=" + sampleX + "," + sampleY
                    + ", sampleFlipped=#" + ColorUtility.ToHtmlStringRGBA(sampleFlipped)
                    + ", sampleUnflipped=#" + ColorUtility.ToHtmlStringRGBA(sampleUnflipped)
                    + ", matches raw/linear/gamma/mirrored=" + paintedBackground + "/" + linearBackground + "/" + gammaBackground + "/" + mirroredBackground
                    + ", glyph=" + paintedForeground + ", frameOpaque=" + opaqueSamples + "/" + sampleCount
                    + ", frameVaried=" + variedSamples + "/" + sampleCount;
                TestContext.Out.WriteLine(diagnostic);
                Assert.That(paintedBackground, Is.GreaterThan(total * 0.45f),
                    "Resolved USS alone is insufficient: the rendered inner CTA must contain its visible background. "
                    + paintedBackground + "/" + total + " matching pixels.\n" + diagnostic);
                Assert.That(paintedForeground, Is.GreaterThan(30),
                    "The rendered CTA must contain visible label glyph pixels, not only a colored empty box.");
                TestContext.Progress.WriteLine("CTA " + _target.width + "x" + _target.height
                    + ": contrast=" + ContrastRatio(background, foreground)
                    + ", background=" + paintedBackground + "/" + total + ", glyph=" + paintedForeground);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(image);
            }
        }

        private static IEnumerator PointerClick(Button button)
        {
            Vector2 centre = button.worldBound.center;
            VisualElement picked = button.panel.Pick(centre);
            Assert.That(ReferenceEquals(picked, button) || (picked != null && button.Contains(picked)), Is.True);
            Event down = new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = centre };
            try
            {
                using (PointerDownEvent pointerDown = PointerDownEvent.GetPooled(down))
                {
                    pointerDown.target = picked;
                    picked.SendEvent(pointerDown);
                }
                yield return null;
            }
            finally
            {
                Event up = new Event { type = EventType.MouseUp, button = 0, clickCount = 1, mousePosition = centre };
                using (PointerUpEvent pointerUp = PointerUpEvent.GetPooled(up))
                {
                    pointerUp.target = button;
                    button.SendEvent(pointerUp);
                }
            }
        }

        private static void SendWheelAtViewport(ScrollView scroll, float deltaY)
        {
            Vector2 position = scroll.contentViewport.worldBound.center;
            VisualElement picked = scroll.panel.Pick(position);
            Assert.That(picked, Is.Not.Null);
            Assert.That(ReferenceEquals(picked, scroll.contentViewport) || scroll.contentViewport.Contains(picked), Is.True,
                "The wheel must be delivered to real visible content instead of bypassing an overlay.");
            Event wheel = new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = position,
                delta = new Vector2(0f, deltaY)
            };
            using (WheelEvent evt = WheelEvent.GetPooled(wheel))
            {
                evt.target = picked;
                picked.SendEvent(evt);
            }
        }

        private static IEnumerator PointerDrag(VisualElement picked, VisualElement captureTarget, Vector2 start, Vector2 end)
        {
            Vector2 last = start;
            Event down = new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = start };
            try
            {
                using (PointerDownEvent pointerDown = PointerDownEvent.GetPooled(down))
                {
                    pointerDown.target = picked;
                    picked.SendEvent(pointerDown);
                }
                yield return null;
                for (int step = 1; step <= 8; step++)
                {
                    Vector2 position = Vector2.Lerp(start, end, step / 8f);
                    Event move = new Event
                    {
                        type = EventType.MouseDrag,
                        button = 0,
                        mousePosition = position,
                        delta = position - last
                    };
                    using (PointerMoveEvent pointerMove = PointerMoveEvent.GetPooled(move))
                    {
                        pointerMove.target = captureTarget;
                        captureTarget.SendEvent(pointerMove);
                    }
                    last = position;
                    yield return null;
                }
            }
            finally
            {
                Event up = new Event { type = EventType.MouseUp, button = 0, clickCount = 1, mousePosition = last };
                using (PointerUpEvent pointerUp = PointerUpEvent.GetPooled(up))
                {
                    pointerUp.target = captureTarget;
                    captureTarget.SendEvent(pointerUp);
                }
            }
        }

        private static void AssertPlanNaturalHeight(VisualElement card)
        {
            IResolvedStyle style = card.resolvedStyle;
            float required = style.paddingTop + style.paddingBottom + style.borderTopWidth + style.borderBottomWidth;
            for (int i = 0; i < card.childCount; i++)
            {
                VisualElement child = card[i];
                if (child.resolvedStyle.display == DisplayStyle.None) continue;
                required += child.layout.height + child.resolvedStyle.marginTop + child.resolvedStyle.marginBottom;
            }
            Assert.That(card.layout.height, Is.GreaterThanOrEqualTo(required - 1.5f),
                "Mobile plan must grow to its real content height: actual=" + card.layout.height + ", content=" + required);
        }

        private void AssertPlanContentOrder(VisualElement card, string[] names)
        {
            Rect bounds = card.worldBound;
            float previousBottom = bounds.yMin;
            for (int i = 0; i < names.Length; i++)
            {
                VisualElement item = Required<VisualElement>(names[i]);
                Assert.That(card.Contains(item), Is.True, names[i]);
                Rect itemBounds = item.worldBound;
                Assert.That(itemBounds.width, Is.GreaterThan(0f), names[i]);
                Assert.That(itemBounds.height, Is.GreaterThan(0f), names[i]);
                Assert.That(itemBounds.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1.5f), names[i]);
                Assert.That(itemBounds.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1.5f), names[i]);
                Assert.That(itemBounds.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1.5f), names[i]);
                Assert.That(itemBounds.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1.5f),
                    names[i] + " overflows its card: child bottom=" + itemBounds.yMax + ", card bottom=" + bounds.yMax);
                Assert.That(itemBounds.yMin, Is.GreaterThanOrEqualTo(previousBottom - 1.5f), names[i] + " overlaps the preceding content.");
                previousBottom = itemBounds.yMax;
            }
        }

        private static float ColorDistance(Color first, Color second)
        {
            return Mathf.Max(Mathf.Abs(first.r - second.r), Mathf.Abs(first.g - second.g), Mathf.Abs(first.b - second.b));
        }

        private static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            return (Mathf.Max(firstLuminance, secondLuminance) + 0.05f) / (Mathf.Min(firstLuminance, secondLuminance) + 0.05f);
        }

        private static float RelativeLuminance(Color color)
        {
            return 0.2126f * LinearChannel(color.r) + 0.7152f * LinearChannel(color.g) + 0.0722f * LinearChannel(color.b);
        }

        private static float LinearChannel(float channel)
        {
            return channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static ThemeStyleSheet FindTheme()
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("RuntimeTheme t:ThemeStyleSheet");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Replace('\\', '/').Contains("/HtmlToUitkShowcase/"))
                    return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
            }
#endif
            return null;
        }

        private T Required<T>(string name) where T : VisualElement
        {
            T element = _screen.Root.Q<T>(name);
            Assert.That(element, Is.Not.Null, name);
            return element;
        }

        private void Submit(string name)
        {
            Button button = Required<Button>(name);
            Assert.That(button.enabledInHierarchy, Is.True, name);
            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        private static void AssertHittable(Button button)
        {
            Assert.That(button.panel, Is.Not.Null, button.name);
            Assert.That(button.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None), button.name);
            Assert.That(button.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible), button.name);
            Rect bounds = button.worldBound;
            Rect viewport = button.panel.visualTree.worldBound;
            Assert.That(bounds.width, Is.GreaterThan(0f), button.name);
            Assert.That(bounds.height, Is.GreaterThan(0f), button.name);
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 0.5f), button.name);
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(viewport.xMax + 0.5f), button.name);
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.5f), button.name);
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.5f), button.name);
            VisualElement picked = button.panel.Pick(bounds.center);
            Assert.That(ReferenceEquals(picked, button) || (picked != null && button.Contains(picked)), Is.True,
                button.name + " is occluded at its centre by " + (picked != null ? picked.name : "no element"));
        }

        private IEnumerator ResizeViewport(int width, int height)
        {
            RenderTexture previous = _target;
            _target = new RenderTexture(width, height, 0);
            _target.Create();
            _panel.targetTexture = _target;
            previous.Release();
            UnityEngine.Object.Destroy(previous);
            yield return null;
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((Mathf.Abs(_screen.Root.panel.visualTree.layout.width - width) > 0.5f
                || Mathf.Abs(_screen.Root.panel.visualTree.layout.height - height) > 0.5f)
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_screen.Root.panel.visualTree.layout.width, Is.EqualTo(width).Within(0.5f), "The real panel must resize before testing media rules.");
            Assert.That(_screen.Root.panel.visualTree.layout.height, Is.EqualTo(height).Within(0.5f));
            yield return null;
        }

        private void AssertCurrentPage(string page)
        {
            Assert.That(_screen.Router.Current, Is.EqualTo(page));
            Assert.That(_controller.CurrentPage, Is.EqualTo(page), "Adapter state must follow the live router.");
            string[] pages = { "home", "pricing", "about", "contact" };
            for (int i = 0; i < pages.Length; i++)
            {
                bool hidden = Required<VisualElement>("vl-page-" + pages[i]).resolvedStyle.display == DisplayStyle.None;
                Assert.That(hidden, Is.EqualTo(pages[i] != page), pages[i]);
            }
        }

        private IEnumerator WaitForReady()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((_screen.Root == null || _screen.Root.panel == null || _screen.Router == null)
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_screen.Root, Is.Not.Null);
            Assert.That(_screen.Root.panel, Is.Not.Null);
            Assert.That(_screen.Router, Is.Not.Null, _screen.LastError);
        }

        private IEnumerator WaitForIdle()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (_screen.Router != null && _screen.Router.IsTransitioning && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_screen.Router, Is.Not.Null);
            Assert.That(_screen.Router.IsTransitioning, Is.False, "Transition did not settle within five seconds.");
            yield return null;
        }
    }
}
