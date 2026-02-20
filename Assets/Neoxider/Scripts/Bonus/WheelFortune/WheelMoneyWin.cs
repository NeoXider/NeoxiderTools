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

            public void Win(int id)
            {
                if (wins == null || id < 0 || id >= wins.Length)
                {
                    if (wins != null)
                        Debug.LogWarning($"[WheelMoneyWin] Win(id={id}) out of range [0, {wins.Length}).", this);
                    return;
                }

                Money.I.Add(wins[id]);

                if (prize != null)
                    prize.text = wins[id].ToString();
            }
        }
    }
}