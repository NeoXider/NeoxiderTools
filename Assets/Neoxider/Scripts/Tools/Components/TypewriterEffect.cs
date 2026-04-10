using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Pause settings for a punctuation character.
    /// </summary>
    [Serializable]
    public class PunctuationPause
    {
        [Tooltip("Punctuation character")] public char character;

        [Tooltip("Extra delay in seconds after this character")] [Min(0)]
        public float pause;

        public PunctuationPause()
        {
        }

        public PunctuationPause(char character, float pause)
        {
            this.character = character;
            this.pause = pause;
        }
    }

    /// <summary>
    ///     Typewriter effect for character-by-character text output (UniTask).
    /// </summary>
    [Serializable]
    public class TypewriterEffect
    {
        /// <summary>
        ///     Default predefined list of punctuation pauses.
        /// </summary>
        public static readonly PunctuationPause[] DefaultPunctuationPauses =
        {
            new('.', 0.3f),
            new('!', 0.3f),
            new('?', 0.3f),
            new(',', 0.15f),
            new(';', 0.15f),
            new(':', 0.2f),
            new('—', 0.2f),
            new('-', 0.1f),
            new('…', 0.5f),
            new('\n', 0.2f)
        };

        [SerializeField] [Min(0.1f)] private float _charactersPerSecond = 70f;
        [SerializeField] private bool _useUnscaledTime;
        [SerializeField] private bool _usePunctuationPauses = true;
        [SerializeField] private List<PunctuationPause> _punctuationPauses = new();

        [Header("Typing Audio")] [SerializeField]
        private AudioSource _typingAudioSource;

        [SerializeField] private AudioClip _typingAudioClip;
        [SerializeField] private bool _playTypingSound;
        [SerializeField] [Min(1)] private int _playTypingSoundEveryCharacters = 3;

        [NonSerialized] private readonly StringBuilder _builder = new(256);
        [NonSerialized] private readonly Dictionary<char, float> _punctuationPauseMap = new();
        [NonSerialized] private CancellationTokenSource _cts;
        [NonSerialized] private int _currentIndex;
        [NonSerialized] private string _fullText = string.Empty;
        [NonSerialized] private int _typedVisibleCharacters;
        [NonSerialized] private int _visibleCharacterCount;

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

        public AudioSource TypingAudioSource
        {
            get => _typingAudioSource;
            set => _typingAudioSource = value;
        }

        public AudioClip TypingAudioClip
        {
            get => _typingAudioClip;
            set => _typingAudioClip = value;
        }

        public bool PlayTypingSound
        {
            get => _playTypingSound;
            set => _playTypingSound = value;
        }

        public int PlayTypingSoundEveryCharacters
        {
            get => _playTypingSoundEveryCharacters;
            set => _playTypingSoundEveryCharacters = Mathf.Max(1, value);
        }

        public List<PunctuationPause> PunctuationPauses => _punctuationPauses;

        public bool IsTyping => _cts != null && !_cts.IsCancellationRequested;

        public float Progress =>
            _visibleCharacterCount <= 0 ? 0f : (float)_typedVisibleCharacters / _visibleCharacterCount;

        public string CurrentText => _builder.ToString();
        public string FullText => _fullText;

        public event Action OnStart;
        public event Action OnComplete;
        public event Action<char> OnCharacterTyped;
        public event Action<float> OnProgressChanged;

        /// <summary>
        ///     Sets the predefined list of punctuation pauses.
        /// </summary>
        public void SetDefaultPunctuationPauses()
        {
            _punctuationPauses.Clear();
            foreach (PunctuationPause p in DefaultPunctuationPauses)
            {
                _punctuationPauses.Add(new PunctuationPause(p.character, p.pause));
            }

            RebuildPauseMap();
        }

        /// <summary>
        ///     Clears all punctuation pauses.
        /// </summary>
        public void ClearPunctuationPauses()
        {
            _punctuationPauses.Clear();
            _punctuationPauseMap.Clear();
        }

        /// <summary>
        ///     Adds or updates the pause for a character (in seconds).
        /// </summary>
        public void SetPunctuationPause(char character, float pause)
        {
            PunctuationPause existing = _punctuationPauses.Find(p => p.character == character);
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
        ///     Sets pauses from a dictionary (replaces current).
        /// </summary>
        public void SetPunctuationPauses(Dictionary<char, float> pauses)
        {
            _punctuationPauses.Clear();
            _punctuationPauseMap.Clear();
            foreach (KeyValuePair<char, float> kvp in pauses)
            {
                _punctuationPauses.Add(new PunctuationPause(kvp.Key, kvp.Value));
                _punctuationPauseMap[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        ///     Adds pauses from a dictionary (merges with current).
        /// </summary>
        public void AddPunctuationPauses(Dictionary<char, float> pauses)
        {
            foreach (KeyValuePair<char, float> kvp in pauses)
            {
                SetPunctuationPause(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        ///     Removes the pause for a character.
        /// </summary>
        public void RemovePunctuationPause(char character)
        {
            _punctuationPauses.RemoveAll(p => p.character == character);
            _punctuationPauseMap.Remove(character);
        }

        /// <summary>
        ///     Gets the pause for a character in seconds (0 if not found).
        /// </summary>
        public float GetPunctuationPause(char character)
        {
            return _punctuationPauseMap.TryGetValue(character, out float pause) ? pause : 0f;
        }

        /// <summary>
        ///     Rebuilds the internal pause map from the list.
        /// </summary>
        public void RebuildPauseMap()
        {
            _punctuationPauseMap.Clear();
            foreach (PunctuationPause p in _punctuationPauses)
            {
                _punctuationPauseMap[p.character] = p.pause;
            }
        }

        /// <summary>
        ///     Starts the typewriter effect.
        /// </summary>
        public async UniTask PlayAsync(string text, Action<string> onTextChanged,
            CancellationToken externalToken = default)
        {
            Stop();

            _fullText = text ?? string.Empty;
            _builder.Clear();
            _currentIndex = 0;
            _typedVisibleCharacters = 0;
            _visibleCharacterCount = CountVisibleCharacters(_fullText);

            if (string.IsNullOrEmpty(_fullText))
            {
                onTextChanged?.Invoke(string.Empty);
                return;
            }

            RebuildPauseMap();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            CancellationToken token = _cts.Token;

            OnStart?.Invoke();
            onTextChanged?.Invoke(string.Empty);

            try
            {
                for (int i = 0; i < _fullText.Length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    if (TryReadRichTextTag(_fullText, i, out string richTextTag, out int richTextTagLength))
                    {
                        _builder.Append(richTextTag);
                        _currentIndex += richTextTagLength;
                        i += richTextTagLength - 1;
                        onTextChanged?.Invoke(_builder.ToString());
                        continue;
                    }

                    char c = _fullText[i];
                    _builder.Append(c);
                    _currentIndex = i + 1;
                    _typedVisibleCharacters++;

                    onTextChanged?.Invoke(_builder.ToString());
                    OnCharacterTyped?.Invoke(c);
                    OnProgressChanged?.Invoke(Progress);
                    TryPlayTypingSound();

                    // Compute delay
                    float totalDelay = 1f / Mathf.Max(0.1f, _charactersPerSecond);
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
                // Cancellation is expected
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
        ///     Stops the effect.
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
        ///     Completes the effect instantly, showing the full text.
        /// </summary>
        public string Complete()
        {
            Stop();
            _currentIndex = _fullText.Length;
            _typedVisibleCharacters = _visibleCharacterCount;
            _builder.Clear();
            _builder.Append(_fullText);
            OnProgressChanged?.Invoke(1f);
            OnComplete?.Invoke();
            return _fullText;
        }

        /// <summary>
        ///     Resets state.
        /// </summary>
        public void Reset()
        {
            Stop();
            _builder.Clear();
            _fullText = string.Empty;
            _currentIndex = 0;
            _typedVisibleCharacters = 0;
            _visibleCharacterCount = 0;
        }

        private static int CountVisibleCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (TryReadRichTextTag(text, i, out _, out int tagLength))
                {
                    i += tagLength - 1;
                    continue;
                }

                count++;
            }

            return count;
        }

        private void TryPlayTypingSound()
        {
            if (!_playTypingSound || _typingAudioSource == null)
            {
                return;
            }

            int playEvery = Mathf.Max(1, _playTypingSoundEveryCharacters);
            if (_typedVisibleCharacters <= 0 || _typedVisibleCharacters % playEvery != 0)
            {
                return;
            }

            if (_typingAudioClip != null)
            {
                _typingAudioSource.PlayOneShot(_typingAudioClip);
                return;
            }

            _typingAudioSource.Play();
        }

        private static bool TryReadRichTextTag(string text, int startIndex, out string tag, out int tagLength)
        {
            tag = string.Empty;
            tagLength = 0;

            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex >= text.Length || text[startIndex] != '<')
            {
                return false;
            }

            int endIndex = text.IndexOf('>', startIndex);
            if (endIndex < 0)
            {
                return false;
            }

            tagLength = endIndex - startIndex + 1;
            tag = text.Substring(startIndex, tagLength);
            return true;
        }
    }
}
