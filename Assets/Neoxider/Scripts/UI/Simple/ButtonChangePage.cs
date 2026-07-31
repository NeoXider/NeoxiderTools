using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        [NeoDoc("UI/ButtonChangePage.md")]
        [CreateFromMenu("Neoxider/UI/ButtonChangePage")]
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(ButtonChangePage))]
        public class ButtonChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler,
            ISubmitHandler
        {
            [Header("Settings")] public bool intecactable = true;

            [Header("References")] [SerializeField]
            private Image _imageTarget;

            [SerializeField] private Selectable _selectable;

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
                _selectable ??= GetComponent<Selectable>();
                _imageTarget ??= GetComponent<Image>();
                startScale = transform.localScale;
            }

            private void OnEnable()
            {
                transform.localScale = startScale;
            }

            private void OnDisable()
            {
                // WHY: a press tween surviving disable would keep writing scale after re-enable
                // and override the OnEnable reset.
                transform.DOKill();
            }

            private void OnValidate()
            {
                _imageTarget ??= GetComponent<Image>();
                _selectable ??= GetComponent<Selectable>();
            }

            public bool Interactable
            {
                get => intecactable && (_selectable == null || _selectable.interactable);
                set
                {
                    intecactable = value;
                    if (_selectable != null)
                    {
                        _selectable.interactable = value;
                    }
                }
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
                {
                    return;
                }

                ExecuteClick();
            }

            public void OnSubmit(BaseEventData eventData)
            {
                ExecuteClick();
            }

            public void ExecuteClick()
            {
                if (!Interactable)
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
                if (!Interactable || !_useAnimImage)
                {
                    return;
                }

                float scale = startScale.x * (1f + _scaleAnim);
                transform.DOScale(scale, _timeAnimImage).SetUpdate(true);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (Interactable && _useAnimImage)
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
