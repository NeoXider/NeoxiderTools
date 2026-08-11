using UnityEngine;

namespace Neo.UI
{
    /// <summary>
    /// A single animator-friendly control point for <see cref="UIMeshRigGraphic"/>.
    /// Animate this RectTransform's position, rotation and scale with Unity Animator.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Neoxider/UI/Mesh Rig Point")]
    public sealed class UIMeshRigPoint : MonoBehaviour
    {
        private const int CurrentSerializedVersion = 1;

        [HideInInspector] [SerializeField] private int _serializedVersion = CurrentSerializedVersion;
        [SerializeField] private string _bindingKey = "Point";
        [SerializeField] private bool _influenceEnabled = true;
        [SerializeField] private Vector2 _restCenterNormalized = new Vector2(0.5f, 0.5f);
        [SerializeField] private float _restRotationDegrees;
        [SerializeField] private Vector3 _restScale = Vector3.one;
        [SerializeField] private Vector3 _restLocalPosition;
        [SerializeField] private Quaternion _restLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 _restLocalScale = Vector3.one;
        [SerializeField] private Vector2 _radiusNormalized = new Vector2(0.2f, 0.2f);
        [Range(0.01f, 1f)] [SerializeField]
        private float _falloff = 0.5f;
        [Min(0f)] [SerializeField] private float _strength = 1f;
        [Range(0f, 1f)] [SerializeField] private float _positionInfluence = 1f;
        [Range(0f, 1f)] [SerializeField] private float _rotationInfluence = 1f;
        [Range(0f, 1f)] [SerializeField] private float _scaleInfluence = 1f;
        [SerializeField] private AnimationCurve _falloffCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private UIMeshRigGraphic _owner;
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

        public Vector2 RadiusNormalized
        {
            get => _radiusNormalized;
            set
            {
                _radiusNormalized = ClampRadius(value);
                NotifyBindingChanged();
            }
        }

        public float Falloff
        {
            get => _falloff;
            set
            {
                _falloff = Mathf.Clamp(value, 0.01f, 1f);
                NotifyBindingChanged();
            }
        }

        private void OnEnable()
        {
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
            if (_serializedVersion < CurrentSerializedVersion)
            {
                _restLocalPosition = transform.localPosition;
                _restLocalRotation = transform.localRotation;
                _restLocalScale = transform.localScale;
            }

            _radiusNormalized = ClampRadius(_radiusNormalized);
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
            if (owner == null)
            {
                return;
            }

            _owner = owner;
            _restCenterNormalized = owner.WorldToNormalized(transform.position);
            NormalizeDirectChildAnchor(owner, _restCenterNormalized);
            _restRotationDegrees = owner.GetRelativeRotationDegrees(transform);
            _restScale = GetRelativeScale(owner.transform, transform);
            _restLocalPosition = transform.localPosition;
            _restLocalRotation = transform.localRotation;
            _restLocalScale = transform.localScale;
            _serializedVersion = CurrentSerializedVersion;
            transform.hasChanged = false;
            NotifyBindingChanged();
        }

        public void ResetPose(UIMeshRigGraphic owner)
        {
            if (owner == null)
            {
                return;
            }

            RectTransform pointRect = (RectTransform)transform;
            if (pointRect.parent == owner.transform)
            {
                pointRect.position = owner.NormalizedToWorld(_restCenterNormalized);
                pointRect.rotation = owner.transform.rotation * Quaternion.Euler(0f, 0f, _restRotationDegrees);
                SetRelativeScale(owner.transform, pointRect, _restScale);
            }
            else
            {
                pointRect.localPosition = _restLocalPosition;
                pointRect.localRotation = _restLocalRotation;
                pointRect.localScale = _restLocalScale;
            }

            transform.hasChanged = false;
            ClearProceduralPose();
            NotifyPoseChanged();
        }

        public float CalculateWeight(Vector2 normalizedPosition)
        {
            if (!_influenceEnabled || !isActiveAndEnabled || _strength <= 0f)
            {
                return 0f;
            }

            float dx = (normalizedPosition.x - _restCenterNormalized.x) / _radiusNormalized.x;
            float dy = (normalizedPosition.y - _restCenterNormalized.y) / _radiusNormalized.y;
            float ellipticalDistance = Mathf.Sqrt(dx * dx + dy * dy);
            if (ellipticalDistance >= 1f)
            {
                return 0f;
            }

            float solidRadius = 1f - _falloff;
            if (ellipticalDistance <= solidRadius)
            {
                return _strength;
            }

            float edgeT = Mathf.InverseLerp(1f, solidRadius, ellipticalDistance);
            float curveWeight = Mathf.Clamp01(_falloffCurve.Evaluate(edgeT));
            return curveWeight * _strength;
        }

        internal Vector2 TransformLocalPoint(UIMeshRigGraphic owner, Vector2 localPoint)
        {
            Vector2 restCenter = owner.NormalizedToLocal(_restCenterNormalized);
            Vector2 currentCenter = owner.WorldToLocal(transform.position);
            float currentRotation = owner.GetRelativeRotationDegrees(transform);
            float rotationDeltaDegrees = Mathf.DeltaAngle(_restRotationDegrees, currentRotation);
            rotationDeltaDegrees += _proceduralRotation;
            float rotationDelta = rotationDeltaDegrees * _rotationInfluence * Mathf.Deg2Rad;
            Vector3 currentScale = GetRelativeScale(owner.transform, transform);
            Vector2 scaleRatio = new Vector2(
                SafeRatio(currentScale.x, _restScale.x),
                SafeRatio(currentScale.y, _restScale.y));
            scaleRatio = Vector2.Scale(scaleRatio, _proceduralScale);
            scaleRatio = Vector2.LerpUnclamped(Vector2.one, scaleRatio, _scaleInfluence);

            Vector2 relative = localPoint - restCenter;
            relative = Vector2.Scale(relative, scaleRatio);
            float cosine = Mathf.Cos(rotationDelta);
            float sine = Mathf.Sin(rotationDelta);
            Vector2 rotated = new Vector2(
                relative.x * cosine - relative.y * sine,
                relative.x * sine + relative.y * cosine);
            Vector2 translatedCenter = Vector2.LerpUnclamped(
                restCenter,
                currentCenter + _proceduralPosition,
                _positionInfluence);
            return translatedCenter + rotated;
        }

        internal void SetRestCenterNormalized(Vector2 value)
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

        private void ResolveOwner()
        {
            _owner = GetComponentInParent<UIMeshRigGraphic>();
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
