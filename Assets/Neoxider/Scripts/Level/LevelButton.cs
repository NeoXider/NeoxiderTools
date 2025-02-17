using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo
{
    namespace Level
    {
        public class LevelButton : MonoBehaviour
        {
            [SerializeField] private Button _button;
            [SerializeField] private LevelManager _levelManager;

            [Space]
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

            private void Start()
            {
                if (_button != null)
                    _button.onClick.AddListener(Click);

            }

            private void OnDestroy()
            {
                if (_button != null)
                    _button.onClick.RemoveListener(Click);
            }

            public void Click()
            {
                if (activ)
                    _levelManager.SetLevel(level);
            }

            public void SetLevelManager(LevelManager levelManager)
            {
                _levelManager = levelManager;
            }

            public void SetVisual(int idVisual, int level) // 0 ������. 1 ������ 2. �������
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

                if(_textLvl != null)
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
                _button ??= GetComponent<Button>();
            }
        }
    }
}