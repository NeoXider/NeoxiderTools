using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace UI
    {
        public class UI : MonoBehaviour
        {
            public static UI I;
            [SerializeField] private GameObject[] _pages;

            [Header("ButtonAutoSetChilds")] [SerializeField]
            private bool _setChild;

            [SerializeField] private Transform _parentPages;

            [Header("Current page (-1 = None, ActivePage)")] [Min(-1)]
            public int id;

            public bool is_debug_change;

            [Space] [Header("On Load Active Page (-1 = None, StartPage)")] [Min(-1)]
            public int startId;

            [Space] [SerializeField] private float _timeDelay = 1.5f;
            [SerializeField] private Animator _animator;

            public UnityEvent<int> OnChangePage;
            public UnityEvent OnStartPage;

            private void Awake()
            {
                I = this;

                if (id >= 0)
                    SetPage(startId);
            }

            private void OnValidate()
            {
                if (!Application.isPlaying && is_debug_change)
                    SetPage(id);

                if (_setChild)
                {
                    _setChild = false;

                    var childs = new List<GameObject>();

                    var parent = _parentPages != null ? _parentPages.transform : transform;

                    foreach (Transform child in parent)
                        if (child.gameObject != gameObject)
                            childs.Add(child.gameObject);

                    _pages = childs.ToArray();
                }
            }

            public void SetPage()
            {
                SetPage(id);
            }

            [Button]
            public void SetPage(int id)
            {
                this.id = id;
                _pages.SetActiveAll(false).SetActiveAtIndex(id, true);

                OnChangePage?.Invoke(id);

                if (id == 0) OnStartPage?.Invoke();
            }

            [Button]
            public void SetOnePage(int id)
            {
                _pages.SetActiveAtIndex(id, false);
                _pages.SetActiveAtIndex(id, true);

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

            public void SetCurrtentPage(bool active)
            {
                _pages[id].SetActive(active);
            }
        }
    }
}