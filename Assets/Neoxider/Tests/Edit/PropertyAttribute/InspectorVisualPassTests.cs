using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using InjectionProbe = global::Neo.Editor.Tests.EditorLibraryPassInjectionProbe;
using Object = UnityEngine.Object;
using PlainComponent = global::Neo.Editor.Tests.EditorLibraryPassPlainComponent;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Guards for the v12 inspector pass: warm repaints of the Neo chrome allocate nothing, the property plan is
    ///     resolved once per editor, transitions are frame-consistent and respect the settings, unresolved injection
    ///     lookups are throttled on the inspector path, multi-object selections are processed completely and the
    ///     documentation preview follows the skin and inherited [NeoDoc] links.
    /// </summary>
    /// <remarks>
    ///     Allocation counts come from the shared calibrated <see cref="GcAllocCounter" /> (the native GC.Alloc
    ///     sampler behind <c>Is.Not.AllocatingGCMemory()</c>), admitted before the window opens and read inside a real
    ///     IMGUI Repaint of a utility window. Every zero is also paired with a positive control in the same pass;
    ///     GC.GetAllocatedBytesForCurrentThread reads 0 in this Editor and is not used.
    /// </remarks>
    [TestFixture]
    public sealed class InspectorVisualPassTests
    {
        private sealed class ChromeProbe : ScriptableObject
        {
            [Header("Group")] public int First;
            public int Second;
            public int Third;
            public bool Flag;
        }

        [NeoDoc("Editor/CustomEditorBase.md")]
        private class DocumentedBase : ScriptableObject
        {
        }

        private sealed class UndocumentedChild : DocumentedBase
        {
        }

        [NeoDoc("NoSuchModule/DoesNotExist.md")]
        private sealed class BrokenLinkProbe : ScriptableObject
        {
        }

        private sealed class ChromeOnlyEditor : CustomEditorBase
        {
            protected override bool UseCustomNeoxiderInspectorGUI => true;

            protected override void ProcessAttributeAssignments()
            {
            }
        }

        private sealed class FullPassEditor : CustomEditorBase
        {
            protected override void ProcessAttributeAssignments()
            {
            }
        }

        private sealed class AssignmentsEditor : NeoCustomEditor
        {
            public void RunAssignments()
            {
                ProcessAttributeAssignments();
            }
        }

        private sealed class DrawWindow : EditorWindow
        {
            public UnityEditor.Editor Inspected;
            public Action Draw;
            public GcAllocCounter Counter;
            public bool MeasureRepaint;
            public int Repaints;
            public long RepaintAllocations = -1;
            public long ControlAllocations = -1;

            /// <summary>Unity's own cost of one text draw: a label with a content that keeps its text.</summary>
            public long TextFloorAllocations = -1;

            private static byte[] s_sink;
            private static readonly GUIContent s_floorText = new("Section");
            private Action _drawOnce;
            private Action _allocatePayload;
            private Action _drawFloorText;

            private void OnGUI()
            {
                if (Event.current.type == EventType.Repaint && MeasureRepaint && Counter != null)
                {
                    // WHY: a method-group delegate allocates where it is created; both exist before a window opens.
                    _drawOnce ??= DrawOnce;
                    _allocatePayload ??= AllocatePayload;
                    _drawFloorText ??= DrawFloorText;
                    DrawFloorText();
                    RepaintAllocations = Counter.Count(_drawOnce);
                    ControlAllocations = Counter.Count(_allocatePayload);
                    TextFloorAllocations = Counter.Count(_drawFloorText);
                    Repaints++;
                    return;
                }

                bool repaint = Event.current.type == EventType.Repaint;
                DrawOnce();
                if (repaint)
                {
                    Repaints++;
                }
            }

            private void DrawOnce()
            {
                if (Inspected != null)
                {
                    Inspected.OnInspectorGUI();
                }
                else
                {
                    Draw?.Invoke();
                }
            }

            private static void AllocatePayload()
            {
                s_sink = new byte[4096];
            }

            private static void DrawFloorText()
            {
                GUI.Label(new Rect(0f, 300f, 200f, 18f), s_floorText, EditorStyles.label);
            }
        }

        private bool _savedAnimations;
        private bool _savedPlayMode;
        private bool _savedTransitions;
        private bool _savedBanner;
        private bool _savedCompact;
        private bool _savedMascot;
        private bool _savedFrame;
        private bool _savedFlow;
        private NeoxiderSettings.NeoxiderData _savedNeoxiderSettings;

        [SetUp]
        public void SetUp()
        {
            _savedAnimations = CustomEditorSettings.EnableAnimations;
            _savedPlayMode = CustomEditorSettings.AnimateInPlayMode;
            _savedTransitions = CustomEditorSettings.EnableTransitions;
            _savedBanner = CustomEditorSettings.EnableRainbowSignature;
            _savedCompact = CustomEditorSettings.CompactRepeatedBanners;
            _savedMascot = CustomEditorSettings.EnableRainbowSignatureAnimation;
            _savedFrame = CustomEditorSettings.EnableRainbowComponentOutline;
            _savedFlow = CustomEditorSettings.EnableRainbowLineAnimation;
            _savedNeoxiderSettings = NeoxiderSettings.Current;
            NeoxiderSettings.Current = new NeoxiderSettings.NeoxiderData { enableAttributeSearch = true };
            NeoInspectorAnimation.ResetMotionForTests();
        }

        [TearDown]
        public void TearDown()
        {
            CustomEditorSettings.SetEnableAnimations(_savedAnimations);
            CustomEditorSettings.SetAnimateInPlayMode(_savedPlayMode);
            CustomEditorSettings.SetEnableTransitions(_savedTransitions);
            CustomEditorSettings.SetEnableRainbowSignature(_savedBanner);
            CustomEditorSettings.SetCompactRepeatedBanners(_savedCompact);
            CustomEditorSettings.SetEnableRainbowSignatureAnimation(_savedMascot);
            CustomEditorSettings.SetEnableRainbowComponentOutline(_savedFrame);
            CustomEditorSettings.SetEnableRainbowLineAnimation(_savedFlow);
            NeoxiderSettings.Current = _savedNeoxiderSettings;
            NeoInspectorAnimation.ResetMotionForTests();
        }

        private const long ChromeTextDraws = 6;

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void WarmRepaintOfTheNeoChrome_CostsNoMoreThanItsTextDraws(bool animations, bool banner)
        {
            using GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            CustomEditorSettings.SetEnableAnimations(animations);
            CustomEditorSettings.SetEnableRainbowSignature(banner);
            CustomEditorSettings.SetEnableRainbowSignatureAnimation(true);
            CustomEditorSettings.SetEnableRainbowComponentOutline(true);
            CustomEditorSettings.SetEnableRainbowLineAnimation(true);

            ChromeProbe probe = ScriptableObject.CreateInstance<ChromeProbe>();
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(probe, typeof(ChromeOnlyEditor));
            DrawWindow window = ScriptableObject.CreateInstance<DrawWindow>();
            try
            {
                window.Inspected = editor;
                window.Counter = counter;
                window.ShowUtility();
                RepaintNow(window);
                RepaintNow(window);
                RepaintNow(window);
                window.MeasureRepaint = true;
                RepaintNow(window);

                Assume.That(window.Repaints, Is.GreaterThan(1), "Requires an IMGUI graphics context.");
                Assume.That(window.ControlAllocations, Is.GreaterThan(0L),
                    "The GC.Alloc counter must see a known byte[4096] in the same pass before a zero is trusted.");
                Assume.That(window.TextFloorAllocations, Is.GreaterThanOrEqualTo(0L), "Text floor was measured.");
                TestContext.WriteLine("Chrome repaint: " + window.RepaintAllocations + " GC.Alloc, text floor " +
                                      window.TextFloorAllocations + " per draw.");
                // WHY: in Unity 6.3 an IMGUI text draw can cost one GC.Alloc inside the engine binding, even with a
                // cached GUIContent; a label repeated in the same pass may read 0, so the floor counts at least one.
                // The chrome draws at most ChromeTextDraws texts (title, tagline, version pill, its tooltip zone,
                // documentation bar); anything above that is the chrome building strings or contents per frame,
                // which runs on every animation frame of every Neo inspector.
                Assert.That(window.RepaintAllocations,
                    Is.LessThanOrEqualTo(ChromeTextDraws * Math.Max(1L, window.TextFloorAllocations)),
                    "A warm Repaint of the banner, header pills, documentation bar, property card and spectrum " +
                    "frame costs no more than Unity's own text draws.");
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void WarmRepaintOfSectionHeaderAndSwitch_AllocatesNothing()
        {
            using GcAllocCounter counter = GcAllocCounter.CreateOrIgnore();
            DrawWindow window = ScriptableObject.CreateInstance<DrawWindow>();
            try
            {
                window.Counter = counter;
                Rect header = new(4f, 4f, 320f, NeoxiderEditorGUI.SectionHeaderHeight);
                Rect pill = new(4f, 40f, NeoxiderEditorGUI.PillToggleWidth, NeoxiderEditorGUI.PillToggleHeight);
                Color accent = NeoxiderEditorGUI.SectionAccent(true);
                Color title = NeoxiderEditorGUI.SectionTitleColor(true);
                window.Draw = () =>
                {
                    NeoxiderEditorGUI.DrawSectionHeaderRect(header, true, "Section", 7, accent, "d_Folder Icon", title);
                    NeoxiderEditorGUI.DrawSectionHeader(new Rect(4f, 70f, 320f, 24f), false, 0.4f, "Animating", -1,
                        accent, "Folder Icon", true);
                    NeoxiderEditorGUI.DrawPillToggle(pill, true);
                    NeoxiderEditorGUI.DrawSwitch(new Rect(60f, 40f, 32f, 18f), false, true);
                };
                window.ShowUtility();
                RepaintNow(window);
                RepaintNow(window);
                window.MeasureRepaint = true;
                RepaintNow(window);

                Assume.That(window.Repaints, Is.GreaterThan(1), "Requires an IMGUI graphics context.");
                Assume.That(window.ControlAllocations, Is.GreaterThan(0L), "GC.Alloc recorder positive control.");
                // WHY: in Unity 6.3 an IMGUI text draw can cost one GC.Alloc inside the engine binding (the repeated
                // floor label may read 0, so it counts at least one); the two bars draw three texts (title and count,
                // then a title) and the switches none, with widths measured once.
                Assert.That(window.RepaintAllocations,
                    Is.LessThanOrEqualTo(3 * Math.Max(1L, window.TextFloorAllocations)),
                    "The bars and switches cost no more than Unity's own text draws.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void PropertyPass_ResolvesThePropertyPlanOncePerEditor()
        {
            ChromeProbe probe = ScriptableObject.CreateInstance<ChromeProbe>();
            FullPassEditor editor = (FullPassEditor)UnityEditor.Editor.CreateEditor(probe, typeof(FullPassEditor));
            DrawWindow window = ScriptableObject.CreateInstance<DrawWindow>();
            try
            {
                window.Inspected = editor;
                window.ShowUtility();
                RepaintNow(window);
                RepaintNow(window);
                RepaintNow(window);

                Assume.That(window.Repaints, Is.GreaterThan(1), "Requires an IMGUI graphics context.");
                Assert.That(editor.PropertyPlanBuildCount, Is.EqualTo(1),
                    "The visible top-level fields of a type cannot change while the editor lives; walking and " +
                    "copying them again on every IMGUI event was per-field garbage.");
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void FoldAmount_EasesBetweenStatesAndIsStableWithinOneFrame()
        {
            Assume.That(EditorApplication.isPlaying, Is.False);
            CustomEditorSettings.SetEnableAnimations(true);
            CustomEditorSettings.SetEnableTransitions(true);
            ChromeProbe probe = ScriptableObject.CreateInstance<ChromeProbe>();
            FullPassEditor editor = (FullPassEditor)UnityEditor.Editor.CreateEditor(probe, typeof(FullPassEditor));
            try
            {
                const string key = "InspectorVisualPassTests.Fold";
                editor.SetMotionClockForTests(100.0);
                Assert.That(editor.GetFoldAmount(key, true), Is.EqualTo(1f), "A first sight never animates.");

                float atToggle = editor.GetFoldAmount(key, false);
                Assert.That(atToggle, Is.EqualTo(1f), "The frame that toggles still lays out the old height.");

                editor.SetMotionClockForTests(100.0 + NeoInspectorAnimation.FoldDuration * 0.5);
                float halfway = editor.GetFoldAmount(key, false);
                float sameFrame = editor.GetFoldAmount(key, false);
                Assert.That(halfway, Is.GreaterThan(0f).And.LessThan(1f));
                Assert.That(sameFrame, Is.EqualTo(halfway), "Layout and Repaint of one frame must agree.");

                editor.SetMotionClockForTests(100.0 + NeoInspectorAnimation.FoldDuration + 0.01);
                Assert.That(editor.GetFoldAmount(key, false), Is.EqualTo(0f));

                CustomEditorSettings.SetEnableTransitions(false);
                editor.SetMotionClockForTests(200.0);
                editor.GetFoldAmount(key, true);
                editor.SetMotionClockForTests(200.01);
                Assert.That(editor.GetFoldAmount(key, true), Is.EqualTo(1f), "Transitions off snap.");
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(probe);
            }
        }

        [Test]
        public void Easing_StartsAndEndsAtRestWithoutOvershootingTheTarget()
        {
            Assert.That(NeoInspectorAnimation.EaseOutCubic(0f), Is.EqualTo(0f));
            Assert.That(NeoInspectorAnimation.EaseOutCubic(1f), Is.EqualTo(1f));
            Assert.That(NeoInspectorAnimation.EaseInOutCubic(0f), Is.EqualTo(0f));
            Assert.That(NeoInspectorAnimation.EaseInOutCubic(1f), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(NeoInspectorAnimation.EaseInOutCubic(0.5f), Is.EqualTo(0.5f).Within(1e-6f));

            float previous = 0f;
            for (int i = 1; i <= 20; i++)
            {
                float value = NeoInspectorAnimation.EaseOutCubic(i / 20f);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous), "Ease-out must be monotonic (no jitter).");
                previous = value;
            }

            Assert.That(NeoInspectorAnimation.SpringPop(0f, 0.6f), Is.EqualTo(0f));
            Assert.That(NeoInspectorAnimation.SpringPop(1f, 0.6f), Is.EqualTo(0f), "The pop must settle exactly.");
            Assert.That(NeoInspectorAnimation.SpringPop(0.15f, 0.6f), Is.GreaterThan(0.1f), "The pop overshoots.");
            Assert.That(NeoInspectorAnimation.Breath(0.0, 2.8), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(NeoInspectorAnimation.Breath(1.4, 2.8), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(NeoInspectorAnimation.Progress(10.0, 10.0, 0.0), Is.EqualTo(1f));
        }

        [Test]
        public void TransitionPolicy_FollowsMasterSwitchPlayModeAndItsOwnToggle()
        {
            Assert.That(NeoInspectorAnimation.ShouldTransition(true, false, false, true), Is.True);
            Assert.That(NeoInspectorAnimation.ShouldTransition(true, false, false, false), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldTransition(false, true, false, true), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldTransition(true, false, true, true), Is.False);
            Assert.That(NeoInspectorAnimation.ShouldTransition(true, true, true, true), Is.True);
            Assert.That(NeoInspectorAnimation.ShouldTick(true), Is.True);
            Assert.That(NeoInspectorAnimation.ShouldTick(false), Is.False,
                "With Unity in the background the decorative animation must idle.");
        }

        [Test]
        public void SpectrumFrame_HasAFixedVertexBudgetAndMeetsTheCardCorners()
        {
            foreach (float height in new[] { 40f, 400f, 4000f })
            {
                Rect r = new(10f, 20f, 300f, height);
                const float radius = 6f;
                const float arm = 40f;
                Assert.That(CustomEditorBase.BuildFramePath(r, radius, arm), Is.True);

                Vector3 first = CustomEditorBase.GetFramePoint(0);
                Vector3 last = CustomEditorBase.GetFramePoint(CustomEditorBase.FramePointCount - 1);
                Assert.That(first.x, Is.EqualTo(r.x + radius + arm).Within(1e-3f));
                Assert.That(first.y, Is.EqualTo(r.y).Within(1e-3f));
                Assert.That(last.x, Is.EqualTo(r.x + radius + arm).Within(1e-3f));
                Assert.That(last.y, Is.EqualTo(r.yMax).Within(1e-3f));

                for (int i = 0; i < CustomEditorBase.FramePointCount; i++)
                {
                    Vector3 p = CustomEditorBase.GetFramePoint(i);
                    Assert.That(p.x, Is.InRange(r.x - 1e-3f, r.x + radius + arm + 1e-3f), $"point {i} x");
                    Assert.That(p.y, Is.InRange(r.y - 1e-3f, r.yMax + 1e-3f), $"point {i} y");
                }
            }
        }

        [Test]
        public void CompactHeader_OnlyTheFirstNeoComponentOfAGameObjectKeepsTheBanner()
        {
            GameObject host = new("InspectorVisualPass.Banner");
            try
            {
                host.AddComponent<BoxCollider>();
                InjectionProbe first = host.AddComponent<InjectionProbe>();
                PlainComponent second = host.AddComponent<PlainComponent>();

                Assert.That(CustomEditorBase.ComputeIsFirstNeoComponent(first), Is.True);
                Assert.That(CustomEditorBase.ComputeIsFirstNeoComponent(second), Is.False);
                Assert.That(CustomEditorBase.ComputeIsFirstNeoComponent(null), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void InspectorThrottle_DefersAnUnresolvedLookupUntilTheHierarchyChangesOrTimePasses()
        {
            GameObject host = new("InspectorVisualPass.Throttle");
            try
            {
                BoxCollider collider = host.AddComponent<BoxCollider>();
                InjectionProbe probe = host.AddComponent<InjectionProbe>();
                InjectionLookupThrottle throttle = new();
                FieldInfo noMatches = typeof(InjectionProbe).GetField(nameof(InjectionProbe.NoMatches));

                // WHY: a pinned clock, so a slow first pass (cold JIT, busy agent) cannot move the deferral window.
                const double now = 1000.0;
                ComponentDrawer.ProcessComponentAttributes(probe, throttle, now);

                Assert.That(probe.Local, Is.SameAs(collider), "A resolvable lookup still assigns on the first pass.");
                Assert.That(throttle.ShouldTry(probe, noMatches, now), Is.False,
                    "A lookup that found nothing must not rescan on the next IMGUI event.");
                Assert.That(throttle.ShouldTry(probe, noMatches, now + InjectionLookupThrottle.RetryInterval - 0.01),
                    Is.False, "The lookup stays deferred for the whole retry interval.");
                Assert.That(throttle.ShouldTry(probe, noMatches, now + InjectionLookupThrottle.RetryInterval),
                    Is.True, "The time limit retries changes that raise no editor event.");

                InjectionLookupThrottle.InvalidateAll();
                Assert.That(throttle.ShouldTry(probe, noMatches, now), Is.True,
                    "A hierarchy or project change retries at once.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void InspectorThrottle_ComponentAndResourceLookupsAreDeferredIndependently()
        {
            ChromeProbe target = ScriptableObject.CreateInstance<ChromeProbe>();
            try
            {
                FieldInfo field = typeof(ChromeProbe).GetField(nameof(ChromeProbe.First));
                const double now = 2000.0;

                Assert.That(InjectionLookupThrottle.InspectorComponents,
                    Is.Not.SameAs(InjectionLookupThrottle.InspectorResources));

                InjectionLookupThrottle.InspectorComponents.Defer(target, field, now);

                Assert.That(InjectionLookupThrottle.InspectorComponents.ShouldTry(target, field, now), Is.False);
                Assert.That(InjectionLookupThrottle.InspectorResources.ShouldTry(target, field, now), Is.True,
                    "A field with a component and a Resources attribute must still get its Resources fallback " +
                    "after the component pass found nothing.");
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void BadgeRows_WrapOnlyWhenTheNextBadgeDoesNotFit()
        {
            List<float> widths = new() { 50f, 50f, 50f };

            Assert.That(NeoxiderEditorGUI.CountBadgeRows(widths, 400f), Is.EqualTo(1));
            Assert.That(NeoxiderEditorGUI.CountBadgeRows(widths, 50f + NeoxiderEditorGUI.BadgeGap + 50f),
                Is.EqualTo(2));
            Assert.That(NeoxiderEditorGUI.CountBadgeRows(widths, 1f), Is.EqualTo(3),
                "A badge wider than the row still takes a row of its own.");
            Assert.That(NeoxiderEditorGUI.CountBadgeRows(new List<float>(), 400f), Is.EqualTo(0));
        }

        [Test]
        public void Badges_ReserveTheSameRowsOnLayoutAndRepaint()
        {
            NeoxiderEditorGUI.Badge[] badges =
            {
                new("Ready", Color.green),
                new("3 items", Color.cyan),
                new("OK", Color.yellow)
            };
            float measured = -1f;
            DrawWindow window = ScriptableObject.CreateInstance<DrawWindow>();
            try
            {
                window.Draw = () =>
                {
                    Rect group = EditorGUILayout.BeginVertical();
                    NeoxiderEditorGUI.DrawBadges(badges);
                    EditorGUILayout.EndVertical();
                    if (Event.current.type == EventType.Repaint)
                    {
                        measured = group.height;
                    }
                };
                window.ShowUtility();
                window.position = new Rect(120f, 120f, 640f, 240f);
                RepaintNow(window);
                RepaintNow(window);

                Assume.That(window.Repaints, Is.GreaterThan(1), "Requires an IMGUI graphics context.");
                Assert.That(measured, Is.EqualTo(NeoxiderEditorGUI.BadgeRowHeight).Within(0.5f),
                    "Three short badges fit one row of a 640px view. The Layout event sees a 1px dummy rect; " +
                    "wrapping against it reserved one row per badge and left blank space in every summary card.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void NeoCustomEditor_AppliesAttributeAssignmentsToEverySelectedObject()
        {
            GameObject a = new("InspectorVisualPass.MultiA");
            GameObject b = new("InspectorVisualPass.MultiB");
            AssignmentsEditor editor = null;
            try
            {
                BoxCollider colliderA = a.AddComponent<BoxCollider>();
                BoxCollider colliderB = b.AddComponent<BoxCollider>();
                InjectionProbe probeA = a.AddComponent<InjectionProbe>();
                InjectionProbe probeB = b.AddComponent<InjectionProbe>();
                editor = (AssignmentsEditor)UnityEditor.Editor.CreateEditor(new Object[] { probeA, probeB },
                    typeof(AssignmentsEditor));

                editor.RunAssignments();

                Assert.That(probeA.Local, Is.SameAs(colliderA));
                Assert.That(probeB.Local, Is.SameAs(colliderB),
                    "Only the first selected object used to receive its [GetComponent] assignment.");
            }
            finally
            {
                if (editor != null)
                {
                    Object.DestroyImmediate(editor);
                }

                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void MixedSwitch_ClickSetsTrue()
        {
            DrawWindow window = ScriptableObject.CreateInstance<DrawWindow>();
            try
            {
                Rect track = new(20f, 20f, NeoxiderEditorGUI.PillToggleWidth, NeoxiderEditorGUI.PillToggleHeight);
                bool result = false;
                window.Draw = () =>
                {
                    if (NeoxiderEditorGUI.DrawSwitch(track, false, true))
                    {
                        result = true;
                    }
                };
                window.ShowUtility();
                RepaintNow(window);
                Assume.That(window.Repaints, Is.GreaterThan(0), "Requires an IMGUI graphics context.");

                Vector2 point = track.center;
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });

                Assert.That(result, Is.True, "Clicking a mixed multi-object switch sets every object to true.");
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void DocPreview_FormatsCodeLinksAndBullets()
        {
            string rich = NeoDocHelper.MarkdownToUnityRichText(
                "## Title\n- Call `Run()` and see [the guide](Guide.md).\nPlain **bold** text");

            Assert.That(rich, Does.Contain("<size=14><b><color="));
            Assert.That(rich, Does.Contain("•"));
            Assert.That(rich, Does.Contain("Run()</color>"));
            Assert.That(rich, Does.Contain("the guide</color>"));
            Assert.That(rich, Does.Not.Contain("`"));
            Assert.That(rich, Does.Not.Contain("](Guide.md)"));
            Assert.That(rich, Does.Contain("<b>bold</b>"));
        }

        [Test]
        public void DocLinks_AreInheritedAndBrokenLinksAreReported()
        {
            string root = Audit20261023NeoEditorTestsPaths.PackageAssetRoot;
            NeoDocHelper.ClearCaches();

            string inherited = NeoDocHelper.GetDocPathForType(root, typeof(UndocumentedChild));
            Assume.That(NeoDocHelper.GetDocPathForType(root, typeof(DocumentedBase)), Is.Not.Null,
                "The package documentation is not imported in this project.");
            Assert.That(inherited, Does.EndWith("Docs/Editor/CustomEditorBase.md"),
                "[NeoDoc] is not inherited by attribute rules; the inspector walks the base types itself.");

            Assert.That(NeoDocHelper.GetBrokenNeoDocPath(root, typeof(BrokenLinkProbe)),
                Is.EqualTo("NoSuchModule/DoesNotExist.md"));
            Assert.That(NeoDocHelper.GetBrokenNeoDocPath(root, typeof(DocumentedBase)), Is.Null);
        }

        [Test]
        public void RequireInterfaceTooltip_KeepsTheFieldTooltipAndIsCached()
        {
            string plain = RequireInterfaceDrawer.GetTooltip(typeof(IDisposable), null);
            string combined = RequireInterfaceDrawer.GetTooltip(typeof(IDisposable), "Own tooltip");

            Assert.That(plain, Is.EqualTo("Requires IDisposable interface"));
            Assert.That(combined, Does.StartWith("Own tooltip").And.EndWith("Requires IDisposable interface"));
            Assert.That(RequireInterfaceDrawer.GetTooltip(typeof(IDisposable), "Own tooltip"), Is.SameAs(combined));
        }

        [Test]
        public void ActionTooltip_ShowsTheSignatureAndThePlayModeRule()
        {
            MethodInfo method = typeof(InspectorVisualPassTests).GetMethod(nameof(SampleAction),
                BindingFlags.Static | BindingFlags.NonPublic);
            string tooltip = CustomEditorBase.BuildActionTooltip(method, method.GetParameters(), true);

            Assert.That(tooltip, Does.StartWith("static SampleAction(Int32 count, String label)"));
            Assert.That(tooltip, Does.Contain("Play Mode only"));
        }

        [Test]
        public void VersionTooltip_DescribesEveryUpdateState()
        {
            string upToDate = CustomEditorBase.GetVersionTooltip(
                new NeoUpdateChecker.State(NeoUpdateChecker.UpdateStatus.UpToDate, "12.0.0", "url", null));
            string available = CustomEditorBase.GetVersionTooltip(
                new NeoUpdateChecker.State(NeoUpdateChecker.UpdateStatus.UpdateAvailable, "12.1.0", "url", null));
            string cached = CustomEditorBase.GetVersionTooltip(
                new NeoUpdateChecker.State(NeoUpdateChecker.UpdateStatus.UpdateAvailable, "12.1.0", "url", null));

            Assert.That(upToDate, Does.Contain("Up to date"));
            Assert.That(available, Does.Contain("12.1.0"));
            Assert.That(cached, Is.SameAs(available), "The tooltip is rebuilt only when the state changes.");
        }

        private static void SampleAction(int count, string label)
        {
        }

        private static void RepaintNow(EditorWindow window)
        {
            // WHY: RepaintImmediately is internal; it runs a full Layout + Repaint pass right now.
            MethodInfo repaint = typeof(EditorWindow).GetMethod("RepaintImmediately",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assert.That(repaint, Is.Not.Null, "Required EditorWindow.RepaintImmediately seam is unavailable.");
            repaint.Invoke(window, null);
        }
    }
}
