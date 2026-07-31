using System.Collections.Generic;
using System.Reflection;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the generic <see cref="CategoryBar"/>: initial selection, selection by index/id,
    ///     wrap and non-wrap Next/Prev navigation, disabled entries, runtime category lists, and the
    ///     id/index events. Item views stay absent — selection logic must tolerate a view-less setup.
    /// </summary>
    [TestFixture]
    public class CategoryBarTests
    {
        private GameObject _go;
        private CategoryBar _bar;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CategoryBarTests");
            _bar = _go.AddComponent<CategoryBar>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static CategoryBar.Entry Entry(string id, bool disabled = false)
        {
            return new CategoryBar.Entry { Id = id, DisplayName = id, Disabled = disabled };
        }

        private void Configure(int startIndex, params CategoryBar.Entry[] entries)
        {
            FieldInfo categories = typeof(CategoryBar).GetField("_categories",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo start = typeof(CategoryBar).GetField("_startIndex",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(categories, "CategoryBar._categories field expected");
            Assert.IsNotNull(start, "CategoryBar._startIndex field expected");

            categories.SetValue(_bar, new List<CategoryBar.Entry>(entries));
            start.SetValue(_bar, startIndex);
            _bar.Initialize();
        }

        private void SetWrap(bool wrap)
        {
            FieldInfo field = typeof(CategoryBar).GetField("_wrapNavigation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "CategoryBar._wrapNavigation field expected");
            field.SetValue(_bar, wrap);
        }

        [Test]
        public void Initialize_SelectsStartIndex()
        {
            Configure(1, Entry("all"), Entry("hats"), Entry("shoes"));

            Assert.AreEqual(1, _bar.CurrentIndex);
            Assert.AreEqual("hats", _bar.CurrentCategoryId);
        }

        [Test]
        public void Initialize_DisabledStartIndex_FallsToNextEnabled()
        {
            Configure(0, Entry("all", disabled: true), Entry("hats"));

            Assert.AreEqual(1, _bar.CurrentIndex);
        }

        [Test]
        public void Initialize_AllDisabled_KeepsNoSelection()
        {
            Configure(0, Entry("all", disabled: true), Entry("hats", disabled: true));

            Assert.AreEqual(-1, _bar.CurrentIndex);
            Assert.AreEqual("", _bar.CurrentCategoryId);
        }

        [Test]
        public void Select_ByIndex_RaisesEventsOnce()
        {
            Configure(0, Entry("all"), Entry("hats"));

            int indexEvents = 0;
            string lastId = null;
            _bar.OnCategorySelected.AddListener(_ => indexEvents++);
            _bar.OnCategoryIdSelected.AddListener(id => lastId = id);

            Assert.IsTrue(_bar.Select(1));
            Assert.AreEqual(1, indexEvents);
            Assert.AreEqual("hats", lastId);

            Assert.IsTrue(_bar.Select(1), "Re-selecting the current entry succeeds...");
            Assert.AreEqual(1, indexEvents, "...but does not raise the event again.");
        }

        [Test]
        public void Select_DisabledOrOutOfRange_Fails()
        {
            Configure(0, Entry("all"), Entry("hats", disabled: true));

            Assert.IsFalse(_bar.Select(1), "Disabled entry cannot be selected.");
            Assert.IsFalse(_bar.Select(5));
            Assert.IsFalse(_bar.Select(-1));
            Assert.IsFalse(_bar.Select("missing"));
            Assert.AreEqual(0, _bar.CurrentIndex);
        }

        [Test]
        public void Select_ById_Works()
        {
            Configure(0, Entry("all"), Entry("hats"), Entry("shoes"));

            Assert.IsTrue(_bar.Select("shoes"));
            Assert.AreEqual(2, _bar.CurrentIndex);
        }

        [Test]
        public void Next_SkipsDisabledAndWraps()
        {
            Configure(0, Entry("all"), Entry("hats", disabled: true), Entry("shoes"));

            _bar.Next();
            Assert.AreEqual("shoes", _bar.CurrentCategoryId, "Disabled entry is skipped.");

            _bar.Next();
            Assert.AreEqual("all", _bar.CurrentCategoryId, "Next past the end wraps to the first entry.");
        }

        [Test]
        public void Prev_WrapsToLastEnabled()
        {
            Configure(0, Entry("all"), Entry("hats"), Entry("shoes", disabled: true));

            _bar.Prev();
            Assert.AreEqual("hats", _bar.CurrentCategoryId,
                "Prev from the first entry wraps and skips the disabled tail.");
        }

        [Test]
        public void Navigation_WithoutWrap_StopsAtEnds()
        {
            Configure(0, Entry("all"), Entry("hats"));
            SetWrap(false);

            _bar.Prev();
            Assert.AreEqual(0, _bar.CurrentIndex, "Prev at the first entry is a no-op without wrap.");

            _bar.Next();
            _bar.Next();
            Assert.AreEqual(1, _bar.CurrentIndex, "Next at the last entry is a no-op without wrap.");
        }

        [Test]
        public void SetCategories_ReplacesListAndKeepsSelectionById()
        {
            Configure(0, Entry("all"), Entry("hats"));
            _bar.Select("hats");

            _bar.SetCategories(new[] { Entry("shoes"), Entry("hats") });

            Assert.AreEqual("hats", _bar.CurrentCategoryId, "Selection is preserved by id after the swap.");
            Assert.AreEqual(1, _bar.CurrentIndex);
        }

        [Test]
        public void SetCategories_WithInitialIndex_SelectsIt()
        {
            Configure(0, Entry("all"));

            _bar.SetCategories(new[] { Entry("a"), Entry("b"), Entry("c") }, 2);

            Assert.AreEqual("c", _bar.CurrentCategoryId);
        }

        [Test]
        public void SetCategories_MissingPreviousId_SelectsFirstEnabled()
        {
            Configure(0, Entry("all"));

            _bar.SetCategories(new[] { Entry("x", disabled: true), Entry("y") });

            Assert.AreEqual("y", _bar.CurrentCategoryId);
        }

        [Test]
        public void SetEntryDisabled_BlocksSelection()
        {
            Configure(0, Entry("all"), Entry("hats"));

            _bar.SetEntryDisabled(1, true);
            Assert.IsFalse(_bar.Select(1));

            _bar.SetEntryDisabled(1, false);
            Assert.IsTrue(_bar.Select(1));
        }
    }
}
