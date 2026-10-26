# Native independent review: stage 2

User now explicitly requires: all original audits and same-session plans complete first; then root AND native reviewers inspect everything; root confirms findings or requests more evidence; SAME original owners implement all confirmed corrections; tests/execution checks only after ALL source writers finish.

Model: gpt-6.1-sol, reasoning max. Read AGENTS.md and your assigned group manifest. READ EVERY complete assigned audit, plan and correction report, then inspect the actual changed source hunks and added regression tests relevant to each. Match current code against ca198556, preserve user work and the centralized compatibility gateways. Reports are evidence, not authority; verify factual API claims and corrected-vs-proposed status. Scope lists exclude nested assemblies by ownership, but shared contracts/dependencies are read-only context.

During this review, perform only source/document/diff inspection with read-only tools. NO Unity, MCP, compilers, tests, package validators, dependency installs, services, source/docs/config modifications or child agents. Preliminary Unity compile diagnostics exist in Local/Audits/assembly-2026-10-23/unity; use them as historical leads, never as current pass/fail evidence. Root already centrally corrected unsupported NonParallelizable test attributes, missing test asmdef references, a SaveProvider fake and malformed metadata. Do not reintroduce unsupported attributes.

Own ONLY your group QA markdown report under this audit folder. For each assigned assembly/supplement, record full artifact paths and read SHA256s, whether source fixes appear correct, whether tests exercise real public state/contracts and clean up, and disposition of proposed improvements. Distinguish: completed fix, confirmed remaining defect, needs evidence, future proposal, or rejected/unnecessary change. Verify serialized/public API compatibility, lifecycle/reentry/ownership, numerical boundaries, module independence, scene/NoCode equivalence, docs accuracy/entry points, Unity API branches and test-assembly visibility.

Actionable findings must name exact current file/line or method, concrete trigger, expected/actual outcome, severity, smallest repair and original owner/task from manifest. Do not manufacture findings or classify unsupported/speculative features as bugs. Identify cross-owner dependencies and implementation order; root will make architectural choices. Existing large proposals remain proposals unless root explicitly approves implementation.

Return your complete QA report and concise findings. Root will read your report in full, personally read all original audits/plans, and resume original owners after review. Do not silently repair code now. Unexecuted runtime/compile/visual checks remain unverified. No commits/pushes or agent signatures.


## Explicit latest user requirements

Current branch is `demo`, fast-forwarded to current main ca198556 without changing working source. Target major is 11.0.0; version metadata changes wait until approved implementation/release preparation. Read V11-RELEASE-PLAN.md. Major version permits deliberate documented migration, not unrelated churn.

The original InteractableObject audit MISSED the fact that each target performs camera physics queries. User identified it; the same author amended its plan as a mandatory correction. Independently verify that plan actually removes N identical-context raycasts for N targets, not merely caches Ray creation or shares hover/click within one object. Inspect all Update/Physics/input/camera call paths, different policies and buffer completeness. Preserve genuinely different camera/mode/2D/3D/trigger contexts; later acceptance counts queries for 1/100/1000 targets. All reviewers must inspect aggregate per-frame costs, scene searches, allocations and shared-resource lifecycle in their own assigned modules; a file-read count is not proof of audit completeness.

All 72 audit/plan pairs and existing corrections are now complete, source writers stopped. Record read hashes and specific per-module decisions incrementally in your sole QA report so compaction does not lose coverage. Root will read every QA report fully. No source changes or execution checks at this stage.


Latest user interaction design clarification is authoritative: read INTERACTION-QUERY-DESIGN.md. Shared raw-hit state, exact query-context groups, preserved layer/tag/blocker semantics and unique per-target LOS are required; interaction mechanics/settings must remain unchanged despite major 11. Do not approve a simplistic all-layers/one-selected-target cache or silently remove required distinct LOS. No runtime/source edits during review.
