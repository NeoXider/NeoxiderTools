using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Tools
    {
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Selector))]
        public class Selector : MonoBehaviour
        {

            [SerializeField] private GameObject[] _items;
            [Header("ButtonAutoSetChilds")]
            [SerializeField] private bool _setChild = false;

            [Space]
            [SerializeField, Min(-1)] private int _startId = -1;
            [SerializeField] private bool _loop = true;

            [Space]
            [SerializeField] private int _currentIndex = 0;
            [SerializeField] private bool _changeDebug = true;

            public UnityEvent<int> OnSelectionChanged;
            public UnityEvent OnFinished;

            private void Awake()
            {
                if (_startId != -1)
                    _currentIndex = _startId;
            }

            private void Start()
            {
                UpdateSelection();
            }

            private void UpdateSelection()
            {
                _items.SetActiveAll(false).SetActiveId(_currentIndex, true);

                OnSelectionChanged?.Invoke(_currentIndex);
            }

            public void SelectNext()
            {
                _currentIndex++;

                if (_currentIndex >= _items.Length)
                {
                    _currentIndex = _loop ? 0 : _items.Length - 1;
                    OnFinished?.Invoke();
                }

                UpdateSelection();
            }

            public void SelectPrevious()
            {
                _currentIndex--;
                if (_currentIndex < 0)
                {
                    _currentIndex = _loop ? _items.Length - 1 : 0;
                }

                UpdateSelection();
            }

            public int GetCurrentIndex()
            {
                return _currentIndex;
            }

            public int GetCount()
            {
                return _items.Length;
            }

            public void SetCurrentIndex(int index)
            {
                if (index >= 0 && index < _items.Length)
                {
                    _currentIndex = index;
                    UpdateSelection();
                }
            }

            public void SetLast()
            {
                _currentIndex = _items.Length - 1;
                UpdateSelection();
            }

            public void SetFirst()
            {
                _currentIndex = 0;
                UpdateSelection();
            }

            private void OnValidate()
            {
                if (_items == null)
                {
                    Debug.LogWarning("items null");
                }

                if (_setChild)
                {
                    _setChild = false;
                    List<GameObject> childs = new List<GameObject>();

                    foreach (Transform child in transform)
                    {
                        if (child.gameObject != gameObject)
                        {
                            childs.Add(child.gameObject);
                        }
                    }

                    _items = childs.ToArray();
                }

                if (_changeDebug && _items != null)
                    UpdateSelection();
            }
        }
    }
}