using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    public class SpinController : MonoBehaviour
    {
        [SerializeField] public CheckSpin checkSpin = new();
        [SerializeField] public BetsData betsData;                // может быть null
        [SerializeField] public SpritesData allSpritesData;       // может быть null

        [Space] [Header("Settings")] [SerializeField] [RequireInterface(typeof(IMoneySpend))]
        private GameObject _moneyGameObject;

        [SerializeField] private bool _priceOnLine = true;
        [SerializeField] private int _countVerticalElements = 3; // видимое окно = 3
        [SerializeField] private Row[] _rows;

        [SerializeField] private bool _isSingleSpeed = true;
        [Range(0f, 1f)] public float chanseWin = 0.5f;

        [Space] [Header("Visual")] [SerializeField]
        private float _delaySpinRoll = 0.2f;

        [SerializeField] private SpeedControll _speedControll = new()
        {
            speed = 5000,
            timeSpin = 1,
            decelerationTime = 1,
            minSpeed = 500,
        };
        [SerializeField] private bool _setSpace;
        [SerializeField] private Vector2 _space = Vector2.one;
        [SerializeField] private float offsetY;
        [SerializeField] private VisualSlotLines _lineSlot = new();

        [Space] [Header("Text")] [SerializeField]
        private TMP_Text _textCountLine;

        [Space] [Header("Events")] public UnityEvent OnStartSpin;
        public UnityEvent OnEndSpin;
        [Tooltip("Событие в конце спина. Передает true, если был выигрыш, иначе false.")]
        public UnityEvent<bool> OnEnd;

        [Space] public UnityEvent<int> OnWin;
        public UnityEvent<int[]> OnWinLines;
        public UnityEvent OnLose;

        [Space] public UnityEvent<string> OnChangeBet;
        public UnityEvent<string> OnChangeMoneyWin;

        [Space] [Header("Debug")] [SerializeField] private bool _firstWin;
        [SerializeField] [Min(1)] private int _countLine = 1;
        [SerializeField] [Min(0)] private int _betsId;

        private SlotElement[,] _elements;
        public SlotElement[,] Elements => _elements;
        
        public SlotVisualData[,] finalVisuals; // будет собираться ИЗ реальных видимых трёх после стопа
        public IMoneySpend moneySpend;

        private int price;

        public int[,] FinalElementIDs
        {
            get
            {
                if (finalVisuals == null) return new int[0, 0];
                var cols = finalVisuals.GetLength(0);
                var rows = finalVisuals.GetLength(1);
                var ids = new int[cols, rows];
                for (var i = 0; i < cols; i++)
                for (var j = 0; j < rows; j++)
                    ids[i, j] = finalVisuals[i, j]?.id ?? -1;
                return ids;
            }
        }

        private void Awake()
        {
            if (_moneyGameObject != null)
                moneySpend ??= _moneyGameObject.GetComponent<IMoneySpend>();
        }

        private void Start()
        {
            SetSpace();
            SetElements();
            _betsId = 0;

            if (allSpritesData != null && allSpritesData.visuals != null && allSpritesData.visuals.Length > 0)
            {
                var initialData = allSpritesData.visuals[0];
                foreach (var row in _rows) row.SetVisuals(initialData);
            }

            SetPrice();
            if (_lineSlot != null) _lineSlot.LineActiv(false);
        }

        private void SetElements()
        {
            if (_rows == null) return;
            _elements = new SlotElement[_rows.Length, _countVerticalElements];
            for (var i = 0; i < _rows.Length; i++)
            for (var j = 0; j < _countVerticalElements; j++)
                if (_rows[i].SlotElements.Length > j)
                    _elements[i, j] = _rows[i].SlotElements[j];
        }

        public void StartSpin()
        {
            if (!IsStop()) return;

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
            var delay = new WaitForSeconds(_delaySpinRoll);
            if (_lineSlot != null) _lineSlot.LineActiv(false);

            // Можно генерить «план» (для будущего), но главное — финал мы снимем с экрана.
            GenerateFinalPlanIds(); // опционально: чтобы сохранялась вероятность win/lose

            // Запускаем кручение — как раньше
            bool hasSprites = allSpritesData != null && allSpritesData.visuals != null && allSpritesData.visuals.Length > 0;

            for (var x = 0; x < _rows.Length; x++)
            {
                if (hasSprites)
                {
                    // Подачу очереди можно оставить любой — результат всё равно будет считан с экрана.
                    _rows[x].Spin(allSpritesData, null);
                }
                yield return delay;
            }

            // Ждём естественной остановки — без вмешательства в спрайты
            yield return new WaitUntil(IsStop);

            // СЧИТЫВАЕМ ТО, ЧТО ВИДИТ ИГРОК: берём 3 видимых из каждого ряда (Top→Down)
            BuildFinalVisualsFromScreen();

            ProcessSpinResult();
        }

        /// <summary>Собирает finalVisuals из реальных видимых элементов (Top→Down) каждого ряда.</summary>
        private void BuildFinalVisualsFromScreen()
        {
            if (_rows == null || _rows.Length == 0)
            {
                finalVisuals = null;
                return;
            }

            finalVisuals = new SlotVisualData[_rows.Length, 3];

            for (int x = 0; x < _rows.Length; x++)
            {
                var row = _rows[x];
                if (row == null)
                {
                    finalVisuals[x, 0] = finalVisuals[x, 1] = finalVisuals[x, 2] = null;
                    continue;
                }

                var visible = row.GetVisibleTopDown(); // гарантированно 3 элемента (Top→Down)

                for (int y = 0; y < 3; y++)
                {
                    var se = visible[y];
                    if (se == null)
                    {
                        finalVisuals[x, y] = null;
                        continue;
                    }

                    // Находим визуал по id (чтобы GameController получил корректные id)
                    SlotVisualData v = null;
                    if (allSpritesData?.visuals != null && allSpritesData.visuals.Length > 0)
                        v = allSpritesData.visuals.FirstOrDefault(t => t.id == se.id);

                    finalVisuals[x, y] = v; // если не нашли — null → id будет -1 (окей как защита)
                }
            }
        }

        /// <summary>
        /// Опционально генерим план id с использованием CheckSpin, чтобы вероятность win/lose сохранялась.
        /// (Непосредственно экраны спрайтов мы не форсируем — только читаем их после стопа.)
        /// </summary>
        private void GenerateFinalPlanIds()
        {
            if (allSpritesData == null || allSpritesData.visuals == null || allSpritesData.visuals.Length == 0)
                return;

            var planIds = new int[_rows.Length, 3];
            for (int x = 0; x < _rows.Length; x++)
                for (int y = 0; y < 3; y++)
                    planIds[x, y] = allSpritesData.visuals[Random.Range(0, allSpritesData.visuals.Length)].id;

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
                        var lines = checkSpin.GetWinningLines(planIds, _countLine);
                        if (lines.Length > 0)
                            checkSpin.SetLose(planIds, lines, totalIdCount, _countLine);
                    }
                }
                catch { /* нет SO — игнор */ }
            }

            // Ничего не навязываем ряду — это лишь «план» для вероятностей/отладки при желании.
        }

        private void ProcessSpinResult()
        {
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
                var finalIds = FinalElementIDs; // теперь это ТО, ЧТО НА ЭКРАНЕ (Top→Down)
                var lines = checkSpin.GetWinningLines(finalIds, _countLine);

                if (lines.Length > 0)
                {
                    var mult = checkSpin.GetMultiplayers(finalIds, _countLine, lines);
                    Win(lines, mult);
                    hasWon = true;
                }
                else Lose();
            }
            catch { Lose(); }

            OnEndSpin?.Invoke();
            OnEnd?.Invoke(hasWon);
        }

        #region SimpleMethods

        public bool IsStop() => _rows == null || _rows.All(row => !row.is_spinning);

        private void Win(int[] lines, float[] mult)
        {
            if (_lineSlot != null) _lineSlot.LineActiv(lines);
            float moneyWin = 0;

            int linePrice = 0;
            if (betsData != null && betsData.bets != null && betsData.bets.Length > 0 && _betsId >= 0 && _betsId < betsData.bets.Length)
                linePrice = betsData.bets[_betsId];

            foreach (var t in mult) moneyWin += t * linePrice;

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
            if (betsData != null && betsData.bets != null && betsData.bets.Length > 0)
            {
                if (_betsId < 0 || _betsId >= betsData.bets.Length) _betsId = 0;
                linePrice = betsData.bets[_betsId];
            }

            price = _priceOnLine ? _countLine * linePrice : linePrice;

            if (_textCountLine != null)
                _textCountLine.text = _countLine.ToString();

            OnChangeBet?.Invoke(price.ToString());

            if (_lineSlot != null && _lineSlot.lines != null && _lineSlot.lines.Length > 0)
            {
                var sequence = Enumerable.Range(0, Mathf.Min(_countLine, _lineSlot.lines.Length)).ToArray();
                _lineSlot.LineActiv(sequence);
            }
        }

        public void AddLine()
        {
            if (!IsStop()) return;

            _countLine++;
            if (_lineSlot != null && _lineSlot.lines != null && _countLine > _lineSlot.lines.Length)
                _countLine = 1;

            SetPrice();
        }

        public void RemoveLine()
        {
            if (!IsStop()) return;

            _countLine--;
            if (_lineSlot != null && _lineSlot.lines != null && _countLine < 1)
                _countLine = _lineSlot.lines.Length;

            if (_countLine < 1) _countLine = 1;
            SetPrice();
        }

        public void SetMaxBet()
        {
            if (!IsStop()) return;

            if (betsData != null && betsData.bets != null && betsData.bets.Length > 0)
                _betsId = betsData.bets.Length - 1;
            else
                _betsId = 0;

            SetPrice();
        }

        public void AddBet()
        {
            if (!IsStop()) return;

            if (betsData != null && betsData.bets != null && betsData.bets.Length > 0)
            {
                _betsId++;
                if (_betsId >= betsData.bets.Length) _betsId = 0;
            }
            else
            {
                _betsId = 0;
            }

            SetPrice();
        }

        public void RemoveBet()
        {
            if (!IsStop()) return;

            if (betsData != null && betsData.bets != null && betsData.bets.Length > 0)
            {
                _betsId--;
                if (_betsId < 0) _betsId = betsData.bets.Length - 1;
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
            if (_rows != null) SetSpace();
        }

        private void SetSpace()
        {
            if (_setSpace == false || _rows == null || _rows.Length == 0) return;

            var isUI = _rows[0].TryGetComponent<RectTransform>(out _);

            for (var i = 0; i < _rows.Length; i++)
            {
                var row = _rows[i];
                if (i > 0)
                {
                    var prevRow = _rows[i - 1];
                    if (isUI && row.transform is RectTransform rt && prevRow.transform is RectTransform prevRt)
                        rt.anchoredPosition = new Vector2(prevRt.anchoredPosition.x + _space.x, prevRt.anchoredPosition.y);
                    else
                        row.transform.localPosition = new Vector3(prevRow.transform.localPosition.x + _space.x,
                            prevRow.transform.localPosition.y, row.transform.localPosition.z);
                }

                row.countSlotElement = _countVerticalElements;
                row.spaceY = _space.y;
                row.offsetY = offsetY;
                row.ApplyLayout();

                if (_isSingleSpeed)
                    row.speedControll = _speedControll;
            }
        }

        #endregion
    }
}
