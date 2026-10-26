# Upgrading to NeoxiderTools 11

Version 11.0.0 is the major release described here. The changes below are the accepted contracts for this update. Windows verification recorded on 26–27 October 2026, including focused CardGame sizing/gameplay acceptance and three manual OS-click rounds, is listed in [Verification 11](../Verification11.md) and [package compatibility](PackageCompatibility.md). Results belong to their tested inputs and qualified unchanged graphs. No published release or Git tag is implied by this checkout.

Кратко: обновите настройки сетевого кошелька, проверьте обработчики покупок и применения RPG buff, а также собственное управление временем StateMachine. Существующие serialized поля и sample layout сохраняются. Остальные изменения и точные C# API описаны в канонических руководствах модулей.

## Networked money

The server now controls remote wallet mutations through `ClientMutationAuthority`. Its default is `NetworkAuthorityMode.ServerOnly`. Pure clients cannot directly credit, spend or set a networked wallet under this policy. Direct server/host calls and offline wallets retain their normal behavior.

Use server-side game rules for purchases and rewards. A trusted cooperative game may explicitly select `None` to retain the earlier shared-wallet request behavior. `OwnerOnly` means the owner of this wallet's `NetworkIdentity`, which can differ from the player or wallet save key. These opt-ins authorize amount-based requests; they do not establish that a reward was earned. Requests still have no new acknowledgement contract. Unknown policies/opcodes and remote refunds are rejected. See [Money](Shop/Money.md) and [network contracts](Network/Contracts.md).

Networked Counter arithmetic now sends an operation and operand, then evaluates against the server's current value. Absolute Set remains last-writer-wins. Remote nonfinite input and relative raw float overflow reject before consuming the sender's budget, including before Int conversion; this does not change trusted direct server/offline numeric policy. Selector's received payload now carries exclusions together with its complete active selection. Use matching 11.0 builds on every peer for both wire changes. See [Counter](Tools/Components/Counter.md) and [Selector](Tools/View/Selector.md).

Selector configurations whose logical bounds do not fit Int32 now throw `ArgumentOutOfRangeException`; an overflowing raw `IndexWithOffset` read throws `OverflowException`. Ordinary valid selection/wrapping remains supported. Validate externally supplied counts/offsets before changing configuration.

## Purchase commit and observers

Shop commits its profile and save before publishing purchase notifications. Mutation or persistence failure remains eligible for debit compensation. Once committed, an observer exception is reported separately and does not refund or undo ownership; later bundle notifications continue.

If your game threw from `OnPurchased` or a visual listener to cancel a purchase, move that validation before the purchase. Inventory grant bridges remain best effort, and arbitrary callbacks or several save keys do not form a disk transaction. See [Shop](Shop/README.md) and [grant bridges](Shop/Bridges.md).

Inventory pickup/drop and Box rewards now retain committed quantity and consumed-price ownership across notification failures. Existing public operations keep their normal results; recovery preserves physical slots and item identity. See [Inventory](Tools/Inventory/README.md) and [collections](Bonus/Collection/README.md).

If a failed world drop cannot return every extracted item, `InventoryDropper` retains the remainder for `RetryPendingReturns()`. Keep that dropper and its inventory alive until recovery completes; this sidecar is not part of inventory JSON. A notification exception can follow a committed return, so inspect the current pending quantity before retrying.

## Shared interaction queries

Objects with an identical physical query context share complete raw hits within a stable interaction epoch. Physical layers, dimensions, exact geometry, distance, cameras and trigger policies remain distinct where required. Each object retains its own filtering, hover, input, network and range decisions. `obstacleLayers` keeps its range/LOS meaning; it is not a new universal mouse-target mask.

No new controller or scene wiring is required. Interaction callbacks establish fresh-query boundaries. Application code that manually simulates physics or changes colliders between interaction passes uses the explicit invalidation/fresh-query path documented in [interaction](Tools/InteractableObject/README.md). The cache cannot infer arbitrary unannounced changes from `frameCount`. Target-specific LOS remains necessary. See [Verification 11](../Verification11.md) for recorded 1/100/1000 query-count and dense-hit scope and qualification of unchanged inputs.

## State, pause and exact numeric output

StateMachine handles nested state changes through bounded dispatch and keeps built-in duration anchors per machine. Code that used a shared predicate's `SetEnterTime` to retime an already running machine must call that machine's `SetCurrentStateEnterTime(float)` instead. Standalone predicate setters retain their standalone behavior. An additive constructor accepts `Func<float>` for an explicitly supplied clock. See [StateMachine](StateMachine/README.md).

PausePage and cursor blockers retain shared ownership until the last owner releases it. They restore the captured baseline and only resume the manager they actually paused. GM abandons a pending stale broadcast when a nested transition supersedes it. Existing synchronous UnityEvents are not an atomic listener transaction. See [pause pages](UI/PausePage.md) and [managers](Tools/Managers/README.md).

Integer text retains an exact final formatted value; interpolation and `currentNum` use float storage. Float animations settle the originally requested float target on completion, including transitions from large values to small fractional values. A corrective final `OnTextUpdated` can occur when the last interpolated string differs from that target. Retired tween callbacks cannot overwrite a replacement numeric or literal write. Derived labels compose the SetText enable/disable hooks. See [SetText](Tools/Text/SetText.md).

Screen depth uses the near-plane-relative convention documented in [numeric edge cases](Extensions/NumericEdgeCases.md) and [movement](Tools/Move/README.md); existing scene values are not rewritten automatically. Camera drag retains its existing pixels × sensitivity × axis speed × scaled delta-time tuning; this update does not add the proposed frame-independent option.

## Cards and interrupted animation

Hand admission commits before its layout animation. `TryAddCardAsync` reports admission; cancelling an accepted animation does not remove the card. Rejected deck deals restore their own source generation; resetting a deck preserves cards already accepted by a hand. Replaced layouts and restarted games stop their obsolete continuations. Explicitly cancelled concrete card animations report `OperationCanceledException`, while superseded internal layout work finishes quietly. Handle cancellation where your code awaits these animations. Built-in sorting uses the optional `IOrderedHandView` API; existing custom `IHandView` implementations keep their fallback. See [Cards](Cards/README.md).

## Resource operations, rewards and RPG effects

Core resource preflight and ability costs honor decrease caps. Operation receipts describe the stored increase/decrease, captured before notification and independently of later callback changes to the balance. Positive spends that cannot decrease the stored float balance are rejected. Float rounding still applies: a nominal decrease of 5 from 100,000,000 can store a delta of 8; an input cap is not an exact-currency guarantee. Target-scoped ability delivery retains global observers and their existing publication order. See [Core](Core/README.md) and [Abilities](Abilities/README.md).

CooldownReward eligibility and persisted timer deadlines use the same trusted UTC clock. Ordinary TimerObject retains its default device clock and existing keys. RPG regen/status catch-up delivers at most 1024 logical ticks per entry per call, drops excess whole intervals and retains fractional phase. Ordinary cadence, pause crossing and status UTC expiry retain their rules; NaN/infinite simulation steps do not advance damage/regen. This bound prevents pathological nontermination and is not a frame-time guarantee. See [time](Tools/Time/README.md), [rewards](Bonus/TimeReward/README.md) and [RPG](Rpg/README.md).

RPG v2 profiles restore their saved effective Max at the load boundary, before Current and percentage regen. They do not reinterpret that value as BaseMax. A later normal derived refresh can recompute an effective override from the base; use existing investment upgrades for durable authored growth.

`RpgCharacter.ApplyLevelGrowth()` explicitly applies the current level in manual or deferred workflows. Ordinary buff/stat refresh retains the last applied growth level. The v2 profile does not persist this anchor: automatic modes restore it from the loaded level; other modes start at one and apply growth explicitly according to the game's policy. No v3 profile conversion is performed. The additive network command uses value 23 and requires the updated adapter on both peers.

`HpRegenPerSecond` is consumed as the legacy HP alias. `AddResourceCurrentFlat` grants on a new effect entry or positive effective stack increase, after derived limits settle and before `OnBuffApplied`. Refresh at cap, nonstack refresh, removal and profile/network restore do not grant again. A postcommit listener exception does not undo the grant. This activates previously inert authored content and changes what the application callback observes. `AttackSpeedPercent` remains an unsupported enum-only option; it does not silently change cooldown, wind-up or AI timing.

RPG projectile query capacity is a buffer hint rather than a hidden target limit. Complete hit collection sorts by distance and stable identity within each dimension; 3D delivery still precedes 2D, and the configured gameplay hit limit remains. This can change which target receives a limited hit when an old buffer saturated. See [projectiles](Rpg/RpgProjectile.md).

## Saved configuration and generated resources

Invalid enabled File encryption makes `SaveProviderSettings.CreateProvider()` and first lazy facade initialization throw `InvalidOperationException` instead of falling back to plaintext. `TryCreateProvider(out ISaveProvider, out string)` provides recoverable configuration validation. Correct the key/IV configuration and retry; invalid assigned scene settings preserve the active provider and do not automatically retry merely because the asset was edited. Existing encrypted files are not opened by rejected construction. See [Save](Save/README.md).

Required JSON arrays must be explicit: FileSaveProvider requires one root `items` array, and `LeaderboardLegacyJson.TryDeserialize` requires one root `users` array. An empty `[]` remains valid; missing, null, wrong-kind or ambiguous duplicate members are rejected. A nested name or quoted text does not supply the root member. Existing wire keys are unchanged.

Inventory Save/Load uses `InventorySaveData.ToJson()` and `InventorySaveData.FromJson(json)` to keep empty slots and simple stacks distinct from stateful item instances. Custom snapshot persistence should use these methods instead of serializing the domain DTO directly with `JsonUtility`: Unity can materialize an absent inline instance with the domain's `Count = 1` default. The codec uses private zero-default wire fields and clears only a completely empty optional wire instance. Explicit item-id-zero/keyless/count-one objects, identities, state versions and payloads remain real data for the existing admission rules. Existing JSON keys, version 1, legacy metadata, layout, capacity and state keys remain unchanged. An external payload that represents an intended null exactly like a real count-one instance is ambiguous; it is not repaired by deleting real data. See [inventory persistence](Tools/Inventory/InventoryComponent.md).

Progression initialization can be retried after a dependency or profile-read failure before commit. Reentrant profile APIs during unfinished setup now throw `InvalidOperationException` instead of using defaults or starting recursive loads; defer them until initialization commits. An observer error after commit keeps the initialized profile, reward watermark and newer provider binding. Restore Core before loading Progression; reloading the same active profile preserves the session's highest rewarded level. See [Progression](Progression/README.md).

Leaderboard users gain a persistent key while retaining the legacy readonly `id`. A unique key or canonical reference can reconnect the player; ambiguous/keyless rows require explicit binding. Names, scores and ranks are not identity. Existing name/score save keys remain. Authored-token recovery uses the serialized key's `FormerlySerializedAs("id")`; raw legacy aggregate JSON has the explicit `LeaderboardLegacyJson.TryDeserialize` path. Consult [recorded frozen-source verification](../Verification11.md) for actual prefab-token recovery and aggregate round-trip results; authored source alone is not evidence. No bulk prefab rewrite is performed. See [leaderboards](Tools/FakeLeaderboard/README.md).

Generated preview sprites, drawing pools, demo UI and native assets are released by their actual owners. Preserve authored/shared assets and do not retain cache-owned preview objects as persistent configuration. Tracked coroutines use their handle cancellation API; raw native StopCoroutine has the observation boundary documented in [lifecycle and ownership](Extensions/LifecycleAndOwnership.md).

HtmlToUitk reserves actual filenames/CSS references and canonical asset aliases; conflicting payloads
reject before commit instead of losing distinct outputs. Newest projections survive same-root reentry
and obsolete failure cleanup. Default mixed-case localization keys and explicit custom markers retain
their source keys. See the [primary guide](UI/HtmlToUitk.md) and
[current CLI/native/manual verification](UI/HtmlToUitkVerification11.md); this is not browser/JS parity.

The Lobby demo generator requires a fresh `.unity` destination inside Assets and refuses existing files/assets. Lobby and gameplay start from persistent Editor templates, so additive creation does not require saving unrelated untitled work. The menu retains its Single replacement after the save/continue decision; cancellation leaves current work intact. See [Editor windows](Editor/EditorWindows.md#lobby-demo-authoring-mirror).

Для генератора lobby выберите новый путь `.unity` внутри Assets: существующие assets не перезаписываются. При additive-создании посторонние несохранённые сцены сохраняют своё состояние; обычная команда меню заменяет сцену только после решения пользователя о сохранении и продолжении.

## Samples and verification

Release samples stay in `Samples~`; developer `Samples` and imported `Assets/Samples/NeoxiderTools/11.0.0/<sample name>` are separate supported layouts. Existing imported 10.x copies are not deleted or rewritten. Follow [samples](Samples.md) when importing the update.

Legacy delivery is choose-one Core (`NeoxiderTools.Core.v11.0.0.unitypackage`, `samples: []`) or Complete
(`NeoxiderTools.v11.0.0.unitypackage`, all three visible `Samples/...` paths). Main UPM source is unchanged.
The separate `HtmlToUitk.Cli.v11.0.0.zip` requires tested.NET 10/LatestMajor while retaining net9 target;
no runtime9 is bundled. Final archive export/import and publication are separate in [Verification 11](../Verification11.md).

Thirteen existing package metadata files had duplicate records or a malformed header. This update retains the first complete existing GUID/importer record and removes only the appended record; the sample-header repair retains its GUID. No current serialized reference used a removed duplicate GUID. If an external project explicitly referenced one of those malformed assets by its second GUID, inspect that reference when upgrading rather than regenerating asset identities. New test metadata uses valid unique GUIDs.

В тринадцати старых `.meta` сохранены первые полные GUID и настройки importer; удалены только повторные записи либо повреждённый префикс заголовка. Ссылки и GUID существующих корректных assets не пересоздаются.

The repository's six bundled Demigiant DLL importer records were reserialized by Unity 6.3 to schema 3 for current Unity compatibility. Native settings and full native importer state compared equal before/after; the DLLs and GUIDs are unchanged. This is a repository dependency-metadata update; integrated 6.3/6.6 verification is recorded separately.

Use an isolated validation host for the broad package suites. Read [EditMode testing](Testing.md), [PlayMode testing](PlayModeTesting.md) and [package compatibility](PackageCompatibility.md) for dependencies and actual pass/fail/skip evidence. Historical reports do not certify the current 11.0.0 source.
