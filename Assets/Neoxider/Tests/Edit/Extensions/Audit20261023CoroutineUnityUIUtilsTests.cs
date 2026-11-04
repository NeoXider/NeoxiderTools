using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Extensions;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023CoroutineUnityUIUtilsTests
    {
        // WHY: Cover native OnEnable admission and synchronous public coordinate/list dispatch.
        // These bodies do not drive or assert an input module's Update/rendering behavior.
        private readonly List<GameObject> _objects = new();
        private readonly List<EventSystem> _suspendedSystems = new();
        private EventSystem _previousSystem;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // WHY: OnEnable must register actual native EventSystem/raycaster components.
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
        public IEnumerator PointerOverUI_Position_ForwardsCoordinatesAndReusesItsBuffer()
        {
            Assert.IsFalse(UIUtils.IsPointerOverUI(new Vector2(75f, 55f)), "No EventSystem means no UI under the pointer.");

            Audit20261023CoroutineUnityRaycaster raycaster = CreateRaycaster();
            Vector2 inside = new(75f, 55f);
            Vector2 outside = new(15f, 15f);

            Assert.IsTrue(UIUtils.IsPointerOverUI(inside));
            Assert.AreEqual(inside, raycaster.LastPosition);
            Assert.IsFalse(UIUtils.IsPointerOverUI(outside));
            Assert.AreEqual(outside, raycaster.LastPosition);
            Assert.IsTrue(UIUtils.IsPointerOverUI(inside), "The shared pointer must not keep the previous query's state.");
            yield break;
        }

        [UnityTest]
        public IEnumerator CoordinateQueries_Warm_AddNoAllocationAboveTheBareRaycast()
        {
            Audit20261023CoroutineUnityRaycaster raycaster = CreateRaycaster();
            EventSystem eventSystem = EventSystem.current;
            Vector2 inside = new(75f, 55f);
            List<RaycastResult> results = new();
            List<RaycastResult> bareResults = new();
            PointerEventData pointer = new(eventSystem);
            pointer.position = inside;
            Assert.AreEqual(1, UIUtils.GetUIElementsAtPosition(inside, results));
            Assert.IsTrue(UIUtils.IsPointerOverUI(inside));
            eventSystem.RaycastAll(pointer, bareResults);
            Assert.AreEqual(1, bareResults.Count);

            int bufferedHits = 0;
            int overUiHits = 0;
            Action buffered = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    bufferedHits += UIUtils.GetUIElementsAtPosition(inside, results);
                }
            };
            Action overUi = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    overUiHits += UIUtils.IsPointerOverUI(inside) ? 1 : 0;
                }
            };
            Action bare = () =>
            {
                for (int i = 0; i < 64; i++)
                {
                    eventSystem.RaycastAll(pointer, bareResults);
                }
            };

            long bufferedCalls = GcAlloc.Count(buffered);
            long overUiCalls = GcAlloc.Count(overUi);
            long bareCalls = GcAlloc.Count(bare);

            Assert.AreEqual(64, bufferedHits);
            Assert.AreEqual(64, overUiHits);
            Assert.IsTrue(results.Exists(hit => hit.gameObject == raycaster.Target));
            Assert.That(bufferedCalls, Is.LessThanOrEqualTo(bareCalls),
                "A retained result list plus the shared pointer must add nothing to EventSystem.RaycastAll.");
            Assert.That(overUiCalls, Is.LessThanOrEqualTo(bareCalls),
                "IsPointerOverUI must add nothing to EventSystem.RaycastAll.");
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

        [UnityTest]
        public IEnumerator SharedPointer_UnloadingTheSceneOfItsEventSystem_ReleasesThePointer()
        {
            CreateRaycaster();
            EventSystem eventSystem = EventSystem.current;
            Scene eventSystemScene = SceneManager.CreateScene("UIUtils pointer owner " + Guid.NewGuid().ToString("N"));
            Scene unrelatedScene = SceneManager.CreateScene("UIUtils unrelated " + Guid.NewGuid().ToString("N"));
            SceneManager.MoveGameObjectToScene(eventSystem.gameObject, eventSystemScene);
            Assert.IsTrue(UIUtils.IsPointerOverUI(new Vector2(75f, 55f)));
            Assert.AreSame(eventSystem, SharedPointerOwner(), "The query warms the shared pointer for this EventSystem.");

            AsyncOperation unrelatedUnload = SceneManager.UnloadSceneAsync(unrelatedScene);
            while (!unrelatedUnload.isDone)
            {
                yield return null;
            }

            Assert.IsNotNull(SharedPointer(), "Unloading another scene keeps the live EventSystem's warm pointer.");

            AsyncOperation ownerUnload = SceneManager.UnloadSceneAsync(eventSystemScene);
            while (!ownerUnload.isDone)
            {
                yield return null;
            }

            Assert.IsTrue(eventSystem == null, "Control: the unload destroyed the EventSystem.");
            Assert.IsNull(SharedPointer(), "The pointer must not keep the destroyed EventSystem reachable.");
            Assert.IsNull(SharedPointerOwnerReference());
        }

        private static PointerEventData SharedPointer()
        {
            return (PointerEventData)UIUtilsStaticField("_sharedPointer").GetValue(null);
        }

        private static EventSystem SharedPointerOwner()
        {
            return (EventSystem)UIUtilsStaticField("_sharedPointerOwner").GetValue(null);
        }

        private static object SharedPointerOwnerReference()
        {
            // WHY: the raw field value; a destroyed EventSystem still compares equal to null through Unity's operator.
            return UIUtilsStaticField("_sharedPointerOwner").GetValue(null);
        }

        private static FieldInfo UIUtilsStaticField(string name)
        {
            FieldInfo field = typeof(UIUtils).GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "UIUtils." + name + " was renamed; update this lifetime test.");
            return field;
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
