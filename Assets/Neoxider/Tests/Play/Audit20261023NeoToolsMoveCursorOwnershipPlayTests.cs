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
    public sealed class Audit20261023NeoToolsMoveCursorOwnershipPlayTests
    {
        private readonly List<GameObject> _roots = new List<GameObject>();
        private bool _priorGlobalManagement;
        private bool _priorVisible;
        private CursorLockMode _priorLock;

        [SetUp]
        public void SetUp()
        {
            _priorGlobalManagement = CursorLockController.GlobalCursorManagement;
            _priorVisible = Cursor.visible;
            _priorLock = Cursor.lockState;
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
                Cursor.lockState = _priorLock;
                Cursor.visible = _priorVisible;
                CursorLockController.GlobalCursorManagement = _priorGlobalManagement;
            }
        }

        [UnityTest]
        public IEnumerator NativeCursor_FocusedWriteReadCapabilityControl()
        {
            yield return AssertFocusedNativeCursorCapability("standalone native cursor control");
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
                            yield return AssertFocusedNativeCursorCapability(context);
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
                            Assert.That(Cursor.lockState,
                                Is.EqualTo(finalLookAutoLocks ? CursorLockMode.Locked : CursorLockMode.None), context);
                            Assert.That(Cursor.visible, Is.EqualTo(!finalLookAutoLocks), context);
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
                    yield return AssertFocusedNativeCursorCapability(
                        $"single owner: destroy={destruction != 0}, look={initialLook}, movement={initialMovement}");
                    GameObject root = CreateRoot();
                    PlayerController3DPhysics player = CreatePlayer(root, initialLook, initialMovement);
                    CursorLockController controller = CreateController(root, player, true, true);
                    int unlocks = 0;
                    ObserveUnlock(controller, () => unlocks++);
                    player.gameObject.SetActive(true);
                    controller.ShowCursor();
                    yield return null;
                    AssertVisibleCursor("single owner before cleanup");
                    Assert.That(player.LookEnabled, Is.False);
                    Assert.That(player.MovementEnabled, Is.False);

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
                    Assert.That(player.LookEnabled, Is.EqualTo(initialLook));
                    Assert.That(player.MovementEnabled, Is.EqualTo(initialMovement));
                    Assert.That(Cursor.lockState, Is.EqualTo(initialLook ? CursorLockMode.Locked : CursorLockMode.None));
                    Assert.That(Cursor.visible, Is.EqualTo(!initialLook));
                    Assert.That(unlocks, Is.EqualTo(1));
                    Object.Destroy(root);
                    yield return null;
                }
            }
        }

        private static IEnumerator AssertFocusedNativeCursorCapability(string context)
        {
            const string requirement = "MOVE_NATIVE_CURSOR_CAPABILITY: run in an interactive validation host with the Game view " +
                                       "focused; native Locked/hidden and None/visible must survive a frame before judging ownership. ";
            Assert.That(Application.isPlaying, Is.True, requirement + context);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
            double deadline = Time.realtimeSinceStartupAsDouble + 2d;
            while (!HasNativeCursorFocus() && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(HasNativeCursorFocus(), Is.True, requirement + context);
            CursorLockMode priorLock = Cursor.lockState;
            bool priorVisible = Cursor.visible;
            try
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked), requirement + context);
                Assert.That(Cursor.visible, Is.False, requirement + context);
                yield return null;
                Assert.That(HasNativeCursorFocus(), Is.True, requirement + context);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked), requirement + context);
                Assert.That(Cursor.visible, Is.False, requirement + context);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), requirement + context);
                Assert.That(Cursor.visible, Is.True, requirement + context);
                yield return null;
                Assert.That(HasNativeCursorFocus(), Is.True, requirement + context);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), requirement + context);
                Assert.That(Cursor.visible, Is.True, requirement + context);
            }
            finally
            {
                Cursor.lockState = priorLock;
                Cursor.visible = priorVisible;
            }
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

        private static void AssertVisibleCursor(string context)
        {
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), context);
            Assert.That(Cursor.visible, Is.True, context);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
