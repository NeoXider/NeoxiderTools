# Neo.Tools.Debug — implementation receipt

Status: **source-final** for the approved request; integrated execution remains pending the global writer barrier.

- Original actor/session: `neo-audit-1023-b06-06`.
- Target: branch `demo`, package `11.0.0`; original baseline `ca198556`.
- Scope: `Neo.Tools.Debug`, its owned public documentation and prefixed regression source.
- Authority: `IMPLEMENTATION-CONTRACT.md`, `Neo.Tools.Debug.implementation-request.md`, and its complete NativeReview-B section. The request assigns no additional numbered review action IDs or confirmed runtime defect. Completion below maps the earlier audit IDs and the assigned documentation/navigation action; it does not invent a new runtime action.

## Completed actions and exact files

| Action | Files | Completed source work |
|---|---|---|
| Preserve DBG-01 and DBG-02 | `Assets/Neoxider/Scripts/Tools/Debug/NeoDebugOverlay.cs` | Retained the existing cached delegate to `Neo.Tools.KeyInputCompat.GetKeyDown(KeyCode)`, safe unavailable-legacy fallback, and passive reflected `HasInstance` check before `AM.I`. Read the entire current file and its diff; no patch was repeated during resumption. |
| Preserve DBG-03 | `Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs` | Retained both corrected source-location Regex patterns, including paths containing parentheses. No further runtime rewrite. |
| Preserve factual API description | `Assets/Neoxider/Scripts/Tools/Debug/FPS.cs` | Retained earlier XML corrections for sampled `CurrentFps` and target `-1`; FPS runtime remains unchanged. |
| Preserve regression source | `Assets/Neoxider/Tests/Edit/Tools/Debug/Audit20261023NeoToolsDebugRegressionTests.cs` and its existing `.meta` | Read the complete current fixture and retained all eight cases and cleanup. The centrally removed unsupported `[NonParallelizable]` attribute remains absent. No duplicate fixture or new test was added. |
| Finish canonical navigation | `Assets/Neoxider/Docs/Tools/Debug/README.md` | Added a scene/NoCode versus C# action table, the current history limitation, and links to the existing parent Tools, Input, Audio and Save canonical entries. Existing links to all three component pages remain. |
| Finish logger contract | `Assets/Neoxider/Docs/Tools/Debug/ErrorLogger.md` | Clarified string-UnityEvent wiring, direct text calls while disabled, automatic subscription lifetime, and the absence of public capture/history-clear APIs. |
| Finish FPS contract | `Assets/Neoxider/Docs/Tools/Debug/FPS.md` | Clarified integer/bool UnityEvent bindings, display-only setup and the difference between per-frame sampled `CurrentFps` and throttled label refresh. |
| Finish overlay contract | `Assets/Neoxider/Docs/Tools/Debug/NeoDebugOverlay.md` | Added an explicit-type runtime C# example, owner `SetActive(bool)` workflow, preservation of the last F3 choice, per-instance input/sampling versus text cadence, Input backlink and list formatting. |

The resumed source-phase writes are the four public guide edits and this receipt. Earlier runtime/regression-source changes and the central NUnit correction were preserved. The original audit and individual plan remain historical evidence and were not edited.

## Public API and compatibility

No additive public API is introduced by this assigned implementation. The current public paths remain `FPS.CurrentFps`, `SetTargetFramerate(int)`, `SetVSync(bool)`, `ErrorLogger.UpdateText(string)` and `AppendText(string)`, plus normal component/GameObject lifecycle controls for the overlay.

Existing serialized names, types, defaults, `errorText`, prefabs, scenes, package/sample layout and asmdefs are unchanged. No package files or GUIDs were newly introduced during resumption; the existing regression `.meta` is retained. No `DocsEn` or shared release/tracker file was changed.

The intentional earlier behavioral corrections are preserved: unresolved/inactive audio managers are no longer discovered/created by displaying diagnostics; supported parenthesized source paths receive the separate highlighted location; unavailable legacy input does not terminate overlay updates. The overlay still has no public force-show/force-hide API. Disabling/re-enabling restores the last chosen visibility rather than forcing it visible.

ErrorLogger history remains unlimited, even in replace mode or with duplicate checking disabled. `UpdateText`/`AppendText` do not clear that history. The bounded `DebugLogBuffer`, typed capture/configuration/clear APIs and related future fixtures described in `Neo.Tools.Debug.plan.md` remain **proposals, not implementations**; the approved request did not require that optional work.

## Dependencies and current source integration

- `Neo.Tools.Input`: current `KeyInputCompat.cs` now delegates backend selection to the shared `InputBackendCompat.UsesLegacyInput` / `MarkLegacyUnavailable` path. `InputBackendCompat.cs` selects configured legacy support centrally and resets its unavailable-backend cache at subsystem registration. This source has landed in the shared tree; Debug consumes the unchanged public `GetKeyDown(KeyCode)` signature through its existing delegate. No independent keyboard mapper, reverse dependency, shared asmdef edit or pending additive API is required here. This is source inspection, not backend execution evidence.
- `Neo.Audio`, `Neo.Save`, `Neo.Tools.Managers`: optional reflected manager members and passive singleton checks remain their owners' contracts. Debug requires no new direct references or owner changes. Metadata retention in stripped players is still a verification risk, not a demonstrated new defect.
- Shared test assemblies and legacy `DebugToolsBehaviorTests` remain with their primary owners. The existing eight cases do not require a new shared reference for this request. Audio-state cases require an isolated/exclusive runner and exact object/static cleanup; no NUnit parallelization annotation supplies that isolation.

There is **no outstanding source dependency blocking this receipt**. Other owners can still be writing; the coordinator must verify the final integrated snapshot after all writers finish.

## Authored acceptance sources — UNEXECUTED

Retained fixture: `Assets/Neoxider/Tests/Edit/Tools/Debug/Audit20261023NeoToolsDebugRegressionTests.cs`.

1. `Overlay_Update_RendersWithTheConfiguredInputBackend`: actual TMP FPS, scene and time-scale output after the update callback. Execute in Input System-only and legacy/Both configurations. `KeyCode.None` does not prove a real F3 event.
2. `Overlay_Readout_DoesNotCreateLazyAudioManager`: absent singleton and unchanged scene manager inventory while lazy creation is enabled.
3. `Overlay_Readout_DoesNotResolveInactiveAudioManager`: an inactive unresolved manager remains unresolved and the readout uses its missing-state marker.
4. `Overlay_Readout_DisplaysAnAlreadyResolvedAudioManager`: the existing manager still appears with its stopped/random-off/empty-clip state.
5. Four `ErrorLogger_HighlightsCompleteScriptPath` cases: ordinary asset path, parenthesized asset path, parenthesized absolute path and alternate package path. Assertions distinguish the cyan highlighted location from the unmodified raw stack.

Fixture cleanup restores captured `AM.CreateInstance` values and destroys task-owned objects. Its assumptions must actually run in a suitable isolated scene: skipped assumptions are not passes. No new source-level fixture is needed for the documentation-only completion.

## Actual inspection and remaining evidence debt

Performed during this resumption: complete contract/request/audit/plan reads, current owned runtime/docs/regression-source reads, current Input boundary reads, canonical README reference enumeration, scoped Git status and source/documentation diff review. These are read-only source inspections. Historical Regex/static receipts in the audit were neither rerun nor represented as passes for the final integrated snapshot.

No Unity/MCP, C# compiler, tests, product checker/validator, installer, service, child agent, Git commit or push was launched. No new compilation, test, native UI/lifecycle, performance or stripping pass is claimed.

After every writer finishes, central acceptance still needs:

- Nonempty discovery and actual execution of this eight-case fixture plus existing Debug behavior coverage, with isolation and cleanup; preserve failures/skips as such.
- Frozen-source compilation on the installed Unity 6.3 and 6.6 hosts, covering the applicable 6.0–6.4 and 6.5+ API branches rather than treating either branch as compiled by inference.
- Actual F3 delivery in Input System-only and legacy/Both; hidden versus disabled behavior, unscaled-time progress, scene changes, owner destruction and generated-canvas removal. Callback-based tests alone do not prove native lifecycle/input.
- Optional-module and stripped/IL2CPP player checks for reflected Input/Audio/Save members; no-manager inspection must leave scene inventory unchanged.
- Final package/docs/UTF-8/meta/GUID and imported-sample checks owned by the coordinator, including this receipt's public links and preserved sample layout.

This receipt closes assigned source/documentation work only. It neither releases the global execution barrier nor authorizes this actor to perform the eventual root push.
