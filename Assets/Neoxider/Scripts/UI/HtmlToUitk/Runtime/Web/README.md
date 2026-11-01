# Optional HTML runtime boundary / Опциональный HTML runtime API

Namespace: `Neo.HtmlToUitk.Web`, existing assembly: `Neo.HtmlToUitk.Runtime`.
This directory contains contracts and a pure URL policy. **No browser, DOM, crawler, renderer,
provider factory or scene component is implemented here.** The existing UITK `IPageBackend` remains
independent. Entry point for the wider module: [HtmlToUitk](../../../../../Docs/UI/HtmlToUitk.md).

## C# API

`HtmlRuntimeOptions` is immutable. Defaults prefer `Auto`, acquisition of one current page, depth zero,
and enabled JavaScript, declarative animations, interactions, resources and Canvas where supported.
These are allowed/enabled preferences, not unconditional content requirements. `RequiredFeatures`
defaults to `None`; caller requirements are combined with explicitly discovered content requirements.
The default load deadline
is 30 seconds; limits are 128 requests, 8 MiB per decoded response, 64 MiB total decoded response
bytes and five redirects per chain. Page/depth limits apply to acquisition, not later browser navigation.
Documents, frames, redirects and script network requests count
toward the same session request/byte budgets. These limits do not bound JS heap, GPU memory or CPU.

```csharp
HtmlRuntimeOptions options = new HtmlRuntimeOptions(
    pageScope: HtmlPageScope.SameDomain,
    maxPages: 8,
    maxDepth: 2,
    features: HtmlRuntimeFeatures.All & ~HtmlRuntimeFeatures.Animations);
HtmlUrlPolicy initial = new HtmlUrlPolicy(
    "https://example.com/start", options, HtmlUrlPolicy.ExactHost);
// Actual content requirements and provider capabilities must be discovered before loading.
// No provider is installed or selected by constructing these objects.
```

`HtmlRuntimeCapabilities.ValidateOptions(options, contentRequiredFeatures)` returns immutable
`HtmlRuntimeEffectiveSettings`: actual backend kind, effective enabled supported features, and the
combined required features. Unsupported preferences are explicitly absent from the effective set.
Default static content can therefore use a native backend without JS/Canvas. Actual required JS/Canvas
still causes rejection/fallback when unsupported; a caller-disabled requirement is also rejected.
Disabling any supported always-on feature is rejected even when content does not require it.

The content argument is mandatory: `None` is an assertion about inspected content, not permission to
guess that it is static. Requirements cover the actual final root; later adopted documents also need
discovery/revalidation before executing behavior. No selector or content inspector is implemented here. Discovery must
precede engine/navigation side effects; unknown requirements must be diagnosed, not treated as `None`.
`Auto` is a preference, never a concrete provider claim. Providers must also reject unsupported feature
combinations, backend kinds, acquisition modes and bounds. Capability values are declarations, not
measured verification or security certification.

Disabling `Animations` concerns supported declarative effects/transitions. It does not authorize
canceling JavaScript timers or game RAF callbacks. Disabling `Resources` requires a provider able
to block non-document resources and script network access; a provider without those controls must
reject the request. Required page behavior is never silently discarded.

## Navigation and URL identity

`TryResolveCrawlPage` admits discovered links for acquisition against an accepted acquired document
(or the qualified root). HTTP(S) only; schemes/IDN hosts/default ports are normalized using `System.Uri`,
DNS trailing dots are removed, and path case/query values/query order remain significant.
`GetDocumentKey` removes only the fragment for deduplication. Userinfo, malformed escaping,
whitespace/control characters and backslashes are rejected. Rejections return a diagnostic and no URL.

- `CurrentPage` acquires one final root document and its needed dependencies without following the
  link graph. Initial root HTTP-to-HTTPS/canonical redirects are allowed within the hop budget.
- `SameDomain` requires an explicit host matcher. `ExactHost` is the built-in opt-in policy and ignores
  scheme/port. IP and localhost targets always require exact host equality. Registrable-domain matching
  requires an injected reviewed public-suffix implementation; no such dependency was found/added.
- `AllLinked` permits HTTP(S) documents on other hosts, within independently enforced limits.

Before root commit, validate every hop with `TryResolveInitialRootRedirect`: check HTTP(S), URL syntax,
userinfo, hop budget and any opt-in top-level lock. `SameDomain` also revalidates the seed domain;
`CurrentPage` allows valid HTTP(S) root redirects without assuming the seed is already canonical.
Then call `QualifyRoot(finalUrl)` at commit and retain the returned immutable policy. Root qualification
checks the final URL, not evidence of requests: the caller owns/validates the actual redirect chain.

After qualification, `TryResolveCrawlPage` and `TryResolveCrawlRedirect` revalidate discovered pages and
their redirects against the final root's acquisition scope. For new documents: check scope, deduplicate
by key, call `CanScheduleCrawlPage` and atomically reserve a slot. Redirects use requests/hops, not another
page/depth slot. Seed depth is zero; the final root consumes one page slot. The acquisition owner tracks
counters/visited keys. These methods perform no crawl, prefetch or network request.

`TryResolveTopLevelNavigation`, `TryResolveFrameNavigation` and `TryResolveResource` do not apply crawl
scope. With default settings a page may load another frame/resource host or follow a user link without
that action becoming crawler acquisition. HTTP(S) normalization still applies; request/byte/deadline and
redirect budgets remain explicit independent obligations, including frame/resource redirect chains.

`NavigationRestrictions` is a separate opt-in flags policy: `TopLevelToRootDocument` and
`FramesToRootDocument` can independently lock exact document identity (fragments allowed). A top-level
lock also restricts initial root redirects; requesting it may deliberately reject a canonical redirect.
Only requested locks require `EnforcesNavigationPolicy`; otherwise browser behavior is not advertised
as strict enforcement. Notification-only stock backend callbacks cannot substantiate that claim.
Unenforceable locks and all unsupported requested load bounds must be rejected before loading.

**This is not resource network isolation.** Images, fonts, scripts and script fetches may use different
hosts when resources are enabled. No DNS/private-address filtering, resource-origin allowlist,
process sandbox or CLR bridge is supplied. Optional adapters must enforce their declared budgets,
including redirects of resources; stronger isolation requires a separate explicit provider contract.
`file:` and filesystem paths are rejected until a file-root containment contract exists.

## Lifetime and integration

The caller owns an `IHtmlRuntimeBackend` and must dispose it. One instance accepts one
`LoadAsync(initialPolicy, contentRequiredFeatures, token)` call and exposes validated `EffectiveSettings`;
new options require a new instance/document. The returned `Task<Uri>` completes after root document
commit and its initial load-complete signal, returning the accepted final URL. This does not mean
network idle, crawl completion, stopped JS or rendered parity. The provider owns handles/callbacks,
deduplication, counters, cancellation, timeout cleanup and later navigation enforcement. See the
interface XML contract for exact cancellation/disposal/error behavior and the post-success token rule.
No implicit Unity thread dispatch, view attachment, history or input surface is provided yet.

There is no Inspector/NoCode setup at this stage. A future optional component can wrap this same typed
API after a real provider passes platform, lifecycle, input and rendering checks. No extra packages,
platform binaries or assembly-definition changes are needed to use the pure contracts.

## Verification scope

The corrected pure contracts passed 65/65 tests with no skips in a standalone .NET host on
1 November 2026 (SDK 10.0.301, target net8.0, NUnit 4.6.1). The first run's 57 passes / four
relative-fragment failures were retained; all original 61 cases also passed in the corrected run.
This is not a Unity import/compile, Player/IL2CPP, browser-provider, rendering or input result.
Native integration and lifecycle enforcement remain unverified.

## Русский

Здесь реализованы только типизированные контракты и политика URL, без браузера и сценового компонента.
Настройки неизменяемые; по умолчанию включены предпочтения поддержанных функций, а не обязательное
требование JS/Canvas. Реальные требования контента и `RequiredFeatures` проверяются отдельно; результат
содержит эффективные настройки. Статическая native IR-страница может использовать default Auto без
JS/Canvas. Неизвестные требования нельзя заменять `None`; инспектор контента здесь не реализован.
Провайдер обязан отклонять неподдержанные/выключенные необходимые функции, отключение always-on функций
и невыполнимые лимиты.
Отключение анимаций не разрешает останавливать игровые JS-таймеры или `requestAnimationFrame`.

`SameDomain` требует явной политики хостов: можно передать `HtmlUrlPolicy.ExactHost` либо проверенный
адаптер public suffix. Совпадение зарегистрированного домена из двух последних частей не вычисляется.
Область обхода относится к приобретению страниц, а не к жизни браузера/iframe/ресурсам. Первичные
HTTP→HTTPS/canonical redirects разрешены в пределах лимита; затем фиксируется итоговый root через
`QualifyRoot`. Redirects обнаруженных страниц проверяются относительно области итогового root.
Строгие top-level/frame ограничения задаются отдельными явными флагами и требуют подтверждённой
capability; они не включены по умолчанию. Сеть ресурсов не изолирована. Локальные файлы не поддержаны.
Счётчики и устранение повторных документов принадлежат владельцу приобретения/провайдеру.

Экземпляр backend принадлежит вызывающему коду и принимает один вызов загрузки. Отмена незавершённой
загрузки освобождает документ; после успешной загрузки его закрывают через `Dispose`. Реальное
встраивание, ввод, безопасность, Player/IL2CPP и визуальное соответствие пока не проверены.

Чистые контракты прошли отдельный .NET-прогон: 65/65 без пропусков. Это не заменяет компиляцию
и выполнение в Unity, а также реальные проверки браузерного backend.
