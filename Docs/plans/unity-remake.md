# The Unity remake

> **Scope:** how the web prototype becomes a native Unity game — the phases,
> what each delivers, and what has to be decided before each starts.
> **Status:** phase 0 done; phase 1 next.

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
