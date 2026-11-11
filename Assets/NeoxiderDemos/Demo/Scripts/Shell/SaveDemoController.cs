using System;
using System.Globalization;
using System.IO;
using Neo.Save;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Save</b> demo scene (SaveDemo.unity). It is a
    ///     <see cref="SaveableBehaviour" />: the coins, name and best time fields carry <see cref="SaveField" /> and go
    ///     through <see cref="SaveManager.Save(MonoBehaviour, bool)" /> / <see cref="SaveManager.Load(MonoBehaviour)" />.
    ///     The wallet is a plain key written with <see cref="SaveProvider.SetLong" /> (money in cents above
    ///     <see cref="int.MaxValue" />). The scene's SaveProviderSettingsComponent selects a
    ///     <see cref="FileSaveProvider" />, so the status rows show <see cref="FileSaveProvider.HasUnsavedChanges" /> and
    ///     how many <see cref="SaveProvider.OnDataSaved" /> events really rewrote the file. Buttons call the public
    ///     methods below through persistent UnityEvents; this component builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Save Demo")]
    public sealed class SaveDemoController : SaveableBehaviour
    {
        /// <summary>SaveProvider key of the 64-bit wallet (cents).</summary>
        public const string WalletKey = "Demo_WalletCents";

        private const string DefaultName = "Rookie";

        private static readonly string[] NamePresets = { "Aurora", "Nova", "Zephyr" };
        private static readonly string GoodHex = "#" + ColorUtility.ToHtmlStringRGB(DemoPalette.Good);
        private static readonly string WarmHex = "#" + ColorUtility.ToHtmlStringRGB(DemoPalette.Warm);

        [SaveField("Demo_Coins")] [SerializeField] private int _coins;
        [SaveField("Demo_Name")] [SerializeField] private string _playerName = DefaultName;
        [SaveField("Demo_BestTime")] [SerializeField] private float _bestTime;

        [Header("[SaveField] values")]
        [SerializeField] private TMP_Text _coinsValue;
        [SerializeField] private TMP_Text _nameValue;
        [SerializeField] private TMP_Text _timeValue;

        [Header("SaveProvider · 64-bit wallet")]
        [SerializeField] private TMP_Text _walletValue;
        [SerializeField] private TMP_Text _walletNote;

        [Header("Provider status")]
        [SerializeField] private TMP_Text _providerValue;
        [SerializeField] private TMP_Text _unsavedValue;
        [SerializeField] private TMP_Text _savedCountValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private System.Random _rng;
        private long _walletCents;
        private int _savedEvents;
        private int _fileWrites;
        private DateTime _lastWriteUtc;
        private bool _subscribed;
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _log != null && _coinsValue != null && _nameValue != null && _timeValue != null &&
                                    _walletValue != null && _providerValue != null && _unsavedValue != null &&
                                    _savedCountValue != null;

        public int Coins => _coins;
        public string PlayerName => _playerName;
        public float BestTime => _bestTime;
        public long WalletCents => _walletCents;

        /// <summary><see cref="SaveProvider.OnDataSaved" /> events seen since the scene started.</summary>
        public int SavedEvents => _savedEvents;

        /// <summary>Saves that changed the save file on disk (always 0 for non-file providers).</summary>
        public int FileWrites => _fileWrites;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!_subscribed)
            {
                SaveProvider.OnDataSaved += HandleDataSaved;
                SaveProvider.OnKeyChanged += HandleKeyChanged;
                _subscribed = true;
            }
        }

        protected override void OnDisable()
        {
            if (_subscribed)
            {
                SaveProvider.OnDataSaved -= HandleDataSaved;
                SaveProvider.OnKeyChanged -= HandleKeyChanged;
                _subscribed = false;
            }

            base.OnDisable();
        }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            _rng = new System.Random();
            // WHY: SaveManager may load in its Awake before SaveProviderSettingsComponent switched the provider; load
            // again from the active provider once every Awake has run.
            SaveManager.Load(this);
            _walletCents = SaveProvider.GetLong(WalletKey);
            _lastWriteUtc = ReadFileStamp(out _);
            RefreshValues();
            RefreshStatus();
            Log("Loaded from " + DescribeProvider() + " · component key " + SaveIdentityUtility.GetComponentKey(this));
        }

        /// <summary>Button target: adds coins to the [SaveField] counter (not saved yet).</summary>
        public void AddCoins(int amount)
        {
            _coins += amount;
            RefreshValues();
            Log("coins = " + _coins + " (unsaved field)");
        }

        /// <summary>Button target: picks one of the preset names by index.</summary>
        public void SetName(int index)
        {
            if (index < 0 || index >= NamePresets.Length)
            {
                Log("Ignored unknown name preset " + index);
                return;
            }

            _playerName = NamePresets[index];
            RefreshValues();
            Log("name = \"" + _playerName + "\" (unsaved field)");
        }

        /// <summary>Button target: rolls a run time and keeps it when it beats the best time.</summary>
        public void NewRunTime()
        {
            _rng ??= new System.Random();
            float run = 5f + (float)_rng.NextDouble() * 25f;
            if (_bestTime <= 0f || run < _bestTime)
            {
                _bestTime = run;
                Log("new best time " + FormatSeconds(run) + " (unsaved field)");
            }
            else
            {
                Log("run " + FormatSeconds(run) + " · slower than best " + FormatSeconds(_bestTime));
            }

            RefreshValues();
        }

        /// <summary>Button target: adds whole dollars to the 64-bit wallet and stages it with SaveProvider.SetLong.</summary>
        public void AddWalletDollars(int dollars)
        {
            long cents = dollars * 100L;
            _walletCents = cents > 0L && _walletCents > long.MaxValue - cents ? long.MaxValue : _walletCents + cents;
            SaveProvider.SetLong(WalletKey, _walletCents);
            RefreshValues();
            RefreshStatus();
            Log("SaveProvider.SetLong(\"" + WalletKey + "\", " + _walletCents.ToString("N0", CultureInfo.InvariantCulture) +
                ") · staged, not on disk yet");
        }

        /// <summary>Button target: writes the [SaveField] values into the shared container and flushes the provider.</summary>
        public void SaveToDisk()
        {
            Log("SaveManager.Save(this) + SaveProvider.Save()");
            SaveManager.Save(this);
            SaveProvider.Save();
            RefreshStatus();
        }

        /// <summary>Button target: re-reads the backing store (dropping staged values) and restores every value.</summary>
        public void LoadFromDisk()
        {
            SaveProvider.Load();
            SaveManager.Load(this);
            _walletCents = SaveProvider.GetLong(WalletKey);
            RefreshValues();
            RefreshStatus();
            Log("SaveProvider.Load() + SaveManager.Load(this) -> restored what is on disk");
        }

        /// <summary>Button target: resets every value and persists the defaults.</summary>
        public void ResetAndClear()
        {
            _coins = 0;
            _playerName = DefaultName;
            _bestTime = 0f;
            _walletCents = 0L;
            Log("reset defaults + DeleteKey(\"" + WalletKey + "\") + SaveManager.Save(this) + SaveProvider.Save()");
            SaveProvider.DeleteKey(WalletKey);
            SaveManager.Save(this);
            SaveProvider.Save();
            RefreshValues();
            RefreshStatus();
        }

        /// <summary>Called by <see cref="SaveManager" /> after a load applied the persisted values.</summary>
        public override void OnDataLoaded()
        {
            RefreshValues();
        }

        private void HandleDataSaved()
        {
            _savedEvents++;
            DateTime stamp = ReadFileStamp(out bool isFile);
            if (!isFile)
            {
                Log("SaveProvider.OnDataSaved -> " + SaveProvider.CurrentProvider.ProviderType + " flushed");
                RefreshStatus();
                return;
            }

            bool written = stamp != _lastWriteUtc;
            if (written)
            {
                _fileWrites++;
                _lastWriteUtc = stamp;
            }

            RefreshStatus();
            Log(written
                ? "SaveProvider.OnDataSaved -> file written"
                : "SaveProvider.OnDataSaved -> nothing changed, file write skipped");
        }

        private void HandleKeyChanged(string key)
        {
            RefreshStatus();
        }

        private void RefreshValues()
        {
            if (_coinsValue != null)
            {
                _coinsValue.text = _coins.ToString(CultureInfo.InvariantCulture);
            }

            if (_nameValue != null)
            {
                _nameValue.text = _playerName;
            }

            if (_timeValue != null)
            {
                _timeValue.text = _bestTime > 0f ? FormatSeconds(_bestTime) : "-";
            }

            if (_walletValue != null)
            {
                _walletValue.text = "$" + (_walletCents / 100m).ToString("N2", CultureInfo.InvariantCulture);
            }

            if (_walletNote != null)
            {
                string cents = _walletCents.ToString("N0", CultureInfo.InvariantCulture) + " cents";
                _walletNote.text = _walletCents > int.MaxValue
                    ? cents + " · <color=" + GoodHex + ">above int.MaxValue</color>"
                    : cents + " · key " + WalletKey;
            }
        }

        private void RefreshStatus()
        {
            ISaveProvider provider = SaveProvider.CurrentProvider;
            FileSaveProvider file = provider as FileSaveProvider;
            if (_providerValue != null)
            {
                _providerValue.text = DescribeProvider();
            }

            if (_unsavedValue != null)
            {
                _unsavedValue.text = file == null
                    ? "n/a"
                    : file.HasUnsavedChanges
                        ? "<color=" + WarmHex + ">YES</color> · press Save"
                        : "<color=" + GoodHex + ">no</color> · up to date";
            }

            if (_savedCountValue != null)
            {
                _savedCountValue.text = file == null
                    ? _savedEvents + " events"
                    : _savedEvents + " events · " + _fileWrites + " writes";
            }
        }

        private static string DescribeProvider()
        {
            ISaveProvider provider = SaveProvider.CurrentProvider;
            return provider is FileSaveProvider file
                ? "File · " + Path.GetFileName(file.FilePath)
                : provider.ProviderType.ToString();
        }

        private static DateTime ReadFileStamp(out bool isFile)
        {
            FileSaveProvider file = SaveProvider.CurrentProvider as FileSaveProvider;
            isFile = file != null;
            if (file == null)
            {
                return DateTime.MinValue;
            }

            try
            {
                return File.Exists(file.FilePath) ? File.GetLastWriteTimeUtc(file.FilePath) : DateTime.MinValue;
            }
            catch (Exception)
            {
                // WHY: a platform without file timestamps only loses the write counter, never the demo.
                return DateTime.MinValue;
            }
        }

        private static string FormatSeconds(float seconds)
        {
            return seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s";
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
                "[SaveDemoController] The scene UI is not authored. Re-import the scene from the Demo download.", this);
        }
    }
}
