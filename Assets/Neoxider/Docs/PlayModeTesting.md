# Play Mode testing

`Neo.Tests.Play` verifies Unity lifecycle behavior and integration between the package's runtime APIs,
scene components and UnityEvents. Its sources live in `Assets/Neoxider/Tests/Play`.

## Running and interpreting coverage

Use Unity Test Runner's PlayMode tab and select `Neo.Tests.Play`, or filter by the fixture under
investigation. The assembly is gated by `UNITY_INCLUDE_TESTS`. Mirror imports, integration-only fixtures,
attributes and setup are guarded by `MIRROR`. Mixed fixtures keep their solo cases compiled: dispatcher
offline fallback, local InteractiveObject, local PhysicsEvents3D and the generic reactive-property bridge.
These four cases do not acquire a host when run directly. Personal Money and the Everyone filter also
remain compiled without Mirror; their Mirror-enabled variants retain the existing host/spawn preconditions
and assertions. Money keeps its isolated save provider through owned-object destruction in either mode.
The dispatcher offline positive remains outside
the integration prerequisite and checks exact void/int/float/string callbacks with no active session.
The current asmdef still lists Mirror and the RPG network adapter; these source guards do not establish
a Mirror-absent import/compile result. Record actual dependency resolution and discovery separately.

The suite covers audio teardown, combined transform animation, slots and wheel completion, dialogue,
inventory, shop purchasing and inventory grants, RPG combat, interaction queries and pointer events,
AnimationFly, UI mesh rigs, network host behavior and selected demo scenes. Legacy network fixtures use
an in-process host and DummyTransport, which opens no sockets. They exercise host authority, handshake,
serialized loopback messages and restart behavior. Names containing `Cmd` or `Globally` do not establish
remote Command admission or synchronization. The separate-peer harness belongs to the network testing
topic; report its actual execution independently.

Both legacy networked dispatcher cases explicitly enable `isNetworked`, configure identity/behaviours
while inactive, require a client spawn and host observer, and check four outgoing/received RPC envelopes
plus exact void/int/float/string callbacks. A local fallback cannot satisfy those assertions. The offline
case remains separate. Relay rejection cases check unchanged valid-channel state, then deliver positive
controls on the same channel. A serialized reliable control drains preceding host-to-client traffic.

`EventChannel_NeverDeliversToTheHostItself` checks the public channel's complete host exclusion:
Publish skips loopback, PublishTo rejects that connection, and even direct serialized ingress produces
zero channel callbacks/ReceivedCount. A direct `99` payload is observed through Mirror's read-only
send/receive diagnostics, followed by the separate reliable barrier, while the real channel handler
remains installed. Exact envelope/payload counts and live ready connection/player state establish the
host admission positive. A channel Received callback on a pure client needs the separate real-peer path;
the host test does not establish remote delivery.

`PhysicsEventsNetworkTests` moves a Rigidbody through a trigger in a dedicated local PhysicsScene and
steps native collision detection. It asserts exactly one enter/exit with the actor's collider and checks
RPC envelopes in the networked case. It does not invoke private physics callbacks or test remote physics.
`InteractiveObjectPlayTests` covers raycasts/obstacles separately. Scene-player pickup tests call their
helper directly and prove clone isolation; RPG snapshot replay proves a codec/apply seam, not a real late join.

`Audit20261023NeoTestsPlay*` regressions protect fixture isolation, save-provider lifetime and culture
handling, imported sample discovery, native object cleanup and bounded network waits. They remain
ordinary NUnit/Unity tests and must be compiled and executed before claiming runtime validation.
The network isolation file adds paused versions of the seven affected fixture groups, exact borrowed
global restoration across two hosts, foreign-session rejection and cleanup after interrupted pickup/copy,
duplicate-bootstrap and Mult scene loads. The Mult cases interrupt both a pending load and a load after
Awake moved the manager into DDOL; they check owned-object release and borrowed-scene/DDOL-root survival.
A further case registers an earlier exact-scene native callback that creates an external DDOL root during
the load. It requires that root and its child never enter the ownership ledger and survive yielded host
cleanup with their exact scene, parent and position, while the actual owned manager and scene are released.
Three further cases exercise native EventSystem registration: restore a live registered original,
leave a disabled original disabled, and leave a destroyed original unselected. All use actual native
component activation/disable/destruction, with no private lifecycle calls or expected-error whitelist.
These are authored regression sources; no implementation-phase execution result is implied.

## Testing public C# APIs

Create the smallest object graph needed for the contract. Subscribe to public events, call the typed
API, then assert observable state and event counts. For example, inventory tests call
`InventoryComponent.AddItemByIdAmount` and `RemoveItemByIdAmount` and check `GetCount`,
`TotalItemCount` and `OnInventoryChanged`. Shop tests call `Buy`/`BuyBundle` and observe ownership,
equipment, inventory grants and spending through `IMoneySpend`.

Install a nonpersisting `ISaveProvider` through `SaveProvider.SetProvider` **before** adding a component
that can load or save in Awake/OnEnable. Keep `SaveProvider.CurrentProvider`, destroy the test's objects
while its isolated provider is still active, then restore that exact provider instance in `finally`.
Do not clear the host project's saves or replace its configured provider with a newly constructed
PlayerPrefs provider. Inventory, shop, money-network and bonus fixtures use an isolated in-memory
provider. Float serialization in this test provider uses invariant culture in both directions.

Track every created GameObject and native asset, including AudioClip, ScriptableObject, Sprite and
Texture2D instances. Release owned objects even after an assertion fails. Stopping a host destroys
spawned players but does not transfer ownership of the original runtime-created player template;
the fixture must also destroy that template. Preserve unrelated scene objects and EventSystems.
Pointer-handler tests can invoke `OnPointerEnter`, `OnPointerExit` and `OnPointerClick` directly without
installing an input module. Device-input behavior needs a separate backend-specific test.

`InteractiveObjectPlayTests` installs its own sole active main camera and explicit ray provider,
temporarily suspending and later restoring borrowed main cameras. Cursor-only exit cases select
`InteractionRayProvider.RayMode.Mouse`, disable the object's screen-center override and put their target
behind the camera, then prove enter before asserting exactly one exit. A separate `Both`-mode positive
uses a real center ray: pointer exit retains hover until native Update observes that aim left the target.
The hover-disabled case proves a real center hit still clicks and checks the mouse-interaction gate on
a later frame. Those cases yield actual frames; public pointer calls do not establish hardware input.

Wait for observable completion with a real-time deadline. A paused game's `Time.deltaTime` cannot
bound a stalled test. Network waits fail after five real-time seconds, including while paused; scene
loads use ten seconds. A timeout must fail the test rather than silently continue to an assertion
that might also pass in the uninitialized state. Restore time scale and other global settings owned
by a fixture during teardown.

Within `Neo.Tests.Play`, `NetworkTestSession` is an internal ownership helper for the legacy fixtures.
Create it before modifying globals or constructing objects, register the exact manager with `OwnManager`
and every fixture-created object with `Own`, then **yield** `Cleanup()` from UnityTearDown. Keep an isolated
save provider installed until this cleanup finishes. The helper restores the exact prior `Transport.active`,
background flag, frame cap, VSync, time scale and Unity random state. It rejects an existing session,
manager (including inactive managers) or scene identity before taking ownership. It never selects and
stops discovered managers as generic teardown. A replacement session is left untouched.

Construct the whole network object while inactive before activating/spawning it. Use
`NetworkTestHelper.WaitForSpawn(identity)` for host/client/observer readiness, `WaitUntil` for the actual
state/event, and `ReliableBarrier()` before final count or negative assertions. A barrier alone is not
proof that a gameplay message was sent; dispatcher tests also count RPC envelopes. Do not replace sleeps
with larger sleeps, use scaled cooldowns as deadlines, or exhaust a nested cleanup IEnumerator manually.
The regression harness yields nested cleanup through UnityTearDown even after an assertion interrupts
the body. These internal APIs do not change the package's gameplay or serialized APIs.

Русский: сетевые legacy fixtures запускаются только в изолированном мире. Cleanup восстанавливает точные
заимствованные globals и уничтожает собственные объекты после остановки своей сессии. Ожидания используют
реальное время и наблюдаемый результат; host loopback, native physics, codec/clone и remote-peer результаты
нужно учитывать отдельно. Standalone dispatcher/interaction/physics/reactive проверки не требуют host;
personal Money и Everyone filter также сохраняются без Mirror, с прежним host-путём при `MIRROR`.
Mult загружается аддитивно: учитываются точная сцена и manager с подтверждённой scene/template-ссылкой,
затем проверяется готовность manager. Само появление чужого DDOL root во время загрузки не даёт ownership.
Cursor-only exit и положительный center-hover проверяются отдельно. Новые регрессии требуют последующего
централизованного запуска.

## Scene and NoCode workflows

Component tests construct scene adapters in code and exercise the same typed methods and UnityEvents
that Inspector bindings call. Reflection is used for serialized authoring fields and selected
integration seams; this does not replace runtime output assertions or validate persistent Inspector
bindings. Demo scene smoke tests provide additional scene-level coverage where their samples exist.

Supported demo roots are:

- Development: `Assets/Neoxider/Samples/Demo`.
- Release package: `Assets/Neoxider/Samples~/Demo`.
- Imported sample: `Assets/Samples/NeoxiderTools/<version>/Demo Scenes` (the legacy package folder
  `Neoxider Tools` and sample folder `Demo` are also recognized).

Discovery prefers development samples, then visible imported samples. A hidden `Samples~` directory
is a fallback and causes an explicit skip because Unity does not compile its sample scripts.
Import Demo Scenes through Package Manager to exercise scene smoke coverage while keeping the package
in release layout. If several imported versions exist, retain the version intended for testing;
automatic version selection is not guaranteed. The optional multiplayer tests in
`MultSceneNetworkTests` need the development project's `Assets/Scenes/Mult.unity` and skip if absent.
That fixture refuses an already loaded Mult scene and existing network managers/sessions. It loads
its own scene additively, waits for actual scene load/Awake and manager publication, and records exact
loaded-scene hierarchies before readiness assertions. A manager moved to DDOL must be anchored to that
scene, its known-owned hierarchy or its serialized scene-player template; only that manager's hierarchy
is then adopted, never an external parent. Foreign replacement managers are refused. A root appearing
during the load is not evidence of ownership: the fixture does not infer ownership from names or a global
before/after root difference. Independent DDOL roots, including those created by earlier sceneLoaded
observers, remain borrowed unless exact scene/hierarchy/reference provenance establishes ownership.
On an interrupted pending load, teardown waits for the owned load before stopping the owned session,
releasing ledgered objects and unloading that exact scene. A load that never completes fails after a
real-time deadline and still needs native failure-path evidence. Teardown preserves borrowed scenes
and both pre-existing and during-load borrowed DDOL roots, and restores the exact prior save provider,
active scene and cursor state. EventSystem.current is restored only when the exact captured original
is still live and active/enabled: native OnEnable/OnDisable maintains uGUI registration, and its setter
rejects null/unregistered instances. An absent, disabled or destroyed original is left to native registry
selection; cleanup does not enable, disable or register foreign EventSystems. Keep the isolated provider
installed until owned-object destruction finishes.

Русский: при cleanup восстанавливается только живой active/enabled EventSystem, захваченный из native
registry. Null, выключенный или уничтоженный оригинал не назначается обратно. Event channel на host
отбрасывает и прямой входящий payload; положительный контроль проверяет реальный serialized ingress
через diagnostics и отдельный barrier, без подмены handler и без утверждений о pure-client доставке.

Record passed, failed and skipped tests separately. A missing sample, a host-only test and a static
source check each provide different evidence; none should be reported as an executed remote-client
or scene interaction check.
