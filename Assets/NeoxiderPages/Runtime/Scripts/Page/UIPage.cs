using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Page = Neo.Pages.UIKit.Page;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(UIPage))]
    /// <summary>
    /// Компонент страницы UI для работы с <see cref="PM"/>.
    /// Поддерживает проигрывание анимации через DOTween Animation (если компонент присутствует).
    /// </summary>
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

        /// <summary>
        /// Включает страницу и (опционально) запускает анимацию появления.
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
        /// Выключает страницу и (опционально) проигрывает анимацию закрытия назад.
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

        private void OnValidate()
        {
            if (page != Page.Other && page != Page.None)
            {
                name = page + " Page";
            }

            _animation ??= GetComponent<DOTweenAnimation>();
        }

        /// <summary>
        /// Устанавливает активность GameObject страницы.
        /// </summary>
        /// <param name="value">true — включить, false — выключить.</param>
        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}