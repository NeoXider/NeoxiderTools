using System.Collections;
using System.Collections.Generic;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.UI
{
        [NeoDoc("UI/UI.md")]
        [CreateFromMenu("Neoxider/UI/UI")]
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(UI))]
        public class UI : MonoBehaviour
        {
            public static UI I;

            // WHY: I keeps the previous session's destroyed component when Enter Play Mode Options
            // disable domain reload, so page switching hits a MissingReferenceException.
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void ResetStaticState()
            {
                I = null;
            }

            [SerializeField] private GameObject[] _pages = System.Array.Empty<GameObject>();

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

            [Tooltip("Count the page delays in unscaled time, so SetPageAnim/SetOnePageAnim/SetPageDelay still " +
                     "switch pages while Time.timeScale is 0 (pause menus). Set the curtain Animator's " +
                     "Update Mode to Unscaled Time as well.")]
            [SerializeField]
            private bool _useUnscaledTime = true;

            public UnityEvent<int> OnChangePage;
            public UnityEvent OnStartPage;

            private Dictionary<int, GameObject> _pageCache;

            /// <summary>
            ///     When true the delays of <see cref="SetPageAnim" />, <see cref="SetOnePageAnim" /> and
            ///     <see cref="SetPageDelay" /> ignore <see cref="Time.timeScale" />.
            /// </summary>
            public bool UseUnscaledTime
            {
                get => _useUnscaledTime;
                set => _useUnscaledTime = value;
            }

            private void Awake()
            {
                I = this;
                BuildPageCache();

                // WHY: startId is the authored start page; gating on the runtime id field made
                // startId=-1 (None) deactivate every page and skipped valid start pages when id=-1.
                if (startId >= 0)
                {
                    SetPage(startId);
                }
            }

            private void OnValidate()
            {
                if (!Application.isPlaying && is_debug_change)
                {
                    SetPage(id);
                }

                if (_setChild)
                {
                    _setChild = false;

                    List<GameObject> childs = new();

                    Transform parent = _parentPages != null ? _parentPages.transform : transform;

                    foreach (Transform child in parent)
                    {
                        if (child.gameObject != gameObject)
                        {
                            childs.Add(child.gameObject);
                        }
                    }

                    _pages = childs.ToArray();
                }
            }

            private void BuildPageCache()
            {
                _pages ??= System.Array.Empty<GameObject>();
                _pageCache = new Dictionary<int, GameObject>(_pages.Length);
                for (int i = 0; i < _pages.Length; i++)
                {
                    if (_pages[i] != null)
                    {
                        _pageCache[i] = _pages[i];
                    }
                }
            }

            private void OnDestroy()
            {
                if (ReferenceEquals(I, this))
                {
                    I = null;
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

                foreach (GameObject page in _pages)
                {
                    if (page != null)
                    {
                        page.SetActive(false);
                    }
                }

                if (_pageCache != null && _pageCache.TryGetValue(id, out GameObject targetPage) && targetPage != null)
                {
                    targetPage.SetActive(true);
                }
                else if (id >= 0 && id < _pages.Length && _pages[id] != null)
                {
                    _pages[id].SetActive(true);
                }

                OnChangePage?.Invoke(id);

                if (id == 0)
                {
                    OnStartPage?.Invoke();
                }
            }

            [Button]
            public void SetOnePage(int id)
            {
                _pages.SetActiveAtIndex(id, false);
                _pages.SetActiveAtIndex(id);

                OnChangePage?.Invoke(id);
            }

            public void SetPageDelay(int id)
            {
                this.id = id;
                if (_useUnscaledTime)
                {
                    StartCoroutine(SetPageDelayCoroutine());
                    return;
                }

                Invoke(nameof(SetPage), _timeDelay);
            }

            private IEnumerator SetPageDelayCoroutine()
            {
                yield return CreateDelay();
                SetPage();
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

                // WHY: scaled waits never finished under a PausePage (time scale 0), so a page switched from a
                // pause menu with an animated transition never appeared.
                yield return CreateDelay();

                if (one)
                {
                    SetOnePage(id);
                }
                else
                {
                    SetPage(id);
                }

                yield return CreateDelay();

                if (_animator != null)
                {
                    _animator.gameObject.SetActive(false);
                }
            }

            private object CreateDelay()
            {
                return _useUnscaledTime ? new WaitForSecondsRealtime(_timeDelay) : new WaitForSeconds(_timeDelay);
            }

            /// <summary>Toggles the currently selected page without changing the selection.</summary>
            public void SetCurrentPage(bool active)
            {
                if (id >= 0 && id < _pages.Length && _pages[id] != null)
                {
                    _pages[id].SetActive(active);
                }
            }

            /// <summary>Legacy misspelled alias kept for existing UnityEvent wiring.</summary>
            public void SetCurrtentPage(bool active)
            {
                SetCurrentPage(active);
            }
        }
}
