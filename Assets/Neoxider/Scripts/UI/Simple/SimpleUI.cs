using Neoxider;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace UI
    {
        public class SimpleUI : MonoBehaviour
        {
            [SerializeField] private GameObject[] _pages;
            [Min(-1)] public int id;

            [Space]
            [SerializeField] private float _timeAnim = 1.5f;
            [SerializeField] private Animator _animator;
            public static SimpleUI Instance;

            public UnityEvent<int> OnChangePage;

            private void Awake()
            {
                Instance = this;
                SetPage(0);
            }

            public void SetPage(int id)
            {
                this.id = id;
                _pages.SetActiveAll(false).SetActiveId(id, true);

                OnChangePage?.Invoke(id);
            }

            public void SetOnePage(int id)
            {
                _pages.SetActiveId(id, true);

                OnChangePage?.Invoke(id);
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

                yield return new WaitForSeconds(_timeAnim);

                if (one) SetOnePage(id);
                else SetPage(id);

                yield return new WaitForSeconds(_timeAnim);

                if (_animator != null) _animator.gameObject.SetActive(false);
            }

            private void OnValidate()
            {
                SetPage(id);
            }
        }
    }
}
