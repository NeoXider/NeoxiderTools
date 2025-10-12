using DG.Tweening;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        ///     A component that sets text on a TMP_Text component with formatting options.
        ///     Useful for displaying numeric values with separators and decimal places.
        /// </summary>
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SetText))]
        public class SetText : MonoBehaviour
        {
            #region Events

            /// <summary>
            ///     Invoked when the text is updated
            /// </summary>
            public UnityEvent<string> OnTextUpdated;

            #endregion

            private void OnEnable()
            {
                _tween?.Play();
            }

            #region Serialized Fields

            [Header("Text Component")] [Tooltip("The TextMeshPro text component to update")]
            public TMP_Text text;

            [Header("Formatting")] [Tooltip("Character used as thousand separator")] [SerializeField]
            protected string separator = ".";

            [Tooltip("Number of decimal places to display for float values")] [SerializeField] [Range(0, 10)]
            protected int @decimal;

            [Tooltip("Text to add before the value")] [SerializeField]
            protected string startAdd = "";

            [Tooltip("Text to add after the value")] [SerializeField]
            protected string endAdd = "";

            [Tooltip("Offset to add to integer values")] [SerializeField]
            protected int indexOffset;

            [Space] [Header("Anim")] [SerializeField]
            private float _timeAnim = 0.25f;

            [SerializeField] private Ease _ease = Ease.OutQuad;
            [SerializeField] private bool _onEnableAnim = true;
            private Tween _tween;
            public float currentNum;

            #endregion

            #region Properties

            /// <summary>
            ///     Gets or sets the separator character
            /// </summary>
            public string Separator
            {
                get => separator;
                set
                {
                    separator = value;
                    // Update text if it's currently displaying a number
                    if (text != null && !string.IsNullOrEmpty(text.text))
                        // Try to parse the current text as a number and update it
                    {
                        if (float.TryParse(text.text.Replace(separator, ""), out float currentValue))
                        {
                            Set(currentValue);
                        }
                    }
                }
            }

            /// <summary>
            ///     Gets or sets the number of decimal places
            /// </summary>
            public int DecimalPlaces
            {
                get => @decimal;
                set
                {
                    @decimal = Mathf.Clamp(value, 0, 10);
                    // Update text if it's currently displaying a number
                    if (text != null && !string.IsNullOrEmpty(text.text))
                        // Try to parse the current text as a number and update it
                    {
                        if (float.TryParse(text.text.Replace(separator, ""), out float currentValue))
                        {
                            Set(currentValue);
                        }
                    }
                }
            }

            /// <summary>
            ///     Gets or sets the index offset
            /// </summary>
            public int IndexOffset
            {
                get => indexOffset;
                set => indexOffset = value;
            }

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
                // Auto-assign text component if not set
                if (text == null)
                {
                    text = GetComponent<TMP_Text>();
                }

                // Ensure decimal places is within valid range
                @decimal = Mathf.Clamp(@decimal, 0, 10);
            }

            #endregion

            #region Public Methods

            /// <summary>
            ///     Sets the text to display an integer value with separator
            /// </summary>
            /// <param name="value">The integer value to display</param>
            public void Set(int value)
            {
                if (text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                Set((float)(value + indexOffset));
            }

            /// <summary>
            ///     Sets the text to display a float value with separator and decimal places
            /// </summary>
            /// <param name="value">The float value to display</param>
            [Button(nameof(Set) + "float")]
            public void Set(float value)
            {
                if (text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                if (_tween != null && _tween.IsActive())
                {
                    _tween.Kill();
                }

                if (_onEnableAnim && !gameObject.activeSelf)
                {
                    currentNum = 0;
                }

                float startValue = currentNum;
                float endValue = value;

                _tween = DOTween.To(() => startValue, x =>
                {
                    currentNum = x;
                    Set(x.FormatWithSeparator(separator, @decimal));
                }, endValue, _timeAnim).SetEase(_ease);

                if (_onEnableAnim && !gameObject.activeSelf)
                {
                    _tween?.Pause();
                }
            }

            /// <summary>
            ///     Sets the text to display a string value
            /// </summary>
            /// <param name="value">The string value to display</param>
#if ODIN_INSPECTOR
            [Button]
#else
            [Button]
#endif
            public void Set(string value = "0")
            {
                if (this.text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                string text = startAdd + value + endAdd;
                this.text.text = text;
                OnTextUpdated?.Invoke(text);
            }

            /// <summary>
            ///     Sets the text to display a percentage value
            /// </summary>
            /// <param name="value">The percentage value (0-100)</param>
            /// <param name="addPercentSign">Whether to add a % sign</param>
            public void SetPercentage(float value, bool addPercentSign = true)
            {
                if (text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                // Clamp value between 0 and 100
                value = Mathf.Clamp(value, 0, 100);

                // Format with decimal places
                string formattedValue = value.FormatWithSeparator(separator, @decimal);

                // Add percent sign if requested
                if (addPercentSign)
                {
                    formattedValue += "%";
                }

                Set(formattedValue);
            }

            /// <summary>
            ///     Sets the text to display a currency value
            /// </summary>
            /// <param name="value">The currency value</param>
            /// <param name="currencySymbol">The currency symbol to use</param>
            public void SetCurrency(float value, string currencySymbol = "$")
            {
                if (text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                // Format with decimal places
                string formattedValue = value.FormatWithSeparator(separator, @decimal);

                // Add currency symbol
                Set(currencySymbol + formattedValue);
            }

            /// <summary>
            ///     Clears the text
            /// </summary>
            public void Clear()
            {
                if (text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }

                text.text = "";
                OnTextUpdated?.Invoke("");
            }

            #endregion
        }
    }
}