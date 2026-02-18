using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Bonus
{
    [NeoDoc("Bonus/WheelFortune/WheelFortune.md")]
    [AddComponentMenu("Neo/" + "Bonus/" + nameof(WheelFortune))]
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
                {
                    _canvasGroup.interactable = true;
                }
            }
        }

        private void Update()
        {
            if (State == SpinState.Aligning)
            {
                AlignWheel();
            }
            else if (State != SpinState.Idle)
            {
                RotateWheel();
            }
        }

        private void OnEnable()
        {
            _wheelTransform.rotation = Quaternion.identity;
            State = SpinState.Idle;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_wheelTransform == null || items == null || items.Length == 0)
            {
                return;
            }

            Vector3 center = _wheelTransform.position;
            float wheelAngle = _wheelAngleInspector;
            float sectorAngle = 360f / items.Length;

            Rect rect = _wheelTransform.rect;
            float radius = Mathf.Max(rect.width, rect.height) * 0.5f;

            Canvas canvas = _wheelTransform.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            radius *= scale;

            for (int i = 0; i <= items.Length; i++)
            {
                float angle = (-i * sectorAngle + _offsetZ + wheelAngle + _wheelOffsetZ) * Mathf.Deg2Rad;
                Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Vector3 startPos = center;
                Vector3 endPos = center + direction * radius;

                if (i == 0)
                {
                    Handles.color = Color.magenta;
                }
                else if (i == items.Length)
                {
                    Handles.color = Color.red;
                }
                else
                {
                    Handles.color = Color.yellow;
                }

                Handles.DrawLine(startPos, endPos);
            }

            if (_arrow != null)
            {
                Vector3 arrowPos = _arrow.position;
                float arrowAngleZ = _arrow.transform.eulerAngles.z;
                float arrowAngleRad = arrowAngleZ * Mathf.Deg2Rad;
                Vector3 arrowDirection = new(Mathf.Sin(arrowAngleRad), Mathf.Cos(arrowAngleRad), 0);
                float arrowLength = radius * 0.8f;
                Vector3 arrowEnd = arrowPos + arrowDirection * arrowLength;

                Handles.color = Color.green;
                Handles.DrawLine(arrowPos, arrowEnd);

                float arrowSize = 15f;
                Vector3 perpDirection = new(-arrowDirection.y, arrowDirection.x, 0);
                Vector3 arrowTip1 = arrowEnd - arrowDirection * arrowSize + perpDirection * arrowSize * 0.5f;
                Vector3 arrowTip2 = arrowEnd - arrowDirection * arrowSize - perpDirection * arrowSize * 0.5f;

                Handles.DrawLine(arrowEnd, arrowTip1);
                Handles.DrawLine(arrowEnd, arrowTip2);
                Handles.DrawLine(arrowTip1, arrowTip2);
            }

            if (Mathf.Abs(_wheelOffsetZ) > 0.01f && _arrow != null)
            {
                float arrowAngle = _arrow.transform.eulerAngles.z;
                float virtualArrowAngle = (arrowAngle - _wheelOffsetZ + 360f) % 360f;
                float virtualAngleRad = virtualArrowAngle * Mathf.Deg2Rad;
                Vector3 virtualDirection = new(Mathf.Sin(virtualAngleRad), Mathf.Cos(virtualAngleRad), 0);

                Handles.color = new Color(0f, 1f, 1f, 0.8f);
                Vector3 virtualArrowEnd = center + virtualDirection * (radius * 0.7f);

                float dashLength = 8f;
                float dashGap = 4f;
                float totalLength = Vector3.Distance(center, virtualArrowEnd);
                Vector3 dir = (virtualArrowEnd - center).normalized;
                float currentDist = 0f;

                while (currentDist < totalLength)
                {
                    Vector3 dashStart = center + dir * currentDist;
                    Vector3 dashEnd = center + dir * Mathf.Min(currentDist + dashLength, totalLength);
                    Handles.DrawLine(dashStart, dashEnd);
                    currentDist += dashLength + dashGap;
                }

                float arrowSize = 12f;
                Vector3 perpDirection = new(-virtualDirection.y, virtualDirection.x, 0);
                Vector3 virtualTip1 = virtualArrowEnd - virtualDirection * arrowSize + perpDirection * arrowSize * 0.4f;
                Vector3 virtualTip2 = virtualArrowEnd - virtualDirection * arrowSize - perpDirection * arrowSize * 0.4f;

                Handles.DrawLine(virtualArrowEnd, virtualTip1);
                Handles.DrawLine(virtualArrowEnd, virtualTip2);
                Handles.DrawLine(virtualTip1, virtualTip2);

                Handles.color = new Color(0f, 1f, 1f, 0.2f);
                float offsetArcRadius = radius * 0.5f;
                float realArrowAngleRad = arrowAngle * Mathf.Deg2Rad;
                Vector3 realDirection = new(Mathf.Sin(realArrowAngleRad), Mathf.Cos(realArrowAngleRad), 0);

                int arcSteps = Mathf.Max(8, Mathf.RoundToInt(Mathf.Abs(_wheelOffsetZ) / 5f));
                for (int step = 0; step < arcSteps; step++)
                {
                    float t = (float)step / arcSteps;
                    float t2 = (float)(step + 1) / arcSteps;
                    float angle1 = Mathf.Lerp(realArrowAngleRad, virtualAngleRad, t);
                    float angle2 = Mathf.Lerp(realArrowAngleRad, virtualAngleRad, t2);

                    Vector3 pos1 = center + new Vector3(Mathf.Sin(angle1), Mathf.Cos(angle1), 0) * offsetArcRadius;
                    Vector3 pos2 = center + new Vector3(Mathf.Sin(angle2), Mathf.Cos(angle2), 0) * offsetArcRadius;

                    Handles.DrawLine(pos1, pos2);
                }

                Vector3 labelPos = center + virtualDirection * (radius * 0.85f);
                GUIStyle style = new();
                style.normal.textColor = new Color(0f, 1f, 1f, 1f);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                Handles.Label(labelPos, $"Δ{_wheelOffsetZ:0}", style);
            }

            int currentResultId = GetResultId();
            float currentSectorStartAngle = (-currentResultId * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;
            float currentSectorEndAngle =
                (-(currentResultId + 1) * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;

            Handles.color = new Color(1f, 0.5f, 0f, 0.3f);

            float arcRadius = radius * 0.9f;
            Vector3 startDir = new(Mathf.Cos(currentSectorStartAngle), Mathf.Sin(currentSectorStartAngle), 0);
            Vector3 endDir = new(Mathf.Cos(currentSectorEndAngle), Mathf.Sin(currentSectorEndAngle), 0);

            Handles.DrawLine(center, center + startDir * arcRadius);
            Handles.DrawLine(center, center + endDir * arcRadius);

            int steps = 20;
            for (int step = 0; step < steps; step++)
            {
                float t1 = (float)step / steps;
                float t2 = (float)(step + 1) / steps;
                float angle1 = Mathf.Lerp(currentSectorStartAngle, currentSectorEndAngle, t1);
                float angle2 = Mathf.Lerp(currentSectorStartAngle, currentSectorEndAngle, t2);

                Vector3 pos1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * arcRadius;
                Vector3 pos2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * arcRadius;

                Handles.DrawLine(pos1, pos2);
            }

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }
#endif

        private void OnValidate()
        {
            _wheelTransform.eulerAngles = new Vector3(0, 0, _wheelAngleInspector);
            if (_debugLogId)
            {
                Debug.Log("Wheel Id: " + GetResultId());
            }

            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_setPrizes && _wheelTransform != null)
            {
                items = new GameObject[_wheelTransform.childCount];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = _wheelTransform.GetChild(i).gameObject;
                }

                _setPrizes = false;
            }

            if (_autoArrangePrizes && _wheelAngleInspector == 0)
            {
                ArrangePrizes();
            }
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
                    {
                        InitAlignment();
                    }
                    else
                    {
                        EndRotation();
                    }
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
            {
                EndRotation();
            }
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
            int resultId = GetResultId();
            float sectorAngle = 360f / items.Length;
            float arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            float currentWheelAngle = _wheelTransform.rotation.eulerAngles.z;
            float targetRelative = resultId * sectorAngle - _wheelOffsetZ;
            float currentRelative = (currentWheelAngle - arrowAngle + 360f) % 360f;
            float diff = targetRelative - currentRelative;
            if (diff > 180f)
            {
                diff -= 360f;
            }
            else if (diff < -180f)
            {
                diff += 360f;
            }

            return currentWheelAngle + diff;
        }

        private void EndRotation()
        {
            State = SpinState.Idle;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
            }

            OnWinIdVariant?.Invoke(GetResultId());
        }

        private int GetResultId()
        {
            float sectorAngle = 360f / items.Length;
            float wheelAngle = _wheelTransform.rotation.eulerAngles.z;
            float arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            float relativeAngle = (wheelAngle + _wheelOffsetZ - arrowAngle + 360f) % 360f;
            int id = Mathf.FloorToInt((relativeAngle + sectorAngle / 2f) / sectorAngle);
            return (id + items.Length) % items.Length;
        }

        [Button]
        public void Spin()
        {
            if (items.Length == 0)
            {
                return;
            }

            if (State == SpinState.Idle && (!_singleUse || (_singleUse && _canUse)))
            {
                _canUse = false;
                State = SpinState.Spinning;
                _currentAngularVelocity = _initialAngularVelocity;
            }

            if (_autoStopTime > 0)
            {
                Invoke(nameof(Stop), Random.Range(_autoStopTime, _autoStopTime + _extraSpinTime));
            }
        }

        [Button]
        public void Stop()
        {
            if (State == SpinState.Idle)
            {
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
            }

            State = SpinState.Decelerating;
        }

        private void ArrangePrizes()
        {
            if (items == null || items.Length == 0)
            {
                return;
            }

            float angleStep = 360f / items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                float angle = -i * angleStep + _offsetZ;
                Transform itemTransform = items[i].transform;
                float positionAngle = (angle + 90f) * Mathf.Deg2Rad;
                itemTransform.localPosition =
                    new Vector3(Mathf.Cos(positionAngle), Mathf.Sin(positionAngle), 0) * _prizeDistance;
                itemTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        public GameObject GetPrize(int id)
        {
            return items[id];
        }
    }
}