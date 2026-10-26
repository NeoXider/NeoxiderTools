# Root acceptance for HTML → UITK and release 11.0.0

Status: source and functional verification accepted; final source commit/tag/publication follows this receipt. Date:2026-10-27 (raw receipts use UTC).

The same native6.1-sol/max developer/reviewers completed the bounded module and packaging reviews. Root read their final changes and retained all actual failures. Output/CSS/asset collision loss, projection reentry/same-root/fault cleanup, localization and portable pure-logic globs are corrected. No new public/serialized/JSON API or TFM was introduced.

## Actual current execution

- Fresh converter solution:2573 Passed /30 NotExecuted /0 Failed; actual installed6.6 capabilities199/199. The30 are7validate-only+22open goldens+1S08 diagnostic debt, not passes.
- Separate .NET pure logic:291/291. SourceNET9 target used actual runtime10.0.9 with process-local Major;NET8 used8.0.21.
- Complete native HTML assembly:370/370/0skip eachSDK6000.3.14f1 and6000.6.0f1, including actual published external CLI conversion3runs, ownership import/7PNG/nativeTheme/PlayUIDocument/red→blue pixels/navigation/modal/tabs/actions/disable-enable/tree replacement.
- Separate actualOS mouse pass in6.3 at1280×720:Settings1,Video,Back,modalCancel,Profile replacement,Save1; no screen/console error. This is separate from programmatic native events.
- Published framework-dependent CLI and the extracted final CLIzip ran outside the repository on.NET10 without RollForward ENV;2UXML+7PNG. Runtimeconfig LatestMajor; no runtime redistrib.
- Native AssetDatabase.ExportPackage Default produced exact-path Core2066 andComplete2639 entries. Streaming tar inspection validated every pathname/GUID/body hash and excluded external vendors/tests/Pro.
- Actual AssetDatabase.ImportPackage imported final Core andComplete on BOTH SDKs without Pro. All4 terminal jobs exit0 with zero GUID/body/loaded-production-assembly/prefab/serialized-reference problems. Core-only preconditions restored between Complete/Core runs; old precondition failures retained.
- Repository package validation:5866 files/71assemblies/3197GUIDs/0errors. This repository profile is distinct from Core-only archive acceptance.

## Packaging corrections and exact evidence

Core initially exposed25 references to sample artwork. Only in isolated staging these9CMFanimationFBX+3CMFtextures+11DemoPNG+2DemoMAT moved toSharedAssets, keeping GUID,body andfullimporter.meta bytes (includingFBXlocal-IDtables). MainSamples~locations remain. Unreferenced empty folders were omitted from export rather than relaxing real-asset GUID checks.

Native Complete import exposed two existing material faults. Native6.3 authoring removed an inactive particle_Mask after HasProperty provedfalse and MainTex/Tint stayedexact; missing optionalShine shader was replaced by builtinUI/Default. The unsupported proprietary glint is explicitly not supplied. Both material GUID metas remain. One newly serialized emptyYAML trailing space was normalized in source only; nativearchives retain standardUnityserialization with identical values.

Raw evidence is underLocal/Audits/release-11-2026-10-27:round3/native,round2/results,legacy-import,archive-inspection.json,shared-dependency-identity.json,sample-material-copyback.json,material-yaml-style-normalization.json,cli-zip-acceptance.json andfinal-acceptance.json. Canonical package evidence is Docs/UI/HtmlToUitkVerification11.md plus Verification11.md. Source is preserved before each correction; wrong NUnit4 CollectionAssert, nonexistentNavMode, duplicatelegacyCMF host import and startup-before-import orchestration failures are not reported as product passes.

## Release scope and limits

Core declares samples:[];Complete declares3visibleSamples paths. SourceUPM retainsSamples~. Both keep external prerequisites separate; sourcearchiveexport-ignore excludesPro andits examples/readme, while UniTask's verbatimMITnotice is preserved. New release assets carry their SHA256SUMS and source/input manifest. Final ref identity is recorded by Git/GitHub, not an invented self-referencing commit hash in this file.

PackageintegrityCI passed. HostedUnityCI lacksGameCIsecrets and is not reported as passed. No certification is claimed for everyUnity6, all resolutions, fullCSS/JS/browser parity, mobile/IL2CPP/consoles, everymanualscene or performancebudgets. Old typed-DLL csprojs remain machine-specific and authoredRT-background blur limitations remain.

Only authorized source/docs/assets are staged; userDocsEn.meta and._count are preserved/excluded. Commit owner must be Neoxider<xotrios@gmail.com>, noCo-Authored-By/generated signatures. User authorized normal demo/main integration and v11.0.0 release, with no tag/history force overwrite.
