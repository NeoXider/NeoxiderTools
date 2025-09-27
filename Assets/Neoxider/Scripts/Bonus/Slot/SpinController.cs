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
        [SerializeField] private CheckSpin _checkSpin = new();
        [SerializeField] private BetsData _betsData;
        [SerializeField] private SpritesData _allSpritesData;

        [Space] [Header("Settings")] [SerializeField] [RequireInterface(typeof(IMoneySpend))]
        private GameObject _moneyGameObject;

        [SerializeField] private bool _priceOnLine = true;
        [SerializeField] private int _countVerticalElements = 3;
        [SerializeField] private Row[] _rows;

        [SerializeField] private bool _isSingleSpeed = true;
        [Range(0f, 1f)] public float chanseWin = 0.5f;

        [Space] [Header("Visual")] [SerializeField]
        private float _delaySpinRoll = 0.2f;

        [SerializeField] private SpeedControll _speedControll = new();
        [SerializeField] private bool _setSpace;
        [SerializeField] private Vector2 _space = Vector2.one;
        [SerializeField] private float offsetY;
        [SerializeField] private VisualSlotLines _lineSlot = new();

        [Space] [Header("Text")] [Space] [SerializeField]
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

        [Space] [Header("Debug")] [SerializeField]
        private bool _firstWin;

        [SerializeField] [Min(1)] private int _countLine = 1;
        [SerializeField] [Min(0)] private int _betsId;

        private SlotElement[,] _elements;

        private SlotVisualData[,] _finalVisuals;
        public IMoneySpend moneySpend;

        private int price;

        public int[,] FinalElementIDs
        {
            get
            {
                if (_finalVisuals == null) return new int[0, 0];

                var rows = _finalVisuals.GetLength(0);
                var cols = _finalVisuals.GetLength(1);
                var ids = new int[rows, cols];

                for (var i = 0; i < rows; i++)
                for (var j = 0; j < cols; j++)
                    ids[i, j] = _finalVisuals[i, j]?.id ?? -1;

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

            if (_allSpritesData != null && _allSpritesData.visuals.Length > 0)
            {
                var initialData = _allSpritesData.visuals[0];
                foreach (var row in _rows) row.SetVisuals(initialData);
            }

            SetPrice();
            _lineSlot.LineActiv(false);
        }

        private void SetElements()
        {
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
            _lineSlot.LineActiv(false);

            GenerateFinalVisuals();

            for (var x = 0; x < _rows.Length; x++)
            {
                var rowVisuals = new SlotVisualData[_countVerticalElements];
                for (var y = 0; y < _countVerticalElements; y++)
                    rowVisuals[y] = _finalVisuals[x, y];

                _rows[x].Spin(_allSpritesData, rowVisuals);
                yield return delay;
            }

            yield return new WaitUntil(IsStop);

            ProcessSpinResult();
        }

        private void GenerateFinalVisuals()
        {
            _finalVisuals = new SlotVisualData[_rows.Length, _countVerticalElements];
            var finalIds = new int[_rows.Length, _countVerticalElements];

            if (_allSpritesData == null || _allSpritesData.visuals.Length == 0) return;

            for (var x = 0; x < _rows.Length; x++)
            for (var y = 0; y < _countVerticalElements; y++)
                finalIds[x, y] = _allSpritesData.visuals[Random.Range(0, _allSpritesData.visuals.Length)].id;

            if (_checkSpin.isActive)
            {
                var totalIdCount = _allSpritesData.visuals.Length;
                if (_firstWin || Random.Range(0f, 1f) < chanseWin)
                {
                    _firstWin = false;
                    _checkSpin.SetWin(finalIds, totalIdCount, _countLine);
                }
                else
                {
                    var winningLines = _checkSpin.GetWinningLines(finalIds, _countLine);
                    if (winningLines.Length > 0)
                        _checkSpin.SetLose(finalIds, winningLines, totalIdCount, _countLine);
                }
            }

            for (var x = 0; x < _rows.Length; x++)
            for (var y = 0; y < _countVerticalElements; y++)
            {
                var id = finalIds[x, y];
                _finalVisuals[x, y] = _allSpritesData.visuals.FirstOrDefault(v => v.id == id);
            }
        }

        private void ProcessSpinResult()
        {
            var hasWon = false;
            if (_checkSpin.isActive)
            {
                var finalIds = FinalElementIDs;
                var lines = _checkSpin.GetWinningLines(finalIds, _countLine);

                if (lines.Length > 0)
                {
                    var mult = _checkSpin.GetMultiplayers(finalIds, _countLine, lines);
                    Win(lines, mult);
                    hasWon = true;
                }
                else
                {
                    Lose();
                }
            }

            OnEndSpin?.Invoke();
            OnEnd?.Invoke(hasWon);
        }

        #region SimpleMethods

        public bool IsStop()
        {
            return _rows.All(row => !row.is_spinning);
        }

        private void Win(int[] lines, float[] mult)
        {
            _lineSlot.LineActiv(lines);
            float moneyWin = 0;

            foreach (var t in mult) moneyWin += t * _betsData.bets[_betsId];

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
            if (_betsData != null)
            {
                var linePrice = _betsData.bets[_betsId];
                price = _priceOnLine ? _countLine * linePrice : linePrice;

                if (_textCountLine != null)
                    _textCountLine.text = _countLine.ToString();

                OnChangeBet?.Invoke(price.ToString());

                var sequence = Enumerable.Range(0, _countLine).ToArray();
                _lineSlot.LineActiv(sequence);
            }
        }

        public void AddLine()
        {
            if (IsStop())
            {
                _countLine++;
                if (_countLine > _lineSlot.lines.Length) _countLine = 1;
                SetPrice();
            }
        }

        public void RemoveLine()
        {
            if (IsStop())
            {
                _countLine--;
                if (_countLine < 1) _countLine = _lineSlot.lines.Length;
                SetPrice();
            }
        }

        public void SetMaxBet()
        {
            if (IsStop())
            {
                _betsId = _betsData.bets.Length - 1;
                SetPrice();
            }
        }

        public void AddBet()
        {
            if (IsStop())
            {
                _betsId++;
                if (_betsId >= _betsData.bets.Length) _betsId = 0;
                SetPrice();
            }
        }

        public void RemoveBet()
        {
            if (IsStop())
            {
                _betsId--;
                if (_betsId < 0) _betsId = _betsData.bets.Length - 1;
                SetPrice();
            }
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
                        rt.anchoredPosition = new Vector2(prevRt.anchoredPosition.x + _space.x, rt.anchoredPosition.y);
                    else
                        row.transform.localPosition = new Vector3(prevRow.transform.localPosition.x + _space.x,
                            row.transform.localPosition.y, row.transform.localPosition.z);
                }

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