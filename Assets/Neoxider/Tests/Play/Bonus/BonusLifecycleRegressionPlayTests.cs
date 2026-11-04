using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play Mode regressions for Bonus lifecycle fixes: a reusable wheel stays interactable after a spin, a wheel
    ///     whose graphic is destroyed mid-spin goes idle quietly, a reel spun before its Awake still settles, and the
    ///     legacy TimeReward timer keeps its running/paused state across deactivation and honours controls called
    ///     while inactive. Collection cases cover non-global collections loading in Awake and one
    ///     CollectionVisualManager per panel with explicit, reassigned or destroyed collections.
    /// </summary>
    public sealed class BonusLifecycleRegressionPlayTests
    {
        private readonly List<Object> _owned = new();
        private ISaveProvider _previousProvider;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            _previousTimeScale = Time.timeScale;
            SaveProvider.SetProvider(new ShopPurchasePlayModeTests.MemorySaveProvider());
            Time.timeScale = 1f;
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
                Time.timeScale = _previousTimeScale;
                if (_previousProvider != null)
                {
                    SaveProvider.SetProvider(_previousProvider);
                    _previousProvider = null;
                }
            }
        }

        [UnityTest]
        public IEnumerator ReusableWheel_AfterASpin_KeepsItsCanvasGroupInteractable()
        {
            WheelFortune wheel = NewWheel(out CanvasGroup group, out _);
            SetField(wheel, "_singleUse", false);
            int results = 0;
            wheel.OnWinIdVariant.AddListener(_ => results++);

            for (int spin = 0; spin < 2; spin++)
            {
                wheel.Spin();
                Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Spinning));
                wheel.Stop();
                yield return WaitUntilIdle(wheel);
                Assert.That(group.interactable, Is.True, "A reusable wheel must not lock its own UI.");
                Assert.That(wheel.canUse, Is.True);
            }

            Assert.That(results, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SingleUseWheel_AfterASpin_StillConsumesItsUse()
        {
            WheelFortune wheel = NewWheel(out CanvasGroup group, out _);
            SetField(wheel, "_singleUse", true);

            wheel.Spin();
            wheel.Stop();
            yield return WaitUntilIdle(wheel);

            Assert.That(wheel.canUse, Is.False);
            Assert.That(group.interactable, Is.False);
            wheel.Spin();
            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Idle));
        }

        [UnityTest]
        public IEnumerator Wheel_GraphicDestroyedMidSpin_GoesIdleWithoutAResult()
        {
            WheelFortune wheel = NewWheel(out _, out RectTransform wheelTransform);
            SetField(wheel, "_singleUse", false);
            SetField(wheel, "_angularDeceleration", 1f);
            int results = 0;
            wheel.OnWinIdVariant.AddListener(_ => results++);

            wheel.Spin();
            yield return null;
            Object.Destroy(wheelTransform.gameObject);
            yield return null;
            yield return null;

            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Idle));
            Assert.That(results, Is.Zero);
        }

        [UnityTest]
        public IEnumerator VisualManager_HiddenBeforeItsCollectionExists_SubscribesWhenShownAgain()
        {
            Assume.That(Collection.IsInitialized, Is.False, "Needs a scene without a Collection.");
            GameObject managerHost = Own(new GameObject("CollectionVisualManager"));
            CollectionVisualManager manager = managerHost.AddComponent<CollectionVisualManager>();
            ItemCollection view = Own(new GameObject("ItemView")).AddComponent<ItemCollection>();
            manager.SetItems(new[] { view });
            yield return null;

            managerHost.SetActive(false);
            GameObject collectionHost = Own(new GameObject("Collection"));
            Collection collection = collectionHost.AddComponent<Collection>();
            collection.SaveKeyPrefix = "BonusLifecycleRegressionVisual_";
            collection.ItemCollectionDatas = new[] { Own(ScriptableObject.CreateInstance<ItemCollectionData>()) };
            collection.Load();
            yield return null;

            managerHost.SetActive(true);
            yield return null;

            Assert.That(collection.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.True, "The manager must react to unlocks after it was shown again.");
        }

        [UnityTest]
        public IEnumerator VisualManager_ExplicitCollection_FollowsARuntimeReassignment()
        {
            Collection first = NewLocalCollection("FirstLocalCollection", "BonusLifecycleExplicitA_");
            Collection second = NewLocalCollection("SecondLocalCollection", "BonusLifecycleExplicitB_");
            GameObject managerHost = Own(new GameObject("ExplicitCollectionVisualManager"));
            managerHost.SetActive(false);
            CollectionVisualManager manager = managerHost.AddComponent<CollectionVisualManager>();
            ItemCollection view = Own(new GameObject("ExplicitItemView")).AddComponent<ItemCollection>();
            manager.SetItems(new[] { view });
            manager.TargetCollection = first;
            managerHost.SetActive(true);
            yield return null;

            Assert.That(manager.TargetCollection, Is.SameAs(first));
            Assert.That(first.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.True, "The manager reacts to its assigned, non-global collection.");

            manager.TargetCollection = second;
            Assert.That(view.IsEnabled, Is.False, "Reassigning redraws from the new collection.");
            first.RemoveItem(0);
            Assert.That(first.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.False, "The previous collection no longer drives the view.");
            Assert.That(second.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.True, "The manager subscribes to the new collection.");

#if UNITY_EDITOR
            SetField(manager, "_collection", first);
            InvokePrivate(manager, "OnValidate");
            Assert.That(manager.TargetCollection, Is.SameAs(second),
                "OnValidate only queues the rebind; redrawing inside it would trip Unity's SetActive guard.");
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!ReferenceEquals(manager.TargetCollection, first) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(manager.TargetCollection, Is.SameAs(first), "An Inspector edit in Play Mode is applied too.");
            first.RemoveItem(0);
            Assert.That(view.IsEnabled, Is.False, "The manager follows the collection picked in the Inspector.");
            second.RemoveItem(0);
            Assert.That(second.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.False, "The collection replaced in the Inspector no longer drives the view.");
#endif
        }

        [UnityTest]
        public IEnumerator VisualManager_DestroyedAssignmentCleared_FollowsTheGlobalCollection()
        {
            Assume.That(Collection.HasInstance, Is.False, "Needs a scene without a global Collection.");
            Collection local = NewLocalCollection("DoomedLocalCollection", "BonusLifecycleDoomed_");
            CollectionVisualManager manager = NewPanel("FallbackPanel", local, false, out _, out ItemCollection view);
            yield return null;
            Assert.That(manager.TargetCollection, Is.SameAs(local));

            Collection global = NewCollection("GlobalCollection", "BonusLifecycleGlobal_", true);
            Object.DestroyImmediate(local.gameObject);
            manager.TargetCollection = null;

            Assert.That(manager.TargetCollection, Is.SameAs(global));
            Assert.That(global.TryAddItem(0), Is.True);
            Assert.That(view.IsEnabled, Is.True,
                "Clearing an assignment that was destroyed must subscribe the manager to Collection.I.");
        }

        [UnityTest]
        public IEnumerator VisualManagers_OnePerPanel_TheNonSingletonOneKeepsItsPanelAndWiresItsButtons()
        {
            Assume.That(CollectionVisualManager.HasInstance, Is.False, "Needs a scene without a registered manager.");
            Collection stickers = NewLocalCollection("Stickers", "BonusLifecycleStickers_");
            Collection trophies = NewLocalCollection("Trophies", "BonusLifecycleTrophies_");
            CollectionVisualManager stickersPanel = NewPanel("StickersPanel", stickers, true, out Button stickerButton, out _);
            CollectionVisualManager trophiesPanel = NewPanel("TrophiesPanel", trophies, false, out Button trophyButton, out _);
            int stickerSelected = -1;
            int trophySelected = -1;
            stickersPanel.OnSetItem.AddListener(id => stickerSelected = id);
            trophiesPanel.OnSetItem.AddListener(id => trophySelected = id);
            yield return null;

            Assert.That(trophiesPanel != null, Is.True, "A manager with Set Instance On Awake off keeps its GameObject.");
            Assert.That(CollectionVisualManager.TryGetInstance(out CollectionVisualManager registered), Is.True);
            Assert.That(registered, Is.SameAs(stickersPanel));

            trophyButton.onClick.Invoke();
            Assert.That(trophySelected, Is.EqualTo(-1), "A locked trophy is not selectable.");
            Assert.That(trophies.TryAddItem(0), Is.True);
            trophyButton.onClick.Invoke();
            Assert.That(trophySelected, Is.EqualTo(0), "The non-singleton manager wires its own item buttons.");

            stickerButton.onClick.Invoke();
            Assert.That(stickerSelected, Is.EqualTo(-1), "Each panel gates by its own collection.");
            Assert.That(stickers.TryAddItem(0), Is.True);
            stickerButton.onClick.Invoke();
            Assert.That(stickerSelected, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator NonGlobalCollection_LoadsItsSavedUnlocksInAwake()
        {
            const string prefix = "BonusLifecycleNonGlobalLoad_";
            SaveProvider.SetInt(CollectionSet.GetItemSaveKey(prefix, 1), 1);
            GameObject host = Own(new GameObject("NonGlobalCollection"));
            host.SetActive(false);
            Collection collection = host.AddComponent<Collection>();
            SetRegistersGlobally(collection, false);
            collection.SaveKeyPrefix = prefix;
            collection.ItemCollectionDatas = new[] { NewItemData(), NewItemData() };
            int loads = 0;
            collection.OnLoadItems.AddListener(() => loads++);

            host.SetActive(true);
            yield return null;

            Assert.That(Collection.TryGetInstance(out Collection registered) && ReferenceEquals(registered, collection),
                Is.False, "A non-global collection stays out of Collection.I.");
            Assert.That(loads, Is.EqualTo(1), "Awake loads it exactly once.");
            Assert.That(collection.HasItem(1), Is.True, "Saved unlocks are read without a manual Load().");
            Assert.That(collection.HasItem(0), Is.False);
            Assert.That(collection.TryAddItem(0), Is.True, "The loaded state accepts new unlocks.");
        }

        [UnityTest]
        public IEnumerator LineRoulett_UnscaledTime_FinishesWhileTheGameIsPaused()
        {
            GameObject root = Own(new GameObject("LineRoulett", typeof(RectTransform)));
            LineRoulett roulette = root.AddComponent<LineRoulett>();
            Image[] images = new Image[3];
            for (int i = 0; i < images.Length; i++)
            {
                GameObject cell = new("Cell" + i, typeof(RectTransform));
                cell.transform.SetParent(root.transform, false);
                cell.transform.position = new Vector3(i, 0f, 0f);
                images[i] = cell.AddComponent<Image>();
            }

            GameObject arrow = new("Arrow", typeof(RectTransform));
            arrow.transform.SetParent(root.transform, false);
            Sprite sprite = Own(Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.zero));
            SetField(roulette, "arrow", arrow.transform);
            SetField(roulette, "images", images);
            SetField(roulette, "timeRoll", 0.05f);
            SetField(roulette, "slowDownTime", 0.05f);
            SetField(roulette, "speed", 1f);
            roulette.sprites = new[] { sprite };
            roulette.UseUnscaledTime = true;
            roulette.OnWin = new UnityEvent<int>();
            int winningId = -2;
            roulette.OnWin.AddListener(id => winningId = id);
            yield return null;

            Time.timeScale = 0f;
            roulette.StartRolling();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (winningId == -2 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(winningId, Is.Zero, "The roulette reports its sprite id at Time.timeScale = 0.");
        }

        [UnityTest]
        public IEnumerator Row_SpunBeforeAwakeUnderAnInactiveParent_FinishesItsSpinOnceActivated()
        {
            GameObject parent = Own(new GameObject("InactiveReelParent"));
            parent.SetActive(false);
            GameObject rowObject = new("Row");
            rowObject.transform.SetParent(parent.transform, false);
            Row row = rowObject.AddComponent<Row>();
            row.countSlotElement = 3;
            row.spaceY = 100f;
            row.offsetY = 0f;
            row.speedControll = new SpeedControll { speed = 8000f, timeSpin = 0.05f };
            for (int i = 0; i < 6; i++)
            {
                GameObject element = new("El" + i, typeof(RectTransform));
                element.transform.SetParent(rowObject.transform, false);
                element.AddComponent<SlotElement>();
            }

            SpritesData sprites = Own(ScriptableObject.CreateInstance<SpritesData>());
            SetField(sprites, "_visuals", new[]
            {
                new SlotVisualData { id = 0 },
                new SlotVisualData { id = 1 },
                new SlotVisualData { id = 2 }
            });
            int stops = 0;
            row.OnStop.AddListener(() => stops++);

            row.Spin(sprites, new[] { 2, 1, 0 });
            Assert.That(row.is_spinning, Is.True, "A reel spun before Awake lays itself out and spins.");

            parent.SetActive(true);
            Assert.That(row.is_spinning, Is.True, "Awake must keep a spin that started before it.");

            float deadline = Time.realtimeSinceStartup + 5f;
            while (row.is_spinning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(row.is_spinning, Is.False, "The reel settles once it is active.");
            Assert.That(stops, Is.EqualTo(1), "Settling raises OnStop exactly once.");
            SlotElement[] bottomUp = row.GetVisibleBottomUp();
            Assert.That(bottomUp.Length, Is.EqualTo(3));
            Assert.That(bottomUp[0].id, Is.EqualTo(2));
            Assert.That(bottomUp[1].id, Is.EqualTo(1));
            Assert.That(bottomUp[2].id, Is.EqualTo(0), "The planned symbols land in the window.");
        }

#pragma warning disable CS0618
        [UnityTest]
        public IEnumerator LegacyTimeReward_DeactivatedAndReactivated_SuspendsTicksAndKeepsItsState()
        {
            GameObject host = Own(new GameObject("LegacyTimeReward"));
            TimeReward reward = host.AddComponent<TimeReward>();
            reward.updateTime = 0f;
            int ticks = 0;
            reward.OnTimeUpdated.AddListener(_ => ticks++);
            yield return null;

            Assert.That(reward.IsTimerRunning, Is.True, "Start begins the timer by default.");

            host.SetActive(false);
            Assert.That(reward.IsTimerRunning, Is.True, "Deactivation suspends the loop but keeps the timer running.");
            ticks = 0;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(ticks, Is.Zero, "An inactive timer does not tick.");

            host.SetActive(true);
            ticks = 0;
            yield return WaitForTick(() => ticks);
            Assert.That(ticks, Is.GreaterThan(0), "Ticking resumes after reactivation.");

            host.SetActive(false);
            reward.PauseTime();
            Assert.That(reward.IsTimerPaused, Is.True, "Pausing works while the object is inactive.");
            host.SetActive(true);
            Assert.That(reward.IsTimerRunning, Is.True);
            Assert.That(reward.IsTimerPaused, Is.True, "A timer paused while inactive comes back paused.");
            ticks = 0;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(ticks, Is.Zero, "A paused timer does not tick.");
        }

        [UnityTest]
        public IEnumerator LegacyTimeReward_StopAndStartWhileInactive_AreHonouredOnReactivation()
        {
            GameObject host = Own(new GameObject("LegacyTimeRewardControls"));
            TimeReward reward = host.AddComponent<TimeReward>();
            reward.updateTime = 0f;
            int ticks = 0;
            int stopped = 0;
            reward.OnTimeUpdated.AddListener(_ => ticks++);
            reward.OnTimerStopped.AddListener(() => stopped++);
            yield return null;

            host.SetActive(false);
            reward.StopTime();
            Assert.That(stopped, Is.EqualTo(1), "Stopping a running timer while inactive raises OnTimerStopped.");
            host.SetActive(true);
            Assert.That(reward.IsTimerRunning, Is.False, "A timer stopped while inactive stays stopped.");
            ticks = 0;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(ticks, Is.Zero, "A stopped timer does not tick.");

            host.SetActive(false);
            reward.StartTime();
            Assert.That(reward.IsTimerRunning, Is.True, "StartTime on an inactive object is deferred, not lost.");
            host.SetActive(true);
            ticks = 0;
            yield return WaitForTick(() => ticks);
            Assert.That(ticks, Is.GreaterThan(0), "A timer started while inactive ticks once activated.");

            reward.StopTime();
            reward.enabled = false;
            reward.StartTime();
            reward.enabled = true;
            Assert.That(reward.IsTimerRunning, Is.True);
            ticks = 0;
            yield return WaitForTick(() => ticks);
            Assert.That(ticks, Is.GreaterThan(0), "A timer started while the component was disabled ticks once enabled.");
        }
#pragma warning restore CS0618

        private static IEnumerator WaitForTick(Func<int> ticks)
        {
            float deadline = Time.realtimeSinceStartup + 2f;
            while (ticks() == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private Collection NewLocalCollection(string name, string savePrefix)
        {
            return NewCollection(name, savePrefix, false);
        }

        /// <summary>One-item collection configured while inactive; activation runs its own Awake load.</summary>
        private Collection NewCollection(string name, string savePrefix, bool registersGlobally)
        {
            GameObject host = Own(new GameObject(name));
            host.SetActive(false);
            Collection collection = host.AddComponent<Collection>();
            SetRegistersGlobally(collection, registersGlobally);
            collection.SaveKeyPrefix = savePrefix;
            collection.ItemCollectionDatas = new[] { NewItemData() };
            host.SetActive(true);
            return collection;
        }

        private CollectionVisualManager NewPanel(string name, Collection collection, bool registersGlobally,
            out Button button, out ItemCollection view)
        {
            GameObject host = Own(new GameObject(name));
            host.SetActive(false);
            CollectionVisualManager manager = host.AddComponent<CollectionVisualManager>();
            SetRegistersGlobally(manager, registersGlobally);
            GameObject viewHost = Own(new GameObject(name + "Item", typeof(RectTransform)));
            button = viewHost.AddComponent<Button>();
            view = viewHost.AddComponent<ItemCollection>();
            SetField(view, "_button", button);
            manager.SetItems(new[] { view });
            manager.TargetCollection = collection;
            host.SetActive(true);
            return manager;
        }

        private ItemCollectionData NewItemData()
        {
            return Own(ScriptableObject.CreateInstance<ItemCollectionData>());
        }

        private static void SetRegistersGlobally<T>(T component, bool registersGlobally) where T : Neo.Tools.Singleton<T>
        {
            FieldInfo field = typeof(Neo.Tools.Singleton<T>).GetField("_setInstanceOnAwake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(component, registersGlobally);
        }

        private static void InvokePrivate(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }

        private WheelFortune NewWheel(out CanvasGroup group, out RectTransform wheelTransform)
        {
            GameObject root = Own(new GameObject("Wheel", typeof(RectTransform)));
            group = root.AddComponent<CanvasGroup>();
            WheelFortune wheel = root.AddComponent<WheelFortune>();

            GameObject wheelObject = new("WheelGraphic", typeof(RectTransform));
            wheelObject.transform.SetParent(root.transform, false);
            wheelTransform = wheelObject.GetComponent<RectTransform>();

            GameObject prize = new("Prize", typeof(RectTransform));
            prize.transform.SetParent(wheelObject.transform, false);

            SetField(wheel, "_canvasGroup", group);
            SetField(wheel, "_wheelTransform", wheelTransform);
            SetField(wheel, "items", new[] { prize });
            SetField(wheel, "_autoStopTime", 0f);
            SetField(wheel, "_enableAlignment", false);
            SetField(wheel, "_initialAngularVelocity", 100f);
            SetField(wheel, "_angularDeceleration", 10000f);
            wheel.canUse = true;
            return wheel;
        }

        private static IEnumerator WaitUntilIdle(WheelFortune wheel)
        {
            float deadline = Time.realtimeSinceStartup + 2f;
            while (wheel.State != WheelFortune.SpinState.Idle && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(wheel.State, Is.EqualTo(WheelFortune.SpinState.Idle), "The wheel settles within two seconds.");
        }

        private T Own<T>(T owned) where T : Object
        {
            _owned.Add(owned);
            return owned;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
