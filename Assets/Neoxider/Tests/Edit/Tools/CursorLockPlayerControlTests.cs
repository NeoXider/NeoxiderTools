using System.Collections.Generic;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Covers the single-cursor-owner contract: CursorLockController drives referenced
    ///     PlayerController3DPhysics instances, the player defers all cursor paths to the owner,
    ///     and the standalone player (no owner) keeps its own Escape/lock behavior.
    /// </summary>
    public sealed class CursorLockPlayerControlTests
    {
        private GameObject _controllerGo;
        private GameObject _playerGo;
        private CursorLockController _controller;
        private PlayerController3DPhysics _player;

        [SetUp]
        public void SetUp()
        {
            CursorLockController.GlobalCursorManagement = true;
            _controllerGo = new GameObject("CursorOwner");
            _controller = _controllerGo.AddComponent<CursorLockController>();
            _playerGo = new GameObject("Player");
            _player = _playerGo.AddComponent<PlayerController3DPhysics>();
        }

        [TearDown]
        public void TearDown()
        {
            CursorLockController.GlobalCursorManagement = true;
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_controllerGo);
        }

        private static List<PlayerController3DPhysics> GetPlayerList(CursorLockController controller)
        {
            FieldInfo field = typeof(CursorLockController).GetField(
                "_playerControllers", BindingFlags.Instance | BindingFlags.NonPublic);
            return (List<PlayerController3DPhysics>)field.GetValue(controller);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        [Test]
        public void ShowCursor_SuspendsLookOnReferencedPlayer()
        {
            _controller.RegisterPlayer(_player);

            _controller.ShowCursor();

            Assert.That(_player.LookEnabled, Is.False);
        }

        [Test]
        public void HideCursor_RestoresSuspendedLook()
        {
            _controller.RegisterPlayer(_player);
            _controller.ShowCursor();

            _controller.HideCursor();

            Assert.That(_player.LookEnabled, Is.True);
        }

        [Test]
        public void HideCursor_DoesNotForceEnable_ExternallyDisabledLook()
        {
            _controller.RegisterPlayer(_player);
            _player.SetLookEnabled(false);

            _controller.ShowCursor();
            _controller.HideCursor();

            Assert.That(_player.LookEnabled, Is.False);
        }

        [Test]
        public void ShowCursor_WithNullListEntry_DoesNotThrow()
        {
            List<PlayerController3DPhysics> list = GetPlayerList(_controller);
            list.Add(null);
            list.Add(_player);

            Assert.DoesNotThrow(() => _controller.ShowCursor());
            Assert.That(_player.LookEnabled, Is.False);
        }

        [Test]
        public void MovementToggle_SuspendsAndRestoresMovement()
        {
            _controller.DisableMovementWhileCursorVisible = true;
            _controller.RegisterPlayer(_player);

            _controller.ShowCursor();
            Assert.That(_player.MovementEnabled, Is.False);

            _controller.HideCursor();
            Assert.That(_player.MovementEnabled, Is.True);
        }

        [Test]
        public void RepeatedShowCursor_IsIdempotent()
        {
            _controller.RegisterPlayer(_player);

            _controller.ShowCursor();
            _controller.ShowCursor();
            _controller.HideCursor();

            Assert.That(_player.LookEnabled, Is.True);
        }

        [Test]
        public void RegisterPlayer_BindsControllerAsCursorOwner()
        {
            _controller.RegisterPlayer(_player);

            Assert.That(_player.ExternalCursorLockController, Is.SameAs(_controller));
            Assert.That(_player.HasExternalCursorControl(), Is.True);
        }

        [Test]
        public void RegisterPlayer_WhileCursorUnlocked_SuspendsImmediately()
        {
            _controller.ShowCursor();

            _controller.RegisterPlayer(_player);

            Assert.That(_player.LookEnabled, Is.False);
        }

        [Test]
        public void UnregisterPlayer_RestoresAndUnbinds()
        {
            _controller.RegisterPlayer(_player);
            _controller.ShowCursor();

            _controller.UnregisterPlayer(_player);

            Assert.That(_player.LookEnabled, Is.True);
            Assert.That(_player.ExternalCursorLockController, Is.Null);
        }

        [Test]
        public void StandalonePlayer_KeepsOwnEscapeAndLockOnStart()
        {
            Assert.That(_player.HasExternalCursorControl(), Is.False);
            Assert.That(_player.ShouldHandleEscape(), Is.True);
            Assert.That(_player.ShouldLockCursorOnStart(), Is.True);
        }

        [Test]
        public void PlayerWithExternalOwner_DefersEscapeAndLockOnStart()
        {
            _player.SetExternalCursorLockController(_controller);

            Assert.That(_player.ShouldHandleEscape(), Is.False);
            Assert.That(_player.ShouldLockCursorOnStart(), Is.False);
        }

        [Test]
        public void PlayerWithDisabledOwner_FallsBackToStandalone()
        {
            _player.SetExternalCursorLockController(_controller);
            _controller.enabled = false;

            Assert.That(_player.HasExternalCursorControl(), Is.False);
            Assert.That(_player.ShouldHandleEscape(), Is.True);
            Assert.That(_player.ShouldLockCursorOnStart(), Is.True);
        }

        [Test]
        public void SetCursorLocked_ForwardsToExternalOwner()
        {
            _player.SetExternalCursorLockController(_controller);

            _player.SetCursorLocked(true);

            Assert.That(_controller.HasCursorOwnership, Is.True);
        }

        [Test]
        public void CursorControlDisabled_SkipsEveryCursorPath()
        {
            _player.SetExternalCursorLockController(_controller);
            _player.CursorControlEnabled = false;

            Assert.That(_player.ShouldHandleEscape(), Is.False);
            Assert.That(_player.ShouldLockCursorOnStart(), Is.False);

            _player.SetCursorLocked(true);
            Assert.That(_controller.HasCursorOwnership, Is.False);
        }

        [Test]
        public void ManageCursorOff_SkipsAutomaticStart_ButManualCallsWork()
        {
            _controller.ManageCursor = false;
            _controller.RegisterPlayer(_player);

            InvokePrivate(_controller, "Start");
            Assert.That(_controller.HasCursorOwnership, Is.False);

            _controller.ShowCursor();
            Assert.That(_controller.HasCursorOwnership, Is.True);
            Assert.That(_player.LookEnabled, Is.False);
        }

        [Test]
        public void GlobalKillSwitch_BlocksCursorAndPlayerDriving_UntilRestored()
        {
            _controller.RegisterPlayer(_player);
            CursorLockController.GlobalCursorManagement = false;

            _controller.ShowCursor();
            Assert.That(_controller.HasCursorOwnership, Is.False);
            Assert.That(_player.LookEnabled, Is.True);

            CursorLockController.GlobalCursorManagement = true;
            _controller.ShowCursor();
            Assert.That(_player.LookEnabled, Is.False);
        }
    }
}
