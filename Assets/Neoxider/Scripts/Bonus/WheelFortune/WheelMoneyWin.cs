using Neo.Shop;
using TMPro;
using UnityEngine;

namespace Neo
{
    namespace Bonus
    {
        public class WheelMoneyWin : MonoBehaviour
        {
            public TMP_Text prize;
            public int[] wins = new int[8];

            public void Win(int id)
            {
                Money.I.Add(wins[id]);

                if (prize != null)
                    prize.text = wins[id].ToString();
            }
        }
    }
}