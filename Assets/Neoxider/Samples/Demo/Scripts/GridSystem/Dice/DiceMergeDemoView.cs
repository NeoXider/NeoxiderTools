using System.Collections.Generic;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/DiceMergeDemoView")]
    public sealed class DiceMergeDemoView : MonoBehaviour
    {
        private const int PlacedDiceSortingOrder = 200;
        private const int TrayDiceSortingOrder = 300;
        private const int DragDiceSortingOrder = 400;

        [SerializeField] private FieldGenerator _generator;
        [SerializeField] private DiceBoardService _diceBoard;
        [SerializeField] private DiceMergeDemoController _controller;
        [SerializeField] private Camera _camera;
        [SerializeField] private Sprite _cellSprite;
        [SerializeField] private GridCellMarker _cellPrefab;
        [SerializeField] private DiceDieView _diePrefab;
        [SerializeField] private Sprite[] _diceSprites = new Sprite[9];
        [SerializeField] private Vector2 _cellSize = new(0.68f, 0.68f);
        [SerializeField] private Vector3 _trayPosition = new(0f, -3.15f, 0f);
        [SerializeField] private Vector3 _dragWorldOffset = new(0f, 0.5f, 0f);
        [SerializeField] private bool _snapDragPreviewToGrid = true;

        [Tooltip("Empty-cell content id used only as a fallback when the DiceBoardService reference is missing. " +
                 "Keep this in sync with DiceBoardService.EmptyContentId (default -1).")]
        [SerializeField] private int _emptyContentId = -1;

        private readonly Dictionary<Vector3Int, CellView> _cells = new();
        private Sprite _fallbackSprite;
        private Transform _boardRoot;
        private Transform _piecesRoot;
        private Transform _trayRoot;
        private Transform _dragRoot;
        private bool _dragging;
        private bool _rebuilding;
        private Vector3 _dragStartWorld;
        private float _dragStartTime;

        public void Configure(
            FieldGenerator generator,
            DiceBoardService diceBoard,
            DiceMergeDemoController controller,
            Camera sceneCamera,
            Sprite cellSprite,
            GridCellMarker cellPrefab,
            DiceDieView diePrefab,
            Sprite[] diceSprites)
        {
            _generator = generator;
            _diceBoard = diceBoard;
            _controller = controller;
            _camera = sceneCamera;
            _cellSprite = cellSprite;
            _cellPrefab = cellPrefab;
            _diePrefab = diePrefab;
            _diceSprites = diceSprites;
            Subscribe();
            Rebuild();
        }

        private void Awake()
        {
            ResolveReferences();
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            EnsureCellViews();
        }

        private void OnDisable()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.RemoveListener(Rebuild);
                _generator.OnCellStateChanged.RemoveListener(RefreshCell);
            }

            if (_diceBoard != null)
            {
                _diceBoard.OnBoardChanged.RemoveListener(RefreshAll);
            }

            if (_controller != null)
            {
                _controller.OnDemoStateChanged.RemoveListener(RefreshTray);
            }
        }

        private void Update()
        {
            HandleInput();
        }

        public void Rebuild()
        {
            if (_rebuilding)
            {
                return;
            }

            _rebuilding = true;
            try
            {
                ResolveReferences();
                ClearChildren();
                _cells.Clear();

                // Drop references to the just-destroyed roots. In play mode Destroy is deferred, so the old objects
                // still live for the rest of the frame; by holding only the fresh instances (never re-finding by name)
                // we cannot accidentally parent placed dice under a root that is about to be destroyed.
                _boardRoot = null;
                _piecesRoot = null;
                _trayRoot = null;
                _dragRoot = null;
                EnsureRoots();

                if (_generator == null)
                {
                    return;
                }

                CreateBoardBackdrop();
                foreach (FieldCell cell in _generator.GetAllCells(true))
                {
                    CreateCell(cell);
                }
            }
            finally
            {
                _rebuilding = false;
            }

            if (_generator == null)
            {
                return;
            }

            RefreshAll();
            RefreshTray();
        }

        private void CreateBoardBackdrop()
        {
            Vector3Int size = _generator.Config != null ? _generator.Config.Size : new Vector3Int(5, 5, 1);
            GameObject backdrop = new("DiceBoardBackdrop");
            backdrop.transform.SetParent(_boardRoot, false);
            Vector3 first = _generator.GetCellWorldCenter(Vector3Int.zero);
            Vector3 last = _generator.GetCellWorldCenter(new Vector3Int(size.x - 1, size.y - 1, 0));
            Vector3 center = (first + last) * 0.5f;
            backdrop.transform.localPosition = _boardRoot.InverseTransformPoint(center) + new Vector3(0f, 0f, 0.08f);
            backdrop.transform.localScale = new Vector3(size.x * 0.82f + 0.28f, size.y * 0.82f + 0.28f, 1f);

            SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite();
            renderer.color = new Color(0.04f, 0.04f, 0.04f, 1f);
            renderer.sortingOrder = -1;
        }

        public void RefreshAll()
        {
            ResolveReferences();
            if (_generator == null)
            {
                return;
            }

            EnsureCellViews();
            EnsurePiecesRoot();
            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                RefreshCell(cell);
            }
        }

        private void EnsureCellViews()
        {
            if (_generator == null || _cells.Count > 0 || _rebuilding)
            {
                return;
            }

            // Only rebuild once the generator actually has cells, otherwise Rebuild would loop on an empty board.
            if (_generator.Cells == null || _generator.Cells.Length == 0)
            {
                return;
            }

            Rebuild();
        }

        public void RefreshTray()
        {
            EnsureTrayRoot();
            ClearTransform(_trayRoot);
            if (_controller == null || _controller.CurrentPiece == null || _trayRoot == null)
            {
                return;
            }

            CreatePieceView(_controller.CurrentPiece, _trayRoot, _trayPosition, TrayDiceSortingOrder);
        }

#if UNITY_EDITOR
        public bool SimulateDragDropForTest(Vector3 releaseWorld)
        {
            DestroyDragPreview();
            _dragRoot = new GameObject("DiceDragPreview").transform;
            _dragRoot.SetParent(transform, false);
            _dragRoot.position = releaseWorld;
            CreatePieceView(_controller != null ? _controller.CurrentPiece : null, _dragRoot, Vector3.zero, DragDiceSortingOrder);
            SetTrayVisible(false);

            bool placed = TryPlaceCurrentPieceAtWorld(releaseWorld);
            DestroyDragPreview();
            SetTrayVisible(true);
            if (placed)
            {
                RefreshAll();
            }

            return placed;
        }
#endif

        private void Subscribe()
        {
            if (_generator != null)
            {
                _generator.OnFieldGenerated.RemoveListener(Rebuild);
                _generator.OnCellStateChanged.RemoveListener(RefreshCell);
                _generator.OnFieldGenerated.AddListener(Rebuild);
                _generator.OnCellStateChanged.AddListener(RefreshCell);
            }

            if (_diceBoard != null)
            {
                _diceBoard.OnBoardChanged.RemoveListener(RefreshAll);
                _diceBoard.OnBoardChanged.AddListener(RefreshAll);
            }

            if (_controller != null)
            {
                _controller.OnDemoStateChanged.RemoveListener(RefreshTray);
                _controller.OnDemoStateChanged.AddListener(RefreshTray);
            }
        }

        private void CreateCell(FieldCell cell)
        {
            Vector3 center = _generator.GetCellWorldCenter(cell.Position);
            GameObject root;
            SpriteRenderer cellRenderer;
            GridCellMarker marker;

            if (_cellPrefab != null)
            {
                marker = Instantiate(_cellPrefab, _boardRoot);
                root = marker.gameObject;
                root.name = $"DiceCell_{cell.Position.x}_{cell.Position.y}_{cell.Position.z}";
                root.transform.position = center;
                cellRenderer = root.GetComponent<SpriteRenderer>();
            }
            else
            {
                root = new GameObject($"DiceCell_{cell.Position.x}_{cell.Position.y}_{cell.Position.z}");
                root.transform.SetParent(_boardRoot, false);
                root.transform.position = center;

                cellRenderer = root.AddComponent<SpriteRenderer>();
                cellRenderer.sprite = _cellSprite != null ? _cellSprite : CreateSolidSprite();
                cellRenderer.sortingOrder = 0;
                root.transform.localScale = new Vector3(_cellSize.x, _cellSize.y, 1f);

                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                marker = root.AddComponent<GridCellMarker>();
            }

            if (cellRenderer == null)
            {
                cellRenderer = root.AddComponent<SpriteRenderer>();
                cellRenderer.sprite = _cellSprite != null ? _cellSprite : CreateSolidSprite();
                cellRenderer.sortingOrder = 0;
            }

            cellRenderer.color = new Color(0.72f, 0.72f, 0.68f, 1f);
            marker.Bind(_generator, cell.Position);

            _cells[cell.Position] = new CellView
            {
                Root = root.transform,
                CellRenderer = cellRenderer
            };
        }

        private void RefreshCell(FieldCell cell)
        {
            if (cell == null || !_cells.TryGetValue(cell.Position, out CellView view))
            {
                return;
            }

            view.CellRenderer.color = cell.IsEnabled
                ? new Color(0.72f, 0.72f, 0.68f, 1f)
                : new Color(0.35f, 0.35f, 0.35f, 0.45f);
            if (!cell.IsEnabled || IsEmptyCellContent(cell.ContentId))
            {
                if (view.DiceRoot != null)
                {
                    DestroyImmediateSafe(view.DiceRoot.gameObject);
                    view.DiceRoot = null;
                }

                return;
            }

            EnsurePiecesRoot();
            Vector3 localPosition = _piecesRoot.InverseTransformPoint(_generator.GetCellWorldCenter(cell.Position));
            if (view.DiceRoot != null)
            {
                DiceDieView existing = view.DiceRoot.GetComponent<DiceDieView>();
                if (existing != null)
                {
                    view.DiceRoot.SetParent(_piecesRoot, false);
                    view.DiceRoot.localPosition = localPosition;
                    view.DiceRoot.name = "Dice_" + cell.ContentId;
                    ApplyDieWorldScale(view.DiceRoot);
                    existing.Initialize(cell.ContentId, ResolveDieSprite(cell.ContentId), PlacedDiceSortingOrder);
                    return;
                }

                DestroyImmediateSafe(view.DiceRoot.gameObject);
                view.DiceRoot = null;
            }

            GameObject die = CreateDieView(cell.ContentId, _piecesRoot, localPosition, PlacedDiceSortingOrder);
            view.DiceRoot = die != null ? die.transform : null;
        }

        private void HandleInput()
        {
            ResolveReferences();
            if (_camera == null || _controller == null || _controller.GameOver)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Vector3 world = ScreenToWorld(Input.mousePosition);
                if (Vector2.Distance(world, _trayPosition) <= 1.25f)
                {
                    StartDrag(world);
                }
            }

            if (_dragging && Input.GetMouseButton(0) && _dragRoot != null)
            {
                _dragRoot.position = ResolveDragPreviewPosition();
            }

            if (_dragging && Input.GetMouseButtonUp(0))
            {
                FinishDrag();
            }
        }

        private void StartDrag(Vector3 world)
        {
            _dragging = true;
            _dragStartWorld = world;
            _dragStartTime = Time.time;
            DestroyDragPreview();
            _dragRoot = new GameObject("DiceDragPreview").transform;
            _dragRoot.SetParent(transform, false);
            _dragRoot.position = ResolveDragPreviewPosition();
            CreatePieceView(_controller.CurrentPiece, _dragRoot, Vector3.zero, DragDiceSortingOrder);
            SetTrayVisible(false);
        }

        private void FinishDrag()
        {
            _dragging = false;
            float dragDistance = Vector2.Distance(_dragStartWorld, ScreenToWorld(Input.mousePosition));
            bool tap = dragDistance < 0.18f && Time.time - _dragStartTime < 0.35f;

            if (tap)
            {
                DestroyDragPreview();
                SetTrayVisible(true);
                _controller.RotateCurrentPiece();
                return;
            }

            Vector3 releaseWorld = ResolveDragPreviewPosition();
            if (_dragRoot != null)
            {
                _dragRoot.position = releaseWorld;
            }

            bool placed = TryPlaceCurrentPieceAtWorld(releaseWorld);

            SetTrayVisible(true);

            // On success the preview dice were promoted into the board (reused as the placed visuals); only a failed
            // drop leaves a stray preview to discard.
            if (!placed)
            {
                DestroyDragPreview();
            }
        }

        public bool TryPlaceCurrentPieceAtWorld(Vector3 releaseWorld)
        {
            ResolveReferences();
            if (_controller == null || _diceBoard == null ||
                !TryResolveDragAnchor(releaseWorld, out Vector3Int anchor))
            {
                return false;
            }

            DicePiece piece = _controller.CurrentPiece;
            if (piece != null && _diceBoard.CanPlace(piece, anchor))
            {
                // Reuse the very objects the player dragged as the placed dice. We register them on their target cells
                // BEFORE mutating the model, so the model's OnCellStateChanged -> RefreshCell reuses (and merges) these
                // exact dice instead of destroying the preview and recreating fresh visuals.
                PromotePreviewToCells(anchor, piece);
                DestroyDragPreview();
            }

            bool placed = _controller.TryPlaceCurrentPiece(anchor);
            if (placed)
            {
                RefreshAll();
            }

            return placed;
        }

        // Hands the dragged preview dice over to their destination cells so they become the persistent placed visuals.
        private void PromotePreviewToCells(Vector3Int anchor, DicePiece piece)
        {
            if (_dragRoot == null || piece == null || _generator == null)
            {
                return;
            }

            EnsurePiecesRoot();

            var previewDice = new List<Transform>(_dragRoot.childCount);
            foreach (Transform child in _dragRoot)
            {
                previewDice.Add(child);
            }

            IReadOnlyList<DicePieceCell> cells = piece.Cells;
            int count = Mathf.Min(cells.Count, previewDice.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3Int position = anchor + cells[i].Offset;
                if (!_cells.TryGetValue(position, out CellView view))
                {
                    continue;
                }

                Transform die = previewDice[i];
                if (view.DiceRoot != null && view.DiceRoot != die)
                {
                    DestroyImmediateSafe(view.DiceRoot.gameObject);
                }

                die.SetParent(_piecesRoot, false);
                die.localPosition = _piecesRoot.InverseTransformPoint(_generator.GetCellWorldCenter(position));
                ApplyDieWorldScale(die);
                view.DiceRoot = die;
            }
        }

        private void ResolveReferences()
        {
            // Resolve siblings/parents only. A global FindObjectOfType would bind to an arbitrary board in scenes that
            // host more than one dice demo, so we deliberately stay local to this view's hierarchy.
            if (_generator == null)
            {
                _generator = GetComponentInParent<FieldGenerator>();
            }

            if (_diceBoard == null)
            {
                _diceBoard = GetComponentInParent<DiceBoardService>();
            }

            if (_controller == null)
            {
                _controller = GetComponentInParent<DiceMergeDemoController>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private bool IsEmptyCellContent(int contentId)
        {
            // Keep the empty threshold consistent with the board service; fall back to the serialized default when the
            // board reference is temporarily unavailable so placed dice never flicker as "empty".
            int emptyContentId = _diceBoard != null ? _diceBoard.EmptyContentId : _emptyContentId;
            return contentId <= emptyContentId;
        }

        private void CreatePieceView(DicePiece piece, Transform parent, Vector3 basePosition, int sortingOrder)
        {
            if (piece == null || parent == null || piece.Cells == null || piece.Cells.Count == 0)
            {
                return;
            }

            foreach (DicePieceCell cell in piece.Cells)
            {
                Vector3 local = new(cell.Offset.x * 0.82f, cell.Offset.y * 0.82f, 0f);
                CreateDieView(cell.Value, parent, basePosition + local, sortingOrder);
            }
        }

        private GameObject CreateDieView(int value, Transform parent, Vector3 localPosition, int sortingOrder)
        {
            if (_diePrefab == null)
            {
                return null;
            }

            DiceDieView dieView = Instantiate(_diePrefab, parent);
            GameObject root = dieView.gameObject;
            root.name = "Dice_" + value;
            root.transform.localPosition = localPosition;
            ApplyDieWorldScale(root.transform);

            dieView.Initialize(value, ResolveDieSprite(value), sortingOrder);

            return root;
        }

        private Vector3 ResolveDragPreviewPosition()
        {
            Vector3 targetWorld = ScreenToWorld(Input.mousePosition) + _dragWorldOffset;
            if (_snapDragPreviewToGrid && _generator != null)
            {
                return _generator.SnapWorldToCellCenter(targetWorld);
            }

            return targetWorld;
        }

        private bool TryResolveDragAnchor(Vector3 targetWorld, out Vector3Int anchor)
        {
            anchor = default;
            return _generator != null && _generator.TryGetCellPositionFromWorld(targetWorld, out anchor);
        }

        private void ApplyDieWorldScale(Transform die)
        {
            Vector3 desiredWorldScale = _diePrefab.transform.localScale;
            Transform parent = die.parent;
            if (parent == null)
            {
                die.localScale = desiredWorldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            die.localScale = new Vector3(
                DivideScale(desiredWorldScale.x, parentScale.x),
                DivideScale(desiredWorldScale.y, parentScale.y),
                DivideScale(desiredWorldScale.z, parentScale.z));
        }

        private void SetTrayVisible(bool visible)
        {
            if (_trayRoot != null)
            {
                _trayRoot.gameObject.SetActive(visible);
            }
        }

        // Cached-root management. We never recover roots via transform.Find: a saved scene can persist these objects
        // and, in play mode, Destroy is deferred, so Find could return a stale/duplicate root and orphan placed dice.
        private void EnsureRoots()
        {
            _boardRoot = CreateRootIfMissing(_boardRoot, "DiceBoardView");
            _piecesRoot = CreateRootIfMissing(_piecesRoot, "DicePlacedPiecesView");
            _trayRoot = CreateRootIfMissing(_trayRoot, "DiceTrayView");
        }

        private Transform CreateRootIfMissing(Transform current, string rootName)
        {
            if (current != null)
            {
                return current;
            }

            Transform root = new GameObject(rootName).transform;
            root.SetParent(transform, false);
            return root;
        }

        private void EnsurePiecesRoot()
        {
            _piecesRoot = CreateRootIfMissing(_piecesRoot, "DicePlacedPiecesView");
        }

        private void EnsureTrayRoot()
        {
            _trayRoot = CreateRootIfMissing(_trayRoot, "DiceTrayView");
        }

        private void DestroyDragPreview()
        {
            if (_dragRoot == null)
            {
                return;
            }

            if (_dragRoot == _boardRoot || _dragRoot == _piecesRoot || _dragRoot == _trayRoot)
            {
                _dragRoot = null;
                return;
            }

            DestroyImmediateSafe(_dragRoot.gameObject);
            _dragRoot = null;
        }

        private Sprite ResolveDieSprite(int value)
        {
            if (value >= 1 && value <= 9 && _diceSprites != null && value - 1 < _diceSprites.Length &&
                _diceSprites[value - 1] != null)
            {
                return _diceSprites[value - 1];
            }

            if (_diceSprites != null && _diceSprites.Length > 8 && _diceSprites[8] != null)
            {
                return _diceSprites[8];
            }

            return CreateSolidSprite();
        }

        private Vector3 ScreenToWorld(Vector3 screenPosition)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            world.z = 0f;
            return world;
        }

        private void ClearChildren()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                DestroyImmediateSafe(child);
            }
        }

        private static void ClearTransform(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var children = new List<GameObject>();
            foreach (Transform child in target)
            {
                children.Add(child.gameObject);
            }

            foreach (GameObject child in children)
            {
                DestroyImmediateSafe(child);
            }
        }

        private static void DestroyImmediateSafe(GameObject obj)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private static float DivideScale(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        private Sprite CreateSolidSprite()
        {
            if (_fallbackSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                _fallbackSprite = Sprite.Create(
                    tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }

            return _fallbackSprite;
        }

        private sealed class CellView
        {
            public Transform Root;
            public SpriteRenderer CellRenderer;
            public Transform DiceRoot;
        }

    }
}
