using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Готовые шаблоны анимаций карты. Переиспользуются в CardViewUniversal и в любых своих вью.
    ///     Все методы статические; принимают Transform (и при необходимости Graphic) и параметры.
    /// </summary>
    public static class CardViewAnimationTemplates
    {
        /// <summary>Разовая анимация: упругий масштаб (DOPunchScale).</summary>
        /// <param name="target">Transform карты</param>
        /// <param name="duration">Длительность</param>
        /// <param name="punch">Сила punch (0.1–0.2 типично)</param>
        /// <param name="config">Опционально: параметры из конфига</param>
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

        /// <summary>Разовая анимация: один цикл пульсации масштаба.</summary>
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

        /// <summary>Зацикленная пульсация масштаба.</summary>
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

        /// <summary>Разовая анимация: тряска позиции.</summary>
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

        /// <summary>Разовая анимация: краткое подсвечивание через масштаб (вспышка).</summary>
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

        /// <summary>Подсвечивание через alpha (для UI Graphic).</summary>
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

        /// <summary>Влёт из позиции с появлением (scale 0 → 1, движение к текущей позиции).</summary>
        /// <param name="target">Transform карты</param>
        /// <param name="fromPosition">Стартовая позиция (world)</param>
        /// <param name="duration">Длительность</param>
        /// <param name="config">Опционально</param>
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

        /// <summary>Зацикленное лёгкое покачивание масштаба.</summary>
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

        /// <summary>Возвращает разовую анимацию по типу (без конфига, с длительностями по умолчанию).</summary>
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

        /// <summary>Возвращает зацикленную анимацию по типу.</summary>
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
