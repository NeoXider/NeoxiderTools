using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo.UI
{
    [NeoDoc("UI/ButtonScale.md")]
    [CreateFromMenu("Neoxider/UI/ButtonScale")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(ButtonScale))]
    public class ButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")] [SerializeField]
        private RectTransform _rectTransform;

        [Header("Settings")] [SerializeField] private Vector2 _pressedSize = new(0.85f, 0.85f);
        [SerializeField] private float resizeDuration = 0.15f;

        [Tooltip("Animate with unscaled time so the press effect keeps working while Time.timeScale is 0 (pause menus).")]
        [SerializeField]
        private bool _useUnscaledTime = true;

        private Vector3 _currentSize;
        private Coroutine _resizeCoroutine;
        private bool _pressed;
        private bool _resizing;

        // WHY: _pressedSize is authored as Vector2; keep the base Z so a press never flattens
        // localScale.z to 0 (Vector2 -> Vector3 assignment would zero it).
        private Vector3 PressedScale => new(_pressedSize.x, _pressedSize.y, _currentSize.z);

        /// <summary>When true the press animation ignores <see cref="Time.timeScale" />.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        private void Awake()
        {
            ResolveRectTransform();
            if (_rectTransform == null)
            {
                enabled = false;
                return;
            }

            _currentSize = _rectTransform.localScale;
        }

        private void OnEnable()
        {
            ResolveRectTransform();
            if (_rectTransform == null)
            {
                return;
            }

            _rectTransform.localScale = _currentSize;
        }

        private void OnDisable()
        {
            if (_resizeCoroutine != null)
            {
                StopCoroutine(_resizeCoroutine);
                _resizeCoroutine = null;
            }

            _pressed = false;
            _resizing = false;
        }

        private void OnValidate()
        {
            ResolveRectTransform();
        }

        // WHY: explicit Unity null check instead of `??=`, which keeps a destroyed or editor fake-null
        // reference and never falls back to the own RectTransform.
        private void ResolveRectTransform()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Resize(true);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            Resize(false);
        }

        /// <summary>Drives the press effect from code or a UnityEvent (true = pressed scale).</summary>
        public void SetPressed(bool pressed)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Resize(pressed);
        }

        private void Resize(bool pressed)
        {
            if (_resizeCoroutine != null)
            {
                StopCoroutine(_resizeCoroutine);
                _resizeCoroutine = null;
            }

            if (_rectTransform == null)
            {
                return;
            }

            if (pressed && !_pressed && !_resizing)
            {
                // WHY: a press from rest takes the button's current scale as the release target; the Awake snapshot
                // went stale once layout or code rescaled the button, and every release snapped back to it.
                _currentSize = _rectTransform.localScale;
            }

            _pressed = pressed;
            _resizing = false;
            _resizeCoroutine = StartCoroutine(ResizeButton(pressed ? PressedScale : _currentSize));
        }

        private IEnumerator ResizeButton(Vector3 targetSize)
        {
            _resizing = true;
            Vector3 initialSize = _rectTransform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < resizeDuration)
            {
                _rectTransform.localScale = Vector3.Lerp(initialSize, targetSize, elapsedTime / resizeDuration);
                // WHY: the scaled delta is 0 while Time.timeScale is 0, which froze the button mid-press.
                elapsedTime += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            _rectTransform.localScale = targetSize;
            _resizing = false;
        }

        #region Sub-Classes

        [Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton>
        {
        }

        #endregion
    }
}
