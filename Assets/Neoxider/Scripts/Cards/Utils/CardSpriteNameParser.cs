using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Neo.Cards
{
    /// <summary>
    ///     What a parsed sprite name represents.
    /// </summary>
    public enum CardSpriteKind
    {
        /// <summary>
        ///     Name was not recognized.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Regular playing card (suit + rank).
        /// </summary>
        Card = 1,

        /// <summary>
        ///     Card back.
        /// </summary>
        Back = 2,

        /// <summary>
        ///     Red joker.
        /// </summary>
        JokerRed = 3,

        /// <summary>
        ///     Black joker.
        /// </summary>
        JokerBlack = 4
    }

    /// <summary>
    ///     Result of parsing a card sprite name.
    /// </summary>
    public readonly struct CardSpriteParseResult
    {
        /// <summary>
        ///     Creates a parse result.
        /// </summary>
        /// <param name="kind">Recognized sprite kind.</param>
        /// <param name="suit">Suit (valid when <see cref="Kind" /> is <see cref="CardSpriteKind.Card" />).</param>
        /// <param name="rank">Rank (valid when <see cref="Kind" /> is <see cref="CardSpriteKind.Card" />).</param>
        public CardSpriteParseResult(CardSpriteKind kind, Suit suit = Suit.Hearts, Rank rank = Rank.Two)
        {
            Kind = kind;
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        ///     Recognized sprite kind.
        /// </summary>
        public CardSpriteKind Kind { get; }

        /// <summary>
        ///     Card suit (only meaningful for <see cref="CardSpriteKind.Card" />).
        /// </summary>
        public Suit Suit { get; }

        /// <summary>
        ///     Card rank (only meaningful for <see cref="CardSpriteKind.Card" />).
        /// </summary>
        public Rank Rank { get; }
    }

    /// <summary>
    ///     Parses card sprite file names into suit/rank/back/joker data.
    ///     Supports English and Russian tokens, numeric ranks (2-14), short forms
    ///     ("AS", "10h", "qd") and common separators ("_", "-", " ", ".").
    ///     Examples: "hearts_02", "ace_of_spades", "KH", "card_back", "joker_red",
    ///     "туз пик", "дама_червы".
    /// </summary>
    public static class CardSpriteNameParser
    {
        private static readonly Dictionary<string, Suit> SuitTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            { "hearts", Suit.Hearts }, { "heart", Suit.Hearts }, { "h", Suit.Hearts },
            { "червы", Suit.Hearts }, { "черви", Suit.Hearts }, { "червей", Suit.Hearts }, { "черв", Suit.Hearts },
            { "diamonds", Suit.Diamonds }, { "diamond", Suit.Diamonds }, { "d", Suit.Diamonds },
            { "бубны", Suit.Diamonds }, { "бубен", Suit.Diamonds }, { "буби", Suit.Diamonds }, { "бубей", Suit.Diamonds },
            { "clubs", Suit.Clubs }, { "club", Suit.Clubs }, { "c", Suit.Clubs },
            { "трефы", Suit.Clubs }, { "треф", Suit.Clubs }, { "крести", Suit.Clubs }, { "крест", Suit.Clubs },
            { "spades", Suit.Spades }, { "spade", Suit.Spades }, { "s", Suit.Spades },
            { "пики", Suit.Spades }, { "пик", Suit.Spades }
        };

        private static readonly Dictionary<string, Rank> RankWordTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            { "jack", Rank.Jack }, { "j", Rank.Jack }, { "валет", Rank.Jack },
            { "queen", Rank.Queen }, { "q", Rank.Queen }, { "дама", Rank.Queen },
            { "king", Rank.King }, { "k", Rank.King }, { "король", Rank.King },
            { "ace", Rank.Ace }, { "a", Rank.Ace }, { "туз", Rank.Ace },
            { "ten", Rank.Ten }, { "nine", Rank.Nine }, { "eight", Rank.Eight },
            { "seven", Rank.Seven }, { "six", Rank.Six }, { "five", Rank.Five },
            { "four", Rank.Four }, { "three", Rank.Three }, { "two", Rank.Two }
        };

        private static readonly HashSet<string> BackTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "back", "cardback", "shirt", "cover", "рубашка"
        };

        private static readonly HashSet<string> JokerTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "joker", "джокер"
        };

        private static readonly HashSet<string> RedTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "red", "красный"
        };

        private static readonly HashSet<string> BlackTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "black", "черный", "чёрный"
        };

        private static readonly HashSet<string> IgnoredTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "of", "card", "карта", "sprite", "png"
        };

        private static readonly Regex TokenSplitRegex = new(@"[^\p{L}\p{Nd}]+", RegexOptions.Compiled);

        private static readonly Regex CompactCardRegex = new(
            "^(?<rank>10|[2-9]|[jqka])(?<suit>[hdcs])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        ///     Tries to parse a sprite name into card data.
        /// </summary>
        /// <param name="spriteName">Sprite or file name (extension allowed).</param>
        /// <param name="result">Parse result when recognized.</param>
        /// <returns>True when the name maps to a card, back, or joker.</returns>
        public static bool TryParse(string spriteName, out CardSpriteParseResult result)
        {
            result = new CardSpriteParseResult(CardSpriteKind.Unknown);

            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return false;
            }

            List<string> tokens = Tokenize(spriteName);
            if (tokens.Count == 0)
            {
                return false;
            }

            if (ContainsAny(tokens, BackTokens))
            {
                result = new CardSpriteParseResult(CardSpriteKind.Back);
                return true;
            }

            if (ContainsAny(tokens, JokerTokens))
            {
                bool isBlack = ContainsAny(tokens, BlackTokens);
                result = new CardSpriteParseResult(isBlack ? CardSpriteKind.JokerBlack : CardSpriteKind.JokerRed);
                return true;
            }

            Suit? suit = null;
            Rank? rank = null;

            foreach (string token in tokens)
            {
                if (IgnoredTokens.Contains(token))
                {
                    continue;
                }

                if (suit == null && rank == null)
                {
                    Match compact = CompactCardRegex.Match(token);
                    if (compact.Success && token.Length > 1)
                    {
                        rank = ParseRankToken(compact.Groups["rank"].Value);
                        suit = SuitTokens[compact.Groups["suit"].Value];
                        continue;
                    }
                }

                if (rank == null)
                {
                    Rank? parsedRank = ParseRankToken(token);
                    if (parsedRank != null)
                    {
                        rank = parsedRank;
                        continue;
                    }
                }

                if (suit == null && SuitTokens.TryGetValue(token, out Suit parsedSuit))
                {
                    suit = parsedSuit;
                }
            }

            if (suit == null || rank == null)
            {
                return false;
            }

            result = new CardSpriteParseResult(CardSpriteKind.Card, suit.Value, rank.Value);
            return true;
        }

        /// <summary>
        ///     Builds a canonical sprite name for a card ("hearts_02" ... "spades_14").
        /// </summary>
        /// <param name="suit">Card suit.</param>
        /// <param name="rank">Card rank.</param>
        /// <returns>Canonical lower-case name.</returns>
        public static string GetCanonicalName(Suit suit, Rank rank)
        {
            return $"{suit.ToEnglishName().ToLowerInvariant()}_{(int)rank:00}";
        }

        private static List<string> Tokenize(string name)
        {
            string[] rawTokens = TokenSplitRegex.Split(name.ToLowerInvariant());
            List<string> tokens = new(rawTokens.Length);

            foreach (string token in rawTokens)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        private static bool ContainsAny(List<string> tokens, HashSet<string> set)
        {
            foreach (string token in tokens)
            {
                if (set.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rank? ParseRankToken(string token)
        {
            if (int.TryParse(token, out int numeric))
            {
                if (numeric >= (int)Rank.Two && numeric <= (int)Rank.Ace)
                {
                    return (Rank)numeric;
                }

                return null;
            }

            if (RankWordTokens.TryGetValue(token, out Rank rank))
            {
                return rank;
            }

            return null;
        }
    }
}
