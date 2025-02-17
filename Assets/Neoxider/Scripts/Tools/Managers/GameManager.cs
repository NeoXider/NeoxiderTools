using UnityEngine;
using System;

namespace Neo.Tools
{
    public class GameManager : Singleton<GameManager>
    {
        #region Public Properties
        /// <summary>
        /// Possible states of the game.
        /// </summary>
        public enum GameState
        {
            NotStarted,
            Playing,
            Win,
            Lose,
            Pause,
            Other,
        }
        public GameState CurrentState => _state;
        public bool IsPlaying => _state == GameState.Playing;

        #endregion
        [SerializeField]
        private GameState _state;

        #region Initialization Methods

        protected override void OnInstanceCreated()
        {
            base.OnInstanceCreated();
        }

        #endregion

        #region Core Game Methods

        public virtual void StartGame()
        {
            EventManager.GameStart();
            _state = GameState.Playing;
        }

        public virtual void StopGame()
        {
            if (_state == GameState.NotStarted) return;

            _state = GameState.NotStarted;
            EventManager.StopGame();
        }

        public virtual void Lose()
        {
            if (_state != GameState.Playing) return;

            _state = GameState.Lose;
            EventManager.StopGame();
            EventManager.GameOver();
        }

        public virtual void Win()
        {
            if (_state != GameState.Playing) return;

            _state = GameState.Win;
            EventManager.StopGame();
            EventManager.Win();
        }
        #endregion
    }
}