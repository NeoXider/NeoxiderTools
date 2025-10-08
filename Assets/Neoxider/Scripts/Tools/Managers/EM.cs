using Neo.Tools;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
        /// <summary>
        ///     EventManager GameStates
        /// </summary>
        public class EM : Singleton<EM>
    {
        [FormerlySerializedAs("OnMenuState")] [Header("Game Events")]
        public UnityEvent OnMenu;

        public UnityEvent OnPreparing;
        public UnityEvent OnGameStart;

        [FormerlySerializedAs("OnRetart")] [FormerlySerializedAs("OnReStart")]
        public UnityEvent OnRestart;

        public UnityEvent OnStopGame;
        public UnityEvent OnWin;
        public UnityEvent OnLose;
        public UnityEvent OnEnd;

        [Space] public UnityEvent<GM.GameState> OnStateChange;

        [Space] [Header("Pause")] public UnityEvent OnPause;
        public UnityEvent OnResume;

        [Space] [Header("Other")] public UnityEvent OnPlayerDeath;

        [Space] [Header("Unity")] public UnityEvent OnAwake;
        public UnityEvent<bool> OnFocusApplication;
        public UnityEvent<bool> OnPauseApplication;
        public UnityEvent OnQuitApplication;

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Preparing()
        {
            I?.OnPreparing?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void GameStart()
        {
            I?.OnGameStart?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Lose()
        {
            I?.OnLose?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Win()
        {
            I?.OnWin?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void End()
        {
            I?.OnEnd?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void StopGame()
        {
            I?.OnStopGame?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void PlayerDied()
        {
            I?.OnPlayerDeath?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Pause()
        {
            I?.OnPause?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Resume()
        {
            I?.OnResume?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Menu()
        {
            I?.OnMenu?.Invoke();
        }

#if ODIN_INSPECTOR
        [Button]
#else
        [Button]
#endif
        public static void Restart()
        {
            I?.OnRestart?.Invoke();
        }

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
    }
}