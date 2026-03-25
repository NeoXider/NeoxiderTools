using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    /// <summary>
    ///     Spin orchestrator:
    ///     - Arranges rows and their parameters (spaceY, offsetY, speedControll)
    ///     - Starts row spins and waits until all rows stop
    ///     - Reads exactly the 3 visible symbols per row into finalVisuals
    ///     - Evaluates win/lose and raises events
    ///     - Exposes a 2D matrix of visible elements (Elements) for effects
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
        [SerializeField] private int _countVerticalElements = 3; // visible window = 3
        [SerializeField] private Row[] _rows;

        [SerializeField] private bool _isSingleSpeed = true;
        [Range(0f, 1f)] public float chanseWin = 0.5f;

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

        [SerializeField] [Min(1)] private int _countLine = 1;
        [SerializeField] [Min(0)] private int _betsId;
        [SerializeField] private bool _logFinalVisuals;

        [Tooltip("From which index to print coordinates in Debug: 0 (default) or 1, etc.")] [SerializeField]
        private int _gridIndexBase = 1;

        public SlotVisualData[,] finalVisuals; // filled from the screen after stop
        public IMoneySpend moneySpend;

        private int price;

        /// <summary>
        ///     2D matrix of references to actual visible elements:
        ///     Elements[x,y] with y=0 bottom, y=2 top. Filled after spin stops.
        /// </summary>
        public SlotElement[,] Elements { get; private set; }

        /// <summary>ID matrix (same orientation as Elements): y=0 bottom, y=2 top.</summary>
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
            _betsId = 0;

            // Initialize row visuals when a sprite set exists
            if (allSpritesData != null && allSpritesData.visuals != null && allSpritesData.visuals.Length > 0)
            {
                SlotVisualData initial = allSpritesData.visuals[0];
                foreach (Row row in _rows)
                {
                    row.SetVisuals(initial);
                }
            }

            SetPrice();
            _lineSlot?.LineActiv(false);
        }

        public void StartSpin()
        {
            if (!IsStop())
            {
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
            _lineSlot?.LineActiv(false);

            GenerateFinalPlanIds(); // kept for win/lose odds and debugging

            bool hasSprites = allSpritesData != null && allSpritesData.visuals != null &&
                              allSpritesData.visuals.Length > 0;

            // Start rows (optional stagger)
            for (int x = 0; x < _rows.Length; x++)
            {
                if (hasSprites)
                {
                    _rows[x].Spin(allSpritesData, null);
                }

                yield return delay;
            }

            // Wait until ALL rows have fully stopped
            yield return new WaitUntil(IsStop);

            // Build screen snapshot and visible-element cache
            BuildVisibleMatrices();

            // Evaluate outcome and raise events
            ProcessSpinResult();
        }

        /// <summary>
        ///     Fills Elements[x,y] (references to visible elements) and finalVisuals[x,y] (data) from the screen.
        ///     y=0 bottom, y=2 top.
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
            int rows = 3;

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

                // Row returns three window slots Top→Down
                SlotElement[] visibleTopDown = row.GetVisibleTopDown();

                // Write Bottom→Top into matrices (y=0 is bottom)
                for (int y = 0; y < rows; y++)
                {
                    SlotElement se = visibleTopDown[rows - 1 - y]; // 2..0 => bottom..top
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

        /// <summary>
        ///     Generate an ID "plan" (does not force rows) for win/lose probability logic.
        /// </summary>
        private void GenerateFinalPlanIds()
        {
            if (allSpritesData?.visuals == null || allSpritesData.visuals.Length == 0)
            {
                return;
            }

            int[,] planIds = new int[_rows.Length, 3];
            for (int x = 0; x < _rows.Length; x++)
            for (int y = 0; y < 3; y++)
            {
                planIds[x, y] = allSpritesData.visuals[Random.Range(0, allSpritesData.visuals.Length)].id;
            }

            if (checkSpin != null && checkSpin.isActive)
            {
                try
                {
                    int totalIdCount = allSpritesData.visuals.Length;
                    if (_firstWin || Random.Range(0f, 1f) < chanseWin)
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
                catch
                {
                    /* no SO — ignore */
                }
            }
        }

        private void ProcessSpinResult()
        {
            // Configurable debug: print coordinates with _gridIndexBase (0, 1, etc.)
            if (_logFinalVisuals && finalVisuals != null)
            {
                StringBuilder sb = new();
                sb.AppendLine("--- Final Visuals Table ---");
                int cols = finalVisuals.GetLength(0);
                int rows = finalVisuals.GetLength(1);

                // Print top-down (like on screen), coordinates use [x+base, y+base]
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
                Lose();
                OnEndSpin?.Invoke();
                OnEnd?.Invoke(false);
                return;
            }

            try
            {
                int[,] finalIds = FinalElementIDs; // what is on screen (bottom-up)
                int[] lines = checkSpin.GetWinningLines(finalIds, _countLine);

                if (lines.Length > 0)
                {
                    float[] mult = checkSpin.GetMultiplayers(finalIds, _countLine, lines);
                    Win(lines, mult);
                    hasWon = true;
                }
                else
                {
                    Lose();
                }
            }
            catch
            {
                Lose();
            }

            OnEndSpin?.Invoke();
            OnEnd?.Invoke(hasWon);
        }

        #region SimpleMethods

        public bool IsStop()
        {
            return _rows == null || _rows.All(row => !row.is_spinning);
        }

        private void Win(int[] lines, float[] mult)
        {
            _lineSlot?.LineActiv(lines);

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

            moneyWin = Mathf.Max(1, moneyWin);

            OnChangeMoneyWin?.Invoke(((int)moneyWin).ToString());
            OnWin?.Invoke((int)moneyWin);
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

            if (_lineSlot?.lines != null && _lineSlot.lines.Length > 0)
            {
                int[] seq = Enumerable.Range(0, Mathf.Min(_countLine, _lineSlot.lines.Length)).ToArray();
                _lineSlot.LineActiv(seq);
            }
        }

        public void AddLine()
        {
            if (!IsStop())
            {
                return;
            }

            _countLine++;
            if (_lineSlot?.lines != null && _countLine > _lineSlot.lines.Length)
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
            if (_lineSlot?.lines != null && _countLine < 1)
            {
                _countLine = _lineSlot.lines.Length;
            }

            if (_countLine < 1)
            {
                _countLine = 1;
            }

            SetPrice();
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

        #endregion
    }
}
