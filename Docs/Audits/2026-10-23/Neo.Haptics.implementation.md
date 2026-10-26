# Neo.Haptics — implementation receipt

Status: **SOURCE-FINAL**. Assigned source/docs/regression-source work is complete.
Target: branch `demo`, package `11.0.0`. Original worker/session continued.

## Public API publication and dependencies

**Additive APIs: none.** No dependent owner needs to wait for a new Haptics member.
The existing `Neo.Haptics.Haptics` facade and numeric values of `HapticType` are preserved.
The proposed `HapticService` / `IHapticBackend` extraction remains unimplemented in the historical
individual plan; no service, new dependency or scene/NoCode component is being introduced.

No source dependency is pending. The current `Neo.Editor.Tests.asmdef` already references
`Neo.Haptics` (line 57 when read). The coordinator's removal of the unavailable
`NonParallelizable` attribute is preserved. Shared-state execution depends on the exclusive
runner contract and fixture cleanup, not that attribute.

## Approved actions completed

The implementation request assigns no additional numbered runtime correction after native review.
Preserved the approved audit fixes, with no duplicate rewrite or regression fixture:

| Action ID | Completed behavior retained |
|-----------|-----------------------------|
| HAP-01 | Per-type cooldown history survives interleaved types; both reset paths clear it. |
| HAP-02 | Null/zero-pair patterns return before gates or `Played`. |
| HAP-03 | Non-finite scalar parameters or paired pattern elements return before gates or `Played`; unmatched tails remain ignored. |
| HAP-04 | Undefined enum values return before enablement/history mutation or notification. |

Completed the request's canonical documentation/navigation and factual API/scene/NoCode work:

- Added a backlink to the existing documentation index and local API/scene navigation; explained
  the `Neo.Haptics` reference required by a custom consumer asmdef.
- Replaced the unsupported claim that Unity always ignores a missing named assembly reference
  with the actual define/reference configuration and explicit backend-present/absent evidence debt.
- Documented synchronous `Played` ordering, propagating observer exceptions after the timestamp is
  recorded, and caller ownership of pattern arrays read again after notification.
- Added the 11.0.0 compatibility behavior while retaining typed C# and thin scene/UnityEvent usage.
  No mandatory scene/NoCode wrapper or additional assembly dependency was introduced.

## Exact files and compatibility

| File | Result |
|------|--------|
| `Assets/Neoxider/Scripts/Haptics/Haptics.cs` | Earlier approved runtime patch preserved byte-for-byte against the reviewed fingerprint; no new runtime edit in this phase. |
| `Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsTests.cs` | Existing meaningful public-event regressions preserved, including the central attribute removal; 12 methods / 23 cases. No duplicate tests added. |
| `Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsTests.cs.meta` | Existing metadata retained; GUID `a117a463833c4036a382c12540757ab0`. |
| `Assets/Neoxider/Docs/Haptics/Haptics.md` | Canonical guide completed as described above; existing metadata retained. |
| `Docs/Audits/2026-10-23/Neo.Haptics.implementation.md` | This new source-final/API/dependency receipt. |

All existing public signatures, enum values, native mappings and the default repeat interval remain
unchanged. The intentional behavior corrections are HAP-01–04: interleaved requests now obey their
type's interval and rejected inputs are unobservable. No serialized field rename, asset migration,
sample-layout change or scene rewrite is required. Finite scalar clamping and paired-length behavior
remain as before. Pattern snapshot ownership is still a proposal; it is not claimed as a completed
native reproduction or implementation.

Historical `Neo.Haptics.md` and `Neo.Haptics.plan.md` are unchanged. Their former missing-reference
and fixture-parallelism statements describe the earlier audit snapshot; this receipt records the
coordinator's subsequent integration. The substantial service/backend/clock improvement remains
explicitly proposed in that plan.

## Authored acceptance cases — UNEXECUTED

The preserved fixture `Neo.Editor.Tests.Audit20261023NeoHapticsTests` observes public output:

- All nine types across two interleaved passes produce one event per type; custom/named `Medium`
  and pattern/named `Heavy` aliases share cooldown across a different intervening type.
- Reset permits each previously played type again; zero interval permits immediate repetition.
- Disabled requests do not reserve cooldown, and a disabled master switch does not call the provider.
- Null/empty patterns and all three non-finite values in scalar/paired inputs emit no event;
  a subsequent valid pulse emits the expected event. Empty cases assert the intermediate empty list,
  rather than merely checking the final count.
- An unmatched non-finite tail is ignored; undefined enum values `-1`, `9`, `int.MaxValue` neither
  appear in the event sequence nor clear a valid type's cooldown.

Fixture cleanup removes its listener, restores master/provider/interval, and clears throttle state.
It must run under the coordinator's exclusive shared-state runner contract. No execution or pass
result is claimed for these authored cases.

## Work actually performed and evidence debt

This phase used small cmd.exe/Python reads, source/diff inspection, reference enumeration and hash
bookkeeping. Confirmed the active branch is `demo`, inspected the single owned runtime source,
fixture, canonical guide and assembly metadata, and read the full implementation contract/request
and historical audit/plan. The resumed interruption had left no implementation receipt or partial
implementation patch to overwrite. Only the canonical guide and this receipt were edited here.

Read-only fingerprints after documentation completion:

| Artifact | SHA-256 |
|----------|---------|
| Runtime source | `11443a1ba5417dcf57ae6fa9868b851eb36465e0fe94bb063c4a958e7baaa682` |
| Regression source | `0d2ea828fc6270c4184e01bf804942a37afb2afc69100c54ebb1fc7dc5f139d5` |
| Canonical guide | `5569aa8006f5cd86ba7d1e455bb0d0347904f3c71014bf90b0f6786a25e75957` |
| Historical audit | `355cff26fd2ca9a6ba90e3302e03f94fa69beee2248d743176b1741ee58dd4c2` |
| Historical plan | `944e6496e6405b15e2c35fbc3bb9ac0af74ce019e095b4203af5643cfc9ac1f4` |

**Remaining source dependencies: none.** Shared test-reference integration is already present and
requires no owner handoff. No further runtime or test-source correction was assigned by this request.

**Execution remains globally deferred:** no compiler, Unity/MCP, test runner, product checker,
validator, service, installer or child actor was launched. No commits or pushes were made.
After every source and documentation writer finishes, root owns frozen-source compilation and
nonempty discovery/execution of the preserved fixture on the installed Unity 6.3/6.6 hosts, with
backend-present/absent branch evidence. Other unsupported/unexecuted version branches stay explicit.
Play Mode pause/domain-reload reset and Android/iOS native dispatch, prepare/stop and waveform
behavior remain evidence debt. Editor events do not establish native payload, missing-reference
resolution or physical motor behavior. Device/vendor availability limits must be reported separately.
