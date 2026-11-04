#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Target feed of the Demo sample's RpgWorldHealthBars (scene search off, Track / Untrack, scene targets) and of
    ///     VampireSurvivorDemoController (claims the bars' feed, death fallback for enemies without RpgDeathHandler).
    ///     Skipped when the Demo sample is not imported.
    /// </summary>
    public sealed class RpgDemoHealthBarFeedTests
    {
        private readonly List<GameObject> _owned = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null) Object.Destroy(_owned[i]);
            }

            _owned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HealthBars_WithSceneSearchOff_DrawOnlyTrackedTargets()
        {
            Type barsType = RequireDemoType("Neo.Rpg.Demo.RpgWorldHealthBars");
            Camera camera = OrthoCamera();
            GameObject targetObject = Host("Tagged Target");
            targetObject.tag = "Respawn";
            RpgCharacter target = targetObject.AddComponent<RpgCharacter>();
            targetObject.SetActive(true);

            Component bars = Bars(barsType, camera, false, null);
            yield return null;
            yield return null;

            Assert.That(Get<int>(bars, "TrackedTargetCount"), Is.Zero, "The tagged character is not searched for.");
            Assert.That(Invoke(bars, "Track", target), Is.True);
            yield return null;
            Assert.That(Get<int>(bars, "TrackedTargetCount"), Is.EqualTo(1));
            Assert.That(Get<int>(bars, "VisibleBarCount"), Is.EqualTo(1));

            targetObject.SetActive(false);
            Invoke(bars, "RefreshTargets");
            Assert.That(Get<int>(bars, "TrackedTargetCount"), Is.Zero, "An inactive target is pruned.");

            targetObject.SetActive(true);
            Assert.That(Invoke(bars, "Track", target), Is.True, "A respawned target is tracked again.");
            yield return null;
            Assert.That(Invoke(bars, "Untrack", target), Is.True);
            yield return null;
            Assert.That(Get<int>(bars, "VisibleBarCount"), Is.Zero, "Untrack hides the bar.");
        }

        [UnityTest]
        public IEnumerator HealthBars_Untrack_KeepsSceneTargetHiddenUntilTrackedAgain()
        {
            Type barsType = RequireDemoType("Neo.Rpg.Demo.RpgWorldHealthBars");
            Camera camera = OrthoCamera();
            GameObject targetObject = Host("Scene Target");
            RpgCharacter target = targetObject.AddComponent<RpgCharacter>();
            targetObject.SetActive(true);

            Component bars = Bars(barsType, camera, false, target);
            yield return null;
            yield return null;
            Assert.That(Invoke(bars, "IsTracked", target), Is.True, "Scene targets are tracked without a search.");

            Invoke(bars, "Untrack", target);
            Invoke(bars, "RefreshTargets");
            yield return null;
            Assert.That(Invoke(bars, "IsTracked", target), Is.False, "A scan must not bring an untracked target back.");
            Assert.That(Get<int>(bars, "VisibleBarCount"), Is.Zero);

            Invoke(bars, "Track", target);
            Invoke(bars, "RefreshTargets");
            yield return null;
            Assert.That(Invoke(bars, "IsTracked", target), Is.True);
            Assert.That(Get<int>(bars, "VisibleBarCount"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator VampireController_FeedsBarsAndDeactivatesEnemyWithoutDeathHandler()
        {
            Type barsType = RequireDemoType("Neo.Rpg.Demo.RpgWorldHealthBars");
            Type controllerType = RequireDemoType("Neo.Rpg.Demo.VampireSurvivorDemoController");
            Camera camera = OrthoCamera();
            GameObject playerObject = Host("Player");
            RpgCharacter player = playerObject.AddComponent<RpgCharacter>();
            playerObject.SetActive(true);
            GameObject enemyObject = Host("Enemy");
            enemyObject.tag = "Enemy";
            RpgCharacter enemy = enemyObject.AddComponent<RpgCharacter>();
            enemyObject.SetActive(true);

            GameObject host = Host("Vampire Controller");
            Component bars = host.AddComponent(barsType);
            SerializedObject barsSerialized = new(bars);
            barsSerialized.FindProperty("_worldCamera").objectReferenceValue = camera;
            barsSerialized.FindProperty("_worldOffset").vector3Value = Vector3.zero;
            barsSerialized.ApplyModifiedPropertiesWithoutUndo();
            Component controller = host.AddComponent(controllerType);
            SerializedObject serialized = new(controller);
            serialized.FindProperty("_player").objectReferenceValue = player;
            serialized.FindProperty("_startSpawnersOnPlay").boolValue = false;
            serialized.FindProperty("_fallbackCloseRangeDamage").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(Get<bool>(bars, "ScanSceneForTargets"), Is.False,
                "The controller feeds the same 'Enemy' characters, so the bars' own search is off.");
            Assert.That(Invoke(bars, "IsTracked", enemy), Is.True, "The controller tracks every enemy it configures.");
            Assert.That(Invoke(bars, "IsTracked", player), Is.False);
            Assert.That(Get<int>(controller, "ActiveEnemyCount"), Is.EqualTo(1));

            LogAssert.Expect(LogType.Warning, new Regex("has no RpgDeathHandler"));
            enemy.OnDeathEvent.Invoke();
            Assert.That(enemyObject.activeSelf, Is.False, "An enemy without RpgDeathHandler is deactivated on death.");
            Assert.That(Get<int>(controller, "KillCount"), Is.EqualTo(1));
            Invoke(bars, "RefreshTargets");
            Assert.That(Invoke(bars, "IsTracked", enemy), Is.False);

            enemyObject.SetActive(true);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Invoke(bars, "IsTracked", enemy), Is.True,
                "The fallback search configures an enemy enabled again after its death.");
        }

        private Camera OrthoCamera()
        {
            Camera camera = Host("Bars Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.gameObject.SetActive(true);
            return camera;
        }

        private Component Bars(Type barsType, Camera camera, bool scanScene, RpgCharacter sceneTarget)
        {
            GameObject host = Host("Bars");
            Component bars = host.AddComponent(barsType);
            SerializedObject serialized = new(bars);
            serialized.FindProperty("_worldCamera").objectReferenceValue = camera;
            serialized.FindProperty("_targetTag").stringValue = "Respawn";
            serialized.FindProperty("_scanSceneForTargets").boolValue = scanScene;
            serialized.FindProperty("_hideDeadTargets").boolValue = false;
            serialized.FindProperty("_worldOffset").vector3Value = Vector3.zero;
            if (sceneTarget != null)
            {
                SerializedProperty targets = serialized.FindProperty("_sceneTargets");
                targets.arraySize = 1;
                targets.GetArrayElementAtIndex(0).objectReferenceValue = sceneTarget;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
            return bars;
        }

        private GameObject Host(string name)
        {
            GameObject host = new("RpgDemoHealthBarFeed " + name);
            host.SetActive(false);
            _owned.Add(host);
            return host;
        }

        private static T Get<T>(object target, string property)
        {
            PropertyInfo info = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(info, Is.Not.Null, property);
            return (T)info.GetValue(target);
        }

        private static object Invoke(object target, string method, params object[] arguments)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(info, Is.Not.Null, method);
            return info.Invoke(target, arguments);
        }

        private static Type RequireDemoType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "Neo.Rpg.Demo") continue;
                Type type = assembly.GetType(name);
                Assert.That(type, Is.Not.Null, name);
                return type;
            }

            Assert.Ignore("Import the Demo Scenes sample (or use the developer Samples layout) to compile Neo.Rpg.Demo.");
            return null;
        }
    }
}
#endif
