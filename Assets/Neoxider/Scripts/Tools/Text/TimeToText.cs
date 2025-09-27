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
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(TimeToText))]
        public class TimeToText : MonoBehaviour
        {
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
            private TMP_Text text;

            [Header("Time Format")] [Tooltip("Whether to display text when time is zero")] [SerializeField]
            private bool _zeroText = true;

            [Tooltip("The format to use when displaying time")]
            private TimeFormat timeFormat = TimeFormat.MinutesSeconds;

            [Header("Text Formatting")] [Tooltip("Text to add before the time value")] [SerializeField]
            private string startAddText = "";

            [Tooltip("Text to add after the time value")] [SerializeField]
            private string endAddText = "";

            [Tooltip("Character to use as separator between time units")] [SerializeField]
            private string separator = ":";

            #endregion

            #region Events

            /// <summary>
            ///     Invoked when the time value reaches zero
            /// </summary>
            public UnityEvent OnEnd;

            /// <summary>
            ///     Invoked when the time value changes
            /// </summary>
            public UnityEvent<float> OnTimeChanged;

            #endregion

            #region Private Fields

            private float lastTime;

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
            ///     Gets the current time value
            /// </summary>
            public float CurrentTime { get; private set; }

            #endregion

            #region Unity Methods

            private void Awake()
            {
                // Ensure text component is assigned
                if (text == null)
                    text = GetComponent<TMP_Text>();
            }

            private void OnValidate()
            {
                // Auto-assign text component if not set
                if (text == null)
                    text = GetComponent<TMP_Text>();
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

                // Store current time
                CurrentTime = time;

                // Check if time has changed
                if (lastTime != time)
                {
                    OnTimeChanged?.Invoke(time);
                    lastTime = time;
                }

                // Display text based on zero text setting
                if ((time == 0 && _zeroText) || time > 0)
                    text.text = startAddText + FormatTime(time, timeFormat, separator) + endAddText;
                else
                    text.text = "";

                // Check if time has reached zero
                if (lastTime != time && time == 0) OnEnd?.Invoke();
            }

            /// <summary>
            ///     Updates the display with the current time value
            /// </summary>
            private void UpdateDisplay()
            {
                if (text == null)
                    return;

                Set(CurrentTime);
            }

            #endregion
        }
    }
}