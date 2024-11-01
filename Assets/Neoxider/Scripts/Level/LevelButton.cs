using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Level
    {
        public class LevelButton : MonoBehaviour
        {
            [SerializeField] private LevelManager _levelManager;
            [SerializeField] private GameObject[] _closes, _opens;
            [SerializeField] private TextMeshProUGUI _textLvl;

            public bool activ;
            public int level;

            [Space]
            public UnityEvent<int> OnChangeVisual;

            [Space]
            public UnityEvent OnDisableVisual;
            public UnityEvent OnEnableVisual;
            public UnityEvent OnCurrentVisual;

            public void Click()
            {
                if (activ)
                    _levelManager.SetLevel(level);
            }

            public void SetLevelManager(LevelManager levelManager)
            {
                _levelManager = levelManager;
            }

            public void SetVisual(int idVisual, int level) // 0 закрыт. 1 открыт 2. текущий
            {
                this.activ = idVisual != 0;
                this.level = level;

                for (int i = 0; i < _closes.Length; i++)
                {
                    _closes[i].SetActive(!activ);
                }

                for (int i = 0; i < _opens.Length; i++)
                {
                    _opens[i].SetActive(activ);
                }

                _textLvl.text = (level + 1).ToString();

                Events(idVisual);
            }

            private void Events(int idVisual)
            {
                OnChangeVisual?.Invoke(idVisual);

                if (idVisual == 0)
                    OnDisableVisual?.Invoke();
                else if (idVisual == 1)
                    OnCurrentVisual?.Invoke();
                else if (idVisual == 2)
                    OnEnableVisual?.Invoke();
            }

            private void OnValidate()
            {

            }
        }
    }
}