using System;

namespace Neo.HtmlToUitk.Web
{
    /// <summary>
    /// Pure HTTP(S) acquisition policy with separate opt-in navigation locks. Not a fetcher/crawler,
    /// resource allowlist, SSRF defense or network/process sandbox. No local-file contract is supplied.
    /// </summary>
    public sealed class HtmlUrlPolicy
    {
        private readonly Func<string, string, bool> _hostPolicy;
        private readonly string _rootDocumentKey;

        /// <param name="hostPolicy">
        /// Required for SameDomain; receives normalized IDN ASCII hosts, without ports/trailing dots.
        /// Use ExactHost explicitly, or inject a reviewed public-suffix-aware matcher. No last-two-label
        /// heuristic is used. IP and localhost names always require exact host equality.
        /// </param>
        public HtmlUrlPolicy(string rootUrl, HtmlRuntimeOptions options = null,
            Func<string, string, bool> hostPolicy = null)
            : this(rootUrl, options, hostPolicy, false)
        {
        }

        private HtmlUrlPolicy(string rootUrl, HtmlRuntimeOptions options,
            Func<string, string, bool> hostPolicy, bool isRootQualified)
        {
            Options = options ?? new HtmlRuntimeOptions();
            if (!TryNormalize(rootUrl, null, out Uri normalized, out string diagnostic))
                throw new ArgumentException(diagnostic, nameof(rootUrl));
            if (Options.PageScope == HtmlPageScope.SameDomain && hostPolicy == null)
                throw new NotSupportedException("SameDomain requires an explicit host policy; registrable-domain matching is not installed.");

            RootUrl = normalized;
            _rootDocumentKey = DocumentKey(normalized);
            _hostPolicy = hostPolicy;
            IsRootQualified = isRootQualified;
        }

        /// <summary>Initial seed until qualification, then the final acquired root identity.</summary>
        public Uri RootUrl { get; }
        public HtmlRuntimeOptions Options { get; }
        public bool IsRootQualified { get; }

        /// <summary>
        /// Follow an initial root redirect before the final root identity exists. Each hop is still
        /// checked for HTTP(S), malformed input, userinfo, hop budget and any explicit top-level lock.
        /// SameDomain also checks both hosts; CurrentPage does not prematurely lock the seed identity.
        /// This is not a frame/resource or discovered-page redirect method.
        /// </summary>
        public bool TryResolveInitialRootRedirect(string location, Uri currentRoot, int redirectsFollowed,
            out Uri url, out string diagnostic)
        {
            url = null;
            if (IsRootQualified)
            {
                diagnostic = "The initial root has already been qualified.";
                return false;
            }
            if (!CanFollowRedirect(currentRoot, redirectsFollowed, out diagnostic))
                return false;
            if (!TryNormalize(currentRoot.OriginalString, null, out Uri basis, out diagnostic) ||
                !TryNormalize(location, basis, out Uri candidate, out diagnostic))
                return false;
            if (Options.PageScope == HtmlPageScope.SameDomain && (!IsHostInScope(basis) || !IsHostInScope(candidate)))
            {
                diagnostic = "The initial root redirect is outside the domain acquisition scope.";
                return false;
            }
            if (!IsNavigationAllowed(candidate, HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument))
            {
                diagnostic = "The initial root redirect violates the explicit top-level navigation lock.";
                return false;
            }

            url = candidate;
            diagnostic = string.Empty;
            return true;
        }

        /// <summary>
        /// Return an immutable policy anchored to the final root at document commit. The acquisition
        /// owner supplies the last accepted hop and owns its request/hop counters; this pure method
        /// cannot prove which requests ran. Call only after validating every initial redirect.
        /// </summary>
        public HtmlUrlPolicy QualifyRoot(Uri finalRoot)
        {
            if (IsRootQualified)
                throw new InvalidOperationException("The initial root has already been qualified.");
            if (finalRoot == null)
                throw new ArgumentNullException(nameof(finalRoot));
            if (!TryNormalize(finalRoot.OriginalString, null, out Uri candidate, out string diagnostic))
                throw new ArgumentException(diagnostic, nameof(finalRoot));
            if (Options.PageScope == HtmlPageScope.SameDomain && !IsHostInScope(candidate))
                throw new ArgumentException("The final root is outside the domain acquisition scope.", nameof(finalRoot));
            if (!IsNavigationAllowed(candidate, HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument))
                throw new ArgumentException("The final root violates the explicit top-level navigation lock.", nameof(finalRoot));
            return new HtmlUrlPolicy(candidate.AbsoluteUri, Options, _hostPolicy, true);
        }

        /// <summary>
        /// Admission of a discovered link for acquisition, after root qualification. This does not
        /// govern user navigation, embedded frames or resources. Null base means the qualified root.
        /// Failure returns a null URL and diagnostic; no request is performed.
        /// </summary>
        public bool TryResolveCrawlPage(string reference, Uri currentDocument, out Uri url, out string diagnostic)
        {
            url = null;
            if (!IsRootQualified)
            {
                diagnostic = "Qualify the initial root before admitting discovered pages.";
                return false;
            }
            Uri basis = RootUrl;
            if (currentDocument != null && !TryNormalize(currentDocument.OriginalString, null, out basis, out diagnostic))
                return false;
            if (!IsCrawlInScope(basis))
            {
                diagnostic = "The base document is outside the acquisition scope.";
                return false;
            }
            if (!TryNormalize(reference, basis, out Uri candidate, out diagnostic))
                return false;
            if (!IsCrawlInScope(candidate))
            {
                diagnostic = "The discovered document is outside " + Options.PageScope + " acquisition scope.";
                return false;
            }

            url = candidate;
            diagnostic = string.Empty;
            return true;
        }

        /// <summary>
        /// Revalidate every redirect of an acquired discovered page against the qualified root scope,
        /// before following it. Redirects consume requests/hops, not another acquisition page/depth slot.
        /// This is distinct from initial root, frame, resource and browser-lifetime navigation redirects.
        /// </summary>
        public bool TryResolveCrawlRedirect(string location, Uri currentDocument, int redirectsFollowed,
            out Uri url, out string diagnostic)
        {
            url = null;
            if (!CanFollowRedirect(currentDocument, redirectsFollowed, out diagnostic))
                return false;
            return TryResolveCrawlPage(location, currentDocument, out url, out diagnostic);
        }

        /// <summary>HTTP(S) top-level navigation; crawl scope is ignored, explicit locks are checked.</summary>
        public bool TryResolveTopLevelNavigation(string reference, Uri currentDocument, out Uri url, out string diagnostic)
        {
            return TryResolveNavigation(reference, currentDocument,
                HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument, out url, out diagnostic);
        }

        /// <summary>HTTP(S) frame navigation; crawl scope is ignored, explicit frame locks are checked.</summary>
        public bool TryResolveFrameNavigation(string reference, Uri currentDocument, out Uri url, out string diagnostic)
        {
            return TryResolveNavigation(reference, currentDocument,
                HtmlRuntimeNavigationRestrictions.FramesToRootDocument, out url, out diagnostic);
        }

        /// <summary>
        /// Normalize an HTTP(S) resource reference only. Neither crawl scope nor document locks are
        /// resource network isolation. A separate network policy would be needed for stronger restrictions.
        /// </summary>
        public bool TryResolveResource(string reference, Uri currentDocument, out Uri url, out string diagnostic)
        {
            return TryResolveWebReference(reference, currentDocument, out url, out diagnostic);
        }

        private bool CanFollowRedirect(Uri currentDocument, int redirectsFollowed, out string diagnostic)
        {
            if (redirectsFollowed < 0)
                throw new ArgumentOutOfRangeException(nameof(redirectsFollowed));
            if (currentDocument == null)
            {
                diagnostic = "A redirect requires the current absolute document URL.";
                return false;
            }
            if (redirectsFollowed >= Options.MaxRedirects)
            {
                diagnostic = "The redirect limit has been reached.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        /// <summary>
        /// Admission for a NEW document after scope checking and deduplication. The caller owns
        /// counters and must reserve a slot atomically before scheduling; this method is stateless.
        /// Seed depth is zero and pagesAlreadyScheduled includes the seed once reserved.
        /// Fragment-only visits and redirect hops do not reserve a new page slot.
        /// </summary>
        public bool CanScheduleCrawlPage(int depth, int pagesAlreadyScheduled, out string diagnostic)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException(nameof(depth));
            if (pagesAlreadyScheduled < 0)
                throw new ArgumentOutOfRangeException(nameof(pagesAlreadyScheduled));
            if (depth > Options.MaxDepth)
            {
                diagnostic = "The link depth limit has been reached.";
                return false;
            }
            if (pagesAlreadyScheduled >= Options.MaxPages)
            {
                diagnostic = "The page limit has been reached.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        /// <summary>
        /// Canonical HTTP(S) key without a fragment. Path case, query values and query order are
        /// significant. Keys normalize URL syntax only; they do not equate server routes or content.
        /// </summary>
        public static string GetDocumentKey(Uri url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            if (!TryNormalize(url.OriginalString, null, out Uri normalized, out string diagnostic))
                throw new ArgumentException(diagnostic, nameof(url));
            return DocumentKey(normalized);
        }

        /// <summary>Explicit host-only matching; allows different HTTP(S) schemes and ports.</summary>
        public static bool ExactHost(string seedHost, string candidateHost)
        {
            return string.Equals(seedHost, candidateHost, StringComparison.Ordinal);
        }

        private bool TryResolveWebReference(string reference, Uri currentDocument, out Uri url, out string diagnostic)
        {
            url = null;
            Uri basis = RootUrl;
            if (currentDocument != null && !TryNormalize(currentDocument.OriginalString, null, out basis, out diagnostic))
                return false;
            return TryNormalize(reference, basis, out url, out diagnostic);
        }

        private bool TryResolveNavigation(string reference, Uri currentDocument,
            HtmlRuntimeNavigationRestrictions restriction, out Uri url, out string diagnostic)
        {
            url = null;
            if (!TryResolveWebReference(reference, currentDocument, out Uri candidate, out diagnostic))
                return false;
            if (!IsNavigationAllowed(candidate, restriction))
            {
                diagnostic = "The target violates the explicit navigation restriction: " + restriction + ".";
                return false;
            }
            url = candidate;
            return true;
        }

        private bool IsNavigationAllowed(Uri candidate, HtmlRuntimeNavigationRestrictions restriction)
        {
            return (Options.NavigationRestrictions & restriction) == 0 ||
                string.Equals(_rootDocumentKey, DocumentKey(candidate), StringComparison.Ordinal);
        }

        private bool IsCrawlInScope(Uri candidate)
        {
            if (Options.PageScope == HtmlPageScope.CurrentPage)
                return string.Equals(_rootDocumentKey, DocumentKey(candidate), StringComparison.Ordinal);
            if (Options.PageScope == HtmlPageScope.AllLinked)
                return true;

            return IsHostInScope(candidate);
        }

        private bool IsHostInScope(Uri candidate)
        {
            string seedHost = RootUrl.IdnHost;
            string candidateHost = candidate.IdnHost;
            if (RequiresExactHost(RootUrl) || RequiresExactHost(candidate))
                return ExactHost(seedHost, candidateHost);
            return _hostPolicy(seedHost, candidateHost);
        }

        private static bool RequiresExactHost(Uri url)
        {
            return url.HostNameType != UriHostNameType.Dns ||
                url.IdnHost == "localhost" || url.IdnHost.EndsWith(".localhost", StringComparison.Ordinal);
        }

        private static string DocumentKey(Uri url)
        {
            UriBuilder builder = new UriBuilder(url) { Fragment = string.Empty };
            return builder.Uri.AbsoluteUri;
        }

        private static bool TryNormalize(string reference, Uri basis, out Uri normalized, out string diagnostic)
        {
            normalized = null;
            diagnostic = "Expected a well-formed HTTP(S) URL without userinfo; local files require a separate file-root contract.";
            if (reference == null || !string.Equals(reference, reference.Trim(), StringComparison.Ordinal))
                return false;
            for (int i = 0; i < reference.Length; i++)
            {
                // Resolution can repair raw spaces/bad escapes; reject them before that happens.
                if (char.IsControl(reference[i]) || reference[i] == '\\' || reference[i] == ' ' ||
                    (reference[i] == '%' && !Uri.IsHexEncoding(reference, i)))
                    return false;
            }

            Uri parsed;
            // Relative fragments are not standalone absolute URLs: resolve before well-formed validation.
            bool created = basis == null
                ? Uri.TryCreate(reference, UriKind.Absolute, out parsed)
                : Uri.TryCreate(basis, reference, out parsed);
            if (!created || !parsed.IsAbsoluteUri || !parsed.IsWellFormedOriginalString() ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(parsed.UserInfo) || string.IsNullOrEmpty(parsed.Host) ||
                HasUserInfoDelimiter(parsed.OriginalString))
                return false;

            try
            {
                string host = parsed.IdnHost.TrimEnd('.').ToLowerInvariant();
                if (host.Length == 0)
                    return false;
                UriBuilder builder = new UriBuilder(parsed) { Host = host };
                normalized = builder.Uri;
                diagnostic = string.Empty;
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool HasUserInfoDelimiter(string absoluteUrl)
        {
            // UserInfo is empty for an explicitly empty userinfo section; reject that syntax too.
            int authorityStart = absoluteUrl.IndexOf("://", StringComparison.Ordinal) + 3;
            for (int i = authorityStart; i < absoluteUrl.Length; i++)
            {
                char character = absoluteUrl[i];
                if (character == '/' || character == '?' || character == '#')
                    return false;
                if (character == '@')
                    return true;
            }

            return false;
        }
    }
}
