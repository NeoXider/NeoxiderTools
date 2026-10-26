# NI63 — exact-inventory and capture interpreter-guard follow-ups

Disposition: **NI59-01 and NI60-01 SOURCE-CLOSED; no confirmed residual in these bounded corrections.** Root admitted all scoped writers stopped. Independent source-only review; no Python/PowerShell/parser/probe/checker, git, preparation, capture, copy, Unity/compiler/test, state change or process operation was executed. Only this report was written. Prior NI59/NI60 findings remain immutable historical pins; the separate C helper/NI61 and Managers/NI62 are outside this review and remain their own acceptance boundaries.

## Exact complete read coverage

The three corrected v2 utility files were read completely and compared with the NI59 source pins; the89-line guard is byte-identical. The complete current103-line capture file is accounted for by its exact inverse to the previously read100 lines plus its sole early guard. Actual final SHA256 values:

| Repository-relative file | Lines | SHA256 |
| --- | ---: | --- |
| `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/prepare_schema_maintenance.py` | 245 | `392d8c97e99dad1edd525dc73ead4c6add1022dee50b093d9c0a6e1c7385f86c` |
| `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/launch_schema_maintenance.ps1` | 170 | `9f54dc47428386c9b9215838bed57b66d2adf01af16e3b3fc9ebf2634331e82f` |
| `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/PreparationPlan.md` | 65 | `c40052e145f5ce26cd9e97c2589095fa60674f65f65db53af5e03c025ccaf70a` |
| `Local/Audits/assembly-2026-10-23/unity/maintenance-source/asset-schema-v2/operator_guard.ps1` | 89 | `d9bf59db9f12f82d6549b6001e5f3db2c5cc1a9db1ddf9f79918078c13e58698` |
| `Local/Audits/assembly-2026-10-23/capture_current_metadata_source.py` | 103 | `131e2c64d22ef460ac6100115684b5577e1e8027f7cbe1c2223bb63d961daabe` |

## NI59-01 — bounded pre-import file-set correction supported

- Python `ordinary`27–36 now checks ancestors root-to-leaf with lstat; `inside`39–44 also rejects absolute/backslash/colon/empty/dot/dot-dot components. `exact_inventory`47–75 validates canonical unique/casefold-unique expected files, enumerates one directory at a time, rejects symlink/reparse entries before descent and nonregular/hardlinked files, rejects actual entry case collisions and every unexpected file, then requires the complete expected set. Missing/permission/enumeration errors fail rather than imply an empty successful inventory.
- The source exact set is checked before all listed SHA reads146, after150 at151, and at preparation finish235. Every copied asset/archive hash and canonical target hash remains checked. Completed archive236 must exactly match its ledger; project237 admits precisely seed-ledger files plus `Receipts/inputs.json` and `Receipts/preparation.json`. There is no wildcard Assets/Receipts exemption.
- PowerShell `Task-Ordinary`14–31 likewise walks ancestors root-to-leaf. `Task-ExactInventory`32–63 rejects noncanonical/duplicate/case-colliding expected names, checks each encountered entry before pushing a directory, rejects reparse/hardlink/nonordinary files and actual case collisions, then enforces exact file-set equality. Its final archive/project checks145–146 are directly before the CreateNew claim147, after the long gate/hash/guard/freshness preflight. An unlisted Editor script/meta, stale stdout/receipt or extra archive member now reaches an explicit rejection before native claim/import. Original SHA checks remain101–116.
- These are exact FILE inventories: ordinary empty directories are traversed and contribute no file member; their names still participate in actual case-collision detection, and a file anywhere beneath them must be explicitly listed. A missing/non-directory required root fails enumeration; no missing archive is silently treated as empty. The separate existing Library/lock/job/log/native-receipt absence checks remain124–126. No broader directory-tree or concurrent-host lease framework is claimed.
- The revised plan37/43/53/65 matches these actual boundaries and keeps meaningful future extra/missing/Editor/stale-control/case/link/in-preflight-injection controls explicitly UNEXECUTED. Authority,314 original identities/provenance/current SHA derivation, canonical232-only eligibility, full owned legacy archive/no `/l` or main copyback, unique prelaunch identity and fresh Library semantics remain unchanged. The unchanged guard preserves both closures, two strict6GiB/8GiB CIM admissions, seven-parameter/path/Hidden/PassThru contract and explicit exit propagation; none has been certified by a new operator run.

## NI60-01 — optimized-interpreter refusal supported

Capture5–6 now uses explicit `if not __debug__: raise RuntimeError(...)` immediately after the docstring, before imports, reads, git, gate work or writes. This remains executable under -O/-OO instead of being stripped like assert. Normal nonoptimized invocation retains the original assertions. Removing exactly this one three-line insertion restores the complete old capture byte-for-byte, SHA `a1d5f26ef090d6c0152c2b5e93036d9718fc6156aa022dc9343f2e1afd07103d`; no readiness, protected-file, selected-input inventory, copy/hash/output or no-authority behavior changed.

All five closing hashes were rechecked unchanged. Source closure grants no utility execution, fresh readiness certificate, metadata request/native authority, upgrade, claim reuse or product verification. Root still needs all separate source/helper reviews closed, a NEW current-source seal/request and explicitly authorized meaningful operator controls before later native boundaries; old v1 probes/native outcomes remain historical.
