using System.Collections.Generic;
using UnityEngine;

namespace Flexalon
{
    /// <summary>
    /// Sometimes, it's useful to generate child objects instead of defining them statically.
    /// The Flexalon Cloner can generate objects from a set of prefabs iteratively or randomly,
    /// and can optionally bind to a data source.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Cloner"), HelpURL("https://www.flexalon.com/docs/cloner")]
    public class FlexalonCloner : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> _objects;
        /// <summary> Prefabs which should be cloned as children. </summary>
        public List<GameObject> Objects
        {
            get => _objects;
            set { _objects = value; MarkDirty(); }
        }

        /// <summary> In which order should prefabs be cloned. </summary>
        public enum CloneTypes
        {
            /// <summary> Clone prefabs in the order they are assigned. </summary>
            Iterative,

            /// <summary> Clone prefabs in a random order. </summary>
            Random
        }

        [SerializeField]
        private CloneTypes _cloneType = CloneTypes.Iterative;
        /// <summary> In which order should prefabs be cloned. </summary>
        public CloneTypes CloneType
        {
            get => _cloneType;
            set { _cloneType = value; MarkDirty(); }
        }

        [SerializeField]
        private uint _count;
        /// <summary> How many clones should be generated. </summary>
        public uint Count
        {
            get => _count;
            set { _count = value; MarkDirty(); }
        }

        [SerializeField]
        private int _randomSeed;
        /// <summary> Seed used for the Random clone type, to ensure results remain consistent. </summary>
        public int RandomSeed
        {
                get => _randomSeed;
                set { _randomSeed = value; MarkDirty(); }
        }

        [SerializeField]
        private GameObject _dataSource = null;
        /// <summary> Can be an gameObject with a component that implements FlexalonDataSource.
        /// The number of objects cloned is set to the number of items in the Data property. </summary>
        public GameObject DataSource
        {
            get => _dataSource;
            set
            {
                UnhookDataSource();
                _dataSource = value;
                HookDataSource();
                MarkDirty();
            }
        }

        [SerializeField, HideInInspector]
        private List<GameObject> _clones = new List<GameObject>();

        void OnEnable()
        {
            HookDataSource();
            MarkDirty();
        }

        private void HookDataSource()
        {
            if (isActiveAndEnabled && _dataSource != null && _dataSource)
            {
                if (_dataSource.TryGetComponent<DataSource>(out var component))
                {
                    component.DataChanged += MarkDirty;
                }
            }
        }

        private void UnhookDataSource()
        {
            if (_dataSource != null && _dataSource)
            {
                if (_dataSource.TryGetComponent<DataSource>(out var component))
                {
                    component.DataChanged -= MarkDirty;
                }
            }
        }

        void OnDisable()
        {
            UnhookDataSource();
            MarkDirty();
        }

        /// <summary> Forces the cloner to regenerate its clones. </summary>
        public void MarkDirty()
        {
            foreach(var clone in _clones)
            {
                if (Application.isPlaying)
                {
                    Destroy(clone);
                }
                else
                {
                    DestroyImmediate(clone);
                }
            }

            _clones.Clear();

            if (isActiveAndEnabled && _objects != null && _objects.Count > 0)
            {
                switch (_cloneType)
                {
                    case CloneTypes.Iterative:
                        GenerateIterativeClones();
                        break;
                    case CloneTypes.Random:
                        GenerateRandomClones();
                        break;
                }
            }
        }

        private IReadOnlyList<object> GetData()
        {
            if (_dataSource != null && _dataSource)
            {
                return _dataSource.GetComponent<DataSource>()?.Data;
            }

            return null;
        }

        private void GenerateIterativeClones()
        {
            int i = 0;
            var data = GetData();
            var count = data?.Count ?? (int)_count;
            while (_clones.Count < count)
            {
                GenerateClone(i, data);
                i = (i + 1) % _objects.Count;
            }
        }

        private void GenerateRandomClones()
        {
            var random = new System.Random(_randomSeed);
            var data = GetData();
            var count = data?.Count ?? (int)_count;
            while (_clones.Count < count)
            {
                GenerateClone(random.Next(_objects.Count), data);
            }
        }

        private void GenerateClone(int index, IReadOnlyList<object> data)
        {
            var clone = Instantiate(_objects[index], Vector3.zero, Quaternion.identity, transform);
            _clones.Add(clone);

            if (data != null && clone.TryGetComponent<DataBinding>(out var dataBinding))
            {
                dataBinding.SetData(data[_clones.Count - 1]);
            }
        }
    }
}