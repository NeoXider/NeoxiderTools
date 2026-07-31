using Neo.Cards;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class CardSpriteNameParserTests
    {
        [TestCase("hearts_02", Suit.Hearts, Rank.Two)]
        [TestCase("hearts_14", Suit.Hearts, Rank.Ace)]
        [TestCase("diamonds_10", Suit.Diamonds, Rank.Ten)]
        [TestCase("clubs_11", Suit.Clubs, Rank.Jack)]
        [TestCase("spades_13", Suit.Spades, Rank.King)]
        [TestCase("ace_of_spades", Suit.Spades, Rank.Ace)]
        [TestCase("queen_of_hearts", Suit.Hearts, Rank.Queen)]
        [TestCase("2_of_clubs", Suit.Clubs, Rank.Two)]
        [TestCase("card_J_diamonds", Suit.Diamonds, Rank.Jack)]
        [TestCase("Hearts-Q", Suit.Hearts, Rank.Queen)]
        [TestCase("10 spades", Suit.Spades, Rank.Ten)]
        public void TryParse_StandardNames_ReturnsCard(string name, Suit expectedSuit, Rank expectedRank)
        {
            bool parsed = CardSpriteNameParser.TryParse(name, out CardSpriteParseResult result);

            Assert.IsTrue(parsed, $"'{name}' should parse");
            Assert.AreEqual(CardSpriteKind.Card, result.Kind);
            Assert.AreEqual(expectedSuit, result.Suit);
            Assert.AreEqual(expectedRank, result.Rank);
        }

        [TestCase("AS", Suit.Spades, Rank.Ace)]
        [TestCase("kh", Suit.Hearts, Rank.King)]
        [TestCase("10c", Suit.Clubs, Rank.Ten)]
        [TestCase("qd", Suit.Diamonds, Rank.Queen)]
        [TestCase("2s", Suit.Spades, Rank.Two)]
        public void TryParse_CompactNames_ReturnsCard(string name, Suit expectedSuit, Rank expectedRank)
        {
            bool parsed = CardSpriteNameParser.TryParse(name, out CardSpriteParseResult result);

            Assert.IsTrue(parsed, $"'{name}' should parse");
            Assert.AreEqual(CardSpriteKind.Card, result.Kind);
            Assert.AreEqual(expectedSuit, result.Suit);
            Assert.AreEqual(expectedRank, result.Rank);
        }

        [TestCase("туз пик", Suit.Spades, Rank.Ace)]
        [TestCase("дама_червы", Suit.Hearts, Rank.Queen)]
        [TestCase("валет бубны", Suit.Diamonds, Rank.Jack)]
        [TestCase("король_трефы", Suit.Clubs, Rank.King)]
        [TestCase("7_пики", Suit.Spades, Rank.Seven)]
        public void TryParse_RussianNames_ReturnsCard(string name, Suit expectedSuit, Rank expectedRank)
        {
            bool parsed = CardSpriteNameParser.TryParse(name, out CardSpriteParseResult result);

            Assert.IsTrue(parsed, $"'{name}' should parse");
            Assert.AreEqual(CardSpriteKind.Card, result.Kind);
            Assert.AreEqual(expectedSuit, result.Suit);
            Assert.AreEqual(expectedRank, result.Rank);
        }

        [TestCase("card_back", CardSpriteKind.Back)]
        [TestCase("back", CardSpriteKind.Back)]
        [TestCase("рубашка", CardSpriteKind.Back)]
        [TestCase("joker_red", CardSpriteKind.JokerRed)]
        [TestCase("joker", CardSpriteKind.JokerRed)]
        [TestCase("joker_black", CardSpriteKind.JokerBlack)]
        [TestCase("джокер черный", CardSpriteKind.JokerBlack)]
        public void TryParse_SpecialNames_ReturnsKind(string name, CardSpriteKind expectedKind)
        {
            bool parsed = CardSpriteNameParser.TryParse(name, out CardSpriteParseResult result);

            Assert.IsTrue(parsed, $"'{name}' should parse");
            Assert.AreEqual(expectedKind, result.Kind);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("background_game")]
        [TestCase("Карта-1")]
        [TestCase("button_close")]
        [TestCase("hearts")]
        [TestCase("14")]
        public void TryParse_InvalidNames_ReturnsFalse(string name)
        {
            bool parsed = CardSpriteNameParser.TryParse(name, out _);

            Assert.IsFalse(parsed, $"'{name}' should not parse");
        }

        [Test]
        public void GetCanonicalName_FormatsAsSuitAndPaddedRank()
        {
            Assert.AreEqual("hearts_02", CardSpriteNameParser.GetCanonicalName(Suit.Hearts, Rank.Two));
            Assert.AreEqual("spades_14", CardSpriteNameParser.GetCanonicalName(Suit.Spades, Rank.Ace));
            Assert.AreEqual("diamonds_11", CardSpriteNameParser.GetCanonicalName(Suit.Diamonds, Rank.Jack));
        }
    }
}
