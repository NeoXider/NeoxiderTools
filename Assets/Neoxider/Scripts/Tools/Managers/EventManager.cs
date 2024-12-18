using Neoxider.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    public class EventManager : Singleton<EventManager>
    {

        [Header("Game Events")]
        public UnityEvent OnGameStart;
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
        protected override void Initialize()
        {
            base.Initialize();
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
            Instance.OnGameStart?.Invoke();
        }

        public static void GameOver()
        {
            Instance.OnGameOver?.Invoke();
        }

        public static void Win()
        {
            Instance.OnWin?.Invoke();
        }

        public static void PlayerDied()
        {
            Instance.OnPlayerDeath?.Invoke();
        }

        public static void Pause()
        {
            Instance.OnPause?.Invoke();
        }

        public static void Resume()
        {
            Instance.OnResume?.Invoke();
        }
    }
}