using Neo;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;

namespace Neo.Pages
{
    /// <summary>
    ///     Opens the Game/Win/Lose/End pages from the game-state events of <see cref="G" /> (GM + EM). Subscribes once
    ///     both managers exist and unsubscribes when destroyed, so a scene reload does not leave stale listeners on a
    ///     persistent EM.
    /// </summary>
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/PageSubscriber.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(PageSubscriber))]
    public class PageSubscriber : MonoBehaviour
    {
        [SerializeField] private PM _pm;
        [SerializeField] private PageId gamePageId;
        [SerializeField] private PageId winPageId;
        [SerializeField] private PageId losePageId;
        [SerializeField] private PageId endPageId;
        [SerializeField] private bool autoResolvePageIds = true;
        [SerializeField] private string gamePageName = "PageGame";
        [SerializeField] private string winPageName = "PageWin";
        [SerializeField] private string losePageName = "PageLose";
        [SerializeField] private string endPageName = "PageEnd";

        private UnityAction _onStart;
        private UnityAction _onRestart;
        private UnityAction _onWin;
        private UnityAction _onLose;
        private UnityAction _onEnd;
        private UnityEvent _startEvent;
        private UnityEvent _restartEvent;
        private UnityEvent _winEvent;
        private UnityEvent _loseEvent;
        private UnityEvent _endEvent;

        /// <summary>True once the page listeners are attached to the game-state events.</summary>
        public bool IsSubscribed { get; private set; }

        private void Awake()
        {
            this.WaitWhile(GameEventsMissing, Init);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private static bool GameEventsMissing()
        {
            // WHY: G.Inited only checks GM; the events come from EM, which may appear later or not at all.
            return !G.Inited || G.OnStart == null || G.OnRestart == null || G.OnWin == null || G.OnLose == null ||
                   G.OnEnd == null;
        }

        private void Init()
        {
            if (this == null || IsSubscribed)
            {
                return;
            }

            if (autoResolvePageIds)
            {
                ResolvePageIds();
            }

            _onStart ??= OpenGamePage;
            _onRestart ??= OpenGamePage;
            _onWin ??= OpenWinPage;
            _onLose ??= OpenLosePage;
            _onEnd ??= OpenEndPage;

            _startEvent = G.OnStart;
            _restartEvent = G.OnRestart;
            _winEvent = G.OnWin;
            _loseEvent = G.OnLose;
            _endEvent = G.OnEnd;

            _startEvent?.AddListener(_onStart);
            _restartEvent?.AddListener(_onRestart);
            _winEvent?.AddListener(_onWin);
            _loseEvent?.AddListener(_onLose);
            _endEvent?.AddListener(_onEnd);
            IsSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!IsSubscribed)
            {
                return;
            }

            _startEvent?.RemoveListener(_onStart);
            _restartEvent?.RemoveListener(_onRestart);
            _winEvent?.RemoveListener(_onWin);
            _loseEvent?.RemoveListener(_onLose);
            _endEvent?.RemoveListener(_onEnd);
            _startEvent = null;
            _restartEvent = null;
            _winEvent = null;
            _loseEvent = null;
            _endEvent = null;
            IsSubscribed = false;
        }

        private void OpenGamePage()
        {
            ChangePage(gamePageId);
        }

        private void OpenWinPage()
        {
            ChangePage(winPageId);
        }

        private void OpenLosePage()
        {
            ChangePage(losePageId);
        }

        private void OpenEndPage()
        {
            ChangePage(endPageId);
        }

        private void ChangePage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            PM pm = ResolvePageManager();
            if (pm == null)
            {
                return;
            }

            pm.ChangePage(pageId);
        }

        private PM ResolvePageManager()
        {
            return _pm != null ? _pm : PM.I;
        }

        private void ResolvePageIds()
        {
            if (gamePageId == null)
            {
                gamePageId = FindPageIdByName(gamePageName);
            }

            if (winPageId == null)
            {
                winPageId = FindPageIdByName(winPageName);
            }

            if (losePageId == null)
            {
                losePageId = FindPageIdByName(losePageName);
            }

            if (endPageId == null)
            {
                endPageId = FindPageIdByName(endPageName);
            }
        }

        private PageId FindPageIdByName(string pageIdName)
        {
            if (string.IsNullOrWhiteSpace(pageIdName))
            {
                return null;
            }

            PM pm = ResolvePageManager();
            if (pm != null)
            {
                return pm.TryGetPageIdByName(pageIdName, out PageId managed) ? managed : null;
            }

            // WHY: without a page manager fall back to scene pages only; Resources.FindObjectsOfTypeAll also walked
            // every loaded prefab and asset.
            UIPage[] pages = FindObjectsByType<UIPage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (UIPage page in pages)
            {
                PageId id = page.PageId;
                if (id != null && id.name == pageIdName)
                {
                    return id;
                }
            }

            return null;
        }
    }
}
