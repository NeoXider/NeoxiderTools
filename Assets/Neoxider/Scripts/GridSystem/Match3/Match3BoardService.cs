using System.Collections;
using System.Collections.Generic;
using Neo;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.Match3
{
    /// <summary>
    /// Runtime board service for Match3: initialization, swap validation, match resolve and refill.
    /// </summary>
    [NeoDoc("GridSystem/Match3/Match3BoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [AddComponentMenu("Neo/GridSystem/Match3/Match3BoardService")]
    public class Match3BoardService : MonoBehaviour
    {
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
        /// Invoked after board content changes.
        /// </summary>
        public UnityEvent OnBoardChanged = new();

        /// <summary>
        /// Invoked after resolve step; argument is number of removed tiles.
        /// </summary>
        public UnityEvent<int> OnMatchesResolved = new();

        /// <summary>
        /// Invoked when board was shuffled due to no available moves.
        /// </summary>
        public UnityEvent OnBoardShuffled = new();

        public event System.Action<Match3ResolvePhase, int> OnResolvePhase;

        private FieldGenerator _generator;
        private Coroutine _resolveRoutine;

        private void Awake()
        {
            _generator = GetComponent<FieldGenerator>();
        }

        private void Start()
        {
            if (_autoGenerateOnStart)
            {
                InitializeBoard();
            }
        }

        [Button("Initialize Board")]
        public void InitializeBoardButton()
        {
            InitializeBoard();
        }

        [Button("Resolve Current Matches")]
        public void ResolveCurrentMatchesButton()
        {
            if (_resolveRoutine != null)
            {
                return;
            }

            List<List<FieldCell>> matches = FindMatches();
            if (matches.Count == 0)
            {
                OnBoardChanged.Invoke();
                return;
            }

            if (_useResolveDelay)
            {
                _resolveRoutine = StartCoroutine(ResolveExistingMatchesRoutine(matches));
                return;
            }

            int resolved = ResolveBoardImmediate(matches);
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
        }

        [Button("Shuffle If No Moves")]
        public void ShuffleIfNoMovesButton()
        {
            ShuffleIfNoMoves();
            OnBoardChanged.Invoke();
        }

        /// <summary>
        /// Fills all enabled cells with random tiles from configured set.
        /// </summary>
        public void InitializeBoard()
        {
            if (_generator == null)
            {
                return;
            }

            FillBoardInitial();
            if (_resolveRoutine != null)
            {
                StopCoroutine(_resolveRoutine);
                _resolveRoutine = null;
            }

            if (!_avoidStartMatches && _resolveSpawnMatchesWhenAllowed)
            {
                List<List<FieldCell>> matches = FindMatches();
                if (matches.Count > 0)
                {
                    if (_useResolveDelay)
                    {
                        OnBoardChanged.Invoke();
                        _resolveRoutine = StartCoroutine(ResolveExistingMatchesRoutine(matches));
                        return;
                    }

                    int resolved = ResolveBoardImmediate(matches);
                    if (resolved > 0)
                    {
                        OnMatchesResolved.Invoke(resolved);
                    }
                }
            }

            OnBoardChanged.Invoke();
        }

        /// <summary>
        /// Tries to swap two adjacent cells and resolves all resulting cascades.
        /// </summary>
        /// <param name="a">First cell position.</param>
        /// <param name="b">Second cell position.</param>
        /// <returns>
        /// True if swap produced at least one valid match and was resolved;
        /// false when swap is invalid or reverted.
        /// </returns>
        public bool TrySwapAndResolve(Vector3Int a, Vector3Int b)
        {
            if (_resolveRoutine != null)
            {
                return false;
            }

            if (!AreAdjacent(a, b))
            {
                return false;
            }

            FieldCell cellA = _generator.GetCell(a);
            FieldCell cellB = _generator.GetCell(b);
            if (!CanUseCell(cellA) || !CanUseCell(cellB))
            {
                return false;
            }

            if (!WouldSwapCreateMatch(a, b))
            {
                OnBoardChanged.Invoke();
                return false;
            }

            if (_useResolveDelay)
            {
                _resolveRoutine = StartCoroutine(ResolveSwapRoutine(cellA, cellB));
                return true;
            }

            SwapContent(cellA, cellB);
            int resolved = ResolveBoardImmediate(FindMatches());
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
            return true;
        }

        /// <summary>
        /// Finds all current match groups on the board.
        /// </summary>
        /// <returns>List of match groups.</returns>
        public List<List<FieldCell>> FindMatches()
        {
            return Match3MatchFinder.FindMatches(_generator, _minMatchLength);
        }

        private int ClearMatches(List<List<FieldCell>> matches)
        {
            int cleared = 0;
            HashSet<FieldCell> unique = new();
            foreach (List<FieldCell> group in matches)
            {
                foreach (FieldCell cell in group)
                {
                    unique.Add(cell);
                }
            }

            foreach (FieldCell cell in unique)
            {
                cell.ContentId = 0;
                cleared++;
            }

            return cleared;
        }

        private void CollapseColumns()
        {
            Vector3Int size = _generator.Config.Size;
            for (int z = 0; z < size.z; z++)
            for (int x = 0; x < size.x; x++)
            {
                int writeY = 0;
                for (int y = 0; y < size.y; y++)
                {
                    FieldCell readCell = _generator.GetCell(new Vector3Int(x, y, z));
                    if (!CanUseCell(readCell) || readCell.ContentId <= 0)
                    {
                        continue;
                    }

                    if (y != writeY)
                    {
                        FieldCell writeCell = _generator.GetCell(new Vector3Int(x, writeY, z));
                        if (CanUseCell(writeCell))
                        {
                            writeCell.ContentId = readCell.ContentId;
                            readCell.ContentId = 0;
                        }
                    }

                    writeY++;
                }
            }
        }

        private void FillEmptyCells()
        {
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
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

        private static bool AreAdjacent(Vector3Int a, Vector3Int b)
        {
            Vector3Int delta = a - b;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) + Mathf.Abs(delta.z) == 1;
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

            int index = Random.Range(0, _availableTiles.Count);
            return (int)_availableTiles[index];
        }

        private void FillBoardInitial()
        {
            Vector3Int size = _generator.Config.Size;
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
            int count = 0;
            Vector3Int current = origin + dir;
            while (true)
            {
                FieldCell cell = _generator.GetCell(current);
                if (!CanUseCell(cell) || cell.ContentId != tileId)
                {
                    break;
                }

                count++;
                current += dir;
            }

            return count;
        }

        public bool ShuffleIfNoMoves()
        {
            if (HasAnyValidSwap())
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

        private bool HasAnyValidSwap()
        {
            Vector3Int size = _generator.Config.Size;
            for (int z = 0; z < size.z; z++)
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int pos = new(x, y, z);
                FieldCell cell = _generator.GetCell(pos);
                if (!CanUseCell(cell))
                {
                    continue;
                }

                if (WouldSwapCreateMatch(pos, pos + Vector3Int.right) || WouldSwapCreateMatch(pos, pos + Vector3Int.up))
                {
                    return true;
                }
            }

            return false;
        }

        private bool WouldSwapCreateMatch(Vector3Int aPos, Vector3Int bPos)
        {
            BoardSnapshot snapshot = CreateSnapshot();
            if (!snapshot.IsUsable(aPos) || !snapshot.IsUsable(bPos))
            {
                return false;
            }

            snapshot.Swap(aPos, bPos);
            return snapshot.CreatesImmediateMatch(aPos, _minMatchLength) ||
                   snapshot.CreatesImmediateMatch(bPos, _minMatchLength);
        }

        private bool ShuffleBoardToPlayableState()
        {
            List<FieldCell> cells = new();
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                if (CanUseCell(cell))
                {
                    cells.Add(cell);
                }
            }

            if (cells.Count == 0)
            {
                return false;
            }

            List<int> values = new(cells.Count);
            foreach (FieldCell cell in cells)
            {
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

                if (FindMatches().Count == 0 && HasAnyValidSwap())
                {
                    return true;
                }
            }

            FillBoardInitial();
            return true;
        }

        private static void ShuffleValues(List<int> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        private int ResolveBoardImmediate(List<List<FieldCell>> initialMatches)
        {
            int resolved = 0;
            int iterations = 0;
            List<List<FieldCell>> matches = initialMatches;
            while (matches.Count > 0 && iterations < _maxCascadeIterations)
            {
                resolved += ClearMatches(matches);
                CollapseColumns();
                FillEmptyCells();
                matches = FindMatches();
                iterations++;
            }

            return resolved;
        }

        private IEnumerator ResolveExistingMatchesRoutine(List<List<FieldCell>> initialMatches)
        {
            int resolved = 0;
            int iterations = 0;
            List<List<FieldCell>> matches = initialMatches;
            while (matches.Count > 0 && iterations < _maxCascadeIterations)
            {
                OnResolvePhase?.Invoke(Match3ResolvePhase.Clear, resolved);
                resolved += ClearMatches(matches);
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                OnResolvePhase?.Invoke(Match3ResolvePhase.Collapse, resolved);
                CollapseColumns();
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                OnResolvePhase?.Invoke(Match3ResolvePhase.Refill, resolved);
                FillEmptyCells();
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                matches = FindMatches();
                iterations++;
            }

            OnResolvePhase?.Invoke(Match3ResolvePhase.Completed, resolved);
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
            _resolveRoutine = null;
        }

        private IEnumerator ResolveSwapRoutine(FieldCell a, FieldCell b)
        {
            SwapContent(a, b);
            OnResolvePhase?.Invoke(Match3ResolvePhase.Swap, 0);
            OnBoardChanged.Invoke();
            yield return new WaitForSeconds(_resolveDelaySeconds);

            int resolved = 0;
            int iterations = 0;
            List<List<FieldCell>> matches = FindMatches();
            while (matches.Count > 0 && iterations < _maxCascadeIterations)
            {
                OnResolvePhase?.Invoke(Match3ResolvePhase.Clear, resolved);
                resolved += ClearMatches(matches);
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                OnResolvePhase?.Invoke(Match3ResolvePhase.Collapse, resolved);
                CollapseColumns();
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                OnResolvePhase?.Invoke(Match3ResolvePhase.Refill, resolved);
                FillEmptyCells();
                OnBoardChanged.Invoke();
                yield return new WaitForSeconds(_resolveDelaySeconds);

                matches = FindMatches();
                iterations++;
            }

            OnResolvePhase?.Invoke(Match3ResolvePhase.Completed, resolved);
            OnMatchesResolved.Invoke(resolved);
            OnBoardChanged.Invoke();
            _resolveRoutine = null;
        }

        private BoardSnapshot CreateSnapshot()
        {
            Vector3Int size = _generator.Config.Size;
            BoardSnapshot snapshot = new(size);
            for (int z = 0; z < size.z; z++)
            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                FieldCell cell = _generator.GetCell(new Vector3Int(x, y, z));
                bool usable = CanUseCell(cell);
                snapshot.SetCell(new Vector3Int(x, y, z), usable, usable ? cell.ContentId : 0);
            }

            return snapshot;
        }

        public enum Match3ResolvePhase
        {
            Swap,
            Clear,
            Collapse,
            Refill,
            Completed
        }

        private sealed class BoardSnapshot
        {
            private readonly bool[,,] _usable;
            private readonly int[,,] _content;
            private readonly Vector3Int _size;

            public BoardSnapshot(Vector3Int size)
            {
                _size = size;
                _usable = new bool[size.x, size.y, size.z];
                _content = new int[size.x, size.y, size.z];
            }

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