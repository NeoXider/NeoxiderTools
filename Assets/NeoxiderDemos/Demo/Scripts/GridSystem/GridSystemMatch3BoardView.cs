using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Demo.GridSystem
{
    /// <summary>
    ///     Replaceable demo view of a <see cref="Match3BoardService" /> board: one tile per
    ///     <see cref="FieldGenerator" /> cell, colored by tile id and cell state, rebuilt when the field is
    ///     regenerated. Click two neighbouring tiles to swap them. Cell objects are created at runtime from the
    ///     live field; the scene only holds this component and its references.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemMatch3BoardView")]
    public class GridSystemMatch3BoardView : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Match3BoardService _match3;
        [SerializeField] private Camera _camera;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Vector2 _cellSize = new(0.9f, 0.9f);

        [Tooltip("Optional tile art (a 9-sliced sprite is drawn sliced). Empty = a plain square owned by this view.")]
        [SerializeField] private Sprite _tileSprite;

        [Tooltip("Tile color of a cell that is not walkable (FieldGenerator.SetWalkable false).")]
        [SerializeField] private Color _blockedColor = new(0.35f, 0.18f, 0.18f, 1f);

        [Tooltip("Tile color of a disabled cell (outside the shape or FieldGenerator.SetEnabled false).")]
        [SerializeField] private Color _disabledColor = new(1f, 1f, 1f, 0.05f);

        [Tooltip("Tile color of an occupied cell (FieldGenerator.SetOccupied true).")]
        [SerializeField] private Color _occupiedColor = new(0.42f, 0.36f, 0.22f, 1f);

        [SerializeField] private Color _selectedOutlineColor = new(1f, 1f, 1f, 1f);

        [Tooltip("Outline of highlighted cells (path or reachable area set through SetHighlightedCells).")]
        [SerializeField] private Color _highlightColor = new(0.25f, 0.85f, 1f, 1f);

        [SerializeField] private float _outlineThickness = 0.06f;

        private readonly Dictionary<Vector3Int, CellView> _views = new();
        private readonly HashSet<Vector3Int> _highlighted = new();
        private Vector3Int? _selected;
        private Sprite _cellSprite;

        /// <summary>Number of cells currently highlighted by <see cref="SetHighlightedCells" />.</summary>
        public int HighlightedCount => _highlighted.Count;

        public void Configure(
            FieldGenerator generator,
            Match3BoardService match3,
            Camera sceneCamera,
            TMP_Text statusText)
        {
            Unsubscribe();
            _generator = generator;
            _match3 = match3;
            _camera = sceneCamera;
            _statusText = statusText;
            if (isActiveAndEnabled)
            {
                Subscribe();
                Rebuild();
            }
        }

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Rebuild();
        }

        private void Subscribe()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.AddListener(Rebuild);
                _generator.OnCellStateChanged.AddListener(HandleCellStateChanged);
            }

            if (_match3 != null)
            {
                _match3.OnBoardChanged.AddListener(RefreshAll);
                _match3.OnMatchesResolved.AddListener(HandleMatchesResolved);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (_cellSprite != null)
            {
                if (Application.isPlaying) Destroy(_cellSprite);
                else DestroyImmediate(_cellSprite);
            }
        }

        private void Unsubscribe()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.RemoveListener(Rebuild);
                _generator.OnCellStateChanged.RemoveListener(HandleCellStateChanged);
            }

            if (_match3 != null)
            {
                _match3.OnBoardChanged.RemoveListener(RefreshAll);
                _match3.OnMatchesResolved.RemoveListener(HandleMatchesResolved);
            }
        }

        private void Update()
        {
            if (_boardUnavailable() || !MouseInputCompat.TryGetButtonDown(0, out bool pressed) || !pressed)
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

            Match3CellMarker marker = hit.collider.GetComponent<Match3CellMarker>();
            if (marker == null)
            {
                return;
            }

            HandleClick(marker.Position);
        }

        public void Rebuild()
        {
            ClearAll();
            _highlighted.Clear();
            if (_generator == null)
            {
                return;
            }

            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                CreateCellView(cell);
            }

            _selected = null;
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
                RefreshCell(cell);
            }

            RefreshSelectionVisual();
        }

        /// <summary>
        ///     Outlines the given cells with the highlight color (for example a path from
        ///     <see cref="GridPathfinder.TryFindPath" />); replaces the previous highlight.
        /// </summary>
        public void SetHighlightedCells(IEnumerable<Vector3Int> cells)
        {
            _highlighted.Clear();
            if (cells != null)
            {
                foreach (Vector3Int cell in cells)
                {
                    _highlighted.Add(cell);
                }
            }

            RefreshSelectionVisual();
        }

        /// <summary>Removes every highlight outline.</summary>
        public void ClearHighlight()
        {
            if (_highlighted.Count == 0)
            {
                return;
            }

            _highlighted.Clear();
            RefreshSelectionVisual();
        }

        private void HandleMatchesResolved(int cleared)
        {
            SetStatus(cleared > 0 ? $"Cleared: {cleared}" : "No matches");
        }

        private void HandleCellStateChanged(FieldCell cell)
        {
            RefreshCell(cell);
            RefreshSelectionVisual();
        }

        private void HandleClick(Vector3Int pos)
        {
            FieldCell clicked = _generator.GetCell(pos);
            if (!CanInteract(clicked))
            {
                _selected = null;
                RefreshSelectionVisual();
                return;
            }

            if (_selected == null)
            {
                _selected = pos;
                RefreshSelectionVisual();
                SetStatus($"Selected: {pos.x},{pos.y}");
                return;
            }

            Vector3Int from = _selected.Value;
            if (from == pos)
            {
                _selected = null;
                RefreshSelectionVisual();
                SetStatus("Selection cleared");
                return;
            }

            if (!AreAdjacent(from, pos))
            {
                _selected = pos;
                RefreshSelectionVisual();
                SetStatus($"Selected: {pos.x},{pos.y}");
                return;
            }

            bool resolved = _match3.TrySwapAndResolve(from, pos);
            _selected = null;
            RefreshSelectionVisual();
            SetStatus(resolved ? "Swap resolved" : "Swap reverted");
        }

        private void CreateCellView(FieldCell cell)
        {
            Vector3 center = _generator.GetCellWorldCenter(cell.Position);
            GameObject root = new($"Match3Cell_{cell.Position.x}_{cell.Position.y}_{cell.Position.z}");
            root.transform.SetParent(transform, false);
            root.transform.position = center;

            Sprite sprite = ResolveTileSprite();
            GameObject tileGo = new("Tile");
            tileGo.transform.SetParent(root.transform, false);
            SpriteRenderer tile = tileGo.AddComponent<SpriteRenderer>();
            tile.sprite = sprite;
            ApplySize(tile, _cellSize);

            GameObject outlineGo = new("Outline");
            outlineGo.transform.SetParent(root.transform, false);
            SpriteRenderer outline = outlineGo.AddComponent<SpriteRenderer>();
            outline.sprite = sprite;
            ApplySize(outline, new Vector2(_cellSize.x + _outlineThickness, _cellSize.y + _outlineThickness));
            outline.color = new Color(0, 0, 0, 0);
            outline.sortingOrder = tile.sortingOrder - 1;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(_cellSize.x, _cellSize.y, 0.2f);
            root.AddComponent<Match3CellMarker>().Position = cell.Position;

            _views[cell.Position] = new CellView
            {
                Tile = tile,
                Outline = outline
            };
        }

        private void RefreshCell(FieldCell cell)
        {
            if (cell == null || !_views.TryGetValue(cell.Position, out CellView view))
            {
                return;
            }

            if (!cell.IsEnabled)
            {
                view.Tile.color = _disabledColor;
                return;
            }

            if (!cell.IsWalkable)
            {
                view.Tile.color = _blockedColor;
                return;
            }

            if (cell.IsOccupied)
            {
                view.Tile.color = _occupiedColor;
                return;
            }

            view.Tile.color = GetTileColor(cell.ContentId);
        }

        private void RefreshSelectionVisual()
        {
            Color hidden = new(0, 0, 0, 0);
            foreach (KeyValuePair<Vector3Int, CellView> pair in _views)
            {
                bool selected = _selected.HasValue && pair.Key == _selected.Value;
                pair.Value.Outline.color = selected
                    ? _selectedOutlineColor
                    : _highlighted.Contains(pair.Key)
                        ? _highlightColor
                        : hidden;
            }
        }

        private void ClearAll()
        {
            List<GameObject> children = new();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _views.Clear();
        }

        private bool _boardUnavailable()
        {
            return _generator == null || _match3 == null || _camera == null;
        }

        private bool CanInteract(FieldCell cell)
        {
            return cell != null && cell.IsEnabled && cell.IsWalkable && !cell.IsOccupied;
        }

        private static bool AreAdjacent(Vector3Int a, Vector3Int b)
        {
            Vector3Int delta = a - b;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) + Mathf.Abs(delta.z) == 1;
        }

        private Sprite ResolveTileSprite()
        {
            return _tileSprite != null ? _tileSprite : CreateSolidSprite();
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

        private Color GetTileColor(int tileId)
        {
            switch ((Match3TileState)tileId)
            {
                case Match3TileState.Red:
                    return new Color(0.95f, 0.3f, 0.3f, 1f);
                case Match3TileState.Green:
                    return new Color(0.3f, 0.9f, 0.4f, 1f);
                case Match3TileState.Blue:
                    return new Color(0.35f, 0.55f, 1f, 1f);
                case Match3TileState.Yellow:
                    return new Color(1f, 0.86f, 0.3f, 1f);
                case Match3TileState.Purple:
                    return new Color(0.7f, 0.4f, 0.95f, 1f);
                default:
                    return new Color(0.2f, 0.2f, 0.2f, 1f);
            }
        }

        private void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text;
            }
        }

        private sealed class CellView
        {
            public SpriteRenderer Tile;
            public SpriteRenderer Outline;
        }

        public sealed class Match3CellMarker : MonoBehaviour
        {
            public Vector3Int Position;
        }
    }
}
