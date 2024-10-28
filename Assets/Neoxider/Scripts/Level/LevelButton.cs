using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    namespace Level
    {
        public class LevelButton : MonoBehaviour
        {
            [SerializeField] private LevelManager _levelManager;
            [SerializeField] private GameObject _close;
            [SerializeField] private TextMeshProUGUI _textLvl;

            public bool activ;
            public int level;

            public UnityEvent<int> OnChangeVisual;

            public void Click()
            {
                if (activ)
                    _levelManager.SetLevel(level);
            }

            public void SetVisual(int idVisual, int level) // 0 закрыт. 1 открыт 2. текущий
            {
                this.activ = idVisual != 0;
                this.level = level;

                _close.SetActive(!activ);

                _textLvl.text = (level + 1).ToString();

                OnChangeVisual?.Invoke(idVisual);
            }

            private void OnValidate()
            {
                _levelManager = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            }
        }
    }
}