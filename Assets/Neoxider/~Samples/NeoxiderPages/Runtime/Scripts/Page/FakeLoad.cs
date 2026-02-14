using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(FakeLoad))]
    /// <summary>
    /// Фейковая загрузка: имитирует прогресс в диапазоне времени и генерирует события.
    /// </summary>
    public class FakeLoad : MonoBehaviour
    {
        private static bool isInitialized;
        [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private Vector2 timeLoad = new(1.5f, 2);
        [SerializeField] private bool isLoadOne = true;

        public UnityEvent OnStart;
        public UnityEvent OnFinisLoad;
        public UnityEvent<int> OnChangePercent;
        public UnityEvent<float> OnChange;

        private void Awake()
        {
            if (_loadOnAwake)
            {
                Load();
            }
        }

        /// <summary>
        ///     Запускает фейковую загрузку (если не заблокирована настройкой one-shot).
        /// </summary>
        public void Load()
        {
            if (!isLoadOne || (isLoadOne && !isInitialized))
            {
                float time = Random.Range(timeLoad.x, timeLoad.y);
                OnStart?.Invoke();
                StartCoroutine(Loading(time));
                isInitialized = true;
            }
        }

        private IEnumerator Loading(float time)
        {
            float timer = 0;

            while (timer < time)
            {
                float percent = timer / time;
                OnChange?.Invoke(percent);
                OnChangePercent?.Invoke((int)(percent * 100));
                yield return null;

                timer += Time.deltaTime;
            }

            EndLoad();
        }

        /// <summary>
        ///     Завершает загрузку и вызывает <see cref="OnFinisLoad" />.
        /// </summary>
        public void EndLoad()
        {
            OnFinisLoad?.Invoke();
        }
    }
}