# Runtime web contracts — 2026-11-01

Status: **execution-backed relative-fragment fix authored; corrected-source execution Pending**.
Root's historical pre-fix run compiled successfully: **57 PASS / 4 FAIL / 0 skips, total 61**.
Branch inspected: `demo`; HEAD `d271531103210ebcd71c3a5d5bbafdb06ff425a5`.
Root-approved `03-runtime-correction.md` applied in the same session for RWC-01/RWC-02; root's pre-run
freeze records independent source acceptance. The subsequent `03-runtime-execution-fix.md` reopens only
the four executed relative-fragment failures; these latest bytes await independent recheck/root rerun.
No other review findings are marked resolved.

## Owned changes

Initial assignment authored only the following new files; the correction edits only their existing
source/tests/README/report. No additional files or metadata changes were needed for the correction:

- `Assets/Neoxider/Scripts/UI/HtmlToUitk/Runtime/Web/`: `HtmlRuntimeOptions.cs`,
  `HtmlRuntimeCapabilities.cs`, `IHtmlRuntimeBackend.cs`, `HtmlUrlPolicy.cs`, bilingual `README.md`,
  all matching metadata, and `Web.meta`.
- `Assets/Neoxider/Tests/Edit/HtmlToUitk/HtmlRuntimeWebContractsTests.cs` and matching metadata.
- This report.

Pre-assignment source, asmdefs, manifests, other reports and pre-existing local changes were not edited.
The original UITK `Neo.HtmlToUitk.Navigation.IPageBackend` remains unchanged. No dependencies installed.

## Reuse decision and evidence

- Inspected `IPageBackend` and `UiDocumentPageBackend`: they expose `VisualElement`, prepared navigation
  metadata and transitions, rather than URL/browser loading. Preserve them; no forced adapter/inheritance.
- Reuse `System.Uri` / `UriBuilder`, `Task`, `CancellationToken` and `IDisposable` from the existing
  platform runtime. No copied third-party code or new licensing obligation. Document identity uses
  an explicit normalized string: [.NET Uri.Equals](https://learn.microsoft.com/en-us/dotnet/api/system.uri.equals)
  deliberately ignores userinfo/fragment, so it is not used as authorization validation.
- Inspected `HtmlToUitkRuntimePlan.md` and `HtmlToUitkOpenSourceResearch.md`. Assumption: the task's
  shorthand `OpenSourceResearch.md` refers to that existing prefixed research document.
  Browser candidates remain research inputs; none was imported or declared functional here.
- Relevant Neoxider reference catalogs and source search found no matching boundary or public-suffix
  API. Searches across `Assets` and `Packages` for public-suffix/registrable-domain implementations
  found no installed support. `SameDomain` therefore requires explicit host-policy injection; no
  last-two-label shortcut or unverified public-suffix dependency was added.
- Existing runtime/test asmdefs already own the new paths and reference NUnit/runtime respectively.
  No shared assembly changes appear necessary from source inspection. The isolated pre-fix .NET host
  compiled; Unity compilation/discovery remains unverified.

## Implemented semantics

Immutable typed options default to acquisition of one current page, zero depth, `Auto`, and enabled
preferences for supported JS, declarative animations, interactions, resources and Canvas. These are
not unconditional content requirements. `RequiredFeatures` defaults to `None`, separate from mandatory
explicit content-required flags supplied after discovery. No content inspector/provider selector exists.
Finite defaults: 30-second initial load deadline,
128 session requests, 8 MiB per decoded response, 64 MiB total and five redirects per chain.
Invalid enums, unknown feature bits and invalid/contradictory bounds throw explicitly.

`ValidateOptions(options, contentRequiredFeatures)` combines caller/content requirements and returns
`HtmlRuntimeEffectiveSettings` with concrete backend kind, effective enabled supported features and
combined requirements. Default static native content no longer requires JS/Canvas; actual required
unsupported features and caller-disabled requirements are rejected, as are disable requests for
supported always-on features. Unsupported preferences are explicitly absent from the effective set;
required behavior cannot be silently discarded. `IHtmlRuntimeBackend` exposes these settings and
requires content requirements in `LoadAsync`; discovery must precede engine/navigation effects, and
unknown requirements cannot be asserted as `None`.

Capabilities also validate backend kinds, acquisition scope and complete load bounds. Strict navigation
enforcement is required only for explicit `NavigationRestrictions`; default behavior does not thereby
claim enforcement. Capability instances remain provider claims, not verification. Providers must also
reject unsupported combinations/platform values. Animation-off never authorizes stopping arbitrary JS
timers/game RAF. Resource-off must block subresources and script networking or be rejected.

URL policy resolves relative/query/fragment links, normalizes HTTP(S)/IDN hosts/default ports and
trailing DNS dots, preserves path case/query ordering, and strips fragments only for document keys.
Malformed input, userinfo including an empty userinfo delimiter, controls/backslashes and non-HTTP(S)
schemes are rejected with diagnostics. `file:` loading remains separate and unsupported.

`CurrentPage` now acquires one final root without traversing the link graph. Initial root redirects use
`TryResolveInitialRootRedirect`: check HTTP(S)/syntax/userinfo/hop bounds and explicit top-level locks;
`SameDomain` also revalidates domain scope. Default HTTP→HTTPS and canonical-path redirects are accepted.
`QualifyRoot` returns an immutable policy anchored to the final URL at commit. The acquisition owner
validates/counts the actual chain; qualification cannot prove which requests happened. Discovered
links/redirects then use `TryResolveCrawlPage` / `TryResolveCrawlRedirect`, with scope revalidation.

`SameDomain` still requires an explicit exact-host policy or reviewed injected matcher; IP and
localhost remain exact-host even with a permissive matcher. Exact-host/domain matching permits
different HTTP(S) schemes/ports; it is not same-origin. Caller/provider owns acquisition deduplication
and atomic page/depth reservations through `CanScheduleCrawlPage`. Redirects consume requests/hops,
not another acquired page/depth slot.

Acquisition scope does not restrict browser-lifetime top-level navigation, frames or resources.
Separate `TryResolveTopLevelNavigation`, `TryResolveFrameNavigation` and `TryResolveResource` methods
make that separation explicit. Opt-in `TopLevelToRootDocument` / `FramesToRootDocument` flags impose
independent exact-document locks and require the explicit enforcement capability. A selected top-level
lock also restricts initial root redirects, deliberately stricter than defaults. Notification-only stock
callbacks must not advertise enforcement. Request/byte/deadline/redirect limits remain explicit, including
frame/resource traffic; acquired-page/depth limits do not become browser-lifetime navigation limits.

No automatic crawling, resource network isolation, SSRF defense, DNS/private-network filtering,
file-root containment, process sandbox,
JS/CLR bridge, JS/GPU memory limit or rendering implementation is claimed.

Backend lifetime is documented: one session/one load call, immutable options, caller-owned disposal,
provider-owned work/handles, explicit completion signal/final URL, cleanup before canceled/faulted
completion, timeout error, idempotent disposal, and stale-callback suppression. After successful loading,
the load token no longer owns the session; `Dispose` closes it. Real provider enforcement is outstanding.

## Verification and remaining work

Authored/updated NUnit cases cover default static native acceptance, actual JS/Canvas requirements,
caller/content requirement union, disabled-required rejection, effective settings, always-on disable
rejection and optional strict-navigation capability gating. URL cases distinguish initial HTTPS/canonical
root redirects and qualification, limits/invalid URLs/domain escapes, discovered-page redirect revalidation,
crawl-only admission, independent frame/resource/user navigation, opt-in top-level/frame locks and
acquisition page/depth boundaries. Existing malformed URL/userinfo, query/path/fragment, IDN/trailing-dot,
deceptive-domain and IP/IPv6/localhost checks remain. These are authored pure contract/policy assertions,
not browser behavior evidence. The root's first isolated run executed all 61 prior cases, with four
failures documented below. Latest source annotation count: 21 `[Test]` plus 44 `[TestCase]` annotations
(65 authored cases); corrected-source compilation/discovery/execution are **Pending**, not a pass.

Performed source/text review only: UTF-8 decoding, no replacement characters, no trailing whitespace,
explicit C# types, matching metadata and a valid README entry link. **This worker launched no compiler,
test suite, Unity Editor/Player, Unity MCP, native provider or input/visual check.** Independent source
review and the initial isolated execution were performed by root's separate workflow. No agents spawned.

## Execution-backed relative-fragment correction

Primary raw evidence: [first-run TRX](D:/Temp/Neoxider-Audit-20261101/verification/results/web-contracts.trx),
root run at 2026-11-01 17:43:59–17:44:03 UTC. Counts: executed 61, passed 57, failed 4, not executed 0.
The standalone host used SDK 10.0.301, target `net8.0`, NUnit 4.6.1 and external legacy assert aliases;
none of that compatibility glue was copied to Unity. This is .NET URL-contract evidence, not a Unity,
browser, rendering or input result. The separate PowerShell probe described by root was not rerun here.

All four failures rejected valid relative references in `TryNormalize` before base resolution:

- `CurrentPage_InitialHttpsAndCanonicalRedirectsQualifyOneFinalRoot`: `#section`.
- `CurrentPage_PreservesFragmentsButRejectsOtherDocuments`: `#new`.
- `RelativeResolution_UsesCurrentDocumentAndKeepsQueryOrder`: `../next?b=2&a=1#item`.
- `StrictTopLevelAndFrameLocks_AreIndependentOfAcquisitionAndResources`: `#fragment`.

Removed the premature `Uri.IsWellFormedUriString(reference, RelativeOrAbsolute)` gate. Relative input
now reaches the existing `Uri.TryCreate(base, reference)` path, followed by absolute
`IsWellFormedOriginalString` validation. Lexical checks still reject trim differences, controls and
backslashes; raw ASCII spaces and invalid percent escapes are rejected explicitly using
`Uri.IsHexEncoding` so resolution cannot silently repair malformed input. Scheme/host/userinfo checks,
diagnostics, IDN/IP/localhost behavior, root/crawl/strict-navigation separation and all bounds are unchanged.

Original 61 test cases and their assertions are unchanged. Added only four focused cases: valid escaped
query-plus-fragment resolution, bad fragment escape, truncated query escape before a fragment, and a
raw path space with a fragment. Removing those additions in memory reproduces the frozen fixture SHA-256,
confirming preservation of the original cases. No tests were ignored, weakened or executed by this worker.

[Exact pre-run freeze](D:/Temp/Neoxider-Audit-20261101/verification/web-contracts-source-freeze.json)
matched all five owned compiled inputs before edits. Only `HtmlUrlPolicy.cs`, the existing fixture and
this report changed in this execution-fix pass; other sources, README, metadata and host files were untouched.

| File | Frozen pre-run SHA-256 | Corrected SHA-256 for independent recheck |
|---|---|---|
| `HtmlUrlPolicy.cs` | `f9d5d4d1fd13392fd0a4c9d6a40a7193448d055ec1bc43f3689f9503c9a9a85c` | `1e143356a8a4e14d9cdae20ec2fcac1075f11d0128dd2a57653453ec8d67011f` |
| `HtmlRuntimeWebContractsTests.cs` | `8948df1da770301dacdf8ce610b2fd12ee94145efc68c45e0f4413aa98520f83` | `1fe17840ce49d95ac54cdd9fb2dcc9f2ad9de89565dce6e3ae6b5d5f9e3be60f` |

Corrected-source execution: **Pending**. Fresh compiler/test/Unity execution by this worker: **NotRun**.

Root next checks:

1. Independently recheck the latest source/test bytes, then root reruns the isolated suite (65 authored
   cases) and retains the historical 57/4 TRX. Then compile/run the fixture on supported Unity versions;
   specifically verify `System.Uri` behavior for IDN, IPv6, escaping and empty-userinfo inputs. Historical
   `Verification~/PureLogic.Tests.csproj` globs exclude `Runtime/Web` and this fixture; older test evidence
   does not cover these contracts. Root owns any shared harness changes.
2. Integrate a real optional provider separately. A basic `LoadURL` wrapper cannot claim the strict
   request/byte limits or requested strict pre-navigation interception without evidence. Plain default
   browser navigation is not enforcement; unsupported bounds/strict requirements still fail.
3. Verify canceled-before-start, cancellation/timeout/disposal races, single-call enforcement, cleanup,
   stale callbacks, actual load event ownership, initial-root qualification versus discovered redirects,
   separate frame/resource policy, later navigation, limits and animation-off preserving game JS/RAF.
   Verify content discovery/effective settings before effects and provider-specific invalid combinations.
   No inspector was implemented by this correction.
4. Then verify supported platforms/Player/IL2CPP, rendering/input and any separately requested sandbox
   or public-suffix implementation. NativeRuntimeReview remains independent.

Active processes: none started or left running by this assignment. Unity and foreign processes were
not inspected, started, stopped or controlled. Git commits/pushes/branch/worktree operations: none.
