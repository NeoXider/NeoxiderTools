using System;

namespace Neo.HtmlToUitk.Web
{
    /// <summary>Acquisition/crawl scope; not a browser-lifetime navigation or resource sandbox.</summary>
    public enum HtmlPageScope
    {
        CurrentPage,
        SameDomain,
        AllLinked
    }

    public enum HtmlRuntimeBackendKind
    {
        Auto,
        Native,
        Web
    }

    [Flags]
    public enum HtmlRuntimeFeatures
    {
        None = 0,
        JavaScript = 1,
        Animations = 2,
        Interactions = 4,
        Resources = 8,
        Canvas = 16,
        All = JavaScript | Animations | Interactions | Resources | Canvas
    }

    [Flags]
    public enum HtmlRuntimeNavigationRestrictions
    {
        None = 0,
        TopLevelToRootDocument = 1,
        FramesToRootDocument = 2,
        All = TopLevelToRootDocument | FramesToRootDocument
    }

    /// <summary>
    /// Immutable enabled preferences plus explicit requirements, not installed backend claims.
    /// Content discovery and capability validation must precede loading/engine side effects.
    /// </summary>
    public sealed class HtmlRuntimeOptions
    {
        public HtmlRuntimeOptions(
            HtmlPageScope pageScope = HtmlPageScope.CurrentPage,
            HtmlRuntimeBackendKind backendKind = HtmlRuntimeBackendKind.Auto,
            HtmlRuntimeFeatures features = HtmlRuntimeFeatures.All,
            int maxPages = 1,
            int maxDepth = 0,
            TimeSpan? loadTimeout = null,
            int maxResourceRequests = 128,
            long maxResourceBytes = 8L * 1024 * 1024,
            long maxTotalResourceBytes = 64L * 1024 * 1024,
            int maxRedirects = 5,
            HtmlRuntimeFeatures requiredFeatures = HtmlRuntimeFeatures.None,
            HtmlRuntimeNavigationRestrictions navigationRestrictions = HtmlRuntimeNavigationRestrictions.None)
        {
            if (!Enum.IsDefined(typeof(HtmlPageScope), pageScope))
                throw new ArgumentOutOfRangeException(nameof(pageScope));
            if (!Enum.IsDefined(typeof(HtmlRuntimeBackendKind), backendKind))
                throw new ArgumentOutOfRangeException(nameof(backendKind));
            if ((features & ~HtmlRuntimeFeatures.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(features));
            if ((requiredFeatures & ~HtmlRuntimeFeatures.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(requiredFeatures));
            if ((navigationRestrictions & ~HtmlRuntimeNavigationRestrictions.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(navigationRestrictions));
            if (maxPages < 1)
                throw new ArgumentOutOfRangeException(nameof(maxPages));
            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (pageScope == HtmlPageScope.CurrentPage && (maxPages != 1 || maxDepth != 0))
                throw new ArgumentException("CurrentPage requires maxPages = 1 and maxDepth = 0.");
            TimeSpan timeout = loadTimeout ?? TimeSpan.FromSeconds(30);
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(loadTimeout));
            if (maxResourceRequests < 1)
                throw new ArgumentOutOfRangeException(nameof(maxResourceRequests));
            if (maxResourceBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(maxResourceBytes));
            if (maxTotalResourceBytes < maxResourceBytes)
                throw new ArgumentOutOfRangeException(nameof(maxTotalResourceBytes));
            if (maxRedirects < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRedirects));

            PageScope = pageScope;
            BackendKind = backendKind;
            Features = features;
            MaxPages = maxPages;
            MaxDepth = maxDepth;
            LoadTimeout = timeout;
            MaxResourceRequests = maxResourceRequests;
            MaxResourceBytes = maxResourceBytes;
            MaxTotalResourceBytes = maxTotalResourceBytes;
            MaxRedirects = maxRedirects;
            RequiredFeatures = requiredFeatures;
            NavigationRestrictions = navigationRestrictions;
        }

        public HtmlPageScope PageScope { get; }
        public HtmlRuntimeBackendKind BackendKind { get; }

        /// <summary>Enabled preferences for supported functionality, not content requirements.</summary>
        public HtmlRuntimeFeatures Features { get; }

        /// <summary>Caller requirements, added to separately discovered content requirements.</summary>
        public HtmlRuntimeFeatures RequiredFeatures { get; }

        /// <summary>
        /// Explicit exact-root navigation locks, separate from acquisition scope. None leaves browser
        /// navigation/frame behavior unchanged. A selected lock also applies to initial root redirects.
        /// </summary>
        public HtmlRuntimeNavigationRestrictions NavigationRestrictions { get; }

        /// <summary>Acquired documents including the final seed; not a browser-lifetime page limit.</summary>
        public int MaxPages { get; }

        /// <summary>Acquisition link depth; seed is zero and redirects do not increase it.</summary>
        public int MaxDepth { get; }

        /// <summary>Wall-clock deadline from LoadAsync entry, including initialization.</summary>
        public TimeSpan LoadTimeout { get; }

        /// <summary>
        /// Session-wide request budget including documents, redirects, frames, subresources and
        /// script network requests. This remains a bound when Resources is disabled.
        /// </summary>
        public int MaxResourceRequests { get; }

        /// <summary>Maximum decoded response bytes per resource, including a main document.</summary>
        public long MaxResourceBytes { get; }

        /// <summary>Session-wide decoded response bytes; this is not a JS/GPU memory limit.</summary>
        public long MaxTotalResourceBytes { get; }

        /// <summary>Maximum redirects per request chain, not a separate page budget.</summary>
        public int MaxRedirects { get; }
    }
}
