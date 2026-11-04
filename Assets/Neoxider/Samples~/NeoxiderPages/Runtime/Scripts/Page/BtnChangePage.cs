using Neo;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Neo.Pages
{
    /// <summary>
    ///     UI button to change pages via <see cref="PM" />.
    ///     Supports press animation and optional <see cref="GameState.State" /> before switching. Reacts to the
    ///     primary pointer button only (left mouse button, touch, pen).
    /// </summary>
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/BtnChangePage.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(BtnChangePage))]
    public class BtnChangePage : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
    {
        public enum Action
        {
            OpenPage = 0,
            Cancel = 1,
            CloseCurrent = 2
        }

        public bool intecactable = true;
        [SerializeField] private Image _imageTarget;

        [FormerlySerializedAs("_pageType")]
        [FormerlySerializedAs("page")]
        [FormerlySerializedAs("pageSelectMode")]
        [Space]
        [Header("Page Settings")]
        [SerializeField]
        private Action action = Action.OpenPage;

        [FormerlySerializedAs("pageId")] [SerializeField]
        private PageId targetPageId;

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
        private Coroutine _scaleRoutine;
        private bool _legacyActionWarned;
        private LayoutGroup _layoutGroup;
        private RectTransform rect;

        private Vector3 startScale;

        /// <summary>When false, clicks and the press animation are ignored (serialized as <c>intecactable</c>).</summary>
        public bool Interactable
        {
            get => intecactable;
            set => intecactable = value;
        }

        /// <summary>What a click does: open <see cref="TargetPageId" />, go back, or close the current page.</summary>
        public Action ButtonAction
        {
            get => action;
            set => action = value;
        }

        /// <summary>
        ///     True when the serialized action is not an <see cref="Action" /> value: a leftover of the retired
        ///     <c>UIKit.Page</c> enum. Such a button opens <see cref="TargetPageId" />, as it always did.
        /// </summary>
        public bool HasLegacyAction => IsLegacyAction(action);

        /// <summary>True for values outside <see cref="Action" /> (old <c>UIKit.Page</c> values such as 12 = Pause).</summary>
        public static bool IsLegacyAction(Action value)
        {
            return value < Action.OpenPage || value > Action.CloseCurrent;
        }

        /// <summary>Page opened by <see cref="Action.OpenPage" />.</summary>
        public PageId TargetPageId
        {
            get => targetPageId;
            set => targetPageId = value;
        }

        private void Awake()
        {
            startScale = transform.localScale;
            rect = transform as RectTransform;
            Transform parent = transform.parent;
            _layoutGroup = parent != null ? parent.GetComponent<LayoutGroup>() : null;
        }

        private void OnEnable()
        {
            transform.localScale = startScale;
            if (_layoutGroup != null && rect != null)
            {
                rect.pivot = Vector2.one * 0.5f;
            }
        }

        private void OnDisable()
        {
            // WHY: disabling stops the coroutine without clearing the handle; the next press must start fresh.
            _scaleRoutine = null;
        }

        private void OnValidate()
        {
            if (_textPage != null)
            {
                if (_changeText)
                {
                    if (action == Action.Cancel)
                    {
                        _textPage.text = "Cancel";
                    }
                    else if (action == Action.CloseCurrent)
                    {
                        _textPage.text = "Close";
                    }
                    else if (targetPageId != null)
                    {
                        _textPage.text = targetPageId.DisplayName;
                    }
                    else
                    {
                        _textPage.text = "Page";
                    }
                }
            }

            if (_imageTarget == null)
            {
                _imageTarget = GetComponent<Image>();
            }
        }

        /// <summary>
        ///     Click handler: runs state (if set), switches page, and invokes <see cref="OnClick" />.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!intecactable || !IsPrimaryButton(eventData))
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
        ///     Press handler: scales down when animation is enabled.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage && IsPrimaryButton(eventData))
            {
                AnimateScale(startScale * (1f + _scaleAnim));
            }
        }

        /// <summary>
        ///     Release handler: restores scale when animation is enabled.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (intecactable && _useAnimImage && IsPrimaryButton(eventData))
            {
                AnimateScale(startScale);
            }
        }

        private static bool IsPrimaryButton(PointerEventData eventData)
        {
            return eventData == null || eventData.button == PointerEventData.InputButton.Left;
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
                _scaleRoutine = null;
            }

            if (!isActiveAndEnabled)
            {
                transform.localScale = targetScale;
                return;
            }

            _scaleRoutine = StartCoroutine(AnimateScaleRoutine(targetScale));
        }

        private IEnumerator AnimateScaleRoutine(Vector3 targetScale)
        {
            Vector3 from = transform.localScale;
            float duration = Mathf.Max(0f, _timeAnimImage);

            if (duration <= 0f)
            {
                transform.localScale = targetScale;
                _scaleRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(from, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
            _scaleRoutine = null;
        }

        /// <summary>
        ///     Runs the action (open page / back / close current) on the scene's <see cref="PM" />. Without a page
        ///     manager the click is ignored. A legacy action value (<see cref="HasLegacyAction" />) opens
        ///     <see cref="TargetPageId" /> and warns once per button.
        /// </summary>
        public void ChangePage()
        {
            PM pm = PM.I;
            if (pm == null)
            {
                NeoDiagnostics.LogWarning("[BtnChangePage] No PM in the scene; the page action is skipped.", this);
                return;
            }

            switch (action)
            {
                case Action.OpenPage:
                    if (targetPageId != null)
                    {
                        pm.ChangePage(targetPageId);
                    }
                    else
                    {
                        NeoDiagnostics.LogWarning("[BtnChangePage] Target PageId is null.", this);
                    }

                    break;
                case Action.Cancel:
                    pm.SwitchToPreviousPage();
                    break;
                case Action.CloseCurrent:
                    pm.CloseCurrentPage();
                    break;
                default:
                    // WHY: values outside Action are leftovers of the retired UIKit.Page enum, kept through the
                    // FormerlySerializedAs chain. Such buttons always opened their target page; dropping that
                    // silently broke old scenes and prefabs.
                    WarnLegacyActionOnce();
                    if (targetPageId != null)
                    {
                        pm.ChangePage(targetPageId);
                    }

                    break;
            }
        }

        private void WarnLegacyActionOnce()
        {
            if (_legacyActionWarned)
            {
                return;
            }

            // WHY: forced, but once per button: the author must learn about the stale value even with
            // NeoDiagnostics warnings off, without a warning on every click.
            _legacyActionWarned = true;
            string outcome = targetPageId != null
                ? "; it opens its Target PageId."
                : " and no Target PageId; the click does nothing.";
            NeoDiagnostics.LogWarning(
                "[BtnChangePage] '" + name + "' has the legacy action value " + ((int)action).ToString() + outcome +
                " Pick an Action in the Inspector.", this, true);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // WHY: Reset used to create and assign a placeholder "PageOpen" asset that no page uses; adding the
            // component (also from scripts and tests) must not write assets. Pick the target in the Inspector.
            action = Action.OpenPage;
        }
#endif
    }
}
