using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Team/faction identity used by targeting filters. <see cref="Neutral" /> belongs to no team.
    /// </summary>
    [Serializable]
    public readonly struct TeamId : IEquatable<TeamId>
    {
        public static readonly TeamId Neutral = new TeamId(0);

        public readonly int Value;

        public TeamId(int value)
        {
            Value = value;
        }

        public bool IsNeutral => Value == 0;

        public bool IsAllyOf(TeamId other)
        {
            return !IsNeutral && Value == other.Value;
        }

        public bool IsEnemyOf(TeamId other)
        {
            return Value != other.Value;
        }

        public bool Equals(TeamId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is TeamId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value == 0 ? "Team(Neutral)" : $"Team({Value})";
        }

        public static bool operator ==(TeamId a, TeamId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(TeamId a, TeamId b)
        {
            return a.Value != b.Value;
        }
    }
}
