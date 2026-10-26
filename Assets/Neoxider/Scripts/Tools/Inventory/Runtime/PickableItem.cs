using UnityEngine;
using System;
using System.Runtime.ExceptionServices;
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

        [Header("Inventory")]
        [SerializeField]
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

        [Header("Collector Validation")]
        [SerializeField]
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
        [Tooltip(
            "Invoked when the item is activated (the hand calls Activate() on use). Subscribe for in-hand use effects.")]
        public UnityEvent OnActivate = new();

        [Header("Events")] public UnityEvent OnCollectStarted = new();
        public UnityEvent<int, int> OnCollected = new();
        public UnityEvent OnCollectFailed = new();
        public UnityEvent OnAfterCollectDespawn = new();

        private bool _wasCollected;
        private bool _isCollecting;
        [SerializeField] [HideInInspector]
        private InventoryItemInstance _storedInstance;

        public int ResolvedItemId => _itemData != null ? _itemData.ItemId : _itemId;
        public int Amount => _amount;
        public InventoryComponent TargetInventory => _targetInventory;

        /// <summary>Copy of the remaining dropped instance, or null for a simple/fully collected pickup.</summary>
        public InventoryItemInstance StoredInstance
        {
            get
            {
                if (_storedInstance == null || _amount <= 0)
                {
                    return null;
                }

                InventoryItemInstance snapshot = _storedInstance.Clone();
                snapshot.Count = _amount;
                return snapshot;
            }
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
        ///     Triggers item activation (e.g. when used in hand). Invokes OnActivate.
        /// </summary>
        public void Activate()
        {
            OnActivate?.Invoke();
        }

        public void Configure(InventoryItemData itemData, int fallbackItemId, int amount,
            InventoryComponent targetInventory = null)
        {
            _itemData = itemData;
            _itemId = itemData != null ? itemData.ItemId : fallbackItemId;
            _amount = Mathf.Max(1, amount);
            _targetInventory = targetInventory;
            _wasCollected = false;
            _storedInstance = null;
        }

        /// <summary>Configures and restores a dropped instance without replacing its identity.</summary>
        public void ConfigureInstance(InventoryItemData itemData, InventoryItemInstance instance,
            InventoryComponent targetInventory = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (itemData != null && itemData.ItemId != instance.ItemId)
            {
                throw new ArgumentException("Item data and instance ids must match.", nameof(itemData));
            }

            Configure(itemData, instance.ItemId, instance.Count, targetInventory);
            _storedInstance = instance.Clone();
            InventoryItemStateUtility.RestoreInstance(gameObject, _storedInstance);
        }

        private bool TryCollect(GameObject collector)
        {
            if (_isCollecting || (_collectOnlyOnce && _wasCollected))
            {
                return false;
            }

            _isCollecting = true;
            try
            {
                return TryCollectCore(collector);
            }
            finally
            {
                _isCollecting = false;
            }
        }

        private bool TryCollectCore(GameObject collector)
        {
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
            int requested = _amount;
            InventoryItemData data = _itemData != null ? _itemData : inventory.GetItemData(itemId);
            bool useInstanceState = _storedInstance != null || (data != null && data.SupportsInstanceState) ||
                                    InventoryItemStateUtility.HasState(gameObject);
            int added = 0;
            ExceptionDispatchInfo firstFault = null;
            if (useInstanceState)
            {
                for (int i = 0; i < requested; i++)
                {
                    InventoryComponent.MutationReceipt receipt;
                    try
                    {
                        receipt = inventory.CommitInstanceAdd(CapturePickupInstance(itemId));
                    }
                    catch (Exception exception)
                    {
                        firstFault = ExceptionDispatchInfo.Capture(exception);
                        break;
                    }

                    added += receipt.AcceptedCount;
                    CommitCollectedAmount(requested, added);
                    receipt.Publish();
                    if (receipt.Fault != null)
                    {
                        firstFault = receipt.Fault;
                        break;
                    }

                    if (receipt.AcceptedCount <= 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                InventoryComponent.MutationReceipt receipt = inventory.CommitItemAdd(itemId, requested);
                added = receipt.AcceptedCount;
                CommitCollectedAmount(requested, added);
                receipt.Publish();
                firstFault = receipt.Fault;
            }

            if (added <= 0)
            {
                firstFault?.Throw();
                OnCollectFailed?.Invoke();
                return false;
            }

            bool fullyCollected = _amount == 0;
            try
            {
                OnCollected?.Invoke(itemId, added);
            }
            catch (Exception exception)
            {
                if (firstFault == null)
                {
                    firstFault = ExceptionDispatchInfo.Capture(exception);
                }
            }

            if (fullyCollected && this != null)
            {
                try
                {
                    if (_disableCollidersOnCollect)
                    {
                        DisableAllColliders();
                    }

                    if (_destroyAfterCollect || _deactivateAfterCollect)
                    {
                        OnAfterCollectDespawn?.Invoke();
                    }
                }
                catch (Exception exception)
                {
                    if (firstFault == null)
                    {
                        firstFault = ExceptionDispatchInfo.Capture(exception);
                    }
                }
                finally
                {
                    if (this != null)
                    {
                        if (_destroyAfterCollect)
                        {
                            Destroy(gameObject);
                        }
                        else if (_deactivateAfterCollect)
                        {
                            gameObject.SetActive(false);
                        }
                    }
                }
            }

            // Explicit reusable grant sources replenish only after a fault-free completed request.
            // A fault must never turn already accepted world quantity into a second grant.
            if (fullyCollected && !_collectOnlyOnce && !_destroyAfterCollect && !_deactivateAfterCollect &&
                firstFault == null)
            {
                _amount = requested;
                _wasCollected = false;
            }

            firstFault?.Throw();
            return true;
        }

        private void CommitCollectedAmount(int requested, int accepted)
        {
            _amount = Math.Max(0, requested - accepted);
            _wasCollected = _amount == 0;
        }

        private InventoryItemInstance CapturePickupInstance(int itemId)
        {
            InventoryItemInstance captured = InventoryItemStateUtility.CaptureInstance(gameObject, itemId);
            if (_storedInstance == null)
            {
                return captured;
            }

            InventoryItemInstance instance = _storedInstance.Clone();
            instance.Count = 1;
            for (int i = 0; i < captured.ComponentStates.Count; i++)
            {
                InventoryItemComponentState state = captured.ComponentStates[i];
                int index = instance.ComponentStates.FindIndex(existing => existing.Key == state.Key);
                if (index < 0)
                {
                    instance.ComponentStates.Add(state);
                }
                else
                {
                    instance.ComponentStates[index] = state;
                }
            }

            return instance;
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
            Collider[] colliders3D = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders3D.Length; i++)
            {
                colliders3D[i].enabled = false;
            }

            Collider2D[] colliders2D = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders2D.Length; i++)
            {
                colliders2D[i].enabled = false;
            }
        }
    }
}
