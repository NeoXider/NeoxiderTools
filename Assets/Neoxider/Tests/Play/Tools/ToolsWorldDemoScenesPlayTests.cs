#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Play-mode smoke of the authored Tools world demos (Follow, DrawExample, MouseEffectorExample,
    ///     CameraConstraint): each scene loads without errors, its controller finds the references saved
    ///     in the scene, and the persistent UnityEvent wiring works at runtime. Sample types are reached by name because
    ///     this assembly must not reference the sample assemblies; hidden Samples~ layouts are skipped.
    /// </summary>
    public sealed class ToolsWorldDemoScenesPlayTests
    {
        private static readonly string[] SampleRootCandidates =
        {
            "Assets/NeoxiderDemos",
            "Assets/Neoxider/Samples~",
            "Assets/Samples/NeoxiderTools",
            "Assets/Samples/Neoxider Tools"
        };

        [UnityTest]
        public IEnumerator FollowDemo_DropTargetRaisesOnTargetLostAndRestoreReassigns()
        {
            yield return LoadScene("Scenes/Tools/Move/Follow.unity");
            Component controller = RequireBuiltController("Neo.Samples.FollowDemoController");
            Follow[] followers = Object.FindObjectsByType<Follow>(FindObjectsSortMode.None);
            Assert.That(followers.Length, Is.EqualTo(5), "Three authored followers, Rail Follower and Turret.");
            yield return WaitFrames(3);

            Invoke(controller, "DropTarget");
            yield return WaitFrames(2);
            foreach (Follow follower in followers)
            {
                Assert.That(follower.GetTarget(), Is.Null, follower.name);
            }

            Component log = RequireSingle("Neo.Samples.DemoLogFeed");
            Assert.That((string)Invoke(log, "GetLine", 0), Does.Contain("onTargetLost"),
                "Follow.onTargetLost is wired to the log feed in the saved scene.");

            Invoke(controller, "RestoreTarget");
            yield return null;
            foreach (Follow follower in followers)
            {
                Assert.That(follower.GetTarget(), Is.Not.Null, follower.name);
            }
        }

        [UnityTest]
        public IEnumerator DrawDemo_ReactiveCountersFeedTheAuthoredLabels()
        {
            yield return LoadScene("Scenes/Tools/DrawExample.unity");
            Component controller = RequireBuiltController("Neo.Samples.DrawerDemoController");
            Component drawer = RequireSingle("Neo.Tools.Drawer");
            SetField(drawer, "isActive", false);

            Invoke(drawer, "BeginLine", new Vector3(-6f, 0f, 0f));
            Invoke(drawer, "AppendPoint", new Vector3(0f, 0f, 0f));
            Invoke(drawer, "AppendPoint", new Vector3(6f, 3f, 0f));
            yield return null;
            Assert.That(RowValue("Value · Points · PointCount"), Is.EqualTo("3"),
                "Drawer.PointCount.OnChanged drives the label through a persistent DemoValueReadout listener.");

            Invoke(drawer, "EndLine");
            yield return null;
            TMP_Text lines = (TMP_Text)GetField(controller, "_linesValue");
            Assert.That(lines.text, Is.EqualTo("1"), "OnLineCreated reaches DrawerDemoController.ReportLine.");
            Invoke(drawer, "DeleteAll");
        }

        [UnityTest]
        public IEnumerator MouseEffectDemo_OnSpawnReachesTheController()
        {
            yield return LoadScene("Scenes/Tools/MouseEffectorExample.unity");
            Component controller = RequireBuiltController("Neo.Samples.MouseEffectDemoController");
            MouseEffect[] effects = Object.FindObjectsByType<MouseEffect>(FindObjectsSortMode.None);
            Assert.That(effects.Length, Is.EqualTo(3));
            MouseEffect spawn = Array.Find(effects, effect => effect.spawnPrefab != null);
            Assert.That(spawn, Is.Not.Null);

            spawn.onSpawn.Invoke();
            Assert.That((int)GetProperty(controller, "SpawnedCount"), Is.EqualTo(1),
                "MouseEffect.onSpawn is wired to MouseEffectDemoController.CountSpawn in the saved scene.");
        }

        [UnityTest]
        public IEnumerator CameraConstraintDemo_JumpIsClampedToTheBounds()
        {
            yield return LoadScene("Scenes/Tools/CameraConstraint.unity");
            Component controller = RequireBuiltController("Neo.Samples.CameraConstraintDemoController");
            CameraConstraint constraint = Object.FindFirstObjectByType<CameraConstraint>();
            Assert.That(constraint, Is.Not.Null);
            yield return null;

            Invoke(controller, "JumpVertical", 1);
            yield return null;
            constraint.GetConstraintBounds(out Vector3 min, out Vector3 max);
            float y = constraint.transform.position.y;
            Assert.That(max.y, Is.GreaterThan(min.y), "The start zoom leaves vertical room inside the bounds.");
            Assert.That(y, Is.EqualTo(max.y).Within(0.01f), "CameraConstraint pulls the jump back to the top edge.");
        }

        private static IEnumerator LoadScene(string relativePath)
        {
            string root = SampleRuntimeSceneSmokeTests.FindSampleDemoRoot(SampleRootCandidates);
            if (root == null || root.Replace('\\', '/').Contains("/Samples~/"))
            {
                Assert.Ignore("Tools world demo smoke needs development or imported samples (hidden Samples~ scripts do not compile).");
            }

            string path = root + "/" + relativePath;
            Assert.That(System.IO.File.Exists(path), Is.True, "Sample scene was not found: " + path);
            Scene scene = EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            double deadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (!scene.isLoaded)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "Sample scene did not load within 10 real-time seconds: " + path);
                yield return null;
            }

            yield return null;
        }

        private static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static Component RequireBuiltController(string typeName)
        {
            Component controller = RequireSingle(typeName);
            Assert.That((bool)GetProperty(controller, "IsSceneBuilt"), Is.True,
                typeName + " has no authored references.");
            return controller;
        }

        private static Component RequireSingle(string typeName)
        {
            Type type = FindType(typeName);
            Assert.That(type, Is.Not.Null, "Type '" + typeName + "' was not found.");
            Object[] found = Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(found.Length, Is.EqualTo(1), typeName);
            return (Component)found[0];
        }

        private static string RowValue(string rowName)
        {
            GameObject row = GameObject.Find(rowName);
            Assert.That(row, Is.Not.Null, "Row '" + rowName + "' was not found.");
            Transform value = row.transform.Find("Value");
            Assert.That(value, Is.Not.Null, rowName + "/Value");
            return value.GetComponent<TMP_Text>().text;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static object Invoke(object target, string method, params object[] arguments)
        {
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (candidate.Name == method && candidate.GetParameters().Length == arguments.Length)
                {
                    return candidate.Invoke(target, arguments);
                }
            }

            Assert.Fail(target.GetType().Name + "." + method + "(" + arguments.Length + " args) was not found.");
            return null;
        }

        private static object GetProperty(object target, string property)
        {
            PropertyInfo info = target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, target.GetType().Name + "." + property);
            return info.GetValue(target);
        }

        private static object GetField(object target, string field)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, target.GetType().Name + "." + field);
            return info.GetValue(target);
        }

        private static void SetField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, target.GetType().Name + "." + field);
            info.SetValue(target, value);
        }
    }
}
#endif
