# Native consumer context review — dependency-only, PRE-Neo

2026-11-02. **Short-host dependency context ACCEPTED в указанном ниже scope.** Root создал и нативно открыл новый Unity host; этот reviewer независимо прочитал complete logs/receipts, hashed payload/locks/configuration и проверил main preservation. Unity, компилятор, тесты, subprocesses и host/config mutations этим reviewer не запускались. Единственная запись — данный отчёт.

Принятый host: `D:/Temp/NeoLibQA1102/Local/Audits/assembly-2026-10-23/unity/final/u`. Current serialized identity: company `NeoQA_20261023_LibClosureU63UpmShort20261102`, product `NeoTools_ConsumerU63UpmShort20261102`, `activeInputHandler=2` (Both). Neo пока отсутствует в actual manifest, native lock, Assets и PackageCache. **Это не Neo compile/import/sample/test pass, не loaded-Pro absence proof и не final package freeze.**

## Exact evidence

| Receipt | SHA256 |
|---|---|
| [Short-path amendment](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/consumer-shortpath-amendment.json) | `4dc9d10d36409cccd3103fafc949b70f0663bb374d141b9aecf62fc0de4ec743` |
| [Short identity/dependencies](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/consumer-short-identity-dependencies-r1.json) | `ac3063971cdb35dd3d6430f6656ce1187d73aa85657bf599d96f07c3c5db1bd0` |
| [Manual payload whitelist](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/consumer-manual-dependencies-staging-r1.json) | `068aedf25f2fd497fac01e1ef9ebeb91bcec6663a461d202ac6fa452d181a68d` |
| [Current context admission](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/consumer-short-context-admission-r1.json) | `618065bed2964af6f3b184cd5a6d710b6c163e2888cde43866779b5dbba3acdd` |

Native launch commands use official installed `6000.3.14f1` Unity.exe, the exact fresh short host, batchmode/D3D11 and natural exit. `consumer-short-u63-create-r1` exit0 created the empty project. `consumer-short-u63-dependency-resolve-r1` exit0 resolved/imported dependencies; complete 2266-line log has **0 C# compiler errors, 0 exception lines, 0 asset-import failures**. Both logs retain ordinary startup licensing access-token/Curl42 messages, so this report does not claim every line containing Error is absent. There is no Neo test XML or invented executed-test count.

Log SHA256: short create `e361110c3370933dd78e0ef60ef20118148484ec46ee3fb794e38c7cdc3b065e`; short dependency resolve `eeec0b94d4daedbcf0073df0366d7e32e5ac63ecaad331980b0b9aeda0588418`. Admission raw launch/log/public-prefs hash chain independently matches its actual files.

## Dependencies, metadata and notices

All **1137 whitelisted files** independently matched both the owned immutable dependency snapshot and their actual short-host destinations by bytes/SHA256:

| Manual dependency | Files including metadata | Preserved license/notice evidence |
|---|---:|---|
| Mirror | 757 | Mirror MIT, Mono.Cecil notice, Telepathy/SimpleWeb/kcp2k notices, renamed BouncyCastle notice |
| UniTask | 340 | Cysharp MIT license and metadata |
| DOTween Core | 40 | Exact existing Core DLL/modules/editor support, copyright/readme/license link and metadata |

The DOTween readme was retained unchanged; Core and Pro have distinct vendor licensing. The original vendor [license page](https://dotween.demigiant.com/license.php) was checked. This review records local QA reuse and notice preservation; it does not authorize distribution of owner assets, modified vendor code or Pro payload. No vendor code was modified by this reviewer.

The full actual Assets inventory contains 1139 files: those 1137 payloads plus only generated parent metadata `Assets/Plugins.meta` and `Assets/Plugins/Demigiant.meta`. No Neo/Pro-named Asset or Neo/Pro package-cache path was found. This filesystem/whitelist check does **not** inspect an Editor AppDomain or prove no Pro assembly was loaded; the later environment probe must retain that separate check.

Actual manifest has 39 entries and native lock 41 resolved packages. Full long/short native lock bytes are identical, SHA256 `7995c9554f39ccfc498e1a566cdf18feeaada58ceb257dc885455452935da227`. Current manifest SHA256 remains `ff1d5e0a90b8d665ad5c6aa99e7effd24c4626b9800bcb0ac7b2f17c8a7c1131`. The complete native version/source vector matches the admission receipt: Input System1.20.1, AI Navigation2.0.16, UGUI2.0.0, Sprite1.0.0, Test Framework1.6.0, NUnit2.0.5 and recorded built-in modules. No manual/unregistered Mirror was added as a testable package; actual manifest does not yet register Neo.

## Expected native dependency side effects

Full actual Mirror editor hooks were read, not inferred from names:

- `CompilerSymbols/PreprocessorDefine.cs` uses InitializeOnLoadMethod and the selected build target to add `MIRROR`, `MIRROR_89_OR_NEWER`, `MIRROR_90_OR_NEWER`, `MIRROR_93_OR_NEWER`, `MIRROR_96_OR_NEWER` to existing defines. Current Standalone string is DOTWEEN plus exactly those five symbols. Removing only that suffix **in memory** reconstructs exact pre-run settings SHA256 `15fe4b664e17f9d23b38922570d3cf2c54e675031033222d1cd9630cb1035b10` from current `4e14eb66f966840ce73ac6b82b0cce671a548172c03a5e94b766ec1f72504641`. No other native host settings delta is hidden by normalization. The host was not restored or written by this reviewer.
- `ScriptTemplates/Editor/MoveToAssetsFolder.cs` uses InitializeOnLoad/SessionState and AssetDatabase.MoveAsset to move the named folder into `Assets/ScriptTemplates`. Native log records the move. All **24** moved files/metas match the source whitelist and recorded destination SHA; every old source path is absent, every new destination is exact. Payload content/GUID metadata was preserved, rather than silently dropped or regenerated.

These changes belong to the new owned consumer host and explain its post-import state. They are not edits to main-project settings or source.

## Long-path failure preserved; short-path comparison qualified

`consumer-empty-u63-dependency-resolve-r1` returned exit0 but its complete log contains **six unique Input System UXML asset-import failures**, with DirectoryNotFoundException. It remains **NOT ACCEPTED as clean dependency import**; its log SHA256 is `12332d5e8a2258619f12915418563b564ca31e3b2b738cd7af8ce90579b9642f`. No Neo was installed there either, so this is not a demonstrated Neo source/package defect.

All six files exist with identical SHA256 in long and short PackageCache. Each corresponding short native log line records successful ScriptedImporter output with an artifact id:

| Existing Input System UXML | Long absolute chars | Short absolute chars |
|---|---:|---:|
| CompositeBindingPropertiesEditor | 268 | 213 |
| CompositePartBindingPropertiesEditor | 272 | 217 |
| InputActionMapsTreeViewItem | 263 | 208 |
| InputActionsProjectSettings | 263 | 208 |
| InputActionsTreeViewItem | 260 | 205 |
| NameAndParameterListViewItemTemplate | 272 | 217 |

Thus the actual ranges are **260–272→205–217**, not six identical208-character paths or six strictly263+ paths. Same registry locks and unchanged file contents plus real short native imports support this shorter-path workaround. They do not establish a universal exact path-length limit, a controlled single-variable experiment for every host input, or a Neo fix. Original long-host evidence is retained.

## Preservation and next boundary

All **5947 main frozen files** from the completed combined readback source manifest were compared to current bytes/SHA without differences. Protected12 matched `pre-main-merge.json`. Both long/short dependency-run public prefs comparisons show **51→51**, no added/changed original names, no registry modification and no private-value publication. Private snapshots, UserSettings and owner application data were not read.

One future-plan qualification remains: the short-path amendment is a preparation/history receipt. Its nested intended Neo commands still contain the old long host/company/product, and nested manifestDraft points to the old Neo source location; the declared future short package path is different. Those commands are explicitly INTENDED_NOT_RUN and were not the successful dependency-run commands. Before final Neo import, root must generate/check fresh exact short-host command vectors and final package location/version/hash rather than execute the inherited nested recipe. This does not invalidate the completed NoNeo dependency context.

Next gates remain OPEN: independent source review of the pending TMP bridge, final reviewed Neo package freeze, actual Neo registration/compile/discovery, all required samples, loaded-Pro absence and runtime/profile checks. The current admission allows progress from a verified dependency-only context; it does not claim package all-pass or final consumer acceptance. No other file was written in this review.

## R2 future-command amendment — caveat resolved, not executed

Root subsequently preserved the original plan/R1 evidence and created immutable [consumer-shortpath-amendment-r2.json](D:/Temp/Neoxider-LibraryClosure-20261102/profiles/consumer-shortpath-amendment-r2.json), SHA256 `c7d082b62b3010159938f92827915e2ccd87bc79475086f27671b5cbccdfd346`. Its full four-profile strings were independently checked. All command `-projectPath`, company/product, expected version11.0.1, Evidence outputs and applicable packageRoot/manifest references now agree with their short profile. UPM references consistently use `D:/Temp/NeoLibQA1102/pkg/neo-11.0.1-reviewed-final`; paired Assets uses its own host `Assets/Neoxider`. No old long-host command alias remains.

Hosts/identities are unique: `/u` exists with the dependency-only NoNeo context; `/a`, `/n`, `/m` remain absent and PLANNED_NOT_CREATED_NOT_RUN. Original-plan SHA and R1 amendment SHA still match. This resolves the future-recipe mismatch noted above. **R2 is coherent source preparation only:** no future Neo command was executed by the generator or this reviewer, and final reviewed package freeze after temporary-helper cleanup, bridge QA and actual import/discovery/sample/profile evidence are still required. Current native dependency receipts and their bounded admission are unchanged.

## TMP bridge alias correction — narrow SOURCE re-admission

**Alias-only correction SOURCE ADMITTED for root staging and one R2 attempt in the exact owned short host.** No remaining concrete blocker was found in this bounded delta. This is not an R2 compile, importer, callback, native asset, cleanup or preference success claim.

The earlier [independent full-source gate](D:/Git/NeoxiderTools/Docs/Audits/2026-11-02/NativeInteractionExecutionReview.md:447), document SHA256 `5e8639f1c156512165a938d7c3e3d5d52ca7e36c3ae0a9873e9d1afc09a705ac`, was read for the original complete bridge/official importer/resource/ownership lifecycle assessment. Archived source `2a652b64f95f274effc2dfcd8f6530b4d1305ca02e9376d1223b273230aa771a` and README `813a264ef94af036168bf82d027eac5b68449832dbce59c0e84aa1c907e79dad` independently match their preserved bytes.

New external source SHA256 is `f238ba799947d2dee0662a3e2eab5784502cbbb74e19841a420ecc5c3ed1fcbb`; README is `275c10b5cdb75eca5bf36977c15e291c2c4097d28e57832329e3306c5e6de67b`. The **entire C# delta is one using directive**:

```csharp
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
```

Reversing it to the prior namespace using restores the archived2a source exactly, byte-for-byte. Function bodies, public entry, ownership/host/hash/resource pins, callback/watchdog/cleanup, output constant and the single official ImportResources(true,false,false) call are unchanged. Every former simple PackageInfo reference now denotes the intended PackageManager type; no other removed namespace type use was found in those call sites. This addresses the observed ambiguity by source inspection, without asserting that future compilation has already succeeded.

### Actual R1 boundary retained

Independently checked `consumer-short-u63-tmp-essentials-r1` launch, complete [R1 log](D:/Temp/NeoLibQA1102/Local/Audits/assembly-2026-10-23/unity/final/u/Evidence/tmp-essentials-preparation-r1.log), staging receipt, [compile-abort receipt](D:/Temp/Neoxider-LibraryClosure-20261102/runs/consumer-short-u63-tmp-essentials-r1/compile-abort-receipt.json) and public prefs comparison. PID29264 ended exit **1** with CS0104 at helper lines82/83/157: PackageInfo was ambiguous between UnityEditor.PackageInfo and UnityEditor.PackageManager.PackageInfo. Log SHA256 `9c4fcc9442ad12aff10b2b6f12bc88bee9489316b2edf60a0c7854dd96cb518a`; abort-receipt SHA256 `7e8d7f1b527ba15a25c4c19a3c87b184995724436c7ef1349682e3dd62dee6d2`.

This was **before executeMethod/native import**, with importer invocations0 and no native resource receipt. Current filesystem checks still find no `Assets/TextMesh Pro`, its meta, or the reserved r1 JSON output. Public prefs stayed51→51 without added/changed names or private-value publication. R1 is a task-temporary **scratch-host helper compile defect**, not a Neo/package/runtime test failure or a Main/Module compile result. No resource/callback/cleanup/loaded-Pro success can be inferred from the unentered bridge; the separate Module source admission is unaffected by this scratch-specific failure.

The new README honestly records that abort and the alias-only correction. Its structured R2 recipe was independently checked: exact short project/CWD/owner activation, public entry, new f238 helper SHA argument, D3D11 and fresh `tmp-essentials-preparation-r2.log`, with no quit/test/nographics flags. Keeping `tmp-essentials-preparation-r1.json` as the output constant is qualified and safe for this attempt because R1 never reserved it and it is still absent; nothing was deleted or overwritten to create that condition. Any later partial/failed state still needs the existing explicit owned-state disposition, not automatic cleanup/reuse.

Root may stage the exact corrected temporary source/meta and execute R2 under its established resource/source/prefs/ownership guards. Accept resource preparation only from the actual native completed receipt/events/assets/GUIDs/whole log and exit0. Final Neo package freeze after required temporary-helper cleanup, actual registration/samples/profile and other consumer gates remain OPEN. This reviewer made no source, host, configuration, process, compiler or import change; only this report was appended.

## Actual TMP R2 — scoped native resource preparation accepted

**PRE-Neo TMP Essentials resource step ACCEPTED** in the exact short Unity6000.3.14f1 / Direct3D11 / Both2 / DOTWEEN host. This is actual native import/asset evidence, superseding the R2 NotRun status above for this one step. It is not Neo registration, sample/runtime/visual/Player or final consumer acceptance.

Completed run: `consumer-short-u63-tmp-essentials-r2`, PID40180, native exit **0**. [Launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/consumer-short-u63-tmp-essentials-r2/launch.json) SHA256 `730b3c217ac946bc3068013ea0dfd53de8794cbbc24ecb95688d6ffe0c5b2b61`; staging SHA256 `9f229a740aa29a9ff2afd3c7a7a77a963800e4f88120c17123ede7084318c037`. Exact staged f238 helper and its original owned meta8b8d42d4... matched; the actual compiled Assembly-CSharp-Editor.dll matched native receipt hash `a2c6d63f14120be7eba29941d89018b37f807e9b49611269492f52ea5008a040`. No helper source or host was changed by this reviewer.

[Native receipt](D:/Temp/NeoLibQA1102/Local/Audits/assembly-2026-10-23/unity/final/u/Evidence/tmp-essentials-preparation-r1.json) SHA256 `c62db6688df4062a1dfe2276520880c9488e532500a3b6829245e414649dd9c3` belongs to this R2 invocation: runId `bef7e80047b240fcaa10ef962ddd87d4`, started13:33:21Z/finished13:33:25Z. The r1-suffixed output was genuinely unused before R2 and was not an overwritten old native result. Status is `verified-tmp-essentials`, nativePreparationVerified=true, assetDatabaseIdle=true, empty error/errorDetails; **1 invocation / 1 matching started / 1 matching completed / 0 failed/cancelled/errors/warnings**. Event order, package name, timestamps and launch/host identity agree.

The complete fresh [R2 log](D:/Temp/NeoLibQA1102/Local/Audits/assembly-2026-10-23/unity/final/u/Evidence/tmp-essentials-preparation-r2.log), 42996 bytes / 532 lines, SHA256 `23357e97f713101ffa707ffc68f0ed40b76ddf8a5431e5300a2e2a01818fbfcb`, has **0 C# compiler errors, 0 exception lines, 0 asset-import failures**. It retains a startup licensing access-token error before the import-hook scope; native receipt errors0 is not rewritten into a claim that no log line ever says Error.

### Official source/archive and actual asset chain

The actual registered BuiltIn `com.unity.ugui`2.0.0 root equals the private resolver's returned native PackageCache root. Package JSON8e66169c... and importer sourceca99679d... match the reviewed pins. The public three-Boolean API used Essentials=true, Examples=false, interactive=false. The actual resolved archive is **941722 bytes**, SHA256 `58564ec64cef8a8cea3d0b8ffb5e6fecf4176d544a2f6a29a2b43a0a33256431`.

This reviewer independently read/hash-checked that archive and parsed its tar metadata **in memory**, without extraction or native loads. All **45 official pathname routes** stay under Assets/TextMesh Pro and have actual imported paths and matching archive-meta GUIDs; no .cs/.dll/.asmdef/.asmref or Examples payload is present. Native receipt records exactly nine loaded/persistent assets, whose current physical file hashes and GUID/path mapping match independently:

| Native role | Actual recorded type / identity |
|---|---|
| Settings | TMP_Settings, GUID3f5b5dff...; actual Resources.Load resolved this settings object |
| Required sample font | TMP_FontAsset, GUID8f586378... |
| Required SDF shader | Shader, GUID68e6db2e... |
| Font/material mobile shader | Shader, GUIDfe393ace... |
| Required Drop Shadow material | Material, GUIDe73a58f6... |
| Settings default sprite | TMP_SpriteAsset, GUIDc41005c1... |
| Settings default style | TMP_StyleSheet, GUIDf952c082... |
| EmojiOne attribution | TextAsset, GUID381dcb09... |
| LiberationSans OFL notice | TextAsset, GUID6e59c59b... |

Exact native validation code requires persistent loaded font/materials, both shaders supported in this D3D11 profile, native settings default-font/sprite/style references, font material and Drop Shadow using the mobile shader, and shadow texture equal to the persistent imported font atlas; reported atlas count is1. Current serialized files independently agree: settings points to those font/sprite/style GUIDs, both materials point to fe393ace..., and both use atlas fileID28684132378477856 embedded in the required font. This is source+native-receipt identity/reference proof with static file corroboration; no second AssetDatabase/Resources load was performed by this reviewer.

Both notice files match the exact original archive bytes: EmojiOne SHA `4b65f8960e4f4c5c8b35fd139e369265e9bb45667fc16461b2ba01188d464d93`, OFL SHA `37f8552e9a874ec10710dc0ede6a9adf168e6609fbd02a507f35629373b85a48`. Imported serialized font/settings bytes are recorded as actual native results; they are not falsely required to equal pre-migration serialized archive bytes.

### Preservation and boundary

Before/after configuration arrays are byte-identical as identities and cover the complete current top-level **22 ProjectSettings/Packages files**, including manifestff1d5e0a..., lock7995c955..., settings4e14eb66.... Their current hashes match as well. All **1137 manual dependency files** still match the prior whitelist after this resource import. No Neo registration/Assets or Examples folder appeared.

The helper's unchanged exact f238 code passed NoNeo/NoPro registered, loaded-assembly, compilation-reference and payload guards before and after resource verification; final Editor compilation graph count is37. This supplies an actual **PRE-Neo resource-context** guard result. It does not certify the later Neo/sample compilation graph or every future loaded assembly. Compiled Both/DOTWEEN, serialized Both2, exact Short identity and D3D11 are invocation guards of the successful native path, not merely intended recipe labels.

The **own R2** main freeze contains5955 files; every one was independently matched to current bytes/SHA. Protected12 and public prefs51→51 also match without added/changed names, registry changes or private-value publication. The count includes the two reviewed temporary Module sources/metas; it is not retroactively presented as an unchanged5951-file snapshot. Only callback/session lifecycle cleanup is implied by the completed reviewed path; the physical owned helper/meta remain present for root's later evidence/cleanup disposition.

R1's CS0104/exit1/pre-entry abort and original failed log remain intact and **not PASS**. The Main Module producer's separate unsupported NUnit-attribute compile abort had0 tests and is not a consumer TMP failure. No Module performance metric is inferred here.

Root can continue the consumer preparation from these verified stock resources, retaining required helper cleanup/final reviewed package freeze and later actual Neo registration/discovery/sample/runtime/visual/Player/profile gates. No global closure is claimed. This acceptance used only readonly files/logs/receipt/source/archive analysis; no Unity, compiler, process, native load, host/configuration or source write was performed by this reviewer. Only this report was appended.
