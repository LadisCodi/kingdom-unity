# The Unity remake

> **Scope:** how the web prototype becomes a native Unity game — the phases,
> what each delivers, and what has to be decided before each starts.
> **Status:** phase 0 done; phase 1 in progress (1a done).

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

## 3. Phase 0 — what is in

- Repo, Git Flow, LFS, working rules, the clean design docs.
- Packages: VContainer, UniTask, Newtonsoft, Cinemachine, UIParticle,
  SoftMask, UI Effect, Graphy; Odin, DOTween, LeanTouch, Feel, All In 1
  Sprite Shader and VFX Toolkit (demos removed).
- Assemblies `Kingdom.Sim` (engine-free), `Kingdom.Game`, `Kingdom.Editor`,
  `Kingdom.Tests`.
- `Rand`, bit-identical to the web prototype (golden tests).
- From ProtoLab, adapted: the menu framework (`UIManager`, `Menu`,
  presenters, `MenuFactory` over a `MenuCatalog`), widgets and pools, the
  safe-area fitter, button press feedback, the camera, world feedback and
  quick-info messages (Feel), the sound service (over `MMSoundManager`).
- `IClock`, `Localization` (English as the key, Spanish overlays),
  `NumberFormat` (locale-aware, the web's rules), the app lifecycle signal.
- Scenes `Boot` and `Game`, the root scope prefab with the loading screen.
- **Deferred to phase 3:** the nav bar. ProtoLab's bar morphs into a close
  button and tabs; the design's bar steps aside while a menu is open
  (`art/ui-menus-redesign.md` §5.4, §6.5), so it is built to the design.

## 4. Decisions taken

- **Game data:** the web prototype's JSON, as-is, is the source of truth;
  edited inside Unity with Odin editor windows, validated by the same rules
  as the tests. ScriptableObjects only for presentation config.
- **Saves:** one typed state with a save version and ordered migrations.
- **ProtoLab:** its UI framework, camera, startup flow, audio, save hook,
  floating feedback and number formatting are copied and adapted; its
  domain modules and save model are not (`CLAUDE.md`, *What comes from
  ProtoLab*).

## 5. Decisions still open

- Target platforms: mobile only, or mobile + WebGL for testers.
- The web repo after the remake: frozen, or kept alive in parallel.

## 6. Phase 1 — the sim in C#

Four deliveries, each proven against the web prototype before the next:

| # | Delivers | Proven by |
|---|---|---|
| 1a | the data files as-is; generated classes; `GameData` (the derivations of `definitions.ts`); `GameState` types; `Rand`, `JsMath`, `RoundPrice` | round-trip of every file; the definitions golden |
| 1b | new game; `Advance` with its boundaries; the economy — wallet, stores, rent, Mana, Knowledge, modifiers, construction, districts, fog, harvest, workers, research, techs' effects | each module's web tests, ported; scripted runs vs goldens |
| 1c | army, training, wounded, lairs and raids, heroes, battle | the same |
| 1d | quests, events and timeline, notices, store and offers, Bag, relics, the client half of the world | the same |

**Porting rules.**

- One web module → one C# static class of pure functions, same name
  (`mana.ts` → `Mana`), in `Sim/<Domain>/`. Its functions take
  `(GameState state, GameData data, …, double now)`; no class holds state.
- Port literally: same order of operations, same rounding, same iteration
  order. Comments carry the web's *why* where it is not obvious.
- `JsMath.Round` for `Math.round`; `Math.Floor`/`Ceiling` are the same in both.
- Iteration order: a `Dictionary` enumerates in insertion order, like a JS
  object — except that **JS puts integer-like keys first, ascending**. Where
  the web keys an object by a number, sort.
- Numbers are `double`; ids are strings; a TS union is a set of string
  constants.
- A module's web tests (`tests/<module>.test.ts`) are ported to NUnit in
  `Tests/Sim/<Domain>/` with the module.
- State-level parity: `Tools/Parity` runs scripted command sequences through
  the web sim and writes the state after each step; the C# replay must write
  the same JSON.

