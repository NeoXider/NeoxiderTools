using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     <see cref="UIInputCompat.IsPointerOverUI" /> and the caller-buffer
    ///     <see cref="UIInputCompat.GetUIElementsUnderCursor(List{RaycastResult})" /> over a real EventSystem,
    ///     GraphicRaycaster and Graphic with a virtual mouse: once warm, each allocates no more than the bare
    ///     <c>EventSystem.RaycastAll</c> with a reused pointer and list, so the wrappers add nothing, while a frozen
    ///     copy of the 11.0.1 body (a new list and a new <c>PointerEventData</c> per call) stays visible to the counter.
    /// </summary>
    /// <remarks>
    ///     WHY: uGUI raycasting may allocate internally (for example while sorting hits); that cost belongs to the
    ///     EventSystem and is measured here as the bare reference instead of being asserted as zero.
    /// </remarks>
    public sealed class Audit20261102NeoToolsInputUIPointerAllocationPlayTests
    {
        private const int Iterations = 512;

        private readonly List<GameObject> _objects = new();
        private readonly List<BorrowedEventSystemState> _borrowedSystems = new();
        private EventSystem _previousEventSystem;

        [SetUp]
        public void SetUp()
        {
            _previousEventSystem = EventSystem.current;
            // WHY: current is backed by UGUI's registered-system list and cannot be set to null; disabling each
            // live current also finds systems hidden from ordinary scene searches.
            while (EventSystem.current != null)
            {
                EventSystem system = EventSystem.current;
                Assert.That(system.isActiveAndEnabled, Is.True, "Cannot safely suspend an inactive registered system.");
                _borrowedSystems.Add(new BorrowedEventSystemState(system));
                system.enabled = false;
                Assert.That(ReferenceEquals(EventSystem.current, system), Is.False,
                    "Native OnDisable must unregister the borrowed EventSystem.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            finally
            {
                _objects.Clear();
                foreach (BorrowedEventSystemState state in _borrowedSystems)
                {
                    if (state.System != null)
                    {
                        state.System.enabled = state.Enabled;
                    }
                }

                if (_previousEventSystem != null && _previousEventSystem.isActiveAndEnabled)
                {
                    EventSystem.current = _previousEventSystem;
                }

                _borrowedSystems.Clear();
                _previousEventSystem = null;
            }
        }

        [UnityTest]
        public IEnumerator WarmIsPointerOverUI_AddsNoAllocationAboveTheEventSystemRaycast()
        {
            using (Audit20261023NeoToolsInteractableObjectInputDriver input = new())
            {
                EventSystem system = CreateObject("Audit UI Pointer EventSystem").AddComponent<EventSystem>();
                GameObject canvasObject = CreateObject("Audit UI Pointer Canvas");
                canvasObject.SetActive(false);
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.targetDisplay = 0;
                canvasObject.AddComponent<GraphicRaycaster>();
                GameObject imageObject = new("Audit UI Pointer Graphic", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image));
                _objects.Add(imageObject);
                imageObject.transform.SetParent(canvasObject.transform, false);
                RectTransform rect = (RectTransform)imageObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                Image image = imageObject.GetComponent<Image>();
                image.raycastTarget = true;
                canvasObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;

                bool hasPointer = MouseInputCompat.TryGetPosition(out Vector3 position);
                string diagnostic = $"Requires the virtual pointer inside the viewport over a rendered graphic: " +
                                    $"available={hasPointer}, position={position}, viewport={Screen.width}x{Screen.height}, " +
                                    $"legacy={InputBackendCompat.UsesLegacyInput}, current={EventSystem.current == system}, " +
                                    $"depth={image.depth}, frame={Time.frameCount}.";
                Assert.That(hasPointer, Is.True, diagnostic + input.Diagnostics);
                Assert.That(InputBackendCompat.UsesLegacyInput, Is.False, diagnostic);
                Assert.That(EventSystem.current, Is.SameAs(system), diagnostic);
                Assert.That(RectTransformUtility.RectangleContainsScreenPoint(rect, position, null), Is.True, diagnostic);
                Assert.That(UIInputCompat.IsPointerOverUI(), Is.True, diagnostic);

                PointerEventData pointer = new(system);
                List<RaycastResult> hits = new();
                List<RaycastResult> bufferHits = new();
                Assert.That(PollWrapper(64), Is.EqualTo(64), diagnostic);
                Assert.That(PollBuffer(bufferHits, 64), Is.EqualTo(64), diagnostic);
                Assert.That(PollBare(system, pointer, hits, 64), Is.EqualTo(64), diagnostic);
                Assert.That(PollPreviousBody(system, 64), Is.EqualTo(64), diagnostic);

                long wrapper = Count(() => PollWrapper(Iterations), "wrapper hit");
                long buffer = Count(() => PollBuffer(bufferHits, Iterations), "caller-buffer hit");
                long bare = Count(() => PollBare(system, pointer, hits, Iterations), "bare hit");
                long previous = Count(() => PollPreviousBody(system, Iterations), "11.0.1 body hit");
                TestContext.WriteLine($"GC.Alloc calls over {Iterations} hit queries: wrapper={wrapper}, " +
                                      $"caller buffer={buffer}, bare RaycastAll={bare}, 11.0.1 body={previous}.");
                Assert.That(wrapper, Is.LessThanOrEqualTo(bare),
                    "UIInputCompat.IsPointerOverUI must not allocate beyond the EventSystem raycast itself.");
                Assert.That(buffer, Is.LessThanOrEqualTo(bare),
                    "GetUIElementsUnderCursor with a retained list must not allocate beyond the raycast itself.");
                Assert.That(previous, Is.GreaterThanOrEqualTo(bare + 2L * Iterations),
                    "Positive control: a list and a PointerEventData per call are visible to the counter.");

                image.raycastTarget = false;
                Assert.That(UIInputCompat.IsPointerOverUI(), Is.False, diagnostic);
                Assert.That(PollMiss(64), Is.EqualTo(64));
                long missWrapper = Count(() => PollMiss(Iterations), "wrapper miss");
                long missBare = Count(() => PollBareMiss(system, pointer, hits, Iterations), "bare miss");
                TestContext.WriteLine($"GC.Alloc calls over {Iterations} miss queries: wrapper={missWrapper}, " +
                                      $"bare RaycastAll={missBare}.");
                Assert.That(missWrapper, Is.LessThanOrEqualTo(missBare),
                    "A miss must not allocate beyond the EventSystem raycast either.");
            }
        }

        /// <summary>
        ///     Counts GC.Alloc calls of one polling loop; the correctness assertion stays outside the window.
        /// </summary>
        private static long Count(Func<int> poll, string label)
        {
            int correct;
            long allocationCount;
            using (GcAllocCounter counter = GcAllocCounter.CreateOrIgnore())
            {
                counter.Begin();
                correct = poll();
                allocationCount = counter.End();
            }

            Assert.That(correct, Is.EqualTo(Iterations), label + " oracle");
            return allocationCount;
        }

        private static int PollWrapper(int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (UIInputCompat.IsPointerOverUI())
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollMiss(int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (!UIInputCompat.IsPointerOverUI())
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollBare(EventSystem system, PointerEventData pointer, List<RaycastResult> hits, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (RaycastBare(system, pointer, hits))
                {
                    correct++;
                }
            }

            return correct;
        }

        private static int PollBareMiss(EventSystem system, PointerEventData pointer, List<RaycastResult> hits,
            int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (!RaycastBare(system, pointer, hits))
                {
                    correct++;
                }
            }

            return correct;
        }

        private static bool RaycastBare(EventSystem system, PointerEventData pointer, List<RaycastResult> hits)
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 position))
            {
                return false;
            }

            pointer.Reset();
            pointer.position = position;
            system.RaycastAll(pointer, hits);
            bool over = hits.Count > 0;
            hits.Clear();
            return over;
        }

        private static int PollBuffer(List<RaycastResult> results, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (UIInputCompat.GetUIElementsUnderCursor(results) > 0 && results.Count > 0)
                {
                    correct++;
                }
            }

            results.Clear();
            return correct;
        }

        private static int PollPreviousBody(EventSystem system, int count)
        {
            int correct = 0;
            for (int i = 0; i < count; i++)
            {
                if (PreviousBodyHits(system))
                {
                    correct++;
                }
            }

            return correct;
        }

        /// <summary>
        ///     Frozen copy of the 11.0.1 call chain behind <c>GetUIElementsUnderCursor().Count &gt; 0</c>.
        /// </summary>
        /// <remarks>
        ///     WHY: the live list-returning API now reuses the shared pointer of UIUtils, so it no longer reproduces
        ///     the old per-call PointerEventData and cannot serve as the positive control.
        /// </remarks>
        private static bool PreviousBodyHits(EventSystem system)
        {
            if (!MouseInputCompat.TryGetPosition(out Vector3 position))
            {
                return false;
            }

            List<RaycastResult> results = new();
            PointerEventData pointer = new(system);
            pointer.position = position;
            system.RaycastAll(pointer, results);
            return results.Count > 0;
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject created = new(objectName);
            _objects.Add(created);
            return created;
        }

        private readonly struct BorrowedEventSystemState
        {
            public readonly EventSystem System;
            public readonly bool Enabled;

            public BorrowedEventSystemState(EventSystem system)
            {
                System = system;
                Enabled = system.enabled;
            }
        }
    }
}
