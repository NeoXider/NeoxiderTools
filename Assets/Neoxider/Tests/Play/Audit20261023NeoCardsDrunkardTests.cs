using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoCardsDrunkardTests
    {
        [UnityTest]
        public IEnumerator ShortWar_WithSceneHands_RetainsReturnedCardsAndCanPlayAnotherRound()
        {
            GameObject root = new("AuditCardsShortWar");
            root.SetActive(false);
            DeckConfig config = ScriptableObject.CreateInstance<DeckConfig>();
            try
            {
                HandComponent player = Child(root, "Player").AddComponent<HandComponent>();
                HandComponent opponent = Child(root, "Opponent").AddComponent<HandComponent>();
                DeckComponent deck = Child(root, "Deck").AddComponent<DeckComponent>();
                CardComponent prefab = Child(root, "Prefab").AddComponent<CardComponent>();
                SetField(prefab, "_flipDuration", 0f);
                SetField(deck, "_config", config);
                SetField(deck, "_cardPrefab", prefab);
                SetField(deck, "_initializeOnStart", false);
                SetField(deck, "_shuffleOnStart", false);
                DrunkardGame game = root.AddComponent<DrunkardGame>();
                SetField(game, "_deckComponent", deck);
                SetField(game, "_initializeOnStart", false);
                SetField(game, "_playerDeckPosition", player.transform);
                SetField(game, "_opponentDeckPosition", opponent.transform);
                SetField(game, "_turnDelay", 0f);
                SetField(game, "_roundDelay", 0f);
                SetField(game, "_cardReturnDelay", 0f);
                SetField(game, "_warContinueDelay", 0f);
                SetField(player, "_arrangeDuration", 0f);
                SetField(opponent, "_arrangeDuration", 0f);
                UnityEvent playerWin = new();
                UnityEvent opponentWin = new();
                SetField(game, "_onPlayerWin", playerWin);
                SetField(game, "_onOpponentWin", opponentWin);
                int winnerEvents = 0;
                playerWin.AddListener(() => winnerEvents++);
                opponentWin.AddListener(() => winnerEvents++);
                root.SetActive(true);

                CardComponent playerCard = Child(root, "PlayerCard").AddComponent<CardComponent>();
                playerCard.SetData(new CardData(Suit.Hearts, Rank.Five));
                CardComponent opponentCard = Child(root, "OpponentCard").AddComponent<CardComponent>();
                opponentCard.SetData(new CardData(Suit.Spades, Rank.Five));
                player.AddCard(playerCard);
                opponent.AddCard(opponentCard);
                Assert.That(game.PlayerCardCount, Is.EqualTo(1));
                Assert.That(game.OpponentCardCount, Is.EqualTo(1));

                yield return game.PlayRound().ToCoroutine();

                Assert.That(game.PlayerCardCount, Is.EqualTo(1), "The tied card is returned to the data reserve.");
                Assert.That(game.OpponentCardCount, Is.EqualTo(1));
                Assert.That(game.IsPlaying, Is.False);
                Assert.That(winnerEvents, Is.Zero, "Insufficient cards for war must not declare a false winner.");

                yield return game.PlayRound().ToCoroutine();

                Assert.That(game.PlayerCardCount, Is.EqualTo(1));
                Assert.That(game.OpponentCardCount, Is.EqualTo(1));
                Assert.That(winnerEvents, Is.Zero);

                SetField(player, "_maxCards", 1);
                SetField(opponent, "_maxCards", 1);
                game.RestartGame();
                yield return null;

                Assert.That(player.Count, Is.EqualTo(1));
                Assert.That(opponent.Count, Is.EqualTo(1));
                Assert.That(game.PlayerCardCount, Is.EqualTo(27));
                Assert.That(game.OpponentCardCount, Is.EqualTo(27));

                yield return game.PlayRound().ToCoroutine();

                Assert.That(game.PlayerCardCount + game.OpponentCardCount, Is.EqualTo(54),
                    "Initial-deal overflow and war winnings must retain every card with finite scene hands.");
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(config);
            }

            yield return null;
        }

        private static GameObject Child(GameObject root, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Serialized fixture field: " + name);
            field.SetValue(target, value);
        }
    }
}
