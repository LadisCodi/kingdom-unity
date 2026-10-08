# The Unity remake

> **Scope:** how the web prototype becomes a native Unity game — the phases,
> what each delivers, and what has to be decided before each starts.
> **Status:** phase 0 in progress.

## 1. Principles

- **The design carries over; the code does not.** `Docs/` is the design.
  The web prototype's simulation rules are ported faithfully to C#; its
  renderer and UI are rebuilt the Unity way.
- **The sim is proven equal, not believed equal.** Golden runs exported from
  the web sim (scripted commands → state as JSON) are replayed by the C# sim
  in NUnit, and must match.
- **Fresh saves.** No web save is migrated.
- **The world server stays.** The authoritative world server keeps running
  as it is; the Unity client talks to it over HTTP.

## 2. Phases

| # | Phase | Delivers |
|---|---|---|
| 0 | **Foundation** | repo, Git Flow, LFS, working rules, clean docs; packages and plugins; assemblies `Kingdom.Sim` / `Kingdom.Game` / `Kingdom.Editor` / `Kingdom.Tests`; DI scopes, UI framework and save base |
| 1 | **Sim core + parity** | the C# sim: state, data loading, `Advance`, `Rand`, economy, harvest, construction, research…; the golden-run exporter in the web repo and the parity tests |
| 2 | **The city** | isometric province with Sprite Renderers, fog, camera and touch, buildings and crews, harvest and collect feedback (particles, DOTween) |
| 3 | **HUD and core menus** | header and purse, nav bar, building card, build menu, tech tree, Bag, heroes, store |
| 4 | **World map and server** | the hex world map and the world-server client |
| 5 | **The rest** | tutorials and dialogue, gacha, offers, friends, notices, Spanish, analytics |
| 6 | **On device** | sprite atlases, profiling on iOS and Android, a dev panel |

## 3. Decisions taken

- **Game data:** the web prototype's JSON, as-is, is the source of truth;
  edited inside Unity with Odin editor windows, validated by the same rules
  as the tests. ScriptableObjects only for presentation config.
- **Saves:** one typed state with a save version and ordered migrations.
- **ProtoLab:** its UI framework, camera, startup flow, audio, save hook,
  floating feedback and number formatting are copied and adapted; its
  domain modules and save model are not (`CLAUDE.md`, *What comes from
  ProtoLab*).

## 4. Decisions still open

- Target platforms: mobile only, or mobile + WebGL for testers.
- The web repo after the remake: frozen, or kept alive in parallel.
