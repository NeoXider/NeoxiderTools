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
        ///     Режим загрузки сцены: синхронно, асинхронно с автоактивацией, асинхронно с ручной активацией или добавление сцены (Additive).
        /// </summary>
        public enum SceneFlowLoadMode
        {
            Sync,
            Async,
            AsyncManual,
            Additive
        }

        /// <summary>
        ///     Формат отображения прогресса в текстовом поле.
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
            [Header("Scene")]
            [SerializeField] private SceneFlowLoadMode _loadMode = SceneFlowLoadMode.Sync;
            [SerializeField] private int _sceneBuildIndex;
            [SerializeField] private string _sceneName = "";
            [Tooltip("При вызове LoadScene() без аргументов: true = по имени, false = по build index")]
            [SerializeField] private bool _useSceneName = false;
            [Tooltip("Для Async: активировать сцену сразу по готовности. Для AsyncManual не используется.")]
            [SerializeField] private bool _activateOnReady = true;

            [Header("Progress UI")]
            [SerializeField] private Text _textProgress;
            [SerializeField] private TextMeshProUGUI _textMeshProgress;
            [SerializeField] private Slider _sliderProgress;
            [SerializeField] private Image _imageProgress;
            [SerializeField] private ProgressTextFormat _progressTextFormat = ProgressTextFormat.Percent;
            [SerializeField] private string _progressPrefix = "Loading... ";
            [SerializeField] private string _readyToProceedText = "Press to continue";
            [SerializeField] private GameObject _progressPanel;

            [Header("Events")]
            [SerializeField] private UnityEvent _onLoadStarted = new UnityEvent();
            [SerializeField] private UnityEventFloat _onProgress = new UnityEventFloat();
            [SerializeField] private UnityEvent _onReadyToProceed = new UnityEvent();
            [SerializeField] private UnityEvent _onLoadCompleted = new UnityEvent();

            [Serializable]
            public class UnityEventFloat : UnityEvent<float> { }

            private AsyncOperation _currentOperation;
            private bool _readyToProceedInvoked;

            /// <summary>Режим загрузки (из настроек компонента).</summary>
            public SceneFlowLoadMode LoadMode { get => _loadMode; set => _loadMode = value; }

            /// <summary>Индекс сцены в Build Settings.</summary>
            public int SceneBuildIndex { get => _sceneBuildIndex; set => _sceneBuildIndex = value; }

            /// <summary>Имя сцены.</summary>
            public string SceneName { get => _sceneName; set => _sceneName = value ?? ""; }

            public UnityEvent OnLoadStarted => _onLoadStarted;
            public UnityEventFloat OnProgress => _onProgress;
            public UnityEvent OnReadyToProceed => _onReadyToProceed;
            public UnityEvent OnLoadCompleted => _onLoadCompleted;

            /// <summary>Загружает сцену по build index. Режим берётся из настроек компонента.</summary>
            public void LoadScene(int buildIndex)
            {
                if (_loadMode == SceneFlowLoadMode.Additive)
                    LoadSceneAdditiveInternal(buildIndex, null);
                else
                    LoadSceneInternal(buildIndex, null);
            }

            /// <summary>Загружает сцену по имени. Режим из настроек.</summary>
            public void LoadScene(string sceneName)
            {
                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogWarning("[SceneFlowController] LoadScene(string): scene name is null or empty.");
                    return;
                }
                if (_loadMode == SceneFlowLoadMode.Additive)
                    LoadSceneAdditiveInternal(-1, sceneName);
                else
                    LoadSceneInternal(-1, sceneName);
            }

            /// <summary>Загружает сцену по полям компонента: если задано имя — по имени, иначе по sceneBuildIndex.</summary>
            public void LoadScene()
            {
                if (_useSceneName && !string.IsNullOrEmpty(_sceneName))
                    LoadScene(_sceneName);
                else
                    LoadScene(_sceneBuildIndex);
            }

            /// <summary>Перезагружает текущую активную сцену.</summary>
            public void Restart()
            {
                int index = SceneManager.GetActiveScene().buildIndex;
                LoadScene(index);
            }

            /// <summary>Выход из приложения.</summary>
            public void Quit()
            {
                Application.Quit();
            }

            /// <summary>Пауза: true → Time.timeScale = 0, false → 1.</summary>
            public void Pause(bool active)
            {
                Time.timeScale = active ? 0f : 1f;
            }

            /// <summary>Активирует асинхронно загруженную сцену. Вызывать после OnReadyToProceed (например по кнопке).</summary>
            public void ProceedScene()
            {
                if (_currentOperation != null)
                    _currentOperation.allowSceneActivation = true;
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
                            SceneManager.LoadScene(sceneName);
                        else
                            SceneManager.LoadScene(buildIndex);
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
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                else if (buildIndex >= 0)
                    SceneManager.LoadScene(buildIndex, LoadSceneMode.Additive);
                else
                    Debug.LogWarning("[SceneFlowController] Additive: need scene name or valid build index.");
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

                _currentOperation.allowSceneActivation = autoActivate;

                if (_progressPanel != null)
                    _progressPanel.SetActive(true);

                _onLoadStarted?.Invoke();

                while (!_currentOperation.isDone)
                {
                    float p = Mathf.Clamp01(_currentOperation.progress);
                    _onProgress?.Invoke(p);
                    ApplyProgressToUI(p);

                    if (!autoActivate && _currentOperation.progress >= 0.9f && !_readyToProceedInvoked)
                    {
                        _readyToProceedInvoked = true;
                        _onReadyToProceed?.Invoke();
                        SetProgressText(_readyToProceedText);
                    }

                    yield return null;
                }

                _currentOperation = null;
                if (_progressPanel != null)
                    _progressPanel.SetActive(false);

                _onLoadCompleted?.Invoke();
            }

            private void ApplyProgressToUI(float progress)
            {
                if (_sliderProgress != null)
                    _sliderProgress.value = progress;
                if (_imageProgress != null)
                    _imageProgress.fillAmount = progress;
                if (_progressTextFormat != ProgressTextFormat.Plain || progress < 0.9f)
                    SetProgressText(progress);
            }

            private void SetProgressText(float progress)
            {
                if (_progressTextFormat == ProgressTextFormat.Percent)
                {
                    int percent = Mathf.RoundToInt(progress * 100f);
                    string t = _progressPrefix + percent + "%";
                    if (_textProgress != null) _textProgress.text = t;
                    if (_textMeshProgress != null) _textMeshProgress.text = t;
                }
            }

            private void SetProgressText(string text)
            {
                if (_textProgress != null) _textProgress.text = text;
                if (_textMeshProgress != null) _textMeshProgress.text = text;
            }
        }
    }
}
