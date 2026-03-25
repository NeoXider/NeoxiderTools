using System;
using Neo.Reactive;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.UI
{
    /// <summary>
    ///     Universal component for switching between two visual states with support for multiple elements.
    ///     Can run standalone or integrate automatically with a Toggle.
    /// </summary>
    /// <remarks>
    ///     Manages multiple Images (sprites and colors), TextMeshPro (colors and text), and GameObjects at once.
    ///     Subscribes to Toggle events when a Toggle is present.
    /// </remarks>
    [NeoDoc("UI/VisualToggle.md")]
    [CreateFromMenu("Neoxider/UI/VisualToggle", "Prefabs/UI/VisualToggle.prefab")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(VisualToggle))]
    public class VisualToggle : MonoBehaviour
    {
        [GetComponent] [SerializeField] private Toggle _toggle;

        [Header("State")] [Tooltip("Current toggle state (false = start, true = end)")] [SerializeField]
        private bool _isActive;

        /// <summary>
        ///     When enabled, invokes events for the current state on Start.
        ///     Useful to initialize dependent systems from the initial state.
        /// </summary>
        [Tooltip("If enabled, invokes events for current state on Start")] [SerializeField]
        private bool _setOnAwake = true;

        [Header("Image Variants")] [Tooltip("Image array for switching sprites between states")] [SerializeField]
        private ImageVariant[] imageV = new ImageVariant[0];

        [Header("Image Colors")] [Tooltip("Image array for switching colors between states")] [SerializeField]
        private ImageColor[] imageC = new ImageColor[0];

        [Header("Text Variants")]
        [Tooltip("TextMeshPro array for switching colors and/or text between states")]
        [SerializeField]
        private TmpColorTextVariant[] textColor = new TmpColorTextVariant[0];

        [Header("GameObject Variants")] [Tooltip("GameObject groups for switching active state")] [SerializeField]
        private GameObjectVariant variants;

        [Tooltip("Invoked when switching to active state (end)")]
        public UnityEvent On;

        [Tooltip("Invoked when switching to inactive state (start)")]
        public UnityEvent Off;

        [Tooltip("Reactive state (true = active); subscribe via Value.OnChanged")]
        public ReactivePropertyBool Value = new();

        private bool _isUpdatingFromToggle;

        /// <summary>Current state (for NeoCondition and reflection).</summary>
        public bool ValueBool => Value.CurrentValue;

        /// <summary>
        ///     Current toggle state.
        /// </summary>
        /// <value>true when in end state, false when in start state</value>
        public bool IsActive
        {
            get => _isActive;
            private set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    UpdateVisuals();
                    InvokeEvents(value);
                }
                else
                {
                    UpdateVisuals();
                }
            }
        }

        private void Awake()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
                _isActive = _toggle.isOn;
            }

            UpdateVisuals();
        }

        private void Start()
        {
            if (_toggle != null)
            {
                SetActive(_toggle.isOn);
            }

            if (_setOnAwake)
            {
                InvokeEvents(_isActive);
            }
        }

        private void OnEnable()
        {
            if (_toggle != null)
            {
                _toggle.isOn = _isActive;
            }
        }

        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        private void OnValidate()
        {
            if (_toggle != null)
            {
                _isActive = _toggle.isOn;
            }

            if (!_isActive)
            {
                AutoSaveStartValues();
            }

            UpdateVisuals();
        }

        private void OnToggleValueChanged(bool value)
        {
            if (!_isUpdatingFromToggle)
            {
                _isUpdatingFromToggle = true;
                SetActive(value);
                _isUpdatingFromToggle = false;
            }
        }

        /// <summary>
        ///     Toggles the current state.
        /// </summary>
        [Button]
        public void Toggle()
        {
            SetActive(!_isActive);
        }

        /// <summary>
        ///     Sets active (end) state and updates visuals.
        /// </summary>
        public void SetActive()
        {
            SetActive(true);
        }

        /// <summary>
        ///     Sets inactive (start) state and updates visuals.
        /// </summary>
        public void SetInactive()
        {
            SetActive(false);
        }

        /// <summary>
        ///     Sets the toggle to the given state.
        /// </summary>
        /// <param name="isActive">true for active (end), false for inactive (start)</param>
        /// <param name="invokeToggleEvent">If true and a Toggle exists, fires its onValueChanged</param>
        [Button]
        public void SetActive(bool isActive, bool invokeToggleEvent = false)
        {
            if (_toggle != null && !_isUpdatingFromToggle)
            {
                if (invokeToggleEvent)
                {
                    _toggle.isOn = isActive;
                }
                else
                {
                    _toggle.SetIsOnWithoutNotify(isActive);
                }
            }

            IsActive = isActive;
        }

        /// <summary>
        ///     Refreshes all visuals to match the current state.
        /// </summary>
        public void UpdateVisuals()
        {
            UpdateImageVariants();
            UpdateImageColors();
            UpdateTextVariants();
            UpdateGameObjectVariants();
        }

        private void InvokeEvents(bool isActive)
        {
            if (isActive)
            {
                On?.Invoke();
            }
            else
            {
                Off?.Invoke();
            }

            Value.Value = isActive;
        }

        private void AutoSaveStartValues()
        {
            foreach (ImageVariant v in imageV)
            {
                if (v.image != null && v.start == null)
                {
                    v.start = v.image.sprite;
                }
            }

            foreach (TmpColorTextVariant t in textColor)
            {
                if (t.tmp != null)
                {
                    if (t.start == default)
                    {
                        t.start = t.tmp.color;
                    }

                    if (!t.useText && string.IsNullOrEmpty(t.startText))
                    {
                        t.startText = t.tmp.text;
                    }
                }
            }
        }

        private void UpdateImageVariants()
        {
            if (imageV == null || imageV.Length == 0)
            {
                return;
            }

            foreach (ImageVariant variant in imageV)
            {
                if (variant.image == null)
                {
                    continue;
                }

                variant.image.sprite = _isActive ? variant.end : variant.start;

                if (variant.setNativeSize)
                {
                    variant.image.SetNativeSize();
                }
            }
        }

        private void UpdateImageColors()
        {
            if (imageC == null || imageC.Length == 0)
            {
                return;
            }

            foreach (ImageColor colorVariant in imageC)
            {
                if (colorVariant.image == null)
                {
                    continue;
                }

                colorVariant.image.color = _isActive ? colorVariant.end : colorVariant.start;
            }
        }

        private void UpdateTextVariants()
        {
            if (textColor == null || textColor.Length == 0)
            {
                return;
            }

            foreach (TmpColorTextVariant textVariant in textColor)
            {
                if (textVariant.tmp == null)
                {
                    continue;
                }

                textVariant.tmp.color = _isActive ? textVariant.end : textVariant.start;

                if (textVariant.useText)
                {
                    textVariant.tmp.text = _isActive ? textVariant.endText : textVariant.startText;
                }
            }
        }

        private void UpdateGameObjectVariants()
        {
            if (variants == null)
            {
                return;
            }

            if (variants.starts != null)
            {
                foreach (GameObject obj in variants.starts)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!_isActive);
                    }
                }
            }

            if (variants.ends != null)
            {
                foreach (GameObject obj in variants.ends)
                {
                    if (obj != null)
                    {
                        obj.SetActive(_isActive);
                    }
                }
            }
        }

        /// <summary>
        ///     Settings for switching Image sprites between states.
        /// </summary>
        [Serializable]
        public class ImageVariant
        {
            [Tooltip("Image component for sprite switching")]
            public Image image;

            [Tooltip("Sprite for initial state (start)")]
            public Sprite start;

            [Tooltip("Sprite for end state (end)")]
            public Sprite end;

            [Tooltip("Automatically set Image size to sprite size")]
            public bool setNativeSize;
        }

        /// <summary>
        ///     Settings for switching Image colors between states.
        /// </summary>
        [Serializable]
        public class ImageColor
        {
            [Tooltip("Image component for color switching")]
            public Image image;

            [Tooltip("Color for initial state (start)")]
            public Color start = Color.white;

            [Tooltip("Color for end state (end)")] public Color end = Color.white;
        }

        /// <summary>
        ///     Settings for switching TextMeshPro colors and/or text between states.
        /// </summary>
        [Serializable]
        public class TmpColorTextVariant
        {
            [Tooltip("TextMeshPro component for switching")]
            public TextMeshProUGUI tmp;

            [Tooltip("Color for initial state (start)")]
            public Color start = Color.white;

            [Tooltip("Color for end state (end)")] public Color end = Color.white;

            [Tooltip("Whether to switch text along with color")]
            public bool useText;

            [Tooltip("Text for initial state (start). Used only if useText = true")]
            public string startText;

            [Tooltip("Text for end state (end). Used only if useText = true")]
            public string endText;
        }

        /// <summary>
        ///     Settings for toggling GameObject groups active between states.
        /// </summary>
        [Serializable]
        public class GameObjectVariant
        {
            [Tooltip("GameObjects active in initial state (start)")]
            public GameObject[] starts;

            [Tooltip("GameObjects active in end state (end)")]
            public GameObject[] ends;
        }
    }
}
