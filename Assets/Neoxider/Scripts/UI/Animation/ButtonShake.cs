using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Neo.UI
{
    public class ButtonShake : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        #region Sub-Classes
        [System.Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton> { }
        #endregion

        [Header("Shake Settings")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private float _shakeDuration = 0;
        [SerializeField] private float _shakeMagnitude = 3;

        [SerializeField] private bool _enableShake = true;
        [SerializeField] private bool _shakeOnStart = false;
        [SerializeField] private bool _shakeOnEnd;

        private Vector2 _startPositions;
        private Coroutine _shakeCoroutine;


        private void Awake()
        {
            _startPositions = _rectTransform.localPosition;
        }

        private void OnEnable()
        {
            _rectTransform.localPosition = _startPositions;

            if (_shakeOnStart)
            {
                StartShaking();
            }
        }

        private void OnDisable()
        {

        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_shakeOnStart)
            {
                StopShaking();
            }
            else
            {
                StartShaking();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_shakeOnEnd)
            {
                StopShaking();
            }
        }

        private void StartShaking()
        {
            if (_shakeCoroutine == null)
            {
                _shakeCoroutine = StartCoroutine(ShakeButton());
            }
            else
            {
                StopShaking();
                StartShaking();
            }
        }

        private void StopShaking()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
                _rectTransform.localPosition = _startPositions;
            }
        }

        private IEnumerator ShakeButton()
        {
            Vector3 originalPosition = _rectTransform.localPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration || _shakeDuration == 0)
            {
                float x = Random.Range(-1f, 1f) * _shakeMagnitude;
                float y = Random.Range(-1f, 1f) * _shakeMagnitude;

                _rectTransform.localPosition = originalPosition + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rectTransform.localPosition = originalPosition;
            _shakeCoroutine = null;
        }

        private void OnValidate()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }
    }
}
