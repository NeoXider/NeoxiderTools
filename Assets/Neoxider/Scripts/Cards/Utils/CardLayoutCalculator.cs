using System.Collections.Generic;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Переиспользуемый расчет позиций/поворотов для Hand/Board/Deck.
    /// </summary>
    public static class CardLayoutCalculator
    {
        /// <summary>
        ///     Рассчитывает список локальных позиций для заданного типа раскладки.
        /// </summary>
        /// <param name="layoutType">Тип раскладки.</param>
        /// <param name="cardCount">Количество карт.</param>
        /// <param name="settings">Параметры раскладки.</param>
        /// <returns>Список локальных позиций в порядке индексов карт.</returns>
        public static List<Vector3> CalculatePositions(CardLayoutType layoutType, int cardCount,
            CardLayoutSettings settings)
        {
            List<Vector3> positions = new(cardCount);
            if (cardCount <= 0)
            {
                return positions;
            }

            switch (layoutType)
            {
                case CardLayoutType.Fan:
                    CalculateFanPositions(positions, cardCount, settings);
                    break;
                case CardLayoutType.Grid:
                    CalculateGridPositions(positions, cardCount, settings);
                    break;
                case CardLayoutType.Stack:
                    CalculateStackPositions(positions, cardCount, settings);
                    break;
                case CardLayoutType.Scattered:
                    CalculateScatteredPositions(positions, cardCount, settings);
                    break;
                case CardLayoutType.Line:
                case CardLayoutType.Slots:
                default:
                    CalculateLinePositions(positions, cardCount, settings);
                    break;
            }

            ApplyPositionJitter(positions, settings.PositionJitter);
            return positions;
        }

        /// <summary>
        ///     Рассчитывает список локальных поворотов для заданного типа раскладки.
        /// </summary>
        /// <param name="layoutType">Тип раскладки.</param>
        /// <param name="cardCount">Количество карт.</param>
        /// <param name="settings">Параметры раскладки.</param>
        /// <returns>Список локальных поворотов в порядке индексов карт.</returns>
        public static List<Quaternion> CalculateRotations(CardLayoutType layoutType, int cardCount,
            CardLayoutSettings settings)
        {
            List<Quaternion> rotations = new(cardCount);
            if (cardCount <= 0)
            {
                return rotations;
            }

            if (layoutType == CardLayoutType.Fan)
            {
                float totalAngle = Mathf.Min(settings.ArcAngle * (cardCount - 1), 60f);
                float step = cardCount > 1 ? totalAngle / (cardCount - 1) : 0f;
                for (int i = 0; i < cardCount; i++)
                {
                    float angle = -totalAngle * 0.5f + step * i;
                    rotations.Add(Quaternion.Euler(0f, 0f, -angle));
                }
            }
            else if (layoutType == CardLayoutType.Scattered)
            {
                for (int i = 0; i < cardCount; i++)
                {
                    float z = Random.Range(-settings.ScatteredRotationRange, settings.ScatteredRotationRange);
                    rotations.Add(Quaternion.Euler(0f, 0f, z));
                }
            }
            else
            {
                for (int i = 0; i < cardCount; i++)
                {
                    rotations.Add(Quaternion.identity);
                }
            }

            ApplyRotationJitter(rotations, settings.RotationJitter);
            return rotations;
        }

        private static void CalculateLinePositions(List<Vector3> positions, int count, CardLayoutSettings settings)
        {
            float totalWidth = (count - 1) * settings.Spacing;
            float startX = -totalWidth * 0.5f;
            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(startX + i * settings.Spacing, 0f, 0f));
            }
        }

        private static void CalculateStackPositions(List<Vector3> positions, int count, CardLayoutSettings settings)
        {
            for (int i = 0; i < count; i++)
            {
                positions.Add(new Vector3(i * settings.StackStep, i * settings.StackStep, 0f));
            }
        }

        private static void CalculateGridPositions(List<Vector3> positions, int count, CardLayoutSettings settings)
        {
            int columns = Mathf.Max(1, settings.GridColumns);
            int rows = Mathf.CeilToInt((float)count / columns);
            float totalHeight = (rows - 1) * settings.GridRowSpacing;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                int itemsInRow = Mathf.Min(columns, count - row * columns);
                float rowWidth = (itemsInRow - 1) * settings.Spacing;

                float x = -rowWidth * 0.5f + col * settings.Spacing;
                float y = totalHeight * 0.5f - row * settings.GridRowSpacing;
                positions.Add(new Vector3(x, y, 0f));
            }
        }

        private static void CalculateFanPositions(List<Vector3> positions, int count, CardLayoutSettings settings)
        {
            if (count == 1)
            {
                positions.Add(Vector3.zero);
                return;
            }

            float totalAngle = Mathf.Min(settings.ArcAngle * (count - 1), 60f);
            float step = totalAngle / Mathf.Max(1, count - 1);
            for (int i = 0; i < count; i++)
            {
                float angle = -totalAngle * 0.5f + step * i;
                float radians = angle * Mathf.Deg2Rad;
                float x = Mathf.Sin(radians) * settings.ArcRadius;
                float y = -Mathf.Cos(radians) * settings.ArcRadius + settings.ArcRadius;
                positions.Add(new Vector3(x, y, 0f));
            }
        }

        private static void CalculateScatteredPositions(List<Vector3> positions, int count, CardLayoutSettings settings)
        {
            float radius = Mathf.Max(0f, settings.ScatteredRadius);
            for (int i = 0; i < count; i++)
            {
                Vector2 random = Random.insideUnitCircle * radius;
                positions.Add(new Vector3(random.x, random.y, 0f));
            }
        }

        private static void ApplyPositionJitter(List<Vector3> positions, Vector3 jitter)
        {
            if (jitter == Vector3.zero)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] += new Vector3(
                    Random.Range(-jitter.x, jitter.x),
                    Random.Range(-jitter.y, jitter.y),
                    Random.Range(-jitter.z, jitter.z));
            }
        }

        private static void ApplyRotationJitter(List<Quaternion> rotations, Vector3 jitter)
        {
            if (jitter == Vector3.zero)
            {
                return;
            }

            for (int i = 0; i < rotations.Count; i++)
            {
                Quaternion delta = Quaternion.Euler(
                    Random.Range(-jitter.x, jitter.x),
                    Random.Range(-jitter.y, jitter.y),
                    Random.Range(-jitter.z, jitter.z));
                rotations[i] = rotations[i] * delta;
            }
        }
    }
}