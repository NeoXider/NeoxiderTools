using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.HtmlToUitk.Web
{
    /// <summary>
    /// Optional provider boundary; this package supplies no browser implementation or factory.
    /// The caller owns and disposes the instance. The provider owns its document, callbacks, network
    /// operations and engine handles, never shared engine state or the caller's token source.
    /// </summary>
    /// <remarks>
    /// One instance represents one session and accepts exactly one LoadAsync call. Concurrent/repeated
    /// calls fail with InvalidOperationException; calls after disposal fail with ObjectDisposedException.
    /// Options are immutable for the session; changing them requires a new backend/document.
    /// Dispose is idempotent: cancel pending work, detach callbacks, release owned handles and prevent
    /// later callbacks from publishing success. A pending load then completes as canceled. Providers
    /// must state thread affinity; no implicit dispatch to Unity's main thread is promised here.
    /// </remarks>
    public interface IHtmlRuntimeBackend : IDisposable
    {
        HtmlRuntimeCapabilities Capabilities { get; }

        /// <summary>Validated settings after preflight; null before validation or if validation fails.</summary>
        HtmlRuntimeEffectiveSettings EffectiveSettings { get; }

        /// <summary>
        /// Actual content requirements must be discovered before this call, never guessed from enabled
        /// preferences. No inspector is supplied here. Validate provider/options/content requirements
        /// before engine or navigation side effects and expose EffectiveSettings, then load policy.RootUrl.
        /// Reject a null or already-qualified initial policy before side effects. Requirements must cover
        /// the actual final root and its required behavior, not merely a redirecting seed URL.
        /// The Task succeeds
        /// only after the root document commits and its initial load-complete signal fires (browser
        /// load event or documented native equivalent); return its final normalized HTTP(S) URL.
        /// This is not network-idle, completion of a crawl, JS quiescence or proof of visual parity.
        /// Acquisition scope does not instruct this method to crawl/prefetch, restrict user navigation,
        /// or lock embedded frames/resources.
        /// </summary>
        /// <remarks>
        /// Validate each initial root redirect with TryResolveInitialRootRedirect and qualify the final
        /// root at commit via QualifyRoot. Check discovered-page acquisition/redirects using crawl methods;
        /// the acquisition owner tracks counters, deduplication and link depth. MaxPages/MaxDepth concern
        /// acquisition only, not later browser-lifetime navigation. Top-level/frame behavior uses the
        /// separate explicit NavigationRestrictions; reject requested locks when not enforceable.
        /// Before adopting later acquired/navigated content, discover/revalidate its requirements too;
        /// unknown/unsupported requirements must be diagnosed instead of dropping required behavior.
        /// Resource traffic has separate enforcement obligations: crawl scope is not a sandbox.
        /// Every redirect of any resource must also respect MaxRedirects and request/byte budgets.
        /// Resources off means blocking non-document resources and script network access; a provider
        /// unable to enforce that must not advertise Resources as configurable.
        /// Animations off affects supported declarative animations/transitions; it must not stop
        /// arbitrary JavaScript timers or game requestAnimationFrame callbacks.
        ///
        /// An already-canceled token must produce a canceled Task without starting the engine/network.
        /// Caller cancellation cancels the pending load and closes its document, completing the Task
        /// as canceled only after owned work is stopped/detached. The provider never cancels/disposes
        /// the caller's CancellationTokenSource. LoadTimeout covers initial loading; expiry faults
        /// with TimeoutException after cleanup. Other errors fault with a diagnostic exception after
        /// cleanup. After any terminal load failure the session is closed; Dispose is still required.
        /// Following success the supplied token no longer controls the live session: Dispose closes it.
        /// Later navigation still gets a per-navigation LoadTimeout; request/byte/redirect budgets remain
        /// session bounds independent of crawl depth/page counts. Exhausted session budgets close
        /// the document and must be surfaced through the optional provider's documented diagnostics.
        /// Providers must settle each Task exactly once and ignore stale completion callbacks.
        /// </remarks>
        Task<Uri> LoadAsync(HtmlUrlPolicy policy, HtmlRuntimeFeatures contentRequiredFeatures,
            CancellationToken cancellationToken = default);
    }
}
