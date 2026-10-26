# Neo.Save — individual implementation plan

References: [completed audit](Neo.Save.md), `PLAN-CONTRACT.md`, and manifest entry. Only this plan changes. Path roots: source `Assets/Neoxider/Scripts/Save/`, tests `Assets/Neoxider/Tests/`, documentation `Assets/Neoxider/`.

## Current outcome

All 20 owned C# files were reviewed. F1–F9 are already fixed in `Providers/FileSaveProvider.cs`, `SaveManager.cs`, and `SaveProvider.cs`: failed slot flush retention, structural recovery, backup preservation, registration ownership, inherited private fields, new-scene restoration, malformed component records, reload dirty state, and empty keys. `TryChangeSlot(string)` is already public; do not schedule these fixes again.

Six guides were updated; two EditMode fixtures specify 22 cases. Static inventory, lexical, UTF-8, metadata, link, API-preservation, and whitespace checks passed. Compilation, NUnit, PlayMode, and device/interruption checks were **not executed**. Current relevant source, tests, and documentation changes were reread.

## Prioritized remaining work

**P1 — confirmed configuration failure; owner: Neo.Save.** `Settings/SaveProviderSettings.cs:63–75` returns a plaintext provider when encryption validation fails. Reproduction: enable encryption, supply an invalid-length key or incomplete key/IV pair, create the provider, write and save. Existing ciphertext can become unreadable and subsequently replaced with plaintext. Source control flow confirms this; Unity reproduction is pending.

Correction: add `TryCreateProvider(out ISaveProvider provider, out string error)` reusing key validation; reject invalid encryption before construction. Retain `CreateProvider()` with a descriptive exception for invalid configuration. Update `SaveProvider.cs` and `Settings/SaveProviderSettingsComponent.cs`; publish initialization after successful construction. Currently `_isInitialized` precedes construction, preventing retry after an exception. Failed scene configuration must retain the existing provider. Preserve valid settings/serialized names. Add `Edit/Save/Audit20261023NeoSaveConfigurationTests.cs`: invalid settings leave primary/backup bytes unchanged; corrected settings initialize and recover values. Document the intentional error-behavior change in `Docs/Save/Settings/SaveProviderSettings.md` and `Docs/Save/SaveProviderSettingsComponent.md`.

| Priority / owner | Evidence and smallest next action | Observable acceptance |
| --- | --- | --- |
| P1 / coordinator, Neo.Save | Execute both existing `Audit20261023NeoSave*Tests.cs` fixtures and existing Save suite after concurrent source audits finish. Resolve reproduced failures in the owned path only. | All 22 cases pass, including retry to the original file and valid-empty-profile behavior; no unexpected error logs. |
| P1 / coordinator, Neo.Save | Current scene regression invokes the callback through reflection; add `Play/Save/Audit20261023NeoSaveLifecycleTests.cs` for actual scene loading, disable staging, and repeated sessions without domain reload. | Incoming objects restore once; resident session values persist; disabled objects save automatic fields without replacing manual values; no duplicate subscriptions. |
| P2 / coordinator, Neo.Save | Android/WebGL fallback commits, encryption, IL2CPP reflection preservation, and settings Awake ordering remain unverified risks. Reproduce before changing implementation. | Backup survives failed commit/restart; supported payloads restore in a player; configured provider exists before initial hydration. |

## Substantial proposal: result-bearing operations and explicit sessions

**P2 core, P3 opt-in adapters; not implemented.** Void provider operations cannot report committed versus failed writes. `GlobalSave` retains one cache across backend changes; provider switching does not itself hydrate scene state. These are current limitations, not additional claims that existing applications have failed.

Reuse `ISaveProvider`, file staging/backup/encryption helpers, `SaveIdentityUtility`, and the manager's existing field discovery, normalization, and read-modify-write logic. Keep the current assembly; no serializer replacement, new singleton, external dependency, or asmdef split is needed.

Proposed additive contracts:

```csharp
public interface ITransactionalSaveProvider : ISaveProvider
{
    SaveOperationResult TrySave();
    SaveOperationResult TryLoad();
}

public interface ISaveSession
{
    string SlotName { get; }
    int Generation { get; }
    ISaveProvider Provider { get; }
    SaveOperationResult Flush();
    SaveOperationResult SwitchSlot(string slotName, SlotSwitchPolicy policy);
}
```

Results distinguish success, backup recovery, missing data, invalid payload, decryption failure, I/O failure, unsupported capability, and busy state. Commit confirmation means successful provider I/O, not guaranteed persistence through power loss. Legacy void-only backends must report unconfirmed capability rather than inferred success.

Create `Transactions/SaveOperationResult.cs`, `ITransactionalSaveProvider.cs`, `ISaveSlotFactory.cs`, `FileSaveSlotFactory.cs`, `ISaveSession.cs`, `ISaveSessionParticipant.cs`, and `SaveSession.cs`. All are inside `Transactions/`; policy/status types can share their corresponding contract file. Modify `Providers/FileSaveProvider.cs` so result-bearing loading builds a candidate dictionary and preserves current state on failure. The slot factory opens a separate validated candidate: calling today's in-place `ChangeSlot` cannot provide transactional rollback.

`SaveSession` receives providers/factory and typed participants explicitly, without scene search. It guards reentrancy, stages participants, flushes the old provider, validates the target, then publishes provider/slot/generation once. Missing targets require an explicit create policy; unreadable targets never imply reset. Post-publication participant callback failures are reported separately; arbitrary scene side effects cannot be rolled back.

Adapt `SaveManager.cs` through existing serialization paths, `SaveProvider.cs` through opt-in binding, and `GlobalSave/GlobalSave.cs` through generation-aware invalidation. Existing `SetProvider` retains replacement without implicit flushing and detaches an opt-in session. Unbound applications keep current behavior.

Add `Edit/Save/Audit20261023NeoSaveTransactionTests.cs` and `Audit20261023NeoSaveSessionTests.cs`. Minimum scenarios: failed flush/target validation preserves live slot and pending values; corrupt or wrong-key targets leave both files unchanged; successful switching increments generation once and restores participants once; nested switches return busy; unloaded-scene entries survive; legacy plaintext/encrypted files still load. Pure C# fake providers verify sequencing; real file fixtures verify bytes and recovered values.

Preserve `ISaveProvider`, existing methods, keys, DTO fields, enum values, and all serialized settings. Do not migrate `Bool_` PlayerPrefs storage or change AES format. Update canonical `Docs/Save/README.md`, `ISaveProvider.md`, `FileSaveProvider.md`, `SaveProvider.md`, `SaveManager.md`, and `GlobalSave.md` with typed C# and Inspector/bootstrap examples; avoid requiring UnityEvent chains. Estimate: configuration correction 0.5–1 day; result/session work 4–6 days plus platform QA. Main risks are event order, callback failure semantics, and legacy global caches.

## Dependencies, order, and stop conditions

Retain `Neo.Tools.Managers`' singleton/reset contract and existing `Neo.PropertyAttribute` / `Neo.Extensions` dependencies. Consumer integration belongs to `Neo.Settings` (`GameSettings.cs`), `Neo.Shop` (`Money.cs`, `Equipment/EquipmentManager.cs`), `Neo.Progression` (`Components/ProgressionManager.cs`), and `Neo.Quest` (`QuestManager.cs`), under their module roots. Their owners adopt stable session contracts afterward; Neo.Save must not reference consumers. Coordinator owns test-reference/sample changes.

Order: configuration correction and regressions → central audit verification → result contracts/file implementation → session core tests → optional adapters and consumer integration → documentation/package validation. Keep `Samples~` for release, recognize developer `Samples` and imported `Assets/Samples/NeoxiderTools/<version>/<sample name>` roots, and add unique metadata for future package files.

Release requires Unity 6.0–6.4 and 6.5+ compilation, EditMode results, and stated PlayMode/device checks. Keep any version gates centralized in `NeoDiagnostics` / `NeoComponentHealth`. Stop finalization on data loss, format/API breakage, or uncompiled supported branches. Record reset policy and callback-result semantics before session expansion; routine scoped fixes require no repeated permission. This phase executes no tests and changes no source.
