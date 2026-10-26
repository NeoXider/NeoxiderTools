using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using Neo.Shop;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoShopRegressionTests
    {
        private readonly List<Object> _objects = new();
        private readonly List<MonoBehaviour> _subscriptions = new();
        private ISaveProvider _previousProvider;
        private Audit20261023NeoShopMemoryProvider _provider;
        private bool _previousErrors;
        private Money _money;
        private ShopBehaviour _shop;
        private ShopItemData _item;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _provider = new Audit20261023NeoShopMemoryProvider();
            SaveProvider.SetProvider(_provider);
            _previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
            _money = CreateMoney("AuditCoins");
            _shop = CreateShop();
            _item = CreateItem("sword", 50);
            _shop.SetItems(new[] { _item });
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                if (_subscriptions[i] != null)
                {
                    InvokeLifecycle(_subscriptions[i], "OnDisable");
                }
            }

            _subscriptions.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            SaveProvider.SetProvider(_previousProvider);
            NeoDiagnostics.Configure(errors: _previousErrors);
        }

        [TestCase(0f, false)]
        [TestCase(100f, false)]
        [TestCase(0f, true)]
        [TestCase(100f, true)]
        public void FailedPurchase_RestoresBalanceWithoutCountingRefundAsIncome(float cap, bool bundlePurchase)
        {
            _money.MaxMoney = cap;
            _money.AddOverflow(150f);
            int purchased = 0;
            _shop.OnPurchasedId.AddListener(_ => purchased++);
            string failedId = null;
            _shop.OnPurchaseFailedId.AddListener(id => failedId = id);

            string purchaseId = bundlePurchase ? "starter" : _item.Id;
            _provider.StringWritesToFail = 1;
            ExpectRollback(purchaseId);
            if (bundlePurchase)
            {
                ShopBundleData bundle = ScriptableObject.CreateInstance<ShopBundleData>();
                _objects.Add(bundle);
                SetPrivate(bundle, "_id", purchaseId);
                SetPrivate(bundle, "_bundlePrice", 50);
                SetPrivate(bundle, "_items", new[] { _item });
                _shop.SetBundles(new[] { bundle });
                _shop.BuyBundle(bundle);
                Assert.That(_shop.IsBundleOwned(bundle), Is.False);
            }
            else
            {
                _shop.Buy(_item);
            }

            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_money.allMoney, Is.EqualTo(150f), "A refunded debit is not new income.");
            Assert.That(_money.LastChangeMoneyValue, Is.EqualTo(50f));
            Assert.That(_shop.IsOwned(_item), Is.False);
            Assert.That(failedId, Is.EqualTo(purchaseId));
            Assert.That(purchased, Is.Zero, "Failed persistence must not notify inventory/grant observers.");
            ShopProfileData saved = JsonUtility.FromJson<ShopProfileData>(SaveProvider.GetString("Shop"));
            Assert.That(saved.IsItemOwned(_item.Id), Is.False);
            Assert.That(saved.EquippedId, Is.Empty);
            Assert.That(_shop.LastNotificationFault, Is.Null);
        }

        [Test]
        public void FailedFreePurchase_RollsBackPersistedOwnershipAndCanBeRetried()
        {
            _shop.SetRuntimePrice(_item, 0f);
            int purchased = 0;
            _shop.OnPurchasedId.AddListener(_ => purchased++);
            int failures = 0;
            _shop.OnPurchaseFailedId.AddListener(_ => failures++);

            _provider.StringWritesToFail = 1;
            ExpectRollback();
            _shop.Buy(_item);

            Assert.That(_shop.IsOwned(_item), Is.False);
            Assert.That(failures, Is.EqualTo(1));
            ShopProfileData saved = JsonUtility.FromJson<ShopProfileData>(SaveProvider.GetString("Shop"));
            Assert.That(saved.IsItemOwned(_item.Id), Is.False);
            Assert.That(purchased, Is.Zero);

            _shop.Buy(_item);
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.EquippedId, Is.EqualTo(_item.Id));
            Assert.That(_money.money, Is.Zero);
            Assert.That(purchased, Is.EqualTo(1));
        }

        [TestCase("OnPurchased")]
        [TestCase("OnPurchasedId")]
        [TestCase("OnShopChanged")]
        [TestCase("OnSelectId")]
        [TestCase("VisualAll")]
        public void PostCommitObserverFault_DoesNotRefundOrSuppressOtherNotificationStages(string observer)
        {
            _money.Add(200f);
            int purchases = 0;
            int failures = 0;
            List<ShopNotificationFault> faults = new();
            _shop.OnPurchasedId.AddListener(_ => purchases++);
            _shop.OnPurchaseFailedId.AddListener(_ => failures++);
            _shop.OnNotificationFault.AddListener(faults.Add);
            switch (observer)
            {
                case "OnPurchased":
                    _shop.OnPurchased.AddListener(_ => throw new InvalidOperationException("observer failed"));
                    break;
                case "OnPurchasedId":
                    _shop.OnPurchasedId.AddListener(_ => throw new InvalidOperationException("observer failed"));
                    break;
                case "OnShopChanged":
                    _shop.OnShopChanged.AddListener(() => throw new InvalidOperationException("observer failed"));
                    break;
                case "OnSelectId":
                    _shop.OnSelectId.AddListener(_ => throw new InvalidOperationException("observer failed"));
                    break;
                case "VisualAll":
                    ShopItem row = CreateRow(out ButtonPrice price);
                    SetPrivate(_shop, "_shopItems", new[] { row });
                    bool firstViewChange = true;
                    row.OnViewChanged.AddListener(() =>
                    {
                        if (firstViewChange)
                        {
                            firstViewChange = false;
                            throw new InvalidOperationException("observer failed");
                        }
                    });
                    break;
            }

            ExpectObserverFault(_item.Id, observer);
            _shop.Buy(_item);

            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_money.allMoney, Is.EqualTo(200f));
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.EquippedId, Is.EqualTo(_item.Id));
            ShopProfileData saved = JsonUtility.FromJson<ShopProfileData>(SaveProvider.GetString("Shop"));
            Assert.That(saved.IsItemOwned(_item.Id), Is.True);
            Assert.That(saved.EquippedId, Is.EqualTo(_item.Id));
            Assert.That(purchases, Is.EqualTo(1));
            Assert.That(failures, Is.Zero);
            Assert.That(faults.Count, Is.EqualTo(1));
            Assert.That(faults[0].PurchaseId, Is.EqualTo(_item.Id));
            Assert.That(faults[0].Observer, Is.EqualTo(observer));
            Assert.That(faults[0].Message, Is.EqualTo("observer failed"));
            Assert.That(_shop.LastNotificationFault, Is.SameAs(faults[0]));
        }

        [Test]
        public void BundleObserverFault_UsesStagedIdsAndContinuesLaterItemsAndBundleEvent()
        {
            ShopItemData hat = CreateItem("hat", 50);
            _shop.SetItems(new[] { _item, hat });
            ShopBundleData bundle = CreateBundle("starter", _item, hat);
            _shop.SetBundles(new[] { bundle });
            _money.Add(200f);
            List<string> delivered = new();
            int bundleEvents = 0;
            bool firstObserverSawCommittedProfile = false;
            _shop.OnPurchasedBundle.AddListener(_ => bundleEvents++);
            _shop.OnPurchasedId.AddListener(id =>
            {
                delivered.Add(id);
                if (id == _item.Id)
                {
                    firstObserverSawCommittedProfile = _shop.IsOwned(hat) && _shop.IsBundleOwned(bundle);
                    SetPrivate(bundle, "_items", Array.Empty<ShopItemData>());
                    throw new InvalidOperationException("first child observer failed");
                }
            });

            ExpectObserverFault(bundle.Id, "OnPurchasedId");
            _shop.BuyBundle(bundle);

            Assert.That(delivered, Is.EqualTo(new[] { _item.Id, hat.Id }));
            Assert.That(bundleEvents, Is.EqualTo(1));
            Assert.That(firstObserverSawCommittedProfile, Is.True);
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.IsOwned(hat), Is.True);
            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_money.allMoney, Is.EqualTo(200f));
            Assert.That(_shop.LastNotificationFault.PurchaseId, Is.EqualTo(bundle.Id));
            Assert.That(_shop.LastNotificationFault.ItemId, Is.EqualTo(_item.Id));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void NestedPurchase_IsRejectedBeforeDebit_AndNextIndependentCallSucceeds(bool outerBundle,
            bool innerBundle)
        {
            ShopItemData hat = CreateItem("hat", 50);
            _shop.SetItems(new[] { _item, hat });
            ShopBundleData first = CreateBundle("first", _item);
            ShopBundleData second = CreateBundle("second", hat);
            _shop.SetBundles(new[] { first, second });
            _money.Add(200f);
            float balanceDuringNestedAttempt = 0f;
            bool nestedItemWasOwned = true;
            _shop.OnPurchasedId.AddListener(id =>
            {
                if (id != _item.Id)
                {
                    return;
                }

                if (innerBundle)
                {
                    _shop.BuyBundle(second);
                }
                else
                {
                    _shop.Buy(hat);
                }

                balanceDuringNestedAttempt = _money.money;
                nestedItemWasOwned = _shop.IsOwned(hat);
                throw new InvalidOperationException("outer observer failed");
            });

            ExpectObserverFault(outerBundle ? first.Id : _item.Id, "OnPurchasedId");
            if (outerBundle)
            {
                _shop.BuyBundle(first);
            }
            else
            {
                _shop.Buy(_item);
            }

            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(balanceDuringNestedAttempt, Is.EqualTo(150f));
            Assert.That(nestedItemWasOwned, Is.False);
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.IsOwned(hat), Is.False);
            if (innerBundle)
            {
                _shop.BuyBundle(second);
                Assert.That(_shop.IsBundleOwned(second), Is.True);
            }
            else
            {
                _shop.Buy(hat);
            }

            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_shop.IsOwned(hat), Is.True);
        }

        [Test]
        public void NestedPurchase_FromBalanceCallback_IsRejectedBeforeTheOuterProfileCommit()
        {
            ShopItemData hat = CreateItem("hat", 50);
            _shop.SetItems(new[] { _item, hat });
            _money.Add(200f);
            UnityAction<float> nested = balance =>
            {
                if (balance == 150f)
                {
                    _shop.Buy(hat);
                }
            };
            _money.CurrentMoney.AddListener(nested);

            _shop.Buy(_item);

            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.IsOwned(hat), Is.False);
            _money.CurrentMoney.RemoveListener(nested);
            _shop.Buy(hat);
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_shop.IsOwned(hat), Is.True);
        }

        [Test]
        public void ThrowingFailureObserver_DoesNotLeaveThePurchaseGuardHeld()
        {
            _money.Add(200f);
            _provider.StringWritesToFail = 1;
            UnityAction<string> failure = _ => throw new InvalidOperationException("failure observer failed");
            _shop.OnPurchaseFailedId.AddListener(failure);
            ExpectRollback();

            Assert.Throws<InvalidOperationException>(() => _shop.Buy(_item));
            Assert.That(_money.money, Is.EqualTo(200f));
            Assert.That(_shop.IsOwned(_item), Is.False);
            _shop.OnPurchaseFailedId.RemoveListener(failure);
            _shop.Buy(_item);
            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_shop.IsOwned(_item), Is.True);
        }

        [Test]
        public void FaultReportingObserverThrows_DoesNotEscapeOrRefundACommittedPurchase()
        {
            _money.Add(200f);
            _shop.OnPurchasedId.AddListener(_ => throw new InvalidOperationException("observer failed"));
            _shop.OnNotificationFault.AddListener(_ => throw new InvalidOperationException("reporting failed"));
            ExpectObserverFault(_item.Id, "OnPurchasedId");
            LogAssert.Expect(LogType.Error, new Regex(@"\[Shop\] Notification-fault observer failed for 'sword'"));

            _shop.Buy(_item);

            Assert.That(_money.money, Is.EqualTo(150f));
            Assert.That(_shop.IsOwned(_item), Is.True);
            Assert.That(_shop.LastNotificationFault.Observer, Is.EqualTo("OnPurchasedId"));
            _shop.Buy(_item);
            Assert.That(_money.money, Is.EqualTo(150f), "An already-owned item is not charged again.");
        }

        [Test]
        public void ClearSelection_RemovesHighlightFromShopOwnedViews()
        {
            ShopItem row = CreateRow(out ButtonPrice price);
            SetPrivate(_shop, "_shopItems", new[] { row });
            _shop.SetRuntimePrice(_item, 0f);
            _shop.Buy(_item);
            Assert.That(price.CurrentType, Is.EqualTo(ButtonPrice.ButtonType.Selected));

            _shop.Select((ShopItemData)null);

            Assert.That(_shop.EquippedId, Is.Empty);
            Assert.That(price.CurrentType, Is.EqualTo(ButtonPrice.ButtonType.Select));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteSpend_IsInvalidAndLeavesTheWalletUntouched(float amount)
        {
            _money.Add(100f);
            MoneySpendResult result = _money.TrySpend(amount);

            Assert.That(result.Status, Is.EqualTo(MoneySpendStatus.RejectedInvalidAmount));
            Assert.That(_money.CanSpend(amount), Is.False);
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
        }

        [TestCase(-10f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidLevelPayout_DoesNotDebitOrCorruptTheMainWallet(float amount)
        {
            _money.Add(100f);
            _money.SetLevelMoney(amount);
            string reason = amount < 0f ? "negative amount" : "amount is not a finite number";
            LogAssert.Expect(LogType.Error, new Regex($@"\[Money\] SetMoneyForLevel rejected: {reason}"));

            float payout = _money.SetMoneyForLevel();

            Assert.That(payout, Is.Zero);
            Assert.That(_money.money, Is.EqualTo(100f));
            Assert.That(_money.allMoney, Is.EqualTo(100f));
            if (float.IsNaN(amount))
            {
                Assert.That(_money.levelMoney, Is.NaN);
            }
            else
            {
                Assert.That(_money.levelMoney, Is.EqualTo(amount));
            }
        }

        [Test]
        public void StartupLoad_NotifiesViewsThatSubscribedBeforeWalletStart()
        {
            SetPrivate(_money, "_persistMoney", true);
            SaveProvider.SetFloat(_money.SaveKey, 77f);
            SaveProvider.SetFloat(_money.SaveKey + nameof(Money.AllMoney), 90f);
            List<float> observed = new();
            _money.CurrentMoney.AddListener(observed.Add);

            InvokeLifecycle(_money, "Start");

            Assert.That(observed, Is.EqualTo(new[] { 77f }));
            Assert.That(_money.money, Is.EqualTo(77f));
            Assert.That(_money.allMoney, Is.EqualTo(90f));
        }

        [Test]
        public void Sanitize_RepairsMissingCollectionsAndKeepsProfileOperationsUsable()
        {
            ShopProfileData profile = new();
            SetPrivate(profile, "_ownedItemIds", null);
            SetPrivate(profile, "_ownedBundleIds", null);
            SetPrivate(profile, "_priceOverrides", null);

            profile.Sanitize();
            Assert.That(profile.TryAddOwnedItem("sword"), Is.True);
            Assert.That(profile.TryAddOwnedBundle("starter"), Is.True);
            profile.SetPriceOverride("sword", 7f);
            Assert.That(profile.IsItemOwned("sword"), Is.True);
            Assert.That(profile.IsBundleOwned("starter"), Is.True);
            Assert.That(profile.GetPriceOrDefault("sword", 50f), Is.EqualTo(7f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFinitePriceOverride_IsIgnoredAndRemovedDuringSanitize(float invalid)
        {
            _shop.SetRuntimePrice(_item, 7f);
            _shop.SetRuntimePrice(_item, invalid);
            Assert.That(_shop.GetPrice(_item), Is.EqualTo(7f));

            ShopProfileData profile = new();
            profile.PriceOverrides.Add(new ShopRuntimePriceEntry("sword", invalid));
            profile.Sanitize();
            Assert.That(profile.GetPriceOrDefault("sword", 50f), Is.EqualTo(50f));
            Assert.That(profile.PriceOverrides, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RestoreEquipment_RejectsSavedItemFromAnotherCategory(bool withSlot)
        {
            EquipmentManager manager = NewObject("Equipment").AddComponent<EquipmentManager>();
            EquipItemDefinition hat = CreateEquipItem("hat", "Head");
            EquipItemDefinition shoe = CreateEquipItem("shoe", "Feet");
            Texture2D texture = new(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            _objects.Add(texture);
            _objects.Add(sprite);
            SetPrivate(shoe, "_sprite", sprite);
            SetPrivate(manager, "_items", new[] { hat, shoe });
            SpriteRenderer target = NewObject("HeadSlot").AddComponent<SpriteRenderer>();
            SetPrivate(manager, "_slots", withSlot
                ? new[] { new EquipmentManager.CategorySlot { CategoryId = "Head", SpriteTarget = target } }
                : Array.Empty<EquipmentManager.CategorySlot>());
            SaveProvider.SetString("Equip_Head", "shoe");

            InvokeLifecycle(manager, "Start");

            Assert.That(manager.GetEquippedId("Head"), Is.Empty);
            Assert.That(manager.IsEquipped("shoe"), Is.False);
            if (withSlot)
            {
                Assert.That(target.sprite, Is.Null);
                Assert.That(target.enabled, Is.False);
            }
        }

        [Test]
        public void StateOnlyEquipment_ToleratesMissingCollectionsAndNullCategory()
        {
            EquipmentManager manager = NewObject("Equipment").AddComponent<EquipmentManager>();
            SetPrivate(manager, "_items", null);
            SetPrivate(manager, "_slots", null);
            EquipItemDefinition item = CreateEquipItem("fallback", null);

            manager.Equip(item);
            Assert.That(manager.GetEquippedId(null), Is.EqualTo(item.Id));
            manager.Equip(CreateEquipItem("", null));
            Assert.That(manager.GetEquippedId(null), Is.EqualTo(item.Id));
            manager.Unequip(null);
            Assert.That(manager.GetEquippedId(null), Is.Empty);
        }

        [Test]
        public void UnequipConfiguredSlot_EmitsOnlyOneStateChange()
        {
            EquipmentManager manager = NewObject("Equipment").AddComponent<EquipmentManager>();
            EquipItemDefinition hat = CreateEquipItem("hat", "Head");
            SetPrivate(manager, "_items", new[] { hat });
            SetPrivate(manager, "_slots", new[] { new EquipmentManager.CategorySlot { CategoryId = "Head" } });
            manager.Equip(hat);
            int cleared = 0;
            manager.OnEquipChanged.AddListener((category, id) => cleared++);

            manager.Unequip("Head");
            manager.Unequip("Head");

            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(manager.GetEquippedId("Head"), Is.Empty);
        }

        [Test]
        public void ReusedRow_UpdatesButtonOnBindingAndAfterSelectionVisuals()
        {
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopPurchaseButtonView view = CreatePurchaseView(row, price);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
            ShopItemData free = CreateItem("hat", 0);
            _shop.SetItems(new[] { _item, free });

            row.Visual(free, 0);
            row.Select(false);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Select));
            Assert.That(row.buttonBuy.interactable, Is.True);

            row.Visual(_item, 50);
            row.Select(false);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
            Assert.That(price.CurrentType, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
            Assert.That(row.buttonBuy.interactable, Is.False);
        }

        [Test]
        public void ChangeWalletForSameItem_RefreshesAndMovesBalanceSubscription()
        {
            _money.Add(100f);
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopPurchaseButtonView view = CreatePurchaseView(row, price);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Buy));
            Money replacement = CreateMoney("OtherCoins");

            _shop.SetMoneySpendSource(replacement.gameObject);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));

            replacement.Add(50f);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Buy));
            Assert.That(row.buttonBuy.interactable, Is.True);
            replacement.Spend(1f);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
        }

        [Test]
        public void FractionalPrice_IsNotTreatedAsFreeByThePurchaseButton()
        {
            _shop.SetRuntimePrice(_item, 0.25f);
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopPurchaseButtonView view = CreatePurchaseView(row, price);

            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
            Assert.That(row.buttonBuy.interactable, Is.False);
            _money.Add(0.25f);
            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Buy));
            Assert.That(row.buttonBuy.interactable, Is.True);
        }

        [Test]
        public void DisabledPurchaseView_ManualRefreshDoesNotResumeBalanceUpdates()
        {
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopPurchaseButtonView view = CreatePurchaseView(row, price);
            Disable(view);
            _money.Add(100f);

            view.Refresh();

            Assert.That(view.CurrentState, Is.EqualTo(ButtonPrice.ButtonType.Unaffordable));
            Assert.That(row.buttonBuy.interactable, Is.False);
        }

        [Test]
        public void ListReenabledWithoutRefreshing_ReconnectsExistingButtonActions()
        {
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopListView list = CreateList(row);
            list.SetButtonAction(ShopListButtonAction.Preview);
            SetPrivate(list, "_refreshOnEnable", false);
            InvokeLifecycle(list, "OnDisable");
            _shop.ShowPreview("");

            InvokeLifecycle(list, "OnEnable");
            row.buttonBuy.onClick.Invoke();

            Assert.That(_shop.PreviewIdString, Is.EqualTo(_item.Id));
        }

        [Test]
        public void DisabledList_SetShopDoesNotReconnectButtonsOrShopEvents()
        {
            ShopItem row = CreateRow(out ButtonPrice price);
            ShopListView list = CreateList(row);
            list.SetButtonAction(ShopListButtonAction.Preview);
            Disable(list);
            ShopBehaviour replacement = CreateShop();
            replacement.SetItems(new[] { _item });
            list.SetShop(replacement);
            replacement.ShowPreview("");
            int refreshes = 0;
            list.OnRefreshed.AddListener(() => refreshes++);

            replacement.RefreshVisuals();
            row.buttonBuy.onClick.Invoke();

            Assert.That(refreshes, Is.Zero);
            Assert.That(replacement.PreviewIdString, Is.Empty);
        }

        [Test]
        public void VariantsPanel_FollowsShopReplacementAndDetachesTheOldShop()
        {
            SetPrivate(_item, "_price", 0);
            SetPrivate(_item, "_isSinglePurchase", false);
            SetPrivate(_shop, "_purchaseFlow", ShopPurchaseFlow.BuyOnly);
            ShopListView list = CreateList();
            ShopVariantsPanel panel = NewObject("Variants").AddComponent<ShopVariantsPanel>();
            SetPrivate(panel, "_listView", list);
            Enable(panel);
            ShopBehaviour replacement = CreateShop();
            SetPrivate(replacement, "_purchaseFlow", ShopPurchaseFlow.BuyOnly);
            ShopItemData hat = CreateItem("hat", 0);
            replacement.SetItems(new[] { hat });
            list.SetShop(replacement);

            _shop.Buy(_item);
            Assert.That(replacement.EquippedId, Is.Empty, "The former shop must not drive the replacement.");
            replacement.Buy(hat);
            Assert.That(replacement.EquippedId, Is.EqualTo(hat.Id));

            Disable(panel);
            replacement.Select("");
            _shop.Buy(_item);
            Assert.That(replacement.EquippedId, Is.Empty, "Disable must also detach the original source.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledTextMoney_SourceSwitchWaitsUntilEnable(bool bySaveKey)
        {
            _money.Add(100f);
            Money replacement = CreateMoney("OtherCoins");
            replacement.Add(20f);
            TextMoney text = NewObject("MoneyText").AddComponent<TextMoney>();
            text.SetMoneySource(_money);
            Enable(text);
            Assert.That(text.amount, Is.EqualTo(100f));
            Disable(text);

            if (bySaveKey)
            {
                text.SetMoneySaveKey(replacement.SaveKey);
            }
            else
            {
                text.SetMoneySource(replacement);
            }

            replacement.Add(10f);
            Assert.That(text.amount, Is.EqualTo(100f));
            Enable(text);
            Assert.That(text.amount, Is.EqualTo(30f));
        }

        [Test]
        public void CategorySelector_NullEntryReportsEmptyAndNavigationStillWraps()
        {
            ShopCategorySelector selector = NewObject("Categories").AddComponent<ShopCategorySelector>();
            SetPrivate(selector, "_categories", new[]
            {
                new ShopCategorySelector.Category { Id = "weapons" }, null
            });
            selector.Next();
            Assert.That(selector.CurrentCategoryId, Is.Empty);
            selector.Next();
            Assert.That(selector.CurrentCategoryId, Is.EqualTo("weapons"));
            SetPrivate(selector, "_categories", null);
            selector.Next();
            selector.Prev();
            selector.Select("weapons");
            Assert.That(selector.CurrentCategoryId, Is.Empty);
        }

        private GameObject NewObject(string name)
        {
            GameObject created = new(name);
            _objects.Add(created);
            return created;
        }

        private Money CreateMoney(string key)
        {
            Money wallet = NewObject(key).AddComponent<Money>();
            SetPrivate(wallet, "_persistMoney", false);
            SetPrivate(wallet, "_moneySave", key);
            return wallet;
        }

        private ShopBehaviour CreateShop()
        {
            ShopBehaviour shop = NewObject("Shop").AddComponent<ShopBehaviour>();
            shop.AutoSpawnItems = false;
            shop.SetMoneySpendSource(_money.gameObject);
            return shop;
        }

        private ShopItemData CreateItem(string id, int price)
        {
            ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
            _objects.Add(item);
            item.AssignIdIfEmpty(id);
            SetPrivate(item, "_price", price);
            return item;
        }

        private EquipItemDefinition CreateEquipItem(string id, string category)
        {
            EquipItemDefinition item = ScriptableObject.CreateInstance<EquipItemDefinition>();
            _objects.Add(item);
            SetPrivate(item, "_id", id);
            SetPrivate(item, "_categoryId", category);
            return item;
        }

        private ShopBundleData CreateBundle(string id, params ShopItemData[] items)
        {
            ShopBundleData bundle = ScriptableObject.CreateInstance<ShopBundleData>();
            _objects.Add(bundle);
            SetPrivate(bundle, "_id", id);
            SetPrivate(bundle, "_bundlePrice", 50);
            SetPrivate(bundle, "_items", items);
            return bundle;
        }

        private ShopItem CreateRow(out ButtonPrice price)
        {
            GameObject created = NewObject("Row");
            ShopItem row = created.AddComponent<ShopItem>();
            row.buttonBuy = created.AddComponent<Button>();
            price = created.AddComponent<ButtonPrice>();
            SetPrivate(row, "buttonPrice", price);
            row.Visual(_item, Mathf.RoundToInt(_shop.GetPrice(_item)));
            return row;
        }

        private ShopPurchaseButtonView CreatePurchaseView(ShopItem row, ButtonPrice price)
        {
            ShopPurchaseButtonView view = row.gameObject.AddComponent<ShopPurchaseButtonView>();
            SetPrivate(view, "_shop", _shop);
            SetPrivate(view, "_item", row);
            SetPrivate(view, "_buttonPrice", price);
            SetPrivate(view, "_button", row.buttonBuy);
            Enable(view);
            return view;
        }

        private ShopListView CreateList(ShopItem row = null)
        {
            ShopListView list = NewObject("List").AddComponent<ShopListView>();
            SetPrivate(list, "_bindShopIfNull", false);
            if (row != null)
            {
                SetPrivate(list, "_views", new List<ShopItem> { row });
            }

            list.SetShop(_shop);
            Enable(list);
            return list;
        }

        private void Enable(MonoBehaviour component)
        {
            component.enabled = true;
            InvokeLifecycle(component, "OnEnable");
            if (!_subscriptions.Contains(component))
            {
                _subscriptions.Add(component);
            }
        }

        private static void Disable(MonoBehaviour component)
        {
            component.enabled = false;
            InvokeLifecycle(component, "OnDisable");
        }

        private static void SetPrivate(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Serialized setup field {name} is missing.");
            field.SetValue(target, value);
        }

        private static void InvokeLifecycle(MonoBehaviour target, string method)
        {
            MethodInfo callback = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null, $"Lifecycle callback {method} is missing.");
            callback.Invoke(target, null);
        }

        private static void ExpectRollback(string itemId = "sword")
        {
            LogAssert.Expect(LogType.Error, new Regex($@"\[Shop\] Purchase of '{itemId}' failed while committing profile"));
        }

        private static void ExpectObserverFault(string purchaseId, string observer)
        {
            LogAssert.Expect(LogType.Error,
                new Regex($@"\[Shop\] Observer '{observer}' failed after purchase '{purchaseId}'"));
        }
    }
}
