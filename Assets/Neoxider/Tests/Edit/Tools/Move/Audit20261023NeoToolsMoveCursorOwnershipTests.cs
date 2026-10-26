using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMoveCursorOwnershipTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<CursorLockController> _controllers = new List<CursorLockController>();
        private bool _priorGlobalManagement;
        private bool _priorVisible;
        private CursorLockMode _priorLock;
        private PlayerController3DPhysics _player;
        private bool _enteredPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _enteredPlayMode = false;
            Assert.That(Application.isPlaying, Is.False, "This Edit fixture must own its Play transition.");
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            Assert.That(Application.isPlaying, Is.True);
            _priorGlobalManagement = CursorLockController.GlobalCursorManagement;
            _priorVisible = Cursor.visible;
            _priorLock = Cursor.lockState;
            CursorLockController.GlobalCursorManagement = true;
            GameObject owner = CreateObject();
            _player = owner.AddComponent<PlayerController3DPhysics>();
            SetField(_player, "_groundCheck", owner.transform);
            SetField(_player, "_cameraPivot", owner.transform);
            SetField(_player, "_logSetupWarnings", false);
            SetField(_player, "_disableLookOnPause", false);
            SetField(_player, "_useGameSettingsMouseSensitivity", false);
            _player.CursorControlEnabled = false;
            _player.SetMoveInput(Vector2.zero);
            owner.GetComponent<Rigidbody>().useGravity = false;
            owner.SetActive(true);
            // Ask through the public editor menu; capability assertions below still require actual native focus/readback.
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!_enteredPlayMode)
            {
                yield break;
            }

            System.Exception cleanupFailure = null;
            try
            {
                for (int i = _controllers.Count - 1; i >= 0; i--)
                {
                    if (_controllers[i] != null)
                    {
                        _controllers[i].ReleaseControl();
                    }
                }

                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            catch (System.Exception exception)
            {
                cleanupFailure = exception;
            }
            finally
            {
                _controllers.Clear();
                _objects.Clear();
                Cursor.lockState = _priorLock;
                Cursor.visible = _priorVisible;
                CursorLockController.GlobalCursorManagement = _priorGlobalManagement;
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            _enteredPlayMode = false;
            if (cleanupFailure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
            }
        }

        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        public void TwoOwners_RestoreEachOriginalGateOnlyAfterLastRelease(
            bool topFirst, bool initialLook, bool initialMovement)
        {
            _player.SetLookEnabled(initialLook);
            _player.SetMovementEnabled(initialMovement);
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            first.ShowCursor();
            first.ShowCursor();
            second.ShowCursor();
            AssertBlocked();

            CursorLockController released = topFirst ? second : first;
            CursorLockController remaining = topFirst ? first : second;
            released.ReleaseControl();
            released.ReleaseControl();
            AssertBlocked();
            Assert.That(remaining.HasCursorOwnership, Is.True);

            remaining.ReleaseControl();
            remaining.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.EqualTo(initialLook));
            Assert.That(_player.MovementEnabled, Is.EqualTo(initialMovement));
        }

        [Test]
        public void SeparateGateRequests_RestoreMovementWhileLookStillHasAnOwner()
        {
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            second.DisableMovementWhileCursorVisible = false;
            first.ShowCursor();
            second.ShowCursor();
            AssertBlocked();

            first.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.False);
            Assert.That(_player.MovementEnabled, Is.True);

            second.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_player.MovementEnabled, Is.True);
        }

        [Test]
        public void UnregisterAndRegister_ReleaseOnlyTheRequestingOwner()
        {
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            first.ShowCursor();
            second.ShowCursor();

            first.UnregisterPlayer(_player);
            AssertBlocked();
            first.RegisterPlayer(_player);
            first.RegisterPlayer(_player);
            second.ReleaseControl();
            AssertBlocked();

            first.UnregisterPlayer(_player);

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_player.MovementEnabled, Is.True);
        }

        [Test]
        public void HideCursor_ReleasesOnlyItsOwnRequests()
        {
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            first.ShowCursor();
            second.ShowCursor();

            first.HideCursor();
            AssertBlocked();
            second.HideCursor();

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_player.MovementEnabled, Is.True);
        }

        [Test]
        public void DisableAndDestroyCleanup_DoNotRestoreAnotherOwnersGate()
        {
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            first.ShowCursor();
            second.ShowCursor();

            first.DisableController();
            Invoke(first, "OnDestroy");
            AssertBlocked();
            Invoke(second, "OnDestroy");
            Invoke(second, "OnDestroy");

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_player.MovementEnabled, Is.True);
        }

        [Test]
        public void DestroyedPlayer_CanBeReleasedWithoutRetainingOtherPlayersBaseline()
        {
            CursorLockController first = CreateController();
            CursorLockController second = CreateController();
            first.ShowCursor();
            second.ShowCursor();
            Object.DestroyImmediate(_player.gameObject);

            first.ReleaseControl();
            second.ReleaseControl();

            GameObject other = CreateObject();
            PlayerController3DPhysics next = other.AddComponent<PlayerController3DPhysics>();
            next.CursorControlEnabled = false;
            next.SetMovementEnabled(false);
            first.RegisterPlayer(next);
            first.ShowCursor();
            first.ReleaseControl();
            Assert.That(next.MovementEnabled, Is.False);
            Assert.That(next.LookEnabled, Is.True);
        }

        [TestCase(false, false, true, true)]
        [TestCase(false, true, true, true)]
        [TestCase(true, false, true, true)]
        [TestCase(true, true, true, true)]
        [TestCase(false, false, false, true)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, true)]
        [TestCase(true, true, false, true)]
        [TestCase(false, false, true, false)]
        [TestCase(false, true, true, false)]
        [TestCase(true, false, true, false)]
        [TestCase(true, true, true, false)]
        [TestCase(false, false, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        public void DefaultPlayer_MixedGatesPreserveSurvivingCursorAndExactBaselines(
            bool lookOwnerIsTop, bool closeTopFirst, bool initialLook, bool initialMovement)
        {
            if (initialLook)
            {
                AssertNativeCursorLockCapability();
            }

            _player = CreateDefaultCursorPlayer(initialLook, initialMovement);
            CursorLockController lookOwner = CreateController(true, false);
            CursorLockController movementOwner = CreateController(false, true);
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(lookOwner));
            int lookUnlocks = 0;
            int movementUnlocks = 0;
            ObserveUnlock(lookOwner, () => lookUnlocks++);
            ObserveUnlock(movementOwner, () => movementUnlocks++);
            CursorLockController top = lookOwnerIsTop ? lookOwner : movementOwner;
            CursorLockController lower = lookOwnerIsTop ? movementOwner : lookOwner;
            lower.ShowCursor();
            top.ShowCursor();
            AssertBlocked();
            AssertVisibleCursor();
            Assert.That(lookUnlocks, Is.EqualTo(1));
            Assert.That(movementUnlocks, Is.EqualTo(1));

            CursorLockController first = closeTopFirst ? top : lower;
            CursorLockController remaining = closeTopFirst ? lower : top;
            first.DisableController();
            first.DisableController();
            first.ReleaseControl();

            Assert.That(first.HasCursorOwnership, Is.False);
            Assert.That(remaining.HasCursorOwnership, Is.True);
            AssertVisibleCursor();
            Assert.That(_player.LookEnabled, Is.EqualTo(first == lookOwner && initialLook));
            Assert.That(_player.MovementEnabled, Is.EqualTo(first == movementOwner && initialMovement));
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(lookOwner));
            // An exposed lower controller keeps its existing activation notification. Internal repair is silent.
            Assert.That(lookUnlocks, Is.EqualTo(closeTopFirst && remaining == lookOwner ? 2 : 1));
            Assert.That(movementUnlocks, Is.EqualTo(closeTopFirst && remaining == movementOwner ? 2 : 1));

            remaining.DisableController();
            remaining.DisableController();
            remaining.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.EqualTo(initialLook));
            Assert.That(_player.MovementEnabled, Is.EqualTo(initialMovement));
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(lookOwner));
            bool finalLookAutoLocks = remaining == lookOwner && initialLook;
            Assert.That(Cursor.lockState, Is.EqualTo(finalLookAutoLocks ? CursorLockMode.Locked : CursorLockMode.None));
            Assert.That(Cursor.visible, Is.EqualTo(!finalLookAutoLocks));
            Assert.That(lookUnlocks, Is.EqualTo(closeTopFirst && remaining == lookOwner ? 2 : 1));
            Assert.That(movementUnlocks, Is.EqualTo(closeTopFirst && remaining == movementOwner ? 2 : 1));
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void DefaultPlayer_SingleOwnerPreservesStandaloneLookRestoration(
            bool initialLook, bool initialMovement)
        {
            if (initialLook)
            {
                AssertNativeCursorLockCapability();
            }

            _player = CreateDefaultCursorPlayer(initialLook, initialMovement);
            CursorLockController controller = CreateController();
            int unlocks = 0;
            ObserveUnlock(controller, () => unlocks++);
            controller.ShowCursor();
            AssertBlocked();

            controller.DisableController();
            controller.DisableController();
            controller.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.EqualTo(initialLook));
            Assert.That(_player.MovementEnabled, Is.EqualTo(initialMovement));
            Assert.That(Cursor.lockState, Is.EqualTo(initialLook ? CursorLockMode.Locked : CursorLockMode.None));
            Assert.That(Cursor.visible, Is.EqualTo(!initialLook));
            Assert.That(unlocks, Is.EqualTo(1));
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(controller));
        }

        [Test]
        public void DefaultPlayer_StandaloneSetLookEnabledStillLocksAndHides()
        {
            AssertNativeCursorLockCapability();
            _player = CreateDefaultCursorPlayer(false, true);
            Assert.That(_player.ExternalCursorLockController, Is.Null);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _player.SetLookEnabled(true);

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(Cursor.visible, Is.False);
        }

        private static void AssertNativeCursorLockCapability()
        {
            const string requirement = "MOVE_NATIVE_CURSOR_CAPABILITY: validate in an interactive, focused Game view in Play; " +
                                       "batch/unfocused None cannot establish standalone auto-lock or ownership correctness.";
            Assert.That(Application.isPlaying, Is.True, requirement);
            Assert.That(Application.isFocused, Is.True, requirement);
            UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
            Assert.That(focused, Is.Not.Null, requirement);
            Assert.That(focused.GetType().Name, Is.EqualTo("GameView"), requirement);
            CursorLockMode priorLock = Cursor.lockState;
            bool priorVisible = Cursor.visible;
            try
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked), requirement);
                Assert.That(Cursor.visible, Is.False, requirement);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), requirement);
                Assert.That(Cursor.visible, Is.True, requirement);
            }
            finally
            {
                Cursor.lockState = priorLock;
                Cursor.visible = priorVisible;
            }
        }

        private PlayerController3DPhysics CreateDefaultCursorPlayer(bool initialLook, bool initialMovement)
        {
            GameObject owner = CreateObject();
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            SetField(player, "_groundCheck", owner.transform);
            SetField(player, "_cameraPivot", owner.transform);
            SetField(player, "_logSetupWarnings", false);
            SetField(player, "_disableLookOnPause", false);
            SetField(player, "_useGameSettingsMouseSensitivity", false);
            Assert.That(player.CursorControlEnabled, Is.True, "Exercise the serialized default, without disabling cursor control.");
            player.SetLookEnabled(initialLook);
            player.SetMovementEnabled(initialMovement);
            player.SetMoveInput(Vector2.zero);
            owner.GetComponent<Rigidbody>().useGravity = false;
            owner.SetActive(true);
            return player;
        }

        private static void ObserveUnlock(CursorLockController controller, UnityAction callback)
        {
            UnityEvent unlocked = new UnityEvent();
            unlocked.AddListener(callback);
            SetField(controller, "_onCursorUnlocked", unlocked);
        }

        private static void AssertVisibleCursor()
        {
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
        }

        private CursorLockController CreateController(bool disableLook = true, bool disableMovement = true)
        {
            GameObject owner = CreateObject();
            CursorLockController controller = owner.AddComponent<CursorLockController>();
            SetField(controller, "_controlMode", CursorLockController.ControlMode.ManualOnly);
            controller.DisableLookWhileCursorVisible = disableLook;
            controller.DisableMovementWhileCursorVisible = disableMovement;
            controller.RegisterPlayer(_player);
            owner.SetActive(true);
            _controllers.Add(controller);
            return controller;
        }

        private GameObject CreateObject()
        {
            GameObject owner = new GameObject("Audit20261023NeoToolsMoveCursorOwner");
            owner.SetActive(false);
            _objects.Add(owner);
            return owner;
        }

        private void AssertBlocked()
        {
            Assert.That(_player.LookEnabled, Is.False);
            Assert.That(_player.MovementEnabled, Is.False);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
