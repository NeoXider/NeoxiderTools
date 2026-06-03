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

        private readonly Dictionary<Vector3Int, CellView> _cells = new();
        private Transform _boardRoot;
        private Transform _trayRoot;
        private Transform _dragRoot;
        private bool _dragging;
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
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RegisterExistingCells();
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
            ClearChildren();
            _cells.Clear();
            _boardRoot = new GameObject("DiceBoardView").transform;
            _boardRoot.SetParent(transform, false);
            _trayRoot = new GameObject("DiceTrayView").transform;
            _trayRoot.SetParent(transform, false);

            if (_generator == null)
            {
                return;
            }

            CreateBoardBackdrop();
            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                CreateCell(cell);
            }

            RefreshAll();
            RefreshTray();
        }

        private void RegisterExistingCells()
        {
            if (_generator == null || _cells.Count > 0)
            {
                return;
            }

            _boardRoot = transform.Find("DiceBoardView");
            _trayRoot = transform.Find("DiceTrayView");

            if (_boardRoot == null)
            {
                Rebuild();
                return;
            }

            foreach (GridCellMarker marker in _boardRoot.GetComponentsInChildren<GridCellMarker>(true))
            {
                FieldCell cell = _generator.GetCell(marker.Position);
                if (cell == null)
                {
                    continue;
                }

                SpriteRenderer renderer = marker.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = marker.gameObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = _cellSprite != null ? _cellSprite : CreateSolidSprite();
                }

                _cells[marker.Position] = new CellView
                {
                    Root = marker.transform,
                    CellRenderer = renderer
                };
            }

            if (_trayRoot == null)
            {
                _trayRoot = new GameObject("DiceTrayView").transform;
                _trayRoot.SetParent(transform, false);
            }
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
            if (_generator == null)
            {
                return;
            }

            foreach (FieldCell cell in _generator.GetAllCells(true))
            {
                RefreshCell(cell);
            }
        }

        public void RefreshTray()
        {
            ClearTransform(_trayRoot);
            if (_controller == null || _controller.CurrentPiece == null || _trayRoot == null)
            {
                return;
            }

            CreatePieceView(_controller.CurrentPiece, _trayRoot, _trayPosition, 20);
        }

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

            if (view.DiceRoot != null)
            {
                DestroyImmediateSafe(view.DiceRoot.gameObject);
                view.DiceRoot = null;
            }

            view.CellRenderer.color = cell.IsEnabled
                ? new Color(0.72f, 0.72f, 0.68f, 1f)
                : new Color(0.35f, 0.35f, 0.35f, 0.45f);
            if (!cell.IsEnabled || cell.ContentId <= _diceBoard.EmptyContentId)
            {
                return;
            }

            GameObject die = CreateDieView(cell.ContentId, view.Root, Vector3.zero, 10);
            view.DiceRoot = die != null ? die.transform : null;
        }

        private void HandleInput()
        {
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
            _dragRoot = new GameObject("DiceDragPreview").transform;
            _dragRoot.SetParent(transform, false);
            _dragRoot.position = ResolveDragPreviewPosition();
            CreatePieceView(_controller.CurrentPiece, _dragRoot, Vector3.zero, 50);
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

            bool placed = false;
            if (TryResolveDragAnchor(releaseWorld, out Vector3Int anchor))
            {
                placed = _controller.TryPlaceCurrentPiece(anchor);
            }

            if (placed)
            {
                RefreshAll();
            }

            DestroyDragPreview();
            SetTrayVisible(true);
        }

        private void CreatePieceView(DicePiece piece, Transform parent, Vector3 basePosition, int sortingOrder)
        {
            if (piece == null)
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

        private void DestroyDragPreview()
        {
            if (_dragRoot == null)
            {
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

        private static Sprite CreateSolidSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        private sealed class CellView
        {
            public Transform Root;
            public SpriteRenderer CellRenderer;
            public Transform DiceRoot;
        }

    }
}
