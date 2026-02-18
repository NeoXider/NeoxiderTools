using Neo;
using Neo.Extensions;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Neo.Pages
{
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/PageSubscriber.md")]
    [AddComponentMenu("Neo/Pages/" + nameof(PageSubscriber))]
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

        private void Awake()
        {
            this.WaitWhile(() => !G.Inited, Init);
        }

        private void Init()
        {
            if (autoResolvePageIds)
            {
                ResolvePageIds();
            }

            G.OnStart.AddListener(() => ChangePage(gamePageId));
            G.OnRestart.AddListener(() => ChangePage(gamePageId));
            G.OnWin.AddListener(() => ChangePage(winPageId));
            G.OnLose.AddListener(() => ChangePage(losePageId));
            G.OnEnd.AddListener(() => ChangePage(endPageId));
        }

        private void ChangePage(PageId pageId)
        {
            if (pageId == null)
            {
                return;
            }

            PM pm = _pm != null ? _pm : PM.I;
            if (pm == null)
            {
                return;
            }

            pm.ChangePage(pageId);
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

        private static PageId FindPageIdByName(string pageIdName)
        {
            if (string.IsNullOrWhiteSpace(pageIdName))
            {
                return null;
            }

            UIPage[] pages = Resources.FindObjectsOfTypeAll<UIPage>();
            foreach (UIPage page in pages)
            {
                if (page == null || !page.gameObject.scene.IsValid())
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
    }
}