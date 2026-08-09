using Neo.Rpg;
using Neo.Rpg.Runtime;
using NUnit.Framework;

namespace Neo.Tests.Edit.RPG
{
    [TestFixture]
    public sealed class RpgCharacterResourceServiceTests
    {
        [Test]
        public void Mutations_ClampAndPublishActualRuntimeState()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition mana = CreateResource("Mana", 40f, 100f);
            int notifications = 0;
            float lastCurrent = -1f;
            service.ResourceChanged += (string id, RpgResourceRuntime resource) =>
            {
                Assert.That(id, Is.EqualTo("Mana"));
                notifications++;
                lastCurrent = resource.Current;
            };
            service.Build(new[] { mana });

            Assert.That(service.Spend("Mana", 15f), Is.True);
            Assert.That(service.Increase("Mana", 500f), Is.EqualTo(75f));
            Assert.That(service.Decrease("Mana", 150f), Is.EqualTo(100f));
            Assert.That(service.GetCurrent("Mana"), Is.Zero);
            Assert.That(service.GetPercent("Mana"), Is.Zero);
            Assert.That(lastCurrent, Is.Zero);
            Assert.That(notifications, Is.EqualTo(3));
        }

        [Test]
        public void SpendPauseAndPerSecondRegen_KeepExistingTimingContract()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition stamina = CreateResource("Stamina", 50f, 100f);
            stamina.regen = new RpgRegenDefinition
            {
                enabled = true,
                mode = RpgRegenMode.FlatPerSecond,
                value = 10f,
                pauseAfterSpend = true,
                pauseAfterSpendSeconds = 2f
            };
            service.Build(new[] { stamina });
            service.RefreshDerived((RpgResourceRuntime resource) => resource.BaseMax,
                (RpgResourceRuntime resource) => resource.Definition.regen.value, false);

            Assert.That(service.Spend("Stamina", 20f), Is.True);
            service.TickRegen(1f, false);
            service.TickRegen(1.1f, false);
            Assert.That(service.GetCurrent("Stamina"), Is.EqualTo(30f));

            service.TickRegen(0.5f, false);
            Assert.That(service.GetCurrent("Stamina"), Is.EqualTo(35f));
        }

        [Test]
        public void PerTickRegen_AccumulatesDeterministicallyAndStopsWhileDead()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition shield = CreateResource("Shield", 10f, 100f);
            shield.regen = new RpgRegenDefinition
            {
                enabled = true,
                mode = RpgRegenMode.PercentMaxPerTick,
                value = 5f,
                tickInterval = 0.25f,
                onlyWhenAlive = true
            };
            service.Build(new[] { shield });

            service.TickRegen(0.75f, true);
            Assert.That(service.GetCurrent("Shield"), Is.EqualTo(10f));

            service.TickRegen(0.75f, false);
            Assert.That(service.GetCurrent("Shield"), Is.EqualTo(25f));
        }

        [Test]
        public void PerTickRegen_ClampsInvalidRuntimeIntervalInsteadOfLoopingForever()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition rage = CreateResource("Rage", 0f, 100f);
            rage.regen = new RpgRegenDefinition
            {
                enabled = true,
                mode = RpgRegenMode.FlatPerTick,
                value = 2f,
                tickInterval = 0f
            };
            service.Build(new[] { rage });

            service.TickRegen(0.01f, false);

            Assert.That(service.GetCurrent("Rage"), Is.EqualTo(2f));
        }

        [Test]
        public void RefreshDerived_ResolvesMaxRegenAndInitialRestore()
        {
            RpgCharacterResourceService service = new();
            RpgResourceDefinition health = CreateResource("Hp", 25f, 100f);
            health.restoreOnAwake = true;
            health.restoreToFull = true;
            service.Build(new[] { health });

            service.RefreshDerived((RpgResourceRuntime resource) => 150f,
                (RpgResourceRuntime resource) => 12f, true);

            Assert.That(service.GetCurrent("Hp"), Is.EqualTo(150f));
            Assert.That(service.GetMax("Hp"), Is.EqualTo(150f));
            Assert.That(service.Resources["Hp"].ResolvedRegenPerSecond, Is.EqualTo(12f));
        }

        [Test]
        public void UnknownResource_UsesStableNoOpResults()
        {
            RpgCharacterResourceService service = new();
            service.Build(null);

            Assert.That(service.Contains("Missing"), Is.False);
            Assert.That(service.Spend("Missing", 1f), Is.False);
            Assert.That(service.Increase("Missing", 1f), Is.Zero);
            Assert.That(service.Decrease("Missing", 1f), Is.Zero);
            Assert.That(service.Restore("Missing"), Is.False);
            Assert.That(service.SetMax("Missing", 10f), Is.False);
            Assert.That(service.GetCurrentState("Missing"), Is.Null);
        }

        private static RpgResourceDefinition CreateResource(string id, float current, float max)
        {
            return new RpgResourceDefinition
            {
                id = new RpgStatId(id),
                startCurrent = current,
                startMax = max,
                restoreOnAwake = true,
                restoreToFull = false
            };
        }
    }
}
