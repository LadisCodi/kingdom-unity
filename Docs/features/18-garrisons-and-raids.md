# 18 · Lairs — a garrison with a clock

> **Scope.** The province's lairs: a monster camp found by revealing its
> ground, that holds a zone around it, raids the city on a clock while it
> stands, and is cleared by a path of fights — then it is gone. The fight is
> [`combat.md`](combat.md); the battle screen is
> [`11a-ruins-ui.md`](11a-ruins-ui.md) §2.5.
>
> **Status: designed; built in the web prototype** — the zone, the clock,
> the raid, the hoard, the path of fights and the claim.

## 1. The rules, up front

1. **A lair is a path of fights**: its tier's count, one board each, fought
   in order. The last one beats it, and nothing stands behind it (§5).
2. **A lair's garrison is generated from its `guard`** (§2). The player
   attacks; the enemy never does.
3. **A lair holds ground.** Every cell within its radius is its zone: no tap,
   no build, no harvest there until it is cleared (§2.1).
4. **Discovering the lair starts its counter**, authored in minutes.
   When it runs out the garrison raids the city, then **three times a day**,
   inside the player's raid window, for as long as it stands.
5. **A raid is not a fight.** Nothing defends. The garrison takes, and the
   answers are to collect and to go and clear the lair.
6. **A raid takes from the buildings' stores, never from the wallet, and only
   materials** ([`03-economy.md`](03-economy.md) §3.2). What the player has
   collected is safe: collecting is the defence. Gold, Food, Wood, Stone. Never
   the Townhall's own Gold — the city's floor — and never Gems, Mana, Knowledge, Stardust, Hero XP, goods, cards, relics, heroes
   or units.
7. **A raid is priced in production, not in units**, capped by a fraction of
   what the stores hold. **A lair carries at most a day of raids** of each
   material; what it takes over that is lost. Clearing it returns its
   hoard.
8. **Claimed, a lair is gone for good.** Its ground is the city's, and it
   never returns.
9. **The lair is the incentive, not the punishment.** It is a small personal
   event with a clock, whose whole job is to send the player's army out.

## 2. The lair

- Authored **per lair**, on the map:

```
lair { tier, size, radius, sight, guard { threat, power, warningMinutes, mix }, flavour }
```

- `size` is the camp's footprint: **2×2** for every lair.
- `radius` is its zone (§2.1), in **Chebyshev** rings around the footprint.
- `sight` is how far its silhouette shows past the fog before it is found
  ([`01-map-and-fog.md`](01-map-and-fog.md) §4.1).
- `flavour` is the card's line over its painting (§7): two lines at most.

- `threat` is a unit type or `Any`. The creature is derived from it; there is
  no second list:

| `threat` | Reads as | `threatMix` | Composition answer |
|---|---|---|---|
| Warrior | **Orcs** | all Warrior | Archers |
| Lancer | **Goblins** | all Lancer | Warriors |
| Archer | **Harpies** | all Archer | Cavalry |
| Cavalry | **Wolf riders** | all Cavalry | Lancers |
| Any | **a Drake** | even across the four | none — a raw power check |

- `power` is what the party has to beat: its attack after the type chart,
  scored against this number. It is on the same scale as a room's `power_req`
  ([`11-expeditions.md`](11-expeditions.md) §6), so the same figure is the
  budget the generator spends on squads, seeded
  from the lair id ([`combat.md`](combat.md) §11). Same lair, same garrison,
  every time.
- **A `mix` makes an army of its own**: weights by unit type, the generator
  spending the budget on those types alone, the heaviest first. Without one,
  the threat takes the lion's share and the rest is split evenly. A world
  camp of the same creature fields the same mix.
- The warning is per lair: a harder lair gets a longer one, because the
  army it needs takes longer to build.
- A lair's **tier** keys the `garrisons` entries that are not per site: take
  seconds, Hero XP and Bag items (§8).

| Lair | Tier | `threat` | `mix` | `power` | Warning | `radius` | Zone | `sight` |
|---|---|---|---|---|---|---|---|---|
| Orcs | 1 | Warrior | Warriors 4 · Lancers 1 | **60** | **30 min** | 2 | 6×6 | 3 |
| Harpies | 2 | Archer | Archers 7 · Cavalry 3 | 300 | 90 min | 2 | 6×6 | 3 |
| Goblins | 3 | Lancer | — | 440 | 120 min | 2 | 6×6 | 3 |
| Wolf riders | 4 | Cavalry | — | 700 | 180 min | 2 | 6×6 | 3 |
| Drake | 5 | Any | — | 1,000 | 240 min | 3 | 8×8 | 4 |

- Placement is authored on the map.

- **`power` is a budget in troops' worth, and the count is what the player
  sees**: the generator spends it on each unit's `power`, so fifteen orcs is
  15 × 3 (§2, [`combat.md`](combat.md) §5).
- **A garrison more than six squads of rank I can hold fields evolved
  creatures** — veteran orcs beside orcs ([`combat.md`](combat.md) §6, §11).
  No province lair today is that big.

### 2.1 The zone

- **Drawn from the moment the lair is discovered** (§3), over every fog
  state: a border and a tint on every cell inside. Until then it is not drawn
  and denies nothing.
- **Inside the zone, while the lair stands:**
  - **No tap on the ground.** A tap on a feature is refused and costs no Mana.
  - **No placing, and no moving a building in.** A footprint with any cell
    inside is refused.
  - **No harvest.** A crew never picks a node inside; a haul already on its
    way lands.
  - **Spells skip it.** An area cast works every cell of its area except the
    zone's.
- **The fog is not the zone's.** A cell inside may be paid for, so the
  frontier can reach the lair.
- **No building ever stands inside**: a building stands only on Revealed
  cells, and revealing one of the zone's discovers the lair first.
- A feature with a footprint is inside if any of its cells is.
- Zones that overlap are one zone; a refusal names the first lair in lair
  order.

## 3. The counter

- A lair is **discovered** the moment any cell of its zone becomes
  **Revealed** ([`01-map-and-fog.md`](01-map-and-fog.md) §4) — by a paid fog
  tap, a building's reveal ring, a spell, anything that reveals. A cell that
  is only Discovered, under the scrim, does not wake it.
- The counter starts on discovery:
  `nextRaidAt = discoveredAt + warningMinutes`.
- After the first raid, `raid.perDay` (3) raids a local day: the window
  `raid.windowStartHour`–`windowEndHour` (9–23) is cut into equal slices, one
  raid in each, at a moment hashed from the lair, the day and the slice.
  Cleared: no counter.
- **One counter per lair.** Several may run at once; raids due at the same
  instant resolve in lair order.
- **It is a timer.** It runs and resolves in full while the player is away.
- A cleared lair is gone for good. No re-infestation.
- **Minutes, not hours.** The Orcs' thirty minutes says *you have this
  session and maybe the next*. What bounds a long absence is the hoard cap
  (§4), not the counter.

## 4. The raid

- Resolved at `nextRaidAt`, with no fight. **The take**, per material in Gold,
  Food, Wood, Stone:

```
base = cityRate × takeSeconds(tier)                  # seconds of the city's own production
take = floor( min(base, stored × raid.takeFractionMax) )
```

- `cityRate` is the city's current production of that material — the crews'
  gather rate, plus rent for Gold. It is a fact about the city, not an
  accrual, so a raid replays identically. **They take from what you make**: a
  material the city does not produce is not taken.
- `stored` is what every store in the city holds of that material.
- `raid.takeFractionMax` (0.5) bounds a raid on nearly empty stores;
  `takeSeconds` bounds one on full stores.
- The take is spread across the buildings in proportion to what each holds.
- A raid that empties a full store sets its crew going again from that moment.
- The **hoard** is a per-lair counter of what it has taken, capped per
  material at a day of raids (`perDay × cityRate × takeSeconds`); a raid over
  the cap still takes, and the rest is lost.
- Each raid posts a *Raided* news (§7).

## 5. Clearing the lair

- **Attack**, on the lair's card (§7), opens the battle screen on the next
  fight ([`11a-ruins-ui.md`](11a-ruins-ui.md) §2.5): the garrison's squads in
  view, their power against the party's, its Mana, the slots, **Attack**.
- **What the player sees is what they fight.** The squads are derived from
  `guard` (§2) and their sum is the number the attempt is scored against, so
  the authored budget never appears on screen and never has to be trusted.
- **A lair wants soldiers**: soldiers alone, or soldiers with heroes. A hero
  alone is refused (*A lair wants soldiers*), and so is nobody at all. The
  first fight in the game is soldiers alone: it comes before the Tavern, and
  the kingdom owns no hero until then.
- **No hero is ever busy.** Every fight in the game resolves the instant it is
  entered, so a hero is never away and never unavailable
  ([`10-heroes.md`](10-heroes.md) §2.7).
- **Supplies** are a flat cost per tier, paid on entry and never refunded.
- The fight resolves on entry, the player attacking
  ([`11-expeditions.md`](11-expeditions.md) §5). A power shortfall warns,
  never blocks. Retry is unlimited and identical to a first attempt.
  - **Win, short of the last fight:** the path moves one step on and pays
    its share of Hero XP. The lair still stands, holds its ground and
    **keeps raiding**.
  - **Win, the last fight:** the garrison is beaten, its counter stops, and
    its card offers **Claim**.
  - **Lose:** the Mana is spent and the path stays where it was.
- **The path** (`garrisons` › `fights`, by tier):

  | Tier | 1 | 2 | 3 | 4 | 5 |
  |---|---|---|---|---|---|
  | Fights | 3 | 4 | 5 | 6 | 7 |

  - **The last fight is the lair's own garrison**, at its `guard.power`.
  - The fights before it ramp evenly up to it from **half its power**
    (`delve.firstFightPower`). The path makes a lair longer, never harder to
    finish.
  - Each fight's garrison is rolled for that fight, the lair's creature in
    the lead.
- **An attempt costs Mana**, win or lose: `combat.fightMana` (20), as every
  attack does ([`08-magic.md`](08-magic.md) §1).
- **The attempt costs soldiers, win or lose**, by the rule every fight
  follows ([`combat.md`](combat.md) §4): the garrison's power against the party's
  defence, most of the fallen into the infirmary and the rest gone.
  - Heroes are never among the dead: a hero can fall in a fight and is whole
    when it ends ([`10-heroes.md`](10-heroes.md) §2.3).
  - **The screen says the price before it is paid** — the expected losses sit
    under the button, beside the Mana.
- **What it pays:**
  - **Hero XP by tier** (`garrisons.heroXp`: 500 · 1,500 · 4,000 · 10,000 ·
    25,000), split evenly across the path: every fight short of the last pays
    its share when it falls, and the last share comes with the claim.
  - **The claim** pays the rest: the hoard in full, the first-clear Knowledge
    lump, the tier's Bag items and relic fragments, and the lair's ground.
  - **Then the lair goes**: its model sinks under a ring of dust and its zone
    fades out. Its cells keep their terrain and fog state and are ordinary,
    buildable ground.
  - The `ClearLairs` quest goal ([`12-quests.md`](12-quests.md) §1.1) counts
    a claimed lair.
- No technology gates a lair.

## 6. The doorway to combat

- The first fight is **the Orcs: the enemy in view, the outcome guaranteed
  by authoring.** It teaches the battle screen, the type chart and the
  board.
- **Twenty orcs is a company's job, not a hero's.** The Orcs are three
  fights, and the company of thirty walks the whole path, its losses carried
  from one fight to the next. The chain musters those thirty soldiers one beat
  before it
  ([`12-quests.md`](12-quests.md) §2), so the fight is won by the army the
  onboarding just built and the hero that leads it — which is what makes the
  military block mean something.
- Discovering the Orcs starts their thirty minutes, so the military block sits
  right after the reveal that finds it in the onboarding
  ([`12-quests.md`](12-quests.md) §2): Warrior → Barracks → first soldier →
  **a company of thirty** → **`DriveThemOut`**.
- Every later lair is the argument for the next hall, the next squad, the next
  tier.

## 7. The screens

- **The raid notices** ([`26-notices.md`](26-notices.md) §2): the
  *Raid coming* bubble while a lair stands — the nearest raid and its
  countdown, with a count when more are open — and a *Raided* news after each
  raid; several raids in one absence are one. Go goes to the lair.
- **The lair's card**, top to bottom:
  - the painting of the creature, with its flavour line;
  - the countdown to the next raid — or, once beaten, *Claim what they left
    behind*;
  - **Progress**: the path, one delve stone a fight joined by a dotted trail.
    A fight won carries a green wax seal, the next is lit, the ones ahead are
    dim, and the last is the boss's horned stone. Under it, *Fight 2 of 3*;
  - the reward the claim pays — the hoard, the last share of Hero XP and the
    Knowledge;
  - **Attack**, which opens the attack screen on the next fight — or
    **Claim**.
- **The playback** of a fight names it — *Orcs · Fight 2 of 3* — and a fight
  short of the last shows its Hero XP as spoils.
- **The map**: the lair's model on its footprint and its zone (§2.1). The
  marker carries the countdown badge while the lair stands.
- **A refused tap inside the zone** says why — *Orcs hold this ground* — and
  points at the lair.
- **The battle screen, on a lair** ([`11a-ruins-ui.md`](11a-ruins-ui.md)
  §2.5): threat always visible, power comparison, its Mana, party,
  **Attack**.
- No raid sheet, no defence screen, no army tab.

## 8. Dials, in the order to reach for them

| Dial | Recommended | Where |
|---|---|---|
| a lair's guard: threat, power, warning in minutes | §2 | the map, per lair |
| a lair's zone, in rings | 2 · 2 · 2 · 2 · 3 | `radius`, the map, per lair |
| take seconds per tier | 300 × tier | `garrisons` › `takeSeconds`, one entry per tier |
| take fraction max, of what the stores hold | 0.5 | `raid.takeFractionMax` (`exploration`) |
| raids a day, and the window they land in | 3 · 9–23 h | `raid.perDay`, `windowStartHour`, `windowEndHour` (`exploration`) |
| what an attempt costs | 20 Mana, as every attack | `combat.fightMana` |
| fights a lair takes, by tier | 3 · 4 · 5 · 6 · 7 | `garrisons` › `fights` |
| how hard its first fight is | half the lair's power | `delve.firstFightPower` (`exploration`) |

## 9. Deliberately not in this design

- **A home defence.** Nothing at home fights a raid; the roster is for
  attacking. A raid is a bill, not a battle.
- **Garrisons on landmarks.** A landmark is claimed for its Gold
  ([`01-map-and-fog.md`](01-map-and-fog.md) §6); the world map's siege is the
  contested landmark ([`15-social.md`](15-social.md) §6).
- **A repeatable lair.** Repeatable content is the world map's.
- Anything behind a lair: depths, rooms, a boss, a Guild.
- A lair found through the scrim: only a Revealed cell discovers it.
- A zone that grows, moves or spreads; a zone on the fog.
- Escalating waves; re-infestation of a cleared lair.
- An authored formation per lair — the generator builds it from `guard`.
- One raid clock for the whole city; counters in hours or days; one counter
  for every lair as a global setting.
- Rousing conditions beyond discovery — a hall, a hero, an army.
- Raids on the wallet.
- Raids on Gems, Mana, Knowledge, Stardust, Hero XP, goods, cards, relics, heroes or
  units.
- Buying protection or buying a lair away: a Gem shield or clear, an ad that
  repels a raid or lifts a zone, a "peace" SKU.
- A base hoard, a loot table on a lair.
- A march, a return march, a wounded state, anything *away* from home.
- Workers fighting; a wall or tower district.
- A creature list beside the threat type; randomness in resolution.
- A widget that opens itself.
- Player-versus-player raiding ([`02-map-scopes.md`](02-map-scopes.md) §5).
- A technology that gates a lair.

**Open questions:** OQ-72, OQ-109, OQ-110, OQ-111, OQ-112 in
[`../open-questions.md`](../open-questions.md).
