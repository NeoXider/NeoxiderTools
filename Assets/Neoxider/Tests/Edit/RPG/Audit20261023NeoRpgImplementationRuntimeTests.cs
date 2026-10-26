using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Runtime;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023NeoRpgImplementationRuntimeTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RefreshDerived_ResolverRebuildCannotOverwriteReplacement(bool rebuildInRegen)
        {
            RpgCharacterResourceService service = CreateService();
            RpgResourceRuntime old = service.Resources["R"];
            float originalRegen = old.ResolvedRegenPerSecond;
            int notifications = 0;
            service.ResourceChanged += (string id, RpgResourceRuntime resource) => notifications++;
            service.RefreshDerived(
                (RpgResourceRuntime resource) =>
                {
                    if (!rebuildInRegen) { ReplaceWithC(service); }
                    return 50f;
                },
                (RpgResourceRuntime resource) => { ReplaceWithC(service); return 999f; }, false);

            Assert.That(service.GetCurrent("C"), Is.EqualTo(7f));
            Assert.That(service.GetMax("C"), Is.EqualTo(7f));
            Assert.That(old.ResolvedRegenPerSecond, Is.EqualTo(originalRegen));
            Assert.That(old.Max, Is.EqualTo(rebuildInRegen ? 50f : 1000000000f));
            Assert.That(notifications, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreAll_ReactiveOrEventRebuildStopsBeforeNextEntry(bool rebuildInReactive)
        {
            RpgCharacterResourceService service = CreateService();
            int notifications = 0;
            if (rebuildInReactive)
            {
                service.GetCurrentState("R").AddListener((float value) => ReplaceWithC(service));
            }
            service.ResourceChanged += (string id, RpgResourceRuntime resource) =>
            {
                notifications++;
                if (!rebuildInReactive) { ReplaceWithC(service); }
            };

            Assert.DoesNotThrow(() => service.RestoreAll());
            Assert.That(service.GetCurrent("C"), Is.EqualTo(7f));
            Assert.That(notifications, Is.EqualTo(rebuildInReactive ? 0 : 1));
        }

        [Test]
        public void SetMax_MaxObserverRebuildSuppressesOldCurrentPercentAndResourceEvents()
        {
            RpgCharacterResourceService service = CreateService();
            RpgResourceRuntime old = service.Resources["R"];
            old.SetCurrent(90f);
            int stale = 0;
            old.MaxState.AddListener((float value) => ReplaceWithC(service));
            old.CurrentState.AddListener((float value) => stale++);
            old.PercentState.AddListener((float value) => stale++);
            service.ResourceChanged += (string id, RpgResourceRuntime resource) => stale++;

            service.SetMax("R", 10f);

            Assert.That(service.GetCurrent("C"), Is.EqualTo(7f));
            Assert.That(old.Current, Is.EqualTo(90f));
            Assert.That(stale, Is.Zero);
        }

        [TestCase(16777216f, 0.125f)]
        [TestCase(float.MaxValue, 0.125f)]
        [TestCase(2048.25f, 0.375f)]
        public void Regen_HugeFiniteDeltaCapsDeliveryAndPreservesSeparateFraction(float delta, float phase)
        {
            RpgCharacterResourceService service = CreateService();
            RpgResourceRuntime resource = service.Resources["R"];
            resource.TickAccumulator = 0.125f;
            int delivered = 0;
            service.ResourceChanged += (string id, RpgResourceRuntime changed) => delivered++;

            service.TickRegen(delta, false);

            Assert.That(delivered, Is.EqualTo(1024));
            Assert.That(resource.Current, Is.EqualTo(2048f));
            Assert.That(resource.TickAccumulator, Is.EqualTo(phase));
            service.TickRegen(0.5f - phase, false);
            Assert.That(delivered, Is.EqualTo(1025));
            Assert.That(resource.TickAccumulator, Is.Zero);
        }

        [Test]
        public void Regen_CallbackReentryDoesNotReplayPendingTicks()
        {
            RpgCharacterResourceService service = CreateService();
            int delivered = 0;
            service.ResourceChanged += (string id, RpgResourceRuntime resource) =>
            {
                delivered++;
                service.TickRegen(float.MaxValue, false);
            };
            service.TickRegen(2f, false);
            Assert.That(delivered, Is.EqualTo(4));
            Assert.That(service.GetCurrent("R"), Is.EqualTo(8f));
        }

        [Test]
        public void Regen_RebuildFromFirstTickLeavesReplacementUntouched()
        {
            RpgCharacterResourceService service = CreateService();
            int delivered = 0;
            service.ResourceChanged += (string id, RpgResourceRuntime resource) =>
            {
                delivered++;
                ReplaceWithC(service);
            };
            Assert.DoesNotThrow(() => service.TickRegen(float.MaxValue, false));
            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(service.GetCurrent("C"), Is.EqualTo(7f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Regen_NonfiniteDeltaDoesNotPoisonNextFiniteTick(float delta)
        {
            RpgCharacterResourceService service = CreateService();
            service.Resources["R"].TickAccumulator = 0.25f;
            service.TickRegen(delta, false);
            Assert.That(service.Resources["R"].TickAccumulator, Is.EqualTo(0.25f));
            service.TickRegen(0.25f, false);
            Assert.That(service.GetCurrent("R"), Is.EqualTo(2f));
        }

        [Test]
        public void Regen_InvalidIntervalOrOverflowAmountRecoversAndPauseCrossingSkipsFrame()
        {
            RpgCharacterResourceService service = CreateService();
            RpgResourceRuntime resource = service.Resources["R"];
            resource.Definition.regen.tickInterval = float.PositiveInfinity;
            service.TickRegen(float.MaxValue, false);
            Assert.That(resource.Current, Is.Zero);
            resource.Definition.regen.tickInterval = 2f;
            resource.ResolvedRegenPerSecond = float.MaxValue;
            service.TickRegen(2f, false);
            Assert.That(resource.Current, Is.Zero);
            resource.ResolvedRegenPerSecond = 4f;
            resource.Definition.regen.tickInterval = 0.5f;
            resource.RegenPauseRemaining = 0.25f;
            service.TickRegen(2f, false);
            Assert.That(resource.Current, Is.Zero);
            service.TickRegen(0.5f, false);
            Assert.That(resource.Current, Is.EqualTo(2f));
        }

        [TestCase("disabled")]
        [TestCase("none")]
        [TestCase("dead")]
        [TestCase("full")]
        [TestCase("paused")]
        public void Regen_ResolvedBonusNeverBypassesDefinitionGates(string gate)
        {
            RpgCharacterResourceService service = CreateService();
            RpgResourceRuntime resource = service.Resources["R"];
            resource.ResolvedRegenPerSecond = 13.5f;
            resource.Definition.regen.mode = RpgRegenMode.FlatPerSecond;
            if (gate == "disabled") { resource.Definition.regen.enabled = false; }
            if (gate == "none") { resource.Definition.regen.mode = RpgRegenMode.None; }
            if (gate == "dead") { resource.Definition.regen.onlyWhenAlive = true; }
            if (gate == "full")
            {
                resource.Definition.regen.onlyWhenNotFull = true;
                resource.SetCurrent(resource.Max);
            }
            if (gate == "paused") { resource.RegenPauseRemaining = 2f; }
            float before = resource.Current;
            service.TickRegen(1f, gate == "dead");
            Assert.That(resource.Current, Is.EqualTo(before));
        }

        [TestCase(16777216f, 0.125f)]
        [TestCase(float.MaxValue, 0.125f)]
        [TestCase(2048.25f, 0.375f)]
        public void Status_HugeDeltaPreservesNegativeDelayedPhaseAndCapsPerEntry(float delta, float untilNext)
        {
            RpgEffectShelf shelf = new();
            shelf.ApplyStatus(CreateStatus());
            int delivered = 0;
            Action<StatusEffectDefinition, float> receive = (StatusEffectDefinition def, float amount) =>
            {
                delivered++;
                Assert.That(amount, Is.EqualTo(1f));
            };
            shelf.Tick(-0.125f, null, null, receive);
            shelf.Tick(delta, null, null, receive);
            Assert.That(delivered, Is.EqualTo(1024));
            shelf.Tick(untilNext - 0.125f, null, null, receive);
            Assert.That(delivered, Is.EqualTo(1024));
            shelf.Tick(0.125f, null, null, receive);
            Assert.That(delivered, Is.EqualTo(1025));
        }

        [Test]
        public void Status_NestedTickAndRemovalCannotReplayLocalPending()
        {
            RpgEffectShelf shelf = new();
            shelf.ApplyStatus(CreateStatus());
            int delivered = 0;
            shelf.Tick(float.MaxValue, null, null, (StatusEffectDefinition def, float amount) =>
            {
                delivered++;
                shelf.Tick(float.MaxValue, null, null, (StatusEffectDefinition nested, float value) => delivered++);
                shelf.ClearAllStatuses();
            });
            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(shelf.ActiveStatuses, Is.Empty);
        }

        [Test]
        public void Status_ExpiredEntryKeepsDamageBeforeExpiryAndFinallyReleasesThrowingTick()
        {
            RpgEffectShelf shelf = new();
            shelf.ApplyStatus(CreateStatus());
            Assert.Throws<InvalidOperationException>(() => shelf.Tick(0.75f, null, null,
                (StatusEffectDefinition def, float amount) => throw new InvalidOperationException("observer")));
            shelf.ActiveStatuses[0].ExpiresAtUtc = 0d;
            List<string> order = new();
            shelf.Tick(0.25f, null, (string id) => order.Add("expired"),
                (StatusEffectDefinition def, float amount) => order.Add("tick"));
            Assert.That(order, Is.EqualTo(new[] { "tick", "expired" }));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Status_InvalidDeltaStillExpiresWithoutCorruptingFinitePhase(float delta)
        {
            RpgEffectShelf shelf = new();
            shelf.ApplyStatus(CreateStatus());
            int delivered = 0;
            Action<StatusEffectDefinition, float> receive = (StatusEffectDefinition def, float amount) => delivered++;
            shelf.Tick(0.25f, null, null, receive);
            shelf.Tick(delta, null, null, receive);
            shelf.Tick(0.25f, null, null, receive);
            Assert.That(delivered, Is.EqualTo(1));
            shelf.ActiveStatuses[0].ExpiresAtUtc = 0d;
            int expired = 0;
            shelf.Tick(delta, null, (string id) => expired++, receive);
            Assert.That(expired, Is.EqualTo(1));
        }

        [Test]
        public void Status_InvalidIntervalOrAmountSkipsDeliveryAndRecovers()
        {
            RpgEffectShelf shelf = new();
            StatusEffectDefinition status = CreateStatus();
            shelf.ApplyStatus(status);
            int delivered = 0;
            Action<StatusEffectDefinition, float> receive = (StatusEffectDefinition def, float amount) => delivered++;
            Set(status, "_tickInterval", float.NaN);
            shelf.Tick(float.MaxValue, null, null, receive);
            Set(status, "_tickInterval", 2f);
            Set(status, "_tickDamagePerSecond", float.MaxValue);
            shelf.Tick(2f, null, null, receive);
            Assert.That(delivered, Is.Zero);
            Set(status, "_tickInterval", 0.5f);
            Set(status, "_tickDamagePerSecond", 2f);
            shelf.Tick(0.5f, null, null, receive);
            Assert.That(delivered, Is.EqualTo(1));
        }

        [TestCase(100000000f, 1000000000f, false)]
        [TestCase(0f, 10f, true)]
        public void Spend_ZeroStoredDebitRejectsBeforeSignalsOrClockMutation(float current, float max, bool atFloor)
        {
            RpgResourceDefinition definition = Pool("R", max, current);
            definition.canGoBelowZero = atFloor;
            definition.minValue = 0f;
            definition.regen.pauseAfterSpend = true;
            definition.regen.pauseAfterSpendSeconds = 2f;
            RpgCharacterResourceService service = new();
            service.Build(new[] { definition });
            RpgResourceRuntime resource = service.Resources["R"];
            resource.RegenPauseRemaining = 0.25f;
            resource.TickAccumulator = 0.125f;
            int currentSignals = 0;
            int maxSignals = 0;
            int percentSignals = 0;
            int changed = 0;
            resource.CurrentState.AddListener((float value) => currentSignals++);
            resource.MaxState.AddListener((float value) => maxSignals++);
            resource.PercentState.AddListener((float value) => percentSignals++);
            service.ResourceChanged += (string id, RpgResourceRuntime pool) => changed++;

            Assert.That(service.Spend("R", 1f), Is.False);
            Assert.That(service.Spend("R", 1f), Is.False, "repeating a rounded-away debit cannot buy for free");

            Assert.That(resource.Current, Is.EqualTo(current));
            Assert.That(resource.Max, Is.EqualTo(max));
            Assert.That(resource.CurrentState.CurrentValue, Is.EqualTo(current));
            Assert.That(resource.MaxState.CurrentValue, Is.EqualTo(max));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(current / max));
            Assert.That(currentSignals, Is.Zero);
            Assert.That(maxSignals, Is.Zero);
            Assert.That(percentSignals, Is.Zero);
            Assert.That(changed, Is.Zero);
            Assert.That(resource.RegenPauseRemaining, Is.EqualTo(0.25f));
            Assert.That(resource.TickAccumulator, Is.EqualTo(0.125f));
        }

        [TestCase(100000000f, 5f, 99999992f)]
        [TestCase(10f, 0.25f, 9.75f)]
        public void Spend_PositiveRoundedOrFractionalStoredDebitRemainsAccepted(float current, float cost, float expected)
        {
            RpgResourceDefinition definition = Pool("R", 1000000000f, current);
            definition.regen.pauseAfterSpend = true;
            definition.regen.pauseAfterSpendSeconds = 2f;
            RpgCharacterResourceService service = new();
            service.Build(new[] { definition });
            RpgResourceRuntime resource = service.Resources["R"];
            resource.RegenPauseRemaining = 0.25f;
            resource.TickAccumulator = 0.125f;
            int currentSignals = 0;
            int maxSignals = 0;
            int percentSignals = 0;
            int changed = 0;
            resource.CurrentState.AddListener((float value) => currentSignals++);
            resource.MaxState.AddListener((float value) => maxSignals++);
            resource.PercentState.AddListener((float value) => percentSignals++);
            service.ResourceChanged += (string id, RpgResourceRuntime pool) => changed++;

            Assert.That(service.Spend("R", cost), Is.True);

            Assert.That(resource.Current, Is.EqualTo(expected));
            Assert.That(resource.CurrentState.CurrentValue, Is.EqualTo(expected));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(expected / resource.Max));
            Assert.That(current - resource.Current, Is.EqualTo(current == 100000000f ? 8f : 0.25f));
            Assert.That(currentSignals, Is.EqualTo(1));
            Assert.That(maxSignals, Is.Zero);
            Assert.That(percentSignals, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(resource.RegenPauseRemaining, Is.EqualTo(2f));
            Assert.That(resource.TickAccumulator, Is.EqualTo(0.125f));
        }

        [TestCase("R", 0f, true)]
        [TestCase("Missing", 0f, true)]
        [TestCase("Missing", -1f, true)]
        [TestCase("Missing", 1f, false)]
        [TestCase("Missing", float.NaN, false)]
        [TestCase("Missing", float.NegativeInfinity, false)]
        [TestCase("R", float.NaN, false)]
        [TestCase("R", float.PositiveInfinity, false)]
        [TestCase("R", float.NegativeInfinity, false)]
        [TestCase(null, 1f, false)]
        public void Spend_PreservesFiniteZeroAndUnknownAdmissionPrecedence(string id, float amount, bool expected)
        {
            RpgCharacterResourceService service = CreateService();
            int changed = 0;
            service.ResourceChanged += (string resourceId, RpgResourceRuntime resource) => changed++;
            Assert.That(service.Spend(id, amount), Is.EqualTo(expected));
            Assert.That(service.GetCurrent("R"), Is.Zero);
            Assert.That(changed, Is.Zero);
        }

        private static RpgResourceDefinition Pool(string id, float max, float current)
        {
            return new RpgResourceDefinition
            {
                id = new RpgStatId(id), startMax = max, startCurrent = current, restoreOnAwake = false,
                regen = new RpgRegenDefinition
                {
                    enabled = true, mode = RpgRegenMode.FlatPerTick, value = 2f, tickInterval = 0.5f,
                    onlyWhenNotFull = false
                }
            };
        }

        private static RpgCharacterResourceService CreateService()
        {
            RpgCharacterResourceService service = new();
            service.Build(new[] { Pool("R", 1000000000f, 0f), Pool("Other", 20f, 20f) });
            return service;
        }

        private static void ReplaceWithC(RpgCharacterResourceService service)
        {
            service.Build(new[] { Pool("C", 7f, 7f) });
        }

        private StatusEffectDefinition CreateStatus()
        {
            StatusEffectDefinition status = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            _objects.Add(status);
            Set(status, "_id", "periodic");
            Set(status, "_duration", 1000000f);
            Set(status, "_tickDamagePerSecond", 2f);
            Set(status, "_tickInterval", 0.5f);
            return status;
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
