using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using Neo.Reactive;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoBonusCollectionTests
    {
        private readonly List<Object> _assets = new();
        private Audit20261023NeoEditorTestsSaveScope _saveScope;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _saveScope = new Audit20261023NeoEditorTestsSaveScope();
            _root = new GameObject(nameof(Audit20261023NeoBonusCollectionTests));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Object.DestroyImmediate(_root);
                foreach (Object asset in _assets)
                {
                    Object.DestroyImmediate(asset);
                }

                _assets.Clear();
            }
            finally
            {
                _saveScope.Dispose();
            }
        }

        [Test]
        public void GetPrize_SkipsEmptyCatalogSlots_AndOnlyPersistsTheGrantedItem()
        {
            ItemCollectionData prize = NewItem();
            Collection collection = NewCollection(null, prize);
            int grants = 0;
            int grantedId = -1;
            collection.OnGetItem = new UnityEvent<int>();
            collection.OnGetItem.AddListener(id => { grants++; grantedId = id; });

            Assert.That(collection.GetPrize(), Is.SameAs(prize));
            Assert.That(collection.HasItem(0), Is.False);
            Assert.That(collection.HasItem(1), Is.True);
            Assert.That(grantedId, Is.EqualTo(1));
            Assert.That(collection.GetPrize(), Is.Null);
            Assert.That(grants, Is.EqualTo(1));
            Assert.That(SaveProvider.HasKey(collection.SaveKeyPrefix + "Item_0"), Is.False);
            Assert.That(SaveProvider.GetInt(collection.SaveKeyPrefix + "Item_1"), Is.EqualTo(1));
        }

        [Test]
        public void TryAddItem_EmptyCatalogSlot_DoesNotChangeCompletionOrEmitAnUnlock()
        {
            Collection collection = NewCollection(null, NewItem());
            int unlocks = 0;
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnItemAdded.AddListener(_ => unlocks++);

            Assert.That(collection.TryAddItem(0), Is.False);
            Assert.That(collection.UnlockedCount, Is.Zero);
            Assert.That(collection.GetCompletionPercentage(), Is.Zero);
            Assert.That(unlocks, Is.Zero);
        }

        [Test]
        public void Box_ProgressPropertyAndAddProgress_UpdateTheBoundBarAndSavedValue()
        {
            Box box = _root.AddComponent<Box>();
            Image bar = NewChild("Progress").AddComponent<Image>();
            SetField(box, "_bar", bar);

            box.progress = 150f;
            Assert.That(bar.fillAmount, Is.EqualTo(0.5f).Within(0.001f));

            box.AddProgress(75f);
            Assert.That(bar.fillAmount, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(box.ProgressValue, Is.EqualTo(225f));
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(225f));
        }

        [Test]
        public void Box_ReentrantCollectionCallback_ConsumesOnePrizeAndOneProgressThreshold()
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Box box = _root.AddComponent<Box>();
            box.progress = box.MaxProgress * 2f;
            int unlocks = 0;
            int taken = 0;
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnItemAdded.AddListener(_ =>
            {
                if (++unlocks == 1)
                {
                    box.TakePrize();
                }
            });
            box.OnTakePrize = new UnityEvent();
            box.OnTakePrize.AddListener(() => taken++);

            box.TakePrize();

            Assert.That(collection.UnlockedCount, Is.EqualTo(1));
            Assert.That(box.progress, Is.EqualTo(box.MaxProgress));
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(box.MaxProgress));
            Assert.That(taken, Is.EqualTo(1));
            box.TakePrize(); // A later call is allowed after the first dispatch releases its guard.
            Assert.That(collection.UnlockedCount, Is.EqualTo(2));
            Assert.That(box.progress, Is.Zero);
            Assert.That(taken, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Box_ThrowingCollectionObserver_KeepsTheGrantedItemPaid_AndReleasesTheGuard(bool onGetItem)
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Box box = _root.AddComponent<Box>();
            box.progress = box.MaxProgress;
            List<int> notifiedIds = new();
            UnityAction<int> observer = id =>
            {
                notifiedIds.Add(id);
                Assert.That(collection.HasItem(id), Is.True);
                Assert.That(SaveProvider.GetInt(collection.SaveKeyPrefix + "Item_" + id), Is.EqualTo(1));
                Assert.That(box.progress, Is.Zero);
                Assert.That(box.ProgressValue, Is.Zero);
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.Zero);
                if (notifiedIds.Count == 1)
                {
                    throw new InvalidOperationException("Committed prize observer failed");
                }
            };
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnGetItem = new UnityEvent<int>();
            (onGetItem ? collection.OnGetItem : collection.OnItemAdded).AddListener(observer);

            Assert.Throws<InvalidOperationException>(() => box.TakePrize());
            CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
            Assert.That(collection.HasItem(1), Is.False);
            Assert.That(SaveProvider.HasKey(collection.SaveKeyPrefix + "Item_1"), Is.False);
            Assert.That(box.progress, Is.Zero);
            box.TakePrize();
            CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
            CollectionAssert.AreEqual(new[] { 0 }, notifiedIds);

            box.AddProgress(box.MaxProgress);
            box.TakePrize();
            CollectionAssert.AreEqual(new[] { 0, 1 }, collection.GetUnlockedIds());
            CollectionAssert.AreEqual(new[] { 0, 1 }, notifiedIds);
            Assert.That(SaveProvider.GetInt(collection.SaveKeyPrefix + "Item_1"), Is.EqualTo(1));
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.Zero);
        }

        [Test]
        public void Box_ExhaustedCollection_ConsumesNoProgressAndPublishesNoPrize()
        {
            Collection collection = NewCollection(NewItem());
            Assert.That(collection.TryAddItem(0), Is.True);
            Box box = _root.AddComponent<Box>();
            box.progress = box.MaxProgress;
            int taken = 0;
            box.OnTakePrize = new UnityEvent();
            box.OnTakePrize.AddListener(() => taken++);

            box.TakePrize();
            box.TakePrize();

            Assert.That(collection.GetUnlockedIds(), Is.EqualTo(new[] { 0 }));
            Assert.That(box.progress, Is.EqualTo(box.MaxProgress));
            Assert.That(box.ProgressValue, Is.EqualTo(box.MaxProgress));
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(box.MaxProgress));
            Assert.That(taken, Is.Zero);

            collection.ItemCollectionDatas = new[] { collection.ItemCollectionDatas[0], NewItem() };
            collection.Load();
            box.TakePrize();
            CollectionAssert.AreEqual(new[] { 0, 1 }, collection.GetUnlockedIds());
            Assert.That(box.progress, Is.Zero);
            Assert.That(taken, Is.EqualTo(1));
        }

        [Test]
        public void Box_ThrowingSaveObserver_StillConsumesTheAcceptedPrizePrice()
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Box box = _root.AddComponent<Box>();
            box.progress = box.MaxProgress;
            string firstItemKey = collection.SaveKeyPrefix + "Item_0";
            Action<string> observer = key =>
            {
                if (key == firstItemKey)
                {
                    throw new InvalidOperationException("Accepted unlock save observer failed");
                }
            };
            SaveProvider.OnKeyChanged += observer;
            try
            {
                Assert.Throws<InvalidOperationException>(() => box.TakePrize());
            }
            finally
            {
                SaveProvider.OnKeyChanged -= observer;
            }

            CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
            Assert.That(SaveProvider.GetInt(firstItemKey), Is.EqualTo(1));
            Assert.That(box.progress, Is.Zero);
            Assert.That(box.ProgressValue, Is.Zero);
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.Zero);
            box.TakePrize();
            Assert.That(collection.HasItem(1), Is.False);
            box.AddProgress(box.MaxProgress);
            box.TakePrize();
            CollectionAssert.AreEqual(new[] { 0, 1 }, collection.GetUnlockedIds());
            Assert.That(box.progress, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Box_ProgressSaveObserver_NestedCreditPreservesPaidCostAndLatestMirrors(bool nestedCredit)
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Assert.That(Collection.I, Is.SameAs(collection));
            Box box = _root.AddComponent<Box>();
            SetField(box, "_maxProgress", 10f);
            box.progress = 20f;
            float expected = nestedCredit ? 15f : 10f;
            float observedRaw = -1f;
            float observedSaved = -1f;
            float observedReactive = -1f;
            List<string> notifications = new();
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnItemAdded.AddListener(_ =>
            {
                observedRaw = box.progress;
                observedSaved = SaveProvider.GetFloat("BoxPrizeprogress");
                observedReactive = box.ProgressValue;
                notifications.Add("item");
            });
            collection.OnGetItem = new UnityEvent<int>();
            collection.OnGetItem.AddListener(_ => notifications.Add("get"));
            box.OnChangeProgress = new UnityEvent<bool>();
            box.OnChangeProgress.AddListener(_ => notifications.Add("progress"));
            box.OnTakePrize = new UnityEvent();
            box.OnTakePrize.AddListener(() => notifications.Add("take"));
            bool handled = false;
            Action<string> observer = key =>
            {
                if (key != "BoxPrizeprogress" || handled)
                {
                    return;
                }

                handled = true;
                if (nestedCredit)
                {
                    box.AddProgress(5f);
                    box.TakePrize(); // Still affordable; the active claim guard must reject it.
                }
            };
            SaveProvider.OnKeyChanged += observer;
            try
            {
                box.TakePrize();

                Assert.That(handled, Is.True);
                CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
                Assert.That(box.progress, Is.EqualTo(expected));
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(expected));
                Assert.That(box.ProgressValue, Is.EqualTo(expected));
                Assert.That(observedRaw, Is.EqualTo(expected));
                Assert.That(observedSaved, Is.EqualTo(expected));
                Assert.That(observedReactive, Is.EqualTo(expected));
                CollectionAssert.AreEqual(nestedCredit
                    ? new[] { "progress", "progress", "item", "get", "take" }
                    : new[] { "progress", "item", "get", "take" }, notifications);

                box.ChangeProgress(2f); // A later independent command must still publish normally.
                Assert.That(box.progress, Is.EqualTo(expected + 2f));
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(expected + 2f));
                Assert.That(box.ProgressValue, Is.EqualTo(expected + 2f));
                Assert.That(collection.UnlockedCount, Is.EqualTo(1));
            }
            finally
            {
                SaveProvider.OnKeyChanged -= observer;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Box_ProgressSaveObserver_SameValueOrAbaWriteSuppressesObsoletePublication(bool aba)
        {
            Collection collection = NewCollection(NewItem());
            Box box = _root.AddComponent<Box>();
            SetField(box, "_maxProgress", 10f);
            RecordedProgress recorded = new();
            box.Progress = recorded;
            box.progress = 20f;
            recorded.Publications.Clear();
            bool handled = false;
            Action<string> observer = key =>
            {
                if (key != "BoxPrizeprogress" || handled)
                {
                    return;
                }

                handled = true;
                if (aba)
                {
                    box.progress = 15f;
                }

                box.progress = 10f;
            };
            SaveProvider.OnKeyChanged += observer;
            try
            {
                box.TakePrize();

                Assert.That(handled, Is.True);
                CollectionAssert.AreEqual(aba ? new[] { 15f, 10f } : new[] { 10f }, recorded.Publications);
                Assert.That(box.progress, Is.EqualTo(10f));
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(10f));
                Assert.That(box.ProgressValue, Is.EqualTo(10f));
                CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
                box.progress = 5f;
                Assert.That(recorded.Publications[recorded.Publications.Count - 1], Is.EqualTo(5f));
                Assert.That(box.ProgressValue, Is.EqualTo(5f));
            }
            finally
            {
                SaveProvider.OnKeyChanged -= observer;
            }
        }

        [Test]
        public void Box_PositiveThresholdWithNoStoredDecrease_RejectsBeforeAnyGrantOrPublication()
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Assert.That(Collection.I, Is.SameAs(collection));
            Assert.That(collection.LockedCount, Is.EqualTo(2));
            Box box = _root.AddComponent<Box>();
            SetField(box, "_maxProgress", 1f);
            Image bar = NewChild("PrecisionProgress").AddComponent<Image>();
            SetField(box, "_bar", bar);
            box.progress = 100_000_000f;
            List<string> notifications = new();
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnItemAdded.AddListener(_ => notifications.Add("item"));
            collection.OnGetItem = new UnityEvent<int>();
            collection.OnGetItem.AddListener(_ => notifications.Add("get"));
            collection.OnCompletionChanged = new UnityEvent<int, int>();
            collection.OnCompletionChanged.AddListener((_, __) => notifications.Add("completion"));
            collection.OnCompletionPercentageChanged = new UnityEvent<float>();
            collection.OnCompletionPercentageChanged.AddListener(_ => notifications.Add("percentage"));
            box.Progress.AddListener(_ => notifications.Add("reactive"));
            box.OnChangeProgress = new UnityEvent<bool>();
            box.OnChangeProgress.AddListener(_ => notifications.Add("progress"));
            box.OnProgressReached = new UnityEvent();
            box.OnProgressReached.AddListener(() => notifications.Add("reached"));
            box.OnProgressNotReached = new UnityEvent();
            box.OnProgressNotReached.AddListener(() => notifications.Add("notReached"));
            box.OnTakePrize = new UnityEvent();
            box.OnTakePrize.AddListener(() => notifications.Add("take"));
            List<string> changedKeys = new();
            Action<string> observer = key => changedKeys.Add(key);
            SaveProvider.OnKeyChanged += observer;
            try
            {
                Assert.That(box.CheckProgress, Is.False);
                box.TakePrize();
                box.TakePrize();

                Assert.That(collection.UnlockedCount, Is.Zero);
                Assert.That(SaveProvider.HasKey(collection.SaveKeyPrefix + "Item_0"), Is.False);
                Assert.That(SaveProvider.HasKey(collection.SaveKeyPrefix + "Item_1"), Is.False);
                Assert.That(box.progress, Is.EqualTo(100_000_000f));
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(100_000_000f));
                Assert.That(box.ProgressValue, Is.EqualTo(100_000_000f));
                Assert.That(bar.fillAmount, Is.EqualTo(1f));
                Assert.That(changedKeys, Is.Empty);
                Assert.That(notifications, Is.Empty);

                SetField(box, "_maxProgress", 5f); // Same usable collection; rounded positive cost is admissible.
                Assert.That(box.CheckProgress, Is.True);
                box.TakePrize();
                CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
                Assert.That(box.progress, Is.EqualTo(99_999_992f));
                Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(99_999_992f));
                Assert.That(box.ProgressValue, Is.EqualTo(99_999_992f));
                CollectionAssert.AreEqual(new[] { collection.SaveKeyPrefix + "Item_0", "BoxPrizeprogress" }, changedKeys);
                CollectionAssert.AreEqual(new[]
                {
                    "reactive", "progress", "reached", "item", "completion", "percentage", "get", "take"
                }, notifications);
            }
            finally
            {
                SaveProvider.OnKeyChanged -= observer;
            }
        }

        [TestCase(2.5f, 0.75f, 1.75f)]
        [TestCase(100_000_000f, 5f, 99_999_992f)]
        [TestCase(20f, 0f, 20f)]
        [TestCase(0f, 0f, 0f)]
        public void Box_EffectiveOrLegacyZeroThreshold_GrantsExactlyOnePrize(float initial, float threshold,
            float expected)
        {
            Collection collection = NewCollection(NewItem(), NewItem());
            Assert.That(Collection.I, Is.SameAs(collection));
            Box box = _root.AddComponent<Box>();
            SetField(box, "_maxProgress", threshold);
            box.progress = initial;
            int added = 0;
            int taken = 0;
            float observedProgress = -1f;
            collection.OnItemAdded = new UnityEvent<int>();
            collection.OnItemAdded.AddListener(_ =>
            {
                added++;
                observedProgress = box.ProgressValue;
            });
            box.OnTakePrize = new UnityEvent();
            box.OnTakePrize.AddListener(() => taken++);

            Assert.That(box.CheckProgress, Is.True);
            box.TakePrize();

            CollectionAssert.AreEqual(new[] { 0 }, collection.GetUnlockedIds());
            Assert.That(SaveProvider.GetInt(collection.SaveKeyPrefix + "Item_0"), Is.EqualTo(1));
            Assert.That(SaveProvider.HasKey(collection.SaveKeyPrefix + "Item_1"), Is.False);
            Assert.That(box.progress, Is.EqualTo(expected));
            Assert.That(SaveProvider.GetFloat("BoxPrizeprogress"), Is.EqualTo(expected));
            Assert.That(box.ProgressValue, Is.EqualTo(expected));
            Assert.That(observedProgress, Is.EqualTo(expected));
            Assert.That(added, Is.EqualTo(1));
            Assert.That(taken, Is.EqualTo(1));
        }

        [Test]
        public void ItemCollection_WithoutAnOptionalChangeEvent_StillPublishesStateTransitionsOnce()
        {
            ItemCollection item = _root.AddComponent<ItemCollection>();
            item.OnChangeEnabled = null;
            item.OnActive = new UnityEvent();
            item.OnDeactivated = new UnityEvent();
            int activated = 0;
            int deactivated = 0;
            item.OnActive.AddListener(() => activated++);
            item.OnDeactivated.AddListener(() => deactivated++);

            item.SetEnabled(true);
            item.SetEnabled(true);
            Assert.That(item.IsEnabled, Is.True);
            Assert.That(activated, Is.EqualTo(1));
            item.SetEnabled(false);
            Assert.That(item.IsEnabled, Is.False);
            Assert.That(deactivated, Is.EqualTo(1));
        }

        [Test]
        public void ItemCollectionInfo_SwitchingToAnItemWithoutAnIcon_ClearsThePreviousIcon()
        {
            Texture2D texture = new(2, 2);
            _assets.Add(texture);
            Sprite icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            _assets.Add(icon);
            ItemCollectionData first = NewItem();
            SetField(first, "_sprite", icon);
            ItemCollectionData second = NewItem();
            ItemCollectionInfo info = _root.AddComponent<ItemCollectionInfo>();
            Image image = NewChild("Icon").AddComponent<Image>();
            SetField(info, "_imageItem", image);

            info.SetData(first);
            Assert.That(image.sprite, Is.SameAs(icon));
            info.SetData(second);
            Assert.That(image.sprite, Is.Null);
        }

        [Test]
        public void VisualManager_RejectsInvalidSelections_AndCanSelectAValidLockedPreview()
        {
            CollectionVisualManager manager = _root.AddComponent<CollectionVisualManager>();
            ItemCollection item = NewChild("Item").AddComponent<ItemCollection>();
            ItemCollection outside = NewChild("Outside").AddComponent<ItemCollection>();
            SetField(manager, "_items", new[] { item });
            manager.OnSetItem = new UnityEvent<int>();
            int selected = -1;
            int selections = 0;
            manager.OnSetItem.AddListener(id => { selected = id; selections++; });

            manager.SetItem(0); // An unlock-gated view without a Collection cannot select.
            Assert.That(selections, Is.Zero);
            manager.EnableSetItem = false;
            manager.SetItem(-1);
            manager.SetItem(1);
            manager.SetItem(outside);
            manager.SetItem((ItemCollection)null);
            Assert.That(selections, Is.Zero);
            manager.SetItem(0);
            Assert.That(selected, Is.Zero);
            Assert.That(selections, Is.EqualTo(1));
        }

        private Collection NewCollection(params ItemCollectionData[] items)
        {
            Collection collection = _root.AddComponent<Collection>();
            collection.SaveKeyPrefix = "Audit20261023NeoBonusCollection_";
            collection.ItemCollectionDatas = items;
            collection.RandomPrize = false;
            collection.Load();
            return collection;
        }

        private ItemCollectionData NewItem()
        {
            ItemCollectionData item = ScriptableObject.CreateInstance<ItemCollectionData>();
            _assets.Add(item);
            return item;
        }

        private GameObject NewChild(string name)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return child;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private sealed class RecordedProgress : ReactivePropertyFloat
        {
            public readonly List<float> Publications = new();

            public override float Value
            {
                get => base.Value;
                set
                {
                    Publications.Add(value);
                    base.Value = value;
                }
            }
        }
    }
}
