# Neo.Save: audit

Status: source audit and scoped fixes complete; Unity compilation and test execution remain centrally pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Save/Neo.Save.asmdef`.
- Scope: 20 C# files; nested assemblies excluded.

## Scope

Read the repository `AGENTS.md`, the entire worker contract, the exact `Neo.Save` manifest entry, and the original report before editing. Checked the Save assembly and EditMode test assembly definitions read-only. No nested assembly sources exist under this inventory root. All 20 manifest C# files were read in full; paths below are relative to `Assets/Neoxider/Scripts/Save/`.

| File | Review evidence / disposition |
| --- | --- |
| `Example/PlayerData.cs` | Reviewed field attributes, loaded-position application, completion flag, and gated diagnostics. Example applies the stored position; it does not continuously sample the transform. Unchanged. |
| `GlobalSave/GlobalData.cs` | Reviewed serializable empty legacy data contract; no scene references. Unchanged. |
| `GlobalSave/GlobalSave.cs` | Reviewed lazy initialization, first-launch/corrupt-input fallback, subsystem reset, staging vs persistence, readiness, and global cache lifetime. Slot/provider cache isolation remains an architectural limitation, addressed in the proposal below. |
| `ISaveIdentityProvider.cs` | Reviewed identity getter contract against utility and manager consumers. Identity must remain deterministic, unique per concrete type, and stable while registered. |
| `ISaveableComponent.cs` | Reviewed callback contract and both manager load paths. No signature changes. |
| `Neo.Save.InternalsVisibleTo.cs` | Reviewed friend assembly name against existing `Neo.Editor.Tests`. Unchanged. |
| `Providers/FileSaveProvider.cs` | Reviewed every typed getter/setter, nullable data, key deletion, staging, encryption, atomic/fallback commits, backup recovery, dirty state, reload, slot switching, and events. Fixed findings F1–F3 and F8–F9. |
| `Providers/ISaveProvider.cs` | Reviewed backend API/event contract and facade compatibility. Existing interface remains unchanged. |
| `Providers/PlayerPrefsSaveProvider.cs` | Reviewed typed access, `Bool_` storage namespace, deletion, save/load event behavior, and PlayerPrefs-wide `DeleteAll`. Existing representation retained; prefix aliases require care. |
| `Providers/SaveProviderType.cs` | Reviewed enum values and settings/facade switches. Serialized numeric values unchanged. |
| `SaveField.cs` | Reviewed attribute targets, field keys, and automatic/manual flag semantics. Public constructor and properties unchanged. |
| `SaveFileEncryption.cs` | Reviewed key/IV length validation, default-pair/custom-pair selection, whitespace handling, AES ownership/disposal, Base64 decoding, exception paths, and options. AES-CBC is existing obfuscation without authentication; no format change. |
| `SaveIdentityUtility.cs` | Reviewed null/destroyed inputs, custom identities, scene path/name fallback, hierarchy/sibling indexes, and same-type component indexes. No obsolete Unity object ID calls. |
| `SaveManager.cs` | Reviewed registration, inherited fields, duplicate ownership, discovery order, initial/additive scene hydration, stale-object cleanup, all save/load entry points, flags, DTO normalization, primitive/list/array conversion, callbacks, and checkpoint/lifecycle flushes. Fixed F4–F7. |
| `SaveManagerSubsystemRegistration.cs` | Reviewed non-generic reset host and registration-cache reset. Retained existing compatibility workaround. |
| `SaveProvider.cs` | Reviewed lazy Resources selection, event subscription transfer, subsystem reset, lifecycle-hook ownership, explicit replacement, and slot delegation. Facade now reports a successful switch only when the provider accepts it. |
| `SaveProviderExtensions.cs` | Reviewed missing/empty array behavior, deletion, invalid tokens, invariant float formatting/parsing, and fallback arrays. Existing tests cover comma-decimal round trips. Unchanged. |
| `SaveableBehaviour.cs` | Reviewed enable registration, disable staging before unregister, manual-field preservation, and callback override path. Existing behavior retained; its ordering exposed F4/F6. |
| `Settings/SaveProviderSettings.cs` | Reviewed serialized defaults, enum fallback, file options, encryption configuration failure, and provider construction. No scene references or serialized-field changes. |
| `Settings/SaveProviderSettingsComponent.cs` | Reviewed Awake initialization, assigned settings vs lazy default fallback, and provider startup order. Scene setup must initialize the selected provider before save consumers; documented explicitly. |

Context read: existing Save provider, encryption, identity, slot, durability, and read-modify-write regressions; relevant `Singleton<T>` lifecycle implementation; existing canonical Save guides. Reuse references consulted: `neoxider-tools` module/idiom catalogs and the extension catalog search. Fixes extend existing provider/manager paths instead of adding a parallel persistence framework.

## Findings and fixes

Evidence below is a source trace and added executable regression specification. No Unity runtime observation or executed NUnit result is claimed.

### F1 — High: failed slot flush discarded pending values

- Trigger: stage values, prevent the active slot's `.tmp` file from being written, then call `ChangeSlot` or `SaveProvider.SetSlot`.
- Before: `Save()` caught the I/O error and returned `void`; `ChangeSlot` unconditionally changed `_filePath` and loaded another dictionary. Pending values became inaccessible and later saves targeted the destination.
- Fix: retained `ChangeSlot(string)` and added `bool TryChangeSlot(string)`. A failed flush retains the dirty dictionary and original slot; destination loading is skipped. The facade uses the result before logging success.
- Sources: `Providers/FileSaveProvider.cs:48`, `Providers/FileSaveProvider.cs:58`, `SaveProvider.cs` slot delegation.
- Regression: `FailedSlotFlush_KeepsPendingDataAndOriginalSlot_ForRetry`, three entry points. A directory at the staging path forces a real write failure; assertions verify retained value, no load event, successful retry to the original file, and untouched destination contents.

### F2 — High: structurally invalid JSON bypassed backup recovery

- Trigger: primary file contains `{}`, a null `items` list, null item/value records, or a value missing its type tag while a usable backup exists.
- Before: JSON parse success could count as a valid save without a usable root collection; null values could enter `_data`, causing typed access to fail or return reset state instead of recovering the profile.
- Fix: validate the root collection and record containers/type tags before accepting the primary. The private DTO no longer initializes an absent collection into a seemingly valid one; writing still explicitly creates the collection. Valid `{"items":[]}` remains an empty profile.
- Sources: `Providers/FileSaveProvider.cs:455`, `Providers/FileSaveProvider.cs:480`.
- Regressions: five `StructurallyDamagedMainFile_RestoresCompleteBackup` cases plus `ValidEmptyProfile_DoesNotRestoreOlderBackup`.

### F3 — High: first save after recovery destroyed the known-good backup

- Trigger: recover from a damaged existing primary, then save again.
- Before: `File.Replace` rotated that damaged primary over the usable backup. The fallback copy/delete path did the same. A subsequent primary failure left no valid recovery copy.
- Fix: track recovery origin. The first successful commit preserves the known-good backup; ordinary rotation resumes afterward. Both the atomic and fallback paths use this condition.
- Sources: `Providers/FileSaveProvider.cs:316`, `Providers/FileSaveProvider.cs:341`, `Providers/FileSaveProvider.cs:375`.
- Regression: `SaveAfterRecovery_PreservesKnownGoodBackup_InsteadOfRotatingDamagedPrimary` verifies new primary contents, damages that primary again, and observes recovery of the original valid backup.

### F4 — High: rejected duplicate identities could act on the registered owner

- Trigger: two live components of the same concrete type return the same identity.
- Before: registration rejected the second instance, but its `Unregister` removed the first instance's entry. Explicit save could serialize the owner's changed fields through the rejected instance. Explicit load wrote the owner's snapshot into the rejected instance; subset load could reload the owner even though only the rejected instance was requested.
- Fix: unregister and both explicit operations require the actual registered instance; bulk/subset loading also verifies ownership. The existing first-owner registration policy and warning behavior remain intact.
- Sources: `SaveManager.cs:214`, `SaveManager.cs:439`, `SaveManager.cs:624`, `SaveManager.cs:686`.
- Regressions: four independent tests observe persisted contents, owner/duplicate field state, callback counts, and retained registration through a save/load round trip.

### F5 — Medium: private base-class save fields were omitted

- Trigger: derive a saveable component from a reusable base declaring private `[SaveField]` fields.
- Before: one `GetFields(Instance | Public | NonPublic)` call did not return private fields declared in base classes. Save/load silently omitted base state.
- Fix: walk the component inheritance chain and collect declared fields once per type, stopping before `MonoBehaviour`. Existing field keys/type names and public APIs remain intact.
- Source: `SaveManager.cs:168` registration path.
- Regression: `PrivateBaseFields_RoundTripWithDerivedFields`, manual and automatic save cases, verifies both base and derived values after restore.

### F6 — High: OnEnable registration prevented new-scene restoration

- Trigger: a new scene's `SaveableBehaviour` registers before `SceneManager.sceneLoaded`, while the persistent manager already exists.
- Before: discovery returned only previously unregistered objects. The already registered new-scene object was excluded from the subsequent load pass and retained defaults.
- Fix: discovery for a scene callback also returns registered saveables in that scene. Previously registered objects in other scenes are excluded from this added hydration path.
- Sources: `SaveManager.cs:230`, `SaveManager.cs:262`.
- Regression: `SceneLoaded_RestoresAlreadyRegisteredSceneObjects_WithoutReloadingResidentObjects` dispatches the actual private callback once after public registration and checks field/callback results in an additive scene and a resident scene. Reflection only supplies the Unity callback; it does not inspect private state. This is an EditMode callback-order regression, not a claim that Play Mode scene integration was run.

### F7 — Medium: malformed component records aborted valid loads and manual writes

- Trigger: shared container has a null component, null field list/record, missing type name, or duplicate component entries.
- Before: manual save/load dereferenced null records; null fields or type metadata could stop an entire bulk load. Explicit load selected the first duplicate while bulk load used the last.
- Fix: all paths use the existing shared reader, now normalizing null records/lists and duplicate component keys with last-entry precedence. Type resolution is inside per-field error handling, with missing type names skipped. Valid remaining fields still restore.
- Sources: `SaveManager.cs:399` reader and both load paths.
- Regressions: `MalformedRecords_DoNotBlockValidFields_AndLastComponentEntryWins` covers both load entry points and current-value preservation; `ManualSave_RepairsNullFieldLists_AndReplacesDuplicateEntries` verifies a usable saved result.

### F8 — Low: explicit reload retained stale dirty state

- Trigger: stage edits, call `Load()` to discard them, then switch slots.
- Before: the stale dirty flag caused an unnecessary save/event and backup rotation even though the staged edits were already discarded.
- Fix: clear dirty state after replacing the dictionary during reload, before notifying listeners.
- Source: `Providers/FileSaveProvider.cs:282`.
- Regression: `ExplicitReload_DiscardsPendingChanges_WithoutFlushingThemDuringSlotSwitch` verifies restored state and zero save events during the subsequent switch.

### F9 — Medium: empty string keys disappeared on disk round trip

- Trigger: `SetInt(string.Empty, value)`, save, recreate provider.
- Before: setters/getters accepted the key, but deserialization filtered it out.
- Fix: retain empty keys; reject null keys as invalid record structure.
- Source: `Providers/FileSaveProvider.cs:480`.
- Regression: `EmptyKey_RoundTripsThroughDisk` checks key existence and exact value.

### Compatibility and retained constraints

- Changed runtime files: `SaveManager.cs`, `Providers/FileSaveProvider.cs`, `SaveProvider.cs` only. Existing public declarations and serialized fields remain; the only added public method is `TryChangeSlot`.
- No asmdef, package/settings, scene, prefab, sample-layout, or shared test changes. No Unity object-ID API or new version branch introduced.
- Existing `PlayerPrefsSaveProvider` bool keys use the `Bool_` prefix, and `DeleteAll()` clears all PlayerPrefs. These legacy storage semantics were retained rather than silently migrating user data.
- `SaveManager` still uses `JsonUtility`, reflection, assembly-qualified type names, and process-wide registration. Unsupported payload shapes, renamed types/keys, and IL2CPP stripping remain integration concerns.
- `GlobalSave` caches one legacy `GlobalData`; switching provider/slot does not automatically invalidate that cache or reload scene components. This requires explicit application orchestration until a session API exists.

## Documentation

Updated six existing English guides, keeping `Assets/Neoxider/Docs/Save/README.md` canonical:

- `README.md`: typed C# quick start, staging vs flushing, component workflow, Inspector/Resources bootstrap order, runtime-spawn restore, failed-slot behavior, and release/developer/imported sample roots.
- `FileSaveProvider.md`: complete constructor, `TryChangeSlot`, explicit reload semantics, structural recovery and backup preservation, events, scene setup, and explicit local types. Removed the false finalizer-save claim.
- `SaveManager.md`: inherited private fields, registration ownership, malformed data policy, scene ordering, manual/automatic flags, `SaveAutoFields` / `FlushNow` / subsystem reset, serialization limits.
- `SaveableBehaviour.md`: disable staging before unregister and explicit runtime-object loading.
- `SaveField.md`: actual automatic save/load entry points and manual overrides; inherited-key uniqueness.
- `SaveProvider.md`: failure-safe slot delegation and result-bearing provider alternative.

Кратко: исправления защищают данные при отказах записи, восстановлении backup, дублирующихся идентификаторах и повреждённых записях. Регистрация, загрузка и запись на диск описаны как отдельные операции; новые сцены корректно учитывают порядок `OnEnable` → `sceneLoaded`.

## Substantial improvement proposal

### Fallible save transactions and explicit slot sessions — proposed, not implemented

**Problem.** `ISaveProvider.Save()` / `Load()` return `void`; failures are observable primarily through logs, and each consumer must infer success. `TryChangeSlot` can now protect the file provider using its private dirty state, but arbitrary providers have no equivalent contract. Scene registrations, provider selection, and `GlobalSave` cache are global and do not share an explicit slot/session boundary. A caller can switch storage while continuing to display old scene/global state. When both disk copies are unreadable or encryption is misconfigured, the current API cannot ask the application whether recovery/reset is allowed before subsequent writes.

**Architecture / proposed public API.** Keep existing `ISaveProvider` and facade methods; add an optional capability interface and a plain C# session coordinator:

```csharp
public interface ITransactionalSaveProvider : ISaveProvider
{
    SaveOperationResult TrySave();
    SaveLoadResult TryLoad();
}

public interface ISaveSession
{
    string SlotName { get; }
    int Generation { get; }
    SaveOperationResult Flush();
    SaveOperationResult SwitchSlot(string slotName, SlotSwitchPolicy policy);
}
```

Results distinguish committed, recovered-from-backup, not-found, invalid-payload, encryption-failure, I/O-failure, and canceled states, with structured diagnostics. `SaveSession` receives a provider and component snapshot participants through initialization; it has no scene search or MonoBehaviour requirement. Switching prepares/flushes the current snapshot, validates the target, and only then publishes a new generation. Cache adapters invalidate by generation. `SaveManager` remains the optional scene adapter, and `SaveProvider` remains a convenience facade over a default session. Resetting an unreadable profile becomes an explicit policy instead of an incidental next save. A reentrancy guard prevents event listeners from initiating a second switch during a transaction.

**Compatibility / migration.** No existing method removal or serialized-field rename. Existing JSON field names and AES/Base64 files remain readable through adapters. Legacy providers continue to work through an adapter whose result explicitly states that durability could not be confirmed; it must not pretend that a `void` return proves success. Applications can opt into sessions incrementally. A separate documented migration can introduce namespaced/versioned PlayerPrefs keys if needed; the current `Bool_` format must remain readable. Scene wrappers preserve current automatic behavior by default.

**Acceptance scenarios.** A failed flush leaves the old session and all pending values accessible; wrong-key/corrupt target loads cannot overwrite either disk copy; successful switching refreshes both component and global caches exactly once; unloaded-scene snapshots survive checkpoints; callbacks cannot reenter a half-switched session; existing plaintext/encrypted files load without rewritten keys; a pure C# fake provider verifies transaction ordering and failure results without Unity. Device integration checks cover fallback commits and process interruption.

**Effort / risk.** Approximately 4–6 focused developer days for result contracts, file-provider capability, session coordinator, legacy adapters, cache integration, migration documentation, and regressions; platform interruption QA is additional. Main risks are event ordering, legacy callbacks relying on global state, and obscuring unknown durability behind compatibility adapters. Land the capability API and tests first, then opt scene/global wrappers in independently.

## Verification and remaining risks

Checks actually run in this worker:

1. Read-only inventory comparison: exact manifest set equals all C# files under the Save source root, **20/20**, with no unreviewed nested sources.
2. Python static scan: strict UTF-8, no replacement characters, lexical C# delimiter balance, and balanced conditional directives for **22 files** (20 owned sources plus two new regression files). This is lexical validation, not a C# parser or compiler.
3. New regression inspection: **14 test methods / 22 parameter-expanded cases**, explicit types, isolated temporary roots, restored static provider/logging state where changed, and owned-object/scene cleanup. Existing tests are unchanged.
4. Metadata: two new `.cs.meta` files have 32-digit GUIDs; a scoped `rg` search across package metadata found each GUID exactly once, on its new file.
5. Documentation: strict UTF-8 and **29 local links** across the six updated guides passed existence checks. Verified the release `Samples~/Demo/Scenes/Save/SaveDemo.unity` path exists.
6. `git diff --check -- Assets/Neoxider/Scripts/Save Assets/Neoxider/Docs/Save Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs Docs/Audits/2026-10-23/Neo.Save.md` passed. A separate Python check covered UTF-8, trailing whitespace, and final newlines for all **14 changed/new owned files**, including untracked tests, metadata, and this report, which ordinary git diffs omit.
7. Public-method declaration comparison against `ca198556` passed for all three changed runtime files: no previous method declaration removed; the only addition is `TryChangeSlot(string)`. All changed runtime files and both new regression files use explicit local types.

New package regression files:

- `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveFileProviderTests.cs` (+ unique metadata).
- `Assets/Neoxider/Tests/Edit/Save/Audit20261023NeoSaveManagerTests.cs` (+ unique metadata).

**Not run:** Unity, MCP, compilation, EditMode/PlayMode runners, builds, platform tests, installations, or child agents, as prohibited by the worker contract. No commits or shared configuration edits were made. Optional C# tree-sitter modules were absent; none were installed.

**Coordinator actions:** compile `Neo.Save` and `Neo.Editor.Tests`, run both new fixtures and the existing Save suite, and verify Unity 6.0–6.4 / 6.5+ compilation centrally. Confirm `JsonUtility` handling of the malformed-input cases on supported Unity versions. Run a real additive scene lifecycle check and verify provider bootstrap ordering in consuming sample scenes. Android/WebGL fallback filesystem commits, interruption behavior, encryption, IL2CPP reflection preservation, and domain-reload-disabled lifecycle behavior remain unexecuted platform/integration checks. No changes to another worker's source/docs/tests or additional assembly references are required for this handoff.
