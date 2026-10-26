# Native implementation review 34 — final Assets discovery provenance correction

Status: **SOURCE REVIEW COMPLETE; NI30-01 correction supported in source. No further bounded source gap found. Actual compilation, PackageInfo, discovery and test outcomes remain UNEXECUTED.**

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Only this final changed-source report is written. No product/public-doc/config/other-report edit, Unity/MCP/API call, compiler/test/syntax/product checker, process probe/management, install/service, child, commit or push. Prior NI24/NI28/NI30/NI33 and local snapshot reports remain immutable. This does not create authorization or open the global gate.

## Final admission and preserved scope

Read the complete latest **128-line SOURCE-FINAL receipt**, exact updated NI30 request, changed source/DTO/call-site bodies, the complete new 172-line fixture and metadata, and changed Testing-guide contracts. Root supplied fresh 22:53:13UTC successful empty exact-CLI liveness and original primary completion; exact `neo-audit-1023-b03-04.meta` independently reads done/0 for its original session. No process was queried by this reviewer.

Initial gate **2026-10-23T22:56:48.994Z**, final gate **2026-10-23T22:58:50.969Z**: **57/57 source/receipt/report/meta identities match**. Compared with NI24's detailed 53-path ledger, **46 of 47 former C# files remain byte-identical**; only the Discovery partial changes and one provenance fixture is added. Testing, receipt/request and exact owner metadata are the other prior-row changes. No unrelated old fixture is re-audited or reapproved.

Readonly byte reconstruction that removes only the NI30 DTO/selector/call-site changes and restores the previous package match/sort reproduces the **exact former 447-line/22,528-byte Discovery SHA `80aff970b750b597b63834f4bee5b9d492e84ac34887e7fecc8b1f59144cff78`**. This pins unchanged async/SDK/graph/tree/host/deadline/reload/terminal bodies to NI24; it is source identity bookkeeping, not syntax or test execution. The main host and Paths full hashes also remain exactly unchanged.

## NI30-01 / P2 — bounded source closure

Owner: SAME **Neo.Editor.Tests / `neo-audit-1023-b03-04`**.

`D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs:331` still obtains actual PackageInfo records from the SDK. At line 345 it stores their projections in observed SDK order, with no fabricated Neo entry. At line 346 the **actual CaptureCompiledEditorGraph** calls the same internal SelectDiscoveryPackageProvenance method exercised by the new fixture, using the actual Application project root. The selected separate discovery.neoPackage summary also populates existing top-level package identity fields.

The selector at lines 356–393:

- gives registered com.neoxider.tools authority, requiring exact 11.0.0 and nonempty observed source/path; wrong registration throws before any Assets fallback;
- canonicalizes only the summary path and preserves the raw registered records;
- only without registered Neo reads exact `<project>/Assets/Neoxider/package.json`, requires an object envelope, parses through existing JsonUtility/manifest DTO and requires exact observed name/version 11.0.0;
- reports kind/source ProjectAssets with the actual Assets root and manifest path, or kind RegisteredPackageInfo with native source and explicit PackageInfo origin.

Missing files, invalid shape/parser errors and identity/version mismatch propagate to the existing failed discovery receipt/exit path; no fallback invents an assumed package version or claims UPM registration. The driver does not register/embed a package, search arbitrary roots, change the planned Assets profile, remove optional dependencies or substitute an assembly/tree.

The main `Audit20261023NeoEditorTestsValidationHost.cs` remains byte-identical at `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207`. In particular ImportPackageSamples lines 119–127 still requires actual Local/Embedded registration, explicit matching frozen root, package/manifest 11.0.0 and all three real UPM samples. ProjectAssets provenance cannot enter that importer as import evidence.

The public method/CLI is unchanged: `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests`. It still requires actual compiled Editor outputs and nonempty real Edit AND Play SDK leaves, with the same asynchronous ownership/deadline/reload/failed-output semantics. No fresh compile/player/native capability result is inferred.

## All new acceptance bodies — 6 methods / 18 authored cases, UNEXECUTED

Read the complete `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs`, including every parameter and owned-project helper.

| Cases | Meaningful admission oracle |
| ---: | --- |
| 1 | Real owned valid Assets manifest plus unrelated Z/A registered records selects exact ProjectAssets identity/root/manifest origin. Both list order/reference membership and serialized record values stay unchanged; no synthetic registered Neo appears. |
| 3 | Registered Local/Embedded/Registry 11 records beat valid Assets, retain the observed source/version, canonicalize only selected root and do not rewrite raw metadata. |
| 1 | Valid registered 11 succeeds despite malformed Assets, proving that fallback is not read on this branch. |
| 1 | Registered 10.9.0 rejects despite valid Assets; exception identifies the registered mismatch and raw record remains 10.9.0. |
| 1 | A genuinely missing manifest in an existing owned directory raises FileNotFoundException. |
| 11 | Empty/whitespace, array/null root, truncated/malformed object, missing/wrong name and missing/empty/wrong version fail with InvalidDataException. Actual JsonUtility syntax behavior still requires execution of these controls. |

These exercise the real selector used by the driver and actual temporary UTF-8 files, not a parallel fake resolver. Projected package records are deliberately **unit admission inputs**, not native PackageInfo observations or SDK-tree evidence. No test mutates real project manifests, registers packages, loads scenes, probes saves/preferences or invokes discovery.

The unique temporary project is fully resolved and checked beneath the intended temp root before creation; finally deletes only that checked GUID-owned project. Streams are closed by file APIs, no ambient service/static hook is replaced, and no unavailable NUnit serial attribute or asmdef dependency is added. New metadata was read individually (GUID 3a22a7718b574fc3987d22f92b72b535); no global GUID/package validator ran.

## Documentation, retained evidence and final handoff

Testing.md lines 198–215 and the latest receipt accurately distinguish ProjectAssets summary from the actual SDK registered list, registered precedence, failure cases and the unchanged strict importer. The internal DTO/selector visibility is test support in the existing assembly; no production public/serialized API or standalone fake Mirror-free driver is introduced.

The 46 exact prior C# matches preserve NI24's legacy clock, source-state restoration, exact sample identity/native-versus-YAML controls, yielded async fault cleanup and machine duration setters. The Discovery inverse identity preserves real graph/DLL/SDK ownership and all old callbacks/deadlines; no old fixture pass is claimed. NI30's earlier finding remains an immutable record of the pre-correction mismatch.

**No further source correction requested in this bounded delta.** Root alone may now complete its final read/closure and separately authorize a frozen snapshot. After that authorization, compile and run the 18 authored admission controls and the retained suites on Unity 6.3/6.6, then invoke this same helper in both actual Assets and registered release-consumer profiles. Require fresh compile/import evidence, truthful selected provenance/raw registry, nonempty real Edit AND Play trees and independent real UPM import results. Missing/invalid manifest or wrong registration must fail; parser/IO/native/cache/reload/dependency outcomes remain actual acceptance debt.

This reviewer stops all source/report writes after this final handoff. No intent/request/snapshot or execution approval was created.

## Delta and retained-context identities

Unchanged 46 C# per-file identities are the exact NI24 ledger at its hash below; the 57-path comparison gate above covers them. Key changed/call-site/strict-import/context records:

| Full path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs` | 25603 | `3f02ffce3dd206e04f8c2143554e88d9b9bdcfd7be3bdc1b26deedcd7796062c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs` | 9180 | `d16fdaa8bfeb7cebfc59947b9cd78f80311f4d433fe275691c8a6b36c9e78efa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs.meta` | 251 | `70a07069753475c44d6c643d175563d332aabd1ca0f0be24d4c163ce31be9cc7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs` | 24412 | `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs` | 7339 | `586593af17694e6bfe3cb922392139aab830602e57c6f05d501c629a0f1188eb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Testing.md` | 16587 | `99640d561e19df9e57833720f8d225fac43d94f6dbc034f91ad8d5db6a85a1ae` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md` | 26560 | `36c9a47644f21d8f32d9d5f49643f64f849d00f995ee02eb37ecdb7d4f7e4e33` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation-request.md` | 21208 | `f0d42059a1ffd304b45ed05547a4d5995b4ecd8ed033d218cb7896d3693800f9` |
| `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/agents/neo-audit-1023-b03-04.meta` | 233 | `3ab673fb3a9a12745b22379aaf459eb5394c0283d7fe8ccaad591ec4f2daa706` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-24.md` | 26302 | `4d6b5f0e27964b6bc41fdeba7406e115e5c7a35eb4e5f4c4e496e20bfb10023c` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-30.md` | 12782 | `a27ffe6d448c9ccec8ec81d2ed5bf8aeb506c41da34f565b3384e7866dd0f513` |
