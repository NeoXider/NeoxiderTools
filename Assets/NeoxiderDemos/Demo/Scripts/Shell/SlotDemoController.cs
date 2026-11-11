using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Neo.Bonus;
using Neo.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Bonus</b> slot machine demo (SlotExample.unity). The scene
    ///     saves the frame, the wallet and every listener in the scene: the bet and payline buttons call
    ///     <see cref="SpinController" /> directly, <see cref="SpinController.OnChangeBet" /> and
    ///     <see cref="SpinController.OnChangeMoneyWin" /> write their labels through <c>TMP_Text.SetText</c>, and the
    ///     coin label is listed in the <see cref="Money" /> wallet's text targets. This component only adds the glue a
    ///     game would write in C#: a funds check before a spin, a forced winning spin, the win-chance slider and the
    ///     log lines. It builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Slot Machine Demo")]
    public sealed class SlotDemoController : MonoBehaviour
    {
        private const string GoodColor = "#42D9A3";
        private const string MutedColor = "#9EA3BD";

        [Header("Module")]
        [Tooltip("Slot machine of the scene (the SlotUI prefab instance).")]
        [SerializeField] private SpinController _slot;

        [Tooltip("Wallet the machine charges bets from and pays wins into.")]
        [SerializeField] private Money _wallet;

        [Tooltip("Balance the wallet starts with on every Play (the demo wallet does not persist).")]
        [SerializeField] [Min(0)] private int _startingCoins = 500;

        [Tooltip("Symbol id that Force a win puts on payline 1.")]
        [SerializeField] [Min(0)] private int _forcedWinSymbol = 3;

        [Header("Controls synced on start")]
        [SerializeField] private Slider _chanceSlider;

        [Header("Live values")]
        [SerializeField] private TMP_Text _resultValue;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private readonly StringBuilder _lineText = new();
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _slot != null && _wallet != null && _resultValue != null && _log != null;

        public SpinController Slot => _slot;
        public Money Wallet => _wallet;
        public int StartingCoins => _startingCoins;

        /// <summary>Winning spins completed since Play started.</summary>
        public int Wins { get; private set; }

        /// <summary>Losing spins completed since Play started.</summary>
        public int Losses { get; private set; }

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_wallet != null)
            {
                _wallet.SetCurrentMoney(_startingCoins);
            }

            if (_chanceSlider != null && _slot != null)
            {
                _chanceSlider.SetValueWithoutNotify(_slot.ChanceWin);
            }

            SetResult("press SPIN", MutedColor);
            Log("Wallet holds " + _startingCoins + " coins - pick a bet and paylines, then SPIN");
        }

        /// <summary>Button target: starts a spin after checking that the wallet can pay the current price.</summary>
        public void Spin()
        {
            if (!CanStartSpin())
            {
                return;
            }

            _slot.StartSpin();
            if (!_slot.IsSpinInProgress)
            {
                Log("StartSpin() was refused by the machine");
            }
        }

        /// <summary>
        ///     Button target: queues a winning outcome on payline 1 with
        ///     <see cref="SpinController.ForceNextOutcomeOnPayline" /> and spins (the code path for scripted wins,
        ///     tutorials and tests).
        /// </summary>
        public void ForceWin()
        {
            if (!CanStartSpin())
            {
                return;
            }

            if (!_slot.ForceNextOutcomeOnPayline(0, _forcedWinSymbol))
            {
                Log("ForceNextOutcomeOnPayline(0, " + _forcedWinSymbol + ") rejected: check the symbol id and paylines");
                return;
            }

            _slot.StartSpin();
            if (!_slot.IsSpinInProgress)
            {
                // WHY: a refused spin must not leave the forced outcome queued for the player's next normal spin.
                _slot.ForceNextOutcome(null);
                Log("StartSpin() was refused; the forced outcome was cleared");
                return;
            }

            Log("ForceNextOutcomeOnPayline(0, " + _forcedWinSymbol + ") + StartSpin(): payline 1 will win");
        }

        /// <summary>Slider target: sets <see cref="SpinController.ChanceWin" /> (the slider label shows the value).</summary>
        public void SetChanceWin(float chance)
        {
            if (_slot != null)
            {
                _slot.ChanceWin = chance;
            }
        }

        /// <summary><see cref="SpinController.OnStartSpin" /> listener: the bet is already charged at this point.</summary>
        public void OnSpinStarted()
        {
            SetResult("spinning...", MutedColor);
            if (_slot == null)
            {
                return;
            }

            Log("StartSpin() paid " + _slot.CurrentSpinPrice + " coins (bet " + CurrentLineBet() + " × " +
                _slot.ActivePaylineCount + " paylines)");
        }

        /// <summary><see cref="SpinController.OnWin" /> listener; Auto Payout has already credited the wallet.</summary>
        public void OnSpinWon(int payout)
        {
            Wins++;
            SetResult("WIN +" + payout, GoodColor);
            string lines = _slot != null ? FormatPaylines(_slot.LastWinningPaylineIndices) : "?";
            Log("OnWin(" + payout + ") on payline " + lines + " - Auto Payout added " + payout + " coins");
        }

        /// <summary><see cref="SpinController.OnLose" /> listener.</summary>
        public void OnSpinLost()
        {
            Losses++;
            SetResult("no win", MutedColor);
            Log("OnLose(): no active payline matched");
        }

        private bool CanStartSpin()
        {
            if (_slot == null)
            {
                WarnUnbuilt();
                return false;
            }

            if (_slot.IsSpinInProgress)
            {
                Log("The reels are still turning (IsSpinInProgress)");
                return false;
            }

            int price = _slot.CurrentSpinPrice;
            if (price > 0 && _wallet != null && _wallet.money < price)
            {
                Log("Not enough coins for a " + price + "-coin spin: press +100 coins or lower the bet");
                return false;
            }

            return true;
        }

        private int CurrentLineBet()
        {
            int[] bets = _slot.betsData != null ? _slot.betsData.bets : null;
            int index = _slot.BetSelectionIndex;
            return bets != null && index >= 0 && index < bets.Length ? bets[index] : 0;
        }

        private string FormatPaylines(IReadOnlyList<int> lines)
        {
            _lineText.Clear();
            if (lines == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    _lineText.Append(", ");
                }

                _lineText.Append((lines[i] + 1).ToString(CultureInfo.InvariantCulture));
            }

            return _lineText.ToString();
        }

        private void SetResult(string text, string color)
        {
            if (_resultValue != null)
            {
                _resultValue.text = "<color=" + color + ">" + text + "</color>";
            }
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
                "[SlotDemoController] The scene is not authored. Re-import the scene from the Demo download.", this);
        }
    }
}
