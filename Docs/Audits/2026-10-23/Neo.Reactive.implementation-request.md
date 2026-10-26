# Neo.Reactive: approved implementation request

Original actor: `neo-audit-1023-b05-02`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### REACTIVE-XML-CONTRACT (P3; approved-source-correction)

Class summary overstates disappeared-subscription warning; collected/count cleanup silent, destroyed-target warning gated/throttled during dispatch.

## Relevant full native sections

Source: NativeReview-A.md

### Neo.Reactive — source review complete; no additional mandatory runtime defect established

Original owner: `neo-audit-1023-b05-02`. Full reads:

- `Docs/Audits/2026-10-23/Neo.Reactive.md` — `8904d7a1181065fe6441ba81e7c32b9d0efdfb6cb2d824264e63d8f5d175150b`.
- `Docs/Audits/2026-10-23/Neo.Reactive.plan.md` — `eedfb54a1446522414297d0361c52c095ffc959f8ead9b2c3a9657f5217d9b0e`.
- `Assets/Neoxider/Tests/Edit/Reactive/Audit20261023NeoReactiveRegressionTests.cs` — `c89f0424f7c535a869534f6e7e6b743926822086edcad4f44f64924725e3188c`.

Both source diffs, complete new fixture, both changed public-guide diffs and full notification/snapshot/subscription/liveness/disposal paths were read. **Completed fixes appear correct:** count-triggered liveness sweep, safe explicit-target initial dispatch, whole-multicast strong fallback and null removal. Code snapshots retain captured values/membership under nested notification; separate nested buffers are cleared in finally, outer shared buffer is cleared, and idempotent handles remove their own captured registration. Tests use public actual values/calls/counts, destroy owned Unity subscribers, and isolate GC targets in a no-inline scope; GC/AOT/native runtime outcomes remain unexecuted. Exception-log expectations require the diagnostics error gate enabled in the later isolated test configuration.

No new mandatory runtime defect was established. The author's remaining **P3 XML correction** is valid: the class summary's universal disappeared-listener “reported once” wording does not describe silent collected-target/count sweeps or throttled destroyed-target warnings. Narrow it to match the already-correct guide, without changing runtime behavior.

Aggregate cost: delivery is O(code listeners + UnityEvent calls), reuses its outer snapshot after growth, allocates nested arrays, and weak setup allocates state/WeakReference/delegate invocation lists. Count reads intentionally scan all registrations; delegate dedup registration is linear. The cached invoker metadata retains MethodInfo/types rather than listener instances. Strong multicast/AOT fallback retains the group, uses final delegate Target for group liveness and shares group exception behavior; independent lifetimes require separate registrations as documented. Held explicit weak handles do not retain the target when the handler is noncapturing. Legacy duplicate delegate handles share one registration by contract, so do not silently change ownership. The additive Observe/retention-metadata API is **future work**, and the amended plan correctly avoids an unnecessary new registry/framework or changes to external interfaces. Player/AOT, native persistent UnityEvent and GC behavior remain **needs evidence**.

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Reactive.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.
