using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/PickableItem.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryPickupBridge")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryPickupBridge))]
    public sealed class InventoryPickupBridge : MonoBehaviour
    {
        [SerializeField] [Tooltip("Target pickable item. If null, component on the same object is used.")]
        private PickableItem _pickableItem;

        private void Awake()
        {
            if (_pickableItem == null)
            {
                _pickableItem = GetComponent<PickableItem>();
            }
        }

        public void Collect()
        {
            _pickableItem?.Collect();
        }

        public void CollectFromCollider(Collider collider3D)
        {
            _pickableItem?.CollectFromCollider(collider3D);
        }

        public void CollectFromCollider2D(Collider2D collider2D)
        {
            _pickableItem?.CollectFromCollider2D(collider2D);
        }

        public void CollectFromGameObject(GameObject collector)
        {
            _pickableItem?.CollectFromGameObject(collector);
        }
    }
}
