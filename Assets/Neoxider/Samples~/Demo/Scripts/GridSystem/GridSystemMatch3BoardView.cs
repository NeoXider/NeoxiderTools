using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neo/Demo/GridSystem/GridSystemMatch3BoardView")]
    public class GridSystemMatch3BoardView : MonoBehaviour
    {
        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private Match3BoardService _match3;
        [SerializeField] private Camera _camera;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Vector2 _cellSize = new(0.9f, 0.9f);
        [SerializeField] private Color _blockedColor = new(0.35f, 0.18f, 0.18f, 1f);
        [SerializeField] private Color _selectedOutlineColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private float _outlineThickness = 0.06f;

        private readonly Dictionary<Vector3Int, CellView> _views = new();
        private Vector3Int? _selected;

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
                _generator.OnCellStateChanged.AddListener(HandleCellStateChanged);
            }

            if (_match3 != null)
            {
                _match3.OnBoardChanged.AddListener(RefreshAll);
                _match3.OnMatchesResolved.AddListener(HandleMatchesResolved);
            }

            Rebuild();
        }

        private void OnDisable()
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
            if (_boardUnavailable() || !Input.GetMouseButtonDown(0))
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

            GameObject tileGo = new("Tile");
            tileGo.transform.SetParent(root.transform, false);
            SpriteRenderer tile = tileGo.AddComponent<SpriteRenderer>();
            tile.sprite = CreateSolidSprite();
            tile.transform.localScale = new Vector3(_cellSize.x, _cellSize.y, 1f);

            GameObject outlineGo = new("Outline");
            outlineGo.transform.SetParent(root.transform, false);
            SpriteRenderer outline = outlineGo.AddComponent<SpriteRenderer>();
            outline.sprite = tile.sprite;
            outline.transform.localScale =
                new Vector3(_cellSize.x + _outlineThickness, _cellSize.y + _outlineThickness, 1f);
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

            if (!cell.IsEnabled || !cell.IsWalkable || cell.IsOccupied)
            {
                view.Tile.color = _blockedColor;
                return;
            }

            view.Tile.color = GetTileColor(cell.ContentId);
        }

        private void RefreshSelectionVisual()
        {
            foreach (KeyValuePair<Vector3Int, CellView> pair in _views)
            {
                bool selected = _selected.HasValue && pair.Key == _selected.Value;
                pair.Value.Outline.color = selected ? _selectedOutlineColor : new Color(0, 0, 0, 0);
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

        private static Sprite CreateSolidSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
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