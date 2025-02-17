//v.1.0.5
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" +"Page/" + nameof(PagesManager))]
        public interface IScreenManagerSubscriber
        {
            void OnChangePage(Page newPage);
        }

        public class PagesManager : MonoBehaviour
        {
            public static PagesManager instance;

            //�������� ��������
            [Header("Pages Used")]
            public Page activPage;
            public Page lastPage;

            [Header("All Pages")]
            [SerializeField]
            private Page[] _pages;

            [Space]

            //������������� �������� (������ �������� ��� ������ ���������)
            [Header("Ignore Page Type")]
            private PageType[] _ignorePageTypes;
            private bool _ignorePageActiv;

            //��� ��������� ������������ �������� ��� ����� � ����������
            [Header("Page last with Change")]
            [SerializeField]
            private PageType[] _onePageTypes;

            [SerializeField]
            private bool _lastPageActiv = true;

            //��������� ������ 1 �������� � ���������� ���������
            [Header("Page with Only Set")]

            [SerializeField]
            private PageType[] _setPageTypes;

            //������������� ��������� �������
            [Header("Pages Together")]
            // �� �������

            //��������� ���������
            [Header("Start Settings")]

            [SerializeField]
            private PageType _startPage = PageType.Menu;

            [Header("Page Change Event")]
            public UnityEvent<Page> OnPageChanged;

            [SerializeField]
            private bool _pageNoneActiv = false;

            [Header("Editor Only")]

            [SerializeField]
            private bool _refresh = true;

            [SerializeField]
            private PageType _activPageEditor;

            [SerializeField]
            private bool _selectPage = true;


            private void Awake()
            {
                if (instance == null)
                {
                    instance = this;
                }
                else
                {
                    Destroy(this);
                }
            }

            private void Start()
            {
                SetPage(_startPage);
                lastPage = null;
            }

            public void ChangePage(PageType pageType, bool lastPageDisable = true)
            {
                if (Array.Exists(_setPageTypes, element => element == pageType))
                {
                    SetPage(pageType);
                }

                if (activPage != null && pageType == activPage.pageType)
                    return;

                Debug.Log("ChangePage - "+ pageType.ToString().AddColor(ColorHTML.yellow, true));

                lastPage = activPage;

                if (pageType == PageType.None)
                {
                    PagesActivate(PageType.None, _pageNoneActiv);
                    activPage = null;
                    return;
                }

                activPage = FindPage(pageType);

                if (activPage == null)
                {
                    Debug.LogError("Change page null PageType: " + pageType.ToString());
                }

                if (lastPage != null)
                    if (lastPageDisable && lastPage.gameObject.activeSelf)
                        if (Array.Exists(_onePageTypes, element => element == activPage.pageType))
                            SetActiv(lastPage, _lastPageActiv);
                        else
                            SetActiv(lastPage, false);

                if (!activPage.gameObject.activeSelf)
                    SetActiv(activPage, true);

                OnPageChanged?.Invoke(activPage);
            }

            public void SwitchLastPage(bool lastPageActiv = false)
            {
                Debug.Log("<color=yellow>SwitchLastPage - " + "</color><color=yellow>" + lastPage.pageType + "</color>");

                Page _page = lastPage;
                lastPage = activPage;
                activPage = _page;
                SetActiv(lastPage, lastPageActiv);
                SetActiv(activPage, true);

                OnPageChanged?.Invoke(activPage);
            }

            public void SetPage(PageType page)
            {
                Debug.Log("SetPage - " + "<color=red>" + page + "</color>");
                lastPage = activPage;
                activPage = PagesActivate(page);
            }

            public Page FindPage(PageType page)
            {
                Page _page = null;

                foreach (var item in _pages)
                {
                    if (item.pageType == page)
                    {
                        _page = item;
                        break;
                    }
                }

                return _page;
            }

            public Page PagesActivate(PageType targetPage, bool activ = true,
                PageType[] ignorPage = null, bool ignorActiv = false, bool otherActiv = false)
            {
                if (ignorPage == null)
                {
                    ignorPage = new PageType[] { PageType.None };
                }

                Page _page = null;

                foreach (var item in _pages)
                {
                    if (item.pageType == targetPage)
                    {
                        _page = item;
                        SetActiv(_page, activ);
                        continue;
                    }
                    else
                    {
                        if (Array.Exists(ignorPage, t => t == item.pageType))
                        {
                            SetActiv(item, ignorActiv);
                        }
                        else
                        {
                            SetActiv(item, otherActiv);
                        }
                    }
                }

                SetActivIgnorePageSettings();
                OnPageChanged?.Invoke(_page);
                return _page;
            }

            public static Page[] FindAllPages()
            {
                var _allPages = Resources.FindObjectsOfTypeAll<Page>();
                var _scenePages = new List<Page>();

                foreach (var page in _allPages)
                {
                    if (page.gameObject.scene.name != null)
                    {
                        _scenePages.Add(page);
                    }
                }

                return _scenePages.ToArray();
            }

            private void SetActiv(Page page, bool activ, bool sendPage = true)
            {
                if (page == null)
                {
                    Debug.LogWarning("null Page");
                    return;
                }

                if (activ)
                {
                    page.gameObject.SetActive(true);
                    if (sendPage)
                        page.StartActiv();
                }
                else
                {
                    if (sendPage)
                        page.EndActiv();

                    page.gameObject.SetActive(false);
                }
            }

            private void SetActivIgnorePageSettings()
            {
                if (_ignorePageTypes != null)
                {
                    foreach (var item in _pages)
                    {
                        if (_ignorePageTypes.Contains(item.pageType))
                        {
                            item.gameObject.SetActive(_ignorePageActiv);
                        }
                    }
                }
            }

            private void CheckDublicate()
            {
                var duplicates = _pages
                           .GroupBy(x => x)
                           .Where(g => g.Count() > 1)
                           .Select(g => g.Key);

                foreach (var d in duplicates)
                {
                    Debug.LogWarning("Be careful there are duplicates: " + d);
                }
            }

            public void OnValidate()
            {
                name = nameof(PagesManager);

                if (_refresh)
                {
                    Page[] scenePages = FindAllPages();

                    if (scenePages.Length > 0)
                    {
                        CheckDublicate();

                        _pages = scenePages;
                        var page = PagesActivate(_activPageEditor, true, new PageType[] { PageType.None }, true, false);
#if UNITY_EDITOR
                        if (_selectPage)
                            UnityEditor.Selection.activeGameObject = page.gameObject;
#endif
                    }
                }
            }
        }
    }
}



