using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
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
            if (_loadOnAwake) Load();
        }

        public void Load()
        {
            if (!isLoadOne || (isLoadOne && !isInitialized))
            {
                var time = Random.Range(timeLoad.x, timeLoad.y);
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
                var percent = timer / time;
                OnChange?.Invoke(percent);
                OnChangePercent?.Invoke((int)(percent * 100));
                yield return null;

                timer += Time.deltaTime;
            }

            EndLoad();
        }

        public void EndLoad()
        {
            OnFinisLoad?.Invoke();
        }
    }
}