using System;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
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

            [Tooltip("The format to use when displaying time (Clock mode only)")] [SerializeField]
            private TimeFormat timeFormat = TimeFormat.MinutesSeconds;

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
            ///     Gets or sets the display mode (Clock or Compact)
            /// </summary>
            public TimeDisplayMode DisplayMode
            {
                get => _displayMode;
                set
                {
                    _displayMode = value;
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
                // Ensure text component is assigned
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
            public void Set(float time = 0)
            {
                if (text == null)
                {
                    Debug.LogWarning("TimeToText: Text component is not assigned");
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
                    float displayValue = _allowNegative && time < 0 ? Mathf.Abs(time) : time;
                    string formatted = _displayMode == TimeDisplayMode.Compact
                        ? TimeSpan.FromSeconds(displayValue).ToCompactString(_compactIncludeSeconds, _compactMaxParts)
                        : displayValue.FormatTime(timeFormat, separator);
                    if (_allowNegative && time < 0)
                    {
                        formatted = "-" + formatted;
                    }

                    if (string.IsNullOrEmpty(startAddText))
                    {
                        text.text = string.IsNullOrEmpty(endAddText) ? formatted : formatted + endAddText;
                    }
                    else
                    {
                        text.text = string.IsNullOrEmpty(endAddText)
                            ? startAddText + formatted
                            : startAddText + formatted + endAddText;
                    }
                }
                else
                {
                    text.text = "";
                }

                if (changed)
                {
                    OnTimeChanged?.Invoke(time);
                    lastTime = time;
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
                if (TimeParsingExtensions.TryParseDuration(raw, out float seconds, separator ?? this.separator))
                {
                    Set(seconds);
                    return true;
                }

                return false;
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
}