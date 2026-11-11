# NeoxiderTools — game systems reference

Dense API reference for gameplay modules. Verified against source in
`Assets/Neoxider/Scripts/<Module>/`. Singletons expose `.I` (see tools.md).

> Combat (damage/abilities/buffs/projectiles) is NOT here — it moved to its own module in v10:
> see **abilities.md** (`Neo.Abilities`, supersedes `Neo.Rpg`).

---

## Bonus / Slot — `Neo.Bonus`

Every Bonus mechanic has a plain C# core (12.0.0) — use it for tests, servers, replays and custom UI, and
the components as scene wrappers:

| Core | What it owns |
|---|---|
| `CollectionSet` (`IReadOnlyCollectionSet`) | independent unlock sets: `new CollectionSet(itemCount, saveKeyPrefix)`, `TryUnlock/TryLock/IsUnlocked`, `TryClaimNext(out index, random, randomIndex)`, `PickLockedIndex`, `Save()/Load()`; events `ItemUnlocked/ItemLocked/CompletionChanged` |
| `BoxProgress` + `BoxProgressRules` | integer piggy bank: `new BoxProgress(threshold)`, `Add(long)`, `CanClaim`, `TryClaim(claimId, out BoxClaimReceipt receipt)` (a retried claim id returns the original receipt with `IsReplay`, never pays twice), `Capture()/Restore(state)`; events `ProgressChanged/Claimed` |
| `CooldownSchedule` | reward cooldown on an injected `INeoClock` (default `NeoTrustedTime`): `new CooldownSchedule(cooldownSeconds, maxClaimsPerTake, availableInitially, clock)`, `TryClaim(out grantedCount)`, `CooldownBacklogMode`, `SaveTo(key)/TryLoadFrom(key)`; static `CountClaimable(...)` is the rule `CooldownReward`/`TimeReward` share |
| `SlotSpinMath` | `ComputeSpinPrice(lineBet, activeLines, pricePerLine)`, `ComputePayout(lineMultipliers, lineBet)` — what the slot charges and pays |

### `SpinController` (MonoBehaviour)
Slot-machine orchestrator. Deducts bet, drives `Row` reels, evaluates paylines.

| Member | Notes |
|---|---|
| `void StartSpin()` | Kicks off spin coroutine |
| `bool IsStop()` | True when all reels idle (no allocation when polled) |
| `SpinResult GetLastResult(bool refreshIfIdle = true)` | Returns the nested immutable `SpinController.SpinResult` struct |
| `float ChanceWin {get;set}` | 0–1 forced-win probability |
| `int ActivePaylineCount {get;set}` | |
| `int BetSelectionIndex {get;set}` | |
| `void ForceNextOutcome(int[,] idsBottomUp)` | shape=[cols,rows], y=0=bottom |
| `bool ForceNextOutcomeOnPayline(int lineIndex, int symbolId)` | |
| `void AddBet()` / `RemoveBet()` / `SetMaxBet()` | |
| `void AddLine()` / `RemoveLine()` | |
| `int LastPayout` | |
| `IReadOnlyList<int> LastWinningPaylineIndices` | |
| `bool StartEconomySpin()` | 9.13.0: builds the whole outcome from the assigned `SlotEconomyDefinition` (weighted pick per cell + special/wild conversion per active payline), queues via `ForceNextOutcome`, spins — no hand-rolled economy chain |
| `int[,] BuildEconomyOutcomeMatrix()` | building block (+ deterministic `Func<int>`-picker overload for tests/replays/server) |
| `SlotEconomyDefinition.LineResult[] EvaluateActivePaylinesWithEconomy(bool refreshIfIdle = true)` | one `LineResult` per active payline of the settled grid |

Events: `OnStartSpin`, `OnEndSpin`, `OnEnd<bool>`, `OnWin<int>`, `OnLose`,
`OnWinLines<int[]>`, `OnChangeBet<string>`, `OnChangeMoneyWin<string>`

`SpinController.SpinResult` (nested readonly struct): `int[,] SymbolIds`, `int[] WinningLines`,
`int[,] WinningLineSymbolIds`, `int Payout`, `bool IsWin`

```csharp
SpinController spin = GetComponent<SpinController>();
spin.ChanceWin = 0.3f;
spin.OnWin.AddListener(payout => Debug.Log($"Won {payout}"));
spin.StartSpin();
SpinController.SpinResult r = spin.GetLastResult();
Debug.Log(r.IsWin);
```

### `Row` (MonoBehaviour)
Single reel column; driven by `SpinController`.

```csharp
void Spin(SpritesData allSpritesData, int[] targetVisibleIdsBottomUp)
void Stop(bool animate = true)
SlotElement GetVisibleElement(int rowFromBottom)   // allocation-free single cell
SlotElement[] GetVisibleTopDown()                  // new array (Array.Empty for an empty reel)
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
void Spin()              // respects _singleUse flag (Single Use off keeps the wheel usable)
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
void Win(int id)         // looks up wins[id] and adds it to Wallet (fallback Money.I)
int[] wins               // serialized prize amounts per sector
Money Wallet {get;set}   // optional explicit wallet
```

### `LineRoulett` (MonoBehaviour) — roulette-style line picker
```csharp
void StartRolling()
bool UseUnscaledTime {get;set}   // finish during a pause
UnityEvent<int> OnWin
```

### `Box` (MonoBehaviour, SaveableBehaviour) — collection progress box
```csharp
void AddProgress()                    // adds AddProgressAmount
void AddProgress(float amount)
void ChangeProgress(float amount)     // delta (can be negative)
void ResetProgress()                  // writes 0 and raises the progress events (NoCode reset)
void TakePrize()                      // requires CheckProgress == true
bool CheckProgress                    // progress >= _maxProgress
float progress {get;set}              // persisted
ReactivePropertyFloat Progress
float MaxProgress
float AddProgressAmount
Collection TargetCollection {get;set} // optional explicit Collection (fallback Collection.I)
UnityEvent OnTakePrize
UnityEvent OnProgressReached
UnityEvent OnProgressNotReached
UnityEvent<bool> OnChangeProgress
```
`Box` keeps float progress for existing scenes; for an integer, idempotent box use `BoxProgress` directly.

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

`Collection` (MonoBehaviour, singleton `.I`) — manages an `ItemCollectionData[]`. Key API: `ItemCollectionData GetPrize()` (picks a random locked item, unlocks + saves it, fires `OnGetItem`; returns null if all items already unlocked), `void AddItem(int id)`, `bool TryAddItem(int id)`, `void AddItem(ItemCollectionData data)`, `void RemoveItem(int id)`, `bool HasItem(int id)`, `void ClearCollection()`, `void UnlockAllItems()`, `float GetCompletionPercentage()`, `string GetCompletionCountText()`, `int ItemCount`, `int UnlockedCount`, `IReadOnlyCollectionSet Set` (read-only queries + C# events; change state through the component), `Func<int,int> RandomIndexSource` (deterministic prize picks). Events: `OnGetItem<int>`, `OnLoadItems`, `OnItemAdded<int>`, `OnItemRemoved<int>`, `OnCompletionChanged<int,int>`, `OnCompletionPercentageChanged<float>`.

---

## Cards — `Neo.Cards`

### `DeckComponent` (MonoBehaviour)
Full deck lifecycle with optional async deal animations.

```csharp
void Initialize()
void Shuffle()                       // model only; ShuffleVisualAsync also reorders the stack visuals
void Reset()
CardComponent DrawCard(bool faceUp = true)
List<CardComponent> DrawCards(int count, bool faceUp = true)
async UniTask<CardComponent> DrawCardAsync(Vector3 targetPos, bool faceUp = true, float duration = 0.3f)
void DealToHand(HandComponent hand, bool faceUp = true)
async UniTask<CardComponent> DealToHandAsync(HandComponent hand, bool faceUp, float? moveDuration = null)
void ReturnCard(CardComponent card, bool toTop = false)   // detaches it from the hand/board that holds it
// props
DeckModel Model
int RemainingCount
bool IsEmpty
CardData? TrumpCard
Suit? TrumpSuit
bool UseFixedShuffleSeed {get;set}   // + int ShuffleSeed: reproducible deals; override CreateShuffleRandom()
// events
OnInitialized, OnShuffled, OnDeckEmpty
OnCardDrawn<CardComponent>
OnCardDealt<CardComponent, HandComponent>
OnVisualStackBuilt
OnShuffleVisualStarted<ShuffleVisualType>
OnShuffleVisualCompleted
```

```csharp
DeckComponent deck = GetComponent<DeckComponent>();
deck.Initialize();
deck.Shuffle();
CardComponent card = await deck.DrawCardAsync(hand.transform.position, faceUp: true);
```

`DeckModel` (plain C#): `new DeckModel()`, `new DeckModel(int seed)`, `new DeckModel(System.Random random)`,
`System.Random ShuffleRandom {get;set}`, `Shuffle()`, `Shuffle(System.Random random)` — seeded shuffles for
replays, tests and lock-step multiplayer.

### `HandComponent` (MonoBehaviour)
Player or AI hand of cards.

```csharp
void AddCard(CardComponent card)
async UniTask AddCardAsync(CardComponent card, bool animate = true)
void RemoveCard(CardComponent card)
async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
bool Contains(CardComponent card)
CardComponent DrawFirst()
CardComponent DrawRandom()
CardComponent DrawRandom(System.Random random)
void SortByRank(bool ascending = true)                 // stable
void SortBySuit(bool ascending = true)
UniTask SortAsync(IComparer<CardData> comparer, bool animate = true)   // any comparer, custom card data
UniTask SortAsync(Comparison<CardData> comparison, bool animate = true)
void Clear()
List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
// props
HandModel Model                                         // HandModel.Sort(comparer | comparison)
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

`CardComparer` (static): `ByValueAscending`, `ByValueDescending`, `ByGroupThenValueAscending` (also for
custom card data; the legacy `ByRank*`/`BySuit*` treat custom cards as equal). `CardLayoutCalculator` has
list-filling overloads `CalculatePositions(type, count, settings, results, seed = 0)` /
`CalculateRotations(...)`; scatter and jitter are deterministic per card index.

### `PokerHandEvaluator` — `Neo.Cards.Poker` (static)
```csharp
static PokerHandResult Evaluate(IEnumerable<CardData> cards)   // 5+ non-joker cards; null throws
static int EvaluateStrength(IReadOnlyList<CardData> cards)     // comparable strength, zero allocation
static PokerCombination GetCombination(int strength)
// PokerHandResult: Combination, CombinationRanks, Kickers, BestHand — IComparable<PokerHandResult>,
//   IsStrongerThan(other), IsEqualTo(other)
```
`PokerCombination` enum: `HighCard` (0) → `RoyalFlush` (ascending, so `>=` compares strength)

```csharp
_handBuffer.Clear();
for (int i = 0; i < hand.Cards.Count; i++) _handBuffer.Add(hand.Cards[i].Data);   // List<CardData>, reused
int strength = PokerHandEvaluator.EvaluateStrength(_handBuffer);
if (PokerHandEvaluator.GetCombination(strength) >= PokerCombination.Flush) AwardPrize();
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
FieldCell GetNearestCellFromWorld(Vector3 worldPosition, bool includeDisabled = false)   // O(1) on gapless rectangular grids
bool TryGetCellPositionFromWorld(Vector3 worldPosition, out Vector3Int position)
bool TrySnapWorldToCellCenter(Vector3 worldPosition, out Vector3 snappedWorldPosition)
IEnumerable<FieldCell> GetAllCells(bool includeDisabled = true)
List<FieldCell> GetNeighbors(FieldCell cell, IEnumerable<Vector3Int> directions = null)
int CollectNeighbors(FieldCell cell, List<FieldCell> results, IEnumerable<Vector3Int> directions = null)  // allocation-free
bool CanPlaceContentFootprint(Vector3Int anchor, IEnumerable<GridPlacementEntry> entries,
    bool requireEnabled = true, bool requireWalkable = true, bool requireUnoccupied = true)
GridPlacementResult PlaceContentFootprint(Vector3Int anchor, IEnumerable<GridPlacementEntry> entries,
    bool requireEnabled = true, bool requireWalkable = true, bool requireUnoccupied = true, bool notify = true)
// 9.11.0: rule-driven layer - new GridPlacementService(field).Place(GridPlacementRequest)
//   GridPlacementRequest: Anchor, Entries, RequireEnabled/Walkable/Unoccupied,
//   CellPredicate, GridOverwritePolicy.Reject|Overwrite, Notify; .Single(pos, contentId)
void SetCell(Vector3Int pos, int type, bool isWalkable)   // + SetWalkable/SetEnabled/SetOccupied/SetContentId (notify)
bool InBounds(Vector3Int position)
// props
FieldCell[,,] Cells
FieldCell[,] Cells2D
FieldGeneratorConfig Config        // MovementPreset: FourDirections2D/EightDirections2D/DiagonalDirections2D/
                                   //   HexDirectionsX/HexDirectionsY/3D presets; UseMovementRule keeps a code-set rule
Grid UnityGrid
// events
UnityEvent OnFieldGenerated
CellChangedEvent OnCellChanged          // UnityEvent<FieldCell>
CellStateChangedEvent OnCellStateChanged // UnityEvent<FieldCell>
```

```csharp
FieldGenerator.I.OnCellStateChanged.AddListener(cell => RefreshView(cell));
FieldGenerator.I.SetOccupied(new Vector3Int(2, 3, 0), true);   // write through the generator so views are notified
```

Who notifies per cell: `FieldGenerator` setters, `TicTacToeBoardService` (moves and resets, after the round
state is committed), `SlidingMergeBoardService`/`DiceBoard`/`GridSlotAllocator` (after the whole board is
written). `Match3BoardService` publishes only board-level events (`OnBoardChanged`, `OnResolvePhase`).

`GridShapeMask` (ScriptableObject): `GridShapeMask.Create(GridShapeMaskMode mode, IEnumerable<Vector3Int>
cells)` for code-built shapes, `SetCells(...)`, `Contains(pos)`, `Mode {get;set}` — editing an asset changes
it for every board that uses it.

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
GridSlotAllocator alloc = new GridSlotAllocator(FieldGenerator.I);
GridPlacementResult placed = alloc.Allocate(slotIndex: 0, contentId: itemId);
if (!placed.Placed) Debug.Log(placed.FailureReason);
```

### `GridPathfinder` (static)
BFS pathfinding across a `FieldGenerator` over reusable buffers (unit cost per step).

```csharp
static GridPathResult FindPath(FieldGenerator generator, GridPathRequest request)
static bool TryFindPath(FieldGenerator generator, GridPathRequest request, List<FieldCell> path,
    out NoPathReason reason)                                   // allocation-free with a reused list
static int CollectReachableCells(FieldGenerator generator, GridPathRequest request, int maxSteps,
    List<FieldCell> results, List<int> stepCounts = null)      // movement ranges, flood fill
// GridPathRequest: Start, End, IgnoreOccupied, IgnoreDisabled, IgnoreWalkability,
//   Func<FieldCell,bool> CustomPassabilityPredicate (evaluated at most once per cell), IEnumerable<Vector3Int> Directions
// GridPathResult: List<FieldCell> Path, NoPathReason Reason, bool HasPath
```

```csharp
GridPathRequest request = new GridPathRequest { Start = unitCell.Position, End = targetCell.Position };
if (GridPathfinder.TryFindPath(FieldGenerator.I, request, _path, out NoPathReason reason)) MoveAlongPath(_path);
GridPathfinder.CollectReachableCells(FieldGenerator.I, request, maxSteps: 3, _reachable);   // highlight move range
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

### `FieldObjectSpawner` (MonoBehaviour)
Spawns prefabs into cells: `SpawnAt(cellPos, prefabIndex, occupiesSpace, ...)`, `GetObjectsInCell(pos)`,
`bool Unregister(GameObject)` (hand an object to your pool without destroying it), `int PruneDestroyedObjects()`
(frees cells of objects destroyed elsewhere).

### Sub-games — `Neo.GridSystem.*`

#### `Match3BoardService` (MonoBehaviour) — `Neo.GridSystem.Match3`
Full match-3 runtime on a `FieldGenerator`. Requires `FieldGenerator` on same object.

```csharp
void InitializeBoard()                            // fill board, optionally resolve spawn matches
bool TrySwapAndResolve(Vector3Int a, Vector3Int b) // (+ Vector2Int overload) swap + cascade; false if no match
List<List<FieldCell>> FindMatches()
bool TryFindValidSwap(out Vector3Int a, out Vector3Int b)   // one board copy per call, allocation-free when warm
bool ShuffleIfNoMoves()                           // auto-shuffles if no swap creates a match
// events
UnityEvent OnBoardChanged, UnityEvent<int> OnMatchesResolved, UnityEvent OnBoardShuffled
event Action<Match3ResolvePhase, int> OnResolvePhase   // Swap|Clear|Collapse|Refill|Completed
```

#### `TicTacToeBoardService` (MonoBehaviour) — `Neo.GridSystem.TicTacToe`
Turn-based tic-tac-toe / Gomoku on a `FieldGenerator`. Tracks `CurrentPlayer` (X/O), `Winner`, `IsFinished`.

```csharp
void ResetBoard()
bool TryMakeMove(Vector2Int pos)    // or (Vector3Int pos); raises FieldGenerator.OnCellStateChanged
bool IsBoardFull()
TicTacToeCellState CurrentPlayer, Winner
bool IsFinished
int WinLineLength {get;set}         // 0 = automatic (3, or the smallest dimension when all exceed 3); 5 = Gomoku
IReadOnlyList<Vector3Int> WinningLine   // cells to highlight
// events
UnityEvent<int> OnPlayerChanged, OnWinnerDetected
UnityEvent OnDrawDetected, OnBoardReset
```
Static `TicTacToeWinChecker.TryGetWinningLine(field, line, out winner, winLineLength, winDirections)` and
`GetWinner(field, winLineLength, winDirections)` work on any generator.

#### `SlidingMergeBoardService` (MonoBehaviour) — `Neo.GridSystem.SlidingMerge`
2048-like sliding/merge loop: `SlidingMergeResult Slide(SlidingMergeDirection)`, `CanSlide(dir)`,
`HasAnyMove()`; `OnNoMovesLeft`, `OnContentSpawned<Vector3Int>`, C# `Slid` / `ContentSpawned`;
`DefaultSpawnContentId`, `SpawnAfterSuccessfulSlide`, `RandomIndexProvider` (seeded spawns), custom
`CanMergeRule` / `MergeRule`. Steps report the consumed tile (`SlidingMergeStep.MergedFrom`/`MergedValue`);
listeners are notified after the whole board is written. Static core: `SlidingMergeResolver.Slide`,
`HasAnyMove`, `TrySpawnRandomContent(field, contentId, out cell, emptyContentId, requireWalkable, randomIndex)`.

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
logic. `DiceBoardService` keeps the scene API and exposes the core via its `Board` property. Both have
`CanPlaceAnywhere(piece, allowRotations = true)` (game over check) and
`TryFindPlacement(piece, allowRotations, out anchor, out clockwiseTurns)` (hints).

---

## Merge — `Neo.Merge`

### `MergeResolver` (static class)
Low-level merge engine used by `GridMergeResolver`; operate at GridSystem level for grid use. Reuses its
search collections per resolve (whole-board scans do not allocate per seed).

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
// nested enums
NpcNavigation.NavigationMode: FollowTarget | Patrol | Combined
NpcNavigation.RotationPolicy: Agent | ManualVelocity

// key methods
void SetFollowTarget(Transform target)
void SetCombinedTarget(Transform target)
void SetMode(NpcNavigation.NavigationMode mode)
void SetRunning(bool enable)
void SetSpeed(float speed)
bool SetDestination(Vector3 destination)
bool RefreshPath()                  // fresh follow path without raising onTargetChanged
void Stop()
void Resume()
// Stop/Resume are also Play Mode-only Inspector buttons
bool IsActive {get;set}, bool IsRunning, bool AutoUpdatePath {get;set}
// key inspector fields
float walkSpeed, runSpeed, stoppingDistance
float aggroDistance, maxFollowDistance
Transform followTarget
Transform[] patrolPoints
BoxCollider patrolZone
Stagger Path Updates (default on: NPCs spawned together re-path at different moments)

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
NpcNavigation nav = GetComponent<NpcNavigation>();
nav.SetFollowTarget(player);
nav.SetMode(NpcNavigation.NavigationMode.FollowTarget);
nav.onDestinationReached.AddListener(pos => PlayArriveAnim());
```
Combined mode retries an unreachable chase after **Path Update Interval**, not every frame.

### `NpcTargetFinder` (MonoBehaviour)
Auto-locates player and assigns to `NpcNavigation`.

```csharp
void FindAndSetTarget()              // searches by tag or name; calls SetFollowTarget
Transform TargetOverride {get;set}   // skip search, use this
// inspector: _targetTag = "Player", _findByTag, _findByName, _targetName, _retryInterval
```
**[no-code]** for simple player-follow: attach `NpcTargetFinder` + `NpcNavigation`, set tag to `"Player"`.

### `NpcAnimatorDriver` (MonoBehaviour)
Reads `NavMeshAgent` speed → drives Animator `Speed` / `IsMoving` params (validated once per controller;
missing parameters are skipped, see `NpcAnimationCore.HasSpeedParameter`/`HasIsMovingParameter`).
**[no-code]**: attach and configure param names in inspector (`speedParameter`, `isMovingParameter`, `dampTime`).

### `NpcRpgCombatBrain` (MonoBehaviour) — `Neo.NPC.Combat`
Combines `NpcNavigation`, `RpgTargetSelector`, and `RpgAttackController` into a decision loop (chase → hold → attack). Evaluates on a configurable `_decisionInterval` tick. NOTE: built on the legacy `Neo.Rpg` layer — fine for existing Rpg scenes; for new games drive `AbilityCasterBehaviour.TryCast*` from your NPC logic instead (see abilities.md).

```csharp
GameObject CurrentTarget { get; }
bool HasTarget { get; }
bool IsActive { get; set; }
NpcCombatPreset Preset { get; set; }
NpcCombatDecisionCore.Decision? CurrentDecision { get; }
void AutoResolveReferences()    // fills missing refs from same GameObject
void EvaluateNow()              // force an immediate decision
GameObject AcquireTarget()      // calls RpgTargetSelector.SelectTarget()
void ClearCombatTarget()        // clears target, restores nav mode if AutoRestoreNavigationMode
bool ForceAttack()              // execute configured NpcCombatPreset attack now
// events (public accessors): OnTargetAcquired<GameObject>, OnTargetLost<GameObject>,
//         OnChaseStarted (once per chase), OnHoldingPosition, OnAttackTriggered,
//         OnAttackFailed<string>, OnDecisionChanged<string>
```

---

## Shop — `Neo.Shop`

### `Shop` (MonoBehaviour)
Handles buy, equip, preview, and bundle logic. Persists owned items + equipped id as JSON.

`ShopPurchaseFlow` enum: `BuyAndEquip | EquipOnly | BuyOnly | Browse`

```csharp
// typed API (preferred) — the outcome without UnityEvents
ShopPurchaseStatus TryBuy(ShopItemData item)        // Purchased, AlreadyOwned, Selected, InsufficientFunds,
ShopPurchaseStatus TryBuy(string id)                //   CommitFailed, PendingServerAuthority, Unavailable, Busy
ShopPurchaseStatus TryBuyBundle(ShopBundleData bundle)
ShopPurchaseStatus TryBuyBundle(string bundleId)
void Buy(ShopItemData item)                         // same, for UnityEvents
void BuyBundle(ShopBundleData bundle)
void Select(ShopItemData item)
void ShowPreview(ShopItemData item)
bool IsOwned(ShopItemData item)
bool CanAfford(ShopItemData item)
float GetPrice(ShopItemData item)
void SetRuntimePrice(ShopItemData item, float price)
void ClearRuntimePrice(ShopItemData item)

// id-based API
void Buy(string id)
void BuyBundle(string bundleId)
void Select(string id)
bool IsOwned(string id)
float GetPrice(string id)
void SetItems(ShopItemData[] items, bool resetPreviewToFirst = true)   // spawns missing rows when Auto Spawn is on
void SetBundles(ShopBundleData[] bundles)
IReadOnlyList<ShopItemData> GetItemsInCategory(string category)
int GetItemsInCategory(string category, List<ShopItemData> results)   // non-allocating
IReadOnlyList<string> GetCategories(bool includeEmpty = false)

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
Shop shop = GetComponent<Shop>();
ShopPurchaseStatus status = shop.TryBuy(skinData);
if (status == ShopPurchaseStatus.InsufficientFunds) ShowNotEnoughCoins();
else if (shop.IsOwned(skinData)) ApplySkin(shop.EquippedId);
```
`PendingServerAuthority` is a local no-op: the Shop never completes it later.

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
Dynamic storefront view: spawns/recycles `ShopItem` rows, filters by category/owned state, auto-wires buy buttons to `Shop`. Key methods: `Refresh()`, `SetCategory(string)`, `SetShop(Shop)`, `SetButtonAction(ShopListButtonAction)`. Events: `OnCategoryChanged`, `OnRefreshed` (once per purchase or bundle).

`TextMoney` binds when a wallet registers (no per-frame scene scan while none exists); `BoundMoney` is the
wallet it shows.

For optional scene wiring, use `ShopResolver.Resolve<T>(context)` / `TryResolve<T>(...)`: it applies the
module's shared parent-first, scene-fallback lookup policy. Prefer explicit serialized references for
stable relationships.

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
void SetMouseSensitivityForMenu(float value)  // deferred persist (a slider drag = one write)
void SetMouseSensitivityForMenuImmediate(float value)
void FlushDeferredSaveCoroutine()
bool HasPendingDeferredSave                   // a Deferred change is still unwritten
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

All animators share the same lifecycle API. `Play`, `Stop`, `Pause`, and `Resume` are also Play Mode-only
Inspector buttons for runtime preview. `AnimationType` enum (`Neo.Animations`): `RandomFlicker`, `Pulsing`,
`SmoothTransition`, `PerlinNoise`, `SinWave`, `Exponential`, `BounceEase`, `ElasticEase`, `CustomCurve`.

Manual clock (12.0.0): `FloatAnimator`, `ColorAnimator`, `Vector3Animator`, `TransformAnimator` (and
`Neo.Tools.LightAnimator`) have `bool ManualUpdate {get;set}` + `void Tick(float deltaTime)` (a no-op while
stopped, paused or disabled) for a shared clock or deterministic tests. The value animators also expose
`float AnimationTime`, `bool RandomizeStartTime {get;set}` (default on) and `bool UseUnscaledTime {get;set}`.
`AnimationUtils.ApplyToRenderer(renderer, MaterialPropertyBlock block, intensity, originalEmission, changeColor,
targetColor, colorBlend)` animates emission through a property block — no `renderer.material` copies.

### `ColorAnimator` (MonoBehaviour)
Drives a color channel (typically on a `Renderer` or `Graphic`).

```csharp
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()  void Tick(float dt)
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
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()  void Tick(float dt)
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
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()  void Tick(float dt)
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
FloatAnimator fa = gameObject.AddComponent<FloatAnimator>();
fa.MinValue = 0f; fa.MaxValue = 1f;
fa.AnimationType = AnimationType.PerlinNoise;
fa.Value.AddListener(v => canvasGroup.alpha = v);
fa.Play();
```

### `TransformAnimator` (MonoBehaviour)
Universal transform animator: constant rotation, curve-eased float (bob) along any direction,
scale pulse, continuous Perlin shake and one-shot impulse shake — combinable channels on a shared clock.

```csharp
void Play()  void Stop()  void Pause()  void Resume()  void ResetTime()  void RandomizeTime()  void Tick(float dt)
void Shake(float strength)          // one-shot impulse; strength < 0 clamps to 0 (null-safe)
void Shake()                        // full-strength impulse ([Button], Play Mode only)
void CaptureBase()                  // re-capture the current local pose as the animation base
void ApplyCurrentState()            // evaluate + apply to the target immediately
bool IsPlaying, bool IsPaused
float Time
Transform Target                    // null-safe: falls back to this transform when unset
bool PlayOnEnable, bool RandomizeStartTime, bool UseUnscaledTime, bool ManualUpdate
TransformAnimationSettings Settings // per-channel config; every eased channel takes an AnimationCurve
UnityEvent OnAnimationStarted, OnAnimationStopped, OnAnimationPaused
```

Pool-safe play-on-enable: `OnEnable` captures the base pose and randomizes the start time/seed, and
`OnDisable` stops and restores the base pose, so a recycled pooled instance never keeps a mid-shake
pose. Scale pulse is symmetric around the base scale. All math lives in the pure, scene-free
`TransformAnimationEvaluator.Evaluate(settings, basePosition, baseEulerAngles, baseScale, time,
randomSeed, impulseTime = -1f, impulseStrength = 1f)` — no MonoBehaviour dependency.

---

## Parallax — `Neo.Parallax`

### `ParallaxLayer` (MonoBehaviour)
Seamlessly tiling parallax layer driven from camera movement. A resting camera costs no transform writes;
warm frames allocate nothing.

**[no-code]** for standard use: add to a layer GameObject, set `parallaxMultiplier` in inspector (0=fixed, 1=full-inverse-parallax), configure `scrollSpeed`, `tileHorizontally/Vertically`.

```csharp
void SetTargetCamera(Camera camera)     // override auto-detected main camera (also after the camera is destroyed)
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
Map Map                     // plain [Serializable] class; Map.IsComplete (GetCopmplete() is obsolete)
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
SceneFlowController flow = GetComponent<SceneFlowController>();
flow.OnProgress.AddListener(p => loadBar.fillAmount = p);
flow.OnReadyToProceed.AddListener(() => proceedButton.SetActive(true));
flow.LoadScene("GameScene");
// later: flow.ProceedScene();  // for AsyncManual mode
```

---

## UI — `Neo.UI`

### `UI` (MonoBehaviour) — `Neo.UI`
Page manager. Sets itself as `UI.I` (plain static field) in `Awake`.

```csharp
static UI I                           // set on Awake; last initialized instance
void SetPage(int id)                  // deactivates all pages, activates page[id]
void SetPage()                        // uses current serialized id field
void SetOnePage(int id)               // activates only page[id] without touching others
void SetPageAnim(int id)              // animated transition via Animator, then SetPage (scaled time)
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
AnimationFlyResult Play(AnimationFlyRequest request)   // works on a copy of the request
AnimationFlyResult PlaySprite(Sprite sprite, int count, Transform start, Transform end,
    Transform parent = null, Action onReward = null)
AnimationFlyResult PlaySpriteWorldToCanvas(Sprite sprite, int count,
    Transform worldStart, RectTransform canvasEnd,
    Transform parent = null, Action onReward = null)
int OwnedVisualCount                                   // visuals the service currently owns (pool + flights)

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

`AnimationFly.AnimationFlyRequest` (nested sealed class):
- `int? Type` / `GameObject Prefab` / `Sprite Sprite` — source
- `int Count = 1`, `int MaxCount`
- `Transform StartTransform / EndTransform`
- `Vector3 StartPosition / EndPosition`
- `Action OnReward`, `Action OnAllArrived`
- `Action<GameObject> OnItemStarted`, `Action<GameObject> OnItemArrived`
- `AnimationFlyMotionPreset? MotionPreset`
- Static factories: `FromPrefab(prefab, count = 1, start = null, end = null)`, `FromSprite(sprite, count = 1, start = null, end = null)`; `Clone()` for templates

`Play` copies the request on entry: set every field (callbacks included) **before** `Play`; later edits do not
affect running flights. A throwing callback is logged, and the visual is still released and the result still
completes.

`AnimationFlyResult` (class): `int TotalCount`, `int StartedCount`, `int CompletedCount`,
`bool IsCompleted`, `IReadOnlyList<GameObject> ActiveItems`

Enums: `AnimationFlyRewardTiming` (`Manual|OnEachArrived|OnAllArrived`),
`AnimationFlyCompletionMode` (`Destroy|DisableAndPool|KeepAlive`)

```csharp
AnimationFly.I.PlaySprite(coinSprite, count: 15,
    start: pickupTransform, end: coinCounterTransform,
    onReward: () => Money.I.Add(100));

// or typed (a class — set fields, don't use `with`):
AnimationFly.AnimationFlyRequest req = AnimationFly.AnimationFlyRequest.FromSprite(coinSprite, 10, pickup, coinUI);
req.MotionPreset = AnimationFlyMotionPreset.Arc;
req.OnAllArrived = () => Money.I.Add(100);
AnimationFly.I.Play(req);
```

### Small UI components — `Neo.UI`
- **`CategoryBar`** — `Select/Next/Prev` (calls before `Start` become the initial selection), C# `event
  Action<int, string> SelectionChanged` (raised before the UnityEvents), `IsInitialized`; `SetEntryDisabled(i, b)`
  plus the one-argument UnityEvent targets `DisableEntry(i)` / `EnableEntry(i)`.
- **`VariantView`** — `SetCurrentState/NextState/PreviousState`, `CurrentStateIndex` (`currentStateIndex` is
  obsolete), C# `event Action<int> StateChanged` + `OnStateChanged` UnityEvent, raised once per real change;
  `Awake` applies the saved state, so spawned views show it at once.
- **`ButtonScale`**, **`ButtonShake`**, **`FakeLoad`** — `UseUnscaledTime` (default **on**: press feedback and
  fake loaders keep working at `Time.timeScale` 0); `ButtonShake.IsShaking`; a repeated `FakeLoad.Load()`
  restarts the load; a one-shot revisit completes on the next frame, a deactivated load resumes on activation,
  `FakeLoad.IsLoading`; `ButtonScale` releases to the scale the button had when the press started.

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
**[no-code]**: sets anchor min/max in `OnValidate`; at runtime `Apply()`, `SetX(float)`, `SetY(float)`,
`SetAnchor(Vector2)` (clamped 0–1) re-anchor it from code or a UnityEvent. Configure `x`/`y` (0–1) in inspector.


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
