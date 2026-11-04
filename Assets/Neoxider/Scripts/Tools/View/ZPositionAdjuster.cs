using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Derives world Z from the normalized world Y every <c>LateUpdate</c>, for 2D depth ordering.
    /// </summary>
    [NeoDoc("Tools/View/ZPositionAdjuster.md")]
    [CreateFromMenu("Neoxider/Tools/View/ZPositionAdjuster")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ZPositionAdjuster))]
    public class ZPositionAdjuster : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("True: normalize Y to 0..1 before scaling. False: normalize to -1..1.")]
        [SerializeField]
        private bool _useNormalizeToUnit = true;

        [Tooltip("Multiplier applied to the normalized Y to get world Z.")]
        [Min(0)]
        [SerializeField]
        private float _ratio = 1;

        private Transform _transform;

        /// <summary>True: Y is normalized to 0..1; false: to -1..1.</summary>
        public bool UseNormalizeToUnit
        {
            get => _useNormalizeToUnit;
            set => _useNormalizeToUnit = value;
        }

        /// <summary>Multiplier applied to the normalized Y; negative values are clamped to zero.</summary>
        public float Ratio
        {
            get => _ratio;
            set => _ratio = Mathf.Max(0f, value);
        }

        private void LateUpdate()
        {
            Adjust();
        }

        private void OnValidate()
        {
            Adjust();
        }

        /// <summary>
        ///     Applies the derived Z now. Writes the transform only when Z actually changes, so a resting object
        ///     raises no transform-changed work (child propagation, physics sync) every frame.
        /// </summary>
        public void Adjust()
        {
            if (ReferenceEquals(_transform, null))
            {
                _transform = transform;
            }

            Vector3 position = _transform.position;
            float z = ComputeDepth(position.y, _useNormalizeToUnit, _ratio);
            if (position.z.Equals(z))
            {
                return;
            }

            position.z = z;
            _transform.position = position;
        }

        /// <summary>The Z that <see cref="Adjust" /> assigns for a world <paramref name="y" />.</summary>
        public static float ComputeDepth(float y, bool useNormalizeToUnit, float ratio)
        {
            float normalized = useNormalizeToUnit ? y.NormalizeToUnit() : y.NormalizeToRange();
            return normalized * ratio;
        }
    }
}
