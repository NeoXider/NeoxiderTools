using System;

namespace Neo.Cards
{
    /// <summary>
    ///     Неизменяемая структура данных игральной карты
    /// </summary>
    [Serializable]
    public readonly struct CardData : IComparable<CardData>, IEquatable<CardData>
    {
        /// <summary>
        ///     Масть карты
        /// </summary>
        public Suit Suit { get; }

        /// <summary>
        ///     Ранг (достоинство) карты
        /// </summary>
        public Rank Rank { get; }

        /// <summary>
        ///     Является ли карта джокером
        /// </summary>
        public bool IsJoker { get; }

        /// <summary>
        ///     Тип джокера (true = красный, false = чёрный). Имеет смысл только если IsJoker = true
        /// </summary>
        public bool IsRedJoker { get; }

        /// <summary>
        ///     Создаёт обычную карту с указанной мастью и рангом
        /// </summary>
        /// <param name="suit">Масть карты</param>
        /// <param name="rank">Ранг карты</param>
        public CardData(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
            IsJoker = false;
            IsRedJoker = false;
        }

        /// <summary>
        ///     Создаёт карту-джокер
        /// </summary>
        /// <param name="isRed">true для красного джокера, false для чёрного</param>
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
        ///     Сравнивает карты по рангу (для игры "Пьяница")
        /// </summary>
        /// <param name="other">Карта для сравнения</param>
        /// <returns>Положительное число если эта карта старше, отрицательное если младше, 0 если равны</returns>
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
        ///     Проверяет, бьёт ли эта карта другую с учётом козыря (для игры "Дурак")
        /// </summary>
        /// <param name="other">Карта, которую нужно побить</param>
        /// <param name="trump">Козырная масть (null если без козыря)</param>
        /// <returns>true если эта карта бьёт other</returns>
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
        ///     Проверяет, можно ли этой картой покрыть атакующую карту
        /// </summary>
        /// <param name="attackCard">Атакующая карта</param>
        /// <param name="trump">Козырная масть</param>
        /// <returns>true если можно покрыть</returns>
        public bool CanCover(CardData attackCard, Suit? trump)
        {
            return Beats(attackCard, trump);
        }

        /// <summary>
        ///     Проверяет, имеет ли карта такой же ранг (для подкидывания в "Дураке")
        /// </summary>
        /// <param name="other">Карта для сравнения</param>
        /// <returns>true если ранги совпадают</returns>
        public bool HasSameRank(CardData other)
        {
            if (IsJoker || other.IsJoker)
            {
                return false;
            }

            return Rank == other.Rank;
        }

        /// <summary>
        ///     Проверяет, имеет ли карта такую же масть
        /// </summary>
        /// <param name="other">Карта для сравнения</param>
        /// <returns>true если масти совпадают</returns>
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
        ///     Проверяет равенство двух карт
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
        ///     Возвращает строковое представление карты
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
        ///     Возвращает полное название карты на русском языке
        /// </summary>
        public string ToRussianString()
        {
            if (IsJoker)
            {
                return IsRedJoker ? "Красный Джокер" : "Чёрный Джокер";
            }

            return $"{Rank.ToRussianName()} {Suit.ToRussianName()}";
        }
    }
}