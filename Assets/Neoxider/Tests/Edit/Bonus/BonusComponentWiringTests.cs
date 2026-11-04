using System.Collections.Generic;
using System.Reflection;
using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Code-first wiring of Bonus components: <see cref="CollectionVisualManager" /> displays and gates selection
    ///     by an explicitly assigned Collection instead of the global singleton. UnityEvent presence on code-built
    ///     components is covered for the whole assembly by <see cref="CodeBuiltComponentEventsTests" />.
    /// </summary>
    public sealed class BonusComponentWiringTests
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
                for (int i = _owned.Count - 1; i >= 0; i--)
                {
                    if (_owned[i] != null)
                    {
                        Object.DestroyImmediate(_owned[i]);
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
        public void VisualManager_ExplicitCollection_DrawsItAndGatesSelectionWithoutTheSingleton()
        {
            Assume.That(Collection.IsInitialized, Is.False, "Needs no registered global Collection.");
            Collection local = NewLocalCollection(2);
            Assert.That(local.TryAddItem(1), Is.True);

            CollectionVisualManager manager = Own(new GameObject("ExplicitVisualManager"))
                .AddComponent<CollectionVisualManager>();
            ItemCollection first = Own(new GameObject("First")).AddComponent<ItemCollection>();
            ItemCollection second = Own(new GameObject("Second")).AddComponent<ItemCollection>();
            manager.SetItems(new[] { first, second });
            int selected = -1;
            manager.OnSetItem.AddListener(id => selected = id);

            Assert.That(manager.TargetCollection, Is.Null, "Without an assignment or a global Collection there is none.");
            manager.TargetCollection = local;
            manager.Visual();

            Assert.That(manager.TargetCollection, Is.SameAs(local));
            Assert.That(first.IsEnabled, Is.False);
            Assert.That(second.IsEnabled, Is.True, "The view reads the assigned collection.");
            Assert.That(second.ItemId, Is.EqualTo(1));

            manager.SetItem(0);
            Assert.That(selected, Is.EqualTo(-1), "A locked item of the assigned collection is not selectable.");
            manager.SetItem(1);
            Assert.That(selected, Is.EqualTo(1));

            manager.TargetCollection = null;
            Assert.That(manager.TargetCollection, Is.Null, "Clearing the assignment returns to the global fallback.");
        }

        [Test]
        public void VisualManager_SetItems_RewiresButtonsAndRedraws()
        {
            Assume.That(Collection.IsInitialized, Is.False, "Needs no registered global Collection.");
            Collection local = NewLocalCollection(2);
            Assert.That(local.TryAddItem(0), Is.True);
            Assert.That(local.TryAddItem(1), Is.True);
            CollectionVisualManager manager = NewPanelManager();
            ItemCollection oldView = NewView("OldView", out Button oldButton);
            ItemCollection newView = NewView("NewView", out Button newButton);
            manager.SetItems(new[] { oldView });
            manager.TargetCollection = local;
            InvokeLifecycle(manager, "Awake");
            InvokeLifecycle(manager, "Start");
            int selected = -1;
            int selections = 0;
            manager.OnSetItem.AddListener(id =>
            {
                selected = id;
                selections++;
            });

            oldButton.onClick.Invoke();
            Assert.That(selected, Is.Zero, "Awake wires the views assigned from code.");

            manager.SetItems(new[] { newView, oldView });
            Assert.That(newView.IsEnabled, Is.True, "Replacing the views redraws them from the bound collection.");
            Assert.That(newView.ItemId, Is.Zero);
            Assert.That(oldView.ItemId, Is.EqualTo(1));

            selections = 0;
            oldButton.onClick.Invoke();
            Assert.That(selected, Is.EqualTo(1), "A kept view is rewired for its new index.");
            newButton.onClick.Invoke();
            Assert.That(selected, Is.Zero);
            Assert.That(selections, Is.EqualTo(2), "Each button keeps exactly one listener.");

            manager.SetItems(null);
            selections = 0;
            oldButton.onClick.Invoke();
            newButton.onClick.Invoke();
            Assert.That(selections, Is.Zero, "Removed views are unwired.");
            Assert.That(manager.ItemsCount, Is.Zero);
        }

        [Test]
        public void ItemUnlock_OnANonGlobalPanel_WritesIntoThePanelsCollection()
        {
            Assume.That(Collection.IsInitialized, Is.False, "Needs no registered global Collection.");
            Collection trophies = NewLocalCollection(1);
            CollectionVisualManager manager = NewPanelManager();
            ItemCollection view = NewView("TrophyView", out _);
            manager.SetItems(new[] { view });
            manager.TargetCollection = trophies;
            manager.Visual();

            view.Unlock();

            Assert.That(trophies.HasItem(0), Is.True,
                "A view without its own Collection unlocks into the collection its manager displays.");
        }

        [Test]
        public void ItemUnlock_WithItsOwnCollection_IgnoresThePanelsCollection()
        {
            Collection panelCollection = NewLocalCollection(1);
            Collection ownCollection = NewLocalCollection(1);
            CollectionVisualManager manager = NewPanelManager();
            ItemCollection view = NewView("OwnView", out _);
            SetField(view, "_collection", ownCollection);
            manager.SetItems(new[] { view });
            manager.TargetCollection = panelCollection;
            manager.Visual();

            view.Unlock();

            Assert.That(ownCollection.HasItem(0), Is.True, "The view's assigned Collection wins.");
            Assert.That(panelCollection.HasItem(0), Is.False);
        }

        private CollectionVisualManager NewPanelManager()
        {
            CollectionVisualManager manager = Own(new GameObject("PanelVisualManager"))
                .AddComponent<CollectionVisualManager>();
            FieldInfo registersGlobally = typeof(Neo.Tools.Singleton<CollectionVisualManager>).GetField(
                "_setInstanceOnAwake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(registersGlobally, Is.Not.Null);
            registersGlobally.SetValue(manager, false);
            return manager;
        }

        private ItemCollection NewView(string name, out Button button)
        {
            GameObject host = Own(new GameObject(name, typeof(RectTransform)));
            button = host.AddComponent<Button>();
            ItemCollection view = host.AddComponent<ItemCollection>();
            SetField(view, "_button", button);
            return view;
        }

        private static void InvokeLifecycle(Component component, string method)
        {
            MethodInfo info = component.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(component, null);
        }

        private Collection NewLocalCollection(int itemCount)
        {
            GameObject host = Own(new GameObject("LocalCollection"));
            Collection collection = host.AddComponent<Collection>();
            ItemCollectionData[] items = new ItemCollectionData[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                items[i] = Own(ScriptableObject.CreateInstance<ItemCollectionData>());
            }

            collection.SaveKeyPrefix = "BonusComponentWiring_";
            collection.ItemCollectionDatas = items;
            collection.Load();
            return collection;
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
