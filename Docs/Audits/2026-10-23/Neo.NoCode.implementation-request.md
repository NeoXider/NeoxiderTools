# Neo.NoCode: approved implementation request

Original actor: `neo-audit-1023-b04-06`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

### EDITOR-COMPONENT-INSTANCE (P2; approved-source-correction)

Editor C1: serialized ComponentIndex exists but runtime chooses first matching type

Root decision: Condition: additive ComponentOccurrence and OtherComponentOccurrence; NoCode: ComponentOccurrence; all default0, count only matching type in stable GetComponents order. Editor exposes those exact fields after owner implementation. Preserve existing ComponentIndex/OtherComponentIndex UI fields; no reinterpretation. Missing/negative occurrence rejects explicitly, no fallback.

### NOCODE-VALIDATE-FALLBACK (P1; approved-source-correction)

OnValidate clears working Reactive fallback without scheduling reconciliation; process dirty before shouldPoll on main thread, Once remains nonpolling.

### NOCODE-REENTRANT-REWIRE (P1; approved-source-correction)

After ApplyFloat reactive |= retains old true after callback switches to plain source; use actual latest subscription state and enable fallback.

## Relevant full native sections

Source: NativeReview-C.md

### C-14 / P2 — Inspector validation disables NoCode plain-source fallback polling

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs:41–45` and `NoCodeFormattedText.cs:70–75` reset `_useReactivePollFallback`, but LateUpdate decides whether to refresh before reconciling it. With Reactive mode bound to an ordinary float, edit Poll Interval during Play, then change the float: UI no longer polls until explicit refresh/re-enable. Expected next allowed poll reflects the new value; Once remains once. Minimum main-thread dirty/reconcile before shouldPoll, no component searches/getters during OnValidate. Owner **Neo.NoCode / `neo-audit-1023-b04-06`**; plan F1 accurately records it. Test both wrappers by output rather than private flags.
Source: NativeReview-C.md

### C-15 / P2 — callback rewire retains obsolete reactive status and loses fallback

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs:181–201`, `RefreshFromSource`: `reactive |= TrySubscribeReactive()` after ApplyFloat keeps earlier true even when a Slider callback changes binding from reactive member to ordinary float, removing the subscription. No fallback is enabled; later plain changes stay invisible. Expected final binding's actual subscription status decides fallback. Use the current result after ApplyFloat (plus existing enabled/mode gates), test old ListenerCount zero and later normalized Slider output 0.75. Owner **Neo.NoCode / `neo-audit-1023-b04-06`**; plan F2 independently confirmed. No session extraction required.
Source: NativeReview-C.md

### Neo.NoCode — reviewed

Full audit/plan, all four changed source diffs, subscription/polling/target reconciliation bodies, complete twenty-case fixture and all four factual guide diffs read. Host cache identity, destroyed-root explicit clearing, overloaded-indexer rejection, disabled snapshot notifications and late text recovery are coherent. Existing public/serialized descriptors retained. C-14/C-15 remain openly recorded. Fixture owns/destroys local hierarchies and asserts actual TMP output/listener counts; SerializedObject/OnValidate drives authoring configuration, not sole outcome assertions. Actual LateUpdate/IL2CPP/TMP behavior remains unexecuted.

Aggregate work: reflection metadata is cached, values are read each refresh; poll cadence defaults 0.16 unscaled, scene-name fallback throttled by existing resolver. Duplicate formatted slots currently force more reconciliation/rebuild work because match walks occurrences while registration deduplicates; this is a measured-work candidate, not a demonstrated wrong-output bug. Reuse unique underlying source identities/handles within a session if implementing optional typed adapters. Do not add global per-frame scene scans or pretend float preserves large integer currency. Existing Neo.Reactive subscription handles suffice; a new reactive framework is unnecessary.

Full read fingerprints for Neo.Tools.Text:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Text.md` | `bdbb0b481c595df7263a434087dd99b4475e44f680a3e8d51b9e5e2f2bee7280` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Text.plan.md` | `98b50a241ea2d654d349a7cac5949520abd2223b57eb16b2425ab976fbf561d6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/SetText.cs` | `4fdc99bce57556d745c80c88dd2b8e38c96f3e84480927402940f31d276ae51f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` | `aaa54f5b448a36fe641fbb3cb1c7027c7249539d01df01fa87072125a64cc2b8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextSetTextTests.cs` | `572b1b8c95c4238300eeb2870feba8559bd4ec347f37fb8092183b32811cfc25` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/UI/Audit20261023NeoToolsTextTimeToTextTests.cs` | `bceb9b2829c59b40afc0939cbc74bc701d14e126a5ea5a853f1a67d6c2366297` |

Full read fingerprints for Neo.Level:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Level.md` | `63d18bb0b9792d652c5a5ada1610b791178bb8310a6e326140d5c28f52067816` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Level.plan.md` | `12d8236491fa74c08684da28a8a8681bd35e268609db2a06c4471a242b76541f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/LevelButton.cs` | `813fa098f2c54249a023e350215fe42ce89a3e1c10fbc8d77ed91947b12d8a17` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/LevelManager.cs` | `7fc63bc89138c80f1079a458d6c89181a08aafb8267c9295fbf3e9be930c7ed9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/Map.cs` | `1be669416b0649772e8b4fe6721c168c7d386025668ab5beb90ef6614701dbaa` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/SceneFlowController.cs` | `1a3a837c215dc2b7f74417c86866206b0efdd0da6870b03781bc0a7503875218` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Level/TextLevel.cs` | `e8065edec2b4eb47dc7d7e6567b859a000e55a08949adb76699df42752f3d79f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelLifecycleTests.cs` | `6485beabff7f34ba99df354ade11f6ba6096245ba318c2f2d416fe2ed337fd96` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Level/Audit20261023NeoLevelProgressTests.cs` | `fb2b19da011955c7c6b10badf9424d262b60f3535380b631b64a39cbc0455adc` |

Full read fingerprints for Neo.NoCode:

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NoCode.md` | `498aec807925a9b0a0af8a4125c4085172cdd39dcfbf3d214f50457455c8b023` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.NoCode.plan.md` | `c029f700d2d55fa8a117f0e1a3b2fee4f8a54336c4fac42b251540af503e1b32` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/ComponentFloatBinding.cs` | `bb431ab10d852b66337afd111690e4ea0022effbb10c85ad56b40c632b1e5df3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeBindText.cs` | `85652c9eac823cd2ae8ce5a1ef7824e649b26870c0b6d69fbf2c928b3b33deb5` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs` | `188879bfa122489d223492bb5e7607123893756859a32def66473711b1ec5294` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs` | `b7dc14e974ff94a492d785d74df01a27edc75762c4df03fa8c32513e77d7a481` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/NoCode/Audit20261023NeoNoCodeBindingTests.cs` | `4c7f719a8d9d6b162f0b0c1ae6d052ad7b2e61a241296e6ede46472f2c26adc4` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.NoCode.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Final NI4/I5 correction handoff

ROOT CONFIRMED I5-02/P2. Read FULL NativeImplementationReview-5.md. NoCodeFormattedText.ReconcileDirtyConfiguration clears _lastText on validation; this repeats OnTextChanged for identical output after PollInterval edit or target rebind. Keep _lastText as notification baseline, separate pending-force-render/targetdirty state to write a new target without duplicate semantic event. Real changed source/format still notifies exactlyonce, preserve enable-time semantics. Extend actual validation/targetrebind tests with notification counts and later changed-value positivecontrol, retain getter/polling/Once assertions. No framework/otherowner edits; source/docs/receipt; no execution.

## Source3 released caller NOCODE-POST-RELOCATION-LIFECYCLE

Continue SAME original actor/session gpt-6.1-sol/max. General execution/freeze remain FALSE. NO Unity/compiler/test/checker/install/service/children/commit/push before ALL writers finish/newfreeze. Primary b03-04 has SOURCE-FINAL completed its54 helper relocations; root FULL-read dedicated21-line audit/17-line plan/112-line mapping and source-final declaration. ROOT NOW RELEASES ONLY your original module fixture CALLER files for the bounded behavioral correction below. The existing moved helper files under Tests/Fixtures, shared asmdefs/refs, original GUID/type mapping and extraction stay PRIMARY-EXCLUSIVE; do not relocate/recreate helpers or undo aliases. Read current source after relocation and actual source3 XML before patching. No production API/friend/serialized settings changes just to hide a fixture precondition. Retain every exact substantive positive/negative/count oracle, no blanket Ignore/expected-error/no-op/private fake lifecycle/ExecuteAlways/manual Update as native acceptance. Actual native callback claims require supported top-level Play transitions or real PlayerLoop/physics; pure public C# contract tests must say what they cover. Preserve UTF8/no-var, Samples~ and protected DocsEn.meta; no314 migration targets. Update honest own source receipt including deferred closure and stop. Proposed correction is unexecuted until later native run. Own NoCodeBindingTests caller is released. Helpers now runtime-compatible. Source3 E59/E60 fail OnValidate SendMessage no receiver/ShouldRunBehaviour; establish genuine serialized authoring validation/public contract or top-level admitted lifecycle. Retain all exact target replacement, same-text write, getter/format/listener/reentry/lazy resolution assertions. No fallback no-op DontRequireReceiver/error whitelist. Moving helpers is not permission to change field semantics.
