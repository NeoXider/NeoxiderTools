using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Neo.UI
{
    [NeoDoc("UI/ButtonShake.md")]
    [CreateFromMenu("Neoxider/UI/ButtonShake")]
    [AddComponentMenu("Neoxider/" + "UI/" + nameof(ButtonShake))]
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
            _rectTransform ??= GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                enabled = false;
                return;
            }

            _startPositions = _rectTransform.localPosition;
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

            _rectTransform.localPosition = _startPositions;

            if (_shakeOnStart)
            {
                StartShaking();
            }
        }

        private void OnDisable()
        {
            StopShaking();
        }

        private void OnValidate()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
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

        /// <summary>Starts the shake from code or a UnityEvent (respects Enable Shake).</summary>
        public void Shake()
        {
            StartShaking();
        }

        /// <summary>Stops the shake and restores the original position.</summary>
        public void StopShake()
        {
            StopShaking();
        }

        private void StartShaking()
        {
            if (!_enableShake)
            {
                return;
            }

            if (_shakeCoroutine == null)
            {
                if (_rectTransform == null)
                {
                    return;
                }

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

        #region Sub-Classes

        [Serializable]
        public class UIButtonEvent : UnityEvent<PointerEventData.InputButton>
        {
        }

        #endregion
    }
}
