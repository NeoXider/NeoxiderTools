using Neo;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Neo.Pages
{
    public enum UIPageAnimationMode
    {
        ForwardOnly = 0,
        BackwardOnly = 1,
        ForwardAndBackward = 2,
        None = 3
    }

    /// <summary>
    ///     UI page component for use with <see cref="PM" />.
    ///     Handles page active state for the PM page controller.
    /// </summary>
    /// <remarks>
    ///     The base page switches instantly. A project page with a show/hide animation derives from this class,
    ///     overrides <see cref="StartActive" />/<see cref="EndActive" /> and reports its timing through
    ///     <see cref="HasShowAnimation" />, <see cref="ShowAnimationDuration" /> and
    ///     <see cref="HideAnimationDuration" />, so <see cref="PM" /> keeps the outgoing page visible until the
    ///     incoming one has finished showing.
    /// </remarks>
    [MovedFrom("")]
    [CreateFromMenu("Neoxider/Pages/UIPage")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(UIPage))]
    [NeoDoc("NeoxiderPages/UIPage.md")]
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

        [Space] [Header("Anim")]
        [Tooltip("None: no animation. ForwardOnly: animate only on show. BackwardOnly: animate only on hide. ForwardAndBackward: animate both show and hide.")]
        [SerializeField]
        private UIPageAnimationMode _animationMode = UIPageAnimationMode.ForwardAndBackward;

        [FormerlySerializedAs("_playBackward")] [HideInInspector] [SerializeField]
        private bool _legacyPlayBackward = true;

        [FormerlySerializedAs("_onlyPlayBackward")] [HideInInspector] [SerializeField]
        private bool _legacyOnlyPlayBackward;

        [HideInInspector] [SerializeField] private bool _legacyAnimationModeMigrated;

        public PageId PageId => pageId;

        /// <summary>Opens this page through the page system (handy inspector test button).</summary>
        [Button("Open")]
        public void OpenThisPage()
        {
            if (pageId != null)
            {
                UIKit.ShowPage(pageId);
            }
        }

        public bool Popup => popup;
        public bool IgnoreOnExclusiveChange => ignoreOnExclusiveChange;
        public UIPageAnimationMode AnimationMode => _animationMode;

        /// <summary>
        ///     True when this page plays a show animation on <see cref="StartActive"/>.
        ///     Used by <see cref="PM"/> to keep the previous page visible until the incoming animation finishes.
        ///     The base page has none.
        /// </summary>
        public virtual bool HasShowAnimation => false;

        /// <summary>Realtime seconds to wait for the show tween (duration + delay). Zero when <see cref="HasShowAnimation"/> is false.</summary>
        public virtual float ShowAnimationDuration => 0f;

        /// <summary>Realtime seconds <see cref="WaitForHideAnimation" /> waits for the hide tween. Zero for the base page.</summary>
        public virtual float HideAnimationDuration => 0f;

        private void OnValidate()
        {
            if (pageId != null)
            {
                name = pageId.DisplayName + " Page";
            }

            MigrateLegacyAnimationModeIfNeeded();
        }

        /// <summary>
        ///     Enables the page (a disable/enable cycle, so OnEnable logic of the page content runs again).
        /// </summary>
        public virtual void StartActive()
        {
            SetActive(false);
            SetActive(true);
            MigrateLegacyAnimationModeIfNeeded();
        }

        /// <summary>
        ///     Disables the page.
        /// </summary>
        public virtual void EndActive()
        {
            if (!gameObject.activeInHierarchy)
            {
                SetActive(false);
                return;
            }

            MigrateLegacyAnimationModeIfNeeded();

            SetActive(false);
        }

        /// <summary>
        ///     Sets the page GameObject active state.
        /// </summary>
        /// <param name="value">true to enable, false to disable.</param>
        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        private void MigrateLegacyAnimationModeIfNeeded()
        {
            if (_legacyAnimationModeMigrated)
            {
                return;
            }

            if (_legacyOnlyPlayBackward)
            {
                _animationMode = UIPageAnimationMode.BackwardOnly;
            }
            else
            {
                _animationMode = _legacyPlayBackward
                    ? UIPageAnimationMode.ForwardAndBackward
                    : UIPageAnimationMode.ForwardOnly;
            }

            _legacyAnimationModeMigrated = true;
        }

        /// <summary>
        ///     Yields until the show tween completes (or immediately when there is no show animation).
        ///     Used by <see cref="PM"/> before deactivating other pages on exclusive switches.
        /// </summary>
        public IEnumerator WaitForShowAnimation()
        {
            if (!HasShowAnimation)
            {
                yield break;
            }

            float wait = ShowAnimationDuration;
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        /// <summary>Yields until the hide tween completes (or immediately when <see cref="HideAnimationDuration" /> is zero).</summary>
        public IEnumerator WaitForHideAnimation()
        {
            float wait = HideAnimationDuration;
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }
        }
    }
}
