using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Hand view: add to the pickable item prefab (same as WorldDropPrefab).
    ///     InventoryHand finds this on the instance when showing an item in hand and applies
    ///     position/rotation offset and base scale; hand-wide scale (delta or fixed) is applied on top.
    /// </summary>
    [NeoDoc("Tools/Inventory/HandView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/HandView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(HandView))]
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField] [Tooltip("Item position offset from the hand anchor (local space).")]
        private Vector3 _positionOffset;

        [SerializeField] [Tooltip("Rotation offset in degrees (Euler) relative to the hand.")]
        private Vector3 _rotationOffset;

        [SerializeField]
        [Tooltip(
            "Base scale for this item in hand (1 = unchanged). Hand-wide scale is applied on top (delta or fixed).")]
        [Min(0.01f)]
        private float _scaleInHand = 1f;

        /// <summary>Position offset in hand local space.</summary>
        public Vector3 PositionOffset => _positionOffset;

        /// <summary>Rotation offset in degrees (Euler).</summary>
        public Vector3 RotationOffset => _rotationOffset;

        /// <summary>Base item scale in hand.</summary>
        public float ScaleInHand => Mathf.Max(0.01f, _scaleInHand);

        private void Reset()
        {
            _scaleInHand = 1f;
        }
    }
}
