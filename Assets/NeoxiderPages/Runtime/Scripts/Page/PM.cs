using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Page = Neo.Pages.UIKit.Page;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Pages
{
    /// <summary>
    /// Интерфейс подписчика, который реагирует на смену страницы в <see cref="PM"/>.
    /// </summary>
    public interface IScreenManagerSubscriber
    {
        /// <summary>
        /// Вызывается при смене активной страницы.
        /// </summary>
        /// <param name="newUiPage">Новая активная страница (может быть null).</param>
        void OnChangePage(UIPage newUiPage);
    }

    /// <summary>
    /// PageManager — компонент управления страницами UI (включение/выключение, переключение, возврат на предыдущую).
    /// </summary>
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(PM))]
    public class PM : MonoBehaviour
    {
        public static PM I;

        [FormerlySerializedAs("currentP")] [FormerlySerializedAs("CurrentPage")] [Header("Active Pages")]
        public UIPage currentUiPage;

        [FormerlySerializedAs("previousP")] [FormerlySerializedAs("PreviousPage")]
        public UIPage previousUiPage;

        [Header("All Pages In Scene")] [SerializeField]
        private UIPage[] allPages;

        [Header("Sets a page with deactivating others")] [SerializeField]
        private Page[] onlySettablePageTypes = { Page.Menu, Page.Game };

        [Header("Startup Page")] [SerializeField]
        private Page startupPage = Page.Menu;

        [Space] [Header("Ignore Specific Page Types")] [SerializeField]
        private Page[] ignoredPageTypes;

        [Space] [Header("Page Change Event")] public UnityEvent<UIPage> OnPageChanged;

        [Space] [Header("Editor Settings")] [SerializeField]
        private bool refreshPagesInEditor = true;

        [SerializeField] private Page editorActivePage;
        [SerializeField] private bool autoSelectEditorPage = true;

        public UIPage[] AllPages => allPages;

        private Dictionary<Page, UIPage> pageDict = new();

        private void Awake()
        {
            if (I == null)
            {
                I = this;
                SetAllPages();
                allPages = allPages.Where(p => p != null).ToArray();
                SetDictPage();
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }


        private void Start()
        {
            ActivateAll(false);
            SetPage(startupPage);
            previousUiPage = null;

            Subscribe();
        }

        private void Subscribe()
        {
            UIKit.OnShowPage.AddListener(ChangePage);

            G.OnWin?.AddListener(() => ChangePage(Page.Win));
            G.OnLose?.AddListener(() => ChangePage(Page.Lose));
            G.OnEnd?.AddListener(() => ChangePage(Page.End));
        }

        /// <summary>
        /// Пересобирает словарь страниц для быстрого поиска по <see cref="UIKit.Page"/>.
        /// </summary>
        public void SetDictPage()
        {
            pageDict = new Dictionary<Page, UIPage>();

            foreach (UIPage page in allPages)
            {
                if (page == null)
                {
                    continue;
                }

                if (!pageDict.ContainsKey(page.page))
                {
                    pageDict.Add(page.page, page);
                }
                else
                {
                    Debug.LogWarning(
                        $"[PM] Дублирующая страница типа {page.page} — будет использоваться первая найденная.");
                }
            }
        }


        /// <summary>
        /// Переключает страницу по типу (автоматически выбирает режим: SetPage или ActivePage).
        /// </summary>
        /// <param name="page">Тип страницы.</param>
        public void ChangePage(Page page)
        {
            if (onlySettablePageTypes.Contains(page))
            {
                SetPage(page);
            }
            else
            {
                ActivePage(page);
            }
        }

        /// <summary>
        /// Активирует страницу, не выключая остальные (если она не относится к onlySettablePageTypes).
        /// </summary>
        /// <param name="page">Тип страницы.</param>
        public void ActivePage(Page page)
        {
            Debug.Log($"[PM] Changing Page to: {page}");

            previousUiPage = currentUiPage;

            if (page == Page.None)
            {
                ActivatePages(Page.None, false);
                currentUiPage = null;
                return;
            }

            currentUiPage = FindPage(page);

            if (currentUiPage == null)
            {
                Debug.LogError($"[PM] Page of type {page} not found!");
                return;
            }

            SetPageActive(currentUiPage, true);

            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        /// Меняет местами текущую и предыдущую страницы.
        /// </summary>
        /// <param name="keepPreviousActive">Если true — предыдущая страница не будет деактивирована.</param>
        public void SwitchToPreviousPage(bool keepPreviousActive = false)
        {
            Debug.Log($"[PM] Switching to Previous Page: {previousUiPage?.page}");

            UIPage temp = previousUiPage;
            previousUiPage = currentUiPage;
            currentUiPage = temp;

            SetPageActive(previousUiPage, keepPreviousActive);
            SetPageActive(currentUiPage, true);

            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        /// Делает страницу активной и деактивирует остальные (с учетом ignore списка).
        /// </summary>
        /// <param name="page">Тип страницы.</param>
        public void SetPage(Page page)
        {
            Debug.Log($"[PM] Setting Page: {page}");

            previousUiPage = currentUiPage;
            currentUiPage = ActivatePages(page, true, ignoredPageTypes);
            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        /// Активирует целевую страницу и управляет состоянием остальных по фильтрам.
        /// </summary>
        /// <param name="targetPage">Целевая страница.</param>
        /// <param name="active">Активировать целевую страницу.</param>
        /// <param name="ignoreList">Список страниц, которые не трогаем.</param>
        /// <param name="otherActive">Состояние остальных страниц (если их не игнорируем).</param>
        /// <returns>Найденная целевая страница или null.</returns>
        public UIPage ActivatePages(Page targetPage, bool active = true,
            Page[] ignoreList = null, bool otherActive = false)
        {
            ignoreList ??= new Page[] { };
            UIPage foundUiPage = null;

            foreach (UIPage page in allPages)
            {
                if (targetPage == Page.None)
                {
                    page.SetActive(false);
                }
                else if (page.page == targetPage)
                {
                    foundUiPage = page;
                    if (!ignoreList.Contains(page.page))
                    {
                        SetPageActive(page, active);
                    }
                }
                else
                {
                    if (!ignoreList.Contains(page.page))
                    {
                        SetPageActive(page, otherActive);
                    }
                }
            }

            OnPageChanged?.Invoke(foundUiPage);
            return foundUiPage;
        }

        public void ActivateAll(bool acteve)
        {
            foreach (UIPage page in allPages)
            {
                if (!ignoredPageTypes.Contains(page.page))
                {
                    page.SetActive(acteve);
                }
            }
        }

        public void Deactivate(Page page, bool send = true)
        {
            UIPage p = FindPage(page);
            p.EndActive();
            currentUiPage = previousUiPage;
            previousUiPage = null;

            if (send && currentUiPage != null)
            {
                OnPageChanged?.Invoke(null);
            }
        }

        /// <summary>
        /// Включает/выключает конкретную страницу через StartActive/EndActive.
        /// </summary>
        /// <param name="uiPage">Страница.</param>
        /// <param name="isActive">Состояние.</param>
        private void SetPageActive(UIPage uiPage, bool isActive)
        {
            if (uiPage == null)
            {
                Debug.LogWarning("[PM] Attempted to activate a null page.");
                return;
            }

            if (isActive)
            {
                uiPage.StartActive();
            }
            else
            {
                uiPage.EndActive();
            }
        }

        /// <summary>
        /// Закрывает текущую страницу (вызывает EndActive у currentUiPage).
        /// </summary>
        public void CloseCurrentPage()
        {
            if (currentUiPage != null)
            {
                currentUiPage.EndActive();
            }
        }

        /// <summary>
        /// Находит все <see cref="UIPage"/> в текущей активной сцене (включая неактивные объекты).
        /// </summary>
        /// <returns>Массив найденных страниц.</returns>
        public UIPage[] FindAllScenePages()
        {
            return Resources.FindObjectsOfTypeAll<UIPage>()
                .Where(p => p.gameObject.scene.IsValid()
                            && gameObject.scene == p.gameObject.scene)
                .ToArray();
        }

        /// <summary>
        /// Находит страницу по типу через внутренний словарь.
        /// </summary>
        /// <param name="page">Тип страницы.</param>
        /// <returns>Страница или null.</returns>
        public UIPage FindPage(Page page)
        {
            if (pageDict == null || pageDict.Count == 0)
            {
                SetDictPage();
            }

            if (pageDict.TryGetValue(page, out UIPage p))
            {
                return p;
            }

            Debug.LogWarning($"[PM] Страница типа {page} не найдена в словаре.");
            return null;
        }


        /// <summary>
        /// Проверяет список страниц на дубликаты и выводит предупреждения.
        /// </summary>
        private void CheckForDuplicates()
        {
            if (allPages.Length == 0)
            {
                return;
            }

            IEnumerable<UIPage> duplicates = allPages
                .Where(x => x != null)
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (UIPage duplicate in duplicates)
            {
                Debug.LogWarning($"[PM] Duplicate page found: {duplicate.name}");
            }
        }

        private void OnValidate()
        {
            name = nameof(PM);

            if (!refreshPagesInEditor || gameObject.scene != null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                SetAllPages();
            }

            ActivateAll(false);
            Page last = editorActivePage;
            UIPage page = ActivatePages(editorActivePage, true, ignoredPageTypes, false);

#if UNITY_EDITOR
            if (autoSelectEditorPage && page != null && last != page.page)
            {
                Selection.activeGameObject = page.gameObject;
            }
#endif
        }

        private void SetAllPages()
        {
            UIPage[] foundPages = FindAllScenePages();

            if (foundPages.Length > 0)
            {
                CheckForDuplicates();

                allPages = foundPages;
            }
        }
    }
}