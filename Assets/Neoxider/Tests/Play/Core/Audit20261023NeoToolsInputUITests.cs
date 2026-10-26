using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoToolsInputUITests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<BorrowedEventSystemState> _borrowedSystems = new();
        private EventSystem _previousEventSystem;

        [SetUp]
        public void SetUp()
        {
            _previousEventSystem = EventSystem.current;
            // current is backed by UGUI's registered-system list. Its setter cannot set null.
            // Disabling each live current also finds systems hidden from ordinary scene searches.
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
                try
                {
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

                    foreach (BorrowedEventSystemState state in _borrowedSystems)
                    {
                        if (state.System != null)
                        {
                            state.System.SetSelectedGameObject(state.Selected != null ? state.Selected : null);
                        }
                    }

                    Assert.That(ReferenceEquals(EventSystem.current, _previousEventSystem), Is.True,
                        "Restore the exact borrowed current identity, including an originally absent system.");
                    foreach (BorrowedEventSystemState state in _borrowedSystems)
                    {
                        Assert.That(state.System != null, Is.True, "A borrowed EventSystem must remain alive.");
                        Assert.That(state.System.enabled, Is.EqualTo(state.Enabled));
                        Assert.That(ReferenceEquals(state.System.currentSelectedGameObject, state.Selected), Is.True);
                    }
                }
                finally
                {
                    _borrowedSystems.Clear();
                    _previousEventSystem = null;
                }
            }
        }

        [Test]
        public void NoEventSystem_ClearsCallerBufferAndReturnsNoHits()
        {
            Assert.That(EventSystem.current, Is.Null, "Borrowed systems must be natively suspended, not destroyed.");
            GameObject stale = CreateObject("Stale UI hit");
            List<RaycastResult> results = new() { new RaycastResult { gameObject = stale } };
            Assert.That(UIUtils.GetUIElementsAtPosition(Vector2.zero, results), Is.Zero);
            Assert.That(results, Is.Empty);
            results.Add(new RaycastResult { gameObject = stale });

            int count = UIInputCompat.GetUIElementsUnderCursor(results);

            Assert.That(count, Is.Zero);
            Assert.That(results, Is.Empty);
            Assert.That(UIInputCompat.IsPointerOverUI(), Is.False);
            Assert.That(UIInputCompat.GetUIElementsUnderCursor(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator CompatibleCursorQuery_HitsRealGraphicAndMatchesCoordinateAPI()
        {
            EventSystem system = CreateObject("Audit UI EventSystem").AddComponent<EventSystem>();
            Assert.That(EventSystem.current, Is.SameAs(system));
            GameObject canvasObject = CreateObject("Audit UI Canvas");
            canvasObject.SetActive(false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = 0;
            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            GameObject imageObject = new("Audit UI Graphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            // A layout flush alone does not prove the graphic has a native render depth.
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return null;

            bool hasPointer = MouseInputCompat.TryGetPosition(out Vector3 position);
            string diagnostic = $"Native UI acceptance requires an in-viewport pointer and rendered graphic: " +
                                $"available={hasPointer}, position={position}, viewport={Screen.width}x{Screen.height}, " +
                                $"focused={Application.isFocused}, legacy={InputBackendCompat.UsesLegacyInput}, " +
                                $"frame={Time.frameCount}, depth={image.depth}, culled={image.canvasRenderer.cull}. " +
                                "Cursor/coordinate parity has not been exercised if a precondition fails.";
            TestContext.WriteLine(diagnostic);
            Assert.That(hasPointer, Is.True, diagnostic);
            Assert.That(Screen.width, Is.GreaterThan(0), diagnostic);
            Assert.That(Screen.height, Is.GreaterThan(0), diagnostic);
            Assert.That(position.x >= 0f && position.x <= Screen.width && position.y >= 0f && position.y <= Screen.height,
                Is.True, diagnostic);
            Assert.That(image.canvas, Is.SameAs(canvas));
            Assert.That(image.isActiveAndEnabled, Is.True);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(image.canvasRenderer.cull, Is.False, diagnostic);
            Assert.That(image.depth, Is.GreaterThanOrEqualTo(0), diagnostic);
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint(rect, position, null), Is.True, diagnostic);

            List<RaycastResult> nativeHits = new();
            PointerEventData pointer = new(system) { position = position };
            raycaster.Raycast(pointer, nativeHits);
            Assert.That(nativeHits.Count, Is.EqualTo(1), diagnostic);
            Assert.That(nativeHits[0].gameObject, Is.SameAs(imageObject));
            Assert.That(nativeHits[0].module, Is.SameAs(raycaster));
            Assert.That(nativeHits[0].screenPosition, Is.EqualTo((Vector2)position));

            List<RaycastResult> expected = new();
            UIUtils.GetUIElementsAtPosition(position, expected);
            Assert.That(expected.Exists(result => result.gameObject == imageObject), Is.True, diagnostic);
            List<RaycastResult> actual = new() { default };

            int count = UIInputCompat.GetUIElementsUnderCursor(actual);

            Assert.That(count, Is.EqualTo(expected.Count));
            Assert.That(actual.Exists(result => result.gameObject == imageObject), Is.True);
            Assert.That(UIInputCompat.IsPointerOverUI(), Is.True);
            Assert.That(UIInputCompat.GetUIElementsUnderCursor().Count, Is.EqualTo(count));
        }

        private readonly struct BorrowedEventSystemState
        {
            public readonly EventSystem System;
            public readonly bool Enabled;
            public readonly GameObject Selected;

            public BorrowedEventSystemState(EventSystem system)
            {
                System = system;
                Enabled = system.enabled;
                Selected = system.currentSelectedGameObject;
            }
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject created = new(objectName);
            _objects.Add(created);
            return created;
        }
    }
}
