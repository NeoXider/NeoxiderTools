using System;
using System.Collections;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Bonus
{
    /// <summary>
    ///     Displays the catalog of a <see cref="Collection" /> through <see cref="ItemCollection" /> views and raises
    ///     <see cref="OnSetItem" /> for selectable items. The first manager registers as
    ///     <c>CollectionVisualManager.I</c>; additional managers (one per panel) need <b>Set Instance On Awake</b> off,
    ///     otherwise the singleton destroys their GameObject.
    /// </summary>
    [CreateFromMenu("Neoxider/Bonus/CollectionVisualManager")]
    [AddComponentMenu("Neoxider/Bonus/" + nameof(CollectionVisualManager))]
    [NeoDoc("Bonus/Collection/CollectionVisualManager.md")]
    public class CollectionVisualManager : Singleton<CollectionVisualManager>
    {
        [SerializeField] [GetComponents(true)] private ItemCollection[] _items;

        [Tooltip("Collection to display. Empty uses the global Collection.I once it exists.")]
        [SerializeField]
        private Collection _collection;

        [SerializeField] private bool _enableSetItem = true;
        [SerializeField] private bool _debugLogWarnings;
        public UnityEvent<int> OnSetItem = new();

        private UnityAction[] _buttonActions;
        private bool _buttonsSubscribed;
        private Collection _subscribedCollection;
        private Collection _appliedCollection;
        private Coroutine _waitRoutine;
        private bool _started;
        private bool _initialized;
        private bool _inspectorRebindQueued;

        public ItemCollection[] Items => _items;
        public int ItemsCount => _items != null ? _items.Length : 0;

        public bool EnableSetItem
        {
            get => _enableSetItem;
            set => _enableSetItem = value;
        }

        /// <summary>
        ///     Collection this manager displays: the subscribed one, otherwise the assigned one, otherwise the global
        ///     <c>Collection.I</c> when it exists (null while none does). Assigning at runtime moves the subscription
        ///     to the new collection and refreshes the views; null returns to the global fallback.
        /// </summary>
        public Collection TargetCollection
        {
            get => _subscribedCollection != null ? _subscribedCollection : ResolveCollection();
            set
            {
                // WHY: ReferenceEquals, because a destroyed assignment compares equal to null with Unity's operator
                // and clearing it would then skip the rebind to the global collection.
                if (ReferenceEquals(_collection, value) && !HasDestroyedSubscription)
                {
                    return;
                }

                _collection = value;
                RebindCollection();
            }
        }

        private bool HasDestroyedSubscription =>
            !ReferenceEquals(_subscribedCollection, null) && _subscribedCollection == null;

        protected override void Awake()
        {
            if (SetInstanceOnAwakeEnabled && TryGetInstance(out CollectionVisualManager registered) && registered != this)
            {
                NeoDiagnostics.LogWarning(
                    $"[{nameof(CollectionVisualManager)}] Another manager is already registered, so this one destroys " +
                    "its GameObject. Turn off Set Instance On Awake on additional managers.", this, _debugLogWarnings);
            }

            base.Awake();
            // WHY: Singleton.Awake initializes only the instance it registers; an additional manager (Set Instance On
            // Awake off) must still wire its item buttons.
            if (!SetInstanceOnAwakeEnabled)
            {
                Init();
            }
        }

        private void Start()
        {
            _started = true;
            BindOrWait();
        }

        private void OnValidate()
        {
            // WHY: an Inspector edit of Collection in Play Mode bypasses the TargetCollection setter. The rebind redraws
            // the views, and their ItemCollection events usually call SetActive, which Unity rejects inside
            // OnValidate, so the rebind runs on the next editor update instead.
            if (!Application.isPlaying || !_started || _inspectorRebindQueued)
            {
                return;
            }

#if UNITY_EDITOR
            _inspectorRebindQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyInspectorCollection;
#endif
        }

        private void ApplyInspectorCollection()
        {
            _inspectorRebindQueued = false;
            if (this == null || !Application.isPlaying || !_started)
            {
                return;
            }

            if (!ReferenceEquals(_collection, _appliedCollection))
            {
                RebindCollection();
            }
        }

        private void OnEnable()
        {
            // WHY: deactivating the object stops the wait coroutine; without a restart a manager that was hidden
            // before its Collection appeared would never subscribe.
            if (_started && _subscribedCollection == null)
            {
                BindOrWait();
            }
        }

        private void OnDisable()
        {
            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }
        }

        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            if (_inspectorRebindQueued)
            {
                UnityEditor.EditorApplication.delayCall -= ApplyInspectorCollection;
                _inspectorRebindQueued = false;
            }
#endif

            Subscriber(false);
            Unsubscribe();
            base.OnDestroy();
        }

        /// <summary>
        ///     Replaces the item views from code, the C# path for the serialized Items array. The buttons of the previous
        ///     views are unwired, the new ones are wired once the manager has initialized (its Awake), and the views are
        ///     redrawn when a collection is already bound.
        /// </summary>
        public void SetItems(ItemCollection[] items)
        {
            Subscriber(false);
            _items = items != null ? (ItemCollection[])items.Clone() : Array.Empty<ItemCollection>();
            if (_initialized)
            {
                Subscriber(true);
            }

            if (_subscribedCollection != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private void RebindCollection()
        {
            if (!_started)
            {
                return;
            }

            Unsubscribe();
            if (_items != null)
            {
                // WHY: until the new collection is bound, ItemCollection.Unlock must not write into the one just left.
                for (int i = 0; i < _items.Length; i++)
                {
                    if (_items[i] != null)
                    {
                        _items[i].SetDisplayedCollection(null);
                    }
                }
            }

            BindOrWait();
        }

        private void BindOrWait()
        {
            _appliedCollection = _collection;
            if (TrySubscribeToCollection())
            {
                if (_waitRoutine != null)
                {
                    StopCoroutine(_waitRoutine);
                    _waitRoutine = null;
                }

                if (_items != null && _items.Length > 0)
                {
                    Visual();
                }

                return;
            }

            if (_waitRoutine == null && isActiveAndEnabled)
            {
                _waitRoutine = StartCoroutine(WaitForCollectionAndSubscribe());
            }
        }

        private Collection ResolveCollection()
        {
            if (_collection != null)
            {
                return _collection;
            }

            return Collection.IsInitialized ? Collection.I : null;
        }

        private bool TrySubscribeToCollection()
        {
            if (_subscribedCollection != null)
            {
                return true;
            }

            if (HasDestroyedSubscription)
            {
                Unsubscribe();
            }

            Collection collection = ResolveCollection();
            if (collection == null)
            {
                return false;
            }

            // WHY: a Collection whose events were cleared from code must not break the view.
            collection.OnItemAdded ??= new UnityEvent<int>();
            collection.OnItemRemoved ??= new UnityEvent<int>();
            collection.OnLoadItems ??= new UnityEvent();
            collection.OnItemAdded.AddListener(OnCollectionItemChanged);
            collection.OnItemRemoved.AddListener(OnCollectionItemChanged);
            collection.OnLoadItems.AddListener(OnCollectionLoaded);
            _subscribedCollection = collection;
            return true;
        }

        private void Unsubscribe()
        {
            // WHY: unsubscribe from the instance that was subscribed, not whichever Collection.I exists now.
            if (!ReferenceEquals(_subscribedCollection, null))
            {
                _subscribedCollection.OnItemAdded?.RemoveListener(OnCollectionItemChanged);
                _subscribedCollection.OnItemRemoved?.RemoveListener(OnCollectionItemChanged);
                _subscribedCollection.OnLoadItems?.RemoveListener(OnCollectionLoaded);
            }

            _subscribedCollection = null;
        }

        protected override void Init()
        {
            base.Init();
            _initialized = true;

            if (_items == null || _items.Length == 0)
            {
                LogWarning("Items array is empty.");
                return;
            }

            Subscriber(true);
        }

        private IEnumerator WaitForCollectionAndSubscribe()
        {
            while (!TrySubscribeToCollection())
            {
                yield return null;
            }

            _waitRoutine = null;
            if (_items != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private void OnCollectionLoaded()
        {
            if (_items != null && _items.Length > 0)
            {
                Visual();
            }
        }

        private void OnCollectionItemChanged(int id)
        {
            if (_items != null && id >= 0 && id < _items.Length)
            {
                UpdateItemVisibility(id);
            }
        }

        private void Subscriber(bool subscribe)
        {
            if (_items == null || subscribe == _buttonsSubscribed)
            {
                return;
            }

            _buttonsSubscribed = subscribe;

            if (subscribe)
            {
                if (_buttonActions == null || _buttonActions.Length != _items.Length)
                {
                    _buttonActions = new UnityAction[_items.Length];
                    for (int i = 0; i < _items.Length; i++)
                    {
                        int id = i;
                        _buttonActions[i] = () => SetItem(id);
                    }
                }
            }

            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null || _items[i].Button == null ||
                    _buttonActions == null || i >= _buttonActions.Length)
                {
                    continue;
                }

                if (subscribe)
                {
                    _items[i].Button.onClick.AddListener(_buttonActions[i]);
                }
                else
                {
                    _items[i].Button.onClick.RemoveListener(_buttonActions[i]);
                }
            }
        }

        [Button]
        public void Visual()
        {
            if (_items == null || _items.Length == 0)
            {
                return;
            }

            Collection collection = TargetCollection;
            if (collection == null)
            {
                LogWarning("Collection is not initialized yet.");
                return;
            }

            int count = Mathf.Min(_items.Length, collection.ItemCount);
            for (int i = 0; i < count; i++)
            {
                UpdateItemVisibility(collection, i);
            }

            for (int i = count; i < _items.Length; i++)
            {
                if (_items[i] != null)
                {
                    _items[i].SetDisplayedCollection(collection);
                }
            }
        }

        public void UpdateItemVisibility(int id)
        {
            Collection collection = TargetCollection;
            if (collection == null)
            {
                return;
            }

            UpdateItemVisibility(collection, id);
        }

        private void UpdateItemVisibility(Collection collection, int id)
        {
            if (_items == null || id < 0 || id >= _items.Length || _items[id] == null)
            {
                return;
            }

            _items[id].SetItemId(id);
            _items[id].SetDisplayedCollection(collection);

            ItemCollectionData itemData = collection.GetItemData(id);
            if (itemData != null)
            {
                _items[id].SetData(itemData);
            }

            _items[id].SetEnabled(collection.HasItem(id));
        }

        public void SetItem(int id)
        {
            if (id < 0 || _items == null || id >= _items.Length || _items[id] == null)
            {
                return;
            }

            Collection collection = TargetCollection;
            if (collection == null && _enableSetItem)
            {
                // WHY: the gate resolved Collection.I before the explicit field existed; keep that lookup (a scene
                // search that may initialize an inactive global Collection) for scenes that rely on it.
                collection = Collection.I;
            }

            if (!_enableSetItem || (collection != null && collection.HasItem(id)))
            {
                OnSetItem?.Invoke(id);
            }
        }

        public ItemCollection GetItem(int id)
        {
            if (_items == null || id < 0 || id >= _items.Length)
            {
                return null;
            }

            return _items[id];
        }

        [Button]
        public void RefreshAllItems()
        {
            Visual();
        }

        public void RefreshItem(int id)
        {
            UpdateItemVisibility(id);
        }

        public void SetItem(ItemCollection itemCollection)
        {
            if (_items == null || itemCollection == null)
            {
                return;
            }

            int id = Array.IndexOf(_items, itemCollection);
            SetItem(id);
        }

        private void LogWarning(string message)
        {
            if (_debugLogWarnings)
            {
                NeoDiagnostics.LogWarning($"[{nameof(CollectionVisualManager)}] {message}", this, true);
            }
        }
    }
}
