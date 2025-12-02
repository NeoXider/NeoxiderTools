using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    /// Настройка паузы для знака препинания.
    /// </summary>
    [Serializable]
    public class PunctuationPause
    {
        [Tooltip("Символ знака препинания")]
        public char character;
        [Tooltip("Дополнительная пауза в секундах после этого символа")]
        [Min(0)] public float pause;

        public PunctuationPause() { }
        
        public PunctuationPause(char character, float pause)
        {
            this.character = character;
            this.pause = pause;
        }
    }

    /// <summary>
    /// Эффект печатной машинки для посимвольного вывода текста (на UniTask).
    /// </summary>
    [Serializable]
    public class TypewriterEffect
    {
        /// <summary>
        /// Предопределённый список пауз для знаков препинания по умолчанию.
        /// </summary>
        public static readonly PunctuationPause[] DefaultPunctuationPauses = new[]
        {
            new PunctuationPause('.', 0.3f),
            new PunctuationPause('!', 0.3f),
            new PunctuationPause('?', 0.3f),
            new PunctuationPause(',', 0.15f),
            new PunctuationPause(';', 0.15f),
            new PunctuationPause(':', 0.2f),
            new PunctuationPause('—', 0.2f),
            new PunctuationPause('-', 0.1f),
            new PunctuationPause('…', 0.5f),
            new PunctuationPause('\n', 0.2f),
        };

        [SerializeField] [Min(0.1f)] private float _charactersPerSecond = 70f;
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField] private bool _usePunctuationPauses = true;
        [SerializeField] private List<PunctuationPause> _punctuationPauses = new List<PunctuationPause>();
        
        [NonSerialized] private readonly StringBuilder _builder = new StringBuilder(256);
        [NonSerialized] private readonly Dictionary<char, float> _punctuationPauseMap = new Dictionary<char, float>();
        [NonSerialized] private string _fullText = string.Empty;
        [NonSerialized] private int _currentIndex;
        [NonSerialized] private CancellationTokenSource _cts;
        
        public event Action OnStart;
        public event Action OnComplete;
        public event Action<char> OnCharacterTyped;
        public event Action<float> OnProgressChanged;
        
        public float CharactersPerSecond
        {
            get => _charactersPerSecond;
            set => _charactersPerSecond = Mathf.Max(0.1f, value);
        }
        
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        public bool UsePunctuationPauses
        {
            get => _usePunctuationPauses;
            set => _usePunctuationPauses = value;
        }

        public List<PunctuationPause> PunctuationPauses => _punctuationPauses;
        
        public bool IsTyping => _cts != null && !_cts.IsCancellationRequested;
        public float Progress => string.IsNullOrEmpty(_fullText) ? 0f : (float)_currentIndex / _fullText.Length;
        public string CurrentText => _builder.ToString();
        public string FullText => _fullText;

        public TypewriterEffect()
        {
            SetDefaultPunctuationPauses();
        }
        
        public TypewriterEffect(float charactersPerSecond, bool useUnscaledTime = false)
        {
            _charactersPerSecond = Mathf.Max(0.1f, charactersPerSecond);
            _useUnscaledTime = useUnscaledTime;
            SetDefaultPunctuationPauses();
        }

        /// <summary>
        /// Устанавливает предопределённый список пауз для знаков препинания.
        /// </summary>
        public void SetDefaultPunctuationPauses()
        {
            _punctuationPauses.Clear();
            foreach (var p in DefaultPunctuationPauses)
            {
                _punctuationPauses.Add(new PunctuationPause(p.character, p.pause));
            }
            RebuildPauseMap();
        }

        /// <summary>
        /// Очищает все паузы для знаков препинания.
        /// </summary>
        public void ClearPunctuationPauses()
        {
            _punctuationPauses.Clear();
            _punctuationPauseMap.Clear();
        }

        /// <summary>
        /// Добавляет или обновляет паузу для символа (в секундах).
        /// </summary>
        public void SetPunctuationPause(char character, float pause)
        {
            var existing = _punctuationPauses.Find(p => p.character == character);
            if (existing != null)
            {
                existing.pause = pause;
            }
            else
            {
                _punctuationPauses.Add(new PunctuationPause(character, pause));
            }
            _punctuationPauseMap[character] = pause;
        }

        /// <summary>
        /// Устанавливает паузы из словаря (заменяет текущие).
        /// </summary>
        public void SetPunctuationPauses(Dictionary<char, float> pauses)
        {
            _punctuationPauses.Clear();
            _punctuationPauseMap.Clear();
            foreach (var kvp in pauses)
            {
                _punctuationPauses.Add(new PunctuationPause(kvp.Key, kvp.Value));
                _punctuationPauseMap[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Добавляет паузы из словаря (объединяет с текущими).
        /// </summary>
        public void AddPunctuationPauses(Dictionary<char, float> pauses)
        {
            foreach (var kvp in pauses)
            {
                SetPunctuationPause(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Удаляет паузу для символа.
        /// </summary>
        public void RemovePunctuationPause(char character)
        {
            _punctuationPauses.RemoveAll(p => p.character == character);
            _punctuationPauseMap.Remove(character);
        }

        /// <summary>
        /// Получает паузу для символа в секундах (0 если не найдено).
        /// </summary>
        public float GetPunctuationPause(char character)
        {
            return _punctuationPauseMap.TryGetValue(character, out float pause) ? pause : 0f;
        }

        /// <summary>
        /// Перестраивает внутренний словарь пауз из списка.
        /// </summary>
        public void RebuildPauseMap()
        {
            _punctuationPauseMap.Clear();
            foreach (var p in _punctuationPauses)
            {
                _punctuationPauseMap[p.character] = p.pause;
            }
        }

        /// <summary>
        /// Запускает эффект печати текста.
        /// </summary>
        public async UniTask PlayAsync(string text, Action<string> onTextChanged, CancellationToken externalToken = default)
        {
            Stop();
            
            _fullText = text ?? string.Empty;
            _builder.Clear();
            _currentIndex = 0;
            
            if (string.IsNullOrEmpty(_fullText))
            {
                onTextChanged?.Invoke(string.Empty);
                return;
            }
            
            RebuildPauseMap();
            
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _cts.Token;
            
            OnStart?.Invoke();
            onTextChanged?.Invoke(string.Empty);
            
            try
            {
                float baseDelay = 1f / _charactersPerSecond;
                
                for (int i = 0; i < _fullText.Length; i++)
                {
                    token.ThrowIfCancellationRequested();
                    
                    char c = _fullText[i];
                    _builder.Append(c);
                    _currentIndex = i + 1;
                    
                    onTextChanged?.Invoke(_builder.ToString());
                    OnCharacterTyped?.Invoke(c);
                    OnProgressChanged?.Invoke(Progress);
                    
                    // Вычисляем задержку
                    float totalDelay = baseDelay;
                    if (_usePunctuationPauses && _punctuationPauseMap.TryGetValue(c, out float punctuationPause))
                    {
                        totalDelay += punctuationPause;
                    }
                    
                    int delayMs = Mathf.RoundToInt(totalDelay * 1000f);
                    if (delayMs > 0)
                    {
                        await UniTask.Delay(delayMs, _useUnscaledTime, PlayerLoopTiming.Update, token);
                    }
                }
                
                OnComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Отмена — нормальное поведение
            }
            finally
            {
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }

        /// <summary>
        /// Останавливает эффект.
        /// </summary>
        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Завершает эффект мгновенно, возвращая полный текст.
        /// </summary>
        public string Complete()
        {
            Stop();
            _currentIndex = _fullText.Length;
            _builder.Clear();
            _builder.Append(_fullText);
            OnProgressChanged?.Invoke(1f);
            OnComplete?.Invoke();
            return _fullText;
        }

        /// <summary>
        /// Сбрасывает состояние.
        /// </summary>
        public void Reset()
        {
            Stop();
            _builder.Clear();
            _fullText = string.Empty;
            _currentIndex = 0;
        }
    }
}
