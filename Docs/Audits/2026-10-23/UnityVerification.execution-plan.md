# Frozen-source Unity execution recipe

Owner: SAME `/root/verify_unity`, native `gpt-6.1-sol` / `max`. Prepared 2026-10-24 local date. **PLAN ONLY. `executionAllowed=false`, `sourceFreeze=false`; no execution is authorized by this document.** This preparation read files/installed executable metadata and wrote only this document. No Unity/MCP, compiler, tests/checkers, player/launcher, installs/services, project copies/layout changes or child agents were started.

This makes [UnityVerification.plan.md](UnityVerification.plan.md) concrete. Historical preliminary hosts/logs remain unchanged. Current helper interfaces below are preparation evidence, not a frozen or executed acceptance claim. The final real-peer harness includes the Counter malformed-payload extension and awaits NI26 independent source approval. The real discovery/compiled-graph addition is SOURCE-FINAL and independently source-reviewed in NI24, fully read by root. Its exact supported entrypoint is recorded below. No execution has begun.

## 1. Actual installed inputs; no capability pass inferred

Read-only Windows file metadata:

| Editor executable | ProductVersion | FileVersion | Bytes |
|---|---|---|---:|
| `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe` | `6000.6.0f1_f7f8ed4d1e24` | `6000.6.0.16251117` | 279976 |
| `C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Unity.exe` | `6000.3.14f1_d68c3f99a318` | `6000.3.14.14060607` | 282544 |

Both have `Editor/Data/PlaybackEngines/windowsstandalonesupport`, its Windows Editor extension/build program, and `Variations/win64_player_development_mono` plus IL2CPP player payload directories. This establishes files present, not successful builds, license readiness, installed C++ tooling, graphics/audio/device availability or an IL2CPP/AOT/server-subtarget pass. The supplied peer helper builds ordinary StandaloneWindows64 development players; its server is a dedicated Mirror role in a headless player, not evidence for Unity's separate Server build subtarget.

Current source project is Unity6000.3.14f1; package metadata is11.0.0/release `Samples~`. Main company/product are `DefaultCompany` / `NeoxiderTools`; never use those for validation. Main input mode is Both (`activeInputHandler:2`), and main Editor settings currently enable enter-play options3. Preserve main settings and record the frozen values again at authorization.

## 2. Mandatory source-freeze guard

Root must explicitly authorize this SAME operator after every runtime/editor/test/harness/docs/config/root writer and all required corrections have finished. A SOURCE-FINAL module, old `globalReviewApproved=true`, or completion of this recipe is insufficient. In particular require final Counter peer extension/native review, Editor.Tests discovery/graph addition, NI18-01 wording, all remaining source-owner closures and root's final metadata/integration work.

The existing authoritative state is `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/implementation-state.json`; it currently says execution=false/freeze=false/root running and contains historical `source-running` rows. Root must reconcile it. For a concrete machine guard, root should supply/approve an immutable verification request at `Local/Audits/assembly-2026-10-23/unity/source-final-verification-request.json` (or explicitly identify another exact path). This file does not exist by implication and the operator must not manufacture authorization. Required request fields: `executionAllowed:true`, `allSourceWritersStopped:true`, `rootSourceFinal:true`, `targetVersion:"11.0.0"`, `branch:"demo"`, `head`, absolute `sourceManifestPath`, its64-hex `sourceManifestSha256`, and zero `pendingRequiredSourceCorrections`. Root binds its reviewed receipt/native-review closure map to that manifest. Any different actual marker schema must be reconciled with root before adapting this guard.

After explicit human/root authorization, the first shell block is a guard, not a Unity invocation:

```powershell
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = 'D:\Git\NeoxiderTools'
$AuditRoot = Join-Path $Repo 'Local\Audits\assembly-2026-10-23\unity'
$RequestPath = Join-Path $AuditRoot 'source-final-verification-request.json'
function Assert-VerificationGate {
    $state = Get-Content -LiteralPath (Join-Path $Repo 'Docs\Audits\2026-10-23\implementation-state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $request = Get-Content -LiteralPath $RequestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($state.sourceFreeze -ne $true -or $state.executionAllowed -ne $true -or $state.rootSourceWriterState -eq 'running' -or
        $request.executionAllowed -ne $true -or $request.allSourceWritersStopped -ne $true -or
        $request.rootSourceFinal -ne $true -or $request.pendingRequiredSourceCorrections -ne 0 -or
        $request.targetVersion -cne '11.0.0' -or $request.branch -cne 'demo') { throw 'Source/execution gate is closed.' }
    if (@($state.owners | Where-Object { $_.state -match '^source-(running|queued|recovering)$' -or $_.needsFollowup -eq $true }).Count -ne 0) {
        throw 'Owner source-state/required-followup rows are not reconciled.'
    }
    $actualHead = git -C $Repo rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read the authorized repository HEAD.' }
    $actualBranch = git -C $Repo branch --show-current
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read the authorized repository branch.' }
    if ($actualHead.Trim() -cne $request.head -or $actualBranch.Trim() -cne $request.branch) { throw 'Authorized branch/HEAD changed.' }
    if ((Get-FileHash -LiteralPath $request.sourceManifestPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne
        $request.sourceManifestSha256.ToLowerInvariant()) { throw 'Authorized source manifest changed.' }
    return $request
}
$Request = Assert-VerificationGate
$SnapshotSha = $Request.sourceManifestSha256.ToLowerInvariant()
$SourceManifest = Get-Content -LiteralPath $Request.sourceManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$FrozenSource = [System.IO.Path]::GetFullPath([string]$Request.sourceRoot)
if ($FrozenSource -cne [System.IO.Path]::GetFullPath([string]$SourceManifest.sourceRoot) -or
    -not (Test-Path -LiteralPath $FrozenSource -PathType Container)) { throw 'Requested frozen source is missing or differs from its manifest.' }
$SnapshotRoot = [System.IO.Path]::GetDirectoryName($FrozenSource)
# Verify ordinary workspace containment and every manifest payload hash before copying/launch.
# This consumes the captured request sourceRoot; it never recaptures a moving checkout.
```

Recheck this gate and the exact authorized source/host input manifest before every launch. Record current full HEAD, branch, dirty/new/deleted paths, capture UTC and every input's relative path/length/raw SHA256. Compare source bytes with the approved manifest before copy, hash the copied ordinary files, and repeat the source read after capture. Abort on drift, missing entries, reparse-point escapes, unknown files used as inputs, or a changed receipt/CLI. Do not treat a bare Git HEAD as the dirty worktree's identity.

Capture immutable input at `.../unity/snapshots/final-<UTC>-<SnapshotSha12>/source/`, with its exact manifest beside it. All future hosts use this input, never a moving main checkout or preliminary host. Do not share mutable files/Library by links or junctions. Preserve the frozen input and historical evidence. Changes requested by a failing run go to root/SAME original owner, close execution while writers work, and require a new freeze/snapshot for affected verification.

Root prepared `Local/Audits/assembly-2026-10-23/prepare_final_snapshot.py` but has NOT invoked it. It requires a separate explicit root-source-final intent, fresh empty exact CLI liveness, all 72 current final receipts personally read, and current required native reports recorded as fully read. After successful capture it publishes the exact request/manifest/sourceRoot and reconciles the authoritative state. If that request supplies an already captured sourceRoot, use and verify those ordinary frozen files; do not capture a second moving checkout. Its manifest labels product inputs separately from copied `Docs/Audits` review context. Product-role main files must remain unchanged; subsequent orchestration state/result-report changes are recorded context updates and are never substituted for compiled inputs. Main public package docs, code, tests, assets and configuration remain product inputs. The user-owned exclusion remains exact.

## 3. Paths, profiles and preparation before first launch

Choose one new32-hex `$RunId = [guid]::NewGuid().ToString('N')` after authorization. `$FinalRoot = "$AuditRoot\final\$RunId"`. Use independent projects:

| Editor directory | Profile directory | Product |
|---|---|---|
| `u6000_6_0f1` and `u6000_3_14f1` | `mirror-on-dev` | `NeoTools_<editor>_mirror_on_dev` |
| same | `release-consumer` | `NeoTools_<editor>_release_consumer` |
| same | `mirror-absent` | `NeoTools_<editor>_mirror_absent` |
| same, when needed | `input-legacy`, `input-new`, `input-both`, `reload-normal`, `reload-fast`, `native-gui` | `NeoTools_<editor>_<profile_with_underscores>` |

Every host company is `NeoQA_20261023_<RunId>`; the complete company/product pair is unique. Assign real variables from the table; angle-bracket text is a recipe substitution, never a literal launch argument. Each project's `Receipts/` contains logs/XML/job metadata, satisfying the helper's inside-project output guard. Store player builds/run roots under `$FinalRoot\peer-builds\<editor>\<PeerRunId>` and `$FinalRoot\peer-runs\<editor>\<PeerRunId>`; those leaf outputs must not exist before their respective build/launch.

Only after the gate opens:

1. Recheck free disk against all planned source copies, independent Libraries, package resolution and three player builds per Editor; reserve capacity for logs and native captures. Recheck licensing and current Editor ownership then. Never control/stop VampireDota or another user project. Only the task's own returned process identities may be controlled. Serialize Editor jobs.
2. Copy ordinary files from the immutable manifest: `Assets`, `Packages`, `ProjectSettings`, root `README.md`, `AGENTS.md`, required root `Images`/guides, `Docs`, `scripts`, and any frozen .NET support inputs actually used by tests. Exclude `.git`, `Local`, `Library`, `Temp`, `Logs`, `UserSettings`, `obj`, `bin`, caches, generated IDE projects and previous outputs. Preserve required authored/vendor payloads; do not accidentally exclude a declared DLL dependency as a generic build output.
3. The user-owned file called out as `UserDocsEn.meta` is exactly `Assets/Neoxider/DocsEn.meta` in this checkout. Record its main raw hash/size, exclude that exact file from snapshot/host copying, and leave main bytes untouched. Do not create DocsEn or copy unrelated user data. Record every explicit excluded source path; a source manifest from root must encode this exclusion.
4. Before the FIRST Unity launch, edit only each host's `ProjectSettings/ProjectSettings.asset`: unique company/product and `UNITY_INCLUDE_TESTS` in the Standalone test-profile defines. Retain the frozen on-profile MIRROR symbols. Verify single exact YAML fields and log before/after bytes/values in a transformation ledger. No new `-persistentDataPath` or similar override exists. Effective roots are checked in Unity below.
5. For `mirror-on-dev`, rename only the isolated `Assets/Neoxider/Samples~` to `Samples`, with its matching folder metadata if present, and transform only that host package's three sample paths to `Samples/...`. Hash/remap this transformation. Frozen/main release layout remains untouched.
6. Before activation/import, handle the known CMF imported duplicate only in this host. Canonical runtime is `Assets/Neoxider/ThirdParty/CharacterMovementFundamentals/Scripts/CMF.asmdef`; retain it. The seven ShowcaseScripts pairs in `Assets/Neoxider/Samples~/CharacterMovementFundamentals` and old `Assets/Samples/CharacterMovementFundamentals` currently have identical code AND meta bytes: AlignRigidbodyToTarget, DemoMenu, DisableShadows, FPSCounter, MouseCursorLock, PlayerData, RotateObject. Recompare frozen full bytes/metas/GUID references. Quarantine only the redundant imported script/meta pairs outside Assets, under this SAME host's `VerificationExcluded/CMFImported/`; retain scenes/art and canonical active scripts. Every move requires resolved source/destination within this host, a reversible exact mapping and hashes. Any divergence, changed GUID ownership or missing reference goes to the sample owner; no arbitrary class deletion.
7. For `release-consumer`, use clean host Assets with the exact frozen dependency/vendor payloads required by the integrated graph (Mirror and existing Plugins, explicitly inventoried; no arbitrary main scene/resource copies to repair packaging). Embed the exact frozen `Assets/Neoxider` package as `Packages/com.neoxider.tools`, keep its `Samples~`/manifest unchanged, add `testables:["com.neoxider.tools"]` to the host manifest and ensure NO second `Assets/Neoxider` copy or old imported Neoxider/CMF samples exists. Keep required root test guides/images. Native registration must actually report Embedded11.0.0 at that exact package root; folder copying alone is not the UPM sample-import result.
8. For true `mirror-absent`, omit external `Assets/Mirror` plus its meta/CompilerSymbols initializer and any real Mirror package dependency from the host; remove MIRROR and MIRROR_* tokens from the host Standalone defines. Keep ALL Neo product/test asmdefs/sources intact. Do not copy the environment/discovery driver into a surrogate assembly or remove inconvenient consumers. Record the actual absent graph, not merely a false symbol. Current Neo.Network/Shop/Rpg.Network/test asmdefs have hard Mirror references, so an import/dependency failure is a real possible outcome and blocks fallback acceptance. Return it to root rather than manufacture a pass.
9. Resolve only frozen declared dependencies. The current root manifest/lock already disagree on some Unity package versions (for example URP17.4.0 versus lock17.3.0); preserve input and record actual host resolution/migration instead of silently choosing another version. Exact optional MCP git lock is `c21bf496bca87d54e75bad048563c3adb1782081`. Existing root authorization permits omitting only its unused development entry/lock in an isolate when the documented two serious exact-cache restoration attempts remain externally TLS-blocked; recheck no direct Neo dependency and record the omission. Main Packages stay untouched. No installs/global package choices.

After every preparation operation, hash source/config/metas against the host transformation ledger. Do not use recursive delete/move without checking resolved absolute containment. A release consumer never activates package Samples; its only visible sample scripts come from real imports.

## 4. Launch convention and ordered import/environment jobs

Every command below runs hidden, with a captured job receipt: snapshot SHA, profile/host path, exact executable/version/arguments, start UTC/PID/start identity, deadline, exit, log/artifact hashes and all derived host transformations. Raw helper JSON/XML is retained; attach snapshot identity through its hashed outer job receipt because the existing environment helper itself has no snapshot field. No command is evaluated while preparing this MD.

Use PowerShell argument quoting, not a shell-string `cmd /c` composition. A reusable later launch form is:

```powershell
function Quote-UnityArgument([string]$value) {
    if ($value.Contains('"') -or $value.Contains("`r") -or $value.Contains("`n") -or $value.EndsWith('\')) { throw 'Unsupported argument.' }
    return '"' + $value + '"'
}
$Unity66 = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$Unity63 = 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe'
$HostPath = Join-Path $FinalRoot 'u6000_6_0f1\mirror-on-dev'
$Company = 'NeoQA_20261023_' + $RunId
$Product = 'NeoTools_u6000_6_0f1_mirror_on_dev'
$EnvironmentMethod = 'Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ProbeEnvironment'
$Common = @('-projectPath', $HostPath, '-buildTarget', 'Win64')
$Identity = @('-neoValidationCompany', $Company, '-neoValidationProduct', $Product)
$Args = @('-batchmode', '-nographics') + $Common + @('-executeMethod', $EnvironmentMethod) + $Identity +
    @('-neoValidationOutput', "$HostPath\Receipts\00-environment.json", '-logFile', "$HostPath\Receipts\00-import-environment.log")
$null = Assert-VerificationGate
$OwnedEditor = Start-Process -FilePath $Unity66 -ArgumentList (($Args | ForEach-Object { Quote-UnityArgument $_ }) -join ' ') -WorkingDirectory $HostPath -WindowStyle Hidden -PassThru
```

Create `Receipts/` before launch and capture the returned identity immediately. Poll bounded jobs in intervals below60seconds so user updates remain possible. Initial import/environment deadline30minutes; later unchanged warm-import helper jobs15minutes. Record timeout/crash independently. Stop only the exact owned process/descendants after graceful shutdown/deadline handling; never terminate by executable name or interfere with shared/user licensing/UPM processes. Preserve first failure and cleanup failure separately.

**Order:** mirror-on-dev6.6 first import/environment, then6.3 import/environment. These are the FIRST Unity jobs: no test/save-mutating suite precedes them. Compilation/package import necessarily precedes executeMethod; company/product isolation is already in place. Do not insert a preliminary test run to make the helper callable. The explicit test-profile define is set before launch. The helper exits batch mode itself; omit `-quit` here.

Require exit0, zero compile/Weaver errors and `environment.status=="complete"`. Check actual project/company/product/Unity version/WindowsEditor, provider type/effective roots, actual persistentDataPath suffix, observed HKCU branch/value encoding, `sentinelRoundTrip=true`, `sentinelRemoved=true`. The expected namespaces are candidates until the helper proves the effective one: Editor `HKCU\Software\Unity\UnityEditor\<company>\<product>`, player `HKCU\Software\<company>\<product>`. The helper round-trips and removes only its GUID sentinel. Unknown provider/outside-root/redirection, missing receipt or namespace overlap stops persistence-dependent execution. No DeleteAll/root-registry cleanup.

## 5. Required discovery/compiled graph gate, then critical and full suites

**Exact CLI now published and source-reviewed:** SAME primary Editor.Tests owner added `Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests` in the guarded owned partial. NI24 independently checked the installed UTF1.6 public instance `RetrieveTestList(TestMode, Action<ITestAdaptor>)` API and actual native Editor graph/loaded DLL/package observations. No `-listTests` flag is supported; a test-run exit0 can also mean zero tests.

Before any suite: reread/hash the final source/receipt and NI24, then run this separate async command on both imported hosts after environment proof. Require complete/exit0, nonempty Edit AND Play inventories, named per-assembly compiler defines/reference/DLL identities and corresponding source/profile identity. It must use the same real host; a broken Mirror-absent graph cannot be disguised with another driver assembly. Discovery itself attests the Editor graph/current trees and does not trigger or prove fresh compilation; pair it with the successful frozen import/compile log and source transformation ledger.

```powershell
$DiscoveryArgs = @('-batchmode') + $Common + @('-executeMethod',
    'Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.DiscoverCompiledTests') + $Identity +
    @('-neoValidationOutput', "$HostPath\Receipts\01-discovery.json", '-logFile', "$HostPath\Receipts\01-discovery.log")
```

Launch through the guarded hidden/owned convention. Omit `-quit`, `-runTests` and `-listTests`. The helper requires batch Editor outside Play and no pending sample import, uses its ten-minute monotonic deadline and terminal exit, and restarts both trees/graph after reload without extending the original deadline.

Discovery acceptance records all cases by assembly/fixture/full name, total discovered/selected, runnable/ignored/explicit/inconclusive reasons and conditional MIRROR/fallback cases. Require the expected new critical cases to exist before selecting them. Inventory all package tests, including `Neo.Editor.Tests`, `Neo.HtmlToUitk.Tests.Editor`, `Neo.Tests.Play` and any actual legacy/sample test assemblies; the integration peer assembly has no NUnit cases. Record actual resolved UTF/NUnit/UniTask/DOTween/Mirror/transport identities. `MIRROR` source grep is not a compiler receipt; vendored Assets/Mirror/CompilerSymbols currently adds it automatically on import, and package versionDefines coexist with global symbols.

Before full Edit tests, build the existing HtmlToUitk process probe from an ordinary task-owned copy of its frozen `.csproj`/`.cs` under `$FinalRoot\support\html-process-probe\src` (or consume root's matching build receipt). Do NOT build inside the immutable snapshot. It targets net9.0 and uses no PackageReference; use the existing installed SDK, no install. The later exact build is:

```powershell
dotnet build "$FinalRoot\support\html-process-probe\src\Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj" --configuration Release
$env:NEO_HTMLTOUITK_PROCESS_PROBE = "$FinalRoot\support\html-process-probe\src\bin\Audit20261023NeoHtmlToUitkEditorProcessProbe\net9.0\Audit20261023NeoHtmlToUitkEditorProcessProbe.dll"
```

Capture build exit/SDK/artifact/source hashes before launching Unity children with that explicit environment. Missing SDK/probe is a named process-test capability gap, not an acceptable hidden skip. Root's broader .NET/checker suite remains separately owned; link its final frozen-input receipts rather than claim Unity ran it.

For each Editor, run critical Edit then critical Play first, using the final acceptance map's exact names and checking every filter clause matched. Initial source-known critical filters include `UpgradeStat_DebitObserverReplacementOwnsItsInvestmentsAndProfile`, `Spend_ZeroStoredDebitRejects`, `Binding_SourceDestroyedByEarlierListenerSuppressesCapturedTailAndCanRebind`, `UnitWrappers_OnlyMatchingTargetAndGlobalsEnterObserverSnapshots`, real Shop save-fault/postcommit cases, Cards ownership/lifecycle, Input replacement cancellation and `ActiveServer_AutoLoadPublishesCompleteSnapshotWithoutReplayingCurrentGrant`. Freeze-time discovery supplies exact complete names/changed additions. Store separate critical XML/logs; never infer selection from the regex text.

Then run FULL Edit and FULL Play on6.6, followed by FULL Edit and FULL Play on6.3. Use fresh process jobs on their corresponding same proven host, no simultaneous Editor/test-state jobs. The concrete full-job argument suffixes are:

```powershell
# $Common and $HostPath are rebound to the exact proven Editor/profile for each job.
$EditArgs = @('-batchmode', '-nographics') + $Common + @('-runTests', '-testPlatform', 'EditMode',
    '-testResults', "$HostPath\Receipts\20-full-edit.xml", '-logFile', "$HostPath\Receipts\20-full-edit.log")
$PlayArgs = @('-batchmode', '-nographics') + $Common + @('-runTests', '-testPlatform', 'PlayMode',
    '-testResults', "$HostPath\Receipts\21-full-play.xml", '-logFile', "$HostPath\Receipts\21-full-play.log")
```

Launch each through the same hidden/owned/guarded convention. Omit `-quit` and `-runSynchronously`; UTF warns that quit breaks command-line tests, and synchronous execution would lose coroutine/lifecycle acceptance. Do not apply a full-job testFilter/assemblyNames restriction that omits another discovered package assembly. Default full-job deadline60minutes, adjusted once from actual discovery/fixture bounds before launch and recorded; no indefinite scaled wait. EnterPlayMode/ExitPlayMode fixtures must settle/exit before the next job. No NUnit shim/NonParallelizable or operator source patch.

Pass requires both process outcome AND parsed raw NUnit XML: discovered/selected/executed nonzero, every required mapped case actually executed, zero failed/error/teardown faults, and explicit per-case `result`/`label`/reason accounting. Keep totals for passed, failed, skipped/ignored, explicit, inconclusive, unsupported and timeouts. Compare the XML full-name set with the discovery inventory; a vanished fixture, all-skipped feature or zero-test exit0 blocks that contract. Expected optional/capability skips retain an unverified disposition and route to later native jobs below. Console import/compile/Weaver/runtime errors remain failures even when another summary is green.

## 6. Release consumer: actual UPM registration/import, then native sample cases

Perform independently on6.6 then6.3 with their release-consumer host identity. First run the same ProbeEnvironment command (new host/product/output), then discovery/compiled graph once available. Require registered Embedded11.0.0 and exact root before import. Explicitly rebind the profile rather than reusing the previous host variables:

```powershell
$EditorKey = 'u6000_6_0f1' # then u6000_3_14f1 with its Editor executable
$HostPath = Join-Path $FinalRoot "$EditorKey\release-consumer"
$Product = 'NeoTools_' + $EditorKey + '_release_consumer'
$Common = @('-projectPath', $HostPath, '-buildTarget', 'Win64')
$Identity = @('-neoValidationCompany', $Company, '-neoValidationProduct', $Product)
$PackageRoot = "$HostPath\Packages\com.neoxider.tools"
$ImportArgs = @('-batchmode', '-nographics') + $Common + @('-executeMethod',
    'Neo.Editor.Tests.Audit20261023NeoEditorTestsValidationHost.ImportPackageSamples') + $Identity +
    @('-neoValidationPackageRoot', $PackageRoot, '-neoValidationOutput', "$HostPath\Receipts\30-upm-import.json",
      '-logFile', "$HostPath\Receipts\30-upm-import.log")
```

No `-quit`; the existing SessionState queue survives domain reload and owns a600second Editor-time timeout/terminal exit. External job bound15minutes. Require statuscomplete/exit0 after native Sample.FindByPackage and Sample.Import(None), exact three identities and fresh destinations:

| Manifest identity | Imported path |
|---|---|
| Demo Scenes | `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes` |
| NeoxiderPages | `Assets/Samples/NeoxiderTools/11.0.0/NeoxiderPages` |
| Character Movement Fundamentals | `Assets/Samples/NeoxiderTools/11.0.0/Character Movement Fundamentals` |

Check actual package source/resolved path/version, importCalled and native imported-state observations, nonempty script/asset/GUID/reference records and native prefab missing-script checks. `importReturnObserved=false` is disclosed if domain reload interrupted the call; imported state/assets still must be verified. `status=importing`, folder copies or old assets are insufficient. After import/compilation, rerun graph/discovery and the actual native SampleAssetValidationTests, SampleSceneCoverageTests, RpgDemo, relevant NeoxiderPages/imported sample scenarios from the frozen acceptance map (critical prefix filters then unrestricted sample suite selection). Native cases in this active imported profile must reach their native assertions. Hidden YAML packaging is a distinct successful/nonempty coverage kind; native Ignore from a hidden-only profile never substitutes for import/load/Playable evidence. Select one current root per manifest identity, not a count of duplicate roots.

Release-consumer dependency/reference failures return to root; do not copy arbitrary main assets or activate package Samples to conceal them. Retain imported root/GUID/reference/scene lifecycle receipts and post-import source/host hashes.

## 7. True Mirror absence and additional native profiles

After integrated jobs, attempt the mirror-absent hosts on6.6 and6.3 with the same first environment/import command. If unresolved hard references prevent compilation/helper availability, preserve exact graph/log diagnostics and mark Mirror-absent compile/discovery BLOCKED/FAILED for that snapshot. Do not remove Neo asmdefs, tests, integration sources or callbacks. If the final approved source genuinely supports absence, require no external Mirror DLLs, no auto-installer, actual compiled symbols/references absent and nonempty applicable fallback discovery/execution through the SAME real driver; network case counts intentionally differ. A present-dependency/disabled-symbol experiment would be separately named and cannot satisfy absence.

Run the existing query/native counters as their own named receipts on both integrated hosts: Edit `Audit20261023NeoToolsInteractableObjectSharedQueryTests`, including complete native17/64/1000 buffers, planar original-argument/filter parity, independent LOS and invalidation; Play filter `PlayerLoop_(Camera|KeyboardLook)(3D|2D)_(1|100|1000)$` must select all12 public PlayerLoop cases in `Neo.Tests.Play.Audit20261023NeoToolsInteractableObjectLifecycleTests`. These assert nativeCalls1/cacheHitsN−1 plus live target state in a warmed stable epoch; preserve retry/growth/fallback/callback/LOS counters and classification work separately. Ability target-routing case has1/100/1000 parameterizations with real invocation/snapshot counters. No claimed CPU/allocation speedup follows from a query-count assertion. Actual Profiler captures/CPU/allocation/retained-context measurements require the existing instrumented native session and measured scope; missing capture/automation is recorded unverified, with no operator test harness invented.

Create fresh isolated input variants with activeInputHandler0/1/2 BEFORE first launch and unique products; environment/graph proof precedes focused Input/MouseEffect/selector/interaction backend cases. Record actual devices/InputSystem/legacy setting. Public provider/ProcessMouseState invocation is API seam coverage, not physical keyboard/touch/device/haptics evidence. Capture actual device disconnect/reconnect/input-module/UI raycast scenarios separately when that native capability is available. Root/owners supply missing native drivers; do not claim a backend because one injected path passed.

Preserve frozen enter-play settings for baseline (currently enabled/options3). Separate `reload-normal` and `reload-fast` hosts record exact EditorSettings transformations and actual domain/scene reload mode; use existing lifecycle/static-state fixtures through real Play transitions, not reflective Reset calls as reload proof. Main settings stay unchanged.

For GUI/render/end-of-frame, use a dedicated task-owned `native-gui` host/job WITHOUT `-batchmode` and WITHOUT `-nographics`, with the same hidden Start-Process/ownership/deadline convention. Supported TestRunner auto-exit still supplies XML. Exact source-known filters include `Neo.Editor.Tests.Audit20261023CoroutineUnityLifecycleTests.EndOfFrame_ResumesAfterLateUpdateInTheSameFrame`, native Coroutine/thumbnail/Editor utility cases, HtmlToUitk runtime blur/material/UI panel cases and active imported Playable sample flows. Record actual non-null graphics device/render targets and measured active tween/VFX/UI captures. A hidden session may fail to supply a rendering GameView/end-frame; that remains a capability failure/unverified slice rather than a fake pass or permission to show/control a user Editor. Actual visible/manual observation, built-in Profiler capture or physical audio/input devices needs its real task-owned capability and root-coordinated session. AudioSource state does not prove audible output. NavMesh/native2D/3D callbacks/Undo/editor authoring/preview/pooling/destruction/reload each retain a real receipt. Missing HDRP/Spine/Cinemachine/vendor/AOT/other-platform capability is explicitly unsupported/unverified; no installs or invented platform passes.

## 8. Separate Telepathy players after final harness review

After the pending Counter extension and independent final harness review/receipt, reread exact CLI/build sources and required receiver-case map from the frozen snapshot. The current entrypoint is `Neo.Tests.Network.Integration.Audit20261023TestPlayNetworkPlayerBuild.Build`; it requires MIRROR+UNITY_INCLUDE_TESTS. Use each successfully compiled integrated host, one Editor at a time, and a NEW `$PeerRunId`32hex per Editor/attempt. Decide it BEFORE build; baked identities make changing it only at launch invalid.

```powershell
$EditorKey = 'u6000_6_0f1' # rebuild/rebind separately for u6000_3_14f1
$HostPath = Join-Path $FinalRoot "$EditorKey\mirror-on-dev"
$Common = @('-projectPath', $HostPath, '-buildTarget', 'Win64')
$PeerRunId = [guid]::NewGuid().ToString('N')
$BuildRoot = Join-Path $FinalRoot "peer-builds\$EditorKey\$PeerRunId"
$PeerRoot = Join-Path $FinalRoot "peer-runs\$EditorKey\$PeerRunId"
$BuildArgs = @('-batchmode', '-nographics') + $Common + @('-executeMethod',
    'Neo.Tests.Network.Integration.Audit20261023TestPlayNetworkPlayerBuild.Build', '-quit',
    '-neoNetworkMatrixBuildRunId', $PeerRunId, '-neoNetworkMatrixBuildSnapshot', $SnapshotSha,
    '-neoNetworkMatrixBuildOutput', $BuildRoot, '-logFile', "$HostPath\Receipts\40-peer-build-$PeerRunId.log")
```

BuildRoot must be absent; its parent may be created inside FinalRoot. The helper alone creates/releases its temporary empty bootstrap scene and builds all three Windows64 players sequentially with Development|IncludeTestAssemblies|StrictMode. It captures/restores host company/product/version/active scene. Require Editor exit0, actual successful BuildReports/Weaver, `build-receipt.json.passed=true`, matching run/SHA/version/options/three products and three real executable files; verify temporary scene/settings restoration against the host ledger. Build failure cannot become a networking test result. No release build/publish is implied.

Launch only after the build Editor has exited. Use the frozen launcher, not the moving main script:

```powershell
$FrozenLauncher = Join-Path $FrozenSource 'scripts\network_playmode_matrix.ps1'
& powershell.exe -NoProfile -File $FrozenLauncher `
  -ServerExe "$BuildRoot\NetworkMatrix_server\NetworkMatrix.exe" `
  -ClientAExe "$BuildRoot\NetworkMatrix_client-a\NetworkMatrix.exe" `
  -ClientBExe "$BuildRoot\NetworkMatrix_client-b\NetworkMatrix.exe" `
  -RunId $PeerRunId -SnapshotSha $SnapshotSha -Port 27763 -OutputRoot $PeerRoot `
  -PhaseTimeout 20 -OverallTimeout 300
```

For6.3 rebind HostPath/Common/Editor/build/run directories and choose a different new run ID and candidate port27764. Ports are candidates, not currently claimed free; the launcher performs its named-mutex/TCP availability reservation and fails safely if occupied. Do not take over an existing listener. A retry uses new identities/builds/output directories, never deletes/reuses prior persistence. Capture launcher stdout/stderr/exit in its outer task-owned job; the launcher starts only its three returned Process handles with hidden windows and owns realtime cleanup.

Actual player company is `NeoQA_Network_<PeerRunId>` and products `NetworkMatrix_server`, `NetworkMatrix_client-a`, `NetworkMatrix_client-b`. Launcher requires prior absence of each actual LocalLow directory and HKCU player key. Every player verifies WindowsPlayer/company/product/actual persistentDataPath before scenario setup; its `-neoNetworkMatrixPersistentDataPath` argument is an expected-value assertion, not a Unity path override. Before first provider construction/load, it emits environment/PID/transport/port, then uses existing FileSaveProvider.PersistenceRoot at `<PeerRoot>/<role>/save` and sentinel round-trip/removal. The NI7 persisted wallet remains in that explicit root; implicit Unity state has its own baked identity. Registry probing reads names only, never clears values/keys.

Require three distinct OS PIDs, production `Mirror.TelepathyTransport`, dedicated/pure-client role and Connected→Authenticated→Ready→PlayerSpawned/world barriers. Preserve each `<role>/events.jsonl`, player log, owned-processes.json, owned-run.json, port-lease.json, persistence-probe.json and matrix-result.json. Validate raw schema/run/SHA/role/PID/epoch/step identities, exact receiver payload/counts cross-correlated with server remote-receiver records, all final required cases (including the new malformed Counter extension), independent money pending-versus-actual receipts, reconnect/restart and real physics/context. A send/control acknowledgment, host body invocation or alive process is insufficient.

Pass requires launcher exit0, matrix-result.passed=true/no firstFailure/no cleanupFailures, all three player exit0 and exactly one successful cleanup/result per role, with no task-owned peer/socket left. Forced owned termination is cleanup debt and prevents a clean pass. Check final expected-case set from the reviewed frozen extension rather than the preparation-time old list. Preserve actual first failure plus cleanup failures; do not weaken a failure into a host/private-method test.

## 9. Final receipt and unresolved prerequisites

Update UnityVerification.md only during the later authorized execution phase. Per snapshot/editor/profile/job report exact command/executable/version/target/backend/dependency graph, source/host transformations, environment/persistence/HKCU proof, discovery set, raw XML/log/JSON hashes, console errors, pass/fail/skip reasons and native/manual limits. Compare main/snapshot input hashes after the run, including untouched user DocsEn.meta, main Packages/ProjectSettings and release Samples~/package layout. Explain every host-only import/migration/metadata change; an unexplained rewrite is a source/import finding. Retain all evidence. Root personally reads the report and artifacts; this operator makes no commits/releases/pushes.

Blocking conditions before authorization: root global freeze/closure manifest, current final approvals and remaining correction closure, and NI26 final malformed-Counter peer harness review. Discovery publication/source review is complete in NI24; its actual compiled/native result remains a later gate. Further actual requirements are optional-Mirror dependency support (hard references cannot be wished away), declared package resolution, net9 process-probe capability and installed/license/build/native graphics/input/audio/device capabilities. Native measurement/manual slices without an existing runnable driver stay explicitly unverified. Attempt a required unavailable path at most twice with a concrete changed condition before retry, retain its exact blocker/owner, and continue only independent safe checks while the gate remains valid. No silent source workaround, substitute driver or fake platform acceptance.

Preparation identities (these will be reread at freeze, not reused as final approval): existing112-line plan `0385ed8259c5629b0a8d20d311533fb393d3f3fad96ae2180e1f2a54dec86fc4`; ValidationHost `95d5e0628c6186ac954407a91323617e3258174f56ebe31464c2b040cd8736c7`; Paths helper `586593af17694e6bfe3cb922392139aab830602e57c6f05d501c629a0f1188eb`; peer build CLI `c6264174fc42c52140192a8a479f20d08d21dcc9f9057b68c8f119d84fc3443c`; peer argument/isolation excerpt `eed0a2230ab9c5ffa15fa0d565d92067ce4f0e334d9315ae115af79ac792ca9a`; launcher CLI excerpt `b993c859d73a8450a3fdb762e1f99fb5b773f0187d307a99aa04df8cf3ee3631`. Environment/UPM helper was read fully; moving peer scenario bodies were deliberately not fully reviewed/approved in this preparation.
