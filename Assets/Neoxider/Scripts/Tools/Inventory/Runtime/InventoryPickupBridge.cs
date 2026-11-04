using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     UnityEvent adapter that forwards collect calls to a <see cref="PickableItem" /> (this object's one by
    ///     default). From C#, call the <see cref="PickableItem" /> methods directly.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventoryPickupBridge.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryPickupBridge")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryPickupBridge))]
    public sealed class InventoryPickupBridge : MonoBehaviour
    {
        [SerializeField] [Tooltip("Target pickable item. If null, component on the same object is used.")]
        private PickableItem _pickableItem;

        private void Awake()
        {
            ResolvePickable();
        }

        /// <summary>Collects without a collector object; see <see cref="PickableItem.Collect" />.</summary>
        public void Collect()
        {
            PickableItem pickable = ResolvePickable();
            if (pickable != null)
            {
                pickable.Collect();
            }
        }

        /// <summary>Collects for the object that owns <paramref name="collider3D" />.</summary>
        public void CollectFromCollider(Collider collider3D)
        {
            PickableItem pickable = ResolvePickable();
            if (pickable != null)
            {
                pickable.CollectFromCollider(collider3D);
            }
        }

        /// <summary>Collects for the object that owns <paramref name="collider2D" />.</summary>
        public void CollectFromCollider2D(Collider2D collider2D)
        {
            PickableItem pickable = ResolvePickable();
            if (pickable != null)
            {
                pickable.CollectFromCollider2D(collider2D);
            }
        }

        /// <summary>Collects for <paramref name="collector" /> (its InventoryComponent may become the target).</summary>
        public void CollectFromGameObject(GameObject collector)
        {
            PickableItem pickable = ResolvePickable();
            if (pickable != null)
            {
                pickable.CollectFromGameObject(collector);
            }
        }

        // WHY: Explicit Unity null checks instead of ?. - a destroyed or unassigned PickableItem is a non-null C#
        // reference, and "?." would still run collection on it and grant items from a dead pickup.
        private PickableItem ResolvePickable()
        {
            if (_pickableItem == null)
            {
                TryGetComponent(out _pickableItem);
            }

            return _pickableItem;
        }
    }
}
