using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit.RPG
{


    public sealed class Audit20261023NeoRpgImplementationCharacterTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null) { Object.DestroyImmediate(_objects[i]); }
            }
            _objects.Clear();
        }

        [TestCase(RpgLevelGrowthMode.None, true)]
        [TestCase(RpgLevelGrowthMode.ManualUpgradePoints, true)]
        [TestCase(RpgLevelGrowthMode.AllStatsEveryLevel, false)]
        [TestCase(RpgLevelGrowthMode.Hybrid, false)]
        public void DeferredGrowth_RefreshBuffUpgradeAndProfileDoNotApplyNewLevel(
            RpgLevelGrowthMode mode, bool automatic)
        {
            RpgCharacter character = CreateCharacter(mode, automatic);
            character.SetLevel(3);
            character.SetStatBase("S", 12f);
            Assert.That(character.GetStat("S"), Is.EqualTo(12f));
            BuffDefinition unrelated = Buff(Modifier(BuffStatType.AddResourceMaxFlat, "Mana", 5f));
            character.ApplyBuff(unrelated);
            character.AddUpgradePoints(1);
            Assert.That(character.UpgradeStat("S"), Is.True);
            Assert.That(character.GetStat("S"), Is.EqualTo(13f));
            character.RemoveBuff(unrelated.Id);
            character.ApplyLevelGrowth();
            Assert.That(character.GetStat("S"), Is.EqualTo(17f));
            character.SetLevel(4);
            character.RefreshAllDerived(false);
            Assert.That(character.GetStat("S"), Is.EqualTo(17f));
            character.ApplyProfile(character.CaptureProfile());
            Assert.That(character.GetStat("S"), Is.EqualTo(13f), "v2 resets the deferred anchor on restore");
            character.ApplyLevelGrowth();
            Assert.That(character.GetStat("S"), Is.EqualTo(19f));
        }

        [TestCase(RpgLevelGrowthMode.AllStatsEveryLevel)]
        [TestCase(RpgLevelGrowthMode.Hybrid)]
        public void AutomaticGrowth_UpDownAndProfileUseCurrentLevel(RpgLevelGrowthMode mode)
        {
            RpgCharacter character = CreateCharacter(mode, true);
            character.SetLevel(4);
            Assert.That(character.GetStat("S"), Is.EqualTo(16f));
            character.SetLevel(2);
            Assert.That(character.GetStat("S"), Is.EqualTo(12f));
            RpgCharacterProfileData profile = character.CaptureProfile();
            profile.Level = 3;
            character.ApplyProfile(profile);
            Assert.That(character.GetStat("S"), Is.EqualTo(14f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StatRefresh_RebuildFromReactiveOrUnityEventAbandonsOldEnumerator(bool unityEvent)
        {
            RpgCharacter character = CreateCharacter();
            RpgCharacterTemplate replacement = Replacement();
            int staleEvents = 0;
            character.OnStatChangedEvent.AddListener((string id, float value) =>
            {
                if (id != "S" || value != 20f) { return; }
                if (unityEvent) { character.ApplyTemplate(replacement); }
                else { staleEvents++; }
            });
            if (!unityEvent)
            {
                character.GetStatState("S").AddListener((float value) => character.ApplyTemplate(replacement));
            }
            Assert.DoesNotThrow(() => character.SetStatBase("S", 20f));
            Assert.That(character.GetStat("C"), Is.EqualTo(7f));
            Assert.That(character.GetResource("C"), Is.EqualTo(7f));
            Assert.That(staleEvents, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HpMutation_ReactiveRebuildSuppressesOldDamageOrHealEvent(bool heal)
        {
            RpgCharacter character = CreateCharacter();
            RpgCharacterTemplate replacement = Replacement();
            int stale = 0;
            character.OnDamagedEvent.AddListener((float value) => stale++);
            character.OnHealedEvent.AddListener((float value) => stale++);
            character.HpState.AddListener((float value) => character.ApplyTemplate(replacement));
            if (heal) { character.Heal(5f); }
            else { character.Damage(5f); }
            Assert.That(stale, Is.Zero);
            Assert.That(character.GetResource("C"), Is.EqualTo(7f));
        }

        [Test]
        public void Initialization_AutoLoadPublishesOneCompleteSnapshotAfterRuntimeRebuilt()
        {
            string key = "Audit20261023NeoRpg_" + Guid.NewGuid().ToString("N");
            RpgCharacterProfileData saved = new() { Xp = 17f };
            saved.Resources.Add(new RpgResourceSaveEntry { Id = RpgResourceId.Hp, Max = 160f, Current = 130f });
            SaveProvider.SetString(key, new RpgCharacterProfileService().Serialize(saved));
            try
            {
                GameObject actor = Track(new GameObject("AuditRpgInitialSnapshot"));
                actor.SetActive(false);
                Audit20261023NeoRpgSnapshotProbe probe = actor.AddComponent<Audit20261023NeoRpgSnapshotProbe>();
                Assert.That(probe != null, Is.True, "snapshot probe must be a live runtime-compatible component");
                RpgCharacter character = actor.AddComponent<RpgCharacter>();
                Assert.That(character != null, Is.True);
                character.Template = Template();
                character.isNetworked = true;
                Set(character, "_saveKey", key);
                Set(character, "_loadOnAwake", true);
                List<string> order = new();
                character.RuntimeRebuilt += () =>
                {
                    order.Add("rebuilt");
                    Assert.That(character.MaxHp, Is.EqualTo(100f));
                };
                character.OnProfileLoadedEvent.AddListener(() => order.Add("loaded"));
                probe.Received = (RpgCharacterProfileData profile) => order.Add("snapshot");

                Assert.That(actor.activeInHierarchy, Is.False);
                Assert.That(order, Is.Empty, "all observers must precede initialization");
                Assert.That(probe.Snapshots, Is.Empty);

                // Public initialization contract: discover the adapter and initialize before zero-damage admission.
                // Keeping the graph inactive avoids treating Edit Mode activation as native Awake evidence.
                Assert.That(character.Damage(0f), Is.Zero);

                Assert.That(order, Is.EqualTo(new[] { "rebuilt", "loaded", "snapshot" }));
                Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
                Assert.That(probe.Snapshots[0].Xp, Is.EqualTo(17f));
                Assert.That(probe.Snapshots[0].Resources[0].Current, Is.EqualTo(130f));
                Assert.That(probe.Snapshots[0].Resources[0].Max, Is.EqualTo(160f));
                Assert.That(probe.Snapshots[0].Stats.Count, Is.EqualTo(2));
            }
            finally { SaveProvider.DeleteKey(key); }
        }

        [Test]
        public void Rebuild_PublicationGuardStartsBeforeClearingOldResources()
        {
            RpgCharacter character = CreateCharacter();
            Audit20261023NeoRpgSnapshotProbe probe = character.gameObject.AddComponent<Audit20261023NeoRpgSnapshotProbe>();
            Set(character, "_networkAdapter", probe);
            character.ApplyTemplate(Replacement());
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
            Assert.That(probe.Snapshots[0].Resources[0].Id, Is.EqualTo("C"));
            Assert.That(probe.Snapshots[0].Resources[0].Current, Is.EqualTo(7f));
        }

        [Test]
        public void ProfileV2_LastSavedEffectiveMaxPrecedesCurrentAndPercentRegen()
        {
            RpgCharacter character = CreateCharacter();
            character.Template.resources[0].regen = new RpgRegenDefinition
                { enabled = true, mode = RpgRegenMode.PercentMaxPerSecond, value = 10f };
            character.RebuildRuntime();
            int deaths = 0;
            List<float> currentValues = new();
            character.OnDeathEvent.AddListener(() => deaths++);
            character.HpState.AddListener((float value) => currentValues.Add(value));
            RpgCharacterProfileData profile = new();
            profile.Resources.Add(new RpgResourceSaveEntry { Id = RpgResourceId.Hp, Max = 5f, Current = 0f });
            profile.Resources.Add(new RpgResourceSaveEntry { Id = RpgResourceId.Hp, Max = 160f, Current = 130f });
            profile.Resources.Add(new RpgResourceSaveEntry { Id = "Unknown", Max = 999f, Current = 999f });

            character.ApplyProfile(profile);
            character.ApplyProfile(profile);

            Assert.That(character.CurrentHp, Is.EqualTo(130f));
            Assert.That(character.MaxHp, Is.EqualTo(160f));
            Assert.That(character.HpPercentValue, Is.EqualTo(0.8125f));
            Assert.That(character.Resources[RpgResourceId.Hp].ResolvedRegenPerSecond, Is.EqualTo(16f));
            Assert.That(character.Resources[RpgResourceId.Hp].BaseMax, Is.EqualTo(100f));
            Assert.That(character.GetResourceMax("Mana"), Is.EqualTo(100f));
            Assert.That(character.Resources.ContainsKey("Unknown"), Is.False);
            Assert.That(currentValues, Is.EqualTo(new[] { 130f }));
            Assert.That(deaths, Is.Zero);
            character.RefreshAllDerived(false);
            Assert.That(character.MaxHp, Is.EqualTo(100f), "later ordinary refresh intentionally uses BaseMax");
            Assert.That(character.CurrentHp, Is.EqualTo(100f));
        }

        [Test]
        public void ProfileV2_RestoredBuffMaxDoesNotCompoundAcrossRoundTrips()
        {
            RpgCharacter character = CreateCharacter();
            BuffDefinition buff = Buff(Modifier(BuffStatType.AddResourceMaxFlat, RpgResourceId.Hp, 20f));
            character.ApplyBuff(buff);
            RpgCharacterProfileData profile = character.CaptureProfile();
            for (int i = 0; i < 3; i++)
            {
                character.ApplyProfile(profile);
                character.RefreshAllDerived(false);
                Assert.That(character.MaxHp, Is.EqualTo(120f));
                Assert.That(character.Resources[RpgResourceId.Hp].BaseMax, Is.EqualTo(100f));
                profile = character.CaptureProfile();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentFlat_NewAndPositiveStackDeltaSettleBeforeAppliedEvent(bool inline)
        {
            RpgCharacter character = CreateCharacter();
            Func<bool> apply = Install(character, inline, true, 2,
                Modifier(BuffStatType.AddResourceMaxFlat, RpgResourceId.Hp, 50f),
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, 40f));
            List<float> settled = new();
            character.OnBuffAppliedEvent.AddListener((string id) => settled.Add(character.CurrentHp));
            Assert.That(apply(), Is.True);
            Assert.That(character.MaxHp, Is.EqualTo(150f));
            Assert.That(apply(), Is.True);
            Assert.That(character.MaxHp, Is.EqualTo(200f));
            Assert.That(apply(), Is.True);
            Assert.That(settled, Is.EqualTo(new[] { 130f, 170f, 170f }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentFlat_NonstackRefreshAndRestoredEffectsNeverReplayGrant(bool inline)
        {
            RpgCharacter character = CreateCharacter();
            Func<bool> apply = Install(character, inline, false, 1,
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, 10f));
            character.Spend(RpgResourceId.Hp, 70f);
            apply();
            Assert.That(character.CurrentHp, Is.EqualTo(30f));
            RpgCharacterProfileData profile = character.CaptureProfile();
            character.Spend(RpgResourceId.Hp, 5f);
            apply();
            Assert.That(character.CurrentHp, Is.EqualTo(25f));
            character.ApplyProfile(profile);
            character.ApplyProfile(character.CaptureProfile());
            Assert.That(character.CurrentHp, Is.EqualTo(30f));
            character.RemoveBuff("grant");
            Assert.That(character.CurrentHp, Is.EqualTo(30f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentFlat_SignedRawDeltaSkipsInvalidOrUnknownTargets(bool inline)
        {
            RpgCharacter character = CreateCharacter();
            int damageEvents = 0;
            character.OnDamagedEvent.AddListener((float value) => damageEvents++);
            Func<bool> apply = Install(character, inline, false, 1,
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, -20f),
                Modifier(BuffStatType.AddResourceCurrentFlat, "Missing", 10f),
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, float.NaN));
            apply();
            Assert.That(character.CurrentHp, Is.EqualTo(70f));
            Assert.That(character.Resources.Count, Is.EqualTo(2));
            Assert.That(damageEvents, Is.Zero);
        }

        [Test]
        public void CurrentFlat_ReentryIsRejectedAndThrowAfterCommitCannotReplayGrant()
        {
            RpgCharacter character = CreateCharacter();
            Func<bool> apply = Install(character, false, false, 1,
                Modifier(BuffStatType.AddResourceMaxFlat, RpgResourceId.Hp, 50f),
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, 40f));
            int rejected = 0;
            character.OnResourceChangedEvent.AddListener((string id, float value) =>
            {
                if (!apply()) { rejected++; }
            });
            UnityAction<string> throwing = (string id) => throw new InvalidOperationException("settled observer");
            character.OnBuffAppliedEvent.AddListener(throwing);
            Assert.Throws<InvalidOperationException>(() => apply());
            Assert.That(character.CurrentHp, Is.EqualTo(130f));
            Assert.That(rejected, Is.GreaterThan(0));
            character.OnBuffAppliedEvent.RemoveListener(throwing);
            Assert.That(apply(), Is.True, "finally must release the application guard");
            Assert.That(character.CurrentHp, Is.EqualTo(130f));
        }

        [Test]
        public void CurrentFlat_RebuildBeforeDeliveryAbandonsOldGrantAndAppliedEvent()
        {
            RpgCharacter character = CreateCharacter();
            Func<bool> apply = Install(character, false, false, 1,
                Modifier(BuffStatType.AddResourceCurrentFlat, RpgResourceId.Hp, 40f));
            RpgCharacterTemplate replacement = Replacement();
            int appliedEvents = 0;
            character.OnBuffAppliedEvent.AddListener((string id) => appliedEvents++);
            character.OnResourceChangedEvent.AddListener((string id, float value) =>
            {
                if (id == RpgResourceId.Hp) { character.ApplyTemplate(replacement); }
            });
            Assert.That(apply(), Is.True);
            Assert.That(character.GetResource("C"), Is.EqualTo(7f));
            Assert.That(appliedEvents, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyHpRegen_IgnoresTargetIdStacksAndComposesBeforeUniversalPercent(bool inline)
        {
            RpgCharacter character = CreateCharacter();
            character.Template.resources[0].regen = new RpgRegenDefinition
                { enabled = true, mode = RpgRegenMode.FlatPerSecond, value = 2f };
            character.RebuildRuntime();
            Func<bool> apply = Install(character, inline, true, 2,
                Modifier(BuffStatType.HpRegenPerSecond, "Mana", 3f));
            apply();
            apply();
            character.ApplyBuff(Buff(Modifier(BuffStatType.RegenFlat, RpgResourceId.Hp, 1f),
                Modifier(BuffStatType.RegenPercent, RpgResourceId.Hp, 50f)));
            Assert.That(character.Resources[RpgResourceId.Hp].ResolvedRegenPerSecond, Is.EqualTo(13.5f));
            Assert.That(character.Resources["Mana"].ResolvedRegenPerSecond, Is.Zero);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void UpgradeStat_DebitObserverReplacementOwnsItsInvestmentsAndProfile(bool rebuildOnly, bool omitStat)
        {
            RpgCharacter character = CreateCharacter();
            character.Template.progression.upgradeRules[0].increasePerPoint = 2f;
            character.AddUpgradePoints(1);
            RpgCharacterTemplate replacement = Template();
            replacement.stats = new[] { new RpgStatDefinition { id = new RpgStatId(omitStat ? "C" : "S"), baseValue = 7f } };
            replacement.progression = character.Template.progression;
            bool replaced = false;
            int rebuilt = 0;
            List<float> statEvents = new();
            RpgCharacterProfileData callbackProfile = null;
            character.RuntimeRebuilt += () => rebuilt++;
            character.OnStatChangedEvent.AddListener((string id, float value) => statEvents.Add(value));
            character.UpgradePointsState.AddListener((int points) =>
            {
                if (points != 0 || replaced) { return; }
                replaced = true;
                if (rebuildOnly)
                {
                    character.Template = replacement;
                    character.RebuildRuntime();
                }
                else { character.ApplyTemplate(replacement); }
                callbackProfile = character.CaptureProfile();
            });

            bool accepted = character.UpgradeStat("S");

            Assert.That(accepted, Is.True, "the original debit was accepted; replacement cancels its continuation");
            Assert.That(replaced, Is.True);
            Assert.That(rebuilt, Is.EqualTo(1));
            Assert.That(character.GetStat(omitStat ? "C" : "S"), Is.EqualTo(7f));
            Assert.That(character.GetUpgradeLevel("S"), Is.Zero);
            Assert.That(character.Stats[omitStat ? "C" : "S"].UpgradeCount, Is.Zero);
            Assert.That(character.UpgradePointsValue, Is.Zero, "no refund is written into replacement state");
            Assert.That(character.UpgradePointsState.CurrentValue, Is.Zero);
            Assert.That(statEvents, Is.EqualTo(new[] { 7f }), "no obsolete refresh publishes S=9");
            Assert.That(callbackProfile, Is.Not.Null);
            Assert.That(callbackProfile.Upgrades, Is.Empty);
            RpgCharacterProfileData finalProfile = character.CaptureProfile();
            Assert.That(finalProfile.Upgrades, Is.Empty, "omitting S cannot leave an obsolete investment row");
            Assert.That(finalProfile.UpgradePoints, Is.Zero);
            Assert.That(finalProfile.Stats.Count, Is.EqualTo(1));
            Assert.That(finalProfile.Stats[0].Id, Is.EqualTo(omitStat ? "C" : "S"));
            Assert.That(finalProfile.Stats[0].Base, Is.EqualTo(7f));
        }

        [Test]
        public void UpgradeStat_OrdinaryDebitStillCommitsCostInvestmentStatAndNotifications()
        {
            RpgCharacter character = CreateCharacter();
            character.Template.progression.upgradeRules[0].increasePerPoint = 2f;
            character.AddUpgradePoints(1);
            List<int> pointSignals = new();
            List<float> statSignals = new();
            List<float> statEvents = new();
            int resourceEvents = 0;
            int rebuilt = 0;
            character.UpgradePointsState.AddListener((int points) => pointSignals.Add(points));
            character.GetStatState("S").AddListener((float value) => statSignals.Add(value));
            character.OnStatChangedEvent.AddListener((string id, float value) =>
            {
                if (id == "S") { statEvents.Add(value); }
            });
            character.OnResourceChangedEvent.AddListener((string id, float value) => resourceEvents++);
            character.RuntimeRebuilt += () => rebuilt++;

            Assert.That(character.UpgradeStat("S"), Is.True);

            Assert.That(character.UpgradePointsValue, Is.Zero);
            Assert.That(character.GetUpgradeLevel("S"), Is.EqualTo(1));
            Assert.That(character.Stats["S"].UpgradeCount, Is.EqualTo(1));
            Assert.That(character.GetStat("S"), Is.EqualTo(12f));
            Assert.That(pointSignals, Is.EqualTo(new[] { 0 }));
            Assert.That(statSignals, Is.EqualTo(new[] { 12f }));
            Assert.That(statEvents, Is.EqualTo(new[] { 12f }));
            Assert.That(resourceEvents, Is.EqualTo(2));
            Assert.That(rebuilt, Is.Zero);
            RpgCharacterProfileData profile = character.CaptureProfile();
            Assert.That(profile.Upgrades.Count, Is.EqualTo(1));
            Assert.That(profile.Upgrades[0].StatId, Is.EqualTo("S"));
            Assert.That(profile.Upgrades[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void Spend_ZeroStoredDebitRejectsWithoutFacadePublicationThenRoundedCostPays()
        {
            RpgCharacter character = CreateCharacter();
            RpgResourceDefinition definition = character.Template.resources[1];
            definition.startMax = 1000000000f;
            definition.startCurrent = 100000000f;
            definition.restoreOnAwake = false;
            definition.regen.enabled = true;
            definition.regen.mode = RpgRegenMode.FlatPerTick;
            definition.regen.value = 2f;
            definition.regen.tickInterval = 0.5f;
            definition.regen.pauseAfterSpend = true;
            definition.regen.pauseAfterSpendSeconds = 2f;
            character.RebuildRuntime();
            RpgResourceRuntime resource = character.Resources["Mana"];
            resource.RegenPauseRemaining = 0.25f;
            resource.TickAccumulator = 0.125f;
            Audit20261023NeoRpgSnapshotProbe probe = character.gameObject.AddComponent<Audit20261023NeoRpgSnapshotProbe>();
            Set(character, "_networkAdapter", probe);
            int currentSignals = 0;
            int maxSignals = 0;
            int percentSignals = 0;
            int resourceEvents = 0;
            resource.CurrentState.AddListener((float value) => currentSignals++);
            resource.MaxState.AddListener((float value) => maxSignals++);
            resource.PercentState.AddListener((float value) => percentSignals++);
            character.OnResourceChangedEvent.AddListener((string id, float value) => resourceEvents++);

            Assert.That(character.Spend("Mana", 1f), Is.False);
            Assert.That(character.TrySpendResource("Mana", 1f, out string failure), Is.False);
            Assert.That(failure, Is.EqualTo("Not enough resource."));
            Assert.That(resource.Current, Is.EqualTo(100000000f));
            Assert.That(resource.Max, Is.EqualTo(1000000000f));
            Assert.That(resource.CurrentState.CurrentValue, Is.EqualTo(100000000f));
            Assert.That(resource.MaxState.CurrentValue, Is.EqualTo(1000000000f));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(0.1f));
            Assert.That(currentSignals, Is.Zero);
            Assert.That(maxSignals, Is.Zero);
            Assert.That(percentSignals, Is.Zero);
            Assert.That(resourceEvents, Is.Zero);
            Assert.That(probe.Snapshots, Is.Empty);
            Assert.That(resource.RegenPauseRemaining, Is.EqualTo(0.25f));
            Assert.That(resource.TickAccumulator, Is.EqualTo(0.125f));

            Assert.That(character.TrySpendResource("Mana", 5f, out failure), Is.True);
            Assert.That(failure, Is.Null);
            Assert.That(resource.Current, Is.EqualTo(99999992f));
            Assert.That(resource.CurrentState.CurrentValue, Is.EqualTo(99999992f));
            Assert.That(resource.PercentState.CurrentValue, Is.EqualTo(resource.Current / resource.Max));
            Assert.That(currentSignals, Is.EqualTo(1));
            Assert.That(maxSignals, Is.Zero);
            Assert.That(percentSignals, Is.EqualTo(1));
            Assert.That(resourceEvents, Is.EqualTo(1));
            Assert.That(resource.RegenPauseRemaining, Is.EqualTo(2f));
            Assert.That(resource.TickAccumulator, Is.EqualTo(0.125f));
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
            RpgResourceSaveEntry saved = probe.Snapshots[0].Resources.Find((RpgResourceSaveEntry row) => row.Id == "Mana");
            Assert.That(saved.Current, Is.EqualTo(99999992f));
        }

        [Test]
        public void Spend_FacadeFractionalAndZeroUnknownNonfinitePathsKeepTheirContracts()
        {
            RpgCharacter character = CreateCharacter();
            Audit20261023NeoRpgSnapshotProbe probe = character.gameObject.AddComponent<Audit20261023NeoRpgSnapshotProbe>();
            Set(character, "_networkAdapter", probe);
            int resourceEvents = 0;
            character.OnResourceChangedEvent.AddListener((string id, float value) => resourceEvents++);
            Assert.That(character.Spend("Missing", 0f), Is.True);
            Assert.That(character.TrySpendResource("Missing", 0f, out string failure), Is.False);
            Assert.That(failure, Is.EqualTo("Unknown resource id."));
            Assert.That(character.Spend("Missing", 1f), Is.False);
            Assert.That(character.Spend("Mana", float.NaN), Is.False);
            Assert.That(character.Spend("Mana", float.PositiveInfinity), Is.False);
            Assert.That(character.GetResource("Mana"), Is.EqualTo(100f));
            Assert.That(resourceEvents, Is.Zero);
            Assert.That(probe.Snapshots, Is.Empty);

            Assert.That(character.TrySpendResource("Mana", 0.25f, out failure), Is.True);
            Assert.That(failure, Is.Null);
            Assert.That(character.GetResource("Mana"), Is.EqualTo(99.75f));
            Assert.That(character.ManaState.CurrentValue, Is.EqualTo(99.75f));
            Assert.That(resourceEvents, Is.EqualTo(1));
            Assert.That(probe.Snapshots.Count, Is.EqualTo(1));
        }

        private RpgCharacter CreateCharacter(RpgLevelGrowthMode mode = RpgLevelGrowthMode.AllStatsEveryLevel,
            bool automatic = true)
        {
            RpgCharacterTemplate template = Template();
            RpgProgressionDefinition progression = Track(ScriptableObject.CreateInstance<RpgProgressionDefinition>());
            progression.growthMode = mode;
            progression.autoApplyGrowthOnLevelUp = automatic;
            progression.upgradeRules = new[] { new RpgStatUpgradeRule { statId = new RpgStatId("S"),
                costPerUpgrade = 1, increasePerPoint = 1f, maxUpgradeCount = -1 } };
            template.progression = progression;
            RpgCharacter character = Track(new GameObject("AuditRpgImplementation")).AddComponent<RpgCharacter>();
            character.ApplyTemplate(template);
            return character;
        }

        private RpgCharacterTemplate Template()
        {
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[]
            {
                new RpgResourceDefinition { id = new RpgStatId(RpgResourceId.Hp), startMax = 100f,
                    startCurrent = 90f, restoreOnAwake = false },
                new RpgResourceDefinition { id = new RpgStatId("Mana"), startMax = 100f }
            };
            template.stats = new[]
            {
                new RpgStatDefinition { id = new RpgStatId("S"), baseValue = 10f, affectedByLevel = true,
                    growth = new RpgStatGrowthRule { AddPerLevel = 2f } },
                new RpgStatDefinition { id = new RpgStatId("Other"), baseValue = 3f }
            };
            return template;
        }

        private RpgCharacterTemplate Replacement()
        {
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[] { new RpgResourceDefinition { id = new RpgStatId("C"), startMax = 7f } };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("C"), baseValue = 7f } };
            return template;
        }

        private BuffDefinition Buff(params BuffStatModifier[] modifiers)
        {
            BuffDefinition buff = Track(ScriptableObject.CreateInstance<BuffDefinition>());
            Set(buff, "_id", "buff-" + Guid.NewGuid().ToString("N"));
            Set(buff, "_duration", 1000000f);
            Set(buff, "_modifiers", modifiers);
            return buff;
        }

        private Func<bool> Install(RpgCharacter character, bool inline, bool stackable, int cap,
            params BuffStatModifier[] modifiers)
        {
            if (!inline)
            {
                BuffDefinition buff = Buff(modifiers);
                Set(buff, "_id", "grant");
                Set(buff, "_stackable", stackable);
                Set(buff, "_maxStacks", cap);
                return () => character.ApplyBuff(buff);
            }
            InlineBuffEntry entry = new();
            Set(entry, "_id", "grant");
            Set(entry, "_duration", 1000000f);
            Set(entry, "_stackable", stackable);
            Set(entry, "_maxStacks", cap);
            Set(entry, "_modifiers", modifiers);
            Set(character, "_inlineBuffs", new[] { entry });
            character.RebuildRuntime();
            return () => character.ApplyInlineBuff(0);
        }

        private static BuffStatModifier Modifier(BuffStatType type, string id, float value)
        {
            BuffStatModifier modifier = new();
            Set(modifier, "_statType", type);
            Set(modifier, "_targetId", new RpgStatId(id));
            Set(modifier, "_value", value);
            return modifier;
        }

        private T Track<T>(T instance) where T : Object { _objects.Add(instance); return instance; }
        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
