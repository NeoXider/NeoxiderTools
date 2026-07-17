using Neo.Save;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the <b>Neo.Save</b> module. This controller <i>is</i> a
    ///     <see cref="SaveableBehaviour" />: its three fields are tagged with <see cref="SaveField" /> and
    ///     persisted through the real <see cref="SaveManager" /> flow (Register on enable →
    ///     <see cref="SaveManager.Save(MonoBehaviour, bool)" /> / <see cref="SaveManager.Load(MonoBehaviour)" />).
    ///     Values survive across play sessions via the active <see cref="SaveProvider" /> (PlayerPrefs by
    ///     default). Save / Load / Reset+Clear are wired to those exact calls and logged.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Save Demo")]
    public sealed class SaveDemoController : SaveableBehaviour
    {
        private static readonly string[] NamePresets = { "Aurora", "Nova", "Zephyr" };

        [SaveField("Demo_Coins")] [SerializeField] private int _coins;
        [SaveField("Demo_Name")] [SerializeField] private string _playerName = "Rookie";
        [SaveField("Demo_BestTime")] [SerializeField] private float _bestTime;

        private NeoDemoShell.Context _shell;
        private TMP_Text _coinsValue;
        private TMP_Text _nameValue;
        private TMP_Text _timeValue;
        private System.Random _rng;

        private void Start()
        {
            _rng = new System.Random();
            _shell = NeoDemoShell.Build("Neo.Save", new Color(0.26f, 0.85f, 0.64f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Save · SaveManager",
                "This component is a SaveableBehaviour. Fields marked [SaveField(key)] persist through SaveManager.",
                "Change coins / name / best-time, then press Save",
                "Save → SaveManager.Save(this); Load → SaveManager.Load(this)",
                "Stop & re-enter Play — values are restored from the SaveProvider");

            _coinsValue = _shell.AddValueLabel("Coins  (key Demo_Coins)");
            _nameValue = _shell.AddValueLabel("Name  (key Demo_Name)");
            _timeValue = _shell.AddValueLabel("Best time  (key Demo_BestTime)");

            _shell.AddButtonRow(("+10 coins", () => AddCoins(10)), ("+100 coins", () => AddCoins(100)));
            _shell.AddButtonRow(
                ("Name: Aurora", () => SetName(0)),
                ("Nova", () => SetName(1)),
                ("Zephyr", () => SetName(2)));
            _shell.AddButtonRow(("New run time", NewRunTime));
            _shell.AddButtonRow(
                ("Save", DoSave),
                ("Load", DoLoad),
                ("Reset + Clear", DoResetAndClear));

            RefreshLabels();
            _shell.Log($"Component key: {SaveIdentityUtility.GetComponentKey(this)}");
        }

        private void AddCoins(int amount)
        {
            _coins += amount;
            RefreshLabels();
            _shell.Log($"coins = {_coins} (unsaved)");
        }

        private void SetName(int index)
        {
            _playerName = NamePresets[index];
            RefreshLabels();
            _shell.Log($"name = \"{_playerName}\" (unsaved)");
        }

        private void NewRunTime()
        {
            float run = 5f + (float)_rng.NextDouble() * 25f;
            if (_bestTime <= 0f || run < _bestTime)
            {
                _bestTime = run;
                _shell.Log($"new best time {run:0.00}s (unsaved)");
            }
            else
            {
                _shell.Log($"run {run:0.00}s — slower than best {_bestTime:0.00}s");
            }

            RefreshLabels();
        }

        private void DoSave()
        {
            SaveManager.Save(this); // writes this component's [SaveField]s into the shared container
            SaveProvider.Save(); // flush provider (PlayerPrefs) to disk
            _shell.Log("SaveManager.Save(this) + SaveProvider.Save() → persisted");
        }

        private void DoLoad()
        {
            SaveManager.Load(this); // OnDataLoaded() refreshes the labels
            _shell.Log("SaveManager.Load(this) → restored from provider");
        }

        private void DoResetAndClear()
        {
            _coins = 0;
            _playerName = "Rookie";
            _bestTime = 0f;
            SaveManager.Save(this); // overwrite the persisted fields with the reset defaults
            SaveProvider.Save();
            RefreshLabels();
            _shell.Log("reset defaults + SaveManager.Save(this) → cleared on disk");
        }

        /// <summary>Called by <see cref="SaveManager" /> after a load applies the persisted values.</summary>
        public override void OnDataLoaded()
        {
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_coinsValue == null)
            {
                return;
            }

            _coinsValue.text = _coins.ToString();
            _nameValue.text = _playerName;
            _timeValue.text = _bestTime > 0f ? $"{_bestTime:0.00}s" : "—";
        }
    }
}
