using System.Globalization;
using System.Text;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Tools.Random</b> demo (ChanceSystemExample.unity). The NoCode half of
    ///     the scene - the Roll button, the rarity highlight, "Last drop", "Index" and the roll counter - is wired
    ///     with persistent UnityEvents on the scene <see cref="ChanceSystemBehaviour" />; its OnIdGenerated event
    ///     also calls <see cref="RecordRoll" /> here. This component runs the C# paths
    ///     (<see cref="ChanceManager.TryEvaluate(out int, out ChanceManager.Entry)" /> and
    ///     <see cref="IntegerChanceTable.Roll" />), mirrors observed versus expected odds into the authored bars
    ///     and logs. It builds no UI at runtime; everything it touches is a serialized scene reference.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Chance Demo")]
    public sealed class ChanceDemoController : MonoBehaviour
    {
        [Header("Module")]
        [Tooltip("Scene chance table: rolled by the NoCode Roll button and sampled by 'Roll x100 in code'.")]
        [SerializeField] private ChanceSystemBehaviour _chance;

        [Tooltip("Exact integer odds rolled by the Draw buttons.")]
        [SerializeField] private IntegerChanceTable _exactOdds = new();

        [Header("Odds vs results (one per table entry)")]
        [SerializeField] private TMP_Text[] _tileOdds = new TMP_Text[0];
        [SerializeField] private TMP_Text[] _oddsCaptions = new TMP_Text[0];
        [SerializeField] private Image[] _observedFills = new Image[0];
        [SerializeField] private RectTransform[] _expectedMarkers = new RectTransform[0];
        [SerializeField] private TMP_Text _sampleValue;

        [Header("Exact odds (one per IntegerChanceTable entry)")]
        [SerializeField] private TMP_Text[] _exactCaptions = new TMP_Text[0];
        [SerializeField] private TMP_Text[] _exactHits = new TMP_Text[0];
        [SerializeField] private TMP_Text _ticketsValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private readonly StringBuilder _line = new(128);
        private long[] _rolls = new long[0];
        private long _sampleSize;
        private long[] _exactCounts = new long[0];
        private long _tickets;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _chance != null && _log != null && _sampleValue != null && _ticketsValue != null &&
                                    _observedFills.Length > 0 && _exactHits.Length > 0;

        /// <summary>Scene chance component this demo rolls.</summary>
        public ChanceSystemBehaviour Chance => _chance;

        /// <summary>Exact integer odds rolled by <see cref="DrawTickets" />.</summary>
        public IntegerChanceTable ExactOdds => _exactOdds;

        /// <summary>Rolls recorded per entry (NoCode rolls and code samples together).</summary>
        public long GetRollCount(int index)
        {
            return index >= 0 && index < _rolls.Length ? _rolls[index] : 0;
        }

        /// <summary>Total rolls recorded since the last <see cref="ResetStats" />.</summary>
        public long SampleSize => _sampleSize;

        /// <summary>Tickets drawn from <see cref="ExactOdds" /> since the last <see cref="ResetStats" />.</summary>
        public long TicketsDrawn => _tickets;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            EnsureBuffers();
            RefreshExpected();
            RefreshObserved();
            RefreshExact();
            Log("Roll with the NoCode button or sample the same table from C#");
        }

        /// <summary>
        ///     OnIdGenerated listener of the scene ChanceSystemBehaviour: counts the NoCode roll and logs it.
        /// </summary>
        public void RecordRoll(int index)
        {
            EnsureBuffers();
            if (index < 0 || index >= _rolls.Length)
            {
                Log("OnIdGenerated(" + index + ") - the table has no selectable entry");
                return;
            }

            _rolls[index]++;
            _sampleSize++;
            RefreshObserved();
            _line.Clear();
            _line.Append("Roll #").Append(_sampleSize.ToString(CultureInfo.InvariantCulture)).Append(" -> ")
                .Append(EntryLabel(index)).Append(" (").Append(Percent(ExpectedShare(index)))
                .Append(") · OnIdGenerated(").Append(index).Append(')');
            Log(_line.ToString());
        }

        /// <summary>Button target: samples the same table from C# without raising its UnityEvents.</summary>
        public void RollInCode(int count)
        {
            if (_chance == null)
            {
                WarnUnbuilt();
                return;
            }

            EnsureBuffers();
            ChanceManager manager = _chance.Manager;
            int rolled = 0;
            for (int i = 0; i < count; i++)
            {
                if (manager.TryEvaluate(out int index, out ChanceManager.Entry _) && index < _rolls.Length)
                {
                    _rolls[index]++;
                    rolled++;
                }
            }

            _sampleSize += rolled;
            RefreshObserved();
            _line.Clear();
            _line.Append("ChanceManager.TryEvaluate × ").Append(count.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" -> sample ").Append(_sampleSize.ToString("N0", CultureInfo.InvariantCulture));
            Log(_line.ToString());
        }

        /// <summary>Button target: draws exact tickets from <see cref="ExactOdds" />.</summary>
        public void DrawTickets(int count)
        {
            EnsureBuffers();
            for (int i = 0; i < count; i++)
            {
                int index = _exactOdds.Roll();
                if (index >= 0 && index < _exactCounts.Length)
                {
                    _exactCounts[index]++;
                }
            }

            _tickets += count;
            RefreshExact();
            _line.Clear();
            _line.Append("IntegerChanceTable.Roll() × ").Append(count.ToString("N0", CultureInfo.InvariantCulture));
            if (_exactCounts.Length > 0)
            {
                _line.Append(" -> ").Append(ExactLabel(0)).Append(' ')
                    .Append(_exactCounts[0].ToString("N0", CultureInfo.InvariantCulture)).Append(" so far");
            }

            Log(_line.ToString());
        }

        /// <summary>Button target: clears the recorded rolls and drawn tickets.</summary>
        public void ResetStats()
        {
            EnsureBuffers();
            System.Array.Clear(_rolls, 0, _rolls.Length);
            System.Array.Clear(_exactCounts, 0, _exactCounts.Length);
            _sampleSize = 0;
            _tickets = 0;
            RefreshObserved();
            RefreshExact();
            Log("Stats reset · Counter.Set(0)");
        }

        private void EnsureBuffers()
        {
            int entries = _chance != null ? _chance.Manager.Count : 0;
            if (_rolls.Length != entries)
            {
                _rolls = new long[entries];
                _sampleSize = 0;
            }

            int exact = _exactOdds != null ? _exactOdds.Count : 0;
            if (_exactCounts.Length != exact)
            {
                _exactCounts = new long[exact];
                _tickets = 0;
            }
        }

        private void RefreshExpected()
        {
            for (int i = 0; i < _rolls.Length; i++)
            {
                float expected = ExpectedShare(i);
                if (i < _tileOdds.Length && _tileOdds[i] != null)
                {
                    _tileOdds[i].text = Percent(expected);
                }

                if (i < _expectedMarkers.Length && _expectedMarkers[i] != null)
                {
                    RectTransform marker = _expectedMarkers[i];
                    marker.anchorMin = new Vector2(expected, 0f);
                    marker.anchorMax = new Vector2(expected, 1f);
                }
            }
        }

        private void RefreshObserved()
        {
            for (int i = 0; i < _rolls.Length; i++)
            {
                float observed = _sampleSize > 0 ? (float)((double)_rolls[i] / _sampleSize) : 0f;
                if (i < _observedFills.Length && _observedFills[i] != null)
                {
                    RectTransform fill = _observedFills[i].rectTransform;
                    fill.anchorMax = new Vector2(Mathf.Clamp01(observed), 1f);
                }

                if (i < _oddsCaptions.Length && _oddsCaptions[i] != null)
                {
                    _line.Clear();
                    _line.Append(EntryLabel(i)).Append(" · expected ").Append(Percent(ExpectedShare(i)))
                        .Append(" · observed ").Append(_sampleSize > 0 ? Percent(observed) : "-");
                    _oddsCaptions[i].SetText(_line);
                }
            }

            if (_sampleValue != null)
            {
                _sampleValue.text = _sampleSize.ToString("N0", CultureInfo.InvariantCulture) + " rolls";
            }
        }

        private void RefreshExact()
        {
            long total = _exactOdds != null ? _exactOdds.TotalWeight : 0;
            for (int i = 0; i < _exactCounts.Length; i++)
            {
                _exactOdds.TryGetOdds(i, out long weight, out long _);
                if (i < _exactCaptions.Length && _exactCaptions[i] != null)
                {
                    _line.Clear();
                    _line.Append(ExactLabel(i)).Append(" · ").Append(weight.ToString("N0", CultureInfo.InvariantCulture))
                        .Append(" in ").Append(total.ToString("N0", CultureInfo.InvariantCulture));
                    _exactCaptions[i].SetText(_line);
                }

                if (i < _exactHits.Length && _exactHits[i] != null)
                {
                    double expected = total > 0 ? (double)_tickets * weight / total : 0d;
                    _line.Clear();
                    _line.Append(_exactCounts[i].ToString("N0", CultureInfo.InvariantCulture)).Append(" (exp. ")
                        .Append(expected.ToString(expected < 10d ? "0.00" : "N0", CultureInfo.InvariantCulture))
                        .Append(')');
                    _exactHits[i].SetText(_line);
                }
            }

            if (_ticketsValue != null)
            {
                _ticketsValue.text = _tickets.ToString("N0", CultureInfo.InvariantCulture);
            }
        }

        private float ExpectedShare(int index)
        {
            return _chance != null ? _chance.GetNormalizedWeight(index) : 0f;
        }

        private string EntryLabel(int index)
        {
            ChanceManager.Entry entry = _chance != null ? _chance.Manager.GetEntry(index) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Label) ? entry.Label : "#" + index;
        }

        private string ExactLabel(int index)
        {
            if (_exactOdds == null || index < 0 || index >= _exactOdds.Count)
            {
                return "#" + index;
            }

            string label = _exactOdds.Entries[index].Label;
            return string.IsNullOrEmpty(label) ? "#" + index : label;
        }

        private static string Percent(float share)
        {
            return share.ToString(share > 0f && share < 0.1f ? "0.0%" : "0%", CultureInfo.InvariantCulture);
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[ChanceDemoController] The scene UI is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.",
                this);
        }
    }
}
