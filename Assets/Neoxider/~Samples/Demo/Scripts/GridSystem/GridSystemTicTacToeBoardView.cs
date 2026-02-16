using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neo/Demo/GridSystem/GridSystemTicTacToeBoardView")]
    public class GridSystemTicTacToeBoardView : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private TicTacToeBoardService _board;
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector2 _cellSize = new(0.9f, 0.9f);
        [SerializeField] private float _textScale = 5f;
        [SerializeField] private Color _enabledColor = new(0.12f, 0.13f, 0.18f, 1f);
        [SerializeField] private Color _blockedColor = new(0.45f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _disabledColor = new(0.22f, 0.22f, 0.22f, 1f);
        [SerializeField] private Color _xColor = new(0.2f, 0.85f, 1f, 1f);
        [SerializeField] private Color _oColor = new(1f, 0.75f, 0.25f, 1f);

        private readonly Dictionary<Vector3Int, CellView> _views = new();

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.AddListener(Rebuild);
                _generator.OnCellStateChanged.AddListener(UpdateSingleCell);
            }

            if (_board != null)
            {
                _board.OnBoardReset.AddListener(RefreshAll);
                _board.OnWinnerDetected.AddListener(HandleWinner);
                _board.OnDrawDetected.AddListener(RefreshAll);
                _board.OnPlayerChanged.AddListener(HandlePlayerChanged);
            }

            Rebuild();
        }

        private void OnDisable()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.RemoveListener(Rebuild);
                _generator.OnCellStateChanged.RemoveListener(UpdateSingleCell);
            }

            if (_board != null)
            {
                _board.OnBoardReset.RemoveListener(RefreshAll);
                _board.OnWinnerDetected.RemoveListener(HandleWinner);
                _board.OnDrawDetected.RemoveListener(RefreshAll);
                _board.OnPlayerChanged.RemoveListener(HandlePlayerChanged);
            }
        }

        private void Update()
        {
            if (_board == null || _generator == null || _camera == null)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return;
            }

            CellViewMarker marker = hit.collider.GetComponent<CellViewMarker>();
            if (marker == null)
            {
                return;
            }

            if (_board.TryMakeMove(marker.Position))
            {
                RefreshAll();
            }
        }

        public void Rebuild()
        {
            ClearAll();
            if (_generator == null)
            {
                return;
            }

            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                CreateCellView(cell);
            }

            RefreshAll();
        }

        public void RefreshAll()
        {
            if (_generator == null)
            {
                return;
            }

            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                UpdateSingleCell(cell);
            }
        }

        private void HandleWinner(int _)
        {
            RefreshAll();
        }

        private void HandlePlayerChanged(int _)
        {
            RefreshAll();
        }

        private void CreateCellView(FieldCell cell)
        {
            Vector3 center = _generator.GetCellWorldCenter(cell.Position);
            GameObject root = new($"Cell_{cell.Position.x}_{cell.Position.y}_{cell.Position.z}");
            root.transform.SetParent(transform, false);
            root.transform.position = center;

            GameObject tileGo = new("Tile");
            tileGo.transform.SetParent(root.transform, false);
            tileGo.transform.localPosition = Vector3.zero;
            SpriteRenderer tile = tileGo.AddComponent<SpriteRenderer>();
            tile.sprite = CreateSolidSprite();
            tile.transform.localScale = new Vector3(_cellSize.x, _cellSize.y, 1f);

            GameObject markGo = new("Mark");
            markGo.transform.SetParent(root.transform, false);
            markGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro mark = markGo.AddComponent<TextMeshPro>();
            mark.alignment = TextAlignmentOptions.Center;
            mark.fontSize = _textScale;
            mark.text = string.Empty;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(_cellSize.x, _cellSize.y, 0.2f);

            CellViewMarker marker = root.AddComponent<CellViewMarker>();
            marker.Position = cell.Position;

            _views[cell.Position] = new CellView
            {
                Tile = tile,
                Mark = mark
            };
        }

        private void UpdateSingleCell(FieldCell cell)
        {
            if (cell == null || !_views.TryGetValue(cell.Position, out CellView view))
            {
                return;
            }

            if (!cell.IsEnabled)
            {
                view.Tile.color = _disabledColor;
            }
            else
            {
                view.Tile.color = cell.IsWalkable ? _enabledColor : _blockedColor;
            }

            TicTacToeCellState state = (TicTacToeCellState)cell.ContentId;
            switch (state)
            {
                case TicTacToeCellState.PlayerX:
                    view.Mark.text = "X";
                    view.Mark.color = _xColor;
                    break;
                case TicTacToeCellState.PlayerO:
                    view.Mark.text = "O";
                    view.Mark.color = _oColor;
                    break;
                default:
                    view.Mark.text = string.Empty;
                    break;
            }
        }

        private void ClearAll()
        {
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _views.Clear();
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        private sealed class CellView
        {
            public SpriteRenderer Tile;
            public TextMeshPro Mark;
        }

        public sealed class CellViewMarker : MonoBehaviour
        {
            public Vector3Int Position;
        }
    }
}