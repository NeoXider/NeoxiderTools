using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Page = UIKit.Page;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Interface for subscribers that react to page changes.
/// </summary>
public interface IScreenManagerSubscriber
{
    void OnChangePage(UIPage newUiPage);
}

/// <summary>
/// PM (Page Manager) handles the activation and transition of UI pages.
/// </summary>
[AddComponentMenu("UI/Page/" + nameof(PM))]
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
    /// Fills out a pages dictionary for quick access through PageType.
    /// </summary>
    public void SetDictPage()
    {
        pageDict = new Dictionary<Page, UIPage>();

        foreach (var page in allPages)
        {
            if (page == null) continue;

            if (!pageDict.ContainsKey(page.page))
                pageDict.Add(page.page, page);
            else
                Debug.LogWarning(
                    $"[PM] Дублирующая страница типа {page.page} — будет использоваться первая найденная.");
        }
    }


    /// <summary>
    /// Changes to a new page auto
    /// </summary>
    public void ChangePage(Page page)
    {
        if (onlySettablePageTypes.Contains(page))
            SetPage(page);
        else
            ActivePage(page);
    }

    /// <summary>
    /// Activate page
    /// </summary>
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
    /// Swaps the current and previous pages.
    /// </summary>
    public void SwitchToPreviousPage(bool keepPreviousActive = false)
    {
        Debug.Log($"[PM] Switching to Previous Page: {previousUiPage?.page}");

        var temp = previousUiPage;
        previousUiPage = currentUiPage;
        currentUiPage = temp;

        SetPageActive(previousUiPage, keepPreviousActive);
        SetPageActive(currentUiPage, true);

        OnPageChanged?.Invoke(currentUiPage);
    }

    /// <summary>
    /// Sets a page with deactivating others.
    /// </summary>
    public void SetPage(Page page)
    {
        Debug.Log($"[PM] Setting Page: {page}");

        previousUiPage = currentUiPage;
        currentUiPage = ActivatePages(page, true, ignoredPageTypes);
        OnPageChanged?.Invoke(currentUiPage);
    }

    /// <summary>
    /// Activates the specified page and deactivates others based on filters.
    /// </summary>
    public UIPage ActivatePages(Page targetPage, bool active = true,
        Page[] ignoreList = null, bool otherActive = false)
    {
        ignoreList ??= new Page[] { };
        UIPage foundUiPage = null;

        foreach (var page in allPages)
            if (targetPage == Page.None)
            {
                //print("None");
                page.SetActive(false);
            }
            else if (page.page == targetPage)
            {
                foundUiPage = page;
                //print("target");
                if (!ignoreList.Contains(page.page))
                    SetPageActive(page, active);
            }
            else
            {
                //print("other");
                if (!ignoreList.Contains(page.page))
                    SetPageActive(page, otherActive);
            }

        OnPageChanged?.Invoke(foundUiPage);
        return foundUiPage;
    }

    public void ActivateAll(bool acteve)
    {
        foreach (var page in allPages)
            if (!ignoredPageTypes.Contains(page.page))
                page.SetActive(acteve);
    }

    public void Deactivate(Page page, bool send = true)
    {
        var p = FindPage(page);
        p.EndActive();
        currentUiPage = previousUiPage;
        previousUiPage = null;

        if (send && currentUiPage != null)
            OnPageChanged?.Invoke(null);
    }

    /// <summary>
    /// Activates or deactivates a page.
    /// </summary>
    private void SetPageActive(UIPage uiPage, bool isActive)
    {
        if (uiPage == null)
        {
            Debug.LogWarning("[PM] Attempted to activate a null page.");
            return;
        }

        if (isActive)
            uiPage.StartActive();
        else
            uiPage.EndActive();
    }

    /// <summary>
    /// Deactivates current page.
    /// </summary>
    public void CloseCurrentPage()
    {
        if (currentUiPage != null) currentUiPage.EndActive();
    }

    /// <summary>
    /// Finds all pages present in the active scene.
    /// </summary>
    public UIPage[] FindAllScenePages()
    {
        return Resources.FindObjectsOfTypeAll<UIPage>()
            .Where(p => p.gameObject.scene.IsValid()
                        && gameObject.scene == p.gameObject.scene)
            .ToArray();
    }

    /// <summary>
    /// Finds a page from a type from a dictionary.
    /// </summary>
    public UIPage FindPage(Page page)
    {
        if (pageDict == null || pageDict.Count == 0) SetDictPage();

        if (pageDict.TryGetValue(page, out var p)) return p;

        Debug.LogWarning($"[PM] Страница типа {page} не найдена в словаре.");
        return null;
    }


    /// <summary>
    /// Checks for duplicate pages in the list.
    /// </summary>
    private void CheckForDuplicates()
    {
        if (allPages.Length == 0) return;

        var duplicates = allPages
            .Where(x => x != null)
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicates) Debug.LogWarning($"[PM] Duplicate page found: {duplicate.name}");
    }

    private void OnValidate()
    {
        name = nameof(PM);

        if (!refreshPagesInEditor || gameObject.scene != null) return;

        if (!Application.isPlaying) SetAllPages();

        ActivateAll(false);
        var last = editorActivePage;
        var page = ActivatePages(editorActivePage, true, ignoredPageTypes, false);

#if UNITY_EDITOR
        if (autoSelectEditorPage && page != null && last != page.page)
            Selection.activeGameObject = page.gameObject;
#endif
    }

    private void SetAllPages()
    {
        var foundPages = FindAllScenePages();

        if (foundPages.Length > 0)
        {
            CheckForDuplicates();

            allPages = foundPages;
        }
    }
}