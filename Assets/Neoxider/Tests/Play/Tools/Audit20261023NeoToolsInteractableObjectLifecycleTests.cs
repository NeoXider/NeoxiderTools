using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public partial class Audit20261023NeoToolsInteractableObjectLifecycleTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<GameObject> _previousMainCameras = new();
        private RenderTexture _texture;
        private Camera _camera;
        private Audit20261023NeoToolsInteractableObjectInputDriver _input;
        private bool _globalTriggers2D;
        private bool _startInside2D;
        private static readonly Vector3 Origin = new(4800f, 4800f, 0f);

        [SetUp]
        public void SetUp()
        {
            _globalTriggers2D = Physics2D.queriesHitTriggers;
            _startInside2D = Physics2D.queriesStartInColliders;
            Physics2D.queriesHitTriggers = true;
            Physics2D.queriesStartInColliders = true;
            foreach (Camera existing in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (existing.CompareTag("MainCamera"))
                {
                    _previousMainCameras.Add(existing.gameObject);
                    existing.gameObject.tag = "Untagged";
                }
            }

            GameObject cameraObject = CreateObject("AutomaticInteractionCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = Origin;
            _camera = cameraObject.AddComponent<Camera>();
            _camera.cullingMask = 0;
            _texture = new RenderTexture(256, 256, 0);
            _camera.targetTexture = _texture;
            cameraObject.AddComponent<InteractionRayProvider>().Mode = InteractionRayProvider.RayMode.ScreenCenter;
            InteractionPhysicsQueries.Invalidate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject owned in _objects)
            {
                if (owned != null)
                {
                    Object.Destroy(owned);
                }
            }

            _objects.Clear();
            yield return null;
            if (_texture != null)
            {
                _texture.Release();
                Object.Destroy(_texture);
            }

            foreach (GameObject previous in _previousMainCameras)
            {
                if (previous != null)
                {
                    previous.tag = "MainCamera";
                }
            }

            _previousMainCameras.Clear();
            _input?.Dispose();
            _input = null;
            Physics2D.queriesHitTriggers = _globalTriggers2D;
            Physics2D.queriesStartInColliders = _startInside2D;
            InteractionPhysicsQueries.Invalidate();
        }

        [UnityTest]
        public IEnumerator NativeDisableFromHoverEnter_DeliversOnlyTheFinalFalseNotification()
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f);
            target.onHoverEnter = new UnityEvent();
            target.onHoverExit = new UnityEvent();
            target.onHoverChanged = new UnityEvent<bool>();
            List<string> sequence = new();
            target.onHoverEnter.AddListener(() =>
            {
                sequence.Add("enter");
                target.enabled = false;
            });
            target.onHoverExit.AddListener(() => sequence.Add("exit"));
            target.onHoverChanged.AddListener(value => sequence.Add("changed:" + value));
            yield return null;

            Assert.That(target.enabled, Is.False);
            Assert.That(target.IsHovered, Is.False);
            Assert.That(sequence, Is.EqualTo(new[] { "enter", "exit", "changed:False" }));
        }

        private enum MouseStage { Down, Up, Click }
        private enum ResetKind { Disable, DisableReenable, Destroy }

        [UnityTest] public IEnumerator MouseDown_DisableStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Down, ResetKind.Disable);
        [UnityTest] public IEnumerator MouseUp_DisableStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Up, ResetKind.Disable);
        [UnityTest] public IEnumerator MouseClick_DisableStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Click, ResetKind.Disable);
        [UnityTest] public IEnumerator MouseDown_DisableReenableStopsOldOperation() => MouseResetStopsTail(MouseStage.Down, ResetKind.DisableReenable);
        [UnityTest] public IEnumerator MouseUp_DisableReenableStopsOldOperation() => MouseResetStopsTail(MouseStage.Up, ResetKind.DisableReenable);
        [UnityTest] public IEnumerator MouseClick_DisableReenableStopsOldOperation() => MouseResetStopsTail(MouseStage.Click, ResetKind.DisableReenable);
        [UnityTest] public IEnumerator MouseDown_DestroyStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Down, ResetKind.Destroy);
        [UnityTest] public IEnumerator MouseUp_DestroyStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Up, ResetKind.Destroy);
        [UnityTest] public IEnumerator MouseClick_DestroyStopsAutomaticTail() => MouseResetStopsTail(MouseStage.Click, ResetKind.Destroy);

        private IEnumerator MouseResetStopsTail(MouseStage stage, ResetKind reset)
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, mouse: true, keyboard: true);
            yield return null; // Warm a real automatic hover/range pass with both devices released.
            Assert.That(target.IsHovered, Is.True);
            int downs = 0;
            int ups = 0;
            int clicks = 0;
            int rangeEnters = 0;
            target.onInteractDown = new UnityEvent();
            target.onInteractUp = new UnityEvent();
            target.onClick = new UnityEvent();
            target.onEnterRange = new UnityEvent();
            target.onInteractDown.AddListener(() => downs++);
            target.onInteractUp.AddListener(() => ups++);
            target.onClick.AddListener(() => clicks++);
            target.onEnterRange.AddListener(() => rangeEnters++);
            UnityEvent interruptedEvent = stage == MouseStage.Down ? target.onInteractDown :
                stage == MouseStage.Up ? target.onInteractUp : target.onClick;
            interruptedEvent.AddListener(() =>
            {
                if (reset == ResetKind.Destroy)
                {
                    Object.DestroyImmediate(target.gameObject);
                }
                else
                {
                    target.enabled = false;
                    if (reset == ResetKind.DisableReenable)
                    {
                        target.enabled = true;
                    }
                }
            });

            if (stage != MouseStage.Down)
            {
                _input.QueueMouse(true);
                yield return null;
                Assert.That(downs, Is.EqualTo(1), "Positive control: automatic mouse down must reach the observer.\n" + _input.Diagnostics);
            }

            _input.QueueMouse(stage == MouseStage.Down);
            _input.QueueKeyboard(true); // Would expose an incorrectly resumed keyboard tail in this same Update.
            yield return null;
            Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, "The native keyboard edge must really be present.\n" + _input.Diagnostics);
            Assert.That(downs, Is.EqualTo(1));
            Assert.That(ups, Is.EqualTo(stage == MouseStage.Down ? 0 : 1));
            Assert.That(clicks, Is.EqualTo(stage == MouseStage.Click ? 1 : 0));
            Assert.That(rangeEnters, Is.Zero, "The interrupted operation must not re-enter range after its reset.");
            LogAssert.NoUnexpectedReceived();

            _input.QueueMouse(false);
            _input.QueueKeyboard(false);
            if (reset == ResetKind.Destroy)
            {
                Assert.That(target == null, Is.True);
            }
            else
            {
                Assert.That(target.enabled, Is.EqualTo(reset == ResetKind.DisableReenable));
                target.enabled = true;
                yield return null;
                Assert.That(rangeEnters, Is.EqualTo(1), "The new enabled lifetime must enter range again.");
                Assert.That(clicks, Is.EqualTo(stage == MouseStage.Click ? 1 : 0), "A reset gesture cannot leak a click.");
            }
        }

        [UnityTest] public IEnumerator RangeEnter_DisableThenReopenEntersAgain() => RangeResetOwnsState(false);
        [UnityTest] public IEnumerator RangeEnter_DisableReenableCancelsKeyboardTail() => RangeResetOwnsState(true);

        private IEnumerator RangeResetOwnsState(bool reenableInsideObserver)
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, keyboard: true, hover: false);
            int enters = 0;
            int keyboardDowns = 0;
            target.onEnterRange = new UnityEvent();
            target.onInteractDown = new UnityEvent();
            target.onInteractDown.AddListener(() => keyboardDowns++);
            target.onEnterRange.AddListener(() =>
            {
                enters++;
                if (enters == 1)
                {
                    target.enabled = false;
                    if (reenableInsideObserver)
                    {
                        target.enabled = true;
                    }
                }
            });
            _input.QueueKeyboard(true);
            yield return null;
            Assert.That(enters, Is.EqualTo(1));
            Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
            Assert.That(keyboardDowns, Is.Zero);
            _input.QueueKeyboard(false);
            target.enabled = true;
            yield return null;
            Assert.That(enters, Is.EqualTo(2), "Nested OnDisable reset wins over the old range transition.");
        }

        [UnityTest]
        public IEnumerator RangeExit_DisableReenableCancelsKeyboardTail()
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, keyboard: true, hover: false);
            JsonUtility.FromJsonOverwrite("{\"keyboardInteractionMode\":1,\"checkObstacles\":false}", target);
            target.DistanceCheckPoint = _camera.transform;
            target.InteractionDistance = 10f;
            int enters = 0;
            int exits = 0;
            int downs = 0;
            target.onEnterRange = new UnityEvent();
            target.onExitRange = new UnityEvent();
            target.onInteractDown = new UnityEvent();
            target.onEnterRange.AddListener(() => enters++);
            target.onInteractDown.AddListener(() => downs++);
            target.onExitRange.AddListener(() =>
            {
                exits++;
                target.enabled = false;
                target.InteractionDistance = 0f; // The old keyboard tail would now be eligible if it continued.
                target.enabled = true;
            });
            yield return null;
            Assert.That(enters, Is.EqualTo(1));
            target.transform.position += Vector3.up * 20f;
            Physics.SyncTransforms();
            InteractionRayProvider.InvalidateQueries();
            _input.QueueKeyboard(true);
            yield return null;
            Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
            Assert.That(exits, Is.EqualTo(1));
            Assert.That(downs, Is.Zero);
            _input.QueueKeyboard(false);
            yield return null;
            Assert.That(enters, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator KeyboardTap_AimChangeRevalidatesUp() => KeyboardTapRevalidatesUp(false, false);
        [UnityTest] public IEnumerator KeyboardTap_RangeChangeRevalidatesUp() => KeyboardTapRevalidatesUp(true, false);
        [UnityTest] public IEnumerator KeyboardTap_DisableReenableCancelsUp() => KeyboardTapRevalidatesUp(false, true);
        [UnityTest] public IEnumerator KeyboardTap_InputFlagChangeSuppressesUp() => KeyboardTapRevalidatesUp(false, false, true);

        private IEnumerator KeyboardTapRevalidatesUp(bool changeRange, bool resetLifetime, bool disableInput = false)
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, keyboard: true, hover: false);
            int downs = 0;
            int ups = 0;
            bool mutate = false;
            target.onInteractDown = new UnityEvent();
            target.onInteractUp = new UnityEvent();
            target.onInteractDown.AddListener(() =>
            {
                downs++;
                if (!mutate)
                {
                    return;
                }

                if (disableInput)
                {
                    target.UseKeyboardInteraction = false;
                }
                else if (resetLifetime)
                {
                    target.enabled = false;
                    target.enabled = true;
                }
                else if (changeRange)
                {
                    target.DistanceCheckPoint = _camera.transform;
                    target.InteractionDistance = 0.1f;
                }
                else
                {
                    _camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
            });
            target.onInteractUp.AddListener(() => ups++);
            yield return null;
            _input.QueueKeyboardTap();
            yield return null;
            AssertNativeTap();
            Assert.That(downs, Is.EqualTo(1), _input.Diagnostics);
            Assert.That(ups, Is.EqualTo(1), "Positive control: an eligible native tap delivers down and up.");
            yield return null; // Clear the first tap's frame flags before queuing the next one.
            mutate = true;
            _input.QueueKeyboardTap();
            yield return null;
            AssertNativeTap();
            Assert.That(downs, Is.EqualTo(2));
            Assert.That(ups, Is.EqualTo(1), "Down's observer invalidated the remaining up eligibility.");
        }

        [UnityTest]
        public IEnumerator HoverCallbackEpoch_DoesNotCancelEligibleMouseOrRange()
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, mouse: true);
            int hovers = 0;
            int downs = 0;
            int clicks = 0;
            int rangeEnters = 0;
            target.onHoverEnter = new UnityEvent();
            target.onInteractDown = new UnityEvent();
            target.onClick = new UnityEvent();
            target.onEnterRange = new UnityEvent();
            target.onHoverEnter.AddListener(() => hovers++);
            target.onInteractDown.AddListener(() => downs++);
            target.onClick.AddListener(() => clicks++);
            target.onEnterRange.AddListener(() => rangeEnters++);
            InteractionPhysicsQueries.ResetCounters();
            _input.QueueMouse(true);
            yield return null;
            Assert.That(hovers, Is.EqualTo(1));
            Assert.That(downs, Is.EqualTo(1), _input.Diagnostics);
            Assert.That(rangeEnters, Is.EqualTo(1));
            Assert.That(InteractionPhysicsQueries.Counters.CallbackBoundaries, Is.GreaterThanOrEqualTo(3));
            _input.QueueMouse(false);
            yield return null;
            Assert.That(clicks, Is.EqualTo(1), "Physics epoch changes refresh geometry without cancelling the target lifetime.");
        }

        [UnityTest] public IEnumerator ClickDisableReopen_ResetsDoubleClickHistory() => DoubleClickReset(false);
        [UnityTest] public IEnumerator ClickDisableReenable_ResetsDoubleClickHistory() => DoubleClickReset(true);

        private IEnumerator DoubleClickReset(bool reenableInsideObserver)
        {
            EnsureInput();
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, mouse: true);
            JsonUtility.FromJsonOverwrite("{\"doubleClickThreshold\":30}", target);
            List<string> clicks = new();
            bool closeOnNextSingle = false;
            target.onClick = new UnityEvent();
            target.onDoubleClick = new UnityEvent();
            target.onClick.AddListener(() =>
            {
                clicks.Add("single");
                if (closeOnNextSingle)
                {
                    closeOnNextSingle = false;
                    target.enabled = false;
                    if (reenableInsideObserver)
                    {
                        target.enabled = true;
                    }
                }
            });
            target.onDoubleClick.AddListener(() => clicks.Add("double"));
            yield return null;
            yield return Click();
            yield return Click();
            Assert.That(clicks, Is.EqualTo(new[] { "single", "double" }), "Ordinary double-click positive control.\n" + _input.Diagnostics);

            target.enabled = false;
            target.enabled = true;
            closeOnNextSingle = true;
            yield return Click();
            Assert.That(target.enabled, Is.EqualTo(reenableInsideObserver));
            target.enabled = true;
            yield return Click();
            Assert.That(clicks, Is.EqualTo(new[] { "single", "double", "single", "single" }),
                "The first click after reopening belongs to the new lifetime.");
            yield return Click();
            Assert.That(clicks, Is.EqualTo(new[] { "single", "double", "single", "single", "double" }));
        }

        [UnityTest]
        public IEnumerator ManualApi_DisabledTargetStillUsesOnlyInteractableGate()
        {
            InteractiveObject target = CreateTarget(Origin + Vector3.forward * 5f, hover: false);
            int calls = 0;
            target.onInteractDown = new UnityEvent();
            target.onInteractUp = new UnityEvent();
            target.onClick = new UnityEvent();
            target.onInteractDown.AddListener(() => calls++);
            target.onInteractUp.AddListener(() => calls++);
            target.onClick.AddListener(() => calls++);
            target.enabled = false;
            target.InteractDown();
            target.InteractUp();
            target.Click();
            Assert.That(calls, Is.EqualTo(3));
            target.interactable = false;
            target.InteractDown();
            target.InteractUp();
            target.Click();
            Assert.That(calls, Is.EqualTo(3));
            yield return null;
        }

        private IEnumerator Click()
        {
            _input.QueueMouse(true);
            yield return null;
            Assert.That(MouseInputCompat.TryGetButton(0, out bool held) && held, Is.True, _input.Diagnostics);
            _input.QueueMouse(false);
            yield return null;
            Assert.That(MouseInputCompat.TryGetButton(0, out held) && !held, Is.True, _input.Diagnostics);
        }

        private void AssertNativeTap()
        {
            Assert.That(KeyInputCompat.GetKeyDown(KeyCode.E), Is.True, _input.Diagnostics);
            Assert.That(KeyInputCompat.GetKeyUp(KeyCode.E), Is.True,
                "Both edges must be present to exercise the stale-up tail.\n" + _input.Diagnostics);
        }

        private void EnsureInput()
        {
            _input ??= new Audit20261023NeoToolsInteractableObjectInputDriver();
        }

        private InteractiveObject CreateTarget(Vector3 position, bool mouse = false, bool keyboard = false,
            bool hover = true, bool twoDimensions = false)
        {
            GameObject instance = CreateObject("AutomaticInteractionTarget");
            instance.SetActive(false);
            instance.transform.position = position;
            if (twoDimensions)
            {
                instance.AddComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
            }
            else
            {
                instance.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            }

            InteractiveObject target = instance.AddComponent<InteractiveObject>();
            JsonUtility.FromJsonOverwrite("{\"_autoCheckEventSystem\":false,\"_autoCreateEventSystemIfMissing\":false}", target);
            target.InteractionDistance = 0f;
            target.UseScreenCenterRay = true;
            target.UseMouseInteraction = mouse;
            target.UseKeyboardInteraction = keyboard;
            target.UseHoverDetection = hover;
            target.onHoverEnter = null;
            target.onHoverExit = null;
            target.onHoverChanged = null;
            target.onEnterRange = null;
            target.onExitRange = null;
            target.onInteractDown = null;
            target.onInteractUp = null;
            target.onClick = null;
            target.onDoubleClick = null;
            target.onRightClick = null;
            target.onMiddleClick = null;
            instance.SetActive(true);
            target.DistanceCheckPoint = null; // Explicitly remove Awake's camera fallback: no unique LOS in stable-context cases.
            return target;
        }

        private static void SetField(InteractiveObject target, string name, object value)
        {
            typeof(InteractiveObject).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static bool InteractedThisFrame(InteractiveObject target)
        {
            return (bool)typeof(InteractiveObject).GetField("isInteractingThisFrame",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private GameObject CreateObject(string name)
        {
            GameObject instance = new(name);
            _objects.Add(instance);
            return instance;
        }
    }
}
