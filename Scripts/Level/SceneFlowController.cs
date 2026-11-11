using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neo.Level
{
        /// <summary>
        ///     Scene load mode: synchronous, asynchronous with auto-activation, asynchronous with manual activation, or additive
        ///     load (Additive).
        /// </summary>
        public enum SceneFlowLoadMode
        {
            Sync,
            Async,
            AsyncManual,
            Additive
        }

        /// <summary>
        ///     How progress is shown in the text field.
        /// </summary>
        public enum ProgressTextFormat
        {
            Plain,
            Percent
        }

        /// <summary>
        ///     Scene loading seam of <see cref="SceneFlowController" /> (assign through
        ///     <see cref="SceneFlowController.SceneLoader" />). The default implementation forwards to
        ///     <see cref="SceneManager" />; replace it to route loads through Addressables, a network scene manager
        ///     or a test double.
        /// </summary>
        public interface ISceneFlowSceneLoader
        {
            /// <summary>Build index of the active scene, used by <see cref="SceneFlowController.Restart" />.</summary>
            int ActiveSceneBuildIndex { get; }

            /// <summary>Loads a scene synchronously by build index (Sync and Additive modes).</summary>
            void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single);

            /// <summary>Loads a scene synchronously by name (Sync and Additive modes).</summary>
            void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);

            /// <summary>
            ///     Starts an asynchronous load by build index. A null result releases the controller's request, so a
            ///     later call can retry.
            /// </summary>
            AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single);

            /// <summary>
            ///     Starts an asynchronous load by name. A null result releases the controller's request, so a later
            ///     call can retry.
            /// </summary>
            AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        }

        [NeoDoc("Level/SceneFlowController.md")]
        [CreateFromMenu("Neoxider/Level/SceneFlowController")]
        [AddComponentMenu("Neoxider/Level/" + nameof(SceneFlowController))]
        public class SceneFlowController : MonoBehaviour
        {
            [Header("Scene")] [SerializeField] private SceneFlowLoadMode _loadMode = SceneFlowLoadMode.Sync;

            [SerializeField] private int _sceneBuildIndex;
            [SerializeField] private string _sceneName = "";

            [Tooltip("When LoadScene() is called with no arguments: true = by name, false = by build index")]
            [SerializeField]
            private bool _useSceneName;

            [Tooltip("For Async: activate the scene as soon as it is ready. Not used for AsyncManual.")]
            [SerializeField]
            private bool _activateOnReady = true;

            [Tooltip("On Start, automatically call LoadScene() using the component fields.")] [SerializeField]
            private bool _loadOnStart;

            [Header("Progress UI")] [SerializeField]
            private Text _textProgress;

            [SerializeField] private TextMeshProUGUI _textMeshProgress;
            [SerializeField] private Slider _sliderProgress;
            [SerializeField] private Image _imageProgress;
            [SerializeField] private ProgressTextFormat _progressTextFormat = ProgressTextFormat.Percent;
            [SerializeField] private string _progressPrefix = "Loading... ";
            [SerializeField] private string _readyToProceedText = "Press to continue";
            [SerializeField] private GameObject _progressPanel;

            [Header("Events")] [SerializeField] private UnityEvent _onLoadStarted = new();

            [SerializeField] private UnityEventFloat _onProgress = new();
            [SerializeField] private UnityEvent _onReadyToProceed = new();
            [SerializeField] private UnityEvent _onLoadCompleted = new();

            private AsyncOperation _currentOperation;
            private bool _readyToProceedInvoked;
            private object _activeRequest;
            private int _shownPercent = -1;
            private static ISceneFlowSceneLoader _sceneLoader = new UnitySceneFlowSceneLoader();

            /// <summary>
            ///     Loader shared by every controller; assigning null restores the <see cref="SceneManager" /> loader.
            ///     Restore it after installing a custom or test loader.
            /// </summary>
            public static ISceneFlowSceneLoader SceneLoader
            {
                get => _sceneLoader;
                set => _sceneLoader = value ?? new UnitySceneFlowSceneLoader();
            }

            /// <summary>Load mode (from component settings).</summary>
            public SceneFlowLoadMode LoadMode
            {
                get => _loadMode;
                set => _loadMode = value;
            }

            /// <summary>Scene build index in Build Settings.</summary>
            public int SceneBuildIndex
            {
                get => _sceneBuildIndex;
                set => _sceneBuildIndex = value;
            }

            /// <summary>Scene name.</summary>
            public string SceneName
            {
                get => _sceneName;
                set => _sceneName = value ?? "";
            }

            /// <summary>Raised when an asynchronous load starts.</summary>
            public UnityEvent OnLoadStarted => _onLoadStarted;

            /// <summary>Raised with the raw Unity progress (0..1) while an asynchronous load runs.</summary>
            public UnityEventFloat OnProgress => _onProgress;

            /// <summary>
            ///     Raised once when a load without automatic activation (<c>AsyncManual</c>, or <c>Async</c> with Activate
            ///     On Ready off) reaches 0.9 and waits for <see cref="ProceedScene" />.
            /// </summary>
            public UnityEvent OnReadyToProceed => _onReadyToProceed;

            /// <summary>Raised after an asynchronous load finished and the progress panel was hidden.</summary>
            public UnityEvent OnLoadCompleted => _onLoadCompleted;

            /// <summary>Whether this controller owns an asynchronous scene load, including manual activation waits.</summary>
            public bool IsLoading => _activeRequest != null;

            private void Start()
            {
                if (_loadOnStart)
                {
                    LoadScene();
                }
            }

            private void OnDisable()
            {
                if (!gameObject.activeInHierarchy)
                {
                    ReleasePendingLoad();
                }
            }

            private void OnDestroy()
            {
                ReleasePendingLoad();
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void ResetStaticState()
            {
                _sceneLoader = new UnitySceneFlowSceneLoader();
            }

            /// <summary>Loads the scene by build index. Mode is taken from component settings.</summary>
            public void LoadScene(int buildIndex)
            {
                if (IsLoading)
                {
                    return;
                }

                if (_loadMode == SceneFlowLoadMode.Additive)
                {
                    LoadSceneAdditiveInternal(buildIndex, null);
                }
                else
                {
                    LoadSceneInternal(buildIndex, null);
                }
            }

            /// <summary>Loads the scene by name. Mode from settings.</summary>
            public void LoadScene(string sceneName)
            {
                if (IsLoading)
                {
                    return;
                }

                if (string.IsNullOrEmpty(sceneName))
                {
                    NeoDiagnostics.LogWarning("[SceneFlowController] LoadScene(string): scene name is null or empty.");
                    return;
                }

                if (_loadMode == SceneFlowLoadMode.Additive)
                {
                    LoadSceneAdditiveInternal(-1, sceneName);
                }
                else
                {
                    LoadSceneInternal(-1, sceneName);
                }
            }

            /// <summary>Loads using component fields: if a name is set, by name; otherwise by sceneBuildIndex.</summary>
            public void LoadScene()
            {
                if (_useSceneName && !string.IsNullOrEmpty(_sceneName))
                {
                    LoadScene(_sceneName);
                }
                else
                {
                    LoadScene(_sceneBuildIndex);
                }
            }

            /// <summary>Reloads the current active scene.</summary>
            public void Restart()
            {
                int index = _sceneLoader.ActiveSceneBuildIndex;
                LoadScene(index);
            }

            /// <summary>Exits the application.</summary>
            public void Quit()
            {
                Application.Quit();
            }

            /// <summary>Pause: true -> Time.timeScale = 0, false -> 1.</summary>
            public void Pause(bool active)
            {
                Time.timeScale = active ? 0f : 1f;
            }

            /// <summary>Activates the asynchronously loaded scene. Call after OnReadyToProceed (e.g. from a button).</summary>
            public void ProceedScene()
            {
                if (_currentOperation != null)
                {
                    _currentOperation.allowSceneActivation = true;
                }
            }

            private void LoadSceneInternal(int buildIndex, string sceneName)
            {
                bool byName = !string.IsNullOrEmpty(sceneName);
                if (!byName && buildIndex < 0)
                {
                    NeoDiagnostics.LogWarning("[SceneFlowController] Invalid scene: no name and buildIndex < 0.");
                    return;
                }

                switch (_loadMode)
                {
                    case SceneFlowLoadMode.Sync:
                        if (byName)
                        {
                            _sceneLoader.LoadScene(sceneName);
                        }
                        else
                        {
                            _sceneLoader.LoadScene(buildIndex);
                        }

                        return;
                    case SceneFlowLoadMode.Async:
                        if (gameObject.activeInHierarchy)
                        {
                            StartCoroutine(LoadSceneCoroutine(buildIndex, sceneName, _activateOnReady));
                        }

                        return;
                    case SceneFlowLoadMode.AsyncManual:
                        if (gameObject.activeInHierarchy)
                        {
                            StartCoroutine(LoadSceneCoroutine(buildIndex, sceneName, false));
                        }

                        return;
                }
            }

            private void LoadSceneAdditiveInternal(int buildIndex, string sceneName)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    _sceneLoader.LoadScene(sceneName, LoadSceneMode.Additive);
                }
                else if (buildIndex >= 0)
                {
                    _sceneLoader.LoadScene(buildIndex, LoadSceneMode.Additive);
                }
                else
                {
                    NeoDiagnostics.LogWarning("[SceneFlowController] Additive: need scene name or valid build index.");
                }
            }

            private IEnumerator LoadSceneCoroutine(int buildIndex, string sceneName, bool autoActivate)
            {
                object request = new();
                _activeRequest = request;
                _readyToProceedInvoked = false;
                _shownPercent = -1;
                bool byName = !string.IsNullOrEmpty(sceneName);
                try
                {
                    AsyncOperation op = byName
                        ? _sceneLoader.LoadSceneAsync(sceneName)
                        : _sceneLoader.LoadSceneAsync(buildIndex);
                    if (!ReferenceEquals(_activeRequest, request))
                    {
                        if (op != null)
                        {
                            op.allowSceneActivation = true;
                        }

                        yield break;
                    }

                    _currentOperation = op;
                    if (op == null)
                    {
                        NeoDiagnostics.LogError("[SceneFlowController] LoadSceneAsync failed.");
                        yield break;
                    }

                    if (!gameObject.activeInHierarchy)
                    {
                        yield break;
                    }

                    op.allowSceneActivation = autoActivate;
                    if (_progressPanel != null)
                    {
                        _progressPanel.SetActive(true);
                    }

                    _onLoadStarted?.Invoke();

                    while (ReferenceEquals(_activeRequest, request) && !op.isDone)
                    {
                        float p = Mathf.Clamp01(op.progress);
                        ApplyProgressToUI(p);
                        if (!ReferenceEquals(_activeRequest, request))
                        {
                            yield break;
                        }

                        _onProgress?.Invoke(p);

                        if (!ReferenceEquals(_activeRequest, request))
                        {
                            yield break;
                        }

                        if (!autoActivate && op.progress >= 0.9f && !_readyToProceedInvoked)
                        {
                            _readyToProceedInvoked = true;
                            SetProgressText(_readyToProceedText);
                            _onReadyToProceed?.Invoke();
                        }

                        yield return null;
                    }

                    if (!ReferenceEquals(_activeRequest, request))
                    {
                        yield break;
                    }

                    ApplyProgressToUI(1f);
                    if (!ReferenceEquals(_activeRequest, request))
                    {
                        yield break;
                    }

                    _onProgress?.Invoke(1f);
                    if (!ReferenceEquals(_activeRequest, request))
                    {
                        yield break;
                    }
                }
                finally
                {
                    if (ReferenceEquals(_activeRequest, request))
                    {
                        ReleasePendingLoad();
                    }
                }

                _onLoadCompleted?.Invoke();
            }

            private void ReleasePendingLoad()
            {
                if (!IsLoading && _currentOperation == null)
                {
                    return;
                }

                AsyncOperation operation = _currentOperation;
                _currentOperation = null;
                _activeRequest = null;
                _readyToProceedInvoked = false;
                if (operation != null)
                {
                    // A native scene load cannot be canceled; release manual activation so it cannot block Unity's queue.
                    operation.allowSceneActivation = true;
                }

                if (_progressPanel != null)
                {
                    _progressPanel.SetActive(false);
                }
            }

            private void ApplyProgressToUI(float progress)
            {
                if (_sliderProgress != null)
                {
                    _sliderProgress.value = progress;
                }

                if (_imageProgress != null)
                {
                    _imageProgress.fillAmount = progress;
                }

                if (!_readyToProceedInvoked || progress >= 1f)
                {
                    SetProgressText(progress);
                }
            }

            private void SetProgressText(float progress)
            {
                if (_progressTextFormat == ProgressTextFormat.Percent)
                {
                    int percent = Mathf.RoundToInt(progress * 100f);
                    // WHY: progress is pushed every frame while loading; rebuild the label only when the shown
                    // percentage changes instead of allocating a new string per frame.
                    if (percent == _shownPercent)
                    {
                        return;
                    }

                    WriteProgressLabel(string.Concat(_progressPrefix, percent.ToString(CultureInfo.InvariantCulture),
                        "%"));
                    _shownPercent = percent;
                }
                else
                {
                    SetProgressText(_progressPrefix);
                }
            }

            private void SetProgressText(string text)
            {
                // WHY: any other label (ready text, plain prefix) replaces the shown percentage, so the next
                // percentage must be written again.
                _shownPercent = -1;
                WriteProgressLabel(text);
            }

            private void WriteProgressLabel(string text)
            {
                if (_textProgress != null)
                {
                    _textProgress.text = text;
                }

                if (_textMeshProgress != null)
                {
                    _textMeshProgress.text = text;
                }
            }

            [Serializable]
            public class UnityEventFloat : UnityEvent<float>
            {
            }

            private sealed class UnitySceneFlowSceneLoader : ISceneFlowSceneLoader
            {
                public int ActiveSceneBuildIndex => SceneManager.GetActiveScene().buildIndex;

                public void LoadScene(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
                {
                    SceneManager.LoadScene(buildIndex, mode);
                }

                public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
                {
                    SceneManager.LoadScene(sceneName, mode);
                }

                public AsyncOperation LoadSceneAsync(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
                {
                    return SceneManager.LoadSceneAsync(buildIndex, mode);
                }

                public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
                {
                    return SceneManager.LoadSceneAsync(sceneName, mode);
                }
            }
        }
}
