using System;
using Neo.HtmlToUitk.Web;
using NUnit.Framework;

namespace Neo.HtmlToUitk.Tests
{
    public sealed class HtmlRuntimeWebContractsTests
    {
        [Test]
        public void Defaults_RequestOneBoundedPageWithAllFeatures()
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions();
            Assert.That(options.PageScope, Is.EqualTo(HtmlPageScope.CurrentPage));
            Assert.That(options.BackendKind, Is.EqualTo(HtmlRuntimeBackendKind.Auto));
            Assert.That(options.Features, Is.EqualTo(HtmlRuntimeFeatures.All));
            Assert.That(options.RequiredFeatures, Is.EqualTo(HtmlRuntimeFeatures.None));
            Assert.That(options.NavigationRestrictions, Is.EqualTo(HtmlRuntimeNavigationRestrictions.None));
            Assert.That(options.MaxPages, Is.EqualTo(1));
            Assert.That(options.MaxDepth, Is.Zero);
            Assert.That(options.LoadTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(options.MaxResourceRequests, Is.GreaterThan(0));
            Assert.That(options.MaxResourceBytes, Is.GreaterThan(0));
            Assert.That(options.MaxTotalResourceBytes, Is.GreaterThanOrEqualTo(options.MaxResourceBytes));
            Assert.That(options.MaxRedirects, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Options_RejectInvalidAndContradictoryBounds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxPages: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxDepth: -1));
            Assert.Throws<ArgumentException>(() => new HtmlRuntimeOptions(maxPages: 2));
            Assert.Throws<ArgumentException>(() => new HtmlRuntimeOptions(maxDepth: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(loadTimeout: TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(loadTimeout: TimeSpan.FromMilliseconds(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxResourceRequests: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxResourceBytes: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxResourceBytes: 10, maxTotalResourceBytes: 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(maxRedirects: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(pageScope: (HtmlPageScope)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(backendKind: (HtmlRuntimeBackendKind)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(features: (HtmlRuntimeFeatures)128));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(requiredFeatures: (HtmlRuntimeFeatures)128));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeOptions(navigationRestrictions: (HtmlRuntimeNavigationRestrictions)128));
        }

        [Test]
        public void DefaultAuto_StaticContentAcceptsNativeWithoutJavaScriptOrCanvas()
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions();
            HtmlRuntimeEffectiveSettings effective = StaticNative().ValidateOptions(options, HtmlRuntimeFeatures.None);
            Assert.That(effective.Options, Is.SameAs(options));
            Assert.That(effective.BackendKind, Is.EqualTo(HtmlRuntimeBackendKind.Native));
            Assert.That(effective.Features, Is.EqualTo(HtmlRuntimeFeatures.Animations | HtmlRuntimeFeatures.Interactions | HtmlRuntimeFeatures.Resources));
            Assert.That(effective.RequiredFeatures, Is.EqualTo(HtmlRuntimeFeatures.None));
            Assert.That(options.Features, Is.EqualTo(HtmlRuntimeFeatures.All));
            // None is an explicit assertion about inspected content, not an implemented content inspector.
        }

        [TestCase(HtmlRuntimeFeatures.JavaScript)]
        [TestCase(HtmlRuntimeFeatures.Canvas)]
        public void ActualBrowserRequirements_AreRejectedByNativeAndAcceptedByWeb(HtmlRuntimeFeatures requirement)
        {
            NotSupportedException error = Assert.Throws<NotSupportedException>(() =>
                StaticNative().ValidateOptions(new HtmlRuntimeOptions(), requirement));
            StringAssert.Contains(requirement.ToString(), error.Message);
            HtmlRuntimeEffectiveSettings effective = FullWeb().ValidateOptions(new HtmlRuntimeOptions(), requirement);
            Assert.That(effective.Features.HasFlag(requirement), Is.True);
            Assert.That(effective.RequiredFeatures, Is.EqualTo(requirement));
        }

        [TestCase(HtmlRuntimeFeatures.JavaScript)]
        [TestCase(HtmlRuntimeFeatures.Canvas)]
        public void DisabledRequiredBehavior_IsRejectedEvenWhenTheBackendSupportsIt(HtmlRuntimeFeatures requirement)
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions(features: HtmlRuntimeFeatures.All & ~requirement);
            NotSupportedException error = Assert.Throws<NotSupportedException>(() => FullWeb().ValidateOptions(options, requirement));
            StringAssert.Contains("disabled", error.Message);
            StringAssert.Contains(requirement.ToString(), error.Message);
        }

        [Test]
        public void CallerAndContentRequirements_AreCombinedAndEffectiveSettingsAreExplicit()
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions(requiredFeatures: HtmlRuntimeFeatures.JavaScript,
                features: HtmlRuntimeFeatures.All & ~HtmlRuntimeFeatures.Animations);
            HtmlRuntimeEffectiveSettings effective = FullWeb().ValidateOptions(options, HtmlRuntimeFeatures.Canvas);
            Assert.That(effective.RequiredFeatures, Is.EqualTo(HtmlRuntimeFeatures.JavaScript | HtmlRuntimeFeatures.Canvas));
            Assert.That(effective.Features, Is.EqualTo(options.Features));
            Assert.Throws<NotSupportedException>(() => StaticNative().ValidateOptions(
                new HtmlRuntimeOptions(requiredFeatures: HtmlRuntimeFeatures.Canvas), HtmlRuntimeFeatures.None));
            Assert.Throws<NotSupportedException>(() => FullWeb().ValidateOptions(
                new HtmlRuntimeOptions(requiredFeatures: HtmlRuntimeFeatures.JavaScript,
                    features: HtmlRuntimeFeatures.All & ~HtmlRuntimeFeatures.JavaScript), HtmlRuntimeFeatures.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => FullWeb().ValidateOptions(options, (HtmlRuntimeFeatures)128));
        }

        [TestCase(HtmlRuntimeFeatures.JavaScript)]
        [TestCase(HtmlRuntimeFeatures.Animations)]
        [TestCase(HtmlRuntimeFeatures.Interactions)]
        [TestCase(HtmlRuntimeFeatures.Resources)]
        [TestCase(HtmlRuntimeFeatures.Canvas)]
        public void Capabilities_RejectDisablingAnAlwaysOnFeature(HtmlRuntimeFeatures feature)
        {
            HtmlRuntimeCapabilities capabilities = new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Web,
                HtmlRuntimeFeatures.All, HtmlRuntimeFeatures.All & ~feature,
                enforcesNavigationPolicy: true, enforcesLoadBounds: true);
            NotSupportedException error = Assert.Throws<NotSupportedException>(() =>
                capabilities.ValidateOptions(new HtmlRuntimeOptions(features: HtmlRuntimeFeatures.All & ~feature), HtmlRuntimeFeatures.None));
            StringAssert.Contains(feature.ToString(), error.Message);
        }

        [Test]
        public void AnimationsOff_PreservesTheSeparateJavaScriptRequest()
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions(features: HtmlRuntimeFeatures.All & ~HtmlRuntimeFeatures.Animations);
            Assert.That(options.Features.HasFlag(HtmlRuntimeFeatures.JavaScript), Is.True);
            Assert.DoesNotThrow(() => FullWeb().ValidateOptions(options, HtmlRuntimeFeatures.JavaScript));
            // Timer/RAF behavior needs a real provider test; flags alone cannot establish it.
        }

        [Test]
        public void Capabilities_RequireConcreteKindAndConsistentFeatureClaims()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeCapabilities(
                HtmlRuntimeBackendKind.Auto, HtmlRuntimeFeatures.All, HtmlRuntimeFeatures.All));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlRuntimeCapabilities(
                HtmlRuntimeBackendKind.Web, (HtmlRuntimeFeatures)128, HtmlRuntimeFeatures.None));
            Assert.Throws<ArgumentException>(() => new HtmlRuntimeCapabilities(
                HtmlRuntimeBackendKind.Web, HtmlRuntimeFeatures.None, HtmlRuntimeFeatures.Canvas));
        }

        [Test]
        public void Capabilities_RejectBackendMismatchAndUnenforceablePolicies()
        {
            Assert.Throws<ArgumentNullException>(() => FullWeb().ValidateOptions(null, HtmlRuntimeFeatures.None));
            Assert.Throws<NotSupportedException>(() => FullWeb().ValidateOptions(
                new HtmlRuntimeOptions(backendKind: HtmlRuntimeBackendKind.Native), HtmlRuntimeFeatures.None));
            Assert.Throws<NotSupportedException>(() => new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Web,
                HtmlRuntimeFeatures.All, HtmlRuntimeFeatures.All, enforcesNavigationPolicy: true).ValidateOptions(new HtmlRuntimeOptions(), HtmlRuntimeFeatures.None));
            HtmlRuntimeCapabilities singlePage = new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Web,
                HtmlRuntimeFeatures.All, HtmlRuntimeFeatures.All, enforcesNavigationPolicy: true, enforcesLoadBounds: true);
            Assert.Throws<NotSupportedException>(() => singlePage.ValidateOptions(new HtmlRuntimeOptions(pageScope: HtmlPageScope.AllLinked), HtmlRuntimeFeatures.None));
            Assert.DoesNotThrow(() => FullWeb().ValidateOptions(new HtmlRuntimeOptions(backendKind: HtmlRuntimeBackendKind.Web), HtmlRuntimeFeatures.None));
        }

        [TestCase(HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument)]
        [TestCase(HtmlRuntimeNavigationRestrictions.FramesToRootDocument)]
        [TestCase(HtmlRuntimeNavigationRestrictions.All)]
        public void StrictNavigation_IsAnOptionalCapabilityNeverImpliedByDefaultScope(HtmlRuntimeNavigationRestrictions restrictions)
        {
            HtmlRuntimeCapabilities ordinaryWeb = new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Web,
                HtmlRuntimeFeatures.All, HtmlRuntimeFeatures.All, enforcesLoadBounds: true);
            Assert.That(ordinaryWeb.EnforcesNavigationPolicy, Is.False);
            Assert.DoesNotThrow(() => ordinaryWeb.ValidateOptions(new HtmlRuntimeOptions(), HtmlRuntimeFeatures.None));
            HtmlRuntimeOptions strict = new HtmlRuntimeOptions(navigationRestrictions: restrictions);
            Assert.Throws<NotSupportedException>(() => ordinaryWeb.ValidateOptions(strict, HtmlRuntimeFeatures.None));
            Assert.DoesNotThrow(() => FullWeb().ValidateOptions(strict, HtmlRuntimeFeatures.None));
        }

        [Test]
        public void CurrentPage_PreservesFragmentsButRejectsOtherDocuments()
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy("HTTPS://EXAMPLE.COM:443/Folder/page?q=1#old");
            Assert.That(policy.TryResolveCrawlPage("#new", null, out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.Fragment, Is.EqualTo("#new"));
            Assert.That(url.Host, Is.EqualTo("example.com"));
            Assert.That(HtmlUrlPolicy.GetDocumentKey(url), Is.EqualTo("https://example.com/Folder/page?q=1"));
            Assert.That(policy.TryResolveCrawlPage(string.Empty, null, out url, out diagnostic), Is.True, diagnostic);
            Assert.That(policy.TryResolveCrawlPage("?q=1", null, out url, out diagnostic), Is.True, diagnostic);
            AssertRejected(policy, "?q=2");
            AssertRejected(policy, "/folder/page?q=1");
            AssertRejected(policy, "http://example.com/Folder/page?q=1");
            AssertRejected(policy, "https://example.com:444/Folder/page?q=1");
        }

        [Test]
        public void RelativeResolution_UsesCurrentDocumentAndKeepsQueryOrder()
        {
            HtmlUrlPolicy policy = AllLinked();
            Assert.That(policy.TryResolveCrawlPage("../next?b=2&a=1#item", new Uri("https://example.com/dir/current"),
                out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.AbsoluteUri, Is.EqualTo("https://example.com/next?b=2&a=1#item"));
            Assert.That(HtmlUrlPolicy.GetDocumentKey(url), Is.Not.EqualTo(
                HtmlUrlPolicy.GetDocumentKey(new Uri("https://example.com/next?a=1&b=2"))));
            Assert.That(policy.TryResolveCrawlPage("//OTHER.EXAMPLE:443/path", null, out url, out diagnostic), Is.True, diagnostic);
            Assert.That(url.AbsoluteUri, Is.EqualTo("https://other.example/path"));
            Assert.That(policy.TryResolveCrawlPage("/encoded%2Fsegment?q=a%2Fb#x", null, out url, out diagnostic), Is.True, diagnostic);
            StringAssert.Contains("%2F", HtmlUrlPolicy.GetDocumentKey(url));
        }

        [Test]
        public void QueryAndFragmentReferences_PreserveValidEscapesAfterResolution()
        {
            HtmlUrlPolicy policy = AllLinked();
            Assert.That(policy.TryResolveCrawlPage("?next=%2F#escaped%20name", null,
                out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.AbsoluteUri, Is.EqualTo("https://example.com/?next=%2F#escaped%20name"));
            Assert.That(HtmlUrlPolicy.GetDocumentKey(url), Is.EqualTo("https://example.com/?next=%2F"));
        }

        [TestCase(null)]
        [TestCase("#bad%zz")]
        [TestCase("?x=%2#fragment")]
        [TestCase("../raw path#fragment")]
        [TestCase("https://")]
        [TestCase("https://[invalid]/")]
        [TestCase("https://example.com:99999/")]
        [TestCase("https://example.com/%zz")]
        [TestCase("relative/%zz")]
        [TestCase("https://example.com/path with space")]
        [TestCase("relative/path with space")]
        [TestCase(" https://example.com/")]
        [TestCase("https://example.com/\n")]
        [TestCase("https://example.com\\@evil.test/")]
        [TestCase("https://user:password@example.com/")]
        [TestCase("https://@example.com/")]
        [TestCase("//user@example.com/")]
        [TestCase("//127.0.0.1@example.com/")]
        [TestCase("javascript:alert(1)")]
        [TestCase("data:text/html,hello")]
        [TestCase("mailto:user@example.com")]
        [TestCase("file:///D:/site/index.html")]
        [TestCase("D:\\site\\index.html")]
        public void InvalidAndNonWebReferences_AreRejectedWithDiagnostics(string reference)
        {
            AssertRejected(AllLinked(), reference);
        }

        [TestCase("/relative-root")]
        [TestCase("file:///D:/site/index.html")]
        [TestCase("https://name@example.com/")]
        [TestCase("https://@example.com/")]
        public void Seed_RequiresAnAbsoluteHttpUrlWithoutUserinfo(string seed)
        {
            Assert.Throws<ArgumentException>(() => new HtmlUrlPolicy(seed));
        }

        [Test]
        public void SameDomain_RequiresAnExplicitHostPolicy()
        {
            HtmlRuntimeOptions options = new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain);
            Assert.Throws<NotSupportedException>(() => CreateQualifiedPolicy("https://shop.example.co.uk/", options));
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://shop.example.co.uk/", options, HtmlUrlPolicy.ExactHost);
            AssertRejected(policy, "https://attacker.co.uk/");
            AssertRejected(policy, "https://example.co.uk/");
            AssertRejected(policy, "https://shop.example.co.uk.evil.test/");
            Assert.That(policy.TryResolveCrawlPage("http://SHOP.EXAMPLE.CO.UK:8080/other", null,
                out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.Port, Is.EqualTo(8080));
        }

        [Test]
        public void ExplicitDomainPolicy_ReceivesNormalizedHostsAndControlsSubdomains()
        {
            Func<string, string, bool> explicitHosts = (string seed, string candidate) =>
                seed == "shop.example.co.uk" && (candidate == seed || candidate == "cdn.example.co.uk");
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://SHOP.EXAMPLE.CO.UK./",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain), explicitHosts);
            Assert.That(policy.TryResolveCrawlPage("https://CDN.EXAMPLE.CO.UK./page", null,
                out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.Host, Is.EqualTo("cdn.example.co.uk"));
            AssertRejected(policy, "https://attacker.co.uk/");
            AssertRejected(policy, "https://unlisted.example.co.uk/");
        }

        [Test]
        public void IdnHosts_MatchTheirPunycodeSpelling()
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://bücher.example/",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain), HtmlUrlPolicy.ExactHost);
            Assert.That(policy.TryResolveCrawlPage("https://xn--bcher-kva.example/page", null,
                out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(url.IdnHost, Is.EqualTo("xn--bcher-kva.example"));
        }

        [TestCase("https://127.0.0.1/", "https://127.0.0.2/")]
        [TestCase("https://[::1]/", "https://[::2]/")]
        [TestCase("http://localhost/", "http://other.localhost/")]
        public void IpAndLocalhost_DoNotWidenThroughAnInjectedDomainMatcher(string seed, string other)
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy(seed, new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain),
                (string seedHost, string candidateHost) => true);
            AssertRejected(policy, other);
            Assert.That(policy.TryResolveCrawlPage("/same", null, out Uri url, out string diagnostic), Is.True, diagnostic);
        }

        [Test]
        public void DiscoveredPageRedirects_RecheckQualifiedDomainScopeAndHaveAnExactHopLimit()
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://example.com/start",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain, maxRedirects: 1), HtmlUrlPolicy.ExactHost);
            Assert.That(policy.TryResolveCrawlRedirect("/next", policy.RootUrl, 0, out Uri url, out string diagnostic), Is.True, diagnostic);
            Assert.That(policy.TryResolveCrawlRedirect("https://evil.test/", url, 0, out Uri denied, out diagnostic), Is.False);
            Assert.That(denied, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
            Assert.That(policy.TryResolveCrawlRedirect("/last", url, 1, out denied, out diagnostic), Is.False);
            StringAssert.Contains("redirect limit", diagnostic);
            Assert.That(policy.TryResolveCrawlRedirect("/last", null, 0, out denied, out diagnostic), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => policy.TryResolveCrawlRedirect("/last", policy.RootUrl, -1, out denied, out diagnostic));
        }

        [Test]
        public void CurrentPage_InitialHttpsAndCanonicalRedirectsQualifyOneFinalRoot()
        {
            HtmlUrlPolicy initial = new HtmlUrlPolicy("http://example.com/start");
            Assert.That(initial.IsRootQualified, Is.False);
            Assert.That(initial.TryResolveCrawlPage("/linked", null, out Uri denied, out string diagnostic), Is.False);
            Assert.That(initial.TryResolveInitialRootRedirect("https://example.com/start", initial.RootUrl, 0,
                out Uri secure, out diagnostic), Is.True, diagnostic);
            Assert.That(initial.TryResolveInitialRootRedirect("/start/", secure, 1, out Uri canonical, out diagnostic), Is.True, diagnostic);
            HtmlUrlPolicy qualified = initial.QualifyRoot(canonical);
            Assert.That(qualified.IsRootQualified, Is.True);
            Assert.That(initial.IsRootQualified, Is.False);
            Assert.That(qualified.RootUrl.AbsoluteUri, Is.EqualTo("https://example.com/start/"));
            Assert.That(qualified.TryResolveCrawlPage("#section", null, out Uri same, out diagnostic), Is.True, diagnostic);
            AssertRejected(qualified, initial.RootUrl.AbsoluteUri);
            AssertRejected(qualified, "/linked");
            Assert.That(qualified.CanScheduleCrawlPage(0, 1, out diagnostic), Is.False);
            Assert.That(qualified.TryResolveInitialRootRedirect("/later", canonical, 2, out denied, out diagnostic), Is.False);
        }

        [Test]
        public void InitialRootRedirects_StillValidateUrlDomainBudgetAndOptionalLocks()
        {
            HtmlUrlPolicy bounded = new HtmlUrlPolicy("http://example.com/start", new HtmlRuntimeOptions(maxRedirects: 1));
            Assert.That(bounded.TryResolveInitialRootRedirect("https://example.com/start/", bounded.RootUrl, 0,
                out Uri accepted, out string diagnostic), Is.True, diagnostic);
            Assert.That(bounded.TryResolveInitialRootRedirect("/again", accepted, 1, out Uri denied, out diagnostic), Is.False);
            StringAssert.Contains("redirect limit", diagnostic);
            Assert.That(bounded.TryResolveInitialRootRedirect("https://user@evil.test/", bounded.RootUrl, 0, out denied, out diagnostic), Is.False);
            Assert.That(bounded.TryResolveInitialRootRedirect("file:///D:/site/index.html", bounded.RootUrl, 0, out denied, out diagnostic), Is.False);
            Assert.That(bounded.TryResolveInitialRootRedirect("/next", null, 0, out denied, out diagnostic), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => bounded.TryResolveInitialRootRedirect("/next", bounded.RootUrl, -1, out denied, out diagnostic));
            HtmlUrlPolicy noRedirects = new HtmlUrlPolicy("https://example.com/", new HtmlRuntimeOptions(maxRedirects: 0));
            Assert.That(noRedirects.TryResolveInitialRootRedirect("/canonical", noRedirects.RootUrl, 0, out denied, out diagnostic), Is.False);

            HtmlUrlPolicy domain = new HtmlUrlPolicy("http://example.com/",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain), HtmlUrlPolicy.ExactHost);
            Assert.That(domain.TryResolveInitialRootRedirect("https://example.com/", domain.RootUrl, 0, out accepted, out diagnostic), Is.True, diagnostic);
            Assert.That(domain.TryResolveInitialRootRedirect("https://evil.test/", domain.RootUrl, 0, out denied, out diagnostic), Is.False);
            Assert.Throws<ArgumentException>(() => domain.QualifyRoot(new Uri("https://evil.test/")));

            HtmlUrlPolicy strict = new HtmlUrlPolicy("http://example.com/start", new HtmlRuntimeOptions(
                navigationRestrictions: HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument));
            Assert.That(strict.TryResolveInitialRootRedirect("https://example.com/start", strict.RootUrl, 0, out denied, out diagnostic), Is.False);
            StringAssert.Contains("explicit", diagnostic);
            Assert.Throws<ArgumentException>(() => strict.QualifyRoot(new Uri("https://example.com/start")));
        }

        [TestCase(HtmlPageScope.CurrentPage)]
        [TestCase(HtmlPageScope.SameDomain)]
        public void CrawlScope_DoesNotLockBrowserFramesResourcesOrUserNavigation(HtmlPageScope scope)
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://example.com/start",
                new HtmlRuntimeOptions(pageScope: scope), HtmlUrlPolicy.ExactHost);
            const string embedded = "https://embedded.other.test/frame";
            AssertRejected(policy, embedded);
            Assert.That(policy.TryResolveFrameNavigation(embedded, policy.RootUrl, out Uri frame, out string diagnostic), Is.True, diagnostic);
            Assert.That(policy.TryResolveResource("../image.png", frame, out Uri resource, out diagnostic), Is.True, diagnostic);
            Assert.That(resource.AbsoluteUri, Is.EqualTo("https://embedded.other.test/image.png"));
            Assert.That(policy.TryResolveTopLevelNavigation(embedded, policy.RootUrl, out Uri navigation, out diagnostic), Is.True, diagnostic);
            Assert.That(navigation, Is.EqualTo(frame));
            // No crawl scheduling/prefetch is performed by any of these resolution methods.
        }

        [Test]
        public void StrictTopLevelAndFrameLocks_AreIndependentOfAcquisitionAndResources()
        {
            HtmlUrlPolicy top = CreateQualifiedPolicy("https://example.com/start", new HtmlRuntimeOptions(
                pageScope: HtmlPageScope.AllLinked, maxPages: 4, maxDepth: 2,
                navigationRestrictions: HtmlRuntimeNavigationRestrictions.TopLevelToRootDocument));
            const string other = "https://other.test/next";
            Assert.That(top.TryResolveCrawlPage(other, null, out Uri acquired, out string diagnostic), Is.True, diagnostic);
            Assert.That(top.TryResolveTopLevelNavigation(other, null, out Uri denied, out diagnostic), Is.False);
            Assert.That(denied, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
            Assert.That(top.TryResolveTopLevelNavigation("#fragment", null, out Uri same, out diagnostic), Is.True, diagnostic);
            Assert.That(top.TryResolveFrameNavigation(other, null, out Uri frame, out diagnostic), Is.True, diagnostic);
            Assert.That(top.TryResolveResource(other, null, out Uri resource, out diagnostic), Is.True, diagnostic);

            HtmlUrlPolicy frames = CreateQualifiedPolicy("https://example.com/start", new HtmlRuntimeOptions(
                navigationRestrictions: HtmlRuntimeNavigationRestrictions.FramesToRootDocument));
            Assert.That(frames.TryResolveTopLevelNavigation(other, null, out acquired, out diagnostic), Is.True, diagnostic);
            Assert.That(frames.TryResolveFrameNavigation(other, null, out denied, out diagnostic), Is.False);
            Assert.That(frames.TryResolveFrameNavigation("#fragment", null, out same, out diagnostic), Is.True, diagnostic);
            Assert.That(frames.TryResolveResource(other, null, out resource, out diagnostic), Is.True, diagnostic);
        }

        [Test]
        public void RootQualification_RejectsInvalidInputsAndCannotBeRepeated()
        {
            HtmlUrlPolicy initial = new HtmlUrlPolicy("https://example.com/");
            Assert.Throws<ArgumentNullException>(() => initial.QualifyRoot(null));
            Assert.Throws<ArgumentException>(() => initial.QualifyRoot(new Uri("relative", UriKind.Relative)));
            Assert.Throws<ArgumentException>(() => initial.QualifyRoot(new Uri("file:///D:/site/index.html")));
            HtmlUrlPolicy qualified = initial.QualifyRoot(initial.RootUrl);
            Assert.Throws<InvalidOperationException>(() => qualified.QualifyRoot(qualified.RootUrl));
        }

        [Test]
        public void UnacceptedBaseDocument_IsRejectedBeforeResolvingRelativeLinks()
        {
            HtmlUrlPolicy policy = CreateQualifiedPolicy("https://example.com/start",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.SameDomain), HtmlUrlPolicy.ExactHost);
            Assert.That(policy.TryResolveCrawlPage("/page", new Uri("https://evil.test/"), out Uri denied, out string diagnostic), Is.False);
            Assert.That(denied, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
            Assert.That(policy.TryResolveCrawlPage("/page", new Uri("relative", UriKind.Relative), out denied, out diagnostic), Is.False);
        }

        [Test]
        public void Scheduling_RespectsPageAndDepthBoundariesIncludingTheSeed()
        {
            HtmlUrlPolicy single = CreateQualifiedPolicy("https://example.com/");
            Assert.That(single.CanScheduleCrawlPage(0, 0, out string diagnostic), Is.True, diagnostic);
            Assert.That(single.CanScheduleCrawlPage(0, 1, out diagnostic), Is.False);
            Assert.That(single.CanScheduleCrawlPage(1, 0, out diagnostic), Is.False);
            HtmlUrlPolicy linked = CreateQualifiedPolicy("https://example.com/",
                new HtmlRuntimeOptions(pageScope: HtmlPageScope.AllLinked, maxPages: 3, maxDepth: 2));
            Assert.That(linked.CanScheduleCrawlPage(2, 2, out diagnostic), Is.True, diagnostic);
            Assert.That(linked.CanScheduleCrawlPage(3, 2, out diagnostic), Is.False);
            Assert.That(linked.CanScheduleCrawlPage(2, 3, out diagnostic), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => linked.CanScheduleCrawlPage(-1, 0, out diagnostic));
            Assert.Throws<ArgumentOutOfRangeException>(() => linked.CanScheduleCrawlPage(0, -1, out diagnostic));
        }

        [Test]
        public void DocumentKeys_AreFragmentIndependentAndRejectLocalFiles()
        {
            Assert.That(HtmlUrlPolicy.GetDocumentKey(new Uri("https://EXAMPLE.com:443/a?x=1#one")),
                Is.EqualTo(HtmlUrlPolicy.GetDocumentKey(new Uri("https://example.com/a?x=1#two"))));
            Assert.Throws<ArgumentNullException>(() => HtmlUrlPolicy.GetDocumentKey(null));
            Assert.Throws<ArgumentException>(() => HtmlUrlPolicy.GetDocumentKey(new Uri("file:///D:/site/index.html")));
        }

        private static HtmlRuntimeCapabilities FullWeb()
        {
            return new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Web, HtmlRuntimeFeatures.All,
                HtmlRuntimeFeatures.All, supportsLinkedPages: true, enforcesNavigationPolicy: true, enforcesLoadBounds: true);
        }

        private static HtmlRuntimeCapabilities StaticNative()
        {
            HtmlRuntimeFeatures supported = HtmlRuntimeFeatures.Animations | HtmlRuntimeFeatures.Interactions | HtmlRuntimeFeatures.Resources;
            return new HtmlRuntimeCapabilities(HtmlRuntimeBackendKind.Native, supported, supported, enforcesLoadBounds: true);
        }

        private static HtmlUrlPolicy CreateQualifiedPolicy(string rootUrl, HtmlRuntimeOptions options = null,
            Func<string, string, bool> hostPolicy = null)
        {
            HtmlUrlPolicy initial = new HtmlUrlPolicy(rootUrl, options, hostPolicy);
            return initial.QualifyRoot(initial.RootUrl);
        }

        private static HtmlUrlPolicy AllLinked()
        {
            return CreateQualifiedPolicy("https://example.com/", new HtmlRuntimeOptions(pageScope: HtmlPageScope.AllLinked));
        }

        private static void AssertRejected(HtmlUrlPolicy policy, string reference)
        {
            Assert.That(policy.TryResolveCrawlPage(reference, null, out Uri url, out string diagnostic), Is.False, reference);
            Assert.That(url, Is.Null);
            Assert.That(diagnostic, Is.Not.Empty);
        }
    }
}
