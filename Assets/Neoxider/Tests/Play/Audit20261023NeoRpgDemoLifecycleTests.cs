#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoRpgDemoLifecycleTests
    {
        private readonly List<GameObject> _owned = new();
        private readonly List<Camera> _disabledCameras = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null) Object.Destroy(_owned[i]);
            }

            _owned.Clear();
            yield return null;
            foreach (Camera camera in _disabledCameras)
            {
                if (camera != null) camera.enabled = true;
            }

            _disabledCameras.Clear();
        }

        [UnityTest]
        public IEnumerator HealthBars_UseAssignedCameraInsteadOfMainCamera()
        {
            Type barsType = RequireDemoType("Neo.Rpg.Demo.RpgWorldHealthBars");
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.enabled && camera.CompareTag("MainCamera"))
                {
                    _disabledCameras.Add(camera);
                    camera.enabled = false;
                }
            }

            Camera main = Host("Main Camera").AddComponent<Camera>();
            main.tag = "MainCamera";
            main.orthographic = true;
            main.transform.position = new Vector3(10000f, 0f, -10f);
            main.gameObject.SetActive(true);
            Camera assigned = Host("Assigned Camera").AddComponent<Camera>();
            assigned.orthographic = true;
            assigned.transform.position = new Vector3(0f, 0f, -10f);
            assigned.gameObject.SetActive(true);

            GameObject target = Host("Target");
            target.tag = "Respawn";
            target.AddComponent<RpgCharacter>();
            target.SetActive(true);
            GameObject host = Host("Bars");
            Component bars = host.AddComponent(barsType);
            SerializedObject serialized = new(bars);
            serialized.FindProperty("_worldCamera").objectReferenceValue = assigned;
            serialized.FindProperty("_targetTag").stringValue = "Respawn";
            serialized.FindProperty("_hideDeadTargets").boolValue = false;
            serialized.FindProperty("_worldOffset").vector3Value = Vector3.zero;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(barsType.GetProperty("VisibleBarCount").GetValue(bars), Is.EqualTo(1),
                "The target is onscreen only through the explicitly assigned camera.");

            Canvas canvas = (Canvas)barsType.GetProperty("Canvas").GetValue(bars);
            ((Behaviour)bars).enabled = false;
            Assert.That(barsType.GetProperty("VisibleBarCount").GetValue(bars), Is.EqualTo(0));
            Assert.That(canvas.GetComponentsInChildren<UnityEngine.UI.Image>().Length, Is.Zero);
            ((Behaviour)bars).enabled = true;
            yield return null;
            Assert.That(barsType.GetProperty("VisibleBarCount").GetValue(bars), Is.EqualTo(1));

            Object.Destroy(bars);
            yield return null;
            yield return null;
            Assert.That(canvas == null, Is.True, "The component owns the fallback canvas it created.");
        }

        [UnityTest]
        public IEnumerator CombatController_Destroy_RemovesEnemyDeathSideEffects()
        {
            Type controllerType = RequireDemoType("Neo.Rpg.Demo.RpgCombatDemoController");
            RpgCharacter player = Host("Player").AddComponent<RpgCharacter>();
            player.gameObject.SetActive(true);
            RpgCharacter enemy = Host("Enemy").AddComponent<RpgCharacter>();
            enemy.gameObject.SetActive(true);
            GameObject host = Host("Combat Controller");
            Component controller = host.AddComponent(controllerType);
            SerializedObject serialized = new(controller);
            serialized.FindProperty("_player").objectReferenceValue = player;
            SerializedProperty enemies = serialized.FindProperty("_enemies");
            enemies.arraySize = 1;
            enemies.GetArrayElementAtIndex(0).objectReferenceValue = enemy;
            serialized.FindProperty("_autoDemoLoop").boolValue = false;
            serialized.FindProperty("_hideEnemiesOnDeath").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            host.SetActive(true);

            enemy.OnDeathEvent.Invoke();
            Assert.That(enemy.gameObject.activeSelf, Is.False, "The live controller should handle the event.");
            enemy.gameObject.SetActive(true);
            Object.Destroy(controller);
            yield return null;

            enemy.OnDeathEvent.Invoke();
            Assert.That(enemy.gameObject.activeSelf, Is.True,
                "An enemy must stop invoking the destroyed controller's hide-on-death action.");
        }

        [UnityTest]
        public IEnumerator DiceView_Reenable_RefreshesPieceChangedWhileDisabled()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Assert.Ignore("The current Dice gameplay sample requires legacy input support.");
            yield break;
#else
            Type viewType = RequireDemoType("Neo.Demo.GridSystem.DiceMergeDemoView");
            GameObject host = Host("Dice");
            host.AddComponent<Grid>();
            Component field = host.AddComponent(RequireType("Neo.GridSystem.FieldGenerator"));
            Invoke(field, "GenerateField", (object)null);
            Component board = host.AddComponent(RequireType("Neo.GridSystem.Dice.DiceBoardService"));
            Invoke(board, "ClearBoard");
            Component controller = host.AddComponent(RequireDemoType("Neo.Demo.GridSystem.DiceMergeDemoController"));
            Invoke(controller, "Configure", field, board, null, null, null);
            Type pieceType = RequireType("Neo.GridSystem.Dice.DicePiece");
            object single = pieceType.GetMethod("Single").Invoke(null, new object[] { 2 });
            Invoke(controller, "ForceCurrentPieceForTest", single);
            Behaviour view = (Behaviour)host.AddComponent(viewType);
            Invoke(view, "Configure", field, board, controller, null, null, null, null, null);
            host.SetActive(true);
            yield return null;

            view.enabled = false;
            object pair = pieceType.GetMethod("Pair").Invoke(null, new object[] { 2, 3 });
            Invoke(controller, "ForceCurrentPieceForTest", pair);
            view.enabled = true;
            yield return null;

            Transform tray = view.transform.Find("DiceTrayView");
            Assert.That(tray.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(2));
#endif
        }

        private GameObject Host(string name)
        {
            GameObject host = new("Audit20261023NeoRpgDemo " + name);
            host.SetActive(false);
            _owned.Add(host);
            return host;
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

        private static Type RequireType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name);
                if (type != null) return type;
            }

            Assert.Fail("Required sample dependency is not loaded: " + name);
            return null;
        }
    }
}
#endif
