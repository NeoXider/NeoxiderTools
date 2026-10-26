# Native implementation review 5

Status: COMPLETE read-only implementation review, 2026-10-24. Three source-final scopes inspected; two corrections remain below. This is not release/compile/test approval or approval of the reviewer's own Extensions work. No source/test/public-doc/config edits, Unity/MCP, compiler, tests, product checkers or child agents.

## Entry gate and receipt read record

All three receipts were read completely before inspecting implementation. Their exact statuses permit this review; no assigned module was skipped as still writing at intake. If a later owner correction changes the snapshot, it needs a new review receipt rather than inheriting this verdict.

| Module / original owner | Full receipt / SHA256 | Status at intake | Review coverage |
| --- | --- | --- | --- |
| Neo.Tools.Text / `neo-audit-1023-b08-04` | `Docs/Audits/2026-10-23/Neo.Tools.Text.implementation.md`<br>`05ae618d62790a1edee7da79523afcba9113f83ba3608d766f9ddaf132f9f2f7` | source-final for owned assembly/docs/regressions; derived consumers explicitly excluded | complete receipt/current sources/diff, both complete fixtures and three guides read; owned fixes appear coherent |
| Neo.NoCode / `neo-audit-1023-b04-06` | `Docs/Audits/2026-10-23/Neo.NoCode.implementation.md`<br>`2312a23832694b4ccf8d866cd208741f92c8c94e54e23dc9deab0bdae8108b1e` | source-final; Editor consumer is a separate handoff | complete receipt/current changed sources/diff, both new fixtures and four guides read; I5-02 remains |
| Neo.Quest / `neo-audit-1023-b05-01` | `Docs/Audits/2026-10-23/Neo.Quest.implementation.md`<br>`2790f883f04b193703bdf681400d553bec3b45921d3996b1458f8b165acaa0d1` | SOURCE-FINAL; Demo consumer explicitly excluded | complete receipt/QuestManager/QuestState/diffs, complete regression/probe and four guides read; requested runtime guards coherent, I5-01 remains |

Pending Level/Shop/TextScore/Demo consumer implementations are not approved here. Text exact integer completion/base hooks/mode atomicity, NoCode occurrence/cache/fallback reentry, and Quest restart/admission/late binary reconciliation/exact-instance unregister are the bounded focus.

## Confirmed findings

### I5-01 / P1 — Quest fixture reintroduces an unsupported parallelization attribute

Owner Neo.Quest / `neo-audit-1023-b05-01`. Current `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs:13` contains `[NonParallelizable]`; the source-final receipt also explicitly advertises it. The active review contract records that root removed this attribute because the selected Unity NUnit environment rejected it. Trigger: compile this fixture in that configured validation closure. Expected: fixture participates in nonempty discovery; actual source reintroduces the same known missing-attribute compile path. Smallest correction: original owner removes the unsupported attribute, retaining the exclusive/serial runner policy and all assertions. This is current source plus historical toolchain evidence, not a newly executed compile failure. No test or source was edited by this reviewer.

### I5-02 / P2 — validation erases FormattedText's notification baseline and repeats unchanged text events

Owner Neo.NoCode / `neo-audit-1023-b04-06`. `Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs:129`, ReconcileDirtyConfiguration, unconditionally sets `_lastText = null`. RefreshFromSource:363 then classifies the next identical formatted result as changed and ApplyText publishes OnTextChanged.

Concrete trigger: an active Reactive/Poll view has rendered ordinary Score0.25 as `0.25`; attach a counting OnTextChanged listener; change only Poll Interval in the Inspector and allow the next due refresh. The bound value and displayed string remain the same, but the listener receives another `0.25`. Assigning a new text target with unchanged output through Inspector validation has the same baseline loss. Expected under the documented value-change event contract (`Assets/Neoxider/Docs/NoCode/README.md:48`): refresh/rebind can write the current value without another semantic change event. Actual source clears the comparison baseline before that decision.

Smallest correction: preserve `_lastText` as the last notified/computed string, and use a separate pending-render/target-dirty flag when validation needs to force a write to a changed target. A changed format/source that actually changes text still notifies; unchanged text written to a new target does not. Preserve existing enable-time semantics separately. Add counts to validation and target-rebind regressions: same-string interval edit/rebind produces zero additional OnTextChanged, a later different value produces exactly one; retain the existing getter/polling/Once assertions. Current follow-up tests verify text/getter/subscription state but do not count these validation notifications. No execution is claimed.

## Module dispositions

### Neo.Tools.Text — bounded owned implementation appears coherent

Read complete SetText.cs and TimeToText.cs, their full baseline-relative changed hunks, both complete current audit fixtures and Tools/Text README.md, SetText.md, TimeToText.md. This review assesses the Text consumer of existing Extensions formatting, without reapproving that formatter's independent runtime implementation.

Set(int) widens the value+IndexOffset sum to long and delegates to SetInteger(long); the distinct additive name preserves existing overloaded Set method groups and UnityEvent bindings. The exact final BigInteger formatter is captured separately from approximate float interpolation/currentNum. OnComplete checks the exact owned tween before clearing it, installs the exact formatter before final text notification and suppresses only an identical completion string. Later formatting-only redraws retain that formatter. New Set/string/float/integer/clear writes kill the old owner, so an older completion cannot commit over the replacement. The reviewed reentrant notification paths have no trailing old formatter assignment after ApplyText returns.

Protected virtual enable/disable hooks preserve existing base tween pause/resume behavior. They do not take ownership of derived subscriptions or resolve deferred Level/Shop/Score sources. SetLink/OnDestroy and actual DOTween/native lifecycle behavior remain unexecuted; no derived consumer is approved by these base methods or by plain SetText fixtures.

TimeToText validates the candidate DisplayMode against CurrentTime before assigning it. Rejected Compact-to-Clock transitions therefore leave the old mode, value, display and event baseline; accepted transitions reuse the formatter. Set commits lastTime before OnTimeChanged, retaining the previous reentrant baseline correction. Existing fields/defaults/Set(float) and immediate BigInteger paths are preserved. Documentation explains exact final versus approximate intermediate/state, offset-once semantics, additive API, extra final notification when digits differ and base-call migration. Atomicity here is the typed DisplayMode API, not proof of every raw serialized Inspector edit.

The two full fixtures contain the receipt's 49 authored cases by source: 31 SetText and 18 TimeToText, including exact literal integer/offset/long endpoints, affixes/format redraw, no redundant exact completion, supersession/reentry, rejected/accepted modes and two real enter/exit-Play base lifecycle scenarios. They assert text/event/current state rather than copied arithmetic. Objects/owned tweens are cleaned and defaultUpdateType is restored in finally. DOTween.ManualUpdate is global manual-tween advancement, so the receipt's isolated-runner requirement matters; it does not preserve unrelated concurrent manual animations. The formatting test's SerializedObject would be clearer with a disposal scope, but no current runtime defect is inferred from that alone. Actual TextLevel/TextMoney/TextScore override and queued-animation acceptance stays with the original dependent owners.

Aggregate work remains per owned tween/explicit redraw; no shared manager, camera query or scene-search loop is introduced. Float interpolation can publish multiple intermediate strings before exact completion, as documented; exact final formatting is not an allocation/performance claim.

Disposition: no additional confirmed Text runtime defect found in this bounded review. Compile, all 49 cases, native tween callback order, real derived lifecycle, supported Unity branches and user-facing visual behavior remain UNEXECUTED.

### Neo.NoCode — occurrence and requested fallback fixes coherent; notification baseline correction remains

Read complete ComponentFloatBinding.cs, NoCodeFloatBindingBehaviour.cs, NoCodeFormattedText.cs and NoCodeBindText.cs, their changed hunks, both complete new follow-up/occurrence fixtures and their reused source-component declaration, plus NoCode README.md, ComponentFloatBinding.md, NoCodeFloatBindingBehaviour.md and GettingStarted.md.

The new `_componentOccurrence` int is additive/default0; its public setter invalidates cache and retains negative input for explicit rejection. Resolution counts only matching full-name/name component types in GetComponents order, skipping unrelated/missing entries. Out-of-range does not select index0; destroyed selected components invalidate and re-resolve the requested occurrence. Existing host-aware cache and explicit destroyed-root versus intentionally cleared-root semantics remain. A live selected component is not rescanned after unrelated composition/order changes until explicit Invalidate/refresh, accurately documented. No old flat Editor ComponentIndex is reinterpreted.

OnValidate now only clamps interval/marks dirty; source cache/getter work is deferred to refresh/active LateUpdate. Reconciliation precedes the polling decision, allowing ordinary/unresolved Reactive sources to keep fallback after Inspector changes. Once stays nonperiodic, Poll In Late Update=false retains manual refresh and one-time subscription reconciliation, and disabled manual reads do not subscribe. After ApplyFloat the latest TrySubscribeReactive result controls fallback; a Slider changing from reactive to plain detaches the old listener and allows a later poll. Public docs explicitly require refresh/re-enable for runtime descriptor rewiring and do not promise idle-source replacement detection or a scene watchdog. I5-02 is a separate existing notification-baseline inconsistency in the formatted wrapper, not a reason to revert the source/getter/poll fixes.

The 26 new source cases (15 follow-up, 11 occurrence) use actual binding reads, getters, ListenerCount, Slider.normalizedValue, TMP strings and JsonUtility descriptor roundtrips. They exercise valid 0/1/2, negative/large/missing selectors, addition/destruction, dirty config in Poll/Reactive/Once, disabled interval polling and real Slider callback rewiring. SerializedObject configuration is scoped/disposed, GameObjects are tracked and destroyed in reverse, and reflection supplies actual validation/LateUpdate/deadline seams. These are not native scheduling/Inspector/IL2CPP execution results. The unchanged 20-case fixture was only consulted for the shared support type, not claimed fully reaudited here.

Aggregate work retains the per-component unscaled polling gate and explicit throttled Find By Name fallback. Cached member resolution does not eliminate reflected value/getter reads on refresh; reactive reconciliation can read them more than once. No new scene-wide watchdog or shared query scheduler is introduced, and no allocation-free claim is accepted.

Disposition: requested occurrence/cache/fallback changes appear correct; I5-02 needs a minimal render-versus-notification distinction and regression assertions. No approval of the pending Editor drawer/Condition occurrence consumer is implied. Actual legacy JSON defaulting, component order/native add/remove, unscaled scheduler behavior, UI lifecycle, stripped reflection members and final assembly closure remain UNEXECUTED.

### Neo.Quest — requested runtime corrections coherent; fixture currently blocked

Read complete QuestManager.cs and QuestState.cs, all current QuestManager/State/FlowConfig/QuestNoCodeAction changed hunks, the complete 741-line current regression fixture and complete new condition-probe source, and Quest README.md, QuestManager.md, Saving.md, Code.md.

Load and successful late RegisterQuest call the same managed reconciliation: lists grow without dropping saved entries; known completed noncounter objectives get progress1, counters retain their progress, and unknown configs/status/flags are retained. Registration does not emit events or Save. First-registration-wins is preserved and documents unique stable IDs. The predicate-result check after every objective condition tests both original state identity and Active status before any credit, including false results, so a restart does not feed the replacement with an old predicate or iteration.

CanAcceptQuest checks absence before and after successful start predicates; the common admission helper avoids the former TryAccept double evaluation. Nested acceptance has one state/event sequence, outer failure and an explicit rejection reason. Exact-instance UnregisterQuest checks the first current live config for the ID and uses ReferenceEquals before removal; it does not reset state, save, destroy data or notify. Stale/foreign/null/destroyed references cannot remove a live replacement. Consumers must call it before destroying an owned config; Demo's actual disposal remains excluded.

The 44 authored cases retain old binary/counter/reentry/bridge/flow assertions and add 17 source cases: 7 late reconciliation, 3 ownership statuses, 2 predicate restarts, 3 acceptance entry points, availability and ordinary single-predicate admission. The public boolean probe clears its one-shot callback before nested evaluation; tests use the real ConditionEntry getter route and observe state identity, exact counts/order, flags/progress and byte-identical staged save JSON. This is stronger than invoking a copied admission algorithm. The fixture establishes a unique save namespace/disabled automatic persistence before activating its inactive manager once, then restores inactive condition context. It uses the ambient provider under that unique key and deletes that key; it is not an in-memory/provider-identity isolation claim. Owned root/configs are destroyed and already destroyed configs are skipped. An empty/exclusive QuestManager test world is required as stated in the receipt.

Documentation accurately distinguishes destructive Restart/Reset from registration release, positional saves and explicit condition evaluation, synchronous nested events and no automatic NextQuestIds processing. The new API preserves existing fields/save keys/format; no new registry/session framework was added. I5-01 prevents treating the fixture as compile/discovery-ready in the selected NUnit closure. Do not weaken its state/event/save assertions to address that attribute issue.

Quest evaluation/notification work remains explicit-call snapshot/list traversal, not a new autonomous Update or physics scan. Snapshot allocation and repeated GetState lookups are retained costs; this review does not claim measured performance or implement the optional registry redesign.

Disposition: the four requested runtime actions appear correct by current source reasoning; remove the unsupported attribute through the same original owner, then compile/run all 44 plus legacy Quest acceptance after the global barrier. Real JsonUtility/native lifecycle/reflection binding and Demo unload/replacement remain UNEXECUTED.

## Reviewed current-source fingerprints

The three receipt hashes at intake were read again at the end and were unchanged. These fingerprints identify the reviewed critical source state, not compiled output:

| Path | SHA256 |
| --- | --- |
| `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` | `75fb3241140c843043e40245214166e3da7e7f14b374595dac8826bc337fd748` |
| `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` | `57629403908197309e8d3672de81a6553f0e047ea0c10478a03fdd63d91bbbaf` |
| `Assets/Neoxider/Scripts/NoCode/ComponentFloatBinding.cs` | `079a9b301378e1e89e48ab072761e9f7bfb34a5eb68daf592a0e339085e8648f` |
| `Assets/Neoxider/Scripts/NoCode/NoCodeFloatBindingBehaviour.cs` | `4acf75fd3b115c037c63ad919e09f45d2da5809371a405b88b7cd929dd17b8ad` |
| `Assets/Neoxider/Scripts/NoCode/NoCodeFormattedText.cs` | `cf707d60d2b7bf76bce2eb0712ae7d21ee93e3df6c1bc855c57bbbb51ab92e50` |
| `Assets/Neoxider/Scripts/Quest/QuestManager.cs` | `35f34f807bcf2256ffa2816838592004bc0a31e5d71252f0fbb90d5adf8a3e8f` |
| `Assets/Neoxider/Tests/Edit/Quest/Audit20261023NeoQuestRegressionTests.cs` | `29542218f72780d903a9a9658429b8730e23b02b2c109fc2f39eeb6680c9315a` |

## Correction order / scope boundary

Root routes I5-01 to the same Quest owner and I5-02 to the same NoCode owner. Both are small corrections, not prerequisites for a new framework. Review their correction receipts/current deltas before carrying these source dispositions forward. Finish and independently verify actual Level/Shop/TextScore/Demo and Editor consumers through their own owners/QA, then root establishes global source freeze and runs the approved compiler/test/native acceptance. This report makes no claim that those still-pending consumers are complete.

## Execution boundary

Performed only source/doc/receipt/diff reads, reference enumeration and SHA256 bookkeeping. No Unity/MCP, compiler/build/test/product-checker/validator, installation/service, git commit/push or child agent. Only `Docs/Audits/2026-10-23/NativeImplementationReview-5.md` was written. Execution evidence remains UNEXECUTED until the global source barrier is lifted by root.
