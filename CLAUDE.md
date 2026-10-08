# Kingdom (Unity) — working notes for Claude

An accessible 4X for mobile: a square-grid city-builder on a fog-shrouded
province that opens onto a shared hex world map. A **native Unity remake** of
the web prototype (`~/Proyectos/Codigames/kingdom`), rebuilt from scratch with
Unity's own tools — Sprite Renderers, URP 2D, particles, uGUI — not a port of
its renderer or UI.

Unity **6000.3.19f1**, URP 2D, Input System. Packages: **VContainer** (DI),
**UniTask** (async), **Odin Inspector**, **DOTween**, **TextMeshPro**,
**Newtonsoft.Json**, **Feel** (feedbacks, with Nice Vibrations), **Cinemachine** (the camera), **LeanTouch** (touch
gestures), **UIParticle** (particles inside uGUI), **SoftMask** and **UI Effect** (soft
masks and effects on uGUI), **All In 1 Sprite Shader** and **All In 1 VFX
Toolkit** (sprite effects, particles), **Graphy** (on-device performance
monitor), **NUnit** (Unity Test
Framework).

**Read [`Docs/overview.md`](Docs/overview.md) before changing behaviour** — the
game in five minutes. Then:

| Where | What it holds |
|---|---|
| [`Docs/README.md`](Docs/README.md) | the index and the design intentions |
| `Docs/features/` | **the live source of truth for the design, one file per feature** |
| [`Docs/open-questions.md`](Docs/open-questions.md) | every decision still to make, with stable ids (`OQ-n`); the taken ones are in [`Docs/open-questions-closed.md`](Docs/open-questions-closed.md) |
| [`Docs/art/`](Docs/art/) | art direction and the UI material guide |
| [`Docs/plans/unity-remake.md`](Docs/plans/unity-remake.md) | the remake plan: phases, what is built, what is next |

`Docs/` is **design**: no implementation detail unless a decision turned on
it. Code-level contracts are in this file. A feature doc's **status** says
how far that design was built and played in the web prototype; what the
Unity build has is tracked only in the remake plan.

## Architecture

All game code lives under `Assets/Kingdom/`, in two layers. Keep the boundary
clean — it is what lets the simulation be tested without a scene and later
run on a server.

- **`Sim/`** (assembly `Kingdom.Sim`, `noEngineReferences: true`) — the
  simulation core. Plain C#: no `UnityEngine`, no VContainer, no clock, no
  I/O. State plus pure functions of `(state, …, now)`. Everything the game
  *is* lives here; everything the game *looks like* does not.
- **`Game/`** (assembly `Kingdom.Game`) — Unity: `MonoBehaviour` views, scene
  wiring, DI `LifetimeScope`s, use cases, presenters, persistence, audio,
  input. Depends on `Sim`, never the reverse.
- **`Editor/`** (assembly `Kingdom.Editor`) — editor-only tools.
- **`Tests/`** (assembly `Kingdom.Tests`, EditMode) — NUnit, mirroring the
  folders it tests.

Namespaces follow the layer and the domain — `Kingdom.Sim.Economy`,
`Kingdom.Game.City` — not every sub-folder.

### Project layout

Code is organised by domain; assets by type — art arrives by type, and
atlases and import settings are set per type.

```
Assets/
├─ Kingdom/
│  ├─ Sim/  Game/  Editor/  Tests/     the four assemblies (Tests/Parity/ holds the golden runs)
│  ├─ Data/                            the game data, JSON
│  │  ├─ Game/  Schema/                one file per collection
│  │  ├─ region-map.json  tech-tree.json
│  │  └─ Localization/es/
│  ├─ Art/                             final sprites only
│  │  ├─ Buildings/  Terrain/  Features/  Fog/
│  │  ├─ Characters/  Heroes/  Units/
│  │  ├─ UI/ (Materials/, Icons/, Currencies/)
│  │  ├─ World/
│  │  └─ Fonts/
│  ├─ Audio/ (Music/, Sfx/, Ambience/)
│  ├─ VFX/                             particles, their materials, shaders
│  ├─ Prefabs/ (UI/Menus/, UI/Widgets/, City/, World/)
│  ├─ Catalogs/                        presentation ScriptableObjects: data id → sprite / prefab / sound
│  ├─ Atlases/                         SpriteAtlas per group
│  ├─ Scenes/                          Boot, Game, Dev/
│  └─ Settings/                        URP, input actions, import presets
└─ Plugins/                            Asset Store packages (Odin, DOTween, LeanTouch)
Packages/                              UPM packages
```

- **No `Resources/` folder.** Prefabs, sprites and sounds reach code
  through **catalogs** registered in the `LifetimeScope`, keyed by the data's
  ids — typed, never looked up by a string path.
  The one exception is a third-party package that loads its own settings
  from there (`Assets/Resources/DOTweenSettings.asset`).
- **File names come from data ids** (`Farm_l1.png`, `Farm_l3.png` — a level
  draws the highest `_l<n>` at or below it), so an editor script fills the
  catalogs and a test catches a building with no sprite or a sprite nothing
  uses.
- **Import settings are per folder**, through Presets filtered by path
  (buildings pivot bottom-centre, UI no mipmaps and 9-sliced). Dropping a
  PNG in its folder needs no manual import tweaking.
- **Source art stays out of `Assets/`** — PSDs, generated art, mockups live
  in the art repo. Only the final, cut and normalised sprite comes in.
- **Two scenes**: `Boot` (splash, loading) and `Game` (the province and the
  world map, two views of one scene).
- **Third-party code is never edited.** Asset Store packages in `Plugins/`,
  everything else through UPM.

### Inside a domain

- **`Sim/<Domain>/`** — state types (plain, serialisable data), the rules
  that change them, and the interfaces (ports) for anything the outside
  world provides.
- **`Game/<Domain>/`**:
  - `UseCases/` — every player action goes through a use case: it calls the
    sim, then drives UI, feedback and presentation. **All use cases live in
    the Game layer.**
  - `Services/` — Game-side services and adapters that implement sim ports.
  - `Persistence/` — save participants.
  - `View/` — `MonoBehaviour`s that show state. A view never mutates state.

### Entities vs. services

- **State objects are data**: their own fields plus simple self-mutators.
  They take no services.
- **Logic that relates several pieces of state lives in services/rules.**
  The dependency points service → state, never the reverse.
- A **registry holds the active set**; callers pull from it and pass the
  pieces into the services.
- **Interfaces for contracts that have, or will have, more than one
  implementation** — sim ports, the world-server client, storage. Don't add
  an interface or an abstract base class for a type with one implementation.

## Six invariants. Breaking one is a bug even if the tests pass.

**1. One-call offline replay equals stepped ticking.** `Advance(state,
toTime)` walks to the *earliest next boundary* and applies discrete work
exactly at it; boundaries are in **absolute time**, never relative to a tick.
Any new scheduled or expiring thing is a candidate in the next-boundary search
plus a branch in the apply-due step — nothing else. The step cap is a
seatbelt, not a design limit: never register a source that fires more often
than the sim needs to observe it. Tests assert one big `Advance` equals many
small ones.

**2. There is no offline cap.** An absence is replayed in full by the one
`Advance`. Production is bounded by ceilings of its own — each building's
store, the Mana pool, the Knowledge bar, the queues. **Anything time-based
that produces needs a ceiling of its own**; say which it is in the doc.

**3. `now` is always passed in.** The sim never reads a clock: no
`DateTime.Now`, no `Time.time`, no `Stopwatch`. The Game layer reads time
only through one injected `IClock`, and exactly **one tick driver** asks it
and passes `now` down. A use case that needs the time takes it from the
clock, never from `DateTime.UtcNow`. Do not add a second driver.
`Time.deltaTime` is for presentation only — camera, tweens, particles —
never for anything the game remembers.

**4. Randomness is counter/hash, not a stream.** `Rand(seed, ...parts)` where
`parts` identify **the event**, never the moment of the query — a stream
would desync because `Advance` groups work differently in replay than live.
Integer arithmetic on `uint` in `unchecked` blocks so it is bit-identical
everywhere. Never `System.Random` or `UnityEngine.Random` in the sim.

**5. Every number is data.** Balance lives in data files, never in code;
code reads them. A new building, unit, hero, quest or technology is a data
entry, not a class. What a legal data file is lives in one place and is
checked by a test. See *Game data* below.

**6. The sim is deterministic and portable.** `double`, never `float`, in sim
state and rules; no `Dictionary` iteration order that leaks into results
(sort, or use ordered collections); nothing culture-dependent (parse and
format with `CultureInfo.InvariantCulture`).

## Game data

- **JSON is the source of truth**: one file per collection, plus the region
  map and the tech tree, in the same shape as the web prototype's data
  (so the parity runs and the world server read the very same files). The
  sim parses them with Newtonsoft into plain C# definitions; it never sees
  a `TextAsset` — the Game layer hands it the text.
- **Edited inside Unity** with Odin editor windows, which validate as you
  type with the same rules the test runs. Until a collection has its window,
  it is edited in the web prototype's data editor and copied over.
- **No ScriptableObjects for balance.** ScriptableObjects are for
  presentation config only: sprites, prefabs, colours, sounds, tween
  timings — what a thing *looks like*, keyed by the data's ids.

## Saves

- **One typed `GameState`** — the sim's state, serialised whole with
  Newtonsoft. No `Dictionary<string, object>` blobs, no per-system save
  participants writing untyped data.
- **`SaveVersion`** in the save; **migrations are ordered, gapless and
  append-only**, one per version that renames, reshapes or changes meaning.
  An additive change (a new optional field with a default) needs no
  migration, only the bump. A save from a newer build is refused, never
  downgraded.
- Saved on pause, on focus loss and on quit, and after every command.

## Code conventions

- **Naming**: `PascalCase` types, methods and properties; `IPascalCase`
  interfaces; `_camelCase` private fields; `UPPER_CASE` constants.
- **Encapsulation**: the public surface is **properties** backed by
  `_`-prefixed private fields; put validation or change notification in the
  setter. Serialised view references are `[SerializeField] private` with a
  get-only property.
- **DI with VContainer**: register in the relevant `LifetimeScope`. Plain C#
  classes take their dependencies in the **constructor**; `MonoBehaviour`s,
  which cannot, use `[Inject]` on a method or field. Don't `new` up services.
  Entry points are `IStartable` / `ITickable`, registered with
  `RegisterEntryPoint<T>()`.
- **Async**: **UniTask** (`async UniTask`), never coroutines.
- **Tweens**: **DOTween** for UI and feedback motion; kill tweens with their
  owner.
- **Lifecycle**: base `MonoBehaviour`s use the Template Method pattern —
  override the hooks (`PreShowInternal`, …), never the orchestrating method.
- **Comments**: **no XML doc comments** unless strictly necessary. Prefer
  self-explanatory code; a plain `//` explains *why*, never *what*.
- **Small files, one type per file**, file name = type name.
- **References are injected or serialised, never searched for.** No
  `GameObject.Find`, `FindWithTag`, `FindObjectOfType` /
  `FindFirstObjectByType`, and no `GetComponent` in per-frame code. Scene
  objects a service needs are registered in the `LifetimeScope`
  (`RegisterComponent`). No singletons, no static mutable state.
- **No allocation in per-frame code** (`Update`, `LateUpdate`, render
  loops): no LINQ, no closures, no string building.

## Feedback

**Code decides WHEN, the editor decides HOW.** Every moment the player should
feel — a collect, a build finishing, a level-up, a price you can't pay — is
a named Feel `MMF_Player` serialised on its view or prefab; code only calls
`PlayFeedbacks()`. No duration, curve or intensity of a feedback is written in
code.

- Feel lives only in `Game`, in views. A use case applies the command to the
  sim, then tells the view, which plays its feedback.
- **Feel** for authored feedback; **DOTween** for motion computed in code
  (menus opening, a bar following a value, a list reordering); **particles**
  are played from a feedback, not by loose components.
- **One sound channel**: Feel's `MMSoundManager` (music, SFX and ambience
  tracks); `SoundService` is a thin layer over it, so the settings' volumes
  reach everything.
- **Haptics** through Nice Vibrations, behind the settings' vibration toggle.
  `.haptic` clips cannot be imported on Linux (no editor plugin there) —
  use its presets, or import clips on macOS/Windows.
- **Camera shake** through Cinemachine Impulse.
- Anything played often (floating numbers on taps) is **pooled**; never one
  instantiate per tap.
- Feel's demos are not in the project; reimport them elsewhere to browse.

## Visual effects

- **Sprite effects** — outline, glow, greyscale, hit flash, dissolve, shine,
  wind sway — come from **All In 1 Sprite Shader** (its URP 2D variant, so
  2D lights apply). **Particles** use **All In 1 VFX Toolkit**'s shader;
  its effect prefabs are a starting library: copy one into
  `Assets/Kingdom/VFX/`, restyle it to the art direction, never edit the
  original.
- **Own Shader Graph shaders only for what is Kingdom's**: the fog of war,
  water and terrain motion, the tutorial cut-out.
- **Few shared materials, one per state** (normal, selected, locked…) —
  never a material per object; enable only the effects in use, since each
  is a shader variant.
- **UI effects** (greyscale, shine, transitions, soft edges) through **UI
  Effect** and **SoftMask** on uGUI; particles inside the UI through
  **UIParticle**.
- **Mobile budget**: overdraw is the cost — few particles, small textures,
  nothing full-screen that runs continuously. **Graphy** shows FPS and
  memory in dev builds; it is never in a release build.
- The packs' demo scenes are not in the project.

## UI

uGUI, **MVP**. A menu is three files in `Game/UI/`, each in its own
sub-folder (these sub-folders *do* carry into the namespace):

- **Data** — `Game/UI/Data/{X}Data.cs`: an immutable DTO (get-only
  properties set in the constructor) the presenter pushes to the view. Only
  for data-driven menus.
- **Menu (view)** — `Game/UI/Menus/{X}Menu.cs`: derives
  `Menu<{X}MenuPresenter>` (or `Menu<{X}MenuPresenter, {X}Data>`). **View
  only**: serialised references exposed as get-only properties, no logic.
  Override lifecycle hooks, never `Show`/`Hide`.
- **Presenter** — `Game/UI/Presenters/{X}MenuPresenter.cs`: derives
  `AbstractMenuPresenter<{X}Menu>`. Holds the logic: injected services,
  subscribe in `BindInternal`, unsubscribe in `UnbindInternal`.

The prefab's file name **equals the class name**; show a menu through the
`UIManager` (`await uiManager.ShowMenu<XMenu>()`).

Rules the player sees:

- **The UI is made of materials** (`Docs/art/ui-menus-redesign.md`): wood,
  parchment, rope, cloth, wax, brass — lit from above, never flat fills or
  plastic gloss. Ask "what is this made of?" before drawing any new UI.
- **Every number the player reads goes through one formatter**, in the
  viewer's locale. Never `ToString()` a count the player reads.
- **Every text the player reads is localised** (English source, Spanish
  translation). No literal strings in views.
- **No emoji glyphs** as icons, anywhere.
- **Countdowns derive from a timestamp**, never a decremented counter.
- **A calculated price or reward is rounded to three significant figures**;
  an authored number never is.

## What comes from ProtoLab

`~/Proyectos/Codigames/ProtoLab` is the team's Unity base. Its pieces are
**copied and adapted** into `Kingdom.Game` (namespaces renamed, the
multi-game `gameId` paths removed, tag lookups replaced by injection), never
referenced:

- UI: `UIManager` (menu stack, grouping, close-top-most), `Menu`,
  `AbstractMenuPresenter`, `MenuBackInputHandler`; `Widget`,
  `StateDrivenWidget`, `StateView` and their pools; `MainNavBar`,
  `SecondaryNavBar`, `SafeAreaFitter`; `ButtonPressScaleFeedback`.
- `CameraManager` (pan, zoom, inertia): LeanTouch gestures move a target a
  Cinemachine camera follows; zoom drives its orthographic size.
- The startup flow (`SplashStartupFlow`, `GameStartupFlow`) and the
  `LifetimeScope` skeleton.
- `SoundService`; the app-lifecycle save hook.
- `WorldFeedback` (floating numbers), `QuickInfoMessages`, `Prettifier`
  (made locale-aware: it becomes the one number formatter).

Not taken: its domain modules (Currencies, Generators, Production, Economy,
Stats, Requirements, Queues, Timer, Offline) — the sim, ported from the web
prototype, owns the domain — and its save model.

## Tests and the gate

- NUnit, EditMode, under `Assets/Kingdom/Tests/` mirroring the folders it
  tests. Class `{Component}Tests`, methods `Method_Should{Behavior}`.
- **The sim is tested without a scene.** Every rule in `Sim/` has tests;
  every invariant has a test that fails when it breaks.
- **The gate**: EditMode tests green and the project compiles for the
  target platform. GitHub runs no tests — the gate is local, before a branch
  is pushed for a PR. Red means no PR.

## Branching — Git Flow

`main` is what has shipped; `develop` is where work lands. Neither is
committed to directly — everything arrives by PR, merged with a merge commit.

| Branch | From | Merges into | For |
|---|---|---|---|
| `feature/<slug>` | `develop` | `develop` | new behaviour, art, docs, chores |
| `bugfix/<slug>` | `develop` | `develop` | a bug not yet on `main` |
| `release/<x.y.z>` | `develop` | `main`, then back into `develop` | shipping; tag `v<x.y.z>` on `main` |
| `hotfix/<slug>` | `main` | `main`, then back into `develop` | a bug that has shipped; bumps the patch |

- **Finishing a feature or bugfix is one motion**: commit, run the gate,
  push, open the PR into `develop`, merge it. No need to ask.
- **A release or hotfix to `main` is only on request.**
- **Stage by file**, never `git add -A` / `git add .`: the user works in the
  repo in parallel. Commit `.meta` files with their asset, always.
- **Binaries go through Git LFS** (`.gitattributes`). Never commit
  `Library/`, `Temp/`, `Logs/`, `UserSettings/` or IDE files.
- Commits use the work identity (`jose.ladislao@codigames.com`).

## Docs house style

Feature docs open with a `>` blockquote giving scope and **status**, use
numbered `##` sections referenced elsewhere as `§n`, carry a table of dials
"in the order to reach for them", and end with **deliberately not in this
design**. **Open questions do not live in the feature doc** — they live in
`Docs/open-questions.md`, and the feature names them by id (`OQ-n`). Docs are
written in **English**.

- Write the specification of HOW something works, not the design process
  for WHY it ended up that way.
- Write only the current design, not how it changed.
- As simple as possible; prefer bullet lists. Less is more.

When a doc and the code disagree, **the code is usually right and the doc is
stale.** Fix the doc in the same PR, and prefer a test over a paragraph for
any number that has been argued twice.

## Don't

- Don't reference `UnityEngine` (or anything with a clock, a file or a
  network) from `Sim/`.
- Don't port the web prototype's renderer or UI code. The sim's rules are
  ported faithfully; everything visual is rebuilt the Unity way.
- Don't hand-edit scenes, prefabs or `.asset` YAML when the editor (or an
  editor script) can do it.
- Don't push to `main` or open a release unless asked.
