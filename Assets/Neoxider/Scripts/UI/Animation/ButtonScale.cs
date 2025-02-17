using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo.UI
{
    public class ButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Sub-Classes
        [System.Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton> { }
        #endregion

        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Vector2 _pressedSize = new(0.85f, 0.85f);
        [SerializeField] private float resizeDuration = 0.15f;

        private Vector2 _currentSize;
        private Coroutine _resizeCoroutine;

        private void Awake()
        {
            _currentSize = _rectTransform.localScale;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (_resizeCoroutine != null)
                StopCoroutine(_resizeCoroutine);

            _resizeCoroutine = StartCoroutine(ResizeButton(_pressedSize));
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (_resizeCoroutine != null)
                StopCoroutine(_resizeCoroutine);

            _resizeCoroutine = StartCoroutine(ResizeButton(_currentSize));
        }

        private void OnEnable()
        {
            _rectTransform.localScale = _currentSize;
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

        private void OnValidate()
        {
            _rectTransform ??= GetComponent<RectTransform>();
        }
    }
}