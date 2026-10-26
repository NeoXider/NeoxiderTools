# Native implementation review 30 — discovery provenance for the Assets source profile

Status: **SOURCE REVIEW COMPLETE; one confirmed P2 driver/profile mismatch NI30-01 requires the SAME primary Editor.Tests owner's bounded correction.** No Unity/API call, helper execution, compilation, test/checker, package registration, profile copy or service/process operation was performed. Actual host discovery remains UNEXECUTED.

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Own only this new report. NI24, NI28, both local snapshot reviews, source/config/public guides and trackers remain unchanged. No children, install, commit or push.

## NI30-01 / P2 — a valid Assets source host cannot satisfy mandatory registration

Owner: **Neo.Editor.Tests / `neo-audit-1023-b03-04`**, the original primary entrypoint author. Exact current source: `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs:319`–340, CaptureCompiledEditorGraph. The package identity is assigned only inside the GetAllRegisteredPackages match. Line 340 unconditionally throws if that match is absent.

Concrete planned source profile:

- `Packages/manifest.json` has no com.neoxider.tools dependency; its lock also has no such package. Readonly directory enumeration finds only manifest.json and packages-lock.json under Packages, with no embedded Neo directory.
- The mirror-on-dev recipe at `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/UnityVerification.execution-plan.md:86`–89 copies the Assets source tree and activates Samples there. It does not install/embed Neo as a UPM package.
- The actual `Assets/Neoxider/package.json` declares name com.neoxider.tools, version 11.0.0 and the three current sample identities. The existing test Paths at 31–53 explicitly use Assets/Neoxider when PackageInfo is absent.

Reproduction at the public contract, source-traced only: prepare that ordinary isolated Assets host with its valid 11.0.0 manifest and real successful compilation, then call DiscoverCompiledTests with the existing owned-host/company/product/output arguments. With Neo correctly unregistered, the graph capture reaches line 340 and throws before the first RetrieveTestList. DiscoveryUpdate calls CaptureCompiledEditorGraph at 241 before invoking the SDK at 254; its catch at 269 sends the failed terminal receipt/exit 1. Valid compiled Editor DLLs and available actual Edit/Play tests cannot reach discovery.

Expected: record honest ProjectAssets provenance from the validated actual Assets manifest and continue the **same real graph/tree** operation. Actual: the helper requires UPM registration even for the explicitly supported source profile. This is an admission defect that blocks planned verification; it is not evidence that a UPM consumer should be unregistered or that a native invocation was attempted.

## Registration semantics and local auto-registration evidence

Read installed Unity 6.6 primary offline documentation at the exact local paths in the ledger. GetAllRegisteredPackages observes cached currently loaded packages; it is not a scan of arbitrary Assets package.json files. The project manifest drives dependency loading, and automatic embedded-package discovery is under the project's Packages folder. Thus the absence of a dependency is combined with an actual empty-of-package Packages directory, rather than assuming that every package needs a dependency entry.

Scoped source searches covered com.neoxider.tools and PackageManager/Client.Add paths across the project Assets, Packages, scripts and Tools sources. No bundled Neo self-registration path was found. The matching Neo sources are package/path readers. In particular `NeoxiderModulePackageInfoUtility.TryGetForAssembly` at 61–88 explicitly treats UPM resolution as the preferred path and falls back to reading package.json for Assets modules where PackageInfo is unavailable. NeoUpdateChecker's known Assets/Packages paths are metadata/repository readers, not registration. This does not claim that an arbitrary external user plugin cannot register a package; the specified fresh frozen profile contains no such Neo registration step.

No SDK/native PackageInfo call was made. Registered-package cache state itself still requires later actual host evidence. The deterministic source failure is the legitimate unregistered branch of this required Assets profile.

## Smallest bounded correction

Keep the public executeMethod/CLI, owned-host guards, graph/loaded-DLL capture, real SDK trees, deadline/reload state and failure semantics unchanged. Within the current driver:

1. Capture and preserve the **actual** registered-package list. If a registered com.neoxider.tools exists, use its native identity/path/source and continue requiring version 11.0.0. A registered wrong version must fail even if a valid Assets manifest also exists.
2. Only when no registered Neo exists, read the exact project Assets/Neoxider/package.json. Require a real readable manifest with exact name com.neoxider.tools and version 11.0.0; missing/malformed/wrong name/wrong version fails. Do not search another arbitrary package root or use an assumed version string.
3. Record the selected summary using an explicit **ProjectAssets** provenance kind/source and the canonical actual Assets root, with manifest origin distinguishable from UPM. Existing receipt packageName/packageVersion/packageSource/packageResolvedPath can carry this small distinction; any additive manifest-origin field remains private receipt data. Do not append a fabricated PackageInfo or Neo entry to the native registered list, or label Assets as Embedded/Local.
4. Keep `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs:119`–127 **unchanged**: ImportPackageSamples still requires actual Local/Embedded registration, explicit matching frozen package root, real version and all three manifest samples. ProjectAssets discovery provenance is not UPM import evidence.

No always-embed change to the Assets profile, surrogate driver, removed Mirror reference, fake graph/list or new package/session framework is justified. The release-consumer profile at recipe 91 continues to embed the real package and prove actual UPM registration/import independently.

Update the primary owned Testing guide/receipt to state both provenance paths without conflating them. The operator can keep the exact current entrypoint and truthful profile layouts.

## Meaningful authored acceptance recommendation — none authored/executed here

The current SampleEvidence fixture already has internal manifest DTOs and helper-oriented tests, but its ten sample-identity/native-scene cases do not exercise discovery provenance selection. A small internal selection/validation helper used by the actual driver is practical; supply already-observed native package records and an explicit project root for owned temporary-manifest tests. Preserve real SDK graph/tree calls outside that seam; do not substitute a synthetic discovery result.

Minimum useful cases:

- No registered Neo + real owned valid Assets/Neoxider/package.json selects ProjectAssets with exact observed name/version/root; the registered input/output list remains unchanged and contains no fabricated Neo.
- Registered Neo 11.0.0 metadata selects its recorded native source/root; ProjectAssets is not used. A registered wrong version with an otherwise valid Assets manifest fails rather than falling back.
- Unregistered Neo with missing, malformed, wrong-name, missing/wrong-version Assets manifest fails before success. Include an unrelated registered-package positive while valid Assets provenance is selected.
- Keep the real UPM importer unregistered rejection and registered Local/Embedded positive contract. Later real Assets and release-consumer hosts must each provide successful fresh compilation plus nonempty actual Edit AND Play SDK trees and their distinct provenance. Pure resolver controls alone cannot prove those native outcomes.

Use the primary owner's existing unique test prefix/fixture ownership and exact temporary-file cleanup; no unavailable NUnit serial attribute or shared asmdef change is needed. These are requested acceptance sources, not a claim that tests were added or passed.

## NI24 constraint and disposition

NI24 correctly documented the then-current registered 11.0.0 precondition at its line 68 and the actual SDK/Editor-graph boundaries. It did **not** catch the incompatibility with the planned Assets profile. This new evidence corrects that missed profile-admission conclusion; NI24 remains an immutable historical source review. Its no-fake-Mirror-free-driver, actual current package/dependency observations, real nonempty trees, strict isolated host and unexecuted-runtime limits remain binding.

Root received the confirmed finding early with the installed primary-doc/source evidence. Return only this bounded driver/owned-test/Testing-receipt correction to SAME `neo-audit-1023-b03-04`, then independently inspect that source-final delta. Do not reopen unrelated module audits or execute before every source writer finishes and root freezes. There is no native registration/discovery/compile pass in this report.

## Read identities and coverage

Initial gate **2026-10-23T22:17:08.312Z**, closing gate **2026-10-23T22:19:20.099Z**: all **16/16 identities matched**. Discovery/Paths/main host/SampleEvidence retain their NI24 full-read hashes; this pass read the relevant current DTO, public entry/update, package capture, fallback/import and fixture seam bodies. Manifest JSONs and local primary documentation were read; the lock was token-searched and Packages directory enumerated. Editor utilities are limited registration/path-reader context, not a new full module audit. The previous request/Testing/recipe/NI24 constraints were read in the relevant sections. Raw hashes are source bookkeeping only.

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.Discovery.cs` | 22528 | `80aff970b750b597b63834f4bee5b9d492e84ac34887e7fecc8b1f59144cff78` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsPaths.cs` | 7339 | `586593af17694e6bfe3cb922392139aab830602e57c6f05d501c629a0f1188eb` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsValidationHost.cs` | 24412 | `1c8e5d91c76f6c88b9d06ae589eb651a8bd089d214c766f18d77c84e117dc207` |
| `D:/Git/NeoxiderTools/Packages/manifest.json` | 2597 | `470a73e9fb0a59fb3a24c83882a66fb94e69c34239a07fbb8f4b8f39687fab53` |
| `D:/Git/NeoxiderTools/Packages/packages-lock.json` | 15427 | `9c4e3fa01f898522f1fa7daf6ccc14818d0c3ca3ea289b9f5450f55abe690144` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/package.json` | 2055 | `ef704423999fffb8e70b42475cdbff47bf88f3edeec66aacf7ffcb19546c5576` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/UnityVerification.execution-plan.md` | 39600 | `2f0da2a2a24c5ff239f36f43570c43cb70fbfd1c22f18c5b3ec11b8b584ccb44` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-24.md` | 26302 | `4d6b5f0e27964b6bc41fdeba7406e115e5c7a35eb4e5f4c4e496e20bfb10023c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Testing.md` | 14947 | `2a63d7106c6c0f5302812041aa4a8d82022a88295c39ca8c8623abda09896ef2` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Editor.Tests.implementation-request.md` | 19376 | `f33f526f83dc2f78d9b89bb3c45e63e67135df856cd5c987ae7c33bc5766083a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Editor/PropertyAttribute/NeoxiderModulePackageInfo.cs` | 9293 | `9a064e5346c57e718685fada64a417654df5fdea64911b989e27bb9e4f959643` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Editor/PropertyAttribute/NeoUpdateChecker.cs` | 20924 | `0df7a1320a3332d6a4ca86bc16d19c4689c4d67fad6173e7601716fb0beb4cfc` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoEditorTestsSampleEvidenceTests.cs` | 9592 | `50a2b0242b0e48123d6c5b2e20d3b73d72bbfcba3e80efc0d0166e483e879aa7` |
| `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Documentation/en/ScriptReference/PackageManager.PackageInfo.GetAllRegisteredPackages.html` | 18195 | `18f66b4e1d3e629dc0f89317859c48651715293d9925e90014cd76d33337d39d` |
| `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Documentation/en/Manual/upm-embed.html` | 15766 | `fe8a2a79e8e4065b6225d69c1a7416318be696625aa9e0f79c77b7dd4625ddb9` |
| `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Documentation/en/Manual/upm-manifestPrj.html` | 23315 | `4d7862e1bd55a1fb46eed83820980fa1d03027ad07ed5aed0293f922bdf77021` |
