using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    public class WheelFortune : MonoBehaviour
    {
        [SerializeField] private bool _singleUse = true;
        [SerializeField] private bool _canUse = true;

        [Header("Stop Timing")]
        [SerializeField] private float _autoStopTime = 0;
        [SerializeField] private float _extraSpinTime = 1f;

        [Space]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Transforms")]
        [Range(-360, 360)]
        [SerializeField] private float _offsetZ = 0;
        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private RectTransform _arrow;

        [Space]
        [Header("Spin Settings")]
        [SerializeField] private bool _rotateLeft = true;
        [SerializeField] private float _initialAngularVelocity = 450f;
        [SerializeField] private float _angularDeceleration = 50f;

        [Header("Alignment")]
        [SerializeField] private bool _enableAlignment = false;
        [SerializeField] private float _alignmentDuration = 0.5f;

        public enum SpinState { Idle, Spinning, Decelerating, Aligning }
        private SpinState _spinState = SpinState.Idle;
        public SpinState State => _spinState;

        [Space]
        [Header("Prize Items")]
        [SerializeField] private bool _autoArrangePrizes = true;
        [SerializeField] private bool _setPrizes;
        [SerializeField] private GameObject[] _prizes;
        [SerializeField] private float _prizeDistance = 200f;

        [Space]
        [Header("Debug")]
        [SerializeField] private bool _debugLogId = false;
        [SerializeField, Range(0, 360)] private float _wheelAngleInspector = 0f;

        private float _alignmentElapsed;
        private float _alignmentStartAngle;
        private float _alignmentTargetAngle;
        private float _currentAngularVelocity;

        public UnityEvent<int> OnWinIdVariant;
        public GameObject[] Prizes => _prizes;

        public bool canUse
        {
            get => _canUse; set
            {
                _canUse = value;
                if (_canvasGroup != null)
                    _canvasGroup.interactable = true;
            }
        }

        private void OnEnable()
        {
            _wheelTransform.rotation = Quaternion.identity;
            _spinState = SpinState.Idle;
            if (_canvasGroup != null)
                _canvasGroup.interactable = true;
        }

        private void Update()
        {
            if (_spinState == SpinState.Aligning)
                AlignWheel();
            else if (_spinState != SpinState.Idle)
                RotateWheel();
        }

        private void RotateWheel()
        {
            _wheelTransform.Rotate(Vector3.back * ((_rotateLeft) ? _currentAngularVelocity : -_currentAngularVelocity) * Time.deltaTime);
            if (_spinState == SpinState.Decelerating)
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
            float t = Mathf.Clamp01(_alignmentElapsed / _alignmentDuration);
            float angle = Mathf.LerpAngle(_alignmentStartAngle, _alignmentTargetAngle, t);
            _wheelTransform.rotation = Quaternion.Euler(0, 0, angle);
            if (t >= 1f)
                EndRotation();
        }

        private void InitAlignment()
        {
            _alignmentStartAngle = _wheelTransform.rotation.eulerAngles.z;
            _alignmentTargetAngle = CalculateTargetAngle();
            _alignmentElapsed = 0f;
            _spinState = SpinState.Aligning;
        }

        private float CalculateTargetAngle()
        {
            int resultId = GetResultId();
            float sectorAngle = 360f / _prizes.Length;
            float arrowAngle = (_arrow != null) ? _arrow.transform.eulerAngles.z : 0f;
            float currentWheelAngle = _wheelTransform.rotation.eulerAngles.z;
            float currentRelative = (currentWheelAngle - arrowAngle + 360f) % 360f;
            float targetRelative = resultId * sectorAngle;
            float diff = targetRelative - currentRelative;
            if (diff > 180f)
                diff -= 360f;
            else if (diff < -180f)
                diff += 360f;
            return currentWheelAngle + diff;
        }

        private void EndRotation()
        {
            _spinState = SpinState.Idle;
            if (_canvasGroup != null)
                _canvasGroup.interactable = true;
            OnWinIdVariant?.Invoke(GetResultId());
        }

        private int GetResultId()
        {
            float sectorAngle = 360f / _prizes.Length;
            float wheelAngle = _wheelTransform.rotation.eulerAngles.z - _offsetZ;
            float arrowAngle = (_arrow != null) ? _arrow.transform.eulerAngles.z : 0f;
            float relativeAngle = (wheelAngle - arrowAngle + 360f) % 360f;
            int id = Mathf.FloorToInt((relativeAngle + sectorAngle / 2f) / sectorAngle);
            return (id + _prizes.Length) % _prizes.Length;
        }

        public void Spin()
        {
            if (_prizes.Length == 0)
                return;
            if (_spinState == SpinState.Idle && (!_singleUse || (_singleUse && _canUse)))
            {
                _canUse = false;
                _spinState = SpinState.Spinning;
                _currentAngularVelocity = _initialAngularVelocity;
            }
            if (_autoStopTime > 0)
                Invoke(nameof(Stop), Random.Range(_autoStopTime, _autoStopTime + _extraSpinTime));
        }

        public void Stop()
        {
            if (_spinState == SpinState.Idle)
                return;
            if (_canvasGroup != null)
                _canvasGroup.interactable = false;
            _spinState = SpinState.Decelerating;
        }

        private void ArrangePrizes()
        {
            if (_prizes == null || _prizes.Length == 0)
                return;
            int count = _prizes.Length;
            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = (count - i) * angleStep + 90f + _offsetZ;
                float rad = angle * Mathf.Deg2Rad;
                _prizes[i].transform.localPosition = new Vector2(Mathf.Cos(rad) * _prizeDistance, Mathf.Sin(rad) * _prizeDistance);
                _prizes[i].transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        public GameObject GetPrize(int id)
        {
            return _prizes[id];
        }

        private void OnValidate()
        {
            _wheelTransform.eulerAngles = new Vector3(0, 0, _wheelAngleInspector);
            if (_debugLogId)
                Debug.Log("Wheel Id: " + GetResultId());
            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_setPrizes && _wheelTransform != null)
            {
                _prizes = new GameObject[_wheelTransform.childCount];
                for (int i = 0; i < _prizes.Length; i++)
                {
                    _prizes[i] = _wheelTransform.GetChild(i).gameObject;
                }

                _setPrizes = false;
            }

            if (_autoArrangePrizes && _wheelAngleInspector == 0)
                ArrangePrizes();
        }
    }
}