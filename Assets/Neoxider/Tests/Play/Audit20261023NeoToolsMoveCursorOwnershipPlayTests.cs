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
    /// <summary>
    ///     Cursor ownership through real disable/destroy lifecycles (OnDisable/OnDestroy ordering across frames). The
    ///     ownership cases run against an in-memory cursor installed in <see cref="MoveCursor" />, so every combination
    ///     is asserted on any host. One smoke case removes it and checks that the product reaches the native
    ///     <see cref="Cursor" />; that case alone is Ignored (MOVE_NATIVE_CURSOR_CAPABILITY) on batch, unfocused or
    ///     lock-refusing hosts, where Unity reads every lock back as None.
    /// </summary>
    public sealed class Audit20261023NeoToolsMoveCursorOwnershipPlayTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();
        private bool _priorGlobalManagement;
        private bool _priorVisible;
        private CursorLockMode _priorLock;
        private FakeCursor _cursor;
        private MoveCursor.IBackend _priorCursorBackend;

        [SetUp]
        public void SetUp()
        {
            _priorGlobalManagement = CursorLockController.GlobalCursorManagement;
            _priorVisible = Cursor.visible;
            _priorLock = Cursor.lockState;
            _cursor = new FakeCursor();
            _priorCursorBackend = MoveCursor.SetBackend(_cursor);
            CursorLockController.GlobalCursorManagement = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                foreach (GameObject root in _roots)
                {
                    if (root != null)
                    {
                        Object.Destroy(root);
                    }
                }

                yield return null;
            }
            finally
            {
                _roots.Clear();
                MoveCursor.SetBackend(_priorCursorBackend);
                _priorCursorBackend = null;
                Cursor.lockState = _priorLock;
                Cursor.visible = _priorVisible;
                CursorLockController.GlobalCursorManagement = _priorGlobalManagement;
            }
        }

        /// <summary>
        ///     Native smoke: without the in-memory cursor, a single owner's ShowCursor and its destroy-time release
        ///     must reach <see cref="Cursor" /> and survive a frame. Ignored where the host cannot hold a native lock.
        /// </summary>
        [UnityTest]
        public IEnumerator NativeCursor_SingleOwnerShowAndDestroyReachTheUnityCursor()
        {
            MoveCursor.SetBackend(null);
            const string context = "native single owner: destroy, look=True, movement=True";
            yield return RequireFocusedNativeCursorCapability(context);
            GameObject root = CreateRoot();
            PlayerController3DPhysics player = CreatePlayer(root, true, true);
            CursorLockController controller = CreateController(root, player, true, true);
            player.gameObject.SetActive(true);
            controller.ShowCursor();
            yield return null;
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), context);
            Assert.That(Cursor.visible, Is.True, context);

            Object.Destroy(controller.gameObject);
            yield return null;

            Assert.That(player.LookEnabled, Is.True, context);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked), context);
            Assert.That(Cursor.visible, Is.False, context);
        }

        [UnityTest]
        public IEnumerator MixedGates_NativeDisableAndDestroyPreserveSurvivingCursor()
        {
            for (int direction = 0; direction < 2; direction++)
            {
                for (int order = 0; order < 2; order++)
                {
                    for (int destruction = 0; destruction < 2; destruction++)
                    {
                        for (int baseline = 0; baseline < 4; baseline++)
                        {
                            bool lookOwnerIsTop = direction != 0;
                            bool closeTopFirst = order != 0;
                            bool destroy = destruction != 0;
                            bool initialLook = (baseline & 1) == 0;
                            bool initialMovement = (baseline & 2) == 0;
                            string context = $"lookTop={lookOwnerIsTop}, topFirst={closeTopFirst}, destroy={destroy}, look={initialLook}, movement={initialMovement}";
                            ResetCursor();
                            GameObject root = CreateRoot();
                            PlayerController3DPhysics player = CreatePlayer(root, initialLook, initialMovement);
                            CursorLockController lookOwner = CreateController(root, player, true, false);
                            CursorLockController movementOwner = CreateController(root, player, false, true);
                            Assert.That(player.ExternalCursorLockController, Is.SameAs(lookOwner), context);
                            int lookUnlocks = 0;
                            int movementUnlocks = 0;
                            ObserveUnlock(lookOwner, () => lookUnlocks++);
                            ObserveUnlock(movementOwner, () => movementUnlocks++);
                            CursorLockController top = lookOwnerIsTop ? lookOwner : movementOwner;
                            CursorLockController lower = lookOwnerIsTop ? movementOwner : lookOwner;
                            player.gameObject.SetActive(true);
                            lower.ShowCursor();
                            top.ShowCursor();
                            yield return null;
                            AssertVisibleCursor(context);
                            Assert.That(player.LookEnabled, Is.False, context);
                            Assert.That(player.MovementEnabled, Is.False, context);

                            CursorLockController first = closeTopFirst ? top : lower;
                            CursorLockController remaining = closeTopFirst ? lower : top;
                            bool lookClosesFirst = ReferenceEquals(first, lookOwner);
                            GameObject firstObject = first.gameObject;
                            if (destroy)
                            {
                                Object.Destroy(firstObject);
                            }
                            else
                            {
                                first.enabled = false;
                            }

                            yield return null;
                            Assert.That(remaining.HasCursorOwnership, Is.True, context);
                            AssertVisibleCursor(context);
                            Assert.That(player.LookEnabled, Is.EqualTo(lookClosesFirst && initialLook), context);
                            Assert.That(player.MovementEnabled, Is.EqualTo(!lookClosesFirst && initialMovement), context);
                            Assert.That(ReferenceEquals(player.ExternalCursorLockController, lookOwner), Is.True,
                                "Internal cleanup must not rebind the explicit external owner. " + context);
                            int expectedLookUnlocks = closeTopFirst && !lookClosesFirst ? 2 : 1;
                            int expectedMovementUnlocks = closeTopFirst && lookClosesFirst ? 2 : 1;
                            Assert.That(lookUnlocks, Is.EqualTo(expectedLookUnlocks), context);
                            Assert.That(movementUnlocks, Is.EqualTo(expectedMovementUnlocks), context);

                            if (destroy)
                            {
                                Object.Destroy(firstObject);
                            }
                            else
                            {
                                first.DisableController();
                                first.ReleaseControl();
                                first.enabled = false;
                            }

                            yield return null;
                            AssertVisibleCursor(context);
                            Assert.That(lookUnlocks, Is.EqualTo(expectedLookUnlocks), context);
                            Assert.That(movementUnlocks, Is.EqualTo(expectedMovementUnlocks), context);
                            GameObject remainingObject = remaining.gameObject;
                            if (destroy)
                            {
                                Object.Destroy(remainingObject);
                            }
                            else
                            {
                                remaining.enabled = false;
                                remaining.DisableController();
                                remaining.ReleaseControl();
                            }

                            yield return null;
                            Assert.That(player.LookEnabled, Is.EqualTo(initialLook), context);
                            Assert.That(player.MovementEnabled, Is.EqualTo(initialMovement), context);
                            bool finalLookAutoLocks = !lookClosesFirst && initialLook;
                            Assert.That(_cursor.LockState,
                                Is.EqualTo(finalLookAutoLocks ? CursorLockMode.Locked : CursorLockMode.None), context);
                            Assert.That(_cursor.Visible, Is.EqualTo(!finalLookAutoLocks), context);
                            Assert.That(lookUnlocks, Is.EqualTo(expectedLookUnlocks), context);
                            Assert.That(movementUnlocks, Is.EqualTo(expectedMovementUnlocks), context);
                            Object.Destroy(root);
                            yield return null;
                        }
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator SingleOwner_NativeCleanupRetainsStandaloneLookAutoLock()
        {
            for (int destruction = 0; destruction < 2; destruction++)
            {
                for (int baseline = 0; baseline < 4; baseline++)
                {
                    bool initialLook = (baseline & 1) == 0;
                    bool initialMovement = (baseline & 2) == 0;
                    string context =
                        $"single owner: destroy={destruction != 0}, look={initialLook}, movement={initialMovement}";
                    ResetCursor();
                    GameObject root = CreateRoot();
                    PlayerController3DPhysics player = CreatePlayer(root, initialLook, initialMovement);
                    CursorLockController controller = CreateController(root, player, true, true);
                    int unlocks = 0;
                    ObserveUnlock(controller, () => unlocks++);
                    player.gameObject.SetActive(true);
                    controller.ShowCursor();
                    yield return null;
                    AssertVisibleCursor(context);
                    Assert.That(player.LookEnabled, Is.False, context);
                    Assert.That(player.MovementEnabled, Is.False, context);

                    if (destruction != 0)
                    {
                        Object.Destroy(controller.gameObject);
                    }
                    else
                    {
                        controller.enabled = false;
                        controller.DisableController();
                        controller.ReleaseControl();
                    }

                    yield return null;
                    Assert.That(player.LookEnabled, Is.EqualTo(initialLook), context);
                    Assert.That(player.MovementEnabled, Is.EqualTo(initialMovement), context);
                    Assert.That(_cursor.LockState,
                        Is.EqualTo(initialLook ? CursorLockMode.Locked : CursorLockMode.None), context);
                    Assert.That(_cursor.Visible, Is.EqualTo(!initialLook), context);
                    Assert.That(unlocks, Is.EqualTo(1), context);
                    Object.Destroy(root);
                    yield return null;
                }
            }
        }

        /// <summary>
        ///     Proves that this host can hold a native cursor state across a frame before the native smoke case is
        ///     judged. A batch, unfocused or lock-refusing host always reads None, which would make a native Locked
        ///     expectation fail without saying anything about the product, so the case is reported as Ignored with the
        ///     observed reason instead.
        /// </summary>
        private static IEnumerator RequireFocusedNativeCursorCapability(string context)
        {
            Assert.That(Application.isPlaying, Is.True, "Native cursor cases run in Play Mode. " + context);
            if (Application.isBatchMode)
            {
                IgnoreMissingCapability("the player or Editor runs in batch mode", context);
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
            double deadline = Time.realtimeSinceStartupAsDouble + 2d;
            while (!HasNativeCursorFocus() && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            if (!HasNativeCursorFocus())
            {
                IgnoreMissingCapability(DescribeMissingFocus(), context);
            }

            CursorLockMode priorLock = Cursor.lockState;
            bool priorVisible = Cursor.visible;
            try
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                RequireCursorReadback(CursorLockMode.Locked, false, "after a direct Locked write", context);
                yield return null;
                RequireFocusAfterFrame(context);
                RequireCursorReadback(CursorLockMode.Locked, false, "one frame after a direct Locked write", context);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RequireCursorReadback(CursorLockMode.None, true, "after a direct None write", context);
                yield return null;
                RequireFocusAfterFrame(context);
                RequireCursorReadback(CursorLockMode.None, true, "one frame after a direct None write", context);
            }
            finally
            {
                Cursor.lockState = priorLock;
                Cursor.visible = priorVisible;
            }
        }

        private static void RequireFocusAfterFrame(string context)
        {
            if (!HasNativeCursorFocus())
            {
                IgnoreMissingCapability("focus was lost during the probe frame (" + DescribeMissingFocus() + ")",
                    context);
            }
        }

        private static void RequireCursorReadback(CursorLockMode expectedLock, bool expectedVisible, string when,
            string context)
        {
            CursorLockMode lockRead = Cursor.lockState;
            bool visibleRead = Cursor.visible;
            if (lockRead != expectedLock || visibleRead != expectedVisible)
            {
                IgnoreMissingCapability("the cursor read back " + lockRead + "/visible=" + visibleRead + " " + when +
                                        " (expected " + expectedLock + "/visible=" + expectedVisible + ")", context);
            }
        }

        private static void IgnoreMissingCapability(string reason, string context)
        {
            Assert.Ignore("MOVE_NATIVE_CURSOR_CAPABILITY unavailable: " + reason + ". The ownership cases still ran " +
                          "against the in-memory cursor; only the native write-through is unverified here. " + context);
        }

        private static string DescribeMissingFocus()
        {
            if (!Application.isFocused)
            {
                return "the application is not focused";
            }

#if UNITY_EDITOR
            UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
            return "the focused window is " + (focused == null ? "none" : focused.GetType().Name) +
                   ", not the Game view";
#else
            return "the player window has no input focus";
#endif
        }

        private static bool HasNativeCursorFocus()
        {
            if (!Application.isFocused)
            {
                return false;
            }

#if UNITY_EDITOR
            UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
            return focused != null && focused.GetType().Name == "GameView";
#else
            return true;
#endif
        }

        private GameObject CreateRoot()
        {
            GameObject root = new GameObject("Audit20261023NeoToolsMoveCursorPlay");
            _roots.Add(root);
            return root;
        }

        private static PlayerController3DPhysics CreatePlayer(GameObject root, bool initialLook, bool initialMovement)
        {
            GameObject owner = CreateChild(root, "DefaultCursorPlayer");
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            SetField(player, "_groundCheck", owner.transform);
            SetField(player, "_logSetupWarnings", false);
            SetField(player, "_disableLookOnPause", false);
            SetField(player, "_useGameSettingsMouseSensitivity", false);
            GameObject pivot = CreateChild(owner, "OwnedCameraPivot");
            SetField(player, "_cameraPivot", pivot.transform);
            Assert.That(player.CursorControlEnabled, Is.True, "Retain the player's actual default cursor-control option.");
            owner.GetComponent<Rigidbody>().useGravity = false;
            player.SetLookEnabled(initialLook);
            player.SetMovementEnabled(initialMovement);
            player.SetMoveInput(Vector2.zero);
            return player;
        }

        private static CursorLockController CreateController(GameObject root, PlayerController3DPhysics player,
            bool disableLook, bool disableMovement)
        {
            GameObject owner = CreateChild(root, "CursorOwner");
            CursorLockController controller = owner.AddComponent<CursorLockController>();
            SetField(controller, "_controlMode", CursorLockController.ControlMode.ManualOnly);
            controller.DisableLookWhileCursorVisible = disableLook;
            controller.DisableMovementWhileCursorVisible = disableMovement;
            controller.RegisterPlayer(player);
            owner.SetActive(true);
            return controller;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject owner = new GameObject(name);
            owner.SetActive(false);
            owner.transform.SetParent(parent.transform, false);
            return owner;
        }

        private static void ObserveUnlock(CursorLockController controller, UnityAction callback)
        {
            UnityEvent unlocked = new UnityEvent();
            unlocked.AddListener(callback);
            SetField(controller, "_onCursorUnlocked", unlocked);
        }

        private void AssertVisibleCursor(string context)
        {
            Assert.That(_cursor.LockState, Is.EqualTo(CursorLockMode.None), context);
            Assert.That(_cursor.Visible, Is.True, context);
        }

        private void ResetCursor()
        {
            _cursor.LockState = CursorLockMode.None;
            _cursor.Visible = true;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        /// <summary>In-memory cursor that holds exactly what was written, like a focused Game view.</summary>
        private sealed class FakeCursor : MoveCursor.IBackend
        {
            public CursorLockMode LockState { get; set; } = CursorLockMode.None;
            public bool Visible { get; set; } = true;
        }
    }
}
