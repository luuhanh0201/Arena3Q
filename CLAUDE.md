# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Arena3Q is a Unity **6000.6.6f1** project (2D URP template) with a playable PvE prototype: Attack and Defense stages, hero summoning, squads, commands, camera and HUD with placeholder art. The startup scene is `Assets/Arena3Q/Scenes/Battle.unity`; the template's `Assets/Welcome/` and `Assets/Scenes/SampleScene.unity` are untouched leftovers.

The gameplay specification lives in `docs/GameplaySpecification.md` (tracked in git as lowercase `docs/`; on Windows `Docs/` is the same folder, so never create a second copy with different casing) and is the source of truth for gameplay. Read it before designing or writing gameplay code. Its markers: ✅ settled (implement exactly), 🟡 provisional default (must be a config option), ⏳ deferred (not in the prototype). If it conflicts with the configuration below (e.g. 3D, legacy input), flag the conflict instead of silently picking one.

## Key configuration

- **Rendering:** URP with the 2D Renderer (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`). Use 2D lights, `SpriteRenderer`, and URP 2D-compatible shaders/materials; built-in pipeline shaders will render pink.
- **Input:** `activeInputHandler: 1` means the **new Input System only**. The legacy `UnityEngine.Input` API will throw at runtime; use `UnityEngine.InputSystem`. A default action asset exists at `Assets/Settings/InputSystem_Actions.inputactions` (`Player` map: Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint; plus a `UI` map), but the battle does not use it: `BattleInputController` reads `Mouse.current` and `EnhancedTouch` directly, and the HUD's `InputSystemUIInputModule` uses its built-in default actions.
- **Target platforms:** Android, iOS and PC, landscape orientation (per the spec). Only an `Android Dev` build profile exists so far, in `Assets/Settings/Build Profiles/`; Android uses the IL2CPP scripting backend. Input must work with both touch and mouse.
- **Packages** (`Packages/manifest.json`): 2D Animation, Aseprite and PSD importers, Tilemap + Tilemap Extras, SpriteShape, Timeline, uGUI, Visual Scripting, Test Framework.

Bugs fixed so far, with root causes and how to avoid repeating them, are logged in `docs/NhatKySuaLoi.md`. Read it before changing gameplay code or running batch builds/tests, and add an entry for every bug you fix.

## Repository layout

```
docs/GameplaySpecification.md   gameplay spec (source of truth)
docs/NhatKySuaLoi.md            bug-fix log (root cause + prevention per fix)
docs/MapSpecification.md        map dimensions, layout, forbidden-zone and art rules (proposal)
Assets/
  Arena3Q/                      everything this project owns
    Scripts/                    Core, Data, Gameplay (Battle, Camera, Heroes, Input, Stages, Troops, Units, Visuals), UI, Editor
    Tests/                      EditMode (Core, Data), PlayMode
    Content/                    generated placeholder assets: Factions, Heroes, Troops, Stages, TestLoadout.asset
    Scenes/Battle.unity         startup scene (first in Build Settings)
  Settings/                     URP / 2D renderer, input actions, scene template, Build Profiles/ (Android Dev)
  Scenes/SampleScene.unity      template leftover, still in Build Settings after Battle
  Welcome/                      template Unity Learn welcome page (Unity.2D.Welcome asmdef)
Packages/, ProjectSettings/     Unity-managed
```

## Code layout

All game code is under `Assets/Arena3Q/Scripts`, one asmdef per folder, dependencies flowing one way: Core ← Data ← Gameplay ← UI, with Editor on top.

- **Core** (`Arena3Q.Core`): pure C# rules, no MonoBehaviours. `BattleRuntimeState` (all per-battle state, new instance per battle), commands (`SummonRequest`, `MoveCommand`, `AttackCommand`) validated by `SummonRules` / `CommandRules`, `BattleOutcomeRules` + `StarRating`, and the brains: `HeroBrain` (the spec §6 state machine), `TroopBrain`, `EnemyBrain`. Brains read the world through sensor interfaces (`IHeroSensor`, ...) and return a `HeroIntent` (Hold / MoveTo / Engage), which keeps them unit-testable.
- **Data** (`Arena3Q.Data`): ScriptableObjects (`HeroDefinition`, `TroopDefinition`, `StageConfig`, `TestLoadout`, `FactionDefinition`) with `Validate()` and converters to Core settings (`StageConfig.CreateBattleSetup`, `CreateHeroBrainSettings`). Every 🟡 rule is a field here.
- **Gameplay** (`Arena3Q.Gameplay`): `BattleController` is the composition root and the only entry point for player actions; `UnitBase` subclasses (`HeroUnit`, `TroopUnit`, `EnemyUnit`, `CastleUnit`) implement the sensors and execute intents; `UnitRegistry` does spatial queries; `BattleInputController` (mouse + touch gestures, tap interpretation in `BattleController.TapAt`) and `BattleCameraController`. Sprites are generated at runtime by `PlaceholderArt`.
  - **Logic vs view positions:** gameplay runs on a flat logic plane. `UnitBase.Position` is the logic position; `transform.position` is only its on-screen projection (`BattleController.ToView`, Y squashed by `cos(viewTiltDegrees)` for the 2.5D tilt). Never read `transform.position` for gameplay, and convert screen ↔ battlefield only through `BattleCameraController.ScreenToWorld` / `WorldToScreen` / `WorldToViewport`, which undo the tilt. Ground decorations go through `AddGroundVisual`.
  - **Forbidden zones** are `IObstacle`s (`RectObstacle`, `RiverObstacle`: centre line + width, passable only at crossings) and block movement: `UnitBase.MoveTowards` follows `BattlefieldLayout.NextWaypoint` (shortest route over a visibility graph of nodes precomputed around the zones) with `ResolveStep`/`PushOut` as a safety net. Stage data: `StageConfig.forbiddenZones` (rects) and `rivers`.
- **UI** (`Arena3Q.UI`): `BattleHud` builds the uGUI HUD from code (cards, timer, edge icons, result screen).
- **Editor** (`Arena3Q.Editor`): `PrototypeContentBuilder` creates the placeholder assets in `Assets/Arena3Q/Content` and the Battle scene (only missing ones; delete an asset to regenerate it) and adds the menu items `Arena3Q > Build Prototype Content / Validate Stages / Open Battle Scene`. `BuildScripts` has the build methods (`Arena3Q > Build`).
- **Tests**: `Assets/Arena3Q/Tests/EditMode` (Core/Data rules) and `Tests/PlayMode` (`BattleFlowTests` drives the real Battle scene with `BattleController.Simulate`; it uses `SerializedObject`, so the file is wrapped in `#if UNITY_EDITOR`). Do not make the PlayMode asmdef Editor-only: the runner would treat it as EditMode tests. The screenshot test writes `Logs/Screenshots/*.png`.

## Working in this repo

- Unity generates `.csproj`/`.slnx` files and everything under `Library/`, `Temp/`, `Logs/`, and `UserSettings/`. Don't edit these by hand; they're gitignored or regenerated.
- Every asset under `Assets/` needs its `.meta` file committed alongside it. When creating, moving, or deleting assets outside the Editor, keep the `.meta` file in sync (moving it with the asset keeps its GUID, so scene and prefab references survive).
- Scenes, prefabs, and `.asset` files are YAML referencing GUIDs/fileIDs. Avoid hand-editing them except for trivial, well-understood changes.
- New gameplay code should live in its own assembly definition so it compiles separately from the template's `Unity.2D.Welcome` assembly. Tests need a separate test asmdef that references `UnityEngine.TestRunner`/`UnityEditor.TestRunner` (Test Framework 1.8).

## Command-line build and test

Everything runs through the Unity Editor executable (normally installed via Unity Hub at `C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe`). The project must not be open in another Editor instance while these run.

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"

# Run EditMode tests (use -testPlatform PlayMode for play mode tests)
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/test-results.xml -logFile -

# Run a single test or fixture
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "Namespace.ClassName.MethodName" -logFile -

# Compile-check scripts without building
& $unity -batchmode -quit -projectPath . -logFile -

# (Re)create missing placeholder content and the Battle scene
& $unity -batchmode -quit -projectPath . -executeMethod Arena3Q.Editor.PrototypeContentBuilder.BuildAll -logFile -

# Builds (output under Builds/, gitignored)
& $unity -batchmode -quit -projectPath . -executeMethod Arena3Q.Editor.BuildScripts.BuildAndroidDev -logFile -
& $unity -batchmode -quit -projectPath . -executeMethod Arena3Q.Editor.BuildScripts.BuildWindowsDev -logFile -
```

In batch mode, `-logFile -` output is easy to lose; writing to `Logs/<name>.log` and grepping for `error CS` works better.

To open the Editor straight into the battle: `Start-Process $unity -ArgumentList '-projectPath "<repo>"', '-executeMethod Arena3Q.Editor.PrototypeContentBuilder.OpenBattleScene'`.

Starting a player build makes Unity rewrite `Assets/Settings/UniversalRP.asset` (shader prefiltering), `UniversalRenderPipelineGlobalSettings.asset` and `ProjectSettings/UnityConnectSettings.asset`; don't commit those side effects together with feature work.
