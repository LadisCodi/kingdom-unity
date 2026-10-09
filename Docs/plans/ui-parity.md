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
| Building card | another layout: the web has art + promise + Upgrade (no price), a grid of stat tiles (store full in red, income, fog ring, training), the villager section (portrait, tag, line, lock reason, Train with ×1, the queue) | major |
| Upgrade | the web opens its own sheet: level 1 → 2 art, improvements with their gains, requirements ✓/✗, price and time, the button and why it is off; Unity upgrades from the card | major |
| Prices everywhere | ~~coin 50 rpx and figure 36 rpx Bold, where the web has 76 rpx and body size ExtraBold~~ — fixed: `Cost.prefab` over buttons, `CostChip` in rows | done |
| Build menu | no builders plaque (1/1) over the window; locked rows not greyed (art and lock), the reason not in clay; Crop plots' art missing; "Construidos" for "Hechos" | medium |
| Ruin card | no ABANDONED tag over the art; price not framed with plain figures; Repair enabled though short of Wood | medium |
| Knowledge sheet | no head row (big book, title, when full) | small |
| Tech sheet | price as a chip; the button smaller; "pour all" reads "+0" | small |
| Camera | the web frames the building above its card | small |

Not built yet, so not differences: the Survey widget, settings and friends
buttons, the builder sheet, the other books' bookmarks.

## 4. Still to capture

House and crew building cards, a building under construction, landmark card,
quest pill (running, done), header and nav states, placement on illegal
ground, toasts.

## 5. Order of the fixes

1. ~~Shared size tokens; prices at their size everywhere.~~ Done: text styles, `Cost.prefab`.
2. The building card and the upgrade sheet.
3. Build menu.
4. Ruin card, knowledge sheet, tech sheet.
5. What §4 turns up.
