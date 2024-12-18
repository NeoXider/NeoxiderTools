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
            [SerializeField] private UnityEngine.UI.Toggle _toggle;
            [SerializeField] private Sprite[] _sprites;
            [SerializeField] private GameObject[] _visuals;

            public bool activ;
            public UnityEvent On;
            public UnityEvent Off;

            private void Start() 
            {
                Set(_toggle.isOn);    
            }

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
                if (activ)
                    On?.Invoke();
                else
                    Off?.Invoke();
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
                _toggle?.onValueChanged.AddListener(Set);
                Actions(_toggle.isOn);
            }

            private void OnDestroy()
            {
                _toggle?.onValueChanged.RemoveListener(Set);
            }

            private void OnValidate()
            {
                if(_toggle != null)
                    activ = _toggle.isOn;
                else
                {
                    _toggle = GetComponent<UnityEngine.UI.Toggle>();
                    _toggle.onValueChanged.AddListener(Set);
                }

                Visual(activ);
            }
        }
    }
}