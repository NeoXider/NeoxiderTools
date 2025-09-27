using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ImageFillAmountAnimator : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private float _duration = 0.5f;
    private Tween _anim;

    private void OnValidate()
    {
        _image ??= GetComponent<Image>();
    }

    public void SetValue(float value)
    {
        _anim.Kill();
        _anim = _image.DOFillAmount(value, _duration);
    }
}