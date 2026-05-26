using UnityEngine;
using UnityEngine.Events;

namespace Neo.GridSystem.SlidingMerge
{
    /// <summary>
    ///     Scene component wrapper for 2048-like slide, merge and spawn gameplay.
    /// </summary>
    [NeoDoc("GridSystem/SlidingMerge/SlidingMergeBoardService.md")]
    [RequireComponent(typeof(FieldGenerator))]
    [CreateFromMenu("Neoxider/GridSystem/SlidingMerge/SlidingMergeBoardService")]
    [AddComponentMenu("Neoxider/GridSystem/SlidingMerge/SlidingMergeBoardService")]
    public class SlidingMergeBoardService : MonoBehaviour
    {
        [SerializeField] private int _emptyContentId;
        [SerializeField] private int _defaultSpawnContentId = 2;
        [SerializeField] private bool _clearOnStart = true;
        [SerializeField] private bool _spawnInitialContentOnStart = true;
        [SerializeField] private int _initialSpawnCount = 2;
        [SerializeField] private bool _spawnAfterSuccessfulSlide = true;

        public UnityEvent OnBoardChanged = new();
        public UnityEvent OnBoardFull = new();
        public UnityEvent<int> OnScoreDelta = new();

        private FieldGenerator _generator;

        public int EmptyContentId
        {
            get => _emptyContentId;
            set => _emptyContentId = value;
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

        [Button("Clear Board")]
        public void ClearBoard()
        {
            if (Generator == null)
            {
                return;
            }

            foreach (FieldCell cell in Generator.GetAllCells(false))
            {
                if (cell.IsWalkable)
                {
                    cell.ContentId = _emptyContentId;
                    Generator.OnCellStateChanged.Invoke(cell);
                }
            }

            OnBoardChanged.Invoke();
        }

        public bool CanSlide(SlidingMergeDirection direction)
        {
            return SlidingMergeResolver.CanSlide(Generator, direction, _emptyContentId);
        }

        public SlidingMergeResult Slide(SlidingMergeDirection direction)
        {
            SlidingMergeResult result = SlidingMergeResolver.Slide(Generator, direction, _emptyContentId);
            if (!result.Changed)
            {
                return result;
            }

            if (result.ScoreDelta > 0)
            {
                OnScoreDelta.Invoke(result.ScoreDelta);
            }

            if (_spawnAfterSuccessfulSlide && !SpawnDefaultContent())
            {
                OnBoardFull.Invoke();
            }

            OnBoardChanged.Invoke();
            return result;
        }

        public bool SpawnDefaultContent()
        {
            return SpawnContent(_defaultSpawnContentId);
        }

        public bool SpawnContent(int contentId)
        {
            bool spawned = SlidingMergeResolver.TrySpawnRandomContent(Generator, contentId, _emptyContentId);
            if (spawned)
            {
                OnBoardChanged.Invoke();
            }

            return spawned;
        }

        [Button("Slide Left")]
        public void SlideLeft()
        {
            Slide(SlidingMergeDirection.Left);
        }

        [Button("Slide Right")]
        public void SlideRight()
        {
            Slide(SlidingMergeDirection.Right);
        }

        [Button("Slide Up")]
        public void SlideUp()
        {
            Slide(SlidingMergeDirection.Up);
        }

        [Button("Slide Down")]
        public void SlideDown()
        {
            Slide(SlidingMergeDirection.Down);
        }
    }
}
