using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Static DOTween helpers for card motion. Used by <see cref="CardViewUniversal" /> and custom views.
    /// </summary>
    public static class CardViewAnimationTemplates
    {
        /// <summary>One-shot bouncy scale (DOPunchScale).</summary>
        /// <param name="target">Card transform.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="punch">Punch strength (0.1–0.2 is typical).</param>
        /// <param name="config">Optional animation config override.</param>
        public static Tween Bounce(Transform target, float duration = 0.25f, float punch = 0.15f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardBounceDuration;
                punch = config.CardBouncePunch;
            }

            Vector3 scale = target.localScale;
            return target.DOPunchScale(scale * punch, duration, 1).SetEase(Ease.OutQuad);
        }

        /// <summary>Single scale pulse cycle.</summary>
        public static Tween Pulse(Transform target, float duration = 0.4f, float scaleDelta = 0.08f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardPulseDuration;
                scaleDelta = config.CardPulseScale;
            }

            Vector3 baseScale = target.localScale;
            float half = duration * 0.5f;
            Sequence s = DOTween.Sequence();
            s.Append(target.DOScale(baseScale * (1f + scaleDelta), half).SetEase(Ease.OutQuad));
            s.Append(target.DOScale(baseScale, half).SetEase(Ease.InQuad));
            return s;
        }

        /// <summary>Looped scale pulse.</summary>
        public static Tween PulseLooped(Transform target, float cycleDuration = 0.8f, float scaleDelta = 0.05f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                cycleDuration = config.CardPulseDuration * 2f;
                scaleDelta = config.CardPulseScale;
            }

            Vector3 baseScale = target.localScale;
            float half = cycleDuration * 0.5f;
            Sequence s = DOTween.Sequence();
            s.Append(target.DOScale(baseScale * (1f + scaleDelta), half).SetEase(Ease.InOutSine));
            s.Append(target.DOScale(baseScale, half).SetEase(Ease.InOutSine));
            s.SetLoops(-1);
            return s;
        }

        /// <summary>One-shot position shake.</summary>
        public static Tween Shake(Transform target, float duration = 0.3f, float strength = 8f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardShakeDuration;
                strength = config.CardShakeStrength;
            }

            return target.DOShakePosition(duration, strength, 20);
        }

        /// <summary>Quick highlight via scale flash.</summary>
        public static Tween Highlight(Transform target, float duration = 0.2f, CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardHighlightDuration;
            }

            Vector3 baseScale = target.localScale;
            float half = duration * 0.5f;
            Sequence s = DOTween.Sequence();
            s.Append(target.DOScale(baseScale * 1.08f, half).SetEase(Ease.OutQuad));
            s.Append(target.DOScale(baseScale, half).SetEase(Ease.InQuad));
            return s;
        }

        /// <summary>Alpha pulse for UI <see cref="Graphic" />.</summary>
        public static Tween HighlightGraphic(Graphic graphic, float duration = 0.2f, CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardHighlightDuration;
            }

            if (graphic == null)
            {
                return null;
            }

            float originalAlpha = graphic.color.a;
            float peakAlpha = Mathf.Min(1f, originalAlpha * 1.3f);
            float half = duration * 0.5f;
            Sequence s = DOTween.Sequence();
            s.Append(DOTween.To(() => graphic.color.a, a =>
            {
                Color c = graphic.color;
                c.a = a;
                graphic.color = c;
            }, peakAlpha, half).SetEase(Ease.OutQuad));
            s.Append(DOTween.To(() => graphic.color.a, a =>
            {
                Color c = graphic.color;
                c.a = a;
                graphic.color = c;
            }, originalAlpha, half).SetEase(Ease.InQuad));
            return s;
        }

        /// <summary>Fly from world position while scaling 0 → base.</summary>
        /// <param name="target">Card transform.</param>
        /// <param name="fromPosition">Start world position.</param>
        /// <param name="duration">Seconds.</param>
        /// <param name="config">Optional config.</param>
        public static Tween FlyIn(Transform target, Vector3 fromPosition, float duration = 0.35f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                duration = config.CardFlyInDuration;
            }

            Vector3 toPos = target.position;
            Vector3 baseScale = target.localScale;
            target.position = fromPosition;
            target.localScale = Vector3.zero;

            Sequence s = DOTween.Sequence();
            s.Join(target.DOMove(toPos, duration).SetEase(Ease.OutCubic));
            s.Join(target.DOScale(baseScale, duration).SetEase(Ease.OutBack));
            return s;
        }

        /// <summary>Subtle breathing scale loop.</summary>
        public static Tween Idle(Transform target, float cycleDuration = 1.2f, float scaleDelta = 0.02f,
            CardAnimationConfig config = null)
        {
            if (config != null)
            {
                cycleDuration = config.CardIdleDuration;
                scaleDelta = config.CardIdleScale;
            }

            Vector3 baseScale = target.localScale;
            float half = cycleDuration * 0.5f;
            Sequence s = DOTween.Sequence();
            s.Append(target.DOScale(baseScale * (1f + scaleDelta), half).SetEase(Ease.InOutSine));
            s.Append(target.DOScale(baseScale * (1f - scaleDelta), half).SetEase(Ease.InOutSine));
            s.SetLoops(-1);
            return s;
        }

        /// <summary>One-shot tween by type (defaults when config omitted).</summary>
        public static Tween PlayOneShot(Transform target, CardViewAnimationType type, float? duration = null)
        {
            float d = duration ?? 0.3f;
            return type switch
            {
                CardViewAnimationType.Bounce => Bounce(target, d),
                CardViewAnimationType.Pulse => Pulse(target, d),
                CardViewAnimationType.Shake => Shake(target, d),
                CardViewAnimationType.Highlight => Highlight(target, d),
                CardViewAnimationType.FlyIn => null,
                _ => null
            };
        }

        /// <summary>Looped tween by type.</summary>
        public static Tween PlayLooped(Transform target, CardViewAnimationType type, float? cycleDuration = null,
            CardAnimationConfig config = null)
        {
            float d = cycleDuration ?? 0.8f;
            return type switch
            {
                CardViewAnimationType.PulseLooped => PulseLooped(target, d, 0.05f, config),
                CardViewAnimationType.Idle => Idle(target, d, 0.02f, config),
                _ => null
            };
        }
    }
}
