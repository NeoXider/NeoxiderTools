using System.Collections;
using Neo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoUICategoryBarTests
    {
        private GameObject _root;
        private CategoryBar _bar;
        private bool _enteredPlayMode;

        private void CreateBar()
        {
            _root = new GameObject("Audit category bar", typeof(RectTransform));
            _bar = _root.AddComponent<CategoryBar>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(_root);
            _root = null;
            _bar = null;
            if (_enteredPlayMode && Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            _enteredPlayMode = false;
        }

        [Test]
        public void SetCategories_BeforeInitialize_HonorsInitialIndexAndSkipsDisabled()
        {
            CreateBar();
            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "blocked", Disabled = true },
                new CategoryBar.Entry { Id = "last" }
            }, 1);

            _bar.Initialize();

            Assert.That(_bar.CurrentIndex, Is.EqualTo(2));
            Assert.That(_bar.CurrentCategoryId, Is.EqualTo("last"));
        }

        [Test]
        public void SetCategories_WithCurrentList_PreservesEntriesAndSelection()
        {
            CreateBar();
            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "last" }
            });
            _bar.Initialize();
            _bar.Select("last");

            _bar.SetCategories(_bar.Categories);

            Assert.That(_bar.Categories.Count, Is.EqualTo(2));
            Assert.That(_bar.CurrentCategoryId, Is.EqualTo("last"));
            Assert.That(_bar.Select("first"), Is.True);
        }

        [Test]
        public void Rebuild_RetiresUnusedAuthoredButtonsAndRestoresThemWhenNeeded()
        {
            CreateBar();
            CreateItem("First");
            CategoryBarItem second = CreateItem("Second");
            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "second" }
            });
            _bar.Initialize();

            _bar.SetCategories(new[] { new CategoryBar.Entry { Id = "only" } });
            Assert.That(second.Button.interactable, Is.False);

            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "second" }
            });
            second.Button.onClick.Invoke();

            Assert.That(second.Button.interactable, Is.True);
            Assert.That(_bar.CurrentCategoryId, Is.EqualTo("second"));
        }

        [UnityTest]
        public IEnumerator DestroyComponent_UnsubscribesAuthoredButtonsButPreservesConsumerListeners()
        {
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            CreateBar();
            GameObject ownerObject = _root;
            CategoryBarItem first = CreateItem("First");
            CategoryBarItem second = CreateItem("Second");
            _bar.SetCategories(new[]
            {
                new CategoryBar.Entry { Id = "first" },
                new CategoryBar.Entry { Id = "second" }
            });
            int selections = 0;
            int consumerClicks = 0;
            _bar.OnCategorySelected.AddListener(_ => selections++);
            second.Button.onClick.AddListener(() => consumerClicks++);
            double startDeadline = EditorApplication.timeSinceStartup + 5d;
            while (_bar != null && _bar.CurrentCategoryId != "first" &&
                   EditorApplication.timeSinceStartup < startDeadline)
            {
                yield return null;
            }

            Assert.That(Application.isPlaying, Is.True);
            Assert.That(_bar != null, Is.True);
            Assert.That(_bar.isActiveAndEnabled, Is.True);
            Assert.That(_bar.CurrentCategoryId, Is.EqualTo("first"), "Native Start must initialize the active bar.");
            selections = 0;
            second.Button.onClick.Invoke();
            Assert.That(selections, Is.EqualTo(1), "The bar's own button listener must work before destruction.");
            Assert.That(consumerClicks, Is.EqualTo(1));
            Assert.That(_bar.CurrentCategoryId, Is.EqualTo("second"));
            Assert.That(_bar.Select("first"), Is.True);
            Assert.That(selections, Is.EqualTo(2));
            selections = 0;
            consumerClicks = 0;

            Object.Destroy(_bar);
            double destructionDeadline = EditorApplication.timeSinceStartup + 5d;
            while (_bar != null && EditorApplication.timeSinceStartup < destructionDeadline)
            {
                yield return null;
            }

            Assert.That(_bar == null, Is.True, "Only the CategoryBar component must have been destroyed.");
            int destroyedFrame = Time.frameCount;
            double cleanupDeadline = EditorApplication.timeSinceStartup + 5d;
            // Editor polls are not native frames. Observe a later frame after the confirmed OnDestroy.
            while (Time.frameCount == destroyedFrame && EditorApplication.timeSinceStartup < cleanupDeadline)
            {
                yield return null;
            }

            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.frameCount, Is.GreaterThan(destroyedFrame),
                "A native frame must advance after owner destruction before borrowed-child survival is checked.");
            Assert.That(ownerObject != null, Is.True);
            Assert.That(_root, Is.SameAs(ownerObject));
            Assert.That(first != null, Is.True);
            Assert.That(second != null, Is.True);
            Assert.That(first.transform.parent, Is.SameAs(ownerObject.transform));
            Assert.That(second.transform.parent, Is.SameAs(ownerObject.transform));
            second.Button.onClick.Invoke();

            Assert.That(selections, Is.Zero);
            Assert.That(consumerClicks, Is.EqualTo(1));
            Assert.That(second, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DestroyComponent_ReleasesSpawnedViewsInExternalRootAndPreservesMarker()
        {
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            for (int withSibling = 0; withSibling < 2; withSibling++)
            {
                CreateBar();
                GameObject ownerObject = _root;
                GameObject externalRoot = new GameObject("External item root", typeof(RectTransform));
                try
                {
                    GameObject sibling = null;
                    Vector3 siblingPosition = new Vector3(31f, 19f, 0f);
                    if (withSibling != 0)
                    {
                        sibling = new GameObject("Foreign sibling", typeof(RectTransform));
                        sibling.transform.SetParent(externalRoot.transform, false);
                        sibling.transform.localPosition = siblingPosition;
                    }

                    CategoryBarItem prefab = CreateItem("Item template");
                    GameObject marker = new GameObject("Marker", typeof(RectTransform));
                    marker.transform.SetParent(ownerObject.transform, false);
                    SerializedObject settings = new SerializedObject(_bar);
                    settings.FindProperty("_itemPrefab").objectReferenceValue = prefab;
                    settings.FindProperty("_itemsRoot").objectReferenceValue = externalRoot.transform;
                    settings.FindProperty("_selectionMarker").objectReferenceValue = marker.transform;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    _bar.SetCategories(new[] { new CategoryBar.Entry { Id = "first" } });
                    double startDeadline = EditorApplication.timeSinceStartup + 5d;
                    while (_bar != null && _bar.CurrentCategoryId != "first" &&
                           EditorApplication.timeSinceStartup < startDeadline)
                    {
                        yield return null;
                    }

                    Assert.That(Application.isPlaying, Is.True);
                    Assert.That(_bar != null, Is.True);
                    Assert.That(_bar.isActiveAndEnabled, Is.True);
                    Assert.That(_bar.CurrentCategoryId, Is.EqualTo("first"), "Native Start must create and select a view.");
                    CategoryBarItem[] initialViews = externalRoot.GetComponentsInChildren<CategoryBarItem>(true);
                    Assert.That(initialViews.Length, Is.EqualTo(1));
                    CategoryBarItem initialView = initialViews[0];
                    Assert.That(initialView, Is.Not.SameAs(prefab));
                    Assert.That(initialView.transform.parent, Is.SameAs(externalRoot.transform));
                    Assert.That(externalRoot.transform.childCount, Is.EqualTo(1 + withSibling));
                    Assert.That(marker.transform.parent, Is.SameAs(initialView.transform));

                    _bar.SetCategories(new[] { new CategoryBar.Entry { Id = "replacement" } });
                    double retirementDeadline = EditorApplication.timeSinceStartup + 5d;
                    while (initialView != null && EditorApplication.timeSinceStartup < retirementDeadline)
                    {
                        yield return null;
                    }

                    Assert.That(initialView == null, Is.True, "The old owned view must retire across deferred Destroy.");
                    CategoryBarItem[] rebuiltViews = externalRoot.GetComponentsInChildren<CategoryBarItem>(true);
                    Assert.That(rebuiltViews.Length, Is.EqualTo(1));
                    CategoryBarItem replacementView = rebuiltViews[0];
                    Assert.That(externalRoot.transform.childCount, Is.EqualTo(1 + withSibling));
                    Assert.That(marker != null, Is.True);
                    Assert.That(marker.transform.parent, Is.SameAs(replacementView.transform));

                    Object.Destroy(_bar);
                    double destructionDeadline = EditorApplication.timeSinceStartup + 5d;
                    while (_bar != null && EditorApplication.timeSinceStartup < destructionDeadline)
                    {
                        yield return null;
                    }

                    Assert.That(_bar == null, Is.True);
                    int destroyedFrame = Time.frameCount;
                    double cleanupDeadline = EditorApplication.timeSinceStartup + 5d;
                    // Require actual replacement destruction and a later native frame, not two Editor polls.
                    while ((replacementView != null || Time.frameCount == destroyedFrame) &&
                           EditorApplication.timeSinceStartup < cleanupDeadline)
                    {
                        yield return null;
                    }

                    Assert.That(Application.isPlaying, Is.True);
                    Assert.That(Time.frameCount, Is.GreaterThan(destroyedFrame),
                        "A native frame must advance after owner destruction before survivor checks.");
                    Assert.That(ownerObject != null, Is.True, "Removing the component must preserve its GameObject.");
                    Assert.That(_root, Is.SameAs(ownerObject));
                    Assert.That(externalRoot != null, Is.True);
                    Assert.That(replacementView == null, Is.True);
                    Assert.That(externalRoot.GetComponentsInChildren<CategoryBarItem>(true).Length, Is.Zero);
                    Assert.That(externalRoot.transform.childCount, Is.EqualTo(withSibling));
                    Assert.That(prefab != null, Is.True);
                    Assert.That(prefab.transform.parent, Is.SameAs(ownerObject.transform));
                    Assert.That(marker != null, Is.True);
                    Assert.That(marker.transform.parent, Is.SameAs(ownerObject.transform));
                    Assert.That(marker.activeSelf, Is.False);
                    if (withSibling != 0)
                    {
                        Assert.That(sibling != null, Is.True);
                        Assert.That(sibling.transform.parent, Is.SameAs(externalRoot.transform));
                        Assert.That(sibling.transform.localPosition, Is.EqualTo(siblingPosition));
                    }
                }
                finally
                {
                    Object.DestroyImmediate(externalRoot);
                    Object.DestroyImmediate(_root);
                    _root = null;
                    _bar = null;
                }
            }
        }

        private CategoryBarItem CreateItem(string name)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(CategoryBarItem));
            item.transform.SetParent(_root.transform, false);
            return item.GetComponent<CategoryBarItem>();
        }
    }
}
