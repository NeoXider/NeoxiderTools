using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ImageFillAmountAnimator : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    private Tween _anim;

    private void OnValidate()
    {
        _image ??= GetComponent<Image>();
    }

    public void SetValue(float value)
    {
        _anim?.Kill();
        _anim = DOTween.To(() => _image.fillAmount, x => _image.fillAmount = x, value, _duration).SetEase(_ease);
    }
}