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
        // WHY: the former whole-millisecond rounding skipped a wait below 0.5 ms; very fast runs (and tests) rely on
        // completing synchronously.
        private const float MinimumWaitSeconds = 0.0005f;

        // WHY: the carried overshoot keeps the rate when a character's delay is shorter than a frame, but one long
        // frame (a loading hitch, a GC spike, an app resume on unscaled time) must not reveal the rest of the line
        // at once. 0.1 s keeps the configured rate down to 10 fps and bounds a catch-up burst to that much typing.
        internal const float MaxCatchUpSeconds = 0.1f;

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

        // WHY: Identifies the current run; a cancelled run observes the cancellation one frame late
        // and must not dispose the CancellationTokenSource of a newer run started in the meantime.
        [NonSerialized] private int _runGeneration;

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

        /// <summary>The typed prefix of <see cref="FullText" />, rich-text tags included.</summary>
        public string CurrentText =>
            _currentIndex >= _fullText.Length ? _fullText : _fullText.Substring(0, _currentIndex);

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
        ///     Starts the typewriter effect, passing the typed prefix (rich-text tags included) after every step.
        /// </summary>
        /// <param name="text">Full text to type; null is treated as empty.</param>
        /// <param name="onTextChanged">Receives the typed prefix; a new string is built for every step.</param>
        /// <param name="externalToken">Cancels the run (the text stays as typed so far).</param>
        public UniTask PlayAsync(string text, Action<string> onTextChanged,
            CancellationToken externalToken = default)
        {
            return RunAsync(text, onTextChanged, null, externalToken);
        }

        /// <summary>
        ///     Starts the typewriter effect without building intermediate strings: reports how many visible
        ///     (non-tag) characters are revealed. Pair it with <c>TMP_Text.maxVisibleCharacters</c> on a label that
        ///     already shows the full text: no per-character allocation and no word-wrap jumps.
        /// </summary>
        /// <param name="text">Full text to type; null is treated as empty.</param>
        /// <param name="onVisibleCharactersChanged">Receives the revealed visible-character count (0 first).</param>
        /// <param name="externalToken">Cancels the run (the reveal stays where it is).</param>
        public UniTask PlayRevealAsync(string text, Action<int> onVisibleCharactersChanged,
            CancellationToken externalToken = default)
        {
            return RunAsync(text, null, onVisibleCharactersChanged, externalToken);
        }

        private async UniTask RunAsync(string text, Action<string> onTextChanged,
            Action<int> onVisibleCharactersChanged, CancellationToken externalToken)
        {
            Stop();

            // WHY: the prefix builder only feeds onTextChanged; a reveal run never touches it, so it costs
            // nothing per character (CurrentText is derived from FullText and the typed index).
            bool buildText = onTextChanged != null;
            _fullText = text ?? string.Empty;
            if (buildText)
            {
                _builder.Clear();
            }

            _currentIndex = 0;
            _typedVisibleCharacters = 0;
            _visibleCharacterCount = CountVisibleCharacters(_fullText);

            if (string.IsNullOrEmpty(_fullText))
            {
                onTextChanged?.Invoke(string.Empty);
                onVisibleCharactersChanged?.Invoke(0);
                return;
            }

            RebuildPauseMap();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            int generation = _runGeneration;
            CancellationToken token = _cts.Token;

            // WHY: A wait resumes on the first frame at or past its due time; carrying that overshoot into the next
            // character keeps the average rate at CharactersPerSecond instead of capping it at one character per
            // frame (70 cps typed about 60 cps at 60 fps). The carry is capped by MaxCatchUpSeconds.
            float overshoot = 0f;

            try
            {
                token.ThrowIfCancellationRequested();
                OnStart?.Invoke();
                token.ThrowIfCancellationRequested();
                onTextChanged?.Invoke(string.Empty);
                onVisibleCharactersChanged?.Invoke(0);
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < _fullText.Length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    if (TryReadRichTextTag(_fullText, i, out int richTextTagLength))
                    {
                        if (buildText)
                        {
                            _builder.Append(_fullText, i, richTextTagLength);
                        }

                        _currentIndex += richTextTagLength;
                        i += richTextTagLength - 1;
                        if (buildText)
                        {
                            onTextChanged(_builder.ToString());
                        }

                        token.ThrowIfCancellationRequested();
                        continue;
                    }

                    char c = _fullText[i];
                    if (buildText)
                    {
                        _builder.Append(c);
                    }

                    _currentIndex = i + 1;
                    _typedVisibleCharacters++;

                    if (buildText)
                    {
                        onTextChanged(_builder.ToString());
                    }

                    onVisibleCharactersChanged?.Invoke(_typedVisibleCharacters);
                    token.ThrowIfCancellationRequested();
                    OnCharacterTyped?.Invoke(c);
                    token.ThrowIfCancellationRequested();
                    OnProgressChanged?.Invoke(Progress);
                    token.ThrowIfCancellationRequested();
                    TryPlayTypingSound();

                    float totalDelay = 1f / Mathf.Max(0.1f, _charactersPerSecond);
                    if (_usePunctuationPauses && _punctuationPauseMap.TryGetValue(c, out float punctuationPause))
                    {
                        totalDelay += punctuationPause;
                    }

                    float wait = TakeWait(totalDelay, ref overshoot);
                    if (wait > 0f)
                    {
                        bool unscaled = _useUnscaledTime;
                        double waitStart = ReadClock(unscaled);
                        await UniTask.Delay(TimeSpan.FromSeconds(wait), unscaled, PlayerLoopTiming.Update, token);
                        overshoot = OvershootAfterWait(wait, (float)(ReadClock(unscaled) - waitStart));
                    }
                }

                token.ThrowIfCancellationRequested();
                _cts.Dispose();
                _cts = null;
                OnComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // WHY: Cancellation is expected
            }
            finally
            {
                // WHY: Only the current run may release the source; after Stop()+PlayAsync() in one frame
                // the stale run's finally would otherwise dispose the new run's source, leaving IsTyping
                // false while typing continues and turning Stop()/Complete() into no-ops.
                if (generation == _runGeneration && _cts != null)
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
            ++_runGeneration;
            CancellationTokenSource source = _cts;
            _cts = null;
            if (source != null)
            {
                source.Cancel();
                source.Dispose();
            }
        }

        /// <summary>
        ///     Completes the effect instantly, showing the full text.
        /// </summary>
        public string Complete()
        {
            string completedText = _fullText;
            Stop();
            int generation = _runGeneration;
            _currentIndex = _fullText.Length;
            _typedVisibleCharacters = _visibleCharacterCount;
            OnProgressChanged?.Invoke(1f);
            if (generation == _runGeneration)
            {
                OnComplete?.Invoke();
            }

            return completedText;
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

        /// <summary>
        ///     Seconds to wait after a character whose delay is <paramref name="delay" />, after spending the time a
        ///     previous wait overshot. Returns 0 when the rest is below <see cref="MinimumWaitSeconds" />: the next
        ///     character is then typed in the same frame.
        /// </summary>
        internal static float TakeWait(float delay, ref float overshoot)
        {
            float due = delay - overshoot;
            if (due < MinimumWaitSeconds)
            {
                overshoot = Mathf.Max(0f, -due);
                return 0f;
            }

            overshoot = 0f;
            return due;
        }

        /// <summary>
        ///     Time a wait of <paramref name="wait" /> seconds overshot, measured on the clock the delay used, capped
        ///     at <see cref="MaxCatchUpSeconds" />.
        /// </summary>
        /// <remarks>
        ///     WHY: never negative, so a clock that does not advance (Edit Mode) falls back to one plain delay per
        ///     character instead of stretching later waits; capped, so a stalled frame types at most
        ///     <see cref="MaxCatchUpSeconds" /> worth of extra characters and shortens a punctuation pause by at
        ///     most that much.
        /// </remarks>
        internal static float OvershootAfterWait(float wait, float elapsed)
        {
            return Mathf.Clamp(elapsed - wait, 0f, MaxCatchUpSeconds);
        }

        // WHY: UniTask.Delay sums Time.deltaTime (or unscaledDeltaTime) per frame, and the difference of the frame
        // start times below equals that sum, so the overshoot is measured on the same clock. The double clocks keep
        // that difference exact in long sessions, where a float clock's step nears a fast character's delay.
        private static double ReadClock(bool unscaled)
        {
            return unscaled ? Time.unscaledTimeAsDouble : Time.timeAsDouble;
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
                if (TryReadRichTextTag(text, i, out int tagLength))
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

        /// <summary>
        ///     Measures a rich-text tag starting at <paramref name="startIndex" /> without allocating a substring.
        /// </summary>
        private static bool TryReadRichTextTag(string text, int startIndex, out int tagLength)
        {
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
            return true;
        }
    }
}
