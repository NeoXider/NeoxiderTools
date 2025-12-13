using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Neo.Extensions;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(PageSubscriber))]
    public class PageSubscriber : MonoBehaviour
    {
        [SerializeField] private PM _pm;
        [SerializeField] private PageId gamePageId;

        private void Awake()
        {
            this.WaitWhile(() => !G.Inited, Init);
        }

        private void Init()
        {
            G.OnStart.AddListener(() =>
            {
                if (gamePageId != null)
                {
                    PM.I.ChangePage(gamePageId);
                }
            });
        }
    }
}