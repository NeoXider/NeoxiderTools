# Native implementation review 14 — Spawner NI8-02, Draw consumer, Money NI7-02

Status: **SOURCE-FINAL, independent SOURCE-ONLY QA**. No additional confirmed source defect was found within the three assigned boundaries. NI8-02 and NI7-02 are corrected in the reviewed source; Draw consumes the source-final provider through exact rental identity and terminal automatic-source retirement. This is a static disposition, not a compilation, discovery, test, native-lifecycle, performance or remote-network pass.

Reviewer: SAME `/root/audit_abilities`, native `gpt-6.1-sol` / `max`, repository `D:/Git/NeoxiderTools`, target `demo` / `11.0.0`. Only this Markdown file is written. No product/public-doc/test/other-report edit, Unity/MCP, compiler/checker/test, service/process management, installer, child, commit or push was used. The global writer/execution barrier remains closed.

## Scope and stable-source gate

Completely read the latest three owner receipts listed in the identity manifest. Initial raw-file identities were captured at **2026-10-23T20:14:43.164310+00:00**. The post-read gate at **2026-10-23T20:25:01.494816+00:00** and final handoff gate at **2026-10-23T20:28:01.253413+00:00** each matched all **22/22** files byte-for-byte; no scoped source-final identity changed during that interval. Any later change invalidates the affected disposition until reread. Context-only files have a separate single-checkpoint manifest below.

| Assigned scope | Original owner/session | Actual review boundary and disposition |
| --- | --- | --- |
| Neo.Tools.Spawner NI8-02 | `neo-audit-1023-b08-03` | Current three-file deferred-construction/registration correction, probe changes, all ten added callback/positive-control cases and four updated guides. Earlier unchanged NI8 provider review is retained as phase evidence; unrelated Spawner behavior is not reaudited. **NI8-02 source correction supported.** |
| Neo.Tools.Draw consumer | `neo-audit-1023-b06-08` | Complete current `Drawer.cs`, every changed regression body and ordinary control in the 39-case fixture, both guides, existing fixture metadata and assembly-reference context. **Assigned reset/rental/material/automatic-source corrections supported in source.** |
| Neo.Shop NI7-02 only | `neo-audit-1023-b06-01` | Current shared Money positive-decrease predicate, authoritative/client branch ordering and Command budget boundary; all twelve added controls, relevant setup/cleanup and Money guide. The authority and Play fixtures were read completely for context; the older 44-case Shop regression fixture and broader purchase/authority work remain the separate original NI7 review. **NI7-02 source correction supported.** |

No Abilities, RPG, Inventory, Time, Demo or other consumer approval is implied. This is independent review of other original owners, not self-approval.

## Spawner — NI8-02 first-construction retirement

**Prior defect / expected contract:** retiring an exclusively owned source from its first prewarm create/get callback must reach the in-flight registration. The initiating factory request must return null, stop further prewarm, and never reinsert the retired pool. Previously, constructor callbacks ran before registration, so retirement could miss it.

The current transition is bounded and preserves ordinary construction:

1. `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs:23` keeps the public constructor signature and synchronous behavior. Its internal overload at line 30 completes pool initialization with callbacks deferred; `Prewarm` at line 59 acquires the configured set before returning it, stopping further acquisition when retired.
2. `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs:85` constructs with deferred prewarm, publishes the exact source registration at line 95, and only then calls prewarm at line 96. The post-callback return checks `IsRetired`; no registration assignment follows callback dispatch. Startup and dynamic creation share this helper. The retired-source marker prevents a duplicate startup entry from recreating the source.
3. `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs:181` follows the same order: registration at line 193, prewarm at line 194, retired result rejected at line 197. The public fallback Spawn path checks a null pool at lines 129–132 before retrieval. Unified retirement still marks/removes exact source identities and preserves storage roots.
4. Current `NeoObjectPool.GetObject` / `ReturnObject` / `Retire` retain the prior reviewed terminal behavior: an acquisition whose callback retired its pool is destructively returned rather than published; valid outstanding rentals stay active until late return; no new rental is created by a retired raw pool. The local callback contract still forbids returning/re-renting the transitioning instance.

The unchanged provider boundary was checked against its prior NI8 identity: `PooledObjectInfo.cs` remains `bcaacd8c…`. Its one slot rejects replacement/stale/foreign/returned registration, detaches the callback before invocation, and allows release/eviction/OnDestroy to consume it only once. Cached IPoolable release observers still run before registered-server Mirror unspawn, then generation cleanup, then storage/deactivation; retirement and deferred construction do not reorder that boundary. No claim of arbitrary throwing/reentrant pool callbacks is introduced.

**Authored regression review:** `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs:97` adds six real first-create/first-get paths across manager, fallback and configured startup. The shared body at line 614 captures the pool during its first callback, confirms retirement/removal, attempts nested acquisition, and checks initial/nested/later null results, one create/get, raw terminal rejection and no factory reinsertion. Startup includes the duplicate source entry. After a destruction boundary it checks zero retained native clones, while an unrelated exact source with the same name and shared root still works. That independent source also supplies the active outstanding/one-cleanup/destructive late-return control. Callbacks never return/re-rent their own transitioning object.

Three ordinary route controls at lines 133–147 and helper line 749 assert prewarm counts, create/get-before-release order, public retrieval and generation reuse. The direct public constructor control at line 151 verifies synchronous prewarm compatibility. The changed probe exposes first-create/get observation and clears static hooks before/after each case. All ten added cases were read, including setup/teardown. The receipt's **52 authored total** includes unchanged earlier fixtures; it is not a discovery/result count.

The four provider guides accurately describe first-callback null acquisition, exact-source retirement, acquired-prewarm cleanup after callback completion, preserved roots and ordinary constructor/callback order. They do not assert that a callback can return/re-rent its transitioning instance.

**Disposition:** no remaining NI8-02 source defect found. Real native destruction timing, startup callback delivery, domain-reload isolation and the existing Mirror release contract still require execution after the barrier.

## Draw — actual consumer, reset reentry and exact generation

The complete current runtime file and all 39 authored cases were read. Public signatures/serialized names remain intact. The assembly context confirms the documented Neo.Reactive, Extensions, PropertyAttribute, Spawner and Managers references; no new assembly wiring is needed.

**Reset and startup ordering:** `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs:324` increments a stroke version, clears raw/rendered geometry and collision, silently commits both metrics to zero, and then notifies changed Distance followed by PointCount. It stops a superseded notification after a replacement Begin. `BeginLine` at line 269 checks captured renderer/version after reset, first-point and timer callbacks; `Timer` at line 152 prevents a stale remaining-percent notification. Append stages geometry and both metrics before its PointCount/Distance delivery and rechecks exact state before later delivery or automatic End. End clears active state before publishing completion. These checks preserve ordinary successful stroke completion and callback-created strokes.

**Rental admission and all later instance access:** `CreateLineRenderer` at line 364 captures one private resource record before installing a material. `TryTrackLine` at line 761 registers cleanup against exact owner plus generation. If another participant already owns the slot, lines 382–387 return the untouched rental and use ordinary creation; the existing participant is not replaced. `CanAccessLine` at line 775/786 requires the same undisposed record, exact pool/generation and active rental, and rejects unknown pooled instances. Its uses were followed through input/follow, reset, append, end, current renderer/collider reads and writes, style updates, deletion and expiry. Generic static geometry helpers retain their independent public utility contract.

**Material release and stale deletion:** `ReleaseLineResources` at line 813 marks the old record disposed before clearing its tracking, cancels expiry, and silently clears an expired active stroke without user callbacks inside pool transition. It restores the borrowed material only when captured generation still matches and the installed material is still the exact owned clone, then destroys only that clone. It intentionally does not require `!IsInPool`, since valid cleanup runs during return. Caller replacements, borrowed materials, textures and sprites remain externally owned. `Delete` captures the resource record at line 859; `DeleteLine` at line 863 rechecks it after Reset callbacks before touching the renderer or returning through its actual captured pool. Bulk deletion at line 940 snapshots records and preserves a new listener-created stroke.

**Automatic source lifetime:** `OnDestroy` at line 198 blocks new drawing, cleans tracked and standalone acquisitions, calls unified retirement only for its unique private automatic source, then destroys that source. Assigned/shared pool prefabs are never retired by this owner. Shared roots are not destroyed, even if another exact source has the same name. External outstanding rentals retain their valid late-return contract. This is a bounded ownership use of the provider, not a global lease/reference-counting framework.

**Authored regression review:** `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` contains **39 authored cases: 34 synchronous plus five UnityTest cases entering Play Mode**, all unexecuted. The sixteen additions are meaningful public/native controls:

- Lines 150 and 191: settled reset geometry/collider/metrics/order and four Distance/PointCount replacement cases through Reset/Begin.
- Line 258: four first-point/timer/remaining/start replacements, asserting actual replacement geometry, timer and emitted start positions.
- Line 316: DeleteAll reset reentry retains the new stroke.
- Line 577: two active/completed two-Drawer cases free/restore the old generation before reissue, then exercise old append/reset/style/collider/end/Delete/DeleteAll and full old-GameObject destruction. The new generation's parent/activity/geometry/width/collider/material must survive.
- Line 641: Reset callback returns/reissues the captured renderer; the outer Delete must not return the replacement.
- Line 680: metadata Return and direct owner ReturnObject, capacity-one eviction, one exact clone disposal, borrowed-material survival and stale old-owner deletion.
- Line 713: **100 full owner-GameObject disposals**, checking terminal raw retrieval, removed registration, zero inactive objects, source/material destruction and native counts returning to baseline; a same-named assigned/shared source, shared container, active rental/material and later retrieval remain usable.
- Line 766: automatic-owner destruction preserves an externally outstanding rental, whose valid late return then destroys it while preserving the borrowed material.

Ordinary controls for geometry, fixed-plane endpoints, completion/reentry, template materials, pooling-flag changes, collider reuse, owner-component destruction, expiry and stale expiry were also read. Fixture cleanup deletes Drawers before pools, restores prior manager singleton/search state, assigns a camera before Awake without claiming MainCamera, and exits entered Play Mode. Existing metadata was read; no unavailable NonParallelizable attribute appears in this fixture. Isolated execution remains necessary.

Both Draw guides match the changed event order, silent external cleanup, exact-generation return/material policy, assigned versus automatic source ownership and one-slot fallback. They explicitly retain coroutine-owner/scene-unload and native material evidence debt.

**Disposition:** no confirmed source defect found in the assigned consumer. The occupied cleanup-slot fallback is traced and documented but lacks its own dedicated authored control among these 39 cases; retain it as acceptance coverage debt, not as a demonstrated runtime bug. Full scene unload, stopped coroutine owner, domain reload, shaders and physical/material destruction are also unexecuted. A hundred-cycle fixture source is not a measured leak/performance result.

## Money/Shop — bounded NI7-02 precision correction

**Prior defect / expected contract:** at stored balance `100000000f`, positive price `1f` is nominally affordable but subtraction stores the same balance. An authoritative spend must reject before events/save/sync/RPC/budget and cannot grant a paid Shop item. Ordinary representable rounding remains legal.

`D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Shop/Money.cs:570` now centralizes finite/sign, affordability and `count == 0f || balanceAfter < balance` admission in existing `CanSpend`. It is read-only. Authoritative `TrySpend` at line 279 uses that check before `SpendLocal`. Negative/nonfinite input keeps InvalidAmount precedence. An affordable zero-cost spend retains its previous path; an ordinary fractional/rounded positive cost need not exactly equal the stored delta. `MoneySpendResult.Amount` and LastChange remain nominal inputs.

The pure-client branch at lines 288–294 still precedes local affordability/effective-decrease admission: a stale client returns RequestedServerAuthority for finite nonnegative input. Server Command admission at line 711 reuses CanSpend before the short-circuit limiter at line 741. Consequently a denied zero-effective spend cannot touch either fresh/pre-existing peer budget or trusted timestamp, ExecuteOp, sync sentinel, or RPC. This preserves existing input caps and operation values; no new numeric backend or purchase acknowledgement is claimed.

The narrow real consumer trace is consistent: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Shop/Shop.cs:647` uses IMoneyCanSpend for CanAfford, and the existing TrySpendCurrency path at line 1177 rejects an unaffordable price before commit/grant. Shop's existing local client affordability precheck is unchanged and explicitly documented; this review does not reopen the broader Shop contract.

**All twelve added cases were read, not executed:**

- `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs:115`: direct CanSpend/CanConfirm/TrySpend/Spend reject price1 at balance1e8, preserving all four wallet observables, currency saves and wallet/save events.
- Line 134: four controls `10 - 0.1`, `1e8 - 5 = 99999992`, zero at 0 and zero at1e8. They assert nominal receipt/LastChange, stored balances, persistence, lifetime/level conservation and spendability above an income cap. No exact nominal-cost/delta equality is required.
- Line 168: four MIRROR-on generated server-body controls cover remote/trusted-null sender and fresh/existing budget. Rejection preserves wallet/save/events, sync sentinel, dictionary identity/content and timestamp; a subsequent rounded effective price5 remains eligible without a consumed limiter window.
- Line 205: two MIRROR-on active/connecting/not-ready client controls cover stale balance0 and zero-effective balance1e8. They expect the actual not-ready warning, retain pending status, send no packet, and restore connectState/ready in finally.
- `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs:84`: real Shop/Inventory bridge with price1, balance1e8 above cap100 and grant mapping3. It checks one ordinary failure, zero inventory/grant/purchase/wallet/save events, unchanged ownership/selection/profile/currency persistence and no observer fault.

The local Command-body helper requires an actual woven UserCode method and fails rather than silently substituting a reflection predicate. Current Mirror source supports the test's context assumptions: NetworkClient connectState/active/ready at lines 63–74, SendCommandInternal's early reliable not-ready warning at NetworkBehaviour lines 335–355, and MethodProcessor's UserCode substitution. These controls still bypass real remote dispatch and provide no network replication receipt. Save provider/static network state cleanup and ordinary offline/zero-cost controls were inspected. The current owner inventory **84 authored total / 59 outside MIRROR** is a receipt/source inventory, not this review's executed count or a renewed review of the older 44 cases.

The Money guide accurately documents zero-effective rejection, nominal versus stored rounding, zero-cost compatibility, the client-first wallet pending branch, retained Shop precheck, and remaining authoritative request/response limitations.

**Disposition:** no remaining NI7-02 source defect found. Actual weaving/discovery, MIRROR-on/off runs, authoritative server/two-client denials with persistence/events/replication/budget observation, host/null context and real Shop grant acceptance remain root-owned verification. This bounded correction does not approve unrelated Shop/Network behavior.

## Required acceptance and limits

All authored cases above remain **UNEXECUTED**. After every source writer finishes, root must compile/import/discover/run the exact frozen snapshot on the intended Unity versions and MIRROR configurations. Required focused acceptance includes the ten first-construction controls, all Draw 39 cases with native destruction/material/100-cycle/shared-root/outstanding-return checks, and the twelve NI7-02 controls plus separate-peer transport evidence. Also cover the documented cleanup-slot collision path. Earlier compiler diagnostics or old passes do not certify these identities.

Observed operations were sequential cmd.exe/Python UTF-8 source/diff reads, reference/method enumeration and raw identity bookkeeping. No tests, builds, validators or whitespace/package/meta/GUID checkers ran. Hash equality establishes reviewed bytes only. It does not prove behavioral execution, memory/FPS measurements, hidden callback safety outside the documented provider contract, full scene unload, or pending Draw/Demo/Bonus/Network consumer integration. No additional architecture or source correction is requested by this review.

## Frozen reviewed identities

SHA-256 is over raw bytes, preserving BOM/newlines. Initial and post-read values matched all 22 entries. Full paths are intentional.

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Spawner.implementation.md` | 15043 | `66f74b9b38daaa01a1bd61f5102ac60f65712ee96422afac3da7b7585c3dfbeb` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Draw.implementation.md` | 9559 | `923ec1b381224adce2e9b0b4e3a7187d1598a1fdb4b10543c1a7ed8959eacd4e` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Shop.implementation.md` | 14666 | `f05229778de4b8583c0ec915715199f138436ffa1a152e7d367ddc6131510203` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` | 10876 | `062d273781f826da1449da1d1b357547f81ebdc3ea340d7109ea90e7d2b85577` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` | 6474 | `8632e8f54ec4a01ca3c49168ce7f60f2f25536cb1f32dec137b981d45a19cd3c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/SpawnUtility.cs` | 9601 | `fe579c07db75db1ddc5f6459688d7b00283ca4bf74933a9212e9deb53556c851` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` | 3390 | `bcaacd8cf2e83c5c4abd766d9b43c6dd146dccbc9ddb74ecb7e5ba5b6371feda` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPlayTests.cs` | 36899 | `1f33cc69e686cdde2422fca313c090db95c1341332b76a9e721fa93942ac3f93` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerPoolProbe.cs` | 688 | `1fe9722c02a24ae8a0d58bf898fb71a83c2d5e0c21387f3a20ec000d2755b107` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/README.md` | 3434 | `f86a9e596d43f7b53222e2b09582eb4c00d1692e3d39a03965bd752c730a6d0e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/NeoObjectPool.md` | 5754 | `5a0e9376c2337f42d778d25918c2f6e89af046d71f0df29d58e4878bbf5fad38` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/PoolManager.md` | 4038 | `f6d845d3dfd8ce5d9a60c1560fe40f86ce3cbaa6e5d2762c6f7bf2c839290012` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Spawner/SpawnUtility.md` | 4543 | `91082c44249394d185c05ba0e877cb42221d3b2ec95efa49173743cbb7b3ddc7` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` | 37604 | `2916263743fb4fee8afcde27380280f71da13b9bc9afc28a3a2f5abb7f2083b3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs` | 39497 | `3f662bbf48647d2810f5fcb903560f801705890ef0187940c240353721823087` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsDrawTests.cs.meta` | 60 | `4fa65ed083053a191f39004a1782a127d71a2c3ba82c8ceb79a3a0226fb9b2b9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Draw/README.md` | 1110 | `7132b7f74cdb55648aeae5d6d5290c0037ef233fa5c25cfa6095a77569e081dd` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Draw/Drawer.md` | 11405 | `76bee0508b32edda47cb418154cfb623d1da2c7e0b6773d9a90dc0b07df29b3c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Shop/Money.cs` | 26257 | `ca141e9f3a54082f66130a6b6ce9877e1f91af136c2886ae630627585bacff0d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Shop/Audit20261023NeoShopMoneyAuthorityTests.cs` | 18670 | `aef82dbfa943cdeea0036f60c631ffa7fa6aa788831942912b29218ef114de1a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoShopCommitPlayModeTests.cs` | 16432 | `ac65ab8d7d08dfd14a98e85f58b248b7d6dfb2d7af6925317b4f16fff56643de` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Shop/Money.md` | 8906 | `3a2a46ac431ee432dece06e61b2af42acf5e551761d61191ec9d188dc21804bf` |

## Context-only read identities

Single post-read checkpoint. Prior NI8 is this same reviewer's completed full provider review; NI7 was consulted only for its NI7-02 finding. Draw asmdef was read fully. Shop, NeoNetworkState and Mirror files were read only at the relevant consumer/state/dispatch/weaving methods; hashes do not imply a full independent audit of those files.

| Full artifact path | Bytes | SHA-256 |
| --- | ---: | --- |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-8.md` | 31536 | `428bf4ad3b4698d410531c976190f60ba71ff5c022f4ead46dd549cec4dbff01` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/NativeImplementationReview-7.md` | 27925 | `1b85c059b89c6bc3a6526590c3a502b9fb6ef6fec188b26bc079c42becf48d1d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Draw/Neo.Tools.Draw.asmdef` | 544 | `ad8ad79ca8f6763945592417b07debd4000ce9a0961e6c345dc64c77f6656a5a` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Shop/Shop.cs` | 46094 | `29c932ce37396bf49dfb95c229a4e20d152f0bf33c62e738cc51ec07e3ab8d31` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Network/Core/NeoNetworkState.cs` | 7775 | `1e86b88bb98d9b6a1cffe0fb026e2905f7fe412ed0d3638fb8cf5a17e20156c6` |
| `D:/Git/NeoxiderTools/Assets/Mirror/Core/NetworkClient.cs` | 90697 | `2405e6205609e3f564e7401f90bc6de9708be3d157fee393f41b913c3e7671dd` |
| `D:/Git/NeoxiderTools/Assets/Mirror/Core/NetworkBehaviour.cs` | 65057 | `8ffa1e6bffc022dc593c2c07e293776c5e6f77c99e80a5fbfbcb7abfec5072f5` |
| `D:/Git/NeoxiderTools/Assets/Mirror/Editor/Weaver/Processors/CommandProcessor.cs` | 5908 | `98b9d391d8fbf3133926d2041a77605f646d5a166b9febb70a0e00be4c4b88fb` |
| `D:/Git/NeoxiderTools/Assets/Mirror/Editor/Weaver/Processors/MethodProcessor.cs` | 6026 | `2ebcd7d0d351cb066b0b9ea35624bf2beaa44c3dda8fbde51c7f4a20771ed04d` |
