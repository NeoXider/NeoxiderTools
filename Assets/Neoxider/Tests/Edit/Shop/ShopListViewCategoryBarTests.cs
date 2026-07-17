using System.Reflection;
using Neo.Shop;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using ShopBehaviour = Neo.Shop.Shop;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the CategoryBar → ShopListView adapter: category application on selection and
    ///     auto-building the bar's entries from the Shop catalog (distinct categories + All entry).
    /// </summary>
    [TestFixture]
    public class ShopListViewCategoryBarTests
    {
        private GameObject _root;
        private ShopBehaviour _shop;
        private ShopListView _listView;
        private CategoryBar _bar;
        private ShopListViewCategoryBar _adapter;
        private ShopItemData _hat;
        private ShopItemData _boots;
        private ShopItemData _hat2;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("ShopListViewCategoryBarTests");
            _shop = _root.AddComponent<ShopBehaviour>();

            _hat = CreateItem("hat", "Hats");
            _hat2 = CreateItem("hat2", "Hats");
            _boots = CreateItem("boots", "Boots");
            _shop.SetItems(new[] { _hat, _boots, _hat2 });

            _listView = _root.AddComponent<ShopListView>();
            SetPrivate(_listView, "_shop", _shop);

            _bar = _root.AddComponent<CategoryBar>();
            _bar.Initialize();

            _adapter = _root.AddComponent<ShopListViewCategoryBar>();
            SetPrivate(_adapter, "_categoryBar", _bar);
            SetPrivate(_adapter, "_listView", _listView);
        }

        [TearDown]
        public void TearDown()
        {
            InvokeLifecycle(_adapter, "OnDisable");
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_hat);
            Object.DestroyImmediate(_hat2);
            Object.DestroyImmediate(_boots);
        }

        private static ShopItemData CreateItem(string id, string category)
        {
            var data = ScriptableObject.CreateInstance<ShopItemData>();
            SetPrivate(data, "_id", id);
            SetPrivate(data, "_category", category);
            return data;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{field} field expected");
            info.SetValue(target, value);
        }

        private static void InvokeLifecycle(Component component, string method)
        {
            MethodInfo info = component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{component.GetType().Name}.{method} expected");
            info.Invoke(component, null);
        }

        [Test]
        public void Selection_DrivesListViewCategory()
        {
            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "", DisplayName = "All" },
                new CategoryBar.Entry { Id = "Hats", DisplayName = "Hats" }
            });
            InvokeLifecycle(_adapter, "OnEnable");

            _bar.Select("Hats");

            Assert.AreEqual("Hats", _listView.Category);
        }

        [Test]
        public void BuildCategoriesFromShop_CreatesAllPlusDistinctCategories()
        {
            SetPrivate(_adapter, "_buildCategoriesFromShop", true);
            InvokeLifecycle(_adapter, "OnEnable");

            Assert.AreEqual(3, _bar.Categories.Count, "All + Hats + Boots (duplicates collapse).");
            Assert.AreEqual("", _bar.Categories[0].Id);
            Assert.AreEqual("Hats", _bar.Categories[1].Id);
            Assert.AreEqual("Boots", _bar.Categories[2].Id);
            Assert.AreEqual("", _bar.CurrentCategoryId, "The show-all entry starts selected.");
        }

        [Test]
        public void BuildCategoriesFromShop_WithoutAllEntry()
        {
            SetPrivate(_adapter, "_buildCategoriesFromShop", true);
            SetPrivate(_adapter, "_includeAllEntry", false);
            InvokeLifecycle(_adapter, "OnEnable");

            Assert.AreEqual(2, _bar.Categories.Count);
            Assert.AreEqual("Hats", _bar.CurrentCategoryId);
            Assert.AreEqual("Hats", _listView.Category, "Initial selection is applied to the list view.");
        }
    }
}
