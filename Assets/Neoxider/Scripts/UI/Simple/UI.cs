using Neo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace UI
    {
        public class UI : MonoBehaviour
        {
            [SerializeField] private GameObject[] _pages;

            [Header("ButtonAutoSetChilds")]
            [SerializeField] private bool _setChild = false;

            [Header("Current page (-1 = None, 0 - StartPage)")]
            [Min(-1)] public int id;

            [Space]
            [SerializeField] private float _timeDelay = 1.5f;
            [SerializeField] private Animator _animator;
            public static UI Instance;

            public UnityEvent<int> OnChangePage;
            public UnityEvent OnStartPage;

            private void Awake()
            {
                Instance = this;
                SetPage(0);
            }

            public void SetPage()
            {
                SetPage(id);
            }

            public void SetPage(int id)
            {
                this.id = id;
                _pages.SetActiveAll(false).SetActiveId(id, true);

                OnChangePage?.Invoke(id);

                if(id == 0)
                {
                    OnStartPage?.Invoke(); 
                }
            }

            public void SetOnePage(int id)
            {
                _pages.SetActiveId(id, false);
                _pages.SetActiveId(id, true);

                OnChangePage?.Invoke(id);
            }

            public void SetPageDelay(int id)
            {
                this.id = id;
                Invoke(nameof(SetPage), _timeDelay);
            }

            public void SetPageAnim(int id)
            {
                StartCoroutine(SetPageAnimCoroutine(id));
            }

            public void SetOnePageAnim(int id)
            {
                StartCoroutine(SetPageAnimCoroutine(id, true));
            }

            private IEnumerator SetPageAnimCoroutine(int id, bool one = false)
            {
                if (_animator != null)
                {
                    _animator.gameObject.SetActive(false);
                    _animator.gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(_timeDelay);

                if (one) SetOnePage(id);
                else SetPage(id);

                yield return new WaitForSeconds(_timeDelay);

                if (_animator != null) _animator.gameObject.SetActive(false);
            }

            public void SetLastPage(bool active)
            {
                _pages[id].SetActive(active);
            }

            private void OnValidate()
            {
                if(!Application.isPlaying)
                    SetPage(id);

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

                    _pages = childs.ToArray();
                }
            }
        }
    }
}
