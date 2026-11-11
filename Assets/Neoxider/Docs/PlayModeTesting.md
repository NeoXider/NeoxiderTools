# Play Mode testing

**What it is:** the guide to `Neo.Tests.Play`, the Play Mode suite in `Assets/Neoxider/Tests/Play`. It verifies Unity lifecycle behaviour and the integration of runtime APIs, scene components and UnityEvents.

**How to use:**
1. Run the suite as described in [Testing — running tests](Testing.md#running-tests): PlayMode tab, `Neo.Tests.Play` or a fixture filter.
2. Check the [Mirror and solo scope](#running-and-interpreting-coverage) before reading results.
3. When you add tests, follow the [C# API](#testing-public-c-apis), [allocation](#allocation-guards-and-benchmarks) and [scene](#scene-and-nocode-workflows) sections.
4. Record passed, failed and skipped tests separately: a missing demo, a host-only test and a source check are different evidence, and none is a remote-client or scene-interaction run.

**Navigation:** [← Testing](Testing.md) · [Network Play Mode](Testing/NetworkPlayMode.md) · [Docs](./README.md)

---

## Running and interpreting coverage

The assembly needs `UNITY_INCLUDE_TESTS`. It covers audio teardown, combined transform animation, slots and wheel completion, dialogue, inventory, shop purchases and grants, RPG combat, interaction queries and pointer events, AnimationFly, UI mesh rigs, network host behaviour and selected demo scenes.

**Mirror and solo cases.** Mirror imports, integration-only fixtures and their setup are guarded by `MIRROR`. Four solo cases stay compiled and start no host: the dispatcher offline fallback (exact void, int, float and string callbacks without a session), local InteractiveObject, local PhysicsEvents3D and the generic reactive-property bridge. Personal Money and the Everyone filter also compile without Mirror; their Mirror variants keep the host and spawn preconditions, and Money keeps its isolated provider until owned objects are destroyed. The asmdef still lists Mirror and the RPG network adapter, so these guards do not prove a Mirror-absent compile.

**What the network fixtures prove.** Legacy fixtures use an in-process host and `DummyTransport`, which opens no sockets: host authority, handshake, serialized loopback and restarts. Names with `Cmd` or `Globally` do not prove remote Command admission or sync; real peers are the [separate matrix](Testing/NetworkPlayMode.md).

- Networked dispatcher cases enable `isNetworked`, configure identity and behaviours while inactive, require a client spawn and a host observer, and count four RPC envelopes plus exact callbacks, so a local fallback cannot pass.
- Relay rejection cases check the valid channel's state is unchanged, then deliver positive controls on that channel; a reliable control drains earlier host-to-client traffic.
- `EventChannel_NeverDeliversToTheHostItself`: Publish skips loopback, PublishTo rejects the host connection, and direct serialized ingress gives zero callbacks and `ReceivedCount`. A direct `99` payload is seen through Mirror's read-only diagnostics, then a reliable barrier, with the real handler installed.
- `PhysicsEventsNetworkTests` moves a Rigidbody through a trigger in a dedicated local PhysicsScene with native collision steps: exactly one enter and one exit with the actor's collider, plus RPC envelopes when networked. No private callbacks, no remote physics.
- `InteractiveObjectPlayTests` covers raycasts and obstacles. Scene-player pickup tests prove clone isolation through their helper. RPG snapshot replay proves the codec seam, not a real late join.

**Isolation regressions.** `Audit20261023NeoTestsPlay*` protect fixture isolation, save-provider lifetime and culture handling, demo discovery, native cleanup and bounded network waits. Compile and run them before claiming runtime validation.

- The network isolation file adds paused versions of seven fixture groups, exact restoration of borrowed globals across two hosts, foreign-session rejection, and cleanup after interrupted pickup and copy, duplicate bootstrap and Mult loads (pending, and after Awake moved the manager into DDOL).
- An external DDOL root created during the load by an earlier exact-scene callback never enters the ownership ledger and survives cleanup with its scene, parent and position.
- Three cases restore a live registered EventSystem, leave a disabled one disabled and leave a destroyed one unselected, through native activation and destruction only.

## Testing public C# APIs

Build the smallest object graph, subscribe to public events, call the typed API and assert state and event counts. Inventory tests call `InventoryComponent.AddItemByIdAmount` / `RemoveItemByIdAmount` and check `GetCount`, `TotalItemCount` and `OnInventoryChanged`; Shop tests call `Buy` / `BuyBundle` and observe ownership, equipment, grants and spending through `IMoneySpend`.

- **Save providers.** Install a nonpersisting `ISaveProvider` with `SaveProvider.SetProvider` **before** adding a component that loads or saves in Awake or OnEnable. Keep `SaveProvider.CurrentProvider`, destroy your objects while the isolated provider is active, then restore that exact instance in `finally`. Never clear the host's saves or swap in a new PlayerPrefs provider. The in-memory test provider serializes floats in the invariant culture.
- **Ownership.** Track every GameObject and native asset (AudioClip, ScriptableObject, Sprite, Texture2D) and release them after failures too. Stopping a host does not destroy the runtime-created player template; destroy it yourself. Leave unrelated scene objects and EventSystems alone.
- **Pointer input.** Call `OnPointerEnter`, `OnPointerExit` and `OnPointerClick` directly, without an input module; device input needs a backend-specific test. `InteractiveObjectPlayTests` installs its own main camera and ray provider and restores borrowed cameras. Cursor-only exit cases use `InteractionRayProvider.RayMode.Mouse`, turn off the screen-center override and put the target behind the camera; a `Both`-mode case keeps hover until native Update sees the aim leave; the hover-disabled case proves a center hit still clicks. Public pointer calls do not prove hardware input.
- **Waits.** Use real-time deadlines; a paused game's `Time.deltaTime` cannot bound a stall. Network waits fail after five real-time seconds, scene loads after ten. A timeout fails the test instead of reaching an assertion that might pass uninitialized. Restore time scale and other globals you changed.

### NetworkTestSession

`NetworkTestSession` is the internal ownership helper of the legacy network fixtures:

1. Create it before changing globals or constructing objects.
2. Register the exact manager with `OwnManager` and every created object with `Own`.
3. Keep the isolated save provider installed and **yield** `Cleanup()` from UnityTearDown.

It restores the exact `Transport.active`, background flag, frame cap, VSync, time scale and Unity random state. It refuses an existing session, manager (inactive included) or scene identity, never stops discovered managers and leaves a replacement session untouched.

Build network objects while inactive, then activate or spawn them. Use `NetworkTestHelper.WaitForSpawn(identity)` for readiness, `WaitUntil` for the state or event, and `ReliableBarrier()` before final count or negative assertions; a barrier alone does not prove a message was sent. Do not lengthen sleeps, use scaled cooldowns as deadlines or drain a nested cleanup IEnumerator by hand. These helpers change no gameplay or serialized API.

## Allocation guards and benchmarks

Play Mode uses the shared `Neo.Tests.Performance` assembly ([Performance and allocation tests](Testing.md#performance-and-allocation-tests)).

- `System.GC.GetAllocatedBytesForCurrentThread()` reads 0 in the Unity 6 Editor and IL2CPP. Use `GcAlloc.AssertNone`, `GcAlloc.TryCount` or `GcAllocCounter.CreateOrIgnore()`; a player without profiler support reports Ignored (`NotSupported`), never passed.
- A window opens and closes in one frame: yield to the state, warm the path, then measure synchronous calls between `Begin()` and `End()`. A window across `yield return` throws.
- PlayerLoop costs use `NeoPerfUpdateWindowBegin` / `NeoPerfUpdateWindowEnd` with `NeoPerfUpdateWindow.Start(frames)`; `NeoPerf.EstimateBytesPerOperation` estimates bytes from `GC Allocated In Frame`.
- Benchmarks carry `[Category(NeoPerf.Category)]` and `[Explicit(NeoPerf.ExplicitReason)]`, print one `NEO_PERF` line per case and run with `-testPlatform PlayMode -testCategory Performance -testFilter "<fixture or test>"`. `GcAllocCounterPlayTests` checks the counter on the real PlayerLoop.

## Scene and NoCode workflows

Component tests build scene adapters in code and call the typed methods and UnityEvents that Inspector bindings use. Reflection sets serialized authoring fields and some seams; it does not replace runtime assertions or validate persistent bindings.

**Demo roots.** Discovery reads `Assets/NeoxiderDemos/<name>`, the same folder in the repository and after importing the downloads; the older imported layouts `Assets/Samples/NeoxiderTools` and `Assets/Samples/Neoxider Tools` are still recognized. A missing tree causes an explicit skip. With several imported versions, keep the one you test: automatic version selection is not guaranteed. `SampleScenePlaySmokeTests` and `AudioDemoScenePlayTests` are described in the [test inventory](Testing.md#sample-demo-and-packaging-tests).

### Mult scene tests

`MultSceneNetworkTests` needs the development project's `Assets/Scenes/Mult.unity` and skips without it; the skip is not a multiplayer pass.

- It refuses an already loaded Mult scene and existing managers or sessions, loads its own scene additively, waits for load, Awake and manager publication, and records the exact hierarchies first.
- A manager moved to DDOL must be anchored to that scene, its owned hierarchy or its serialized scene-player template; only that hierarchy is adopted and foreign managers are refused.
- Ownership is never inferred from names or a before/after root difference. DDOL roots created during the load, including by earlier `sceneLoaded` observers, stay borrowed without exact provenance.
- After an interrupted pending load, teardown waits for it, stops the owned session, releases ledgered objects and unloads that exact scene. A load that never completes fails after a real-time deadline.
- Teardown keeps borrowed scenes and DDOL roots and restores the prior save provider, active scene and cursor state.
- `EventSystem.current` is restored only when the captured original is still live and active, because native OnEnable and OnDisable maintain uGUI registration and the setter rejects null or unregistered instances. Cleanup never enables, disables or registers foreign EventSystems.

## По-русски

Legacy-тесты сети работают в изолированном мире: in-process host и `DummyTransport` без сокетов; имя с `Cmd` или `Globally` не доказывает доставку на клиент, удалённые пиры проверяет отдельная матрица. Без Mirror компилируются автономные проверки dispatcher, interaction, physics, reactive, а также personal Money и фильтр Everyone. Изолированный провайдер сохранений ставится до создания компонентов и восстанавливается в `finally`; ожидания используют реальное время. Cleanup восстанавливает точные заимствованные globals и уничтожает только свои объекты; чужой DDOL root, появившийся при загрузке Mult, не становится своим. Окно подсчёта аллокаций открывается и закрывается в одном кадре.
