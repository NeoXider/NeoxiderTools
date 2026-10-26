# Native review C: narrow RPG contracts for 11.0.0

Status: completed read-only contract review, 2026-10-23, same native auditor/session (`gpt-6.1-sol`, `max`). Only this report is written. Original audit/plan and NativeReview-C.md remain phase snapshots. No implementation, compiler, test, Unity/MCP or validator execution is claimed.

## Recommended decision

1. A **final-Max snapshot restoration fix** is possible inside the existing APIs and v2 payload. It must not be confused with durable BaseMax. Preserve saved effective Max during the final restoration calculation instead of overwriting it; recompute percentage regen from that final Max. Explicitly document template-rebalance and notification semantics.
2. **HpRegenPerSecond** has a working public legacy math helper but is missing from RpgCharacter's actual regen path. Wire this alias into the existing resolver, with canonical HP, clamped stacks and current regen gates. No profile/API framework is needed.
3. **AddResourceCurrentFlat** is explicitly described as one-shot in source XML but never consumed. Completing it needs a small, explicit application/stack/event policy; it does not require replay markers or a new save format if profile loading never grants it again.
4. **AttackSpeedPercent** is currently an enum-only option. Automatic cooldown/wind-up/AI timing behavior has no established consumer contract. Preserve its value and disclose that it is unsupported until that feature is deliberately selected; do not silently map it to every attack timer.
5. Durable, independently editable resource bases require an explicit distinction in APIs/storage. A small optional base/presence extension is sufficient if selected; a general v3 character/transaction framework is not a prerequisite. Never infer BaseMax by copying a v2 final Max.

Original runtime owner: **Neo.Rpg / `neo-audit-1023-b05-03`**. Network integration: **`neo-audit-1023-b05-06`**; sample upgrade workflow: **`neo-audit-1023-b05-05`**. This reviewer does not edit those sources.

## Exact current Max and save behavior

All paths here are absolute within `D:/Git/NeoxiderTools`.

| Current source | Actual contract/effect |
|---|---|
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs:20–22,41` | BaseMax is a public mutable runtime property initialized from definition.startMax. It is distinct from effective Max. Definition/template data is not changed by writing BaseMax. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:607–623` | SetMaxResource/AddMaxResource route through the existing network command when needed; locally delegate to service.SetMax/AddMax. Neither changes BaseMax. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs:180–205` | Set writes an effective Max; Add uses **current effective Max + delta**. Both use runtime clamping, refresh the configured regen resolver, then ResourceChanged. Unknown ID returns false at service level; facade retains its void signature. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatResolver.cs:87–163` | Derived Max starts at BaseMax, adds invested flat/base-percent upgrade modifiers, then buff flat/percent, then maxCap/nonnegative limits. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1221–1238` | RefreshAllDerived always reuses that base resolver. Any buff apply/remove/clear and stat-base/upgrade refresh can replace an explicit live Max. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1397–1429` | CaptureProfile stores resource ID, **Current and effective Max**, stat bases, investments, active effect entries and scalar state. It does not capture resource BaseMax or cooldown ready-times. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileData.cs:15,47–49,170–179` | Profile is version2; each resource row has only id/current/max. Sanitize unconditionally replaces Version with CurrentVersion. Clone retains final Max; no base-presence information exists. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:1462–1515` | ApplyProfile sanitizes the supplied object, restores scalar state, writes saved Max before saved Current, restores stat bases/investments/effect entries, then RefreshAllDerived(false) overwrites resource Max from BaseMax. Unknown resource/stat definitions are not created. |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs:435–445` | RebuildRuntime discards runtime resources/effects/investments and reconstructs definitions, with optional configured auto-load. It is not a permanent-base mutation API. |

Public evidence: `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgCharacter.md:79` calls setters “change Max (buffs / upgrades)”; lines148–152 describe resource persistence and storage-independent snapshots. `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgNoCodeAction.md:29` exposes the same Max operations. `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgDemoUI.cs:30–35` calls AddMaxResource from UpgradePlayerMaxHp. None explains that a later derived refresh discards this live edit. This is an observable contract gap, not evidence that v2 secretly stores the base.

Concrete source witnesses (not executed):

- Base100, no modifiers, SetMaxResource(HP,160), RestoreResource(HP): current/max160. Capture/Apply the same profile currently ends current/max100 (assuming no overfill), because final refresh clamps the restored160 down. Percent-max10% regen likewise ends10/s rather than the restored16/s.
- Base100 with +20 Max buff: captured Max120. Assigning that saved Max to BaseMax and restoring the buff produces140. With +50% buff,150 becomes225. Therefore the tempting Max→BaseMax patch double-counts valid data.
- Base100 with +50% active buff yields150. Current AddMaxResource(HP,10) yields160. Reinterpreting that call as BaseMax+=10 yields165; SetMaxResource(HP,160) as a base setter yields240. Signature preservation does not make that behavior compatible.

## Small correction without profile migration: final snapshot Max

**Confirmed issue:** an explicitly stored effective Max is read and immediately discarded by ApplyProfile's final resolution. This prevents the advertised snapshot roundtrip, even without any modifiers.

Recommended 11.0 contract: a known saved resource row is an **effective-state snapshot at completion of ApplyProfile**. Honor its clamped final Max, then its Current; compute percentage regen using that final Max. Do not change BaseMax, definitions, saved keys, enums or method signatures. Existing normal captures with matching definitions/modifiers produce the same final values; explicit effective-Max captures stop losing their value on restore.

Small implementation shape in existing `RpgCharacter.cs`/resource resolver paths: supply a **restore-call-only max resolver/lookup** to the final derived refresh: use the saved final Max for known saved rows, ordinary ResolveResourceMax for unsaved rows, and the existing regen resolver for both. Preserve Max-before-Current validation and actual resource limits. A private overload/helper is enough; no lasting override layer, new manager or batch framework is needed. Preserve existing last-row-wins behavior if constructing an ID lookup from duplicate rows. Discard this lookup after the restore, including exceptions/reentry; it must not affect future normal RefreshAllDerived calls.

This deliberately fixes snapshot restoration only. A later ordinary buff/stat refresh can still recompute the live override from the unchanged BaseMax, just as after the existing SetMaxResource. Do not claim permanent upgrades/base persistence are solved. Choose/document whether loaded v2 effective Max or newly rebalanced template is authoritative at the **load boundary**: this recommendation uses the snapshot there. Projects wanting rebase-to-new-definitions need an explicit load/rebase policy, rather than silently discarding one field. This is a behavior/event-order decision for 11.0, but needs no data-format migration.

### Events and save semantics to preserve/clarify

- Runtime SetMax pushes MaxState, optionally clamps Current and pushes CurrentState/PercentState, resolves regen, then service ResourceChanged. Character.NotifyResource (`RpgCharacter.cs:1255–1270`) sends current-value scene output, reconciles HP death/revival and invokes the network adapter. AddMax has the same sequence. An increasing Max does **not** automatically refill Current.
- ApplyProfile already sends reactive changes during restoration, and its final resource notifications currently see recomputed/clamped state. The fix must make final ResourceChanged/OnProfileLoaded/network payload reflect the restored final Max/Current/rate. Avoid temporarily resolving a smaller Max that discards the saved Current before the final value is restored. Do not introduce extra death/revival solely from restoration ordering.
- Keep the approved initialization/rebuild/profile publication guard separate (NativeReview-C C-27/C-32): no network capture of half-initialized collections. Do not promise arbitrary multi-resource reactive callbacks are transactional; lower-level reactive events already occur during restoration. Guard generation/reentry and publish the settled snapshot after the operation, without requiring a framework.
- CaptureProfile calls EnsureInitialized. On an already ready character it does no provider I/O; first initialization can honor LoadOnAwake. Thus “storage-independent” is not a guarantee that first capture never initializes/auto-loads.
- SaveProfile (`RpgCharacter.cs:1383–1392`) serializes a clone via profile service, SetString(saveKey), SaveProvider.Save(), then OnProfileSaved. The provider's void Save does not prove disk durability. LoadProfile (`1431–1458`) reads the same key, deserializes, applies, then OnProfileLoaded; errors are caught/logged. ResetProfile (`1518–1527`) deletes/flushed key and does not reset the live runtime. Preserve these method/event meanings and keys; no claim of persistence rollback.

## What actually requires base-state migration

If the desired operation is **permanent base160 + temporary buff20 =180**, surviving later refresh and save/rebuild, current v2 data cannot identify that base. Final Max can be an override, a cap, authored base, upgrade bonus, buff bonus or a combination; subtracting current bonuses is also unreliable after catalog/stack/cap changes.

Minimum additive API if selected: service SetBaseMax/AddBaseMax and facade SetResourceBaseMax/AddResourceBaseMax, applying normal derived resolution once. Existing live-final setters and NoCode/network enum values keep their semantics. Store finite nonnegative runtime base independently; final maxCap still applies in the resolver, and never mutate a shared template. Existing UpgradeStat plus derived resource modifiers already supplies **persisted authored upgrades** through investment counts; reuse it when that is the actual feature instead of misusing live-Max setters.

Minimum persistence extension: optional HasBaseMax/BaseMax on the existing resource row, cloned/serialized/restored explicitly, while retaining Max as the final snapshot field. Presence distinguishes legitimate base0 from absent old data. The coordinator may call this a small v3 or a documented additive extension; it is two fields plus policy, not a new state/transaction architecture. For old rows without a base, recommended compatible default is authored/current runtime base, **never saved final Max interpreted as base**. This cannot recover already lost custom bases; a game's known old save layout may supply an explicit migration. Repeated load must not compound bonuses. If version-based migration is chosen, inspect/migrate Version **before Sanitize**, which currently overwrites it; handle unsupported future versions deliberately. Coordinate Network's same DTO/codec and matching peer versions; ignored fields do not prove mixed-version behavioral compatibility.

## Three modifier consumers: evidence and smallest policy

| Modifier | Exact current consumer evidence | Classification/recommendation |
|---|---|---|
| HpRegenPerSecond =3 | Enum legacy XML says targetId ignored (`Enums/BuffStatType.cs:5–8,18`). `Runtime/RpgCombatMath.cs:29–32,171–182` already computes clamped-stack legacy regen. Repository call-site search found no RpgCharacter use of that helper. Facade passes projected modifiers to `RpgStatResolver.ResolveRegen`, whose cases at238–245 only consume RegenFlat/RegenPercent. | **Confirmed missing legacy alias in the actual facade path.** Small resolver integration is justified without changing save/API semantics. Do not describe the helper itself as broken or the option as globally unused. |
| AddResourceCurrentFlat =202 | Source XML explicitly says one-shot (`Enums/BuffStatType.cs:34–35`). Shelf projects it with every active modifier (`Runtime/RpgEffectShelf.cs:316–354`), but no resolver/apply path consumes this enum. | **Declared one-shot behavior is unimplemented.** Finish on accepted application/stack change, after selecting precise rules below; do not put it in recurring RefreshAllDerived. No new save fields needed for a no-replay load policy. |
| AttackSpeedPercent =311 | Only declaration/XML reference in package source (`Enums/BuffStatType.cs:55–56`). `RpgAttackController.cs:761` uses definition.Cooldown unchanged. `Interfaces/IRpgCombatReceiver.cs` has outgoing/movement multipliers, no attack-speed capability. RpgStatPreset includes AttackSpeed, but that is an arbitrary stat ID, not an automatic cooldown consumer. | **Unused/unimplemented option, feature-policy decision.** Preserve enum/value, explicitly list unsupported behavior. Automatic cooldown/wind-up/AI timing changes are not an established contract to infer silently. |

### Legacy HP regen: safe bounded correction

Consume HpRegenPerSecond only for canonical `RpgResourceId.Hp` (`"HP"`); ignore its targetId as other legacy aliases do. Add its clamped-stack flat units/second at the same stage as RegenFlat, before applicable RegenPercent. Reuse the shelf's clamped stack projection. Do not run both the legacy math helper and the resolver over the same modifiers and count them twice. Original three-argument resolver continues delegating to the four-argument path.

Preserve definition.enabled as master ticking gate, existing mode dispatch, onlyWhenAlive/NotFull, spend/damage pauses, caps, tick cadence and no offline immediate healing on load. Disabled/None regen must not start solely from this alias. Positive example: base2/s + legacy3/s ×2 stacks + universal1/s, then +50% =13.5/s. Same legacy modifier with targetId Mana still affects HP, not Mana. Nonpositive resulting rate follows existing no-tick policy. This requires only current resolver/facade tests and truthful docs, not changed profiles or a clock framework.

### One-shot current: bounded completion, explicit event decision

Recommended policy if included in 11.0: grant one unit of each modifier on a **new entry**, and one per **positive effective stack increase**. Nonstack refresh or a refresh already at cap grants zero; removal/expiry never reverses a consumed Current change. Existing shelf IsNew is not enough for stacking: capture effective previous/new counts before external callbacks, rather than multiplying the entire resulting stack count on every reapply. Capture affected IDs/finite amounts then; do not re-read mutable entry/definition after callbacks. Both SO and inline apply routes must agree.

Use existing resource mutation/clamp/HP reconciliation paths; do not create missing resources. If signed values are supported, positive delta uses Increase, negative uses Decrease (raw resource delta, not buffed Damage), with that rule documented. Reject nonfinite one-shot amounts before poisoning a pool. A mixed Max+Current buff must resolve the new Max before applying Current, so HP90/100 plus Max50/Current40 becomes130/150 rather than losing30 to the old ceiling.

**Observable order must be approved:** current ApplyBuff/ApplyInlineEntry is shelf commit → OnBuffApplied → derived refresh. Adding a grant after that callback can lose the grant if the listener throws; adding before derived refresh clips against the old Max. Recommended settled-effect path is reserve/capture accepted stack delta, resolve derived limits, commit current delta, then OnBuffApplied; release guard in finally. This changes what OnBuffApplied observes, so document it rather than claiming zero event-order change. Preserve its existing acceptance/refresh notification cardinality and return value; zero one-shot delta at cap need not make ApplyBuff fail. Bounded generation/reentry is enough; no arbitrary listener rollback promised.

A throwing post-commit listener does not roll back the accepted stack or grant. A later explicit Apply is a new application under the same stack policy: another positive stack can legitimately grant again. Without a request ID this API cannot promise transport/retry deduplication. Use a nonstack or at-cap retry to test that notification failure alone does not replay a consumed grant.

Save/replication already stores the resulting Current and active entries. ApplyProfile/RestoreActiveEffects must **restore**, never route through grant-on-apply. Thus repeated load, client snapshots, formatting/buff refresh, expiration and stat changes cannot grant again, and no serialized “already granted” flag is needed for this policy. Adding reapplication grants after load is a separate explicit Apply, not a restore side effect. Activation still changes previously inert existing content and needs a release note.

### Attack speed: do not invent a consumer contract

If selected separately: an additive concrete GetAttackSpeedMultiplier/optional capability can preserve custom receiver default1; do not add a mandatory member to IRpgCombatReceiver. Specify first whether positive percent divides **future accepted cooldowns** by `(1 + percent/100)`, whether zero/negative effective speed blocks or clamps, and whether CastDelay, projectile speed or AI attempt intervals change. Recommended narrow feature affects next cooldown only, preserves already committed readyAt and leaves those other timings unchanged. Current cooldowns are not in profile DTO, so this needs no profile migration, but it is still a gameplay feature with explicit bounds/reentry/accepted-rate capture and tests. Existing AddStatPercent targeting a configured AttackSpeed stat already affects that stat value; it does not automatically speed attacks. Do not advertise it as a replacement for the missing option's mechanical behavior.

## Minimum regression specifications (not authored/run here)

Owner may add uniquely prefixed `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/Audit20261023NeoRpgContractsTests.cs` and focused Play/Network companion only after permission to write source. Assert public state/events/serialized payload, not implementation fields.

| Change/decision | Minimum observable acceptance |
|---|---|
| Existing final snapshot restore | Base100, explicit effective Max160, Current130; Capture→Apply and Save→Load finish160/130, Percent0.8125 and 10%-regen16/s. No refill on SetMax. A later ordinary Refresh intentionally returns derived100 and clamps Current if no overfill; document this boundary. |
| Valid modifiers/limits | Normal Base100 + flat20 or percent50 snapshots return120/150, never140/225. Repeated ApplyProfile idempotent. Existing cap/overfill/unknown-ID policies preserved; unsaved rows resolve normally. |
| Events/publication | Final resource event and OnProfileLoaded observe final Max/Current/rate. No synthetic death/revival from temporary clamps; network final snapshot matches source. Profile callbacks replacing/rebuilding abort obsolete continuation (existing C-27/C-32 work). |
| Durable base only if selected | New base160 + buff20 yields180; save/load/repeated refresh/removal returns160, cap handled once; shared template untouched. Legacy row lacking base never double-counts modifiers; explicit base0 preserved; future-version/migration policy tested before sanitize. |
| HpRegen alias | HP base2 + legacy3×2 + flat1 +50% resolves13.5/s; flat/tick application matches cadence, Mana unchanged despite legacy targetId, disabled/master/alive/full/pause gates unchanged; helper still returns its existing result; snapshot restore does not heal immediately. |
| One-shot only if selected | New stack +10, next stack +10, cap refresh +0; nonstack refresh +0; repeated refresh/load/snapshot +0; removal +0; SO/inline parity. Mixed max/current130/150, missing target/invalid amount no poison, reentry cannot double-grant, thrown post-commit listener plus nonstack/at-cap retry does not replay a grant (a deliberate new stack remains a new application), observed settled OnBuffApplied order. |
| Attack-speed feature only if selected | +100% changes next cooldown2→1, old readyAt unchanged, custom receiver default1, policy for ≤0/nonfinite factor explicit; CastDelay/projectile/AI interval unaffected; no accidental use of arbitrary AttackSpeed stat without configured contract. |

Existing tests read as context: RpgCharacterResourceServiceTests, RpgCharacterProfileServiceTests, RpgCombatEdgeTests and the audit profile/regen fixtures. Current serializer roundtrip cases do not establish facade Max roundtrip or the missing consumer behavior. All these new acceptance cases remain specifications, not pass results.

## Scope and verification receipt

This task performed source/document/call-site reads and hashing only. No runtime source/tests/public docs/asmdef/package/sample/settings edited, no child agent, build, compiler, Unity/MCP, test runner, package validator, install, service or commit. NativeReview-C.md stays unchanged. Normal UTF-8 text is used; no console-based repair of Russian files.

Read-region/file fingerprints at this contract review (SHA-256; not compilation evidence):

| Full path | SHA-256 |
|---|---|
| `D:/Git/NeoxiderTools/AGENTS.md` | `19e4e16eb1be1463ad4937584e19db783864a2009317ac5fd448eed631ac240f` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | `dd2e3bdcd5289c72b126c4101394541f0087a02cd01bd9e3266847efe1275daf` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Components/RpgAttackController.cs` | `b826bf64fbf932aad55dae867370112a7391d0a2dff17d62a38380f8982a4c7e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgResourceRuntime.cs` | `cac5b5a11e816ac472a8e28a8a9eff6e774605da5c2deb0e88e683f127d0192b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterResourceService.cs` | `57562958371244a698f7293300de28c9f0e3b174aa36532c50d422d8c19a73d9` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileData.cs` | `504467f86274c0e869c355eb6a3ea13d1fd903fe36360175ac521dfbaa60ec67` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCharacterProfileService.cs` | `92bf6baf496877d07de91d318c4a1ec7e7816f178e2d80d29a72953b58c62fe3` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgStatResolver.cs` | `cabb70bb7aaf8224fe8a8822d631f47caaf637e148be9bed30af54da2f749f68` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgCombatMath.cs` | `7003f69bbe6f6b7c5463bc2eb3c0ad9745e26b16ff079459293e9d07e0c6a035` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgEffectShelf.cs` | `b41e90d340c012ea332e7b929f0b7ec592619f1be732409f44af8d866b4f1506` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Runtime/RpgActiveEffectEntries.cs` | `11e9fc8eb0d5042237dff8f81584b0600047ddb25f039d21ad31ad4a108ef6ba` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Enums/BuffStatType.cs` | `1f545d6fa751430fda1acd62af7ec0d52bafab76ddffa5ac50e19a092f628654` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Interfaces/IRpgCombatReceiver.cs` | `7abb83394f1572cdd7445591a05c5a992f601720164cacc3452d683c540a65d2` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgResourceDefinition.cs` | `f443154608443c76814f36b5656124eea8dc589a4ca19a90d2cc89f288dc4520` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgRegenDefinition.cs` | `a2b4cf55c7250d41342531c475ed56da0af4c732c8e63dfe9006a000f8f54315` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/RpgStatPreset.cs` | `dadd115c02f62c1920848d56f8bddc0e1d2a28e38fa7f1dc7b0f86419fc50a86` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/BuffStatModifier.cs` | `6c9a01dbef97b16be463eddc3f5ad852aeb0f9d3744f8fb00c805718a595f81e` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/BuffDefinition.cs` | `b17f3c2a66cf53fa4e8d7653694dba32e78ed79c4e62dec7fedc1dd29351e93b` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Rpg/Data/InlineBuffEntry.cs` | `f1775e02e3ed4ff5fc78deb9bf4576f93cf5c4dd8ca97011359cca0177b89c33` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` | `7c8a4fddbd1e2c65e52a4dc85ee515e0deaed7fcfd6bab6b30440269891110d0` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Samples~/Demo/Scripts/RpgDemoUI.cs` | `78a07d5aed488f6d311b5de955d0a0ac1be1aa6db73a683b8ec35e4cc40d8057` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/README.md` | `a47cd616b7629bf9b3f78b91882e6d26357894667fb9565fa22e84959a73efc6` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgCharacter.md` | `19be6d32c11b10ea151de0ec6d4dc087f3794f3406bdaedb6faef1f86d902a27` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/RpgNoCodeAction.md` | `b8f359f2f7c6fe7501f7d3c3090226657d795c70a9ec93e59781ea82af9e3528` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/InternalTypes.md` | `5788b2775fe6a6829fd9ea60062f9cefee0ae0e2b78aa853779ea9262651311c` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Docs/Rpg/Data/BuffDefinition.md` | `cb918b51267bde47405c9a6bc8086dabf960773a1afd743d3014dd5996c25390` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/RpgCharacterResourceServiceTests.cs` | `b53cd91d60fad026328d9299dc739e877813544e76eff390a2ff5d23431eb338` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/RpgCharacterProfileServiceTests.cs` | `2cbb48550fae49b3a71f515f035e21fdbd142b634dfb844b3276c16f0936f77d` |
| `D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Edit/RPG/RpgCombatEdgeTests.cs` | `05e87ad10e3d62d5ae6fd46d598a5ad2ed337c7b736608468879b19df0c9a5ac` |
