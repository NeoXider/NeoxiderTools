using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neo/" + "UI/" + nameof(ButtonChangePage))]
        public class ButtonChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
        {
            [Header("Settings")] public bool intecactable = true;

            [Header("References")] [SerializeField]
            private Image _imageTarget;

            [Header("Page")] [SerializeField] private bool _canSwitchPage = true;
            [SerializeField] private int _idPage;
            [SerializeField] private bool _onePage;
            [SerializeField] private bool _useAnimPage;

            [Header("Animation")] [SerializeField] private bool _useAnimImage = true;
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
                if (!intecactable)
                {
                    return;
                }

                if (_canSwitchPage)
                {
                    if (_onePage)
                    {
                        SetOnePage(_idPage);
                    }
                    else
                    {
                        if (_useAnimPage)
                        {
                            SetPageAnim(_idPage);
                        }
                        else
                        {
                            SetPage(_idPage);
                        }
                    }
                }

                OnClick?.Invoke();
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (!intecactable || !_useAnimImage)
                {
                    return;
                }

                float scale = startScale.x * (1f + _scaleAnim);
                transform.DOScale(scale, _timeAnimImage).SetUpdate(true);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (intecactable && _useAnimImage)
                {
                    transform.DOScale(startScale, _timeAnimImage).SetUpdate(true);
                }
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