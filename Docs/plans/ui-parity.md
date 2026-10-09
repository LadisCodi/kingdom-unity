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
| Building card | done: the web's layout from kit pieces — portrait, description, Upgrade with its call to action (gem Finish and the bar while building), stat band, villager panel (bust, tag, ×1/×10/×100/All, priced or gated Train, batch with gem Finish), crew stepper. Still to do: the working hammer over the portrait, the camera framing the building above the card | done |
| Upgrade | done: its own sheet in the card's place — levels with plaques, improvements, requirements ✓/✗, wide price with the wait, Upgrade locked with why | done |
| Prices everywhere | ~~coin 50 rpx and figure 36 rpx Bold, where the web has 76 rpx and body size ExtraBold~~ — fixed: inline-icon `PriceLabel` over buttons and in buy boxes, `CostChip` in rows | done |
| Build menu | done: the header's contextual plaque (builders free; villagers free on a crew's card), the window under it, shut rows (research or cap) on locked paper with drained art, padlock and the reason in clay, Crop plots' art, "Hechos", tab badges, the scrim under sheets | done |
| Ruin card | done: docked like a card, ABANDONED tag on its art, priced Repair off when the purse is short | done |
| Knowledge sheet | done: head row (big book, title, when full), offers priced on kit buttons | done |
| Tech sheet | done: kit buttons with their icons in their words, the big Research | done |
| Placement | done: portrait tile, priced Build (bare when moving), the wait with its hourglass | done |
| Landmark card | done: docked, status tag, priced Claim | done |
| Camera | the web frames the building above its card | small |

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
