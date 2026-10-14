using System;
using System.Collections.Generic;
using System.Threading;
using HtmlToUitk.Capabilities.Model;
using HtmlToUitk.Convert.Config;
using HtmlToUitk.Core.Css;
using HtmlToUitk.Core.Style;
using HtmlToUitk.Core.Uxml;
using HtmlToUitk.Rules;

namespace HtmlToUitk.Convert
{
    public interface IFileSystem
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string ReadAllText(string path);
        byte[] ReadAllBytes(string path);
        IReadOnlyList<string> EnumerateFiles(string directory, bool recursive);
        void WriteAllBytes(string path, byte[] content);
        void CreateDirectory(string path);
    }

    public sealed class StageContext
    {
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
        public DiagnosticBag Diagnostics;
        public IFileSystem Fs;
        public CancellationToken Cancel;
    }

    public interface IPipelineStage<TIn, TOut>
    {
        string Name { get; }
        TOut Run(TIn input, StageContext context);
    }

    public sealed class ConversionJob
    {
        public JobSpec Spec;
        public ConversionConfig Config;
        public CapabilityMatrix Capabilities;
    }

    public sealed class PageInput
    {
        public string Path;
        public string Text;
        public string Name;
    }

    public sealed class StyleInput
    {
        public string Path;
        public string Text;
        public StyleOrigin Origin;
    }

    public sealed class UssInput
    {
        public string Path;
        public string Text;
    }

    public sealed class PartialInput
    {
        public string Id;
        public string Path;
        public string Text;
    }

    public sealed class IncludeEdge
    {
        public string From;
        public string To;
        public string Kind;
        public SourceSpan Span;
    }

    public sealed class IncludeGraph
    {
        public IReadOnlyList<string> Nodes = Array.Empty<string>();
        public IReadOnlyList<IncludeEdge> Edges = Array.Empty<IncludeEdge>();
    }

    public sealed class InputSet
    {
        public IReadOnlyList<PageInput> Pages = Array.Empty<PageInput>();
        public IReadOnlyList<StyleInput> Styles = Array.Empty<StyleInput>();
        public IReadOnlyList<UssInput> UssPassThrough = Array.Empty<UssInput>();
        public IReadOnlyList<PartialInput> Partials = Array.Empty<PartialInput>();
        public IncludeGraph Includes = new IncludeGraph();
    }

    public sealed class TokenEntry
    {
        public string Name;
        public CssValue Value;
        public string Scope;
        public bool FromSkin;
        public SourceSpan Span;
    }

    public sealed class TokenTable
    {
        public IReadOnlyList<TokenEntry> Entries = Array.Empty<TokenEntry>();
    }

    public sealed class KeyUse
    {
        public string Key;
        public string ElementName;
        public string Attribute;
        public SourceSpan Span;
    }

    public sealed class KeyTable
    {
        public IReadOnlyList<KeyUse> Uses = Array.Empty<KeyUse>();
        public IReadOnlyList<string> NewKeys = Array.Empty<string>();
    }

    public sealed class NavPage
    {
        public string Id;
        public string Element;
        public string Focus;
        public string TransitionIn;
        public string TransitionOut;
        public int TransitionMs = -1;
    }

    public sealed class NavEdge
    {
        public string From;
        public string Trigger;
        public string Action;
        public string To;
        public string Mode;
        public string Transition;
        public int Ms = -1;
    }

    public sealed class NavModal
    {
        public string Id;
        public string Element;
        public string OwnerPage;
        public IReadOnlyList<string> CloseTriggers = Array.Empty<string>();
    }

    public sealed class NavTab
    {
        public string Tab;
        public string Panel;
    }

    public sealed class NavTabGroup
    {
        public string Group;
        public string Page;
        public string Default;
        public IReadOnlyList<NavTab> Tabs = Array.Empty<NavTab>();
    }

    public sealed class NavModel
    {
        public string Document;
        public string Start;
        public IReadOnlyList<NavPage> Pages = Array.Empty<NavPage>();
        public IReadOnlyList<NavEdge> Edges = Array.Empty<NavEdge>();
        public IReadOnlyList<NavModal> Modals = Array.Empty<NavModal>();
        public IReadOnlyList<NavTabGroup> Tabs = Array.Empty<NavTabGroup>();
    }

    public sealed class AnimStep
    {
        public double Offset;
        public string Easing;
        public IReadOnlyDictionary<string, string> Set = Empty.Strings;
    }

    public sealed class AnimDef
    {
        public string Id;
        public string Keyframes;
        public double Duration;
        public double Delay;
        public int Iterations = 1;
        public string Direction = "normal";
        public string Fill = "none";
        public string Easing;
        public IReadOnlyList<AnimStep> Steps = Array.Empty<AnimStep>();
    }

    public sealed class AnimBinding
    {
        public string Selector;
        public string Animation;
        public string Trigger;
        public double Stagger;
        public string ReducedMotion;
    }

    public sealed class AnimModel
    {
        public IReadOnlyList<AnimDef> Animations = Array.Empty<AnimDef>();
        public IReadOnlyList<AnimBinding> Bindings = Array.Empty<AnimBinding>();
    }

    public sealed class MediaQuery
    {
        public double? MinWidth, MaxWidth, MinHeight, MaxHeight, MinAspect, MaxAspect;
        public string Orientation;
    }

    public sealed class MediaClass
    {
        public string Selector;
        public string Class;
        public MediaQuery Query = new MediaQuery();
    }

    public sealed class MediaQuerySpec
    {
        public string Root;
        public IReadOnlyList<MediaClass> Classes = Array.Empty<MediaClass>();
    }

    public sealed class Specs
    {
        public AnimModel Anim;
        public NavModel Nav;
        public MediaQuerySpec Media;
    }

    public sealed class AnalysisResult
    {
        public IReadOnlyList<UiDocument> Documents = Array.Empty<UiDocument>();
        public IStyleModel Style;
        public IReadOnlyDictionary<string, IStyleModel> StyleByDocument = new Dictionary<string, IStyleModel>(0);
        public TokenTable Tokens;
        public KeyTable Keys;
        public NavModel Nav;
        public AnimModel Anim;
        public IReadOnlyList<Construct> Constructs = Array.Empty<Construct>();
    }

    public sealed class ConversionPlan
    {
        public IReadOnlyList<(Construct Construct, TierDecision Decision)> Actions = Array.Empty<(Construct, TierDecision)>();
    }

    public enum SheetRole { Tokens, Shared, Page, Skin, PassThrough }

    public sealed class SheetEntry
    {
        public string Id;
        public string RelativePath;
        public SheetRole Role;
        public Stylesheet Sheet;
    }

    public sealed class SheetSet
    {
        public IReadOnlyList<SheetEntry> Sheets = Array.Empty<SheetEntry>();
    }

    public sealed class SheetBuilder
    {
        private readonly List<string> _ids = new List<string>();
        private readonly Dictionary<string, List<StyleRule>> _rules = new Dictionary<string, List<StyleRule>>(StringComparer.Ordinal);

        public IReadOnlyList<string> SheetIds => _ids;

        public void AddRule(string sheetId, StyleRule rule)
        {
            if (sheetId == null) throw new ArgumentNullException(nameof(sheetId));
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            List<StyleRule> list;
            if (!_rules.TryGetValue(sheetId, out list))
            {
                list = new List<StyleRule>();
                _rules[sheetId] = list;
                _ids.Add(sheetId);
            }
            list.Add(rule);
        }

        public IReadOnlyList<StyleRule> Rules(string sheetId)
        {
            List<StyleRule> list;
            return sheetId != null && _rules.TryGetValue(sheetId, out list) ? (IReadOnlyList<StyleRule>)list : Array.Empty<StyleRule>();
        }

        public void ReplaceRules(string sheetId, IReadOnlyList<StyleRule> rules)
        {
            if (sheetId == null) throw new ArgumentNullException(nameof(sheetId));
            List<StyleRule> list;
            if (!_rules.TryGetValue(sheetId, out list))
            {
                list = new List<StyleRule>();
                _rules[sheetId] = list;
                _ids.Add(sheetId);
            }
            list.Clear();
            if (rules != null) list.AddRange(rules);
        }
    }

    public sealed class GeneratedAsset
    {
        public string RelativePath;
        public byte[] Content;
        public string GeneratedBy;
    }

    public interface IAssetSink
    {
        string Add(GeneratedAsset asset);
    }

    public interface ISpecSink
    {
        void AddAnimation(AnimDef animation);
        void AddAnimationBinding(AnimBinding binding);
        void AddMediaClass(string rootName, MediaClass mediaClass);
        void SetNav(NavModel nav);
    }

    public interface INameGenerator
    {
        string Generate(string role, string constructKey);
    }

    public interface IElementEditor
    {
        void InsertChild(UxmlElement parent, int index, UxmlElement child);
        void AppendChild(UxmlElement parent, UxmlElement child);
        void RemoveElement(UxmlElement element);
        void ReplaceElement(UxmlElement element, UxmlElement replacement);
        UxmlElement Wrap(UxmlElement element, UxmlElement wrapper);
        void MoveChildren(UxmlElement from, UxmlElement to);
        void AddClass(UxmlElement element, string className);
        void RemoveClass(UxmlElement element, string className);
        void SetAttribute(UxmlElement element, string name, string value);
    }

    public sealed class RuntimeComponentEntry
    {
        public string ComponentId;
        public string Selector;
        public string EmulationId;
        public IReadOnlyDictionary<string, string> Parameters = Empty.Strings;
        public SourceSpan Span;
    }

    public sealed class KeyMockText
    {
        public string Key;
        public string Text;
        public string ElementName;
        public string Document;
        public SourceSpan Span;
    }

    public sealed class TransformedModel
    {
        public IReadOnlyList<RuntimeComponentEntry> RuntimeEntries = Array.Empty<RuntimeComponentEntry>();
        public IReadOnlyList<KeyMockText> KeyTexts = Array.Empty<KeyMockText>();
        public IReadOnlyList<UiDocument> Documents = Array.Empty<UiDocument>();
        public SheetSet Sheets = new SheetSet();
        public IReadOnlyList<GeneratedAsset> Assets = Array.Empty<GeneratedAsset>();
        public Specs Specs = new Specs();
        public IReadOnlyList<string> RuntimeComponents = Array.Empty<string>();
    }

    public enum FileKind { Uxml, Uss, Json, Png, Text, Other }

    public sealed class OutputFile
    {
        public string RelativePath;
        public byte[] Content;
        public FileKind Kind;
    }

    public sealed class OutputSet
    {
        public IReadOnlyList<OutputFile> Files = Array.Empty<OutputFile>();
    }
}
