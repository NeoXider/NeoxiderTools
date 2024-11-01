using Neoxider.Shop;
using TMPro;
using UnityEngine;

namespace Neoxider
{
    namespace Bonus
    {
        public class WheelMoneyWin : MonoBehaviour
        {
            public TMP_Text prize;
            public int[] wins = new int[8];

            public void Win(int id)
            {
                Money.Instance.Add(wins[id]);

                if (prize != null)
                    prize.text = wins[id].ToString();
            }
        }
    }
}
