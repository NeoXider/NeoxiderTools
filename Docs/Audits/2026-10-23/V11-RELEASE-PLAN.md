# NeoxiderTools 11.0.0: release preparation

User-selected target branch: `demo`. It was 67 commits behind current main with no unique commits and was fast-forwarded locally to `ca198556` before switching; all uncommitted audit/source work stayed in place. No commits, pushes, tags or publication performed.

Target next major: **11.0.0**, current evidence baseline **10.19.0 / ca198556**. Preserve historical audit/version receipts. Apply version metadata, current README badges/entry points, PackageHealthCheck.VersionMentions, bundled skill metadata, sample examples, changelog and migration guide coherently during the post-review implementation/release-preparation stage, before final tests.

Required stages: all audits/plans complete (including explicit interaction raycast amendment); root plus native independent review; SAME original owners implement all approved corrections; only after every source writer finishes, global compilation/tests/packaging checks. Large API changes require explicit compatibility/migration reasoning and acceptance scenarios; the major bump is not a reason for unrelated API churn. Preserve serialized data or provide a documented intentional migration.

Explicit user priority: eliminate one camera physics query per InteractiveObject. Plan a shared camera/player query context, passive targets/dispatch, separate genuinely different ray/physics policies, complete hit buffers and lifecycle invalidation. Acceptance must count queries for 1/100/1000 same-context targets and preserve interaction behavior. No tests execute before the final global source barrier.

Keep sample layout coherent with actual work: release Samples~ and package paths; developer Samples only when intentionally activating sample authoring, imported samples remain host-project copies. Current root layout is not renamed just because the branch is demo.
