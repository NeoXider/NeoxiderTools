using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Effects
{
    public static class BackdropBlur
    {
        public static bool IsNativeAvailable(PanelSettings settings)
        {
#if HTU_BACKDROP_FILTER
            return settings != null && settings.renderMode == PanelRenderMode.ScreenSpaceOverlay;
#else
            return false;
#endif
        }

        public static bool TryApplyNative(VisualElement target, float radiusPx, PanelSettings settings)
        {
#if HTU_BACKDROP_FILTER
            if (target == null || !IsNativeAvailable(settings))
                return false;
            target.style.backdropFilter = new List<FilterFunction> { BlurFunction(radiusPx) };
            return true;
#else
            return false;
#endif
        }

        public static void ClearNative(VisualElement target)
        {
#if HTU_BACKDROP_FILTER
            if (target != null)
                target.style.backdropFilter = StyleKeyword.Null;
#endif
        }

        internal static Action CaptureNative(VisualElement target)
        {
#if HTU_BACKDROP_FILTER
            return CaptureValue(target.style.backdropFilter, value => target.style.backdropFilter = value);
#else
            return null;
#endif
        }

#if HTU_BACKDROP_FILTER
        private static Action CaptureValue<T>(T saved, Action<T> restore)
        {
            return () => restore(saved);
        }
#endif

        public static bool TryApplyFilterBlur(VisualElement target, float radiusPx)
        {
#if HTU_BACKDROP_FILTER
            if (target == null)
                return false;
            target.style.filter = new List<FilterFunction> { BlurFunction(radiusPx) };
            return true;
#else
            return false;
#endif
        }

        public static void ClearFilter(VisualElement target)
        {
#if HTU_BACKDROP_FILTER
            if (target != null)
                target.style.filter = StyleKeyword.Null;
#endif
        }

#if HTU_BACKDROP_FILTER
        private static FilterFunction BlurFunction(float radiusPx)
        {
            FilterFunction blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter(radiusPx));
            return blur;
        }
#endif
    }
}
