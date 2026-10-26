using System;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    public sealed class Audit20261023NeoToolsManagersGameStateTests
    {
        private GameObject _managerObject;
        private GameObject _eventsObject;
        private Audit20261023NeoToolsManagersGameManagerProbe _manager;
        private Audit20261023NeoToolsManagersEventHubProbe _events;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            _eventsObject = new GameObject("Game state audit events");
            _eventsObject.SetActive(false);
            _events = _eventsObject.AddComponent<Audit20261023NeoToolsManagersEventHubProbe>();
            _events.OnPause = new UnityEvent();
            _events.OnResume = new UnityEvent();
            _events.OnMenu = new UnityEvent();
            _events.OnPreparing = new UnityEvent();
            _events.OnGameStart = new UnityEvent();
            _events.OnRestart = new UnityEvent();
            _events.OnStopGame = new UnityEvent();
            _events.OnWin = new UnityEvent();
            _events.OnLose = new UnityEvent();
            _events.OnEnd = new UnityEvent();
            _events.OnStateChange = new UnityEvent<GM.GameState>();
            _events.AttachForTest();
            _managerObject = new GameObject("Game state audit manager");
            _managerObject.SetActive(false);
            _manager = _managerObject.AddComponent<Audit20261023NeoToolsManagersGameManagerProbe>();
            _manager.State = GM.GameState.Game;
        }

        [TearDown]
        public void TearDown()
        {
            if (_manager != null)
            {
                _manager.ReleaseForTest();
                Object.DestroyImmediate(_managerObject);
            }

            if (_events != null)
            {
                _events.ReleaseForTest();
                Object.DestroyImmediate(_eventsObject);
            }

            Time.timeScale = _originalTimeScale;
        }

        [TestCase(GM.GameState.NotStarted)]
        [TestCase(GM.GameState.Preparing)]
        [TestCase(GM.GameState.Menu)]
        [TestCase(GM.GameState.Game)]
        public void LeavingPauseThroughGameplayMethods_RestoresTheCapturedTimeScale(GM.GameState target)
        {
            Time.timeScale = 0.375f;
            _manager.Pause();
            Assert.That(Time.timeScale, Is.Zero);

            switch (target)
            {
                case GM.GameState.NotStarted:
                    _manager.StopGame();
                    break;
                case GM.GameState.Preparing:
                    _manager.Preparing();
                    break;
                case GM.GameState.Menu:
                    _manager.Menu();
                    break;
                case GM.GameState.Game:
                    _manager.StartGame();
                    break;
            }

            Assert.That(_manager.State, Is.EqualTo(target));
            Assert.That(Time.timeScale, Is.EqualTo(0.375f));
        }

        [Test]
        public void DirectStateChange_OutOfPause_RestoresTimeScaleBeforeStateNotification()
        {
            Time.timeScale = 0.5f;
            _manager.Pause();
            float observedTimeScale = -1;
            _events.OnStateChange.AddListener(state => observedTimeScale = Time.timeScale);

            _manager.State = GM.GameState.Other;

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Other));
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            Assert.That(observedTimeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void Resume_PreservesAnAlreadyZeroTimeScale()
        {
            Time.timeScale = 0;
            _manager.Pause();

            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.Zero, "GM must preserve a pause established by another system.");
        }

        [Test]
        public void ChangingPauseOptionWhilePaused_DoesNotLoseTheCapturedTimeScale()
        {
            Time.timeScale = 0.25f;
            _manager.Pause();
            _manager.useTimeScalePause = false;

            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.25f));
        }

        [Test]
        public void AssigningTheSamePauseState_DoesNotOverwriteTheResumeTarget()
        {
            Time.timeScale = 0.5f;
            _manager.Pause();
            _manager.State = GM.GameState.Pause;

            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void OnPauseListener_CanResumeWithoutLeavingTheGameFrozen()
        {
            Time.timeScale = 0.5f;
            _events.OnPause.AddListener(_manager.Resume);

            _manager.Pause();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void PauseStateListener_CanResumeWithoutLeavingTheGameFrozen()
        {
            Time.timeScale = 0.5f;
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnStateChange.AddListener(state =>
            {
                if (state == GM.GameState.Pause)
                {
                    _manager.Resume();
                }
            });

            _manager.Pause();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(new[] { "state:Pause", "state:Game", "resume" }, trace);
        }

        [Test]
        public void ResumeListener_ObservesTheRestoredTimeScale()
        {
            Time.timeScale = 0.5f;
            _manager.Pause();
            float observedTimeScale = -1;
            _events.OnResume.AddListener(() => observedTimeScale = Time.timeScale);

            _manager.Resume();

            Assert.That(observedTimeScale, Is.EqualTo(0.5f));
            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
        }

        [Test]
        public void DestroyingAPausedManager_ReleasesItsTimeScalePause()
        {
            Time.timeScale = 0.5f;
            _manager.Pause();

            _manager.ReleaseForTest();

            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void EventOnlyPause_LeavesTimeScaleUnderExternalControl()
        {
            _manager.useTimeScalePause = false;
            Time.timeScale = 0.5f;
            _manager.Pause();
            Time.timeScale = 0.25f;

            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.25f));
        }

        [TestCase(0f)]
        [TestCase(0.35f)]
        [TestCase(1f)]
        public void Pause_AdvancesThePublicVersionOnceBeforeCallbacks_AndPreservesExactTime(float baseline)
        {
            Time.timeScale = baseline;
            ulong previousVersion = _manager.TransitionVersion;
            List<ulong> versions = new();
            List<float> timeScales = new();
            _events.OnStateChange.AddListener(state =>
            {
                if (state == GM.GameState.Pause)
                {
                    versions.Add(_manager.TransitionVersion);
                    timeScales.Add(Time.timeScale);
                }
            });
            _events.OnPause.AddListener(() =>
            {
                versions.Add(_manager.TransitionVersion);
                timeScales.Add(Time.timeScale);
            });

            _manager.Pause();
            _manager.Pause();

            ulong pauseVersion = unchecked(previousVersion + 1UL);
            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(pauseVersion));
            CollectionAssert.AreEqual(new[] { pauseVersion, pauseVersion }, versions);
            CollectionAssert.AreEqual(new[] { 0f, 0f }, timeScales);

            _manager.Resume();
            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(pauseVersion + 1UL)));
            Assert.That(Time.timeScale, Is.EqualTo(baseline));
        }

        [Test]
        public void SameStateAssignmentOutsideCallbacks_KeepsItsLegacyNotificationWithoutAdvancingVersion()
        {
            ulong previousVersion = _manager.TransitionVersion;
            List<GM.GameState> payloads = new();
            _events.OnStateChange.AddListener(payloads.Add);

            _manager.State = GM.GameState.Game;

            CollectionAssert.AreEqual(new[] { GM.GameState.Game }, payloads);
            Assert.That(_manager.TransitionVersion, Is.EqualTo(previousVersion));
        }

        [Test]
        public void RepeatingTheCurrentStateFromItsCallback_DoesNotRepeatThePayload()
        {
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            int attempts = 0;
            _events.OnStateChange.AddListener(state =>
            {
                attempts++;
                // Keep the regression bounded even if nested no-op dispatch is reintroduced.
                if (attempts < 4)
                {
                    _manager.State = state;
                }
            });

            _manager.Preparing();

            Assert.That(attempts, Is.EqualTo(1));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 1UL)));
            CollectionAssert.AreEqual(new[] { "state:Preparing", "preparing" }, trace);
        }

        [Test]
        public void ThrowingStateObserver_ReleasesTheNotificationGuardWithoutRewindingTheVersion()
        {
            ulong previousVersion = _manager.TransitionVersion;
            UnityAction<GM.GameState> failure = state =>
                throw new InvalidOperationException("Expected state observer failure");
            _events.OnStateChange.AddListener(failure);

            Assert.Throws<InvalidOperationException>(() => _manager.State = GM.GameState.Preparing);

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Preparing));
            ulong committedVersion = unchecked(previousVersion + 1UL);
            Assert.That(_manager.TransitionVersion, Is.EqualTo(committedVersion));
            _events.OnStateChange.RemoveListener(failure);
            List<GM.GameState> payloads = new();
            _events.OnStateChange.AddListener(payloads.Add);

            _manager.State = GM.GameState.Preparing;

            CollectionAssert.AreEqual(new[] { GM.GameState.Preparing }, payloads);
            Assert.That(_manager.TransitionVersion, Is.EqualTo(committedVersion));
        }

        [Test]
        public void ResumeStateListener_CanPauseAgainWithoutAnObsoleteResumeBroadcast()
        {
            Time.timeScale = 0.35f;
            _manager.Pause();
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnStateChange.AddListener(state =>
            {
                if (state == GM.GameState.Game)
                {
                    _manager.Pause();
                }
            });

            _manager.Resume();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(new[] { "state:Game", "state:Pause", "pause" }, trace);
        }

        [Test]
        public void ResumeThenPauseInsideThePauseStateCallback_OnlyBroadcastsTheNewPauseGeneration()
        {
            Time.timeScale = 0.35f;
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            bool redirected = false;
            _events.OnStateChange.AddListener(state =>
            {
                if (state == GM.GameState.Pause && !redirected)
                {
                    redirected = true;
                    _manager.Resume();
                    _manager.Pause();
                    _manager.State = GM.GameState.Pause;
                }
            });

            _manager.Pause();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Pause));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 3UL)));
            CollectionAssert.AreEqual(
                new[] { "state:Pause", "state:Game", "resume", "state:Pause", "pause" }, trace);
        }

        [TestCase(GM.GameState.Win)]
        [TestCase(GM.GameState.Lose)]
        [TestCase(GM.GameState.End)]
        public void TerminalState_StopObserverTransition_CancelsTheOldOutcomeBroadcast(GM.GameState outcome)
        {
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnStopGame.AddListener(_manager.Preparing);

            switch (outcome)
            {
                case GM.GameState.Win:
                    _manager.Win();
                    break;
                case GM.GameState.Lose:
                    _manager.Lose();
                    break;
                case GM.GameState.End:
                    _manager.End();
                    break;
            }

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Preparing));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(
                new[] { "state:" + outcome, "stop", "state:Preparing", "preparing" }, trace);
        }

        [Test]
        public void WinStateObserver_StartsANewGameWithoutOldStopOrWinBroadcasts()
        {
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnStateChange.AddListener(state =>
            {
                if (state == GM.GameState.Win)
                {
                    _manager.StartGame();
                }
            });

            _manager.Win();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Game));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(new[] { "state:Win", "state:Game", "start" }, trace);
        }

        [Test]
        public void Menu_StopObserverTransition_IsNotOverwrittenByTheOuterMenu()
        {
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnStopGame.AddListener(_manager.Preparing);

            _manager.Menu();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Preparing));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(
                new[] { "state:NotStarted", "stop", "state:Preparing", "preparing" }, trace);
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void CompositeFlow_FromPause_StopsWhenAResumeObserverTransitions(bool menu, bool restart)
        {
            Time.timeScale = 0.35f;
            _manager.Pause();
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();
            _events.OnResume.AddListener(_manager.Preparing);

            if (menu)
            {
                _manager.Menu();
            }
            else
            {
                _manager.StartGame(restart);
            }

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Preparing));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(
                new[] { "state:Game", "resume", "state:Preparing", "preparing" }, trace);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CompositeFlow_PreservesAnOverrideThatDoesNotPerformTheOptionalResume(bool menu)
        {
            Time.timeScale = 0.35f;
            _manager.Pause();
            _manager.SkipResume = true;
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();

            if (menu)
            {
                _manager.Menu();
            }
            else
            {
                _manager.StartGame();
            }

            Assert.That(_manager.State, Is.EqualTo(menu ? GM.GameState.Menu : GM.GameState.Game));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f));
            Assert.That(_manager.TransitionVersion,
                Is.EqualTo(unchecked(previousVersion + (menu ? 2UL : 1UL))));
            CollectionAssert.AreEqual(menu
                ? new[] { "state:NotStarted", "stop", "state:Menu", "menu" }
                : new[] { "state:Game", "start" }, trace);
        }

        [Test]
        public void Menu_PreservesAnOverrideThatDoesNotPerformTheOptionalStop()
        {
            _manager.SkipStop = true;
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();

            _manager.Menu();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Menu));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 1UL)));
            CollectionAssert.AreEqual(new[] { "state:Menu", "menu" }, trace);
        }

        [Test]
        public void LazyEventHubAwake_ChangingState_CancelsThePendingPauseBroadcast()
        {
            _events.ReleaseForTest();
            Object.DestroyImmediate(_eventsObject);
            _eventsObject = new GameObject("Lazy game state audit events");
            _eventsObject.SetActive(false);
            _events = _eventsObject.AddComponent<Audit20261023NeoToolsManagersEventHubProbe>();
            _events.OnAwake = new UnityEvent();
            _events.OnPause = new UnityEvent();
            _events.OnStateChange = new UnityEvent<GM.GameState>();
            List<string> trace = new();
            _events.OnAwake.AddListener(() => _manager.State = GM.GameState.Other);
            _events.OnPause.AddListener(() => trace.Add("pause"));
            _events.OnStateChange.AddListener(state => trace.Add("state:" + state));
            Time.timeScale = 0.35f;
            ulong previousVersion = _manager.TransitionVersion;

            _manager.Pause();

            Assert.That(EM.TryGetInstance(out EM resolved), Is.True);
            Assert.That(resolved, Is.SameAs(_events));
            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Other));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 2UL)));
            CollectionAssert.AreEqual(new[] { "state:Other" }, trace);
        }

        [Test]
        public void OrdinaryCommands_PreserveTheirExistingStateAndGameplayEventOrder()
        {
            Time.timeScale = 0.35f;
            ulong previousVersion = _manager.TransitionVersion;
            List<string> trace = CaptureTrace();

            _manager.Pause();
            _manager.Resume();
            _manager.StartGame(true);
            _manager.Menu();
            _manager.Preparing();
            _manager.StartGame();
            _manager.Win();

            Assert.That(_manager.State, Is.EqualTo(GM.GameState.Win));
            Assert.That(Time.timeScale, Is.EqualTo(0.35f));
            Assert.That(_manager.TransitionVersion, Is.EqualTo(unchecked(previousVersion + 7UL)));
            CollectionAssert.AreEqual(new[]
            {
                "state:Pause", "pause", "state:Game", "resume", "state:Game", "restart",
                "state:NotStarted", "stop", "state:Menu", "menu", "state:Preparing", "preparing",
                "state:Game", "start", "state:Win", "stop", "win"
            }, trace);
        }

        private List<string> CaptureTrace()
        {
            List<string> trace = new();
            _events.OnStateChange.AddListener(state => trace.Add("state:" + state));
            _events.OnPause.AddListener(() => trace.Add("pause"));
            _events.OnResume.AddListener(() => trace.Add("resume"));
            _events.OnMenu.AddListener(() => trace.Add("menu"));
            _events.OnPreparing.AddListener(() => trace.Add("preparing"));
            _events.OnGameStart.AddListener(() => trace.Add("start"));
            _events.OnRestart.AddListener(() => trace.Add("restart"));
            _events.OnStopGame.AddListener(() => trace.Add("stop"));
            _events.OnWin.AddListener(() => trace.Add("win"));
            _events.OnLose.AddListener(() => trace.Add("lose"));
            _events.OnEnd.AddListener(() => trace.Add("end"));
            return trace;
        }
    }




}
