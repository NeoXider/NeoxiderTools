using System.Collections.Generic;
using System.Reflection;
using Neo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     Regressions of the 2026-11 UI quality pass: CategoryBar selection made before Start, Unity-null
    ///     reference resolution in ButtonChangePage, the typed VariantView state events, and mesh-rig points
    ///     outliving their owner.
    /// </summary>
    public sealed class UIComponentRegressionTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject item in _objects)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void CategoryBar_SelectBeforeStart_BecomesTheInitialSelectionWithOneEvent()
        {
            CategoryBar bar = CreateBar("first", "second", "third");
            List<string> unityEventIds = new List<string>();
            List<int> typedIndices = new List<int>();
            bar.OnCategoryIdSelected.AddListener(unityEventIds.Add);
            bar.SelectionChanged += (index, id) => typedIndices.Add(index);

            bool selected = bar.Select("third");
            Assert.That(bar.IsInitialized, Is.False, "Selecting before Start must not build item views.");
            bar.Initialize();

            Assert.That(selected, Is.True);
            Assert.That(bar.IsInitialized, Is.True);
            Assert.That(bar.CurrentIndex, Is.EqualTo(2), "Start must not replace a selection made before it.");
            CollectionAssert.AreEqual(new[] { "third" }, unityEventIds);
            CollectionAssert.AreEqual(new[] { 2 }, typedIndices);
        }

        [Test]
        public void CategoryBar_DisabledSelectBeforeStart_IsRejectedAndKeepsTheStartIndex()
        {
            CategoryBar bar = CreateBar("first", "second");
            bar.SetEntryDisabled(1, true);

            Assert.That(bar.Select(1), Is.False);
            Assert.That(bar.IsInitialized, Is.False);

            bar.Initialize();
            Assert.That(bar.CurrentIndex, Is.Zero);
        }

        [Test]
        public void CategoryBar_NextBeforeStart_StepsFromTheStartIndexWithOneEvent()
        {
            CategoryBar bar = CreateBar("first", "second", "third");
            List<int> typedIndices = new List<int>();
            List<int> unityIndices = new List<int>();
            bar.SelectionChanged += (index, id) => typedIndices.Add(index);
            bar.OnCategorySelected.AddListener(unityIndices.Add);

            bar.Next();

            Assert.That(bar.IsInitialized, Is.False);
            Assert.That(bar.CurrentIndex, Is.EqualTo(1));

            bar.Initialize();

            Assert.That(bar.CurrentIndex, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 1 }, typedIndices, "Only the final entry may be announced.");
            CollectionAssert.AreEqual(new[] { 1 }, unityIndices);
        }

        [Test]
        public void CategoryBar_SelectionBeforeStart_DoesNotSpawnItemsOrRewriteTheStartIndex()
        {
            CategoryBar bar = CreateBar("first", "second", "third");
            GameObject template = Track(new GameObject(
                "Item template",
                typeof(RectTransform),
                typeof(Button),
                typeof(CategoryBarItem)));
            GameObject itemsRoot = Track(new GameObject("Items root", typeof(RectTransform)));
            SerializedObject settings = new SerializedObject(bar);
            settings.FindProperty("_itemPrefab").objectReferenceValue = template.GetComponent<CategoryBarItem>();
            settings.FindProperty("_itemsRoot").objectReferenceValue = itemsRoot.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(bar.Select(2), Is.True);
            bar.Prev();
            bar.Next();
            bar.Prev();

            settings.Update();
            Assert.That(settings.FindProperty("_startIndex").intValue, Is.Zero,
                "A pre-Start selection must not become a persistent authoring change.");
            Assert.That(itemsRoot.transform.childCount, Is.Zero,
                "The Edit Mode Next/Prev inspector buttons must not spawn item prefabs into the scene.");
            Assert.That(bar.CurrentIndex, Is.EqualTo(1));

            bar.Initialize();

            Assert.That(itemsRoot.transform.childCount, Is.EqualTo(3));
            Assert.That(bar.CurrentIndex, Is.EqualTo(1));
        }

        [Test]
        public void CategoryBar_SelectionChanged_FiresBeforeUnityEventsOncePerChange()
        {
            CategoryBar bar = CreateBar("first", "second");
            bar.Initialize();
            List<string> order = new List<string>();
            bar.SelectionChanged += (index, id) => order.Add("typed:" + id);
            bar.OnCategorySelected.AddListener(index => order.Add("unity:" + index));

            bar.Select(1);
            bar.Select(1);

            CollectionAssert.AreEqual(new[] { "typed:second", "unity:1" }, order);
        }

        [Test]
        public void CategoryBar_SetCategoriesAfterInitialize_ReannouncesTheKeptSelectionOnce()
        {
            CategoryBar bar = CreateBar("first", "second");
            bar.Initialize();
            bar.Select("second");
            List<string> announced = new List<string>();
            bar.SelectionChanged += (index, id) => announced.Add(index + ":" + id);

            bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "replacement" },
                new CategoryBar.Entry { Id = "second" }
            });

            Assert.That(bar.CurrentCategoryId, Is.EqualTo("second"));
            CollectionAssert.AreEqual(new[] { "1:second" }, announced,
                "Filtered lists re-apply the kept category to the new entries, so it is announced exactly once.");
        }

        [Test]
        public void ButtonChangePage_DestroyedSelectableReference_FallsBackToOwnSelectable()
        {
            GameObject stale = Track(new GameObject("Stale selectable", typeof(RectTransform)));
            Button staleButton = stale.AddComponent<Button>();
            GameObject buttonObject = Track(new GameObject("Button", typeof(RectTransform)));
            Button own = buttonObject.AddComponent<Button>();
            own.interactable = false;
            ButtonChangePage page = buttonObject.AddComponent<ButtonChangePage>();
            SetPrivateField(page, "_selectable", staleButton);
            Object.DestroyImmediate(stale);

            InvokePrivate(page, "Awake");

            Assert.That(page.Interactable, Is.False,
                "A destroyed serialized reference must be replaced by the Selectable on the same object.");
        }

        [Test]
        public void VariantView_StateEvents_FireOncePerActualChange()
        {
            GameObject root = Track(new GameObject("Variant events"));
            VariantView view = root.AddComponent<VariantView>();
            SerializedObject settings = new SerializedObject(view);
            SerializedProperty colors = settings.FindProperty("_imageColors");
            colors.arraySize = 1;
            colors.GetArrayElementAtIndex(0).FindPropertyRelative("colors").arraySize = 3;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(view.MaxStates, Is.EqualTo(3));

            List<int> typed = new List<int>();
            List<int> unity = new List<int>();
            view.StateChanged += typed.Add;
            view.OnStateChanged.AddListener(unity.Add);

            view.SetCurrentState(1);
            view.SetCurrentState(1);
            view.NextState();
            view.NextState();
            view.PreviousState();
            view.SetCurrentState(7);

            CollectionAssert.AreEqual(new[] { 1, 2, 1 }, typed);
            CollectionAssert.AreEqual(typed, unity);
            Assert.That(view.CurrentStateIndex, Is.EqualTo(1));
        }

        [Test]
        public void MeshRigPoint_AfterGraphicOwnerIsDestroyed_PoseAndDisableDoNotThrow()
        {
            GameObject rigObject = Track(new GameObject(
                "Destroyed owner rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic)));
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();
            UIMeshRigPoint[] points = UIMeshRigLayoutBuilder.Apply(rig, UIMeshRigLayoutPreset.SimpleBounce, true, false);
            Assert.That(points, Has.Length.EqualTo(1));

            Object.DestroyImmediate(rig);

            Assert.DoesNotThrow(() => points[0].SetProceduralPose(new Vector2(2f, 1f), 5f, Vector2.one));
            Assert.DoesNotThrow(() => points[0].ClearProceduralPose());
            points[0].gameObject.SetActive(false);
            points[0].gameObject.SetActive(true);
        }

        private CategoryBar CreateBar(params string[] ids)
        {
            GameObject root = Track(new GameObject("Category bar", typeof(RectTransform)));
            CategoryBar bar = root.AddComponent<CategoryBar>();
            List<CategoryBar.Entry> entries = new List<CategoryBar.Entry>();
            foreach (string id in ids)
            {
                entries.Add(new CategoryBar.Entry { Id = id, DisplayName = id });
            }

            bar.SetCategories(entries);
            return bar;
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }
    }
}
