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
        ///     Whether this card uses a custom id instead of the built-in suit/rank model.
        /// </summary>
        public bool IsCustom { get; }

        /// <summary>
        ///     Stable custom id for non-standard card games.
        /// </summary>
        public string CustomId { get; }

        /// <summary>
        ///     Optional display name for custom cards.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        ///     Generic comparable value for custom games (power, cost, rarity order, etc.).
        /// </summary>
        public int SortValue { get; }

        /// <summary>
        ///     Optional grouping key for custom games (faction, class, color, suit-like group).
        /// </summary>
        public string Group { get; }

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
            IsCustom = false;
            CustomId = string.Empty;
            DisplayName = string.Empty;
            SortValue = (int)rank;
            Group = suit.ToString();
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
            IsCustom = false;
            CustomId = string.Empty;
            DisplayName = isRedJoker ? "Red Joker" : "Black Joker";
            SortValue = int.MaxValue;
            Group = "Joker";
        }

        private CardData(string customId, string displayName, int sortValue, string group)
        {
            if (string.IsNullOrWhiteSpace(customId))
            {
                throw new ArgumentException("Custom card id must be stable and non-empty.", nameof(customId));
            }

            Suit = default;
            Rank = default;
            IsJoker = false;
            IsRedJoker = false;
            IsCustom = true;
            CustomId = customId.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? CustomId : displayName.Trim();
            SortValue = sortValue;
            Group = string.IsNullOrWhiteSpace(group) ? string.Empty : group.Trim();
        }

        /// <summary>
        ///     Creates a non-standard card for custom games (TCG, board-game cards, ability cards, etc.).
        /// </summary>
        public static CardData CreateCustom(string customId, string displayName = null, int sortValue = 0,
            string group = null)
        {
            return new CardData(customId, displayName, sortValue, group);
        }

        /// <summary>
        ///     Compares cards by rank (for War-style games).
        /// </summary>
        /// <param name="other">Other card.</param>
        /// <returns>Positive if this card is higher, negative if lower, zero if equal.</returns>
        public int CompareTo(CardData other)
        {
            if (IsCustom || other.IsCustom)
            {
                int valueCompare = SortValue.CompareTo(other.SortValue);
                if (valueCompare != 0)
                {
                    return valueCompare;
                }

                return string.Compare(CustomId, other.CustomId, StringComparison.Ordinal);
            }

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
            if (IsCustom || other.IsCustom)
            {
                if (!IsCustom || !other.IsCustom)
                {
                    return false;
                }

                bool sameGroup = string.IsNullOrEmpty(Group) || string.IsNullOrEmpty(other.Group) ||
                                 Group == other.Group;
                return sameGroup && SortValue > other.SortValue;
            }

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
            if (IsCustom || other.IsCustom)
            {
                return IsCustom && other.IsCustom && SortValue == other.SortValue;
            }

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
            if (IsCustom || other.IsCustom)
            {
                return IsCustom && other.IsCustom && !string.IsNullOrEmpty(Group) && Group == other.Group;
            }

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
            if (IsCustom && other.IsCustom)
            {
                return CustomId == other.CustomId;
            }

            if (IsCustom || other.IsCustom)
            {
                return false;
            }

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
            if (IsCustom)
            {
                return HashCode.Combine(IsCustom, CustomId);
            }

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
            if (IsCustom)
            {
                return string.IsNullOrEmpty(DisplayName) ? CustomId : DisplayName;
            }

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
            if (IsCustom)
            {
                return string.IsNullOrEmpty(DisplayName) ? CustomId : DisplayName;
            }

            if (IsJoker)
            {
                return IsRedJoker ? "Red Joker" : "Black Joker";
            }

            return $"{Rank.ToEnglishName()} of {Suit.ToEnglishName()}";
        }
    }
}
