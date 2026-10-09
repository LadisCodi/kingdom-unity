# Kingdom (Unity) — working notes for Claude

An accessible 4X for mobile: a square-grid city-builder on a fog-shrouded
province that opens onto a shared hex world map. **Rebuilt from scratch in
Unity** from the design in `Docs/`. The web prototype
(`~/Proyectos/Codigames/kingdom`) is a reference to consult, never code to
port: nothing is translated line by line, and nothing has to give the web's
exact numbers.

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

Three layers of code under `Assets/Codigames/`. **The namespace is the path**
(`Assets/Codigames/Modules/Wallet` → `Codigames.Modules.Wallet`). Each layer
only knows the ones to its left:

```
Modules  ←  Kingdom  ←  Game
```

| Layer | Assembly | What it is | Unity? |
|---|---|---|---|
| `Modules/<Module>/` | `Codigames.Modules.<Module>` | **game-agnostic systems**: a wallet, timed queues, stores that fill, modifiers, fog of war, the logic of menus, sound, camera… | **never** (`noEngineReferences`) |
| `Kingdom/` | `Codigames.Kingdom` | **this game's rules**: what a Farm is, how a tap pays, when a lair raids. Composes modules and implements their ports. | **never** (`noEngineReferences`) |
| `Game/` | `Codigames.Game` | **the game in Unity**: views, presenters, `LifetimeScope`s, ScriptableObjects, adapters for the ports. | yes |

### Modules

**The code in Modules knows nothing of Unity — nor of any other package.**
Every module is `noEngineReferences`, references no other assembly but .NET,
and is tested with `dotnet test`. Whatever needs Unity or a package is
abstracted behind an interface the module declares, and implemented in
`Game`.

Example — sound: the module's `ISoundCatalog` returns an `ISound`, never an
`AudioClip`; its `SoundService` plays through an `ISoundPlayer` port. In
`Game`, a `UnitySound : ISound` holds the `AudioClip` and what it needs to be
played, the catalog is a ScriptableObject that returns `UnitySound`s, and the
player implements `ISoundPlayer` with Feel's `MMSoundManager`.

- **Independent.** A module depends on **no other module and no package** —
  only on `Modules/Core` (below). Copy its folder and `Core` to another
  project, implement its ports, and it works.
- **`Modules/Core` is the base, not a peer.** It holds what every module may
  share and nothing with behaviour of a system: our own `Vector2` and
  `Vector3` (public fields, so they serialise exactly as written),
  `IIdentifiable`, `ICatalog<T>` / `Catalog<T>` (definitions by id, in
  authored order) and `IRegistry<T>` / `Registry<T>` (live instances by id,
  announcing what comes and goes). The hierarchy is `Core → Module`; never
  `Module → Module`.
- **Ports for everything outside.** What a module needs from the world is an
  interface it declares (`ISoundPlayer`, `IAppLifecycleNotifier`); `Game`
  implements it and registers it in DI. The module never asks who.
- **Engine types never cross into a module.** No `GameObject`,
  `MonoBehaviour`, `Transform`, `Vector3`, `AudioClip`, `Sprite`, no tween,
  no Feel player: a module speaks in its own interfaces and in .NET types
  (`Core`'s `Vector2`/`Vector3` for positions, `Task` for what takes time).
  `Game` converts at the edge.
- **No DI framework, no attributes from packages.** Dependencies arrive in
  the constructor; registration and `[Inject]` belong to `Game`.
- **Behaviour lives in the module, the engine in `Game`.** A module holds the
  rules (a menu stack and its groups, a camera's inertia and bounds, a pool);
  `Game` holds the MonoBehaviours, prefabs, ScriptableObjects and package
  calls that make them real. Something that merely *starts* in Unity — an app
  callback, a frame tick, a touch — reaches the module through a hook in
  `Game` that calls its methods (`AppLifecycle` ← `Game/App/AppLifecycleHook`).
- **Generic, not Kingdom-shaped.** A module knows "a currency", "a cell", "a
  job in a queue" — never "Gold", "the Farm" or "Housing". Where it needs a
  game's type it is generic over it (`FogOfWar<TCell>`) or asks a port.
- **Inside**, ProtoLab's shape: `Domain/` (contracts, value objects, ports),
  `Core/` (entities, abstract bases holding shared logic), `Services/`. These
  role folders organise; they do not add to the namespace.
- **Tests** in `<Module>/Tests/`, an assembly of their own.

### Kingdom

- The rules of this game, **pure C#**: no `UnityEngine`, no clock, no files,
  no network. Everything the game *is*; nothing it *looks like*.
- **Composes modules** and implements their ports with Kingdom's rules (fog
  adjacency is 4-way; a cell's reveal price comes from its ring).
- **Asks the outside through ports** of its own: the clock, where the save
  lives, the balance (below).
- Organised by domain (`Kingdom/City/`, `Kingdom/Harvest/`, `Kingdom/Fog/` …).

### Game

- The **composition root**: the `LifetimeScope`s register modules, Kingdom
  and the adapters that implement every port.
- Views, presenters, use cases, ScriptableObjects, editor tools.
- Nothing depends on `Game`.
- Organised by domain (`Game/City/`, `Game/UI/` …); inside a domain:
  `UseCases/` (every player action goes through one: it calls Kingdom, then
  drives UI and feedback), `Services/`, `View/` (a view never mutates state),
  `Editor/`.

### Balance: definitions are interfaces

- **Kingdom declares what it reads** as small, read-only interfaces. A
  definition is its identity plus **parts**, each its own responsibility —
  `IBuildingDefinition` exposes `Cost`, `Duration` and `Gates` — so a rule
  reads only the part it needs.
- A definition is `IIdentifiable`: a **stable `Id`**. State and saves
  reference ids, never assets. A definition may point to another by
  reference (the construction settings' Townhall is the building's asset).
- Each collection reaches Kingdom as an `ICatalog<T>` (`Core`); global
  numbers as a settings interface (`IConstructionSettings`). Registered in
  `ProjectLifetimeScope`.
- **What a legal definition is** lives in Kingdom (`BuildingRules`,
  `ConstructionRules`): pure, tested with small definitions built in the test
  (`Kingdom/Tests/Builders`), never with the real balance.
- **Game implements them with ScriptableObjects** in `Assets/Data/`, one asset
  per entry (`Assets/Data/<Collection>/<Id>.asset`) and one per collection
  (`Assets/Data/<Collection>.asset`, its entries in authored order):
  - `DefinitionAsset` — an entry: its `Id` (the asset name by default) and its
    `Problems()`, which are Kingdom's rules;
  - `DefinitionCollection<TDefinition, TAsset>` — a collection, which **is**
    the `ICatalog<TDefinition>`; a new collection derives from it and adds
    nothing but its title;
  - `DataSettings` — a group of global numbers.
  The parts are `[Serializable]` classes inside the asset (`BuildingCostData`
  …). Odin attributes go on the assets' own fields; what only the player sees
  (names, prose, the build tab) sits on the asset beside the rules, never on
  Kingdom's interfaces.
- **The *Kingdom › Data* window** (Odin) lists every collection with its
  entries and every settings group, creates entries, and shows each item's
  problems as it is edited. **`DataValidator`** gathers every problem —
  entries, settings, rules across collections — for the window, *Validate
  all* and the `Balance_ShouldHaveNoProblems` test.
- **The web prototype's balance** (`Tools/WebData`, its JSON and schemas) is
  imported once per collection, when its system is rebuilt: *Kingdom ›
  Import web prototype data* (`Game/Editor/WebImport`). After that the assets
  are the source of truth. The importer's classes are generated from the web
  schemas (`dotnet run --project Tools/Codegen`).

### SOLID, always

Every change is checked against these. When something does not fit, stop and
ask rather than bend the structure.

- **Single responsibility.** One reason to change per class. Entities hold
  data and simple self-mutators; logic that relates several things lives in a
  service; a service does one job. A class that needs "and" to describe it is
  two classes.
- **Open/closed.** The game grows by adding data, new implementations of a
  port and new modules — not by editing a module for Kingdom's sake. A
  `switch` over kinds that grows with content is a missing abstraction.
- **Liskov.** Any implementation of a port or definition can stand in for
  another: no implementation throws "not supported", no caller checks the
  concrete type.
- **Interface segregation.** Interfaces are small and named for one need.
  A caller depends only on what it uses; a wide interface is split by
  responsibility.
- **Dependency inversion.** Depend on abstractions owned by the layer that
  uses them: a module's ports are declared in the module, Kingdom's in
  Kingdom; Game implements them. Dependencies arrive by constructor injection.

Two corollaries:
- **An interface where there is a contract** — a port, a definition, a
  service with (or that will have) more than one implementation. Not for a
  class that is just an internal detail.
- **No static mutable state, no singletons.** Static is only for pure
  functions and constants. Play mode enters with **domain reload off**
  (Project Settings › Editor › Enter Play Mode Options), so a static that
  changes would carry over from one play session to the next. A plugin's
  statics that do not reset themselves are emptied in a
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` hook
  (`Game/Map/LeanTouchReset.cs`).

### Project layout

Code under `Assets/Codigames/`; content outside it, by type.

```
Assets/
├─ Codigames/
│  ├─ Modules/<Module>/    Codigames.Modules.<Module> (+ Tests/)
│  ├─ Kingdom/             Codigames.Kingdom (+ Tests/)
│  └─ Game/                Codigames.Game (+ Editor/, Tests/)
├─ Data/                   balance: one ScriptableObject per entry, one per collection
├─ Art/                    final sprites only: Buildings/ Terrain/ Characters/ UI/ …
├─ Audio/  VFX/  Prefabs/  Scenes/
├─ Catalogs/               presentation ScriptableObjects: id → sprite / prefab / sound
├─ Settings/               URP, input actions, import presets, VContainer
└─ Plugins/                Asset Store packages (never edited)
Tools/                     outside Unity: PureTests, Codegen, WebData (the web's balance, to import)
```

- **No `Resources/` folder.** Prefabs, sprites and sounds reach code through
  **catalogs** registered in a `LifetimeScope`, keyed by ids. The one
  exception is a package that loads its own settings from there
  (`Assets/Resources/DOTweenSettings.asset`, `Assets/TextMesh Pro/Resources/TMP Settings.asset`).
- **File names come from ids** (`Farm_l1.png`, `Farm_l3.png` — a level draws
  the highest `_l<n>` at or below it), so an editor script fills catalogs and
  a test catches a missing sprite.
- **Import settings are per folder**, through Presets filtered by path.
- **Source art stays out of `Assets/`**; only the final, cut sprite comes in.
- **Two scenes**: `Boot` (splash, loading) and `Game` (the province and the
  world map, two views of one scene).
- **Third-party code is never edited.**

### The province map

- **Painted with Tilemaps**, in `Prefabs/Map/Province.prefab` (placed in the
  `Game` scene): an isometric `Grid` (cell 1 × 0.5), a `Terrain` layer and a
  `Features` layer of `VariantTile`s — a tile stands for an id ("Grassland",
  "Trees") and draws one of its variants by a hash of the cell, as the web did.
  Edit it with the Tile Palette.
- **Coordinates**: Kingdom works in the province's cells (the design's: x
  grows east-south, y west-south on screen); `ProvinceCoordinates` converts to
  Unity's isometric cells. `ProvinceMap` reads the tilemaps for Kingdom
  (`IProvinceMap`).
- **Depth**: the 2D renderer sorts by screen height (custom axis 0, 1, 0);
  standing sprites have their pivot at the bottom centre and are as wide as
  their footprint's diamond.
- **The camera** matches the web's: about three cells across at the start,
  bounded by the painted province.
- **Art from the web**: terrain textures are baked to diamonds by
  `Tools/Art/bake-terrain.sh`; features and buildings are copied as they are.
  `Game/Editor/Art/ArtImportRules` sets each folder's import settings. The
  web's map came in through *Kingdom › Import web prototype map*.

### How the game starts

- `Settings/VContainerSettings` (preloaded) names the **root scope**:
  `Prefabs/App/ProjectLifetimeScope.prefab` — clock, localization, number
  format, sound and the loading screen; it lives as long as the app.
- `Boot` (scene 0): `BootLifetimeScope` → `BootFlow` covers the screen and
  loads `Game`.
- `Game`: `GameLifetimeScope` (the province, UI root, menus, camera,
  feedback) → `GameStartupFlow` readies the game and lifts the loading
  screen. `KingdomInstaller` registers Kingdom there: one `KingdomState`
  (new, for now), the services that change it, and the `Timeline` every timed
  system is on. `KingdomTicker` is **the one tick driver**: every frame it
  advances the timeline to `IClock.NowMs`.
- The city is drawn by `CityView` (a `DistrictView` per district: its tier's
  art on its plot, faint with the web's wood-and-glass bar while it is built)
  and `GroundView` (the features layer kept to the ground's state).
- Fingers on the map go through `MapGestures`: a tap names a province cell,
  and an `IMapDragHandler` (placement's ghost) may claim a finger before the
  camera pans with it. A finger that starts over the UI is never the map's.
- Crews (`Kingdom/Crews/Workforce`) run their own steps inside the advance,
  each at its absolute moment (no timeline boundary per step); `CrewsView`
  draws them from `CharacterCatalog` (the web's frames in `Art/Characters`,
  planted by the feet listed in `characters.json`).
- The fog (`Kingdom/Fog/FogOfWar`) is drawn by `FogView` on two tilemaps
  under the province's grid: the bank's floor and the veil between terrain
  and features, the clouds sorted with the features. `GroundView` draws
  nothing under the bank; placement and harvest ask `IRevealedGround`.
  `MapTaps` says what a tap means while no menu is open: on the fog, a share
  of its price (`FogInput`); on a building, a
  collect when its store is ready (`CollectInput`, free) and its card
  (`DistrictCardMenu`) otherwise; on the ground, a harvest (`HarvestInput`).
- Play in the editor always starts from `Boot` (`Game/Editor/PlayFromBoot.cs`).

## Five invariants. Breaking one is a bug even if the tests pass.

**1. One-call offline replay equals stepped ticking.** Advancing to `now`
walks to the *earliest next boundary* and applies discrete work exactly at
it; boundaries are in **absolute time**, never relative to a tick. A new
scheduled or expiring thing registers its next boundary and what happens at
it — nothing else. Tests assert one big advance equals many small ones.

**2. There is no offline cap.** An absence is replayed in full. Production is
bounded by ceilings of its own — each building's store, the Mana pool, the
Knowledge bar, the queues. **Anything time-based that produces needs a
ceiling of its own**; say which it is in the doc.

**3. `now` is always passed in.** Kingdom never reads a clock. Game reads time
only through the injected `IClock`, and exactly **one tick driver** asks it
and passes `now` down. `Time.deltaTime` is for presentation only — camera,
tweens, particles — never for anything the game remembers.

**4. Randomness is counter/hash, not a stream.** `Rand(seed, ...parts)` where
`parts` identify **the event**, never the moment of the query, so replay and
live play roll the same. Never `System.Random` or `UnityEngine.Random` in
Kingdom.

**5. Every number is data.** Balance lives in `Assets/Data`, never in code. A
new building, unit, hero, quest or technology is a data entry, not a class.

## Saves

- **One typed state**, owned by Kingdom, serialised whole. No
  `Dictionary<string, object>` blobs, no untyped per-system participants.
- **A save version**; **migrations are ordered, gapless and append-only**,
  one per version that renames, reshapes or changes meaning. An additive
  change (a new optional field with a default) needs only the bump. A save
  from a newer build is refused, never downgraded.
- Where the save is written is a port; Game writes it on pause, on focus
  loss, on quit and after every command.
- The machinery is `Modules/Saves` (`SaveSlot`: storage and codec ports,
  migrations on the raw document, a newer save refused and never written
  over, an unreadable one set aside). Kingdom's is `Game/Saves`:
  `KingdomSaves.VERSION` and its `MIGRATIONS`, JSON through
  `KingdomSaveCodec`, a file in the persistent folder (`FileSaveStorage`).
  `KingdomLoader` starts the saved kingdom, else a new one; `KingdomSaver`
  writes a few seconds after a change and at once on `Suspending`.
  *Kingdom › Save › Delete save* starts the editor over.

## Code conventions

- **Naming**: `PascalCase` types, methods and properties; `IPascalCase`
  interfaces; `_camelCase` private fields; `UPPER_CASE` constants.
- **Encapsulation**: the public surface is **properties** backed by
  `_`-prefixed private fields; put validation or change notification in the
  setter. Serialised references are `[SerializeField] private` with a
  get-only property.
- **DI with VContainer**, registered in `Game`'s `LifetimeScope`s. Plain C#
  classes take their dependencies in the **constructor**; `MonoBehaviour`s,
  which cannot, use `[Inject]` on a method. Don't `new` up services. Entry
  points are `IStartable` / `ITickable` (`RegisterEntryPoint<T>()`).
- **Localization and number formatting are injected services**
  (`Localizer`, `NumberFormat`), never statics.
- **Async**: **UniTask** (`async UniTask`), never coroutines.
- **Tweens**: **DOTween** for motion computed in code; kill tweens with their
  owner.
- **Lifecycle**: base `MonoBehaviour`s use the Template Method pattern —
  override the hooks (`PreShowInternal`, …), never the orchestrating method.
- **Comments**: **no XML doc comments** unless strictly necessary. Prefer
  self-explanatory code; a plain `//` explains *why*, never *what*.
- **Small files, one type per file**, file name = type name.
- **References are injected or serialised, never searched for.** No
  `GameObject.Find`, `FindWithTag`, `FindObjectOfType` /
  `FindFirstObjectByType`, and no `GetComponent` in per-frame code.
- **No allocation in per-frame code** (`Update`, `LateUpdate`): no LINQ, no
  closures, no string building.
- **Determinism in Kingdom**: no iteration order that leaks into results
  without being defined; parse and format with
  `CultureInfo.InvariantCulture`.

## Feedback

**Code decides WHEN, the editor decides HOW.** Every moment the player should
feel — a collect, a build finishing, a level-up, a price you can't pay — is
a named Feel `MMF_Player` serialised on its view or prefab; code only calls
`PlayFeedbacks()`. No duration, curve or intensity of a feedback is written in
code.

- Feel lives only in `Game`, in views; the Feedback module only declares
  what a feedback is. A use case applies the command to the
  sim, then tells the view, which plays its feedback.
- **Feel** for authored feedback; **DOTween** for motion computed in code
  (menus opening, a bar following a value, a list reordering); **particles**
  are played from a feedback, not by loose components.
- **One sound channel**: Feel's `MMSoundManager` (music, SFX and ambience
  tracks); `SoundService` is a thin layer over it, so the settings' volumes
  reach everything.
- **The sounds are the web's**: `Assets/Audio/{Sfx,Music,Ambience,Voice}`
  (import settings by folder, `AudioImportRules`), each a `SoundCatalog`
  entry by the web's name (`SoundIds`) with its takes, level, pitch wobble
  and base pitch as the web mixes them (`Assets/Audio/sounds.json`, from its
  `sfx.ts`). `MusicDirector` plays the town's playlist (`Playlist`, in the
  audio module), `AmbienceDirector` the bed for the ground under the camera,
  `SoundCues` what the kingdom does by itself; a menu's buttons click.
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
  `Assets/VFX/`, restyle it to the art direction, never edit the
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

uGUI, **MVP**. The UI module (`Codigames.Modules.UI`) holds the logic — the
menu stack and its groups (`UIManager`), presenters (`AbstractMenuPresenter`,
`AbstractDataMenuPresenter`) and the contracts `IMenuView`,
`IMenuViewFactory`, `IMenuGroups`. `Game/UI` holds the Unity half: `Menu`
(the `MonoBehaviour` every menu view derives from), widgets, `MenuFactory`
(views from `MenuCatalog`'s prefabs), `UIRoot`, the safe area, button
feedback and the back button. A menu is three files in `Game/UI/`, each in its
own sub-folder:

- **Data** — `Game/UI/Data/{X}Data.cs`: an immutable DTO (get-only
  properties set in the constructor) the presenter pushes to the view. Only
  for data-driven menus.
- **Menu (view)** — `Game/UI/Menus/{X}Menu.cs`: derives `Menu`. **View
  only**: serialised references, setters and C# events for what the player
  does, no logic. Override lifecycle hooks, never `Show`/`Hide`.
- **Presenter** — `Game/UI/Presenters/{X}MenuPresenter.cs`: derives
  `AbstractMenuPresenter<{X}Menu>` (or `AbstractDataMenuPresenter`). Holds
  the logic: services in its constructor (with the `IMenuViewFactory` it
  passes to the base), state pushed in `BindInternal`, view events wired in
  `SubscribeToViewEventsInternal`.

The prefab's file name **equals the class name** (`Assets/Prefabs/UI/`, listed
in `MenuCatalog`); register the presenter `As<IMenuPresenter>()` in
`GameLifetimeScope` and show the menu through the `UIManager`
(`await uiManager.ShowMenu<XMenu>()`). A **persistent** menu (the header) is
not closable and is shown by `GameStartupFlow`. Pieces a menu repeats live in
`Game/UI/Hud` or `Game/UI/Widgets`.

Layout is in the web's **reference pixels**: the canvas is 1125×2436 with
*Expand*, so a size in the web's CSS (`calc(var(--rpx) * N)`) is `N` here. A
three-sliced piece keeps its ends through its import border
(`ArtImportRules`) and `pixelsPerUnitMultiplier` = source end ÷ drawn end. A
bar that bleeds under the notch or the home bar uses `SafeAreaBleed`. Text is
TextMeshPro on the web's two families (`Assets/Art/Fonts`): **Nunito** for
everything (Regular 400, SemiBold 600, Bold 700, ExtraBold 800 for a number
that leads its row) and **Alegreya Black** for titles.

**A label names a role, never a size.** The roles are TextMeshPro styles in
`Assets/Settings/UI/Text Styles.asset` (TMP's default sheet): `Title`,
`Heading`, `Body`, `Strong`, `Lead`, `Desc`, `Helper`, `Price`, `Button`… —
the web's `--text-*` and `--weight-*` tokens in rpx. Set it with the label's
*Text Style*; retune a role in the sheet and every label follows. A role that
sets a weight (`<font-weight=…>`) sits on **Nunito Regular**: the fonts'
weight tables swap the face, and Regular is the only base that can go down
to 400. A label on a material of its own (a slab button, a plank, a title
band) keeps its face and takes a size-only role. Colour stays out of the
sheet. `TextStyleTests` refuses a menu label with no role. A price over a
button is `Cost.prefab` (the header coin's 76 rpx); a price in a list row is
`CostChip.prefab`.

Rules the player sees:

- **The UI is made of materials** (`Docs/art/ui-menus-redesign.md`): wood,
  parchment, rope, cloth, wax, brass — lit from above, never flat fills or
  plastic gloss. Ask "what is this made of?" before drawing any new UI.
- **Every number the player reads goes through `NumberFormat`**, in the
  viewer's locale. Never `ToString()` a count the player reads.
- **Every text the player reads goes through `Localizer`** (English source,
  Spanish translation). No literal strings in views; a label written in a
  prefab carries `LocalizedText`. The Spanish lives in `Assets/Localization/es`
  (`LocalizationCatalog`): `web-ui.json` and `web-data.json` converted from
  the web (its UI lines, and its data overlays as English → Spanish), and
  `unity.json` for every line this build adds — `LocalizationTests` refuses a
  `Tr("…")` with no Spanish. Spanish is Spain's, *tú*. The language is the
  device's, or `PlayerPrefs` `kingdom.language` (`en-US` / `es-ES`).
- **No emoji glyphs** as icons, anywhere.
- **Countdowns derive from a timestamp**, never a decremented counter.
- **A calculated price or reward is rounded to three significant figures**;
  an authored number never is.

## What comes from ProtoLab

`~/Proyectos/Codigames/ProtoLab` is the team's Unity base. Its pieces were
**copied and split**: the behaviour into engine-free modules, the Unity half
into `Game`.

| ProtoLab piece | Module (pure) | `Game` (Unity) |
|---|---|---|
| menu framework, widgets, safe area, button feedback | `UI`: `UIManager`, presenters, contracts | `Game/UI`: `Menu`, widgets, `MenuFactory`, `MenuCatalog`, `UIRoot`, `SafeAreaFitter`, `ButtonPressScaleFeedback`, `MenuBackInputHandler` |
| `CameraManager` | `Cameras`: `CameraController` (drag, inertia, elastic bounds, zoom, glide) | `Game/Cameras`: `CinemachineCameraRig`, `CameraInputHook` (LeanTouch), `CameraSettings`, `CameraTicker` |
| `SoundService` | `Audio`: `SoundService`, `ISound`, `ISoundCatalog`, `ISoundPlayer` | `Game/Audio`: `UnitySound`, `SoundCatalog`, `FeelSoundPlayer` |
| world feedback, quick-info messages | `Feedback`: the contracts | `Game/Feedback`: the Feel views, pools and catalogs |
| app lifecycle hook | `Lifecycle`: `AppLifecycle` | `Game/App/AppLifecycleHook` |
| `AbstractCatalog`, `AbstractRegistry` | `Core`: `Catalog<T>`, `Registry<T>` | the ScriptableObjects that fill them |

## Driving the editor

The editor is usually open on the project: never run Unity in batch mode
against it. Drive it with the Unity CLI (`unity status`, then
`unity command <name> --project-path <project>`):

- `recompile` + `recompile_status`, then `console_status` / `console` for
  errors; `run_tests --mode EditMode` runs the suite in the open editor. Let
  the test run finish its cleanup (a few seconds) before `editor_play`, or its
  own errors land in the console.
- `editor_play` / `editor_stop`, `capture_game_view` (its `save_path` is
  relative to `Assets/` — write it under `Temp/` outside, or delete it after).
- Scenes, prefabs and assets are made by editor code, never by writing YAML:
  `eval` runs a method body (no `using`s — fully qualified names); anything
  longer is a temporary class in `Game/Editor/`, compiled, run, then deleted.
  After `NewScene`, reload assets with `AssetDatabase.LoadAssetAtPath` before
  assigning them — references held across it are lost.
- **VContainer rewrites a new file named `*LifetimeScope.cs`** with its empty
  template when Unity imports it. Create the file, let it import, then write
  its content.

## Tests and the gate

- NUnit. Each module has `Tests/`; Kingdom has `Kingdom/Tests/`; Game has
  `Game/Tests/` (EditMode, with the editor: the balance test lives there). Class `{Component}Tests`, methods `Method_Should{Behavior}`.
- **Pure code is tested without Unity**: `dotnet test Tools/PureTests` builds
  every module and Kingdom with their tests (C# 9, warnings are errors) in
  seconds.
- Every rule in Kingdom has tests; every invariant has a test that fails when
  it breaks.
- **The gate**: `dotnet test Tools/PureTests` green; Unity compiles with no
  errors or warnings; EditMode tests green; Play from `Boot` reaches `Game`
  with an empty console. GitHub runs no tests — the gate is local, before a
  branch is pushed for a PR. Red means no PR.

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
  network) from `Kingdom` or a pure module.
- Don't make a module depend on Unity, a package, another module or
  Kingdom.
- Don't translate the web prototype's code. Read it to understand a rule;
  build the rule the way this architecture asks.
- Don't hand-edit scenes, prefabs or `.asset` YAML when the editor (or an
  editor script) can do it.
- Don't push to `main` or open a release unless asked.
