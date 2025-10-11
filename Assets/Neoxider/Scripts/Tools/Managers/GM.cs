using Neo.Extensions;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     GameManager
    /// </summary>
    public class GM : Singleton<GM>
    {
        [GUIColor(1,0.5, 0.5)]
        [SerializeField] private GameState _state = GameState.Menu;
        [SerializeField] private GameState _lastState;
        public bool useTimeScalePause = true;
        [Space] public int fps = 60;
        private float lastTimeScale = 1;

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
                _lastState = _state;
                _state = value;
                EM.I.OnStateChange?.Invoke(_state);
                Debug.Log("[GM]".SetColor(Color.yellow)
                          + $": {State}".Gradient(Color.cyan, Color.blue));
            }
        }

        public bool startOnAwake = true;
        public bool IsPlaying => State == GameState.Game;
        public bool IsNotPlaying => State != GameState.Game;

        #endregion

        #region Initialization Methods

        protected override void OnInstanceCreated()
        {
            base.OnInstanceCreated();
        }

        protected override void Init()
        {
            print("GameManager Init");

            if (startOnAwake)
                State = _state;

            Application.targetFrameRate = fps;
        }

        #endregion

        #region Core Game Methods
        
        [Button]
        public virtual void Menu()
        {
            StopGame();
            State = GameState.Menu;
            EM.Menu();
        }
        
        [Button]
        public virtual void Preparing()
        {
            State = GameState.Preparing;
            EM.Preparing();
        }
        
        [Button]
        public virtual void StartGame(bool restart = false)
        {
            if (State == GameState.Pause)
                Resume();

            State = GameState.Game;
            if (restart)
                EM.Restart();
            else
                EM.GameStart();
        }
        
        [Button]
        public virtual void StopGame()
        {
            if (State == GameState.NotStarted) return;

            State = GameState.NotStarted;
            EM.StopGame();
        }
        
        [Button]
        public virtual void Lose()
        {
            if (State != GameState.Game) return;

            State = GameState.Lose;
            EM.StopGame();
            EM.Lose();
        }
        
        [Button]
        public virtual void Win()
        {
            if (State != GameState.Game) return;

            State = GameState.Win;
            EM.StopGame();
            EM.Win();
        }
        
        [Button]
        public virtual void End()
        {
            if (State != GameState.Game) return;

            State = GameState.End;
            EM.StopGame();
            EM.End();
        }
        
        [Button]
        public virtual void Pause()
        {
            if (State == GameState.Pause) return;

            State = GameState.Pause;
            EM.Pause();

            if (useTimeScalePause)
            {
                lastTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
        }
        
        [Button]
        public virtual void Resume()
        {
            if (State != GameState.Pause) return;

            State = _lastState;
            EM.Resume();

            if (useTimeScalePause)
            {
                if (lastTimeScale <= 0)
                    lastTimeScale = 1;

                Time.timeScale = lastTimeScale;
            }
        }

        #endregion
    }
}