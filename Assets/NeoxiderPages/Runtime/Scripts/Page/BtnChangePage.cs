using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Page = Neo.Pages.UIKit.Page;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(BtnChangePage))]
    /// <summary>
    /// UI-кнопка для смены страниц через <see cref="PM"/>.
    /// Поддерживает анимацию нажатия и опциональное выполнение <see cref="GameState.State"/> перед переключением.
    /// </summary>
    public class BtnChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        public bool intecactable = true;
        [SerializeField] private Image _imageTarget;

        [FormerlySerializedAs("_pageType")] [Space] [Header("Page Settings")] [SerializeField]
        private Page page = Page.Menu;

        [SerializeField] private bool _canSwitchPage = true;

        [SerializeField] private GameState.State _executeState;

        [Space] [Header("Animation")] [SerializeField]
        private bool _useAnimImage = true;

        [SerializeField] private float _timeAnimImage = 0.3f;
        [SerializeField] private float _scaleAnim = -0.15f;

        [Space] [Header("SetText")] [SerializeField]
        private bool _changeText = true;

        [SerializeField] private TMP_Text _textPage;

        public UnityEvent OnClick;

        private Vector3 startScale;
        private RectTransform rect;
        private LayoutGroup _layoutGroup;


        private void Awake()
        {
            startScale = transform.localScale;
            rect = transform.GetComponent<RectTransform>();
            _layoutGroup = transform.parent.GetComponent<LayoutGroup>();
        }

        private void OnEnable()
        {
            transform.localScale = startScale;
            if (_layoutGroup != null)
            {
                rect.pivot = Vector2.one * 0.5f;
            }
        }

        /// <summary>
        /// Обработчик клика: выполняет состояние (если задано), переключает страницу и вызывает <see cref="OnClick"/>.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!intecactable)
            {
                return;
            }

            GameState.Set(_executeState);

            if (_canSwitchPage)
            {
                ChangePage();
            }

            OnClick?.Invoke();
        }

        /// <summary>
        /// Обработчик отпускания: возвращает scale к исходному (если включена анимация).
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage)
            {
                transform.DOScale(startScale, _timeAnimImage).SetUpdate(true);
            }
        }

        /// <summary>
        /// Обработчик нажатия: уменьшает scale (если включена анимация).
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage)
            {
                if (intecactable && _useAnimImage)
                {
                    float scale = startScale.x * (_scaleAnim > 0 ? 1 + _scaleAnim : 1 + _scaleAnim);
                    transform.DOScale(scale, _timeAnimImage).SetUpdate(true);
                }
            }
        }

        /// <summary>
        /// Выполняет переключение страницы согласно выбранному <see cref="UIKit.Page"/>.
        /// </summary>
        public void ChangePage()
        {
            if (page == Page._ChangeLastPage)
            {
                PM.I.SwitchToPreviousPage();
            }
            else if (page == Page._CloseCurrentPage)
            {
                PM.I.CloseCurrentPage();
            }
            else
            {
                PM.I.ChangePage(page);
            }
        }

        private void OnValidate()
        {
            if (_textPage != null)
            {
                if (_changeText)
                {
                    _textPage.text = page == Page._ChangeLastPage ? "Back" : page.ToString();
                }
            }

            _imageTarget ??= GetComponent<Image>();
        }
    }
}