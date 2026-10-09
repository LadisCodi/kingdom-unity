# The Unity remake

> **Scope:** how Kingdom is rebuilt in Unity — the order of the work, what
> each step delivers, and what is still to decide.
> **Status:** foundation, architecture, balance and the city done; the economy
> under way (harvest by tap, the Mana pool, stores, collecting, villagers and
> rent built; crews next).

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
| 4 | **The economy** — under way | harvest by tap ✓, Mana ✓, stores and collecting ✓, villager training and rent ✓, harvest by crew ✓ |
| 5 | **The fog** — under way | reveal ✓, the Gold price by ring ✓, the Townhall's reach ✓; treasures, landmarks, sighting, ruins |
| 6 | **Research** | Knowledge, the tech tree and its gates |
| 7 | **The rest, in order** | army and lairs, heroes, quests and tutorials, store, world map |
| 8 | **On device** | atlases, profiling on iOS and Android, the dev panel |

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

- **Research gates wait for research.** Harvest sources, buildings and
  terrain are imported with their technology gates, which are enforced once
  the tech tree exists (step 6); until then everything on the ground can be
  tapped.

## 4. Decisions still open

- The web repo after the remake: frozen, or kept alive in parallel.
