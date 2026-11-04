using System;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     A component that formats and displays time values on a TMP_Text component.
    ///     Useful for countdown timers, elapsed time displays, or any time-based UI elements.
    /// </summary>
    [NeoDoc("Tools/Text/TimeToText.md")]
    [CreateFromMenu("Neoxider/Tools/Text/TimeToText")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(TimeToText))]
    public class TimeToText : MonoBehaviour
    {
        #region Events

        /// <summary>
        ///     Invoked when the time value changes
        /// </summary>
        public UnityEvent<float> OnTimeChanged;

        #endregion

        #region Private Fields

        private float lastTime;

        private string _cachedDisplay;
        private long _cachedUnitKey;
        private bool _cachedNegative;
        private TimeDisplayMode _cachedMode;
        private TimeFormat _cachedFormat;
        private bool _cachedCompactUseTimeFormat;
        private bool _cachedCompactIncludeSeconds;
        private int _cachedCompactMaxParts;
        private string _cachedSeparator;
        private string _cachedStartAddText;
        private string _cachedEndAddText;

        #endregion

        #region Static Methods

        /// <summary>
        ///     Formats a time value according to the specified format
        /// </summary>
        /// <param name="time">The time value in seconds</param>
        /// <param name="format">The format to use</param>
        /// <param name="separator">The separator character to use</param>
        /// <returns>Formatted time string</returns>
        public static string FormatTime(float time, TimeFormat format = TimeFormat.Seconds, string separator = ":")
        {
            return time.FormatTime(format, separator);
        }

        #endregion

        #region Serialized Fields

        [Header("Text Component")] [Tooltip("The TextMeshPro text component to update")]
        public TMP_Text text;

        [Header("Time Format")] [Tooltip("Whether to display text when time is zero")] [SerializeField]
        private bool _zeroText = true;

        [Tooltip("Whether to allow and display negative time values")] [SerializeField]
        private bool _allowNegative;

        [Tooltip("Display mode: Clock = colon-separated (11:11), Compact = unit format (11d 11m)")] [SerializeField]
        private TimeDisplayMode _displayMode = TimeDisplayMode.Clock;

        [Tooltip("The format to use when displaying time (Clock mode, and Compact mode if enabled)")] [SerializeField]
        private TimeFormat timeFormat = TimeFormat.MinutesSeconds;

        [Tooltip(
            "If true, Compact mode forces the units specified in Time Format. If false, it dynamically skips zero units.")]
        [SerializeField]
        private bool _compactUseTimeFormat = true;

        [Tooltip("Include seconds in compact output (Compact mode only)")] [SerializeField]
        private bool _compactIncludeSeconds = true;

        [Tooltip("Maximum number of units in compact output (Compact mode only)")] [SerializeField] [Min(1)]
        private int _compactMaxParts = 3;

        [Header("Text Formatting")] [Tooltip("Text to add before the time value")] [SerializeField]
        private string startAddText = "";

        [Tooltip("Text to add after the time value")] [SerializeField]
        private string endAddText = "";

        [Tooltip("Character to use as separator between time units")] [SerializeField]
        private string separator = ":";

        [Header("Diagnostics")] [SerializeField]
        private bool _logWarnings;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the time format
        /// </summary>
        public TimeFormat TimeFormat
        {
            get => timeFormat;
            set
            {
                timeFormat = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets whether to display text when time is zero
        /// </summary>
        public bool ZeroText
        {
            get => _zeroText;
            set
            {
                _zeroText = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets whether negative time values are allowed and displayed
        /// </summary>
        public bool AllowNegative
        {
            get => _allowNegative;
            set
            {
                _allowNegative = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets the separator character
        /// </summary>
        public string Separator
        {
            get => separator;
            set
            {
                separator = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Text placed before the formatted time (Inspector "Start Add Text"); redraws immediately.
        /// </summary>
        public string Prefix
        {
            get => startAddText;
            set
            {
                startAddText = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Text placed after the formatted time (Inspector "End Add Text"); redraws immediately.
        /// </summary>
        public string Suffix
        {
            get => endAddText;
            set
            {
                endAddText = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets the display mode (Clock or Compact)
        /// </summary>
        public TimeDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (!CanDisplayTime(CurrentTime, value))
                {
                    if (_logWarnings)
                    {
                        NeoDiagnostics.LogWarning(
                            "[TimeToText] Current time cannot be displayed in the requested mode.", this);
                    }

                    return;
                }

                _displayMode = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets whether Compact mode strictly uses TimeFormat.
        /// </summary>
        public bool CompactUseTimeFormat
        {
            get => _compactUseTimeFormat;
            set
            {
                _compactUseTimeFormat = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets whether to include seconds in compact output
        /// </summary>
        public bool CompactIncludeSeconds
        {
            get => _compactIncludeSeconds;
            set
            {
                _compactIncludeSeconds = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets or sets the maximum number of units in compact output
        /// </summary>
        public int CompactMaxParts
        {
            get => _compactMaxParts;
            set
            {
                _compactMaxParts = Mathf.Max(1, value);
                UpdateDisplay();
            }
        }

        /// <summary>
        ///     Gets the current time value
        /// </summary>
        public float CurrentTime { get; private set; }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }
        }

        private void OnValidate()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            _compactMaxParts = Mathf.Max(1, _compactMaxParts);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets the text to display the specified time value
        /// </summary>
        /// <param name="time">The time value in seconds</param>
        /// <remarks>Non-finite or out-of-range values leave the current display and time unchanged.</remarks>
        public void Set(float time = 0)
        {
            if (text == null)
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning("[TimeToText] Text component is not assigned.", this);
                }

                return;
            }

            if (!CanDisplayTime(time))
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning("[TimeToText] Time is outside the supported display range.", this);
                }

                return;
            }

            if (!_allowNegative && time < 0f)
            {
                time = 0f;
            }

            float prevTime = lastTime;
            bool changed = !Mathf.Approximately(prevTime, time);
            CurrentTime = time;

            bool shouldDisplay = (time == 0 && _zeroText) || time > 0 || (_allowNegative && time < 0);
            if (shouldDisplay)
            {
                bool negative = _allowNegative && time < 0;
                float displayValue = negative ? Mathf.Abs(time) : time;
                // WHY: re-assigning the same cached string lets TMP skip the rebuild and keeps per-frame timers
                // allocation-free while the visible unit (usually a whole second) does not change.
                text.text = GetDisplayText(displayValue, negative);
            }
            else
            {
                text.text = "";
            }

            if (changed)
            {
                lastTime = time;
                OnTimeChanged?.Invoke(time);
            }
        }

        /// <summary>
        ///     Tries to parse a duration string (SS, MM:SS, HH:MM:SS, DD:HH:MM:SS) and set the displayed time.
        /// </summary>
        /// <param name="raw">Input duration text.</param>
        /// <param name="separator">Optional token separator.</param>
        /// <returns>True when parsing succeeds and text is updated.</returns>
        public bool TrySetFromString(string raw, string separator = null)
        {
            if (text != null &&
                TimeParsingExtensions.TryParseDuration(raw, out float seconds, separator ?? this.separator) &&
                CanDisplayTime(seconds))
            {
                Set(seconds);
                return true;
            }

            return false;
        }

        private bool CanDisplayTime(float time)
        {
            return CanDisplayTime(time, _displayMode);
        }

        /// <summary>
        ///     Returns the full display text (sign and affixes included), reusing the previous string while every
        ///     formatting input and the displayed unit are unchanged.
        /// </summary>
        private string GetDisplayText(float displayValue, bool negative)
        {
            bool compact = _displayMode == TimeDisplayMode.Compact;
            TimeSpan span = compact ? TimeSpan.FromSeconds(displayValue) : default;
            long unitKey = GetDisplayUnitKey(displayValue, span, compact);

            if (_cachedDisplay != null &&
                unitKey == _cachedUnitKey &&
                negative == _cachedNegative &&
                _displayMode == _cachedMode &&
                timeFormat == _cachedFormat &&
                _compactUseTimeFormat == _cachedCompactUseTimeFormat &&
                _compactIncludeSeconds == _cachedCompactIncludeSeconds &&
                _compactMaxParts == _cachedCompactMaxParts &&
                string.Equals(separator, _cachedSeparator, StringComparison.Ordinal) &&
                string.Equals(startAddText, _cachedStartAddText, StringComparison.Ordinal) &&
                string.Equals(endAddText, _cachedEndAddText, StringComparison.Ordinal))
            {
                return _cachedDisplay;
            }

            string formatted = compact
                ? _compactUseTimeFormat
                    ? span.ToCompactString(timeFormat)
                    : span.ToCompactString(_compactIncludeSeconds, _compactMaxParts)
                : displayValue.FormatTime(timeFormat, separator);

            bool hasStart = !string.IsNullOrEmpty(startAddText);
            bool hasEnd = !string.IsNullOrEmpty(endAddText);
            string sign = negative ? "-" : string.Empty;
            string display = !hasStart && !hasEnd && !negative
                ? formatted
                : string.Concat(hasStart ? startAddText : string.Empty, sign, formatted,
                    hasEnd ? endAddText : string.Empty);

            _cachedDisplay = display;
            _cachedUnitKey = unitKey;
            _cachedNegative = negative;
            _cachedMode = _displayMode;
            _cachedFormat = timeFormat;
            _cachedCompactUseTimeFormat = _compactUseTimeFormat;
            _cachedCompactIncludeSeconds = _compactIncludeSeconds;
            _cachedCompactMaxParts = _compactMaxParts;
            _cachedSeparator = separator;
            _cachedStartAddText = startAddText;
            _cachedEndAddText = endAddText;
            return display;
        }

        /// <summary>
        ///     Smallest unit the current format can show: equal keys always produce equal text.
        /// </summary>
        private long GetDisplayUnitKey(float displayValue, TimeSpan span, bool compact)
        {
            if (compact)
            {
                // WHY: compact text is built from this exact TimeSpan. Dynamic compact output and the whole-second
                // formats only show whole seconds (and larger units derived from them); anything else is keyed by
                // the exact TimeSpan.
                return !_compactUseTimeFormat || ShowsWholeSecondsOrCoarser(timeFormat)
                    ? span.Ticks / TimeSpan.TicksPerSecond
                    : span.Ticks;
            }

            if (ShowsWholeSecondsOrCoarser(timeFormat))
            {
                // WHY: the clock formatter truncates to whole seconds (saturating at int.MaxValue) before splitting.
                return (int)Math.Min((double)displayValue, int.MaxValue);
            }

            // WHY: fractional and unknown formats are not keyed by unit; only an identical value reuses the text.
            return BitConverter.DoubleToInt64Bits(displayValue);
        }

        /// <summary>
        ///     Allow-list of formats whose output depends only on the whole seconds. A format missing here (a
        ///     sub-second one, or one added later) falls back to exact-value caching instead of showing stale text.
        /// </summary>
        private static bool ShowsWholeSecondsOrCoarser(TimeFormat format)
        {
            switch (format)
            {
                case TimeFormat.Seconds:
                case TimeFormat.Minutes:
                case TimeFormat.MinutesSeconds:
                case TimeFormat.Hours:
                case TimeFormat.HoursMinutes:
                case TimeFormat.HoursMinutesSeconds:
                case TimeFormat.Days:
                case TimeFormat.DaysHours:
                case TimeFormat.DaysHoursMinutes:
                case TimeFormat.DaysHoursMinutesSeconds:
                    return true;
                default:
                    return false;
            }
        }

        private bool CanDisplayTime(float time, TimeDisplayMode displayMode)
        {
            if (float.IsNaN(time) || float.IsInfinity(time))
            {
                return false;
            }

            double seconds = !_allowNegative && time < 0f ? 0d : Math.Abs((double)time);
            if (displayMode != TimeDisplayMode.Compact)
            {
                // WHY: the clock formatter converts total seconds to Int32 before splitting units.
                return seconds <= int.MaxValue;
            }

            return seconds < TimeSpan.MaxValue.TotalSeconds;
        }

        /// <summary>
        ///     Updates the display with the current time value
        /// </summary>
        private void UpdateDisplay()
        {
            if (text == null)
            {
                return;
            }

            Set(CurrentTime);
        }

        #endregion
    }
}
