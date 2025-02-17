using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo
{
    namespace UI
    {
        [AddComponentMenu("Neoxider/" + "UI/" + nameof(ToggleView))]
        public class ToggleView : MonoBehaviour
        {
            [SerializeField] private Toggle _toggle;
            [SerializeField] private Sprite[] _sprites;
            [SerializeField] private GameObject[] _visuals;

            public bool activ;
            public UnityEvent On;
            public UnityEvent Off;
            public UnityEvent<bool> OnValueChanged;

            private void Start()
            {
                if(_toggle != null)
                    Set(_toggle.isOn);
            }

            void OnEnable()
            {
                if(_toggle != null)
                    _toggle.isOn = activ;
            }

            public void Switch()
            {
                Set(!activ);
            }

            public void Set(bool activ)
            {
                if (activ != this.activ)
                {
                    this.activ = activ;
                    
                    if(_toggle != null)
                        _toggle.isOn = activ;

                    Actions(activ);
                }

                Visual(activ);
            }

            private void Actions(bool activ)
            {
                if (activ)
                    On?.Invoke();
                else
                    Off?.Invoke();

                OnValueChanged?.Invoke(activ);
            }

            private void Visual(bool activ)
            {
                int id = activ ? 1 : 0;

                if (_sprites != null && _sprites.Length == 2)
                    _toggle.image.sprite = _sprites[id];

                if (_visuals != null && _visuals.Length == 2)
                    for (int i = 0; i < _visuals.Length; i++)
                    {
                        _visuals[i].SetActive(id == i);
                    }
            }

            private void Awake()
            {
                if (_toggle != null)
                {
                    _toggle?.onValueChanged.AddListener(Set);
                    Actions(_toggle.isOn);
                }
            }

            private void OnDestroy()
            {
                if (_toggle != null)
                {
                    _toggle?.onValueChanged.RemoveListener(Set);
                }
            }

            private void OnValidate()
            {
                if (_toggle != null)
                    activ = _toggle.isOn;
                else
                {
                    _toggle = GetComponent<UnityEngine.UI.Toggle>();

                    if (_toggle != null)
                    {
                        _toggle.onValueChanged.AddListener(Set);
                    }
                }

                Visual(activ);
            }
        }
    }
}