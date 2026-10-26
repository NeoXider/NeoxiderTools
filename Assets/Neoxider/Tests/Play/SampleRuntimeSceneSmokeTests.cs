#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.UI;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
                AssertFilledImagesHaveSprites();
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

        [UnityTest]
        public IEnumerator InventoryDemo_DepositsPlacesRecallsAndConsumesExactSelection()
        {
            yield return LoadScene("Scenes/Inventory/InventoryUseDemo.unity");
            object controller = FindRequiredComponent("Neo.Samples.InventoryDemoController", "inventory demo");
            Canvas inventoryCanvas = ((Component)controller).GetComponentInChildren<Canvas>();
            Assert.That(inventoryCanvas, Is.Not.Null);
            AssertRectInsideViewport((RectTransform)inventoryCanvas.transform.Find("Content"), "inventory controls");
            InventoryHand hand = Object.FindFirstObjectByType<InventoryHand>();
            Assert.That(hand, Is.Not.Null);
            InventoryComponent hotbar = hand.Inventory;
            InventoryComponent catchBag = GameObject.Find("Temporary catch - four slots").GetComponent<InventoryComponent>();
            InventoryComponent podium = GameObject.Find("Podium - exact unique instance").GetComponent<InventoryComponent>();
            InventoryItemInstance selected = catchBag.GetSlot(1).Instance;
            Assert.That(hotbar.SlotCapacity, Is.EqualTo(9));
            Assert.That(GetProperty<int>(controller, "Health"), Is.EqualTo(45));

            ClickDemoButton("Btn_Deposit catch");
            Assert.That(catchBag.IsEmpty, Is.True);
            Assert.That(hotbar.GetSlot(2).Instance.InstanceId, Is.EqualTo(selected.InstanceId));
            hand.SetSlotIndex(2);
            ClickDemoButton("Btn_Use selected [E]");
            Assert.That(hotbar.GetSlot(2).IsEmpty, Is.True);
            Assert.That(podium.GetSlot(0).Instance.InstanceId, Is.EqualTo(selected.InstanceId));
            Assert.That(podium.GetSlot(0).Instance.ComponentStates[0].Json, Is.EqualTo(selected.ComponentStates[0].Json));
            ClickDemoButton("Btn_Recall from podium");
            Assert.That(podium.IsEmpty, Is.True);
            Assert.That(hotbar.GetSlot(2).Instance.InstanceId, Is.EqualTo(selected.InstanceId));
            hand.SetSlotIndex(0);
            ClickDemoButton("Btn_Use selected [E]");
            Assert.That(hotbar.GetSlot(0).Count, Is.EqualTo(2));
            Assert.That(GetProperty<int>(controller, "Health"), Is.EqualTo(70));
            Assert.That(GetProperty<bool>(controller, "IsGoalComplete"), Is.True);
            ClickDemoButton("Btn_Reset demo");
            Assert.That(GetProperty<int>(controller, "Health"), Is.EqualTo(45));
            Assert.That(GetProperty<bool>(controller, "IsGoalComplete"), Is.False);
            Assert.That(hotbar.GetSlot(0).Count, Is.EqualTo(3));
            Assert.That(catchBag.TotalItemCount, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AbilitiesGuide_PreservesNoCodeHealAndShowsLevelScaledDamageAndReset()
        {
            yield return LoadScene("Scenes/Abilities/AbilitiesNoCodeDemo.unity");
            object guide = FindRequiredComponent("Neo.Samples.AbilitiesDemoGuide", "abilities guide");
            AssertFilledImagesHaveSprites();
            AssertRectInsideViewport(GameObject.Find("How to play").GetComponent<RectTransform>(), "abilities guide");
            AssertRectInsideViewport(GameObject.Find("Btn_Cast Heal").GetComponent<RectTransform>(), "authored heal control");
            AssertRectInsideViewport(GameObject.Find("Btn_Hit Player 20").GetComponent<RectTransform>(), "authored damage control");
            Type unitType = FindType("Neo.Abilities.AbilityUnitBehaviour");
            object player = GameObject.Find("Player").GetComponent(unitType);
            object dummy = GameObject.Find("TrainingDummy").GetComponent(unitType);
            float initialPlayer = GetProperty<float>(player, "CurrentHealth");
            float initialDummy = GetProperty<float>(dummy, "CurrentHealth");
            ClickDemoButton("Btn_Hit Player 20");
            Assert.That(GetProperty<float>(player, "CurrentHealth"), Is.EqualTo(initialPlayer - 20f).Within(0.01f));
            ClickDemoButton("Btn_Cast Heal");
            Assert.That(GetProperty<float>(player, "CurrentHealth"), Is.EqualTo(initialPlayer - 5f).Within(0.01f));
            ClickDemoButton("Btn_Cast Heal");
            Assert.That(GetProperty<float>(player, "CurrentHealth"), Is.EqualTo(initialPlayer - 5f).Within(0.01f),
                "A second immediate cast must be rejected by the real cooldown.");
            Invoke(guide, "Zap");
            Assert.That(GetProperty<float>(dummy, "CurrentHealth"), Is.EqualTo(initialDummy - 12f).Within(0.01f));
            Assert.That(GetProperty<float>(guide, "ZapCooldown"), Is.GreaterThan(0f));
            Invoke(guide, "NextLevel");
            Invoke(guide, "NextLevel");
            double cooldownDeadline = Time.realtimeSinceStartupAsDouble + 6d;
            while (GetProperty<float>(guide, "ZapCooldown") > 0f)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(cooldownDeadline),
                    "Native auto-tick did not finish Zap cooldown. Last UI result: " + GetProperty<string>(guide, "LastResult"));
                yield return null;
            }
            Invoke(guide, "Zap");
            Assert.That(GetProperty<float>(dummy, "CurrentHealth"), Is.EqualTo(initialDummy - 42f).Within(0.01f),
                GetProperty<string>(guide, "LastResult"));
            Invoke(guide, "ResetDemo");
            Assert.That(GetProperty<float>(player, "CurrentHealth"), Is.EqualTo(initialPlayer).Within(0.01f));
            Assert.That(GetProperty<float>(dummy, "CurrentHealth"), Is.EqualTo(initialDummy).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CardGame_HandCardsKeepCanvasScaleAndStayInsideViewport()
        {
            yield return LoadScene("Scenes/_ExampleGame/CardGame.unity");
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            GameObject gamePage = null;
            foreach (GameObject candidate in objects)
            {
                if (candidate.scene == scene && candidate.name == "Game Page") gamePage = candidate;
                if (candidate.scene == scene && candidate.name == "Menu Page") candidate.SetActive(false);
            }

            Assert.That(gamePage, Is.Not.Null, "The legacy game sample must include its gameplay page.");
            gamePage.SetActive(true);
            yield return WaitFrames(5);
            object game = FindRequiredComponent("Neo.Cards.DrunkardGame", "card game");
            Assert.That(GetProperty<int>(game, "PlayerCardCount") + GetProperty<int>(game, "OpponentCardCount"),
                Is.EqualTo(36), "The same full deck must be dealt before checking presentation.");
            foreach (string property in new[] { "PlayerHand", "OpponentHand" })
            {
                object hand = GetProperty<object>(game, property);
                IEnumerable cards = GetProperty<IEnumerable>(hand, "Cards");
                int count = 0;
                foreach (object card in cards)
                {
                    RectTransform rect = ((Component)card).transform as RectTransform;
                    Assert.That(rect, Is.Not.Null);
                    Assert.That(rect.localScale.x, Is.EqualTo(0.7f).Within(0.0001f),
                        "Canvas parenting must retain the prefab's UI scale, rather than inverse Canvas compensation.");
                    Assert.That(rect.localScale.y, Is.EqualTo(0.7f).Within(0.0001f));
                    AssertRectInsideViewport(rect, property + " card");
                    count++;
                }

                Assert.That(count, Is.EqualTo(18));
            }

            Component playerHand = (Component)GetProperty<object>(game, "PlayerHand");
            RectTransform pileButton = (RectTransform)playerHand.transform.Find("Button");
            Assert.That(pileButton, Is.Not.Null);
            Assert.That(EventSystem.current, Is.Not.Null);
            PointerEventData pointer = new(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, pileButton.TransformPoint(pileButton.rect.center))
            };
            List<RaycastResult> hits = new();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "The visible pile must have a rendered UI hit target.");
            int roundsStarted = 0;
            UnityEvent roundStarted = GetProperty<UnityEvent>(game, "OnRoundStarted");
            UnityAction observeRound = () => roundsStarted++;
            roundStarted.AddListener(observeRound);
            try
            {
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(roundsStarted, Is.EqualTo(1),
                    "The actual top graphic must route a click to Play; a foreground empty Button must not swallow it.");
                Assert.That(GetProperty<int>(game, "PlayerCardCount") + GetProperty<int>(game, "OpponentCardCount"),
                    Is.LessThan(36), "The UI click must draw cards, not merely invoke a diagnostic event.");
            }
            finally
            {
                roundStarted.RemoveListener(observeRound);
            }

            yield return WaitFrames(2);
        }

        private static void AssertFilledImagesHaveSprites()
        {
            Image[] images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
            foreach (Image image in images)
            {
                if (image.type != Image.Type.Filled) continue;
                Assert.That(image.sprite, Is.Not.Null,
                    image.name + ": a Filled Image without a sprite renders a full quad regardless of fillAmount.");
            }
        }

        private static void AssertRectInsideViewport(RectTransform rect, string description)
        {
            Assert.That(rect, Is.Not.Null, description);
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corner);
                Assert.That(screen.x, Is.InRange(-0.5f, Screen.width + 0.5f), description + " horizontal clipping");
                Assert.That(screen.y, Is.InRange(-0.5f, Screen.height + 0.5f), description + " vertical clipping");
            }
        }

        private static void ClickDemoButton(string objectName)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                if (button.name != objectName) continue;
                Assert.That(button.interactable, Is.True, objectName);
                button.onClick.Invoke();
                return;
            }
            Assert.Fail("Missing playable demo control: " + objectName);
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            scenePath = ResolveScenePath(scenePath);
            Scene scene =
                EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            double deadline = Time.realtimeSinceStartupAsDouble + 10d;
            while (!scene.isLoaded)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "Sample scene did not load within 10 real-time seconds: " + scenePath);
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
            string demoRoot = FindSampleDemoRoot(SampleRootCandidates);
            if (demoRoot != null)
            {
                return demoRoot;
            }

            // WHY: in a UPM consumer without imported samples there is nothing to smoke — skip, don't fail.
            Assert.Ignore(
                "No Neoxider Demo sample root found. Expected Assets/Neoxider/Samples/Demo during development, " +
                "Assets/Neoxider/Samples~/Demo for UPM packaging, or Assets/Samples/NeoxiderTools/<version>/Demo Scenes after import.");
            return SampleRootCandidates[0];
        }

        // Keep filesystem discovery testable without loading scenes or changing package sample layout.
        internal static string FindSampleDemoRoot(IEnumerable<string> rootCandidates)
        {
            string hiddenDemoRoot = null;
            foreach (string root in rootCandidates)
            {
                string candidate = null;
                if (Directory.Exists(root + "/Demo"))
                {
                    candidate = root + "/Demo";
                }
                else if (Directory.Exists(root + "/Demo Scenes"))
                {
                    candidate = root + "/Demo Scenes";
                }

                if (candidate != null)
                {
                    if (!IsHiddenSampleRoot(candidate))
                    {
                        return candidate;
                    }

                    hiddenDemoRoot ??= candidate;
                    continue;
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

            return hiddenDemoRoot;
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
