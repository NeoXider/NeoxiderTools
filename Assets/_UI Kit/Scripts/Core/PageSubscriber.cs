using UnityEngine;
using Page = UIKit.Page;

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