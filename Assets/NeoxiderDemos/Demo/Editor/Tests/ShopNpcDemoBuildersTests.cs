using System;
using System.Collections.Generic;
using System.Text;
using Neo.Samples;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     Edit-mode contracts of the Shop and NPC demo builders. Each test copies the shipped scene, builds it twice and
    ///     checks that the hand-authored module objects survive, the pre-kit leftovers are migrated, the persistent
    ///     wiring is in place and the second pass changes nothing. Module types (Shop, Money, NpcNavigation) are
    ///     addressed by name so this assembly needs no extra module references.
    /// </summary>
    public sealed class ShopNpcDemoBuildersTests
    {
        private const string ShopScene = "Scenes/Shop/Shop.unity";
        private const string NpcScene = "Scenes/Ai/AiMoveExample.unity";

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
        public void ShopBuilder_AuthorsTheStorefrontAndWiresShopAndMoney()
        {
            DemoSceneResult first = BuildTwice(ShopScene, "ShopDemoBuilder.Build", out DemoSceneResult second);

            ShopDemoController controller = Single<ShopDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                Assert.That(root.name, Is.Not.EqualTo("Canvas"), "The pre-kit portrait storefront must be retired.");
            }

            Component shop = SingleByName("Shop");
            Component money = SingleByName("Money");
            Assert.That(Reference(controller, "_shop"), Is.SameAs(shop));
            Assert.That(Reference(controller, "_money"), Is.SameAs(money));

            SerializedObject shopData = new(shop);
            int catalog = shopData.FindProperty("_shopItemDatas").arraySize;
            SerializedProperty cards = shopData.FindProperty("_shopItems");
            Assert.That(cards.arraySize, Is.EqualTo(catalog), "One authored card per catalog item.");
            for (int i = 0; i < cards.arraySize; i++)
            {
                Component card = cards.GetArrayElementAtIndex(i).objectReferenceValue as Component;
                Assert.That(card, Is.Not.Null, "Shop Items[" + i + "]");
                Assert.That(DemoSceneBuildContext.IsGenerated(card.gameObject), Is.True,
                    "Cards live in the authored frame, not in a runtime-spawned list.");
                Assert.That(card.GetComponent("ShopPurchaseButtonView"), Is.Not.Null, card.name);
                Assert.That(DescribeSerialized(card, "OnSelectItem"), Is.EqualTo("GameObject.SetActive"), card.name);
            }

            Assert.That(shopData.FindProperty("_autoSpawnItems").boolValue, Is.False);
            Assert.That(shopData.FindProperty("_prefab").objectReferenceValue, Is.Null);
            Assert.That(shopData.FindProperty("_shopItemPreview").objectReferenceValue, Is.Not.Null);
            Assert.That(shopData.FindProperty("moneySpendSource").objectReferenceValue, Is.SameAs(money.gameObject));
            Assert.That(DescribeSerialized(shop, "OnPurchasedId"), Is.EqualTo("ShopDemoController.OnItemPurchased"),
                "Rebuilding must not duplicate the listener on the hand-authored Shop.");
            Assert.That(DescribeSerialized(shop, "OnShopChanged"), Is.EqualTo("ShopDemoController.RefreshLabels"));
            Assert.That(new SerializedObject(money).FindProperty("t_money").arraySize, Is.Zero,
                "The retired legacy money text must not stay referenced.");

            Component textMoney = SingleByName("TextMoney");
            Assert.That(new SerializedObject(textMoney).FindProperty("_moneySource").objectReferenceValue, Is.SameAs(money));
            Assert.That(DemoWiring.Describe(FindButton("Btn_+100").onClick), Is.EqualTo("Money.Add; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Sale -50%").onClick), Is.EqualTo("ShopDemoController.StartSale"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Reset shop + wallet").onClick),
                Is.EqualTo("ShopDemoController.ResetDemo"));
            AssertStable(first, second);
        }

        [Test]
        public void NpcBuilder_MigratesTheLegacyAgentsAndWiresFourNpcs()
        {
            DemoSceneResult first = BuildTwice(NpcScene, "AiMoveExampleBuilder.Build", out DemoSceneResult second);

            NpcDemoController controller = Single<NpcDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, first.Summary());
            Assert.That(AllByName("AiNavigation"), Is.Empty, "The deprecated AiNavigation must be migrated.");
            Assert.That(AllByName("NpcNavigation").Count, Is.EqualTo(4), "Chaser, Guard, Patroller and Sentry.");
            Assert.That(Single<Camera>().transform.parent, Is.Null, "The overview camera must not ride on an agent.");

            Component chaser = (Component)Reference(controller, "_chaser");
            Component guard = (Component)Reference(controller, "_guard");
            Component sentry = (Component)Reference(controller, "_sentry");
            Assert.That(DemoSceneBuildContext.IsGenerated(chaser.gameObject), Is.False, "The chaser is the hand-authored agent.");
            Assert.That(DemoSceneBuildContext.IsGenerated(guard.gameObject), Is.False, "The guard is the hand-authored agent.");
            Assert.That(DescribeSerialized(chaser, "onMovementStarted"), Is.EqualTo("NpcDemoController.OnChaserStarted"),
                "Rebuilding must not duplicate listeners on the hand-authored chaser.");
            Assert.That(DescribeSerialized(guard, "onModeChanged"), Is.EqualTo("NpcDemoController.OnGuardModeChanged"));
            Assert.That(DescribeSerialized(sentry, "onStartFollowing"),
                Is.EqualTo("GameObject.SetActive; NpcDemoController.OnSentryChaseStarted"));
            Assert.That(new SerializedObject(sentry).FindProperty("patrolZone").objectReferenceValue, Is.Not.Null);

            Assert.That(DemoWiring.Describe(FindButton("Btn_Run").onClick), Is.EqualTo("NpcNavigation.SetRunning"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Stop").onClick), Is.EqualTo("NpcNavigation.Stop; DemoLogFeed.Log"));
            Assert.That(DemoWiring.Describe(FindButton("Btn_Patrol").onClick), Is.EqualTo("NpcDemoController.SetGuardMode"));
            AssertStable(first, second);
        }

        private DemoSceneResult BuildTwice(string relativeScenePath, string builderSuffix, out DemoSceneResult second)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(builderSuffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, builderSuffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            OpenCopy(relativeScenePath);

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            return first;
        }

        private static void AssertStable(DemoSceneResult first, DemoSceneResult second)
        {
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners), second.Summary());
        }

        private void OpenCopy(string relativeScenePath)
        {
            string source = DemoSampleRoots.Demo + "/" + relativeScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
            {
                Assert.Ignore("Demo scene not available in this layout: " + source);
            }

            string folderName = "__NeoShopNpcBuilderTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + System.IO.Path.GetFileName(relativeScenePath);
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Temporary scene did not open: " + path);
            SceneManager.SetActiveScene(_scene);
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

        private List<Component> AllByName(string typeName)
        {
            List<Component> found = new();
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && component.GetType().Name == typeName)
                    {
                        found.Add(component);
                    }
                }
            }

            return found;
        }

        private Component SingleByName(string typeName)
        {
            List<Component> found = AllByName(typeName);
            Assert.That(found.Count, Is.EqualTo(1), typeName);
            return found[0];
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

            Assert.Fail("Missing demo button " + name);
            return null;
        }

        private static string DescribeSerialized(Object owner, string eventField)
        {
            SerializedProperty calls = new SerializedObject(owner).FindProperty(eventField + ".m_PersistentCalls.m_Calls");
            Assert.That(calls, Is.Not.Null, owner.GetType().Name + "." + eventField);
            StringBuilder builder = new();
            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                Object target = call.FindPropertyRelative("m_Target").objectReferenceValue;
                if (i > 0)
                {
                    builder.Append("; ");
                }

                builder.Append(target != null ? target.GetType().Name : "<missing>").Append('.')
                    .Append(call.FindPropertyRelative("m_MethodName").stringValue);
            }

            return builder.ToString();
        }

        private static Object Reference(Object owner, string field)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + field);
            Object value = property.objectReferenceValue;
            Assert.That(value, Is.Not.Null, owner.GetType().Name + "." + field + " must be assigned by the builder.");
            return value;
        }
    }
}
