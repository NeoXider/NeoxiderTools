#if UNITY_TMPRO

using TMPro;
using UnityEngine;

namespace Flexalon.Samples
{
    // Implements DataBinding by binding a string to a TMP_Text.
    [DisallowMultipleComponent, AddComponentMenu("Flexalon Samples/Text Data Binding")]
    public class TextDataBinding : MonoBehaviour, DataBinding
    {
        private TMP_Text _text;

        void OnEnable()
        {
            _text = GetComponentInChildren<TMP_Text>();
        }

        public void SetData(object data)
        {
            _text.text = (string) data;
        }
    }
}

#endif