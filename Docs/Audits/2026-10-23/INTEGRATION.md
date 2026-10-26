# Integration review and verification

## Current correction after focused native checks — 2026-10-26

The completed `p6` diagnostics used immutable snapshot `92c01d54d785875c7d26035630b17fbdf4454170c53ae243bfa31970ad206521`. Both Unity 6.3 and 6.6 compiled/imported successfully and discovered 4586 Edit / 727 Play cases. Each engine executed 481 selected cases: **460 Passed / 21 Failed / 0 Skipped**, with exact SDK/XML identity matching. These were targeted checks, rather than unrestricted full suites.

All **145 selected Play cases passed on both engines**: Interactor112, including the twelve Camera/Keyboard 2D/3D 1/100/1000 query matrices, plus NetworkIsolation19 and NetworkManagerSession14. Other passing groups include all Inventory Commit39/Runtime53, SaveManager10, NPC18, Condition25, Binding8, Cards6, Interactive4, Sample9, Other8, Level8 and legacy Image17. Six new Image target-lifetime cases pass. The four canonical/middle-escaped FileSaveProvider payloads pass; first-character escaping is a separate pending diagnostic.

The remaining failures are retained: Text11 lacks the shipped SDK's genuine Play/initialization prerequisite; View's dirty-callback case observes immediate retirement/zero/hidden successfully but checks OnKill before deferred cleanup; two prefab fixtures attempt a native-forbidden save of a missing script. Lobby generation still reaches illegal additive creation with untitled work, and the legacy leaderboard reader skips Mono's mapped escaped member names. These causes have bounded SAME-author corrections and independent review; authored changes are not native passes.

All twelve p6 jobs stopped naturally before source reopened. All10442 frozen/main product inputs and protected user metadata were preserved; owned Editors=0. The canonical request/intent were archived unchanged. Source execution is closed during the next correction batch. Unrestricted suites, true optional-dependency/UPM/import profiles, independent peer processes and selected manual acceptance remain required before the authorized normal push to `demo`. **No new commit, push or release acceptance is claimed.**

The current package/text/documentation lane passed with 5825 package files, 71 assemblies, 3175 GUIDs and zero errors; strict UTF-8 checked 8618 texts. README links and five raster identities passed. Exact-input .NET evidence remains 2852 Passed / 30 historical NotExecuted / 0 Failed, explicitly reused with the recorded net9-target/runtime10 qualification.

## Historical source correction after full runs — 2026-10-25

The `p5` run tested snapshot `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. Every listed full suite was unfiltered, used real D3D11 graphics and produced a complete terminal XML:

| Editor | Full EditMode: passed / failed / skipped | Full PlayMode: passed / failed / skipped |
|--------|----------------------------------------|----------------------------------------|
| Unity 6000.3.14f1 | 4469 / 66 / 2 | 704 / 19 / 1 |
| Unity 6000.6.0f1 | 4470 / 65 / 2 | 704 / 19 / 1 |

Both Editors then passed **35 targeted nonbatch EditMode and 4 targeted PlayMode cases each**, with no skips. These runs established focused cursor capability/ownership and the real end-of-frame ordering that the batch host could not establish. The raw full-suite failures remain unchanged; a capability follow-up is not a rewritten full-suite pass.

Root and independent reviewers read all failure groups, identities, messages and stack variants. Twelve original `gpt-6.1-sol` / `max` sessions now own bounded corrections. These include legal persistent-scene fixture creation, serialized binding prerequisites, inventory inline-instance admission, escaped legacy JSON keys, deferred text binding/tween ownership and interaction test/native parity. Runtime defects are distinguished from failed fixture prerequisites; native callback invalidation and all substantive assertions remain required.

All owned Editor jobs stopped before source work reopened. Main/frozen input preservation and the excluded user file were verified first. Execution is closed during this correction batch; new compilation and tests require all writers to finish, reviewed changes and a new snapshot. Current-source Mirror-absent/no-Pro/UPM imports, input/reload variants, real peer processes and remaining manual acceptance still need their own evidence. **No new release commit, push or overall pass is claimed.**

## Earlier frozen-snapshot checkpoint — 2026-10-25

Target: **11.0.0 on `demo`; full Unity acceptance pending, no new release commit or push.** The user authorizes the final normal push to `demo` after the required verification. All 72 source owners have stopped; root has accounted for their current receipts and required independent reviews.

The current frozen source is `final-20261025T151934Z`, manifest SHA256 `956e2aa29ea85e7dea90df498a70392083af4a067058cbcbe24891105966efd1`. It contains the NPC test compiler correction, the shared interaction-query work, the Pages prefab repairs, the stylesheet fix and the latest README with five generated raster images. Source approval and visual review are not runtime test results.

- The package checker actually passed: **5814 files, 71 assemblies, 3169 GUIDs, zero errors**. Strict UTF-8 decoding passed for 8607 captured product text files. Staged and unstaged whitespace checks passed; three standard empty-value trailing spaces in a new documentation folder meta remain deferred until the owned Unity jobs stop. No introduced explicit-type candidates were found; unchanged legacy code is not claimed to be globally free of `var`.
- Exact input and dependency identity permits reuse of the retained .NET results: **2852 Passed, 30 historical NotExecuted, zero Failed** across seven TRX files. This is not a fresh run. The net9-target tests used runtime10 with process-local major roll-forward; exact runtime9 acceptance remains absent.
- The README and all five actual PNGs were reviewed by root and independently in NI66. Concept artwork is identified as such. The unchanged `.github/UNITY_CI.md` is a supplemental documentation input omitted from the snapshot, rather than a broken main README link.
- Full Unity 6.3/6.6 EditMode and PlayMode results, optional-dependency/sample checks, real peer processes and selected manual acceptance remain pending. Historical Source3 failures are retained and do not certify this corrected source.

The earlier proposal to rewrite all 232 canonical assets is withdrawn. The native logs show old-schema diagnostics followed by `Imported` and successful compilation/discovery; those messages alone did not prove 232 broken assets or blocked tests. All 314 classified asset bodies/metas, including the 82 legacy imported targets, remain unchanged. The separately accepted six-DLL metadata repair is retained. The optional Mirror Icosphere example still has its known unresolved material mapping; no causal Neo test failure currently justifies inventing a material or deleting that mapping. Actual failing asset oracles must be assessed individually without suppressing logs or weakening tests.

Evidence is retained under `Local/Audits/assembly-2026-10-23/`; the final .NET/package lane is `final-verification/final-20261025T151934Z-956e2aa29ea8-c/FinalResults.md`. A single generated Python bytecode cache outside the manifest is inventoried as excluded diagnostic context. All authored frozen inputs, main product bytes and the protected user-owned `DocsEn.meta` retain their recorded hashes.

## Historical preparation baseline

Baseline `ca198556`, package 10.19.0. All workers: `gpt-6.1-sol`, reasoning `max`.

## Cross-assembly follow-ups

- Network.Contracts NC-03: review `NeoNetworkState.IsAuthorized` accepting unnamed `NetworkAuthorityMode` integers via permissive default; confirm Network worker fixed it, otherwise coordinator owns a narrow fail-closed correction/regression.
- Link new narrow assembly guides (Editor, Contracts, Bridges, testing helpers) from canonical module entries after all owners finish.
- Add only required missing references to shared test asmdefs after inspecting new tests; assembly owners may not change shared configuration.
- Generate missing folder metadata without touching the user's `Assets/Neoxider/DocsEn.meta`.

## Required integrated checks

- Package checker: baseline passed (5158 files, 68 assembly definitions including bundled CMF and optional Demo, 0 errors); final pending.
- UTF-8, known encoding-corruption markers, added explicit C# types, whitespace, Markdown links, GUID uniqueness: final pending.
- Unity 6000.3.14f1 compilation and EditMode/PlayMode: pending source-final integration.
- Unity 6000.6.0f1 isolated project: compilation and EditMode/PlayMode pending; checks the alternate centralized ID API branch.
- HtmlToUitk .NET suite: initial restore/build succeeded; .NET 9 runtime absent (8 and 10 installed). Rerun with process-scoped major roll-forward is running; exact environment/results will be recorded.

These baseline audits describe demonstrated source findings. Unexecuted compile/runtime checks are not passes. The latest status and publication authorization are recorded above; no new release commit or push has occurred.
