using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        /// <summary>
        /// A component that sets text on a TMP_Text component with formatting options.
        /// Useful for displaying numeric values with separators and decimal places.
        /// </summary>
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SetText))]
        public class SetText : MonoBehaviour
        {
            #region Serialized Fields
            [Header("Text Component")]
            [Tooltip("The TextMeshPro text component to update")]
            [SerializeField] protected TMP_Text _text;

            [Header("Formatting")]
            [Tooltip("Character used as thousand separator")]
            [SerializeField] protected string _separator = ".";
            
            [Tooltip("Number of decimal places to display for float values")]
            [SerializeField] [Range(0, 10)] protected int _decimal = 2;
            
            [Tooltip("Text to add before the value")]
            [SerializeField] protected string startAdd = "";
            
            [Tooltip("Text to add after the value")]
            [SerializeField] protected string endAdd = "";
            
            [Tooltip("Offset to add to integer values")]
            [SerializeField] protected int _indexOffset;
            #endregion
            
            #region Events
            /// <summary>
            /// Invoked when the text is updated
            /// </summary>
            public UnityEvent<string> OnTextUpdated;
            #endregion
            
            #region Properties
            /// <summary>
            /// Gets or sets the separator character
            /// </summary>
            public string Separator
            {
                get => _separator;
                set
                {
                    _separator = value;
                    // Update text if it's currently displaying a number
                    if (_text != null && !string.IsNullOrEmpty(_text.text))
                    {
                        // Try to parse the current text as a number and update it
                        if (float.TryParse(_text.text.Replace(_separator, ""), out float currentValue))
                        {
                            Set(currentValue);
                        }
                    }
                }
            }
            
            /// <summary>
            /// Gets or sets the number of decimal places
            /// </summary>
            public int DecimalPlaces
            {
                get => _decimal;
                set
                {
                    _decimal = Mathf.Clamp(value, 0, 10);
                    // Update text if it's currently displaying a number
                    if (_text != null && !string.IsNullOrEmpty(_text.text))
                    {
                        // Try to parse the current text as a number and update it
                        if (float.TryParse(_text.text.Replace(_separator, ""), out float currentValue))
                        {
                            Set(currentValue);
                        }
                    }
                }
            }
            
            /// <summary>
            /// Gets or sets the index offset
            /// </summary>
            public int IndexOffset
            {
                get => _indexOffset;
                set => _indexOffset = value;
            }
            #endregion

            #region Unity Methods
            private void Awake()
            {
                // Ensure text component is assigned
                if (_text == null)
                    _text = GetComponent<TMP_Text>();
            }

            private void OnValidate()
            {
                // Auto-assign text component if not set
                if (_text == null)
                    _text = GetComponent<TMP_Text>();
                
                // Ensure decimal places is within valid range
                _decimal = Mathf.Clamp(_decimal, 0, 10);
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// Sets the text to display an integer value with separator
            /// </summary>
            /// <param name="value">The integer value to display</param>
            public void Set(int value)
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                Set((value + _indexOffset).FormatWithSeparator(_separator));
            }

            /// <summary>
            /// Sets the text to display a float value with separator and decimal places
            /// </summary>
            /// <param name="value">The float value to display</param>
            public void Set(float value)
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                Set(value.FormatWithSeparator(_separator, _decimal));
            }

            /// <summary>
            /// Sets the text to display a string value
            /// </summary>
            /// <param name="value">The string value to display</param>
            public void Set(string value)
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                var text = startAdd + value + endAdd;
                _text.text = text;
                OnTextUpdated?.Invoke(text);
            }
            
            /// <summary>
            /// Sets the text to display a percentage value
            /// </summary>
            /// <param name="value">The percentage value (0-100)</param>
            /// <param name="addPercentSign">Whether to add a % sign</param>
            public void SetPercentage(float value, bool addPercentSign = true)
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                // Clamp value between 0 and 100
                value = Mathf.Clamp(value, 0, 100);
                
                // Format with decimal places
                string formattedValue = value.FormatWithSeparator(_separator, _decimal);
                
                // Add percent sign if requested
                if (addPercentSign)
                    formattedValue += "%";
                
                Set(formattedValue);
            }
            
            /// <summary>
            /// Sets the text to display a currency value
            /// </summary>
            /// <param name="value">The currency value</param>
            /// <param name="currencySymbol">The currency symbol to use</param>
            public void SetCurrency(float value, string currencySymbol = "$")
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                // Format with decimal places
                string formattedValue = value.FormatWithSeparator(_separator, _decimal);
                
                // Add currency symbol
                Set(currencySymbol + formattedValue);
            }
            
            /// <summary>
            /// Clears the text
            /// </summary>
            public void Clear()
            {
                if (_text == null)
                {
                    Debug.LogWarning("SetText: Text component is not assigned");
                    return;
                }
                
                _text.text = "";
                OnTextUpdated?.Invoke("");
            }
            #endregion
        }
    }
}