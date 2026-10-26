using System;
using System.Numerics;
using DG.Tweening;
using Neo.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     A component that sets text on a TMP_Text component with formatting options.
    ///     Useful for displaying numeric values with separators and decimal places.
    /// </summary>
    [NeoDoc("Tools/Text/SetText.md")]
    [CreateFromMenu("Neoxider/Tools/Text/SetText")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SetText))]
    public class SetText : MonoBehaviour
    {
        #region Events

        /// <summary>
        ///     Invoked when the text is updated
        /// </summary>
        public UnityEvent<string> OnTextUpdated;

        #endregion

        protected virtual void OnEnable()
        {
            if (_tween != null && _tween.IsActive())
            {
                _tween.Play();
            }
        }

        protected virtual void OnDisable()
        {
            if (_onEnableAnim && _tween != null && _tween.IsActive())
            {
                _tween.Pause();
            }
        }

        private void OnDestroy()
        {
            StopAnimation();
        }

        private void StopAnimation()
        {
            Tween tween = _tween;
            _tween = null;
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        private void RefreshNumber()
        {
            if (text != null && _formatCurrentNumber != null)
            {
                ApplyText(_formatCurrentNumber());
            }
        }

        private void ApplyText(string value)
        {
            string formatted = startAdd + value + endAdd;
            text.text = formatted;
            OnTextUpdated?.Invoke(formatted);
        }

        private NumberFormatOptions BuildNumberFormatOptions(string prefix = "", string suffix = "")
        {
            return new NumberFormatOptions(
                numberNotation,
                @decimal,
                roundingMode,
                trimTrailingZeros,
                separator,
                decimalSeparator,
                prefix,
                suffix);
        }

        private string FormatNumber(float value)
        {
            NumberFormatOptions options = BuildNumberFormatOptions();
            return value.ToPrettyString(options);
        }

        #region Serialized Fields

        [Header("Text Component")] [Tooltip("The TextMeshPro text component to update")]
        public TMP_Text text;

        [Header("Formatting")] [Tooltip("Character used as thousand separator")] [SerializeField]
        protected string separator = ".";

        [Tooltip("Number of decimal places to display for float values")] [SerializeField] [Range(0, 10)]
        protected int @decimal;

        [Tooltip("Number notation style used for numeric values")] [SerializeField]
        protected NumberNotation numberNotation = NumberNotation.Grouped;

        [Tooltip("Rounding strategy used before converting number to string")] [SerializeField]
        protected NumberRoundingMode roundingMode = NumberRoundingMode.ToEven;

        [Tooltip("Trim trailing zeros in decimal part")] [SerializeField]
        protected bool trimTrailingZeros = true;

        [Tooltip("Decimal separator used in the final text")] [SerializeField]
        protected string decimalSeparator = ".";

        [Tooltip("Text to add before the value")] [SerializeField]
        protected string startAdd = "";

        [Tooltip("Text to add after the value")] [SerializeField]
        protected string endAdd = "";

        [Tooltip("Offset to add to integer values")] [SerializeField]
        protected int indexOffset;

        [Header("Diagnostics")] [SerializeField]
        private bool _logWarnings;

        [Space] [Header("Anim")] [SerializeField]
        private float _timeAnim = 0.25f;

        [SerializeField] private Ease _ease = Ease.OutQuad;
        [SerializeField] private bool _onEnableAnim = true;
        private Tween _tween;
        private Func<string> _formatCurrentNumber;
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
                if (separator == value)
                {
                    return;
                }

                separator = value;
                RefreshNumber();
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
                int decimals = Mathf.Clamp(value, 0, 10);
                if (@decimal == decimals)
                {
                    return;
                }

                @decimal = decimals;
                RefreshNumber();
            }
        }

        public NumberNotation NumberNotationStyle
        {
            get => numberNotation;
            set
            {
                if (numberNotation == value)
                {
                    return;
                }

                numberNotation = value;
                RefreshNumber();
            }
        }

        public NumberRoundingMode RoundingMode
        {
            get => roundingMode;
            set
            {
                if (roundingMode == value)
                {
                    return;
                }

                roundingMode = value;
                RefreshNumber();
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

            @decimal = Mathf.Clamp(@decimal, 0, 10);

            if (string.IsNullOrEmpty(decimalSeparator))
            {
                decimalSeparator = ".";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Animates an integer plus IndexOffset and preserves its exact final formatted value.
        /// </summary>
        /// <param name="value">The integer value to display</param>
        public void Set(int value)
        {
            SetInteger((long)value + indexOffset);
        }

        /// <summary>
        ///     Animates a final integer value without applying IndexOffset.
        ///     Interpolation and currentNum use float; the completed display retains the exact integer.
        /// </summary>
        /// <param name="value">The final integer value, including any caller-supplied offset.</param>
        public void SetInteger(long value)
        {
            if (text == null)
            {
                LogMissingTextWarning();
                return;
            }

            Set((float)value);
            Tween tween = _tween;
            BigInteger integer = new BigInteger(value);
            // Replace float completion: integer text is exact while currentNum stays interpolated.
            tween.OnComplete(() =>
            {
                if (_tween != tween)
                {
                    return;
                }

                StopAnimation();
                _formatCurrentNumber = () => integer.ToPrettyString(BuildNumberFormatOptions());
                if (text != null)
                {
                    string formatted = _formatCurrentNumber();
                    if (text.text != startAdd + formatted + endAdd)
                    {
                        ApplyText(formatted);
                    }
                }
            });
        }

        /// <summary>
        ///     Sets the text to display a float value with separator and decimal places
        /// </summary>
        /// <param name="value">The float value to display</param>
        [Button(nameof(Set) + "float", PlayModeOnly = true)]
        public void Set(float value)
        {
            if (text == null)
            {
                LogMissingTextWarning();
                return;
            }

            StopAnimation();

            if (_onEnableAnim && !isActiveAndEnabled)
            {
                currentNum = 0;
            }

            _formatCurrentNumber = () => FormatNumber(currentNum);
            Tween tween = null;
            tween = DOTween.To(() => currentNum, x =>
            {
                if (tween == null || _tween != tween)
                {
                    return;
                }

                currentNum = x;
                RefreshNumber();
            }, value, _timeAnim).SetEase(_ease).SetTarget(this).SetRecyclable(false).SetLink(gameObject);
            _tween = tween;
            tween.OnComplete(() =>
            {
                if (_tween != tween)
                {
                    return;
                }

                // Interpolation from a large float can lose a small target in start + change.
                currentNum = value;
                if (text != null)
                {
                    string formatted = FormatNumber(value);
                    if (text.text != startAdd + formatted + endAdd)
                    {
                        ApplyText(formatted);
                    }
                }
            });

            if (_onEnableAnim && !isActiveAndEnabled)
            {
                _tween?.Pause();
            }
        }

        /// <summary>
        ///     Sets the text to display a string value
        /// </summary>
        /// <param name="value">The string value to display</param>
        [Button]
        public void Set(string value = "0")
        {
            if (this.text == null)
            {
                LogMissingTextWarning();
                return;
            }

            StopAnimation();
            _formatCurrentNumber = null;
            ApplyText(value);
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
                LogMissingTextWarning();
                return;
            }

            value = Mathf.Clamp(value, 0, 100);
            StopAnimation();
            _formatCurrentNumber = () => value.ToPrettyString(
                BuildNumberFormatOptions(suffix: addPercentSign ? "%" : ""));
            RefreshNumber();
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
                LogMissingTextWarning();
                return;
            }

            StopAnimation();
            _formatCurrentNumber = () => value.ToPrettyString(BuildNumberFormatOptions(currencySymbol));
            RefreshNumber();
        }

        /// <summary>
        ///     Sets the text to display a BigInteger value without animation.
        /// </summary>
        public void SetBigInteger(BigInteger value)
        {
            if (text == null)
            {
                LogMissingTextWarning();
                return;
            }

            StopAnimation();
            _formatCurrentNumber = () => value.ToPrettyString(BuildNumberFormatOptions());
            RefreshNumber();
        }

        /// <summary>
        ///     Parses string as BigInteger and sets formatted value.
        /// </summary>
        public void SetBigInteger(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Set();
                return;
            }

            if (!BigInteger.TryParse(value, out BigInteger parsed))
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning($"[SetText] Cannot parse BigInteger from '{value}'.", this);
                }

                return;
            }

            SetBigInteger(parsed);
        }

        /// <summary>
        ///     Sets value using custom format options.
        /// </summary>
        public void SetFormatted(float value, NumberFormatOptions options)
        {
            Set(value.ToPrettyString(options));
        }

        /// <summary>
        ///     Sets value using custom format options.
        /// </summary>
        public void SetFormatted(BigInteger value, NumberFormatOptions options)
        {
            Set(value.ToPrettyString(options));
        }

        /// <summary>
        ///     Clears the text
        /// </summary>
        public void Clear()
        {
            StopAnimation();
            _formatCurrentNumber = null;

            if (text == null)
            {
                LogMissingTextWarning();
                return;
            }

            text.text = "";
            OnTextUpdated?.Invoke("");
        }

        private void LogMissingTextWarning()
        {
            if (_logWarnings)
            {
                NeoDiagnostics.LogWarning("[SetText] Text component is not assigned.", this);
            }
        }

        #endregion
    }
}
