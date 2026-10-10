# Plan — UI parity with the web prototype

> **Scope:** every screen already built in Unity, held to the web prototype's
> look, layout, sizes and flow; and the rule every new screen is built by.
> **Status:** audit under way; fixes in the order below.

## 1. The method

- Capture the web screen and the Unity screen in the same state, at
  1125 × 2436, side by side (web: CDP iPhone X; Unity: the Game view).
  Tutorials off on both (`Kingdom › Dev › Tutorials off`; the web's veteran
  flag), offers closed.
- A screen is done when the pair matches: the same blocks in the same
  order, the same sizes (within a few rpx), the same states (enabled, short,
  locked, empty), the same flow (what a tap opens).
- Sizes come from the shared tokens (§2), never typed per screen.

## 2. Sizes (web tokens in reference pixels)

`--px` = 1125 / 402 ≈ 2.8 rpx.

| Role | Web | rpx |
|---|---|---|
| Window title (Alegreya Black) | `--text-title` 28 px | 78 |
| Body: names, values, copy | `--text-body` 17 px | 48 |
| Button label | `--text-button` 16 px | 45 |
| Description, section heading, helper | 13 px | 36 |
| A price's coin (header coin) | `--cost-icon` | 76 |
| A price line's coin, over a button | `.k-price-term` 28 px | 78 |
| Gutter (sheet side padding) | 12 px | 34 |
| Between sections / under a heading | 14 px / 6 px | 39 / 17 |
| Radius / small radius | 10 / 8 px | 28 / 22 |

Weights: title 800 (Nunito ExtraBold), strong 700 (Bold), body 600
(SemiBold), small 400 (Regular). A price's figure is 800, tight, clay when
short.

The text roles are TextMeshPro styles (`Assets/Settings/UI/Text Styles.asset`);
a label picks one and never sets a size of its own (`TextStyleTests`).

## 3. Differences found

| Screen | Difference | Weight |
|---|---|---|
| Building card | done: the web's layout from kit pieces — portrait, description, Upgrade with its call to action (gem Finish and the bar while building), stat band, villager panel (bust, tag, ×1/×10/×100/All, priced or gated Train, batch with gem Finish), crew stepper.; the hammer works over the portrait while it is built, and the camera moves the building into the map above the card (ruins and landmarks too) | done |
| Upgrade | done: its own sheet in the card's place — levels with plaques, improvements, requirements ✓/✗, wide price with the wait, Upgrade locked with why | done |
| Prices everywhere | ~~coin 50 rpx and figure 36 rpx Bold, where the web has 76 rpx and body size ExtraBold~~ — fixed: inline-icon `PriceLabel` over buttons and in buy boxes, `CostChip` in rows | done |
| Build menu | done: the header's contextual plaque (builders free; villagers free on a crew's card), the window under it, shut rows (research or cap) on locked paper with drained art, padlock and the reason in clay, Crop plots' art, "Hechos", tab badges, the scrim under sheets | done |
| Ruin card | done: docked like a card, ABANDONED tag on its art, priced Repair off when the purse is short | done |
| Knowledge sheet | done: head row (big book, title, when full), offers priced on kit buttons | done |
| Tech sheet | done: kit buttons with their icons in their words, the big Research | done |
| Placement | done: portrait tile, priced Build (bare when moving), the wait with its hourglass | done |
| Landmark card | done: docked, status tag, priced Claim | done |
| Bag | done: five tabs in rows of four (an empty one dulled), square tiles tinted by rarity with their size, count, badge and sparkle, the popover under the tile's row (choice plates, − slider + with Max, total, Use), boost ribbons | done |
| Relics | done: the Bag's Relics tab (the Shrines standing and what the Build menu adds, City and World under thin heads, two cards a row as tall as the taller — the art a silhouette in fragments, dimmed under rising Zs asleep, lit awake; the blue level seal; the six slots with the missing ones in chalk inside a dashed outline; Ready to restore or how many of six; the gold plank with the time awake; the priced emerald Activate asleep; In the Bag); a relic's sheet (the relic with AWAKE on its plank, its sentence with its numbers, its tiles two a row now → next level, its level with the six slots and their counts, Restore or Level up — wood while Activate is on the page, greyed without a set — where fragments come from, its Shrine and the Shrines to host it in, Remove, the wax ASLEEP beside Activate or the window running down, how soon the Mana refills and a flask, the Store). the Shrine's card (the chapel's painting as the press — empty and calling with turning rays and the badge when a relic could go there, the relic on the altar with its name, level and effect on the dark band, Activate and a flask asleep, the window running down awake); the relic picker (the restored city relics three a row with their effect, the Shrine mark and the check, the Shrine's slot in its green panel, Select) and the move confirmation; on the map the relic in its Shrine (afloat with its halo, bob and window arc awake, small and dim on the altar asleep under a parchment bubble with its Zs that opens the card), the awake aura's enchanted ground (violet floor breathing, haze, sigils, motes, bright edge, the wave breathing out), the +10% over every roof it pays, and a wake's ring and floaters. Not yet: fragment prizes in the reveal and the lair claim | partly |
| Notices | done: the news column bottom right (wooden bubbles, wax seal for a group, +N past four, pop and chime, an unread news blinking out after ten seconds), hidden under any menu; the card — one news with its paragraph, wide picture and Go, a group a row each | done |
| Harmony and neighbours | done: the Decoration tab's line (what a decoration is for; supply of demand and what a surplus pays once anything demands), "Needs {n} more Harmony" on a shut row, the card's Harmony (a decoration's supply, the Townhall's supply and demand), a house's rent verdict and the neighbours' times, the Harmony gate on the upgrade sheet, the ghost's pills (what it gives its neighbours, what it receives). Not yet: the +supply chip on a row once Harmony is demanded | done |
| Work area | done: a producer's reach while it is placed, moved or its card is open — sky glow under a white line on the floor, the features its crew would work rimmed white — and, placing, what each of them holds (toned against the authored stock); a crop plot's yield on its ghost. Not yet: the line's rounded corners | done |
| Workshops and goods | done: the workshop's panel on its card (good and recipe, in store, crew, the queue's slots with progress and ✕, next out with Speed up or Finish, Make with its price and how many are queued), goods in every price line (build rows, placement, upgrade sheet, technology), a long price drawn smaller to fit, "Goods ready" notices, the picker for a workshop. Not yet: the building's icon on Make | done |
| Military halls and the Infirmary | done: a hall's training block (the rank shown with its coin, the type chip, the four numbers, Train priced or gated with the web's words, the batch with Speed up or Finish), its rank menu (each rank's numbers and why it is shut), the army plaque in the header, the hall's cap and training time on its band, the ward (beds taken, a row a wounded troop with Heal priced), "Training complete" notices, a hall's line on the map. Not yet: the trainee's face on the map bar, the type chip's tooltip | done |
| Lairs | done: the lair's card (painting and flavour, the raid countdown, the path of fights with its stones, the reward with the hoard first, Attack or Claim), the held zone's tint and border on the map, the model and its warning bubble, the lair's silhouette past the fog, the standing raid bubble with its plaque, the raid and lair-sighted news, "{creature} hold this ground" refusals, the raid alarm. Not yet: the claimed lair's dust as it goes, the reward flying home from the claim | done |
| Attack sheet | done: the enemy's board with its creatures and power, our board (six slots, a tap sends a squad home), the roster (a tap sends a squad, drained when none are left), Quick deploy, the Mana price over Attack, the expected losses from the same fight, the block reasons; the muster music; the hero slots under a dashed line (a hero's card, an open slot, the padlock and the next slot's Gems), every hero who can fight leading by default; the hero picker (filters, sort, the cards with their power, the check on the chosen, the party in a green panel, Select, an exhausted hero refused) | done |
| Battle playback | done: the field with every slot walking where the log has it, lunges and flinches, arrows, slashes, thrusts, sparks, helmets, chips, dust and shocks, the life rings with their pale ghost, counts and floating numbers merged per slot, deaths cracked and stamped, the power bar rolling and shaking, the place's plaque, ×1/×2/×4 and Skip, the march on, holds on heavy blows, the slow last blow and its flash, Victory and Defeat, the way out, every sound and the battle music. the spoils on the chest reveal; the skills — the ribbon naming each on its cloth, the caster's glow and motes, each skill's own cast (a Volley's arrows from the sky, Cleave and Crush lunges, the Ambush across the board, Sharpshot's beam, bolts of light for heals, shields and dazes, the Wave's shock) and impact (Crush shaking the board), the rallies named after the march with the whole side flaring, shield bubbles that wobble and shatter, daze stars and the dimmed portrait, every skill's sound; heroes under their own portraits | done |
| Heroes | done: the roster (the Heroes tab: All and a tab per type latched pushed in, the sort by level or rarity, how many found, the cards three to a row — rarity face, art, gilt frame, type banner, stars, level pill, HP when hurt, rank numeral, the orb when a level, a star or a rank is ready — then the ones not found yet as silhouettes with their fragments); a hero's card in the roster's place (the rarity's ribbon, the name, the type's badge, the hero on its rarity's vault with stars, stats and Ascend over the fade, the skill with its pips and next rank, the boon, Level Up or the ceiling, Recruit for one not found); Hero XP and Stardust in the header's place. Call for aid on the roster and on a hero not found. Not yet: the fragment price shows the shared fragment mark rather than the hero's face, the stat and skill rise animations | partly |
| Chest reveal | done: its own stage over everything (the hall, the carpet, embers), the chest by kind — silver, gold, relic, war — dropping, wiggling open with its bloom, the count left on its tag; a card rising face down from the mouth, the back's ink as ornate as it is rare, a tap to flip, the next to send it home dimmed; a ten-call's goods together face up, the bag card's bars filling at once, fragments bars and the NEW seal on a recruit; a new hero's moment (glowing trembling back, veil, rays, kicker, name/title/rarity, flash, shake, confetti cannons and rain, fireworks, fanfare); Skip; the summary lighting up between Rewards and Collect; every sound and the feast music; a fight's spoils on the war chest | done |
| Store: Heroes tab | done: the merchant's shop behind, the tab strip and the close, the carousel of every hero drifting and fading, Call for aid, the odds on a tap, the common call in nailed wood with its blue plank and the golden call in gold plate with a Legendary rising out of it (a new one each visit), the free calls line ticking, the ×1 as Free / an ad / a key and the ×10's keys, the locked page until a Tavern stands; the keys in the header's place; the rewarded video's stand-in (Advertisement, countdown, Claim). Not yet: the Offers, Supplies and Gems tabs, the + on the keys | partly |
| Speed-up picker | done: the timer (icon, bar, time left), Auto's plan, one row per fitting speed-up (scrolling past five), gem Finish; Speed up with its hourglass on the card's timers | done |

Not built yet, so not differences: the Survey widget, settings and friends
buttons, the builder sheet, the other books' bookmarks.

## 4. Still to capture

House and crew building cards, a building under construction, landmark card,
quest pill (running, done), header and nav states, placement on illegal
ground, toasts.

## 5. Order of the fixes

1. ~~Shared size tokens; prices at their size everywhere.~~ Done: text styles, inline icons (`PriceLine`).
2. ~~The building card and the upgrade sheet.~~ Done, with the kit of prefabs.
3. ~~Every existing menu's buttons, prices and sections onto the kit.~~ Done for placement, ruin, landmark, tech sheet and knowledge; the build menu's rows and tabs and the quest pill keep their own pieces.
4. ~~Build menu.~~ Done.
5. Ruin card, knowledge sheet, tech sheet.
6. What §4 turns up.
