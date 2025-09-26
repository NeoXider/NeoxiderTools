using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Page = UIKit.Page;

[AddComponentMenu("UI/" + "Page/" + nameof(UIPage))]
public class UIPage : MonoBehaviour
{
    [FormerlySerializedAs("pageType")] public Page page = Page.Other;

    [Space] [Header("Anim")] [SerializeField]
    private DOTweenAnimation _animation;

    [SerializeField] private bool _playBackward = true;
    [SerializeField] private bool _onlyPlayBackward;


    private void OnRewind()
    {
        SetActive(false);
    }

    public virtual void StartActive()
    {
        SetActive(false);
        SetActive(true);
        CancelInvoke(nameof(OnRewind));

        if (!_onlyPlayBackward)
            _animation?.DORestart();
    }

    public virtual void EndActive()
    {
        if (_playBackward && _animation != null && _animation.isActive)
        {
            _animation?.DOPlayBackwards();
            Invoke(nameof(OnRewind), _animation.duration + _animation.delay);
        }
        else
        {
            SetActive(false);
        }
    }

    private void OnValidate()
    {
        if (page != Page.Other && page != Page.None)
            name = page + " Page";


        _animation ??= GetComponent<DOTweenAnimation>();
    }

    public void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }
}