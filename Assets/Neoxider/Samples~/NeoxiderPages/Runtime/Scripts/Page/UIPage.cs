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

    [MovedFrom("")]
    [CreateFromMenu("Neoxider/Pages/UIPage")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(UIPage))]
    [NeoDoc("NeoxiderPages/UIPage.md")]
    /// <summary>
    /// UI page component for use with <see cref="PM"/>.
    /// Handles page active state for the PM page controller.
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

        [Space] [Header("Anim")]
        [Tooltip("None: no animation. ForwardOnly: animate only on show. BackwardOnly: animate only on hide. ForwardAndBackward: animate both show and hide.")]
        [SerializeField]
        private UIPageAnimationMode _animationMode = UIPageAnimationMode.ForwardAndBackward;

        [FormerlySerializedAs("_playBackward")] [HideInInspector] [SerializeField]
        private bool _legacyPlayBackward = true;

        [FormerlySerializedAs("_onlyPlayBackward")] [HideInInspector] [SerializeField]
        private bool _legacyOnlyPlayBackward;

        [HideInInspector] [SerializeField] private bool _legacyAnimationModeMigrated;

        private Coroutine _deactivateRoutine;

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
        /// </summary>
        public bool HasShowAnimation => false;

        /// <summary>Realtime seconds to wait for the show tween (duration + delay). Zero when <see cref="HasShowAnimation"/> is false.</summary>
        public float ShowAnimationDuration
        {
            get
            {
                if (!HasShowAnimation)
                {
                    return 0f;
                }

                return 0f;
            }
        }

        public float HideAnimationDuration
        {
            get
            {
                return 0f;
            }
        }

        private void OnValidate()
        {
            if (pageId != null)
            {
                name = pageId.DisplayName + " Page";
            }

            MigrateLegacyAnimationModeIfNeeded();
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
            StopDeactivateRoutine();
            SetActive(false);
            SetActive(true);
            CancelInvoke(nameof(OnRewind));

            MigrateLegacyAnimationModeIfNeeded();

            if (CanPlayForward())
            {
                // Animation hooks are intentionally optional for the sample.
            }
        }

        /// <summary>
        ///     Disables the page and optionally plays the close animation backward.
        /// </summary>
        public virtual void EndActive()
        {
            StopDeactivateRoutine();
            CancelInvoke(nameof(OnRewind));

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

        private bool CanPlayForward()
        {
            return _animationMode == UIPageAnimationMode.ForwardOnly ||
                   _animationMode == UIPageAnimationMode.ForwardAndBackward;
        }

        private bool CanPlayBackward()
        {
            return _animationMode == UIPageAnimationMode.BackwardOnly ||
                   _animationMode == UIPageAnimationMode.ForwardAndBackward;
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

        public IEnumerator WaitForHideAnimation()
        {
            float wait = HideAnimationDuration;
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        private void StopDeactivateRoutine()
        {
            if (_deactivateRoutine == null)
            {
                return;
            }

            StopCoroutine(_deactivateRoutine);
            _deactivateRoutine = null;
        }
    }
}
