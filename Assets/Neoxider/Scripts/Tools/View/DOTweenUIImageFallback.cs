#if DOTWEEN && !DOTWEEN_MODULE_UI
using System;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine.UI;

namespace DG.Tweening
{
    internal static class DOTweenUIImageFallback
    {
        public static TweenerCore<float, float, FloatOptions> DOFillAmount(this Image target, float endValue,
            float duration)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            return DOTween.To(() => target.fillAmount, x => target.fillAmount = x, endValue, duration)
                .SetTarget(target);
        }
    }
}
#endif