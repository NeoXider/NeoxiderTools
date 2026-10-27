using System;
using System.Collections.Generic;
using System.Net.Mail;
using Neo.HtmlToUitk.Animation;
using Neo.HtmlToUitk.Json;
using Neo.HtmlToUitk.Localization;
using Neo.HtmlToUitk.Navigation;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Typed interaction adapter for the MIT Velora source converted by HtmlToUitk.
/// The tree, styles, responsive rules, routes and keyframes are converter outputs.
/// React forms, theme switching and the typewriter are the explicit C# adaptation.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(UIDocument), typeof(UitkScreen))]
public sealed class VeloraShowcaseController : MonoBehaviour
{
    public const string OriginalUrl = "https://velora.colorlib.com/templates/saas";
    public const string ComponentsUrl = "https://velora.colorlib.com/components";
    public const string BlocksUrl = "https://velora.colorlib.com/blocks";
    public const string TemplatesUrl = "https://velora.colorlib.com/templates";
    public const string ThemesUrl = "https://velora.colorlib.com/themes";
    public const string ChangelogUrl = "https://velora.colorlib.com/changelog";
    public const string InstallCommand = "npx shadcn@latest add @velora/aurora-background";
    private const string ResourceRoot = "Velora/";

    [SerializeField] private UIDocument document;
    [SerializeField] private UitkScreen screen;
    [SerializeField] private VisualTreeAsset visualTree;
    [SerializeField] private TextAsset navigation;
    [SerializeField] private TextAsset animations;
    [SerializeField] private TextAsset mediaQueries;
    [SerializeField] private TextAsset localization;
    [SerializeField] private Font interfaceFont;

    private readonly List<IDisposable> _actions = new List<IDisposable>();
    private readonly List<Action> _releaseCallbacks = new List<Action>();
    private readonly string[] _words = { "alive.", "effortless.", "unforgettable." };
    private VisualElement _root;
    private VisualElement _app;
    private UitkRouter _router;
    private IVisualElementScheduledItem _typewriter;
    private IVisualElementScheduledItem _scrollRequest;
    private TextElement _word;
    private int _wordIndex;
    private int _characterCount;
    private bool _deleting;
    private double _nextCharacterTime;
    private string _pendingScroll;
    private bool _initialized;
    private bool _synchronizing;

    public string CurrentPage { get; private set; } = "home";
    public bool DarkTheme { get; private set; }
    public bool ReducedMotion { get; private set; }
    public int InteractionCount { get; private set; }
    public string Status { get; private set; }

    private void OnEnable()
    {
        if (!_initialized) Initialize();
        else RefreshBindings();
    }

    private void Update()
    {
        RefreshBindings();
    }

    private void OnDisable()
    {
        ReleaseBindings();
    }

    /// <summary>Uses serialized assets when supplied, otherwise the included Resources assets.</summary>
    public void Initialize()
    {
        ReleaseBindings();
        if (document == null) document = GetComponent<UIDocument>();
        if (screen == null) screen = GetComponent<UitkScreen>();
        if (visualTree == null) visualTree = Resources.Load<VisualTreeAsset>(ResourceRoot + "Screens/Velora");
        if (navigation == null) navigation = Resources.Load<TextAsset>(ResourceRoot + "Screens/Velora.nav");
        if (animations == null) animations = Resources.Load<TextAsset>(ResourceRoot + "Screens/Velora.anim");
        if (mediaQueries == null) mediaQueries = Resources.Load<TextAsset>(ResourceRoot + "Screens/Velora.mq");
        if (localization == null) localization = Resources.Load<TextAsset>(ResourceRoot + "Velora.strings");
        if (interfaceFont == null) interfaceFont = Resources.Load<Font>(ResourceRoot + "Fonts/Geist-Regular");

        if (document == null || screen == null || visualTree == null || navigation == null ||
            animations == null || mediaQueries == null || localization == null)
        {
            Status = "Missing showcase assets. Keep Resources/Velora together or assign the serialized fields.";
            Debug.LogError("[Velora showcase] " + Status, this);
            return;
        }

        document.visualTreeAsset = visualTree;
        screen.Localizer = new SourceLocalizer(localization.text);
        screen.Configure(document, navigation, animations, mediaQueries);
        _initialized = true;
        RefreshBindings();
        if (!string.IsNullOrEmpty(screen.LastError))
        {
            Status = screen.LastError;
            Debug.LogError("[Velora showcase] " + Status, this);
        }
    }

    /// <summary>
    /// Synchronizes this adapter with the screen's current projection, including manual roots.
    /// Hosts may call this immediately after ReloadRoot/ConfigureRoot; Update also observes changes.
    /// This method never configures the screen or replaces its root.
    /// </summary>
    public bool RefreshBindings()
    {
        if (_synchronizing) return _root != null && _router != null;
        VisualElement currentRoot = isActiveAndEnabled && screen != null && screen.isActiveAndEnabled ? screen.Root : null;
        UitkRouter currentRouter = currentRoot != null ? screen.Router : null;
        if (ReferenceEquals(currentRoot, _root) && ReferenceEquals(currentRouter, _router))
            return _root != null && _router != null;

        _synchronizing = true;
        try
        {
            ReleaseBindings();
            _root = currentRoot;
            _router = currentRouter;
            if (_root == null || _router == null)
            {
                Status = "Waiting for an active UitkScreen projection.";
                return false;
            }

            _app = Find<VisualElement>("velora-root");
            _app?.EnableInClassList("vl-dark", DarkTheme);
            if (interfaceFont != null) _root.style.unityFontDefinition = FontDefinition.FromFont(interfaceFont);
            if (screen.Animator != null) screen.Animator.ReducedMotion = ReducedMotion;
            _router.OnShown += OnPageShown;
            _router.OnWillHide += OnPageWillHide;
            _router.OnModalOpened += OnModalOpened;
            BindHeader();
            BindActions();
            BindFaq("home", 4);
            BindFaq("pricing", 3);
            BindSearch();
            ConfigureScrolling();
            StartTypewriter();
            ApplyActiveNavigation(_router.Current);
            SetText("theme-toggle", DarkTheme ? "☀" : "☾");
            SetText("motion-toggle", ReducedMotion ? "Motion off" : "Motion on");
            if (ReducedMotion && _word != null) _word.text = "alive.";
            Status = "Ready: native HTML→UITK routes, gradients, keyframes and media queries.";
            return true;
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private void BindHeader()
    {
        Bind("brand-home", () => Navigate("home"));
        Bind("nav-templates", () => OpenExternal(TemplatesUrl));
        Bind("nav-pricing", () => Navigate("pricing"));
        Bind("nav-changelog", () => OpenExternal(ChangelogUrl));
        Bind("mobile-home", () => OpenExternal(TemplatesUrl));
        Bind("mobile-pricing", () => Navigate("pricing"));
        Bind("mobile-about", () => Navigate("about"));
        Bind("mobile-contact", () => Navigate("contact"));
        Bind("mobile-search", OpenSearch);
        Bind("nav-components", () => OpenExternal(ComponentsUrl));
        Bind("nav-blocks", () => OpenExternal(BlocksUrl));
        Bind("mobile-features", () => OpenExternal(ComponentsUrl));
        Bind("mobile-blocks", () => OpenExternal(BlocksUrl));
        Bind("mobile-themes", () => OpenExternal(ThemesUrl));
        Bind("mobile-changelog", () => OpenExternal(ChangelogUrl));
        Bind("nav-themes", () => OpenExternal(ThemesUrl));
        Bind("theme-toggle", ToggleTheme);
        Bind("motion-toggle", ToggleMotion);
        Bind("menu-toggle", ToggleMenu);
        Bind("search-open", OpenSearch);
        Bind("original-open", OpenOriginal);
    }

    private void BindActions()
    {
        Bind("home-browse", () => ScrollHomeTo("features-section"));
        Bind("free-browse", () => OpenExternal(ComponentsUrl));
        Bind("dashboard-period", CycleDashboardPeriod);
        Bind("about-original", OpenOriginal);
        Bind("start-source", OpenOriginal);
        Bind("copy-command", CopyInstallCommand);
        Bind("contact-submit", SubmitContact);
        Bind("contact-again", ResetContact);
        Bind("waitlist-submit", JoinWaitlist);
        string[] pages = { "home", "pricing", "about", "contact" };
        for (int i = 0; i < pages.Length; i++)
        {
            Bind("footer-source-" + pages[i], OpenOriginal);
        }
    }

    private void BindFaq(string page, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int index = i;
            Bind(page + "-faq-" + index, () => ToggleFaq(page, index, count));
        }
    }

    private void BindSearch()
    {
        TextField input = Find<TextField>("search-input");
        if (input != null)
        {
            EventCallback<ChangeEvent<string>> callback = change =>
            {
                if (RefreshBindings() && ReferenceEquals(input, Find<TextField>("search-input")))
                    FilterSearch(change.newValue);
            };
            input.RegisterValueChangedCallback(callback);
            _releaseCallbacks.Add(() => input.UnregisterValueChangedCallback(callback));
        }
        Bind("search-aurora", () => SelectSearchResult("home-hero"));
        Bind("search-text", () => SelectSearchResult("home-hero"));
        Bind("search-marquee", () => SelectSearchResult("stack-marquee"));
        Bind("search-chart", () => SelectSearchResult("home-dashboard"));
    }

    private void ConfigureScrolling()
    {
        _root.Query<ScrollView>(className: "vl-page").ForEach(scroll =>
        {
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scroll.mouseWheelScrollSize = 72f;
            StyleNativeScroller(scroll.verticalScroller);
        });
    }

    private static void StyleNativeScroller(Scroller scroller)
    {
        // Generated media classes belong to authored nodes; native ScrollView children do not
        // receive them. Style only the native chrome here, keeping its slider/input handlers.
        scroller.style.width = 8f;
        scroller.style.minWidth = 8f;
        scroller.style.maxWidth = 8f;
        scroller.style.flexShrink = 0f;
        scroller.lowButton.style.display = DisplayStyle.None;
        scroller.highButton.style.display = DisplayStyle.None;
        Slider slider = scroller.slider;
        slider.style.flexGrow = 1f;
        slider.style.flexShrink = 1f;
        VisualElement input = slider.Q<VisualElement>(className: BaseSlider<float>.inputUssClassName);
        VisualElement container = slider.Q<VisualElement>(className: BaseSlider<float>.dragContainerUssClassName);
        VisualElement tracker = slider.Q<VisualElement>(className: BaseSlider<float>.trackerUssClassName);
        VisualElement dragger = slider.Q<VisualElement>(className: BaseSlider<float>.draggerUssClassName);
        VisualElement border = slider.Q<VisualElement>(className: BaseSlider<float>.draggerBorderUssClassName);
        VisualElement[] chrome = { scroller, slider, input, container, tracker, dragger };
        for (int i = 0; i < chrome.Length; i++)
        {
            VisualElement element = chrome[i];
            if (element == null) continue;
            IStyle style = element.style;
            style.backgroundImage = StyleKeyword.None;
            style.backgroundColor = Color.clear;
            style.borderLeftWidth = 0f;
            style.borderRightWidth = 0f;
            style.borderTopWidth = 0f;
            style.borderBottomWidth = 0f;
            style.marginLeft = 0f;
            style.marginRight = 0f;
            style.marginTop = 0f;
            style.marginBottom = 0f;
            style.paddingLeft = 0f;
            style.paddingRight = 0f;
            style.paddingTop = 0f;
            style.paddingBottom = 0f;
            style.minWidth = 8f;
            style.width = 8f;
        }
        if (border != null) border.style.display = DisplayStyle.None;
        if (dragger == null) return;
        dragger.style.width = 6f;
        dragger.style.minWidth = 6f;
        dragger.style.minHeight = 30f;
        dragger.style.marginLeft = 1f;
        dragger.style.backgroundColor = new Color(0.45f, 0.52f, 0.63f, 0.72f);
        dragger.style.borderTopLeftRadius = 4f;
        dragger.style.borderTopRightRadius = 4f;
        dragger.style.borderBottomLeftRadius = 4f;
        dragger.style.borderBottomRightRadius = 4f;
    }

    public void Navigate(string page)
    {
        if (!RefreshBindings()) return;
        CloseMenu();
        _pendingScroll = null;
        if (_router == null) return;
        if (_router.Current == page)
        {
            ScrollView current = Find<ScrollView>("page-" + page);
            if (current != null) current.scrollOffset = Vector2.zero;
            return;
        }
        _router.Navigate(page);
    }

    public void ScrollHomeTo(string elementName)
    {
        if (!RefreshBindings()) return;
        CloseMenu();
        _pendingScroll = elementName;
        if (_router == null) return;
        if (_router.Current != "home") _router.Navigate("home");
        else ScheduleScroll(elementName);
    }

    private void OnPageShown(string page)
    {
        if (!HasCurrentProjection()) { RefreshBindings(); return; }
        CurrentPage = page;
        InteractionCount++;
        CloseMenu();
        ApplyActiveNavigation(page);
        ScrollView scroll = Find<ScrollView>("page-" + page);
        if (scroll != null) scroll.scrollOffset = Vector2.zero;
        if (page == "home" && _pendingScroll != null) ScheduleScroll(_pendingScroll);
    }

    private void OnPageWillHide(string page)
    {
        if (!HasCurrentProjection()) { RefreshBindings(); return; }
        VisualElement pageRoot = Find<VisualElement>("page-" + page);
        if (pageRoot == null || screen.Animator == null) return;
        pageRoot.Query<VisualElement>(className: "vl-loop").ForEach(element => screen.Animator.Stop(element));
    }

    private void OnModalOpened(string modal)
    {
        if (!HasCurrentProjection()) { RefreshBindings(); return; }
        CloseMenu();
    }

    private void ScheduleScroll(string elementName)
    {
        _scrollRequest?.Pause();
        VisualElement owner = _root;
        UitkRouter router = _router;
        _scrollRequest = _root.schedule.Execute(() =>
        {
            if (!HasCurrentProjection() || !ReferenceEquals(owner, _root) || !ReferenceEquals(router, _router)) return;
            ScrollView scroll = Find<ScrollView>("page-home");
            VisualElement target = Find<VisualElement>(elementName);
            if (scroll != null && target != null) scroll.ScrollTo(target);
            _pendingScroll = null;
        }).StartingIn(60);
    }

    private void ApplyActiveNavigation(string page)
    {
        CurrentPage = page;
        Find<Button>("nav-pricing")?.EnableInClassList("vl-nav-active", page == "pricing");
    }

    public void ToggleTheme()
    {
        RefreshBindings();
        DarkTheme = !DarkTheme;
        _app?.EnableInClassList("vl-dark", DarkTheme);
        SetText("theme-toggle", DarkTheme ? "☀" : "☾");
        Status = DarkTheme ? "Dark theme" : "Light theme";
    }

    public void ToggleMotion()
    {
        RefreshBindings();
        ReducedMotion = !ReducedMotion;
        if (screen != null && screen.Animator != null)
        {
            screen.Animator.StopAll(false);
            screen.Animator.ReducedMotion = ReducedMotion;
            if (!ReducedMotion)
            {
                VisualElement page = Find<VisualElement>("page-" + CurrentPage);
                screen.Animator.Play(AnimTrigger.OnShow, page);
            }
        }
        SetText("motion-toggle", ReducedMotion ? "Motion off" : "Motion on");
        if (ReducedMotion && _word != null) _word.text = "alive.";
        Status = ReducedMotion ? "Reduced motion enabled" : "Animations enabled";
    }

    private void ToggleMenu()
    {
        VisualElement menu = Find<VisualElement>("mobile-nav");
        if (menu == null) return;
        menu.EnableInClassList("vl-hidden", !menu.ClassListContains("vl-hidden"));
    }

    private void CloseMenu()
    {
        Find<VisualElement>("mobile-nav")?.AddToClassList("vl-hidden");
    }

    public void OpenOriginal()
    {
        OpenExternal(OriginalUrl);
    }

    private void OpenExternal(string url)
    {
        CloseMenu();
        Status = "Opening " + url;
        Application.OpenURL(url);
    }

    public void OpenSearch()
    {
        if (!RefreshBindings()) return;
        CloseMenu();
        _router?.OpenModal("search");
        TextField input = Find<TextField>("search-input");
        if (input != null)
        {
            input.value = string.Empty;
            input.Focus();
        }
        FilterSearch(string.Empty);
    }

    private void FilterSearch(string query)
    {
        string normalized = (query ?? string.Empty).Trim();
        string[] ids = { "search-aurora", "search-text", "search-marquee", "search-chart" };
        string[] searchTerms = { "aurora background gradient", "animated gradient text typewriter", "marquee logos testimonials", "dashboard mockup chart revenue" };
        int matches = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            bool show = normalized.Length == 0 || searchTerms[i].IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0;
            Find<Button>(ids[i])?.EnableInClassList("vl-hidden", !show);
            if (show) matches++;
        }
        SetText("search-status", matches == 0 ? "No matching adapted components. Try aurora, text, marquee or chart." : matches + " adapted components found.");
    }

    private void SelectSearchResult(string target)
    {
        _router?.CloseModal("search");
        ScrollHomeTo(target);
    }

    public void CopyInstallCommand()
    {
        RefreshBindings();
        GUIUtility.systemCopyBuffer = InstallCommand;
        SetText("start-feedback", "Copied to clipboard. Run this in your React project.");
        SetText("copy-command", "✓  Copied");
        Status = "Install command copied";
    }

    public void SubmitContact()
    {
        if (!RefreshBindings()) return;
        string person = Value("contact-name").Trim();
        string email = Value("contact-email").Trim();
        string message = Value("contact-message").Trim();
        if (person.Length == 0 || !ValidEmail(email) || message.Length < 10)
        {
            SetText("contact-feedback", "Enter your name, a valid email and a message of at least 10 characters.");
            Status = "Contact validation failed";
            return;
        }
        Find<VisualElement>("contact-fields")?.AddToClassList("vl-hidden");
        Find<VisualElement>("contact-success")?.RemoveFromClassList("vl-hidden");
        Status = "Contact validated locally; no data transmitted";
    }

    public void ResetContact()
    {
        if (!RefreshBindings()) return;
        Find<VisualElement>("contact-fields")?.RemoveFromClassList("vl-hidden");
        Find<VisualElement>("contact-success")?.AddToClassList("vl-hidden");
        SetText("contact-feedback", "Demo form — no data leaves this Unity player.");
        Status = "Contact form reopened";
    }

    public void JoinWaitlist()
    {
        if (!RefreshBindings()) return;
        if (!ValidEmail(Value("waitlist-email").Trim()))
        {
            SetText("waitlist-feedback", "Enter a valid email to preview the waitlist state.");
            Status = "Waitlist validation failed";
            return;
        }
        SetText("waitlist-feedback", "You're on the local demo list. No email was transmitted.");
        SetText("waitlist-submit", "✓  Added locally");
        Find<Button>("waitlist-submit")?.SetEnabled(false);
        Status = "Waitlist preview completed locally";
    }

    private static bool ValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        int at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@') || email.IndexOf(' ') >= 0 ||
            email.IndexOf('\r') >= 0 || email.IndexOf('\n') >= 0 || email.IndexOf('\t') >= 0) return false;
        try
        {
            MailAddress address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.Ordinal) && address.DisplayName.Length == 0 &&
                address.Host.IndexOf('.') > 0 && !address.Host.EndsWith(".", StringComparison.Ordinal) &&
                address.Host.IndexOf("..", StringComparison.Ordinal) < 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void ToggleFaq(string page, int selected, int count)
    {
        VisualElement selectedAnswer = Find<VisualElement>(page + "-answer-" + selected);
        bool open = selectedAnswer != null && selectedAnswer.ClassListContains("vl-hidden");
        for (int i = 0; i < count; i++)
        {
            VisualElement answer = Find<VisualElement>(page + "-answer-" + i);
            answer?.EnableInClassList("vl-hidden", i != selected || !open);
            Button question = Find<Button>(page + "-faq-" + i);
            if (question == null) continue;
            string original = question.text.TrimEnd(' ', '+', '−');
            question.text = original + (i == selected && open ? "  −" : "  +");
        }
    }

    private void CycleDashboardPeriod()
    {
        Button period = Find<Button>("dashboard-period");
        bool quarter = period != null && period.text == "Last 30 days";
        SetText("dashboard-period", quarter ? "Last 90 days" : "Last 30 days");
        SetText("dashboard-stat-value-0", quarter ? "$142,870" : "$48,291");
        SetText("dashboard-stat-value-1", quarter ? "34,260" : "12,480");
        SetText("dashboard-stat-value-2", quarter ? "5.1%" : "4.6%");
        SetText("dashboard-chart-title", quarter ? "Quarterly revenue" : "Monthly revenue");
        VisualElement bars = Find<VisualElement>("revenue-bars");
        if (bars != null && screen.Animator != null)
        {
            bars.Query<VisualElement>(className: "vl-bar").ForEach(bar => screen.Animator.PlayManual("bar-rise", bar));
        }
        Status = quarter ? "Dashboard: 90 days" : "Dashboard: 30 days";
    }

    private void StartTypewriter()
    {
        _word = Find<TextElement>("hero-word");
        if (_word == null) return;
        _wordIndex = 0;
        _characterCount = _words[0].Length;
        _deleting = true;
        _nextCharacterTime = Time.realtimeSinceStartupAsDouble + 2.2;
        _typewriter = _root.schedule.Execute(TypewriterTick).Every(90);
    }

    private void TypewriterTick()
    {
        if (!HasCurrentProjection()) { RefreshBindings(); return; }
        if (_word == null || ReducedMotion || CurrentPage != "home") return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (now < _nextCharacterTime) return;
        string current = _words[_wordIndex];
        if (_deleting)
        {
            _characterCount--;
            if (_characterCount <= 0)
            {
                _characterCount = 0;
                _deleting = false;
                _wordIndex = (_wordIndex + 1) % _words.Length;
                current = _words[_wordIndex];
                _nextCharacterTime = now + 0.25;
            }
        }
        else
        {
            _characterCount++;
            if (_characterCount >= current.Length)
            {
                _characterCount = current.Length;
                _deleting = true;
                _nextCharacterTime = now + 2.2;
            }
        }
        _word.text = current.Substring(0, _characterCount);
    }

    private void Bind(string name, Action action)
    {
        _actions.Add(screen.RegisterAction("vl-" + name, (Button button) =>
        {
            if (!RefreshBindings() || !ReferenceEquals(button, Find<Button>(name))) return;
            InteractionCount++;
            action();
        }));
    }

    private T Find<T>(string name) where T : VisualElement
    {
        if (_root == null) return null;
        string target = "vl-" + name;
        if (_root is T rootElement && rootElement.name == target) return rootElement;
        return _root.Q<T>(target);
    }

    private string Value(string name)
    {
        TextField field = Find<TextField>(name);
        return field != null ? field.value ?? string.Empty : string.Empty;
    }

    private void SetText(string name, string value)
    {
        TextElement element = Find<TextElement>(name);
        if (element != null) element.text = value;
    }

    private void ReleaseBindings()
    {
        _typewriter?.Pause();
        _scrollRequest?.Pause();
        _typewriter = null;
        _scrollRequest = null;
        if (_router != null)
        {
            _router.OnShown -= OnPageShown;
            _router.OnWillHide -= OnPageWillHide;
            _router.OnModalOpened -= OnModalOpened;
        }
        for (int i = 0; i < _actions.Count; i++) _actions[i].Dispose();
        _actions.Clear();
        for (int i = 0; i < _releaseCallbacks.Count; i++) _releaseCallbacks[i]();
        _releaseCallbacks.Clear();
        _root = null;
        _app = null;
        _router = null;
        _word = null;
        _pendingScroll = null;
    }

    private bool HasCurrentProjection()
    {
        return isActiveAndEnabled && screen != null && screen.isActiveAndEnabled &&
            _root != null && _router != null && ReferenceEquals(_root, screen.Root) &&
            ReferenceEquals(_router, screen.Router);
    }

    private sealed class SourceLocalizer : ILocalizer
    {
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>(StringComparer.Ordinal);

        public SourceLocalizer(string json)
        {
            JsonValue english = MiniJson.Parse(json)["en"];
            foreach (KeyValuePair<string, JsonValue> entry in english.Members)
            {
                _strings[entry.Key] = entry.Value.AsString(entry.Key);
            }
        }

        public string Resolve(string key)
        {
            return _strings.TryGetValue(key, out string value) ? value : key;
        }
    }
}
