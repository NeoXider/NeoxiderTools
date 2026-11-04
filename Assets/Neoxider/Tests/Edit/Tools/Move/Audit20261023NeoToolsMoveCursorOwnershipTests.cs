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
    /// <summary>
    ///     Cursor ownership between several <see cref="CursorLockController" /> instances and one
    ///     <see cref="PlayerController3DPhysics" />: gate baselines, release order, the surviving cursor request and
    ///     the player's standalone auto-lock after the last owner lets go. The Move module reads and writes the cursor
    ///     through <see cref="MoveCursor" />, and this fixture installs an in-memory cursor there, so every case runs
    ///     on any host, including batch mode and an unfocused Game view where Unity refuses a native lock. The native
    ///     write-through is covered by one PlayMode smoke case that may be Ignored on such hosts.
    /// </summary>
    public sealed class Audit20261023NeoToolsMoveCursorOwnershipTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<CursorLockController> _controllers = new List<CursorLockController>();
        private bool _priorGlobalManagement;
        private PlayerController3DPhysics _player;
        private bool _enteredPlayMode;
        private FakeCursor _cursor;
        private MoveCursor.IBackend _priorCursorBackend;
        private bool _installedCursor;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _enteredPlayMode = false;
            Assert.That(Application.isPlaying, Is.False, "This Edit fixture must own its Play transition.");
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            Assert.That(Application.isPlaying, Is.True);
            _priorGlobalManagement = CursorLockController.GlobalCursorManagement;
            _cursor = new FakeCursor();
            _priorCursorBackend = MoveCursor.SetBackend(_cursor);
            _installedCursor = true;
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
                if (_installedCursor)
                {
                    MoveCursor.SetBackend(_priorCursorBackend);
                    _installedCursor = false;
                }

                _priorCursorBackend = null;
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
            // WHY: an exposed lower controller keeps its existing activation notification; internal repair is silent.
            Assert.That(lookUnlocks, Is.EqualTo(closeTopFirst && remaining == lookOwner ? 2 : 1));
            Assert.That(movementUnlocks, Is.EqualTo(closeTopFirst && remaining == movementOwner ? 2 : 1));

            remaining.DisableController();
            remaining.DisableController();
            remaining.ReleaseControl();

            Assert.That(_player.LookEnabled, Is.EqualTo(initialLook));
            Assert.That(_player.MovementEnabled, Is.EqualTo(initialMovement));
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(lookOwner));
            bool finalLookAutoLocks = remaining == lookOwner && initialLook;
            Assert.That(_cursor.LockState, Is.EqualTo(finalLookAutoLocks ? CursorLockMode.Locked : CursorLockMode.None));
            Assert.That(_cursor.Visible, Is.EqualTo(!finalLookAutoLocks));
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
            Assert.That(_cursor.LockState, Is.EqualTo(initialLook ? CursorLockMode.Locked : CursorLockMode.None));
            Assert.That(_cursor.Visible, Is.EqualTo(!initialLook));
            Assert.That(unlocks, Is.EqualTo(1));
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(controller));
        }

        /// <remarks>
        ///     Destroy and SetActive(false) keep the controller's <c>enabled</c> flag true while it releases its
        ///     players, so the player must stop deferring to it during that cleanup to auto-lock as it does after
        ///     <see cref="CursorLockController.DisableController" />.
        /// </remarks>
        [TestCase(OwnerCleanup.DisableController, true)]
        [TestCase(OwnerCleanup.DisableComponent, true)]
        [TestCase(OwnerCleanup.DeactivateObject, true)]
        [TestCase(OwnerCleanup.DestroyObject, true)]
        [TestCase(OwnerCleanup.DisableController, false)]
        [TestCase(OwnerCleanup.DisableComponent, false)]
        [TestCase(OwnerCleanup.DeactivateObject, false)]
        [TestCase(OwnerCleanup.DestroyObject, false)]
        public void DefaultPlayer_EveryOwnerCleanupPathRestoresTheStandaloneLookAutoLock(OwnerCleanup cleanup,
            bool initialLook)
        {
            _player = CreateDefaultCursorPlayer(initialLook, true);
            CursorLockController controller = CreateController();
            controller.ShowCursor();
            AssertBlocked();
            AssertVisibleCursor();

            switch (cleanup)
            {
                case OwnerCleanup.DisableController:
                    controller.DisableController();
                    break;
                case OwnerCleanup.DisableComponent:
                    controller.enabled = false;
                    break;
                case OwnerCleanup.DeactivateObject:
                    controller.gameObject.SetActive(false);
                    break;
                case OwnerCleanup.DestroyObject:
                    Object.DestroyImmediate(controller.gameObject);
                    break;
            }

            Assert.That(_player.LookEnabled, Is.EqualTo(initialLook), cleanup.ToString());
            Assert.That(_player.MovementEnabled, Is.True, cleanup.ToString());
            Assert.That(_cursor.LockState, Is.EqualTo(initialLook ? CursorLockMode.Locked : CursorLockMode.None),
                cleanup + ": with no surviving owner the restored look locks the cursor like a standalone player.");
            Assert.That(_cursor.Visible, Is.EqualTo(!initialLook), cleanup.ToString());
        }

        /// <remarks>
        ///     An owner on the player root leaves together with its player (despawn, respawn, deactivation). The look
        ///     it hands back during that cleanup must not auto-lock the cursor for a player that is leaving play too.
        /// </remarks>
        [TestCase(OwnerCleanup.DeactivateObject)]
        [TestCase(OwnerCleanup.DestroyObject)]
        public void DefaultPlayer_OwnerOnThePlayerRootLeavingWithThePlayer_DoesNotRelockTheCursor(OwnerCleanup cleanup)
        {
            _player = CreateDefaultPlayerWithRootOwner(out CursorLockController controller);
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(controller));
            controller.ShowCursor();
            AssertBlocked();
            AssertVisibleCursor();
            GameObject root = _player.gameObject;

            if (cleanup == OwnerCleanup.DestroyObject)
            {
                Object.DestroyImmediate(root);
            }
            else
            {
                root.SetActive(false);
                Assert.That(_player.LookEnabled, Is.True, "The owner still hands the look gate back.");
                Assert.That(_player.MovementEnabled, Is.True, "The owner still hands the movement gate back.");
            }

            // WHY: the player recognises that it leaves play through its own activeInHierarchy, which Unity reports as
            // false inside the OnDisable that SetActive(false) or a GameObject destroy drives.
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.None),
                cleanup + ": a player leaving play with its owner must not lock the cursor (the player's GameObject " +
                "still read as active in hierarchy while the owner cleaned up).");
            Assert.That(_cursor.Visible, Is.True, cleanup.ToString());
        }

        /// <remarks>
        ///     A deactivated owner runs no Update, so it handles no Escape. After its cleanup the player treats it
        ///     like an owner with <c>enabled = false</c>: it keeps the binding, runs its own cursor paths and writes
        ///     the cursor itself instead of pushing the inactive owner onto the cursor stack, and it defers again once
        ///     the owner is active.
        /// </remarks>
        [Test]
        public void DeactivatedOwner_IsTreatedAsAbsentUntilItIsActiveAgain()
        {
            _player = CreateDefaultCursorPlayer(true, true);
            CursorLockController controller = CreateController();
            controller.ShowCursor();
            Assert.That(_player.HasExternalCursorControl(), Is.True);
            Assert.That(_player.ShouldHandleEscape(), Is.False);

            controller.gameObject.SetActive(false);

            Assert.That(controller.IsReleasingOwnership, Is.False, "The cleanup flag only spans OnDisable.");
            Assert.That(_player.ExternalCursorLockController, Is.SameAs(controller), "The binding is kept.");
            Assert.That(_player.HasExternalCursorControl(), Is.False,
                "Cleanup and the time after it agree: an inactive owner does not own the cursor.");
            Assert.That(_player.ShouldHandleEscape(), Is.True,
                "The player must handle Escape while its owner cannot, or a locked cursor has no way out.");
            Assert.That(_player.ShouldLockCursorOnStart(), Is.True);
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(_cursor.Visible, Is.False);

            _player.SetCursorLocked(false);

            Assert.That(controller.HasCursorOwnership, Is.False,
                "A request must not be forwarded to the inactive owner and push it onto the cursor stack.");
            AssertVisibleCursor();

            controller.gameObject.SetActive(true);

            Assert.That(_player.HasExternalCursorControl(), Is.True, "A reactivated owner owns the cursor again.");
            Assert.That(_player.ShouldHandleEscape(), Is.False);
            _player.SetCursorLocked(true);
            Assert.That(controller.HasCursorOwnership, Is.True, "Requests are forwarded to the active owner again.");
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(_cursor.Visible, Is.False);
        }

        [Test]
        public void DefaultPlayer_StandaloneSetLookEnabledStillLocksAndHides()
        {
            _player = CreateDefaultCursorPlayer(false, true);
            Assert.That(_player.ExternalCursorLockController, Is.Null);
            _cursor.LockState = CursorLockMode.None;
            _cursor.Visible = true;

            _player.SetLookEnabled(true);

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(_cursor.Visible, Is.False);
        }

        [Test]
        public void MoveCursor_SwapsBackendsAndFallsBackToTheNativeCursor()
        {
            MoveCursor.IBackend installed = MoveCursor.SetBackend(null);
            try
            {
                Assert.That(installed, Is.SameAs(_cursor), "SetBackend returns the backend it replaces.");
                Assert.That(MoveCursor.HasBackend, Is.False);
                Assert.That(MoveCursor.LockState, Is.EqualTo(Cursor.lockState));
                Assert.That(MoveCursor.Visible, Is.EqualTo(Cursor.visible));
            }
            finally
            {
                MoveCursor.SetBackend(installed);
            }

            Assert.That(MoveCursor.HasBackend, Is.True);
            MoveCursor.LockState = CursorLockMode.Confined;
            MoveCursor.Visible = false;
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.Confined));
            Assert.That(_cursor.Visible, Is.False);
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

        private PlayerController3DPhysics CreateDefaultPlayerWithRootOwner(out CursorLockController controller)
        {
            GameObject owner = CreateObject();
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            SetField(player, "_groundCheck", owner.transform);
            SetField(player, "_cameraPivot", owner.transform);
            SetField(player, "_logSetupWarnings", false);
            SetField(player, "_disableLookOnPause", false);
            SetField(player, "_useGameSettingsMouseSensitivity", false);
            Assert.That(player.CursorControlEnabled, Is.True,
                "Exercise the serialized default, without disabling cursor control.");
            player.SetMoveInput(Vector2.zero);
            owner.GetComponent<Rigidbody>().useGravity = false;
            controller = owner.AddComponent<CursorLockController>();
            SetField(controller, "_controlMode", CursorLockController.ControlMode.ManualOnly);
            controller.DisableLookWhileCursorVisible = true;
            controller.DisableMovementWhileCursorVisible = true;
            controller.RegisterPlayer(player);
            _controllers.Add(controller);
            owner.SetActive(true);
            return player;
        }

        private static void ObserveUnlock(CursorLockController controller, UnityAction callback)
        {
            UnityEvent unlocked = new UnityEvent();
            unlocked.AddListener(callback);
            SetField(controller, "_onCursorUnlocked", unlocked);
        }

        private void AssertVisibleCursor()
        {
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(_cursor.Visible, Is.True);
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

        /// <summary>How a cursor owner is taken out of play.</summary>
        public enum OwnerCleanup
        {
            DisableController,
            DisableComponent,
            DeactivateObject,
            DestroyObject
        }

        /// <summary>In-memory cursor that holds exactly what was written, like a focused Game view.</summary>
        private sealed class FakeCursor : MoveCursor.IBackend
        {
            public CursorLockMode LockState { get; set; } = CursorLockMode.None;
            public bool Visible { get; set; } = true;
        }
    }
}
