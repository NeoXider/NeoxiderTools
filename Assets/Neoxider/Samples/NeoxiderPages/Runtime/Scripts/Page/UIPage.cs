using DG.Tweening;
using Neo;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Neo.Pages
{
    [MovedFrom("")]
    [CreateFromMenu("Neoxider/Pages/UIPage")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(UIPage))]
    [NeoDoc("NeoxiderPages/UIPage.md")]
    /// <summary>
    /// UI page component for use with <see cref="PM"/>.
    /// Plays animation via DOTween Animation when the component is present.
    /// </summary>
    public class UIPage : MonoBehaviour
    {
        [FormerlySerializedAs("pageType")] [FormerlySerializedAs("page")] [Header("Id")] [SerializeField]
        private PageId pageId;

        [FormerlySerializedAs("overlay")]
        [Tooltip("When enabled, page opens as popup (on top, without deactivating others).")]
        [SerializeField]
        private bool popup;

        [Tooltip("When enabled, PM will not deactivate this page on Exclusive switches.")] [SerializeField]
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
        ///     Enables the page and optionally plays the show animation.
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
        ///     Disables the page and optionally plays the close animation backward.
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
        ///     Sets the page GameObject active state.
        /// </summary>
        /// <param name="value">true to enable, false to disable.</param>
        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
    }
}
