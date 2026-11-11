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

        [Tooltip("Shake with unscaled time so a timed shake still ends while Time.timeScale is 0 (pause menus).")]
        [SerializeField]
        private bool _useUnscaledTime = true;

        private Coroutine _shakeCoroutine;

        // WHY: captured when a shake starts, not in Awake. Layout groups, animations and code move the button
        // after Awake; restoring an Awake snapshot teleported it back to a stale position.
        private Vector3 _shakeOrigin;

        /// <summary>When true a timed shake counts unscaled time.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>True while a shake is running.</summary>
        public bool IsShaking => _shakeCoroutine != null;

        private void Awake()
        {
            ResolveRectTransform();
            if (_rectTransform == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            ResolveRectTransform();
            if (_rectTransform == null)
            {
                return;
            }

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
            ResolveRectTransform();
        }

        private void ResolveRectTransform()
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
        [Button(PlayModeOnly = true)]
        public void Shake()
        {
            StartShaking();
        }

        /// <summary>Stops the shake and restores the original position.</summary>
        [Button(PlayModeOnly = true)]
        public void StopShake()
        {
            StopShaking();
        }

        private void StartShaking()
        {
            if (!_enableShake || !isActiveAndEnabled)
            {
                return;
            }

            if (_shakeCoroutine == null)
            {
                if (_rectTransform == null)
                {
                    return;
                }

                _shakeOrigin = _rectTransform.localPosition;
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
                if (_rectTransform != null)
                {
                    _rectTransform.localPosition = _shakeOrigin;
                }
            }
        }

        private IEnumerator ShakeButton()
        {
            float elapsed = 0f;

            while (elapsed < _shakeDuration || _shakeDuration == 0)
            {
                float x = Random.Range(-1f, 1f) * _shakeMagnitude;
                float y = Random.Range(-1f, 1f) * _shakeMagnitude;

                _rectTransform.localPosition = _shakeOrigin + new Vector3(x, y, 0);
                elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            _rectTransform.localPosition = _shakeOrigin;
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
