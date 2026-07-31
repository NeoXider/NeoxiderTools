# NeoxiderTools — game systems reference

Dense API reference for gameplay modules. Verified against source in
`Assets/Neoxider/Scripts/<Module>/`. Singletons expose `.I` (see tools.md).

> Combat (damage/abilities/buffs/projectiles) is NOT here — it moved to its own module in v10:
> see **abilities.md** (`Neo.Abilities`, supersedes `Neo.Rpg`).

---

## Bonus / Slot — `Neo.Bonus`

### `SpinController` (MonoBehaviour)
Slot-machine orchestrator. Deducts bet, drives `Row` reels, evaluates paylines.

| Member | Notes |
|---|---|
| `void StartSpin()` | Kicks off spin coroutine |
| `bool IsStop()` | True when all reels idle |
| `SpinResult GetLastResult(bool refreshIfIdle = true)` | Returns immutable result struct |
| `float ChanceWin {get;set}` | 0–1 forced-win probability |
| `int ActivePaylineCount {get;set}` | |
| `int BetSelectionIndex {get;set}` | |
| `void ForceNextOutcome(int[,] idsBottomUp)` | shape=[cols,rows], y=0=bottom |
| `bool ForceNextOutcomeOnPayline(int lineIndex, int symbolId)` | |
| `void AddBet()` / `RemoveBet()` / `SetMaxBet()` | |
| `void AddLine()` / `RemoveLine()` | |
| `int LastPayout` | |
| `IReadOnlyList<int> LastWinningPaylineIndices` | |
| `void StartEconomySpin()` | 9.13.0: builds the whole outcome from the assigned `SlotEconomyDefinition` (weighted pick per cell + special/wild conversion per active payline), queues via `ForceNextOutcome`, spins — no hand-rolled economy chain |
| `int[,] BuildEconomyOutcomeMatrix()` | building block (+ deterministic `Func<int>`-picker overload for tests/replays/server) |
| `List<LineResult> EvaluateActivePaylinesWithEconomy()` | one `LineResult` per active payline of the settled grid |

Events: `OnStartSpin`, `OnEndSpin`, `OnEnd<bool>`, `OnWin<int>`, `OnLose`,
`OnWinLines<int[]>`, `OnChangeBet<string>`, `OnChangeMoneyWin<string>`

`SpinResult` struct: `int[,] SymbolIds`, `int[] WinningLines`,
`int[,] WinningLineSymbolIds`, `int Payout`, `bool IsWin`

```csharp
var spin = GetComponent<SpinController>();
spin.ChanceWin = 0.3f;
spin.OnWin.AddListener(payout => Debug.Log($"Won {payout}"));
spin.StartSpin();
SpinResult r = spin.GetLastResult();
Debug.Log(r.IsWin);
```

### `Row` (MonoBehaviour)
Single reel column; driven by `SpinController`.

```csharp
void Spin(SpritesData allSpritesData, int[] targetVisibleIdsBottomUp)
void Stop(bool animate = true)
SlotElement[] GetVisibleTopDown()
SlotElement[] GetVisibleBottomUp()
bool is_spinning
UnityEvent OnStop
```

### `SlotElement` (MonoBehaviour)
Single symbol cell in a reel.

```csharp
int id { get; private set; }
void SetVisuals(SlotVisualData data)
void SetVisuals(SlotVisualData data, bool smooth)
void SetMotionStretch(float yScale)
```

### `WheelFortune` (MonoBehaviour)
Prize wheel.

```csharp
void Spin()              // respects _singleUse flag
void Stop()              // begin deceleration
SpinState State          // Idle/Spinning/Decelerating/Aligning
bool canUse {get;set}
GameObject[] Items
GameObject GetPrize(int id)
static int ResolveSectorIndex(float wheelEulerZ, float arrowEulerZ, float wheelOffsetZ, int itemCount)
UnityEvent<int> OnWinIdVariant
```

### `WheelMoneyWin` (MonoBehaviour) — companion to WheelFortune
```csharp
void Win(int id)         // looks up wins[id], calls Money.I.Add(wins[id])
int[] wins               // serialized prize amounts per sector
```

### `LineRoulett` (MonoBehaviour) — roulette-style line picker
```csharp
void StartRolling()
UnityEvent<int> OnWin
```

### `Box` (MonoBehaviour, SaveableBehaviour) — collection progress box
```csharp
void AddProgress()                    // adds AddProgressAmount
void AddProgress(float amount)
void ChangeProgress(float amount)     // delta (can be negative)
void TakePrize()                      // requires CheckProgress == true
bool CheckProgress                    // progress >= _maxProgress
float progress {get;set}              // persisted
ReactivePropertyFloat Progress
float MaxProgress
float AddProgressAmount
UnityEvent OnTakePrize
UnityEvent OnProgressReached
UnityEvent OnProgressNotReached
UnityEvent<bool> OnChangeProgress
```

### `ItemCollection` (MonoBehaviour) — single collectible item in a collection
```csharp
void SetEnabled(bool active)
void SetData(ItemCollectionData data)
void Unlock()                         // calls Collection.I.AddItem(ItemId)
int ItemId
bool IsEnabled
UnityEvent<bool> OnChangeEnabled
UnityEvent OnActive
UnityEvent OnDeactivated
```

`Collection` (MonoBehaviour, singleton `.I`) — manages an `ItemCollectionData[]`. Key API: `ItemCollectionData GetPrize()` (picks a random locked item, unlocks + saves it, fires `OnGetItem`; returns null if all items already unlocked), `void AddItem(int id)`, `bool TryAddItem(int id)`, `void AddItem(ItemCollectionData data)`, `void RemoveItem(int id)`, `bool HasItem(int id)`, `void ClearCollection()`, `void UnlockAllItems()`, `float GetCompletionPercentage()`, `string GetCompletionCountText()`, `int ItemCount`, `int UnlockedCount`. Events: `OnGetItem<int>`, `OnLoadItems`, `OnItemAdded<int>`, `OnItemRemoved<int>`, `OnCompletionChanged<int,int>`, `OnCompletionPercentageChanged<float>`.

---

## Cards — `Neo.Cards`

### `DeckComponent` (MonoBehaviour)
Full deck lifecycle with optional async deal animations.

```csharp
void Initialize()
void Shuffle()
void Reset()
CardComponent DrawCard(bool faceUp = true)
List<CardComponent> DrawCards(int count, bool faceUp = true)
async UniTask<CardComponent> DrawCardAsync(Vector3 targetPos, bool faceUp = true, float duration = 0.3f)
void DealToHand(HandComponent hand, bool faceUp = true)
async UniTask<CardComponent> DealToHandAsync(HandComponent hand, bool faceUp, float? moveDuration = null)
void ReturnCard(CardComponent card, bool toTop = false)
// props
DeckModel Model
int RemainingCount
bool IsEmpty
CardData? TrumpCard
Suit? TrumpSuit
// events
OnInitialized, OnShuffled, OnDeckEmpty
OnCardDrawn<CardComponent>
OnCardDealt<CardComponent, HandComponent>
OnVisualStackBuilt
OnShuffleVisualStarted<ShuffleVisualType>
OnShuffleVisualCompleted
```

```csharp
var deck = GetComponent<DeckComponent>();
deck.Initialize();
deck.Shuffle();
var card = await deck.DrawCardAsync(hand.transform.position, faceUp: true);
```

### `HandComponent` (MonoBehaviour)
Player or AI hand of cards.

```csharp
void AddCard(CardComponent card)
async UniTask AddCardAsync(CardComponent card, bool animate = true)
void RemoveCard(CardComponent card)
async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
CardComponent DrawFirst()
CardComponent DrawRandom()
void SortByRank(bool ascending = true)
void SortBySuit(bool ascending = true)
void Clear()
List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
// props
HandModel Model
IReadOnlyList<CardComponent> Cards
int Count
bool IsEmpty
bool IsFull
CardLayoutType LayoutType
// events
OnCardAdded<CardComponent>, OnCardRemoved<CardComponent>
OnCardClicked<CardComponent>
OnHandChanged
OnCardCountChanged<int>
```

### `PokerHandEvaluator` — `Neo.Cards.Poker` (static)
```csharp
static PokerHandResult Evaluate(IEnumerable<CardData> cards)
// needs 5–7 non-joker cards
// PokerHandResult: Combination, MainRanks, Kickers, Cards — implements IComparable<PokerHandResult>
```
`PokerCombination` enum: `RoyalFlush` → `HighCard`

```csharp
var result = PokerHandEvaluator.Evaluate(hand.Cards.Select(c => c.Data));
if (result.Combination >= PokerCombination.Flush) AwardPrize();
```

---

## GridSystem — `Neo.GridSystem`

### `FieldGenerator` (MonoBehaviour, global singleton `I`)
Generates and owns the grid. `[RequireComponent(typeof(Grid))]`

```csharp
static FieldGenerator I                              // latest initialized instance
void GenerateField(FieldGeneratorConfig config = null)
void ApplyShapeMask()
FieldCell GetCell(Vector3Int pos)
FieldCell GetCell(int x, int y)
FieldCell GetCell(Vector2Int pos)
FieldCell GetCellFromWorld(Vector3 worldPosition)
bool TryGetCellPositionFromWorld(Vector3 worldPosition, out Vector3Int position)
bool TrySnapWorldToCellCenter(Vector3 worldPosition, out Vector3 snappedWorldPosition)
IEnumerable<FieldCell> GetAllCells(bool enabledOnly = false)
IEnumerable<FieldCell> GetNeighbors(FieldCell cell, IEnumerable<Vector3Int> directions)
GridPlacementResult PlaceContentFootprint(Vector3Int origin, GridPlacementEntry[] footprint)
// 9.11.0: rule-driven layer - new GridPlacementService(field).Place(GridPlacementRequest)
//   GridPlacementRequest: Anchor, Entries, RequireEnabled/Walkable/Unoccupied,
//   CellPredicate, GridOverwritePolicy.Reject|Overwrite, Notify; .Single(pos, contentId)
bool InBounds(Vector3Int position)
// props
FieldCell[,,] Cells
FieldCell[,] Cells2D
FieldGeneratorConfig Config
Grid UnityGrid
// events
UnityEvent OnFieldGenerated
CellChangedEvent OnCellChanged
CellStateChangedEvent OnCellStateChanged
```

```csharp
FieldCell cell = FieldGenerator.I.GetCell(new Vector3Int(2, 3, 0));
cell.IsOccupied = true;
FieldGenerator.I.OnCellChanged.AddListener((c, prev) => RefreshView(c));
```

### `GridSlotAllocator` (pure C# class)
Named-slot manager on top of a `FieldGenerator`. Only `GridType.Rectangular` with `Size.z == 1`.

```csharp
GridSlotAllocator(FieldGenerator field)
GridPlacementResult Allocate(Vector3Int position, int contentId)
GridPlacementResult Allocate(int slotIndex, int contentId)
bool Release(Vector3Int position, int emptyContentId = -1, bool notify = true)
bool Release(int slotIndex, int emptyContentId = -1, bool notify = true)
void Clear(int emptyContentId = -1, bool notify = true)
bool TryGetSlotPosition(int slotIndex, out Vector3Int position)
bool TryGetSlotIndex(Vector3Int position, out int slotIndex)
bool IsAvailable(Vector3Int position)
bool IsAvailable(int slotIndex)
bool TryAllocateFirstAvailable(IEnumerable<Vector3Int>, int, out Vector3Int, out GridPlacementResult)
bool TryFindFirstAvailable(IEnumerable<Vector3Int>, out Vector3Int)
int Capacity
bool HasAvailableSlot
FieldGenerator Field
```

```csharp
var alloc = new GridSlotAllocator(FieldGenerator.I);
if (alloc.HasAvailableSlot)
    alloc.TryAllocateFirstAvailable(alloc.Field.GetAllCells().Select(c => c.Position),
        contentId: itemId, out _, out _);
```

### `GridPathfinder` (static)
BFS pathfinding across a `FieldGenerator`.

```csharp
static GridPathResult FindPath(FieldGenerator generator, GridPathRequest request)
// GridPathRequest: Start, End, IgnoreOccupied, IgnoreDisabled, IgnoreWalkability,
//   Func<FieldCell,bool> CustomPassabilityPredicate, IEnumerable<Vector3Int> Directions
// GridPathResult: List<FieldCell> Path, NoPathReason Reason, bool HasPath
```

```csharp
var result = GridPathfinder.FindPath(FieldGenerator.I, new GridPathRequest {
    Start = unitCell.Position, End = targetCell.Position,
    IgnoreOccupied = false
});
if (result.HasPath) MoveAlongPath(result.Path);
```

### `GridMergeResolver` — `Neo.GridSystem.Merge` (static)
Match-3-style merge on a `FieldGenerator`.

```csharp
static GridMergeResult Resolve(FieldGenerator generator, GridMergeRequest request)
// GridMergeResult: List<GridMergeGroupResult> Groups, List<FieldCell> ChangedCells,
//   List<Vector3Int> ChangedPositions, bool CascadeLimitReached
// GridMergeGroupResult: SeedCell, ResultCell, SourceContentId, ResultContentId,
//   List<FieldCell> Cells, List<FieldCell> ClearedCells, List<Vector3Int> Positions
```

### Sub-games — `Neo.GridSystem.*`

#### `Match3BoardService` (MonoBehaviour) — `Neo.GridSystem.Match3`
Full match-3 runtime on a `FieldGenerator`. Requires `FieldGenerator` on same object.

```csharp
void InitializeBoard()                            // fill board, optionally resolve spawn matches
bool TrySwapAndResolve(Vector3Int a, Vector3Int b) // swap + cascade; returns false if no match
List<List<FieldCell>> FindMatches()
bool TryFindValidSwap(out Vector3Int a, out Vector3Int b)
bool ShuffleIfNoMoves()                           // auto-shuffles if no swap creates a match
// events
UnityEvent OnBoardChanged, UnityEvent<int> OnMatchesResolved, UnityEvent OnBoardShuffled
event Action<Match3ResolvePhase, int> OnResolvePhase   // Swap|Clear|Collapse|Refill|Completed
```

#### `TicTacToeBoardService` (MonoBehaviour) — `Neo.GridSystem.TicTacToe`
Turn-based tic-tac-toe on a `FieldGenerator`. Tracks `CurrentPlayer` (X/O), `Winner`, `IsFinished`.

```csharp
void ResetBoard()
bool TryMakeMove(Vector2Int pos)    // or (Vector3Int pos)
bool IsBoardFull()
TicTacToeCellState CurrentPlayer, Winner
bool IsFinished
// events
UnityEvent<int> OnPlayerChanged, OnWinnerDetected
UnityEvent OnDrawDetected, OnBoardReset
```

#### `DicePieceGenerator` (pure C# class) — `Neo.GridSystem.Dice`
Generates single/pair dice pieces from a value pool, with uniform or weighted distribution.

```csharp
DicePieceGenerator(Func<int,int> range = null)         // optional RNG override
DicePiece Generate(IReadOnlyList<int> pool, bool? forcePair = null)
DicePiece GenerateWeighted(IReadOnlyList<DiceValueWeight> weights, bool? forcePair = null)
static List<int> CreateD6Pool()                         // values 1–6
static List<int> CreateSequentialPool(int min, int max) // [min..max] inclusive
static List<int> CreateDefaultPool()                    // values 1–5
// DicePiece: DicePiece.Single(value) / DicePiece.Pair(v1, v2)
// DiceValueWeight: Value, Weight
```

9.12.0: dice placement/merge logic is a plain-C# core — `Neo.GridSystem.Dice.DiceBoard` (constructible
over any generated `FieldGenerator`; C# events `BoardChanged`/`MergesResolved`) for tests/server/replay
logic. `DiceBoardService` keeps the scene API and exposes the core via its `Board` property.

---

## Merge — `Neo.Merge`

### `MergeResolver` (static class)
Low-level merge engine used by `GridMergeResolver`; operate at GridSystem level for grid use.

```csharp
// Non-generic static class; method is generic:
static MergeResult<TItem,TValue> Resolve<TItem,TValue>(MergeRequest<TItem,TValue> request)
// MergeRequest<TItem,TValue> fields: Seeds, Items, GetValue, SetValue, GetNeighbors,
//   CanUseItem, IsEmptyValue, AreValuesEqual, SelectResultItem, GetMergedValue,
//   EmptyValue, MinGroupSize (default 3), CascadeMode, Mutate, MaxCascadeIterations (default 128)
// MergeResult<TItem,TValue>: List<MergeGroupResult<TItem,TValue>> Groups,
//   List<TItem> ChangedItems, bool CascadeLimitReached
// MergeGroupResult<TItem,TValue>: Items, ClearedItems, SeedItem, ResultItem, SourceValue, ResultValue
```

Prefer `GridMergeResolver.Resolve` when operating on a `FieldGenerator`.

---

## NPC — `Neo.NPC`

### `NpcNavigation` (MonoBehaviour)
NavMeshAgent wrapper with follow, patrol, and combined modes.

```csharp
// enums
NavigationMode: FollowTarget | Patrol | Combined
RotationPolicy: Agent | ManualVelocity

// key methods
void SetFollowTarget(Transform target)
void SetCombinedTarget(Transform target)
void SetMode(NavigationMode mode)
void SetRunning(bool enable)
void SetSpeed(float speed)
bool SetDestination(Vector3 destination)
void Stop()
void Resume()
// key inspector fields
bool isActive
float walkSpeed, runSpeed, stoppingDistance
float aggroDistance, maxFollowDistance
Transform followTarget
Transform[] patrolPoints
BoxCollider patrolZone

// events
onMovementStarted, onMovementStopped
onModeChanged<NavigationMode>
onTargetChanged<Transform>
onDestinationReached<Vector3>
onPatrolStarted, onPatrolCompleted
onPatrolPointReached<int>
onStartFollowing, onStopFollowing
onSpeedChanged<float>
onPathBlocked<Vector3>
onPathUpdated<Vector3>
```

```csharp
var nav = GetComponent<NpcNavigation>();
nav.SetFollowTarget(player);
nav.SetMode(NavigationMode.FollowTarget);
nav.onDestinationReached.AddListener(pos => PlayArriveAnim());
```

### `NpcTargetFinder` (MonoBehaviour)
Auto-locates player and assigns to `NpcNavigation`.

```csharp
void FindAndSetTarget()              // searches by tag or name; calls SetFollowTarget
Transform TargetOverride {get;set}   // skip search, use this
// inspector: _targetTag = "Player", _findByTag, _findByName, _targetName, _retryInterval
```
**[no-code]** for simple player-follow: attach `NpcTargetFinder` + `NpcNavigation`, set tag to `"Player"`.

### `NpcAnimatorDriver` (MonoBehaviour)
Reads `NavMeshAgent` speed → drives Animator `Speed` / `IsMoving` params.
**[no-code]**: attach and configure param names in inspector (`speedParameter`, `isMovingParameter`, `dampTime`).

### `NpcRpgCombatBrain` (MonoBehaviour) — `Neo.NPC.Combat`
Combines `NpcNavigation`, `RpgTargetSelector`, and `RpgAttackController` into a decision loop (chase → hold → attack). Evaluates on a configurable `_decisionInterval` tick. NOTE: built on the legacy `Neo.Rpg` layer — fine for existing Rpg scenes; for new games drive `AbilityCasterBehaviour.TryCast*` from your NPC logic instead (see abilities.md).

```csharp
GameObject CurrentTarget { get; }
bool HasTarget { get; }
void AutoResolveReferences()    // fills missing refs from same GameObject
void EvaluateNow()              // force an immediate decision
GameObject AcquireTarget()      // calls RpgTargetSelector.SelectTarget()
void ClearCombatTarget()        // clears target, restores nav mode if AutoRestoreNavigationMode
bool ForceAttack()              // execute configured NpcCombatPreset attack now
// events: OnTargetAcquired<GameObject>, OnTargetLost<GameObject>,
//         OnChaseStarted, OnHoldingPosition, OnAttackTriggered,
//         OnAttackFailed<string>, OnDecisionChanged<string>
```

---

## Shop — `Neo.Shop`

### `Shop` (MonoBehaviour)
Handles buy, equip, preview, and bundle logic. Persists owned items + equipped id as JSON.

`ShopPurchaseFlow` enum: `BuyAndEquip | EquipOnly | BuyOnly | Browse`

```csharp
// typed API (preferred)
void Buy(ShopItemData item)
void BuyBundle(ShopBundleData bundle)
void Select(ShopItemData item)
void ShowPreview(ShopItemData item)
bool IsOwned(ShopItemData item)
float GetPrice(ShopItemData item)
void SetRuntimePrice(ShopItemData item, float price)
void ClearRuntimePrice(ShopItemData item)

// id-based API
void Buy(string id)
void BuyBundle(string bundleId)
void Select(string id)
bool IsOwned(string id)
float GetPrice(string id)
void SetItems(ShopItemData[] items, bool notify = false)
void SetBundles(ShopBundleData[] bundles)
List<ShopItemData> GetItemsInCategory(string category)
List<string> GetCategories(bool includeEmpty = false)

// props
string EquippedId
string PreviewIdString
ShopItemData[] ShopItemDatas
ShopBundleData[] Bundles

// events
OnSelect<int>, OnPurchased<int>, OnPurchaseFailed<int>
OnLoad
OnSelectId (UnityEvent<string>)
OnPurchasedId, OnPurchaseFailedId
OnPurchasedBundle (ShopBundleEvent)
OnShopChanged
```

```csharp
var shop = GetComponent<Shop>();
shop.OnPurchasedId.AddListener(id => UnlockSkin(id));
shop.OnPurchaseFailedId.AddListener(id => ShowNotEnoughCoins());
shop.Buy(skinData);
if (shop.IsOwned(skinData)) ApplySkin(shop.EquippedId);
```

### `ShopItem` (MonoBehaviour)
View cell for a single shop item or bundle.

```csharp
void Visual(ShopItemData data, int price)        // populate text/sprites/price
void Visual(ShopBundleData data, int price)      // bundle variant
void Select(bool active)                         // toggle selected visual state via ButtonPrice
void Clear()
string BoundItemId { get; }
ShopItemData BoundItemData { get; }
ShopBundleData BoundBundleData { get; }
Button buttonBuy                                 // public; wired by ShopListView
UnityEvent OnSelectItem, OnDeselectItem
```

### `ButtonPrice` (MonoBehaviour) — `Neo.UI`
Buy/Select/Selected/Unaffordable button that formats and displays a price. Calls `SetPrice(int)` to update text and auto-switch type (price==0 → Select state). 9.11.0: `ButtonType.Unaffordable` (+ optional `Visual.unaffordable` group, `OnUnaffordable`, `CurrentType`) — normally driven by `ShopPurchaseButtonView`.

### `ShopListView` (MonoBehaviour)
Dynamic storefront view: spawns/recycles `ShopItem` rows, filters by category/owned state, auto-wires buy buttons to `Shop`. Key methods: `Refresh()`, `SetCategory(string)`, `SetShop(Shop)`, `SetButtonAction(ShopListButtonAction)`. Events: `OnCategoryChanged`, `OnRefreshed`.

9.11.0 additions: `Shop.CanAfford(item/id)` + `Shop.ResolveCurrencyMoney(id)` (same wallet resolution as
`Buy`, incl. per-item currency override; custom wallets implement `IMoneyCanSpend`); `ShopPurchaseButtonView`
(reactive Buy/Select/Selected/Unaffordable per `ShopItem`, wallet-balance subscription, `Button.interactable`);
`ShopVariantsPanel` + `IShopVariantView`/`ShopVariantStateView` (unowned/owned/equipped variants panel over
`ShopListView` + optional `EquipmentManager`, buy-then-equip, `Unequip()`); `ShopListViewCategoryBar` adapter
for the generic `Neo.UI.CategoryBar` (selection state, marker, disabled entries, id/index events);
`ShopListView.Views` / `ShopListView.ButtonAction` getters. 9.12.0: `ShopListViewCategoryBar` auto
categories — `Build Categories From Shop` fills the bar from the catalog on enable (one entry per distinct
`ShopItemData.Category`, optional show-all entry); `BuildCategoriesFromShop()` re-runs after catalog swaps.

`ShopItemData` (ScriptableObject) public properties: `string Id`, `string nameItem`, `int price`, `bool isSinglePurchase`, `string Category`, `string description`, `Sprite sprite`, `Sprite icon`, `string CurrencyOverrideSaveKey`. Also `void AssignIdIfEmpty(string)` used internally by `Shop`.

---

## Settings — `Neo.Settings`

### `GameSettings` (static class)
Application settings with SaveProvider persistence. Requires `GameSettingsComponent` in scene.

```csharp
// read-only props
static float MouseSensitivity
static GraphicsPreset GraphicsPreset    // Minimal|Low|Medium|High|Maximum|Custom
static int QualityLevelIndex
static bool FullScreen
static FullScreenMode FullScreenModeValue
static bool ResolutionAuto
static int ResolutionIndex
static int FramerateCap
static bool VSync

// Set* mutations — each takes optional SettingsPersistMode (Immediate|Deferred|SkipUntilFlush)
static void SetMouseSensitivity(float value, SettingsPersistMode mode = Immediate)
static void SetGraphicsPreset(GraphicsPreset preset, SettingsPersistMode mode = Immediate)
static void SetQualityLevel(int index, SettingsPersistMode mode = Immediate)
static void SetFullScreen(bool fullScreen, SettingsPersistMode mode = Immediate)
static void SetFullScreenMode(FullScreenMode mode, SettingsPersistMode persist = Immediate)
static void SetResolutionAuto(bool auto, SettingsPersistMode mode = Immediate)
static void SetResolutionIndex(int index, SettingsPersistMode mode = Immediate)
static void SetFramerateCap(int targetFrameRate, SettingsPersistMode mode = Immediate)
static void SetVSync(bool enabled, SettingsPersistMode mode = Immediate)
static void ResetGroup(SettingsGroup group)  // Input|Graphics|Display|Performance

// persist helpers
static void LoadState()    // read from SaveProvider + apply to engine
static void SaveState()    // write to SaveProvider
static void FlushPendingSettingsSave()       // stop debounce coroutine, save now

// events
static event Action OnSettingsChanged        // after any Set* or LoadState
static event Action OnAfterSettingsLoaded    // once after LoadState
```

### `GameSettingsComponent` (MonoBehaviour, singleton `.I`)
Scene service that wires `GameSettings` save context. Most code only touches static `GameSettings`.

```csharp
void ReloadFromDisk()                         // calls GameSettings.LoadState()
void SaveNow()                                // calls GameSettings.SaveState()
void SetMouseSensitivityForMenu(float value)  // deferred persist
void SetMouseSensitivityForMenuImmediate(float value)
void FlushDeferredSaveCoroutine()
```

```csharp
// read setting:
float sens = GameSettings.MouseSensitivity;

// change and persist:
// (use SettingsView inspector bindings for UI sliders, or:)
GameSettings.LoadState();
GameSettings.OnSettingsChanged += RefreshGraphics;
```

### `SettingsView` (MonoBehaviour) — `Neo.Settings`
Binds Unity UI controls (Slider, Dropdown, Toggle, Button) to `GameSettings`. Assign controls in inspector; handles population of preset/resolution/framerate dropdowns, and calls the matching `GameSettings.Set*` methods on change. Key public methods: `BindAll()`, `RefreshFromSettings()`. Commit mode: `DebouncedLive` (sliders use `Deferred` persist) or `ApplyButton`.

**[no-code]**: assign UI controls to `SettingsView` in inspector — no code needed for a full settings panel.

---

## Animations — `Neo.Animations`

All three animators share the same lifecycle API. `AnimationType` enum (`Neo.Animations`): `RandomFlicker`, `Pulsing`, `SmoothTransition`, `PerlinNoise`, `SinWave`, `Exponential`, `BounceEase`, `ElasticEase`, `CustomCurve`.

### `ColorAnimator` (MonoBehaviour)
Drives a color channel (typically on a `Renderer` or `Graphic`).

```csharp
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()
Color CurrentColor
bool IsPlaying, bool IsPaused
Color StartColor {get;set}
Color EndColor {get;set}
float AnimationSpeed {get;set}
AnimationType AnimationType {get;set}
bool playOnStart
UnityEvent<Color> OnColorChanged
UnityEvent OnAnimationStarted, OnAnimationStopped, OnAnimationPaused
```

### `FloatAnimator` (MonoBehaviour)
Animates a float value; exposes `ReactivePropertyFloat` for binding.

```csharp
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()
float CurrentValue
bool IsPlaying, bool IsPaused
float MinValue {get;set}
float MaxValue {get;set}
float AnimationSpeed {get;set}
ReactivePropertyFloat Value
UnityEvent OnAnimationStarted, OnAnimationStopped, OnAnimationPaused
```

### `Vector3Animator` (MonoBehaviour)
Animates a Vector3.

```csharp
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()
Vector3 CurrentVector
bool IsPlaying, bool IsPaused
Vector3 StartVector {get;set}
Vector3 EndVector {get;set}
float AnimationSpeed {get;set}
UnityEvent<Vector3> OnVectorChanged
UnityEvent OnAnimationStarted, OnAnimationStopped
```

```csharp
// drive an alpha fade:
var fa = gameObject.AddComponent<FloatAnimator>();
fa.MinValue = 0f; fa.MaxValue = 1f;
fa.AnimationType = AnimationType.PerlinNoise;
fa.Value.AddListener(v => canvasGroup.alpha = v);
fa.Play();
```

---

## Parallax — `Neo`

### `ParallaxLayer` (MonoBehaviour)
Seamlessly tiling parallax layer driven from camera movement.

**[no-code]** for standard use: add to a layer GameObject, set `parallaxMultiplier` in inspector (0=fixed, 1=full-inverse-parallax), configure `scrollSpeed`, `tileHorizontally/Vertically`.

```csharp
void SetTargetCamera(Camera camera)     // override auto-detected main camera
Camera TargetCamera                     // read-only
```

Inspector fields: `Vector2 parallaxMultiplier`, `Vector2 scrollSpeed`, `bool tileHorizontally`,
`bool tileVertically`, `Sprite[] spriteVariants`, `bool randomiseOnInit`, `bool randomiseOnRecycle`,
`bool fitToMaxSpriteSize`, `Vector2Int paddingTiles`, `SpriteRenderer templateRenderer`

---

## Level — `Neo.Level`

### `LevelManager` (MonoBehaviour, singleton `.I`)
Tracks current/max level and map id; persists progress.

```csharp
void NextLevel()
void SetLevel(int idLevel)
void SetLastLevel()
void Restart()
void SetMapId(int id)
void SetLastMap()
void SaveLevel()            // advances stored max if currentLevel == _currentLevel
int GetLastLevelId()
int GetLastIdMap()
static int GetLoopLevel(int idLevel, int count)
// props
int CurrentLevel
int MaxLevel
int MapId
Map Map                     // plain [Serializable] class (not a ScriptableObject/MonoBehaviour)
// events
UnityEvent<int> OnChangeLevel
UnityEvent<int> OnChangeMap
UnityEvent<int> OnChangeMaxLevel
```

```csharp
LevelManager.I.OnChangeLevel.AddListener(lvl => UpdateLevelUI(lvl));
LevelManager.I.NextLevel();
```

### `LevelButton` (MonoBehaviour)
UI button representing a level on a map screen. Calls `LevelManager.SetLevel(level)` on click only when `activ == true`.

```csharp
void Click()
void SetLevelManager(LevelManager levelManager)
void SetVisual(int idVisual, int level)   // idVisual: 0=locked, 1=current, 2=unlocked
bool activ
int level
UnityEvent<int> OnChangeVisual            // fires idVisual value
UnityEvent OnDisableVisual, OnEnableVisual, OnCurrentVisual
```

### `SceneFlowController` (MonoBehaviour)
Scene loading with async/additive modes.

`SceneFlowLoadMode` enum: `Sync | Async | AsyncManual | Additive`

```csharp
void LoadScene()                    // uses component's serialized fields
void LoadScene(int buildIndex)
void LoadScene(string sceneName)
void Restart()                      // reload active scene
void Quit()
void Pause(bool active)
void ProceedScene()                 // manual-mode continuation
// props
SceneFlowLoadMode LoadMode
int SceneBuildIndex
string SceneName
// events
OnLoadStarted, OnProgress<float>, OnReadyToProceed, OnLoadCompleted
```

```csharp
var flow = GetComponent<SceneFlowController>();
flow.OnProgress.AddListener(p => loadBar.fillAmount = p);
flow.OnReadyToProceed.AddListener(() => proceedButton.SetActive(true));
flow.LoadScene("GameScene");
// later: flow.ProceedScene();  // for AsyncManual mode
```

---

## UI — `Neo` / `Neo.UI`

### `UI` (MonoBehaviour) — `Neo.UI`
Page manager. Sets itself as `UI.I` (plain static field) in `Awake`.

```csharp
static UI I                           // set on Awake; last initialized instance
void SetPage(int id)                  // deactivates all pages, activates page[id]
void SetPage()                        // uses current serialized id field
void SetOnePage(int id)               // activates only page[id] without touching others
void SetPageAnim(int id)              // animated transition via Animator, then SetPage
void SetOnePageAnim(int id)           // animated transition via Animator, then SetOnePage
void SetPageDelay(int id)             // delayed SetPage via Invoke
void SetCurrtentPage(bool active)     // toggle active page on/off
int id                                // current page index (-1 = none)
int startId                           // page activated on Awake
UnityEvent<int> OnChangePage
UnityEvent OnStartPage                // fired when id == 0
```

### `AnimationFly` (MonoBehaviour, singleton `.I`)
Fly animations: coins/XP/items flying from a source to a destination.

`AnimationFlyMotionPreset` enum: `Arc | Fountain | Magnet | FountainMagnet | Scatter`

```csharp
// typed API (preferred)
AnimationFlyResult Play(AnimationFlyRequest request)
AnimationFlyResult PlaySprite(Sprite sprite, int count, Transform start, Transform end,
    Transform parent = null, Action onReward = null)
AnimationFlyResult PlaySpriteWorldToCanvas(Sprite sprite, int count,
    Transform worldStart, RectTransform canvasEnd,
    Transform parent = null, Action onReward = null)

// type-index API (legacy, uses bonusPrefabList)
void Execute(int type, int bonusCount, Vector3 start, ...)
void PlayByType(int type, int bonusCount, Transform start, Transform end)
void PlayByTypeWorldToCanvas(int type, int bonusCount, Transform worldStart, RectTransform canvasEnd)
void PlayByTypeCanvasToCanvas(int, int, RectTransform, RectTransform)
void PlayByTypeWorldToWorld(int, int, Transform, Transform)

// utilities
static Vector3 CanvasToWorldPosition(Vector2 uiPos, Canvas canvas = null, Camera camera = null, float? worldDepth = null)
static Vector2 WorldToCanvasPosition(Vector3 worldPos, Canvas canvas = null, Camera camera = null)
```

`AnimationFlyRequest` (class):
- `int? Type` / `GameObject Prefab` / `Sprite Sprite` — source
- `int Count = 1`, `int MaxCount`
- `Transform StartTransform / EndTransform`
- `Vector3 StartPosition / EndPosition`
- `Action OnReward`, `Action OnAllArrived`
- `Action<GameObject> OnItemStarted`, `Action<GameObject> OnItemArrived`
- `AnimationFlyMotionPreset? MotionPreset`
- Static factories: `AnimationFlyRequest.FromPrefab(...)`, `AnimationFlyRequest.FromSprite(...)`

`AnimationFlyResult` (class): `int TotalCount`, `int StartedCount`, `int CompletedCount`,
`bool IsCompleted`, `IReadOnlyList<GameObject> ActiveItems`

Enums: `AnimationFlyRewardTiming` (`Manual|OnEachArrived|OnAllArrived`),
`AnimationFlyCompletionMode` (`Destroy|DisableAndPool|KeepAlive`)

```csharp
AnimationFly.I.PlaySprite(coinSprite, count: 15,
    start: pickupTransform, end: coinCounterTransform,
    onReward: () => Money.I.Add(100));

// or typed (AnimationFlyRequest is a class — set properties, don't use `with`):
var req = AnimationFlyRequest.FromSprite(coinSprite);
req.Count = 10;
req.StartTransform = pickup;
req.EndTransform = coinUI;
req.MotionPreset = AnimationFlyMotionPreset.Arc;
req.OnAllArrived = () => Money.I.Add(100);
AnimationFly.I.Play(req);
```

### `ButtonChangePage` (MonoBehaviour) — `Neo.UI`
Animated button that navigates pages via `UI.I`.

```csharp
bool Interactable {get;set}
void ExecuteClick()             // programmatic click
void SetPage(int id)
void SetPageAnim(int id)
void SetOnePage(int id)
UnityEvent OnClick
```
**[no-code]**: wire `_idPage` in inspector; calls `UI.I.SetPage` automatically on click.

### `AnchorMove` (MonoBehaviour) — `Neo.UI`
**[no-code]**: sets anchor min/max in `OnValidate`. No runtime public API. Configure `x`/`y` (0–1) in inspector.


## New in 9.7.0

### `EquipmentManager` + `EquipItemDefinition` (Neo.Shop) - dress-up / skins
```csharp
void EquipById(string itemId)       // NoCode entry: wire purchase/cell click here
void Equip(EquipItemDefinition item)
void Unequip(string categoryId)
void ToggleById(string itemId)
string GetEquippedId(string categoryId)
bool IsEquipped(string itemId)
UnityEvent<string,string> OnEquipChanged   // (categoryId, itemId)
```
One item per category, sprite applied to a `SpriteRenderer`/`Image` slot (optional SetNativeSize),
persisted via `SaveProvider` (`Equip_<category>`). Catalog = `EquipItemDefinition[]` (SO: Id, CategoryId,
Sprite; Id convention = ShopItemData id). Do NOT hand-roll wardrobes anymore.

### `SlotEconomyDefinition` (Neo.Bonus) - slot symbol economy (SO)
```csharp
int PickWeightedId()                    // per reel, weighted drop
int PickWeightedId(Func<Symbol,float> weightSelector)            // custom weights (9.11.0)
int PickWeightedId(Func<Symbol,float> selector, float roll01)    // deterministic (tests/replays)
void ApplySpecialRule(int[] lineIds)    // one special converts whole payline (ForceLineOnSpecial)
LineResult EvaluateLine(int[] lineIds)  // .Symbol .IsWin .SpecialTriggered .MoneyReward .BonusReward
```
Create -> Neoxider -> Bonus -> Slot Economy. Pairs with `SpinController`; pay result into `Money`.
Per-machine weights (9.11.0): `SpinController.Economy` + `SymbolWeightOverrides` (`SlotSymbolWeightOverrides`,
matched by symbol id, 0 disables, `NormalizeWeights()`), `SpinController.PickEconomySymbolId()`,
Inspector context menu "Normalize Weights".

### `ResourceRegen` (Neo.Bonus) - energy/lives in ONE component
Couples `CooldownReward` (AutoClaim forced on) + capped `Money` (`Amount Per Claim` per cycle) +
optional `TimeToText` countdown (0 while full). Over-cap sources call `Money.AddOverflow` themselves.

### `ShopCategorySelector` (Neo.Shop) - prev/next category pill
`Next()/Prev()/Select(id)`, drives `ShopListView.SetCategory`; tabs variant = `ShopCategoryButton`.
