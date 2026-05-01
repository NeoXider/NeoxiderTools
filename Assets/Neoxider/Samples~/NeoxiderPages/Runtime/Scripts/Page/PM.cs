using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Neo;
using Neo.Audio;
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
    ///     Subscriber interface that reacts to page changes in <see cref="PM" />.
    /// </summary>
    public interface IScreenManagerSubscriber
    {
        /// <summary>
        ///     Called when the active page changes.
        /// </summary>
        /// <param name="newUiPage">New active page (may be null).</param>
        void OnChangePage(UIPage newUiPage);
    }

    /// <summary>
    ///     PageManager — UI page controller (enable/disable, switch, return to previous).
    /// </summary>
    [MovedFrom("")]
    [CreateFromMenu("Neoxider/Pages/PM")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(PM))]
    [NeoDoc("NeoxiderPages/PM.md")]
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
        private const string MenuPageIdName = "PageMenu";

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
            EnsureDefaultStartupPage();
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

            if (!refreshPagesInEditor || !gameObject.scene.IsValid())
            {
                return;
            }

            if (!Application.isPlaying)
            {
                SetAllPages();
                EnsureDefaultStartupPage();
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
            EnsureDefaultStartupPage();
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
        ///     Convenience switch by known name (convention: PageWin/PageLose/PageEnd).
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
        ///     Convenience switch by PageId asset name (e.g. "PageEnd").
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
            SetAllPages();

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
        ///     Rebuilds the page dictionary for fast lookup by <see cref="PageId" />.
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
                            $"[PM] Duplicate page for PageId {page.PageId.name} — the first match will be used.");
                    }
                }
            }
        }


        /// <summary>
        ///     Switches page by PageId asset.
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
        ///     Activates a page without deactivating others.
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
        ///     Swaps current and previous pages.
        /// </summary>
        /// <param name="keepPreviousActive">If true, the previous page stays active.</param>
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
        ///     Makes a page active and deactivates others (respecting the ignore list).
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
        ///     Activates the target page by <see cref="PageId" /> and sets other pages according to filters.
        /// </summary>
        /// <param name="targetPageId">Target PageId.</param>
        /// <param name="active">Whether to activate the target page.</param>
        /// <param name="ignoreList">Pages whose active state is left unchanged.</param>
        /// <param name="otherActive">Active state for other pages when not ignored.</param>
        /// <returns>The found target page, or null.</returns>
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
            if (allPages == null || allPages.Length == 0)
            {
                return;
            }

            foreach (UIPage page in allPages)
            {
                if (page == null)
                {
                    continue;
                }

                if (page.PageId != null && ignoredPageIds != null && ignoredPageIds.Contains(page.PageId))
                {
                    continue;
                }

                page.SetActive(acteve);
            }
        }

        /// <summary>
        ///     Enables/disables a specific page via StartActive/EndActive.
        /// </summary>
        /// <param name="uiPage">Page.</param>
        /// <param name="isActive">Desired active state.</param>
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
        ///     Closes the current page (calls EndActive on currentUiPage).
        /// </summary>
        public void CloseCurrentPage()
        {
            if (currentUiPage != null)
            {
                currentUiPage.EndActive();
            }
        }

        /// <summary>
        ///     Finds all <see cref="UIPage" /> under this PM instance (descendants of the PM transform),
        ///     including components on inactive GameObjects.
        /// </summary>
        /// <returns>Array of found pages.</returns>
        public UIPage[] FindAllScenePages()
        {
            return GetComponentsInChildren<UIPage>(true);
        }

        /// <summary>
        ///     Finds a page by PageId using the internal dictionary.
        /// </summary>
        /// <returns>Page or null.</returns>
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

            SetAllPages();
            SetDictPage();
            if (pageIdDict.TryGetValue(pageId, out p))
            {
                return p;
            }

            Debug.LogWarning($"[PM] Page for PageId {pageId.name} not found in dictionary.");
            return null;
        }


        /// <summary>
        ///     Checks the page list for duplicates and logs warnings.
        /// </summary>
        private void CheckForDuplicates(UIPage[] pages)
        {
            if (pages == null || pages.Length == 0)
            {
                return;
            }

            IEnumerable<UIPage> duplicates = pages
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
            CheckForDuplicates(foundPages);
            allPages = foundPages ?? Array.Empty<UIPage>();
            if (allPages.Length == 0)
            {
                pageIdDict?.Clear();
            }
        }

        private static bool IsOther(PageId pageId)
        {
            return pageId != null && pageId.name == "PageOther";
        }

        private void EnsureDefaultStartupPage()
        {
            if (startupPage != null)
            {
                return;
            }

            PageId menuPageId = TryFindPageIdByName(MenuPageIdName);
            if (menuPageId != null)
            {
                startupPage = menuPageId;
            }
        }
    }
}
