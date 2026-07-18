using Neo.Shop;
using TMPro;
using UnityEngine;

namespace Neo
{
    namespace Bonus
    {
        [NeoDoc("Bonus/WheelFortune/WheelMoneyWin.md")]
        [CreateFromMenu("Neoxider/Bonus/WheelMoneyWin")]
        [AddComponentMenu("Neoxider/" + "Bonus/" + nameof(WheelMoneyWin))]
        public class WheelMoneyWin : MonoBehaviour
        {
            [Header("References")] public TMP_Text prize;

            [Header("Settings")] public int[] wins = new int[8];
            [SerializeField] private bool _debugLogWarnings;

            public void Win(int id)
            {
                if (wins == null || id < 0 || id >= wins.Length)
                {
                    if (_debugLogWarnings && wins != null)
                    {
                        NeoDiagnostics.LogWarning(
                            $"[WheelMoneyWin] Win(id={id}) out of range [0, {wins.Length}).",
                            this,
                            true);
                    }

                    return;
                }

                if (Money.I != null)
                {
                    Money.I.Add(wins[id]);
                }
                else if (_debugLogWarnings)
                {
                    NeoDiagnostics.LogWarning(
                        "[WheelMoneyWin] No Money wallet in the scene — prize amount not deposited.",
                        this,
                        true);
                }

                if (prize != null)
                {
                    prize.text = wins[id].ToString();
                }
            }
        }
    }
}
