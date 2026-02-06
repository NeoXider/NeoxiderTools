using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Pages
{
    /// <summary>
    ///     Интерфейс подписчика, который реагирует на смену страницы в <see cref="PM" />.
    /// </summary>
    public interface IScreenManagerSubscriber
    {
        /// <summary>
        ///     Вызывается при смене активной страницы.
        /// </summary>
        /// <param name="newUiPage">Новая активная страница (может быть null).</param>
        void OnChangePage(UIPage newUiPage);
    }

    /// <summary>
    ///     PageManager — компонент управления страницами UI (включение/выключение, переключение, возврат на предыдущую).
    /// </summary>
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(PM))]
    public class PM : Singleton<PM>
    {
        public enum KnownPage
        {
            Win,
            Lose,
            End
        }

        private const string WinPageIdName = "PageWin";
        private const string LosePageIdName = "PageLose";
        private const string EndPageIdName = "PageEnd";

        [Header("GM Integration")] [SerializeField]
        private bool integrateWithGM = true;

        [FormerlySerializedAs("currentP")] [FormerlySerializedAs("CurrentPage")] [Header("Active Pages")]
        public UIPage currentUiPage;

        [FormerlySerializedAs("previousP")] [FormerlySerializedAs("PreviousPage")]
        public UIPage previousUiPage;

        [Header("All Pages In Scene")] [SerializeField]
        private UIPage[] allPages;

        [Header("Startup Page")] [SerializeField]
        private PageId startupPage;

        [Space] [Header("Ignore Specific Pages (do not change active state)")] [SerializeField]
        private PageId[] ignoredPageIds;

        [Space] [Header("Page Change Event")] public UnityEvent<UIPage> OnPageChanged;

        [Space] [Header("Editor Settings")] [SerializeField]
        private bool refreshPagesInEditor = true;

        [SerializeField] private PageId editorActivePageId;
        [SerializeField] private bool autoSelectEditorPage = true;

        private Coroutine gmSubscribeRoutine;
        private UnityAction onEndHandler;
        private UnityAction onLoseHandler;
        private UnityAction onWinHandler;

        private Dictionary<PageId, UIPage> pageIdDict = new();

        public UIPage[] AllPages => allPages;


        private void Start()
        {
            ActivateAll(false);
            if (startupPage != null)
            {
                SetPage(startupPage);
            }

            previousUiPage = null;

            Subscribe();

            if (integrateWithGM)
            {
                StartGMIntegration();
            }
        }

        private void OnDestroy()
        {
            UIKit.OnShowPage.RemoveListener(ChangePage);
            UIKit.OnShowPageByName.RemoveListener(ChangePageByName);
            StopGMIntegration();
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
            PageId last = editorActivePageId;
            UIPage page = editorActivePageId != null
                ? ActivatePages(editorActivePageId, true, ignoredPageIds)
                : null;

#if UNITY_EDITOR
            if (autoSelectEditorPage && page != null && last != page.PageId)
            {
                Selection.activeGameObject = page.gameObject;
            }
#endif
        }

        protected override void Init()
        {
            base.Init();

            SetAllPages();
            allPages = allPages.Where(p => p != null).ToArray();
            SetDictPage();
        }

        private void Subscribe()
        {
            UIKit.OnShowPage.AddListener(ChangePage);
            UIKit.OnShowPageByName.AddListener(ChangePageByName);
        }

        private void StartGMIntegration()
        {
            StopGMIntegration();
            gmSubscribeRoutine = StartCoroutine(WaitAndSubscribeGM());
        }

        private void StopGMIntegration()
        {
            if (gmSubscribeRoutine != null)
            {
                StopCoroutine(gmSubscribeRoutine);
                gmSubscribeRoutine = null;
            }

            if (onWinHandler != null)
            {
                G.OnWin?.RemoveListener(onWinHandler);
                onWinHandler = null;
            }

            if (onLoseHandler != null)
            {
                G.OnLose?.RemoveListener(onLoseHandler);
                onLoseHandler = null;
            }

            if (onEndHandler != null)
            {
                G.OnEnd?.RemoveListener(onEndHandler);
                onEndHandler = null;
            }
        }

        private IEnumerator WaitAndSubscribeGM()
        {
            while (!G.Inited || G.OnWin == null || G.OnLose == null || G.OnEnd == null)
            {
                yield return null;
            }

            if (!integrateWithGM)
            {
                gmSubscribeRoutine = null;
                yield break;
            }

            onWinHandler = () => ChangePage(KnownPage.Win);
            onLoseHandler = () => ChangePage(KnownPage.Lose);
            onEndHandler = () => ChangePage(KnownPage.End);

            G.OnWin.AddListener(onWinHandler);
            G.OnLose.AddListener(onLoseHandler);
            G.OnEnd.AddListener(onEndHandler);

            gmSubscribeRoutine = null;
        }

        /// <summary>
        ///     Удобный вызов переключения страницы по известному имени (по соглашению: PageWin/PageLose/PageEnd).
        /// </summary>
        public void ChangePage(KnownPage page)
        {
            switch (page)
            {
                case KnownPage.Win:
                    ChangePageByName(WinPageIdName);
                    break;
                case KnownPage.Lose:
                    ChangePageByName(LosePageIdName);
                    break;
                case KnownPage.End:
                    ChangePageByName(EndPageIdName);
                    break;
                default:
                    Debug.LogWarning($"[PM] Unknown page: {page}");
                    break;
            }
        }

        /// <summary>
        ///     Удобный вызов переключения страницы по имени PageId (например, "PageEnd").
        /// </summary>
        public void ChangePageByName(string pageIdName)
        {
            if (string.IsNullOrWhiteSpace(pageIdName))
            {
                Debug.LogWarning("[PM] PageId name is empty.");
                return;
            }

            PageId id = TryFindPageIdByName(pageIdName);
            if (id == null)
            {
                Debug.LogError($"[PM] PageId '{pageIdName}' not found in scene pages.");
                return;
            }

            ChangePage(id);
        }

        private PageId TryFindPageIdByName(string pageIdName)
        {
            if (allPages == null || allPages.Length == 0)
            {
                SetAllPages();
            }

            if (allPages == null || allPages.Length == 0)
            {
                return null;
            }

            foreach (UIPage page in allPages)
            {
                if (page == null)
                {
                    continue;
                }

                PageId id = page.PageId;
                if (id != null && id.name == pageIdName)
                {
                    return id;
                }
            }

            return null;
        }

        /// <summary>
        ///     Пересобирает словарь страниц для быстрого поиска по <see cref="PageId" />.
        /// </summary>
        public void SetDictPage()
        {
            pageIdDict = new Dictionary<PageId, UIPage>();

            foreach (UIPage page in allPages)
            {
                if (page == null)
                {
                    continue;
                }

                if (page.PageId != null)
                {
                    if (!pageIdDict.ContainsKey(page.PageId))
                    {
                        pageIdDict.Add(page.PageId, page);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[PM] Дублирующая страница PageId {page.PageId.name} — будет использоваться первая найденная.");
                    }
                }
            }
        }


        /// <summary>
        ///     Переключает страницу по ассету-идентификатору.
        /// </summary>
        public void ChangePage(PageId pageId)
        {
            if (pageId == null)
            {
                Debug.LogWarning("[PM] PageId is null.");
                return;
            }

            if (IsOther(pageId))
            {
                Debug.LogWarning("[PM] Switching to PageOther is disabled.");
                return;
            }

            UIPage target = FindPage(pageId);
            if (target == null)
            {
                Debug.LogError($"[PM] PageId {pageId.name} not found in scene!");
                return;
            }

            if (target.Popup)
            {
                ActivePage(pageId);
            }
            else
            {
                SetPage(pageId);
            }
        }

        /// <summary>
        ///     Активирует страницу, не выключая остальные.
        /// </summary>
        public void ActivePage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            if (IsOther(pageId))
            {
                Debug.LogWarning("[PM] Switching to PageOther is disabled.");
                return;
            }

            Debug.Log($"[PM] Activating PageId: {pageId.name}");

            previousUiPage = currentUiPage;

            currentUiPage = FindPage(pageId);

            if (currentUiPage == null)
            {
                Debug.LogError($"[PM] PageId {pageId.name} not found!");
                return;
            }

            SetPageActive(currentUiPage, true);

            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        ///     Меняет местами текущую и предыдущую страницы.
        /// </summary>
        /// <param name="keepPreviousActive">Если true — предыдущая страница не будет деактивирована.</param>
        public void SwitchToPreviousPage(bool keepPreviousActive = false)
        {
            Debug.Log($"[PM] Switching to Previous Page: {previousUiPage?.PageId?.name}");

            UIPage temp = previousUiPage;
            previousUiPage = currentUiPage;
            currentUiPage = temp;

            SetPageActive(previousUiPage, keepPreviousActive);
            SetPageActive(currentUiPage, true);

            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        ///     Делает страницу активной и деактивирует остальные (с учетом ignore списка).
        /// </summary>
        public void SetPage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            if (IsOther(pageId))
            {
                Debug.LogWarning("[PM] Switching to PageOther is disabled.");
                return;
            }

            Debug.Log($"[PM] Setting PageId: {pageId.name}");

            previousUiPage = currentUiPage;
            currentUiPage = ActivatePages(pageId, true, ignoredPageIds);
            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        ///     Активирует целевую страницу и управляет состоянием остальных по фильтрам.
        /// </summary>
        /// <param name="targetPage">Целевая страница.</param>
        /// <param name="active">Активировать целевую страницу.</param>
        /// <param name="ignoreList">Список страниц, которые не трогаем.</param>
        /// <param name="otherActive">Состояние остальных страниц (если их не игнорируем).</param>
        /// <returns>Найденная целевая страница или null.</returns>
        /// <summary>
        ///     Активирует целевую страницу по <see cref="PageId" /> и управляет состоянием остальных по фильтрам.
        /// </summary>
        public UIPage ActivatePages(PageId targetPageId, bool active = true,
            PageId[] ignoreList = null, bool otherActive = false)
        {
            if (targetPageId == null)
            {
                return null;
            }

            ignoreList ??= new PageId[] { };
            UIPage foundUiPage = null;

            foreach (UIPage page in allPages)
            {
                if (page == null)
                {
                    continue;
                }

                bool ignoreById = page.PageId != null && ignoreList.Contains(page.PageId);

                if (page.PageId == targetPageId)
                {
                    foundUiPage = page;
                    if (!ignoreById)
                    {
                        SetPageActive(page, active);
                    }
                }
                else
                {
                    if (page.IgnoreOnExclusiveChange)
                    {
                        continue;
                    }

                    if (!ignoreById)
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
                if (page.PageId != null && ignoredPageIds != null && ignoredPageIds.Contains(page.PageId))
                {
                    continue;
                }

                page.SetActive(acteve);
            }
        }

        /// <summary>
        ///     Включает/выключает конкретную страницу через StartActive/EndActive.
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
        ///     Закрывает текущую страницу (вызывает EndActive у currentUiPage).
        /// </summary>
        public void CloseCurrentPage()
        {
            if (currentUiPage != null)
            {
                currentUiPage.EndActive();
            }
        }

        /// <summary>
        ///     Находит все <see cref="UIPage" /> в текущей активной сцене (включая неактивные объекты).
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
        ///     Находит страницу по типу через внутренний словарь.
        /// </summary>
        /// <returns>Страница или null.</returns>
        public UIPage FindPage(PageId pageId)
        {
            if (pageId == null)
            {
                return null;
            }

            if (pageIdDict == null || pageIdDict.Count == 0)
            {
                SetDictPage();
            }

            if (pageIdDict.TryGetValue(pageId, out UIPage p))
            {
                return p;
            }

            Debug.LogWarning($"[PM] Страница PageId {pageId.name} не найдена в словаре.");
            return null;
        }


        /// <summary>
        ///     Проверяет список страниц на дубликаты и выводит предупреждения.
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

        private void SetAllPages()
        {
            UIPage[] foundPages = FindAllScenePages();

            if (foundPages.Length > 0)
            {
                CheckForDuplicates();

                allPages = foundPages;
            }
        }

        private static bool IsOther(PageId pageId)
        {
            return pageId != null && pageId.name == "PageOther";
        }
    }
}