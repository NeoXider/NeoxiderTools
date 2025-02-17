using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    public class EventManager : Singleton<EventManager>
    {

        [Header("Game Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnStopGame;
        public UnityEvent OnWin;
        public UnityEvent OnGameOver;

        [Space]
        [Header("Other")]
        public UnityEvent OnPlayerDeath;


        [Space]
        [Header("Pause")]
        public UnityEvent OnPause;
        public UnityEvent OnResume;

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

        public static void GameStart()
        {
            if (Instance != null)
                Instance.OnGameStart?.Invoke();
        }

        public static void GameOver()
        {
            if (Instance != null)
                Instance.OnGameOver?.Invoke();
        }

        public static void Win()
        {
            if (Instance != null)
                Instance.OnWin?.Invoke();
        }

        public static void PlayerDied()
        {
            if (Instance != null)
                Instance.OnPlayerDeath?.Invoke();
        }

        public static void Pause()
        {
            if (Instance != null)
                Instance.OnPause?.Invoke();
        }

        public static void Resume()
        {
            if (Instance != null)
                Instance.OnResume?.Invoke();
        }

        public static void StopGame()
        {
            if (Instance != null)
                Instance.OnStopGame?.Invoke();
        }
    }
}