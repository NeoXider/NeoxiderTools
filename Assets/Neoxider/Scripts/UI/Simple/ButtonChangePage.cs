using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        public class ButtonChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
        {
            public bool intecactable = true;
            [SerializeField] private Image _imageTarget;

            [Space] [SerializeField] private bool _canSwitchPage = true;
            [SerializeField] private int _idPage;
            [SerializeField] private bool _onePage;
            [SerializeField] private bool _useAnimPage;

            [Space] [SerializeField] private bool _useAnimImage = true;
            [SerializeField] private float _timeAnimImage = 0.3f;
            [SerializeField] private float _scaleAnim = -0.15f;

            public UnityEvent OnClick;

            private Vector3 startScale;

            private void Awake()
            {
                startScale = transform.localScale;
            }

            private void OnEnable()
            {
                transform.localScale = startScale;
            }

            private void OnValidate()
            {
                _imageTarget ??= GetComponent<Image>();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (!intecactable) return;

                if (_canSwitchPage)
                {
                    if (_onePage)
                    {
                        SetOnePage(_idPage);
                    }
                    else
                    {
                        if (_useAnimPage)
                            SetPageAnim(_idPage);
                        else
                            SetPage(_idPage);
                    }
                }

                OnClick?.Invoke();
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (intecactable && _useAnimImage)
                    if (intecactable && _useAnimImage)
                    {
                        var scale = startScale.x * (_scaleAnim > 0 ? 1 + _scaleAnim : 1 + _scaleAnim);
                        transform.DOScale(scale, _timeAnimImage).SetUpdate(true);
                    }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (intecactable && _useAnimImage) transform.DOScale(startScale, _timeAnimImage).SetUpdate(true);
            }

            public void SetOnePage(int id)
            {
                UI.I?.SetOnePage(id);
            }

            public void SetPage(int id)
            {
                UI.I?.SetPage(id);
            }

            public void SetPageAnim(int id)
            {
                UI.I?.SetPageAnim(id);
            }
        }
    }
}