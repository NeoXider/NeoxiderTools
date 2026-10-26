# Root review: Input I11-01 correction

Status: **source-closed; execution UNEXECUTED**. Independent root review of the SAME Input owner's correction following NativeImplementationReview-11. This does not change that historical report or claim a compiler/runtime pass.

Root read the complete updated two-part implementation receipt, current MouseInputManager release block at lines 145–215, all four new fault/normal/replacement regression bodies at dispatch partial lines 480–715, and the complete 75-line manager guide. Other source retains the earlier NI11 scoped disposition; it is not represented as a newly complete independent audit here.

The release path records completion only after the relevant Release phases and current-dispatch admission succeed. It clears pending ownership before Click on success. The finally block additionally checks both successful release completion and the original dispatch version. An earlier throwing Release/ReleaseIn observer therefore preserves unfinished ownership for later cancellation; a nested replacement's failed Release cannot be erased by an older successful call's finally. Exceptions still propagate synchronously.

The four authored controls cover ordinary and `in` Release faults, manager-only disable with the effect still enabled, exact exception identity, late notification suppression, native follower/trail state and position, repeated cancellation, successful normal release, and a nested replacement fault unwinding through an older successful release. Recovery checks real replacement hit and exact event/start/stop totals. These are meaningful source oracles, **not executed results**. Input now declares 76 authored cases; native device, GC, frame/lifecycle and backend acceptance remains with the final isolated runner.

No new source correction is requested for I11-01. Current file identities are recorded below by raw-byte bookkeeping. Root did not execute Unity, a compiler, tests or product validators for this review.

## Read identities

| File | SHA256 |
| --- | --- |
| `Docs/Audits/2026-10-23/Neo.Tools.Input.implementation.md` | `bab82632a7c9b8489e914cbf14eeeb7ebe08978b52b69d34dc50cc8685b6ec16` |
| `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs` | `2bbd14b2d9227b9d9ae4709b9078dfddf570226bbcfa96199a8396a0bed17d70` |
| `Assets/Neoxider/Tests/Play/Core/Audit20261023NeoToolsInputBehaviorTests.Dispatch.cs` | `cece96c21a4fa83944f82910f11c431e6bdbe8c2f2642b708d58e7e7934eebf0` |
| `Assets/Neoxider/Docs/Tools/Input/MouseInputManager.md` | `3b6c63d513c65889a0244f2dac91cce2951d9d8dc5b34dc1731632aa0aae18ea` |
