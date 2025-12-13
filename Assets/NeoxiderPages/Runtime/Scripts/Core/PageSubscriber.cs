using UnityEngine;
using Page = Neo.Pages.UIKit.Page;
using UnityEngine.Scripting.APIUpdating;
using Neo.Extensions;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(PageSubscriber))]
    public class PageSubscriber : MonoBehaviour
    {
        [SerializeField] private PM _pm;

        private void Awake()
        {
            this.WaitWhile(() => !G.Inited, Init);
        }

        private void Init()
        {
            G.OnStart.AddListener(() => PM.I.ChangePage(Page.Game));
        }
    }
}