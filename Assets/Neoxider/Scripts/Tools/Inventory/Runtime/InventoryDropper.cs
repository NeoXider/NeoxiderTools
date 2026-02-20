using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [NeoDoc("Tools/Inventory/InventoryDropper.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryDropper")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryDropper))]
    public sealed class InventoryDropper : MonoBehaviour
    {
        [Header("Links")] [SerializeField]
        [Tooltip("Inventory source. If null and Auto Find enabled, InventoryComponent.FindDefault() is used.")]
        private InventoryComponent _inventory;

        [SerializeField] [Tooltip("Optional drop position source. If empty, this transform is used.")]
        private Transform _dropPoint;

        [SerializeField] [Tooltip("Resolve InventoryComponent.FindDefault() if Inventory is not assigned.")]
        private bool _autoFindInventory = true;

        [Header("Input")] [SerializeField] [Tooltip("Allow dropping via keyboard input.")]
        private bool _allowDropInput = true;

        [SerializeField] [Tooltip("Master switch for dropping logic.")]
        private bool _canDrop = true;

        [SerializeField] [Tooltip("Key used for default drop action.")]
        private KeyCode _dropKey = KeyCode.G;

        [SerializeField] [Tooltip("Use inventory.SelectedItemId when pressing drop key.")]
        private bool _dropSelectedOnKey = true;

        [SerializeField] [Tooltip("Fallback item id when Drop Selected On Key is disabled. Use -1 to drop the last item (by snapshot order).")]
        private int _dropItemIdOnKey;

        [SerializeField] [Tooltip("When current item (selected or configured id) has count 0, drop the next available item from inventory.")]
        private bool _dropNextWhenEmpty = true;

        [SerializeField] [Min(1)] [Tooltip("Amount dropped by keyboard action.")]
        private int _dropAmountOnKey = 1;

        [Header("Spawn")] [SerializeField] [Tooltip("Fallback prefab when item has no WorldDropPrefab.")]
        private GameObject _fallbackDropPrefab;

        [SerializeField] [Tooltip("Random offset radius around drop point.")]
        [Min(0f)]
        private float _randomRadius;

        [SerializeField] [Tooltip("Forward force direction in local space.")]
        private Vector3 _throwDirection = Vector3.forward;

        [SerializeField] [Tooltip("Impulse force applied to spawned rigidbody.")]
        [Min(0f)]
        private float _throwImpulse = 2f;

        [Header("Physics")] [SerializeField] [Tooltip("Ensure Rigidbody on spawned object.")]
        private bool _addRigidbody3D = true;

        [SerializeField] [Tooltip("Ensure Rigidbody2D on spawned object.")]
        private bool _addRigidbody2D;

        [SerializeField] [Tooltip("Ensure collider exists on spawned object.")]
        private bool _addColliderIfMissing = true;

        [SerializeField] [Tooltip("Use trigger collider for auto-pick flow.")]
        private bool _useTriggerCollider = true;

        [SerializeField] [Min(0.01f)] private float _defaultColliderRadius = 0.35f;

        [Header("Pickup")] [SerializeField]
        [Tooltip("Ensure PickableItem component and configure it for dropped item.")]
        private bool _configurePickableItem = true;

        [SerializeField] [Tooltip("Set target inventory on dropped PickableItem.")]
        private bool _setTargetInventoryForPickable;

        [Header("Events")] public UnityEvent<int, int, GameObject> OnItemDropped = new();
        public UnityEvent<int, int> OnDropFailed = new();

        public InventoryComponent Inventory => _inventory;
        public bool CanDrop
        {
            get => _canDrop;
            set => _canDrop = value;
        }

        private void Update()
        {
            if (!_allowDropInput || !_canDrop || !Input.GetKeyDown(_dropKey))
            {
                return;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, _dropAmountOnKey);
                return;
            }

            int itemId = ResolveItemIdForKeyDrop(inv);
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, _dropAmountOnKey);
                return;
            }

            DropById(itemId, _dropAmountOnKey);
        }

        /// <summary>
        ///     Resolves which item id to drop on key press: selected, configured id, or -1 = last; with fallback to next available when empty.
        /// </summary>
        private int ResolveItemIdForKeyDrop(InventoryComponent inv)
        {
            int candidate = _dropSelectedOnKey
                ? inv.SelectedItemId
                : (_dropItemIdOnKey == -1 ? inv.GetLastItemId() : _dropItemIdOnKey);

            if (_dropNextWhenEmpty && (candidate < 0 || inv.GetCount(candidate) <= 0))
            {
                candidate = inv.GetFirstItemId();
            }

            return candidate;
        }

        public int DropSelected(int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            int itemId = ResolveItemIdForKeyDrop(inv);
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemId, amount);
        }

        public int DropById(int itemId, int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            if (amount <= 0)
            {
                OnDropFailed?.Invoke(itemId, amount);
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(itemId, amount);
                return 0;
            }

            InventoryItemData data = inv.GetItemData(itemId);
            int removed = inv.RemoveItemByIdAmount(itemId, amount);
            if (removed <= 0)
            {
                OnDropFailed?.Invoke(itemId, amount);
                return 0;
            }

            SpawnDroppedItem(inv, data, itemId, removed);
            return removed;
        }

        public int DropData(InventoryItemData itemData, int amount = 1)
        {
            if (!_canDrop)
            {
                return 0;
            }

            if (itemData == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemData.ItemId, amount);
        }

        public int DropByIdOne(int itemId)
        {
            return DropById(itemId, 1);
        }

        /// <summary>
        ///     Drops the first item (by snapshot order) from inventory. Returns amount actually dropped, or 0 if empty or CanDrop is false.
        /// </summary>
        public int DropFirst(int amount = 1)
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            int itemId = inv.GetFirstItemId();
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemId, amount);
        }

        /// <summary>
        ///     Drops the last item (by snapshot order) from inventory. Returns amount actually dropped, or 0 if empty or CanDrop is false.
        /// </summary>
        public int DropLast(int amount = 1)
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            int itemId = inv.GetLastItemId();
            if (itemId < 0)
            {
                OnDropFailed?.Invoke(-1, amount);
                return 0;
            }

            return DropById(itemId, amount);
        }

        public int DropConfiguredById()
        {
            if (!_canDrop)
            {
                return 0;
            }

            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                OnDropFailed?.Invoke(_dropItemIdOnKey, _dropAmountOnKey);
                return 0;
            }

            int itemId = _dropItemIdOnKey == -1 ? inv.GetLastItemId() : _dropItemIdOnKey;
            if (_dropNextWhenEmpty && (itemId < 0 || inv.GetCount(itemId) <= 0))
            {
                itemId = inv.GetFirstItemId();
            }

            if (itemId < 0)
            {
                OnDropFailed?.Invoke(_dropItemIdOnKey, _dropAmountOnKey);
                return 0;
            }

            return DropById(itemId, _dropAmountOnKey);
        }

        public void SetDropEnabled(bool enabled)
        {
            _canDrop = enabled;
        }

        public void SetDropItemId(int itemId)
        {
            _dropItemIdOnKey = itemId;
        }

        private void SpawnDroppedItem(InventoryComponent inventory, InventoryItemData itemData, int itemId, int amount)
        {
            GameObject prefab = itemData != null && itemData.WorldDropPrefab != null ? itemData.WorldDropPrefab : _fallbackDropPrefab;
            if (prefab == null)
            {
                OnDropFailed?.Invoke(itemId, amount);
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            GameObject dropped = Instantiate(prefab, spawnPosition, Quaternion.identity);

            EnsureColliders(dropped);
            ApplyPhysicsImpulse(dropped);

            if (_configurePickableItem)
            {
                ConfigurePickable(dropped, inventory, itemData, itemId, amount);
            }

            OnItemDropped?.Invoke(itemId, amount, dropped);
        }

        private InventoryComponent ResolveInventory()
        {
            if (_inventory != null)
            {
                return _inventory;
            }

            if (!_autoFindInventory)
            {
                return null;
            }

            _inventory = InventoryComponent.FindDefault();
            return _inventory;
        }

        private Vector3 GetSpawnPosition()
        {
            Transform source = _dropPoint != null ? _dropPoint : transform;
            Vector3 basePosition = source.position;
            if (_randomRadius <= 0f)
            {
                return basePosition;
            }

            Vector2 offset = Random.insideUnitCircle * _randomRadius;
            return basePosition + new Vector3(offset.x, 0f, offset.y);
        }

        private void EnsureColliders(GameObject dropped)
        {
            if (!_addColliderIfMissing)
            {
                return;
            }

            if (_addRigidbody2D)
            {
                Collider2D col2D = dropped.GetComponent<Collider2D>();
                if (col2D == null)
                {
                    CircleCollider2D created = dropped.AddComponent<CircleCollider2D>();
                    created.radius = _defaultColliderRadius;
                    created.isTrigger = _useTriggerCollider;
                }
            }
            else
            {
                Collider col = dropped.GetComponent<Collider>();
                if (col == null)
                {
                    SphereCollider created = dropped.AddComponent<SphereCollider>();
                    created.radius = _defaultColliderRadius;
                    created.isTrigger = _useTriggerCollider;
                }
            }
        }

        private void ApplyPhysicsImpulse(GameObject dropped)
        {
            Vector3 direction = transform.TransformDirection(_throwDirection.normalized);

            if (_addRigidbody2D)
            {
                Rigidbody2D rb2D = dropped.GetComponent<Rigidbody2D>();
                if (rb2D == null)
                {
                    rb2D = dropped.AddComponent<Rigidbody2D>();
                }

                if (_throwImpulse > 0f)
                {
                    Vector2 force = new(direction.x, direction.z);
                    if (force.sqrMagnitude > 0f)
                    {
                        rb2D.AddForce(force * _throwImpulse, ForceMode2D.Impulse);
                    }
                }
            }

            if (_addRigidbody3D)
            {
                Rigidbody rb = dropped.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = dropped.AddComponent<Rigidbody>();
                }

                if (_throwImpulse > 0f && direction.sqrMagnitude > 0f)
                {
                    rb.AddForce(direction * _throwImpulse, ForceMode.Impulse);
                }
            }
        }

        private void ConfigurePickable(GameObject dropped, InventoryComponent inventory, InventoryItemData itemData, int itemId,
            int amount)
        {
            PickableItem pickable = dropped.GetComponent<PickableItem>();
            if (pickable == null)
            {
                pickable = dropped.AddComponent<PickableItem>();
            }

            pickable.Configure(itemData, itemId, amount,
                _setTargetInventoryForPickable ? inventory : pickable.TargetInventory);
        }
    }
}
