using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Neo.Bonus
{
    [Serializable]
    public class CheckSpin
    {
        private const int FallbackLegacyUnusedSentinel = -1000001;

        public bool isActive = true;

        [SerializeField] private LinesData _linesData;

        [SerializeField] private SpriteMultiplayerData _spritesMultiplierData;

        [Tooltip("Match length along a payline (e.g. 3 for three-in-a-row).")] [SerializeField] [Min(2)]
        private int _sequenceLength = 3;

        [Tooltip("Fallback без Lines Data: нижняя граница ряда окна **включительно** (0 = низ окна). **−1** = автоматически низ (0).")]
        [SerializeField]
        private int _fallbackWindowRowMin = -1;

        [Tooltip("Fallback: верхняя граница **включительно**. **−1** = автоматически верх окна (WindowHeight−1). Для одной линии задайте то же значение, что и у Min.")]
        [SerializeField]
        private int _fallbackWindowRowMax = -1;

        /// <summary>
        ///     Старый формат: одно поле «ряд fallback». Если не равно сентинелу — имеет приоритет над Min/Max (миграция префабов).
        /// </summary>
        [SerializeField, HideInInspector, FormerlySerializedAs("_fallbackWindowRowIndex")]
        private int _legacyFallbackWindowRowIndexOrUnused = FallbackLegacyUnusedSentinel;

        public int SequenceLength => Mathf.Max(2, _sequenceLength);

        /// <summary>Сериализованный нижний предел (−1 = авто).</summary>
        public int FallbackWindowRowMinRaw => _fallbackWindowRowMin;

        /// <summary>Сериализованный верхний предел (−1 = авто).</summary>
        public int FallbackWindowRowMaxRaw => _fallbackWindowRowMax;

        /// <summary>Lines Data asset (may be null → fallback horizontal lines).</summary>
        public LinesData LinesDataAsset
        {
            get => _linesData;
            set => _linesData = value;
        }

        /// <summary>Per-symbol payout multipliers (may be null).</summary>
        public SpriteMultiplayerData SpritesMultiplierData
        {
            get => _spritesMultiplierData;
            set => _spritesMultiplierData = value;
        }

        /// <summary>Minimum symbols in a row on a payline (≥ 2).</summary>
        public void SetSequenceLength(int matchLength)
        {
            _sequenceLength = Mathf.Max(2, matchLength);
        }

        /// <summary>
        ///     Sets fallback window row range when <see cref="LinesData"/> is unused. Use −1 on either bound for auto
        ///     (min→0, max→windowHeight−1). Clears legacy single-row migration binding.
        /// </summary>
        public void SetFallbackPaylineWindowRows(int minInclusiveOrMinusOneAuto, int maxInclusiveOrMinusOneAuto)
        {
            _legacyFallbackWindowRowIndexOrUnused = FallbackLegacyUnusedSentinel;
            _fallbackWindowRowMin = minInclusiveOrMinusOneAuto;
            _fallbackWindowRowMax = maxInclusiveOrMinusOneAuto;
        }

        /// <summary>Makes Min/Max authoritative (drops migrated single-index fallback from old prefabs).</summary>
        public void ClearLegacyFallbackSingleRowBinding()
        {
            _legacyFallbackWindowRowIndexOrUnused = FallbackLegacyUnusedSentinel;
        }

        /// <summary>Effective paylines for evaluation (from Lines Data or fallback horizontal lines).</summary>
        public LinesData.InnerArray[] GetEffectiveLines(int columnCount, int windowRowCount)
        {
            columnCount = Mathf.Max(0, columnCount);
            windowRowCount = Mathf.Max(1, windowRowCount);

            if (_linesData != null && _linesData.lines != null && _linesData.lines.Length > 0)
            {
                List<LinesData.InnerArray> ok = new();
                foreach (LinesData.InnerArray line in _linesData.lines)
                {
                    if (line?.corY == null || line.corY.Length != columnCount)
                    {
                        continue;
                    }

                    bool bad = line.corY.Any(y => y < 0 || y >= windowRowCount);
                    if (bad)
                    {
                        continue;
                    }

                    ok.Add(line);
                }

                return ok.Count > 0 ? ok.ToArray() : BuildFallbackLine(columnCount, windowRowCount);
            }

            return BuildFallbackLine(columnCount, windowRowCount);
        }

        /// <summary>How many payline definitions exist for this grid size.</summary>
        public int GetPaylineDefinitionCount(int columnCount, int windowRowCount)
        {
            return GetEffectiveLines(columnCount, windowRowCount).Length;
        }

        /// <summary>
        ///     Resolved inclusive fallback row range (0 = bottom). Uses legacy single-row field when migrated from old assets.
        /// </summary>
        public void GetResolvedFallbackWindowRowRange(int windowRowCount, out int minRow, out int maxRow)
        {
            windowRowCount = Mathf.Max(1, windowRowCount);
            int autoMin = 0;
            int autoMax = windowRowCount - 1;

            if (_legacyFallbackWindowRowIndexOrUnused != FallbackLegacyUnusedSentinel)
            {
                int legacy = _legacyFallbackWindowRowIndexOrUnused;
                int row = legacy < 0
                    ? Mathf.Clamp((windowRowCount - 1) / 2, 0, windowRowCount - 1)
                    : Mathf.Clamp(legacy, 0, windowRowCount - 1);
                minRow = maxRow = row;
                return;
            }

            minRow = _fallbackWindowRowMin < 0 ? autoMin : Mathf.Clamp(_fallbackWindowRowMin, 0, windowRowCount - 1);
            maxRow = _fallbackWindowRowMax < 0 ? autoMax : Mathf.Clamp(_fallbackWindowRowMax, 0, windowRowCount - 1);
            if (minRow > maxRow)
            {
                (minRow, maxRow) = (maxRow, minRow);
            }
        }

        /// <summary>
        ///     Representative row for UI/gizmo (middle of fallback range).
        /// </summary>
        public int GetResolvedFallbackWindowRow(int windowRowCount)
        {
            GetResolvedFallbackWindowRowRange(windowRowCount, out int minR, out int maxR);
            return (minR + maxR) / 2;
        }

        /// <summary>True when no valid rows come from <see cref="LinesData"/> (using horizontal fallback).</summary>
        public bool UsesFallbackPaylinesOnly(int columnCount, int windowRowCount)
        {
            columnCount = Mathf.Max(0, columnCount);
            windowRowCount = Mathf.Max(1, windowRowCount);

            if (_linesData == null || _linesData.lines == null || _linesData.lines.Length == 0)
            {
                return true;
            }

            foreach (LinesData.InnerArray line in _linesData.lines)
            {
                if (line?.corY == null || line.corY.Length != columnCount)
                {
                    continue;
                }

                if (line.corY.Any(y => y < 0 || y >= windowRowCount))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private LinesData.InnerArray[] BuildFallbackLine(int columnCount, int windowRowCount)
        {
            if (columnCount <= 0)
            {
                return Array.Empty<LinesData.InnerArray>();
            }

            GetResolvedFallbackWindowRowRange(windowRowCount, out int rMin, out int rMax);

            LinesData.InnerArray[] lines = new LinesData.InnerArray[rMax - rMin + 1];
            for (int i = 0; i < lines.Length; i++)
            {
                int row = rMin + i;
                int[] corY = new int[columnCount];
                for (int c = 0; c < columnCount; c++)
                {
                    corY[c] = row;
                }

                lines[i] = new LinesData.InnerArray { corY = corY };
            }

            return lines;
        }

        public float[] GetMultiplayers(int[,] elementIds, int countLine, int[] lines = null)
        {
            List<float> multipliers = new();
            int cols = elementIds.GetLength(0);
            int rows = elementIds.GetLength(1);
            LinesData.InnerArray[] defs = GetEffectiveLines(cols, rows);
            int evalLines = Mathf.Min(Mathf.Max(1, countLine), defs.Length);

            if (lines == null)
            {
                lines = GetWinningLines(elementIds, countLine);
            }

            foreach (int lineIndex in lines)
            {
                if (lineIndex < 0 || lineIndex >= defs.Length || lineIndex >= evalLines)
                {
                    continue;
                }

                float mult = GetMaxMultiplierForLine(elementIds, defs[lineIndex]);
                multipliers.Add(mult);
            }

            return multipliers.ToArray();
        }

        public int[] GetWinningLines(int[,] elementIds, int countLine, int sequenceLength = -1)
        {
            int seq = sequenceLength > 0 ? sequenceLength : SequenceLength;
            int cols = elementIds.GetLength(0);
            int rows = elementIds.GetLength(1);
            LinesData.InnerArray[] defs = GetEffectiveLines(cols, rows);
            int evalLines = Mathf.Min(Mathf.Max(1, countLine), defs.Length);

            List<int> winningLines = new();
            for (int i = 0; i < defs.Length && i < evalLines; i++)
            {
                Dictionary<int, int> lineSpriteCounts = GetInfoInSequenceLine(elementIds, defs[i], seq);
                if (lineSpriteCounts.Count > 0)
                {
                    winningLines.Add(i);
                }
            }

            return winningLines.ToArray();
        }

        private Dictionary<int, int> GetInfoInSequenceLine(int[,] elementIds, LinesData.InnerArray currentLine,
            int sequenceLength)
        {
            Dictionary<int, int> idCounts = new();
            if (elementIds.GetLength(0) < currentLine.corY.Length)
            {
                return idCounts;
            }

            int rows = elementIds.GetLength(1);
            for (int x = 0; x < currentLine.corY.Length; x++)
            {
                int y = currentLine.corY[x];
                if (y < 0 || y >= rows)
                {
                    return idCounts;
                }
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

        public void SetWin(int[,] elementIds, int totalIdCount, int countLine)
        {
            int cols = elementIds.GetLength(0);
            int rows = elementIds.GetLength(1);
            LinesData.InnerArray[] defs = GetEffectiveLines(cols, rows);
            if (defs.Length == 0)
            {
                return;
            }

            int evalLines = Mathf.Min(Mathf.Max(1, countLine), defs.Length);

            if (GetWinningLines(elementIds, countLine).Length == 0)
            {
                int linePick = Random.Range(0, evalLines);
                SetWinLineFull(elementIds, defs[linePick], totalIdCount);
            }
        }

        private void SetWinLineFull(int[,] elementIds, LinesData.InnerArray innerArray, int totalIdCount)
        {
            int cols = elementIds.GetLength(0);
            int winId = Random.Range(0, totalIdCount);

            for (int x = 0; x < cols && x < innerArray.corY.Length; x++)
            {
                int y = innerArray.corY[x];
                elementIds[x, y] = winId;
            }
        }

        public void SetLose(int[,] elementIds, int[] lineWin, int totalIdCount, int countLine)
        {
            const int maxIterations = 64;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                foreach (int lineIndex in lineWin)
                {
                    int cols = elementIds.GetLength(0);
                    int rows = elementIds.GetLength(1);
                    LinesData.InnerArray[] defs = GetEffectiveLines(cols, rows);
                    if (lineIndex >= 0 && lineIndex < defs.Length)
                    {
                        SetLoseLine(elementIds, defs[lineIndex], totalIdCount);
                    }
                }

                int[] stillWinning = GetWinningLines(elementIds, countLine);
                if (stillWinning.Length == 0)
                {
                    return;
                }

                lineWin = stillWinning;
            }
        }

        private void SetLoseLine(int[,] elementIds, LinesData.InnerArray currentLine, int totalIdCount)
        {
            for (int x = 1; x < currentLine.corY.Length; x++)
            {
                if (elementIds[x - 1, currentLine.corY[x - 1]] == elementIds[x, currentLine.corY[x]])
                {
                    int currentId = elementIds[x, currentLine.corY[x]];
                    int newId = currentId;
                    int guard = 0;
                    while (newId == currentId && guard++ < 256)
                    {
                        newId = Random.Range(0, totalIdCount);
                    }

                    elementIds[x, currentLine.corY[x]] = newId;
                    return;
                }
            }
        }

        private float GetMaxMultiplierForLine(int[,] elementIds, LinesData.InnerArray currentLine)
        {
            Dictionary<int, int> spriteCount = GetInfoInSequenceLine(elementIds, currentLine, SequenceLength);
            float maxMultiplier = 0;

            foreach (KeyValuePair<int, int> item in spriteCount)
            {
                float multSprite = GetMultiplier(item.Key, item.Value);
                if (multSprite > maxMultiplier)
                {
                    maxMultiplier = multSprite;
                }
            }

            return maxMultiplier;
        }

        private float GetMultiplier(int id, int count)
        {
            if (_spritesMultiplierData?.spritesMultiplier?.spriteMults == null)
            {
                return 1f;
            }

            foreach (SpriteMultiplayerData.IdMult spriteMult in _spritesMultiplierData.spritesMultiplier.spriteMults)
            {
                if (spriteMult.id == id && spriteMult.countMult != null)
                {
                    foreach (SpriteMultiplayerData.CountMultiplayer countMultiplier in spriteMult.countMult)
                    {
                        if (countMultiplier.count == count)
                        {
                            return countMultiplier.mult;
                        }
                    }
                }
            }

            return 1f;
        }
    }
}
