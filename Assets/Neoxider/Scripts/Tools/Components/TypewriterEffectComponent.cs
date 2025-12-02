using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// MonoBehaviour-обёртка для TypewriterEffect (на UniTask).
    /// </summary>
    [AddComponentMenu("Neo/" + "Tools/" + nameof(TypewriterEffectComponent))]
    public class TypewriterEffectComponent : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Текстовый компонент для вывода. Если не указан, ищется на этом объекте")]
        [SerializeField] private TMP_Text _targetText;
        
        [Header("Settings")]
        [Tooltip("Автоматически запустить эффект при Start")]
        [SerializeField] private bool _autoStart = true;
        [Tooltip("Запускать эффект каждый раз при OnEnable")]
        [SerializeField] private bool _playOnEnable;
        [Tooltip("Текст для автозапуска. Если пусто, берётся из TargetText")]
        [SerializeField] [TextArea(2, 5)] private string _autoStartText;
        [SerializeField] private TypewriterEffect _effect = new TypewriterEffect();
        
        [Header("Events")]
        public UnityEvent OnStart;
        public UnityEvent OnComplete;
        public UnityEvent<char> OnCharacterTyped;
        public UnityEvent<float> OnProgressChanged;
        
        private CancellationTokenSource _cts;
        private bool _hasStarted;
        
        public TypewriterEffect Effect => _effect;
        public TMP_Text TargetText
        {
            get => _targetText;
            set => _targetText = value;
        }
        
        public bool IsTyping => _effect?.IsTyping ?? false;
        public float Progress => _effect?.Progress ?? 0f;

        private void Awake()
        {
            if (_targetText == null)
                _targetText = GetComponent<TMP_Text>();
            
            if (_effect == null)
                _effect = new TypewriterEffect();
            
            _effect.RebuildPauseMap();
            
            _effect.OnStart += () => OnStart?.Invoke();
            _effect.OnComplete += () => OnComplete?.Invoke();
            _effect.OnCharacterTyped += c => OnCharacterTyped?.Invoke(c);
            _effect.OnProgressChanged += p => OnProgressChanged?.Invoke(p);
        }

        private void Start()
        {
            _hasStarted = true;
            if (_autoStart)
            {
                PlayAutoText();
            }
        }

        private void OnEnable()
        {
            if (_playOnEnable && _hasStarted)
            {
                PlayAutoText();
            }
        }

        /// <summary>
        /// Запускает эффект с текстом из AutoStartText или TargetText.
        /// </summary>
        public void PlayAutoText()
        {
            string text = string.IsNullOrEmpty(_autoStartText) && _targetText != null 
                ? _targetText.text 
                : _autoStartText;
            Play(text);
        }

        /// <summary>
        /// Запускает эффект печати текста.
        /// </summary>
        public void Play(string text)
        {
            if (_targetText == null)
            {
                Debug.LogWarning($"[TypewriterEffectComponent] TargetText не назначен на {gameObject.name}", this);
                return;
            }
            
            Stop();
            _cts = new CancellationTokenSource();
            PlayInternalAsync(text, _cts.Token).Forget();
        }

        private async UniTaskVoid PlayInternalAsync(string text, CancellationToken token)
        {
            await _effect.PlayAsync(text, t =>
            {
                if (_targetText != null)
                    _targetText.text = t;
            }, token);
        }

        /// <summary>
        /// Останавливает эффект и показывает весь текст.
        /// </summary>
        public void Complete()
        {
            if (_effect != null && _targetText != null)
            {
                _targetText.text = _effect.Complete();
            }
            CancelToken();
        }

        /// <summary>
        /// Останавливает эффект, сохраняя текущий текст.
        /// </summary>
        public void Stop()
        {
            _effect?.Stop();
            CancelToken();
        }

        private void CancelToken()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Очищает текст.
        /// </summary>
        public void Clear()
        {
            Stop();
            _effect?.Reset();
            if (_targetText != null)
                _targetText.text = string.Empty;
        }

        /// <summary>
        /// Если печатает — завершает мгновенно, иначе возвращает false.
        /// </summary>
        public bool TrySkip()
        {
            if (!IsTyping) return false;
            Complete();
            return true;
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }
    }
}
