using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Tools
{
    /// <summary>
    ///     EventManager GameStates
    /// </summary>
    [CreateFromMenu("Neoxider/Tools/Managers/EM")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(EM))]
    [NeoDoc("Tools/Managers/EM.md")]
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
        public static void End()
        {
            I?.OnEnd?.Invoke();
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

        [Button]
        public static void Menu()
        {
            I?.OnMenu?.Invoke();
        }

        [Button]
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