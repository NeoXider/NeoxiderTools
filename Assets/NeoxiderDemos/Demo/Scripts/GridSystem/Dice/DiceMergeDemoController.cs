using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Merge;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Game loop of the authored dice merge demo: spawns the next piece from the dice pool, places it through
    ///     <see cref="DiceBoardService" />, scores merges and detects game over with
    ///     <see cref="DiceBoardService.CanPlaceAnywhere" />. The scene builder saves the UI and the persistent
    ///     listeners (buttons, the merge-size slider, <see cref="DiceBoardService.OnMergesResolved" />); this
    ///     component builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/GridSystem/DiceMergeDemoController")]
    public sealed class DiceMergeDemoController : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private DiceBoardService _diceBoard;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _poolText;
        [SerializeField] private TMP_Text _statusText;

        [Tooltip("Optional log feed that receives every action and merge.")]
        [SerializeField] private DemoLogFeed _log;

        private readonly DicePieceGenerator _pieceGenerator = new();
        private readonly List<int> _pool = DicePieceGenerator.CreateDefaultPool();
        private DicePiece _currentPiece;
        private int _score;
        private int _removedPoolBase = 1;
        private bool _gameOver;

        public UnityEvent OnDemoStateChanged = new();

        public DicePiece CurrentPiece => _currentPiece;
        public int Score => _score;
        public bool GameOver => _gameOver;
        public IReadOnlyList<int> Pool => _pool;

        public void Configure(
            FieldGenerator generator,
            DiceBoardService diceBoard,
            TMP_Text scoreText,
            TMP_Text poolText,
            TMP_Text statusText)
        {
            _generator = generator;
            _diceBoard = diceBoard;
            _scoreText = scoreText;
            _poolText = poolText;
            _statusText = statusText;
            RefreshTexts();
        }

        private void Start()
        {
            if (_currentPiece == null)
            {
                RestartDemo();
            }
        }

        public void RestartDemo()
        {
            if (_generator == null || _diceBoard == null)
            {
                _currentPiece = null;
                _gameOver = true;
                SetStatus("Demo is not wired");
                OnDemoStateChanged.Invoke();
                return;
            }

            _score = 0;
            _removedPoolBase = 1;
            _gameOver = false;
            _pool.Clear();
            _pool.AddRange(DicePieceGenerator.CreateDefaultPool());
            EnsureBoard();
            _diceBoard.ClearBoard();
            SpawnNextPiece();
            SetStatus("Drag the next piece onto the board");
        }

        public void SpawnNextPiece()
        {
            _currentPiece = _pieceGenerator.Generate(_pool);
            _gameOver = !CanPlaceCurrentPieceAnywhere();
            if (_gameOver)
            {
                SetStatus("Game over: the piece fits nowhere · CanPlaceAnywhere");
            }
            else
            {
                SetStatusText("New piece in the tray");
            }

            RefreshTexts();
            OnDemoStateChanged.Invoke();
        }

        public void RotateCurrentPiece()
        {
            if (_currentPiece == null || _currentPiece.CellCount < 2 || _gameOver)
            {
                return;
            }

            _currentPiece = _currentPiece.RotateClockwise();
            SetStatus("Pair rotated · DicePiece.RotateClockwise");
            OnDemoStateChanged.Invoke();
        }

        /// <summary>Button target: reports the first anchor and rotation where the current piece fits.</summary>
        public void ShowHint()
        {
            if (_currentPiece == null || _diceBoard == null || _gameOver)
            {
                return;
            }

            if (_diceBoard.TryFindPlacement(_currentPiece, true, out Vector3Int anchor, out int turns))
            {
                SetStatus($"Hint: fits at {anchor.x},{anchor.y}" + (turns > 0 ? $" after {turns} turn(s)" : string.Empty) +
                          " · TryFindPlacement");
                return;
            }

            SetStatus("Hint: no placement left");
        }

        /// <summary>Slider target: connected dice needed for a merge (DiceBoardService.MinMergeGroupSize).</summary>
        public void SetMergeGroupSize(float size)
        {
            if (_diceBoard == null)
            {
                return;
            }

            int value = Mathf.Max(2, Mathf.RoundToInt(size));
            if (_diceBoard.MinMergeGroupSize == value)
            {
                return;
            }

            _diceBoard.MinMergeGroupSize = value;
            Log($"Merge group size {value} · DiceBoardService.MinMergeGroupSize");
        }

        /// <summary>Persistent target of <see cref="DiceBoardService.OnMergesResolved" />.</summary>
        public void HandleMergesResolved(GridMergeResult result)
        {
            if (result == null || !result.HasChanges)
            {
                return;
            }

            foreach (GridMergeGroupResult group in result.Groups)
            {
                Log($"{group.Positions.Count}×{group.SourceContentId} merged into {group.ResultContentId} · OnMergesResolved");
            }
        }

        public bool TryPlaceCurrentPiece(Vector3Int anchor)
        {
            if (_gameOver || _currentPiece == null || _diceBoard == null)
            {
                return false;
            }

            DicePlacementResult placement = _diceBoard.Place(_currentPiece, anchor, true);
            if (!placement.Placed)
            {
                SetStatus("Cannot place there");
                return false;
            }

            ApplyMergeRewards(placement.MergeResult);
            ApplyPoolProgression(placement.MergeResult);
            SpawnNextPiece();
            return true;
        }

        public void ForceCurrentPieceForTest(DicePiece piece)
        {
            _currentPiece = piece;
            _gameOver = !CanPlaceCurrentPieceAnywhere();
            RefreshTexts();
            OnDemoStateChanged.Invoke();
        }

        public void FillBoardForGameOverTest()
        {
            EnsureBoard();
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                cell.ContentId = 9;
                cell.IsOccupied = true;
                _generator.OnCellStateChanged.Invoke(cell);
            }

            ForceCurrentPieceForTest(DicePiece.Single(1));
        }

        private void ApplyMergeRewards(GridMergeResult result)
        {
            if (result == null || !result.HasChanges)
            {
                return;
            }

            foreach (GridMergeGroupResult group in result.Groups)
            {
                _score += group.ResultContentId;
            }
        }

        private void ApplyPoolProgression(GridMergeResult result)
        {
            if (result == null || !result.HasChanges)
            {
                RefreshTexts();
                return;
            }

            int highestResult = 0;
            foreach (GridMergeGroupResult group in result.Groups)
            {
                highestResult = Mathf.Max(highestResult, group.ResultContentId);
            }

            while (highestResult >= _removedPoolBase + 7)
            {
                int removed = _removedPoolBase;
                int added = _removedPoolBase + 5;
                _pool.Remove(removed);
                if (!_pool.Contains(added))
                {
                    _pool.Add(added);
                }

                RemoveValueFromBoard(removed);
                Log($"Pool shifted: {removed} retired, {added} added");
                _removedPoolBase++;
            }

            RefreshTexts();
        }

        private void RemoveValueFromBoard(int value)
        {
            foreach (FieldCell cell in _generator.GetAllCells(false))
            {
                if (cell.ContentId != value)
                {
                    continue;
                }

                cell.ContentId = _diceBoard.EmptyContentId;
                cell.IsOccupied = false;
                _generator.OnCellStateChanged.Invoke(cell);
            }
        }

        private bool CanPlaceCurrentPieceAnywhere()
        {
            return _currentPiece != null && _generator != null && _diceBoard != null &&
                   _diceBoard.CanPlaceAnywhere(_currentPiece);
        }

        private void EnsureBoard()
        {
            if (_generator != null && (_generator.Cells == null || _generator.Cells.Length == 0))
            {
                _generator.GenerateField();
            }
        }

        private void RefreshTexts()
        {
            if (_scoreText != null)
            {
                _scoreText.text = _score.ToString();
            }

            if (_poolText != null)
            {
                _poolText.text = string.Join(", ", _pool);
            }
        }

        private void SetStatus(string text)
        {
            SetStatusText(text);
            Log(text);
        }

        private void SetStatusText(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        private void Log(string text)
        {
            if (_log != null)
            {
                _log.Log(text);
            }
        }
    }
}
