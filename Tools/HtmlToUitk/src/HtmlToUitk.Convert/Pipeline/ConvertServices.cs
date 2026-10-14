using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Emulations;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert
{
    public sealed class ConvertServices
    {
        public IHtmlParser HtmlParser = new HtmlParser();
        public ICssParser CssParser = Core.Css.CssParser.Instance;
        public IStyleEngine StyleEngine = new StyleEngine();
        public IUxmlWriter UxmlWriter = new UxmlWriter();
        public IRuleEngine RuleEngine;
        public IReadOnlyList<IConstructDetector> Detectors = Array.Empty<IConstructDetector>();
        public ITierPlanner Planner;
        public IRuleCatalog Catalog;
        public EmulationRegistry Emulations = DefaultEmulations.Create();
    }

    public sealed class RunOptions
    {
        public bool Force;
        public bool ValidateOnly;
        public bool Overwrite = true;
        public bool SkipUnchanged = true;
    }

    public static class DefaultEmulations
    {
        public static EmulationRegistry Create()
        {
            EmulationRegistry registry = new EmulationRegistry();
            registry.Register(new ShorthandExpandEmulation());
            registry.Register(new RelativeUnitsEmulation());
            registry.Register(new ViewportUnitsEmulation());
            registry.Register(new CalcFoldEmulation());
            registry.Register(new StaticSelectorsEmulation());
            registry.Register(new PseudoElementsEmulation());
            registry.Register(new GridFlattenEmulation());
            registry.Register(new GapMarginsEmulation());
            registry.Register(new OverflowScrollViewEmulation());
            registry.Register(new FixedToRootEmulation());
            registry.Register(new ZOrderEmulation());
            registry.Register(new TransformDecomposeEmulation());
            registry.Register(new MediaBreakpointsEmulation());
            registry.Register(new KeyframesAnimatorEmulation());
            registry.Register(new PageRouterEmulation());
            registry.Register(new GradientBakeEmulation());
            return registry;
        }
    }
}
