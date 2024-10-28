using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neoxider
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(Toggle))]
        public class Toggle : MonoBehaviour
        {
            public bool activ;

            public UnityEvent<bool> OnChange;
            public UnityEvent On;
            public UnityEvent Off;

            [SerializeField] private Button _button;
            [SerializeField] private Sprite[] _sprites;
            [SerializeField] private GameObject[] _visuals;

            public void Switch()
            {
                Set(!activ);
            }

            public void Set(bool activ)
            {
                this.activ = activ;
                Visual(activ);

                Actions(activ);
            }

            private void Actions(bool activ)
            {
                OnChange?.Invoke(activ);

                if (activ)
                    On?.Invoke();
                else
                    Off?.Invoke();
            }

            private void Visual(bool activ)
            {
                int id = activ ? 1 : 0;

                if (_sprites != null && _sprites.Length == 2)
                    _button.image.sprite = _sprites[id];

                if (_visuals != null && _visuals.Length == 2)
                    for (int i = 0; i < _visuals.Length; i++)
                    {
                        _visuals[i].SetActive(id == i);
                    }
            }

            private void OnEnable()
            {
                _button?.onClick.AddListener(Switch);
            }

            private void OnDisable()
            {
                _button?.onClick.RemoveListener(Switch);
            }

            private void OnValidate()
            {
                Visual(activ);

                if (_button == null)
                    _button = GetComponent<Button>();
            }


        }
    }
}