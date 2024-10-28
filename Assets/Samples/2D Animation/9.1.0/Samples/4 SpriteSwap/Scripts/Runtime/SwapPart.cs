using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

#if UGUI_ENABLED
using UnityEngine.UI;
#endif

namespace Unity.U2D.Animation.Sample
{
    [Serializable]
    internal struct SwapOptionData
    {
#if UGUI_ENABLED        
        public Dropdown dropdown;
#endif
        public SpriteResolver spriteResolver;
        public string category;
    }

    internal class SwapPart : MonoBehaviour
    {
        public SpriteLibrary spriteLibrary;
        public SwapOptionData[] swapOptionData;
        
        void Start()
        {
#if UGUI_ENABLED            
            foreach (var swapOption in swapOptionData)
            {
                swapOption.dropdown.ClearOptions();
                var libraryAsset = spriteLibrary.spriteLibraryAsset;
                var labels = libraryAsset.GetCategoryLabelNames(swapOption.category);
                var dropDownOption = new List<Dropdown.OptionData>(labels.Count());
                foreach (var label in labels)
                {
                    dropDownOption.Add(new Dropdown.OptionData(label));
                }
                swapOption.dropdown.options = dropDownOption;
                swapOption.dropdown.onValueChanged.AddListener((x)=>
                {
                    swapOption.spriteResolver.SetCategoryAndLabel(swapOption.category, swapOption.dropdown.options[x].text);
                });
            }
#endif
        }
    }

}
