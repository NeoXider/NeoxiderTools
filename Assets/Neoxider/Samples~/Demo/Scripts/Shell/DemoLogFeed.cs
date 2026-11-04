using System;
using System.Text;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bottom "LOG FEED" of an authored demo frame. Shows the newest line first, keeps a bounded number of
    ///     lines and collapses an identical repeated line into a counter. Call <see cref="Log" /> from code or
    ///     wire it as a persistent UnityEvent listener with a string argument. There is no Update loop; the
    ///     text is rebuilt into a reused <see cref="StringBuilder" /> only when a line is logged.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Demo Log Feed")]
    [DisallowMultipleComponent]
    public sealed class DemoLogFeed : MonoBehaviour
    {
        /// <summary>Upper bound for <see cref="MaxLines" />.</summary>
        public const int MaxCapacity = 32;

        private const string NewestMarker = "› ";
        private const string OlderIndent = "  ";
        private const string RepeatMarker = "  ×";
        private const string ColorClose = "</color>";

        [Tooltip("TextMeshPro label the feed renders into.")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("How many lines stay visible; older lines drop off.")]
        [SerializeField] [Range(1, MaxCapacity)] private int _maxLines = 5;

        [SerializeField] private Color _lineColor = new(0.62f, 0.64f, 0.74f, 1f);

        [Tooltip("Alpha of the oldest visible line; newer lines fade toward full alpha.")]
        [SerializeField] [Range(0f, 1f)] private float _oldestAlpha = 0.35f;

        [Tooltip("Line shown before anything is logged.")]
        [SerializeField] private string _readyMessage = "ready";

        [Tooltip("Also mirror every line to the Unity console (Editor and development builds only).")]
        [SerializeField] private bool _mirrorToConsole;

        private string[] _lines;
        private int[] _repeats;
        private string[] _prefixes;
        private StringBuilder _builder;
        private int _capacity;
        private int _head = -1;
        private int _count;

        /// <summary>Raised after a line was accepted (also for a collapsed repeat).</summary>
        public event Action<string> Logged;

        public TMP_Text Target => _text;
        public int MaxLines => _maxLines;

        /// <summary>Number of distinct visible lines.</summary>
        public int Count => _count;

        private void Awake()
        {
            EnsureBuffers();
            if (_count == 0)
            {
                Render();
            }
        }

        private void OnValidate()
        {
            _maxLines = Mathf.Clamp(_maxLines, 1, MaxCapacity);
            _prefixes = null;
        }

        /// <summary>Pushes a line onto the feed. Empty messages are ignored.</summary>
        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            EnsureBuffers();
            if (_count > 0 && string.Equals(_lines[_head], message, StringComparison.Ordinal))
            {
                _repeats[_head]++;
            }
            else
            {
                _head = (_head + 1) % _capacity;
                _lines[_head] = message;
                _repeats[_head] = 1;
                if (_count < _capacity)
                {
                    _count++;
                }
            }

            Render();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_mirrorToConsole)
            {
                Debug.Log("[DemoLog] " + message, this);
            }
#endif
            Logged?.Invoke(message);
        }

        /// <summary>Removes every line and shows the ready message again.</summary>
        public void Clear()
        {
            EnsureBuffers();
            for (int i = 0; i < _capacity; i++)
            {
                _lines[i] = null;
                _repeats[i] = 0;
            }

            _head = -1;
            _count = 0;
            Render();
        }

        /// <summary>Returns a visible line, newest first (index 0 is the latest).</summary>
        public string GetLine(int index)
        {
            return IsValidIndex(index) ? _lines[ToSlot(index)] : null;
        }

        /// <summary>How many times the line at <paramref name="index" /> was logged in a row.</summary>
        public int GetRepeatCount(int index)
        {
            return IsValidIndex(index) ? _repeats[ToSlot(index)] : 0;
        }

        /// <summary>Points the feed at another label (code-built frames, tests).</summary>
        public void SetTarget(TMP_Text text)
        {
            _text = text;
            Render();
        }

        private bool IsValidIndex(int index)
        {
            return _lines != null && index >= 0 && index < _count;
        }

        private int ToSlot(int index)
        {
            return (_head - index + _capacity) % _capacity;
        }

        private void EnsureBuffers()
        {
            int capacity = Mathf.Clamp(_maxLines, 1, MaxCapacity);
            if (_lines == null || _capacity != capacity)
            {
                _capacity = capacity;
                _lines = new string[capacity];
                _repeats = new int[capacity];
                _head = -1;
                _count = 0;
                _prefixes = null;
            }

            _builder ??= new StringBuilder(256);
            if (_prefixes != null)
            {
                return;
            }

            _prefixes = new string[capacity];
            for (int i = 0; i < capacity; i++)
            {
                float alpha = Mathf.Lerp(1f, _oldestAlpha, i / (float)capacity) * _lineColor.a;
                Color color = new(_lineColor.r, _lineColor.g, _lineColor.b, alpha);
                _prefixes[i] = "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">";
            }
        }

        private void Render()
        {
            if (_text == null)
            {
                return;
            }

            EnsureBuffers();
            _builder.Clear();
            if (_count == 0)
            {
                _builder.Append(_prefixes[0]).Append(NewestMarker).Append(_readyMessage).Append(ColorClose);
            }

            for (int i = 0; i < _count; i++)
            {
                int slot = ToSlot(i);
                _builder.Append(_prefixes[i]).Append(i == 0 ? NewestMarker : OlderIndent).Append(_lines[slot]);
                if (_repeats[slot] > 1)
                {
                    _builder.Append(RepeatMarker).Append(_repeats[slot]);
                }

                _builder.Append(ColorClose);
                if (i < _count - 1)
                {
                    _builder.Append('\n');
                }
            }

            _text.SetText(_builder);
        }
    }
}
