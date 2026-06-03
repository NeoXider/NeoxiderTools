using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.GridSystem.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/DiceMergeDemoController")]
    public sealed class DiceMergeDemoController : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private DiceBoardService _diceBoard;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _poolText;
        [SerializeField] private TMP_Text _statusText;

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
            _score = 0;
            _removedPoolBase = 1;
            _gameOver = false;
            _pool.Clear();
            _pool.AddRange(DicePieceGenerator.CreateDefaultPool());
            EnsureBoard();
            _diceBoard.ClearBoard();
            SpawnNextPiece();
            SetStatus("Drag dice onto the board");
        }

        public void SpawnNextPiece()
        {
            _currentPiece = _pieceGenerator.Generate(_pool);
            _gameOver = !CanPlaceCurrentPieceAnywhere();
            SetStatus(_gameOver ? "Game over" : "New dice spawned");
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
            SetStatus("Pair rotated");
            OnDemoStateChanged.Invoke();
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
            if (_currentPiece == null || _generator == null || _diceBoard == null)
            {
                return false;
            }

            foreach (DicePiece orientation in EnumerateOrientations(_currentPiece))
            {
                foreach (FieldCell cell in _generator.GetAllCells(false))
                {
                    if (_diceBoard.CanPlace(orientation, cell.Position))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<DicePiece> EnumerateOrientations(DicePiece piece)
        {
            DicePiece current = piece;
            int count = piece.CellCount > 1 ? 4 : 1;
            for (int i = 0; i < count; i++)
            {
                yield return current;
                current = current.RotateClockwise();
            }
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
                _scoreText.text = "Score: " + _score;
            }

            if (_poolText != null)
            {
                _poolText.text = "Pool: " + string.Join(", ", _pool);
            }
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }
    }
}
