using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    /// GameManager
    /// </summary>
    public class GM : Singleton<GM>
    {
        #region Public Properties

        /// <summary>
        /// Possible states of the game.
        /// </summary>
        public enum GameState
        {
            NotStarted,
            Preparing,
            Playing,
            Win,
            Lose,
            Pause,
            Other,
        }

        public GameState State
        {
            get => _state;
            set => _state = value;
        }

        public bool IsPlaying => _state == GameState.Playing;
        public bool IsNotPlaying => _state != GameState.Playing;


        [SerializeField] [RequireInterface(typeof(IGameState))]
        private GameObject[] _iGameInstallers;

        #endregion

        [SerializeField] private GameState _state;

        public List<IGameState> IGameInstallers = new List<IGameState>();

        #region Initialization Methods

        protected override void OnInstanceCreated()
        {
            base.OnInstanceCreated();
        }

        protected override void Init()
        {
            print("GameManager Init");
            for (int i = 0; i < _iGameInstallers.Length; i++)
            {
                IGameInstallers.Add(_iGameInstallers[i].GetComponent<IGameState>());
            }
        }

        #endregion

        #region Core Game Methods

        [Button]
        public virtual void Preparing()
        {
            _state = GameState.Preparing;
            EM.Preparing();

            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.Prepare();
            }
        }

        [Button]
        public virtual void StartGame()
        {
            _state = GameState.Playing;
            EM.GameStart();

            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.StartGame();
            }
        }

        [Button]
        public virtual void StopGame()
        {
            if (_state == GameState.NotStarted) return;

            _state = GameState.NotStarted;
            EM.StopGame();

            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.StopGame();
            }
        }

        [Button]
        public virtual void Lose()
        {
            if (_state != GameState.Playing) return;

            _state = GameState.Lose;
            EM.StopGame();
            EM.Lose();

            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.Lose();
            }
        }

        [Button]
        public virtual void Win()
        {
            if (_state != GameState.Playing) return;

            _state = GameState.Win;
            EM.StopGame();
            EM.Win();

            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.Win();
            }
        }

        [Button]
        public virtual void Pause()
        {
            _state = GameState.Pause;
            EM.Pause();
            
            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.Pause();
            }
        }

        [Button]
        public virtual void Resume()
        {
            _state = GameState.Playing;
            EM.Resume();
            
            foreach (var iGameInstaller in IGameInstallers)
            {
                iGameInstaller?.Resume();
            }
        }

        #endregion

        public void AddInstaller(IGameState gameState)
        {
            if (!IGameInstallers.Contains(gameState))
            {
                IGameInstallers.Add(gameState);
            }
        }

        public void RemoveInstaller(IGameState gameState)
        {
            if (IGameInstallers.Contains(gameState))
            {
                IGameInstallers.Remove(gameState);
            }
        }
    }
}