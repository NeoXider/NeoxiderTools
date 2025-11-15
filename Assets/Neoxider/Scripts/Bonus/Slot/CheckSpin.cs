using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    [Serializable]
    public class CheckSpin
    {
        public bool isActive = true;
        [SerializeField] private LinesData _linesData;
        [SerializeField] private SpriteMultiplayerData _spritesMultiplierData;

        // Получение множителей для выигрышных линий
        public float[] GetMultiplayers(int[,] elementIds, int countLine, int[] lines = null)
        {
            List<float> multiplayes = new();
            if (lines == null)
            {
                lines = GetWinningLines(elementIds, countLine);
            }

            foreach (int lineIndex in lines)
            {
                float mult = GetMaxMultiplierForLine(elementIds, _linesData.lines[lineIndex]);
                multiplayes.Add(mult);
            }

            return multiplayes.ToArray();
        }

        // Получение индексов всех выигрышных линий
        public int[] GetWinningLines(int[,] elementIds, int countLine, int sequenceLength = 3)
        {
            List<int> winningLines = new();
            for (int i = 0; i < _linesData.lines.Length && i < countLine; i++)
            {
                Dictionary<int, int> lineSpriteCounts =
                    GetInfoInSequenceLine(elementIds, _linesData.lines[i], sequenceLength);
                if (lineSpriteCounts.Count > 0)
                {
                    winningLines.Add(i);
                }
            }

            return winningLines.ToArray();
        }

        // Получение информации о последовательностях одинаковых ID в линии
        private Dictionary<int, int> GetInfoInSequenceLine(int[,] elementIds, LinesData.InnerArray currentLine,
            int sequenceLength)
        {
            Dictionary<int, int> idCounts = new();
            if (elementIds.GetLength(0) < currentLine.corY.Length)
            {
                return idCounts;
            }

            for (int x = 1; x < currentLine.corY.Length; x++)
            {
                int lastY = currentLine.corY[x - 1];
                int currentY = currentLine.corY[x];

                if (elementIds[x - 1, lastY] == elementIds[x, currentY])
                {
                    int elementId = elementIds[x, currentY];
                    if (idCounts.ContainsKey(elementId))
                    {
                        idCounts[elementId]++;
                    }
                    else
                    {
                        idCounts[elementId] = 2;
                    }
                }
            }

            return idCounts.Where(kv => kv.Value >= sequenceLength).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // Генерация выигрышной комбинации
        public void SetWin(int[,] elementIds, int totalIdCount, int countLine)
        {
            if (GetWinningLines(elementIds, countLine).Length == 0)
            {
                int randWinLineIndex = Random.Range(0, countLine);
                SetWinLine(elementIds, _linesData.lines[randWinLineIndex], totalIdCount);
            }
        }

        // Установка выигрышной линии
        private void SetWinLine(int[,] elementIds, LinesData.InnerArray innerArray, int totalIdCount)
        {
            int randStart = Random.Range(0, elementIds.GetLength(0) - 2); // -2 to ensure we can place 3 items
            int winId = Random.Range(0, totalIdCount);

            for (int x = randStart; x < randStart + 3; x++)
            {
                elementIds[x, innerArray.corY[x]] = winId;
            }
        }

        // Превращение выигрышной комбинации в проигрышную
        public void SetLose(int[,] elementIds, int[] lineWin, int totalIdCount, int countLine)
        {
            foreach (int lineIndex in lineWin)
            {
                SetLoseLine(elementIds, _linesData.lines[lineIndex], totalIdCount);
            }

            int[] countWinLine = GetWinningLines(elementIds, countLine);
            if (countWinLine.Length > 0)
            {
                SetLose(elementIds, countWinLine, totalIdCount, countLine);
            }
        }

        // "Ломаем" выигрышную линию
        private void SetLoseLine(int[,] elementIds, LinesData.InnerArray currentLine, int totalIdCount)
        {
            for (int x = 1; x < currentLine.corY.Length; x++)
            {
                if (elementIds[x - 1, currentLine.corY[x - 1]] == elementIds[x, currentLine.corY[x]])
                {
                    int currentId = elementIds[x, currentLine.corY[x]];
                    int newId = currentId;
                    while (newId == currentId)
                    {
                        newId = Random.Range(0, totalIdCount);
                    }

                    elementIds[x, currentLine.corY[x]] = newId;
                    return; // Достаточно сломать в одном месте
                }
            }
        }

        // Получение максимального множителя для линии
        private float GetMaxMultiplierForLine(int[,] elementIds, LinesData.InnerArray currentLine)
        {
            Dictionary<int, int> spriteCount = GetInfoInSequenceLine(elementIds, currentLine, 3);
            float maxMultiplier = 0;

            foreach (KeyValuePair<int, int> item in spriteCount)
            {
                float multSprite = GetMultiplayer(item.Key, item.Value);
                if (multSprite > maxMultiplier)
                {
                    maxMultiplier = multSprite;
                }
            }

            return maxMultiplier;
        }

        // Получение множителя для конкретного ID и количества
        private float GetMultiplayer(int id, int count)
        {
            foreach (SpriteMultiplayerData.IdMult spriteMult in _spritesMultiplierData.spritesMultiplier.spriteMults)
            {
                if (spriteMult.id == id)
                {
                    foreach (SpriteMultiplayerData.CountMultiplayer countMultiplayer in spriteMult.countMult)
                    {
                        if (countMultiplayer.count == count)
                        {
                            return countMultiplayer.mult;
                        }
                    }
                }
            }

            return 0;
        }
    }
}