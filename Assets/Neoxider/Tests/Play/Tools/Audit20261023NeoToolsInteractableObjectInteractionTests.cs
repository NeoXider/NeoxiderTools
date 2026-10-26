using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Tests.Play
{
    public class Audit20261023NeoToolsInteractableObjectInteractionTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<GameObject> _previousMainCameras = new();
        private Camera _camera;
        private RenderTexture _texture;
        private InteractionRayProvider _provider;
        private InteractiveObject _interactive;
        private static readonly bool[] Flags = { false, true };
        private static readonly string[] AvailabilityCauses = { "hover", "interactable", "disable" };
        private static readonly InteractionRayProvider.RayMode[] CenterModes =
            { InteractionRayProvider.RayMode.Both, InteractionRayProvider.RayMode.ScreenCenter };

        [SetUp]
        public void SetUp()
        {
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.CompareTag("MainCamera"))
                {
                    _previousMainCameras.Add(camera.gameObject);
                    camera.gameObject.tag = "Untagged";
                }
            }

            GameObject cameraObject = CreateObject("AuditInteractionCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(1750f, 1750f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.cullingMask = 0;
            _texture = new RenderTexture(512, 512, 0);
            _camera.targetTexture = _texture;
            _provider = cameraObject.AddComponent<InteractionRayProvider>();
            _provider.Mode = InteractionRayProvider.RayMode.ScreenCenter;

            GameObject target = CreateObject("AuditInteractionTarget");
            target.transform.position = cameraObject.transform.position + Vector3.forward * 5f;
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.25f;
            _interactive = AddInteractive(target);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            if (_texture != null)
            {
                _texture.Release();
                Object.DestroyImmediate(_texture);
            }

            foreach (GameObject cameraObject in _previousMainCameras)
            {
                if (cameraObject != null)
                {
                    cameraObject.tag = "MainCamera";
                }
            }

            _previousMainCameras.Clear();
        }

        [Test]
        public void PointerHover_OnlyEmitsRealTransitions()
        {
            int enters = 0;
            int exits = 0;
            List<bool> changes = new();
            _interactive.onHoverEnter.AddListener(() => enters++);
            _interactive.onHoverExit.AddListener(() => exits++);
            _interactive.onHoverChanged.AddListener(changes.Add);
            PointerEventData pointer = new(EventSystem.current);

            _interactive.OnPointerEnter(pointer);
            _interactive.OnPointerEnter(pointer);
            _interactive.OnPointerExit(pointer);
            _interactive.OnPointerExit(pointer);

            Assert.That(enters, Is.EqualTo(1));
            Assert.That(exits, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(new[] { true, false }));
            Assert.That(_interactive.IsHovered, Is.False);
        }

        [UnityTest]
        public IEnumerator HoverEnter_ReentrantReset_DoesNotPublishAnObsoleteTrue([ValueSource(nameof(Flags))] bool disableComponent)
        {
            // SetUp may already have had an automatic hover pass before this coroutine starts.
            _interactive.transform.position += Vector3.right * 10f;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(_interactive.IsHovered, Is.False, "The enter observer requires a real unhovered starting state.");

            List<string> sequence = new();
            _interactive.onHoverEnter.AddListener(() =>
            {
                sequence.Add("enter");
                if (disableComponent)
                {
                    _interactive.enabled = false;
                }
                else
                {
                    _interactive.UseHoverDetection = false;
                }
            });
            _interactive.onHoverExit.AddListener(() => sequence.Add("exit"));
            _interactive.onHoverChanged.AddListener(value => sequence.Add("changed:" + value));

            _interactive.transform.position = _camera.transform.position + Vector3.forward * 5f;
            Physics.SyncTransforms();
            yield return null;

            Assert.That(sequence, Does.Contain("enter"), "The actual camera pass must reach the reset observer.");
            Assert.That(_interactive.IsHovered, Is.False);
            Assert.That(sequence, Is.EqualTo(new[] { "enter", "exit", "changed:False" }));
        }

        [Test]
        public void HoverExit_ReentrantEnter_DoesNotPublishAnObsoleteFalse()
        {
            List<string> sequence = new();
            PointerEventData pointer = new(EventSystem.current);
            bool enteredAgain = false;
            _interactive.onHoverEnter.AddListener(() => sequence.Add("enter"));
            _interactive.onHoverChanged.AddListener(value => sequence.Add("changed:" + value));
            _interactive.OnPointerEnter(pointer);
            sequence.Clear();
            _interactive.onHoverExit.AddListener(() =>
            {
                sequence.Add("exit");
                if (!enteredAgain)
                {
                    enteredAgain = true;
                    _interactive.OnPointerEnter(pointer);
                }
            });

            _interactive.OnPointerExit(pointer);

            Assert.That(_interactive.IsHovered, Is.True);
            Assert.That(sequence, Is.EqualTo(new[] { "exit", "enter", "changed:True" }));
        }

        [UnityTest]
        public IEnumerator LosingInteractionAvailability_ClearsHoverExactlyOnce([ValueSource(nameof(AvailabilityCauses))] string cause)
        {
            int exits = 0;
            _interactive.onHoverExit.AddListener(() => exits++);
            PointerEventData pointer = new(EventSystem.current);
            _interactive.OnPointerEnter(pointer);
            Assert.That(_interactive.IsHovered, Is.True);

            if (cause == "hover")
            {
                _interactive.UseHoverDetection = false;
            }
            else if (cause == "interactable")
            {
                _interactive.interactable = false;
            }
            else
            {
                _interactive.enabled = false;
            }

            yield return null;
            _interactive.OnPointerExit(pointer);
            Assert.That(_interactive.IsHovered, Is.False);
            Assert.That(exits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LostCamera_ClearsAnExistingRaycastHover()
        {
            int exits = 0;
            _interactive.onHoverExit.AddListener(() => exits++);
            _interactive.UseScreenCenterRay = true;
            Physics.SyncTransforms();
            yield return null;
            Assert.That(_interactive.IsHovered, Is.True);

            Object.DestroyImmediate(_camera.gameObject);
            yield return null;

            Assert.That(_interactive.IsHovered, Is.False);
            Assert.That(exits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BothMode_ClickDoesNotReuseTheCenterHoverHit()
        {
            // Pick the viewport half away from the current pointer. Missing/non-finite input
            // also has no click target, while a center hover remains available without a mouse.
            bool hasPointer = MouseInputCompat.TryGetPosition(out Vector3 pointerPosition);
            _camera.rect = new Rect(hasPointer && pointerPosition.x > 256f ? 0f : 0.5f, 0f, 0.5f, 1f);
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _provider.Mode = InteractionRayProvider.RayMode.Both;
            _interactive.UseMouseInteraction = true;
            int clicks = 0;
            _interactive.onClick.AddListener(() => clicks++);
            Physics.SyncTransforms();

            yield return null;
            _interactive.OnPointerClick(new PointerEventData(EventSystem.current));

            Assert.That(_interactive.IsHovered, Is.True, "Both mode hovers at the viewport center.");
            Assert.That(clicks, Is.Zero, "The pointer ray misses this target even though the center ray hits it.");

            _provider.Mode = InteractionRayProvider.RayMode.ScreenCenter;
            yield return null;
            _interactive.OnPointerClick(new PointerEventData(EventSystem.current));

            Assert.That(clicks, Is.EqualTo(1), "ScreenCenter mode uses the center ray for clicks as well.");
        }

        [UnityTest]
        public IEnumerator CenterHover_IsNotOverriddenByEventSystemCursorEnterOrExit(
            [ValueSource(nameof(CenterModes))] InteractionRayProvider.RayMode mode)
        {
            _provider.Mode = mode;
            GameObject sideTarget = CreateObject("AuditCursorTarget");
            sideTarget.transform.position = _camera.transform.position + new Vector3(4f, 0f, 5f);
            sideTarget.AddComponent<BoxCollider>();
            InteractiveObject sideInteractive = AddInteractive(sideTarget);
            int sideEnters = 0;
            sideInteractive.onHoverEnter.AddListener(() => sideEnters++);
            PhysicsRaycaster raycaster = _camera.GetComponent<PhysicsRaycaster>();
            PointerEventData pointer = new(EventSystem.current)
            {
                pointerCurrentRaycast = new RaycastResult
                {
                    gameObject = sideTarget,
                    module = raycaster,
                    worldPosition = sideTarget.transform.position
                }
            };
            Physics.SyncTransforms();
            yield return null;

            sideInteractive.OnPointerEnter(pointer);
            _interactive.OnPointerExit(pointer);

            Assert.That(_interactive.IsHovered, Is.True, "The crosshair still points at the center target.");
            Assert.That(sideInteractive.IsHovered, Is.False, "A cursor hit must not replace crosshair hover.");
            Assert.That(sideEnters, Is.Zero);
        }

        [TestCase(InteractionRayProvider.RayMode.ScreenCenter)]
        [TestCase(InteractionRayProvider.RayMode.Both)]
        public void CenterRay_UsesTheOffsetCameraViewport(InteractionRayProvider.RayMode mode)
        {
            _camera.rect = new Rect(0.5f, 0.25f, 0.5f, 0.5f);
            _provider.Mode = mode;
            Ray expected = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Assert.That(_provider.TryGetHoverRay(out Ray hoverRay), Is.True);
            Assert.That(Vector3.Distance(hoverRay.origin, expected.origin), Is.LessThan(0.0001f));
            Assert.That(Vector3.Angle(hoverRay.direction, _camera.transform.forward), Is.LessThan(0.001f));
            if (mode == InteractionRayProvider.RayMode.ScreenCenter)
            {
                Assert.That(_provider.TryGetClickRay(out Ray clickRay), Is.True);
                Assert.That(Vector3.Distance(clickRay.origin, expected.origin), Is.LessThan(0.0001f));
                Assert.That(Vector3.Angle(clickRay.direction, _camera.transform.forward), Is.LessThan(0.001f));
            }
        }

        [Test]
        public void ProviderLookup_FollowsTheCurrentMainCamera()
        {
            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.SameAs(_provider));
            _camera.gameObject.tag = "Untagged";
            GameObject nextCamera = CreateObject("AuditNextCamera");
            nextCamera.AddComponent<Camera>();
            nextCamera.tag = "MainCamera";

            InteractionRayProvider nextProvider = InteractionRayProvider.FindOnMainCamera();

            Assert.That(nextProvider, Is.Not.Null);
            Assert.That(nextProvider.gameObject, Is.SameAs(nextCamera));
            Assert.That(nextProvider, Is.Not.SameAs(_provider));
        }

        [Test]
        public void ProviderLookup_DoesNotReturnADisabledProviderOrAnUntaggedCamera()
        {
            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.SameAs(_provider));
            _provider.enabled = false;
            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.Null);
            _provider.enabled = true;
            _camera.gameObject.tag = "Untagged";

            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.Null);
        }

        [Test]
        public void SceneSetup_InstallsBothPhysicsRaycastersOnEveryNewCamera()
        {
            GameObject initial3D = CreateObject("AuditInitial3DTarget");
            initial3D.AddComponent<BoxCollider>();
            AddInteractive(initial3D);
            GameObject initial2D = CreateObject("AuditInitial2DTarget");
            initial2D.AddComponent<BoxCollider2D>();
            AddInteractive(initial2D); // Each valid collider graph receives one native activation.
            AssertHasBothRaycasters(_camera);
            _camera.gameObject.tag = "Untagged";
            GameObject nextCamera = CreateObject("AuditNextRaycasterCamera");
            Camera next = nextCamera.AddComponent<Camera>();
            nextCamera.tag = "MainCamera";
            GameObject next3D = CreateObject("AuditNext3DTarget");
            next3D.AddComponent<BoxCollider>();
            AddInteractive(next3D);
            GameObject next2D = CreateObject("AuditNext2DTarget");
            next2D.AddComponent<BoxCollider2D>();
            AddInteractive(next2D);

            AssertHasBothRaycasters(next);
        }

        [Test]
        public void UiPointerClick_WithAGraphicHit_DoesNotRequireAPhysicsCollider()
        {
            GameObject canvasObject = CreateObject("AuditInteractionCanvas", typeof(RectTransform), typeof(Canvas));
            GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            GameObject uiObject = CreateObject("AuditInteractionUi", typeof(RectTransform));
            uiObject.transform.SetParent(canvasObject.transform, false);
            InteractiveObject ui = AddInteractive(uiObject);
            ui.UseMouseInteraction = true;
            int clicks = 0;
            ui.onClick.AddListener(() => clicks++);
            PointerEventData pointer = new(EventSystem.current)
            {
                pointerCurrentRaycast = new RaycastResult { gameObject = canvasObject, module = raycaster }
            };

            ui.OnPointerClick(pointer);
            Assert.That(clicks, Is.Zero, "A hit outside this UI target's hierarchy is not a click on it.");

            pointer.pointerCurrentRaycast = new RaycastResult { gameObject = uiObject, module = raycaster };
            ui.OnPointerClick(pointer);

            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(uiObject.GetComponent<Collider>(), Is.Null);
            Assert.That(uiObject.GetComponent<Collider2D>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator SharedLevelRoot_DoesNotHideTheTargetOrIgnoreSiblingWalls([ValueSource(nameof(Flags))] bool blocked)
        {
            GameObject root = CreateObject("AuditSharedLevelRoot");
            _camera.transform.SetParent(root.transform, true);
            _interactive.transform.SetParent(root.transform, true);
            _interactive.DistanceCheckPoint = _camera.transform;
            if (blocked)
            {
                GameObject wall = CreateObject("AuditSharedRootWall");
                wall.transform.SetParent(root.transform, true);
                wall.transform.position = _camera.transform.position + Vector3.forward * 2f;
                wall.AddComponent<BoxCollider>();
            }

            Physics.SyncTransforms();
            yield return null;

            Assert.That(_interactive.IsHovered, Is.EqualTo(!blocked),
                "Organizing the camera, target and walls under one root must preserve target/obstacle selection.");
        }

        private GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject instance = new(name, components);
            _objects.Add(instance);
            return instance;
        }

        private static InteractiveObject AddInteractive(GameObject target)
        {
            target.SetActive(false);
            InteractiveObject interactive = target.AddComponent<InteractiveObject>();
            JsonUtility.FromJsonOverwrite("{\"_autoCheckEventSystem\":false,\"_autoCreateEventSystemIfMissing\":false}", interactive);
            interactive.InteractionDistance = 0f;
            interactive.UseKeyboardInteraction = false;
            interactive.UseMouseInteraction = false;
            interactive.onHoverEnter = new UnityEvent();
            interactive.onHoverExit = new UnityEvent();
            interactive.onHoverChanged = new UnityEvent<bool>();
            interactive.onClick = new UnityEvent();
            target.SetActive(true);
            interactive.DistanceCheckPoint = null;
            return interactive;
        }

        private static void AssertHasBothRaycasters(Camera camera)
        {
            int raycasters3D = 0;
            int raycasters2D = 0;
            foreach (PhysicsRaycaster raycaster in camera.GetComponents<PhysicsRaycaster>())
            {
                if (raycaster is Physics2DRaycaster)
                {
                    raycasters2D++;
                }
                else
                {
                    raycasters3D++;
                }
            }

            Assert.That(raycasters3D, Is.EqualTo(1));
            Assert.That(raycasters2D, Is.EqualTo(1));
        }
    }
}
