using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// A single animator-friendly control point shared by the uGUI and world Mesh Rig adapters.
    /// Animate this RectTransform's position, rotation and scale with Unity Animator.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Neoxider/UI/Mesh Rig Point")]
    [NeoDoc("UI/UIMeshRig.md")]
    public sealed class UIMeshRigPoint : MonoBehaviour
    {
        private const int CurrentSerializedVersion = 2;

        [HideInInspector] [SerializeField] private int _serializedVersion = CurrentSerializedVersion;

        [Header("Influence")]
        [Tooltip("Turns this point's deformation on or off without deleting it.")]
        [SerializeField] private bool _influenceEnabled = true;

        [Tooltip("Vertices inside this ellipse follow the point at full Strength.")]
        [SerializeField] private Vector2 _innerRadiusNormalized = new Vector2(0.1f, 0.1f);

        [Tooltip("Influence fades to zero at this ellipse. Vertices outside it do not move.")]
        [SerializeField] private Vector2 _radiusNormalized = new Vector2(0.2f, 0.2f);

        [Tooltip("Controls how influence fades between the Inner and Outer ellipses.")]
        [SerializeField] private UIMeshRigFalloffPreset _falloffPreset = UIMeshRigFalloffPreset.Smooth;

        [Tooltip("Multiplies the point's influence. 1 is normal, 0 does nothing, above 1 exaggerates.")]
        [Min(0f)] [SerializeField] private float _strength = 1f;

        [Header("Deformation Channels")]
        [Tooltip("How much of the point's movement reaches the mesh.")]
        [Range(0f, 1f)] [SerializeField] private float _positionInfluence = 1f;

        [Tooltip("How much of the point's rotation reaches the mesh.")]
        [Range(0f, 1f)] [SerializeField] private float _rotationInfluence = 1f;

        [Tooltip("How much of the point's scale reaches the mesh.")]
        [Range(0f, 1f)] [SerializeField] private float _scaleInfluence = 1f;

        [Header("Identity & Falloff Curve")]
        [Tooltip("Stable name used by Animator bindings and by tools that look points up by key.")]
        [SerializeField] private string _bindingKey = "Point";

        [Tooltip("Falloff shape used when the preset is Custom. X = 0 at Outer, 1 at Inner.")]
        [SerializeField] private AnimationCurve _falloffCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [HideInInspector] [SerializeField] private Vector2 _restCenterNormalized = new Vector2(0.5f, 0.5f);
        [HideInInspector] [SerializeField] private float _restRotationDegrees;
        [HideInInspector] [SerializeField] private Vector3 _restScale = Vector3.one;
        [HideInInspector] [SerializeField] private Vector3 _restLocalPosition;
        [HideInInspector] [SerializeField] private Quaternion _restLocalRotation = Quaternion.identity;
        [HideInInspector] [SerializeField] private Vector3 _restLocalScale = Vector3.one;
        [HideInInspector] [Range(0.01f, 1f)] [SerializeField] private float _falloff = 0.5f;

        private IUIMeshRigOwner _owner;
        private Vector2 _proceduralPosition;
        private float _proceduralRotation;
        private Vector2 _proceduralScale = Vector2.one;

        public string BindingKey => _bindingKey;

        public bool InfluenceEnabled
        {
            get => _influenceEnabled;
            set
            {
                if (_influenceEnabled == value)
                {
                    return;
                }

                _influenceEnabled = value;
                NotifyBindingChanged();
            }
        }

        public Vector2 RestCenterNormalized => _restCenterNormalized;
        public float RestRotationDegrees => _restRotationDegrees;
        public Vector3 RestScale => _restScale;

        public float Strength
        {
            get => _strength;
            set
            {
                _strength = Mathf.Max(0f, value);
                NotifyBindingChanged();
            }
        }

        public AnimationCurve FalloffCurve => _falloffCurve;
        public UIMeshRigFalloffPreset FalloffPreset => _falloffPreset;
        public Vector2 OuterRadiusNormalized => _radiusNormalized;
        public Vector2 InnerRadiusNormalized => _innerRadiusNormalized;

        public Vector2 RadiusNormalized
        {
            get => _radiusNormalized;
            set
            {
                _radiusNormalized = ClampRadius(value);
                _innerRadiusNormalized = ClampInnerRadius(_innerRadiusNormalized, _radiusNormalized);
                NotifyBindingChanged();
            }
        }

        public float Falloff
        {
            get => _falloff;
            set
            {
                _falloff = Mathf.Clamp(value, 0.01f, 1f);
                _innerRadiusNormalized = _radiusNormalized * (1f - _falloff);
                NotifyBindingChanged();
            }
        }

        public void SetInfluenceRadii(Vector2 innerRadius, Vector2 outerRadius)
        {
            _radiusNormalized = ClampRadius(outerRadius);
            _innerRadiusNormalized = ClampInnerRadius(innerRadius, _radiusNormalized);
            _falloff = CalculateLegacyFalloff(_innerRadiusNormalized, _radiusNormalized);
            NotifyBindingChanged();
        }

        public void ApplyFalloffPreset(UIMeshRigFalloffPreset preset)
        {
            _falloffPreset = preset;
            if (preset != UIMeshRigFalloffPreset.Custom)
            {
                _falloffCurve = UIMeshRigFalloffPresets.Create(preset);
            }

            NotifyBindingChanged();
        }

        public void UseFullSmoothFalloff()
        {
            _innerRadiusNormalized = Vector2.zero;
            ApplyFalloffPreset(UIMeshRigFalloffPreset.Smooth);
        }

        private void OnEnable()
        {
            EnsureSerializedData();
            ResolveOwner();
            NotifyBindingChanged();
        }

        private void OnDisable()
        {
            NotifyBindingChanged();
        }

        private void OnTransformParentChanged()
        {
            ResolveOwner();
            NotifyBindingChanged();
        }

        private void OnValidate()
        {
            EnsureSerializedData();
            _radiusNormalized = ClampRadius(_radiusNormalized);
            _innerRadiusNormalized = ClampInnerRadius(_innerRadiusNormalized, _radiusNormalized);
            _falloff = CalculateLegacyFalloff(_innerRadiusNormalized, _radiusNormalized);
            _falloff = Mathf.Clamp(_falloff, 0.01f, 1f);
            _strength = Mathf.Max(0f, _strength);
            _positionInfluence = Mathf.Clamp01(_positionInfluence);
            _rotationInfluence = Mathf.Clamp01(_rotationInfluence);
            _scaleInfluence = Mathf.Clamp01(_scaleInfluence);
            if (_falloffCurve == null || _falloffCurve.length == 0)
            {
                _falloffCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (string.IsNullOrWhiteSpace(_bindingKey))
            {
                _bindingKey = gameObject.name;
            }

            _serializedVersion = CurrentSerializedVersion;
            ResolveOwner();
            NotifyBindingChanged();
        }

        public void CaptureRestPose(UIMeshRigGraphic owner)
        {
            CaptureRestPose((IUIMeshRigOwner)owner);
        }

        public void CaptureRestPose(UIMeshRigWorldRenderer owner)
        {
            CaptureRestPose((IUIMeshRigOwner)owner);
        }

        public void CaptureRestPose(UIMeshRigSpriteRenderer owner)
        {
            CaptureRestPose((IUIMeshRigOwner)owner);
        }

        public void CaptureRestPose(IUIMeshRigOwner owner)
        {
            if (owner == null)
            {
                return;
            }

            _owner = owner;
            _restCenterNormalized = owner.WorldToNormalized(transform.position);
            UIMeshRigGraphic graphic = owner as UIMeshRigGraphic;
            if (graphic != null)
            {
                NormalizeDirectChildAnchor(graphic, _restCenterNormalized);
            }
            _restRotationDegrees = owner.GetRelativeRotationDegrees(transform);
            _restScale = GetRelativeScale(owner.RigTransform, transform);
            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
            _restLocalScale = transform.localScale;
            _serializedVersion = CurrentSerializedVersion;
            transform.hasChanged = false;
            NotifyBindingChanged();
        }

        public void ResetPose(UIMeshRigGraphic owner)
        {
            ResetPose((IUIMeshRigOwner)owner);
        }

        public void ResetPose(UIMeshRigWorldRenderer owner)
        {
            ResetPose((IUIMeshRigOwner)owner);
        }

        public void ResetPose(UIMeshRigSpriteRenderer owner)
        {
            ResetPose((IUIMeshRigOwner)owner);
        }

        public void ResetPose(IUIMeshRigOwner owner)
        {
            if (owner == null)
            {
                return;
            }

            RectTransform pointRect = transform as RectTransform;
            UIMeshRigGraphic graphic = owner as UIMeshRigGraphic;
            if (graphic != null && pointRect != null && pointRect.parent == owner.RigTransform)
            {
                pointRect.position = owner.NormalizedToWorld(_restCenterNormalized);
                pointRect.rotation = owner.RigTransform.rotation * Quaternion.Euler(0f, 0f, _restRotationDegrees);
                SetRelativeScale(owner.RigTransform, pointRect, _restScale);
            }
            else
            {
                transform.localPosition = _restLocalPosition;
                transform.localRotation = _restLocalRotation;
                transform.localScale = _restLocalScale;
            }

            transform.hasChanged = false;
            ClearProceduralPose();
            NotifyPoseChanged();
        }

        public float CalculateWeight(Vector2 normalizedPosition)
        {
            EnsureSerializedData();
            if (!_influenceEnabled || !isActiveAndEnabled || _strength <= 0f)
            {
                return 0f;
            }

            return UIMeshRigGeometryBuilder.EvaluateInfluence(CreateBindingState(), normalizedPosition);
        }

        internal Vector2 TransformLocalPoint(UIMeshRigGraphic owner, Vector2 localPoint)
        {
            return TransformLocalPoint((IUIMeshRigOwner)owner, localPoint);
        }

        internal Vector2 TransformLocalPoint(IUIMeshRigOwner owner, Vector2 localPoint)
        {
            Vector2 origin = owner.NormalizedToLocal(Vector2.zero);
            UIMeshRigCoordinateSpace space = new UIMeshRigCoordinateSpace(
                origin,
                owner.NormalizedToLocal(Vector2.one) - origin);
            return UIMeshRigGeometryBuilder.ApplyPose(CreateState(owner), localPoint, space);
        }

        internal UIMeshRigPointState CreateState(IUIMeshRigOwner owner)
        {
            EnsureSerializedData();
            Vector2 currentCenter = owner.WorldToLocal(transform.position) + _proceduralPosition * owner.MotionUnitScale;
            float currentRotation = owner.GetRelativeRotationDegrees(transform);
            float rotationDeltaDegrees = Mathf.DeltaAngle(_restRotationDegrees, currentRotation) + _proceduralRotation;
            Vector3 currentScale = GetRelativeScale(owner.RigTransform, transform);
            Vector2 scaleRatio = new Vector2(
                SafeRatio(currentScale.x, _restScale.x),
                SafeRatio(currentScale.y, _restScale.y));
            scaleRatio = Vector2.Scale(scaleRatio, _proceduralScale);
            return new UIMeshRigPointState(
                _influenceEnabled && isActiveAndEnabled,
                _restCenterNormalized,
                _innerRadiusNormalized,
                _radiusNormalized,
                _strength,
                _falloffCurve,
                currentCenter,
                rotationDeltaDegrees,
                scaleRatio,
                _positionInfluence,
                _rotationInfluence,
                _scaleInfluence);
        }

        private UIMeshRigPointState CreateBindingState()
        {
            return new UIMeshRigPointState(
                _influenceEnabled && isActiveAndEnabled,
                _restCenterNormalized,
                _innerRadiusNormalized,
                _radiusNormalized,
                _strength,
                _falloffCurve,
                Vector2.zero,
                0f,
                Vector2.one,
                _positionInfluence,
                _rotationInfluence,
                _scaleInfluence);
        }

        public void SetRestCenterNormalized(Vector2 value)
        {
            _restCenterNormalized = new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
            NotifyBindingChanged();
        }

        public void SetBindingKey(string value)
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? gameObject.name : value.Trim();
            if (_bindingKey == nextValue)
            {
                return;
            }

            _bindingKey = nextValue;
            NotifyBindingChanged();
        }

        /// <summary>
        /// Adds a transient pose on top of the Transform pose. Procedural motion therefore composes with
        /// Unity Animator instead of competing for the same RectTransform properties.
        /// </summary>
        public void SetProceduralPose(Vector2 position, float rotationDegrees, Vector2 scaleMultiplier)
        {
            _proceduralPosition = position;
            _proceduralRotation = rotationDegrees;
            _proceduralScale = new Vector2(
                Mathf.Max(0.0001f, scaleMultiplier.x),
                Mathf.Max(0.0001f, scaleMultiplier.y));
            NotifyPoseChanged();
        }

        public void ClearProceduralPose()
        {
            _proceduralPosition = Vector2.zero;
            _proceduralRotation = 0f;
            _proceduralScale = Vector2.one;
            NotifyPoseChanged();
        }

        // WHY: nearest ancestor that implements IUIMeshRigOwner, not a hard-coded list of renderer types —
        // adding an output adapter used to mean editing this method and was silently forgotten.
        private void ResolveOwner()
        {
            _owner = UIMeshRigOwnerResolver.Find(transform);
        }

        private void NotifyBindingChanged()
        {
            if (_owner != null)
            {
                _owner.NotifyBindingChanged();
            }
        }

        private void NotifyPoseChanged()
        {
            if (_owner != null)
            {
                _owner.NotifyPoseChanged();
            }
        }

        private void NormalizeDirectChildAnchor(UIMeshRigGraphic owner, Vector2 normalizedCenter)
        {
            RectTransform pointRect = (RectTransform)transform;
            if (pointRect.parent != owner.transform)
            {
                return;
            }

            Vector3 worldPosition = pointRect.position;
            pointRect.anchorMin = normalizedCenter;
            pointRect.anchorMax = normalizedCenter;
            pointRect.position = worldPosition;
        }

        private static Vector2 ClampRadius(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp(value.x, 0.005f, 1f),
                Mathf.Clamp(value.y, 0.005f, 1f));
        }

        private static Vector2 ClampInnerRadius(Vector2 value, Vector2 outer)
        {
            return new Vector2(
                Mathf.Clamp(value.x, 0f, outer.x),
                Mathf.Clamp(value.y, 0f, outer.y));
        }

        private void EnsureSerializedData()
        {
            if (_serializedVersion >= CurrentSerializedVersion)
            {
                return;
            }

            _innerRadiusNormalized = _radiusNormalized * (1f - Mathf.Clamp01(_falloff));
            _falloffPreset = UIMeshRigFalloffPreset.Smooth;
            _serializedVersion = CurrentSerializedVersion;
        }

        private static float GetEllipseBoundaryDistance(Vector2 direction, Vector2 radius)
        {
            float length = direction.magnitude;
            if (length <= 0.000001f || radius.x <= 0.000001f || radius.y <= 0.000001f)
            {
                return 0f;
            }

            Vector2 unit = direction / length;
            float denominator = Mathf.Sqrt(
                unit.x * unit.x / (radius.x * radius.x) +
                unit.y * unit.y / (radius.y * radius.y));
            return denominator > 0.000001f ? 1f / denominator : 0f;
        }

        private static float CalculateLegacyFalloff(Vector2 inner, Vector2 outer)
        {
            float fractionX = outer.x > 0.00001f ? inner.x / outer.x : 0f;
            float fractionY = outer.y > 0.00001f ? inner.y / outer.y : 0f;
            return Mathf.Clamp(1f - (fractionX + fractionY) * 0.5f, 0.01f, 1f);
        }

        private static Vector3 GetRelativeScale(Transform owner, Transform point)
        {
            Vector3 ownerScale = owner.lossyScale;
            Vector3 pointScale = point.lossyScale;
            return new Vector3(
                SafeRatio(pointScale.x, ownerScale.x),
                SafeRatio(pointScale.y, ownerScale.y),
                SafeRatio(pointScale.z, ownerScale.z));
        }

        private static void SetRelativeScale(Transform owner, Transform point, Vector3 relativeScale)
        {
            Transform parent = point.parent;
            if (parent == null)
            {
                point.localScale = relativeScale;
                return;
            }

            Vector3 ownerScale = owner.lossyScale;
            Vector3 parentScale = parent.lossyScale;
            point.localScale = new Vector3(
                SafeRatio(ownerScale.x * relativeScale.x, parentScale.x),
                SafeRatio(ownerScale.y * relativeScale.y, parentScale.y),
                SafeRatio(ownerScale.z * relativeScale.z, parentScale.z));
        }

        private static float SafeRatio(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) > 0.00001f ? numerator / denominator : 1f;
        }
    }
}
