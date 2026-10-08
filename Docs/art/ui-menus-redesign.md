# Menus — the UI guide

> **Scope.** The chrome: the principles every menu follows, the shared UI kit
> (palette, materials, buttons, type, icons, layout, motion), the screens that
> belong to no single feature, and the cross-cutting rules. A feature's own
> screens are specified in its feature doc and follow this guide.
>
> **Not in scope: the world.** Terrain, buildings, units, the hex board and
> the fog are [`art-direction.md`](art-direction.md).
>
> **Status: designed.**

## 1. Units

Sizes are given in two units, both of the **reference frame** (§3.6):

- **rpx** — one reference pixel: 1/2436 of the frame's height, on a
  1125 × 2436 portrait frame.
- **pt** — one point of the reference phone shown 402 pt wide
  (1 pt = 1125/402 ≈ 2.8 rpx).

---

## 2. Design principles

1. **The kingdom stays on screen.** Menus are windows over a live,
   dimmed-but-visible map — never opaque full-screen pages. The exceptions
   are the few `tall` screens of §3.6, the Research book first among them.
2. **One primary action per screen.** Every panel has exactly one big
   green button. Everything else is smaller, quieter, or a tap target on
   the map.
3. **Show the outcome, not the stat.** A reach is drawn on the map, not
   written as a radius; a rate rides with a picture of what makes it. Numbers
   stay, but they ride along with a picture.
4. **Nothing is greyed out without a reason attached.** A disabled button
   always sits next to one short sentence saying what unlocks it, in
   plain words: "Needs a bigger Townhall", not "Townhall lvl 3 required".
5. **Everything is a material you could touch.** The UI is made of warm,
   natural things — carved and painted wood, yellowed parchment and paper,
   rope, cloth, wax, brass, glass with something inside it — each drawn with
   its texture and lit from above, never a flat fill.
   - A symbol ON a piece is worked INTO the material: carved, engraved,
     embossed or stamped, never a flat glyph pasted on top (the close
     button's X is a groove in its red wood).
   - A pressed control is the same material pushed in, not a darker copy.
   - Colour comes from the material (red lacquer, green paint, gold cloth),
     not from a coloured shape.
   - No hairlines, no glass-morphism, no neon, no cold grey, no plastic
     gloss. When in doubt, ask "what is this made of?" — and draw that.
6. **Big, few, forgiving targets.** Minimum 44 × 44 pt touch targets, body
   text ≥ 16 pt, the smallest helper line ≥ 13 pt. Portrait-first (§3.6),
   one-thumb reachable: primary actions in the bottom third.
7. **The world is the menu where possible.** Tapping a building opens its
   card; interactions go onto the map rather than into lists.

### Anti-goals (the "not a 4X" checklist)

| Avoid | Instead |
|---|---|
| A permanent bar of every resource counter | The few coins in play, Mana and Gems, plus a tap-to-expand purse (§5.1) |
| Tables of stats with `→` deltas | Before/after pictures, one delta line max |
| A dotted node graph on a dark grid | An illustrated page with landmarks |
| Grey disabled rows | Rows that stay warm, with a padlock and a reason |
| "Power 6/20", "Slots: 1 busy / 2" | "Your warband: 6 of 20 strong" with pips |
| Full-screen modal takeover | A window, the map visible around it |
| Tiny muted helper text | 13 pt and up, warm brown, always a full sentence |

---

## 3. The shared UI kit

Everything in §5 assumes this kit.

### 3.1 Palette

The chrome and the world are the same game, so the palette is the world's.

| Name | Hex | Use |
|---|---|---|
| parchment | `#F4E4C1` | Panel fill, card bodies |
| parchment-shade | `#E2CCA0` | Inset rows, alternating list bands |
| wood | `#A9713F` | Frame faces, headers, nav bar |
| wood-dark | `#5C3A1E` | Outlines, frame shadow side |
| wood-light | `#C89159` | Frame top bevel, highlights |
| ink | `#3B2412` | Primary text on parchment |
| ink-muted | `#7A5C3E` | Helper text, secondary rows |
| leaf | `#6FBF4A` | Confirm buttons, "affordable", progress fill |
| leaf-dark | `#3F8A2E` | Confirm button outline / shadow lip |
| gold | `#F2B233` | Highlights, CTA glow, rewards, quest accents |
| gold-dark | `#C98A16` | Gold outlines |
| clay | `#D4553E` | Can't afford, destructive, danger |
| sky | `#4FA3C7` | In-progress timers, research, information |
| locked | `#CBBA96` | Locked/unavailable fills (warm, never grey) |

Rule: **no pure black, no blue-grey, no pure white.** Outlines are
wood-dark, not black. Disabled is `locked`, not reduced opacity.

### 3.2 Surfaces

The materials are painted pictures — textures and nine-sliced pieces — never
flat colour with bevels.

- **The window** (§3.7) — every menu that leaves the map in sight: a ring of
  warm wood, cream paper inside, a header band.
- **Card / list row** — parchment-shade, a wood-dark outline, rounded
  corners. Its left slot always holds a **48 × 48 sprite or icon**, never a
  bare glyph.
- **Section** — a flat panel a shade darker than the paper, in a flat thin
  outline, rounded, no bevel, no texture. It marks off a group of things
  inside a menu: a priced button, a stat tile, the district card's portrait.
- **Scrim** — when a window is open, the map dims to 35 % warm brown, never
  to near-black. The kingdom stays legible.
- **Wood** carries planks, the header and the nav beam; **parchment** the
  build cards, the research page and the cards on the map (the daily pill,
  the builder plaque, the quest scroll); **cloth** the banner, dyed by its
  tone; **rope** the grab handle and dividers; **wax** the research seals.
- The header's plank, the nav's beam and tabs, and the cards on the map are
  painted plates: the bevel, the rim, the grain and the corner nails are in
  the art. Nails sit at the corners of the nav beam and the plaque; a titled
  plank has bare corners.
- **The header beam** (§5.1): one recessed slot per resource — the coins at
  the left; Mana and Gems at the right, past a hanging rope — the green `+`
  knob inside the Gems slot, the Mana slot filled as its own gauge with the
  pool over it, and the round Settings knob hanging on a rope under the
  beam's right end.
- **The nav's tabs** are wood plates on the wood beam — the same grain
  darkened, a lit top edge, the word in cream with a shadow — and the lit tab
  is a gold plate that stands proud of the beam's top edge.

### 3.3 Buttons

Three materials, one silhouette — a rounded slab with a lip under its face —
nine-sliced so a label of any length fits.

- **Wood** for plain actions: the ones a player does, not the ones they want.
- **Paint** for everyday coloured actions — upgrade, train, build: one matte
  colour, a soft lighter band along the top, a darker lip, little texture.
- **Gemstone** for the premium and magical actions — buy a pack, cast a
  spell, open a pack, gacha calls, claiming a reward: polished stone in a
  thin gold bezel, a clean gradient, one soft highlight band.

Every colour comes in both finishes, paint and gem. A colour is painted by
default; the Gems kind is a gemstone by default.

| Kind | Colour | Used for |
|---|---|---|
| secondary (default) | wood | Select, filters, amount picker, buy with coins |
| primary | green · emerald | Build, Train, Upgrade, Start — the one green action |
| blue | blue · sapphire | watch a video, a free call |
| gold | gold · topaz | an advanced call |
| destructive | red · ruby | Reset, Cancel |
| gem | purple · amethyst | anything that spends Gems: Finish now, buy a slot |

- **Four states**: **normal** and **pressed** are drawn (the pressed one is
  the same slab pushed in: no lip, face lower); **highlight** (brighter and a
  touch warmer) and **disabled** (drained toward a muted blue-grey) are
  derived from the normal art, so all four share one outline.
- **Size**: one height (46 pt), a floor on width (112 pt) and a ceiling
  (144 pt; 160 pt for a priced button, frame included), so the slab is always
  drawn at its own proportions. A screen that stretches a button gets it at
  the ceiling, centred; a label longer than the ceiling widens its button
  just enough to fit, never cut. Anything taller than a label goes outside
  the slab.
- **Label**: light ink with a 1 pt outline and a 2 pt drop below, in a
  darker tone of the slab's own colour (dark green on green, dark brown on
  wood, slate on disabled).
- A disabled button keeps its **reason line beside it**.
- **Round** (a knob): wood and every colour in both finishes, with the same
  four states, as a disc, for a one-glyph action — worker `−` / `+`, zoom.
  Drawn at the header's Gems `+` size (50 rpx) inside a hit area of at least
  44 pt. The glyph is carved into the face in a darker tone of the material,
  never a flat white sign. The window's close is its own red button (§3.7).
- **A price's icon is the plank's coin size** (76 rpx, the header's icon
  cell) everywhere a price is written — above a slab, in a cost chip, on a
  button's face — and stands proud of its line rather than making it taller.
- **Priced**: the cost sits **above** the slab, outside it — icon + amount
  per term, any term the player cannot pay in clay — and the two are grouped
  on a small section; the whole is one press (§6.4).

### 3.4 Type & numbers

- **One family: Nunito.** A title and a caption out of one family read as one
  voice; what separates them is the **weight**, not the family.
- **Four weights are four roles**, and a rule names the role rather than the
  number:

| Role | Weight | Used for |
|---|---|---|
| title | **800** ExtraBold | headings — *Store*, *Heroes*, *Timber!* |
| strong | **700** Bold | buttons, amounts, names |
| body | **600** SemiBold | ordinary prose — the default |
| small | **400** Regular | the small description under it |

- **600 for prose.** On parchment at body size, Regular reads thin and
  SemiBold reads right; 400 is left to the helper line, where the *contrast*
  against the 600 above it does the work.
- **Text roles** — a menu names the role, never a size, so every window
  reads the same:

| Role | Size | Used for |
|---|---|---|
| title | 28 pt | a window's title, on its header band |
| body | 17 pt | what the window is about: names, values, copy |
| desc | 13 pt | the line that describes it: a description, a rate, a note |
| heading | 13 pt | a section heading (small uppercase) |
| helper | 13 pt | a caption or fine print: a tag, a badge, a timer |
| button | 16 pt | a button's label |

- Tiny tags (a count on a slot, a timer on a portrait) may go to 11–12 pt;
  a sentence never does, and nothing goes under 11.
- A description clamped to two lines ellipsises.
- Every number is set in the text face; Nunito's digits are one width at every
  weight, so counters do not jitter.
- Counters are always paired with an icon on the left.
- Big numbers get thousands separators, written in the viewer's locale; never
  more than one decimal.
- Durations read as words at small values: `instant`, `8s`, `2m 30s`,
  `1h 05m`. Never raw seconds above 90.
- **A power is always the crossed swords and the figure** — never the word
  "Power".

### 3.5 Icons

- Every icon is drawn **smooth**, on a common cell, and shown at whatever size
  the layout asks — 76 rpx on the header coins and in prices (§3.3), 28 pt in
  the nav and in a list row, 16 pt inline — resampled smooth.
- The **locked variant** (desaturated toward `locked`) is derived, never
  drawn, so a row never shifts when it locks.
- Icons are chunky, simple, readable silhouettes with a thin dark-brown
  outline, soft two-tone shading and a small highlight.
- **No emoji anywhere**, and nothing falls back to one.

### 3.6 Layout

- **Full-bleed at any aspect ratio**: a phone, a 3:4 tablet, a desktop window.
  No pillarbox — a wider screen shows more map.
- **Reference resolution: 1125 × 2436** (the iPhone X, portrait). The UI
  scales **matching height**: one reference pixel is 1/2436 of the frame's
  height, so every piece keeps its share of the screen's height. The width is
  a ceiling: on a screen narrower than the reference (9:20, 9:21) the scale
  follows the width instead, so the 1125-wide header always fits.
- **Everything scales together**: text, buttons, tiles and gaps shrink and
  grow with the frame as one picture, and every device shows the same
  composition. Kept in real pixels: hairlines and the 44 pt minimum tap
  area. A piece may be drawn smaller than 44 pt; its hit area is not.
- **Safe zones**: the header is a 107-rpx beam (62-rpx slots, 30-rpx figures,
  76-rpx icon cells) plus the top safe-area inset; the nav is a beam of 57 pt
  painted plates (28 pt icons, 14 pt labels) with 4 pt above and below, plus
  the bottom safe-area inset. Each inset is reserved once, by the bar that
  owns that edge.
- **Windows fit their content, capped at 70 %** of the frame. Only the
  research page, the heroes roster, the reliquary, the battle board, the
  Build menu and the Bag are `tall`.
- **A card about something on the map frames it**: when the district or site
  card opens, the camera centres the building in the band between the header
  and the card's top edge, once.
- A window sits 24 pt below the header and 8 pt above the nav, with a 12 pt
  gutter at the sides.
- Sizes on the reference phone: list rows 60 pt, store cards ~110 pt with a
  100 × 88 vignette, the daily pill 127 × 47, the builder plaque 30 pt tall,
  the Townhall's portrait 104 pt and its villagers 40 pt, hero tiles 3 across
  at 6 pt gaps with 13 pt ribbons and 20 pt feet.

### 3.7 Motion and the shared pieces

Windows slide up; counters roll rather than snap.

**The window**: every menu that leaves the map in sight — each sheet, the
district card — is one simple panel of warm wood with rounded corners, a drop
shadow all round to lift it off the map, and warm cream paper with a very
soft texture inside, with a margin (52 rpx from the frame's outer edge)
before the contents start. Three pieces, each cut to slice: the wood ring
(nine-sliced, centre empty), the paper (under the ring), and the header band.
It is 1053 rpx wide (the reference width less the gutters), centred — edge to
edge on the reference phone, with the map showing either side on anything
wider. A centred window keeps its own narrower cap. The district card is the
full reference width (1125 rpx): on the iPhone X the wood of its sides
touches the screen's edges.

- **In**: the contents hidden; the frame fades in and grows from its least
  height (its top and bottom slices, nothing between) to its full height in
  160 ms — from the foot for a bottom window, from the middle for a centred
  one; the contents fade in from 80 % of the way (about 0.2 s in all).
- **Out** (closing back to the map): the contents fade out (60 ms), then the
  frame shrinks back to its least height and fades (140 ms); the window stays
  on screen, untappable, until it has. Switching straight to another menu
  is immediate.
- **The header band**: every titled window has one — a wooden band seated on
  the top of the frame, standing 26 rpx proud of it and 22 rpx in from each
  side, so the frame's wood shows round it; fixed height (120 rpx),
  three-sliced in width (its rounded ends kept, the plain middle stretched).
  The title is centred on it, in a soft vertical gradient (pale cream to warm
  cream, lit from above) with a dark-wood outline; the buttons are anchored
  to its right, the close always last and any other actions before it. A
  window without a title has the frame and no band, and carries its own way
  out.
- **The close**: a round button of red lacquered wood with the X carved
  into it, the last button on the header. Its pressed twin is the same wood
  pushed in.
- **Spacing, the same in every menu**: the blocks of a window's content —
  the top row, the stats, each section — stand 14 pt apart; a section's
  heading sits 6 pt above what it heads, and takes the place of the gap
  before its section rather than adding its own. The header band is a
  heading too: the content starts one heading gap under it.
- **Section headings**: a short rule, the label in small uppercase wood, then
  a rule to the edge.
- **Progress bar**, one for the whole UI: a glass tube with coloured liquid
  in it, three painted layers drawn bottom to top — the **base** (the tube's
  dark inside), the **fill** (the colour, the tube's whole length, uncovered
  from the left so its level is a straight edge), and the **border** (the
  glass: outline and shine) — each sliced at its rounded ends (fixed height,
  stretched middle). Its reading — a count, a time left — always sits INSIDE
  it, centred, never under or beside it, in light ink over a warm dark shadow
  (a drop and a soft halo), neutral on any fill. A timer's bar moves
  smoothly, frame by frame, to full over the time left. Four tones, one per
  meaning: **gold** a goal (quests, collections), **green** something being
  made (training, construction), **blue** a resource filling or a timer
  (Mana, research), **red** a danger or a countdown to one.
- **Tooltip** (any element): a tap opens a parchment bubble under its anchor,
  arrow up, with a soft pop; a second tap or a tap anywhere else closes it;
  one open at a time in the whole UI. It fades and scales in from the arrow
  (0.14–0.18 s) and out again (0.12 s).

**One call to action**: every "there is something for you here" in the game
wears the same badge — a red enamel stud on its host's corner that gives a
small nudge every few seconds, and the count on it past one ("2" … "9", then
"9+").

- Where: the nav tabs, a startable technology, the daily and season pills, a
  hero tile with something to do, a claimable daily or season-pass cell and
  the pass's Claim, the vault, an affordable chest, a relic page ready to
  close, the quest scroll when done, the Store's, Bag's and Friends' tabs,
  the offers widget and its splash's tabs, and a notice bubble's count.
- No two badges nudge in step: each takes its phase from the clock plus an
  offset of its own, keyed on what it marks, so a screen that rebuilds does
  not restart it either. It pops in only when it appears on a host that
  persists (the nav, the pills, the quest scroll).

**A claimed reward flies into the header**: the quest claim, the daily /
season chests, and what the player's own TAP gathers — a resource cell, a
building's worked cells, a house's rent (crews' deliveries and the rent tick
land as numbers on the map only):

- It bursts from where it was claimed — the tapped cell, the tap that
  claimed it, or the centre of the screen — with a flash and, for a claimed
  reward (not a tap, which has its own sound), a powerup chime.
- Each resource in it leaves as N fragments of its icon: one per minute of
  the city's own production the reward is worth, at least 3, at most 12, and
  5 for a coin the city does not produce (Gems) or produces none of yet. A
  tap that gathered fewer than 5 flies one fragment a unit instead.
- Each fragment bursts out to a spot of its own, hangs, then flies in an arc
  into that resource's slot, 70 ms after the one before; a second resource
  leaves 180 ms after the first. Burst 260 ms, flight 620 ms, speeding up.
- The header counts the reward in as fragments land: each adds its share,
  the icon swells, sparks fly off it, and a tick plays — a coin for Gold and
  Gems, a pop for goods — each a shade higher than the last.
- Only what the plank shows flies; the wallet holds the reward from the
  instant of the claim either way. Reduced motion: no flight, the new totals
  just show.

**Short of funds** shakes **the counter**, not the button. A claim pops a
small burst of gold sparks. Nothing pulses forever except the call-to-action
badge.

---

## 4. Where screens are specified

- A feature's screens live in its feature doc, built from this kit.
- §5 holds the screens that belong to no single feature: the header, the
  quest scroll, the banner and toast, the nav, building and placing, the
  district card, Settings and the welcome-back report.

## 5. Screens

### 5.1 Resource header

**Purpose.** Tell the player what they have, right now, without making
them read.

- **The coins, at the left**: **Gold**, **Food**, **Wood** from the start —
  the three that gate the early game — and **Stone** once its gating
  technology is complete or its balance is above zero. The technology clause
  makes it sticky (a counter never vanishes when the player spends back to
  zero); the balance clause covers a reward arriving early. It slides in
  with a one-off banner. Four coins is the worst case.
- **Mana, then Gems past the rope, at the right — pinned.** Mana pays for
  every tap, so it is unconditional: a player whose tap just refused must be
  able to read why without scrolling the header, and Gems is what refills it.
  The Gems slot carries the green `+`.
- **Mana's slot is its own gauge** with the pool over it; while it fills, the
  pool and the next unit's countdown ("+1 in 4m 12s") take turns in the same
  place, 3 s each with a fade, and any change to the pool shows the pool at
  once.
- **Narrow screens**: the row never wraps. The coins scroll horizontally
  inside their own box, and everything from Mana rightwards stays pinned.
- **Purse (tap to expand)**: the full wallet — the coins, Gems, and Knowledge
  once the player has met it — as a plain list.
- **The Knowledge tab, always — under the plank.** The bar paces the whole
  game, so it is prominent and never contextual
  ([`../features/07-research.md`](../features/07-research.md) §3):
  - A painted wooden tab with a gold-inlaid rim, nine-sliced, **centred under
    the plank** as if it came out from behind it: straight bottom edge,
    rounded bottom corners, no point or decoration.
  - **Straight on the wood, with no dark slot round them**: the Knowledge book
    at the plank's icon size, the number held, and **ten narrow tall
    segments** packed tight, one per point of the cap.
  - Under the segments, a small caption that **takes turns with a
    crossfade**, as the Mana readout does: *+1 in 42m* (the next point) and
    *Full in 3h* (the whole bar).
  - **Full**: every segment lit with a soft glow, a glint on the book, and the
    caption reads *Full*. Over the cap (a lump, a purchase) the number reads
    what is held — *23* — and the segments stay all lit.
  - A **+** knob inside the tab, the same as the Gems', opens the Knowledge
    sheet; tapping the tab opens it too. The sheet is the bar and **three
    offers side by side**, each on a stat tile, set straight on the sheet with
    no box or title round them, the sheet a little wider than a centred
    window's default so the buttons run nearly edge to edge: the amount on
    top as the book and a number, and the price on the button's face — 1
    Knowledge for Gold (dearer every time), 1 for Gems, 10 for Gems. An offer
    the player cannot pay goes dark.
  - About 100 pt wide, the segments ~3 pt each. **The Daily chest pill starts
    below the tab** rather than beside it.
  - It **slides up behind the plank while any menu is open**, and back down
    when the menu closes (260 ms in, 220 ms out; none under reduced motion).
    The Research book carries Knowledge on the plank instead, beside Gold.
- **City status — one contextual plaque**, not permanent widgets: a small
  wooden plaque under the coins, showing whichever number the player can
  currently act on, and hidden otherwise:

  | When | Shows |
  |---|---|
  | A worker building's card is open | **Workers** free to assign |
  | Build or placement is open | **Free builders `n`** |

- **Population is on the map**, as a pill over the **Townhall** — where
  villagers are trained, so the number and the control that changes it are
  one object.

### 5.2 Quest tracker

**Purpose.** The single answer to "what do I do now?" — the most important
UI element in the game for a new player.

- **The card is the button.** Tapping it does the only thing there is to
  do: point at the goal while the quest is running, take the reward when it
  is done.
- **The reward only appears once the quest is complete** — its arrival is
  what makes finishing feel like a payout.
- **Bottom left**, 25 rpx from the left edge and 20 rpx above the nav: the
  thumb lives at the bottom, and the top belongs to the header and the fog
  the player is tapping.
- **The scroll**: parchment between two rollers, one nine-sliced piece, so
  both width and height stretch; 520 × 200 rpx while a quest runs (it grows
  with the words). The trough and its gold fill are painted and sliced at
  their rounded ends.
- **Running**: the title and description; under them, the goal's mark resting
  on the start of the trough, which runs to the right roller, the count
  inside it. No control on it: a tap anywhere points at the goal.
- **Done**: only the reward — each prize an icon and its count, no label, no
  chip — and a green **Claim** slab under it, shrunk to fit; no title, mark,
  description or trough. It glows gold round the parchment, bobs gently, and
  wears the call-to-action badge.
- **A new quest unrolls**: the parchment fades in and widens rightwards from
  its left roller (520 ms), and the words fade in from 360 ms, just before it
  is fully open — with the scroll-open sound.
- **A claimed quest rolls up**: the words fade out (160 ms), then the
  parchment narrows back to its left roller and fades (420 ms) — with the
  scroll-close sound. A claim that hands over the next quest plays both,
  **0.5 s** apart. Nothing on the scroll can be tapped while it moves.
- A quest that arrives while a window covers the map unrolls when the map
  comes back.

### 5.3 Banner & toast

Two channels: *good news you should enjoy* (banner) and *why that didn't
work* (toast). Good news and bad news never look alike.

**The banner**

- A painted cloth hanging from a wooden rod under the header, ending in a
  point; **gold** for something new, **green** for something built, **blue**
  for something learned. Fixed width (420 rpx: the point cannot stretch
  sideways), nine-sliced vertically, so it is as tall as its words.
- Top to bottom: the subject's art, what happened, its name, a line about it.
- **In**: the words hidden, the cloth fades in and grows from its rolled
  height (the rod and the point, nothing between) to its full height in
  520 ms; the words fade in from 80 % of the way.
- **Out** (after 5 s, or a tap): the words fade out (160 ms), then the cloth
  shrinks back to its rolled height and fades (420 ms). Banners queue, one at
  a time.

**The toast**

- One sentence of white text with a soft dark shadow, no slip and no icon, in
  the middle of the screen. It floats up, holds 2 s and fades.
- A refusal plays the denial sound; a confirmation looks the same and plays
  none.
- Toasts queue and dismiss on a tap.

### 5.4 Bottom nav

**Purpose.** Reach the places; leave the map for one of them.

- **Five tabs**: **Store · Relics · Heroes · Research · Build**, each an icon
  over a label (§3.6), the bottom inset under them. A tab not yet opened is
  padlocked with its reason
  ([`../features/22-progression.md`](../features/22-progression.md)).
- **Settings is not a tab.** It is a drawer opened twice a month, so it is an
  icon-only round wooden knob hanging under the header's right end.
- A tab wears the call-to-action badge with its count when the screen behind
  it has something pressable now (§6.7).
- **The nav steps aside while any window is open** (§6.5); every window
  carries its own close.

### 5.5 Build menu

**Purpose.** Choose what to add to the kingdom.

**The flow.** Nav **Build** → Build menu → tap a row → Placement → **Build**
→ the map, with the building under construction.

- **Close** in placement returns to the Build menu, on the same tab and
  scroll position. The menu's close returns to the map.
- While the menu or placement is open, the header plaque shows **Free
  builders `n`** (§5.1).

**The Build tab (nav).**

- The call-to-action badge with a count: how many buildings can be started
  right now — unlocked, under their cap and affordable. No badge at zero
  (§6.7).
- A quest that points at a building highlights the tab and that row.

**The menu.**

- **The whole height under the header** (a tall window): the tabs stay put
  and only the list under them scrolls. Placement closes it and opens on the
  map.
- **Three tabs** — the nav's wooden tab plates. The selected one is the plate
  pressed into the wood, not lit or gilded. Each wears the same badge and
  count as the nav tab:

  | Tab | Buildings |
  |---|---|
  | **Economy** | Housing, Farm, FarmLands, Sawmill, Quarry, Docks, Sanctum, Carpenter, Mason's Yard, Smelter, Rune Carver |
  | **Military** | Barracks, Spear Hall, Shooting Grounds, Stables, Infirmary |
  | **Decoration** | Garden, Well, Orchard, Statue, Plaza, Shrine |

- The Decoration tab carries the Harmony line above its rows — `supply /
  demand` and what the surplus is paying — once something demands Harmony.
  Before that it carries one tip: *A house beside a decoration earns more
  Gold*.
- Opens on the tab last used; the first time, on Economy.
- Buildings sit in **a vertical list of full-width rows**; about six fit on
  the reference phone, the last cut by the edge so it says there is more.
- Order within a tab: what can be built now, then what cannot be paid for
  yet, then what is capped, then what a technology still has to open — each
  group in the authored build order.

**The row**, left to right:

| Part | Shows |
|---|---|
| Art | The level-1 sprite in a frame of darker paper, its roof a little past the frame's top |
| Name | The name, and the ordinal it would get, small and quiet (*Housing #3*) — the price is that instance's |
| Promise | The building's promise, at most two lines |
| Cost | Chips for every coin and good; a short chip turns clay. A decoration adds a `+N Harmony` chip once Harmony is demanded |
| Side plaque | ⏳ build time — the wait at the cell the ghost will appear on — over **Built `n / max`**, the same width down the list |

**Row states.**

| State | Looks | Tap |
|---|---|---|
| Startable | Plain | Opens placement |
| **New** | A *New!* wax seal on the corner, until the tab has been seen once | Opens placement |
| Can't afford | Plain; the short chips in clay | Refused: the row shakes and the short chips pulse |
| At the Townhall cap | Drained parchment, sepia art, a padlock; in place of promise and price: *Needs Townhall level 3* | Refused |
| At the absolute cap | As above: *You have as many as the realm allows* | Refused |
| Short of Harmony | As above: *Needs 12 more Harmony* | Refused |
| Tech-locked | As above, at the end of the list: *Research Fishing*; no side plaque | Refused |
| Quest target | Highlighted | As its state |

- A free builder is **not** a row state: a build with every builder busy is
  refused when it is confirmed (§5.6).
- A lock wins over the clay chips: a capped row shows its reason only.

### 5.6 Placement

**Purpose.** Put the building somewhere good, and understand why one cell
is better than another. Only reached with the price in hand (§5.5).

**On the map.**

- The camera centres on the legal cell closest to the Townhall and the ghost
  appears there.
- The ghost is the building half-transparent on its footprint, outlined in
  gold, inside its **reach** drawn as one thin white line; each captured
  cell is labelled with its depot
  ([`../features/05-city-and-districts.md`](../features/05-city-and-districts.md)
  §4.1).
- **Move arrows**: four green arrows on the ground round the ghost's
  footprint, one per side, pointing outward along the isometric grid's axes.
  An arrow shows wherever the map goes on that way. They bob gently outward
  along their axis (none under reduced motion), and hide while the ghost is
  held.
- No verdict: the depot labels on the captured cells are the whole reading
  of a spot.
- Only the Docks outline their legal cells.
- Gestures ([`../features/05-city-and-districts.md`](../features/05-city-and-districts.md)
  §4.3): drag the ghost to carry it; tap any cell to send it there; a drag
  that starts off the ghost pans the camera; a long press on a movable
  building picks it up. The ghost lifts while held, goes anywhere on the map
  and never commits.
- The ghost floats over its plot — the plot washed white (red where
  illegal), its shadow under it — and lifts higher while held.
- **On a cell it may not stand on, the ghost is red** — rim and a red wash
  over its body — and the Build / Move button is disabled, the reason
  printed beside it.
- The first placement ever shows a one-time coach line: *Drag the building,
  or tap where it should go*.

**The bar** — a small window across the bottom, in the format of every other
menu:

- **Header**: the building's name and its ordinal (*Sawmill #2*), and the
  close at its right. Close is the cancel — back to the Build menu (§5.5).
- **Body**, one row: the level-1 sprite on its tile; the building's promise,
  and under it ⏳ the build time; **Build** — primary, priced (§6.4), at the
  kit's default size.
- No legal cell anywhere: Build is disabled and the bar says *Nowhere legal
  to build it*.

**Confirming.**

| Case | Result |
|---|---|
| A builder is free | Paid, placement closes, the building stands in scaffolding with its timer and a builder walks to it; the map stays where it is |
| Every builder is busy | The builder sheet opens over placement; dismissing it returns to the positioned ghost ([`../features/06-construction.md`](../features/06-construction.md) §2) |

**The builder sheet** — a centred window over the dimmed placement screen:

- Header *Builders* and its close — the only way out; there is no *Not now*.
- Top row: an illustration of two builders, a man and a woman, on its tile;
  beside it *All n builders are busy* and *Nothing waits in line — finish a
  job to free a builder.*
- The crew: a vertical list, one row per builder up to the ceiling.
  - **Busy** — the building's sprite; its name and what is being done
    (*Housing #5 · Building*, *Quarry · Upgrading to Lv 3*) over the blue bar
    with the time left inside it; the priced gem **Finish** at the right.
  - **Empty, the next to hire** — a dashed socket with a builder's
    silhouette, *A third builder*, and the priced gem **Hire**.
  - **Free** — a builder whose job ended while the sheet is open, on its own
    or by Finish: a gold builder medallion, *Free* in leaf and *Ready to
    build the Sawmill*, and the priced primary **Build** — the placement
    bar's own price. The row wears a thin leaf rim and a soft glow. Build
    starts the build on the ghost's cell and closes the sheet.
  - **Empty, further up** — the socket and its name alone, faded.
- With a free row the headline reads *A builder is free* and the line under
  it *Build the Sawmill now, or keep it for later.*
- At the ceiling there is no empty row, and the list ends with *4 is as
  large as a crew gets.*
- The sheet never closes on its own: a job that ends turns its row Free in
  place. Closing it with a free builder returns to the positioned ghost.

**Moving** uses the same screen: no price and no time, and the button reads
*Move*.

### 5.7 District card

**Purpose.** Everything you can do to one building, in one place — the
most-used window in the game.

- **The header**: the building's name and its level a size down (*Housing
  #3* *Lv 2*), then **Move** (when the building can move) and Close. Move is
  the close's twin in wood — a round wood button with four-way arrows carved
  into it.
- **The head**: one row — the portrait, the description (bold, lighter ink),
  and Upgrade (the kit's default button, its fixed size) — each anchored to
  the top and growing down. The card lists no requirements: the upgrade
  popup shows each one and whether it is met. Upgrade wears the call to
  action when every requirement and every cost is met and a builder is free —
  the same check the upgrade itself runs. The portrait is a section with a
  small leafy ornament pressed into each corner, and the building drawn
  larger than the tile, clipped to it.
- **Section headings** (§3.7) head each section, shown only when the section
  is: what the building does — the unit it trains (*Warrior*, *Villager*),
  *Ward*, *Workshop*, *Residents*, *Workers*, *Crops*, *Harmony* — and
  *Neighbours* when an adjacency is in effect. The stat tiles have no
  heading.
- **The building on the map** pulses white while its card is open — its art
  a touch brighter and a soft white glow around its edge, once every 1.4 s —
  so the card's building is told apart from its neighbours.

**Stat tiles** (the card's figures): one tile per figure — a big icon, then
the SHORT name (bold, in ink; eight letters at most — *Range*, *Speed*,
*Training* — so three fit a phone's width; the full name is the tile's
tooltip and the upgrade popup's) over the value (lighter ink), at the
building's CURRENT level only (the next level's value is the upgrade
popup's).

- Rates are per hour: a producer's output and a house's rent (*Gold
  +1.8k/h*, the level's rent bonus counted in, so a house has no Rent tile).
- A house's *Beds* reads residents/beds (*2/2*) and it has no Residents
  section.
- *Storage* reads held/capacity (*120/8.6k*), its value in clay when the
  store is full; the card has no Collect button — a tap on the building
  collects.
- Each tile is a section, 112 × 58 pt (narrower only where three would not
  fit), 14 pt apart; three to a row, centred, a fourth wrapping to a centred
  row of its own.

**The training widget** — one block for every building that turns something
out: the Townhall's villagers, a hall's soldiers. Each building trains ONE
thing; a hall picks only its unit's rank.

- **Rank picker** ([`../features/combat.md`](../features/combat.md) §6.4):
  on a hall, the portrait is a button (a carved caret on its rim) that opens
  a list in the panel under the numbers — one row a rank: portrait with its
  coin, name, the four stats as values, and a padlock with its reason on a
  rank still shut. The picked rank is what the panel shows and Train trains;
  a newly unlocked rank is picked for the player.
- **Unit portrait** (wherever a unit is shown round): three layers — a round
  paper base in a flat outline, a circular mask inside it, and the unit's
  bust drawn a little larger than the mask, so a bust that carries a
  medallion of its own has that ring cut away.
- **Panel** (a section), headed by the unit's name on the section's rule: the
  round portrait with how many the player owns on a pill centred over its
  foot (*x17*: soldiers in the army, or the city's villagers), the tags, one
  line of flavour, and the priced Train button — its costs above it. The
  training time is the building's own stat tile (*Training*), one trainee per
  building.
- A soldier adds its four numbers — Attack, Damage, Defence, Health — as
  small tiles (the mark and the number, the name under them) in one row under
  the portrait and the flavour, beside the Train button, which runs down past
  them; the bust rises out of the panel through its top edge, so the tiles'
  feet line up with Train's whenever the flavour is no taller than the bust.
- A gate keeps the button, disabled, and puts a padlock and a short reason
  where its price would be: *No house to live in*, *Max army reached*,
  *Needs Archery*, *Room for 7*.
- **Amount.** A round wooden knob over the panel's top-right corner turns
  through *x1*, *x10*, *x100* and *All* on each tap; every card shares it for
  the session. Train orders that many as ONE order, priced whole — all of
  them or none. *All* is as many as the room and the purse allow, at least
  one. A fixed amount the room cannot hold is a gate (*Room for 7*).
- **Sound.** Train only clicks; the hall's batch sounds once, when its line
  runs dry.
- **Hold to train.** A tap on Train is one press. Held, it presses itself:
  after 0.35 s at 3 a second, climbing to 15 a second over 2.5 s. It stops on
  release, when the finger moves (a scroll), or where a tap would find the
  button dead — a gate, or the purse short — without opening the shortfall
  sheet. While it repeats it stays pushed in.
- **Tags:** a chip for the unit's type (blue: Melee, Ranged, Mounted; Worker
  for a villager). Tapping it opens the tooltip with the type chart's word on
  it: *Strong vs Lancers, weak vs Archers*. Paper chips are kept for special
  traits.
- **Batch**, at the foot of the same panel: one building trains one unit, so
  its whole line is one batch — the unit's portrait with its count (*x5*);
  beside it *Training* over the bar (the time left inside it) and *Total
  time: 1m 20s* under it; and the gem Finish button, under a rule and with no
  heading of its own. Nothing in the line: *Nothing in training*, centred,
  and no Finish — at the same height as a batch, so the card does not jump
  when training starts or ends.

**The workers block** (worker buildings), a stepper:

- the red − knob, the villager's round portrait, *2 / 3* — the crew in title
  type, the most it can hold smaller and muted — and the green + knob;
- nothing else: the stat tiles lead with what the crew MAKES (*Food
  +2.7k/h*, the resource as the tile's word) and how fast its level makes the
  crew work (*Speed ×1.25* — ×1 at level 1, climbing with each level). What
  there is to work is the map's to show: while the card is open — and while
  the building is placed or moved — the area is outlined and every tree,
  field or rock its crew would work wears the placement ghost's white rim.
  The crew size, range and haul are the upgrade popup's only;
- no tip: how many villagers a building's fields keep busy is the player's
  to see by watching them work;
- the villagers still free to assign are the header's plaque while the card
  is open — a plain count, not a share.

**Under construction** (a building being built or upgraded):

- the head's Upgrade slot holds the gem Finish button with its price;
- under the portrait: a slim blue bar with the time left inside it, laid
  over the tile's bottom edge;
- under the description, centred at the foot of the head: what is being
  done, in one word (*Upgrading*, *Building*, or *Waiting* for a builder),
  breathing slowly between 75 % and full opacity;
- a painted hammer floats over the portrait, with no base, and works it in a
  loop, like a magic hammer: one blow at the right corner, a flight round in
  a loop over the picture and across to the left corner — keeping its
  bearing, never turning over — two small taps there, and an arc back. Each
  blow throws a few sparks where it lands. Still with reduced motion;
- nothing else on the card changes, and there is nothing at its foot.

**The upgrade popup** (the card's Upgrade opens it), a centred window titled
*Upgrade to Level 3*:

- two portraits, the building at its current level and at the next, a
  yellow arrow between them, each with its level on a badge under it
  (*Level 2* blue, *Level 3* green) — painted enamel plaques, flat, no lip;
- *Improvements*: one row per stat that improves — its icon, its full name,
  its CURRENT value, and what the level adds as green text on the right
  (*x1.25* … *+0.25*, *ring 5* … *+2 rings*). A stat the level does not
  change is not listed;
- *Requirements*, shown only when the level has any: one row each — its
  icon, what it asks, and a green tick or a red cross; an unmet row is red
  text on a light red row. Nothing links to where it is met;
- the price and the build time over the Upgrade button. An unmet requirement
  locks the button (a padlock) with *Complete all requirements to upgrade*
  under it. A short purse turns its price red and the button off with no
  line; with no builder free the line reads *Every builder is busy*.

### 5.8 Research

The research book — one pinned page on a stack of papers, bookmarks where the
nav bar was, every technology in one of three states, the stat-tile card with
the kit's bar, quill arrows, and a loose research page for a technology — is
specified in [`../features/07-research.md`](../features/07-research.md) §5.

### 5.9 Settings

**Purpose.** Sound, save, and the escape hatch. A window opened from the
Settings knob (§5.4); each row has its own painted mark in a parchment
vignette.

- **Sound**: **Music**, **Sound effects**, **Ambience** — three switches,
  each a carved wooden trough with a round brass knob, leaf-green when on.
- **Your kingdom**: where it is saved, in plain words ("Your kingdom is
  saved to this device" / "…to the cloud"), and the last-saved time.
- **Start over**: a two-step confirm that states the consequence in the
  first step, not the second.
- The version line as small print at the very bottom.

### 5.10 Welcome back

**Purpose.** Pay off the idle half of the design. A window on load, only when
the absence exceeded ~2 minutes:

- "Welcome back — your kingdom worked for **6h 20m**" (and, when some
  building's store filled, "Some stores filled up before you got back — tap
  them to collect" as a gentle nudge, not a scold).
- What the stores gained as a short list of **icon + amount** rows: Gold
  from rent, each resource delivered by workers, villagers trained.
- What finished while away: buildings completed, upgrades — each with its
  sprite.
- One green button: **Continue**. The stores are collected on the map, a
  building at a time.
- If the quest advanced, hand off directly to the quest scroll.

---

## 6. Cross-cutting rules

### 6.1 Every disabled control gets a reason

Rendered next to it, in plain words, always a full sentence — except a
price, whose reason is its colour (§6.4).

### 6.2 Say what a technology unlocks before it is researched

The player always sees what a technology gives — the building, unit or
bonus, with its art — before buying it, never only from a banner afterwards.

### 6.3 The type floor

A sentence is never under 13 pt; a tiny tag never under 11 pt (§3.4).

### 6.4 A price belongs to the button that spends it

- When an action has a cost, that cost is part of the thing pressed: above
  the slab on its own section, or on the button's face where room is tight —
  one icon-and-amount term per currency. **Any term the player cannot pay is
  drawn in clay.**
- The player reads the verb and what it costs in one glance, and the number
  never disappears when it matters most.
- **The red is the reason.** An action blocked *only* by its price needs no
  sentence: affordability is a colour, not a reason. A reason line is for
  obstacles that are **not** money — a Townhall level, a missing technology,
  a busy hero, nowhere legal to build. The price, the red and the disabled
  state always come together.
- **What stays outside the price:** consequences — a build duration,
  "instant", "takes 2m 30s". Those are what you get, not what you pay.
- **Non-wallet prices count too.** A per-collectible counter (Fragments) goes
  in the price like everything else, reading `have / needed` so the gap is
  the thing you see.

### 6.5 The header outranks every menu; the nav bar steps aside

- A menu is opened **over** the game. The resource header stays above it,
  undimmed and tappable, full-screen menus included: what you can afford is
  the reason you opened the menu.
- **The nav bar leaves while a window is open** — a sheet, or the district
  card: it slides down out of the frame (200 ms) and slides back up when the
  map returns. Every window brings its own way out, and the space the bar
  held goes to the window.
- **The Settings knob hides while any window is open**, like the nav bar: it
  is the affordance for the map.
- The stack, bottom to top: map · the notices column · district card ·
  **menus and sheets** · header · nav bar · the battle playback · the gacha
  reveal · the rewarded video. Every menu shares one layer, so a new screen
  gets the right behaviour without being enumerated.

### 6.6 A centred window, for a question

Bottom-anchoring is the default because most windows are drawers over a
screen you are still using. A short, modal, one-decision window — an offer, a
confirmation — sits in the middle of the play area instead, because a drawer
is the wrong metaphor for something that wants an answer before you carry on.

### 6.7 A lit tab never lies

- A tab wears the call to action when the screen behind it has something the
  player can press **this second** — not when it merely contains content.
  Build: something affordable *and* placeable.
- Inside a screen, the same question is asked per item and answered with the
  same badge: "exists" and "go" are different things.
- The light and the button answer **the same question with the same check**.
  A light that drifts from its button is worse than no light, because it
  sends the player to a screen where nothing is pressable.

### 6.8 A screen does not blink

A screen that updates — every second while something ticks — keeps its
images, its scroll position and its state; its slide-in plays once, when it
opens.

### 6.9 A labelled button is one verb

- A button's label is a single verb: *Build*, *Move*, *Hire*, *Finish*,
  *Upgrade* — never *Move here* or *Hire a builder*.
- What the verb acts on is said by the screen around it: the row, the
  window's title, the ghost on the map.

---

## 7. The dials, in the order to reach for them

| Dial | Moves | Reach for it when |
|---|---|---|
| **Reference frame** (1125 × 2436, match height) | the size of everything | never, in practice — every size is derived from it |
| **Text roles** (§3.4) | readability of every window | a window reads crowded, or a line truncates |
| **Window cap** (70 % of the frame) | how much map stays visible | a window scrolls when it should not, or hides the map |
| **Button height and width bounds** (46 pt; 112–144 pt) | how much a label can say | labels wrap or buttons look stretched |
| **Minimum tap area** (44 pt) | how forgiving a tap is | mis-taps on small pieces |
| **Motion timings** (§3.7) | how lively the chrome feels | windows feel sluggish, or snap |

## 8. Deliberately not in this design

- **A full-screen opaque menu** over the map, outside the few `tall` screens.
- **A permanent bar of every resource**, and a header that wraps to two rows.
- **Pixel art in the chrome**, flat fills, plastic gloss, glass, neon, cold
  grey or blue-grey panels, hairlines.
- **Emoji**, anywhere.
- **A second font family.**
- **A grey disabled row** with no reason beside it.
- **A swap of the nav for a single Close button** — every window carries its
  own close.
