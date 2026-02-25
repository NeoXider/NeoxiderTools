using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Режим масштаба предмета в руке: фиксированное значение или относительное (1 + offset).
    /// </summary>
    public enum HandScaleMode
    {
        Fixed,
        Relative
    }

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

        [SerializeField]
        [Tooltip("Опционально: InventoryDropper для DropEquipped(). Если задан, выбросить предмет в руке можно через него (префаб, физика, подбор).")]
        private InventoryDropper _dropper;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Префаб по умолчанию, если у предмета нет WorldDropPrefab в базе.")]
        private GameObject _fallbackHandPrefab;

        [SerializeField]
        [Tooltip("Режим масштаба: Fixed — множитель (Hand Scale Fixed), Relative — дельта 1 + Hand Scale Offset. При использовании HandView на предметах удобнее Relative (дельта поверх масштаба вьюшки).")]
        private HandScaleMode _scaleInHandMode = HandScaleMode.Relative;

        [SerializeField]
        [Tooltip("Фиксированный масштаб в руке (например 1 или 0.5). Используется при Scale In Hand Mode = Fixed.")]
        private float _handScaleFixed = 1f;

        [SerializeField]
        [Tooltip("Смещение масштаба в руке: итог = 1 + offset (например −0.5 → 0.5). Используется при Scale In Hand Mode = Relative.")]
        private float _handScaleOffset;

        [SerializeField]
        [Tooltip("При true у предмета в руке отключаются все коллайдеры (Collider/Collider2D на объекте и детях), чтобы не толкать и не участвовать в столкновениях. По умолчанию включено.")]
        private bool _disableCollidersInHand = true;

        [Header("Selector Sync")]
        [SerializeField]
        [Tooltip("При изменении инвентаря синхронизировать Selector.Count и текущий индекс (зажать в допустимых границах).")]
        private bool _syncSelectorOnInventoryChanged = true;

        [SerializeField]
        [Tooltip("При пустом инвентаре — пустой слот (Count=1, ничего не в руке). При наличии предметов — разрешить индекс -1 в Selector (ничего не в руке); включите у Selector Allow Empty Effective Index.")]
        private bool _allowEmptySlot = true;

        [Header("Drop (when Dropper assigned)")]
        [SerializeField]
        [Tooltip("По нажатию клавиши дропа (например G) сбрасывать предмет из руки через Dropper. У назначенного Dropper ввод по клавише временно отключается, чтобы дроп обрабатывала только рука.")]
        private bool _allowDropInput = true;

        [SerializeField]
        [Tooltip("Клавиша выброса предмета из руки.")]
        private KeyCode _dropKey = KeyCode.G;

        [Header("Use")]
        [SerializeField]
        [Tooltip("По нажатию клавиши применения вызывать UseEquippedItem().")]
        private bool _allowUseInput = true;

        [SerializeField]
        [Tooltip("Клавиша применения предмета в руке.")]
        private KeyCode _useKey = KeyCode.E;

        [Header("Events")]
        public UnityEvent<int> OnEquippedChanged = new();

        [Tooltip("Вызывается при UseEquippedItem(). Передаётся itemId. Данные предмета — inventory.GetItemData(itemId) или EquippedItemData. Подпишите для эффекта; при расходе — TryConsume(itemId, 1).")]
        public UnityEvent<int> OnUseItemRequested = new();

        private int _slotIndex;
        private GameObject _spawnedInstance;
        private bool _isSyncingSelector;
        private bool _savedDropperAllowInput = true;

        public InventoryComponent Inventory => _inventory;
        public int SlotIndex => _slotIndex;

        /// <summary>ItemId предмета в руке (−1 если пусто). Для NeoCondition: Source = Component → InventoryHand, Property = EquippedItemId.</summary>
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

            if (_dropper != null)
            {
                _savedDropperAllowInput = _dropper.AllowDropInput;
                _dropper.AllowDropInput = false;
            }

            RefreshSlotFromInventory();
        }

        private void OnDisable()
        {
            if (_dropper != null)
            {
                _dropper.AllowDropInput = _savedDropperAllowInput;
            }

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

        private void Update()
        {
            if (_allowUseInput && KeyInputCompat.GetKeyDown(_useKey))
            {
                UseEquippedItem();
                return;
            }

            if (_dropper == null || !_allowDropInput || !KeyInputCompat.GetKeyDown(_dropKey))
            {
                return;
            }

            DropEquipped(1);
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
            if (_isSyncingSelector) return;
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
        ///     «Применить» предмет в руке: вызывает OnUseItemRequested(itemId), затем у экземпляра в руке — PickableItem.Activate() (если есть). Вызывайте из кода/кнопки или по клавише Use (E по умолчанию).
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

            if (_spawnedInstance != null)
            {
                PickableItem pickable = _spawnedInstance.GetComponent<PickableItem>();
                pickable?.Activate();
            }
        }

        /// <summary>
        ///     Выбросить предмет в руке через InventoryDropper (если назначен). Возвращает количество выброшенных, 0 если нет предмета, нет Dropper или дроп запрещён.
        /// </summary>
        public int DropEquipped(int amount = 1)
        {
            if (_dropper == null || amount <= 0)
            {
                return 0;
            }

            int itemId = EquippedItemId;
            if (itemId < 0)
            {
                return 0;
            }

            return _dropper.DropById(itemId, amount);
        }

        /// <summary>
        ///     Установить выбранный слот по индексу. При Allow Empty Slot допустим индекс -1 (ничего не в руке).
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

            if (index == -1 && _allowEmptySlot)
            {
                _slotIndex = -1;
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                OnEquippedChanged?.Invoke(-1);
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

            if (_slotIndex < 0)
            {
                inv.SelectedItemId = -1;
                SyncSelectorAndRefreshHand();
                OnEquippedChanged?.Invoke(-1);
                return;
            }

            int itemId = inv.GetItemIdAtSlotIndex(_slotIndex);
            inv.SelectedItemId = itemId;
            SyncSelectorAndRefreshHand();
            OnEquippedChanged?.Invoke(itemId);
        }

        private void SyncSelectorAndRefreshHand()
        {
            if (_isSyncingSelector) return;
            _isSyncingSelector = true;
            try
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
            finally
            {
                _isSyncingSelector = false;
            }
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

            HandView handView = _spawnedInstance.GetComponentInChildren<HandView>(true);
            float baseScale = handView != null ? handView.ScaleInHand : 1f;
            if (handView != null)
            {
                _spawnedInstance.transform.localPosition = handView.PositionOffset;
                _spawnedInstance.transform.localRotation = Quaternion.Euler(handView.RotationOffset);
            }
            else
            {
                _spawnedInstance.transform.localPosition = Vector3.zero;
                _spawnedInstance.transform.localRotation = Quaternion.identity;
            }

            float handScale = _scaleInHandMode == HandScaleMode.Relative ? (1f + _handScaleOffset) : _handScaleFixed;
            _spawnedInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, baseScale * handScale);

            SetPhysicsOnInstance(_spawnedInstance, false);
            if (_disableCollidersInHand)
            {
                SetCollidersOnInstance(_spawnedInstance, false);
            }
        }

        private static void SetPhysicsOnInstance(GameObject instance, bool enabled)
        {
            if (instance == null) return;
            Rigidbody[] rbs = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++)
            {
                rbs[i].isKinematic = !enabled;
            }
            Rigidbody2D[] rb2ds = instance.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < rb2ds.Length; i++)
            {
                rb2ds[i].simulated = enabled;
            }
        }

        private static void SetCollidersOnInstance(GameObject instance, bool enabled)
        {
            if (instance == null) return;
            Collider[] cols = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i].enabled = enabled;
            }
            Collider2D[] cols2D = instance.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols2D.Length; i++)
            {
                cols2D[i].enabled = enabled;
            }
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
