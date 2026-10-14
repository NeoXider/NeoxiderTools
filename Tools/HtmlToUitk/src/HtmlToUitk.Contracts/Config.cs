using System;
using System.Collections.Generic;

namespace HtmlToUitk.Convert.Config
{
    public sealed class LayoutConfig
    {
        public string Screens = "Screens";
        public string Skin = "Skin";
        public string Strings = "Localization";
        public string Shared = "Shared";
        public string Generated = "Generated";
    }

    public sealed class UnityConfig
    {
        public string Version = "6000.6";
        public string Capabilities = "spec/capabilities.6000.6.json";
        public string PanelSettings;
        public IReadOnlyList<int> ReferenceResolution = new[] { 1920, 1080 };
    }

    public sealed class LocalizationConfig
    {
        public IReadOnlyList<string> Files = Array.Empty<string>();
        public string Format = "locale-map";
        public IReadOnlyList<string> Locales = new[] { "en" };
        public string MockLocale = "en";
        public string KeyPattern = @"^[a-z][A-Za-z0-9_-]*(\.[A-Za-z0-9_-]+)+$";
        public string Placeholder = "[TODO] ";
        public bool AllowGlyphOnly = true;
        public string KeysStub = "{screen}.newkeys.json";
    }

    public sealed class NamingConfig
    {
        public string NamePattern = @"^[a-z][a-z0-9]*(-[a-z0-9]+)*$";
        public string ClassPattern = @"^[a-z][a-z0-9]*(-[a-z0-9]+)*(__[a-z0-9]+(-[a-z0-9]+)*)?(--[a-z0-9]+(-[a-z0-9]+)*)?$";
        public IReadOnlyList<string> Prefixes = Array.Empty<string>();
        public string DynamicClassPattern = "^is-";
        public IReadOnlyList<string> DynamicClasses = Array.Empty<string>();
    }

    public sealed class RulesConfig
    {
        public double MinFontSize = 13;
        public double MinTouchTarget = 40;
        public string UndefinedClass = "warn";
        public string ColorLiterals = "allow";
        public bool StrictWarnings;
        public IReadOnlyList<string> Disable = Array.Empty<string>();
        public bool AllowLayoutAnimation;
        public int MaxKeyframeSteps = 8;
    }

    public sealed class UxmlConfig
    {
        public int Indent = 4;
        public IReadOnlyList<string> Namespaces = new[] { "ui", "uie" };
        public IReadOnlyDictionary<string, string> InteractiveAttributes = Empty.Strings;
        public IReadOnlyList<string> AllowInlineStyles = new[] { "display: none;" };
        public string TemplateSrcStyle = "relative";
    }

    public sealed class UssConfig
    {
        public string TokenScopeSelector;
        public bool RedefineSkinTokens;
        public bool PruneDeadRules;
        public bool SectionOrder = true;
        public string UtilityPattern = "^u-";
    }

    public sealed class OutputConfig
    {
        public string TokensFile = "{skin}/{screen}.tokens.uss";
    }

    public sealed class EmulationConfig
    {
        public bool StrictNative;
        public bool AllowRuntime = true;
        public IReadOnlyList<string> Disable = Array.Empty<string>();
        public int GridMaxTracks = 24;
        public string BakedTextureFolder = "Generated";
        public double RootFontSize = 16;
        public IReadOnlyList<int> ReferenceResolution = new[] { 1920, 1080 };
    }

    public sealed class ExternalLintConfig
    {
        public IReadOnlyList<string> Command = Array.Empty<string>();
        public IReadOnlyList<string> Mirror = Array.Empty<string>();
        public string Cwd = ".";
    }

    public sealed class LintConfig
    {
        public bool Builtin = true;
        public ExternalLintConfig External;
    }

    public sealed class CodeBindingsConfig
    {
        public IReadOnlyList<string> Sources = Array.Empty<string>();
        public IReadOnlyList<string> Patterns = Array.Empty<string>();
        public IReadOnlyList<string> ConstantFiles = Array.Empty<string>();
        public string ConstantPattern;
    }

    public sealed class PreviewConfig
    {
        public IReadOnlyList<string> Theme = Array.Empty<string>();
        public string PanelSettings;
        public IReadOnlyList<string> Viewports = new[] { "1920x1080" };
        public string Backdrop = "#101418";
        public string Locale;
        public CodeBindingsConfig CodeBindings;
        public bool DefaultTheme = true;
        public bool Transitions;
        public IReadOnlyDictionary<string, string> TextOverrides = Empty.Strings;
        public IReadOnlyDictionary<string, string> Fragments = Empty.Strings;
    }

    public sealed class PagesConfig
    {
        public string Backend = "standalone";
        public string HiddenClass = "is-hidden";
        public string ActiveClass = "is-active";
    }

    public sealed class AnimationConfig
    {
        public string Backend = "animator-json";
        public string ReducedMotion = "simplify";
    }

    public sealed class ConversionConfig
    {
        public string ProjectRoot = ".";
        public string UiRoot = "UI";
        public LayoutConfig Layout = new LayoutConfig();
        public UnityConfig Unity = new UnityConfig();
        public LocalizationConfig Localization = new LocalizationConfig();
        public NamingConfig Naming = new NamingConfig();
        public RulesConfig Rules = new RulesConfig();
        public IReadOnlyDictionary<string, string> Fonts = Empty.Strings;
        public UxmlConfig Uxml = new UxmlConfig();
        public UssConfig Uss = new UssConfig();
        public OutputConfig Output = new OutputConfig();
        public EmulationConfig Emulation = new EmulationConfig();
        public LintConfig Lint = new LintConfig();
        public PreviewConfig Preview = new PreviewConfig();
        public PagesConfig Pages = new PagesConfig();
        public AnimationConfig Animation = new AnimationConfig();
    }

    public sealed class JobInputs
    {
        public IReadOnlyList<string> Html = Array.Empty<string>();
        public IReadOnlyList<string> Css = Array.Empty<string>();
        public string CssOrder = "before";
        public IReadOnlyList<string> Uss = Array.Empty<string>();
        public IReadOnlyList<string> Partials = Array.Empty<string>();
    }

    public sealed class JobFolders
    {
        public string Uxml = "Screens";
        public string Uss = "Screens";
        public string Shared = "Shared";
        public string Tokens = "Shared";
        public string Strings = "Localization";
        public string Generated = "Generated";
        public string Components = "Components";
    }

    public sealed class JobFileOutput
    {
        public string Name;
        public string Folder;
        public string Assemble;
    }

    public sealed class JobOutput
    {
        public string Root;
        public string Assemble = "perFile";
        public JobFolders Folders = new JobFolders();
        public IReadOnlyDictionary<string, JobFileOutput> PerFile = new Dictionary<string, JobFileOutput>(0);
        public string Partials = "templates";
    }

    public sealed class JobWatch
    {
        public bool Enabled;
        public int DebounceMs = 400;
    }

    public sealed class JobSpec
    {
        public int Schema = 1;
        public string Name;
        public string Config;
        public string ConfigOverridesJson;
        public JobInputs Inputs = new JobInputs();
        public JobOutput Output = new JobOutput();
        public JobWatch Watch = new JobWatch();
    }
}
