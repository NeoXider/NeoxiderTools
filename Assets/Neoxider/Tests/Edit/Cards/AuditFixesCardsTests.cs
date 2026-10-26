using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
    ///         Queue cases are synchronous C# contracts through the current operation seam, including
    ///         rejected cancelled/replaced operations. The separate both-hand case enters native Play
    ///         Mode and observes real hand admission; it does not exercise the entire game round.
    ///     </para>
    /// </summary>
    public sealed class AuditFixesCardsTests
    {
        private static readonly CardData PlayerCard = new(Suit.Hearts, Rank.Five);
        private static readonly CardData OpponentCard = new(Suit.Spades, Rank.King);

        [UnityTearDown]
        public IEnumerator ExitNativeHandBanking()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
        }

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
                DestroyOwnedGame(root);
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
                DestroyOwnedGame(root);
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
                DestroyOwnedGame(root);
            }
        }

        private static void RunMoveCardsToWinner(DrunkardGame game, bool playerWins)
        {
            RunMoveCardsToWinner(game, playerWins, BeginOperation(game));
        }

        private static object BeginOperation(DrunkardGame game)
        {
            MethodInfo begin = typeof(DrunkardGame).GetMethod("BeginNewGame",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(begin, Is.Not.Null, "The operation seam must start a real game generation/token.");
            return begin.Invoke(game, null);
        }

        private static void RunMoveCardsToWinner(DrunkardGame game, bool playerWins, object operation)
        {
            Type operationType = typeof(DrunkardGame).GetNestedType("GameOperation", BindingFlags.NonPublic);
            Assert.That(operationType, Is.Not.Null);
            Assert.That(operation, Is.TypeOf(operationType));
            MethodInfo method = typeof(DrunkardGame).GetMethod(
                "MoveCardsToWinnerAsync",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool), typeof(CardData), typeof(CardData), operationType },
                null);
            Assert.That(method, Is.Not.Null,
                "The banking seam must include the current GameOperation ownership/generation context.");

            UniTask task = (UniTask)method.Invoke(game, new object[] { playerWins, PlayerCard, OpponentCard, operation });
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True,
                "The queue-driven winner path must not await anything, otherwise EditMode cannot observe it.");
            awaiter.GetResult();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MoveCardsToWinner_CancelledOrReplacedOperation_CannotBankAgain(bool replaceGeneration)
        {
            GameObject root = new GameObject("AuditFixesDrunkardCancelledBanking");
            try
            {
                DrunkardGame game = root.AddComponent<DrunkardGame>();
                object operation = BeginOperation(game);
                RunMoveCardsToWinner(game, true, operation);
                Assert.That(game.PlayerCardCount, Is.EqualTo(2));
                Assert.That(game.OpponentCardCount, Is.Zero);

                if (replaceGeneration) BeginOperation(game);
                else GetCancellation(game).Cancel();

                Assert.Throws<OperationCanceledException>(() => RunMoveCardsToWinner(game, false, operation));
                Assert.That(game.PlayerCardCount, Is.EqualTo(2), "A rejected operation cannot remove accepted cards.");
                Assert.That(game.OpponentCardCount, Is.Zero, "A rejected operation cannot bank another trick.");

                RunMoveCardsToWinner(game, false, BeginOperation(game));
                Assert.That(game.PlayerCardCount, Is.EqualTo(2));
                Assert.That(game.OpponentCardCount, Is.EqualTo(2), "A fresh operation must still bank both cards.");
            }
            finally
            {
                DestroyOwnedGame(root);
            }
        }

        private static CancellationTokenSource GetCancellation(DrunkardGame game)
        {
            FieldInfo field = typeof(DrunkardGame).GetField("_gameCancellation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            CancellationTokenSource cancellation = (CancellationTokenSource)field.GetValue(game);
            Assert.That(cancellation, Is.Not.Null);
            return cancellation;
        }

        [UnityTest]
        public IEnumerator MoveCardsToWinner_BothHands_BanksBothRealViewsForEitherWinner()
        {
            yield return new EnterPlayMode();
            foreach (bool playerWins in new[] { true, false })
            {
                GameObject root = null;
                DeckConfig configuration = null;
                try
                {
                    DrunkardGame game = CreateNativeHandBankingGame(out root, out configuration);
                    DeckComponent deck = root.GetComponent<DeckComponent>();
                    Assert.That(deck.Config, Is.SameAs(configuration));
                    Assert.That(deck.CardPrefab, Is.Not.Null);
                    int firstFrame = Time.frameCount;
                    Stopwatch startDeadline = Stopwatch.StartNew();
                    while (Time.frameCount - firstFrame < 2 && startDeadline.Elapsed.TotalSeconds < 10d)
                        yield return null;
                    Assert.That(Time.frameCount - firstFrame, Is.GreaterThanOrEqualTo(2),
                        "Real Start must run with the owned deck prerequisites before banking.");
                    Assert.That(game.UsePlayerHand && game.UseOpponentHand, Is.True);
                    yield return ObserveBanking(game, playerWins, BeginOperation(game));

                    HandComponent winner = playerWins ? game.PlayerHand : game.OpponentHand;
                    HandComponent loser = playerWins ? game.OpponentHand : game.PlayerHand;
                    Assert.That(winner.Count, Is.EqualTo(2));
                    Assert.That(loser.Count, Is.Zero);
                    Assert.That(game.PlayerCardCount, Is.EqualTo(playerWins ? 2 : 0));
                    Assert.That(game.OpponentCardCount, Is.EqualTo(playerWins ? 0 : 2));
                    CollectionAssert.AreEquivalent(new[] { PlayerCard, OpponentCard },
                        new[] { winner.Cards[0].Data, winner.Cards[1].Data });
                    foreach (CardComponent card in winner.Cards)
                        Assert.That(card.transform.parent, Is.SameAs(winner.transform));
                }
                finally
                {
                    try { if (root != null) DestroyOwnedGame(root); }
                    finally { if (configuration != null) Object.DestroyImmediate(configuration); }
                }
            }
        }

        private static DrunkardGame CreateNativeHandBankingGame(out GameObject root, out DeckConfig configuration)
        {
            Assert.That(Application.isPlaying, Is.True);
            root = new GameObject("AuditFixesDrunkardNativeHands");
            root.SetActive(false);
            configuration = ScriptableObject.CreateInstance<DeckConfig>();
            DeckComponent deck = root.AddComponent<DeckComponent>();
            SetPrivate(deck, "_config", configuration);
            SetPrivate(deck, "_initializeOnStart", false);
            GameObject templateObject = new GameObject("OwnedCardTemplate");
            templateObject.transform.SetParent(root.transform);
            templateObject.SetActive(false);
            CardComponent template = templateObject.AddComponent<CardComponent>();
            template.Config = configuration;
            SetPrivate(deck, "_cardPrefab", template);
            DrunkardGame game = root.AddComponent<DrunkardGame>();
            SetPrivate(game, "_deckComponent", deck);
            SetPrivate(game, "_initializeOnStart", false);
            SetPrivate(game, "_playerDeckPosition", CreateHand(root, "PlayerHand"));
            SetPrivate(game, "_opponentDeckPosition", CreateHand(root, "OpponentHand"));
            SetPrivate(game, "_cardReturnDelay", 0f);
            foreach (HandComponent hand in root.GetComponentsInChildren<HandComponent>(true))
                SetPrivate(hand, "_arrangeDuration", 0f);

            GameObject playerObject = new GameObject("PlayerTableCard");
            playerObject.transform.SetParent(root.transform);
            CardComponent player = playerObject.AddComponent<CardComponent>();
            player.SetData(PlayerCard, false);
            SetPrivate(game, "_playerCardView", player);
            GameObject opponentObject = new GameObject("OpponentTableCard");
            opponentObject.transform.SetParent(root.transform);
            CardComponent opponent = opponentObject.AddComponent<CardComponent>();
            opponent.SetData(OpponentCard, false);
            SetPrivate(game, "_opponentCardView", opponent);
            root.SetActive(true);
            return game;
        }

        private static IEnumerator ObserveBanking(DrunkardGame game, bool playerWins, object operation)
        {
            MethodInfo method = typeof(DrunkardGame).GetMethod("MoveCardsToWinnerAsync",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(bool), typeof(CardData), typeof(CardData), operation.GetType() }, null);
            Assert.That(method, Is.Not.Null);
            UniTask task = (UniTask)method.Invoke(game, new object[] { playerWins, PlayerCard, OpponentCard, operation });
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Stopwatch deadline = Stopwatch.StartNew();
            while (!awaiter.IsCompleted && deadline.Elapsed.TotalSeconds < 10d) yield return null;
            Assert.That(awaiter.IsCompleted, Is.True, "Native hand banking must settle within the unscaled bound.");
            awaiter.GetResult();
        }

        private static void DestroyOwnedGame(GameObject root)
        {
            DrunkardGame game = root.GetComponent<DrunkardGame>();
            FieldInfo field = typeof(DrunkardGame).GetField("_gameCancellation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            CancellationTokenSource cancellation = game != null
                ? (CancellationTokenSource)field?.GetValue(game)
                : null;
            try
            {
                cancellation?.Cancel();
            }
            finally
            {
                try { Object.DestroyImmediate(root); }
                finally { cancellation?.Dispose(); }
            }
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
