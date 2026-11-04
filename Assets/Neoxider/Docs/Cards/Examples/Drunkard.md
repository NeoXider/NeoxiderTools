# DrunkardGame

**What it is:** `DrunkardGame` (`Scripts/Cards/Drunkard/DrunkardGame.cs`, namespace `Neo.Cards`) is a complete two-player "Drunkard" (War) game built only from the public Cards API: `DeckComponent` deals, two `HandComponent` piles (or data queues) hold each side's cards, and an optional `BoardComponent` shows the initial deal. Use it as a playable reference for composing your own card game. The ready scene is described in [Cards README → Demo scene](../README.md#demo-scene).

**How to use:**
1. Add the component (menu `Neoxider/Cards/DrunkardGame`) and assign a `DeckComponent` with a valid config and
   card prefab.
2. Set each side's deck and card positions. A `HandComponent` on a side's deck-position transform shows that
   side's pile; without one the side keeps its cards in a data queue.
3. Call `Play()` from a button/UnityEvent, or await `PlayRound()` from C#; subscribe to the card-count and winner
   events for UI.

---

## Setup

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

With an **Initial Board**, the whole deck is drawn first and then placed on that board in one batch (`BoardComponent.PlaceCardsImmediate`, a single layout pass instead of one per card) before it is split between the sides; the board's `OnCardPlaced` events arrive after the whole deck is on it. The deck then leaves the board in one batch too (`BoardComponent.RemoveCards`, one layout pass, no `OnBoardCleared`) and is dealt to the sides.

This also covers a restart from Unity hierarchy callbacks while an initial card is parented. Once the replacement setup completes, the interrupted setup cannot place its old card into the replacement board or hands; deferred destruction removes only that obsolete card.

Это касается и перезапуска из колбэков иерархии Unity во время начальной раскладки: после завершения новой раздачи прерванная раздача не кладёт свою старую карту ни на новую доску, ни в руки, а отложенное уничтожение удаляет только эту устаревшую карту.

Active trick/war views are tracked before their first animation and released from table ownership before admission to a hand. Restart cleans the distinct table-owned objects; already banked views belong to the receiving hand. The public `PlayRound()` task completes quietly when its game is replaced, while standalone concrete card cancellation APIs report cancellation. Existing serialized fields, Inspector events and public entry points remain available.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_deckComponent` | Required deck; provides the config and card prefab for every dealt and spawned card. |
| `_initializeOnStart` | Deal a new game in `Start`. |
| `_debug` / `_debugWarnings` | Gated diagnostic logs (deal summary, drawn cards) and warnings. |
| `_cardsParent` | Parent for table cards during a round (falls back to this object). |
| `_initialBoard` | Optional board that shows the whole deck before it is split between the sides. |
| `_playerDeckPosition` / `_opponentDeckPosition` | Where each side's cards come from; a `HandComponent` here becomes that side's visual pile. |
| `_playerCardPosition` / `_opponentCardPosition` | Where each side's played card lands on the table. |
| `_cardMoveDuration` | Seconds for a card flight. |
| `_roundDelay` / `_turnDelay` / `_warContinueDelay` / `_cardReturnDelay` | Pauses after a round, between turns, between war steps and between returned cards. |
| `_playerGoesFirst` | Which side plays its card first. |
| `_onPlayerCardCountChanged` / `_onOpponentCardCountChanged` | Card totals (visible hand plus reserve) for score labels. |
| `_onGameStarted` / `_onGameRestarted` / `_onPlayerWin` / `_onOpponentWin` | Game flow events. |
| `_onRoundStarted` / `_onRoundEnded` / `_onPlayerWonRound` / `_onOpponentWonRound` / `_onWarStarted` / `_onWarEnded` | Round and war events. |

C# accessors: `GameStarted`, `IsPlaying`, `PlayerCardCount`, `OpponentCardCount`, `PlayerHand`, `OpponentHand`, `UsePlayerHand`, `UseOpponentHand` and the `On...` event properties that expose the fields above.

## See Also

- [Module Root](../README.md)
