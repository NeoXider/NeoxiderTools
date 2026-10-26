> Historical frozen-input checks are retained below, including their original failures. Current native/profile/manual acceptance is recorded in [Verification11](../../../Assets/Neoxider/Verification11.md) and [RootIntegration](RootIntegration.md). The first snapshot's FAILED status does not describe the latest corrected source.

# Root package verification — frozen input 1

Status: **FAILED, corrections required.** Actual execution began only after all 72 source owners and the required reviews finished. This result must not be presented as a release pass.

The root-authorized snapshot is `final-20261023T230131Z`, source manifest SHA-256 `0e433bf9848fe86066661c9209ebc955ed75a285e73d1e0a21f458a925e93260`, `demo` at `ca1985564dfe7726834e2832d51c33e0e63e060c`, target 11.0.0. It contains 10,252 files / 142,721,271 bytes. The main product files and frozen inputs were hash-checked before and after the commands; the user-owned `Assets/Neoxider/DocsEn.meta` remained excluded and unchanged.

Actual retained receipt: `Local/Audits/assembly-2026-10-23/unity/root-checks/20261023T230322Z/receipt.json`, completed 2026-10-23 23:04:01 UTC.

| Check | Actual outcome |
| --- | --- |
| `python scripts/check_package.py` | Exit 1. Checked 5,697 package files, 69 assemblies and 3,102 valid GUID records. Found 26 errors across 14 metadata files. |
| `git -c core.safecrlf=false diff --check` | Exit 0; empty output. |
| `python scripts/audit_runtime_hotpaths.py --json <owned output>` | Exit 0; full heuristic inventory retained. This is a source reporting scan, not a runtime measurement or proof that every reported call is a defect. |
| Strict UTF-8 reading of manifest product text inputs | 8,490 files read, zero decoding errors. Valid encoding does not prove that every string is semantically correct. |
| Frozen/main/protected input hash binding | Unchanged at all checks in this root run. |

The package failures are duplicate metadata/GUID headers in 12 files, a malformed first metadata header in the old NeoxiderPages sample, and an invalid GUID in the new Level regression metadata. Exact paths and diagnostics are retained verbatim in `package.log`; no source or metadata was changed during these jobs.

The same independent reviewer is tracing historical GUID ownership and actual references before proposing the smallest corrections. Unity is restricted to initial compile/environment diagnostics for this snapshot, and the independent .NET operator may finish its immutable-copy diagnostics. Full Unity suites, UPM imports and peer players wait for corrected inputs. A correction phase requires owned jobs to stop, a closed execution gate and a new reviewed snapshot before affected execution resumes.

Unity/.NET/native/manual outcomes have their own receipts. No successful build, test count, performance result, commit or push is asserted here.

## Corrected frozen input 2 — package checks passed

The new source is `final-20261023T235218Z`, manifest SHA-256 `9142923cb57ef2a4e6047e3bc1627174ab5a15e9ef2f6823c08ca6a7052611ef`, 10,257 files / 142,803,798 bytes. The first failure above remains unchanged historical evidence. All required source corrections, including NI35/36 and native six-DLL metadata copyback, were completed before this new freeze.

Actual receipt: `Local/Audits/assembly-2026-10-23/unity/root-checks/20261023T235753Z/receipt.json`, completed 2026-10-23 23:58:10 UTC. Package check exited 0: 5,697 package files, 69 assemblies, 3,103 GUIDs, zero errors. The additional valid GUID is the previously rejected Level fixture. Package log SHA-256 is `9ccc68de414b973abbc65a32dd13374403578ed887814054b4b39143edb10c1f`.

`git diff --check` exited 0 with empty output. The hotpath reporting scan exited 0; its findings remain heuristics requiring context/runtime measurement. All 8,490 manifest product text inputs decoded successfully as UTF-8. Frozen/main product and protected user-file hashes matched before and after all commands. These successful static/package checks do not replace actual Unity compilation, discovery, runtime, network or visual acceptance on this corrected snapshot.
