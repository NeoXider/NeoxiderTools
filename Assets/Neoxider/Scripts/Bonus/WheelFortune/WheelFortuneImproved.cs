using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Neo.Tools;

namespace Neo.Bonus
{
    [NeoDoc("Bonus/WheelFortune/WheelFortuneImproved.md")]
    [CreateFromMenu("Neoxider/Bonus/WheelFortuneImproved")]
    [AddComponentMenu("Neoxider/Bonus/" + nameof(WheelFortuneImproved))]
    public class WheelFortuneImproved : MonoBehaviour
    {
        public enum SpinState
        {
            Idle,
            Spinning,
            Decelerating,
            Aligning
        }

        [Header("Usage")]
        [Tooltip("If true, wheel can be spun only once until canUse is set again.")]
        [SerializeField] private bool _singleUse = true;

        [Tooltip("Whether the wheel can be spun. Also drives CanvasGroup.interactable when assigned.")]
        [SerializeField] private bool _canUse = true;

        [Header("Stop Timing")]
        [Tooltip("Auto-call Stop after this time (seconds). 0 = manual only.")]
        [SerializeField] private float _autoStopTime;

        [Tooltip("Extra random time added to auto-stop interval.")]
        [SerializeField] private float _extraSpinTime = 1f;

        [Space]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Transforms")]
        [Tooltip("Wheel rotation offset (degrees).")]
        [Range(-360, 360)] [SerializeField] private float _offsetZ;

        [Tooltip("Arrow visual offset (degrees).")]
        [Range(-360, 360)] [SerializeField] private float _wheelOffsetZ;

        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private RectTransform _arrow;

        [Space]
        [Header("Spin Settings")]
        [Tooltip("Rotate wheel counter-clockwise when true.")]
        [SerializeField] private bool _rotateLeft = true;

        [Tooltip("Initial angular speed (degrees per second).")]
        [SerializeField] private float _initialAngularVelocity = 450f;

        [Tooltip("Deceleration (degrees per second squared).")]
        [SerializeField] private float _angularDeceleration = 50f;

        [Header("Alignment")]
        [Tooltip("Smoothly align to winning sector when deceleration ends. Required for weighted/forced results.")]
        [SerializeField] private bool _enableAlignment = true;

        [Tooltip("Duration of alignment tween (seconds).")]
        [SerializeField] private float _alignmentDuration = 0.5f;

        [Space]
        [Header("Prize Items")]
        [Tooltip("Auto-arrange child transforms in a circle in OnValidate when angle is 0.")]
        [SerializeField] private bool _autoArrangePrizes = true;

        [SerializeField] private bool _setPrizes;

        [FormerlySerializedAs("_prizes")]
        [SerializeField] private GameObject[] items;

        [Tooltip("Radius for auto-arranged prizes (local units).")]
        [SerializeField] private float _prizeDistance = 200f;

        [Header("Chances (optional)")]
        [Tooltip("One weight per sector; index matches Items order. Empty or zero sum = equal chances.")]
        [SerializeField] private float[] _sectorWeights;

        [Tooltip("Optional ChanceData asset; sector count should match Items length.")]
        [SerializeField] private ChanceData _chanceData;

        [Tooltip("Optional ChanceSystemBehaviour; use when weights are set on another component.")]
        [SerializeField] private ChanceSystemBehaviour _chanceSystem;

        [Space]
        [Header("Debug")]
        [SerializeField] private bool _debugLogId;

        [SerializeField] [Range(0, 360)] private float _wheelAngleInspector;

        public UnityEvent<int> OnWinIdVariant;
        public UnityEvent OnSpinStarted;
        public UnityEvent OnDecelerationStarted;
        public UnityEvent OnAlignmentStarted;
        [Tooltip("Invoked when the wheel has fully stopped (after alignment or deceleration). Use for closing UI or enabling buttons.")]
        public UnityEvent OnStopped;
        [Tooltip("Invoked when Spin/SpinToResult was called but could not start (e.g. already spinning or single-use exhausted).")]
        public UnityEvent OnSpinBlocked;

        private float _alignmentElapsed;
        private float _alignmentStartAngle;
        private float _alignmentTargetAngle;
        private float _currentAngularVelocity;
        private int _pendingResultId = -1;

        public SpinState State { get; private set; } = SpinState.Idle;

        public GameObject[] Items => items;

        public bool canUse
        {
            get => _canUse;
            set
            {
                _canUse = value;
                if (_canvasGroup != null)
                    _canvasGroup.interactable = value;
            }
        }

        private bool IsSetupValid()
        {
            return _wheelTransform != null && items != null && items.Length > 0;
        }

        private void Update()
        {
            if (!IsSetupValid())
                return;

            if (State == SpinState.Aligning)
                AlignWheel();
            else if (State != SpinState.Idle)
                RotateWheel();
        }

        private void OnEnable()
        {
            if (_wheelTransform != null)
                _wheelTransform.rotation = Quaternion.identity;
            State = SpinState.Idle;
            _pendingResultId = -1;
            if (_canvasGroup != null)
                _canvasGroup.interactable = _canUse;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_wheelTransform == null || items == null || items.Length == 0)
                return;

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
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                Vector3 startPos = center;
                Vector3 endPos = center + direction * radius;

                if (i == 0)
                    Handles.color = Color.magenta;
                else if (i == items.Length)
                    Handles.color = Color.red;
                else
                    Handles.color = Color.yellow;

                Handles.DrawLine(startPos, endPos);
            }

            if (_arrow != null)
            {
                Vector3 arrowPos = _arrow.position;
                float arrowAngleZ = _arrow.transform.eulerAngles.z;
                float arrowAngleRad = arrowAngleZ * Mathf.Deg2Rad;
                Vector3 arrowDirection = new Vector3(Mathf.Sin(arrowAngleRad), Mathf.Cos(arrowAngleRad), 0);
                float arrowLength = radius * 0.8f;
                Vector3 arrowEnd = arrowPos + arrowDirection * arrowLength;

                Handles.color = Color.green;
                Handles.DrawLine(arrowPos, arrowEnd);

                float arrowSize = 15f;
                Vector3 perpDirection = new Vector3(-arrowDirection.y, arrowDirection.x, 0);
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
                Vector3 virtualDirection = new Vector3(Mathf.Sin(virtualAngleRad), Mathf.Cos(virtualAngleRad), 0);

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
                Vector3 perpDirection = new Vector3(-virtualDirection.y, virtualDirection.x, 0);
                Vector3 virtualTip1 = virtualArrowEnd - virtualDirection * arrowSize + perpDirection * arrowSize * 0.4f;
                Vector3 virtualTip2 = virtualArrowEnd - virtualDirection * arrowSize - perpDirection * arrowSize * 0.4f;

                Handles.DrawLine(virtualArrowEnd, virtualTip1);
                Handles.DrawLine(virtualArrowEnd, virtualTip2);
                Handles.DrawLine(virtualTip1, virtualTip2);

                Handles.color = new Color(0f, 1f, 1f, 0.2f);
                float offsetArcRadius = radius * 0.5f;
                float realArrowAngleRad = arrowAngle * Mathf.Deg2Rad;

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
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(0f, 1f, 1f, 1f);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                Handles.Label(labelPos, $"Δ{_wheelOffsetZ:0}", style);
            }

            int currentResultId = GetResultIdFromAngle();
            float currentSectorStartAngle = (-currentResultId * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;
            float currentSectorEndAngle = (-(currentResultId + 1) * sectorAngle + _offsetZ + wheelAngle) * Mathf.Deg2Rad;

            Handles.color = new Color(1f, 0.5f, 0f, 0.3f);

            float arcRadius = radius * 0.9f;
            Vector3 startDir = new Vector3(Mathf.Cos(currentSectorStartAngle), Mathf.Sin(currentSectorStartAngle), 0);
            Vector3 endDir = new Vector3(Mathf.Cos(currentSectorEndAngle), Mathf.Sin(currentSectorEndAngle), 0);

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
            if (_wheelTransform != null)
                _wheelTransform.eulerAngles = new Vector3(0, 0, _wheelAngleInspector);

            if (_debugLogId && IsSetupValid())
                Debug.Log("Wheel Id: " + GetResultIdFromAngle(), this);

            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_setPrizes && _wheelTransform != null)
            {
                items = new GameObject[_wheelTransform.childCount];
                for (int i = 0; i < items.Length; i++)
                    items[i] = _wheelTransform.GetChild(i).gameObject;

                _setPrizes = false;
            }

            if (_sectorWeights != null && items != null && _sectorWeights.Length != items.Length)
            {
                var newWeights = new float[items.Length];
                for (int i = 0; i < newWeights.Length; i++)
                    newWeights[i] = i < _sectorWeights.Length ? Mathf.Max(0f, _sectorWeights[i]) : 1f;
                _sectorWeights = newWeights;
            }

            if (_autoArrangePrizes && _wheelAngleInspector == 0)
                ArrangePrizes();
        }

        private void RotateWheel()
        {
            if (_wheelTransform == null)
                return;

            _wheelTransform.Rotate(Vector3.back * (_rotateLeft ? _currentAngularVelocity : -_currentAngularVelocity) * Time.deltaTime);

            if (State == SpinState.Decelerating)
            {
                _currentAngularVelocity -= Time.deltaTime * _angularDeceleration;
                if (_currentAngularVelocity <= 0f)
                {
                    if (_enableAlignment && _pendingResultId >= 0)
                        InitAlignment();
                    if (State != SpinState.Aligning)
                        EndRotation();
                }
            }
        }

        private void AlignWheel()
        {
            if (_wheelTransform == null)
                return;

            _alignmentElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_alignmentElapsed / _alignmentDuration);
            float angle = Mathf.LerpAngle(_alignmentStartAngle, _alignmentTargetAngle, t);
            _wheelTransform.rotation = Quaternion.Euler(0, 0, angle);
            if (t >= 1f)
                EndRotation();
        }

        private void InitAlignment()
        {
            if (_wheelTransform == null || items == null || items.Length == 0)
                return;

            _alignmentStartAngle = _wheelTransform.rotation.eulerAngles.z;
            _alignmentTargetAngle = CalculateTargetAngle(_pendingResultId);
            _alignmentElapsed = 0f;
            State = SpinState.Aligning;
            OnAlignmentStarted?.Invoke();
        }

        private float CalculateTargetAngle(int resultId)
        {
            if (items == null || items.Length == 0)
                return _wheelTransform != null ? _wheelTransform.rotation.eulerAngles.z : 0f;

            int id = (resultId % items.Length + items.Length) % items.Length;
            float sectorAngle = 360f / items.Length;
            float arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            float currentWheelAngle = _wheelTransform.rotation.eulerAngles.z;
            float targetRelative = id * sectorAngle - _wheelOffsetZ;
            float currentRelative = (currentWheelAngle - arrowAngle + 360f) % 360f;
            float diff = targetRelative - currentRelative;
            if (diff > 180f)
                diff -= 360f;
            else if (diff < -180f)
                diff += 360f;

            return currentWheelAngle + diff;
        }

        private void EndRotation()
        {
            int winId = (!_enableAlignment || _pendingResultId < 0) && IsSetupValid()
                ? GetResultIdFromAngle()
                : (_pendingResultId >= 0 ? _pendingResultId : 0);
            _pendingResultId = -1;

            State = SpinState.Idle;
            if (_canvasGroup != null)
                _canvasGroup.interactable = _canUse;

            OnWinIdVariant?.Invoke(winId);
            OnStopped?.Invoke();
        }

        private int GetResultIdFromAngle()
        {
            if (_wheelTransform == null || items == null || items.Length == 0)
                return 0;

            float sectorAngle = 360f / items.Length;
            float wheelAngle = _wheelTransform.rotation.eulerAngles.z;
            float arrowAngle = _arrow != null ? _arrow.transform.eulerAngles.z : 0f;
            float relativeAngle = (wheelAngle + _wheelOffsetZ - arrowAngle + 360f) % 360f;
            int id = Mathf.FloorToInt((relativeAngle + sectorAngle / 2f) / sectorAngle);
            return (id + items.Length) % items.Length;
        }

        private int ChooseResultIndex()
        {
            if (_chanceSystem != null)
            {
                int id = _chanceSystem.GetId();
                if (id >= 0 && id < items.Length)
                    return id;
            }

            if (_chanceData != null && _chanceData.Manager != null)
            {
                if (_chanceData.Manager.TryEvaluate(out int index, out _) && index >= 0 && index < items.Length)
                    return index;
            }

            if (_sectorWeights != null && _sectorWeights.Length == items.Length)
            {
                float sum = 0f;
                for (int i = 0; i < _sectorWeights.Length; i++)
                    sum += Mathf.Max(0f, _sectorWeights[i]);

                if (sum > 0f)
                {
                    float value = Random.Range(0f, sum);
                    float cumulative = 0f;
                    for (int i = 0; i < _sectorWeights.Length; i++)
                    {
                        cumulative += Mathf.Max(0f, _sectorWeights[i]);
                        if (value <= cumulative)
                            return i;
                    }
                    return _sectorWeights.Length - 1;
                }
            }

            return Random.Range(0, items.Length);
        }

        [Button]
        public void Spin()
        {
            if (!IsSetupValid())
                return;

            if (State != SpinState.Idle || (_singleUse && !_canUse))
            {
                OnSpinBlocked?.Invoke();
                return;
            }

            CancelInvoke(nameof(Stop));

            _pendingResultId = ChooseResultIndex();
            _canUse = false;
            State = SpinState.Spinning;
            _currentAngularVelocity = _initialAngularVelocity;
            OnSpinStarted?.Invoke();

            if (_autoStopTime > 0)
                Invoke(nameof(Stop), Random.Range(_autoStopTime, _autoStopTime + _extraSpinTime));
        }

        /// <summary>Spins the wheel and guarantees the given sector index will win. Id must be in [0, Items.Length).</summary>
        [Button] 
        public void SpinToResult(int id)
        {
            if (!IsSetupValid())
                return;

            if (id < 0 || id >= items.Length)
            {
                Debug.LogWarning($"[WheelFortuneImproved] SpinToResult({id}) out of range [0, {items.Length}). Ignored.", this);
                return;
            }

            if (State != SpinState.Idle || (_singleUse && !_canUse))
            {
                OnSpinBlocked?.Invoke();
                return;
            }

            CancelInvoke(nameof(Stop));

            _pendingResultId = id;
            _canUse = false;
            State = SpinState.Spinning;
            _currentAngularVelocity = _initialAngularVelocity;
            OnSpinStarted?.Invoke();

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
            OnDecelerationStarted?.Invoke();
        }

        [Button]
        public void AllowSpinAgain()
        {
            canUse = true;
        }

        [Button]
        public void LogState()
        {
            int fromAngle = IsSetupValid() ? GetResultIdFromAngle() : -1;
            Debug.Log($"[WheelFortuneImproved] State={State}, canUse={_canUse}, resultFromAngle={fromAngle}, itemsCount={items?.Length ?? 0}", this);
        }

        [Button]
        public void ResetWheelAngle()
        {
            if (_wheelTransform != null)
                _wheelTransform.rotation = Quaternion.identity;
        }

        [Button]
        public void ArrangePrizesFromEditor()
        {
            ArrangePrizes();
        }

        private void ArrangePrizes()
        {
            if (items == null || items.Length == 0)
                return;

            float angleStep = 360f / items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                float angle = -i * angleStep + _offsetZ;
                Transform itemTransform = items[i].transform;
                float positionAngle = (angle + 90f) * Mathf.Deg2Rad;
                itemTransform.localPosition = new Vector3(Mathf.Cos(positionAngle), Mathf.Sin(positionAngle), 0) * _prizeDistance;
                itemTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>Returns the prize GameObject at the given index, or null if out of range.</summary>
        public GameObject GetPrize(int id)
        {
            if (items == null || id < 0 || id >= items.Length)
                return null;
            return items[id];
        }
    }
}
