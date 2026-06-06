#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class SampleRuntimeSceneSmokeTests
    {
        private const string ProgressionScene = "Scenes/Progression_Demo.unity";
        private const string Match3Scene = "Scenes/GridSystem/GridSystemMatch3Demo.unity";
        private const string DiceMergeScene = "Scenes/GridSystem/GridSystemDiceMergeDemo.unity";
        private const string TicTacToeScene = "Scenes/GridSystem/GridSystemTicTacToeDemo.unity";
        private const string RpgCombatScene = "Scenes/RpgCombatNpcDemo.unity";
        private const string VampireScene = "Scenes/VampireSurvivorMCP.unity";
        private const string AnimationFlyScene = "Scenes/UI/AnimationFlyDemo.unity";

        private static readonly string[] CoreModuleScenes =
        {
            "Scenes/Audio/AudioDemo.unity",
            "Scenes/Level/LevelFlowDemo.unity",
            "Scenes/Quests/QuestDemo.unity",
            "Scenes/Save/SaveDemo.unity",
            "Scenes/Settings/SettingsDemo.unity",
            "Scenes/NoCode/NoCodeBindingDemo.unity",
            "Scenes/StateMachine/StateMachineDemo.unity",
            "Scenes/Parallax/ParallaxDemo.unity",
            "Scenes/Network/NetworkDemo.unity",
            "Scenes/UI/AnimationFlyDemo.unity"
        };

        private static readonly string[] SampleRootCandidates =
        {
            "Assets/Neoxider/Samples",
            "Assets/Neoxider/Samples~",
            "Assets/Samples/NeoxiderTools",
            "Assets/Samples/Neoxider Tools"
        };

        [UnityTest]
        public IEnumerator ProgressionDemo_UpdatesLevelThroughRuntimeUi()
        {
            yield return LoadScene(ProgressionScene);

            object ui = FindRequiredComponent("Neo.Samples.ProgressionDemoUI", "progression demo UI");
            object level = FindRequiredComponent("Neo.Core.Level.LevelComponent", "level component");
            int startLevel = GetProperty<int>(level, "Level");

            Invoke(ui, "AddLargeXp");
            yield return null;

            Assert.That(GetProperty<int>(level, "Level"), Is.GreaterThan(startLevel),
                "Progression demo should level up when its runtime UI adds a large XP amount.");
        }

        [UnityTest]
        public IEnumerator GridDemos_BootstrapPlayableBoards()
        {
            yield return LoadScene(Match3Scene);
            yield return WaitFrames(10);
            object field = FindRequiredComponent("Neo.GridSystem.FieldGenerator", "Match3 field generator");
            FindRequiredComponent("Neo.GridSystem.Match3.Match3BoardService", "Match3 board service");
            Assert.That(GetProperty<Array>(field, "Cells"), Is.Not.Null, "Match3 demo should generate field cells.");

            yield return LoadScene(TicTacToeScene);
            yield return WaitFrames(10);
            field = FindRequiredComponent("Neo.GridSystem.FieldGenerator", "TicTacToe field generator");
            FindRequiredComponent("Neo.GridSystem.TicTacToe.TicTacToeBoardService", "TicTacToe board service");
            FindRequiredComponent("Neo.Demo.GridSystem.GridSystemTicTacToeBoardView", "TicTacToe board view");
            Assert.That(GetProperty<Array>(field, "Cells"), Is.Not.Null, "TicTacToe demo should generate field cells.");
        }

        [UnityTest]
        public IEnumerator DiceMergeDemo_PlacesMergesScoresAndCanReachGameOver()
        {
            yield return LoadScene(DiceMergeScene);
            yield return WaitFrames(12);

            object controller = FindRequiredComponent(
                "Neo.Demo.GridSystem.DiceMergeDemoController",
                "Dice merge demo controller");
            object field = FindRequiredComponent("Neo.GridSystem.FieldGenerator", "Dice field generator");
            object view = FindRequiredComponent("Neo.Demo.GridSystem.DiceMergeDemoView", "Dice merge demo view");
            FindRequiredComponent("Neo.GridSystem.Dice.DiceBoardService", "Dice board service");
            AssertDiceDropCellsReadyForRaycast();

            Type dicePieceType = FindType("Neo.GridSystem.Dice.DicePiece");
            Assert.That(dicePieceType, Is.Not.Null);
            object pair = dicePieceType.GetMethod("Pair", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { 4, 2 });
            Invoke(controller, "ForceCurrentPieceForTest", pair);

            Vector3 releaseWorld = GetCellWorldCenter(field, 2, 2);
            object placed = Invoke(view, "SimulateDragDropForTest", releaseWorld);
            Assert.That(placed, Is.EqualTo(true),
                "Dice demo drag/drop simulation should place a pair on an empty board.");
            yield return WaitFrames(3);
            AssertDicePlacedPiecesRootHasDieView(2);
            AssertDicePlacedViewsRenderAboveCells();
            SetField(view, "_diceBoard", null);
            Invoke(view, "RefreshAll");
            yield return WaitFrames(2);
            AssertDicePlacedPiecesRootHasDieView(2);

            Invoke(controller, "RestartDemo");
            yield return WaitFrames(2);

            object piece = dicePieceType.GetMethod("Single", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { 1 });
            Invoke(controller, "ForceCurrentPieceForTest", piece);

            SetCellContent(field, 1, 0, 1);
            SetCellContent(field, 0, 1, 1);
            releaseWorld = GetCellWorldCenter(field, 0, 0);
            placed = Invoke(view, "SimulateDragDropForTest", releaseWorld);
            Assert.That(placed, Is.EqualTo(true), "Dice demo drag/drop simulation should place the current piece.");
            yield return WaitFrames(2);

            Assert.That(GetProperty<int>(controller, "Score"), Is.GreaterThan(0));
            AssertDicePlacedPiecesRootHasDieView(1);
            Invoke(view, "RefreshAll");
            Invoke(view, "RefreshAll");
            yield return WaitFrames(3);
            AssertDicePlacedPiecesRootHasDieView(1);
            AssertDicePlacedViewsRenderAboveCells();
            AssertDiceDieViewsKeepConsistentWorldScale();

            Invoke(controller, "FillBoardForGameOverTest");
            yield return null;

            Assert.That(GetProperty<bool>(controller, "GameOver"), Is.True);
        }

        [UnityTest]
        public IEnumerator RpgCombatNpcDemo_RunsCombatAndEnemyHealthBars()
        {
            yield return LoadScene(RpgCombatScene);
            yield return WaitFrames(20);

            object controller =
                FindRequiredComponent("Neo.Rpg.Demo.RpgCombatDemoController", "RPG combat demo controller");
            Assert.That(GetProperty<int>(controller, "EnemyCount"), Is.GreaterThan(0));
            Assert.That(GetProperty<int>(controller, "LivingEnemyCount"), Is.GreaterThan(0));

            for (int i = 0; i < 64 && !GetProperty<bool>(controller, "PlayerDead"); i++)
            {
                Invoke(controller, "PressurePlayer");
                yield return null;
            }

            Assert.That(GetProperty<bool>(controller, "PlayerDead"), Is.True,
                "RPG combat demo should allow enemies to kill the player through the scene controller.");

            object bars = FindRequiredComponent("Neo.Rpg.Demo.RpgWorldHealthBars", "RPG enemy health bars");
            yield return new WaitForSeconds(0.35f);
            Assert.That(GetProperty<int>(bars, "TrackedTargetCount"), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator VampireSurvivorDemo_RunsSpawnsDeathAndEnemyHealthBars()
        {
            yield return LoadScene(VampireScene);
            yield return new WaitForSeconds(1f);

            object controller = FindRequiredComponent(
                "Neo.Rpg.Demo.VampireSurvivorDemoController",
                "Vampire Survivor demo controller");
            Assert.That(GetProperty<int>(controller, "ActiveEnemyCount"), Is.GreaterThan(0));

            for (int i = 0; i < 8 && !GetProperty<bool>(controller, "PlayerDead"); i++)
            {
                Invoke(controller, "DamagePlayer", 9999f);
                yield return null;
            }

            Assert.That(GetProperty<bool>(controller, "PlayerDead"), Is.True,
                "Vampire Survivor demo should enter player death state when the controller applies lethal damage.");

            object bars =
                FindRequiredComponent("Neo.Rpg.Demo.RpgWorldHealthBars", "Vampire Survivor enemy health bars");
            yield return new WaitForSeconds(0.35f);
            Assert.That(GetProperty<int>(bars, "TrackedTargetCount"), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator CoreModuleDemoScenes_LoadAndTickWithoutRuntimeErrors()
        {
            for (int i = 0; i < CoreModuleScenes.Length; i++)
            {
                string scenePath = ResolveScenePath(CoreModuleScenes[i]);
                yield return LoadScene(scenePath);
                yield return WaitFrames(12);

                Scene activeScene = SceneManager.GetActiveScene();
                Assert.That(activeScene.path, Is.EqualTo(scenePath));
                Assert.That(activeScene.rootCount, Is.GreaterThan(0), scenePath);
            }
        }

        [UnityTest]
        public IEnumerator AnimationFlyDemo_ExposesButtonsSlidersAndStartsVisuals()
        {
            yield return LoadScene(AnimationFlyScene);
            yield return WaitFrames(3);

            object controller = FindRequiredComponent(
                "Neo.Samples.AnimationFlyDemoController",
                "AnimationFly demo controller");
            object eventSystem = FindRequiredComponent("UnityEngine.EventSystems.EventSystem", "EventSystem");
            object canvasScaler =
                FindRequiredComponent("UnityEngine.UI.CanvasScaler", "AnimationFly demo canvas scaler");

            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(GetProperty<Vector2>(canvasScaler, "referenceResolution"),
                Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(GetProperty<bool>(controller, "FlyRootBlocksRaycasts"), Is.False,
                "AnimationFly demo fly root must not block button and slider raycasts.");
            Assert.That(GetProperty<int>(controller, "DemoButtonCount"), Is.GreaterThanOrEqualTo(6));
            Assert.That(GetProperty<int>(controller, "DemoSliderCount"), Is.GreaterThanOrEqualTo(6));
            Invoke(controller, "ResetCounters");
            Invoke(controller, "PlayWorldToWallet");
            yield return WaitFrames(3);

            Assert.That(GetProperty<int>(controller, "StartedCounter"), Is.GreaterThan(0),
                "AnimationFly demo should start visual items through its runtime controls.");
            Invoke(controller, "ResetCounters");
            Invoke(controller, "PlaySampleSpriteToUi");
            yield return WaitFrames(3);
            Assert.That(GetProperty<int>(controller, "StartedCounter"), Is.GreaterThan(0),
                "AnimationFly demo should start visual items with a real sample sprite asset.");
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            scenePath = ResolveScenePath(scenePath);
            Scene scene =
                EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!scene.isLoaded)
            {
                yield return null;
            }

            yield return null;
        }

        private static string ResolveScenePath(string relativeOrAbsolutePath)
        {
            if (IsHiddenSampleRoot(relativeOrAbsolutePath))
            {
                Assert.Ignore(
                    "Runtime sample scene smoke requires development/imported samples because Unity does not compile hidden Samples~ scripts. Samples~ wiring is covered by SampleSceneCoverageTests.");
            }

            if (File.Exists(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            string root = GetActiveSampleDemoRoot();
            if (IsHiddenSampleRoot(root))
            {
                Assert.Ignore(
                    "Runtime sample scene smoke requires development/imported samples because Unity does not compile hidden Samples~ scripts. Samples~ wiring is covered by SampleSceneCoverageTests.");
            }

            string relativePath = relativeOrAbsolutePath.StartsWith("Demo/", StringComparison.Ordinal)
                ? relativeOrAbsolutePath.Substring("Demo/".Length)
                : relativeOrAbsolutePath;
            string path = root + "/" + relativePath;
            Assert.That(File.Exists(path), Is.True, "Sample scene was not found: " + path);
            return path;
        }

        private static string GetActiveSampleDemoRoot()
        {
            foreach (string root in SampleRootCandidates)
            {
                if (Directory.Exists(root + "/Demo"))
                {
                    return root + "/Demo";
                }

                if (Directory.Exists(root + "/Demo Scenes"))
                {
                    return root + "/Demo Scenes";
                }

                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string childPathRaw in Directory.EnumerateDirectories(root))
                {
                    string childPath = childPathRaw.Replace('\\', '/');
                    if (Directory.Exists(childPath + "/Demo Scenes"))
                    {
                        return childPath + "/Demo Scenes";
                    }

                    if (Directory.Exists(childPath + "/Demo"))
                    {
                        return childPath + "/Demo";
                    }
                }
            }

            Assert.Fail(
                "No Neoxider Demo sample root found. Expected Assets/Neoxider/Samples/Demo during development, " +
                "Assets/Neoxider/Samples~/Demo for UPM packaging, or Assets/Samples/NeoxiderTools/<version>/Demo Scenes after import.");
            return SampleRootCandidates[0];
        }

        private static bool IsHiddenSampleRoot(string root)
        {
            return root.Replace('\\', '/').Contains("/Samples~/");
        }

        private static IEnumerator WaitFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        private static object FindRequiredComponent(string typeName, string label)
        {
            Type type = FindType(typeName);
            Assert.That(type, Is.Not.Null, $"Type '{typeName}' was not found.");

            Object[] objects = Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is not Component component)
                {
                    continue;
                }

                if (component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded &&
                    component.gameObject.activeInHierarchy)
                {
                    return component;
                }
            }

            Assert.Fail($"Scene is missing active {label} ({typeName}).");
            return null;
        }

        private static void AssertDiceDropCellsReadyForRaycast()
        {
            Type markerType = FindType("Neo.GridSystem.GridCellMarker");
            Assert.That(markerType, Is.Not.Null, "Grid cell marker type was not found.");

            List<Component> markers = FindActiveComponents(markerType);
            Assert.That(markers.Count, Is.EqualTo(25), "Dice demo should expose one raycast marker per 5x5 cell.");

            foreach (Component marker in markers)
            {
                Assert.That(marker.GetComponent<Collider>(), Is.Not.Null,
                    $"{marker.name} should have a Collider so drag/drop release can raycast the target cell.");
            }
        }

        private static void AssertDiceDieViewsKeepConsistentWorldScale()
        {
            Type dieViewType = FindType("Neo.Demo.GridSystem.DiceDieView");
            Assert.That(dieViewType, Is.Not.Null, "Dice die view type was not found.");

            List<Component> views = FindActiveComponents(dieViewType);
            Assert.That(views.Count, Is.GreaterThanOrEqualTo(2),
                "Dice demo should have at least one placed die and one tray die after placement.");

            Vector3 expected = views[0].transform.lossyScale;
            foreach (Component view in views)
            {
                Vector3 actual = view.transform.lossyScale;
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f), view.name);
                Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f), view.name);
                Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f), view.name);
            }
        }

        private static void AssertDicePlacedPiecesRootHasDieView(int minCount)
        {
            Type dieViewType = FindType("Neo.Demo.GridSystem.DiceDieView");
            Assert.That(dieViewType, Is.Not.Null, "Dice die view type was not found.");

            List<Component> views = FindActiveComponents(dieViewType);
            int placedCount = 0;
            foreach (Component view in views)
            {
                Transform current = view.transform;
                while (current != null)
                {
                    if (current.name == "DicePlacedPiecesView")
                    {
                        placedCount++;
                        break;
                    }

                    current = current.parent;
                }
            }

            Assert.That(placedCount, Is.GreaterThanOrEqualTo(minCount),
                "Dice demo should create persistent placed die visuals under DicePlacedPiecesView after placement.");
        }

        private static void AssertDicePlacedViewsRenderAboveCells()
        {
            Type dieViewType = FindType("Neo.Demo.GridSystem.DiceDieView");
            Type markerType = FindType("Neo.GridSystem.GridCellMarker");
            Assert.That(dieViewType, Is.Not.Null, "Dice die view type was not found.");
            Assert.That(markerType, Is.Not.Null, "Grid cell marker type was not found.");

            int maxCellOrder = int.MinValue;
            foreach (Component marker in FindActiveComponents(markerType))
            {
                SpriteRenderer renderer = marker.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    maxCellOrder = Mathf.Max(maxCellOrder, renderer.sortingOrder);
                }
            }

            foreach (Component view in FindActiveComponents(dieViewType))
            {
                if (!HasParentNamed(view.transform, "DicePlacedPiecesView"))
                {
                    continue;
                }

                SpriteRenderer renderer = view.GetComponent<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, view.name);
                Assert.That(renderer.sortingOrder, Is.GreaterThan(maxCellOrder),
                    "Placed dice visuals must render above board cells.");
            }
        }

        private static bool HasParentNamed(Transform transform, string parentName)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == parentName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static List<Component> FindActiveComponents(Type type)
        {
            var result = new List<Component>();
            Object[] objects = Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is Component component &&
                    component.gameObject.scene.IsValid() &&
                    component.gameObject.scene.isLoaded &&
                    component.gameObject.activeInHierarchy)
                {
                    result.Add(component);
                }
            }

            return result;
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"{target.GetType().Name}.{propertyName} property was not found.");
            return (T)property.GetValue(target);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{methodName} method was not found.");
            return method.Invoke(target, args);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name}.{fieldName} field was not found.");
            field.SetValue(target, value);
        }

        private static Vector3 GetCellWorldCenter(object field, int x, int y)
        {
            MethodInfo method = field.GetType().GetMethod(
                "GetCellWorldCenter",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Vector3Int) },
                null);
            Assert.That(method, Is.Not.Null);
            return (Vector3)method.Invoke(field, new object[] { new Vector3Int(x, y, 0) });
        }

        private static void SetCellContent(object field, int x, int y, int value)
        {
            MethodInfo getCell = field.GetType().GetMethod(
                "GetCell",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(int), typeof(int) },
                null);
            Assert.That(getCell, Is.Not.Null);
            object cell = getCell.Invoke(field, new object[] { x, y });
            Assert.That(cell, Is.Not.Null);
            cell.GetType().GetField("ContentId")?.SetValue(cell, value);
            cell.GetType().GetField("IsOccupied")?.SetValue(cell, value != 0);
        }
    }
}
#endif
