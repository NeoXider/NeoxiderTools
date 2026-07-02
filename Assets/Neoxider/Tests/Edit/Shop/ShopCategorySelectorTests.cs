using System.Reflection;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the 9.7.0 category pill: wrap-around Next/Prev cycling and Select-by-id.
    ///     UI references stay null — Apply() must tolerate a text/icon/list-view-less setup.
    /// </summary>
    [TestFixture]
    public class ShopCategorySelectorTests
    {
        private GameObject _go;
        private ShopCategorySelector _selector;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ShopCategorySelectorTests");
            _selector = _go.AddComponent<ShopCategorySelector>();

            var categories = new[]
            {
                new ShopCategorySelector.Category { Id = "all", DisplayName = "All" },
                new ShopCategorySelector.Category { Id = "hats", DisplayName = "Hats" },
                new ShopCategorySelector.Category { Id = "shoes", DisplayName = "Shoes" }
            };

            FieldInfo field = typeof(ShopCategorySelector).GetField("_categories",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "ShopCategorySelector._categories field expected");
            field.SetValue(_selector, categories);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void CurrentCategoryId_DefaultsToFirstCategory()
        {
            Assert.AreEqual("all", _selector.CurrentCategoryId);
        }

        [Test]
        public void Next_CyclesForwardAndWraps()
        {
            _selector.Next();
            Assert.AreEqual("hats", _selector.CurrentCategoryId);

            _selector.Next();
            Assert.AreEqual("shoes", _selector.CurrentCategoryId);

            _selector.Next();
            Assert.AreEqual("all", _selector.CurrentCategoryId, "Next past the end wraps to the first category");
        }

        [Test]
        public void Prev_WrapsToLastCategory()
        {
            _selector.Prev();
            Assert.AreEqual("shoes", _selector.CurrentCategoryId);
        }

        [Test]
        public void Select_JumpsToCategoryById()
        {
            _selector.Select("hats");
            Assert.AreEqual("hats", _selector.CurrentCategoryId);
        }

        [Test]
        public void Select_UnknownId_KeepsCurrentCategory()
        {
            _selector.Select("hats");
            _selector.Select("does-not-exist");

            Assert.AreEqual("hats", _selector.CurrentCategoryId);
        }

        [Test]
        public void EmptyCategoryList_IsSafe()
        {
            FieldInfo field = typeof(ShopCategorySelector).GetField("_categories",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(_selector, new ShopCategorySelector.Category[0]);

            Assert.DoesNotThrow(() =>
            {
                _selector.Next();
                _selector.Prev();
                _selector.Select("hats");
            });
            Assert.AreEqual("", _selector.CurrentCategoryId);
        }
    }
}
