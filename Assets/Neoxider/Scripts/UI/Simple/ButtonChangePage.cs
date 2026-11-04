using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.UI
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
                ResolveReferences();
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
                ResolveReferences();
            }

            // WHY: explicit Unity null checks instead of `??=`, which keeps a destroyed or editor fake-null
            // reference and never falls back to the components on this object.
            private void ResolveReferences()
            {
                if (_selectable == null)
                {
                    _selectable = GetComponent<Selectable>();
                }

                if (_imageTarget == null)
                {
                    _imageTarget = GetComponent<Image>();
                }
            }

            public bool Interactable
            {
                get => intecactable && (_selectable == null || _selectable.IsInteractable());
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

                // WHY: scale every axis of the captured scale; the uniform DOScale(float) taken from x flattened or
                // stretched buttons whose base scale is not uniform.
                transform.DOScale(startScale * (1f + _scaleAnim), _timeAnimImage).SetUpdate(true);
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
                if (TryGetPages(out UI pages))
                {
                    pages.SetOnePage(id);
                }
            }

            public void SetPage(int id)
            {
                if (TryGetPages(out UI pages))
                {
                    pages.SetPage(id);
                }
            }

            public void SetPageAnim(int id)
            {
                if (TryGetPages(out UI pages))
                {
                    pages.SetPageAnim(id);
                }
            }

            // WHY: a Unity null check; `?.` skips Unity's destroyed-object check and would forward the call to a
            // destroyed page manager.
            private static bool TryGetPages(out UI pages)
            {
                pages = UI.I;
                return pages != null;
            }
        }
}
