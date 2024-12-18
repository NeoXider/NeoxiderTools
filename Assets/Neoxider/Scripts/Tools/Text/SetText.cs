using Neoxider;
using TMPro;
using UnityEngine;


namespace Neoxider
{
    namespace Tools
    {
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SetText))]
        public class SetText : MonoBehaviour
        {
            [SerializeField]
            private TMP_Text _text;

            [SerializeField]
            private string _separator = ".";

            [SerializeField]
            private int _decimal = 2;

            public string startAdd = "";
            public string endAdd = "";


            public void Set(int value)
            {
                Set(value.FormatWithSeparator(_separator));
            }

            public void Set(float value)
            {
                Set(value.FormatWithSeparator(_separator, _decimal));
            }

            public void Set(string value)
            {
                string text = startAdd + value + endAdd;
                _text.text = text;
            }

            private void OnValidate()
            {
                if (_text == null)
                {
                    _text = GetComponent<TMP_Text>();
                }
            }
        }
    }
}