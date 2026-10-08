# 28 · Languages — English and Spanish

> **Scope.** The game in the player's language: which one, how the game's
> text and the data's text are translated, and how numbers follow it.
>
> **Status: designed; built in the web prototype.** Everything the player
> reads is in both languages but the names the shared world generates.

## 1. The choice

- Two languages: **English** and **Spanish** (Spain; the UI says *tú*, the
  cast calls the player *Majestad* and speaks with a reverential *vos* — all
  but Hob and Grukk, who say *tú*; never *usted*).
- First boot: the first of the device's languages the game speaks, else
  English.
- Settings › **Language**: each language named in itself (*English*,
  *Español*). Choosing one reloads the game in it.
- The choice lives on the device, not in the save.
- The data tools are always in English: they edit the source.
- The world server reads English.

## 2. The game's text

- Every text the game shows has its English as the key and its Spanish
  beside it; a plural has a form per count.
- One English word that is two in Spanish carries a context: *Free* on a
  builder is *Libre* in Spanish (*Gratis* elsewhere).
- Placeholders are `{name}`, filled with numbers already formatted.
- What the catalog lacks shows in English.
- A text with no Spanish, a Spanish with other placeholders, and a key the
  game does not say are refused.

## 3. The data's text

- The English in the game data is the source. The Spanish is an overlay per
  group: path → the English it was translated from, and the Spanish.
- The game reads a localized copy; a text whose English changed since shows in
  English until translated again.
- A sync adds new texts to the overlays, drops removed ones, and reports what
  is missing or stale.
- Localized: buildings (name, promise, description), goods, items, quests,
  store, speakers, unlock splashes, villains, world buildings, scene lines,
  lair flavour, ruins, technologies.

## 3b. The generated prose

- A technology card and a quest line are built from the data. Each stat's
  sentence is a translated template; Spanish names a building's target in
  parentheses or after a colon (*Desbloquea: Granja y Aserradero*,
  *+25% de almacén (Granja)*), so no article has to agree with it.
- Decimals the game writes follow the language.

## 4. Numbers

- Written in the language's locale: *25,000* / *4.99* in English, *25.000* /
  *4,99* in Spanish.

## Dials, in the order to reach for them

| Dial | Where |
|---|---|
| A game text's Spanish | the Spanish catalog, per area |
| A data text's Spanish | the data overlay, per group, after a sync |
| Which data fields are localized | the list of localized data groups |
| A new language | its name, its number locale and its catalogs |

## Deliberately not in this design

- Translating without a reload.
- Names the shared world generates (dungeons, rivals): every player on a
  board reads the same name.
- Latin-American Spanish as a separate language.
