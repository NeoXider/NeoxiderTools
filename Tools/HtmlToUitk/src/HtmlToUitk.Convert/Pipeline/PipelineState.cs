using System;
using System.Collections.Generic;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Convert.Dialect;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Html;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;

namespace HtmlToUitk.Convert
{
    internal sealed class SkinRef
    {
        public string Src;
        public string Path;
        public Stylesheet Sheet;
        public SourceSpan Span;
    }

    internal sealed class PageState
    {
        public PageInput Input;
        public HtmlDocument Html;
        public UiDocument Document;
        public List<Stylesheet> Cascade = new List<Stylesheet>();
        public List<SkinRef> Skins = new List<SkinRef>();
        public IStyleModel Style;
        public MapResult Map;
        public LocTable Loc;
        public NavModel Nav;
        public AnimModel Anim;
        public string ScreenName;
        public IReadOnlyList<string> StyleHrefOrder = new List<string>();
    }

    internal sealed class PipelineState
    {
        public ConversionJob Job;
        public IFileSystem Fs;
        public ConvertServices Services;
        public RunOptions Options;
        public string ProjectRoot = "";
        public string UiRootPath = "";
        public LocTable Loc = new LocTable();
        public readonly List<PageState> Pages = new List<PageState>();
        public readonly List<Stylesheet> ConfigSheets = new List<Stylesheet>();
        public readonly List<Stylesheet> UssSheets = new List<Stylesheet>();
        public readonly List<string> UssPaths = new List<string>();
        public readonly Dictionary<string, PageState> PageByPath = new Dictionary<string, PageState>(StringComparer.Ordinal);
        public AnalysisResult Analysis;
        public ConversionPlan Plan;
        public TransformedModel Model;
        public IncludeGraph Includes = new IncludeGraph();
        public string ConfigDirectory = "";
    }
}
