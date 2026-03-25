using System;

namespace Neo.Cards
{
    /// <summary>
    ///     Immutable playing card data.
    /// </summary>
    [Serializable]
    public readonly struct CardData : IComparable<CardData>, IEquatable<CardData>
    {
        /// <summary>
        ///     Card suit.
        /// </summary>
        public Suit Suit { get; }

        /// <summary>
        ///     Card rank.
        /// </summary>
        public Rank Rank { get; }

        /// <summary>
        ///     Whether this card is a joker.
        /// </summary>
        public bool IsJoker { get; }

        /// <summary>
        ///     Joker color (true = red, false = black). Meaningful only when <see cref="IsJoker" /> is true.
        /// </summary>
        public bool IsRedJoker { get; }

        /// <summary>
        ///     Creates a standard card with the given suit and rank.
        /// </summary>
        /// <param name="suit">Suit.</param>
        /// <param name="rank">Rank.</param>
        public CardData(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
            IsJoker = false;
            IsRedJoker = false;
        }

        /// <summary>
        ///     Creates a joker card.
        /// </summary>
        /// <param name="isRed">True for red joker, false for black.</param>
        public static CardData CreateJoker(bool isRed)
        {
            return new CardData(isRed);
        }

        private CardData(bool isRedJoker)
        {
            Suit = default;
            Rank = default;
            IsJoker = true;
            IsRedJoker = isRedJoker;
        }

        /// <summary>
        ///     Compares cards by rank (for War-style games).
        /// </summary>
        /// <param name="other">Other card.</param>
        /// <returns>Positive if this card is higher, negative if lower, zero if equal.</returns>
        public int CompareTo(CardData other)
        {
            if (IsJoker && other.IsJoker)
            {
                return 0;
            }

            if (IsJoker)
            {
                return 1;
            }

            if (other.IsJoker)
            {
                return -1;
            }

            return Rank.CompareTo(other.Rank);
        }

        /// <summary>
        ///     Returns whether this card beats the other with optional trump suit (Durak-style).
        /// </summary>
        /// <param name="other">Card to beat.</param>
        /// <param name="trump">Trump suit, or null if none.</param>
        /// <returns>True if this card beats <paramref name="other" />.</returns>
        public bool Beats(CardData other, Suit? trump)
        {
            if (IsJoker || other.IsJoker)
            {
                return false;
            }

            if (trump.HasValue)
            {
                bool thisTrump = Suit == trump.Value;
                bool otherTrump = other.Suit == trump.Value;

                if (thisTrump && !otherTrump)
                {
                    return true;
                }

                if (!thisTrump && otherTrump)
                {
                    return false;
                }
            }

            if (Suit == other.Suit)
            {
                return Rank > other.Rank;
            }

            return false;
        }

        /// <summary>
        ///     Returns whether this card can cover the attacking card.
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>True if this card can cover.</returns>
        public bool CanCover(CardData attackCard, Suit? trump)
        {
            return Beats(attackCard, trump);
        }

        /// <summary>
        ///     Returns whether ranks match (for discarding rules in Durak).
        /// </summary>
        /// <param name="other">Other card.</param>
        /// <returns>True if ranks are equal.</returns>
        public bool HasSameRank(CardData other)
        {
            if (IsJoker || other.IsJoker)
            {
                return false;
            }

            return Rank == other.Rank;
        }

        /// <summary>
        ///     Returns whether suits match.
        /// </summary>
        /// <param name="other">Other card.</param>
        /// <returns>True if suits are equal.</returns>
        public bool HasSameSuit(CardData other)
        {
            if (IsJoker || other.IsJoker)
            {
                return false;
            }

            return Suit == other.Suit;
        }

        public static bool operator >(CardData a, CardData b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(CardData a, CardData b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >=(CardData a, CardData b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <=(CardData a, CardData b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator ==(CardData a, CardData b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(CardData a, CardData b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        ///     Equality check.
        /// </summary>
        public bool Equals(CardData other)
        {
            if (IsJoker && other.IsJoker)
            {
                return IsRedJoker == other.IsRedJoker;
            }

            if (IsJoker || other.IsJoker)
            {
                return false;
            }

            return Suit == other.Suit && Rank == other.Rank;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is CardData other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (IsJoker)
            {
                return HashCode.Combine(IsJoker, IsRedJoker);
            }

            return HashCode.Combine(Suit, Rank);
        }

        /// <summary>
        ///     Short string representation (e.g. A♠).
        /// </summary>
        public override string ToString()
        {
            if (IsJoker)
            {
                return IsRedJoker ? "Red Joker" : "Black Joker";
            }

            return $"{Rank.ToShortString()}{Suit.ToSymbol()}";
        }

        /// <summary>
        ///     Verbose English description (rank and suit names).
        /// </summary>
        public string ToLongEnglishString()
        {
            if (IsJoker)
            {
                return IsRedJoker ? "Red Joker" : "Black Joker";
            }

            return $"{Rank.ToEnglishName()} of {Suit.ToEnglishName()}";
        }
    }
}
