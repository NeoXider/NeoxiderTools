using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     PlayMode integration tests for InteractiveObject.
    ///     These tests run with real Unity physics so that raycasts, obstacles,
    ///     triggers, and distance checks work exactly as they would in a live scene.
    /// </summary>
    [TestFixture]
    public class InteractiveObjectPlayTests
    {
        private Camera _cam;
        private InteractionRayProvider _rayProvider;
        private readonly List<Camera> _suspendedMainCameras = new List<Camera>();
        private GameObject _camObj;
        private EventSystem _eventSystem;
        private EventSystem _previousEventSystem;
        private readonly List<GameObject> _ownedObjects = new List<GameObject>();
        private readonly List<EventSystem> _suspendedEventSystems = new List<EventSystem>();

        /// <summary>
        ///     Creates minimal scene: camera + EventSystem.  
        ///     Physics needs at least one FixedUpdate to register new colliders.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SuspendExistingEventSystems();
            foreach (Camera camera in Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.isActiveAndEnabled && camera.CompareTag("MainCamera"))
                {
                    _suspendedMainCameras.Add(camera);
                    camera.enabled = false;
                }
            }

            // WHY: Camera tagged MainCamera so InteractiveObject.Awake finds it
            _camObj = Track(new GameObject("MainCamera"));
            _cam = _camObj.AddComponent<Camera>();
            _camObj.tag = "MainCamera";
            _camObj.transform.position = new Vector3(0, 0, -10f);
            _camObj.transform.rotation = Quaternion.identity; // WHY: looking along +Z toward the test objects

            _rayProvider = _camObj.AddComponent<InteractionRayProvider>();
            _rayProvider.Mode = InteractionRayProvider.RayMode.Mouse;
            Assert.That(Camera.main, Is.SameAs(_cam), "Pointer context must resolve the fixture's sole main camera.");

            GameObject esObj = Track(new GameObject("EventSystem"));
            _eventSystem = esObj.AddComponent<EventSystem>();
            // Pointer handlers are invoked directly; no input backend module is needed.
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject go in _ownedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _ownedObjects.Clear();
            foreach (Camera camera in _suspendedMainCameras)
            {
                if (camera != null)
                {
                    camera.enabled = true;
                }
            }

            _suspendedMainCameras.Clear();
            foreach (EventSystem eventSystem in _suspendedEventSystems)
            {
                if (eventSystem != null)
                {
                    eventSystem.enabled = true;
                }
            }

            _suspendedEventSystems.Clear();
            if (_previousEventSystem != null && _previousEventSystem.isActiveAndEnabled)
            {
                EventSystem.current = _previousEventSystem;
            }

            yield return null;
        }

        private GameObject Track(GameObject ownedObject)
        {
            _ownedObjects.Add(ownedObject);
            return ownedObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo fi = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}");
            return (T)fi.GetValue(target);
        }

        private static bool InvokeIsInRange(InteractiveObject obj)
        {
            MethodInfo mi = typeof(InteractiveObject).GetMethod("IsInRange",
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                System.Type.EmptyTypes, null);
            Assert.IsNotNull(mi, "Method 'IsInRange()' not found");
            return (bool)mi.Invoke(obj, null);
        }

        private static bool InvokeIsInViewForKeyboardInteraction(InteractiveObject obj)
        {
            MethodInfo mi = typeof(InteractiveObject).GetMethod("IsInViewForKeyboardInteraction",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Method 'IsInViewForKeyboardInteraction()' not found");
            return (bool)mi.Invoke(obj, null);
        }

        private void SuspendExistingEventSystems()
        {
            _previousEventSystem = EventSystem.current;
            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (eventSystem != null && eventSystem.isActiveAndEnabled)
                {
                    _suspendedEventSystems.Add(eventSystem);
                    eventSystem.enabled = false;
                }
            }
        }

        private InteractiveObject CreateInteractive(
            Vector3 position,
            float interactionDistance = 5f,
            bool checkObstacles = true,
            bool includeTriggerInObstacle = false,
            Transform distanceCheckPoint = null)
        {
            GameObject go = Track(new GameObject("Interactive"));
            go.transform.position = position;
            SphereCollider col = go.AddComponent<SphereCollider>();
            col.radius = 0.5f;

            InteractiveObject io = go.AddComponent<InteractiveObject>();
            io.interactable = true;

            // WHY: Initialize events to avoid null refs in callbacks
            io.onInteractDown = new UnityEngine.Events.UnityEvent();
            io.onInteractUp = new UnityEngine.Events.UnityEvent();
            io.onHoverEnter = new UnityEngine.Events.UnityEvent();
            io.onHoverExit = new UnityEngine.Events.UnityEvent();
            io.onHoverChanged = new UnityEngine.Events.UnityEvent<bool>();
            io.onClick = new UnityEngine.Events.UnityEvent();
            io.onDoubleClick = new UnityEngine.Events.UnityEvent();
            io.onRightClick = new UnityEngine.Events.UnityEvent();
            io.onMiddleClick = new UnityEngine.Events.UnityEvent();
            io.onEnterRange = new UnityEngine.Events.UnityEvent();
            io.onExitRange = new UnityEngine.Events.UnityEvent();

            SetPrivateField(io, "interactionDistance", interactionDistance);
            SetPrivateField(io, "checkObstacles", checkObstacles);
            SetPrivateField(io, "includeTriggerCollidersInObstacleCheck", includeTriggerInObstacle);

            if (distanceCheckPoint != null)
            {
                SetPrivateField(io, "distanceCheckPoint", distanceCheckPoint);
            }

            return io;
        }

        private GameObject CreateWall(Vector3 position, Vector3 scale)
        {
            GameObject wall = Track(new GameObject("Wall"));
            wall.transform.position = position;
            wall.transform.localScale = scale;
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.isTrigger = false;
            return wall;
        }

        private GameObject CreateTriggerZone(Vector3 position, Vector3 scale)
        {
            GameObject trigger = Track(new GameObject("TriggerZone"));
            trigger.transform.position = position;
            trigger.transform.localScale = scale;
            BoxCollider box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            return trigger;
        }

        /// <summary>Object within range → IsInRange = true.</summary>
        [UnityTest]
        public IEnumerator InRange_NoObstacles_ReturnsTrue()
        {
            GameObject checkPoint = Track(new GameObject("CheckPoint"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 3f),
                5f, false,
                distanceCheckPoint: checkPoint.transform);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Object at distance 3 should be in range with interactionDistance=5");
        }

        /// <summary>Object outside range → IsInRange = false.</summary>
        [UnityTest]
        public IEnumerator OutOfRange_NoObstacles_ReturnsFalse()
        {
            GameObject checkPoint = Track(new GameObject("CheckPoint"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 8f),
                5f, false,
                distanceCheckPoint: checkPoint.transform);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInRange(io),
                "Object at distance 8 should be OUT of range with interactionDistance=5");
        }

        /// <summary>interactionDistance=0 means unlimited.</summary>
        [UnityTest]
        public IEnumerator UnlimitedDistance_AlwaysInRange()
        {
            GameObject checkPoint = Track(new GameObject("CheckPoint"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 999f),
                0f, false,
                distanceCheckPoint: checkPoint.transform);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Distance=0 should mean unlimited range (always in range)");
        }

        /// <summary>
        ///     Wall between check point and interactive object → blocked.
        /// </summary>
        [UnityTest]
        public IEnumerator Wall_Blocks_Interaction_WhenCheckObstacles()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 4f),
                10f, true,
                distanceCheckPoint: checkPoint.transform);

            CreateWall(new Vector3(0, 0, 2f), new Vector3(5, 5, 0.2f));

            // WHY: Wait for physics to register colliders
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInRange(io),
                "Wall between player and interactive object should BLOCK interaction when checkObstacles=true");
        }

        /// <summary>
        ///     Same setup but checkObstacles=false → wall ignored.
        /// </summary>
        [UnityTest]
        public IEnumerator Wall_DoesNotBlock_WhenCheckObstaclesDisabled()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 4f),
                10f, false,
                distanceCheckPoint: checkPoint.transform);

            CreateWall(new Vector3(0, 0, 2f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Wall should be IGNORED when checkObstacles=false");
        }

        /// <summary>
        ///     Wall BEHIND the interactive object → does NOT block.
        /// </summary>
        [UnityTest]
        public IEnumerator Wall_BehindObject_DoesNotBlock()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 3f),
                10f, true,
                distanceCheckPoint: checkPoint.transform);

            CreateWall(new Vector3(0, 0, 6f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Wall behind the object should NOT block interaction");
        }

        /// <summary>
        ///     Trigger zone between check point and object → obstacle check ignores triggers by default.
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerZone_DoesNotBlock_WhenIncludeTriggersFalse()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 4f),
                10f, true,
                false,
                checkPoint.transform);

            CreateTriggerZone(new Vector3(0, 0, 2f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Trigger collider should NOT block obstacle check when includeTriggerCollidersInObstacleCheck=false");
        }

        /// <summary>
        ///     When includeTriggerCollidersInObstacleCheck=true,
        ///     a trigger acts as an obstacle.
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerZone_Blocks_WhenIncludeTriggersTrue()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 4f),
                10f, true,
                true,
                checkPoint.transform);

            CreateTriggerZone(new Vector3(0, 0, 2f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInRange(io),
                "Trigger collider SHOULD block obstacle check when includeTriggerCollidersInObstacleCheck=true");
        }

        /// <summary>
        ///     Interactive object (BoxCollider) sits inside a large trigger zone.
        ///     The trigger must not block the obstacle ray from reaching the inner collider.
        /// </summary>
        [UnityTest]
        public IEnumerator ObjectInsideTrigger_StillInteractable()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 3f),
                10f, true,
                false,
                checkPoint.transform);

            GameObject triggerWrap = CreateTriggerZone(new Vector3(0, 0, 3f), new Vector3(4, 4, 4f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Object placed inside a trigger zone should STILL be interactable (triggers ignored in obstacle check)");
        }

        /// <summary>
        ///     Interactive object with its own trigger collider: interaction should still work.
        /// </summary>
        [UnityTest]
        public IEnumerator ObjectWithTriggerCollider_StillInteractable()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            GameObject go = Track(new GameObject("InteractiveTrigger"));
            go.transform.position = new Vector3(0, 0, 3f);
            SphereCollider col = go.AddComponent<SphereCollider>();
            col.radius = 0.5f;
            col.isTrigger = true;

            InteractiveObject io = go.AddComponent<InteractiveObject>();
            io.interactable = true;
            io.onInteractDown = new UnityEngine.Events.UnityEvent();
            io.onInteractUp = new UnityEngine.Events.UnityEvent();
            io.onHoverEnter = new UnityEngine.Events.UnityEvent();
            io.onHoverExit = new UnityEngine.Events.UnityEvent();
            io.onHoverChanged = new UnityEngine.Events.UnityEvent<bool>();
            io.onClick = new UnityEngine.Events.UnityEvent();
            io.onDoubleClick = new UnityEngine.Events.UnityEvent();
            io.onRightClick = new UnityEngine.Events.UnityEvent();
            io.onMiddleClick = new UnityEngine.Events.UnityEvent();
            io.onEnterRange = new UnityEngine.Events.UnityEvent();
            io.onExitRange = new UnityEngine.Events.UnityEvent();

            SetPrivateField(io, "interactionDistance", 10f);
            SetPrivateField(io, "checkObstacles", true);
            SetPrivateField(io, "includeTriggerCollidersInObstacleCheck", false);
            SetPrivateField(io, "distanceCheckPoint", checkPoint.transform);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            // WHY: The obstacle raycast ignores triggers, but IsTargetHierarchyCollider
            // should still detect this object's own collider when the ray hits it.
            // IsInRange distance check should pass since 3 < 10.
            Assert.IsTrue(InvokeIsInRange(io),
                "Object with trigger as its own collider should still be reachable");
        }

        [UnityTest]
        public IEnumerator KeyboardView_DoesNotFallbackToDistance_WhenDistancePointMissing()
        {
            InteractiveObject io = CreateInteractive(new Vector3(3f, 0, 3f),
                10f, false);

            SetPrivateField(io, "distanceCheckPoint", null);
            SetPrivateField(io, "viewCheckPoint", null);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInViewForKeyboardInteraction(io),
                "Keyboard interaction must require the look ray to hit the object, not only distance.");
        }

        /// <summary>
        ///     Wall blocks but trigger doesn't in the same scene.
        /// </summary>
        [UnityTest]
        public IEnumerator TriggerBeforeWall_WallStillBlocks()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 6f),
                15f, true,
                false,
                checkPoint.transform);

            CreateTriggerZone(new Vector3(0, 0, 1f), new Vector3(5, 5, 0.2f));
            CreateWall(new Vector3(0, 0, 3f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInRange(io),
                "Wall should STILL block even if a trigger zone is closer to the player");
        }

        /// <summary>Hover enter fires when interactable.</summary>
        [UnityTest]
        public IEnumerator HoverEnter_FiresEvent_WhenInteractable()
        {
            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 2f),
                0f, false);

            bool hovered = false;
            io.onHoverEnter.AddListener(() => hovered = true);

            yield return null; // WHY: Let Awake run

            PointerEventData pointerData = new PointerEventData(_eventSystem);
            io.OnPointerEnter(pointerData);

            Assert.IsTrue(hovered, "onHoverEnter should fire");
            Assert.IsTrue(io.IsHovered, "IsHovered should be true");
        }

        /// <summary>Hover does NOT fire when not interactable.</summary>
        [UnityTest]
        public IEnumerator HoverEnter_DoesNotFire_WhenNotInteractable()
        {
            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 2f),
                0f, false);
            io.interactable = false;

            bool hovered = false;
            io.onHoverEnter.AddListener(() => hovered = true);

            yield return null;

            PointerEventData pointerData = new PointerEventData(_eventSystem);
            io.OnPointerEnter(pointerData);

            Assert.IsFalse(hovered, "onHoverEnter should NOT fire when not interactable");
            Assert.IsFalse(io.IsHovered, "IsHovered should be false");
        }

        /// <summary>Hover exit fires after enter.</summary>
        [UnityTest]
        public IEnumerator HoverExit_FiresEvent()
        {
            InteractiveObject io = CreateInteractive(_cam.transform.position - _cam.transform.forward * 2f,
                0f, false);
            io.UseScreenCenterRay = false;
            int enters = 0;
            int exits = 0;
            bool exited = false;
            io.onHoverEnter.AddListener(() => enters++);
            io.onHoverExit.AddListener(() => { exited = true; exits++; });
            yield return null;

            Assert.That(_rayProvider.Mode, Is.EqualTo(InteractionRayProvider.RayMode.Mouse));
            Assert.That(io.IsHovered, Is.False, "The target behind the camera has no automatic ray hover.");
            PointerEventData pointerData = new PointerEventData(_eventSystem);
            io.OnPointerEnter(pointerData);
            Assert.That(io.IsHovered, Is.True, "Establish an actual enter before testing exit.");
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(exits, Is.Zero);
            io.OnPointerExit(pointerData);
            Assert.IsTrue(exited, "onHoverExit should fire after OnPointerExit in cursor-only mode");
            Assert.IsFalse(io.IsHovered, "IsHovered should be false after exit");
            io.OnPointerExit(pointerData);
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(exits, Is.EqualTo(1), "Repeated exit must not duplicate the transition.");
        }

        /// <summary>onHoverChanged passes correct bool parameter.</summary>
        [UnityTest]
        public IEnumerator HoverChanged_PassesCorrectBool()
        {
            InteractiveObject io = CreateInteractive(_cam.transform.position - _cam.transform.forward * 2f,
                0f, false);
            io.UseScreenCenterRay = false;
            bool? lastHoverState = null;
            List<bool> states = new List<bool>();
            io.onHoverChanged.AddListener(state => { lastHoverState = state; states.Add(state); });
            yield return null;

            Assert.That(io.IsHovered, Is.False);
            Assert.That(states, Is.Empty);
            PointerEventData pointerData = new PointerEventData(_eventSystem);
            io.OnPointerEnter(pointerData);
            Assert.AreEqual(true, lastHoverState, "onHoverChanged should pass true on enter");
            Assert.That(io.IsHovered, Is.True);
            io.OnPointerExit(pointerData);
            Assert.AreEqual(false, lastHoverState, "onHoverChanged should pass false on exit");
            Assert.That(io.IsHovered, Is.False);
            io.OnPointerExit(pointerData);
            Assert.That(states, Is.EqualTo(new[] { true, false }));
        }

        [UnityTest]
        public IEnumerator CenterHover_PointerExitRetainsRayHoverUntilNativeAimLeaves()
        {
            _rayProvider.Mode = InteractionRayProvider.RayMode.Both;
            InteractiveObject io = CreateInteractive(new Vector3(0f, 0f, 2f), 0f, false);
            int enters = 0;
            int exits = 0;
            List<bool> states = new List<bool>();
            io.onHoverEnter.AddListener(() => enters++);
            io.onHoverExit.AddListener(() => exits++);
            io.onHoverChanged.AddListener(state => states.Add(state));
            yield return new WaitForFixedUpdate();
            yield return null;

            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Assert.That(Physics.Raycast(ray, out RaycastHit hit, 100f), Is.True);
            Assert.That(hit.collider.gameObject, Is.SameAs(io.gameObject));
            Assert.That(io.IsHovered, Is.True, "A real center ray must establish hover before pointer exit.");
            Assert.That(enters, Is.EqualTo(1));
            io.OnPointerExit(new PointerEventData(_eventSystem));
            Assert.That(io.IsHovered, Is.True, "A valid center-hover positive survives cursor exit.");
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(exits, Is.Zero);
            Assert.That(states, Is.EqualTo(new[] { true }));

            io.transform.position = _cam.transform.position - _cam.transform.forward * 2f;
            Physics.SyncTransforms();
            InteractionRayProvider.InvalidateQueries();
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (io.IsHovered && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null; // Real native Update, not a private lifecycle invocation.
            }

            Assert.That(io.IsHovered, Is.False);
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(exits, Is.EqualTo(1));
            Assert.That(states, Is.EqualTo(new[] { true, false }));
        }

        [UnityTest]
        public IEnumerator HoverDisabled_CenterHitStillClicksAndMouseGateRemainsEffective()
        {
            _rayProvider.Mode = InteractionRayProvider.RayMode.ScreenCenter;
            InteractiveObject io = CreateInteractive(new Vector3(0f, 0f, 2f), 0f, false);
            io.UseHoverDetection = false;
            io.UseScreenCenterRay = true;
            int clicks = 0;
            int enters = 0;
            int exits = 0;
            io.onClick.AddListener(() => clicks++);
            io.onHoverEnter.AddListener(() => enters++);
            io.onHoverExit.AddListener(() => exits++);
            yield return new WaitForFixedUpdate();
            yield return null;

            Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Assert.That(Physics.Raycast(ray, out RaycastHit hit, 100f), Is.True);
            Assert.That(hit.collider.gameObject, Is.SameAs(io.gameObject));
            Assert.That(io.IsHovered, Is.False);
            PointerEventData pointerData = new PointerEventData(_eventSystem)
                { button = PointerEventData.InputButton.Left };
            io.OnPointerClick(pointerData);
            Assert.That(clicks, Is.EqualTo(1), "Hover detection is not a click prerequisite.");
            io.UseMouseInteraction = false;
            yield return null; // Cross a frame so click de-duplication cannot mask a broken mouse gate.
            io.OnPointerClick(pointerData);
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(enters, Is.Zero);
            Assert.That(exits, Is.Zero);
        }

        /// <summary>
        ///     Moving the check point in/out of range triggers distance events.
        /// </summary>
        [UnityTest]
        public IEnumerator DistanceEvents_FireOnRangeTransition()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = new Vector3(0, 0, 0);

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 3f),
                5f, false,
                distanceCheckPoint: checkPoint.transform);

            bool entered = false;
            bool exited = false;
            io.onEnterRange.AddListener(() => entered = true);
            io.onExitRange.AddListener(() => exited = true);

            // WHY: Wait for Awake + first Update
            yield return null;
            yield return null;

            Assert.IsTrue(entered, "onEnterRange should fire when player starts within range");
            Assert.IsFalse(exited, "onExitRange should NOT fire while still in range");

            checkPoint.transform.position = new Vector3(0, 0, -20f);
            entered = false;
            yield return null;

            Assert.IsTrue(exited, "onExitRange should fire when player leaves range");
        }

        [UnityTest]
        public IEnumerator PublicAPI_InteractionDistance_GetSet()
        {
            InteractiveObject io = CreateInteractive(Vector3.zero, 5f, false);
            yield return null;

            Assert.AreEqual(5f, io.InteractionDistance, 0.01f);

            io.InteractionDistance = 12f;
            Assert.AreEqual(12f, io.InteractionDistance, 0.01f);

            // WHY: Negative values should be clamped to 0
            io.InteractionDistance = -5f;
            Assert.AreEqual(0f, io.InteractionDistance, 0.01f);
        }

        [UnityTest]
        public IEnumerator PublicAPI_DistanceCheckPoint_GetSet()
        {
            InteractiveObject io = CreateInteractive(Vector3.zero, 5f, false);

            GameObject newPoint = Track(new GameObject("NewCheckPoint"));
            newPoint.transform.position = new Vector3(1, 2, 3);

            yield return null;

            io.DistanceCheckPoint = newPoint.transform;
            Assert.AreEqual(newPoint.transform, io.DistanceCheckPoint);
        }

        [UnityTest]
        public IEnumerator PublicAPI_ToggleFlags()
        {
            InteractiveObject io = CreateInteractive(Vector3.zero, 5f, false);
            yield return null;

            io.UseMouseInteraction = false;
            Assert.IsFalse(io.UseMouseInteraction);

            io.UseHoverDetection = false;
            Assert.IsFalse(io.UseHoverDetection);

            io.UseKeyboardInteraction = false;
            Assert.IsFalse(io.UseKeyboardInteraction);

            io.UseMouseInteraction = true;
            Assert.IsTrue(io.UseMouseInteraction);
        }

        /// <summary>
        ///     Wall on a different layer that is NOT in obstacleLayers → does not block.
        /// </summary>
        [UnityTest]
        public IEnumerator Wall_OnDifferentLayer_DoesNotBlock()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 4f),
                10f, true,
                distanceCheckPoint: checkPoint.transform);

            SetPrivateField(io, "obstacleLayers", (LayerMask)(1 << 8));

            // WHY: Wall on default layer (0) — NOT included in obstacleLayers
            GameObject wall = CreateWall(new Vector3(0, 0, 2f), new Vector3(5, 5, 0.2f));
            wall.layer = 0;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Wall on a layer NOT in obstacleLayers should NOT block interaction");
        }

        /// <summary>
        ///     Two obstacles between player and object. 
        ///     The nearest non-target collider blocks.
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleWalls_NearestBlocks()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 8f),
                15f, true,
                distanceCheckPoint: checkPoint.transform);

            CreateWall(new Vector3(0, 0, 3f), new Vector3(5, 5, 0.2f));
            CreateWall(new Vector3(0, 0, 5f), new Vector3(5, 5, 0.2f));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsFalse(InvokeIsInRange(io),
                "Multiple walls between player and object should block");
        }

        /// <summary>
        ///     Check point and interactive object at nearly the same position.
        ///     distance < 0.01 → shortcut returns true (no raycast needed).
        /// </summary>
        [UnityTest]
        public IEnumerator VeryCloseDistance_AlwaysInRange()
        {
            GameObject checkPoint = Track(new GameObject("Player"));
            checkPoint.transform.position = Vector3.zero;

            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 0.005f),
                10f, true,
                distanceCheckPoint: checkPoint.transform);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(InvokeIsInRange(io),
                "Overlapping positions should always be in range (distance < 0.01 shortcut)");
        }

        [UnityTest]
        public IEnumerator Disabled_Interactable_DoesNotFireClick()
        {
            InteractiveObject io = CreateInteractive(new Vector3(0, 0, 2f),
                0f, false);

            bool clicked = false;
            io.onClick.AddListener(() => clicked = true);
            io.interactable = false;

            yield return null;

            SetPrivateField(io, "hasCurrentMouseHit", true);
            PointerEventData pointerData = new PointerEventData(_eventSystem)
            {
                button = PointerEventData.InputButton.Left
            };
            io.OnPointerClick(pointerData);

            Assert.IsFalse(clicked, "Click should NOT fire when interactable=false");
        }
    }
}
