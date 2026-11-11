using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Abilities;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Abilities
{
    /// <summary>
    ///     The ability scene validator reports wiring that would fail silently at runtime, and the Survivor demo
    ///     scene has none.
    /// </summary>
    public sealed class AbilitySceneValidatorTests
    {
        private const string SurvivorScene = "Assets/NeoxiderDemos/Demo/Scenes/SurvivorDemo.unity";

        [Test]
        public void Validate_ReportsUnknownAbilityArchetypeCueAndPayload()
        {
            // WHY: the test runner's untitled scene cannot have a second new scene opened next to it.
            Scene scene = SceneManager.GetActiveScene();
            GameObject hubObject = null;
            GameObject player = null;
            AbilityLibrary library = ScriptableObject.CreateInstance<AbilityLibrary>();
            AbilityDefinition bolt = ScriptableObject.CreateInstance<AbilityDefinition>();
            try
            {
                bolt.Blueprint.Id = "bolt";
                bolt.Blueprint.Delivery = AbilityDeliveryType.Projectile;
                bolt.Blueprint.ProjectileArchetypeId = "missing_archetype";
                bolt.Blueprint.ImpactCue = "missing_cue";
                bolt.Blueprint.ImpactEffects.Add(new EffectNodeData { OpId = "projectile", AbilityId = "missing_payload" });
                SerializedObject libraryObject = new SerializedObject(library);
                SerializedProperty abilities = libraryObject.FindProperty("_abilities");
                abilities.arraySize = 1;
                abilities.GetArrayElementAtIndex(0).objectReferenceValue = bolt;
                libraryObject.ApplyModifiedPropertiesWithoutUndo();

                hubObject = new GameObject("Hub");
                AbilitySystemBehaviour hub = hubObject.AddComponent<AbilitySystemBehaviour>();
                SerializedObject hubSerialized = new SerializedObject(hub);
                SerializedProperty libraries = hubSerialized.FindProperty("_libraries");
                libraries.arraySize = 1;
                libraries.GetArrayElementAtIndex(0).objectReferenceValue = library;
                hubSerialized.ApplyModifiedPropertiesWithoutUndo();

                player = new GameObject("Player");
                AbilityAutoCaster autoCaster = player.AddComponent<AbilityAutoCaster>();
                SerializedObject casterSerialized = new SerializedObject(autoCaster);
                SerializedProperty ids = casterSerialized.FindProperty("_abilityIds");
                ids.arraySize = 2;
                ids.GetArrayElementAtIndex(0).stringValue = "bolt";
                ids.GetArrayElementAtIndex(1).stringValue = "ghost";
                casterSerialized.ApplyModifiedPropertiesWithoutUndo();

                List<string> issues = Validate(scene);
                string all = string.Join("\n", issues);
                Assert.That(issues.Count, Is.EqualTo(4), all);
                Assert.That(all, Does.Contain("'ghost'"));
                Assert.That(all, Does.Contain("'missing_archetype'"));
                Assert.That(all, Does.Contain("'missing_cue'"));
                Assert.That(all, Does.Contain("'missing_payload'"));
            }
            finally
            {
                Object.DestroyImmediate(hubObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(library);
                Object.DestroyImmediate(bolt);
            }
        }

        [Test]
        public void SurvivorDemoScene_HasNoSilentAbilityWiringErrors()
        {
            if (!File.Exists(SurvivorScene))
            {
                Assert.Ignore("The Demo download (Assets/NeoxiderDemos/Demo) is not in this project.");
            }

            Scene scene = EditorSceneManager.OpenScene(SurvivorScene, OpenSceneMode.Additive);
            try
            {
                List<string> issues = Validate(scene);
                Assert.That(issues, Is.Empty, string.Join("\n", issues));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static List<string> Validate(Scene scene)
        {
            // WHY: this test assembly does not reference the abilities editor assembly, like the other editor tests.
            Type validator = Type.GetType("Neo.Abilities.Editor.AbilitySceneValidator, Neo.Abilities.Editor", true);
            MethodInfo validate = validator.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            return (List<string>)validate.Invoke(null, new object[] { scene });
        }
    }
}
