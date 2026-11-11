using System;
using System.Reflection;
using Neo.Rpg.Demo;
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
    ///     End-to-end checks of the Neo.Rpg demo builders on copies of the real scenes: bootstrap leftovers are removed,
    ///     references, NoCode bindings and persistent listeners are authored, and a second pass changes nothing. Rpg
    ///     types are reached through serialized fields and reflection, so this assembly needs no extra references.
    /// </summary>
    public sealed class RpgDemoBuildersTests
    {
        private const string QuickBuilderSuffix = "RpgCharacterQuickDemoBuilder.Build";
        private const string CombatBuilderSuffix = "RpgCombatNpcDemoBuilder.Build";
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] QuickDemoSingletons =
        {
            "Rpg Demo Player", "Rpg Demo Enemy", "Rpg Demo Ground", "RpgCharacterQuickDemo", "Directional Light"
        };

        private string _folder;
        private Scene _scene;

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            if (!string.IsNullOrEmpty(_folder) && AssetDatabase.IsValidFolder(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }

            _scene = default;
            _folder = null;
        }

        [Test]
        public void QuickDemoBuilder_AuthorsNoCodeBindingsAndRemovesBootstrapLeftovers()
        {
            DemoSceneBuilderInfo builder = Builder(QuickBuilderSuffix);
            OpenCopy(builder.ResolveAssetPath(), "RpgQuickDemoCopy.unity");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());
            foreach (string objectName in QuickDemoSingletons)
            {
                Assert.That(CountRoots(objectName), Is.LessThanOrEqualTo(1), objectName + " duplicates must be removed.");
            }

            RpgCharacterQuickDemo controller = Single<RpgCharacterQuickDemo>();
            Assert.That(controller.IsSceneBuilt, Is.True, "Controller references must be assigned by the builder.");
            Component player = (Component)Reference(controller, "_player");
            Assert.That(Reference(player, "_template"), Is.Not.Null, "The player is configured by a template asset.");

            Assert.That(DescribeButton("Btn_Damage 25"), Is.EqualTo("RpgCharacterQuickDemo.DamagePlayer"));
            Assert.That(DescribeButton("Btn_Upgrade Vitality"), Is.EqualTo("RpgCharacterQuickDemo.UpgradePlayerStat"));
            Assert.That(DescribeButton("Btn_NoCode Heal Player 20"), Is.EqualTo("RpgNoCodeAction.Execute"));
            Assert.That(DescribeButton("Btn_Fire Hit 40"), Is.EqualTo("RpgCharacterQuickDemo.HitEnemy"));

            Component binding = FindByTypeName("Neo.Rpg.Components.RpgResourceBinding");
            Assert.That(binding, Is.Not.Null, "Resource rows are bound with RpgResourceBinding.");
            Assert.That(DemoWiring.Describe(EventField(binding, "_onPercent")), Is.EqualTo("Image.set_fillAmount"));

            Component condition = FindByTypeName("Neo.Condition.NeoCondition");
            Assert.That(condition, Is.Not.Null, "The low HP row carries a NeoCondition.");
            Assert.That(DemoWiring.Describe(EventField(condition, "_onResult")), Is.EqualTo("GameObject.SetActive"));
            Assert.That(ArraySize(condition, "_conditions"), Is.EqualTo(1));

            ModuleDemoSceneInfo info = Single<ModuleDemoSceneInfo>();
            Assert.That(info.HasAuthoredOverlay, Is.True);
            Assert.That(info.ModuleName, Does.Contain("Neo.Rpg"));

            UnityEventBase levelChanged = EventField(player, "_onLevelChanged");
            Assert.That(DemoWiring.Describe(levelChanged), Does.Contain("DemoValueReadout.SetInt"));
            int levelListeners = DemoWiring.Count(levelChanged);

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(levelChanged), Is.EqualTo(levelListeners),
                "Listeners on the hand-authored character must not pile up.");
            Assert.That(ArraySize(condition, "_conditions"), Is.EqualTo(1), "The condition list must not grow.");
        }

        [Test]
        public void CombatDemoBuilder_AssignsReferencesAndWiresNpcEvents()
        {
            DemoSceneBuilderInfo builder = Builder(CombatBuilderSuffix);
            OpenCopy(builder.ResolveAssetPath(), "RpgCombatDemoCopy.unity");

            DemoSceneResult first = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(first.Success, Is.True, first.Summary());

            RpgCombatDemoController controller = Single<RpgCombatDemoController>();
            Assert.That(controller.IsSceneBuilt, Is.True, "HUD references must be assigned by the builder.");
            Assert.That(Reference(controller, "_player"), Is.Not.Null);
            Assert.That(ArraySize(controller, "_enemies"), Is.EqualTo(2), "Both NPCs are assigned explicitly.");

            RpgWorldHealthBars bars = Single<RpgWorldHealthBars>();
            Assert.That(Reference(bars, "_canvas"), Is.Not.Null, "The health bar canvas is authored in the scene.");
            Assert.That(new SerializedObject(bars).FindProperty("_createCanvasIfMissing").boolValue, Is.False);
            Assert.That(new SerializedObject(bars).FindProperty("_scanSceneForTargets").boolValue, Is.False,
                "The bars draw the assigned enemies instead of searching the scene.");
            Assert.That(ArraySize(bars, "_sceneTargets"), Is.EqualTo(2));

            Assert.That(DescribeButton("Btn_Player Hit"), Is.EqualTo("RpgCombatDemoController.DamageNearestEnemy"));
            Assert.That(DescribeButton("Btn_Restore All"), Is.EqualTo("RpgCombatDemoController.RestoreAll"));
            Assert.That(DescribeButton("Btn_NoCode Sword Slash"), Is.EqualTo("RpgNoCodeAction.Execute"));

            Component contact = FindByTypeName("Neo.Rpg.RpgContactDamage");
            Assert.That(contact, Is.Not.Null);
            UnityEventBase targetFound = EventField(contact, "_onTargetFound");
            Assert.That(DemoWiring.Describe(targetFound), Does.Contain("DemoLogFeed.Log"));

            UnityEventBase death = EventField((Component)Reference(controller, "_player"), "_onDeath");
            Assert.That(DemoWiring.Describe(death), Is.EqualTo("GameObject.SetActive; GameObject.SetActive"));

            DemoSceneResult second = DemoSceneRunner.BuildOpenScene(_scene, new[] { builder });
            Assert.That(second.Success, Is.True, second.Summary());
            Assert.That(second.Created, Is.Zero, second.Summary());
            Assert.That(second.Removed, Is.Zero, second.Summary());
            Assert.That(second.Listeners, Is.EqualTo(first.Listeners));
            Assert.That(DemoWiring.Count(death), Is.EqualTo(2), "Player death listeners must not pile up.");
            Assert.That(DemoWiring.Count(targetFound), Is.EqualTo(1), "Contact listeners must not pile up.");
        }

        private static DemoSceneBuilderInfo Builder(string suffix)
        {
            DemoSceneBuilderInfo builder = DemoSceneRunner.DiscoverBuilders()
                .Find(info => info.Id.EndsWith(suffix, StringComparison.Ordinal));
            Assert.That(builder, Is.Not.Null, suffix + " must be registered.");
            Assert.That(builder.Error, Is.Null);
            string source = builder.ResolveAssetPath();
            Assume.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(source), Is.Not.Null, "Scene not found: " + source);
            return builder;
        }

        private static Object Reference(Object owner, string field)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + field);
            return property.objectReferenceValue;
        }

        private static int ArraySize(Object owner, string field)
        {
            SerializedProperty property = new SerializedObject(owner).FindProperty(field);
            Assert.That(property, Is.Not.Null, owner.GetType().Name + "." + field);
            return property.arraySize;
        }

        private static UnityEventBase EventField(Component owner, string field)
        {
            Assert.That(owner, Is.Not.Null, "Missing owner of " + field);
            for (Type type = owner.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo info = type.GetField(field, InstanceFields | BindingFlags.DeclaredOnly);
                if (info != null)
                {
                    return (UnityEventBase)info.GetValue(owner);
                }
            }

            Assert.Fail(owner.GetType().Name + " has no UnityEvent field " + field);
            return null;
        }

        private void OpenCopy(string source, string fileName)
        {
            string folderName = "__NeoRpgDemoBuildersTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _folder = "Assets/" + folderName;
            string path = _folder + "/" + fileName;
            Assert.That(AssetDatabase.CopyAsset(source, path), Is.True, "Could not copy " + source);
            _scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(_scene.IsValid() && _scene.isLoaded, Is.True, "Scene copy did not open: " + path);
        }

        private int CountRoots(string objectName)
        {
            int count = 0;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    count++;
                }
            }

            return count;
        }

        private T Single<T>() where T : Component
        {
            T found = null;
            int count = 0;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    found = component;
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1), typeof(T).Name);
            return found;
        }

        private Component FindByTypeName(string fullName)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && component.GetType().FullName == fullName)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        private string DescribeButton(string objectName)
        {
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == objectName)
                    {
                        return DemoWiring.Describe(button.onClick);
                    }
                }
            }

            Assert.Fail("Missing button " + objectName);
            return null;
        }
    }
}
