# Kingdom — the design documentation

**Kingdom** is a **4X for people who bounce off 4X** — a city-builder on a
fog-shrouded province that opens onto a hex world shared with other players,
built for mobile. **This folder is the design.** It describes the game as
currently designed — not its history, and not how it is coded.

## Start here

| File | What it is |
|---|---|
| **[`overview.md`](overview.md)** | **The game in five minutes** — the pitch, the promises, the loops, the scopes. Read this first. |
| [`open-questions.md`](open-questions.md) | **Every decision still to make**, and every known soft spot, with a stable id (`OQ-n`) that the feature docs point at. |
| [`open-questions-closed.md`](open-questions-closed.md) | The decisions already taken, and why. The ledger, never the authority. |

## The design intentions

Every feature below is shaped by these.

**The three promises**

1. **Your city can never be attacked. Everything outside it can be.** The
   province is inviolable. The only thing that ever takes from it is a lair
   you have found and left standing — three raids a day from the buildings'
   stores, never the wallet, and what it carries handed back when you clear
   it. **What a player
   can take from a player is territory**: a claimed hex on the world map, never
   a building and never a purse. Every other pressure is *opportunity that
   expires* — a pool that overflows, a window that closes, a haul you chose to
   risk.
2. **The best-managed economy wins.** Combat is a sink for the economy, not a
   test of reflexes. A fight is composed, never played.
3. **Wallets buy power, comfort and breadth — but never exclusivity.**
   Nothing is purchase-only that cannot also be earned, and every paid ladder
   is earned first.

**The five working rules**

1. **It is played in visits, not sittings** — ~30 minutes a day across two or
   three check-ins. **If a feature needs more, the feature is wrong.**
2. **Price every reward in a duration of the player's own production**, never in
   absolute amounts. A tap pays seconds of WORK on what you tapped; a
   Survey chest pays hours of production. A ladder is relative too: a Wonder's cost
   is a curve, not a table.
3. **There is no offline cap.** An absence is replayed in full; what the city
   makes is bounded by what it can hold — each building's store, the Mana
   pool, the Knowledge bar, the queues.
4. **Adding a wallet row needs an argument.** Eleven rows, five things on the
   plank. A counter beside the thing it belongs to usually beats a coin — the
   argument that wins is that the thing is a *price* on a button, which is
   what the two gacha keys are.
5. **One job per currency.**

**The paid fog is the differentiator.** It pays back in resources,
treasures, abandoned buildings and landmarks that make exploration compound
([`01`](features/01-map-and-fog.md)) — and it hides the lairs the army has to
clear ([`18`](features/18-garrisons-and-raids.md)).

## The features

One file per feature, in the order a player meets them.

| # | Feature | Covers |
|---|---|---|
| 1 | [The map and the fog](features/01-map-and-fog.md) | the grid, terrain, features, the three fog states, the reveal curve, what the fog holds — **treasures and abandoned buildings** |
| 2 | [Map scopes](features/02-map-scopes.md) | **structural** — the three scopes, who is authoritative over each, what the save records, and what the promises allow to be contested |
| 3 | [The economy](features/03-economy.md) | every currency and its one job, housing taxes, adjacency, villager training, what a tap is worth |
| 4 | [Harvest](features/04-harvest.md) | **the cell as a depot, the tap as a duration**, the strike, migration, the map's production ceiling |
| 5 | [The city](features/05-city-and-districts.md) | every district, the Townhall as era gate, **every level's cost authored and multiplied by the building's instance ordinal**, placement, moving a building; the building list is [`buildings.md`](features/buildings.md) |
| 6 | [Construction](features/06-construction.md) | no waiting line, builders, and the offer a refused build raises |
| 7 | [Research](features/07-research.md) | **spellbooks — three general ones (Civics, Warfare, Magic) plus books the player FINDS (the Sagas, the Atlas)**, each opened by a fact about the world — one flow-chart page each, eras opened by exploring, climbing bonuses only, and Knowledge as the research clock; the node list is [`tech-tree.md`](features/tech-tree.md) |
| 8 | [Magic](features/08-magic.md) | Mana and its cap, the Sanctum, landmarks, and the rewarded ad as one loop |
| 9 | [Relics and the collection](features/09-relics.md) | eight relics as **permanent passives with no ceiling**, levelled by **card albums in a 28-day shared season** — packs, duplicates, the vault, trading, wildcards, the season hero |
| 10 | [Heroes and the gacha](features/10-heroes.md) | thirty-two heroes as **a body and a type buff** on the battle board, XP-bought levels, Fragment-plus-Stardust ascension, Gem-bought hero slots, the two-banner gacha with pity and no dead pulls |
| 11 | [Ruins](features/11-expeditions.md) | **depths of numbered rooms**, a boss at the end of every depth, per-room rewards — the world board's dungeons; the resolver is [`combat.md`](features/combat.md) |
| 11a | [Ruins — the screens](features/11a-ruins-ui.md) | the battle screen, the playback, and the room screens |
| — | [Combat](features/combat.md) | **the resolver every fight goes through** — a deterministic tick auto-battler on a six-slot board, squads by unit type and tier, heroes and villains in slots of their own, and the event stream the renderer replays; the army cap and the four military halls. **The resolver 11, 18 and the world map all call** |
| — | [Buildings](features/buildings.md) | **the building list** — every district, its levels, the late ladder, the decorations |
| — | [The tech tree](features/tech-tree.md) | **the node list** — every technology by book and era, generated from the tree file |
| 12 | [Quests and onboarding](features/12-quests.md) | the quest chain and the authored onboarding it carries |
| 13 | [Events](features/13-events.md) | **the archetype we author ten times a year** — points, the fog island, the track, the shop, the deadline |
| 14 | [Monetisation](features/14-monetization.md) | what a wallet may buy, six ad placements, and a **simulated** store that never charges — payer profiles with a monthly budget, Gem packs, builders, the hero banner |
| 15 | [The social layer](features/15-social.md) | identity, the friends list, neighbours and capped daily help, a guild, a weekly collective bar, and the siege that clears the world map's landmarks |
| 16 | [Wonders](features/16-wonders.md) | **the ladder with no top** — buildings whose upgrade curve never ends |
| 17 | [Workshops and refined goods](features/17-workshops-and-goods.md) | the four goods, the four buildings that make them, and the queue a villager works — the first producer that is a crew from the start |
| 18 | [Lairs](features/18-garrisons-and-raids.md) | **a garrison with a clock** — the province's lairs: the minute-scale counter discovery starts, the bounded and recoverable raids it makes while it stands, and the fight that clears it: the doorway to combat |
| 19 | [The world map](features/19-world-map.md) | **the shared board** — 91 hexes and six players in rings around the Dark Portal, explorers that march to reveal, connection chains and inactive hexes, conquest against denial, the Fortress, dungeons, and the weekly Portal dive |
| 21 | [Harmony and the decorations](features/21-harmony.md) | the city stat six decorations supply and the levels from 8 demand — a gate, never a drain, priced in variety and the workshop queue |
| 22 | [Progression](features/22-progression.md) | **how the game opens up** — the doors and what opens each, the five books and the milestones that open them, the Orcs, the Thorned Shrine and the Watchtower as places that open mechanics, heroes by story then by the Tavern, the first pack, and the pace of the tree |
| 23 | [Tutorials](features/23-tutorials.md) | the **First Morning** — ten scripted minutes, beat by beat — then one introduction per system, help when stuck, and the input lock |
| 24 | [Dialogue](features/24-dialogue.md) | the **visual-novel stage** every tutorial speaks through: a character each side, a box that can sit anywhere, the pointer, the conditions, and the cast led by **Isolde, the Royal Advisor** |
| 25 | [The Survey](features/25-the-survey.md) | **a ladder that pays for exploring** — 36 levels over the whole province, climbed by cells revealed, a free column and a paid one bought once; never resets |
| 26 | [Notices](features/26-notices.md) | **the bubbles in the corner** — news and standing states for what the player may not have seen, in the province and on the board, each a card with a way to go there |
| 27 | [Plantables](features/27-plantables.md) | **editing the ground** — crop plots planted from the Build menu as features, and trees and plots moved with a long press, growing again where they land |
| 28 | [Languages](features/28-languages.md) | **English and Spanish** — the choice in Settings, every text the player reads translated — the interface's and the game data's — numbers in the language's locale |

## Art

The art direction for the WORLD and the UI system for the chrome.

| File | What it covers |
|---|---|
| [`art/art-direction.md`](art/art-direction.md) | **how every asset of the WORLD is made** — the 2:1 isometric projection, canvas sizes and anchors, terrain, buildings, units, the hex board, the map's states, and the pipeline |
| [`art/ui-menus-redesign.md`](art/ui-menus-redesign.md) | the UI system for the chrome, its palette and its shapes |

## House rules for these docs

- **These are DESIGN documents.** They specify HOW the game works. No
  implementation detail unless a decision turns on it; code-level contracts
  live in `CLAUDE.md`.
- **Specification, not design process.** Write what the feature does, not why
  it does it that way, and not the alternatives that were considered.
- **The current design only.** No history: not how a feature has changed, not
  when, not why.
- **As simple as possible.** Prefer bullet lists and tables to prose. Less is
  more.
- **A feature doc opens with a scope-and-status blockquote**, uses numbered `##`
  sections referenced elsewhere as `§n`, carries a **dials table in the order to
  reach for them**, and ends with a **deliberately not in this design** list —
  one line per exclusion.
- **Open questions live in one file**, not scattered. A feature doc names them by
  id.
- **When a doc and the code disagree, the code is usually right and the doc is
  stale.** Fix the doc in the same commit, and prefer a test over a paragraph
  for any number that has now been argued twice.
- **The game data is the source of truth for every number**, the map and the
  tech tree included. A doc quoting a number is a convenience, never the
  authority.
- **Docs are written in English.** Keep it that way.
