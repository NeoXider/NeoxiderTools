using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Neo.Extensions;
using Neo.Tools;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    // Runs from the Edit test assembly; the runner enters Play Mode for real Unity lifecycle/coroutine behavior.
    public class Audit20261023NeoLevelLifecycleTests
    {
        private readonly List<GameObject> _objects = new();
        private bool _previousErrorsEnabled;
        private ISceneFlowSceneLoader _previousSceneLoader;
        private bool _runtimeContextCaptured;
        private Scene _runtimeScene;

        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True, "The fixture requires native Play Mode.");
            yield return WaitForNativeState(() => true, "the first native Play frame");
            _previousErrorsEnabled = NeoDiagnostics.RuntimeErrorsEnabled;
            _previousSceneLoader = SceneFlowController.SceneLoader;
            _runtimeContextCaptured = true;
            NeoDiagnostics.Configure(errors: true);
            _runtimeScene = SceneManager.CreateScene("Neo.Level lifecycle " + Guid.NewGuid().ToString("N"));
            Assert.That(_runtimeScene.isLoaded, Is.True);
        }

        [UnityTearDown]
        public IEnumerator LeaveRuntime()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    TextLevel label = _objects[i].GetComponent<TextLevel>();
                    if (label != null)
                    {
                        DOTween.Kill(label);
                    }

                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            if (_runtimeScene.IsValid() && _runtimeScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_runtimeScene);
            }

            if (_runtimeContextCaptured)
            {
                SceneFlowController.SceneLoader = _previousSceneLoader;
                NeoDiagnostics.Configure(errors: _previousErrorsEnabled);
                _runtimeContextCaptured = false;
            }
            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }
        }

        [UnityTest]
        public IEnumerator TextLevel_ReenabledBeforeManagerCreationResumesWaiting()
        {
            AssertNoLevelManager();
            TextLevel label = CreateLabel();
            TMP_Text target = label.text;
            List<string> updates = new();
            label.OnTextUpdated = new UnityEvent<string>();
            label.OnTextUpdated.AddListener(updates.Add);
            yield return WaitForStarted(label);
            Assert.That(updates, Is.Empty);
            Assert.That(DOTween.IsTweening(label), Is.False, "No manager snapshot has been bound yet.");
            label.gameObject.SetActive(false);
            yield return WaitForNativeState(() => !label.isActiveAndEnabled, "the disabled waiting label");
            label.gameObject.SetActive(true);
            yield return WaitForNativeState(() => label.isActiveAndEnabled, "the reenabled waiting label");

            LevelManager manager = CreateManager(16);
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            Assert.That(LevelManager.I, Is.SameAs(manager), "The deferred singleton source must resolve the registered owner.");
            yield return WaitForOwnedZeroDurationBinding(label, target, updates, "1", 1);
            Assert.That(label.currentNum, Is.EqualTo(1f));
            Assert.That(updates, Is.EqualTo(new[] { "1" }));
            manager.SetLevel(4);
            Assert.That(published, Is.EqualTo(new[] { 4 }), "The owned manager must publish the tested value once.");
            PrepareOwnedZeroDurationTween(label, target);
            yield return WaitForTweenCompletion(label, "5");

            Assert.That(label.currentNum, Is.EqualTo(5f));
            Assert.That(label.text.text, Is.EqualTo("5"));
            Assert.That(updates, Is.EqualTo(new[] { "1", "5" }));

            manager.SetLevel(6);
            Assert.That(published, Is.EqualTo(new[] { 4, 6 }));
            PrepareOwnedZeroDurationTween(label, target);
            yield return WaitForTweenCompletion(label, "7");
            Assert.That(label.currentNum, Is.EqualTo(7f));
            Assert.That(label.text.text, Is.EqualTo("7"));
            Assert.That(updates, Is.EqualTo(new[] { "1", "5", "7" }));
        }

        [UnityTest]
        public IEnumerator TextLevel_DisabledWhileWaitingDoesNotSubscribeUntilEnabled()
        {
            AssertNoLevelManager();
            TextLevel label = CreateLabel();
            TMP_Text target = label.text;
            List<string> updates = new();
            label.OnTextUpdated = new UnityEvent<string>();
            label.OnTextUpdated.AddListener(updates.Add);
            yield return WaitForStarted(label);
            string waitingText = target.text;
            label.enabled = false;

            LevelManager manager = CreateManager(16);
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            Assert.That(LevelManager.I, Is.SameAs(manager), "OnEnable must use the registered owned singleton.");
            manager.SetLevel(4);
            Assert.That(published, Is.EqualTo(new[] { 4 }));
            yield return WaitForNativeState(() => true, "a manager event while the waiting label is disabled");

            Assert.That(updates, Is.Empty, "A disabled label must not subscribe through a deferred callback.");
            Assert.That(DOTween.IsTweening(label), Is.False, "A disabled waiter must not even queue a manager tween.");
            Assert.That(label.currentNum, Is.Zero);
            Assert.That(target.text, Is.EqualTo(waitingText));
            label.enabled = true;
            Assert.That(label.didStart && label.isActiveAndEnabled, Is.True);
            // OnEnable binds synchronously, but SetText still creates a clock-driven tween at duration zero.
            PrepareOwnedZeroDurationTween(label, target);
            yield return WaitForTweenCompletion(label, "5");
            Assert.That(label.currentNum, Is.EqualTo(5f));
            Assert.That(label.text.text, Is.EqualTo("5"), "The OnEnable snapshot must finish at the manager's current value.");
            Assert.That(updates, Is.EqualTo(new[] { "5" }));
            manager.SetLevel(6);
            Assert.That(published, Is.EqualTo(new[] { 4, 6 }));
            PrepareOwnedZeroDurationTween(label, target);
            yield return WaitForTweenCompletion(label, "7");

            Assert.That(label.currentNum, Is.EqualTo(7f));
            Assert.That(label.text.text, Is.EqualTo("7"));
            Assert.That(updates, Is.EqualTo(new[] { "5", "7" }));
        }

        [UnityTest]
        public IEnumerator TextLevel_BoundIntegerEventsFinishExactlyAndRetainFormatting()
        {
            LevelManager manager = CreateManager(int.MaxValue);
            TextLevel label = CreateLabel(1f, manager);
            SetSerializedField(typeof(SetText), label, "startAdd", "Level ");
            SetSerializedField(typeof(SetText), label, "endAdd", " XP");
            string lastText = null;
            label.OnTextUpdated = new UnityEvent<string>();
            label.OnTextUpdated.AddListener(value => lastText = value);
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            yield return WaitForStarted(label);

            int[] levels = { 16777216, int.MaxValue - 1, 0, int.MaxValue - 1, int.MinValue, int.MaxValue };
            int[] offsets = { 1, int.MaxValue, int.MinValue, int.MinValue, int.MinValue, int.MaxValue };
            string[] plain = { "16777217", "4294967293", "-2147483648", "-2", "-4294967296", "4294967294" };
            string[] grouped =
                { "16,777,217", "4,294,967,293", "-2,147,483,648", "-2", "-4,294,967,296", "4,294,967,294" };
            for (int i = 0; i < levels.Length; i++)
            {
                label.IndexOffset = offsets[i];
                label.NumberNotationStyle = NumberNotation.Plain;
                if (i < 4)
                {
                    manager.SetLevel(levels[i]);
                }
                else
                {
                    // Exercise the receiver's full signed signal range independently of selection clamping.
                    manager.OnChangeLevel.Invoke(levels[i]);
                }
                Assert.That(published, Has.Count.EqualTo(i + 1));
                Assert.That(published[i], Is.EqualTo(levels[i]));
                PrepareOwnedTween(label, true);
                yield return WaitForTweenCompletion(label, "Level " + plain[i] + " XP");

                Assert.That(label.text.text, Is.EqualTo("Level " + plain[i] + " XP"));
                Assert.That(lastText, Is.EqualTo("Level " + plain[i] + " XP"));

                label.Separator = ",";
                label.NumberNotationStyle = NumberNotation.Grouped;
                Assert.That(label.text.text, Is.EqualTo("Level " + grouped[i] + " XP"));
                Assert.That(lastText, Is.EqualTo("Level " + grouped[i] + " XP"));
            }
        }

        [UnityTest]
        public IEnumerator TextLevel_ComponentDisableFreezesActiveTweenAndEnableBindsLatestLevel()
        {
            LevelManager manager = CreateManager(100);
            TextLevel label = CreateLabel(1f, manager);
            int updates = 0;
            label.OnTextUpdated = new UnityEvent<string>();
            label.OnTextUpdated.AddListener(value => updates++);
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            yield return WaitForStarted(label);
            label.IndexOffset = 0;
            label.NumberNotationStyle = NumberNotation.Plain;
            manager.SetLevel(80);
            Tween activeTween = PrepareOwnedTween(label, true);
            yield return WaitForNativeState(() => label.currentNum > 0f, "a native tween update");
            Assert.That(label.currentNum, Is.GreaterThan(0f).And.LessThan(80f));

            label.enabled = false;
            Assert.That(activeTween.IsActive(), Is.True);
            Assert.That(activeTween.IsPlaying(), Is.False, "Native OnDisable must pause the owned tween.");
            float frozenNumber = label.currentNum;
            string frozenText = label.text.text;
            int frozenUpdates = updates;
            manager.SetLevel(90);
            yield return WaitForNativeTime(0.5d);

            Assert.That(label.currentNum, Is.EqualTo(frozenNumber));
            Assert.That(label.text.text, Is.EqualTo(frozenText));
            Assert.That(updates, Is.EqualTo(frozenUpdates));

            label.enabled = true;
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "90");
            Assert.That(label.currentNum, Is.EqualTo(90f));
            Assert.That(label.text.text, Is.EqualTo("90"));

            manager.SetLevel(91);
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "91");
            Assert.That(label.text.text, Is.EqualTo("91"));
            Assert.That(published, Is.EqualTo(new[] { 80, 90, 91 }));

            UnityEvent<int> previousEvent = manager.OnChangeLevel;
            label.enabled = false;
            Object.Destroy(manager.gameObject);
            yield return WaitForNativeState(() => manager == null, "destruction of the owned previous manager");
            LevelManager replacement = CreateManager(100);
            replacement.SetLevel(12);
            SetSerializedField(typeof(TextLevel), label, "_levelSource", replacement);
            label.enabled = true;
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "12");
            int replacementUpdates = updates;
            previousEvent.Invoke(14);
            yield return WaitForNativeTime(0.2d);
            Assert.That(label.currentNum, Is.EqualTo(12f));
            Assert.That(label.text.text, Is.EqualTo("12"));
            Assert.That(updates, Is.EqualTo(replacementUpdates), "The previous manager event must remain unsubscribed.");
            Assert.That(DOTween.IsTweening(label), Is.False);
            replacement.SetLevel(13);
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "13");
            Assert.That(label.currentNum, Is.EqualTo(13f));
            Assert.That(label.text.text, Is.EqualTo("13"));
        }

        [UnityTest]
        public IEnumerator TextLevel_ComponentEnableResumesQueuedTweenAndDeferredManagerBinding()
        {
            AssertNoLevelManager();
            TextLevel label = CreateLabel(1f);
            label.NumberNotationStyle = NumberNotation.Plain;
            int updates = 0;
            label.OnTextUpdated = new UnityEvent<string>();
            label.OnTextUpdated.AddListener(value => updates++);
            yield return WaitForStarted(label);
            label.IndexOffset = 0;
            label.enabled = false;
            label.Set(42);
            Tween queuedTween = PrepareOwnedTween(label, false);
            string queuedText = label.text.text;
            int queuedUpdates = updates;
            yield return WaitForNativeTime(0.5d);
            Assert.That(label.currentNum, Is.Zero);
            Assert.That(label.text.text, Is.EqualTo(queuedText));
            Assert.That(updates, Is.EqualTo(queuedUpdates));

            label.enabled = true;
            Assert.That(queuedTween.IsPlaying(), Is.True, "Native OnEnable must resume the queued owned tween.");
            yield return WaitForTweenCompletion(label, "42");
            Assert.That(label.currentNum, Is.EqualTo(42f));
            Assert.That(label.text.text, Is.EqualTo("42"));

            LevelManager manager = CreateManager(100);
            yield return WaitForNativeState(() => DOTween.IsTweening(label), "the deferred manager's initial tween");
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "0");
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            manager.SetLevel(7);
            Assert.That(published, Is.EqualTo(new[] { 7 }));
            PrepareOwnedTween(label, true);
            yield return WaitForTweenCompletion(label, "7");
            Assert.That(label.text.text, Is.EqualTo("7"));
        }

        [UnityTest]
        public IEnumerator RuntimeLevelButton_BindsMissingButtonReferenceAndUnsubscribesOnDestroy()
        {
            LevelManager manager = CreateManager(16);
            List<int> published = new();
            manager.OnChangeLevel.AddListener(published.Add);
            GameObject buttonObject = CreateObject("Runtime level button", typeof(RectTransform));
            buttonObject.SetActive(false);
            Button uiButton = buttonObject.AddComponent<Button>();
            LevelButton levelButton = buttonObject.AddComponent<LevelButton>();
            SetSerializedField(typeof(LevelButton), levelButton, "_button", null);
            levelButton.SetLevelManager(manager);
            levelButton.SetVisual(1, 1);
            buttonObject.SetActive(true);
            Assert.That(levelButton.didAwake, Is.True, "The owned button must receive native Awake.");
            yield return WaitForStarted(levelButton);

            uiButton.onClick.Invoke();
            Assert.That(manager.CurrentLevel, Is.EqualTo(1));
            Assert.That(published, Is.EqualTo(new[] { 1 }), "One native Start subscription must deliver one click.");

            Object.Destroy(levelButton);
            yield return WaitForNativeState(() => levelButton == null, "native destruction of the owned level button");
            manager.SetLevel(0);
            Assert.That(published, Is.EqualTo(new[] { 1, 0 }));
            uiButton.onClick.Invoke();
            Assert.That(manager.CurrentLevel, Is.Zero);
            Assert.That(published, Is.EqualTo(new[] { 1, 0 }), "The destroyed owner must receive no further click.");
        }

        [UnityTest]
        public IEnumerator SceneFlow_RejectsReentrantLoadAndCanRetryAfterNullOperation()
        {
            GameObject controllerObject = CreateObject("Scene flow");
            SceneFlowController controller = controllerObject.AddComponent<SceneFlowController>();
            RecordingLoader loader = new() { Controller = controller, ReenterOnFirstAsyncRequest = true };
            SceneFlowController.SceneLoader = loader;
            controller.LoadMode = SceneFlowLoadMode.AsyncManual;
            int completions = 0;
            controller.OnLoadCompleted.AddListener(() => completions++);

            LogAssert.Expect(LogType.Error, "[SceneFlowController] LoadSceneAsync failed.");
            controller.LoadScene(1);

            Assert.That(loader.ObservedLoading, Is.True);
            Assert.That(loader.AsyncCalls, Is.EqualTo(1));
            Assert.That(loader.SyncCalls, Is.Zero);
            Assert.That(controller.IsLoading, Is.False);

            controller.LoadMode = SceneFlowLoadMode.Async;
            LogAssert.Expect(LogType.Error, "[SceneFlowController] LoadSceneAsync failed.");
            controller.LoadScene("Retry");

            Assert.That(loader.AsyncCalls, Is.EqualTo(2));
            Assert.That(controller.IsLoading, Is.False);
            Assert.That(completions, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneFlow_InactiveAsyncRequestDoesNotReachLoaderOrBlockLaterLoad()
        {
            GameObject controllerObject = CreateObject("Inactive scene flow");
            SceneFlowController controller = controllerObject.AddComponent<SceneFlowController>();
            RecordingLoader loader = new() { Controller = controller };
            SceneFlowController.SceneLoader = loader;
            controller.LoadMode = SceneFlowLoadMode.AsyncManual;
            controllerObject.SetActive(false);

            controller.LoadScene(1);

            Assert.That(loader.AsyncCalls, Is.Zero);
            Assert.That(controller.IsLoading, Is.False);
            controllerObject.SetActive(true);
            controller.LoadMode = SceneFlowLoadMode.Sync;
            controller.LoadScene(2);
            Assert.That(loader.SyncCalls, Is.EqualTo(1));
            Assert.That(loader.LastBuildIndex, Is.EqualTo(2));
            yield return null;
        }

        private TextLevel CreateLabel(float duration = 0f, LevelManager source = null)
        {
            GameObject labelObject = CreateObject("Level label", typeof(RectTransform));
            labelObject.SetActive(false);
            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            TextLevel label = labelObject.AddComponent<TextLevel>();
            label.text = text;
            SetSerializedField(typeof(SetText), label, "_timeAnim", duration);
            SetSerializedField(typeof(TextLevel), label, "_levelSource", source);
            labelObject.SetActive(true);
            Assert.That(label.didAwake, Is.True, "The owned label must receive native Awake.");
            AssertOwnedLabelTarget(label, text);
            return label;
        }

        private LevelManager CreateManager(int levelCount)
        {
            AssertNoLevelManager();
            GameObject managerObject = CreateObject("Late level manager");
            managerObject.SetActive(false);
            LevelManager manager = managerObject.AddComponent<LevelManager>();
            SetSerializedField(typeof(LevelManager), manager, "_saveKey", Guid.NewGuid().ToString("N"));
            if (levelCount > 0)
            {
                SetSerializedField(typeof(LevelManager), manager, "_maps",
                    new[] { new Map { isInfinity = false, countLevels = levelCount } });
            }

            manager.OnChangeLevel = new UnityEvent<int>();
            managerObject.SetActive(true);
            Assert.That(manager.didAwake, Is.True, "The owned manager must register through native Awake.");
            Assert.That(manager.isActiveAndEnabled, Is.True);
            Assert.That(LevelManager.TryGetInstance(out LevelManager registered), Is.True);
            Assert.That(registered, Is.SameAs(manager), "A lazy scene lookup must not stand in for native registration.");
            return manager;
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(_runtimeScene.IsValid() && _runtimeScene.isLoaded, Is.True);
            GameObject instance = new(name, components);
            _objects.Add(instance);
            SceneManager.MoveGameObjectToScene(instance, _runtimeScene);
            Assert.That(Application.IsPlaying(instance), Is.True, "Owned components must belong to the runtime world.");
            return instance;
        }

        private static void AssertNoLevelManager()
        {
            LevelManager[] managers = Object.FindObjectsByType<LevelManager>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(managers, Is.Empty, "The fixture requires no preexisting manager; foreign owners are left intact.");
            Assert.That(LevelManager.TryGetInstance(out LevelManager existing), Is.False,
                "The fixture must not replace or reset a foreign singleton.");
        }

        private static IEnumerator WaitForStarted(MonoBehaviour owner)
        {
            Assert.That(owner.didAwake, Is.True);
            Assert.That(owner.isActiveAndEnabled, Is.True);
            yield return WaitForNativeState(() => owner != null && owner.didStart,
                owner.GetType().Name + ".Start on an owned runtime object");
        }

        private static IEnumerator WaitForNativeState(Func<bool> condition, string description, double? deadline = null)
        {
            Assert.That(Application.isPlaying, Is.True);
            int initialFrame = Time.frameCount;
            double end = deadline ?? Time.realtimeSinceStartupAsDouble + 5d;
            while ((Time.frameCount <= initialFrame || !condition()) && Time.realtimeSinceStartupAsDouble < end)
            {
                yield return null;
            }

            Assert.That(Application.isPlaying, Is.True, description);
            Assert.That(Time.frameCount, Is.GreaterThan(initialFrame), "No native Play frame advanced: " + description);
            Assert.That(condition(), Is.True, "Native precondition did not complete: " + description);
        }

        private static IEnumerator WaitForNativeTime(double seconds)
        {
            double end = Time.realtimeSinceStartupAsDouble + seconds;
            yield return WaitForNativeState(() => Time.realtimeSinceStartupAsDouble >= end,
                "unscaled native time while the owned label is observed");
        }

        private static IEnumerator WaitForTweenCompletion(TextLevel label, string expectedText)
        {
            yield return WaitForNativeState(() => !DOTween.IsTweening(label) && label.text.text == expectedText,
                "the owned tween completion and exact text " + expectedText);
        }

        private static IEnumerator WaitForOwnedZeroDurationBinding(TextLevel label, TMP_Text target,
            List<string> updates, string expectedText, int expectedUpdateCount)
        {
            AssertOwnedLabelTarget(label, target);
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            // Separate a missing deferred callback from a callback that created an unadvanced tween.
            yield return WaitForNativeState(() => DOTween.IsTweening(label) || updates.Count >= expectedUpdateCount,
                "the deferred callback's owned tween or committed output", deadline);
            AssertOwnedLabelTarget(label, target);
            if (DOTween.IsTweening(label))
            {
                PrepareOwnedZeroDurationTween(label, target);
            }

            // A normally advancing clock may have completed the zero-duration tween before observation.
            // Both phases share the original five-second deadline.
            yield return WaitForNativeState(() => !DOTween.IsTweening(label) && target.text == expectedText,
                "the deferred zero-duration tween's exact completion " + expectedText, deadline);
            AssertOwnedLabelTarget(label, target);
            Assert.That(updates, Has.Count.EqualTo(expectedUpdateCount));
            Assert.That(updates[expectedUpdateCount - 1], Is.EqualTo(expectedText));
        }

        private static void AssertOwnedLabelTarget(TextLevel label, TMP_Text target)
        {
            Assert.That(target != null, Is.True, "The owned TMP target must remain live.");
            Assert.That(label.text, Is.SameAs(target), "Binding must not replace the label's configured TMP target.");
            Assert.That(target.gameObject, Is.SameAs(label.gameObject));
            Assert.That(Application.IsPlaying(target), Is.True);
            Assert.That(target.didAwake && target.isActiveAndEnabled, Is.True,
                "The configured TMP component must be initialized in the runtime world.");
        }

        private static void PrepareOwnedZeroDurationTween(TextLevel label, TMP_Text target)
        {
            AssertOwnedLabelTarget(label, target);
            Tween tween = PrepareOwnedTween(label, true);
            Assert.That(tween.Duration(false), Is.Zero, "Keep the original zero-duration fixture input.");
            TestContext.WriteLine("Owned zero-duration label tween: frame=" + Time.frameCount +
                ", timeScale=" + Time.timeScale + ", scaledDelta=" + Time.deltaTime +
                ", unscaledDelta=" + Time.unscaledDeltaTime + ", elapsed=" + tween.Elapsed(false));
        }

        private static void SetSerializedField(Type declaringType, object target, string name, object value)
        {
            FieldInfo field = declaringType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static Tween PrepareOwnedTween(TextLevel label, bool playing)
        {
            List<Tween> tweens = DOTween.TweensByTarget(label);
            Assert.That(tweens, Is.Not.Null, "The public manager event or Set API must create a live owned tween.");
            Assert.That(tweens, Has.Count.EqualTo(1), "The label must own exactly one live tween.");
            Tween tween = tweens[0];
            Assert.That(tween.IsActive(), Is.True);
            Assert.That(tween.IsPlaying(), Is.EqualTo(playing));
            // Advance only this label's tween through the real PlayerLoop, independent of borrowed timeScale.
            tween.SetUpdate(UpdateType.Normal, true).SetEase(Ease.Linear);
            return tween;
        }

        private sealed class RecordingLoader : ISceneFlowSceneLoader
        {
            public SceneFlowController Controller { get; set; }
            public bool ReenterOnFirstAsyncRequest { get; set; }
            public bool ObservedLoading { get; private set; }
            public int AsyncCalls { get; private set; }
            public int SyncCalls { get; private set; }
            public int LastBuildIndex { get; private set; }
            public int ActiveSceneBuildIndex => 0;

            public void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
            {
                SyncCalls++;
                LastBuildIndex = buildIndex;
            }

            public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
            {
                SyncCalls++;
            }

            public AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
            {
                return RecordAsyncRequest();
            }

            public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
            {
                return RecordAsyncRequest();
            }

            private AsyncOperation RecordAsyncRequest()
            {
                AsyncCalls++;
                ObservedLoading = Controller.IsLoading;
                if (ReenterOnFirstAsyncRequest && AsyncCalls == 1)
                {
                    Controller.LoadMode = SceneFlowLoadMode.Sync;
                    Controller.LoadScene(99);
                }

                return null;
            }
        }
    }
}
