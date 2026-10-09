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
| Notices | done: the news column bottom right (wooden bubbles, wax seal for a group, +N past four, pop and chime, an unread news blinking out after ten seconds), hidden under any menu; the card — one news with its paragraph, wide picture and Go, a group a row each | done |
| Harmony and neighbours | done: the Decoration tab's line (what a decoration is for; supply of demand and what a surplus pays once anything demands), "Needs {n} more Harmony" on a shut row, the card's Harmony (a decoration's supply, the Townhall's supply and demand), a house's rent verdict and the neighbours' times, the Harmony gate on the upgrade sheet, the ghost's pills (what it gives its neighbours, what it receives). Not yet: the +supply chip on a row once Harmony is demanded | done |
| Work area | done: a producer's reach while it is placed, moved or its card is open — sky glow under a white line on the floor, the features its crew would work rimmed white — and, placing, what each of them holds (toned against the authored stock); a crop plot's yield on its ghost. Not yet: the line's rounded corners | done |
| Workshops and goods | done: the workshop's panel on its card (good and recipe, in store, crew, the queue's slots with progress and ✕, next out with Speed up or Finish, Make with its price and how many are queued), goods in every price line (build rows, placement, upgrade sheet, technology), a long price drawn smaller to fit, "Goods ready" notices, the picker for a workshop. Not yet: the building's icon on Make | done |
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
