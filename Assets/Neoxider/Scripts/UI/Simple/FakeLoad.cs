using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Neo.UI
{
    /// <summary>
    ///     Fake loading: simulates progress over a time range and raises events. In one-shot mode a later
    ///     <see cref="Load" /> in the same session completes on the next frame, so <see cref="OnFinisLoad" /> always
    ///     fires. A load pauses while the GameObject is inactive and continues when it is active again.
    /// </summary>
    [NeoDoc("UI/FakeLoad.md")]
    [CreateFromMenu("Neoxider/UI/FakeLoad", "Prefabs/UI/Page/Fake Load.prefab")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(FakeLoad))]
    [MovedFrom(true, sourceNamespace: "Neo")]
    public class FakeLoad : MonoBehaviour
    {
        private static bool isInitialized;

        // WHY: without this reset the one-shot flag leaks into the next play session when
        // Enter Play Mode Options disable domain reload, so Load() never fires again.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            isInitialized = false;
        }

        [Header("Settings")] [SerializeField] private bool _loadOnAwake = true;
        [SerializeField] private Vector2 timeLoad = new(1.5f, 2);
        [SerializeField] private bool isLoadOne = true;

        [Tooltip("Count unscaled time so the loader still finishes when a previous scene left Time.timeScale at 0.")]
        [SerializeField]
        private bool _useUnscaledTime = true;

        public UnityEvent OnStart = new();
        public UnityEvent OnFinisLoad = new();
        public UnityEvent<int> OnChangePercent = new();
        public UnityEvent<float> OnChange = new();

        private Coroutine _loading;
        private int _activeStream;
        private int _streamSerial;
        private float _streamDuration;
        private float _streamElapsed;
        private bool _streamFinishesNextFrame;

        /// <summary>When true the fake progress ignores <see cref="Time.timeScale" />.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>True while a load runs or waits to complete (including a paused load on an inactive object).</summary>
        public bool IsLoading => _activeStream != 0;

        private void Awake()
        {
            if (_loadOnAwake)
            {
                Load();
            }
        }

        private void OnEnable()
        {
            // WHY: deactivating the GameObject killed the routine (a page system hides every page before it shows
            // the startup page); continue the same stream instead of leaving the loading page up forever.
            if (_activeStream != 0 && _loading == null)
            {
                RunStream(_activeStream);
            }
        }

        private void OnDisable()
        {
            // WHY: Unity stops the coroutines of a deactivated GameObject without notice; forget the dead handle
            // so OnEnable resumes. A disabled component on an active object keeps its coroutine running.
            if (_loading != null && !gameObject.activeInHierarchy)
            {
                _loading = null;
            }
        }

        /// <summary>
        ///     Starts the fake progress, or, when one-shot mode already ran this session, completes on the next frame
        ///     (at once outside Play Mode). A call while a progress load is running restarts it, so
        ///     <see cref="OnFinisLoad" /> fires once.
        /// </summary>
        public void Load()
        {
            bool completesInstantly = isLoadOne && isInitialized;
            if (completesInstantly && _activeStream != 0 && _streamFinishesNextFrame)
            {
                return;
            }

            // WHY: the running check comes before the one-shot check; a second call during the first load must
            // restart it instead of finishing instantly while the first stream later finishes again.
            if (_activeStream != 0 || !completesInstantly)
            {
                StopLoading();
                float time = Random.Range(timeLoad.x, timeLoad.y);
                OnStart?.Invoke();
                isInitialized = true;
                BeginStream(time, false);
                return;
            }

            if (!Application.isPlaying)
            {
                // WHY: coroutines do not tick outside Play Mode; finish now so edit-time callers still complete.
                EndLoad();
                return;
            }

            // WHY: one-shot mode on a scene revisit must still complete, but not synchronously: Load runs from
            // Awake, often inside the page switch that shows the loading page, and an OnFinisLoad that switches
            // pages there is undone by the outer switch.
            BeginStream(0f, true);
        }

        /// <summary>
        ///     Completes loading now: stops a running progress stream, emits the final full-progress ticks and invokes
        ///     <see cref="OnFinisLoad" />.
        /// </summary>
        public void EndLoad()
        {
            StopLoading();

            // WHY: the progress loop exits before emitting the final tick, so a bound bar would freeze
            // just short of full; push 100% before finishing.
            OnChange?.Invoke(1f);
            OnChangePercent?.Invoke(100);
            OnFinisLoad?.Invoke();
        }

        private void BeginStream(float duration, bool finishNextFrame)
        {
            int stream = unchecked(++_streamSerial);
            if (stream == 0)
            {
                stream = _streamSerial = 1;
            }

            _activeStream = stream;
            _streamDuration = duration;
            _streamElapsed = 0f;
            _streamFinishesNextFrame = finishNextFrame;
            RunStream(stream);
        }

        private void RunStream(int stream)
        {
            if (!gameObject.activeInHierarchy)
            {
                // WHY: an inactive object cannot run coroutines; OnEnable starts the stream on activation.
                return;
            }

            IEnumerator body = _streamFinishesNextFrame ? FinishNextFrame(stream) : Loading(stream);
            Coroutine routine = StartCoroutine(body);
            // WHY: a zero-length load (or a listener that ends or restarts the load on its first tick) finishes
            // inside StartCoroutine; keeping that dead handle would make the next Load() restart a finished load.
            if (_activeStream == stream)
            {
                _loading = routine;
            }
        }

        private IEnumerator FinishNextFrame(int stream)
        {
            yield return null;

            if (_activeStream != stream)
            {
                yield break;
            }

            _activeStream = 0;
            _loading = null;
            EndLoad();
        }

        private IEnumerator Loading(int stream)
        {
            while (_streamElapsed < _streamDuration)
            {
                float percent = _streamElapsed / _streamDuration;
                OnChange?.Invoke(percent);
                // WHY: a progress listener may end or restart the load; the superseded stream must stop reporting.
                if (_activeStream != stream)
                {
                    yield break;
                }

                OnChangePercent?.Invoke((int)(percent * 100));
                yield return null;

                if (_activeStream != stream)
                {
                    yield break;
                }

                _streamElapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            }

            if (_activeStream != stream)
            {
                yield break;
            }

            _activeStream = 0;
            _loading = null;
            EndLoad();
        }

        private void StopLoading()
        {
            _activeStream = 0;
            _streamFinishesNextFrame = false;
            if (_loading == null)
            {
                return;
            }

            StopCoroutine(_loading);
            _loading = null;
        }
    }
}
