using DG.Tweening;
using Neo;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Neo.Pages
{
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/UIPage.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(UIPage))]
    /// <summary>
    /// Компонент страницы UI для работы с <see cref="PM"/>.
    /// Поддерживает проигрывание анимации через DOTween Animation (если компонент присутствует).
    /// </summary>
    public class UIPage : MonoBehaviour
    {
        [FormerlySerializedAs("pageType")] [FormerlySerializedAs("page")] [Header("Id")] [SerializeField]
        private PageId pageId;

        [FormerlySerializedAs("overlay")]
        [Tooltip("When enabled, page opens as popup (on top, without deactivating others).")]
        [SerializeField]
        private bool popup;

        [Tooltip("When enabled, PM will not deactivate this page on Exclusive switches.")]
        [SerializeField]
        private bool ignoreOnExclusiveChange;

        [Space] [Header("Anim")] [SerializeField]
        private DOTweenAnimation _animation;

        [SerializeField] private bool _playBackward = true;
        [SerializeField] private bool _onlyPlayBackward;

        public PageId PageId => pageId;
        public bool Popup => popup;
        public bool IgnoreOnExclusiveChange => ignoreOnExclusiveChange;

        private void OnValidate()
        {
            if (pageId != null)
            {
                name = pageId.DisplayName + " Page";
            }

            _animation ??= GetComponent<DOTweenAnimation>();
        }


        private void OnRewind()
        {
            SetActive(false);
        }

        /// <summary>
        ///     Включает страницу и (опционально) запускает анимацию появления.
        /// </summary>
        public virtual void StartActive()
        {
            SetActive(false);
            SetActive(true);
            CancelInvoke(nameof(OnRewind));

            if (!_onlyPlayBackward)
            {
                _animation?.DORestart();
            }
        }

        /// <summary>
        ///     Выключает страницу и (опционально) проигрывает анимацию закрытия назад.
        /// </summary>
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

        /// <summary>
        ///     Устанавливает активность GameObject страницы.
        /// </summary>
        /// <param name="value">true — включить, false — выключить.</param>
        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}