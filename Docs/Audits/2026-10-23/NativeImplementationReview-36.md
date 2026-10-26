# Native implementation review 36 — NI35-01 alias closure

Same independent native C reviewer/session; bounded source-only continuation of immutable `NativeImplementationReview-35.md`. Repository: `D:/Git/NeoxiderTools`. Only this report was written. No Unity, compiler, tests, validator, process probe or maintenance was executed; no authorization or product file was edited.

## Admission and exact scope

Root reported SAME primary owner DONE/exit 0 and fresh exact-CLI liveness empty at 2026-10-23 23:37:22 UTC. Reviewed only the two authorized alias insertions, all actual `PackageInfo` / `DependencyInfo` uses across the three helpers, current full receipt, retained provenance-fixture identity and closing raw-file hashes. No broader source reassessment was needed.

Read all 144 lines / 30192 bytes of `Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation.md`, SHA-256 `7a035526be8a866a8ae8493982300e7a0ddebeb0c2ddcd215ba72cbc48f21f30`. Its NI35 continuation at lines 15–21 accurately distinguishes the source-derived residual from old actual compiler diagnostics and leaves all aliases uncompiled/unexecuted.

## Exact correction

Both files below `Assets/Neoxider/Tests/Edit/Editor/` add exactly one physical line: `using PackageInfo = UnityEditor.PackageManager.PackageInfo;`.

| File / alias line | Pinned previous SHA-256 | Current SHA-256 |
| --- | --- | --- |
| `Audit20261023NeoEditorTestsValidationHost.cs:13` | `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207` | `a0f7206efba4d678f668bd473f6ec1c5638ff95204675fea5eb58644da80239d` |
| `Audit20261023NeoEditorTestsValidationHost.Discovery.cs:12` | `3f02ffce3dd206e04f8c2143554e88d9b9bdcfd7be3bdc1b26deedcd7796062c` | `95cbb42b2142730075e4c45c76c0aafb44956f8369d1c8a610da16052edeca6e` |

For each file, raw-byte removal of its single added alias line produces **exactly** the previous frozen snapshot-1 bytes and pinned NI35 hash. This establishes retention of all prior method bodies, ordering, guards, callbacks, deadlines, terminal handling, provenance selection and CLI behavior without relying on a line-count claim.

The earlier Paths alias remains at line 5 with unchanged SHA-256 `e20adc92fdc27f99f8e5968dfc85d34671c43c5b836c48e0363c3a39587bf9e6`. Its return type and SDK iteration at lines 31/35, main host's local at line 120 and Discovery's SDK iteration at line 332 all now bind to the intended file-local package-manager type. Discovery retains `using UnityEditor.PackageManager;` at line 8 and actual `DependencyInfo` / `Array.Empty<DependencyInfo>()` usage at line 335. Remaining package-info occurrences at lines 370–371 are provenance strings, not unresolved types.

The 18 previously reviewed provenance controls are unchanged by fingerprint: `Audit20261023NeoEditorTestsDiscoveryProvenanceTests.cs`, SHA-256 `d16fdaa8bfeb7cebfc59947b9cd78f80311f4d433fe275691c8a6b36c9e78efa`. No fixture rewrite, API/settings change or new test framework was introduced for declaration-only qualification. Prior NI24/34/35 unchanged-source coverage is retained, not re-certified as executed.

## Disposition

**NI35-01 is closed at source level.** The exact accepted two-file correction resolves the remaining file-local ambiguity and introduces no logic delta. Closing reads retained the hashes listed above and the full current receipt hash; no moving-file divergence was observed within this review window.

All three aliases still require fresh compilation on both actual Unity versions. Existing first-snapshot CS0104 and other diagnostics remain historical evidence, not current results. Native discovery/import/tests and the separate six-vendor-DLL metadata migration are not approved by this source closure. Root retains ownership of freeze, maintenance and execution authorization.
