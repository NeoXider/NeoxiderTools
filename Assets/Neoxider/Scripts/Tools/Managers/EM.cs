using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    /// <summary>
    /// EventManager GameStates
    /// </summary>
    public class EM : Singleton<EM>
    {
        [Header("Game Events")]
        public UnityEvent OnPreparing;
        public UnityEvent OnGameStart;
        public UnityEvent OnStopGame;
        public UnityEvent OnWin;
        public UnityEvent OnLose;

        [Space]
        [Header("Pause")]
        public UnityEvent OnPause;
        public UnityEvent OnResume;

        [Space]
        [Header("Other")]
        public UnityEvent OnPlayerDeath;

        [Space]
        [Header("Unity")]
        public UnityEvent OnAwake;
        public UnityEvent<bool> OnFocusApplication;
        public UnityEvent<bool> OnPauseApplication;
        public UnityEvent OnQuitApplication;

        #region Unity Callbacks

        protected override void Init()
        {
            base.Init();
            OnAwake?.Invoke();
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            OnFocusApplication?.Invoke(focusStatus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnPauseApplication?.Invoke(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            OnQuitApplication?.Invoke();
        }

        #endregion

        [Button]
        public static void Preparing()
        {
            I?.OnPreparing?.Invoke();
        }

        [Button]
        public static void GameStart()
        {
            I?.OnGameStart?.Invoke();
        }

        [Button]
        public static void Lose()
        {
            I?.OnLose?.Invoke();
        }

        [Button]
        public static void Win()
        {
            I?.OnWin?.Invoke();
        }

        [Button]
        public static void StopGame()
        {
            I?.OnStopGame?.Invoke();
        }

        [Button]
        public static void PlayerDied()
        {
            I?.OnPlayerDeath?.Invoke();
        }

        [Button]
        public static void Pause()
        {
            I?.OnPause?.Invoke();
        }

        [Button]
        public static void Resume()
        {
            I?.OnResume?.Invoke();
        }
    }
}