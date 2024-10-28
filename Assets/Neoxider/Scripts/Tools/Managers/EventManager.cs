using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("Game Events")]
        public UnityEvent OnGameStart;
        public UnityEvent OnGameOver;
        public UnityEvent OnPlayerDeath;
        public UnityEvent OnLevelComplete;

        [Space, Header("Pause")]
        public UnityEvent OnPause;
        public UnityEvent OnResume;

        private void Awake()
        {
            Instance = this;
        }

        public static void GameStarted()
        {
            Instance.OnGameStart?.Invoke();
        }

        public static void GameOver()
        {
            Instance.OnGameOver?.Invoke();
        }

        public static void PlayerDied()
        {
            Instance.OnPlayerDeath?.Invoke();
        }

        public static void LevelCompleted()
        {
            Instance.OnLevelComplete?.Invoke();
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