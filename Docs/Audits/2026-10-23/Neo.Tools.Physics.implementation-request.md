# Neo.Tools.Physics: approved implementation request

Original actor: `neo-audit-1023-b08-01`. Target `demo` / `11.0.0`. Root has read your complete audit and plan.

Read `IMPLEMENTATION-CONTRACT.md` first. Do not execute tests/build/validators during this phase.

Keep nearest-assembly ownership. Finish bounded confirmed fixes and owned canonical documentation/backlinks. No optional broad framework is required.

## Assigned review actions

No additional confirmed runtime defect was assigned after independent review. Preserve your coherent earlier patch. Finish canonical docs/navigation and factual API/scene/NoCode contracts in owned scope, keep at least one substantial improvement described as a proposal, and write an honest source-final receipt. Do not manufacture a runtime rewrite or duplicate tests.

## Relevant full native sections

Source: NativeReview-C.md

### C-01 / P2 — ImpulseZone dereferences its destroyed emitter after an enter callback

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs`, `ApplyImpulse`, lines 214–222; `CalculateDirection`, line 245.
- Trigger: a valid target enters a zone; its `OnObjectEntered` listener immediately destroys the zone component/GameObject (for example `DestroyImmediate(zone.gameObject)` in an EditMode/public-event driver). The target collider and Rigidbody remain alive.
- Expected: the cancelled/destroyed zone returns without force or a later applied event. Actual source path: the guard at 217 checks only the target objects, passes, then `CalculateDirection` accesses the destroyed component's `transform.position`, causing a missing-reference failure. The new target-destruction fixture does not cover emitter destruction. This is not a claim about deferred `Destroy` timing.
- Smallest repair: add an emitter Unity-null/lifetime check after the callback, before any emitter-backed access; retain the in-progress `finally`. Add a public event regression observing zero force/applied notifications after emitter destruction. Do not silently change whether explicit manual calls on a merely disabled live component are supported.
- Original owner/task: **Neo.Tools.Physics / `neo-audit-1023-b08-01`**. No cross-assembly change is required. Related callback destruction in the other two components needs a source/driver check rather than a blanket rewrite.
Source: NativeReview-C.md

### C-02 / P3 — Physics entry overstates center-of-mass direction for the native explosion mode

- Current location: `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Tools/Physics/README.md`, paragraph beginning “Each explosion or magnetic query”; source `ExplosiveForce.Explode` at `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs:220`; detailed `ExplosiveForce.md` Force Mode row.
- The entry says radial direction and custom falloff use center of mass. The native `AddExplosionForce` branch delegates direction/application to Unity; the detailed page correctly distinguishes its closest-surface direction from AddForce's center-of-mass direction. Expected documentation agrees between entry and detailed guide.
- Smallest repair: qualify that COM direction applies to AddForce/magnetic evaluation, while custom falloff uses COM in both modes. Keep runtime behavior/settings and the numerical regression unchanged. Owner: **Neo.Tools.Physics / `neo-audit-1023-b08-01`**.
Source: NativeReview-C.md

### Neo.Tools.Physics — reviewed

Original owner/task: `neo-audit-1023-b08-01`. Full audit/plan, all changed source hunks and all three current component bodies, complete 20-case regression source and four guides read. The focused deduplication, custom attenuation, deadline maintenance, body identity, reentry reservation and magnetic cleanup/cache changes are coherent by source inspection. Fixture observations use accumulated impulse/force and membership/events; owned remote hierarchy is destroyed and no global physics simulation setting is changed. Real scheduling/contacts and native forces remain unexecuted; manual lifecycle messages are not an engine proof. C-01 remains outside the existing callback fixture; C-02 is a documentation inconsistency.

Aggregate work: each active magnet independently queries at line 375, scans its own full history and applies field-specific force/membership. Distinct centers/radii/masks are genuinely different queries; do not remove per-field forces or identity semantics. Co-located fields with identical raw query context could share discovery, but this is a profiling/optional-backend opportunity, not proof that all fields should share one result. The continuous-cadence proposal correctly keeps legacy tuning by default and separates discovery from integration; it does not yet address saturated buffers or same-context cross-component discovery reuse. Require explicit incomplete-query handling and query-count/velocity evidence before advertising completeness/performance. Reused buffers must not be called persistent immutable snapshots. Impulse history scan is per zone/body; explosion allocation/query is explicit-call work.

Read fingerprints:

| Full artifact/source path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.md` | `bee7c69e320df04e855d2e1dc4a0df942994102969fc32c25ad488e3d3ba9f83` |
| `D:/Git/NeoxiderTools/Docs/Audits/2026-10-23/Neo.Tools.Physics.plan.md` | `38e20b27f984d6ce9b0035d8e557150fd5e8781ce08bd328c11b58f24cd9fba8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` | `04397b5006978e809b5287235bd592a8c0ee9e69ad307ed71f5ffab760315bf8` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` | `9953be72a50ab10e7b8540166801630e22c1dc25caf7ae99946c905685ecfed1` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` | `ea7cb40ec3dac120f89a35cca968f0b373d2f8845f625feca1f5226f29833577` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsPhysicsRegressionTests.cs` | `4a3970efc828245c84314112d9c0e6361b517edf0fc9f27b2fa877583e93ea70` |

## Completion receipt

Write `Docs/Audits/2026-10-23/Neo.Tools.Physics.implementation.md` with exact actions/files/additive API/dependencies, migration details and authored acceptance (UNEXECUTED). If blocked on another additive API, complete independent files and identify it precisely for root continuation. Do not edit shared review-action/manifest/other actor reports.

## Root Physics emitter callback liveness followup

Root FULL-read final55line receipt andfollowedexplicitremainingleads: actualExplosiveForce.Explode163–248, Magnetic Update/Fixed184–204/toggle335–351/field353–445. Readnew RootPhysicsEmitterReview.md. ConfirmedP2 ExplosiveForce OnObjectAffected destroys emitter, nextiterationcol.gameObject==gameObject185accessdestroyedowner; OnExplode destroys thenautoDestroygameObject241/245also. MinimalUnitythis==null fences after externalcallbacks/beforeobsoleteforce/completion/cleanup, preservealreadyappliedforce anddisabledLIVE manualExplode/normalorder. Magnetic requires Unity-null beforeisActiveAndEnabled atentryandafterentercallbacks; togglecallbackalsoflowsintoentry. ExactdestroyedBehaviourgetterruntimeoutcomeUNVERIFIED; no inventedexceptionclaim. Realpublicevent DestroyImmediate(component/GO) controls with2independentbodies, actualforce/count andno oldtail; completionobserverdestroy controls plusordinary/live-disabled explicitexplosionpositive. Useexisting23casefixture andcleanup; native/PlaydeliverywherefeasiblebutNOexecutionbeforefreeze. Preservephysicalmasks/fields/defaults/delayedDestroymeaning, OnDisablefieldcleanup; no broadgenerationframework/nativecapacityredesign. Updateown2guides/receipt. SAMEoriginalsession, noforeignfiles/sharedconfigs/checkers/compiler/Unity/tests yet.

## Snapshot3 actual PHYSICS-NATIVE-CALLBACK-ADMISSION

SAME original source actor, gpt-6.1-sol/max. Root closed execution after all owned Unity jobs stopped. NO compiler/Unity/checkers/tests until ALL writers finish and a new global freeze. Root personally read all98 graphical Edit failure messages/counts and all43 Play groups, independent full-edit-failure-triage.md and full-play-failure-triage.md. Read exact assigned cases in Local/Audits/assembly-2026-10-23/unity/final/s3/u63/Receipts/22-full-edit-graphics.xml (SHA7c6d24e1...) and21-full-play-graphics.xml (SHA2b152164...) plus Local/Audits/assembly-2026-10-23/full-edit-failure-action-index.json. PRIMARY b03-04 exclusively owns ALL helper moves/extractions, Fixtures assembly/shared refs and files in Local/Audits/assembly-2026-10-23/runtime-fixture-relocation-reservations.json. Check map before every edit. Do not race/extract helpers or change reserved fixture callers; work only disjoint source now, explicitly report deferred reserved work for root to resume after release. Preserve public/serialized API/settings/GUIDs and EVERY substantive positive/negative/count/ownership/payload oracle. Establish a real positive precondition before inferring runtime defect. Native mode failures cannot be hidden with ExecuteAlways, manual Update/private lifecycle as native evidence, blanket Ignore, expected-error whitelist, arbitrary tolerance/default change, reflection UserCode bypass or lost cases. Use supported TOP-LEVEL UnitySetUp/UnityTearDown/UnityTest Enter/ExitPlayMode for true lifecycle, or existing public API for explicit pure contract tests; nested ExitPlayMode is unsupported. No user/foreign scenes/providers/sessions may be reset/deleted; only owned test resources. No other owners/shared asmdefs/vendor314 migration targets, no children/commits/push. Update own canonical docs if actual contract changes and own honest implementation receipt; no test pass inferred from source. Stop writes when done; name unresolved native evidence. 20 own PhysicsRegression Edit cases fail ShouldRunBehaviour from SendMessage(Update/trigger/collision). Run genuine callback/lifecycle controls in admitted Play context (real frame/physics where claimed), or explicit existing public core APIs for pure filtering/timer semantics; no fake native Update. Retain independent cooldowns, emitter liveness/ownership, layers/tags/triggers, positive collision/hit and observer reentry oracles. No blanket error expectations, skipped case bodies or production mechanics change.
