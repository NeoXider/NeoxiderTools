using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    [System.Serializable]
    public class CheckSpin
    {
        public bool isActive = true;
        [SerializeField] private LinesData _linesData;
        [SerializeField] private SpriteMultiplayerData _spritesMultiplierData;

        //�������� ��� ���������
        public float[] GetMultiplayers(Sprite[,] mass, int countLine, int[] lines = null)
        {
            List<float> multiplayes = new List<float>();

            if (lines == null)
            {
                lines = GetWinningLines(mass, countLine);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                float mult = GetMaxMultiplierForLine(mass, _linesData.lines[lines[i]]);
                multiplayes.Add(mult);
            }

            return multiplayes.ToArray();
        }

        //�������� ��� ������ ���������� �����
        public int[] GetWinningLines(Sprite[,] mass, int countLine, LinesData.InnerArray[] lines = null, int sequenceLength = 3)
        {
            List<int> winningLines = new List<int>();

            if (lines == null)
                lines = _linesData.lines;

            foreach (var currentLine in lines)
            {
                Dictionary<Sprite, int> lineSpriteCounts = GetInfoInSequenceLine(mass, currentLine, sequenceLength);

                if (lineSpriteCounts.Count > 0)
                {
                    winningLines.Add(Array.IndexOf(lines, currentLine));
                }
            }

            winningLines = winningLines.Where(item => item < countLine).ToList();

            return winningLines.ToArray();
        }

        //�������� ������� � �� ���������� � ���
        private Dictionary<Sprite, int> GetInfoInSequenceLine(Sprite[,] mass, LinesData.InnerArray currentLine, int sequenceLength)
        {
            Dictionary<Sprite, int> spriteCounts = new Dictionary<Sprite, int>();

            for (int x = 1; x < currentLine.corY.Length; x++)
            {
                int last = x - 1;

                if (mass[last, currentLine.corY[last]] == mass[x, currentLine.corY[x]])
                {
                    if (spriteCounts.ContainsKey(mass[last, currentLine.corY[last]]))
                    {
                        spriteCounts[mass[last, currentLine.corY[last]]]++;
                    }
                    else
                    {
                        spriteCounts[mass[last, currentLine.corY[last]]] = 2;
                    }
                }
            }

            // ��������� ������ �������, � ������� ���������� � ���� ������ ��� ����� sequenceLength
            Dictionary<Sprite, int> filteredSpriteCounts =
                spriteCounts.Where(kv => kv.Value >= sequenceLength)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return filteredSpriteCounts;
        }

        //������� ��������
        public void SetWin(Sprite[,] spritesEnd, Sprite[] _allSprites, int countLine)
        {
            if (GetWinningLines(spritesEnd, countLine).Length == 0)
            {
                int randWinLine = Random.Range(0, countLine);

                SetWinLine(spritesEnd, _linesData.lines[randWinLine], _allSprites);
            }
        }

        //������� ����� ����������
        private void SetWinLine(Sprite[,] spritesEnd, LinesData.InnerArray innerArray, Sprite[] allSprites)
        {
            int randStart = Random.Range(0, spritesEnd.GetLength(0) - 3);
            Sprite sprite = GetRandomSprite(allSprites);

            for (int x = randStart; x < randStart + 3; x++)
            {
                spritesEnd[x, innerArray.corY[x]] = sprite;
            }
        }

        //������� ���������
        public void SetLose(Sprite[,] spritesEnd, int[] lineWin, Sprite[] _allSprites, int countLine)
        {
            for (int i = 0; i < lineWin.Length; i++)
            {
                LinesData.InnerArray currentLine = _linesData.lines[lineWin[i]];
                SetLoseLine(spritesEnd, _allSprites, currentLine);
            }

            int[] countWinLine = GetWinningLines(spritesEnd, countLine);

            if (countWinLine.Length > 0)
                SetLose(spritesEnd, countWinLine, _allSprites, countLine);
        }

        //������� ����� �����������
        private void SetLoseLine(Sprite[,] mass, Sprite[] _allSprites, LinesData.InnerArray currentLine)
        {
            for (int x = 1; x < currentLine.corY.Length; x++)
            {
                int last = x - 1;

                if (mass[last, currentLine.corY[last]] == mass[x, currentLine.corY[x]])
                {
                    Sprite curSprite = mass[last, currentLine.corY[last]];
                    List<Sprite> newSprites = new List<Sprite>(_allSprites);
                    newSprites.Remove(curSprite);
                    mass[last, currentLine.corY[last]] = GetRandomSprite(newSprites.ToArray());
                }
            }

            Dictionary<Sprite, int> sppriteCount = GetInfoInSequenceLine(mass, currentLine, 3);

            if (sppriteCount.Count >= 1)
            {
                SetLoseLine(mass, _allSprites, currentLine);
            }
        }

        //�������� ������������ ����������� � �����
        private float GetMaxMultiplierForLine(Sprite[,] mass, LinesData.InnerArray currentLine)
        {
            Dictionary<Sprite, int> spriteCount = GetInfoInSequenceLine(mass, currentLine, 3);
            float maxMultiplier = 0;

            foreach (var item in spriteCount)
            {
                float multSprite = GetMultiplayer(_spritesMultiplierData.spritesMultiplier, item.Key, item.Value);

                if (multSprite > maxMultiplier)
                {
                    maxMultiplier = multSprite;
                }
            }

            return maxMultiplier;
        }

        //�������� �����������
        private float GetMultiplayer(SpriteMultiplayerData.SpritesMultiplier spritesMultiplier, Sprite sprite, int count)
        {
            foreach (var spriteMult in spritesMultiplier.spriteMults)
            {
                if (spriteMult.sprite == sprite)
                {
                    foreach (var countMultiplayer in spriteMult.countMult)
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

        //�������� ��������� ��������
        private Sprite GetRandomSprite(Sprite[] newSprites)
        {
            return newSprites[Random.Range(0, newSprites.Length)];
        }
    }
}