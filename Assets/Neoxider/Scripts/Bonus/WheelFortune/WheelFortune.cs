using System.Linq;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            _wheelTransform.eulerAngles = new Vector3(0, 0, _wheelAngleInspector);
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
            var angle = Mathf.LerpAngle(_alignmentStartAngle, _alignmentTargetAngle, t);
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
            var targetRelative = resultId * sectorAngle - _wheelOffsetZ;
            var currentRelative = (currentWheelAngle - arrowAngle + 360f) % 360f;
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
            var wheelAngle = _wheelTransform.rotation.eulerAngles.z;
            var arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            var relativeAngle = (wheelAngle + _wheelOffsetZ - arrowAngle + 360f) % 360f;
            var id = Mathf.FloorToInt((relativeAngle + sectorAngle / 2f) / sectorAngle);
            return (id + items.Length) % items.Length;
        }

[Button]
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

[Button]
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

            var angleStep = 360f / items.Length;
            for (var i = 0; i < items.Length; i++)
            {
                var angle = -i * angleStep + _offsetZ;
                var itemTransform = items[i].transform;
                var positionAngle = (angle + 90f) * Mathf.Deg2Rad;
                itemTransform.localPosition = new Vector3(Mathf.Cos(positionAngle), Mathf.Sin(positionAngle), 0) * _prizeDistance;
                itemTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        public GameObject GetPrize(int id)
        {
            return items[id];
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_wheelTransform == null || items == null || items.Length == 0)
                return;

            var center = _wheelTransform.position;
            var wheelAngle = _wheelAngleInspector;
            var sectorAngle = 360f / items.Length;
            
            var rect = _wheelTransform.rect;
            var radius = Mathf.Max(rect.width, rect.height) * 0.5f;
            
            Canvas canvas = _wheelTransform.GetComponentInParent<Canvas>();
            var scale = canvas != null ? canvas.scaleFactor : 1f;
            radius *= scale;

            for (var i = 0; i <= items.Length; i++)
            {
                var angle = (-i * sectorAngle + _offsetZ + wheelAngle + _wheelOffsetZ) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var startPos = center;
                var endPos = center + direction * radius;
                
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
                var arrowPos = _arrow.position;
                var arrowAngleZ = _arrow.transform.eulerAngles.z;
                var arrowAngleRad = arrowAngleZ * Mathf.Deg2Rad;
                var arrowDirection = new Vector3(Mathf.Sin(arrowAngleRad), Mathf.Cos(arrowAngleRad), 0);
                var arrowLength = radius * 0.8f;
                var arrowEnd = arrowPos + arrowDirection * arrowLength;
                
                Handles.color = Color.green;
                Handles.DrawLine(arrowPos, arrowEnd);
                
                var arrowSize = 15f;
                var perpDirection = new Vector3(-arrowDirection.y, arrowDirection.x, 0);
                var arrowTip1 = arrowEnd - arrowDirection * arrowSize + perpDirection * arrowSize * 0.5f;
                var arrowTip2 = arrowEnd - arrowDirection * arrowSize - perpDirection * arrowSize * 0.5f;
                
                Handles.DrawLine(arrowEnd, arrowTip1);
                Handles.DrawLine(arrowEnd, arrowTip2);
                Handles.DrawLine(arrowTip1, arrowTip2);
            }

            if (Mathf.Abs(_wheelOffsetZ) > 0.01f && _arrow != null)
            {
                var arrowAngle = _arrow.transform.eulerAngles.z;
                var virtualArrowAngle = (arrowAngle - _wheelOffsetZ + 360f) % 360f;
                var virtualAngleRad = virtualArrowAngle * Mathf.Deg2Rad;
                var virtualDirection = new Vector3(Mathf.Sin(virtualAngleRad), Mathf.Cos(virtualAngleRad), 0);
                
                Handles.color = new Color(0f, 1f, 1f, 0.8f);
                var virtualArrowEnd = center + virtualDirection * (radius * 0.7f);
                
                var dashLength = 8f;
                var dashGap = 4f;
                var totalLength = Vector3.Distance(center, virtualArrowEnd);
                var dir = (virtualArrowEnd - center).normalized;
                var currentDist = 0f;
                
                while (currentDist < totalLength)
                {
                    var dashStart = center + dir * currentDist;
                    var dashEnd = center + dir * Mathf.Min(currentDist + dashLength, totalLength);
                    Handles.DrawLine(dashStart, dashEnd);
                    currentDist += dashLength + dashGap;
                }
                
                var arrowSize = 12f;
                var perpDirection = new Vector3(-virtualDirection.y, virtualDirection.x, 0);
                var virtualTip1 = virtualArrowEnd - virtualDirection * arrowSize + perpDirection * arrowSize * 0.4f;
                var virtualTip2 = virtualArrowEnd - virtualDirection * arrowSize - perpDirection * arrowSize * 0.4f;
                
                Handles.DrawLine(virtualArrowEnd, virtualTip1);
                Handles.DrawLine(virtualArrowEnd, virtualTip2);
                Handles.DrawLine(virtualTip1, virtualTip2);
                
                Handles.color = new Color(0f, 1f, 1f, 0.2f);
                var offsetArcRadius = radius * 0.5f;
                var realArrowAngleRad = arrowAngle * Mathf.Deg2Rad;
                var realDirection = new Vector3(Mathf.Sin(realArrowAngleRad), Mathf.Cos(realArrowAngleRad), 0);
                
                var arcSteps = Mathf.Max(8, Mathf.RoundToInt(Mathf.Abs(_wheelOffsetZ) / 5f));
                for (var step = 0; step < arcSteps; step++)
                {
                    var t = (float)step / arcSteps;
                    var t2 = (float)(step + 1) / arcSteps;
                    var angle1 = Mathf.Lerp(realArrowAngleRad, virtualAngleRad, t);
                    var angle2 = Mathf.Lerp(realArrowAngleRad, virtualAngleRad, t2);
                    
                    var pos1 = center + new Vector3(Mathf.Sin(angle1), Mathf.Cos(angle1), 0) * offsetArcRadius;
                    var pos2 = center + new Vector3(Mathf.Sin(angle2), Mathf.Cos(angle2), 0) * offsetArcRadius;
                    
                    Handles.DrawLine(pos1, pos2);
                }
                
                var labelPos = center + virtualDirection * (radius * 0.85f);
                var style = new GUIStyle();
                style.normal.textColor = new Color(0f, 1f, 1f, 1f);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                Handles.Label(labelPos, $"Δ{_wheelOffsetZ:0}", style);
            }

            var currentResultId = GetResultId();
            var currentSectorStartAngle = (-currentResultId * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;
            var currentSectorEndAngle = (-(currentResultId + 1) * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;
            
            Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
            
            var arcRadius = radius * 0.9f;
            var startDir = new Vector3(Mathf.Cos(currentSectorStartAngle), Mathf.Sin(currentSectorStartAngle), 0);
            var endDir = new Vector3(Mathf.Cos(currentSectorEndAngle), Mathf.Sin(currentSectorEndAngle), 0);
            
            Handles.DrawLine(center, center + startDir * arcRadius);
            Handles.DrawLine(center, center + endDir * arcRadius);
            
            var steps = 20;
            for (var step = 0; step < steps; step++)
            {
                var t1 = (float)step / steps;
                var t2 = (float)(step + 1) / steps;
                var angle1 = Mathf.Lerp(currentSectorStartAngle, currentSectorEndAngle, t1);
                var angle2 = Mathf.Lerp(currentSectorStartAngle, currentSectorEndAngle, t2);
                
                var pos1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * arcRadius;
                var pos2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * arcRadius;
                
                Handles.DrawLine(pos1, pos2);
            }

            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
        }
#endif
    }
}