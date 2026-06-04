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
            if (lines == null || lines.Length == 0)
            {
                return;
            }

            foreach (GameObject item in lines)
            {
                if (item != null)
                {
                    item.SetActive(false);
                }
            }

            if (idList == null)
            {
                return;
            }

            foreach (int id in idList)
            {
                if (id >= 0 && id < lines.Length && lines[id] != null)
                {
                    lines[id].SetActive(true);
                }
            }
        }

        public void LineActiv(bool activ)
        {
            if (lines == null)
            {
                return;
            }

            foreach (GameObject item in lines)
            {
                if (item != null)
                {
                    item.SetActive(activ);
                }
            }
        }
    }
}
