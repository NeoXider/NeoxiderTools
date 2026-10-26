# Deck configuration inspector

`Neo.Cards.Editor` supplies the custom inspector for [DeckConfig](./DeckConfig.md), the reusable asset described in the [Cards module guide](./README.md). Create an asset with **Create > Neoxider > Cards > Deck Config**, then select it in the Project window. This editor assembly is optional for gameplay; runtime code references `Neo.Cards`.

The inspector edits one selected configuration at a time. It authors card data and artwork; scene components consume that asset separately.

## Standard decks

Leave **Use Custom Deck** disabled. Choose **Sprite deck type** for the available artwork and **Play deck type** for the generated deck. A 36-card sprite set starts at six; 52- and 54-card sets start at two. Suit lists store sprites in ascending rank order. Jokers are shown as required artwork when either setting selects a 54-card deck; missing joker sprites are validation warnings.

Assign the back, suit lists, and jokers manually, or use **Auto-Fill From Folder...**:

1. Select an existing folder inside the project's `Assets` directory. The `Assets` root itself is supported; similarly named sibling directories such as `AssetsBackup` are rejected.
2. Auto-fill reads Sprite assets, including sprite-sheet sub-assets, in the selected folder and its subfolders.
3. Names are interpreted by `CardSpriteNameParser`: for example `hearts_02`, `ace_of_spades`, `KH`, `10c`, `туз пик`, `card_back`, and `joker_red`.
4. All four suit lists are resized and cleared before assignment. The folder supplies the complete standard suit set, so unmatched slots become empty. Existing back and joker assignments remain when the folder has no replacement for them. Custom card entries remain intact.
5. The first candidate for a slot wins. Further distinct candidates are reported as conflicts for suits, backs, and both jokers. Discovery order is not an artwork-selection policy: keep one candidate per slot in production folders.
6. The summary dialog and explicit-action Console message report assigned **suit cards**, unrecognized names, out-of-range ranks, and conflicts. Backs and jokers do not increase the suit-card count. Undo restores the previous assignments.

Unrecognized names and missing cards do not abort auto-fill. Check **Configuration validation** after importing, and use Undo if the selected folder was incomplete. Cancelling folder selection, selecting an invalid folder, or finding no sprites leaves the configuration unchanged.

## Custom decks

Enable **Use Custom Deck** to edit **Custom Cards**. Each entry exposes its id, display name, sort value, group, and optional face sprite. Standard suit settings remain stored when switching modes. The back sprite is shared by both modes; standard suit controls and name-based auto-fill are hidden in custom mode.

Use unique nonempty ids. Empty/duplicate ids and an empty custom deck are errors. Missing custom face sprites and a missing back are warnings, allowing custom views or sprite overrides to supply artwork. See [DeckConfig](./DeckConfig.md) for the runtime custom-id contract.

## Scene and Inspector/NoCode workflow

1. Author the `DeckConfig` asset, choose its standard/custom mode, and review **Configuration validation**. The asset stores reusable data; scene references belong on components.
2. Assign the asset to the **Config** field of a [DeckComponent](./DeckComponent.md), and assign a **Card Prefab** when cards need scene visuals. Configure the prefab's view and the deck's spawn point in those components.
3. Use **Initialize On Start**, or call `DeckComponent.Initialize()` from C# or a UnityEvent. Initialization reads the asset's selected mode. `Shuffle()` and the component's documented draw/deal methods provide subsequent gameplay actions; authoring the asset does not initialize an existing scene deck automatically.
4. For a standalone [CardComponent](./CardComponent.md), assign its **Config** directly. Code can also set `CardComponent.Config` before setting card data. Cards spawned by `DeckComponent` receive that deck's configuration.

The inspector needs no additional NoCode bridge. UnityEvents call existing component methods; pure C# consumers can generate card data without creating scene components.

## C# path and validation

Given an assigned `DeckConfig deckConfig`, the same authored data is available through the runtime API:

```csharp
using System.Collections.Generic;
using Neo.Cards;
using UnityEngine;

List<CardData> cards = deckConfig.GenerateDeck();
bool valid = deckConfig.Validate(out List<string> errors, out List<string> warnings);

if (cards.Count > 0)
{
    Sprite face = deckConfig.GetSprite(cards[0]);
}
```

`GenerateDeck()` selects the configured standard/custom mode. `GenerateCustomDeck()` always reads custom entries; `GenerateDeck(DeckType)` explicitly generates a standard deck. The inspector applies serialized edits before calling `Validate`, so the displayed diagnostics describe the currently applied asset values. Validation does not automatically change assets or scenes.

Folder auto-fill is a private inspector operation, with no supported public batch-import API. Custom sprite loaders can reuse the runtime `CardSpriteNameParser`; runtime consumers do not need to reference `Neo.Cards.Editor` or `UnityEditor`.

Previews use Unity's cached asset previews, with a thumbnail fallback while a preview is loading. They preserve aspect ratio and avoid direct rectangle access on tightly packed atlas sprites. Preview textures belong to Unity's editor cache. Repaint requests follow Unity's global preview-loading state, which can include previews requested by other inspectors.

## Compatibility

Existing assets keep their serialized field names and standard/custom data when switching modes. Folder auto-fill still replaces the four standard suit lists rather than merging missing cards. Conflicting back and joker sprites now follow the same first-candidate rule as suit sprites, so check the conflict report when upgrading an artwork folder that previously depended on silent replacement.

See [Cards](./README.md) for the canonical module entry, [DeckConfig](./DeckConfig.md) for configuration and parsing, [CardData](./CardData.md) for generated values, and the [CardComponent](./CardComponent.md) / [DeckComponent](./DeckComponent.md) guides for scene integration.
