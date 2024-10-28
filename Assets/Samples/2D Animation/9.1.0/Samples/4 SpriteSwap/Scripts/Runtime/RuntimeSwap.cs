using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Unity.U2D.Animation.Sample
{
    internal class RuntimeSwap : MonoBehaviour
    {
        [Serializable]
        class SwapEntry
        {
            public Sprite sprite = null;
            public string category = "";
            public string entry = "";
        }
        
        [Serializable]
        class SwapGroup
        {
            public SwapEntry[] swapEntries = null;
        }

        [SerializeField]
        SwapGroup[] m_SwapGroup = null;

        [SerializeField]
        SpriteLibrary m_SpriteLibraryTarget = null;
        
        public void OverrideEntry(int i)
        {
            if (m_SwapGroup == null || m_SwapGroup.Length < i)
                return;
            foreach (var entry in m_SwapGroup[i].swapEntries)
            {
                m_SpriteLibraryTarget.AddOverride(entry.sprite, entry.category, entry.entry);   
            }
        }

        public void ResetEntry(int i)
        {
            if (m_SwapGroup == null || m_SwapGroup.Length < i)
                return;
            foreach (var entry in m_SwapGroup[i].swapEntries)
            {
                m_SpriteLibraryTarget.RemoveOverride(entry.category, entry.entry);    
            }
        }
    }
}
