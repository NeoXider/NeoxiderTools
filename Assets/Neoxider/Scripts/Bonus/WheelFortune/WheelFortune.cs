using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Neo.Bonus
{
    public class WheelFortune : MonoBehaviour
    {
        public enum SpinState
        {
            Idle,
            Spinning,
            Decelerating,
            Aligning
        }

        [SerializeField] private bool _singleUse = true;
        [SerializeField] private bool _canUse = true;

        [Header("Stop Timing")] [SerializeField]
        private float _autoStopTime;

        [SerializeField] private float _extraSpinTime = 1f;

        [Space] [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Transforms")] [Range(-360, 360)] [SerializeField]
        private float _offsetZ;

        [Header("Transforms")] [Range(-360, 360)] [SerializeField]
        private float _wheelOffsetZ;


        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private RectTransform _arrow;

        [Space] [Header("Spin Settings")] [SerializeField]
        private bool _rotateLeft = true;

        [SerializeField] private float _initialAngularVelocity = 450f;
        [SerializeField] private float _angularDeceleration = 50f;

        [Header("Alignment")] [SerializeField] private bool _enableAlignment;
        [SerializeField] private float _alignmentDuration = 0.5f;

        [Space] [Header("Prize Items")] [SerializeField]
        private bool _autoArrangePrizes = true;

        [SerializeField] private bool _setPrizes;

        [FormerlySerializedAs("_prizes")] [SerializeField]
        private GameObject[] items;

        [SerializeField] private float _prizeDistance = 200f;

        [Space] [Header("Debug")] [SerializeField]
        private bool _debugLogId;

        [SerializeField] [Range(0, 360)] private float _wheelAngleInspector;

        public UnityEvent<int> OnWinIdVariant;

        private float _alignmentElapsed;
        private float _alignmentStartAngle;
        private float _alignmentTargetAngle;
        private float _currentAngularVelocity;

        public SpinState State { get; private set; } = SpinState.Idle;

        public GameObject[] Items => items;

        public bool canUse
        {
            get => _canUse;
            set
            {
                _canUse = value;
                if (_canvasGroup != null)
                    _canvasGroup.interactable = true;
            }
        }

        private void Update()
        {
            if (State == SpinState.Aligning)
                AlignWheel();
            else if (State != SpinState.Idle)
                RotateWheel();
        }

        private void OnEnable()
        {
            _wheelTransform.rotation = Quaternion.identity;
            State = SpinState.Idle;
            if (_canvasGroup != null)
                _canvasGroup.interactable = true;
        }

        private void OnValidate()
        {
            _wheelTransform.eulerAngles = new Vector3(0, 0, _wheelAngleInspector - _wheelOffsetZ);
            if (_debugLogId)
                Debug.Log("Wheel Id: " + GetResultId());
            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_setPrizes && _wheelTransform != null)
            {
                items = new GameObject[_wheelTransform.childCount];
                for (var i = 0; i < items.Length; i++) items[i] = _wheelTransform.GetChild(i).gameObject;

                _setPrizes = false;
            }

            if (_autoArrangePrizes && _wheelAngleInspector == 0)
                ArrangePrizes();
        }

        private void RotateWheel()
        {
            _wheelTransform.Rotate(Vector3.back * (_rotateLeft ? _currentAngularVelocity : -_currentAngularVelocity) *
                                   Time.deltaTime);
            if (State == SpinState.Decelerating)
            {
                _currentAngularVelocity -= Time.deltaTime * _angularDeceleration;
                if (_currentAngularVelocity <= 0f)
                {
                    if (_enableAlignment)
                        InitAlignment();
                    else
                        EndRotation();
                }
            }
        }

        private void AlignWheel()
        {
            _alignmentElapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_alignmentElapsed / _alignmentDuration);
            var angle = Mathf.LerpAngle(_alignmentStartAngle, _alignmentTargetAngle - _wheelOffsetZ, t);
            _wheelTransform.rotation = Quaternion.Euler(0, 0, angle);
            if (t >= 1f)
                EndRotation();
        }

        private void InitAlignment()
        {
            _alignmentStartAngle = _wheelTransform.rotation.eulerAngles.z;
            _alignmentTargetAngle = CalculateTargetAngle();
            _alignmentElapsed = 0f;
            State = SpinState.Aligning;
        }

        private float CalculateTargetAngle()
        {
            var resultId = GetResultId();
            var sectorAngle = 360f / items.Length;
            var arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            var currentWheelAngle = _wheelTransform.rotation.eulerAngles.z;
            var currentRelative = (currentWheelAngle - arrowAngle + 360f) % 360f;
            var targetRelative = resultId * sectorAngle;
            var diff = targetRelative - currentRelative;
            if (diff > 180f)
                diff -= 360f;
            else if (diff < -180f)
                diff += 360f;
            return currentWheelAngle + diff;
        }

        private void EndRotation()
        {
            State = SpinState.Idle;
            if (_canvasGroup != null)
                _canvasGroup.interactable = true;
            OnWinIdVariant?.Invoke(GetResultId());
        }

        private int GetResultId()
        {
            var sectorAngle = 360f / items.Length;
            var wheelAngle = _wheelTransform.rotation.eulerAngles.z - _offsetZ;
            var arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            var relativeAngle = (wheelAngle - arrowAngle + 360f) % 360f;
            var id = Mathf.FloorToInt((relativeAngle + sectorAngle / 2f) / sectorAngle);
            return (id + items.Length) % items.Length;
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
        public void Spin()
        {
            if (items.Length == 0)
                return;
            if (State == SpinState.Idle && (!_singleUse || (_singleUse && _canUse)))
            {
                _canUse = false;
                State = SpinState.Spinning;
                _currentAngularVelocity = _initialAngularVelocity;
            }

            if (_autoStopTime > 0)
                Invoke(nameof(Stop), Random.Range(_autoStopTime, _autoStopTime + _extraSpinTime));
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#else
        [Button]
#endif
        public void Stop()
        {
            if (State == SpinState.Idle)
                return;
            if (_canvasGroup != null)
                _canvasGroup.interactable = false;
            State = SpinState.Decelerating;
        }

        private void ArrangePrizes()
        {
            if (items == null || items.Length == 0)
                return;

            items.Select(t => t.transform)
                .ArrangeInCircle(transform.position,
                    _prizeDistance,
                    _offsetZ);
        }

        public GameObject GetPrize(int id)
        {
            return items[id];
        }
    }
}