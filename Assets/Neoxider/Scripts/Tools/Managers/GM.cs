using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     GameManager
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Managers/GM")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(GM))]
    [NeoDoc("Tools/Managers/GM.md")]
    public class GM : Singleton<GM>
    {
        [GUIColor(1, 0.5, 0.5)] [SerializeField]
        private GameState _state = GameState.Menu;

        [SerializeField] private GameState _lastState;
        [SerializeField] private bool _debugLog;
        public bool useTimeScalePause = true;
        [Space] public int fps = 60;
        private float lastTimeScale = 1;
        private bool _ownsTimeScalePause;
        private ulong _transitionVersion;
        private bool _isNotifyingStateChange;
        private ulong _lastStateChangeVersion;

        #region Public Properties

        /// <summary>
        ///     Possible states of the game.
        /// </summary>
        public enum GameState
        {
            NotStarted,
            Menu,
            Preparing,
            Game,
            Win,
            Lose,
            End,
            Pause,
            Other
        }

        public GameState State
        {
            get => _state;
            set
            {
                // A listener may repeat its current payload. Keep ordinary explicit notifications,
                // but do not recursively publish the same transition from inside a state callback.
                if (_state == value && _isNotifyingStateChange &&
                    _lastStateChangeVersion == _transitionVersion)
                {
                    return;
                }

                if (_state != value)
                {
                    _transitionVersion = unchecked(_transitionVersion + 1UL);
                    if (_state == GameState.Pause)
                    {
                        RestoreTimeScale();
                    }

                    _lastState = _state;
                }

                _state = value;
                ulong transitionVersion = _transitionVersion;
                if (EM.TryGetInstance(out EM em))
                {
                    bool wasNotifyingStateChange = _isNotifyingStateChange;
                    _isNotifyingStateChange = true;
                    _lastStateChangeVersion = transitionVersion;
                    try
                    {
                        em.OnStateChange?.Invoke(value);
                    }
                    finally
                    {
                        _isNotifyingStateChange = wasNotifyingStateChange;
                    }
                }

                if (_debugLog && IsTransitionCurrent(transitionVersion))
                {
                    NeoDiagnostics.Log("[GM]".SetColor(Color.yellow)
                                       + $": {value}".Gradient(Color.cyan, Color.blue), this);
                }
            }
        }

        public bool startOnAwake = true;
        public bool IsPlaying => State == GameState.Game;
        public bool IsNotPlaying => State != GameState.Game;

        /// <summary>
        ///     Version of the latest effective state transition, advanced before state callbacks.
        ///     Compare with a captured version to detect intervening transitions, including reentry.
        /// </summary>
        public ulong TransitionVersion => _transitionVersion;

        #endregion

        #region Initialization Methods

        protected override void Init()
        {
            base.Init();
            if (_debugLog)
            {
                NeoDiagnostics.Log("[GM] Initialized", this);
            }

            if (startOnAwake)
            {
                State = _state;
            }

            Application.targetFrameRate = fps;
        }

        #endregion

        #region Core Game Methods

        [Button(PlayModeOnly = true)]
        public virtual void Menu()
        {
            if (State == GameState.Pause)
            {
                ulong previousVersion = _transitionVersion;
                ulong resumeVersion = ExpectedTransitionVersion(_lastState);
                Resume();
                if (!IsTransitionCurrent(previousVersion) && !IsTransitionCurrent(resumeVersion))
                {
                    return;
                }
            }

            ulong versionBeforeStop = _transitionVersion;
            ulong stopVersion = ExpectedTransitionVersion(GameState.NotStarted);
            StopGame();
            if (!IsTransitionCurrent(versionBeforeStop) && !IsTransitionCurrent(stopVersion))
            {
                return;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.Menu);
            State = GameState.Menu;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnMenu?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void Preparing()
        {
            ulong transitionVersion = ExpectedTransitionVersion(GameState.Preparing);
            State = GameState.Preparing;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnPreparing?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void StartGame(bool restart = false)
        {
            if (State == GameState.Pause)
            {
                ulong previousVersion = _transitionVersion;
                ulong resumeVersion = ExpectedTransitionVersion(_lastState);
                Resume();
                if (!IsTransitionCurrent(previousVersion) && !IsTransitionCurrent(resumeVersion))
                {
                    return;
                }
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.Game);
            State = GameState.Game;
            if (!TryGetTransitionEvents(transitionVersion, out EM em))
            {
                return;
            }

            if (restart)
            {
                em.OnRestart?.Invoke();
            }
            else
            {
                em.OnGameStart?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void StopGame()
        {
            if (State == GameState.NotStarted)
            {
                return;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.NotStarted);
            State = GameState.NotStarted;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnStopGame?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void Lose()
        {
            if (State != GameState.Game)
            {
                return;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.Lose);
            State = GameState.Lose;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnStopGame?.Invoke();
            }

            if (TryGetTransitionEvents(transitionVersion, out em))
            {
                em.OnLose?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void Win()
        {
            if (State != GameState.Game)
            {
                return;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.Win);
            State = GameState.Win;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnStopGame?.Invoke();
            }

            if (TryGetTransitionEvents(transitionVersion, out em))
            {
                em.OnWin?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void End()
        {
            if (State != GameState.Game)
            {
                return;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.End);
            State = GameState.End;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnStopGame?.Invoke();
            }

            if (TryGetTransitionEvents(transitionVersion, out em))
            {
                em.OnEnd?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void Pause()
        {
            if (State == GameState.Pause)
            {
                return;
            }

            if (useTimeScalePause)
            {
                lastTimeScale = Time.timeScale;
                _ownsTimeScalePause = true;
                Time.timeScale = 0;
            }

            ulong transitionVersion = ExpectedTransitionVersion(GameState.Pause);
            State = GameState.Pause;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnPause?.Invoke();
            }
        }

        [Button(PlayModeOnly = true)]
        public virtual void Resume()
        {
            if (State != GameState.Pause)
            {
                return;
            }

            GameState resumeState = _lastState;
            ulong transitionVersion = ExpectedTransitionVersion(resumeState);
            State = resumeState;
            if (TryGetTransitionEvents(transitionVersion, out EM em))
            {
                em.OnResume?.Invoke();
            }
        }

        private ulong ExpectedTransitionVersion(GameState state)
        {
            return _state == state ? _transitionVersion : unchecked(_transitionVersion + 1UL);
        }

        private bool IsTransitionCurrent(ulong transitionVersion)
        {
            return this != null && _transitionVersion == transitionVersion;
        }

        private bool TryGetTransitionEvents(ulong transitionVersion, out EM em)
        {
            em = null;
            if (!IsTransitionCurrent(transitionVersion))
            {
                return false;
            }

            // Lazy EM initialization can itself change GM state. Fence both sides of resolution.
            em = EM.I;
            return em != null && IsTransitionCurrent(transitionVersion);
        }

        protected override void OnDestroy()
        {
            RestoreTimeScale();
            base.OnDestroy();
        }

        private void RestoreTimeScale()
        {
            if (_ownsTimeScalePause)
            {
                _ownsTimeScalePause = false;
                Time.timeScale = lastTimeScale;
            }
        }

        #endregion
    }
}
