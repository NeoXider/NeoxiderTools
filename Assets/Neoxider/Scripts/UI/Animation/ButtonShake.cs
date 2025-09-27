using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Neo.UI
{
    public class ButtonShake : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Shake Settings")] [SerializeField]
        private RectTransform _rectTransform;

        [SerializeField] private float _shakeDuration;
        [SerializeField] private float _shakeMagnitude = 3;

        [SerializeField] private bool _enableShake = true;
        [SerializeField] private bool _shakeOnStart;
        [SerializeField] private bool _shakeOnEnd;
        private Coroutine _shakeCoroutine;

        private Vector2 _startPositions;


        private void Awake()
        {
            _startPositions = _rectTransform.localPosition;
        }

        private void OnEnable()
        {
            _rectTransform.localPosition = _startPositions;

            if (_shakeOnStart) StartShaking();
        }

        private void OnDisable()
        {
        }

        private void OnValidate()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_shakeOnStart)
                StopShaking();
            else
                StartShaking();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_shakeOnEnd) StopShaking();
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
            var originalPosition = _rectTransform.localPosition;
            var elapsed = 0f;

            while (elapsed < _shakeDuration || _shakeDuration == 0)
            {
                var x = Random.Range(-1f, 1f) * _shakeMagnitude;
                var y = Random.Range(-1f, 1f) * _shakeMagnitude;

                _rectTransform.localPosition = originalPosition + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _rectTransform.localPosition = originalPosition;
            _shakeCoroutine = null;
        }

        #region Sub-Classes

        [Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton>
        {
        }

        #endregion
    }
}