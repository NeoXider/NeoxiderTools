using System;
using System.Collections.Generic;
using Neo.Bonus;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Contracts of the plain C# Bonus cores: <see cref="CollectionSet" />, <see cref="BoxProgress" /> with
    ///     <see cref="BoxProgressRules" />, <see cref="CooldownSchedule" /> and <see cref="SlotSpinMath" />.
    /// </summary>
    public sealed class BonusPlainCoreTests
    {
        private readonly List<Object> _owned = new();
        private Audit20261023NeoEditorTestsSaveScope _saveScope;

        [SetUp]
        public void SetUp()
        {
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (Object owned in _owned)
                {
                    if (owned != null)
                    {
                        Object.DestroyImmediate(owned);
                    }
                }

                _owned.Clear();
            }
            finally
            {
                _saveScope.Dispose();
            }
        }

        [Test]
        public void CollectionSet_IndependentSets_AreClaimedPersistedAndResetSeparately()
        {
            CollectionSet stickers = new(3, "Stickers_");
            CollectionSet trophies = new(2, "Trophies_");

            Assert.That(stickers.TryUnlock(1), Is.True);
            Assert.That(trophies.TryUnlock(0), Is.True);
            Assert.That(trophies.TryUnlock(1), Is.True);
            Assert.That(trophies.IsComplete, Is.True);

            Assert.That(trophies.LockAll(), Is.EqualTo(2));

            Assert.That(stickers.IsUnlocked(1), Is.True, "Resetting one set must not touch another.");
            Assert.That(stickers.UnlockedCount, Is.EqualTo(1));
            Assert.That(SaveProvider.GetInt("Stickers_Item_1"), Is.EqualTo(1));
            Assert.That(SaveProvider.GetInt("Trophies_Item_0"), Is.Zero);

            CollectionSet reloaded = new(3, "Stickers_");
            reloaded.Load();
            Assert.That(reloaded.IsUnlocked(1), Is.True);
            Assert.That(reloaded.IsUnlocked(0), Is.False);
        }

        [Test]
        public void CollectionSet_PickLockedIndex_UsesInjectedRandomAndSkipsUnclaimableSlots()
        {
            CollectionSet set = new(4, null, index => index != 1);
            set.TryUnlock(0);
            int requestedRange = -1;

            int picked = set.PickLockedIndex(true, maxExclusive =>
            {
                requestedRange = maxExclusive;
                return 1;
            });

            Assert.That(requestedRange, Is.EqualTo(2), "Only slots 2 and 3 are locked and claimable.");
            Assert.That(picked, Is.EqualTo(3));
            Assert.That(set.PickLockedIndex(false), Is.EqualTo(2));
            Assert.That(set.TryUnlock(1), Is.False, "A filtered slot can never be unlocked.");
        }

        [Test]
        public void CollectionSet_TryClaimNext_RaisesEventsAfterStateChangeUntilExhausted()
        {
            CollectionSet set = new(2);
            List<int> unlocked = new();
            List<string> completion = new();
            set.ItemUnlocked += index =>
            {
                Assert.That(set.IsUnlocked(index), Is.True, "State is committed before the event.");
                unlocked.Add(index);
            };
            set.CompletionChanged += (count, total) => completion.Add(count + "/" + total);

            Assert.That(set.TryClaimNext(out int first, false), Is.True);
            Assert.That(set.TryClaimNext(out int second, false), Is.True);
            Assert.That(set.TryClaimNext(out int none, false), Is.False);

            Assert.That(first, Is.Zero);
            Assert.That(second, Is.EqualTo(1));
            Assert.That(none, Is.EqualTo(-1));
            Assert.That(unlocked, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(completion, Is.EqualTo(new[] { "1/2", "2/2" }));
            Assert.That(set.IsComplete, Is.True);
        }

        [Test]
        public void Collection_SetEvents_FollowTheComponentAndSurviveAReloadThatResizesStorage()
        {
            GameObject root = Own(new GameObject(nameof(Collection_SetEvents_FollowTheComponentAndSurviveAReloadThatResizesStorage)));
            Collection collection = root.AddComponent<Collection>();
            collection.SaveKeyPrefix = "BonusPlainCoreCollection_";
            collection.ItemCollectionDatas = new[] { NewItem() };
            collection.RandomPrize = false;
            collection.Load();

            List<int> unlocked = new();
            IReadOnlyCollectionSet set = collection.Set;
            set.ItemUnlocked += unlocked.Add;

            collection.ItemCollectionDatas = new[] { NewItem(), NewItem(), NewItem() };
            collection.Load();

            Assert.That(collection.Set, Is.SameAs(set), "Subscribers keep the same core after the array is replaced.");
            Assert.That(set.ItemCount, Is.EqualTo(3));
            Assert.That(collection.TryAddItem(2), Is.True);
            Assert.That(unlocked, Is.EqualTo(new[] { 2 }));
            Assert.That(set.IsUnlocked(2), Is.True);
            Assert.That(collection.EnabledItems[2], Is.True, "The core and the serialized array are one storage.");
        }

        [Test]
        public void Collection_Set_ExposesNoMutatorsSoChangesGoThroughTheComponent()
        {
            Assert.That(typeof(Collection).GetProperty(nameof(Collection.Set))?.PropertyType,
                Is.EqualTo(typeof(IReadOnlyCollectionSet)));

            string[] mutators =
            {
                nameof(CollectionSet.TryUnlock), nameof(CollectionSet.TryLock), nameof(CollectionSet.UnlockAll),
                nameof(CollectionSet.LockAll), nameof(CollectionSet.TryClaimNext), nameof(CollectionSet.Load),
                nameof(CollectionSet.Save)
            };
            foreach (string mutator in mutators)
            {
                Assert.That(typeof(IReadOnlyCollectionSet).GetMethod(mutator), Is.Null,
                    mutator + " would bypass the component's persistence and UnityEvents.");
            }

            Assert.That(typeof(IReadOnlyCollectionSet).GetProperty(nameof(CollectionSet.SaveKeyPrefix)), Is.Null);
        }

        [Test]
        public void CollectionSet_OutOfRangeInjectedRandom_ThrowsInsteadOfGrantingTheWrongSlot()
        {
            CollectionSet set = new(3);

            Assert.Throws<InvalidOperationException>(() => set.PickLockedIndex(true, maxExclusive => maxExclusive));
            Assert.Throws<InvalidOperationException>(() => set.TryClaimNext(out _, true, _ => -1));
            Assert.That(set.UnlockedCount, Is.Zero, "A rejected roll unlocks nothing.");
        }

        [Test]
        public void Collection_RandomIndexSource_MakesPrizePicksDeterministic()
        {
            GameObject root = Own(new GameObject(nameof(Collection_RandomIndexSource_MakesPrizePicksDeterministic)));
            Collection collection = root.AddComponent<Collection>();
            collection.SaveKeyPrefix = "BonusPlainCoreRandom_";
            ItemCollectionData[] items = { NewItem(), NewItem(), NewItem() };
            collection.ItemCollectionDatas = items;
            collection.RandomPrize = true;
            collection.Load();
            collection.RandomIndexSource = maxExclusive => maxExclusive - 1;

            Assert.That(collection.GetPrize(), Is.SameAs(items[2]));
            Assert.That(collection.GetPrize(), Is.SameAs(items[1]));
        }

        [Test]
        public void BoxProgress_IntegerThreshold_ClaimsConsumeExactlyAndReplaysAreIdempotent()
        {
            BoxProgress box = new(threshold: 300);
            List<BoxClaimReceipt> claims = new();
            box.Claimed += claims.Add;

            box.Add(250);
            Assert.That(box.CanClaim, Is.False);
            Assert.That(box.TryClaim("run-1", out _), Is.False);

            box.Add(400);
            Assert.That(box.TryClaim("run-1", out BoxClaimReceipt first), Is.True);
            Assert.That(first.IsReplay, Is.False);
            Assert.That(first.ConsumedAmount, Is.EqualTo(300));
            Assert.That(first.RemainingProgress, Is.EqualTo(350));

            Assert.That(box.TryClaim("run-1", out BoxClaimReceipt replay), Is.True);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.Sequence, Is.EqualTo(first.Sequence));
            Assert.That(box.Progress, Is.EqualTo(350), "A replayed claim id consumes nothing.");
            Assert.That(claims.Count, Is.EqualTo(1), "A replay does not raise Claimed again.");

            Assert.That(box.TryClaim("run-2", out BoxClaimReceipt second), Is.True);
            Assert.That(second.Sequence, Is.EqualTo(2));
            Assert.That(box.Progress, Is.EqualTo(50));
        }

        [Test]
        public void BoxProgress_CaptureRestore_KeepsClaimIdsIdempotentAcrossRestart()
        {
            BoxProgress before = new(100);
            before.Add(250);
            before.TryClaim("purchase-42", out _);
            string json = JsonUtility.ToJson(before.Capture());

            BoxProgress after = new(100);
            after.Restore(JsonUtility.FromJson<BoxProgressState>(json));

            Assert.That(after.Progress, Is.EqualTo(150));
            Assert.That(after.ClaimSequence, Is.EqualTo(1));
            Assert.That(after.TryClaim("purchase-42", out BoxClaimReceipt replay), Is.True);
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(after.Progress, Is.EqualTo(150));
        }

        [Test]
        public void BoxProgress_AutomaticClaims_NeverCollideWithCallerClaimIds()
        {
            BoxProgress box = new(10, 100);

            Assert.That(box.TryClaim(out BoxClaimReceipt automatic), Is.True);
            Assert.That(automatic.ClaimId, Is.Null);
            Assert.That(automatic.Sequence, Is.EqualTo(1));

            Assert.That(box.TryClaim("1", out BoxClaimReceipt explicitOne), Is.True);
            Assert.That(explicitOne.IsReplay, Is.False, "Caller id \"1\" is not automatic claim number 1.");
            Assert.That(box.Progress, Is.EqualTo(80));

            BoxProgress restored = new(10);
            restored.Restore(JsonUtility.FromJson<BoxProgressState>(JsonUtility.ToJson(box.Capture())));
            Assert.That(restored.TryClaim(string.Empty, out BoxClaimReceipt emptyId), Is.True);
            Assert.That(emptyId.IsReplay, Is.False, "An empty id is automatic, even after a JsonUtility round trip.");
            Assert.That(emptyId.Sequence, Is.EqualTo(3));
            Assert.That(restored.TryClaim("1", out BoxClaimReceipt replay), Is.True);
            Assert.That(replay.IsReplay, Is.True, "The explicit id survives the restore.");
            Assert.That(restored.Progress, Is.EqualTo(70));
        }

        [Test]
        public void BoxProgress_AddSaturatesAndNeverGoesNegative()
        {
            BoxProgress box = new(10, long.MaxValue - 1);
            Assert.That(box.Add(100), Is.EqualTo(long.MaxValue));
            Assert.That(box.Add(long.MinValue), Is.Zero);
            Assert.That(box.Add(-5), Is.Zero);
        }

        [Test]
        public void BoxProgressRules_FloatRules_MatchTheBoxComponentContract()
        {
            Assert.That(BoxProgressRules.CanClaim(300f, 300f), Is.True);
            Assert.That(BoxProgressRules.CanClaim(299f, 300f), Is.False);
            Assert.That(BoxProgressRules.CanClaim(0f, 0f), Is.True, "A zero threshold is a free claim.");
            Assert.That(BoxProgressRules.CanClaim(1e20f, 1f), Is.False, "A threshold lost to float precision is not claimable.");
            Assert.That(BoxProgressRules.GetProgressAfterClaim(450f, 300f), Is.EqualTo(150f));
            Assert.That(BoxProgressRules.GetFillRatio(150f, 300f), Is.EqualTo(0.5f));
            Assert.That(BoxProgressRules.GetFillRatio(5L, 0L), Is.Zero);
        }

        [Test]
        public void CooldownSchedule_InjectedClock_CountsCyclesAndResetsFromClaimTime()
        {
            FakeClock clock = new(new DateTime(2031, 5, 6, 7, 8, 9, DateTimeKind.Utc));
            CooldownSchedule schedule = new(60f, maxClaimsPerTake: -1, availableInitially: true, clock: clock);
            int granted = 0;
            schedule.Claimed += count => granted += count;

            Assert.That(schedule.GetClaimableCount(), Is.EqualTo(1));
            Assert.That(schedule.TryClaim(out int firstCount), Is.True);
            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(schedule.TryClaim(out _), Is.False, "The cooldown restarts at the claim.");
            Assert.That(schedule.GetSecondsUntilNext(), Is.EqualTo(60d).Within(1e-6));

            clock.Advance(150d);
            Assert.That(schedule.GetClaimableCount(), Is.EqualTo(2));
            Assert.That(schedule.TryClaim(out int secondCount), Is.True);
            Assert.That(secondCount, Is.EqualTo(2));
            Assert.That(schedule.GetSecondsUntilNext(), Is.EqualTo(60d).Within(1e-6),
                "ResetFromClaimTime drops the 30 s remainder.");
            Assert.That(granted, Is.EqualTo(3));
        }

        [Test]
        public void CooldownSchedule_PreserveRemainder_SurvivesARestartThroughSave()
        {
            FakeClock clock = new(new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            CooldownSchedule schedule = new(100f, maxClaimsPerTake: 3, clock: clock)
            {
                BacklogMode = CooldownBacklogMode.PreserveElapsedRemainder
            };
            schedule.Restart();
            clock.Advance(1050d);

            Assert.That(schedule.TryClaim(out int granted), Is.True);
            Assert.That(granted, Is.EqualTo(3), "10.5 cycles capped at 3 per take.");
            schedule.SaveTo("BonusPlainCoreCooldown");

            CooldownSchedule restarted = new(100f, maxClaimsPerTake: 3, clock: clock)
            {
                BacklogMode = CooldownBacklogMode.PreserveElapsedRemainder
            };
            Assert.That(restarted.TryLoadFrom("BonusPlainCoreCooldown"), Is.True);
            Assert.That(restarted.LastClaimUtc, Is.EqualTo(schedule.LastClaimUtc));
            Assert.That(restarted.GetClaimableCount(), Is.EqualTo(3));
            restarted.TryClaim(out int second);
            restarted.TryClaim(out int third);
            restarted.TryClaim(out int fourth);
            Assert.That(new[] { second, third, fourth }, Is.EqualTo(new[] { 3, 3, 1 }), "3/3/3/1 over 10.5 cycles.");
            Assert.That(restarted.GetClaimableCount(), Is.Zero);
            Assert.That(restarted.GetSecondsUntilNext(), Is.EqualTo(50d).Within(1e-3),
                "Half a cooldown remains under PreserveElapsedRemainder.");
        }

        [Test]
        public void CooldownSchedule_ReentrantClaimFromListener_IsRejected()
        {
            FakeClock clock = new(new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            // WHY: with a preserved backlog the outer claim leaves rewards claimable, so only the in-progress guard
            // can reject the nested claim.
            CooldownSchedule schedule = new(10f, maxClaimsPerTake: 1, clock: clock)
            {
                BacklogMode = CooldownBacklogMode.PreserveElapsedRemainder
            };
            schedule.Restart();
            clock.Advance(55d);
            bool nestedResult = true;
            int nestedGranted = -1;
            schedule.Claimed += _ => nestedResult = schedule.TryClaim(out nestedGranted);

            Assert.That(schedule.TryClaim(out int granted), Is.True);
            Assert.That(granted, Is.EqualTo(1));
            Assert.That(nestedResult, Is.False, "A claim from a Claimed listener is rejected.");
            Assert.That(nestedGranted, Is.Zero);
            Assert.That(schedule.GetClaimableCount(), Is.EqualTo(1),
                "Backlog is still claimable afterwards, so the guard (not the cooldown) rejected the nested claim.");
        }

        [Test]
        public void CooldownSchedule_MakeAvailableNow_GrantsExactlyOneFractionalCooldown()
        {
            FakeClock clock = new(new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            CooldownSchedule schedule = new(0.3f, maxClaimsPerTake: -1, clock: clock);

            Assert.That(schedule.GetClaimableCount(), Is.Zero, "Never started and not available initially.");
            schedule.MakeAvailableNow(clock.UtcNow);
            Assert.That(schedule.GetClaimableCount(), Is.EqualTo(1));
        }

        [Test]
        public void CooldownSchedule_CountClaimable_IsTheSharedRule()
        {
            DateTime last = new(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(CooldownSchedule.CountClaimable(true, last, 10f, last.AddSeconds(35), 1, false), Is.EqualTo(1));
            Assert.That(CooldownSchedule.CountClaimable(true, last, 10f, last.AddSeconds(35), -1, false), Is.EqualTo(3));
            Assert.That(CooldownSchedule.CountClaimable(false, default, 10f, last, 1, true), Is.EqualTo(1));
            Assert.That(CooldownSchedule.CountClaimable(false, default, 10f, last, 1, false), Is.Zero);
            Assert.That(CooldownSchedule.CountClaimable(true, last, 0f, last.AddDays(1), -1, true), Is.Zero);
        }

        [Test]
        public void SlotSpinMath_PriceAndPayout_SaturateAndIgnoreInvalidInput()
        {
            Assert.That(SlotSpinMath.ComputeSpinPrice(10, 5, true), Is.EqualTo(50));
            Assert.That(SlotSpinMath.ComputeSpinPrice(10, 5, false), Is.EqualTo(10));
            Assert.That(SlotSpinMath.ComputeSpinPrice(-10, 5, true), Is.Zero);
            Assert.That(SlotSpinMath.ComputeSpinPrice(int.MaxValue, 3, true), Is.EqualTo(int.MaxValue));

            Assert.That(SlotSpinMath.ComputePayout(new[] { 2f, 0.5f, float.NaN, -1f }, 10), Is.EqualTo(25));
            Assert.That(SlotSpinMath.ComputePayout(new[] { 0.25f }, 10), Is.EqualTo(2), "2.5 rounds half to even.");
            Assert.That(SlotSpinMath.ComputePayout(new[] { float.MaxValue }, 1000), Is.EqualTo(int.MaxValue));
            Assert.That(SlotSpinMath.ComputePayout(null, 10), Is.Zero);
        }

        private ItemCollectionData NewItem()
        {
            return Own(ScriptableObject.CreateInstance<ItemCollectionData>());
        }

        private T Own<T>(T owned) where T : Object
        {
            _owned.Add(owned);
            return owned;
        }

        private sealed class FakeClock : INeoClock
        {
            public FakeClock(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; private set; }

            public double MonotonicSeconds { get; private set; }

            public void Advance(double seconds)
            {
                UtcNow = UtcNow.AddSeconds(seconds);
                MonotonicSeconds += seconds;
            }
        }
    }
}
