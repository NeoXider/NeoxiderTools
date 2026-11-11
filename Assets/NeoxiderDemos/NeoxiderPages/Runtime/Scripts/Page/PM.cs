using System;
using System.Collections;
using System.Collections.Generic;
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
    /// <remarks>
    ///     Page switches and setup warnings log only when <see cref="DebugLogs" /> is on (or NeoDiagnostics enables
    ///     runtime logs/warnings); errors for pages that cannot be found log unless NeoDiagnostics errors are off.
    /// </remarks>
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
        private const string OtherPageIdName = "PageOther";

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

        [Tooltip("When enabled, opening an exclusive non-popup page closes active popup pages through UIPage.EndActive().")]
        [SerializeField] private bool closePopupsOnExclusivePageChange = true;

        [Space] [Header("Page Change Event")] public UnityEvent<UIPage> OnPageChanged;

        [Space] [Header("Editor Settings")] [SerializeField]
        private bool refreshPagesInEditor = true;

        [SerializeField] private PageId editorActivePageId;
        [SerializeField] private bool autoSelectEditorPage = true;

        [Header("Debug")]
        [Tooltip("Logs page switches and setup warnings (missing, duplicate or blocked pages) of this manager. " +
                 "Errors for pages that cannot be found log unless NeoDiagnostics errors are off.")]
        [SerializeField]
        private bool debugLogs;

        private Coroutine gmSubscribeRoutine;
        private Coroutine exclusivePageTransitionRoutine;
        private UnityAction onEndHandler;
        private UnityAction onLoseHandler;
        private UnityAction onWinHandler;

        private readonly Dictionary<PageId, UIPage> pageIdDict = new();
        private readonly Dictionary<string, PageNameEntry> pageIdsByName = new(StringComparer.Ordinal);
        private readonly Dictionary<PageId, bool> otherPageFlags = new();
        private bool pageNameIndexDirty = true;

        public UIPage[] AllPages => allPages;

        /// <summary>Opening a non-popup page closes active popups (Inspector: Close Popups On Exclusive Page Change).</summary>
        public bool ClosePopupsOnExclusivePageChange
        {
            get => closePopupsOnExclusivePageChange;
            set => closePopupsOnExclusivePageChange = value;
        }

        /// <summary>Logs page switches and setup warnings of this manager (Inspector: Debug Logs).</summary>
        public bool DebugLogs
        {
            get => debugLogs;
            set => debugLogs = value;
        }

        private bool InfoLogsEnabled => debugLogs || NeoDiagnostics.RuntimeLogsEnabled;
        private bool WarningsEnabled => debugLogs || NeoDiagnostics.RuntimeWarningsEnabled;

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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UIKit.OnShowPage.RemoveListener(ChangePage);
            UIKit.OnShowPageByName.RemoveListener(ChangePageByName);
            StopGMIntegration();
            StopExclusivePageTransition();
        }

        private void OnValidate()
        {
            name = nameof(PM);
            pageNameIndexDirty = true;

            // WHY: the editor page preview never runs in Play Mode: there the pages belong to the game,
            // and OnValidate also fires on scene load, which would flip them behind its back.
            if (!refreshPagesInEditor || Application.isPlaying || !gameObject.scene.IsValid())
            {
                return;
            }

#if UNITY_EDITOR
            // WHY: toggling objects inside OnValidate makes Unity warn "SendMessage cannot be called during
            // Awake, CheckConsistency, or OnValidate" for every UI element touched: apply on the next tick.
            EditorApplication.delayCall -= RefreshEditorPreview;
            EditorApplication.delayCall += RefreshEditorPreview;
#endif
        }

#if UNITY_EDITOR
        private void RefreshEditorPreview()
        {
            if (this == null || Application.isPlaying || !gameObject.scene.IsValid() || !refreshPagesInEditor)
            {
                return;
            }

            SetAllPages();
            EnsureDefaultStartupPage();
            PageId last = editorActivePageId;
            UIPage page = ShowEditorPreview(editorActivePageId);

            if (autoSelectEditorPage && page != null && last != page.PageId)
            {
                Selection.activeGameObject = page.gameObject;
            }
        }

        /// <summary>
        ///     Edit Mode preview: shows <paramref name="pageId" /> and hides the other pages (the ignore list is
        ///     respected) without raising <see cref="OnPageChanged" /> or changing the page history. Does nothing in
        ///     Play Mode, where the pages belong to the game.
        /// </summary>
        /// <returns>The shown page, or null.</returns>
        public UIPage PreviewPageInEditor(PageId pageId)
        {
            if (Application.isPlaying || pageId == null)
            {
                return null;
            }

            SetAllPages();
            return ShowEditorPreview(pageId);
        }

        private UIPage ShowEditorPreview(PageId pageId)
        {
            // WHY: the preview must not raise OnPageChanged: Editor-and-Runtime listeners and project subscribers
            // would react to an edit-time page flip.
            ActivateAll(false);
            return pageId != null ? ActivatePagesCore(pageId, true, ignoredPageIds, false) : null;
        }
#endif

        protected override void Init()
        {
            base.Init();

            SetAllPages();
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
                    if (WarningsEnabled)
                    {
                        NeoDiagnostics.LogWarning($"[PM] Unknown page: {page}", this, true);
                    }

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
                if (WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning("[PM] PageId name is empty.", this, true);
                }

                return;
            }

            if (!TryGetPageIdByName(pageIdName, out PageId id))
            {
                NeoDiagnostics.LogError(
                    $"[PM] PageId '{pageIdName}' not found in scene pages. Known PageId names: {DescribeKnownPageIdNames()}.",
                    this);
                return;
            }

            ChangePage(id);
        }

        /// <summary>
        ///     Finds the <see cref="PageId" /> of a page under this PM by asset name ("PageGame"); falls back to the
        ///     page GameObject name ("Game Page"). Rescans the PM hierarchy only when the known pages do not match.
        /// </summary>
        /// <returns>True when a page with that name exists under this PM.</returns>
        public bool TryGetPageIdByName(string pageIdName, out PageId pageId)
        {
            pageId = null;
            if (string.IsNullOrWhiteSpace(pageIdName))
            {
                return false;
            }

            pageId = FindPageIdInKnownPages(pageIdName);
            if (pageId == null)
            {
                // WHY: pages added under the PM after startup are only known after a rescan.
                SetAllPages();
                pageId = FindPageIdInKnownPages(pageIdName);
            }

            return pageId != null;
        }

        private PageId FindPageIdInKnownPages(string pageIdName)
        {
            if (pageNameIndexDirty)
            {
                RebuildPageNameIndex();
            }

            if (!pageIdsByName.TryGetValue(pageIdName, out PageNameEntry entry))
            {
                return null;
            }

            if (entry.IsCurrent)
            {
                return entry.PageId;
            }

            // WHY: the indexed page was destroyed or got another PageId since the index was built.
            RebuildPageNameIndex();
            return pageIdsByName.TryGetValue(pageIdName, out entry) ? entry.PageId : null;
        }

        private void RebuildPageNameIndex()
        {
            // WHY: Object.name allocates a new string on every read; the names are read once per page list, not
            // on every ChangePageByName (the GM Win/Lose/End path).
            pageIdsByName.Clear();
            pageNameIndexDirty = false;
            if (allPages == null)
            {
                return;
            }

            for (int i = 0; i < allPages.Length; i++)
            {
                UIPage page = allPages[i];
                PageId id = page != null ? page.PageId : null;
                if (id != null)
                {
                    pageIdsByName.TryAdd(id.name, new PageNameEntry(page, id));
                }
            }

            // WHY: Fallback: match by the page GameObject name ("Game Page"), a common author mistake
            // when the PageId asset is named differently ("PageGame"). Asset names added above win.
            for (int i = 0; i < allPages.Length; i++)
            {
                UIPage page = allPages[i];
                PageId id = page != null ? page.PageId : null;
                if (id != null)
                {
                    pageIdsByName.TryAdd(page.gameObject.name, new PageNameEntry(page, id));
                }
            }
        }

        private string DescribeKnownPageIdNames()
        {
            List<string> names = new();
            if (allPages != null)
            {
                foreach (UIPage page in allPages)
                {
                    if (page != null && page.PageId != null)
                    {
                        names.Add(page.PageId.name);
                    }
                }
            }

            return string.Join(", ", names);
        }

        /// <summary>
        ///     Rebuilds the page dictionary for fast lookup by <see cref="PageId" />.
        /// </summary>
        public void SetDictPage()
        {
            pageIdDict.Clear();
            if (allPages == null)
            {
                return;
            }

            foreach (UIPage page in allPages)
            {
                if (page == null || page.PageId == null)
                {
                    continue;
                }

                if (!pageIdDict.TryAdd(page.PageId, page) && WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning(
                        $"[PM] Duplicate page for PageId {page.PageId.name} — the first match will be used.", this, true);
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
                if (WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning("[PM] PageId is null.", this, true);
                }

                return;
            }

            if (IsOther(pageId))
            {
                WarnOtherPageBlocked();
                return;
            }

            UIPage target = FindPage(pageId);
            if (target == null)
            {
                NeoDiagnostics.LogError($"[PM] PageId {pageId.name} not found in scene!", this);
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
        ///     Activates a page without deactivating others. When the page cannot be found the current and previous
        ///     pages are kept.
        /// </summary>
        public void ActivePage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            if (IsOther(pageId))
            {
                WarnOtherPageBlocked();
                return;
            }

            UIPage page = FindPage(pageId);
            if (page == null)
            {
                NeoDiagnostics.LogError($"[PM] PageId {pageId.name} not found!", this);
                return;
            }

            if (InfoLogsEnabled)
            {
                NeoDiagnostics.Log($"[PM] Activating PageId: {pageId.name}", this, true);
            }

            RememberPreviousPage(page);
            currentUiPage = page;
            SetPageActive(page, true);

            OnPageChanged?.Invoke(page);
        }

        /// <summary>
        ///     Swaps current and previous pages. Does nothing when there is no previous page.
        /// </summary>
        /// <param name="keepPreviousActive">If true, the previous page stays active.</param>
        public void SwitchToPreviousPage(bool keepPreviousActive = false)
        {
            // WHY: with no history the swap would set currentUiPage to null and blank the UI; keep the current page.
            // A history that points at the current page would show and then close that same page.
            if (previousUiPage == null || previousUiPage == currentUiPage)
            {
                if (WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning("[PM] SwitchToPreviousPage called with no previous page.", this, true);
                }

                return;
            }

            if (InfoLogsEnabled)
            {
                PageId previousId = previousUiPage.PageId;
                NeoDiagnostics.Log("[PM] Switching to Previous Page: " + (previousId != null ? previousId.name : "<no PageId>"),
                    this, true);
            }

            UIPage temp = previousUiPage;
            previousUiPage = currentUiPage;
            currentUiPage = temp;

            ApplyExclusivePageTransition(currentUiPage, previousUiPage, keepPreviousActive);

            OnPageChanged?.Invoke(currentUiPage);
        }

        /// <summary>
        ///     Makes a page active and deactivates others (respecting the ignore list). Raises
        ///     <see cref="OnPageChanged" /> once; when the page cannot be found the current and previous pages are kept.
        /// </summary>
        public void SetPage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            if (IsOther(pageId))
            {
                WarnOtherPageBlocked();
                return;
            }

            if (InfoLogsEnabled)
            {
                NeoDiagnostics.Log($"[PM] Setting PageId: {pageId.name}", this, true);
            }

            UIPage page = ActivatePagesCore(pageId, true, ignoredPageIds, false);
            if (page == null)
            {
                if (WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning($"[PM] SetPage: no page under this PM uses PageId {pageId.name}.", this,
                        true);
                }

                return;
            }

            RememberPreviousPage(page);
            currentUiPage = page;
            OnPageChanged?.Invoke(page);
        }

        /// <summary>
        ///     Activates the target page by <see cref="PageId" /> and sets other pages according to filters. Does not
        ///     change <see cref="currentUiPage" />; raises <see cref="OnPageChanged" /> with the found page.
        /// </summary>
        /// <param name="targetPageId">Target PageId.</param>
        /// <param name="active">Whether to activate the target page.</param>
        /// <param name="ignoreList">Pages whose active state is left unchanged.</param>
        /// <param name="otherActive">Active state for other pages when not ignored.</param>
        /// <returns>The found target page, or null.</returns>
        public UIPage ActivatePages(PageId targetPageId, bool active = true,
            PageId[] ignoreList = null, bool otherActive = false)
        {
            UIPage foundUiPage = ActivatePagesCore(targetPageId, active, ignoreList, otherActive);
            if (foundUiPage != null)
            {
                OnPageChanged?.Invoke(foundUiPage);
            }

            return foundUiPage;
        }

        private UIPage ActivatePagesCore(PageId targetPageId, bool active, PageId[] ignoreList, bool otherActive)
        {
            if (targetPageId == null)
            {
                return null;
            }

            ignoreList ??= Array.Empty<PageId>();
            UIPage foundUiPage = FindPageInAllPages(targetPageId);
            if (foundUiPage == null)
            {
                // WHY: a page added under the PM after startup is only known after a rescan.
                SetAllPages();
                foundUiPage = FindPageInAllPages(targetPageId);
                if (foundUiPage == null)
                {
                    return null;
                }
            }

            bool ignoreTarget = ContainsPageId(ignoreList, foundUiPage.PageId);
            StopExclusivePageTransition();

            if (active)
            {
                if (!ignoreTarget)
                {
                    SetPageActive(foundUiPage, true);
                }

                // WHY: coroutines only tick in Play Mode on an active PM; elsewhere (editor preview, inactive
                // manager) the other pages are hidden at once instead of never.
                if (ShouldDeferHidingOtherPages(foundUiPage) && Application.isPlaying && isActiveAndEnabled)
                {
                    exclusivePageTransitionRoutine = StartCoroutine(
                        DeactivateOtherPagesAfterIncomingShow(foundUiPage, targetPageId, ignoreList, otherActive));
                }
                else
                {
                    DeactivateOtherPages(targetPageId, ignoreList, otherActive);
                }
            }
            else if (!ignoreTarget)
            {
                SetPageActive(foundUiPage, false);
            }

            return foundUiPage;
        }

        private void RememberPreviousPage(UIPage next)
        {
            // WHY: reopening the current page must not overwrite the history with itself; Back would then show and
            // close that same page and leave an empty screen.
            if (currentUiPage != next)
            {
                previousUiPage = currentUiPage;
            }
        }

        private UIPage FindPageInAllPages(PageId targetPageId)
        {
            if (allPages == null)
            {
                return null;
            }

            for (int i = 0; i < allPages.Length; i++)
            {
                UIPage page = allPages[i];
                if (page != null && page.PageId == targetPageId)
                {
                    return page;
                }
            }

            return null;
        }

        private static bool ShouldDeferHidingOtherPages(UIPage incoming)
        {
            return incoming != null && incoming.HasShowAnimation;
        }

        private static bool ContainsPageId(PageId[] pageIds, PageId pageId)
        {
            if (pageIds == null || pageId == null)
            {
                return false;
            }

            for (int i = 0; i < pageIds.Length; i++)
            {
                if (pageIds[i] == pageId)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyExclusivePageTransition(UIPage incoming, UIPage outgoing, bool keepOutgoingActive)
        {
            StopExclusivePageTransition();

            if (incoming != null)
            {
                SetPageActive(incoming, true);
            }

            if (keepOutgoingActive || outgoing == null)
            {
                return;
            }

            SetPageActive(outgoing, false);
        }

        private void DeactivateOtherPages(PageId targetPageId, PageId[] ignoreList, bool otherActive)
        {
            if (allPages == null)
            {
                return;
            }

            for (int i = 0; i < allPages.Length; i++)
            {
                UIPage page = allPages[i];
                if (page == null || page.PageId == targetPageId)
                {
                    continue;
                }

                ApplyOtherPageState(page, ignoreList, otherActive);
            }
        }

        private IEnumerator DeactivateOtherPagesAfterIncomingShow(UIPage incoming, PageId targetPageId,
            PageId[] ignoreList, bool otherActive)
        {
            if (incoming != null)
            {
                yield return incoming.WaitForShowAnimation();
            }

            DeactivateOtherPages(targetPageId, ignoreList, otherActive);
            exclusivePageTransitionRoutine = null;
        }

        private void ApplyOtherPageState(UIPage page, PageId[] ignoreList, bool otherActive)
        {
            if (page.IgnoreOnExclusiveChange)
            {
                return;
            }

            if (ContainsPageId(ignoreList, page.PageId))
            {
                return;
            }

            if (page.Popup && !closePopupsOnExclusivePageChange)
            {
                return;
            }

            if (page.Popup && !page.gameObject.activeInHierarchy)
            {
                return;
            }

            SetPageActive(page, otherActive);
        }

        private void StopExclusivePageTransition()
        {
            if (exclusivePageTransitionRoutine == null)
            {
                return;
            }

            StopCoroutine(exclusivePageTransitionRoutine);
            exclusivePageTransitionRoutine = null;
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

                if (ContainsPageId(ignoredPageIds, page.PageId))
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
                if (WarningsEnabled)
                {
                    NeoDiagnostics.LogWarning("[PM] Attempted to activate a null page.", this, true);
                }

                return;
            }

            if (isActive)
            {
                uiPage.StartActive();
                return;
            }

            if (!uiPage.gameObject.activeInHierarchy)
            {
                uiPage.SetActive(false);
                return;
            }

            uiPage.EndActive();
        }

        /// <summary>
        ///     Closes the current page (calls EndActive on currentUiPage). When the closed page was a popup over the
        ///     previous page, that still visible page becomes current again and <see cref="OnPageChanged" /> is raised.
        /// </summary>
        public void CloseCurrentPage()
        {
            UIPage closing = currentUiPage;
            if (closing == null)
            {
                return;
            }

            closing.EndActive();

            // WHY: closing a popup reveals the page under it. Keeping the closed popup as current made Back and
            // Android-Back mappings act on a page that is no longer on screen.
            if (previousUiPage != null && previousUiPage != closing && previousUiPage.gameObject.activeInHierarchy)
            {
                currentUiPage = previousUiPage;
                previousUiPage = null;
                OnPageChanged?.Invoke(currentUiPage);
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
        ///     Finds a page by PageId using the internal dictionary; rescans the PM hierarchy when the cached entry is
        ///     missing, destroyed or now uses another PageId.
        /// </summary>
        /// <returns>Page or null.</returns>
        public UIPage FindPage(PageId pageId)
        {
            if (pageId == null)
            {
                return null;
            }

            if (pageIdDict.Count == 0)
            {
                SetDictPage();
            }

            if (TryGetCachedPage(pageId, out UIPage page))
            {
                return page;
            }

            SetAllPages();
            SetDictPage();
            if (TryGetCachedPage(pageId, out page))
            {
                return page;
            }

            if (WarningsEnabled)
            {
                NeoDiagnostics.LogWarning($"[PM] Page for PageId {pageId.name} not found in dictionary.", this, true);
            }

            return null;
        }

        private bool TryGetCachedPage(PageId pageId, out UIPage page)
        {
            // WHY: the dictionary is built once; a cached page can be destroyed or get another PageId later.
            return pageIdDict.TryGetValue(pageId, out page) && page != null && page.PageId == pageId;
        }

        private void SetAllPages()
        {
            UIPage[] foundPages = FindAllScenePages();
            allPages = foundPages ?? Array.Empty<UIPage>();
            pageNameIndexDirty = true;
            otherPageFlags.Clear();
            if (allPages.Length == 0)
            {
                pageIdDict.Clear();
            }
        }

        private bool IsOther(PageId pageId)
        {
            if (pageId == null)
            {
                return false;
            }

            // WHY: Object.name allocates a new string; read it once per PageId instead of on every switch.
            if (!otherPageFlags.TryGetValue(pageId, out bool isOther))
            {
                isOther = pageId.name == OtherPageIdName;
                otherPageFlags[pageId] = isOther;
            }

            return isOther;
        }

        private readonly struct PageNameEntry
        {
            public readonly UIPage Page;
            public readonly PageId PageId;

            public PageNameEntry(UIPage page, PageId pageId)
            {
                Page = page;
                PageId = pageId;
            }

            public bool IsCurrent => Page != null && Page.PageId == PageId;
        }

        private void WarnOtherPageBlocked()
        {
            if (WarningsEnabled)
            {
                NeoDiagnostics.LogWarning("[PM] Switching to PageOther is disabled.", this, true);
            }
        }

        private void EnsureDefaultStartupPage()
        {
            if (startupPage != null)
            {
                return;
            }

            if (TryGetPageIdByName(MenuPageIdName, out PageId menuPageId))
            {
                startupPage = menuPageId;
            }
        }
    }
}
