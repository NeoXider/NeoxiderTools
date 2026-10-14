using System;
using System.Collections.Generic;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert.Emulations
{
    public static class EmulationIds
    {
        public const string ShorthandExpand = "shorthand-expand";
        public const string RelativeUnits = "relative-units";
        public const string ViewportUnits = "viewport-units";
        public const string CalcFold = "calc-fold";
        public const string StaticSelectors = "static-selectors";
        public const string PseudoElements = "pseudo-elements";
        public const string GridFlatten = "grid-flatten";
        public const string GapMargins = "gap-margins";
        public const string OverflowScrollview = "overflow-scrollview";
        public const string FixedToRoot = "fixed-to-root";
        public const string ZOrder = "z-order";
        public const string TransformDecompose = "transform-decompose";
        public const string GradientBake = "gradient-bake";
        public const string ShadowBake = "shadow-bake";
        public const string FilterBake = "filter-bake";
        public const string MediaBreakpoints = "media-breakpoints";
        public const string KeyframesAnimator = "keyframes-animator";
        public const string BackdropBlur = "backdrop-blur";
        public const string BlendMaterial = "blend-material";
        public const string PageRouter = "page-router";

        public static readonly IReadOnlyList<string> All = new[]
        {
            ShorthandExpand, RelativeUnits, ViewportUnits, CalcFold, StaticSelectors, PseudoElements, GridFlatten, GapMargins,
            OverflowScrollview, FixedToRoot, ZOrder, TransformDecompose, GradientBake, ShadowBake, FilterBake, BackdropBlur,
            MediaBreakpoints, KeyframesAnimator, BlendMaterial, PageRouter
        };
    }

    public interface IEmulation
    {
        string Id { get; }
        Tier Tier { get; }
        IReadOnlyList<string> Handles { get; }
        IReadOnlyList<string> RuntimeComponents { get; }
        int Order { get; }
        EmulationOutcome Apply(EmulationContext context, Construct construct);
    }

    public sealed class EmulationOutcome
    {
        public bool Handled;
        public IReadOnlyList<Diagnostic> Diagnostics = Array.Empty<Diagnostic>();
    }

    public sealed class EmulationContext
    {
        public UiDocument Document;
        public IStyleModel Style;
        public SheetBuilder Sheets;
        public IAssetSink Assets;
        public ISpecSink Specs;
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
        public INameGenerator Names;
        public IElementEditor Editor;
    }

    public sealed class EmulationRegistry
    {
        private readonly List<IEmulation> _all = new List<IEmulation>();
        private readonly Dictionary<string, IEmulation> _byKey = new Dictionary<string, IEmulation>(StringComparer.Ordinal);
        private List<IEmulation> _ordered;

        public void Register(IEmulation emulation)
        {
            if (emulation == null) throw new ArgumentNullException(nameof(emulation));
            _all.Add(emulation);
            _ordered = null;
            for (int i = 0; i < emulation.Handles.Count; i++) _byKey[emulation.Handles[i]] = emulation;
        }

        public IEmulation For(string constructKey)
        {
            IEmulation emulation;
            return constructKey != null && _byKey.TryGetValue(constructKey, out emulation) ? emulation : null;
        }

        public IReadOnlyList<IEmulation> Ordered
        {
            get
            {
                if (_ordered == null)
                {
                    var indexed = new List<KeyValuePair<int, IEmulation>>(_all.Count);
                    for (int i = 0; i < _all.Count; i++) indexed.Add(new KeyValuePair<int, IEmulation>(i, _all[i]));
                    indexed.Sort((a, b) =>
                    {
                        int c = a.Value.Order.CompareTo(b.Value.Order);
                        if (c != 0) return c;
                        c = string.CompareOrdinal(a.Value.Id, b.Value.Id);
                        return c != 0 ? c : a.Key.CompareTo(b.Key);
                    });
                    var list = new List<IEmulation>(indexed.Count);
                    for (int i = 0; i < indexed.Count; i++) list.Add(indexed[i].Value);
                    _ordered = list;
                }
                return _ordered;
            }
        }
    }
}
