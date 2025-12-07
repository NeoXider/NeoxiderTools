#if UNITY_TMPRO

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Flexalon.Samples
{
    // Provides the text of an TMP_InputField as a data source which can be assigned to a FlexalonCloner.
    [AddComponentMenu("Flexalon Samples/Input Field Data Source")]
    public class InputFieldDataSource : MonoBehaviour, DataSource
    {
        [SerializeField]
        private TMP_InputField _inputField;
        public TMP_InputField InputField
        {
            get => _inputField;
            set
            {
                _inputField = value;
                UpdateData(_inputField.text);
            }
        }

        public event Action DataChanged;

        private List<string> _data = new List<string>();
        public IReadOnlyList<object> Data => _data;

        void OnEnable()
        {
            _inputField.onValueChanged.AddListener(UpdateData);
            UpdateData(_inputField.text);
        }

        void OnDisable()
        {
            _inputField.onValueChanged.RemoveListener(UpdateData);
        }

        private void UpdateData(string text)
        {
            _data.Clear();
            foreach (char c in text)
            {
                _data.Add(c.ToString());
            }

            DataChanged?.Invoke();
        }
    }
}

#endif