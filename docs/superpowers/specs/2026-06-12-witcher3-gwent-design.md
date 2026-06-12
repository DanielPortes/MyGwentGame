# Witcher 3 Gwent Design

## Goal

Implement a playable Unity version of the original Gwent minigame from The Witcher 3: Wild Hunt, not the later standalone GWENT ruleset. The implementation must support faction selection, deck selection, the board, card play, round flow, scoring, abilities, local visual assets, and basic animations.

## Scope

The target ruleset is the 2015 base minigame: Northern Realms, Nilfgaardian Empire, Monsters, Scoia'tael, and Neutral cards. Skellige is excluded from the first complete implementation because it was added later in Blood and Wine, not the original 2015 game.

The repository currently contains only a small subset of original-looking card art and faction backs. The implementation will use local assets when they match a card or faction, and will render custom placeholder cards for missing art. It will not download or commit proprietary assets from the internet.

## Rules

- A match is best of three rounds.
- Each player chooses a faction and leader before the match.
- Each player draws 10 cards from their deck.
- Each player can mulligan up to 2 cards once before the first round.
- Players alternate turns; a turn can play one card, use a leader ability, or pass.
- A passed player cannot act again in that round.
- A round ends when both players have passed or both players have no playable cards.
- The higher score wins the round.
- Nilfgaard wins drawn rounds.
- A non-Nilfgaard draw makes both players lose the round.
- Northern Realms draws one card after winning a round.
- Monsters keeps one random non-hero unit on the board after each round.
- Scoia'tael can choose who starts the match.
- Rows are Close Combat, Ranged, and Siege for each player.
- Weather affects both players' rows: Biting Frost sets Close units to 1, Impenetrable Fog sets Ranged units to 1, Torrential Rain sets Siege units to 1. Clear Weather removes all weather.
- Commander's Horn doubles non-hero units in one row and is limited to one per row.
- Tight Bond multiplies matching non-hero units by the count of matching cards in the same row.
- Morale Boost adds +1 to other non-hero units in the row.
- Spy cards are played on the opponent's side and draw two cards for the owner.
- Medic cards restore one eligible non-hero, non-special unit from discard and play it immediately.
- Muster plays matching cards from the owner's hand and deck.
- Agile cards can be placed on Close or Ranged once.
- Decoy returns one eligible non-hero unit on the player's board to hand and plays Decoy to discard.
- Scorch destroys the strongest non-hero unit(s) on the battlefield after active modifiers.
- Hero cards are immune to weather, Horn, Morale, Tight Bond, Scorch, Decoy, and Medic restoration.

## Architecture

The implementation will be data-driven.

- `Assets/Scripts/Gwent/Core`: pure C# rules engine with no Unity UI dependency. This layer owns cards, decks, hands, board state, scoring, turn state, round resolution, and ability effects.
- `Assets/Scripts/Gwent/Data`: built-in catalog for base factions, leaders, and cards. The first pass can include a representative full playable catalog, with local art mappings for available assets.
- `Assets/Scripts/Gwent/UI`: Unity MonoBehaviours that render the engine state, handle player input, drive simple animations, and call the core API.
- `Assets/Resources/Gwent`: card art mappings and UI assets.
- `Assets/Tests/EditMode/Gwent`: EditMode tests for all core rules.

## UI Flow

1. Start screen shows faction choices.
2. Deck screen shows selected faction, leader, deck list, unit count, special count, and start button.
3. Mulligan screen shows the initial 10-card hand and two redraw opportunities.
4. Match screen shows opponent board, weather lane, player board, hand, scores, round gems, pass button, leader button, deck/discard counts, and hover zoom.
5. Result screen shows match winner and offers replay.

## Visual Direction

Use a dark parchment/wood battlefield with faction-colored row accents, three rows per side, gold-trimmed card frames, compact score medals, and red round gems. Use local assets for known art and backs:

- `default-northern_realms.png`
- `default-nilfgaard.png`
- `default-monster.png`
- `border-gold.png`
- available card portraits and videos in `Assets/Assets`

Missing art renders as a styled placeholder card with faction color, card name, row icon, strength, and ability badges. Basic animations include card slide from hand to row, flip/reveal, score pulse, pass banner, weather overlay, and round-result gem changes.

## Testing

Core rules must be covered with EditMode tests before UI integration. Required tests include draw/mulligan, turn/pass flow, row placement, weather, Horn, Tight Bond, Morale, Spy, Medic, Muster, Agile, Decoy, Scorch, hero immunity, faction bonuses, and match completion.

Unity verification requires:

- EditMode test run with zero failures.
- Windows batchmode build success.
- Manual or automated playthrough evidence for selecting a faction, mulligan, playing cards, passing rounds, and finishing a match.
