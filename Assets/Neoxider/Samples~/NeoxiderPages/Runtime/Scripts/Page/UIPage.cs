using DG.Tweening;
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
        public bool Popup => popup;
        public bool IgnoreOnExclusiveChange => ignoreOnExclusiveChange;
        public UIPageAnimationMode AnimationMode => _animationMode;

        /// <summary>
        ///     True when this page plays a DOTween show animation on <see cref="StartActive"/>.
        ///     Used by <see cref="PM"/> to keep the previous page visible until the incoming animation finishes.
        /// </summary>
        public bool HasShowAnimation => CanPlayForward() && _animation != null;

        /// <summary>Realtime seconds to wait for the show tween (duration + delay). Zero when <see cref="HasShowAnimation"/> is false.</summary>
        public float ShowAnimationDuration
        {
            get
            {
                if (!HasShowAnimation)
                {
                    return 0f;
                }

                float fromTween = GetAnimationWaitSeconds();
                float fromComponent = _animation.duration + _animation.delay;
                return Mathf.Max(fromTween, fromComponent);
            }
        }

        public float HideAnimationDuration
        {
            get
            {
                if (!CanPlayBackward() || _animation == null)
                {
                    return 0f;
                }

                float fromTween = GetAnimationWaitSeconds();
                float fromComponent = _animation.duration + _animation.delay;
                return Mathf.Max(fromTween, fromComponent);
            }
        }

        private void OnValidate()
        {
            if (pageId != null)
            {
                name = pageId.DisplayName + " Page";
            }

            _animation ??= GetComponent<DOTweenAnimation>();
            MigrateLegacyAnimationModeIfNeeded();
            ConfigureAnimation();
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
                PlayForwardRestart();
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

            if (CanPlayBackward() && _animation != null)
            {
                if (PlayBackwardRestart())
                {
                    _deactivateRoutine = StartCoroutine(DeactivateAfterAnimationRealtime());
                }
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

        private void ConfigureAnimation()
        {
            if (_animation == null)
            {
                return;
            }

            _animation.autoKill = false;
            _animation.isIndependentUpdate = true;

            if (_animation.tween != null && _animation.tween.active)
            {
                _animation.tween.SetAutoKill(false);
                _animation.tween.SetUpdate(true);
            }
        }

        private void EnsureAnimationTween()
        {
            if (_animation == null)
            {
                return;
            }

            ConfigureAnimation();
            if (_animation.tween == null || !_animation.tween.active)
            {
                _animation.CreateTween(false, false);
                ConfigureAnimation();
            }
        }

        private void PlayForwardRestart()
        {
            EnsureAnimationTween();
            if (_animation?.tween == null)
            {
                return;
            }

            _animation.tween.Pause();
            _animation.tween.Rewind();
            _animation.tween.PlayForward();
        }

        private bool PlayBackwardRestart()
        {
            EnsureAnimationTween();
            if (_animation?.tween == null)
            {
                SetActive(false);
                return false;
            }

            Tween tween = _animation.tween;
            tween.Pause();
            tween.Goto(tween.Duration(true), false);
            tween.PlayBackwards();
            return true;
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

            EnsureAnimationTween();
            Tween tween = _animation != null ? _animation.tween : null;
            if (tween != null && tween.active && !tween.IsComplete())
            {
                yield return tween.WaitForCompletion(true);
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

        private IEnumerator DeactivateAfterAnimationRealtime()
        {
            float wait = GetAnimationWaitSeconds();
            if (wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }

            _deactivateRoutine = null;
            SetActive(false);
        }

        private float GetAnimationWaitSeconds()
        {
            if (_animation == null)
            {
                return 0f;
            }

            if (_animation.tween != null)
            {
                return _animation.tween.Duration(true) + _animation.delay;
            }

            return _animation.duration + _animation.delay;
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
