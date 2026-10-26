# Native implementation review 19 — Input / Components / View

Status: **COMPLETE — source-only review; all execution UNEXECUTED**. Same native `gpt-6.1-sol` / `max` session. Only this report was written. No product/tests/public-doc/config/other-report edits, Unity/compiler/test/checker execution, services/process management, children or commits.

## Intake / source-final gate

All three final implementation receipts were read in full. Input and Components were admitted first: recorded launch/recovery PIDs Input 26008/92148 and Components 98876/35608 were absent. View's receipt was absent on first intake, then appeared SOURCE-FINAL while recovery PID 100332 remained bash.exe; only its receipt was read at that checkpoint. View source was admitted after both 80736/100332 were absent and its receipt hash remained unchanged. Root was notified at both gates. At the closing process enumeration, **2026-10-23T20:55:52Z**, all six recorded PIDs were absent. This is an exact recorded-process/source-stability gate, not a claim that all repository writers have finished.

Receipt and reviewed runtime/fixture hashes were stable between intake/read and closing SHA reads. Source deltas were inspected against base `ca198556`. Old native reviews remain historical evidence; this report does not rewrite them or imply their earlier checks ran against this snapshot.

| Scope | Disposition |
|---|---|
| Input I11-01 bounded recheck | **Source-closed.** Faulted Release ownership survives; successful Release clears it; old tails cannot erase a replacement gesture. Prior NI11 coverage remains in force at the unchanged hashes below. |
| Components Counter A26/A27 | Revision fences and authoritative relative operations are coherent. **One required P2 correction remains: I19-01**, remote numeric admission before sender budget. Root independently confirmed and accepted the bounded repair. |
| Components TextScore A28 | **Source-consistent.** Consumes protected base lifecycle hooks and typed integer Set; meaningful component/GameObject disable controls exist. Root's missing Play assembly reference correction is now present. |
| View A29–A32 | **Source-consistent for this phase.** Materialized replacement, complete received snapshots, pre-budget admission and wide index arithmetic are coherent. No additional confirmed defect found. |

## Confirmed correction: I19-01 — P2, Counter admits non-finite remote mutations

**Original owner/task:** `Neo.Tools.Components`, `neo-audit-1023-b06-05`; return to the SAME original owner. Current locations: `Assets/Neoxider/Scripts/Tools/Components/Counter.cs:474` (`CmdSetValue`), `:485` (`CmdApplyOperation`), `:363` / `:382` (`ApplyDelta` / `ApplyFactor`), with raw candidates at `:373` / `:392` and Int rounding at `:376` / `:395`.

**Concrete triggers:** A network-enabled Float Counter has finite value 10. A remote sender submits absolute Set(NaN/+Infinity/-Infinity), or known Add/Multiply with one of those operands. Alternatively, the stored value and operand are both finite but their raw float arithmetic overflows, e.g. `float.MaxValue * 2f`. Both Command ingress paths reach `RateLimitCheck(sender)` without checking numeric payload/candidate validity. The relative path then calculates and potentially rounds the result. A known operation is whitelisted, but numeric admission is absent.

**Expected:** Invalid remote numeric input/result is rejected before budget consumption and before value, persistence, notifications or replication change. A finite absolute Set can repair a previously corrupt non-finite stored value. Ordinary finite inputs and Int rounding retain their behavior.

**Actual source:** The invalid request can consume the sender budget and poison the Float Counter, reactive/typed notifications, saved value, SyncVar and outgoing RPC. Finite-operand overflow also reaches mutation. In Int mode, `Mathf.RoundToInt` can turn an invalid raw float candidate into a finite integer representation, so checking only after rounding does not establish valid admission. A following legitimate request from the same sender can then be throttled. The exact engine conversion of non-finite floats is not relied upon for the Float-mode defect.

**Minimum correction, accepted by root:** Keep validation confined to remote Command ingress. After local-only/known-operation rejection, require finite absolute input; for Add/Multiply require finite operand and the **raw float candidate against the server's current value**, before Int rounding. Reject before `RateLimitCheck(sender)`. Do not require the old current to be finite for absolute Set, which is the repair path. Preserve direct public server/host/offline behavior, cooperative `requiresAuthority = false`, operation whitelist, division/rounding semantics and normal receive ordering. There is no clamp in this Counter; a new clamp or general numeric/authority framework is unnecessary.

**Required authored acceptance, UNEXECUTED:** Use the actual Command receive/woven path already present in the owner's network fixture. Cover NaN and both infinities for absolute and relative requests; cover finite operands with overflowing raw result for Add and Multiply, including an Int-mode control that would otherwise hide invalid arithmetic through rounding. For every denial assert unchanged current, SyncVar/initial serialized state, outgoing RPC payload/counts, reactive/typed/repeat events, save value/write count, and sender budget by an immediate valid same-peer request. Keep ordinary finite Add/Multiply, Int rounding and per-peer positive controls. Include finite absolute repair from a corrupt old value. Preserve exact assertions and task-owned save keys; these controls must not become empty or permissive assertions.

Root's independent source read and acceptance cover both missing payload and candidate checks. This report does **not** claim the correction has landed.

## Input I11-01 bounded closure

Read full `RootImplementationReview-I11.md`, the current exact Release block in `MouseInputManager.cs:163–199`, every new regression body in `Audit20261023NeoToolsInputBehaviorTests.Dispatch.cs:480–715`, relevant helpers, and the complete manager guide. The current block sets pending release ownership before dispatch and clears it only after both Release paths succeed. The success/version-qualified finally does not clear a newer gesture's pending state. An observer exception still propagates synchronously; no exception-isolation framework was introduced.

All four new expanded cases have substantive state oracles: ordinary Release/ReleaseIn fault controls retain the thrown exception identity and actual effect-follow target; manager-only disable after removing the faulty observer emits one cancellation and stops the effect; successful ordinary Release completes normally; a replacement Press plus faulted Release during an older Click retains the new ownership through the old finally. Coordinates, follower/trail state, cancellation/spawn/start/stop counts, recovery and repeated-loss behavior are asserted. Previously accepted replacement/version/subscriber behavior was retained; `MouseEffect.cs` and the main fixture are byte-identical to the prior reviewed hashes. The 76 total is an authored count, not discovery or execution evidence.

The guide accurately describes retained incomplete Release ownership, synchronous faults, normal success and replacement protection. No additional Input correction is requested by NI19. Native input backends, warm-cache/GC behavior and all prior execution debt remain unexecuted here.

## Components coverage and acceptance

Read the complete Components request/receipt, all 580 lines of Counter and 94 lines of TextScore, full Numeric/Binding/Network fixture bodies, protected SetText lifecycle/type context, scoped runtime deltas, and full current Counter/TextScore/Components/CameraAspectRatioScaler guides plus the legacy AttackSystem backlink. Camera preview/render-texture source paths were checked as documentation context; the other earlier runtime fixes remain accepted historical scope.

- Counter's relative client paths send Add/Multiply plus operand before reading the client balance; signed subtraction and reciprocal division retain their overload rules. Absolute Set dispatches before local equality suppression. Server evaluation therefore preserves sequential deltas from independent peers.
- `ApplyValueLocally` increments a mutation revision and checks after reactive notifications, typed notifications and each repeat invocation, then before save/replication. A nested Set/Load suppresses the old tail. Already-running UnityEvent batches keep their captured argument; no arbitrary batch rollback is claimed. Load refreshes server snapshot state, host RPC does not apply twice, and registry release uses the captured key.
- TextScore overrides both protected base hooks and calls them. Disable stops its acquired wait and disposes its exact subscription; re-enable resolves and binds again. `ReactivePropertyInt.Subscribe(Set)` and initial `Set(CurrentValue)` select the integer overload; final exact display comes from the separately owned SetText contract. No new TextScore float conversion was introduced.
- The six new expanded Numeric cases cover reactive/typed re-entry in both modes, repeat supersession, saved final values and an ordinary notification-order positive control. The two Binding cases start a real partial tween with one subscription, disable the component or GameObject, verify frozen text/value beyond the original tween duration and zero owned subscription, then re-enable to the latest score with one listener. They preserve the base animation option.
- The six new Network cases cover direct/woven absolute re-entry, all resulting RPC payloads, actual registered receiver dispatch and initial serialized state, authoritative sequential operations, and invalid-operation/local-only requests followed by a valid same-peer request. Existing host/snapshot/load controls remain. Private received-body reflection is not substituted for these paths, but this remains in-process evidence, not independent remote transport.
- Cleanup uses owned objects/transports, unique save keys and captured singleton/RNG/time/provider settings as applicable. Network fixtures require the exclusive isolated runner. No foreign session, global save deletion or unrelated object cleanup is authorized. The 46 total cases and 14 new cases are source counts only.

At intake, `Neo.Tests.Play` lacked its direct `Neo.Tools.Text` reference needed by the Binding fixture's inherited-type/reflection use. Root owns shared assembly configuration and supplied it during this review. The final actual asmdef contains the reference (`a7c2ed5d…` below); **the integration dependency is source-resolved**, with compilation still unexecuted. Edit assembly references for Components/View/Input/Text/Network/Contracts/Reactive are also present.

Docs describe float-backed precision, direct reactive-output bypass, cooperative network policy, authoritative relative operations, callback ordering, exact subscription cleanup, base lifecycle policy, render-texture dimensions and editor preview opt-out accurately. The bounded I19-01 remote finite contract will need its corresponding Counter guide/receipt update by the same owner; a broad exact-integer backend, wallet permission policy or transaction framework stays a proposal.

## View coverage and acceptance

Read the complete View request/receipt, all 1948 lines of Selector and 575 lines of SelectorModel, both new fixtures in full, both retained audit fixtures in full, complete phase guides (View README, Selector and SelectorModel), and scoped runtime/doc deltas. No View source was read for approval while its recorded recovery wrapper remained present.

- **A29:** Both collection setters materialize a deferred replacement before clearing; direct self-alias remains a no-op and null clears only the requested set. Throwing enumeration preserves that destination. The deferred filtered-set tests follow replacement with a real random choice, so they test behavior as well as collection contents. `Configure` is correctly documented as a separate, nontransactional whole-call contract.
- **A30:** `ApplyReceivedNetworkSelection` at Selector393 stages index, fill and exclusions, applies ordinary selection without publication, applies complete active snapshot, then publishes once. Both initial state and RPC delegate to it; the RPC now carries exclusions with active state. Selector-level selection/object/save/count observers run after the snapshot. Per-item Unity/SelectorItem activation callbacks can observe intermediate item changes; docs explicitly disclose that boundary.
- **A31:** Both protected selection/random server cores require network/server/known-authorized policy, valid bounds and applicable input before `RateLimitCheck(sender)`. All four exclusion Command paths also reject local-only/unauthorized policy before sender budget. `NeoNetworkState.IsAuthorized` rejects unknown policies before trusted allowance; None/ServerOnly/OwnerOnly and exact null/host behavior remain. Exclusions retain their existing arbitrary integer-set/CSV semantics; outside-range entries and empty clearing are not newly invalid payloads.
- **A32:** Long bounds, wrapping, step arithmetic and inclusive enumerators handle `int.MaxValue` singleton and `int.MinValue` empty-bound cases without overflow. Component Count/IndexOffset setters validate representable bounds before assignment. Raw `IndexWithOffset` is deliberately checked; effective lookups clamp widened sums. Existing field names/defaults, manual selection/exclusion semantics, public methods and save suffixes remain. Private RPC/Command additions require matching 11.0 peers and are documented.
- All 16 new Edit cases were read: deferred replacements, throwing/null controls, extreme wrapping, max-bound singleton random/unique/step/enumeration, min-bound previous/empty cycle, wide effective vs checked raw sum, and model/component rejection preserving previous valid state. They include actual selected objects and active state, not only private intermediate fields.
- All 13 new Mirror-conditioned Play cases were read: denied owner/peer requests, per-peer accepted cooldowns, immediate valid recovery after local-only/invalid-index/disabled-random/unknown-policy requests, all named trusted-null controls, received nonempty active/excluded snapshots in GameObject and SelectorItem modes, and ordinary local fill event order. The snapshot cases inspect final active/excluded sets inside each Selector-level callback and exact selection/object/count order.
- The ten retained Selector and eleven retained Lifecycle cases keep offset/remove/child-refresh, empty selection, save-before-Start and real nonempty star/light/material controls. The private memory provider is restored; spawned objects/materials belong to the fixture. New network setup refuses a foreign session/manager, captures transport/RNG and shuts down only its owned server.

The new network fixture calls the production protected cores on a spawned server identity and uses the real per-connection limiter with a 1000-second test override. `ReplayInitialState` exercises the shared application path using assigned incoming fields. These are useful source-authored controls, **not Command weaving, independent-client transport or actual remote receiver evidence**. The 50-case total is authored only. The separate-peer harness and full frozen-snapshot execution remain with their original owners.

Selection/model access still materializes/copies sets and enumerates pools; the wide arithmetic correction is not a per-frame/zero-allocation guarantee. Huge virtual-pool allocation/cost remains disclosed in SelectorModel docs. No mandatory registry/controller/framework or new scene wiring is proposed. No additional bounded View correction is required by this review.

## Read artifacts and SHA256

Paths in this first table are relative to `Docs/Audits/2026-10-23/`; every listed receipt/request/contract was read completely. NI11 is retained prior-session coverage and was not rewritten.

| Artifact | SHA256 |
|---|---|
| IMPLEMENTATION-CONTRACT.md | `d5bc7a2d934e20f40647c45d53746c3734a1dbbfe95f182600d66977e9bbcbab` |
| NATIVE-REVIEW-CONTRACT.md | `c1bb559c242ee21a44542abacc013cba2f7955d64c154350a4f336cbd11f1edd` |
| Neo.Tools.Input.implementation.md | `bab82632a7c9b8489e914cbf14eeeb7ebe08978b52b69d34dc50cc8685b6ec16` |
| RootImplementationReview-I11.md | `8b90f706a49dd5cc97b12512289ac5382b95eb13dffbb30dc33b19329610c75c` |
| NativeImplementationReview-11.md (retained) | `2c35b60808b3f95224d4dcfa1e6f612c5437e6b3ef2f83a250bca62b8858aae9` |
| Neo.Tools.Components.implementation-request.md | `1a36251743b66298a37cd8c38420cd364edf2222746f6ca3c7f7e30d7c23d459` |
| Neo.Tools.Components.implementation.md | `8650b4ccb12f5c6d59684348e8edb3dd38730ecf1486e450cad50b6f4788e90b` |
| Neo.Tools.View.implementation-request.md | `0dc4122a683af72863411431dafe7c2e54ccb20dd92fa8f1236975cba807f299` |
| Neo.Tools.View.implementation.md | `976d42ab54f1ac088c4bb8cf0dcb3ed563c3f2fbd2ecaf2bd559a259faa5ed76` |

Paths below are relative to `Assets/Neoxider/`. Full source/fixture coverage is described above; dependency hooks/camera/network/assembly files are context reads, not a reopening of their whole owned scopes.

| Current file | SHA256 |
|---|---|
| Scripts/Tools/Input/MouseInputManager.cs | `2bbd14b2d9227b9d9ae4709b9078dfddf570226bbcfa96199a8396a0bed17d70` |
| Scripts/Tools/Input/MouseEffect.cs (unchanged prior NI11) | `13ef2a08f1f06cbd4b0997033a2a77c7635663e03f51d061632c7d67e68296a9` |
| Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.cs (unchanged prior NI11) | `0c13bc41fc6c1045efc4f1210ea355abdbc7319c50ec0b869df3ad4d1673b595` |
| Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.Dispatch.cs | `cece96c21a4fa83944f82910f11c431e6bdbe8c2f2642b708d58e7e7934eebf0` |
| Docs/Tools/Input/MouseInputManager.md | `3b6c63d513c65889a0244f2dac91cce2951d9d8dc5b34dc1731632aa0aae18ea` |
| Scripts/Tools/Components/Counter.cs | `88a947d160f242049555b73ef9fcd50043bb469b41806838c0798bd6999348d9` |
| Scripts/Tools/Components/TextScore.cs | `2d87c77e58b1beb0dcda35586d84194c2c7fe3d63c268b0078c907b9181db41e` |
| Scripts/Tools/Text/SetText.cs (dependency) | `75fb3241140c843043e40245214166e3da7e7f14b374595dac8826bc337fd748` |
| Scripts/Tools/Components/CameraAspectRatioScaler.cs (doc context) | `e029409abcb086d0124d36a773055373a9637b1656d4082845163047dd2289f5` |
| Tests/Edit/Tools/Audit20261023NeoToolsComponentsNumericTests.cs | `5ae3056eb9d963fca2ff9236f608cc19eda360fd7ffed8ad5da969d7983fe7b2` |
| Tests/Play/Audit20261023NeoToolsComponentsBindingTests.cs | `312e2aa3af5c4cbcfca04a63778de2dada38542dd615516245417358d75cf398` |
| Tests/Play/Network/Audit20261023NeoToolsComponentsNetworkTests.cs | `b6ce63563aa424f872fa4cf2e0efb2c84ef170ef625764b52daffed47258c0e4` |
| Scripts/Tools/View/Selector.cs | `8117a7719e9f3808a7c48e0afc1f2bfeeed4336c106957d0d133b618a6fc3d15` |
| Scripts/Tools/View/SelectorModel.cs | `45928c9c1ab01181de097ce5ca72d3614eabf6015d80c7a051a6fd87619d116e` |
| Tests/Edit/Tools/Audit20261023NeoToolsViewSelectorInputTests.cs | `d634f105b755ebc2ae76c1474d36ff6106f4334425fdbfc7ecc0af8694e24793` |
| Tests/Play/Tools/Audit20261023NeoToolsViewNetworkTests.cs | `9c6cfbd416ab4451e385ddb76ae5b5baba50be8f6d933e85a652f268b9dc2422` |
| Tests/Edit/Tools/Audit20261023NeoToolsViewSelectorTests.cs | `fe8eadd9c11654ac7e1a3ed6401df18b29fdaee7850a07285667a5d64c4dfecd` |
| Tests/Play/Tools/Audit20261023NeoToolsViewLifecycleTests.cs | `10d5fbde3917a8a62a0469d266071ad43fd0695c735572742891be1dcbf39d4a` |
| Docs/Tools/Components/Counter.md | `d16090d7a7463f98d28c41bbfb32a582aa1bcddc20d01cf24df60ca2466059d9` |
| Docs/Tools/Components/TextScore.md | `bc0ab8897e205309aa415bdcc2c234e9d5340bd493d0a02e0e2538d922f611ef` |
| Docs/Tools/Components/README.md | `3573265870975264fcf7f13838fdc6c9cecdd7dbf1de71f09321f2d428d6288a` |
| Docs/Tools/CameraAspectRatioScaler.md | `65e728b5d8ebb7616c83647ac107dc0ad2a2192126917adc095c2d92fa2fd161` |
| Docs/Tools/Components/AttackSystem/README.md | `f3ec12f26ab786f0e8221b96171b70b116fd4003c26e5b3c3e3035f2a850b451` |
| Docs/Tools/View/README.md | `5531b907ddbbe986aa4aea3be53b273a847bcca221b4a887589f40886c5bf86f` |
| Docs/Tools/View/Selector.md | `c182839fe28928ab4bb3b131b339024a8b29e82af042bfe83534d1aeb75751b5` |
| Docs/Tools/View/SelectorModel.md | `58c7be0fb2a2435f43690b8e31c93c8b2cf85c45e9b49749ea1143a5372b412b` |
| Scripts/Network/Core/NeoNetworkState.cs (dependency) | `1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6` |
| Scripts/Network/Core/NeoNetworkComponent.cs (limiter context) | `45012ee0df1c54ffe20ae7b2786c33daf1c0ad57f81a0bd36f62ec6bf6913e60` |
| Tests/Play/Neo.Tests.Play.asmdef (root-owned integration) | `a7c2ed5da43c940c4757a507ab1c07bb10ff992f66c001177ce0c8a03a429a9d` |
| Tests/Edit/Neo.Editor.Tests.asmdef (visibility context) | `204a5ba6382792131c48cae74f38e27f5431909c0c0866c3b2905be0f2c4b4a5` |

## Execution boundary and handoff

All compile, NUnit discovery/execution, native tween/input behavior, Mirror-on/off branches, independent server/two-client transport, package/docs/meta/UTF-8 validation and installed Unity version acceptance are **UNEXECUTED**. No mock count, local host, protected-core call, source case count or historical pass is promoted to current native acceptance. The other source owners and execution barrier remain under root coordination.

Handoff: return I19-01 plus the bounded denial/repair/ordinary controls and Counter documentation update to SAME Components owner. Input I11-01 needs no additional source correction. View's reviewed phase needs no additional correction. Large numeric/storage/performance frameworks remain proposals; pending other consumers/harness scopes are not approved by this report.
