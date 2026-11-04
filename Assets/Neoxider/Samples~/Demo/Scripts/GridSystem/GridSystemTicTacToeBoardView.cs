using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Replaceable demo view of a <see cref="TicTacToeBoardService" /> board: one tile and one X/O mark per
    ///     <see cref="FieldGenerator" /> cell. It redraws single cells from
    ///     <see cref="FieldGenerator.OnCellStateChanged" /> (raised by the service for moves and resets) and
    ///     highlights <see cref="TicTacToeBoardService.WinningLine" /> after a win. Click a cell to play it.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemTicTacToeBoardView")]
    public class GridSystemTicTacToeBoardView : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private TicTacToeBoardService _board;
        [SerializeField] private Camera _camera;
        [SerializeField] private Vector2 _cellSize = new(0.9f, 0.9f);
        [SerializeField] private float _textScale = 5f;

        [Tooltip("Optional tile art (a 9-sliced sprite is drawn sliced). Empty = a plain square owned by this view.")]
        [SerializeField] private Sprite _tileSprite;

        [SerializeField] private Color _enabledColor = new(0.12f, 0.13f, 0.18f, 1f);
        [SerializeField] private Color _blockedColor = new(0.45f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _disabledColor = new(0.22f, 0.22f, 0.22f, 1f);

        [Tooltip("Tile color of the cells in TicTacToeBoardService.WinningLine once a player has won.")]
        [SerializeField] private Color _winColor = new(0.26f, 0.85f, 0.64f, 1f);

        [SerializeField] private Color _xColor = new(0.2f, 0.85f, 1f, 1f);
        [SerializeField] private Color _oColor = new(1f, 0.75f, 0.25f, 1f);

        private readonly Dictionary<Vector3Int, CellView> _views = new();
        private Sprite _cellSprite;

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
            }
        }

        private void Update()
        {
            if (_board == null || _generator == null || _camera == null)
            {
                return;
            }

            if (!MouseInputCompat.TryGetButtonDown(0, out bool pressed) || !pressed)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!MouseInputCompat.TryGetPosition(out Vector3 pointer))
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return;
            }

            CellViewMarker marker = hit.collider.GetComponent<CellViewMarker>();
            if (marker == null)
            {
                return;
            }

            // WHY: no RefreshAll here - the service raises FieldGenerator.OnCellStateChanged for the marked cell.
            _board.TryMakeMove(marker.Position);
        }

        private void OnDestroy()
        {
            if (_cellSprite != null)
            {
                if (Application.isPlaying) Destroy(_cellSprite);
                else DestroyImmediate(_cellSprite);
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
            tile.sprite = _tileSprite != null ? _tileSprite : CreateSolidSprite();
            ApplySize(tile, _cellSize);

            GameObject markGo = new("Mark");
            markGo.transform.SetParent(root.transform, false);
            markGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro mark = markGo.AddComponent<TextMeshPro>();
            mark.alignment = TextAlignmentOptions.Center;
            mark.fontSize = _textScale;
            mark.fontStyle = FontStyles.Bold;
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
            else if (IsOnWinningLine(cell.Position))
            {
                view.Tile.color = _winColor;
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

        private bool IsOnWinningLine(Vector3Int position)
        {
            if (_board == null || _board.Winner == TicTacToeCellState.Empty)
            {
                return false;
            }

            IReadOnlyList<Vector3Int> line = _board.WinningLine;
            for (int i = 0; i < line.Count; i++)
            {
                if (line[i] == position)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
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

        // WHY: a 9-sliced sprite keeps its rounded corners at any cell size; plain sprites are scaled to the cell.
        private static void ApplySize(SpriteRenderer renderer, Vector2 size)
        {
            Sprite sprite = renderer.sprite;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
                renderer.transform.localScale = Vector3.one;
                return;
            }

            Vector2 bounds = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.transform.localScale = new Vector3(
                size.x / Mathf.Max(bounds.x, 0.0001f),
                size.y / Mathf.Max(bounds.y, 0.0001f),
                1f);
        }

        private Sprite CreateSolidSprite()
        {
            if (_cellSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _cellSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }

            return _cellSprite;
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
