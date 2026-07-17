using System;

namespace Neo.Abilities
{
    /// <summary>
    ///     Stable serializable identity of a unit inside an <see cref="AbilitySystem" />.
    ///     Safe to send over the network; never holds a Unity object reference.
    /// </summary>
    [Serializable]
    public readonly struct UnitId : IEquatable<UnitId>
    {
        public static readonly UnitId None = new UnitId(0);

        public readonly uint Value;

        public UnitId(uint value)
        {
            Value = value;
        }

        public bool IsValid => Value != 0;

        public bool Equals(UnitId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public override string ToString()
        {
            return Value == 0 ? "Unit(None)" : $"Unit({Value})";
        }

        public static bool operator ==(UnitId a, UnitId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(UnitId a, UnitId b)
        {
            return a.Value != b.Value;
        }
    }
}
