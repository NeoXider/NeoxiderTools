using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Система «руки»: один выбранный предмет из инвентаря отображается в заданной точке (например рука).
    ///     Переключение влево/вправо по слотам с count &gt; 0. Интеграция с Selector для No-Code переключения.
    /// </summary>
    [NeoDoc("Tools/Inventory/InventoryHand.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/InventoryHand")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(InventoryHand))]
    public sealed class InventoryHand : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField]
        [Tooltip("Инвентарь. Если null и Auto Find включён — InventoryComponent.FindDefault().")]
        private InventoryComponent _inventory;

        [SerializeField]
        [Tooltip("Точка, в которой показывается модель выбранного предмета (например рука).")]
        private Transform _handAnchor;

        [SerializeField]
        [Tooltip("Опционально: Selector для переключения. Count синхронизируется с числом слотов, Next/Previous меняют выбранный слот.")]
        private Selector _selector;

        [SerializeField]
        [Tooltip("Разрешить автоматический поиск инвентаря, если не назначен.")]
        private bool _autoFindInventory = true;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Префаб по умолчанию, если у предмета нет WorldDropPrefab в базе.")]
        private GameObject _fallbackHandPrefab;

        [Header("Selector Sync")]
        [SerializeField]
        [Tooltip("При изменении инвентаря синхронизировать Selector.Count и текущий индекс (зажать в допустимых границах).")]
        private bool _syncSelectorOnInventoryChanged = true;

        [SerializeField]
        [Tooltip("При пустом инвентаре разрешить Selector индекс -1 (ничего не в руке).")]
        private bool _allowEmptySlot = true;

        [Header("Events")]
        public UnityEvent<int> OnEquippedChanged = new();

        [Tooltip("Вызывается при UseEquippedItem(). Передаётся itemId. Данные предмета — inventory.GetItemData(itemId) или EquippedItemData. Подпишите для эффекта; при расходе — TryConsume(itemId, 1).")]
        public UnityEvent<int> OnUseItemRequested = new();

        private int _slotIndex;
        private GameObject _spawnedInstance;

        public InventoryComponent Inventory => _inventory;
        public int SlotIndex => _slotIndex;
        public int EquippedItemId => ResolveInventory()?.GetItemIdAtSlotIndex(_slotIndex) ?? -1;
        public InventoryItemData EquippedItemData => ResolveInventory()?.GetItemData(EquippedItemId);

        private void OnEnable()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv != null)
            {
                inv.OnInventoryChanged.AddListener(OnInventoryChanged);
            }

            if (_selector != null)
            {
                _selector.OnSelectionChanged.AddListener(OnSelectorSelectionChanged);
            }

            RefreshSlotFromInventory();
        }

        private void OnDisable()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv != null)
            {
                inv.OnInventoryChanged.RemoveListener(OnInventoryChanged);
            }

            if (_selector != null)
            {
                _selector.OnSelectionChanged.RemoveListener(OnSelectorSelectionChanged);
            }
        }

        private void OnInventoryChanged()
        {
            if (!_syncSelectorOnInventoryChanged)
            {
                return;
            }

            RefreshSlotFromInventory();
        }

        private void OnSelectorSelectionChanged(int index)
        {
            SetSlotIndex(index);
        }

        /// <summary>
        ///     Перейти к следующему слоту (влево/вперёд по списку). С зацикливанием.
        /// </summary>
        public void SelectNext()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                return;
            }

            _slotIndex = (_slotIndex + 1) % count;
            ApplySlotAndSync();
        }

        /// <summary>
        ///     Перейти к предыдущему слоту (вправо/назад по списку). С зацикливанием.
        /// </summary>
        public void SelectPrevious()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                return;
            }

            _slotIndex = _slotIndex <= 0 ? count - 1 : _slotIndex - 1;
            ApplySlotAndSync();
        }

        /// <summary>
        ///     «Использовать» предмет в руке: вызывает OnUseItemRequested(itemId). Логику (трата предмета, эффект) реализуйте в подписчике; данные — GetItemData(itemId) или EquippedItemData.
        /// </summary>
        public void UseEquippedItem()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int itemId = EquippedItemId;
            if (itemId < 0)
            {
                return;
            }

            OnUseItemRequested?.Invoke(itemId);
        }

        /// <summary>
        ///     Установить выбранный слот по индексу (0 .. GetNonEmptySlotCount()-1).
        /// </summary>
        public void SetSlotIndex(int index)
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                _slotIndex = 0;
                SyncSelectorAndRefreshHand();
                return;
            }

            _slotIndex = Mathf.Clamp(index, 0, count - 1);
            ApplySlotAndSync();
        }

        /// <summary>
        ///     Обновить состояние из инвентаря: зажать индекс, синхронизировать Selector и отобразить предмет в руке.
        /// </summary>
        public void RefreshSlotFromInventory()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                DestroySpawned();
                SyncSelectorAndRefreshHand();
                return;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                _slotIndex = 0;
                inv.SelectedItemId = -1;
                DestroySpawned();
                SyncSelectorAndRefreshHand();
                return;
            }

            if (_slotIndex >= count)
            {
                _slotIndex = count - 1;
            }

            int itemId = inv.GetItemIdAtSlotIndex(_slotIndex);
            inv.SelectedItemId = itemId;
            SyncSelectorAndRefreshHand();
        }

        private void ApplySlotAndSync()
        {
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int count = inv.GetNonEmptySlotCount();
            if (count <= 0)
            {
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                return;
            }

            int itemId = inv.GetItemIdAtSlotIndex(_slotIndex);
            inv.SelectedItemId = itemId;
            SyncSelectorAndRefreshHand();
            OnEquippedChanged?.Invoke(itemId);
        }

        private void SyncSelectorAndRefreshHand()
        {
            InventoryComponent inv = ResolveInventory();
            int count = inv != null ? inv.GetNonEmptySlotCount() : 0;

            if (_selector != null)
            {
                _selector.Count = count > 0 ? count : (_allowEmptySlot ? 1 : 0);
                if (count > 0)
                {
                    _selector.Set(_slotIndex);
                }
                else if (_allowEmptySlot && _selector.Count == 1)
                {
                    _selector.Set(0);
                }
            }

            RefreshHandVisual();
        }

        private void RefreshHandVisual()
        {
            DestroySpawned();

            Transform anchor = _handAnchor != null ? _handAnchor : transform;
            InventoryComponent inv = ResolveInventory();
            if (inv == null)
            {
                return;
            }

            int itemId = inv.GetItemIdAtSlotIndex(_slotIndex);
            if (itemId < 0)
            {
                return;
            }

            InventoryItemData data = inv.GetItemData(itemId);
            GameObject prefab = data != null && data.WorldDropPrefab != null ? data.WorldDropPrefab : _fallbackHandPrefab;
            if (prefab == null)
            {
                return;
            }

            _spawnedInstance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            _spawnedInstance.transform.localPosition = Vector3.zero;
            _spawnedInstance.transform.localRotation = Quaternion.identity;
            _spawnedInstance.transform.localScale = Vector3.one;
        }

        private void DestroySpawned()
        {
            if (_spawnedInstance != null)
            {
                Destroy(_spawnedInstance);
                _spawnedInstance = null;
            }
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
    }
}
