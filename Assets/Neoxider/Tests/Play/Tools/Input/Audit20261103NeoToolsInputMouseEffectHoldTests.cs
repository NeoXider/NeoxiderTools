using System.Text.RegularExpressions;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     MouseEffect hold spawning against a manager whose Hold events are off (the default of an auto-created
    ///     manager): the requirement is visible through <see cref="MouseEffect.RequiresHoldEvents" />, a gated warning
    ///     names the fix once, and turning Enable Hold on makes the same effect spawn.
    /// </summary>
    public partial class Audit20261023NeoToolsInputBehaviorTests
    {
        [Test]
        public void MouseEffect_RequiresHoldEvents_FollowsHoldSpawningSettings()
        {
            MouseEffect effect = CreateEffect();
            Assert.That(effect.RequiresHoldEvents, Is.False, "Without a prefab nothing spawns.");

            effect.spawnPrefab = CreateObject("Audit Hold Requirement Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Press;
            effect.spawnDuringHold = true;
            Assert.That(effect.RequiresHoldEvents, Is.True, "Repeat spawning while held needs Hold events.");

            effect.spawnDuringHold = false;
            Assert.That(effect.RequiresHoldEvents, Is.False);
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Hold;
            Assert.That(effect.RequiresHoldEvents, Is.True);
        }

        [Test]
        public void MouseEffect_HoldSpawningWithHoldOff_WarnsOnceAndSpawnsAfterEnablingHold()
        {
            bool savedWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            NeoDiagnostics.Configure(warnings: true);
            try
            {
                _manager.enableHold = false;
                MouseEffect effect = CreateEffect();
                effect.enabled = false;
                effect.spawnPrefab = CreateObject("Audit Hold Warning Source");
                effect.spawnTrigger = MouseEffect.SpawnTrigger.Hold;
                effect.holdInterval = 0f;

                LogAssert.Expect(LogType.Warning, new Regex("needs MouseInputManager Enable Hold"));
                effect.enabled = true;
                effect.enabled = false;
                effect.enabled = true;
                LogAssert.NoUnexpectedReceived();

                _manager.ProcessMouseState(_screenPosition, true, true, false);
                _manager.ProcessMouseState(_screenPosition, false, true, false);
                Assert.That(effect.transform.childCount, Is.Zero, "With Enable Hold off no Hold event arrives.");

                _manager.enableHold = true;
                _manager.ProcessMouseState(_screenPosition, false, true, false);
                Assert.That(effect.transform.childCount, Is.EqualTo(1), "The same effect spawns once Hold is on.");
            }
            finally
            {
                NeoDiagnostics.Configure(warnings: savedWarnings);
            }
        }

        [Test]
        public void MouseEffect_HoldSpawningWithHoldOff_StaysQuietWhileWarningsAreOff()
        {
            bool savedWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            NeoDiagnostics.Configure(warnings: false);
            try
            {
                _manager.enableHold = false;
                MouseEffect effect = CreateEffect();
                effect.enabled = false;
                effect.spawnPrefab = CreateObject("Audit Hold Quiet Source");
                effect.enabled = true;
                Assert.That(effect.RequiresHoldEvents, Is.True, "Spawn During Hold is on by default.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                NeoDiagnostics.Configure(warnings: savedWarnings);
            }
        }
    }
}
