using System;

namespace Neo.HtmlToUitk.Web
{
    /// <summary>
    /// Provider claims for the actual platform and configuration. Constructing this value does not
    /// verify a browser, sandbox, renderer or Player build. Optional adapters supply their own evidence.
    /// </summary>
    public sealed class HtmlRuntimeCapabilities
    {
        public HtmlRuntimeCapabilities(
            HtmlRuntimeBackendKind backendKind,
            HtmlRuntimeFeatures supportedFeatures,
            HtmlRuntimeFeatures configurableFeatures,
            bool supportsLinkedPages = false,
            bool enforcesNavigationPolicy = false,
            bool enforcesLoadBounds = false)
        {
            if (backendKind != HtmlRuntimeBackendKind.Native && backendKind != HtmlRuntimeBackendKind.Web)
                throw new ArgumentOutOfRangeException(nameof(backendKind), "Report a concrete backend, not Auto.");
            if ((supportedFeatures & ~HtmlRuntimeFeatures.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(supportedFeatures));
            if ((configurableFeatures & ~supportedFeatures) != 0)
                throw new ArgumentException("Configurable features must also be supported.", nameof(configurableFeatures));

            BackendKind = backendKind;
            SupportedFeatures = supportedFeatures;
            ConfigurableFeatures = configurableFeatures;
            SupportsLinkedPages = supportsLinkedPages;
            EnforcesNavigationPolicy = enforcesNavigationPolicy;
            EnforcesLoadBounds = enforcesLoadBounds;
        }

        public HtmlRuntimeBackendKind BackendKind { get; }
        public HtmlRuntimeFeatures SupportedFeatures { get; }

        /// <summary>
        /// Features the provider can disable as well as enable. Without this claim a supported
        /// feature is always on; requesting it off is rejected rather than ignored.
        /// </summary>
        public HtmlRuntimeFeatures ConfigurableFeatures { get; }

        /// <summary>Can acquire linked documents with the requested crawl scope and bounds.</summary>
        public bool SupportsLinkedPages { get; }

        /// <summary>
        /// Can enforce explicit top-level/frame navigation locks before requests, including redirects.
        /// This is optional, independent of crawl admission, and must not be claimed for notification-only APIs.
        /// </summary>
        public bool EnforcesNavigationPolicy { get; }

        /// <summary>
        /// All requested acquisition page/depth, deadline, session request/byte and redirect limits can be enforced.
        /// Observing a violation after an unbounded transfer is not enforcement.
        /// </summary>
        public bool EnforcesLoadBounds { get; }

        /// <summary>
        /// Call after actual content/provider discovery, before creating/loading a document. An explicit
        /// None asserts that inspected content has no feature requirements; this method is not an inspector.
        /// Return effective supported settings, without discarding any required content behavior.
        /// </summary>
        public HtmlRuntimeEffectiveSettings ValidateOptions(HtmlRuntimeOptions options, HtmlRuntimeFeatures contentRequiredFeatures)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if ((contentRequiredFeatures & ~HtmlRuntimeFeatures.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(contentRequiredFeatures));
            if (options.BackendKind != HtmlRuntimeBackendKind.Auto && options.BackendKind != BackendKind)
                throw new NotSupportedException("Requested backend kind is unavailable: " + options.BackendKind + ".");

            HtmlRuntimeFeatures required = options.RequiredFeatures | contentRequiredFeatures;
            HtmlRuntimeFeatures disabled = required & ~options.Features;
            if (disabled != HtmlRuntimeFeatures.None)
                throw new NotSupportedException("Required features are disabled by the caller: " + disabled + ".");
            HtmlRuntimeFeatures missing = required & ~SupportedFeatures;
            if (missing != HtmlRuntimeFeatures.None)
                throw new NotSupportedException("Required features are unsupported: " + missing + ".");

            HtmlRuntimeFeatures cannotDisable = SupportedFeatures & ~options.Features & ~ConfigurableFeatures;
            if (cannotDisable != HtmlRuntimeFeatures.None)
                throw new NotSupportedException("Requested features cannot be disabled: " + cannotDisable + ".");
            if (options.PageScope != HtmlPageScope.CurrentPage && !SupportsLinkedPages)
                throw new NotSupportedException("This backend supports only CurrentPage.");
            if (options.NavigationRestrictions != HtmlRuntimeNavigationRestrictions.None && !EnforcesNavigationPolicy)
                throw new NotSupportedException("This backend cannot enforce the requested strict navigation restrictions.");
            if (!EnforcesLoadBounds)
                throw new NotSupportedException("This backend cannot enforce every requested load bound.");

            return new HtmlRuntimeEffectiveSettings(options, BackendKind, options.Features & SupportedFeatures, required);
        }
    }

    /// <summary>Validated provider selection/settings; not evidence that content inspection or loading occurred.</summary>
    public sealed class HtmlRuntimeEffectiveSettings
    {
        internal HtmlRuntimeEffectiveSettings(HtmlRuntimeOptions options, HtmlRuntimeBackendKind backendKind,
            HtmlRuntimeFeatures features, HtmlRuntimeFeatures requiredFeatures)
        {
            Options = options;
            BackendKind = backendKind;
            Features = features;
            RequiredFeatures = requiredFeatures;
        }

        public HtmlRuntimeOptions Options { get; }
        public HtmlRuntimeBackendKind BackendKind { get; }
        public HtmlRuntimeFeatures Features { get; }
        public HtmlRuntimeFeatures RequiredFeatures { get; }
    }
}
