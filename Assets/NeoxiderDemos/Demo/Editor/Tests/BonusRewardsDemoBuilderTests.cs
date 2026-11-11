using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contract of the Bonus Rewards demo builder on an empty scene: the reward loop (Collection with its
    ///     slot views and details row, Box, CooldownReward, the legacy TimeReward and a Money wallet) is authored with
    ///     explicit references and persistent wiring, every save key stays in the NeoDemo. namespace, and a second
    ///     build changes nothing. Bonus and Shop types are reached by name and through serialized fields, so this
    ///     assembly needs no extra module references.
    /// </summary>
    public sealed class BonusRewardsDemoBuilderTests
    {
        private const string BuilderId = "BonusRewardsDemoBuilder.Build";
        private const string SaveNamespace = "NeoDemo.";
        private const int CatalogSize = 6;

        private static readonly string[] SaveKeyFields = { "_saveKeyPrefix", "_saveName", "_addKey", "_moneySave" };

        private string _folder;
        private Scene _scene;
        private Scene _previousActive;

        [SetUp]
        public void SetUp()
        {
            _previousActive = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (_previousActive.IsValid() && _previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(_previousActive);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
                _folder = null;
            }
        }

        [Test]
        public void Builder_AuthorsTheCollectionWithExplicitReferences()
        {
            BuildEmptyScene();

            Component collection = SingleByTypeName("Collection");
            SerializedObject collectionData = new(collection);
            Assert.That(collectionData.FindProperty("_setInstanceOnAwake").boolValue, Is.False,
                "The demo collection must not claim the global Collection.I slot.");
            SerializedProperty catalog = collectionData.FindProperty("_itemCollectionDatas");
            Assert.That(catalog.arraySize, Is.EqualTo(CatalogSize));
            for (int i = 0; i < catalog.arraySize; i++)
            {
                Assert.That(catalog.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null, "Catalog entry " + i);
            }

            Component manager = SingleByTypeName("CollectionVisualManager");
            SerializedObject managerData = new(manager);
            Assert.That(managerData.FindProperty("_collection").objectReferenceValue, Is.SameAs(collection));
            Assert.That(managerData.FindProperty("_setInstanceOnAwake").boolValue, Is.False,
                "A game's global CollectionVisualManager must not make the demo row destroy itself.");
            SerializedProperty items = managerData.FindProperty("_items");
            Assert.That(items.arraySize, Is.EqualTo(CatalogSize));
            for (int i = 0; i < items.arraySize; i++)
            {
                Object view = items.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.That(view, Is.Not.Null, "Slot view " + i);
                SerializedObject viewData = new(view);
                Assert.That(viewData.FindProperty("_collection").objectReferenceValue, Is.SameAs(collection));
                Assert.That(viewData.FindProperty("_button").objectReferenceValue, Is.Not.Null, "Slot button " + i);
                Assert.That(viewData.FindProperty("_itemImage").objectReferenceValue, Is.Not.Null, "Slot picture " + i);
            }

            Component details = SingleByTypeName("ItemCollectionInfo");
            Assert.That(new SerializedObject(details).FindProperty("_collection").objectReferenceValue,
                Is.SameAs(collection));
            Assert.That(DemoWiring.Describe(Event(manager, "OnSetItem")),
                Is.EqualTo("ItemCollectionInfo.SetItemId; DemoLogFeed.Log"));

            Component box = SingleByTypeName("Box");
            SerializedObject boxData = new(box);
            Assert.That(boxData.FindProperty("_collection").objectReferenceValue, Is.SameAs(collection));
            Assert.That(boxData.FindProperty("_bar").objectReferenceValue, Is.Not.Null);
            Assert.That(boxData.FindProperty("_maxProgress").floatValue, Is.GreaterThan(0f));

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Bonus"));
        }

        [Test]
        public void Builder_WiresTheRewardLoopPersistently()
        {
            BuildEmptyScene();

            Component gift = SingleByTypeName("CooldownReward");
            Component coins = SingleByTypeName("TimeReward");
            Component box = SingleByTypeName("Box");
            Assert.That(DemoWiring.Describe(Event(gift, "OnRewardClaimed")), Is.EqualTo("Box.AddProgress; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(coins, "OnRewardClaimed")), Is.EqualTo("Money.Add; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(Event(box, "OnChangeProgress")), Is.EqualTo("Button.set_interactable"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Claim gift").onClick), Is.EqualTo("CooldownReward.Take"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Claim coins").onClick), Is.EqualTo("TimeReward.Take"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+100 progress").onClick),
                Is.EqualTo("Box.AddProgress; DemoLogFeed.Log"));

            Button open = FindButton("Btn_Open chest");
            Assert.That(DemoWiring.Describe(open.onClick), Is.EqualTo("Box.TakePrize; DemoLogFeed.Log"),
                "Every Open chest click is logged, including the silent one on a complete collection.");
            Assert.That(open.interactable, Is.False, "The chest starts empty, so Open chest starts disabled.");

            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset demo progress").onClick),
                Is.EqualTo("Collection.ClearCollection; Box.set_progress; Box.AddProgress; " +
                           "CooldownReward.SetRewardAvailableNow; TimeReward.SetRewardAvailableNow; " +
                           "Money.SetCurrentMoney; DemoLogFeed.Log"));
        }

        [Test]
        public void Builder_KeepsEverySaveKeyInTheDemoNamespace()
        {
            BuildEmptyScene();

            List<string> checkedKeys = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                    {
                        continue;
                    }

                    SerializedObject data = new(behaviour);
                    foreach (string field in SaveKeyFields)
                    {
                        SerializedProperty key = data.FindProperty(field);
                        if (key == null || key.propertyType != SerializedPropertyType.String)
                        {
                            continue;
                        }

                        string owner = behaviour.GetType().Name + "." + field;
                        Assert.That(key.stringValue, Does.Contain(SaveNamespace),
                            owner + " would share a save key with a real game.");
                        checkedKeys.Add(owner);
                    }
                }
            }

            Assert.That(checkedKeys, Is.SupersetOf(new[]
            {
                "Collection._saveKeyPrefix", "Box._saveName", "CooldownReward._addKey", "TimeReward._addKey",
                "Money._moneySave"
            }));
            Assert.That(new SerializedObject(SingleByTypeName("Money")).FindProperty("_persistMoney").boolValue, Is.False,
                "The demo wallet is session-only.");
        }

        [Test]
        public void Builder_RebuildsWithoutChanges()
        {
            DemoSceneResult first = BuildEmptyScene();
            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { Builder() });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(Event(SingleByTypeName("CooldownReward"), "OnRewardClaimed")), Is.EqualTo(2));
        }

        private DemoSceneResult BuildEmptyScene()
        {
            DemoSceneBuilderInfo builder = Builder();
            _scene = DemoTestScenes.OpenEmpty("__NeoBonusRewardsDemoTests", out _folder);
            DemoSceneResult result = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(result.Success, Is.True, result.Summary());
            return result;
        }

        private static DemoSceneBuilderInfo Builder()
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(BuilderId, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, BuilderId + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            return builder;
        }

        private static UnityEventBase Event(Component owner, string member)
        {
            Type type = owner.GetType();
            FieldInfo field = type.GetField(member, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                return (UnityEventBase)field.GetValue(owner);
            }

            PropertyInfo property = type.GetProperty(member, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, type.Name + "." + member);
            return (UnityEventBase)property.GetValue(owner);
        }

        private Button FindButton(string name)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == name)
                    {
                        return button;
                    }
                }
            }

            Assert.Fail("Button '" + name + "' not found.");
            return null;
        }

        private T Single<T>() where T : Component
        {
            List<T> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                found.AddRange(root.GetComponentsInChildren<T>(true));
            }

            Assert.That(found.Count, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }

        private Component SingleByTypeName(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour != null && behaviour.GetType().Name == typeName)
                    {
                        found.Add(behaviour);
                    }
                }
            }

            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
        }
    }
}
