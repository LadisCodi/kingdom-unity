# The Unity remake

> **Scope:** how Kingdom is rebuilt in Unity — the order of the work, what
> each step delivers, and what is still to decide.
> **Status:** foundation, architecture, balance, the city, the economy and the
> fog done; research built; the quest chain, the doors and the tutorial
> stage under way.

## 1. Principles

- **Rebuilt, not ported.** `Docs/` is the specification. The web prototype is
  read to understand a rule, never translated; the Unity game does not have to
  give the web's exact numbers.
- **Systems first, then Kingdom.** Every system is thought as a game-agnostic
  module, then applied to Kingdom (`CLAUDE.md`, *Architecture*).
- **SOLID, always.**
- **Playable steps.** Each step after the foundation ends with something to
  play in the editor: its rules in Kingdom, its modules, its views.
- **Fresh saves.** No web save is migrated.
- **The world server stays.** The authoritative world server keeps running as
  it is; the Unity client talks to it over HTTP.

## 2. Steps

| # | Step | Delivers |
|---|---|---|
| 0 | **Foundation** ✓ | repo, Git Flow, LFS, rules, clean docs; packages and plugins; scenes `Boot` and `Game`; ProtoLab's UI, camera, audio and feedback |
| 1 | **Architecture** ✓ | `Codigames/{Modules,Kingdom,Game}`, content outside, modules independent behind ports, `dotnet test` for pure code |
| 2 | **Balance** ✓ | definition interfaces in Kingdom; ScriptableObjects in `Assets/Data`; a one-off import from the web's JSON; the *Kingdom › Data* Odin window and its validation |
| 3 | **The city** ✓ | the province grid with its terrain and features; place, build and upgrade buildings with builders; the wallet; the offline advance |
| 4 | **The economy** ✓ | harvest by tap, Mana, stores and collecting, villager training and rent, harvest by crew; tap feedback (punch, reward flight) |
| 5 | **The fog** ✓ | reveal, the Gold price by ring, the Townhall's reach and its border, mountain blocks, abandoned buildings and their repair, treasures, landmarks and their claim, sighting; the notices (built, sighted, the chain done) ✓, lairs for the army |
| 6 | **Research** — under way | the Knowledge bar ✓, buying Knowledge ✓, the tree and its book ✓, a technology's sheet ✓, gates enforced ✓, bonuses read ✓; goods and precious materials in a price (with goods), the Atlas (with landmarks), Knowledge lumps (with landmarks, lairs and quests) |
| 7 | **The rest, in order** | crop plots planted and sown ✓, the builder's hammer ✓, the ground's states ✓, moving buildings, trees and plots ✓; the quest chain ✓ and the doors ✓; the tutorial stage — scenes, speakers, the box and its cast, typing and voices, the director ✓, the pointer (hand, halo, motes, the plot's glow, the camera to the target), locks and idle help ✓, the unlock splash ✓; the Bag and its speed-up picker ✓, decorations, Harmony and neighbours ✓, workshops and refined goods ✓, the army (units and ranks, halls, the Infirmary) ✓, lairs (found, held ground, raids and hoards, the card, the map, the standing raid notice) ✓, battles (the resolver and the generator, golden-tested against the web's output; the attack sheet; the playback) ✓, heroes — the rules (levels, ascension, skill ranks, wounds, slots, Legendary boons through the modifier stack) and heroes in a fight, golden-tested ✓, the gacha and the screens next; then store, world map |
| 8 | **On device** | the Graphy overlay in the editor and development builds ✓ (Ctrl+G its modes, Ctrl+H show/hide); atlases, profiling on iOS and Android, the dev panel |

## 3. Decisions taken

- **Platforms:** mobile only — Android and iOS.
- **Goal:** everything the web prototype has, playable, with its sounds,
  animations and feedback, built natively in Unity. New art is generated with
  ChatGPT through Chrome, following the documented prompts.

- **Balance:** read-only interfaces in Kingdom, implemented by
  ScriptableObjects in Game, edited with Odin; the web's JSON is imported once
  and then removed.
- **Saves:** one typed state, a save version, ordered migrations.
- **ProtoLab:** its UI framework, camera, sound, floating feedback,
  quick-info messages and lifecycle hook are modules here; its domain modules
  and save model are not taken.

- **Research gates are enforced.** A building, a level, one more of a
  building, a harvest source and a terrain wait for the technology that opens
  them. Until the quest chain pays the opening's Knowledge, a new kingdom
  earns it from the bar's drip or buys it.

## 4. Decisions still open

- The web repo after the remake: frozen, or kept alive in parallel.
