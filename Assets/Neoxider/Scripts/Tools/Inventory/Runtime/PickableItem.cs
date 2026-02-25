using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/PickableItem.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/PickableItem")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(PickableItem))]
    public sealed class PickableItem : MonoBehaviour
    {
        [Header("Item")] [SerializeField] [Tooltip("Preferred item source. If set, itemId is ignored.")]
        private InventoryItemData _itemData;

        [SerializeField] [Tooltip("Fallback id when ItemData is not assigned.")]
        private int _itemId;

        [SerializeField] [Min(1)] [Tooltip("Amount to add on pickup.")]
        private int _amount = 1;

        [Header("Inventory")] [SerializeField]
        [Tooltip("Optional target inventory. If null, FindDefault() will be used.")]
        private InventoryComponent _targetInventory;

        [SerializeField] [Tooltip("Try to resolve InventoryComponent.FindDefault() when target is null.")]
        private bool _autoFindInventory = true;

        [Header("Auto Collect")] [SerializeField] [Tooltip("Collect when entering 3D trigger.")]
        private bool _collectOnTrigger3D = true;

        [SerializeField] [Tooltip("Collect when entering 2D trigger.")]
        private bool _collectOnTrigger2D;

        [SerializeField] [Tooltip("Secondary collector tag filter (empty = disabled).")]
        private string _requiredCollectorTag = "";

        [Header("Collector Validation")] [SerializeField]
        [Tooltip("Require InventoryComponent on collector object (or its parents if enabled). Enabled by default.")]
        private bool _requireCollectorInventory = true;

        [SerializeField] [Tooltip("When collector inventory is required, search InventoryComponent in parents too.")]
        private bool _searchCollectorInventoryInParents = true;

        [SerializeField] [Tooltip("Use collector's InventoryComponent as target inventory when found.")]
        private bool _useCollectorInventoryAsTarget = true;

        [Header("Post Collect")] [SerializeField] [Tooltip("If true, collects only once.")]
        private bool _collectOnlyOnce = true;

        [SerializeField] [Tooltip("Disable all colliders after successful collect.")]
        private bool _disableCollidersOnCollect = true;

        [SerializeField] [Tooltip("Destroy object after successful collect.")]
        private bool _destroyAfterCollect = true;

        [SerializeField] [Tooltip("Deactivate object if destroy is disabled.")]
        private bool _deactivateAfterCollect;

        [Header("Activation (when used in hand)")]
        [Tooltip("Вызывается при активации предмета (рука вызывает Activate() при применении). Подпишите для эффекта использования в руке.")]
        public UnityEvent OnActivate = new();

        [Header("Events")] public UnityEvent OnCollectStarted = new();
        public UnityEvent<int, int> OnCollected = new();
        public UnityEvent OnCollectFailed = new();
        public UnityEvent OnAfterCollectDespawn = new();

        private bool _wasCollected;

        public int ResolvedItemId => _itemData != null ? _itemData.ItemId : _itemId;
        public int Amount => _amount;
        public InventoryComponent TargetInventory => _targetInventory;

        public bool Collect()
        {
            return TryCollect(null);
        }

        public bool Pickup()
        {
            return TryCollect(null);
        }

        public bool CollectFromGameObject(GameObject collector)
        {
            return TryCollect(collector);
        }

        public bool PickupFromGameObject(GameObject collector)
        {
            return TryCollect(collector);
        }

        public bool CollectFromCollider(Collider collider3D)
        {
            return TryCollect(collider3D != null ? collider3D.gameObject : null);
        }

        public bool PickupFromCollider(Collider collider3D)
        {
            return TryCollect(collider3D != null ? collider3D.gameObject : null);
        }

        public bool CollectFromCollider2D(Collider2D collider2D)
        {
            return TryCollect(collider2D != null ? collider2D.gameObject : null);
        }

        public bool PickupFromCollider2D(Collider2D collider2D)
        {
            return TryCollect(collider2D != null ? collider2D.gameObject : null);
        }

        public void SetTargetInventory(InventoryComponent inventory)
        {
            _targetInventory = inventory;
        }

        /// <summary>
        ///     Вызвать активацию предмета (например при применении в руке). Вызывает OnActivate.
        /// </summary>
        public void Activate()
        {
            OnActivate?.Invoke();
        }

        public void Configure(InventoryItemData itemData, int fallbackItemId, int amount, InventoryComponent targetInventory = null)
        {
            _itemData = itemData;
            _itemId = itemData != null ? itemData.ItemId : fallbackItemId;
            _amount = Mathf.Max(1, amount);
            _targetInventory = targetInventory;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_collectOnTrigger3D)
            {
                return;
            }

            TryCollect(other != null ? other.gameObject : null);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_collectOnTrigger2D)
            {
                return;
            }

            TryCollect(other != null ? other.gameObject : null);
        }

        private bool TryCollect(GameObject collector)
        {
            if (_collectOnlyOnce && _wasCollected)
            {
                return false;
            }

            InventoryComponent collectorInventory = ResolveCollectorInventory(collector);
            if (_requireCollectorInventory && collectorInventory == null)
            {
                OnCollectFailed?.Invoke();
                return false;
            }

            if (!PassCollectorFilter(collector))
            {
                return false;
            }

            InventoryComponent inventory = ResolveInventory(collectorInventory);
            if (inventory == null)
            {
                OnCollectFailed?.Invoke();
                return false;
            }

            int itemId = ResolvedItemId;
            if (_amount <= 0)
            {
                OnCollectFailed?.Invoke();
                return false;
            }

            OnCollectStarted?.Invoke();
            int added = inventory.AddItemByIdAmount(itemId, _amount);
            if (added <= 0)
            {
                OnCollectFailed?.Invoke();
                return false;
            }

            _wasCollected = true;
            OnCollected?.Invoke(itemId, added);

            if (_disableCollidersOnCollect)
            {
                DisableAllColliders();
            }

            if (_destroyAfterCollect)
            {
                OnAfterCollectDespawn?.Invoke();
                Destroy(gameObject);
            }
            else if (_deactivateAfterCollect)
            {
                OnAfterCollectDespawn?.Invoke();
                gameObject.SetActive(false);
            }

            return true;
        }

        private InventoryComponent ResolveInventory(InventoryComponent collectorInventory)
        {
            if (_useCollectorInventoryAsTarget && collectorInventory != null)
            {
                return collectorInventory;
            }

            if (_targetInventory != null)
            {
                return _targetInventory;
            }

            if (!_autoFindInventory)
            {
                return null;
            }

            _targetInventory = InventoryComponent.FindDefault();
            return _targetInventory;
        }

        private InventoryComponent ResolveCollectorInventory(GameObject collector)
        {
            if (collector == null)
            {
                return null;
            }

            if (_searchCollectorInventoryInParents)
            {
                return collector.GetComponentInParent<InventoryComponent>();
            }

            return collector.GetComponent<InventoryComponent>();
        }

        private bool PassCollectorFilter(GameObject collector)
        {
            if (string.IsNullOrWhiteSpace(_requiredCollectorTag))
            {
                return true;
            }

            return collector != null && collector.CompareTag(_requiredCollectorTag);
        }

        private void DisableAllColliders()
        {
            Collider[] colliders3D = GetComponents<Collider>();
            for (int i = 0; i < colliders3D.Length; i++)
            {
                colliders3D[i].enabled = false;
            }

            Collider2D[] colliders2D = GetComponents<Collider2D>();
            for (int i = 0; i < colliders2D.Length; i++)
            {
                colliders2D[i].enabled = false;
            }
        }
    }
}
