using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Cards
{
    /// <summary>
    ///     Shared layout math for hand, board, and deck stacks.
    /// </summary>
    /// <remarks>
    ///     Scattered placement and jitter are deterministic per card index and <c>seed</c>: re-running a layout
    ///     after a card is added keeps every existing card where it was, and layout never consumes the global
    ///     <see cref="UnityEngine.Random" /> state that gameplay code may rely on. Pass a different seed to get a
    ///     different (still stable) arrangement. The list-filling overloads reuse caller buffers and do not
    ///     allocate.
    /// </remarks>
    public static class CardLayoutCalculator
    {
        private const int ChannelJitterX = 0;
        private const int ChannelJitterY = 1;
        private const int ChannelJitterZ = 2;
        private const int ChannelScatterRadius = 3;
        private const int ChannelScatterAngle = 4;
        private const int ChannelScatterRotation = 5;
        private const int ChannelRotationJitterX = 6;
        private const int ChannelRotationJitterY = 7;
        private const int ChannelRotationJitterZ = 8;

        /// <summary>
        ///     Computes local positions for a layout type and card count.
        /// </summary>
        /// <param name="layoutType">Layout kind.</param>
        /// <param name="cardCount">Number of cards.</param>
        /// <param name="settings">Layout parameters.</param>
        /// <returns>Local positions in card index order.</returns>
        public static List<Vector3> CalculatePositions(CardLayoutType layoutType, int cardCount,
            CardLayoutSettings settings)
        {
            List<Vector3> positions = new(Mathf.Max(0, cardCount));
            CalculatePositions(layoutType, cardCount, settings, positions);
            return positions;
        }

        /// <summary>
        ///     Fills <paramref name="results" /> with local positions without allocating (the list is cleared first).
        /// </summary>
        /// <param name="layoutType">Layout kind.</param>
        /// <param name="cardCount">Number of cards; zero or negative yields an empty list.</param>
        /// <param name="settings">Layout parameters.</param>
        /// <param name="results">Destination list, reused between calls.</param>
        /// <param name="seed">Seed for scattered placement and jitter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is null.</exception>
        public static void CalculatePositions(CardLayoutType layoutType, int cardCount, CardLayoutSettings settings,
            List<Vector3> results, int seed = 0)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (cardCount <= 0)
            {
                return;
            }

            switch (layoutType)
            {
                case CardLayoutType.Fan:
                    CalculateFanPositions(results, cardCount, settings);
                    break;
                case CardLayoutType.Grid:
                    CalculateGridPositions(results, cardCount, settings);
                    break;
                case CardLayoutType.Stack:
                    CalculateStackPositions(results, cardCount, settings);
                    break;
                case CardLayoutType.Scattered:
                    CalculateScatteredPositions(results, cardCount, settings, seed);
                    break;
                case CardLayoutType.Line:
                case CardLayoutType.Slots:
                default:
                    CalculateLinePositions(results, cardCount, settings);
                    break;
            }

            ApplyPositionJitter(results, settings.PositionJitter, seed);
        }

        /// <summary>
        ///     Computes local rotations for a layout type and card count.
        /// </summary>
        /// <param name="layoutType">Layout kind.</param>
        /// <param name="cardCount">Number of cards.</param>
        /// <param name="settings">Layout parameters.</param>
        /// <returns>Local rotations in card index order.</returns>
        public static List<Quaternion> CalculateRotations(CardLayoutType layoutType, int cardCount,
            CardLayoutSettings settings)
        {
            List<Quaternion> rotations = new(Mathf.Max(0, cardCount));
            CalculateRotations(layoutType, cardCount, settings, rotations);
            return rotations;
        }

        /// <summary>
        ///     Fills <paramref name="results" /> with local rotations without allocating (the list is cleared first).
        /// </summary>
        /// <param name="layoutType">Layout kind.</param>
        /// <param name="cardCount">Number of cards; zero or negative yields an empty list.</param>
        /// <param name="settings">Layout parameters.</param>
        /// <param name="results">Destination list, reused between calls.</param>
        /// <param name="seed">Seed for scattered rotation and rotation jitter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="results" /> is null.</exception>
        public static void CalculateRotations(CardLayoutType layoutType, int cardCount, CardLayoutSettings settings,
            List<Quaternion> results, int seed = 0)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (cardCount <= 0)
            {
                return;
            }

            if (layoutType == CardLayoutType.Fan)
            {
                float totalAngle = Mathf.Min(settings.ArcAngle * (cardCount - 1), 60f);
                float step = cardCount > 1 ? totalAngle / (cardCount - 1) : 0f;
                for (int i = 0; i < cardCount; i++)
                {
                    float angle = -totalAngle * 0.5f + step * i;
                    results.Add(Quaternion.Euler(0f, 0f, -angle));
                }
            }
            else if (layoutType == CardLayoutType.Scattered)
            {
                float range = settings.ScatteredRotationRange;
                for (int i = 0; i < cardCount; i++)
                {
                    float z = Range(-range, range, seed, i, ChannelScatterRotation);
                    results.Add(Quaternion.Euler(0f, 0f, z));
                }
            }
            else
            {
                for (int i = 0; i < cardCount; i++)
                {
                    results.Add(Quaternion.identity);
                }
            }

            ApplyRotationJitter(results, settings.RotationJitter, seed);
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

        private static void CalculateScatteredPositions(List<Vector3> positions, int count,
            CardLayoutSettings settings, int seed)
        {
            float radius = Mathf.Max(0f, settings.ScatteredRadius);
            for (int i = 0; i < count; i++)
            {
                // WHY: sqrt keeps the distribution uniform over the disk, like Random.insideUnitCircle did.
                float distance = Mathf.Sqrt(Noise(seed, i, ChannelScatterRadius)) * radius;
                float angle = Noise(seed, i, ChannelScatterAngle) * Mathf.PI * 2f;
                positions.Add(new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f));
            }
        }

        private static void ApplyPositionJitter(List<Vector3> positions, Vector3 jitter, int seed)
        {
            if (jitter == Vector3.zero)
            {
                return;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] += new Vector3(
                    Range(-jitter.x, jitter.x, seed, i, ChannelJitterX),
                    Range(-jitter.y, jitter.y, seed, i, ChannelJitterY),
                    Range(-jitter.z, jitter.z, seed, i, ChannelJitterZ));
            }
        }

        private static void ApplyRotationJitter(List<Quaternion> rotations, Vector3 jitter, int seed)
        {
            if (jitter == Vector3.zero)
            {
                return;
            }

            for (int i = 0; i < rotations.Count; i++)
            {
                Quaternion delta = Quaternion.Euler(
                    Range(-jitter.x, jitter.x, seed, i, ChannelRotationJitterX),
                    Range(-jitter.y, jitter.y, seed, i, ChannelRotationJitterY),
                    Range(-jitter.z, jitter.z, seed, i, ChannelRotationJitterZ));
                rotations[i] = rotations[i] * delta;
            }
        }

        private static float Range(float min, float max, int seed, int index, int channel)
        {
            return min + (max - min) * Noise(seed, index, channel);
        }

        /// <summary>
        ///     Stateless hash noise in [0, 1) for (seed, index, channel).
        /// </summary>
        private static float Noise(int seed, int index, int channel)
        {
            unchecked
            {
                uint hash = Mix((uint)seed + 0x9E3779B9u);
                hash = Mix(hash ^ ((uint)index + 0x7F4A7C15u));
                hash = Mix(hash ^ ((uint)channel + 0x2545F491u));
                return (hash >> 8) * (1f / 16777216f);
            }
        }

        private static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return value;
            }
        }
    }
}
