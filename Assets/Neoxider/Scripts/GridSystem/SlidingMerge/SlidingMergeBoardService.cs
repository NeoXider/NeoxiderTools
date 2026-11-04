using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     Scene component wrapper for 2048-like slide, merge and spawn gameplay. The rules live in the pure
    ///     <see cref="SlidingMergeResolver" />; this component adds Inspector settings, spawning after moves and events.
    /// </summary>
    [NeoDoc("GridSystem/SlidingMerge/SlidingMergeBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/SlidingMerge/SlidingMergeBoardService")]
    [AddComponentMenu("Neoxider/GridSystem/SlidingMerge/SlidingMergeBoardService")]
    public class SlidingMergeBoardService : MonoBehaviour
    {
        [Tooltip("Content id that marks an empty cell.")]
        [SerializeField] private int _emptyContentId;

        [Tooltip("Value written by SpawnDefaultContent, the initial spawn and the spawn after each successful slide.")]
        [SerializeField] private int _defaultSpawnContentId = 2;

        [Tooltip("Empty every enabled, walkable cell in Start.")]
        [SerializeField] private bool _clearOnStart = true;

        [Tooltip("Spawn Initial Spawn Count values in Start.")]
        [SerializeField] private bool _spawnInitialContentOnStart = true;

        [Tooltip("How many default values to spawn in Start.")]
        [SerializeField, Min(0)] private int _initialSpawnCount = 2;

        [Tooltip("Spawn one default value after every slide that changed the board.")]
        [SerializeField] private bool _spawnAfterSuccessfulSlide = true;

        /// <summary>Raised after any board change made through this component (clear, slide, spawn).</summary>
        public UnityEvent OnBoardChanged = new();

        /// <summary>Raised when the spawn after a successful slide found no empty cell.</summary>
        public UnityEvent OnBoardFull = new();

        /// <summary>Raised with the merged-value sum of a slide that merged at least one pair.</summary>
        public UnityEvent<int> OnScoreDelta = new();

        /// <summary>Raised after a successful slide (and its spawn) when no direction can change the board any more.</summary>
        public UnityEvent OnNoMovesLeft = new();

        /// <summary>Raised with the cell position of every spawned value, for spawn animations.</summary>
        public UnityEvent<Vector3Int> OnContentSpawned = new();

        private FieldGenerator _generator;

        /// <summary>C# event raised with the result of every slide that changed the board, before its spawn.</summary>
        public event Action<SlidingMergeResult> Slid;

        /// <summary>C# event raised with every spawned cell.</summary>
        public event Action<FieldCell> ContentSpawned;

        /// <summary>Content id that marks an empty cell.</summary>
        public int EmptyContentId
        {
            get => _emptyContentId;
            set => _emptyContentId = value;
        }

        /// <summary>Value spawned by <see cref="SpawnDefaultContent" />.</summary>
        public int DefaultSpawnContentId
        {
            get => _defaultSpawnContentId;
            set => _defaultSpawnContentId = value;
        }

        /// <summary>Spawn one default value after every slide that changed the board.</summary>
        public bool SpawnAfterSuccessfulSlide
        {
            get => _spawnAfterSuccessfulSlide;
            set => _spawnAfterSuccessfulSlide = value;
        }

        /// <summary>
        ///     Optional spawn index source (receives the candidate count, returns an index). Uses
        ///     <see cref="UnityEngine.Random" /> when null; inject it for deterministic tests and replays.
        /// </summary>
        public Func<int, int> RandomIndexProvider { get; set; }

        /// <summary>Optional custom merge predicate forwarded to <see cref="SlidingMergeResolver" />.</summary>
        public Func<int, int, bool> CanMergeRule { get; set; }

        /// <summary>Optional custom merged-value calculator forwarded to <see cref="SlidingMergeResolver" />.</summary>
        public Func<int, int, int> MergeRule { get; set; }

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

        private void Awake()
        {
            _generator = Generator;
        }

        private void Start()
        {
            if (_clearOnStart)
            {
                ClearBoard();
            }

            if (_spawnInitialContentOnStart)
            {
                for (int i = 0; i < _initialSpawnCount; i++)
                {
                    SpawnDefaultContent();
                }
            }
        }

        /// <summary>
        ///     Empties every enabled, walkable cell. Cell notifications follow after every cell has been cleared.
        /// </summary>
        [Button("Clear Board", PlayModeOnly = true)]
        public void ClearBoard()
        {
            FieldGenerator generator = Generator;
            if (generator == null)
            {
                return;
            }

            FieldCell[,,] cells = generator.Cells;
            if (cells != null)
            {
                foreach (FieldCell cell in cells)
                {
                    if (IsClearable(cell))
                    {
                        cell.ContentId = _emptyContentId;
                    }
                }

                foreach (FieldCell cell in cells)
                {
                    if (IsClearable(cell))
                    {
                        generator.OnCellStateChanged?.Invoke(cell);
                    }
                }
            }

            OnBoardChanged.Invoke();
        }

        private static bool IsClearable(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && cell.IsWalkable;
        }

        /// <summary>Returns true when sliding in <paramref name="direction" /> would change the board.</summary>
        public bool CanSlide(SlidingMergeDirection direction)
        {
            return SlidingMergeResolver.CanSlide(Generator, direction, _emptyContentId, CanMergeRule, MergeRule);
        }

        /// <summary>Returns true when at least one direction can still change the board.</summary>
        public bool HasAnyMove()
        {
            return SlidingMergeResolver.HasAnyMove(Generator, _emptyContentId, CanMergeRule, MergeRule);
        }

        /// <summary>
        ///     Slides the board, raises score/board events, spawns the next value when configured and reports a locked
        ///     board through <see cref="OnNoMovesLeft" />.
        /// </summary>
        public SlidingMergeResult Slide(SlidingMergeDirection direction)
        {
            SlidingMergeResult result =
                SlidingMergeResolver.Slide(Generator, direction, _emptyContentId, CanMergeRule, MergeRule);
            if (!result.Changed)
            {
                return result;
            }

            Slid?.Invoke(result);
            if (result.ScoreDelta > 0)
            {
                OnScoreDelta.Invoke(result.ScoreDelta);
            }

            if (_spawnAfterSuccessfulSlide && !SpawnDefaultContent())
            {
                OnBoardFull.Invoke();
            }

            OnBoardChanged.Invoke();
            if (!HasAnyMove())
            {
                OnNoMovesLeft.Invoke();
            }

            return result;
        }

        /// <summary>Spawns <see cref="DefaultSpawnContentId" /> into a random empty cell.</summary>
        public bool SpawnDefaultContent()
        {
            return SpawnContent(_defaultSpawnContentId);
        }

        /// <summary>Spawns <paramref name="contentId" /> into a random empty cell.</summary>
        /// <returns>False when no empty cell exists.</returns>
        public bool SpawnContent(int contentId)
        {
            bool spawned = SlidingMergeResolver.TrySpawnRandomContent(
                Generator, contentId, out FieldCell spawnedCell, _emptyContentId, true, RandomIndexProvider);
            if (spawned)
            {
                ContentSpawned?.Invoke(spawnedCell);
                OnContentSpawned.Invoke(spawnedCell.Position);
                OnBoardChanged.Invoke();
            }

            return spawned;
        }

        [Button("Slide Left", PlayModeOnly = true)]
        public void SlideLeft()
        {
            Slide(SlidingMergeDirection.Left);
        }

        [Button("Slide Right", PlayModeOnly = true)]
        public void SlideRight()
        {
            Slide(SlidingMergeDirection.Right);
        }

        [Button("Slide Up", PlayModeOnly = true)]
        public void SlideUp()
        {
            Slide(SlidingMergeDirection.Up);
        }

        [Button("Slide Down", PlayModeOnly = true)]
        public void SlideDown()
        {
            Slide(SlidingMergeDirection.Down);
        }
    }
}
