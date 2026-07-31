using System.Reflection;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     A trick must always be banked somewhere. Player and opponent hands are two independent optional
    ///     components, so the decision "move visuals into a hand" or "enqueue the data" belongs to the
    ///     winning side alone — exactly how <c>MoveAllWarCardsToWinnerAsync</c> already branches. In a mixed
    ///     hand/queue setup the old shared condition dropped both cards of every trick the hand-less side
    ///     won, shrinking its own count until <c>CheckGameEnd</c> declared the wrong winner.
    ///     <para>
    ///         Only the queue-driven winner cases are exercised here: the hand-driven branch awaits
    ///         <c>UniTask.Delay</c>, which never resumes in EditMode because no player loop runs.
    ///     </para>
    /// </summary>
    public sealed class AuditFixesCardsTests
    {
        private static readonly CardData PlayerCard = new(Suit.Hearts, Rank.Five);
        private static readonly CardData OpponentCard = new(Suit.Spades, Rank.King);

        [Test]
        public void MoveCardsToWinner_OpponentWinsWithOnlyAPlayerHand_BanksBothCardsInTheOpponentQueue()
        {
            GameObject root = new("AuditFixesDrunkardPlayerHandOnly");

            try
            {
                DrunkardGame game = root.AddComponent<DrunkardGame>();
                SetPrivate(game, "_playerDeckPosition", CreateHand(root, "PlayerHand"));

                Assert.That(game.UsePlayerHand, Is.True);
                Assert.That(game.UseOpponentHand, Is.False);

                RunMoveCardsToWinner(game, false);

                Assert.That(game.OpponentCardCount, Is.EqualTo(2),
                    "The opponent has no hand, so its winnings must land in the data queue.");
                Assert.That(game.PlayerCardCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MoveCardsToWinner_PlayerWinsWithOnlyAnOpponentHand_BanksBothCardsInThePlayerQueue()
        {
            GameObject root = new("AuditFixesDrunkardOpponentHandOnly");

            try
            {
                DrunkardGame game = root.AddComponent<DrunkardGame>();
                SetPrivate(game, "_opponentDeckPosition", CreateHand(root, "OpponentHand"));

                Assert.That(game.UsePlayerHand, Is.False);
                Assert.That(game.UseOpponentHand, Is.True);

                RunMoveCardsToWinner(game, true);

                Assert.That(game.PlayerCardCount, Is.EqualTo(2),
                    "The player has no hand, so its winnings must land in the data queue.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MoveCardsToWinner_WithoutAnyHand_KeepsBankingBothCardsForTheWinner()
        {
            GameObject root = new("AuditFixesDrunkardNoHands");

            try
            {
                DrunkardGame game = root.AddComponent<DrunkardGame>();

                Assert.That(game.UsePlayerHand, Is.False);
                Assert.That(game.UseOpponentHand, Is.False);

                RunMoveCardsToWinner(game, true);
                Assert.That(game.PlayerCardCount, Is.EqualTo(2));

                RunMoveCardsToWinner(game, false);
                Assert.That(game.OpponentCardCount, Is.EqualTo(2));
                Assert.That(game.PlayerCardCount, Is.EqualTo(2), "The loser's queue must not change.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void RunMoveCardsToWinner(DrunkardGame game, bool playerWins)
        {
            MethodInfo method = typeof(DrunkardGame).GetMethod(
                "MoveCardsToWinnerAsync",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool), typeof(CardData), typeof(CardData) },
                null);
            Assert.That(method, Is.Not.Null, "Method `MoveCardsToWinnerAsync(bool, CardData, CardData)` was not found.");

            UniTask task = (UniTask)method.Invoke(game, new object[] { playerWins, PlayerCard, OpponentCard });
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True,
                "The queue-driven winner path must not await anything, otherwise EditMode cannot observe it.");
            awaiter.GetResult();
        }

        private static Transform CreateHand(GameObject root, string name)
        {
            GameObject hand = new(name);
            hand.transform.SetParent(root.transform);
            hand.AddComponent<HandComponent>();
            return hand.transform;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field `{fieldName}` was not found.");
            field.SetValue(target, value);
        }
    }
}
