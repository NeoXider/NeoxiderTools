using DG.Tweening;
using UnityEngine;
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

            [Space]
            [SerializeField]
            private bool _canSwitchPage = true;
            [SerializeField] private int _idPage;
            [SerializeField] private bool _onePage = false;
            [SerializeField] private bool _useAnimPage = false;

            [Space]
            [SerializeField] private bool _useAnimImage = true;
            [SerializeField] private float _timeAnimImage = 0.3f;
            [SerializeField] private float _scaleAnim = -0.15f;


            private Vector3 startScale;

            private void Awake()
            {
                startScale = transform.localScale;
            }

            private void OnEnable()
            {
                transform.localScale = startScale;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (!intecactable) return;

                if (_canSwitchPage)
                {
                    if (_onePage) UI.Instance?.SetOnePage(_idPage);
                    else
                    {
                        if (_useAnimPage)
                        {
                            UI.Instance.SetPageAnim(_idPage);
                        }
                        else
                        {
                            UI.Instance?.SetPage(_idPage);
                        }
                    }
                }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (intecactable && _useAnimImage)
                {
                    transform.DOScale(startScale, _timeAnimImage);
                }
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (intecactable && _useAnimImage)
                {
                    if (intecactable && _useAnimImage)
                    {
                        float scale = startScale.x * (_scaleAnim > 0 ? 1 + _scaleAnim : 1 + _scaleAnim);
                        transform.DOScale(scale, _timeAnimImage);
                    }
                }
            }

            private void OnValidate()
            {
                _imageTarget ??= GetComponent<Image>();
            }
        }
    }
}