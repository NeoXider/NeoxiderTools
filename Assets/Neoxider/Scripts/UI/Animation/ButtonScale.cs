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

        private Vector2 _currentSize;
        private Coroutine _resizeCoroutine;

        private void Awake()
        {
            _rectTransform ??= GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                enabled = false;
                return;
            }

            _currentSize = _rectTransform.localScale;
        }

        private void OnEnable()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

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
        }

        private void OnValidate()
        {
            _rectTransform ??= GetComponent<RectTransform>();
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (_resizeCoroutine != null)
            {
                StopCoroutine(_resizeCoroutine);
            }

            if (_rectTransform == null)
            {
                return;
            }

            _resizeCoroutine = StartCoroutine(ResizeButton(_pressedSize));
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (_resizeCoroutine != null)
            {
                StopCoroutine(_resizeCoroutine);
            }

            if (_rectTransform == null)
            {
                return;
            }

            _resizeCoroutine = StartCoroutine(ResizeButton(_currentSize));
        }

        private IEnumerator ResizeButton(Vector2 targetSize)
        {
            Vector2 initialSize = _rectTransform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < resizeDuration)
            {
                _rectTransform.localScale = Vector2.Lerp(initialSize, targetSize, elapsedTime / resizeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _rectTransform.localScale = targetSize;
        }

        #region Sub-Classes

        [Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton>
        {
        }

        #endregion
    }
}
