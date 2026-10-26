using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    // Uses the coordinator's exclusive runtime test world; global state is restored by TearDown.
    public sealed class Audit20261023NeoUIPausePageTests
    {
        private readonly List<GameObject> _pages = new List<GameObject>();
        private readonly List<GameObject> _managers = new List<GameObject>();
        private Audit20261023NeoUIPauseHub _hub;
        private float _originalScale;
        private int _originalFrameRate;
        private bool _originalCursorVisible;
        private CursorLockMode _originalCursorLock;

        [SetUp]
        public void SetUp()
        {
            _originalScale = Time.timeScale;
            _originalFrameRate = Application.targetFrameRate;
            _originalCursorVisible = Cursor.visible;
            _originalCursorLock = Cursor.lockState;
            GameObject hubObject = new GameObject("PausePage owned event hub");
            _hub = hubObject.AddComponent<Audit20261023NeoUIPauseHub>();
            _hub.OnPause = new UnityEvent();
            _hub.OnResume = new UnityEvent();
            _hub.OnStateChange = new UnityEvent<GM.GameState>();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_hub != null)
                {
                    _hub.OnPause.RemoveAllListeners();
                    _hub.OnResume.RemoveAllListeners();
                    _hub.OnStateChange.RemoveAllListeners();
                }

                foreach (GameObject page in _pages)
                {
                    if (page != null)
                    {
                        Object.DestroyImmediate(page);
                    }
                }

                foreach (GameObject manager in _managers)
                {
                    if (manager != null)
                    {
                        Object.DestroyImmediate(manager);
                    }
                }

                if (_hub != null)
                {
                    Object.DestroyImmediate(_hub.gameObject);
                }
            }
            finally
            {
                _pages.Clear();
                _managers.Clear();
                Time.timeScale = _originalScale;
                Application.targetFrameRate = _originalFrameRate;
                Cursor.lockState = _originalCursorLock;
                Cursor.visible = _originalCursorVisible;
            }
        }

        [UnityTest]
        public IEnumerator SinglePage_RestoresExactBaselineBeforeResumeObservers()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            List<float> observed = new List<float>();
            _hub.OnResume.AddListener(() => observed.Add(Time.timeScale));
            float[] baselines = { 1f, 0.35f, 0f };
            for (int index = 0; index < baselines.Length; index++)
            {
                float baseline = baselines[index];
                Time.timeScale = baseline;
                GameObject page = CreatePage();
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Pause));

                page.SetActive(false);

                Assert.That(Time.timeScale, Is.EqualTo(baseline).Within(0.0001f));
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
                Assert.That(observed.Count, Is.EqualTo(index + 1));
                Assert.That(observed[index], Is.EqualTo(baseline).Within(0.0001f));
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(baseline).Within(0.0001f));
            }

            Assert.That(manager.PauseCalls, Is.EqualTo(3));
            Assert.That(manager.ResumeCalls, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator OverlappingPages_ShareOneGmTransitionInBothClosureOrders()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            for (int order = 0; order < 2; order++)
            {
                Time.timeScale = 0.35f;
                int pausesBefore = manager.PauseCalls;
                int resumesBefore = manager.ResumeCalls;
                GameObject first = CreatePage();
                GameObject second = CreatePage();
                Assert.That(manager.PauseCalls, Is.EqualTo(pausesBefore + 1));

                (order == 0 ? first : second).SetActive(false);

                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Pause));
                Assert.That(manager.ResumeCalls, Is.EqualTo(resumesBefore));
                (order == 0 ? second : first).SetActive(false);
                Assert.That(manager.ResumeCalls, Is.EqualTo(resumesBefore + 1));
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
                Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GmJoiningExistingTimeOwner_CapturesTrueBaselineInBothClosureOrders()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            List<float> observed = new List<float>();
            _hub.OnResume.AddListener(() => observed.Add(Time.timeScale));
            for (int order = 0; order < 2; order++)
            {
                Time.timeScale = 0.35f;
                GameObject timeOwner = CreatePage(sendPause: false);
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
                GameObject gmOwner = CreatePage(useTimeScale: false);
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Pause));

                (order == 0 ? timeOwner : gmOwner).SetActive(false);
                Assert.That(Time.timeScale, Is.Zero);
                (order == 0 ? gmOwner : timeOwner).SetActive(false);

                Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
                Assert.That(observed[order], Is.EqualTo(0.35f).Within(0.0001f));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AlreadyPausedGm_IsNotAcquiredOrResumedByPage()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 0.35f;
            manager.Pause();
            GameObject page = CreatePage();

            page.SetActive(false);

            Assert.That(manager.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(manager.PauseCalls, Is.EqualTo(1));
            Assert.That(manager.ResumeCalls, Is.Zero);
            Assert.That(Time.timeScale, Is.Zero);
            manager.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplacementGm_DoesNotReceivePreviousOwnersResume()
        {
            Audit20261023NeoUIPauseManager original = CreateManager();
            Time.timeScale = 0.35f;
            GameObject page = CreatePage();
            Object.Destroy(original.gameObject);
            yield return null;
            Audit20261023NeoUIPauseManager replacement = CreateManager();
            replacement.Pause();

            page.SetActive(false);

            Assert.That(replacement.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(replacement.PauseCalls, Is.EqualTo(1));
            Assert.That(replacement.ResumeCalls, Is.Zero);
            Assert.That(Time.timeScale, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExternalResumeAndRepause_InvalidatesPagesGmTransition()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 0.35f;
            GameObject page = CreatePage();
            manager.Resume();
            manager.Pause();

            page.SetActive(false);

            Assert.That(manager.ResumeCalls, Is.EqualTo(1));
            Assert.That(manager.PauseCalls, Is.EqualTo(2));
            Assert.That(manager.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            manager.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnPauseObserverClosingPage_ReleasesAfterAcquisitionSettles()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 0.35f;
            GameObject page = CreatePage(activate: false);
            _hub.OnPause.AddListener(() => page.SetActive(false));

            page.SetActive(true);

            Assert.That(page.activeSelf, Is.False);
            Assert.That(manager.PauseCalls, Is.EqualTo(1));
            Assert.That(manager.ResumeCalls, Is.EqualTo(1));
            Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator ThrowingPauseObserverClosingPage_StillReleasesAcquiredGmAndTime()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 0.35f;
            GameObject page = CreatePage(activate: false);
            _hub.OnPause.AddListener(() =>
            {
                page.SetActive(false);
                throw new InvalidOperationException("Expected pause observer fault");
            });
            LogAssert.Expect(LogType.Exception, new Regex("Expected pause observer fault"));

            page.SetActive(true);

            Assert.That(page.activeSelf, Is.False);
            Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(manager.ResumeCalls, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReentrantStateResume_DoesNotTransferOrPublishStalePauseOwnership()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 1f;
            List<string> trace = new List<string>();
            _hub.OnPause.AddListener(() => trace.Add("pause"));
            _hub.OnResume.AddListener(() => trace.Add("resume"));
            _hub.OnStateChange.AddListener((GM.GameState state) =>
            {
                if (state == GM.GameState.Pause)
                {
                    manager.Resume();
                }
            });

            GameObject page = CreatePage();
            Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(trace, Is.EqualTo(new[] { "resume" }));
            page.SetActive(false);

            Assert.That(manager.ResumeCalls, Is.EqualTo(1));
            Assert.That(trace, Is.EqualTo(new[] { "resume" }));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CursorOwners_DeferForceLockUntilLastReleaseAndForgetItForNextGroup()
        {
            Assert.That(Application.isFocused, Is.True, "Native cursor-lock control requires a focused host.");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked),
                "Direct native lock write/read must succeed before any PausePage cursor owner exists.");
            Assert.That(Cursor.visible, Is.False);
            yield return null;
            Assert.That(Application.isFocused, Is.True, "Focus must persist through the native control frame.");
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked),
                "Direct native lock must persist for a frame without PausePage ownership.");
            Assert.That(Cursor.visible, Is.False);

            for (int force = 1; force >= 0; force--)
            {
                for (int order = 0; order < 2; order++)
                {
                    Assert.That(Application.isFocused, Is.True, "The ownership matrix requires the same focused host.");
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = false;
                    CursorLockMode baselineLock = Cursor.lockState;
                    bool baselineVisible = Cursor.visible;
                    Assert.That(baselineLock, Is.EqualTo(CursorLockMode.None));
                    Assert.That(baselineVisible, Is.False);
                    GameObject first = CreatePage(false, false, true, forceLock: force == 1);
                    Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                    Assert.That(Cursor.visible, Is.True);
                    GameObject second = CreatePage(false, false, true);
                    Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                    Assert.That(Cursor.visible, Is.True);
                    yield return null;
                    Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                    Assert.That(Cursor.visible, Is.True);
                    (order == 0 ? first : second).SetActive(false);

                    Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                    Assert.That(Cursor.visible, Is.True);
                    (order == 0 ? second : first).SetActive(false);
                    Assert.That(Cursor.lockState, Is.EqualTo(force == 1 ? CursorLockMode.Locked : baselineLock));
                    Assert.That(Cursor.visible, Is.EqualTo(force == 1 ? false : baselineVisible));
                    yield return null;
                    Assert.That(Application.isFocused, Is.True, "Focus must persist through final cursor restoration.");
                    Assert.That(Cursor.lockState, Is.EqualTo(force == 1 ? CursorLockMode.Locked : baselineLock));
                    Assert.That(Cursor.visible, Is.EqualTo(force == 1 ? false : baselineVisible));
                }
            }
        }

        [UnityTest]
        public IEnumerator TimeAndCursorChannels_ReleaseIndependentlyOfUnrelatedActivePage()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            bool baselineVisible = Cursor.visible;
            GameObject timeOwner = CreatePage(true, false, false, pauseScale: 0.2f);
            GameObject cursorOwner = CreatePage(false, false, true);
            Assert.That(Time.timeScale, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(Cursor.visible, Is.True);

            timeOwner.SetActive(false);

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Cursor.visible, Is.True);
            cursorOwner.SetActive(false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Cursor.visible, Is.EqualTo(baselineVisible));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimeOnlyRequests_UseMinimumAndRetainFirstBaselineUntilLastRelease()
        {
            Time.timeScale = 1f;
            GameObject slower = CreatePage(true, false, pauseScale: 0.2f);
            GameObject faster = CreatePage(true, false, pauseScale: 0.4f);
            Assert.That(Time.timeScale, Is.EqualTo(0.2f).Within(0.0001f));

            slower.SetActive(false);

            Assert.That(Time.timeScale, Is.EqualTo(0.4f).Within(0.0001f));
            faster.SetActive(false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingOptionsWhileOpen_StillReleasesCapturedOwnership()
        {
            Audit20261023NeoUIPauseManager manager = CreateManager();
            Time.timeScale = 0.35f;
            GameObject page = CreatePage(controlCursor: true);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new PageConfiguration()), page.GetComponent<PausePage>());

            page.SetActive(false);

            Assert.That(manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(manager.ResumeCalls, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f).Within(0.0001f));
            yield return null;
        }

        private Audit20261023NeoUIPauseManager CreateManager()
        {
            GameObject root = new GameObject("PausePage owned GM");
            root.SetActive(false);
            _managers.Add(root);
            Audit20261023NeoUIPauseManager manager = root.AddComponent<Audit20261023NeoUIPauseManager>();
            manager.startOnAwake = false;
            root.SetActive(true);
            manager.State = GM.GameState.Game;
            return manager;
        }

        private GameObject CreatePage(bool useTimeScale = true, bool sendPause = true, bool controlCursor = false,
            float pauseScale = 0f, bool forceLock = false, bool activate = true)
        {
            GameObject root = new GameObject("Owned pause page");
            root.SetActive(false);
            _pages.Add(root);
            PausePage page = root.AddComponent<PausePage>();
            PageConfiguration configuration = new PageConfiguration
            {
                _useTimeScale = useTimeScale,
                _timeScaleOnPause = pauseScale,
                _sendPause = sendPause,
                _controlCursor = controlCursor,
                _afterPauseCursor = forceLock
                    ? PausePage.AfterPauseCursorBehavior.ForceLockedHidden
                    : PausePage.AfterPauseCursorBehavior.RestorePrevious
            };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(configuration), page);
            if (activate)
            {
                root.SetActive(true);
            }

            return root;
        }

        [Serializable]
        private sealed class PageConfiguration
        {
            public bool _useTimeScale;
            public float _timeScaleOnPause;
            public bool _sendPause;
            public bool _controlCursor;
            public PausePage.AfterPauseCursorBehavior _afterPauseCursor;
        }
    }

    public sealed class Audit20261023NeoUIPauseManager : GM
    {
        public int PauseCalls { get; private set; }
        public int ResumeCalls { get; private set; }
        protected override bool DontDestroyOnLoadEnabled => false;

        public override void Pause()
        {
            PauseCalls++;
            base.Pause();
        }

        public override void Resume()
        {
            ResumeCalls++;
            base.Resume();
        }
    }

    public sealed class Audit20261023NeoUIPauseHub : EM
    {
        protected override bool DontDestroyOnLoadEnabled => false;
    }
}
