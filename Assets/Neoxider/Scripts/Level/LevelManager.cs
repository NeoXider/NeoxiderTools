using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Neoxider
{
    namespace Level
    {
        public class LevelManager : MonoBehaviour
        {
            [SerializeField] private Transform _parentLevel;
            [SerializeField] private LevelButton[] lvlBtns;

            public int complexity;
            public Level[] levels;

            public int currentLevel;
            public bool isLoopLevel = false;

            private void OnEnable()
            {
                UpdateVisuals();
            }

            public void SetComplexity(int id)
            {
                complexity = id;
            }

            public void NextLevel()
            {
                SetLevel(currentLevel + 1);
            }

            public void SaveLevel()
            {
                if (CurrentLevel().level == currentLevel)
                    CurrentLevel().SaveLevel();
            }

            private Level CurrentLevel()
            {
                return levels[complexity];
            }

            private void UpdateVisuals()
            {
                Level curLevel = CurrentLevel();

                foreach (var item in lvlBtns)
                {
                    item.transform.parent.gameObject.SetActive(false);
                }

                for (int i = 0; i < lvlBtns.Length && i < curLevel.countLevels; i++)
                {
                    lvlBtns[i].transform.parent.gameObject.SetActive(true);

                    int idVisual = i < curLevel.level ? 1 : (i == curLevel.level ? 2 : 0);
                    lvlBtns[i].SetVisual(idVisual, i);
                }
            }

            internal void SetLevel(int idLevel)
            {
                Level curLvl = CurrentLevel();
                currentLevel = isLoopLevel ? GetLoopLevel(idLevel, curLvl.countLevels) : math.min(idLevel, curLvl.countLevels - 1);
            }

            private static int GetLoopLevel(int idLevel, int count)
            {
                return (idLevel + count) % count;
            }

            private void OnValidate()
            {
                HashSet<LevelButton> btns = new HashSet<LevelButton>();

                foreach (var par in _parentLevel.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var child in par.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.TryGetComponent(out LevelButton levelButton))
                            btns.Add(levelButton);
                    }
                }

                lvlBtns = new LevelButton[btns.Count];
                btns.CopyTo(lvlBtns);

                for (int i = 0; i < levels.Length; i++)
                {
                    levels[i].idComplexity = i;
                }
            }
        }
    }
}
