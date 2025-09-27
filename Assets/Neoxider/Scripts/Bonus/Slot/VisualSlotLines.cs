using System;
using UnityEngine;

namespace Neo.Bonus
{
    [Serializable]
    public class VisualSlotLines
    {
        public GameObject[] lines;

        public void LineActiv(int[] idList)
        {
            foreach (var item in lines)
                item.SetActive(false);

            foreach (var id in idList)
                if (id >= 0 && id < lines.Length)
                    lines[id].SetActive(true);
        }

        public void LineActiv(bool activ)
        {
            foreach (var item in lines)
                item.SetActive(activ);
        }
    }
}