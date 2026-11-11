using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.GridSystem.Match3
{
    /// <summary>
    ///     Runtime board service for Match3: initialization, swap validation, match resolve and refill.
    /// </summary>
    [NeoDoc("GridSystem/Match3/Match3BoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/Match3/Match3BoardService")]
    [AddComponentMenu("Neoxider/GridSystem/Match3/Match3BoardService")]
    public class Match3BoardService : MonoBehaviour
    {
        /// <summary>
        ///     Stage reported by <see cref="OnResolvePhase" /> during a delayed resolve.
        /// </summary>
        public enum Match3ResolvePhase
        {
            /// <summary>The two tiles were swapped.</summary>
            Swap,

            /// <summary>Matched tiles are about to be cleared.</summary>
            Clear,

            /// <summary>Remaining tiles are about to fall into the gaps.</summary>
            Collapse,

            /// <summary>Empty cells are about to receive new tiles.</summary>
            Refill,

            /// <summary>The cascade finished; the argument is the total number of cleared tiles.</summary>
            Completed
        }

        [SerializeField] private int _minMatchLength = 3;
        [SerializeField] private bool _autoGenerateOnStart = true;
        [SerializeField] private bool _avoidStartMatches = true;
        [SerializeField] private bool _resolveSpawnMatchesWhenAllowed = true;
        [SerializeField] private bool _useResolveDelay;
        [SerializeField] private float _resolveDelaySeconds = 0.08f;
        [SerializeField] private int _maxCascadeIterations = 32;

        [SerializeField] private List<Match3TileState> _availableTiles = new()
        {
            Match3TileState.Red,
            Match3TileState.Green,
            Match3TileState.Blue,
            Match3TileState.Yellow,
            Match3TileState.Purple
        };

        /// <summary>
        ///     Invoked after board content changes.
        /// </summary>
        public UnityEvent OnBoardChanged = new();

        /// <summary>
        ///     Invoked after resolve step; argument is number of removed tiles.
        /// </summary>
        public UnityEvent<int> OnMatchesResolved = new();

        /// <summary>
        ///     Invoked when board was shuffled due to no available moves.
        /// </summary>
        public UnityEvent OnBoardShuffled = new();

        private FieldGenerator _generator;
        private Coroutine _resolveRoutine;

        // WHY: the busy state is a flag plus a token instead of the coroutine handle. The routine's first step runs
        // inside StartCoroutine, before any handle exists, and its completion events must already see the service
        // idle. A cancel bumps the token, so a routine that cannot be stopped by handle exits at its next check.
        private bool _resolving;
        private int _resolveToken;
        private WaitForSeconds _resolveWait;
        private float _resolveWaitSeconds = float.NaN;

        // WHY: swap validation needs a board copy and a neighbor list. They are rented for one synchronous call and
        // handed back, so repeated hint and shuffle searches reuse them instead of creating new ones, while a nested
        // call (none exists today) would simply get fresh buffers. Every rent refills the copy from the live board;
        // no board content is cached between calls.
        private BoardSnapshot _spareSnapshot;
        private List<FieldCell> _spareNeighbors;

        // WHY: resolve and shuffle buffers follow the same rent/return rule, so a warm cascade does not allocate
        // and a re-entrant call from an event or random-provider callback gets its own buffers.
        private ResolveScratch _spareScratch;
        private readonly List<FieldCell> _collapseSegment = new();
        private readonly List<int> _collapseValues = new();

        /// <summary>
        ///     Optional random index source for new tiles (initial fill and refill) and the no-moves shuffle. It
        ///     receives the exclusive upper bound and returns an index; the result is clamped into range. Uses
        ///     <see cref="UnityEngine.Random" /> when null; inject it for deterministic tests, replays or a seeded
        ///     game-wide random source.
        /// </summary>
        public Func<int, int> RandomIndexProvider { get; set; }

        /// <summary>
        ///     True while a delayed resolve (Use Resolve Delay) is running; swaps and manual resolves are rejected
        ///     until it completes.
        /// </summary>
        /// <remarks>
        ///     It is already true inside the first phase callbacks (<see cref="Match3ResolvePhase.Swap" /> or the first
        ///     <see cref="Match3ResolvePhase.Clear" /> and their <see cref="OnBoardChanged" />) and already false inside
        ///     the completion callbacks (<see cref="Match3ResolvePhase.Completed" />, <see cref="OnMatchesResolved" />
        ///     and the final <see cref="OnBoardChanged" />), so a listener can chain the next move from them.
        /// </remarks>
        public bool IsResolving => _resolving;

        private void Awake()
        {
            _generator = GetComponent<FieldGenerator>();
        }

        private FieldGenerator Generator
        {
            get
            {
                if (_generator == null)
                {
                    _generator = GetComponent<FieldGenerator>();
                }

                return _generator;
            }
        }

        private void Start()
        {
            if (_autoGenerateOnStart)
            {
                InitializeBoard();
            }
        }

        private void OnDisable()
        {
            // WHY: Unity kills the resolve coroutine on disable, so the tail that clears the busy state never
            // runs; a stale busy state would block every TrySwapAndResolve until a full InitializeBoard.
            CancelResolve();
        }

        /// <summary>
        ///     Raised at each stage of a delayed resolve with the number of tiles cleared so far.
        /// </summary>
        public event Action<Match3ResolvePhase, int> OnResolvePhase;

        /// <summary>
        ///     Inspector helper for <see cref="InitializeBoard" />.
        /// </summary>
        [Button("Initialize Board", PlayModeOnly = true)]
        public void InitializeBoardButton()
        {
            InitializeBoard();
        }

        /// <summary>
        ///     Resolves matches already on the board (for example after external edits); publishes only
        ///     <see cref="OnBoardChanged" /> when there are none. Ignored while <see cref="IsResolving" />.
        /// </summary>
        [Button("Resolve Current Matches", PlayModeOnly = true)]
        public void ResolveCurrentMatchesButton()
        {
            if (_resolving)
            {
                return;
            }

            if (!HasCurrentMatch())
            {
                OnBoardChanged.Invoke();
                return;
            }

            if (UseDelayedResolve)
            {
                StartResolveRoutine(null, null);
                return;
            }

            int resolved = ResolveBoardImmediate();
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
        }

        /// <summary>
        ///     Inspector helper for <see cref="ShuffleIfNoMoves" /> that also publishes <see cref="OnBoardChanged" />.
        /// </summary>
        [Button("Shuffle If No Moves", PlayModeOnly = true)]
        public void ShuffleIfNoMovesButton()
        {
            ShuffleIfNoMoves();
            OnBoardChanged.Invoke();
        }

        /// <summary>
        ///     Fills all enabled cells with random tiles from configured set.
        /// </summary>
        public void InitializeBoard()
        {
            if (Generator == null)
            {
                return;
            }

            FillBoardInitial();
            CancelResolve();

            if (!_avoidStartMatches && _resolveSpawnMatchesWhenAllowed && HasCurrentMatch())
            {
                if (UseDelayedResolve)
                {
                    OnBoardChanged.Invoke();
                    StartResolveRoutine(null, null);
                    return;
                }

                int resolved = ResolveBoardImmediate();
                if (resolved > 0)
                {
                    OnMatchesResolved.Invoke(resolved);
                }
            }

            ShuffleIfNoMoves();
            OnBoardChanged.Invoke();
        }

        /// <summary>
        ///     Tries to swap two adjacent cells using 2D coordinates (z=0) and resolves resulting cascades.
        /// </summary>
        public bool TrySwapAndResolve(Vector2Int a, Vector2Int b)
        {
            return TrySwapAndResolve(new Vector3Int(a.x, a.y, 0), new Vector3Int(b.x, b.y, 0));
        }

        /// <summary>
        ///     Tries to swap two adjacent cells and resolves all resulting cascades.
        /// </summary>
        /// <param name="a">First cell position.</param>
        /// <param name="b">Second cell position.</param>
        /// <returns>
        ///     True if swap produced at least one valid match and was resolved;
        ///     false when swap is invalid or reverted.
        /// </returns>
        public bool TrySwapAndResolve(Vector3Int a, Vector3Int b)
        {
            if (_resolving)
            {
                return false;
            }

            FieldGenerator generator = Generator;
            if (generator == null)
            {
                return false;
            }

            if (!AreAdjacent(a, b))
            {
                return false;
            }

            FieldCell cellA = generator.GetCell(a);
            FieldCell cellB = generator.GetCell(b);
            if (!CanUseCell(cellA) || !CanUseCell(cellB))
            {
                return false;
            }

            if (!WouldSwapCreateMatch(a, b))
            {
                OnBoardChanged.Invoke();
                return false;
            }

            if (UseDelayedResolve)
            {
                StartResolveRoutine(cellA, cellB);
                return true;
            }

            SwapContent(cellA, cellB);
            int resolved = ResolveBoardImmediate();
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
            return true;
        }

        /// <summary>
        ///     Finds the first adjacent swap that creates a match without mutating the board.
        /// </summary>
        /// <remarks>
        ///     Cells are visited in backing-array x, then y, then z order and neighbors in movement-rule order; the first
        ///     usable pair whose swap completes a run is returned. One board copy is taken per call, so the cost is
        ///     O(board + probes); the copy and the neighbor list are reused by later calls while the board size is
        ///     unchanged.
        /// </remarks>
        public bool TryFindValidSwap(out Vector3Int a, out Vector3Int b)
        {
            a = default;
            b = default;

            FieldGenerator generator = Generator;
            FieldCell[,,] cells = generator != null ? generator.Cells : null;
            if (cells == null)
            {
                return false;
            }

            BoardSnapshot snapshot = null;
            List<FieldCell> neighbors = RentNeighbors();
            try
            {
                // WHY: walks the array exactly like FieldGenerator.GetAllCells(false) (null and disabled cells are
                // rejected by CanUseCell) without creating its iterator.
                int sizeX = cells.GetLength(0);
                int sizeY = cells.GetLength(1);
                int sizeZ = cells.GetLength(2);
                for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    FieldCell cell = cells[x, y, z];
                    if (!CanUseCell(cell))
                    {
                        continue;
                    }

                    generator.CollectNeighbors(cell, neighbors);
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        FieldCell neighbor = neighbors[i];
                        if (!CanUseCell(neighbor) || neighbor == cell)
                        {
                            continue;
                        }

                        snapshot ??= RentSnapshot();
                        if (WouldSwapCreateMatch(snapshot, cell.Position, neighbor.Position))
                        {
                            a = cell.Position;
                            b = neighbor.Position;
                            return true;
                        }
                    }
                }
            }
            finally
            {
                ReturnNeighbors(neighbors);
                ReturnSnapshot(snapshot);
            }

            return false;
        }

        /// <summary>
        ///     Finds all current match groups on the board.
        /// </summary>
        /// <returns>List of match groups.</returns>
        public List<List<FieldCell>> FindMatches()
        {
            return Match3MatchFinder.FindMatches(Generator, _minMatchLength);
        }

        private bool HasCurrentMatch()
        {
            return Match3MatchFinder.HasMatch(Generator, _minMatchLength);
        }

        private int CollectCurrentMatches(HashSet<FieldCell> matched)
        {
            return Match3MatchFinder.CollectMatchedCells(Generator, _minMatchLength, matched);
        }

        private static int ClearCells(HashSet<FieldCell> matched)
        {
            foreach (FieldCell cell in matched)
            {
                cell.ContentId = 0;
            }

            return matched.Count;
        }

        private void CollapseColumns()
        {
            // WHY: collapse runs no callbacks (plain ContentId writes), so dedicated buffers cannot be re-entered.
            List<FieldCell> segment = _collapseSegment;
            List<int> values = _collapseValues;
            Vector3Int size = _generator.GeneratedSize;
            for (int z = 0; z < size.z; z++)
            for (int x = 0; x < size.x; x++)
            {
                segment.Clear();
                for (int y = 0; y < size.y; y++)
                {
                    FieldCell cell = _generator.GetCell(new Vector3Int(x, y, z));
                    if (!CanUseCell(cell))
                    {
                        CollapseSegment(segment, values);
                        segment.Clear();
                        continue;
                    }

                    segment.Add(cell);
                }

                CollapseSegment(segment, values);
            }

            segment.Clear();
        }

        private static void CollapseSegment(List<FieldCell> segment, List<int> values)
        {
            if (segment.Count == 0)
            {
                return;
            }

            values.Clear();
            for (int i = 0; i < segment.Count; i++)
            {
                int contentId = segment[i].ContentId;
                if (contentId > 0)
                {
                    values.Add(contentId);
                }
            }

            for (int i = 0; i < segment.Count; i++)
            {
                segment[i].ContentId = i < values.Count ? values[i] : 0;
            }
        }

        private void FillEmptyCells()
        {
            FieldCell[,,] cells = _generator.Cells;
            if (cells == null)
            {
                return;
            }

            // WHY: same x, then y, then z order as FieldGenerator.GetAllCells, so a seeded random source yields the
            // same refill as before, without creating that iterator.
            int sizeX = cells.GetLength(0);
            int sizeY = cells.GetLength(1);
            int sizeZ = cells.GetLength(2);
            for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
            for (int z = 0; z < sizeZ; z++)
            {
                FieldCell cell = cells[x, y, z];
                if (CanUseCell(cell) && cell.ContentId <= 0)
                {
                    cell.ContentId = GetRandomTileId();
                }
            }
        }

        private bool CanUseCell(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && !cell.IsOccupied && cell.IsWalkable;
        }

        private bool AreAdjacent(Vector3Int a, Vector3Int b)
        {
            FieldGenerator generator = Generator;
            if (generator == null)
            {
                return false;
            }

            FieldCell cellA = generator.GetCell(a);
            if (cellA == null)
            {
                return false;
            }

            FieldCell cellB = generator.GetCell(b);
            if (cellB == null)
            {
                return false;
            }

            List<FieldCell> neighbors = RentNeighbors();
            try
            {
                generator.CollectNeighbors(cellA, neighbors);
                return neighbors.Contains(cellB);
            }
            finally
            {
                ReturnNeighbors(neighbors);
            }
        }

        private void SwapContent(FieldCell a, FieldCell b)
        {
            int temp = a.ContentId;
            a.ContentId = b.ContentId;
            b.ContentId = temp;
        }

        private int GetRandomTileId()
        {
            if (_availableTiles == null || _availableTiles.Count == 0)
            {
                return (int)Match3TileState.Red;
            }

            int index = NextRandomIndex(_availableTiles.Count);
            return (int)_availableTiles[index];
        }

        private int NextRandomIndex(int count)
        {
            Func<int, int> provider = RandomIndexProvider;
            return provider != null ? Mathf.Clamp(provider(count), 0, count - 1) : Random.Range(0, count);
        }

        private void FillBoardInitial()
        {
            Vector3Int size = _generator.GeneratedSize;
            for (int z = 0; z < size.z; z++)
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                FieldCell cell = _generator.GetCell(new Vector3Int(x, y, z));
                if (!CanUseCell(cell))
                {
                    continue;
                }

                cell.ContentId = GetInitialTileId(cell.Position);
            }
        }

        private int GetInitialTileId(Vector3Int pos)
        {
            if (!_avoidStartMatches)
            {
                return GetRandomTileId();
            }

            const int maxAttempts = 24;
            int fallback = GetRandomTileId();
            for (int i = 0; i < maxAttempts; i++)
            {
                int candidate = GetRandomTileId();
                if (!CreatesImmediateMatch(pos, candidate))
                {
                    return candidate;
                }

                fallback = candidate;
            }

            return fallback;
        }

        private bool CreatesImmediateMatch(Vector3Int pos, int candidate)
        {
            return CountInDirection(pos, Vector3Int.left, candidate) +
                   CountInDirection(pos, Vector3Int.right, candidate) + 1 >= _minMatchLength
                   || CountInDirection(pos, Vector3Int.down, candidate) +
                   CountInDirection(pos, Vector3Int.up, candidate) + 1 >= _minMatchLength;
        }

        private int CountInDirection(Vector3Int origin, Vector3Int dir, int tileId)
        {
            FieldGenerator generator = Generator;
            if (generator == null)
            {
                return 0;
            }

            int count = 0;
            Vector3Int current = origin + dir;
            while (true)
            {
                FieldCell cell = generator.GetCell(current);
                if (!CanUseCell(cell) || cell.ContentId != tileId)
                {
                    break;
                }

                count++;
                current += dir;
            }

            return count;
        }

        /// <summary>
        ///     Shuffles the usable tiles when no adjacent swap can create a match.
        /// </summary>
        /// <remarks>
        ///     Tries up to 80 shuffles, then one fresh fill, for a board without current matches and with a valid
        ///     move. If none is found the original content is restored. Warm calls reuse the service's buffers.
        /// </remarks>
        /// <returns>True when the board was shuffled (and <see cref="OnBoardShuffled" /> raised).</returns>
        public bool ShuffleIfNoMoves()
        {
            if (Generator == null)
            {
                return false;
            }

            if (TryFindValidSwap(out _, out _))
            {
                return false;
            }

            if (ShuffleBoardToPlayableState())
            {
                OnBoardShuffled.Invoke();
                return true;
            }

            return false;
        }

        private bool WouldSwapCreateMatch(Vector3Int aPos, Vector3Int bPos)
        {
            BoardSnapshot snapshot = RentSnapshot();
            try
            {
                return WouldSwapCreateMatch(snapshot, aPos, bPos);
            }
            finally
            {
                ReturnSnapshot(snapshot);
            }
        }

        private bool WouldSwapCreateMatch(BoardSnapshot snapshot, Vector3Int aPos, Vector3Int bPos)
        {
            if (!snapshot.IsUsable(aPos) || !snapshot.IsUsable(bPos))
            {
                return false;
            }

            snapshot.Swap(aPos, bPos);
            try
            {
                return snapshot.CreatesImmediateMatch(aPos, _minMatchLength) ||
                       snapshot.CreatesImmediateMatch(bPos, _minMatchLength);
            }
            finally
            {
                snapshot.Swap(aPos, bPos);
            }
        }

        private bool ShuffleBoardToPlayableState()
        {
            ResolveScratch scratch = RentScratch();
            try
            {
                return ShuffleBoardToPlayableState(scratch);
            }
            finally
            {
                ReturnScratch(scratch);
            }
        }

        private bool ShuffleBoardToPlayableState(ResolveScratch scratch)
        {
            List<FieldCell> cells = scratch.Cells;
            FieldCell[,,] board = _generator.Cells;
            if (board != null)
            {
                // WHY: same x, then y, then z order as FieldGenerator.GetAllCells, so a seeded random source shuffles
                // exactly as before, without creating that iterator.
                int sizeX = board.GetLength(0);
                int sizeY = board.GetLength(1);
                int sizeZ = board.GetLength(2);
                for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                for (int z = 0; z < sizeZ; z++)
                {
                    FieldCell cell = board[x, y, z];
                    if (CanUseCell(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            if (cells.Count == 0)
            {
                return false;
            }

            List<int> values = scratch.Values;
            List<int> originalValues = scratch.OriginalValues;
            for (int i = 0; i < cells.Count; i++)
            {
                FieldCell cell = cells[i];
                originalValues.Add(cell.ContentId);
                values.Add(cell.ContentId > 0 ? cell.ContentId : GetRandomTileId());
            }

            const int maxShuffleAttempts = 80;
            for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
            {
                ShuffleValues(values);
                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].ContentId = values[i];
                }

                if (!HasCurrentMatch() && TryFindValidSwap(out _, out _))
                {
                    return true;
                }
            }

            FillBoardInitial();
            if (!HasCurrentMatch() && TryFindValidSwap(out _, out _))
            {
                return true;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].ContentId = originalValues[i];
            }

            return false;
        }

        private void ShuffleValues(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = NextRandomIndex(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        /// <summary>
        ///     Clears, collapses and refills current matches until the board is stable or the cascade limit is reached.
        /// </summary>
        /// <returns>Number of cleared cells (a cell in a cross counts once per cascade step).</returns>
        private int ResolveBoardImmediate()
        {
            ResolveScratch scratch = RentScratch();
            try
            {
                HashSet<FieldCell> matched = scratch.Matched;
                int resolved = 0;
                int iterations = 0;
                while (iterations < _maxCascadeIterations && CollectCurrentMatches(matched) > 0)
                {
                    resolved += ClearCells(matched);
                    CollapseColumns();
                    FillEmptyCells();
                    iterations++;
                }

                return resolved;
            }
            finally
            {
                ReturnScratch(scratch);
            }
        }

        private bool UseDelayedResolve => _useResolveDelay && gameObject.activeInHierarchy;

        private void StartResolveRoutine(FieldCell swapA, FieldCell swapB)
        {
            // WHY: busy before StartCoroutine, because the routine's first step and its events run inside that call.
            _resolving = true;
            int token = ++_resolveToken;
            Coroutine handle = StartCoroutine(ResolveRoutine(swapA, swapB, token));
            if (token != _resolveToken)
            {
                return;
            }

            // WHY: a routine that finished without yielding (a cascade limit of zero) is no longer busy, and a routine
            // that could not start would otherwise stay busy forever; only a running one keeps its handle.
            if (handle != null && _resolving)
            {
                _resolveRoutine = handle;
            }
            else
            {
                _resolving = false;
            }
        }

        private void CancelResolve()
        {
            _resolveToken++;
            _resolving = false;
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }
        }

        private WaitForSeconds ResolveWait()
        {
            // WHY: one cached instance per delay value instead of three new ones per cascade step.
            if (_resolveWait == null || !_resolveWaitSeconds.Equals(_resolveDelaySeconds))
            {
                _resolveWaitSeconds = _resolveDelaySeconds;
                _resolveWait = new WaitForSeconds(_resolveDelaySeconds);
            }

            return _resolveWait;
        }

        /// <summary>
        ///     Delayed resolve: an optional swap step, then clear, collapse and refill steps with a delay after each.
        /// </summary>
        /// <remarks>
        ///     The routine re-checks <paramref name="token" /> after every external callback and every delay and stops
        ///     as soon as a cancel (disable, <see cref="InitializeBoard" />) made it stale, so it never edits a board
        ///     it no longer owns.
        /// </remarks>
        private IEnumerator ResolveRoutine(FieldCell swapA, FieldCell swapB, int token)
        {
            if (swapA != null && swapB != null)
            {
                SwapContent(swapA, swapB);
                OnResolvePhase?.Invoke(Match3ResolvePhase.Swap, 0);
                if (token != _resolveToken)
                {
                    yield break;
                }

                OnBoardChanged.Invoke();
                if (token != _resolveToken)
                {
                    yield break;
                }

                yield return ResolveWait();
                if (token != _resolveToken)
                {
                    yield break;
                }
            }

            int resolved = 0;
            int iterations = 0;
            while (iterations < _maxCascadeIterations)
            {
                int cleared = ClearMatchesStep(token, resolved);
                if (token != _resolveToken)
                {
                    yield break;
                }

                if (cleared == 0)
                {
                    break;
                }

                resolved += cleared;
                OnBoardChanged.Invoke();
                if (token != _resolveToken)
                {
                    yield break;
                }

                yield return ResolveWait();
                if (token != _resolveToken)
                {
                    yield break;
                }

                OnResolvePhase?.Invoke(Match3ResolvePhase.Collapse, resolved);
                if (token != _resolveToken)
                {
                    yield break;
                }

                CollapseColumns();
                OnBoardChanged.Invoke();
                if (token != _resolveToken)
                {
                    yield break;
                }

                yield return ResolveWait();
                if (token != _resolveToken)
                {
                    yield break;
                }

                OnResolvePhase?.Invoke(Match3ResolvePhase.Refill, resolved);
                if (token != _resolveToken)
                {
                    yield break;
                }

                FillEmptyCells();
                OnBoardChanged.Invoke();
                if (token != _resolveToken)
                {
                    yield break;
                }

                yield return ResolveWait();
                if (token != _resolveToken)
                {
                    yield break;
                }

                iterations++;
            }

            // WHY: idle before the completion events, so their listeners can chain the next move or start a new level;
            // the handle is dropped here because nothing is left to stop.
            _resolving = false;
            _resolveRoutine = null;
            OnResolvePhase?.Invoke(Match3ResolvePhase.Completed, resolved);
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
        }

        /// <summary>
        ///     The synchronous clear step of the delayed resolve: collects the current matches, raises the
        ///     <see cref="Match3ResolvePhase.Clear" /> phase and clears the matched cells.
        /// </summary>
        /// <remarks>
        ///     The scratch is rented and returned inside this step, so no buffer is held across a delay: Unity does not
        ///     run an iterator's finally block when <c>StopCoroutine</c> or a disable stops the routine, and a shuffle
        ///     between steps reuses the warm buffers.
        /// </remarks>
        /// <returns>Cleared cells; 0 when nothing matched or the Clear listener made the routine stale.</returns>
        private int ClearMatchesStep(int token, int resolved)
        {
            ResolveScratch scratch = RentScratch();
            try
            {
                HashSet<FieldCell> matched = scratch.Matched;
                if (CollectCurrentMatches(matched) == 0)
                {
                    return 0;
                }

                OnResolvePhase?.Invoke(Match3ResolvePhase.Clear, resolved);
                return token == _resolveToken ? ClearCells(matched) : 0;
            }
            finally
            {
                ReturnScratch(scratch);
            }
        }

        /// <summary>
        ///     Takes the spare board copy (or a new one when absent or sized for another generation) and fills every
        ///     slot from the live board, so the result equals a freshly created copy.
        /// </summary>
        private BoardSnapshot RentSnapshot()
        {
            Vector3Int size = _generator.GeneratedSize;
            BoardSnapshot snapshot = _spareSnapshot;
            _spareSnapshot = null;
            if (snapshot == null || snapshot.Size != size)
            {
                snapshot = new BoardSnapshot(size);
            }

            for (int z = 0; z < size.z; z++)
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int position = new Vector3Int(x, y, z);
                FieldCell cell = _generator.GetCell(position);
                bool usable = CanUseCell(cell);
                snapshot.SetCell(position, usable, usable ? cell.ContentId : 0);
            }

            return snapshot;
        }

        private void ReturnSnapshot(BoardSnapshot snapshot)
        {
            if (snapshot != null)
            {
                _spareSnapshot = snapshot;
            }
        }

        private List<FieldCell> RentNeighbors()
        {
            List<FieldCell> neighbors = _spareNeighbors ?? new List<FieldCell>(8);
            _spareNeighbors = null;
            return neighbors;
        }

        private void ReturnNeighbors(List<FieldCell> neighbors)
        {
            // WHY: cleared so the spare list never keeps cells of a regenerated board alive.
            neighbors.Clear();
            _spareNeighbors = neighbors;
        }

        private ResolveScratch RentScratch()
        {
            ResolveScratch scratch = _spareScratch ?? new ResolveScratch();
            _spareScratch = null;
            return scratch;
        }

        private void ReturnScratch(ResolveScratch scratch)
        {
            // WHY: cleared so the spare buffers never keep cells of a regenerated board alive.
            scratch.Clear();
            _spareScratch = scratch;
        }

        private sealed class ResolveScratch
        {
            public readonly HashSet<FieldCell> Matched = new();
            public readonly List<FieldCell> Cells = new();
            public readonly List<int> Values = new();
            public readonly List<int> OriginalValues = new();

            public void Clear()
            {
                Matched.Clear();
                Cells.Clear();
                Values.Clear();
                OriginalValues.Clear();
            }
        }

        private sealed class BoardSnapshot
        {
            private readonly int[,,] _content;
            private readonly Vector3Int _size;
            private readonly bool[,,] _usable;

            public BoardSnapshot(Vector3Int size)
            {
                _size = size;
                _usable = new bool[size.x, size.y, size.z];
                _content = new int[size.x, size.y, size.z];
            }

            public Vector3Int Size => _size;

            public void SetCell(Vector3Int pos, bool usable, int contentId)
            {
                _usable[pos.x, pos.y, pos.z] = usable;
                _content[pos.x, pos.y, pos.z] = contentId;
            }

            public bool IsUsable(Vector3Int pos)
            {
                if (!InBounds(pos))
                {
                    return false;
                }

                return _usable[pos.x, pos.y, pos.z];
            }

            public void Swap(Vector3Int a, Vector3Int b)
            {
                int temp = _content[a.x, a.y, a.z];
                _content[a.x, a.y, a.z] = _content[b.x, b.y, b.z];
                _content[b.x, b.y, b.z] = temp;
            }

            public bool CreatesImmediateMatch(Vector3Int pos, int minMatchLength)
            {
                int value = _content[pos.x, pos.y, pos.z];
                if (value <= 0)
                {
                    return false;
                }

                return CountInDirection(pos, Vector3Int.left, value) + CountInDirection(pos, Vector3Int.right, value) +
                       1 >= minMatchLength
                       || CountInDirection(pos, Vector3Int.down, value) + CountInDirection(pos, Vector3Int.up, value) +
                       1 >= minMatchLength;
            }

            private int CountInDirection(Vector3Int origin, Vector3Int dir, int value)
            {
                int count = 0;
                Vector3Int current = origin + dir;
                while (InBounds(current) && _usable[current.x, current.y, current.z] &&
                       _content[current.x, current.y, current.z] == value)
                {
                    count++;
                    current += dir;
                }

                return count;
            }

            private bool InBounds(Vector3Int pos)
            {
                return pos.x >= 0 && pos.x < _size.x && pos.y >= 0 && pos.y < _size.y && pos.z >= 0 && pos.z < _size.z;
            }
        }
    }
}
