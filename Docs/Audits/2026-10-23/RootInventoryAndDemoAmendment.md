# Inventory generalization and demo clarity — root continuation

The user supplied DiveForBrainrots as a reference for collection-game requirements. Its source was read;
that project was not modified. Distinct UID/DataID/level/mutation instances, temporary carry, nine fixed
slots and podium transfers are the concrete integration case. NeoxiderTools already had the storage and
paired transfer primitives; type-only use could consume another same-type instance and a hand callback
could switch selection before the old use activated a replacement visual.

Root added an owner-bound, plain C# InventorySelection, exact take/consume/transfer component paths and
an optional InventoryUseAction adapter. Existing interfaces, serialized fields, integer events, saves and
asset GUIDs remain compatible. Identity validation deliberately allows current count/payload updates for
the same UID. Ordinary stacks have slot/type identity, not a fabricated per-stack UID. These limits and
the commit/observer-fault contract are documented in the canonical module entry and new API pages.

The new InventoryUseDemo authors independent catch/hotbar/podium components and shows a complete
deposit/select/place/recall/consume/heal/reset loop with UID and state feedback. The existing Abilities
NoCode scene keeps its authored damage/heal wiring and gains instructions, numeric state, feedback,
manual Zap, level controls, explicit auto-cast and reset. No game-specific specimen/heal rule was put in
library runtime. Samples remain in release Samples~ layout.

Root reviewed the new source and authored regressions for exact UID/payload, stale/foreign selection,
quantity admission, observer/effect faults, reentrant hand use and actual sample button flows. These
regressions are authored, not yet claimed as native passes. The true Mirror-absent compile also exposed
four test-only conditional-compilation errors; the shared RPG fixture alias and three optional
NetworkIdentity additions now follow their real dependencies without dropping local test assertions.

Chosen native/CLI reviewers exhausted their account usage limit, so this bounded continuation is by
root. It is not represented as an independent agent review. Prior audit/review receipts and raw native
failures remain intact. New compilation, targeted regression and compatibility evidence is required;
manual scene acceptance remains pending until automated work finishes, as the user requested.

First P10 native slice: Unity6.3 compiled successfully, all132 Inventory Edit cases passed (including17
new selection cases), and the actual Inventory sample button flow passed. The Abilities guide flow
failed at the second level-scaled hit. Inspection found the sample Zap asset's AmountLevelSource=None,
which fixed its array lookup at level1 despite the new unit-level control. The sample now explicitly uses
CasterUnitLevel and describes that contract. The regression still requires exact12/30 damage and immediate
cooldown rejection; its later cast now waits on the public native cooldown within6 real-time seconds
instead of assuming a fixed WaitForSeconds interval means the scene system has reached READY.
Raw failed P10 XML/logs remain unchanged; these corrections require fresh native verification.

P11 confirmed the real second Zap dealt30 damage and that Inventory flow still passed. The remaining
cumulative-HP assertion exposed the original training template's10HP/s regeneration, restoring the dummy
between casts (70HP after the second hit, not58). Both templates are used only by this scene. The dummy's
health regeneration is now0, matching the existing player setting, so the teaching result remains visible;
mana and reusable library behavior remain unchanged. Display names and the on-screen explanation now
make that sample-only contract explicit. The exact damage/reset assertions are preserved for the next run.

P12: Inventory132 and both actual sample flows passed on Unity6.3/6.6. True Mirror-absent profiles compiled,
loaded no Mirror assembly/defines, and passed6/6 local cases on each SDK. New-only input passed17 Edit
and92 Play cases on each; the remaining strict cursor parity case lacks a native pointer in batch and
must pass in a visible focused Game View. Raw1-failure XML is retained, not reclassified as a clean pass.

The true Embedded UPM/no-Pro63 profile compiled, but the import helper failed before invoking any import:
it hardcoded the wrong assembly name for public UnityEditor.PackageManager.UI.Sample. Installed SDK PE
metadata confirms the public type in UnityEditor.CoreModule (and the UnityEditor facade). Resolution now
starts from typeof(Editor).Assembly and retains public facade/older-module fallbacks. All three native
Sample.FindByPackage/Import and asset/GUID/missing-script assertions remain unchanged; no sample was
copied to impersonate an import. This helper-only source correction needs actual import verification.

P13 resolved the real public API and natively imported Demo Scenes (Import returnedtrue). Validation
then ran too early and reported CardGame's PM prefab GUID6db8ef... from the still-unimported sibling
NeoxiderPages sample. Import orchestration now imports all3 first, then runs the unchanged complete
asset/GUID/native missing-script validation. Package/README/sample guide explicitly describe sibling
dependencies of legacy integration scenes; focused module demos stay independently explorable.
The failed actual import remains raw evidence; its task-owned imported output must be archived reversibly
outside Assets before a fresh native import attempt. This is not permission to delete user samples.

P14 all3 actual imports returnedtrue. Full native GUID inventory then exposed25 unresolved GUIDs in19
legacy assets. The generic native RemoveUnusedOverrides candidate was rejected: its before/after
snapshot changed2 derived font sizes and Canvas scale; no candidate was copied. Source repair is instead
limited to292 overrides targeting a nonexistent prefab,2403 retired prefab-parent metadata fields,
44 legacy uGUI DLL references (14Text,17Image,7Button,2Toggle and one each of CanvasScaler,
GraphicRaycaster, EventSystem and StandaloneInputModule, mapped to exact SDK MonoScripts), one inactive missing
audio resource, and the proven missing Pages artwork/font references. Existing packaged/sample-owned
art and LilitaOne font/material replace missing assets; currency uses the shared star icon. Component,
prefab GUIDs, events and live layout fields remain intact. Native/visual verification is still required.

The native no-Pro diagnostic14=13P1F also identified2 real Pro components on the runtime Fake Load
prefab. Its3-second clockwise spinner now uses existing ConstantRotator at-120deg/s on localZ;
the old rotation component fileID and prefab GUID are retained, with no external old-component callers.

The user additionally requested dependency/MCP updates. Official compatible stable targets were
resolved, Mirror96.0.1→96.11.3 and UniTask2.5.10→2.5.11 installed with stable GUIDs, retired vendor files
archived, and Unity packages updated. Native Unity6.3 Package Manager resolution/compilation completed
exit0; its genuine lock was copied back, including MCP10.3.0 at aa5fc638d623f56178d50329d9ad8c541a57fe66.
The matching MCP Python server10.3.0 is installed and local HTTP initialization succeeds. The existing
client connection retained a startup failure; editor/client reconnection remains to verify. Old
functional passes cannot stand in for affected regressions after these dependency changes.
DOTween1.2.765/Pro1.0.381 stays paired: the official author requires a licensed Pro>=1.0.405 before
upgrading the free Core, and no licensed update package was found in the local cache.

P15 updated-dependency true Embedded/no-Pro acceptance is complete on both SDKs: all3 genuine public
UPM imports,28/28 Edit cases (including9 Pages positives),19/19 Play cases, zero skips, Pro payload and
loaded assemblies absent. Full6.3 Edit ran4611:4596P/13F/2explicit skips;11F are the known native cursor
batch capability and2 are the synthetic not-ready Money client controls. Updated Mirror now fills
NetworkBehaviour.netIdentity from native NetworkIdentity.Awake; the old synchronous Edit fixture had no
such lifecycle. These2 cases move to Play with inactive component setup and real activation, an explicit
identity-cache positive, and every original pending-status/amount/balance/event/save/budget oracle.
No production Money policy or implementation changes were made for a fixture precondition. The client
state remains a declared local branch control, not packet/remote acknowledgement evidence. Actual3OSpeer
matrix remains required. All raw P15 results remain untouched; context correction needs new execution.

P17 native Money2/2 passed. Updated full Play63 ran731:636P/94F/1optional HDRP skip.88 failures share
three fixture dispatch helpers that truncated32-bit hashes, while updated Mirror's actual public
GetStableHashCode16 uses XOR-folding (RemoteCalls.RegisterDelegate uses that API). Helpers now call the
same public API, retaining real registered woven receivers, payload consumption, authority and event
oracles; no UserCode shortcut or fake registration was added. Two RPG host fixtures now configure
identities/behaviours inactive then activate and assert the native cache before woven SyncVar access.
Four cursor cases require actual GUI acceptance. Production networking remains unchanged for these
fixture corrections; all failed raw XML/logs stay retained. Targeted90 then full updated suites required.
