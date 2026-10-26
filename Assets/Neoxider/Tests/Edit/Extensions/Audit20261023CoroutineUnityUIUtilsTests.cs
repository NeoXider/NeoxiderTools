using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023CoroutineUnityUIUtilsTests
    {
        // Cover native OnEnable admission and synchronous public coordinate/list dispatch.
        // These bodies do not drive or assert an input module's Update/rendering behavior.
        private readonly List<GameObject> _objects = new();
        private readonly List<EventSystem> _suspendedSystems = new();
        private EventSystem _previousSystem;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // OnEnable must register actual native EventSystem/raycaster components.
            // This mode instruction stays in the top-level setup enumerator.
            yield return new EnterPlayMode();
            Assert.IsTrue(Application.isPlaying);
            _previousSystem = EventSystem.current;
            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (eventSystem.enabled)
                {
                    _suspendedSystems.Add(eventSystem);
                    eventSystem.enabled = false;
                }
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            foreach (EventSystem eventSystem in _suspendedSystems)
            {
                if (eventSystem != null)
                {
                    eventSystem.enabled = true;
                }
            }

            if (_previousSystem != null)
            {
                EventSystem.current = _previousSystem;
            }

            _objects.Clear();
            _suspendedSystems.Clear();
            _previousSystem = null;
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PositionBuffer_ForwardsCoordinatesAndReplacesPreviousHits()
        {
            Audit20261023CoroutineUnityRaycaster raycaster = CreateRaycaster();
            GameObject stale = CreateObject("Audit stale UI hit");
            List<RaycastResult> results = new() { new RaycastResult { gameObject = stale } };
            Vector2 inside = new(75f, 55f);

            int count = UIUtils.GetUIElementsAtPosition(inside, results);

            Assert.AreEqual(results.Count, count);
            Assert.IsTrue(results.Exists(hit => hit.gameObject == raycaster.Target));
            Assert.IsFalse(results.Exists(hit => hit.gameObject == stale));
            Assert.AreEqual(inside, raycaster.LastPosition);

            Vector2 outside = new(15f, 15f);
            count = UIUtils.GetUIElementsAtPosition(outside, results);

            Assert.AreEqual(results.Count, count);
            Assert.IsFalse(results.Exists(hit => hit.gameObject == raycaster.Target));
            Assert.IsFalse(results.Exists(hit => hit.gameObject == stale));
            Assert.AreEqual(outside, raycaster.LastPosition);
            yield break;
        }

        [UnityTest]
        public IEnumerator PositionQuery_ReturnsIndependentCallerOwnedLists()
        {
            Audit20261023CoroutineUnityRaycaster raycaster = CreateRaycaster();
            List<RaycastResult> first = UIUtils.GetUIElementsAtPosition(new Vector2(75f, 55f));
            List<RaycastResult> second = UIUtils.GetUIElementsAtPosition(new Vector2(76f, 56f));

            Assert.AreNotSame(first, second);
            Assert.IsTrue(first.Exists(hit => hit.gameObject == raycaster.Target));
            Assert.IsTrue(second.Exists(hit => hit.gameObject == raycaster.Target));
            first.Clear();
            Assert.IsTrue(second.Exists(hit => hit.gameObject == raycaster.Target));
            yield break;
        }

        [UnityTest]
        public IEnumerator PositionBuffer_WithoutEventSystem_ClearsStaleResults()
        {
            Assert.IsNull(EventSystem.current);
            GameObject stale = CreateObject("Audit stale result without EventSystem");
            List<RaycastResult> results = new() { new RaycastResult { gameObject = stale } };

            Assert.AreEqual(0, UIUtils.GetUIElementsAtPosition(new Vector2(75f, 55f), results));
            Assert.IsEmpty(results);
            Assert.IsEmpty(UIUtils.GetUIElementsAtPosition(new Vector2(75f, 55f)));
            yield break;
        }

        [UnityTest]
        public IEnumerator PositionBuffer_NullBuffer_ReportsTheInvalidArgument()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => UIUtils.GetUIElementsAtPosition(Vector2.zero, null));
            Assert.AreEqual("results", exception.ParamName);
            yield break;
        }

        private Audit20261023CoroutineUnityRaycaster CreateRaycaster()
        {
            EventSystem eventSystem = CreateObject("Audit coordinate EventSystem").AddComponent<EventSystem>();
            Assert.IsNotNull(eventSystem);
            Assert.IsTrue(eventSystem.isActiveAndEnabled);
            EventSystem.current = eventSystem;
            Assert.AreSame(eventSystem, EventSystem.current, "Native OnEnable must admit the EventSystem.");
            Audit20261023CoroutineUnityRaycaster raycaster = CreateObject("Audit coordinate raycaster")
                .AddComponent<Audit20261023CoroutineUnityRaycaster>();
            Assert.IsNotNull(raycaster, "Unity must attach the relocated runtime raycaster.");
            Assert.AreEqual("Neo.Tests.Fixtures", raycaster.GetType().Assembly.GetName().Name);
            Assert.IsTrue(raycaster.isActiveAndEnabled);
            CollectionAssert.Contains(RaycasterManager.GetRaycasters(), raycaster,
                "Native OnEnable must register the raycaster; the fixture must not register it manually.");
            raycaster.Target = CreateObject("Audit coordinate UI target");
            return raycaster;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            _objects.Add(gameObject);
            return gameObject;
        }
    }

}
