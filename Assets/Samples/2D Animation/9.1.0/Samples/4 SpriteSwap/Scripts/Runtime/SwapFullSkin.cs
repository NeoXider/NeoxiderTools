using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

#if UGUI_ENABLED
using UnityEngine.UI;
#endif

namespace Unity.U2D.Animation.Sample
{
    internal class SwapFullSkin : MonoBehaviour
    {
        public SpriteLibraryAsset[] spriteLibraries;
        public SpriteLibrary spriteLibraryTarget;
        
#if UGUI_ENABLED        
        public Dropdown dropDownSelection;
#endif

        // Start is called before the first frame update
        void Start()
        {
            UpdateSelectionChoice();
        }

        void OnDropDownValueChanged(int value)
        {
            spriteLibraryTarget.spriteLibraryAsset = spriteLibraries[value];
        }

        internal void UpdateSelectionChoice()
        {
#if UGUI_ENABLED            
            dropDownSelection.ClearOptions();
            var options = new List<Dropdown.OptionData>(spriteLibraries.Length);
            for (int i = 0; i < spriteLibraries.Length; ++i)
            {
                options.Add(new Dropdown.OptionData(spriteLibraries[i].name));
            }
            dropDownSelection.options = options;
            dropDownSelection.onValueChanged.AddListener(OnDropDownValueChanged);
#endif
        }
    }

}
