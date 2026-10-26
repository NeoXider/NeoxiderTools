# Neo.Cards: audit

Status: source audit and scoped edits complete; central compilation and Unity test execution required.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Cards/Neo.Cards.asmdef`.
- Scope: 45 C# files; nested assemblies excluded.
- Changes: 17 owned runtime sources, 10 existing public documentation pages, three new regression files with 41 cases total and unique metadata.
- No commits, shared configuration/asmdef changes, Unity/MCP calls, runners, installations, or child agents. Self-review only; no independent QA was performed.
- `Scripts/Cards/Editor`, its new regression file, and `Docs/Cards/Editor.md` belong to the concurrent editor worker and were not edited.

## Scope

- `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs`
- `Assets/Neoxider/Scripts/Cards/Components/CardComponent.cs`
- `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs`
- `Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs`
- `Assets/Neoxider/Scripts/Cards/Config/CardAnimationConfig.cs`
- `Assets/Neoxider/Scripts/Cards/Config/CardLayoutSettings.cs`
- `Assets/Neoxider/Scripts/Cards/Config/CardSettingsRuntime.cs`
- `Assets/Neoxider/Scripts/Cards/Config/DeckConfig.cs`
- `Assets/Neoxider/Scripts/Cards/Config/HandLayoutType.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Data/CardData.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/BoardMode.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/CardDisplayMode.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/CardLocation.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/CardViewAnimationType.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/DeckType.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Rank.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/ShuffleVisualType.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/StackZSortingStrategy.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Suit.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardContainer.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardDisplayMode.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardView.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardViewAnimations.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IDeckView.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IHandView.cs`
- `Assets/Neoxider/Scripts/Cards/Core/Model/CardContainerModel.cs`
- `Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs`
- `Assets/Neoxider/Scripts/Cards/Model/BoardModel.cs`
- `Assets/Neoxider/Scripts/Cards/Model/DeckModel.cs`
- `Assets/Neoxider/Scripts/Cards/Model/HandModel.cs`
- `Assets/Neoxider/Scripts/Cards/Poker/PokerCombination.cs`
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandEvaluator.cs`
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandResult.cs`
- `Assets/Neoxider/Scripts/Cards/Poker/PokerRules.cs`
- `Assets/Neoxider/Scripts/Cards/Presenter/CardPresenter.cs`
- `Assets/Neoxider/Scripts/Cards/Presenter/DeckPresenter.cs`
- `Assets/Neoxider/Scripts/Cards/Presenter/HandPresenter.cs`
- `Assets/Neoxider/Scripts/Cards/Utils/CardComparer.cs`
- `Assets/Neoxider/Scripts/Cards/Utils/CardLayoutCalculator.cs`
- `Assets/Neoxider/Scripts/Cards/Utils/CardSpriteNameParser.cs`
- `Assets/Neoxider/Scripts/Cards/View/CardView.cs`
- `Assets/Neoxider/Scripts/Cards/View/CardViewAnimationTemplates.cs`
- `Assets/Neoxider/Scripts/Cards/View/CardViewUniversal.cs`
- `Assets/Neoxider/Scripts/Cards/View/DeckView.cs`
- `Assets/Neoxider/Scripts/Cards/View/HandView.cs`

## Findings and fixes

All 45 inventory files above were read. Findings below are demonstrated by source paths and concrete regression scenarios; the authored Unity regressions have **not** been executed by this worker.

Severity: P1 = lost/incorrect gameplay state or exposed hidden card art; P2 = inconsistent public contract or invalid input being scored.

| ID | Severity, trigger, and observable consequence before the fix | Source and completed change |
|---|---|---|
| F01 | P1. Initialize with an explicit/custom list, draw, then reset: `Reset()` called `Initialize(DeckType)`, producing a standard deck instead of the initialized composition. Passing `deck.Cards` or a deferred query cleared its own source; an enumeration failure destroyed the previous draw/discard state. | `Model/DeckModel.cs`, `Initialize(IEnumerable<CardData>)`, `Initialize(DeckType)`, `Reset`: snapshot explicit input before mutation, retain its original order/duplicates for reset, and clear that snapshot when standard initialization is chosen. Null/failing enumeration leaves prior state intact. |
| F02 | P2. A custom asset ID such as `" wolf "` generated `CardData.CustomId == "wolf"`, but sprite lookup compared the untrimmed entry and returned null. `"wolf"` and `" wolf "` also passed duplicate validation while producing equal identities. | `Config/DeckConfig.cs`, `GetCustomSprite`, `ValidateCustomCards`: trim IDs consistently and retain ordinal case sensitivity. `GenerateCustomDeck` also tolerates a null serialized list, matching validation/lookup handling. |
| F03 | P1. `DeckPresenter.Initialize` selected only `GameDeckType`, silently replacing custom cards with a standard deck; a previous standard trump preview could remain in a custom configuration. | `Presenter/DeckPresenter.cs`, `Initialize`: use `DeckConfig.GenerateDeck()`, and hide the standard preview for custom/non-36 configurations. |
| F04 | P1. `hand.AddRange(hand.Cards)` treated the source backing list as its pending list, appended one card, and invalidated the enumerator, leaving partial state without a final hand-change event. Listener mutation of an external source list caused the same failure. | `Model/HandModel.cs`, `AddRange`: always snapshot before capacity validation and additions. Streaming `AddRangeUntilFull` deliberately remains incremental. |
| F05 | P1. `BoardModel(1)` reported `CanAdd == false` after one card, but inherited `Add` still inserted a second card and emitted change events. | `Core/Model/CardContainerModel.cs`, `Add`: enforce virtual `CanAdd` before mutation. `Model/BoardModel.cs`: reject negative constructor capacity; zero capacity rejects all cards. |
| F06 | P1. Re-adding the same component/presenter/view inserted another reference to one physical card, inflated counts, and could duplicate subscriptions or make later removal inconsistent. | `Components/HandComponent.cs`, `Components/BoardComponent.cs`, `Presenter/HandPresenter.cs`, `View/HandView.cs`: reject duplicate object instances. Board force-placement also returns before capacity expansion for an existing member. Separate objects with equal `CardData` remain supported. |
| F07 | P1. Presenter rank/suit sorting changed model and presenter order, then only called `ArrangeCardsAsync` on the unchanged view list. Displayed order and subsequent index-based removal disagreed. | `Presenter/HandPresenter.cs`, both sort methods: synchronize view membership with existing `IHandView` remove/add APIs before final arrangement. No interface member was added. |
| F08 | P2. `CardPresenter` cached the requested face flag even when `CardViewUniversal` enforced AlwaysFaceUp/AlwaysFaceDown; direct view flips also left the presenter stale. | `Presenter/CardPresenter.cs`: `IsFaceUp` reads the actual view; SetData/Flip/FlipAsync delegate the state transition to that view. Public getter signature is retained. |
| F09 | P1. Flip a rendered card with no back sprite, or clear overrides without a configured replacement: `UpdateVisual` returned on null and retained the previous face artwork. | `Components/CardComponent.cs`, `View/CardView.cs`, `View/CardViewUniversal.cs`, `UpdateVisual`: assign the resolved sprite even when null, clearing stale art for both Image and SpriteRenderer. |
| F10 | P1. Deal to an already full hand: the deck consumed a logical/visual card, the hand rejected it, and the deck still signalled a successful deal. | `Components/DeckComponent.cs`, `DealToHandAsync`: reject an already full destination before touching either source. Two pre-existing local `var` declarations in this changed file now use explicit types. Concurrent capacity changes across later awaits remain a separate risk below. |
| F11 | P2. Five custom cards all had default suit/rank fields, so poker reported a flush; invalid standard enum values could also produce fabricated results. | `Poker/PokerHandEvaluator.cs`, `Evaluate`: reject custom or out-of-range suit/rank cards with `ArgumentException`. Valid standard evaluation and joker filtering are preserved. Existing locals in this edited file now have explicit types. |
| F12 | P1. A tied round with insufficient cards returned the pile to data queues, while hand-backed `PlayerCardCount`/`OpponentCardCount` ignored those queues. The game declared a false winner and could not draw the reserved cards. | `Drunkard/DrunkardGame.cs`: public counts include scene hands plus reserves, matching the already-existing queue fallback in draw methods. |
| F13 | P1. Initial dealing or trick/war banking into a finite scene hand silently rejected overflow, losing card data and leaving rejected views without an owner. | `Drunkard/DrunkardGame.cs`, initial distribution and both normal/war winner paths: reuse one local banking operation that adds to the hand or enqueues the data and releases the rejected visual. F12 makes those overflow reserves countable and playable. |

### Review coverage and boundaries

- **Components (4/4):** followed card initialization, serialized defaults/references, pointer subscriptions, reparenting, add/remove/sort, capacity, board slot generation, deck empty/reset events, visual stacks, asynchronous layout/deal paths, and destroy cleanup. Existing fields and public signatures are retained. Runtime Inspector buttons and deferred destruction still require central Editor/Play Mode checks.
- **Config (5/5):** checked `CardAnimationConfig`, `CardLayoutSettings`, `CardSettingsRuntime`, `DeckConfig`, and legacy `HandLayoutType`/`CardLayoutType` mappings. ScriptableObjects contain reusable values/assets rather than scene-object dependencies. Global animation fallback remains shared static state; multiple-deck/domain-reload-disabled lifecycle is a follow-up check.
- **Data, enums, interfaces, base model (17/17):** inspected all `CardData` constructor paths, custom identity/equality/hash/rank-vs-group semantics, joker behaviour, all nine enum files, all six interface contracts, and `CardContainerModel`. Custom equality intentionally uses ID; comparison can use SortValue, so equal definitions with different metadata are the caller's responsibility. `Mutable` remains a compatibility escape hatch.
- **Models (3/3):** traced standard/explicit initialization, shuffle multiset preservation, top-index draws with duplicates, discard/reset, container events, finite/unlimited hand capacity, range operations, index mutations, suit/rank queries, and min/max selection. Existing duplicate-draw and joker-only min/max fixes were retained.
- **Poker (4/4):** reviewed all combination branches, wheel/royal straight detection, subset generation, rank/kicker comparison, null winner entries, ties, Hold'em composition, and outs. Evaluator remains intended for legal standard decks; duplicate physical cards and combinatorial cost for large collections remain input responsibilities.
- **Presenters (3/3):** traced model/view order, generated composition, duplicate identity vs instance membership, click subscriptions, return/reset/disposal, face state, and custom view contracts. Existing public interfaces were reused, rather than adding an adapter hierarchy.
- **Utilities (3/3):** reviewed all comparer branches, trump ordering, all six layout modes/jitter, parser tokens/numeric/compact/joker/back handling and canonical naming. Classic rank/suit comparers and queries are not a custom-game rules engine.
- **Views (5/5):** reviewed both card views, `DeckView`, `HandView`, and every animation template, including renderer clearing, pointer lifecycle, tween ownership/linking, loop dictionaries, removal vs destruction, and animation configuration precedence. Cut/riffle shuffle timing and generic FlyIn dispatch are existing limited implementations, now described accurately in internal-type docs.
- **Drunkard (1/1):** reviewed all 1,160 original lines: startup/caching, mixed hand/queue modes, normal tricks, repeated/short wars, hidden-vs-visible pile matching, reserves, events, spawning/destruction, restart distribution, and asynchronous round lifecycle. The two demonstrated conservation defects are fixed; active-round restart remains explicitly unverified below.

Coverage totals: 4 + 5 + 17 + 3 + 4 + 3 + 3 + 5 + 1 = **45 owned C# files**. Dependencies/tests were read as context. Nested editor source was excluded from the owned review and edits.

### Regression artifacts

- `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsModelTests.cs`: **17 cases**. F01: explicit/reset snapshots, standard reinitialization, deferred self-source, failed enumeration/null atomicity, empty composition; F04: self-range and listener-mutated source; F05: rejection without state/events and capacity boundaries; F11: custom/invalid inputs plus a valid royal flush with an ignored joker.
- `Assets/Neoxider/Tests/Edit/Cards/Audit20261023NeoCardsSceneTests.cs`: **23 cases**. F02 normalized sprite/validation identity; F01/F03 component/presenter custom reset; F10 full-hand rejection with/without a visual stack; F08 fixed/direct view face state; F06 identity rejection while retaining equal-data copies; F07 both sort directions with model/view order, real transform positions, and subsequent removal; F09 all three card types with both renderer types.
- `Assets/Neoxider/Tests/Play/Audit20261023NeoCardsDrunkardTests.cs`: **1 public-flow scenario**. F12 plays two short-war rounds with scene hands and asserts no false winner. F13 restarts a deterministic 54-card game into one-slot hands, asserts 27 total cards per side, then plays a war and asserts all 54 remain.
- Tests assert public output/state and use real module views/models. Serialized fields are used only to author fixtures; Play Mode reflection only supplies Inspector configuration. Each new C# file has its own GUID `.meta`.
- The null-list tolerance in F02 and private force-placement early return in F06 were checked statically; their complete serialized/restore workflows still need central Unity coverage.

## Documentation

Updated ten existing English pages under `Assets/Neoxider/Docs/Cards`, without adding a competing module entry point:

1. `README.md`: pure runtime example, reset snapshots, capacity, instance ownership, component/Inspector path, view face/order contracts, missing art, and classic rules boundaries.
2. `DeckConfig.md`: trimmed ordinal custom identity, duplicate definitions vs copies, presenter parity, missing-back rendering.
3. `DeckComponent.md`: actual Inspector setup, C# deal flow, reset/visual ownership, full-hand rejection, and serialized competing deals.
4. `HandComponent.md`: duplicate-instance behaviour, explicit C# types, range snapshots, and ownership.
5. `BoardComponent.md`: duplicate placement and pure-model capacity contract.
6. `View/HandView.md`: membership order, lifetime, presenter sorting, and compatibility-path layout cost.
7. `View/CustomCardViewGuide.md`: actual face state and reusable remove/add contract for custom views.
8. `Poker/PokerHandEvaluator.md`: concrete runtime API, input rules, joker filtering, legal decks, scene integration, and complexity.
9. `InternalTypes.md`: corrected enum members, global settings, model/presenter relationships, interfaces, and animation limitations that previously described nonexistent APIs.
10. `Examples/Drunkard.md`: hand/queue setup, total counts, overflow reserves, and restart timing.

Reuse receipt: read the applicable NeoxiderTools Cards/module references and searched the collection/layout extension reference. Changes reuse existing `CardData`, `ICardContainer`, `IHandView`, component/model methods, and BCL snapshots/queues. The Drunkard banking helper is local game-specific glue around those APIs; no equivalent existing bank-to-hand-or-reserve operation was found. No packages/helpers were installed.

## Substantial improvement proposal

### P01 — Atomic runtime card-zone transfers with cancellable visual adapters (not implemented)

**Problem.** Current owners expose independently mutable models and view lists. Transfers consist of draw/remove, optional animation awaits, then add. A destination can fill or a scene can restart between those steps; membership can drift or success can be reported after rejection. `CardData` equality also represents a definition rather than a physical copy. Correct hand sorting currently needs multiple existing view operations, causing repeated layouts and immediate repositioning before the final animation.

**Proposed architecture/public API.** Introduce a plain C# `CardInstance` containing a stable `CardInstanceId` and `CardData`, plus a runtime `CardZoneModel` with an ordered read-only collection, capacity policy, and one batched change event. `CardTransferService.TryTransfer(source, destination, instanceId, out CardTransferResult)` preflights both owners and atomically changes both zones. `CardTransferResult` distinguishes destination-full, missing-source, duplicate-instance, and success; data equality never identifies a physical copy. For animations that must precede the commit, an explicit reservation handle must support `Commit`/`Cancel` with exactly-once release.

Adapters animate a completed transfer result, rather than owning the authoritative move. An optional `IOrderedHandView` extension can expose `SetOrderAsync(IReadOnlyList<ICardView>, bool animate, CancellationToken)` and apply one layout pass; the existing `IHandView` interface remains intact for older custom views. Each adapter owns a linked operation cancellation source/generation, so restart cancels prior work and late continuations cannot emit successful events for a replacement game. A later `DrunkardRoundModel` can produce pure draw/award/war transitions using the same zones, leaving `DrunkardGame` as scene rendering and UnityEvent forwarding.

**Compatibility/migration.** Add APIs within the existing runtime module first. Preserve serialized component fields, enum numeric values, existing UnityEvents, `CardData` definition equality, and old component/presenter entry points. Legacy methods delegate through an opt-in adapter until parity is demonstrated. Do not remove `Mutable` or change saved card identity formats in the audit patch. Existing games can continue passing `CardData`; new instance IDs are assigned at zone initialization and must be persisted explicitly only when the game opts into instance-based saves. No scene/prefab migration is needed for default legacy behaviour.

**Acceptance scenarios.** Two concurrent deals to the last free hand slot conserve every instance and signal one success; full-zone transfer changes neither source nor destination; equal-definition cards retain separate identities; cancellation before/during/after animation releases reservations exactly once; restart mid-flip/mid-war leaves no prior callbacks or orphaned views; sort plus indexed removal keeps model/view order with one layout batch; mixed queue/hand Drunkard and one-slot overflow preserve the complete multiset across repeated wars, restart, and scene destruction. Supply deterministic pure-model tests and central Play Mode animation/lifetime tests, including a custom `IHandView` compatibility implementation.

**Effort/risk.** Approximately 5–8 engineering days for the runtime transfer core, opt-in adapters, and targeted tests; a separate 3–5 days for a pure Drunkard round model. Medium-to-high integration risk: event timing, saved instance IDs, custom-view lifetime assumptions, and cancellation semantics need migration tests. Keep this separate from the completed focused fixes.

## Verification and remaining risks

### Actual worker validation

- Confirmed `HEAD == ca198556` and matched all 45 manifest source paths, excluding the nested editor assembly.
- Scoped `git diff --check -- <owned paths>` and explicit whitespace checks for new/untracked artifacts; no runners were invoked.
- Strict UTF-8 decoding and absence of U+FFFD replacement characters in the 45 inventory sources, ten changed public docs, three new tests and metadata, and this report. Russian text was read as UTF-8; no visual console-based encoding rewrites were made.
- Read-only lexical delimiter checks for all 45 inventory files plus the three owned new test files. This is a structural scan, **not C# compilation**.
- Existing public/protected declaration-line comparison on the 17 changed runtime files; no removed declarations. Existing private field names are retained, including serialized fields; added snapshot state is runtime-only.
- Verified all three new `.meta` GUIDs are unique against `Assets/**/*.meta`, exact test filenames use the assigned prefix, new test locals use explicit types, and serialized fixture field names exist in runtime sources.
- Checked 41 module-relative file targets in edited docs (anchor fragments were not validated) and the required test-assembly references. Concurrent `Audit20261023NeoCardsEditorTests.cs` is not one of this worker's three regression artifacts.

Final static receipt: **62 owned text files** decoded successfully; **48 C# files** passed lexical balance; **17 changed sources** retained declaration lines/private field names and have explicit locals; the three exact new test files contain **17 + 23 + 1 = 41 unexecuted cases**; all three new GUIDs were unique; scoped **`git diff --check` exited 0**. Git emitted only CRLF-conversion notices for the two model files, not whitespace errors.

SHA-256 over ordered `manifest path:source SHA-256` lines for the 45 final owned sources: `227dcf4b204e564f3251761149476a75469f3ed9cbec27f1314fc9475d3c25b1`. This fingerprint excludes nested editor source and test/docs content.

### Coordinator actions / unexecuted validation

1. `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` currently lacks an explicit **`UnityEngine.UI`** reference needed by the new Image regression cases. Add centrally; the existing assembly already references `Neo.Cards` and `UniTask`.
2. `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` currently lacks **`Neo.Cards`** and **`UniTask`**, required for the new Drunkard scenario. Add centrally before import/compilation. No shared asmdef was edited here.
3. Run the 40 new Edit Mode cases, existing Cards suites, and the new Play Mode scenario. Compilation, tests, import/console checks, visual motion, resource lifetime, and player builds remain **unverified**, as required by the worker restrictions. Compile under the supported Unity 6.0–6.4 and 6.5+ branches centrally; no obsolete object-ID API was added.

### Remaining source risks / follow-up evidence

- **P1, concurrent mutation during animations:** `DeckComponent.DealToHandAsync` can still face a destination filled by another caller after its entry guard and before `hand.AddCardAsync`; the latter returns no acceptance result. Hand/board layout routines also retain calculated arrays across awaits while their memberships can change. P01 addresses reservations/atomic mutation; current docs require serialized competing deals.
- **P1, restart/interrupt lifecycle:** `DrunkardGame.RestartGameAsync` resets flags/queues without cancelling the existing `PlayRound`; its `_ct` belongs to object destruction. Several card flip tweens are not stored/linked consistently, and universal one-shot/template animations can outlive their caller. Central Play Mode must exercise mid-round restart and destruction before changing completion/cancellation event semantics. This patch does not claim those paths are fixed.
- **P1, mixed logical/visual deck operations:** `DrawCard` creates a new view without consuming an existing visual-stack view; `Shuffle` changes model order without reordering a stack. Mixing these with stack-based dealing can drift data/view identity. The public guide now states the consistent stack workflow; a unified zone owner is proposed rather than a silent ownership migration.
- **P2, compatibility/performance:** sort synchronization uses repeated nonanimated remove/add layout passes and can change a custom view's side effects or animation appearance. The four deterministic sort cases cover state/order; motion still needs visual QA. Classic suit/rank helpers/comparers expose built-in fields for custom cards, so custom rules should use IDs/groups/SortValue rather than assuming generic query support.
- **Lifecycle/configuration checks:** domain reload disabled with shared global animation fallback; empty-deck preview refresh; manual vs generated board slots; Inspector button invocation outside Play Mode; configured zero/negative durations. These were inspected in source, but no Unity behaviour is certified by static checks.

Проверены все 45 файлов своей сборки. Исправлены потери и рассинхронизация карт, добавлены 41 регрессионный сценарий и актуализированы публичные инструкции. Unity и тестовые раннеры не запускались; ссылки тестовых сборок и проверки прерываемых анимаций переданы координатору.
