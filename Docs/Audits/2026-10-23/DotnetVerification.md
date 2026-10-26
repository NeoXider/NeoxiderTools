# Frozen-source .NET verification

Status: **COMPLETE — authorized standalone .NET diagnostics passed, with30 preserved historical skips. Exact .NET9 runtime and native Unity acceptance are not established.** SAME `/root/audit_extensions`, native `gpt-6.1-sol`/max. All .NET jobs stopped normally; no timeout, source fix, SDK install, global environment/runtimeconfig change, Unity launch, foreign process control or commit. Product/package/metadata fixes remain root/original-owner work.

## Frozen binding and ownership

Root explicitly opened execution for this operator after final capture/guards. Request: `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/unity/source-final-verification-request.json`. Manifest SHA256 **`0e433bf9848fe86066661c9209ebc955ed75a285e73d1e0a21f458a925e93260`**, sourceRoot `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/unity/snapshots/final-20261023T230131Z/source`. Branch demo, target11.0.0, HEAD `ca1985564dfe7726834e2832d51c33e0e63e060c`; dirty/new source identity is the manifest, not HEAD alone. Request flags, state freeze/execution/root-source-final, no required followups, branch/HEAD and manifest binding were checked before launch.

Owned run root, abbreviated **R** below: `D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/dotnet/final-20261023T230131Z-0e433bf9848f-b01`. All10252 captured files/142721271bytes were verified and copied ordinarily into R/source, without links/shared Libraries or mutation of main/snapshot. Logs, results, temp data, CLI home, NuGet caches, bin/obj and fixture .qa outputs belong to R. Root's package checker independently reported malformed metadata; root explicitly directed these independent .NET diagnostics to continue on this unchanged snapshot. Those errors were not patched or declared repaired here.

Only approved input transformation: copied Verification~/PureLogic.Tests.csproj has exactly three hardcoded Compile prefixes relocated from the main checkout to R/source. Exact old/new text, one match each and before/after hashes are in `R/receipts/purelogic-relocation.json`; all37 resolved authored C# inputs are copied manifest entries. SDK-generated obj attributes stay owned outputs. Main/snapshot projects remain unchanged. Version-named verification/typecheck projects and generated root Neo.* projects were not run as test suites.

Post-run verification23:13:54–23:14:09UTC: all10252 frozen files and10252 copied inputs match, allowing only that recorded project relocation; all9894 main product-role files match; zero unexplained mismatches. Main user-owned DocsEn.meta remains hash `484583f4dab609dd5ac4a8a5894005c09da76f07ea850b6582116053c5053dfc` and is absent from frozen/copy payloads. Captured review-context reports are not compiler inputs or a reason to relabel later product revisions.

## Actual toolchain and runtime qualification

Executed installed `C:/Program Files/dotnet/dotnet.exe` --info/--list-sdks/--list-runtimes. Only SDK **10.0.301**, MSBuild18.6.4; Microsoft.NETCore.App **8.0.21 and10.0.9**, no9.x runtime. Root then explicitly authorized process-local **DOTNET_ROLL_FORWARD=Major** for net9-target test/probe diagnostics. No runtimeconfig/global setting/source was changed and no runtime installed. Host traces record net9-target resolution/CoreCLR10.0.9; net8 tests naturally select8.0.21, while the outer SDK itself uses10.0.9. This is **net9 target / net10 execution evidence**, not an exact-net9-runtime pass.

All six solution test projects resolved Microsoft.NET.Test.Sdk17.14.1, NUnit4.6.1, NUnit3TestAdapter4.6.0. Recorded transitive packages include TestPlatform.ObjectModel/TestHost and CodeCoverage17.14.1, Newtonsoft.Json13.0.3, System.Collections.Immutable/System.Reflection.Metadata8.0.0. All16 assets manifests use only R/nuget/packages. SDK/runtime identities, environment, full command argv, cwd, process PID, timestamps/deadline/exit and raw stdout/stderr hashes are retained per job. Initial localized SDK/probe logs are preserved raw; subsequent CLI language was process-local English.

## Commands actually executed

All cwd R/source. S is `R/source/Tools/HtmlToUitk/HtmlToUitk.sln`; V is `R/source/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~`. Exact absolute arguments are in the named receipts; no filters or weakened assertions were added.

| Job / UTC | Actual command and result |
|---|---|
| SDK inventory23:06:12–14 | `dotnet --info`, `--list-sdks`, `--list-runtimes`; each exit0. |
| Probe build23:06:14–23 | `dotnet build V/Audit20261023NeoHtmlToUitkEditorProcessProbe.csproj --configuration Release --verbosity minimal`; exit0. |
| Solution restore23:08:03–16 | `dotnet restore S --packages R/nuget/packages --verbosity minimal`; exit0. |
| Solution build23:08:17–28 | `dotnet build S --configuration Release --no-restore --verbosity minimal -m:1`; exit0, zero errors/one CS0219 warning in Convert/Pipeline/Assembler.cs144 for unused text. |
| Probe smoke23:09:30–34 | `dotnet <owned probe.dll> run R/tmp/probe-smoke.job`, with complete mode and root-approved Major; exit0/expected stdout+stderr+owned PID record. This proves process launch/contract only; its synthetic PASS marker is not product conversion/native acceptance. |
| Full solution23:09:36–23:12:18 | `dotnet test S --configuration Release --no-restore --no-build -m:1 --logger trx;LogFilePrefix=HtmlToUitk --results-directory R/results/html-solution --diag R/logs/solution-tests.vstest.log`; exit0. |
| PureLogic restore23:12:20–27 | `dotnet restore V/PureLogic.Tests.csproj --packages R/nuget/packages --verbosity minimal`; exit0. |
| Full PureLogic23:12:29–53 | `dotnet test V/PureLogic.Tests.csproj --configuration Release --no-restore --logger trx;LogFileName=PureLogic.trx --results-directory R/results/pure-logic -m:1 --diag R/logs/purelogic-tests.vstest.log`; exit0. |

## Actual results

TRX result rows and console summaries agree. Skipped values below count actual NotExecuted rows; they are not passes. Seven distinct nonempty TRX files preserve every assembly result; no logger collision or missing suite occurred.

| Assembly | Target / execution runtime | Reported total | Passed | Skipped | Failed |
|---|---|---:|---:|---:|---:|
| HtmlToUitk.Core.Tests | net9 /10.0.9 Major |1196|1196|0|0|
| HtmlToUitk.Capabilities.Tests | net9 /10.0.9 Major |199|199|0|0|
| HtmlToUitk.Rules.Tests | net9 /10.0.9 Major |974|973|1|0|
| HtmlToUitk.Bake.Tests | net8 /8.0.21 |139|139|0|0|
| HtmlToUitk.Convert.Tests | net8 /8.0.21 |64|35|29|0|
| HtmlToUitk.Cli.Tests | net9 /10.0.9 Major |19|19|0|0|
| **Full solution** | mixed as above |**2591**|**2561**|**30**|**0**|
| **PureLogic.Tests**, separate full suite | net8 /8.0.21 |**291**|**291**|**0**|**0**|

Real installed Unity6.6 DLL cases **HtmlToUitk.Capabilities.Tests.CheckedInCapabilitiesTests.FreshReadMatchesCheckedInFileWhenUnityIsPresent** and **RealAssemblyReadIsReproducibleWhenUnityIsPresent** both **Passed**, not Ignore. UNITY_MANAGED_DIR was actual `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Managed/UnityEngine`. Captured182 actual installed DLL identities. UIElementsModule is2579968bytes, SHA256 `f70334cd9de6d2b56bc19c65c54d9c2cda21280b402d23a1da935b65c06d5754`. These are real metadata-reader checks, not native Unity import/render/visual proof. Checked-in capability JSON was not regenerated.

The30 preserved historical skips are29 converter cases plus Rules Fixture_S08_tokens-and-skin. Converter22 KnownOpen cases retain their exact source reasons: S21 gap expansion; S30 media; S33 native gradient normalization; S34 gradient bake; S35 shadow; S37 blur; S39/S41 keyframes; S43 page routing; M01/M02/M03/M04/M05/M12 multi-file golden verification; M07/M08 partials; M10 single-document; M13 mapping; M14 shared tokens; M15 job globs; M16 incremental regeneration. Seven converter cases are validate-only: M06/M09/M11/S05/S12/S40/S44. Rules S08 retains the known J001 root-custom-property versus expected C034 issue. `R/receipts/test-results.json` preserves every full test name and exact raw reason alongside its TRX; no Ignore/expectation/filter was edited. The older2559/32 text was not reused as current evidence: the two formerly optional real-DLL cases actually ran, giving2561/30 here.

## Probe handoff and evidence identities

Early build/path receipt was sent to root and sole Unity operator A before full suites. Exact DLL:

`D:/Git/NeoxiderTools/Local/Audits/assembly-2026-10-23/dotnet/final-20261023T230131Z-0e433bf9848f-b01/source/Assets/Neoxider/Tests/Edit/HtmlToUitk/Verification~/bin/Audit20261023NeoHtmlToUitkEditorProcessProbe/net9.0/Audit20261023NeoHtmlToUitkEditorProcessProbe.dll`

DLL SHA256 **`254f7be2baebc78e4b334a486efb35619be35cb9e1b4abeec64579b000a8fa7a`**. Project/source/props and DLL/runtimeconfig/deps identities are retained. Initial handoff disclosed missing9 runtime; the separate qualified runtime10 handoff records the subsequent root-approved smoke. A confirmed receipt and owns any native Editor process tests; those were not run by this operator.

All paths below relative to R. Per-job JSON references exact stdout/stderr and source request; test-results JSON includes all seven TRX paths/hashes. Runtime-evidence JSON links trace logs showing actual CoreCLR choices.

| Evidence | SHA256 |
|---|---|
| receipts/input-copy.json | `bccb9694a15ae7d553333dd363c1f6e68cf28efac075b11b392d9124cd5608f8` |
| receipts/purelogic-relocation.json | `dd7834875622d51a5930ae80a862809e6f4d725ad89fde57e56267d2d9d89794` |
| receipts/test-results.json | `59bc389a1205d557aa727a6c0e47f61b4aa2ce0289e9b53a43584d018b20009d` |
| receipts/runtime-evidence.json | `f409d4e81d3bd820bce19bdf26184f16ceda4b9471404bb7475b458ef6327cc4` |
| receipts/post-run-inputs.json | `89de6225bc46e3ec0d82a55feb739c383b38477113fec02d79c90738c180dc61` |
| receipts/unity-managed-inputs.json | `aaa3b9e7e0dd013a936861ac680bfcfa33badce66a16747a8f3e11f67f235bc9` |
| receipts/resolved-dependencies.json | `91f70a1c194fd41bb1d79851bf3bfa0c67efa4be002126bc8256b59e4ac04080` |
| receipts/process-probe-build.json | `28cf975ad2fbea045b03e8aad887e95b10182ba46a8eb5fbd1fef6e3b4514ac8` |
| receipts/process-probe-runtime10-smoke.json | `dcd3d0a18d9794e28766e3bde21e5df179f346580872acc8172c62b2d9d08c5b` |
| receipts/process-probe-runtime10-handoff.json | `fc9f1b23fb02a465fc8ec7c433a1350db6ce7009e369c44c83f02bc8486e6f70` |

TRX hashes, ordered Core/Capabilities/Rules/Bake/Convert/CLI/PureLogic: `5f2cec2dd936c0d9432f58a1e0113dc79cd98eed36b5db17d1a178cec99a255c`; `2b93928f48f7c1bd2aaefd92ad71a5079a21d77e8979340b64d2f4272a73a9f9`; `fbe4dad53f25ca54c36efcd18895a1ae2393339acaae300860ffb67dcb0bd5ac`; `26ed1f3b3a81e315cc2dcbb5f0faba26a89cdaae0e17cdfb96e09dae4b7a9733`; `05d88128b6c593a6f386a4602caa532bf2c0bc5188d91f2b754e656425a4ba0c`; `6aa3b68c2900dd7b18afe412e76a91a86cc959ab10031c3027854286a422d62b`; `0591ff351ad0ce1c3b30b0af999ba08b7ff75fb8ed3fa3f15f092f863190d53b`.

## Limits

This completes the assigned standalone .NET jobs on this exact snapshot/toolchain. Exact-runtime9 execution remains unavailable;30 historical cases remain skipped. No native Unity/editor/player/device/rendering/performance/network transport, package/link/UTF-8/metadata validation or broad library pass is claimed. Root's independent metadata failure and A's Unity outcomes remain separate. No source patch was made; this report and Local execution evidence do not authorize further launches after root closes the gate or substitute for a later corrected freeze.
