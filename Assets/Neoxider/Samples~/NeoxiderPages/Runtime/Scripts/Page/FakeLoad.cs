using System.Collections;
using Neo;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Neo.Pages
{
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/FakeLoad.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(FakeLoad))]
    /// <summary>
    /// Fake loading: simulates progress over a time range and raises events.
    /// </summary>
    public class FakeLoad : MonoBehaviour
    {
        private static bool isInitialized;
        [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private Vector2 timeLoad = new(1.5f, 2);
        [SerializeField] private bool isLoadOne = true;

        // WHY: initialize inline so a runtime-spawned FakeLoad (AddComponent, not deserialized from a
        // scene) still has non-null events to subscribe to and invoke.
        public UnityEvent OnStart = new();
        public UnityEvent OnFinisLoad = new();
        public UnityEvent<int> OnChangePercent = new();
        public UnityEvent<float> OnChange = new();

        private void Awake()
        {
            if (_loadOnAwake)
            {
                Load();
            }
        }

        /// <summary>
        ///     Starts fake loading (unless blocked by one-shot setting).
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
        ///     Completes loading and invokes <see cref="OnFinisLoad" />.
        /// </summary>
        public void EndLoad()
        {
            // WHY: the progress loop exits before emitting the final tick, so a bound bar would freeze
            // just short of full; push 100% before finishing.
            OnChange?.Invoke(1f);
            OnChangePercent?.Invoke(100);
            OnFinisLoad?.Invoke();
        }
    }
}
