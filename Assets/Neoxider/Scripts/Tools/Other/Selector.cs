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
            [SerializeField] private bool _start0;
            [SerializeField] private bool _loop = true;

            [Space]
            [SerializeField] private int _currentIndex = 0;
            [SerializeField] private bool _changeDebug = true;

            public UnityEvent<int> OnSelectionChanged;

            private void Start()
            {
                if (_start0)
                    _currentIndex = 0;

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
                    Debug.LogError("items null");

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

                if (_changeDebug)
                    UpdateSelection();
            }
        }
    }
}