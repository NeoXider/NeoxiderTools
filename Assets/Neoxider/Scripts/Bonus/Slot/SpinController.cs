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
    /// Оркестратор спина:
    /// - Расставляет ряды и их параметры (spaceY, offsetY, speedControll)
    /// - Стартует спин рядов и ждёт полного останова
    /// - Считывает ИМЕННО видимые 3 символа из каждого ряда и собирает finalVisuals
    /// - Считает выигрыш/проигрыш и шлёт события
    /// - Даёт доступ к двумерной матрице видимых элементов (Elements) для эффектов
    /// Здесь НЕТ анимаций — весь движок вращения/торможения внутри Row.
    /// </summary>
    public class SpinController : MonoBehaviour
    {
        [SerializeField] public CheckSpin checkSpin = new();
        [SerializeField] public BetsData betsData; // может быть null
        [SerializeField] public SpritesData allSpritesData; // может быть null

        [Space, Header("Settings")]
        [SerializeField, RequireInterface(typeof(IMoneySpend))]
        private GameObject _moneyGameObject;

        [SerializeField] private bool _priceOnLine = true;
        [SerializeField] private int _countVerticalElements = 3; // видимое окно = 3
        [SerializeField] private Row[] _rows;

        [SerializeField] private bool _isSingleSpeed = true;
        [Range(0f, 1f)] public float chanseWin = 0.5f;

        [Space, Header("Visual")]
        [SerializeField] private float _delaySpinRoll = 0.2f;

        [SerializeField] private SpeedControll _speedControll = new()
        {
            speed = 5000,
            timeSpin = 1,
        };

        [SerializeField] private bool _setSpace;
        [SerializeField] private Vector2 _space = Vector2.one;
        [SerializeField] private float offsetY;
        [SerializeField] private VisualSlotLines _lineSlot = new();

        [Space, Header("Text")]
        [SerializeField] private TMP_Text _textCountLine;

        [Space, Header("Events")]
        public UnityEvent OnStartSpin;
        public UnityEvent OnEndSpin;
        /// <summary>Передает true, если был выигрыш.</summary>
        public UnityEvent<bool> OnEnd;

        [Space] public UnityEvent<int> OnWin;
        public UnityEvent<int[]> OnWinLines;
        public UnityEvent OnLose;

        [Space] public UnityEvent<string> OnChangeBet;
        public UnityEvent<string> OnChangeMoneyWin;

        [Space, Header("Debug")]
        [SerializeField] private bool _firstWin;
        [SerializeField, Min(1)] private int _countLine = 1;
        [SerializeField, Min(0)] private int _betsId;
        [SerializeField] private bool _logFinalVisuals;

        [Tooltip("С какого числа печатать координаты в Debug: 0 (по умолчанию) или 1 и т.п.")]
        [SerializeField] private int _gridIndexBase = 1;

        public SlotVisualData[,] finalVisuals; // собираем ИЗ экрана после стопа
        public IMoneySpend moneySpend;

        /// <summary>
        /// Двумерная матрица ССЫЛОК на реальные видимые элементы:
        /// Elements[x,y], где y=0 — низ, y=2 — верх. Заполняется после остановки.
        /// </summary>
        public SlotElement[,] Elements { get; private set; }

        private int price;

        /// <summary>Матрица ID (в той же ориентации, что Elements): y=0 — низ, y=2 — верх.</summary>
        public int[,] FinalElementIDs
        {
            get
            {
                if (finalVisuals == null) return new int[0, 0];
                int cols = finalVisuals.GetLength(0);
                int rows = finalVisuals.GetLength(1);
                int[,] ids = new int[cols, rows];
                for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    ids[x, y] = finalVisuals[x, y]?.id ?? -1;
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
            _betsId = 0;

            // Инициализируем визуалы рядов (если есть набор)
            if (allSpritesData != null && allSpritesData.visuals != null && allSpritesData.visuals.Length > 0)
            {
                var initial = allSpritesData.visuals[0];
                foreach (var row in _rows) row.SetVisuals(initial);
            }

            SetPrice();
            _lineSlot?.LineActiv(false);
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
            _lineSlot?.LineActiv(false);

            GenerateFinalPlanIds(); // оставляем для вероятностей/отладки

            bool hasSprites = allSpritesData != null && allSpritesData.visuals != null &&
                              allSpritesData.visuals.Length > 0;

            // Стартуем ряды (стаггер по желанию)
            for (int x = 0; x < _rows.Length; x++)
            {
                if (hasSprites) _rows[x].Spin(allSpritesData, null);
                yield return delay;
            }

            // Ждём полного останова ВСЕХ рядов
            yield return new WaitUntil(IsStop);

            // Собираем реальный экран и кэш видимых элементов
            BuildVisibleMatrices();

            // Считаем результат и шлём события
            ProcessSpinResult();
        }

        /// <summary>
        /// Заполняет Elements[x,y] (ССЫЛКИ на видимые элементы) и finalVisuals[x,y] (данные) из экрана.
        /// y=0 низ, y=2 верх.
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
                var row = _rows[x];
                if (row == null)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        Elements[x, y] = null;
                        finalVisuals[x, y] = null;
                    }
                    continue;
                }

                // Row даёт Top→Down три «окна»
                SlotElement[] visibleTopDown = row.GetVisibleTopDown();

                // Записываем Bottom→Top в матрицы (y=0 — низ)
                for (int y = 0; y < rows; y++)
                {
                    var se = visibleTopDown[rows - 1 - y]; // 2..0 ⇒ низ..верх
                    Elements[x, y] = se;

                    SlotVisualData v = null;
                    if (se != null && allSpritesData?.visuals != null && allSpritesData.visuals.Length > 0)
                        v = allSpritesData.visuals.FirstOrDefault(t => t.id == se.id);

                    finalVisuals[x, y] = v;
                }
            }
        }

        /// <summary>
        /// Публичный геттер (получить актуальную матрицу элементов).
        /// Если спин в покое — обновляет из экрана; во время спина вернёт последний кэш.
        /// </summary>
        public SlotElement[,] GetElementsMatrix(bool refreshIfIdle = true)
        {
            if (refreshIfIdle && IsStop()) BuildVisibleMatrices();
            return Elements;
        }

        /// <summary>
        /// Публичный геттер ID-матрицы (в той же ориентации, что Elements): y=0 низ.
        /// </summary>
        public int[,] GetElementIDsMatrix(bool refreshIfIdle = true)
        {
            if (refreshIfIdle && IsStop()) BuildVisibleMatrices();
            return FinalElementIDs;
        }

        /// <summary>
        /// Генерация «плана» id (не форсируем ряды) — для вероятностей win/lose.
        /// </summary>
        private void GenerateFinalPlanIds()
        {
            if (allSpritesData?.visuals == null || allSpritesData.visuals.Length == 0) return;

            int[,] planIds = new int[_rows.Length, 3];
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
                        int[] lines = checkSpin.GetWinningLines(planIds, _countLine);
                        if (lines.Length > 0)
                            checkSpin.SetLose(planIds, lines, totalIdCount, _countLine);
                    }
                }
                catch { /* нет SO — игнор */ }
            }
        }

        private void ProcessSpinResult()
        {
            // Корректный, настраиваемый Debug: координаты печатаем с базой _gridIndexBase (0 или 1 и т.п.)
            if (_logFinalVisuals && finalVisuals != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("--- Final Visuals Table ---");
                int cols = finalVisuals.GetLength(0);
                int rows = finalVisuals.GetLength(1);

                // печать сверху-вниз (визуально как на экране), НО координаты [x+base, y+base]
                for (int y = rows - 1; y >= 0; y--)
                {
                    var parts = new List<string>(cols);
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
                int[,] finalIds = FinalElementIDs; // это ТО, ЧТО НА ЭКРАНЕ (Bottom-Up)
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

        public bool IsStop() => _rows == null || _rows.All(row => !row.is_spinning);

        private void Win(int[] lines, float[] mult)
        {
            _lineSlot?.LineActiv(lines);

            float moneyWin = 0;
            int linePrice = 0;

            if (betsData?.bets != null && betsData.bets.Length > 0 && _betsId >= 0 && _betsId < betsData.bets.Length)
                linePrice = betsData.bets[_betsId];

            foreach (float t in mult) moneyWin += t * linePrice;
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
                if (_betsId < 0 || _betsId >= betsData.bets.Length) _betsId = 0;
                linePrice = betsData.bets[_betsId];
            }

            price = _priceOnLine ? _countLine * linePrice : linePrice;

            if (_textCountLine != null) _textCountLine.text = _countLine.ToString();
            OnChangeBet?.Invoke(price.ToString());

            if (_lineSlot?.lines != null && _lineSlot.lines.Length > 0)
            {
                int[] seq = Enumerable.Range(0, Mathf.Min(_countLine, _lineSlot.lines.Length)).ToArray();
                _lineSlot.LineActiv(seq);
            }
        }

        public void AddLine()
        {
            if (!IsStop()) return;
            _countLine++;
            if (_lineSlot?.lines != null && _countLine > _lineSlot.lines.Length) _countLine = 1;
            SetPrice();
        }

        public void RemoveLine()
        {
            if (!IsStop()) return;
            _countLine--;
            if (_lineSlot?.lines != null && _countLine < 1) _countLine = _lineSlot.lines.Length;
            if (_countLine < 1) _countLine = 1;
            SetPrice();
        }

        public void SetMaxBet()
        {
            if (!IsStop()) return;

            if (betsData?.bets != null && betsData.bets.Length > 0)
                _betsId = betsData.bets.Length - 1;
            else
                _betsId = 0;

            SetPrice();
        }

        public void AddBet()
        {
            if (!IsStop()) return;

            if (betsData?.bets != null && betsData.bets.Length > 0)
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

            if (betsData?.bets != null && betsData.bets.Length > 0)
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
            if (!_setSpace || _rows == null || _rows.Length == 0) return;

            bool isUI = _rows[0].TryGetComponent<RectTransform>(out _);

            for (int i = 0; i < _rows.Length; i++)
            {
                Row row = _rows[i];

                if (i > 0)
                {
                    Row prevRow = _rows[i - 1];
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
