# Assembly audit worker contract

Worktree: `D:/Git/NeoxiderTools`; baseline `ca198556`, package `10.19.0`; model `gpt-6.1-sol`, reasoning `max`.

Read `AGENTS.md`, your exact entry in `manifest.json`, and your dedicated report. Every assembly is a separate assigned worker. Own only the source files in your entry (nested assemblies excluded), docs in `docs` with exclusions, your report, and NEW uniquely prefixed regression tests. Read dependencies/tests as context. Do not edit another worker's code/tests/docs; message coordinator with cross-boundary findings.

## Required outcome

1. Inspect EVERY owned C# file and relevant runtime/editor/NoCode workflows, lifecycle, resource ownership, null/boundary inputs, serialization, dependencies, version compatibility, logging and public API/test coverage.
2. Fix actual demonstrated defects using focused backwards-compatible edits. Do not invent a defect or perform cosmetic refactoring for a quota. Preserve serialized fields/public APIs; explicit C# types, no `var`. Use existing package primitives. Read relevant `C:/Users/victor/.codex/skills/neoxider-tools` reference before adding helpers.
3. For each real fix, add useful public-behavior regression tests if practical, with new filenames prefixed by your entry's `testPrefix`. Tests must observe real state/output, not mirror implementation or only assert no exception. Do not edit shared asmdefs: report any required references to coordinator.
4. Update existing canonical module docs and relevant guide(s), explaining public C# API and scene/NoCode workflows where applicable. Preserve established English style; a concise Russian explanation in audit is helpful. Prefer editing existing docs over adding a redundant guide. Nested assembly gets its own narrow topic (`Editor.md`, `Contracts.md`, etc.); if new, coordinator links it from module entry. Do not touch `DocsEn.meta` or create DocsEn. New package files need unique GUID `.meta`; coordinator handles folder metadata.
5. In your separate MD report, replace pending placeholders with full scope review, severity/trigger/observable consequence/source for findings, actual fixes, documentation, and AT LEAST ONE SUBSTANTIAL IMPROVEMENT PROPOSAL: problem, proposed architecture/public API, compatibility/migration, acceptance scenarios, effort/risk. Proposal need not be implemented; clearly distinguish it from completed fixes.
6. Verify static consistency and `git diff --check -- <your files>`; record checks actually run and remaining risks. Compile/tests are centrally pending until coordinator runs them.

## Shared operations reserved for coordinator

No git commits/push/pull/switch/stash, installs, package/asmdef/settings edits, scene/prefab/sample layout changes, Unity/MCP, starting Unity/builds/test runners or shared services. Use read-only shell inspection and scoped source/docs edits. Shared filesystem has concurrent workers: never revert others or treat new worker files as unexpected user changes.

Prior read-only audit leads are under `Local/Audits/modules-2026-10-12-4ca6f50f/` (old revision, verify current code before reusing findings). Module tests have already evolved; do not assume old issues persist.

Return <=200 words: changed source/docs/tests, defects fixed, actual checks, unverified risks and cross-boundary actions. Keep your MD detailed enough for review. Never claim Unity tests passed without execution.
