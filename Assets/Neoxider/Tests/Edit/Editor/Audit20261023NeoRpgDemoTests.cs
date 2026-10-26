using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Match3;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // The Condition sample deliberately demonstrates legacy Health.

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoRpgDemoTests
    {
        private readonly List<Object> _owned = new();
        private bool _ownsPlaySession;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ExceptionDispatchInfo firstFault = null;
            bool deferredDestruction = false;
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (_owned[i] == null) continue;
                    if (Application.isPlaying)
                    {
                        deferredDestruction = true;
                        Object.Destroy(_owned[i]);
                    }
                    else Object.DestroyImmediate(_owned[i]);
                }
                catch (Exception failure)
                {
                    RetainFirstFault(ref firstFault, failure);
                }
            }

            _owned.Clear();
            if (deferredDestruction)
            {
                IEnumerator barrier = null;
                try { barrier = WaitForNativeFrames(); }
                catch (Exception failure) { RetainFirstFault(ref firstFault, failure); }
                if (barrier != null)
                {
                    // Only poll our frame observer here; a fault must not skip the owned mode exit.
                    while (true)
                    {
                        bool pending;
                        try { pending = barrier.MoveNext(); }
                        catch (Exception failure)
                        {
                            RetainFirstFault(ref firstFault, failure);
                            break;
                        }

                        if (!pending) break;
                        yield return null;
                    }

                    try { (barrier as IDisposable)?.Dispose(); }
                    catch (Exception failure) { RetainFirstFault(ref firstFault, failure); }
                }
            }

            try
            {
                if (_ownsPlaySession && EditorApplication.isPlayingOrWillChangePlaymode)
                    yield return new ExitPlayMode();
            }
            finally
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) _ownsPlaySession = false;
                firstFault?.Throw();
            }
        }

        private static void RetainFirstFault(ref ExceptionDispatchInfo firstFault, Exception failure)
        {
            if (firstFault == null) firstFault = ExceptionDispatchInfo.Capture(failure);
        }

        [TestCase(5, 4, 1, 5)]
        [TestCase(5, 4, 3, 13)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(5, -4, 3, 1)]
        [TestCase(int.MaxValue, int.MaxValue, 2, int.MaxValue)]
        [TestCase(5, 4, int.MinValue, 5)]
        public void SurvivorXpThreshold_IsPositiveAndDoesNotOverflow(int baseXp, int step, int level, int expected)
        {
            Type type = RequireDemoType("Neo.Samples.Survivor.SurvivorConfig");
            ScriptableObject config = ScriptableObject.CreateInstance(type);
            _owned.Add(config);
            type.GetField("BaseXp").SetValue(config, baseXp);
            type.GetField("XpPerLevel").SetValue(config, step);

            Assert.That(Invoke(config, "XpForLevel", level), Is.EqualTo(expected));
        }

        [Test]
        public void ConditionActions_UseConfiguredDamageHealingAndScore()
        {
            Component ui = AddDemo("Neo.Demo.Condition.ConditionDemoUI");
            Health health = Host("Health").AddComponent<Health>();
            health.maxHp = 100;
            health.Restore();
            SetReference(ui, "_health", health);
            ScoreManager score = Host("Score").AddComponent<ScoreManager>();
            SerializedObject serializedScore = new(score);
            serializedScore.FindProperty("_bestScore").intValue = int.MaxValue;
            serializedScore.ApplyModifiedPropertiesWithoutUndo();
            SetReference(ui, "_scoreManager", score);
            Invoke(ui, "ConfigureAmounts", 7, 3, 11);

            Invoke(ui, "DealDamage");
            Assert.That(health.HpValue, Is.EqualTo(93));
            Invoke(ui, "HealPlayer");
            Assert.That(health.HpValue, Is.EqualTo(96));
            Invoke(ui, "AddScore");
            Assert.That(score.ScoreValue, Is.EqualTo(11));

            Invoke(ui, "ConfigureAmounts", -7, -3, -11);
            Invoke(ui, "DealDamage");
            Invoke(ui, "HealPlayer");
            Assert.That(health.HpValue, Is.EqualTo(96));
            Invoke(ui, "AddScore");
            Assert.That(score.ScoreValue, Is.EqualTo(11));
        }

        [Test]
        public void Match3Configure_RebindsGenerationAndDisconnectsPreviousField()
        {
            Component view = AddDemo("Neo.Demo.GridSystem.GridSystemMatch3BoardView");
            view.gameObject.SetActive(true);
            FieldGenerator first = Field(2, 2);
            Match3BoardService firstBoard = first.gameObject.AddComponent<Match3BoardService>();
            Invoke(view, "Configure", first, firstBoard, null, null);
            Assert.That(view.transform.childCount, Is.EqualTo(4));

            first.Config.Size = new Vector3Int(3, 2, 1);
            first.GenerateField();
            Assert.That(view.transform.childCount, Is.EqualTo(6));

            FieldGenerator second = Field(1, 3);
            Match3BoardService secondBoard = second.gameObject.AddComponent<Match3BoardService>();
            Invoke(view, "Configure", second, secondBoard, null, null);
            Transform firstNewCell = view.transform.GetChild(0);
            first.GenerateField();
            Assert.That(view.transform.GetChild(0), Is.SameAs(firstNewCell),
                "The previous generator must not rebuild a rebound view.");

            second.Config.Size = new Vector3Int(1, 2, 1);
            second.GenerateField();
            Assert.That(view.transform.childCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GridRebuild_ReplacesEveryCellAndOwnsOneSprite_Match3()
        {
            RequireIdleEditor();
            RequireDemoType("Neo.Demo.GridSystem.GridSystemMatch3BoardView");
            yield return new EnterPlayMode();
            _ownsPlaySession = true;
            yield return VerifyGridRebuildAndSpriteOwnership("Neo.Demo.GridSystem.GridSystemMatch3BoardView", 8);
        }

        [UnityTest]
        public IEnumerator GridRebuild_ReplacesEveryCellAndOwnsOneSprite_TicTacToe()
        {
            RequireIdleEditor();
            RequireDemoType("Neo.Demo.GridSystem.GridSystemTicTacToeBoardView");
            yield return new EnterPlayMode();
            _ownsPlaySession = true;
            yield return VerifyGridRebuildAndSpriteOwnership("Neo.Demo.GridSystem.GridSystemTicTacToeBoardView", 4);
        }

        private void RequireIdleEditor()
        {
            Assert.That(_ownsPlaySession, Is.False, "A previous fixture-owned Play session has not been released.");
            Assert.That(Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode, Is.False,
                "The fixture must acquire its own Play session; a preexisting session is not eligible.");
        }

        private static IEnumerator WaitForNativeFrames()
        {
            const double timeoutSeconds = 10d;
            System.Diagnostics.Stopwatch realtime = System.Diagnostics.Stopwatch.StartNew();
            int previousFrame = Time.frameCount;
            int observedAdvances = 0;
            while (observedAdvances < 2)
            {
                if (!Application.isPlaying)
                    throw new InvalidOperationException("Play Mode ended before the native frame barrier completed.");
                if (realtime.Elapsed.TotalSeconds >= timeoutSeconds)
                    throw new TimeoutException($"Expected two native Time.frameCount advances within {timeoutSeconds}s; " +
                        $"observed {observedAdvances}, last frame {previousFrame}.");

                int currentFrame = Time.frameCount;
                if (currentFrame < previousFrame)
                    throw new InvalidOperationException("The native frame counter reset during deferred destruction.");
                if (currentFrame > previousFrame)
                {
                    observedAdvances++;
                    previousFrame = currentFrame;
                }

                if (observedAdvances < 2) yield return null;
            }
        }

        private IEnumerator VerifyGridRebuildAndSpriteOwnership(string typeName, int rendererCount)
        {
            Assert.That(Application.isPlaying, Is.True, "Ownership needs an admitted native component lifecycle.");
            FieldGenerator field = Field(2, 2);
            field.gameObject.SetActive(true);
            Component view = AddDemo(typeName);
            SetReference(view, "_generator", field);
            view.gameObject.SetActive(true);
            Assert.That(view.transform.childCount, Is.EqualTo(4),
                "Native OnEnable must build the cells before any explicit Rebuild call.");
            SpriteRenderer[] initialRenderers = view.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(initialRenderers.Length, Is.EqualTo(rendererCount));
            Sprite sprite = initialRenderers[0].sprite;
            Assert.That(sprite, Is.Not.Null);
            Texture2D sharedTexture = Texture2D.whiteTexture;
            Assert.That(sprite.texture, Is.SameAs(sharedTexture));

            Component otherView = AddDemo(typeName);
            SetReference(otherView, "_generator", field);
            otherView.gameObject.SetActive(true);
            Assert.That(otherView.transform.childCount, Is.EqualTo(4));
            Sprite otherSprite = otherView.GetComponentInChildren<SpriteRenderer>().sprite;
            Assert.That(otherSprite, Is.Not.Null.And.Not.SameAs(sprite));
            Sprite borrowedArt = Sprite.Create(sharedTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            _owned.Add(borrowedArt);
            SpriteRenderer borrowedRenderer = Host("Borrowed art").AddComponent<SpriteRenderer>();
            borrowedRenderer.sprite = borrowedArt;
            borrowedRenderer.gameObject.SetActive(true);

            for (int rebuild = 0; rebuild < 3; rebuild++)
            {
                Transform[] oldCells = CellRoots(view.transform);
                Invoke(view, "Rebuild");
                // Edit-runner polls are not native frames; observe the actual player frame counter.
                yield return WaitForNativeFrames();
                Assert.That(view.transform.childCount, Is.EqualTo(4), "Rebuild must remove all previous cells.");
                foreach (Transform oldCell in oldCells)
                    Assert.That(oldCell == null, Is.True, "Every old cell must be replaced, not reused or left behind.");
                SpriteRenderer[] renderers = view.GetComponentsInChildren<SpriteRenderer>();
                Assert.That(renderers.Length, Is.EqualTo(rendererCount));
                foreach (SpriteRenderer renderer in renderers) Assert.That(renderer.sprite, Is.SameAs(sprite));
            }

            Transform[] finalCells = CellRoots(view.transform);
            Object.Destroy(view.gameObject);
            yield return WaitForNativeFrames();
            Assert.That(view == null, Is.True);
            foreach (Transform cell in finalCells) Assert.That(cell == null, Is.True);
            Assert.That(sprite == null, Is.True, "Destroying the view must release its generated sprite.");
            Assert.That(sharedTexture != null && Texture2D.whiteTexture == sharedTexture, Is.True,
                "The shared Unity texture must remain alive.");
            Assert.That(otherView != null && otherSprite != null, Is.True,
                "Removing one owner must preserve another view and its independently generated sprite.");
            Assert.That(otherView.transform.childCount, Is.EqualTo(4));
            SpriteRenderer[] otherRenderers = otherView.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(otherRenderers.Length, Is.EqualTo(rendererCount));
            foreach (SpriteRenderer renderer in otherRenderers) Assert.That(renderer.sprite, Is.SameAs(otherSprite));
            Assert.That(borrowedArt != null && borrowedRenderer != null, Is.True);
            Assert.That(borrowedRenderer.sprite, Is.SameAs(borrowedArt));
            Assert.That(field != null && field.gameObject.activeInHierarchy, Is.True,
                "The borrowed generator must survive the view owner.");
        }

        private static Transform[] CellRoots(Transform owner)
        {
            Transform[] cells = new Transform[owner.childCount];
            for (int i = 0; i < cells.Length; i++) cells[i] = owner.GetChild(i);
            return cells;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DiceConfigure_WithMissingRepairAssets_KeepsDiceVisible(bool useAuthoredPrefab)
        {
            Component view = AddDemo("Neo.Demo.GridSystem.DiceMergeDemoView");
            view.gameObject.SetActive(true);
            FieldGenerator field = Field(2, 2);
            DiceBoardService board = field.gameObject.AddComponent<DiceBoardService>();
            board.ClearBoard();
            Component controller = field.gameObject.AddComponent(RequireDemoType("Neo.Demo.GridSystem.DiceMergeDemoController"));
            Invoke(controller, "Configure", field, board, null, null, null);
            Invoke(controller, "ForceCurrentPieceForTest", DicePiece.Single(2));

            Component prefab = null;
            Sprite authoredSprite = null;
            if (useAuthoredPrefab)
            {
                prefab = AddDemo("Neo.Demo.GridSystem.DiceDieView");
                prefab.gameObject.SetActive(true);
                prefab.transform.localScale = new Vector3(0.31f, 0.37f, 1f);
                authoredSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
                _owned.Add(authoredSprite);
                Sprite[] sprites = new Sprite[9];
                sprites[1] = authoredSprite;
                Invoke(view, "Configure", field, board, controller, null, null, null, prefab, sprites);
            }

            Invoke(view, "Configure", field, board, controller, null, null, null, null, null);
            Transform tray = view.transform.Find("DiceTrayView");
            Assert.That(tray.childCount, Is.EqualTo(1));
            SpriteRenderer renderer = tray.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.gameObject.activeInHierarchy, Is.True);
            if (useAuthoredPrefab)
            {
                Assert.That(renderer.sprite, Is.SameAs(authoredSprite));
                Assert.That(renderer.transform.lossyScale.x, Is.EqualTo(0.31f).Within(0.001f));
            }
            else
            {
                TMPro.TMP_Text value = tray.GetComponentInChildren<TMPro.TMP_Text>(true);
                Assert.That(value, Is.Not.Null, "Fallback dice need a number when no pip sprite exists.");
                Assert.That(value.text, Is.EqualTo("2"));
                Assert.That(value.gameObject.activeInHierarchy, Is.True);
            }

            Assert.That(Invoke(controller, "TryPlaceCurrentPiece", Vector3Int.zero), Is.EqualTo(true));
            Invoke(view, "RefreshAll");
            Transform pieces = view.transform.Find("DicePlacedPiecesView");
            Assert.That(pieces.childCount, Is.EqualTo(1));
            Assert.That(pieces.GetComponentInChildren<SpriteRenderer>(true).sprite, Is.Not.Null);
        }

        [Test]
        public void DiceConfigure_DisconnectsPreviousGenerator()
        {
            Component view = AddDemo("Neo.Demo.GridSystem.DiceMergeDemoView");
            view.gameObject.SetActive(true);
            FieldGenerator first = Field(2, 2);
            DiceBoardService firstBoard = first.gameObject.AddComponent<DiceBoardService>();
            Invoke(view, "Configure", first, firstBoard, null, null, null, null, null, null);
            FieldGenerator second = Field(2, 2);
            DiceBoardService secondBoard = second.gameObject.AddComponent<DiceBoardService>();
            Invoke(view, "Configure", second, secondBoard, null, null, null, null, null, null);
            Transform boardRoot = view.transform.Find("DiceBoardView");

            first.OnFieldGenerated.Invoke();
            Assert.That(view.transform.Find("DiceBoardView"), Is.SameAs(boardRoot));
            second.Config.Size = new Vector3Int(1, 2, 1);
            second.GenerateField();
            Assert.That(view.transform.Find("DiceBoardView").childCount, Is.EqualTo(3),
                "The new field should create two cells and its backdrop.");
        }

        [Test]
        public void DiceRestart_WithoutServices_ReportsUnavailableState()
        {
            Component controller = AddDemo("Neo.Demo.GridSystem.DiceMergeDemoController");
            Invoke(controller, "RestartDemo");
            Assert.That(controller.GetType().GetProperty("GameOver").GetValue(controller), Is.EqualTo(true));
            Assert.That(controller.GetType().GetProperty("CurrentPiece").GetValue(controller), Is.Null);
        }

        private FieldGenerator Field(int width, int height)
        {
            GameObject host = Host("Field");
            host.AddComponent<Grid>();
            FieldGenerator field = host.AddComponent<FieldGenerator>();
            field.DebugEnabled = false;
            field.Config.Size = new Vector3Int(width, height, 1);
            field.GenerateField();
            return field;
        }

        private Component AddDemo(string name) => Host(name).AddComponent(RequireDemoType(name));

        private GameObject Host(string name)
        {
            GameObject host = new("Audit20261023NeoRpgDemo " + name);
            host.SetActive(false);
            _owned.Add(host);
            return host;
        }

        private static void SetReference(Component target, string field, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
