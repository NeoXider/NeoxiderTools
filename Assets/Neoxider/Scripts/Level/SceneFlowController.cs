using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Neo
{
    namespace Level
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

        [NeoDoc("Level/SceneFlowController.md")]
        [CreateFromMenu("Neoxider/Level/SceneFlowController")]
        [AddComponentMenu("Neoxider/Level/" + nameof(SceneFlowController))]
        public class SceneFlowController : MonoBehaviour
        {
            [Header("Scene")] [SerializeField] private SceneFlowLoadMode _loadMode = SceneFlowLoadMode.Sync;

            [SerializeField] private int _sceneBuildIndex;
            [SerializeField] private string _sceneName = "";

            [Tooltip("When LoadScene() is called with no arguments: true = by name, false = by build index")] [SerializeField]
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

            public UnityEvent OnLoadStarted => _onLoadStarted;
            public UnityEventFloat OnProgress => _onProgress;
            public UnityEvent OnReadyToProceed => _onReadyToProceed;
            public UnityEvent OnLoadCompleted => _onLoadCompleted;

            private void Start()
            {
                if (_loadOnStart)
                {
                    LoadScene();
                }
            }

            /// <summary>Loads the scene by build index. Mode is taken from component settings.</summary>
            public void LoadScene(int buildIndex)
            {
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
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("[SceneFlowController] LoadScene(string): scene name is null or empty.");
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
                int index = SceneManager.GetActiveScene().buildIndex;
                LoadScene(index);
            }

            /// <summary>Exits the application.</summary>
            public void Quit()
            {
                Application.Quit();
            }

            /// <summary>Pause: true → Time.timeScale = 0, false → 1.</summary>
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
                    Debug.LogWarning("[SceneFlowController] Invalid scene: no name and buildIndex < 0.");
                    return;
                }

                switch (_loadMode)
                {
                    case SceneFlowLoadMode.Sync:
                        if (byName)
                        {
                            SceneManager.LoadScene(sceneName);
                        }
                        else
                        {
                            SceneManager.LoadScene(buildIndex);
                        }

                        return;
                    case SceneFlowLoadMode.Async:
                        StartCoroutine(LoadSceneCoroutine(buildIndex, sceneName, true));
                        return;
                    case SceneFlowLoadMode.AsyncManual:
                        StartCoroutine(LoadSceneCoroutine(buildIndex, sceneName, false));
                        return;
                }
            }

            private void LoadSceneAdditiveInternal(int buildIndex, string sceneName)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
                else if (buildIndex >= 0)
                {
                    SceneManager.LoadScene(buildIndex, LoadSceneMode.Additive);
                }
                else
                {
                    Debug.LogWarning("[SceneFlowController] Additive: need scene name or valid build index.");
                }
            }

            private IEnumerator LoadSceneCoroutine(int buildIndex, string sceneName, bool autoActivate)
            {
                _readyToProceedInvoked = false;
                bool byName = !string.IsNullOrEmpty(sceneName);

                _currentOperation = byName
                    ? SceneManager.LoadSceneAsync(sceneName)
                    : SceneManager.LoadSceneAsync(buildIndex);
                if (_currentOperation == null)
                {
                    Debug.LogError("[SceneFlowController] LoadSceneAsync failed.");
                    yield break;
                }

                // Guard against overlapping load requests:
                // another coroutine may overwrite/null _currentOperation while this one is still running.
                var op = _currentOperation;

                op.allowSceneActivation = autoActivate;

                if (_progressPanel != null)
                {
                    _progressPanel.SetActive(true);
                }

                _onLoadStarted?.Invoke();

                while (op != null && !op.isDone)
                {
                    float p = Mathf.Clamp01(op.progress);
                    _onProgress?.Invoke(p);
                    ApplyProgressToUI(p);

                    if (!autoActivate && op.progress >= 0.9f && !_readyToProceedInvoked)
                    {
                        _readyToProceedInvoked = true;
                        _onReadyToProceed?.Invoke();
                        SetProgressText(_readyToProceedText);
                    }

                    yield return null;
                }
                if (op == null)
                {
                    Debug.LogWarning("[SceneFlowController] Load operation became null. Possibly overlapping load requests.");
                    yield break;
                }

                _currentOperation = null;
                if (_progressPanel != null)
                {
                    _progressPanel.SetActive(false);
                }

                _onLoadCompleted?.Invoke();
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

                if (_progressTextFormat != ProgressTextFormat.Plain || progress < 0.9f)
                {
                    SetProgressText(progress);
                }
            }

            private void SetProgressText(float progress)
            {
                if (_progressTextFormat == ProgressTextFormat.Percent)
                {
                    int percent = Mathf.RoundToInt(progress * 100f);
                    string t = _progressPrefix + percent + "%";
                    if (_textProgress != null)
                    {
                        _textProgress.text = t;
                    }

                    if (_textMeshProgress != null)
                    {
                        _textMeshProgress.text = t;
                    }
                }
            }

            private void SetProgressText(string text)
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
        }
    }
}
