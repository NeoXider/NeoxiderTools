using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Bonus
{
    /// <summary>
    ///     Spin orchestrator:
    ///     - Arranges rows and their parameters (spaceY, offsetY, speedControll)
    ///     - Starts row spins and waits until all rows stop
    ///     - Reads visible symbols in each column window into finalVisuals (y=0 bottom)
    ///     - Evaluates win/lose and raises events
    ///     - Exposes payline queries: window-row matrix, symbol IDs per line, <see cref="SlotElement"/> chains,
    ///       and last-spin winning indices / element lists for animations
    ///     - Optional win-line playback via assigned LineRenderer(s) after a winning spin
    ///     No spin animation here — rotation/braking lives in Row.
    /// </summary>
    [NeoDoc("Bonus/Slot/SpinController.md")]
    [CreateFromMenu("Neoxider/Bonus/SpinController")]
    [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(SpinController))]
    public class SpinController : MonoBehaviour
    {
        [SerializeField] public CheckSpin checkSpin = new();
        [SerializeField] public BetsData betsData; // may be null
        [SerializeField] public SpritesData allSpritesData; // may be null

        [Space] [Header("Settings")] [SerializeField] [RequireInterface(typeof(IMoneySpend))]
        private GameObject _moneyGameObject;

        [SerializeField] private bool _priceOnLine = true;

        [Tooltip("Visible window height in symbol rows (maps to Row.countSlotElement).")]
        [SerializeField]
        private int _countVerticalElements = 3;

        [SerializeField] private Row[] _rows;

        [SerializeField] private bool _isSingleSpeed = true;

        [FormerlySerializedAs("chanseWin")] [Range(0f, 1f)] [SerializeField]
        private float chanceWin = 0.5f;

        /// <summary>Probability [0–1] that the spin plan favors a win when CheckSpin is active.</summary>
        public float ChanceWin
        {
            get => chanceWin;
            set => chanceWin = Mathf.Clamp01(value);
        }

        [Obsolete("Use " + nameof(ChanceWin) + " (field renamed from chanseWin).")]
        public float chanseWin
        {
            get => chanceWin;
            set => chanceWin = Mathf.Clamp01(value);
        }

        [Space] [Header("Visual")] [SerializeField]
        private float _delaySpinRoll = 0.2f;

        [SerializeField] private SpeedControll _speedControll = new()
        {
            speed = 5000,
            timeSpin = 1
        };

        [SerializeField] private bool _setSpace;
        [SerializeField] private Vector2 _space = Vector2.one;
        [SerializeField] private float offsetY;
        [SerializeField] private VisualSlotLines _lineSlot = new();

        [Space] [Header("Win line (optional LineRenderer)")] [SerializeField]
        private WinLineRendererPlayback _winLinePlayback = new();

        [Space] [Header("Text")] [SerializeField]
        private TMP_Text _textCountLine;

        [Space] public UnityEvent OnStartSpin;

        public UnityEvent OnEndSpin;

        /// <summary>Invoked with true when the spin is a win.</summary>
        public UnityEvent<bool> OnEnd;

        [Space] public UnityEvent<int> OnWin;
        public UnityEvent<int[]> OnWinLines;
        public UnityEvent OnLose;

        [Space] public UnityEvent<string> OnChangeBet;
        public UnityEvent<string> OnChangeMoneyWin;

        [Space] [Header("Debug")] [SerializeField]
        private bool _firstWin;

        [Tooltip("How many paylines are active for betting and evaluation (first N definitions from Lines Data or fallback).")]
        [SerializeField]
        [Min(1)]
        private int _countLine = 1;

        [SerializeField] [Min(0)] private int _betsId;
        [SerializeField] private bool _logFinalVisuals;

        [Tooltip("From which index to print coordinates in Debug: 0 (default) or 1, etc.")] [SerializeField]
        private int _gridIndexBase = 1;

        [Tooltip("Общий выключатель отладочных гизмо слота в Scene (линии выплат и подписи ячеек).")]
        [SerializeField]
        private bool _slotSceneDebugEnabled = true;

        [Tooltip("Гизмо линий выплат при выделении этого объекта (если общий выключатель включён).")]
        [SerializeField]
        private bool _drawPaylineGizmos = true;

        [Tooltip("Подписи и маркеры SlotElement ([col,row]) для барабанов этого контроллера.")]
        [SerializeField]
        private bool _drawSlotElementGizmos = true;

        [SerializeField] private Color _gizmoPaylineColor = new(0.2f, 1f, 0.4f, 0.85f);

        [Tooltip("Payline polyline width in Scene view (anti-aliased).")] [SerializeField] [Min(1f)]
        private float _gizmoPaylineWidth = 8f;

        [Tooltip("Joint disc radius as a fraction of HandleUtility.GetHandleSize at each cell.")]
        [SerializeField]
        [Min(0.02f)]
        private float _gizmoJointRadiusMultiplier = 0.16f;

        [Tooltip("Payline banner: vertical lift × HandleUtility.GetHandleSize(SpinController transform).")]
        [SerializeField]
        [Min(0.05f)]
        private float _gizmoBannerRaiseHandles = 2.45f;

        [SerializeField]
        [Min(8)]
        private int _gizmoBannerFontSize = 18;

        [SerializeField]
        private Color _gizmoBannerTextColor = new(0.95f, 1f, 0.72f, 1f);

        [SerializeField]
        [Min(8)]
        private int _gizmoPaylineCaptionFontSize = 13;

        [SerializeField]
        private Color _gizmoPaylineCaptionColor = new(1f, 1f, 1f, 1f);

        [Tooltip("Per-line «Линия N» label lift × GetHandleSize(midpoint).")]
        [SerializeField]
        [Min(0.02f)]
        private float _gizmoPaylineCaptionRaiseHandles = 0.4f;

        [SerializeField]
        private Color _gizmoPaylineWarningColor = new(1f, 0.52f, 0.32f, 1f);

        [Tooltip("Warning label offset below banner × GetHandleSize(SpinController).")]
        [SerializeField]
        [Min(0.05f)]
        private float _gizmoBannerWarningDownHandles = 0.7f;

        public SlotVisualData[,] finalVisuals; // filled from the screen after stop
        public IMoneySpend moneySpend;

        private int price;

        /// <summary>Master Scene-debug toggle (paylines + cell labels).</summary>
        public bool SlotSceneDebugEnabled => _slotSceneDebugEnabled;

        /// <summary>Payline overlay when this <see cref="SpinController"/> is selected.</summary>
        public bool DrawPaylineGizmosInScene => _slotSceneDebugEnabled && _drawPaylineGizmos;

        /// <summary><see cref="SlotElement"/> gizmos for reels under this controller.</summary>
        public bool DrawSlotElementGizmosInScene => _slotSceneDebugEnabled && _drawSlotElementGizmos;

        /// <summary>Visible window height (at least 1).</summary>
        public int WindowHeight => Mathf.Max(1, _countVerticalElements);

        /// <summary>
        ///     2D matrix of references to actual visible elements:
        ///     Elements[x,y] with y=0 bottom. Filled after spin stops.
        /// </summary>
        public SlotElement[,] Elements { get; private set; }

        private Coroutine _winLineCoroutine;

        private int[] _lastWinningPaylineIndices = Array.Empty<int>();

        /// <summary>ID matrix (same orientation as Elements): y=0 bottom.</summary>
        public int[,] FinalElementIDs
        {
            get
            {
                if (finalVisuals == null)
                {
                    return new int[0, 0];
                }

                int cols = finalVisuals.GetLength(0);
                int rows = finalVisuals.GetLength(1);
                int[,] ids = new int[cols, rows];
                for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                {
                    ids[x, y] = finalVisuals[x, y]?.id ?? -1;
                }

                return ids;
            }
        }

        /// <summary>
        ///     Definition indices that won on the last completed spin (empty after a new spin starts or on lose).
        ///     Matches indices into <see cref="GetPaylineDefinitionsSnapshot"/>.
        /// </summary>
        public IReadOnlyList<int> LastWinningPaylineIndices => Array.AsReadOnly(_lastWinningPaylineIndices);

        /// <summary>
        ///     How many payline definitions participate in evaluation (<see cref="_countLine"/> capped by available definitions).
        /// </summary>
        public int EvaluatedPaylineDefinitionCount => ComputeEvaluatedPaylineDefinitionCount();

        /// <summary>Assigned reel columns (left→right). Replace only while idle.</summary>
        public Row[] Rows
        {
            get => _rows;
            set
            {
                if (!IsStop())
                {
                    return;
                }

                _rows = value;
                SetSpace();
                ClampPaylineCountToDefinitions();
                SetPrice();
            }
        }

        /// <summary>How many paylines are active for bets/check (clamped to available definitions).</summary>
        public int ActivePaylineCount
        {
            get => _countLine;
            set
            {
                if (!IsStop())
                {
                    return;
                }

                int cap = GetPaylineDefinitionCapInternal();
                _countLine = Mathf.Clamp(Mathf.Max(1, value), 1, cap);
                SetPrice();
            }
        }

        /// <summary>Visible symbol rows in the window (maps to each Row.countSlotElement).</summary>
        public int VisibleWindowRows
        {
            get => _countVerticalElements;
            set
            {
                if (!IsStop())
                {
                    return;
                }

                _countVerticalElements = Mathf.Max(1, value);
                SetSpace();
                ClampPaylineCountToDefinitions();
                SetPrice();
            }
        }

        /// <summary>Index into <see cref="betsData.bets"/> (clamped when bets exist).</summary>
        public int BetSelectionIndex
        {
            get => _betsId;
            set
            {
                if (!IsStop())
                {
                    return;
                }

                if (betsData?.bets == null || betsData.bets.Length == 0)
                {
                    _betsId = 0;
                }
                else
                {
                    _betsId = Mathf.Clamp(value, 0, betsData.bets.Length - 1);
                }

                SetPrice();
            }
        }

        /// <summary>Delay between starting each column spin.</summary>
        public float DelayBetweenColumnSpins
        {
            get => _delaySpinRoll;
            set => _delaySpinRoll = Mathf.Max(0f, value);
        }

        /// <summary>Optional LineRenderer win-line playback settings (mutate fields from code).</summary>
        public WinLineRendererPlayback WinLinePlayback => _winLinePlayback;

        /// <summary>Current spin price after last <see cref="SetPrice"/> (same basis as next <see cref="StartSpin"/>).</summary>
        public int CurrentSpinPrice => price;

        /// <summary>
        ///     Batch-configure window height, active lines, and fallback row range on <see cref="checkSpin"/>.
        ///     Clears migrated legacy single-row binding when updating fallback bounds (same as <see cref="CheckSpin.SetFallbackPaylineWindowRows"/>).
        /// </summary>
        public void ConfigureSlotRuntime(int visibleWindowRows, int activePaylineCount,
            int fallbackMinInclusiveOrMinusOneAuto = -1, int fallbackMaxInclusiveOrMinusOneAuto = -1)
        {
            if (!IsStop())
            {
                return;
            }

            _countVerticalElements = Mathf.Max(1, visibleWindowRows);
            checkSpin?.SetFallbackPaylineWindowRows(fallbackMinInclusiveOrMinusOneAuto,
                fallbackMaxInclusiveOrMinusOneAuto);

            SetSpace();
            int cap = GetPaylineDefinitionCapInternal();
            _countLine = Mathf.Clamp(Mathf.Max(1, activePaylineCount), 1, cap);
            SetPrice();
        }

        /// <summary>Immutable snapshot for UI, saves, or gameplay logic.</summary>
        public SpinRuntimeSnapshot GetRuntimeSnapshot(bool refreshMatricesIfIdle = true)
        {
            if (refreshMatricesIfIdle && IsStop())
            {
                BuildVisibleMatrices();
            }

            int cols = _rows?.Length ?? 0;
            int winH = WindowHeight;
            int totalDefs = checkSpin != null && cols > 0
                ? checkSpin.GetPaylineDefinitionCount(cols, winH)
                : 0;
            bool fbOnly = checkSpin != null && cols > 0 && checkSpin.UsesFallbackPaylinesOnly(cols, winH);
            int fbMn = 0;
            int fbMx = 0;
            if (checkSpin != null && cols > 0)
            {
                checkSpin.GetResolvedFallbackWindowRowRange(winH, out fbMn, out fbMx);
            }

            int[] winCopy = _lastWinningPaylineIndices.Length > 0
                ? (int[])_lastWinningPaylineIndices.Clone()
                : Array.Empty<int>();

            return new SpinRuntimeSnapshot(
                IsStop(),
                winH,
                cols,
                _countLine,
                ComputeEvaluatedPaylineDefinitionCount(),
                totalDefs,
                _betsId,
                price,
                checkSpin != null && checkSpin.isActive,
                fbOnly,
                checkSpin?.FallbackWindowRowMinRaw ?? -1,
                checkSpin?.FallbackWindowRowMaxRaw ?? -1,
                fbMn,
                fbMx,
                winCopy);
        }

        private void Awake()
        {
            if (_moneyGameObject != null)
            {
                moneySpend ??= _moneyGameObject.GetComponent<IMoneySpend>();
            }
        }

        private void Start()
        {
            SetSpace();

            // Initialize row visuals when a sprite set exists
            if (allSpritesData != null && allSpritesData.visuals != null && allSpritesData.visuals.Length > 0)
            {
                SlotVisualData initial = allSpritesData.visuals[0];
                foreach (Row row in _rows)
                {
                    if (row != null)
                    {
                        row.SetVisuals(initial);
                    }
                }
            }

            SetPrice();
            _lineSlot?.LineActiv(false);
            StopWinLinePlayback();
        }

        private void OnDisable()
        {
            StopWinLinePlayback();
        }

        public void StartSpin()
        {
            if (!IsStop())
            {
                return;
            }

            if (_rows == null || _rows.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SpinController)}] No Row assigned.", this);
                return;
            }

            if (allSpritesData?.visuals == null || allSpritesData.visuals.Length == 0)
            {
                Debug.LogWarning($"[{nameof(SpinController)}] {nameof(SpritesData)} or visuals missing.", this);
                return;
            }

            SetPrice();

            if (moneySpend == null || moneySpend.Spend(price))
            {
                OnChangeMoneyWin?.Invoke("");
                StartCoroutine(StartSpinCoroutine());
                OnStartSpin?.Invoke();
            }
        }

        private IEnumerator StartSpinCoroutine()
        {
            WaitForSeconds delay = new(_delaySpinRoll);
            _lastWinningPaylineIndices = Array.Empty<int>();
            StopWinLinePlayback();
            _lineSlot?.LineActiv(false);

            int[,] planIds = BuildPlanIdMatrix();

            int v = WindowHeight;

            for (int x = 0; x < _rows.Length; x++)
            {
                Row row = _rows[x];
                if (row == null)
                {
                    yield return delay;
                    continue;
                }

                int[] columnTargets = new int[v];
                for (int y = 0; y < v; y++)
                {
                    columnTargets[y] = planIds[x, y];
                }

                row.Spin(allSpritesData, columnTargets);
                yield return delay;
            }

            yield return new WaitUntil(IsStop);

            BuildVisibleMatrices();

            ProcessSpinResult();
        }

        /// <summary>
        ///     Fills Elements[x,y] (references to visible elements) and finalVisuals[x,y] (data) from the screen.
        ///     y=0 bottom.
        /// </summary>
        private void BuildVisibleMatrices()
        {
            if (_rows == null || _rows.Length == 0)
            {
                Elements = null;
                finalVisuals = null;
                return;
            }

            int cols = _rows.Length;
            int rows = WindowHeight;

            Elements = new SlotElement[cols, rows];
            finalVisuals = new SlotVisualData[cols, rows];

            for (int x = 0; x < cols; x++)
            {
                Row row = _rows[x];
                if (row == null)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        Elements[x, y] = null;
                        finalVisuals[x, y] = null;
                    }

                    continue;
                }

                SlotElement[] visibleTopDown = row.GetVisibleTopDown();
                int take = Mathf.Min(rows, visibleTopDown.Length);

                for (int y = 0; y < rows; y++)
                {
                    SlotElement se = y < take ? visibleTopDown[rows - 1 - y] : null;
                    Elements[x, y] = se;

                    SlotVisualData v = null;
                    if (se != null && allSpritesData?.visuals != null && allSpritesData.visuals.Length > 0)
                    {
                        v = allSpritesData.visuals.FirstOrDefault(t => t.id == se.id);
                    }

                    finalVisuals[x, y] = v;
                }
            }
        }

        /// <summary>
        ///     Public accessor for the element matrix.
        ///     When idle, refreshes from the screen; during spin returns the last cache.
        /// </summary>
        public SlotElement[,] GetElementsMatrix(bool refreshIfIdle = true)
        {
            if (refreshIfIdle && IsStop())
            {
                BuildVisibleMatrices();
            }

            return Elements;
        }

        /// <summary>
        ///     Public accessor for the ID matrix (same orientation as Elements): y=0 is bottom.
        /// </summary>
        public int[,] GetElementIDsMatrix(bool refreshIfIdle = true)
        {
            if (refreshIfIdle && IsStop())
            {
                BuildVisibleMatrices();
            }

            return FinalElementIDs;
        }

        /// <summary>Effective payline definitions for current grid (Lines Data or fallback). Order matches line indices.</summary>
        public LinesData.InnerArray[] GetPaylineDefinitionsSnapshot()
        {
            if (_rows == null || _rows.Length == 0 || checkSpin == null)
            {
                return Array.Empty<LinesData.InnerArray>();
            }

            return checkSpin.GetEffectiveLines(_rows.Length, WindowHeight);
        }

        /// <summary>
        ///     Matrix <c>[lineDefinitionIndex, column]</c> = window row from bottom (same as <c>corY</c> in Lines Data).
        /// </summary>
        public int[,] GetPaylineWindowRowsMatrix()
        {
            LinesData.InnerArray[] defs = GetPaylineDefinitionsSnapshot();
            int cols = _rows?.Length ?? 0;
            if (defs.Length == 0 || cols <= 0)
            {
                return new int[0, 0];
            }

            int[,] m = new int[defs.Length, cols];
            for (int li = 0; li < defs.Length; li++)
            {
                int[] cor = defs[li]?.corY;
                if (cor == null || cor.Length != cols)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        m[li, c] = -1;
                    }

                    continue;
                }

                for (int c = 0; c < cols; c++)
                {
                    m[li, c] = cor[c];
                }
            }

            return m;
        }

        /// <summary>
        ///     Same as <see cref="GetPaylineWindowRowsMatrix"/> but only the first <see cref="EvaluatedPaylineDefinitionCount"/> lines (bet/eval scope).
        /// </summary>
        public int[,] GetActivePaylineWindowRowsMatrix()
        {
            int take = ComputeEvaluatedPaylineDefinitionCount();
            return SlicePaylineMajorDimension(GetPaylineWindowRowsMatrix(), take);
        }

        /// <summary>
        ///     Symbol IDs along each payline: <c>[lineDefinitionIndex, column]</c> from the visible grid (<c>y=0</c> bottom).
        /// </summary>
        public int[,] GetPaylineSymbolIdsMatrix(bool refreshIfIdle = true)
        {
            int[,] grid = GetElementIDsMatrix(refreshIfIdle);
            LinesData.InnerArray[] defs = GetPaylineDefinitionsSnapshot();
            int cols = _rows?.Length ?? 0;
            if (defs.Length == 0 || cols <= 0 || grid.Length == 0)
            {
                return new int[0, 0];
            }

            int gh = grid.GetLength(1);
            int[,] m = new int[defs.Length, cols];
            for (int li = 0; li < defs.Length; li++)
            {
                int[] cor = defs[li]?.corY;
                if (cor == null || cor.Length != cols)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        m[li, c] = -1;
                    }

                    continue;
                }

                for (int c = 0; c < cols; c++)
                {
                    int y = cor[c];
                    m[li, c] = (uint)y < (uint)gh ? grid[c, y] : -1;
                }
            }

            return m;
        }

        /// <summary>
        ///     Same as <see cref="GetPaylineSymbolIdsMatrix"/> restricted to evaluated (active) paylines.
        /// </summary>
        public int[,] GetActivePaylineSymbolIdsMatrix(bool refreshIfIdle = true)
        {
            int take = ComputeEvaluatedPaylineDefinitionCount();
            return SlicePaylineMajorDimension(GetPaylineSymbolIdsMatrix(refreshIfIdle), take);
        }

        /// <summary>
        ///     Visible <see cref="SlotElement"/> along payline <paramref name="lineDefinitionIndex"/> (column order).
        /// </summary>
        public bool TryGetPaylineSlotElements(int lineDefinitionIndex, out SlotElement[] elements,
            bool refreshIfIdle = true)
        {
            elements = null;
            SlotElement[,] mat = GetElementsMatrix(refreshIfIdle);
            LinesData.InnerArray[] defs = GetPaylineDefinitionsSnapshot();
            int cols = _rows?.Length ?? 0;
            if (mat == null || defs.Length == 0 || cols <= 0 || (uint)lineDefinitionIndex >= (uint)defs.Length)
            {
                return false;
            }

            int[] cor = defs[lineDefinitionIndex]?.corY;
            if (cor == null || cor.Length != cols)
            {
                return false;
            }

            int gh = mat.GetLength(1);
            elements = new SlotElement[cols];
            for (int c = 0; c < cols; c++)
            {
                int y = cor[c];
                elements[c] = (uint)y < (uint)gh ? mat[c, y] : null;
            }

            return true;
        }

        /// <summary>
        ///     For each entry in <see cref="LastWinningPaylineIndices"/>, elements along that payline (after last spin).
        /// </summary>
        public SlotElement[][] GetLastWinningPaylinesSlotElements(bool refreshIfIdle = true)
        {
            if (_lastWinningPaylineIndices.Length == 0)
            {
                return Array.Empty<SlotElement[]>();
            }

            SlotElement[][] rows = new SlotElement[_lastWinningPaylineIndices.Length][];
            for (int i = 0; i < _lastWinningPaylineIndices.Length; i++)
            {
                int defIx = _lastWinningPaylineIndices[i];
                rows[i] = TryGetPaylineSlotElements(defIx, out SlotElement[] els, refreshIfIdle)
                    ? els
                    : Array.Empty<SlotElement>();
            }

            return rows;
        }

        /// <summary>
        ///     Symbol IDs on winning paylines from last spin: <c>[whichWin, column]</c> (parallel to <see cref="LastWinningPaylineIndices"/>).
        /// </summary>
        public int[,] GetLastWinningPaylinesSymbolIds(bool refreshIfIdle = true)
        {
            int[,] full = GetPaylineSymbolIdsMatrix(refreshIfIdle);
            if (_lastWinningPaylineIndices.Length == 0 || full.Length == 0)
            {
                return new int[0, 0];
            }

            int cols = full.GetLength(1);
            int[,] m = new int[_lastWinningPaylineIndices.Length, cols];
            int lineCount = full.GetLength(0);
            for (int i = 0; i < _lastWinningPaylineIndices.Length; i++)
            {
                int li = _lastWinningPaylineIndices[i];
                if ((uint)li >= (uint)lineCount)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        m[i, c] = -1;
                    }

                    continue;
                }

                for (int c = 0; c < cols; c++)
                {
                    m[i, c] = full[li, c];
                }
            }

            return m;
        }

        /// <summary>
        ///     Window rows for winning paylines from last spin: <c>[whichWin, column]</c>.
        /// </summary>
        public int[,] GetLastWinningPaylinesWindowRows()
        {
            int[,] full = GetPaylineWindowRowsMatrix();
            if (_lastWinningPaylineIndices.Length == 0 || full.Length == 0)
            {
                return new int[0, 0];
            }

            int cols = full.GetLength(1);
            int[,] m = new int[_lastWinningPaylineIndices.Length, cols];
            int lineCount = full.GetLength(0);
            for (int i = 0; i < _lastWinningPaylineIndices.Length; i++)
            {
                int li = _lastWinningPaylineIndices[i];
                if ((uint)li >= (uint)lineCount)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        m[i, c] = -1;
                    }

                    continue;
                }

                for (int c = 0; c < cols; c++)
                {
                    m[i, c] = full[li, c];
                }
            }

            return m;
        }

        private int GetPaylineDefinitionCapInternal()
        {
            if (_rows == null || _rows.Length == 0 || checkSpin == null)
            {
                return 1;
            }

            return Mathf.Max(1, checkSpin.GetPaylineDefinitionCount(_rows.Length, WindowHeight));
        }

        private void ClampPaylineCountToDefinitions()
        {
            int cap = GetPaylineDefinitionCapInternal();
            _countLine = Mathf.Clamp(Mathf.Max(1, _countLine), 1, cap);
        }

        private int ComputeEvaluatedPaylineDefinitionCount()
        {
            if (_rows == null || _rows.Length == 0 || checkSpin == null)
            {
                return 0;
            }

            int defs = GetPaylineDefinitionCapInternal();
            return Mathf.Min(Mathf.Max(1, _countLine), defs);
        }

        private static int[,] SlicePaylineMajorDimension(int[,] full, int takeLines)
        {
            if (full.Length == 0 || takeLines <= 0)
            {
                return new int[0, 0];
            }

            int major = full.GetLength(0);
            int cols = full.GetLength(1);
            takeLines = Mathf.Min(takeLines, major);
            int[,] sliced = new int[takeLines, cols];
            for (int i = 0; i < takeLines; i++)
            {
                for (int c = 0; c < cols; c++)
                {
                    sliced[i, c] = full[i, c];
                }
            }

            return sliced;
        }

        /// <summary>Random plan plus optional win/lose shaping for CheckSpin.</summary>
        private int[,] BuildPlanIdMatrix()
        {
            int cols = _rows?.Length ?? 0;
            int vr = WindowHeight;
            int[,] planIds = new int[cols, vr];

            if (allSpritesData?.visuals == null || allSpritesData.visuals.Length == 0)
            {
                return planIds;
            }

            int nSym = allSpritesData.visuals.Length;
            for (int x = 0; x < cols; x++)
            for (int y = 0; y < vr; y++)
            {
                planIds[x, y] = allSpritesData.visuals[Random.Range(0, nSym)].id;
            }

            if (checkSpin != null && checkSpin.isActive)
            {
                try
                {
                    int totalIdCount = nSym;
                    if (_firstWin || Random.Range(0f, 1f) < chanceWin)
                    {
                        _firstWin = false;
                        checkSpin.SetWin(planIds, totalIdCount, _countLine);
                    }
                    else
                    {
                        int[] lines = checkSpin.GetWinningLines(planIds, _countLine);
                        if (lines.Length > 0)
                        {
                            checkSpin.SetLose(planIds, lines, totalIdCount, _countLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{nameof(SpinController)}] Plan build failed: {ex.Message}", this);
                }
            }

            return planIds;
        }

        private void ProcessSpinResult()
        {
            if (_logFinalVisuals && finalVisuals != null)
            {
                StringBuilder sb = new();
                sb.AppendLine("--- Final Visuals Table ---");
                int cols = finalVisuals.GetLength(0);
                int rows = finalVisuals.GetLength(1);

                for (int y = rows - 1; y >= 0; y--)
                {
                    List<string> parts = new(cols);
                    for (int x = 0; x < cols; x++)
                    {
                        int id = finalVisuals[x, y]?.id ?? -1;
                        parts.Add($"[{x + _gridIndexBase},{y + _gridIndexBase}] = {id}");
                    }

                    sb.AppendLine(string.Join(", ", parts));
                }

                Debug.Log(sb.ToString());
            }

            bool hasWon = false;

            if (finalVisuals == null || checkSpin == null || !checkSpin.isActive)
            {
                _lastWinningPaylineIndices = Array.Empty<int>();
                Lose();
                OnEndSpin?.Invoke();
                OnEnd?.Invoke(false);
                return;
            }

            try
            {
                int[,] finalIds = FinalElementIDs;
                int[] lines = checkSpin.GetWinningLines(finalIds, _countLine);

                if (lines.Length > 0)
                {
                    _lastWinningPaylineIndices = (int[])lines.Clone();
                    float[] mult = checkSpin.GetMultiplayers(finalIds, _countLine, lines);
                    Win(lines, mult);
                    hasWon = true;
                }
                else
                {
                    _lastWinningPaylineIndices = Array.Empty<int>();
                    Lose();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{nameof(SpinController)}] Result evaluation failed: {ex.Message}", this);
                _lastWinningPaylineIndices = Array.Empty<int>();
                Lose();
            }

            OnEndSpin?.Invoke();
            OnEnd?.Invoke(hasWon);
        }

        #region SimpleMethods

        public bool IsStop()
        {
            return _rows == null || _rows.All(row => row == null || !row.is_spinning);
        }

        private void Win(int[] lines, float[] mult)
        {
            StopWinLinePlayback();

            _lineSlot?.LineActiv(lines);

            if (_winLinePlayback.IsActive)
            {
                _winLineCoroutine = StartCoroutine(WinLinePlaybackRoutine(lines));
            }

            float moneyWin = 0;
            int linePrice = 0;

            if (betsData?.bets != null && betsData.bets.Length > 0 && _betsId >= 0 && _betsId < betsData.bets.Length)
            {
                linePrice = betsData.bets[_betsId];
            }

            foreach (float t in mult)
            {
                moneyWin += t * linePrice;
            }

            int payout = Mathf.Max(0, Mathf.RoundToInt(moneyWin));

            OnChangeMoneyWin?.Invoke(payout.ToString());
            OnWin?.Invoke(payout);
            OnWinLines?.Invoke(lines);
        }

        private void Lose()
        {
            OnChangeMoneyWin?.Invoke(0.ToString());
            OnLose?.Invoke();
        }

        private void SetPrice()
        {
            int linePrice = 0;

            if (betsData?.bets != null && betsData.bets.Length > 0)
            {
                if (_betsId < 0 || _betsId >= betsData.bets.Length)
                {
                    _betsId = 0;
                }

                linePrice = betsData.bets[_betsId];
            }

            price = _priceOnLine ? _countLine * linePrice : linePrice;

            if (_textCountLine != null)
            {
                _textCountLine.text = _countLine.ToString();
            }

            OnChangeBet?.Invoke(price.ToString());

            UpdatePaylineVisualHighlight();
        }

        /// <summary>Highlights payline UI objects for the first N active logical lines.</summary>
        private void UpdatePaylineVisualHighlight()
        {
            if (_lineSlot?.lines == null || _lineSlot.lines.Length == 0 || _rows == null || _rows.Length == 0)
            {
                return;
            }

            int defCount = checkSpin != null
                ? checkSpin.GetPaylineDefinitionCount(_rows.Length, WindowHeight)
                : 1;

            int active = Mathf.Min(_countLine, defCount, _lineSlot.lines.Length);
            if (active <= 0)
            {
                return;
            }

            int[] seq = Enumerable.Range(0, active).ToArray();
            _lineSlot.LineActiv(seq);
        }

        public void AddLine()
        {
            if (!IsStop())
            {
                return;
            }

            _countLine++;
            int maxDefs = GetMaxPaylineCountForUi();
            if (_lineSlot?.lines != null && maxDefs > 0 && _countLine > maxDefs)
            {
                _countLine = 1;
            }

            SetPrice();
        }

        public void RemoveLine()
        {
            if (!IsStop())
            {
                return;
            }

            _countLine--;
            int maxDefs = GetMaxPaylineCountForUi();
            if (maxDefs > 0 && _countLine < 1)
            {
                _countLine = maxDefs;
            }

            if (_countLine < 1)
            {
                _countLine = 1;
            }

            SetPrice();
        }

        private int GetMaxPaylineCountForUi()
        {
            int fromCheck = checkSpin != null && _rows != null && _rows.Length > 0
                ? checkSpin.GetPaylineDefinitionCount(_rows.Length, WindowHeight)
                : 1;

            int fromVisual = _lineSlot?.lines?.Length ?? int.MaxValue;
            return Mathf.Min(fromCheck, fromVisual);
        }

        public void SetMaxBet()
        {
            if (!IsStop())
            {
                return;
            }

            if (betsData?.bets != null && betsData.bets.Length > 0)
            {
                _betsId = betsData.bets.Length - 1;
            }
            else
            {
                _betsId = 0;
            }

            SetPrice();
        }

        public void AddBet()
        {
            if (!IsStop())
            {
                return;
            }

            if (betsData?.bets != null && betsData.bets.Length > 0)
            {
                _betsId++;
                if (_betsId >= betsData.bets.Length)
                {
                    _betsId = 0;
                }
            }
            else
            {
                _betsId = 0;
            }

            SetPrice();
        }

        public void RemoveBet()
        {
            if (!IsStop())
            {
                return;
            }

            if (betsData?.bets != null && betsData.bets.Length > 0)
            {
                _betsId--;
                if (_betsId < 0)
                {
                    _betsId = betsData.bets.Length - 1;
                }
            }
            else
            {
                _betsId = 0;
            }

            SetPrice();
        }

        private void OnValidate()
        {
            _rows ??= GetComponentsInChildren<Row>(true);
            if (_rows != null)
            {
                SetSpace();
            }

            // Keeps serialized line count consistent with Lines Data / fallback geometry (also matches gizmo eval cap).
            ClampPaylineCountToDefinitions();
        }

        private void SetSpace()
        {
            if (!_setSpace || _rows == null || _rows.Length == 0)
            {
                return;
            }

            bool isUI = _rows[0].TryGetComponent<RectTransform>(out _);

            for (int i = 0; i < _rows.Length; i++)
            {
                Row row = _rows[i];

                if (i > 0)
                {
                    Row prevRow = _rows[i - 1];
                    if (isUI && row.transform is RectTransform rt && prevRow.transform is RectTransform prevRt)
                    {
                        rt.anchoredPosition =
                            new Vector2(prevRt.anchoredPosition.x + _space.x, prevRt.anchoredPosition.y);
                    }
                    else
                    {
                        row.transform.localPosition = new Vector3(prevRow.transform.localPosition.x + _space.x,
                            prevRow.transform.localPosition.y, row.transform.localPosition.z);
                    }
                }

                row.countSlotElement = _countVerticalElements;
                row.spaceY = _space.y;
                row.offsetY = offsetY;
                row.ApplyLayout();

                if (_isSingleSpeed)
                {
                    row.speedControll = _speedControll;
                }
            }
        }

        private void StopWinLinePlayback()
        {
            if (_winLineCoroutine != null)
            {
                StopCoroutine(_winLineCoroutine);
                _winLineCoroutine = null;
            }

            _winLinePlayback?.ClearAll();
        }

        private IEnumerator WinLinePlaybackRoutine(int[] winningLineIndices)
        {
            if (!_winLinePlayback.IsActive || winningLineIndices == null || winningLineIndices.Length == 0 ||
                checkSpin == null || _rows == null || _rows.Length == 0)
            {
                yield break;
            }

            int winH = WindowHeight;
            LinesData.InnerArray[] defs = checkSpin.GetEffectiveLines(_rows.Length, winH);

            while (true)
            {
                bool parallelFeasible = _winLinePlayback.layout ==
                                        WinLineRendererPlayback.LayoutMode.ParallelWhenPossible &&
                                        winningLineIndices.Length > 1 &&
                                        _winLinePlayback.CountAssignedRenderers() >= winningLineIndices.Length;

                if (parallelFeasible)
                {
                    bool orderedOk = true;
                    for (int i = 0; i < winningLineIndices.Length; i++)
                    {
                        if (_winLinePlayback.RendererAt(i) == null)
                        {
                            orderedOk = false;
                            break;
                        }
                    }

                    if (orderedOk)
                    {
                        yield return PlayWinLinesParallel(winningLineIndices, defs);
                    }
                    else
                    {
                        yield return PlayWinLinesSequential(winningLineIndices, defs);
                    }
                }
                else
                {
                    yield return PlayWinLinesSequential(winningLineIndices, defs);
                }

                if (!_winLinePlayback.loopUntilNextSpin)
                {
                    break;
                }

                if (_winLinePlayback.cycleGapSeconds > 0f)
                {
                    yield return new WaitForSeconds(_winLinePlayback.cycleGapSeconds);
                }
            }

            _winLinePlayback.ClearAll();
        }

        private IEnumerator PlayWinLinesSequential(int[] winningLineIndices, LinesData.InnerArray[] defs)
        {
            LineRenderer lr = _winLinePlayback.FirstRenderer();
            if (lr == null)
            {
                yield break;
            }

            foreach (int lineIx in winningLineIndices)
            {
                if (lineIx < 0 || lineIx >= defs.Length)
                {
                    continue;
                }

                LinesData.InnerArray def = defs[lineIx];
                Vector3[] pts = BuildPaylineWorldPoints(def);
                if (pts == null || pts.Length < 2)
                {
                    continue;
                }

                _winLinePlayback.ConfigurePolyline(lr, pts);

                float elapsed = 0f;
                while (elapsed < _winLinePlayback.holdSeconds)
                {
                    elapsed += Time.deltaTime;
                    _winLinePlayback.ApplyVisualFrame(lr, elapsed);
                    yield return null;
                }

                lr.enabled = false;
                lr.positionCount = 0;

                if (_winLinePlayback.stepGapSeconds > 0f)
                {
                    yield return new WaitForSeconds(_winLinePlayback.stepGapSeconds);
                }
            }
        }

        private IEnumerator PlayWinLinesParallel(int[] winningLineIndices, LinesData.InnerArray[] defs)
        {
            int n = winningLineIndices.Length;
            LineRenderer[] active = new LineRenderer[n];
            float phaseSkew = 0.09f;

            for (int i = 0; i < n; i++)
            {
                LineRenderer lr = _winLinePlayback.RendererAt(i);
                int lineIx = winningLineIndices[i];
                if (lr == null || lineIx < 0 || lineIx >= defs.Length)
                {
                    _winLinePlayback.ClearAll();
                    yield break;
                }

                Vector3[] pts = BuildPaylineWorldPoints(defs[lineIx]);
                if (pts == null || pts.Length < 2)
                {
                    _winLinePlayback.ClearAll();
                    yield break;
                }

                _winLinePlayback.ConfigurePolyline(lr, pts);
                active[i] = lr;
            }

            float elapsed = 0f;
            while (elapsed < _winLinePlayback.holdSeconds)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < n; i++)
                {
                    if (active[i] != null && active[i].enabled)
                    {
                        _winLinePlayback.ApplyVisualFrame(active[i], elapsed + i * phaseSkew);
                    }
                }

                yield return null;
            }

            _winLinePlayback.ClearAll();
        }

        private Vector3[] BuildPaylineWorldPoints(LinesData.InnerArray line)
        {
            if (_rows == null || line.corY == null || line.corY.Length != _rows.Length)
            {
                return null;
            }

            Vector3[] pts = new Vector3[line.corY.Length];
            Vector3 off = _winLinePlayback.worldOffset;

            for (int col = 0; col < line.corY.Length; col++)
            {
                Row row = _rows[col];
                if (row == null)
                {
                    return null;
                }

                pts[col] = PaylineLineGeometry.GetCellWorld(row, line.corY[col]) + off;
            }

            return pts;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!DrawPaylineGizmosInScene || _rows == null || _rows.Length == 0 || checkSpin == null)
            {
                return;
            }

            int winH = WindowHeight;
            LinesData.InnerArray[] defs = checkSpin.GetEffectiveLines(_rows.Length, winH);
            if (defs.Length == 0)
            {
                return;
            }

            int eval = Mathf.Min(Mathf.Max(1, _countLine), defs.Length);
            bool fbOnly = checkSpin.UsesFallbackPaylinesOnly(_rows.Length, winH);
            checkSpin.GetResolvedFallbackWindowRowRange(winH, out int fbMin, out int fbMax);
            int drawn = 0;

            Vector3 hdrPos = transform.position;
            float hdrScale = HandleUtility.GetHandleSize(hdrPos);

            GUIStyle hdrStyle = new(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Max(8, _gizmoBannerFontSize),
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            hdrStyle.normal.textColor = _gizmoBannerTextColor;

            string fbRowsTxt = fbMin == fbMax
                ? "ряд <b>Y=" + fbMin + "</b>"
                : "ряды <b>Y=" + fbMin + "–" + fbMax + "</b>";

            string srcLine = fbOnly
                ? "<color=#FFF59D>Fallback:</color> горизонталь · " + fbRowsTxt +
                  " <i>(0 = низ окна)</i>"
                : "<color=#A8E7FF>Lines Data</color> (ScriptableObject)";

            string hdr = "<size=14><b>Neo.Bonus — линии выплат</b></size>\n" +
                         "Гизмо: <b>" + eval + "</b> из <b>" + defs.Length + "</b> определений · активных в ставке: <b>" +
                         _countLine + "</b> · подряд символов: " + checkSpin.SequenceLength + "\n" +
                         "<i>Число линий на сцене = min(активных, определений); fallback даёт столько горизонталей, сколько рядов в диапазоне после clamp к окну.</i>\n" +
                         srcLine;

            Handles.Label(hdrPos + Vector3.up * (hdrScale * _gizmoBannerRaiseHandles), hdr, hdrStyle);

            GUIStyle lineLbl = new(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Max(8, _gizmoPaylineCaptionFontSize),
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            lineLbl.normal.textColor = _gizmoPaylineCaptionColor;

            for (int li = 0; li < eval; li++)
            {
                LinesData.InnerArray line = defs[li];
                if (line.corY == null || line.corY.Length != _rows.Length)
                {
                    continue;
                }

                Color hsl = Color.HSVToRGB((li * 0.17f) % 1f, 0.92f, 1f);
                Color lineCol = _gizmoPaylineColor;
                lineCol.r = hsl.r;
                lineCol.g = hsl.g;
                lineCol.b = hsl.b;

                List<Vector3> pts = new(_rows.Length);
                for (int col = 0; col < _rows.Length; col++)
                {
                    Row row = _rows[col];
                    if (row == null)
                    {
                        continue;
                    }

                    int rowFromBottom = line.corY[col];
                    Vector3 p = PaylineLineGeometry.GetCellWorld(row, rowFromBottom) +
                                Vector3.forward * (0.03f + li * 0.0025f);
                    pts.Add(p);
                }

                if (pts.Count < 2)
                {
                    continue;
                }

                Handles.color = lineCol;
                Handles.DrawAAPolyLine(_gizmoPaylineWidth, pts.ToArray());

                foreach (Vector3 p in pts)
                {
                    float rh = HandleUtility.GetHandleSize(p) * _gizmoJointRadiusMultiplier;
                    Handles.DrawSolidDisc(p, Vector3.forward, rh);
                }

                Vector3 mid = Vector3.zero;
                foreach (Vector3 q in pts)
                {
                    mid += q;
                }

                mid /= pts.Count;

                string corDesc = string.Join(", ", line.corY.Select(y => y.ToString()));
                string hex = ColorUtility.ToHtmlStringRGBA(lineCol);
                string caption = "<color=#" + hex + ">Линия " + li + "</color>\n" + "corY: [" + corDesc + "]";
                if (fbOnly && defs.Length == 1 && li == 0)
                {
                    caption += "\n<i>Одна fallback-линия (нет Lines Data)</i>";
                }
                else if (fbOnly && defs.Length > 1 && li == 0)
                {
                    caption += "\n<i>Fallback: несколько горизонталей по диапазону рядов</i>";
                }

                Handles.Label(
                    mid + Vector3.up * (HandleUtility.GetHandleSize(mid) * _gizmoPaylineCaptionRaiseHandles),
                    caption,
                    lineLbl);
                drawn++;
            }

            if (drawn < eval)
            {
                GUIStyle warn = new(EditorStyles.boldLabel)
                {
                    fontSize = Mathf.Max(8, _gizmoPaylineCaptionFontSize),
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                warn.normal.textColor = _gizmoPaylineWarningColor;
                Handles.Label(
                    hdrPos + Vector3.down * (hdrScale * _gizmoBannerWarningDownHandles),
                    "<b>Внимание:</b> ожидалось нарисовать " + eval + " линий, фактически " + drawn +
                    " — проверьте null у <b>Row</b> в массиве колонок или длину <b>corY</b>.",
                    warn);
            }
        }
#endif

        /// <summary>Immutable snapshot from <see cref="GetRuntimeSnapshot"/>.</summary>
        public readonly struct SpinRuntimeSnapshot
        {
            public SpinRuntimeSnapshot(
                bool isIdle,
                int windowHeight,
                int columnCount,
                int activePaylineCount,
                int evaluatedPaylineCount,
                int totalPaylineDefinitionCount,
                int betIndex,
                int spinPrice,
                bool checkSpinActive,
                bool usesFallbackPaylinesOnly,
                int fallbackMinRaw,
                int fallbackMaxRaw,
                int fallbackResolvedMinRow,
                int fallbackResolvedMaxRow,
                int[] lastWinningPaylineIndicesCopy)
            {
                IsIdle = isIdle;
                WindowHeight = windowHeight;
                ColumnCount = columnCount;
                ActivePaylineCount = activePaylineCount;
                EvaluatedPaylineCount = evaluatedPaylineCount;
                TotalPaylineDefinitionCount = totalPaylineDefinitionCount;
                BetIndex = betIndex;
                SpinPrice = spinPrice;
                CheckSpinActive = checkSpinActive;
                UsesFallbackPaylinesOnly = usesFallbackPaylinesOnly;
                FallbackMinRaw = fallbackMinRaw;
                FallbackMaxRaw = fallbackMaxRaw;
                FallbackResolvedMinRow = fallbackResolvedMinRow;
                FallbackResolvedMaxRow = fallbackResolvedMaxRow;
                LastWinningPaylineIndicesCopy = lastWinningPaylineIndicesCopy ?? Array.Empty<int>();
            }

            public bool IsIdle { get; }

            public int WindowHeight { get; }

            public int ColumnCount { get; }

            public int ActivePaylineCount { get; }

            public int EvaluatedPaylineCount { get; }

            public int TotalPaylineDefinitionCount { get; }

            public int BetIndex { get; }

            public int SpinPrice { get; }

            public bool CheckSpinActive { get; }

            public bool UsesFallbackPaylinesOnly { get; }

            public int FallbackMinRaw { get; }

            public int FallbackMaxRaw { get; }

            public int FallbackResolvedMinRow { get; }

            public int FallbackResolvedMaxRow { get; }

            /// <summary>Copy of last winning line definition indices (allocate sparingly).</summary>
            public int[] LastWinningPaylineIndicesCopy { get; }
        }

        #endregion
    }
}
