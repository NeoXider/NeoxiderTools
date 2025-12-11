using Neo.Shop;
using TMPro;
using UnityEngine;

namespace Neo
{
    namespace Bonus
    {
        [AddComponentMenu("Neo/" + "Bonus/" + nameof(WheelMoneyWin))]
        public class WheelMoneyWin : MonoBehaviour
        {
            [Header("References")] public TMP_Text prize;

            [Header("Settings")] public int[] wins = new int[8];

            public void Win(int id)
            {
                Money.I.Add(wins[id]);

                if (prize != null)
                {
                    prize.text = wins[id].ToString();
                }
            }
        }
    }
}