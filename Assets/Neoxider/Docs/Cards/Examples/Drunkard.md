# DrunkardGame

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

Assign a `DeckComponent` with a valid config and card prefab. Each side can use a scene `HandComponent` (on its deck-position transform) or a data queue. Call `Play()` from a button/UnityEvent, or await `PlayRound()` from C#; `IsPlaying` prevents overlapping round starts. Subscribe to card-count and winner events for UI.

For a Canvas-based game, keep the UI spawn point inside that Canvas: [DeckComponent](../DeckComponent.md#ui-card-creation)
then retains the card prefab's local size. Hand and board transfers preserve the card's existing world pose.
Give centered card stacks enough distance from the screen edge for half a card plus the configured stack
offset and padding. In the bundled CardGame, the `380 × 554`, scale `0.7` cards use separate player/bot
edge offsets so both complete piles remain visible.

Для UI-карт точка появления должна находиться под Canvas. Масштаб задаётся префабом, а не обратной
компенсацией Canvas. Центрированную стопку нельзя ставить точно на границу экрана: учитывайте половину
высоты карты и смещение всей стопки. Это касается и начальной раздачи, и возвращённых после раунда карт.

`PlayerCardCount` and `OpponentCardCount` include both visible hand cards and queued reserve data. If a tie cannot continue because either side lacks two more cards, the pile is returned to each side's reserve. Those cards remain playable in later rounds even when a scene hand is assigned; an empty visual hand alone does not mean that side has lost.

If a finite scene hand fills during initial dealing or banking a trick/war pile, further cards are stored in that side's data reserve. Counts include these overflow cards; they are drawn after the visible hand is exhausted.

`RestartGame()` is safe during a turn delay, move, flip, war or card return. It cancels the old game generation before clearing views/queues and dealing again. Each awaited continuation and callback boundary checks that generation. An old round cannot award cards, publish winner/war/round completion events or clear `IsPlaying` for a replacement round. Starting a round during the redeal is ignored; use `OnGameRestarted` when UI should enable the next turn.

Active trick/war views are tracked before their first animation and released from table ownership before admission to a hand. Restart cleans the distinct table-owned objects; already banked views belong to the receiving hand. The public `PlayRound()` task completes quietly when its game is replaced, while standalone concrete card cancellation APIs report cancellation. Existing serialized fields, Inspector events and public entry points remain available.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `GameStarted` | Game Started. |
| `IsPlaying` | Is Playing. |
| `OnGameRestarted` | On Game Restarted. |
| `OnGameStarted` | On Game Started. |
| `OnOpponentCardCountChanged` | On Opponent Card Count Changed. |
| `OnOpponentWin` | On Opponent Win. |
| `OnOpponentWonRound` | On Opponent Won Round. |
| `OnPlayerCardCountChanged` | On Player Card Count Changed. |
| `OnPlayerWin` | On Player Win. |
| `OnPlayerWonRound` | On Player Won Round. |
| `OnRoundEnded` | On Round Ended. |
| `OnRoundStarted` | On Round Started. |
| `OnWarEnded` | On War Ended. |
| `OnWarStarted` | On War Started. |
| `OpponentCardCount` | Opponent Card Count. |
| `OpponentHand` | Opponent Hand. |
| `PlayerCardCount` | Player Card Count. |
| `PlayerHand` | Player Hand. |
| `UseOpponentHand` | Use Opponent Hand. |
| `UsePlayerHand` | Use Player Hand. |
| `_cardMoveDuration` | Card Move Duration. |
| `_cardReturnDelay` | Card Return Delay. |
| `_cardsParent` | Cards Parent. |
| `_debug` | Debug. |
| `_deckComponent` | Deck Component. |
| `_initialBoard` | Initial Board. |
| `_onGameRestarted` | On Game Restarted. |
| `_onGameStarted` | On Game Started. |
| `_onOpponentCardCountChanged` | On Opponent Card Count Changed. |
| `_onOpponentWin` | On Opponent Win. |
| `_onOpponentWonRound` | On Opponent Won Round. |
| `_onPlayerCardCountChanged` | On Player Card Count Changed. |
| `_onPlayerWin` | On Player Win. |
| `_onPlayerWonRound` | On Player Won Round. |
| `_onRoundEnded` | On Round Ended. |
| `_onRoundStarted` | On Round Started. |
| `_onWarEnded` | On War Ended. |
| `_onWarStarted` | On War Started. |
| `_opponentCardPosition` | Opponent Card Position. |
| `_opponentDeckPosition` | Opponent Deck Position. |
| `_playerCardPosition` | Player Card Position. |
| `_playerDeckPosition` | Player Deck Position. |
| `_turnDelay` | Turn Delay. |
| `_warContinueDelay` | War Continue Delay. |

## See Also

- [Module Root](../README.md)
