using System;
using System.Collections.Generic;

namespace Neo.Abilities
{
    /// <summary>
    ///     Case-insensitive comparer for ability, modifier, property, state, event, resource and op ids. Same
    ///     equality as <see cref="StringComparer.OrdinalIgnoreCase" /> (each UTF-16 unit compared after
    ///     <see cref="char.ToUpperInvariant" />), with an ASCII fast path and its own hash.
    /// </summary>
    // WHY: in Unity's Mono, StringComparer.OrdinalIgnoreCase hashes through the culture tables (about 0.7 us for
    // a 12-character id, ten times an ordinal lookup), and every property, state, slot, op and event lookup of
    // every unit, cast and tick pays it.
    internal sealed class AbilityIdComparer : IEqualityComparer<string>
    {
        public static readonly AbilityIdComparer Instance = new AbilityIdComparer();

        private AbilityIdComparer()
        {
        }

        /// <summary>Static shortcut of <see cref="Equals(string, string)" /> for comparison loops.</summary>
        public static bool Same(string a, string b)
        {
            return Instance.Equals(a, b);
        }

        public bool Equals(string a, string b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                int x = a[i];
                int y = b[i];
                if (x != y && Fold(x) != Fold(y))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(string value)
        {
            if (value == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = (int)2166136261;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash ^ Fold(value[i])) * 16777619;
                }

                return hash;
            }
        }

        private static int Fold(int c)
        {
            if ((uint)(c - 'a') <= 'z' - 'a')
            {
                return c - ('a' - 'A');
            }

            return c < 0x80 ? c : char.ToUpperInvariant((char)c);
        }
    }
}
