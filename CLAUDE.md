# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Arena3Q is a freshly created Unity **6000.6.6f1** project from the Unity 2D URP template. There is no game code yet: the only script is the template's `Assets/Welcome/Welcome2DScript.cs` (Unity Learn welcome page, in the `Unity.2D.Welcome` asmdef), and the only scene in the build is `Assets/Scenes/SampleScene.unity`. Expect to create the gameplay architecture from scratch.

The gameplay specification lives in `docs/GameplaySpecification.md` and is the source of truth for gameplay. Read it before designing or writing gameplay code. Its markers: ✅ settled (implement exactly), 🟡 provisional default (must be a config option), ⏳ deferred (not in the prototype). If it conflicts with the configuration below (e.g. 3D, legacy input), flag the conflict instead of silently picking one.

## Key configuration

- **Rendering:** URP with the 2D Renderer (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`). Use 2D lights, `SpriteRenderer`, and URP 2D-compatible shaders/materials; built-in pipeline shaders will render pink.
- **Input:** `activeInputHandler: 1` means the **new Input System only**. The legacy `UnityEngine.Input` API will throw at runtime; use `UnityEngine.InputSystem`. A default action asset exists at `Assets/Settings/InputSystem_Actions.inputactions` (`Player` map: Move, Look, Attack, Interact, Crouch, Jump, Previous, Next, Sprint; plus a `UI` map).
- **Target platforms:** Android, iOS and PC, landscape orientation (per the spec). Only an `Android Dev` build profile exists so far, in `Assets/Settings/Build Profiles/`; Android uses the IL2CPP scripting backend. Input must work with both touch and mouse.
- **Packages** (`Packages/manifest.json`): 2D Animation, Aseprite and PSD importers, Tilemap + Tilemap Extras, SpriteShape, Timeline, uGUI, Visual Scripting, Test Framework.

## Working in this repo

- Unity generates `.csproj`/`.slnx` files and everything under `Library/`, `Temp/`, `Logs/`, and `UserSettings/`. Don't edit these by hand; they're gitignored or regenerated.
- Every asset under `Assets/` needs its `.meta` file committed alongside it. When creating, moving, or deleting assets outside the Editor, keep the `.meta` file in sync (moving it with the asset keeps its GUID, so scene and prefab references survive).
- Scenes, prefabs, and `.asset` files are YAML referencing GUIDs/fileIDs. Avoid hand-editing them except for trivial, well-understood changes.
- New gameplay code should live in its own assembly definition so it compiles separately from the template's `Unity.2D.Welcome` assembly. Tests need a separate test asmdef that references `UnityEngine.TestRunner`/`UnityEditor.TestRunner` (Test Framework 1.8).

## Command-line build and test

There are no build scripts yet; everything runs through the Unity Editor executable (normally installed via Unity Hub at `C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe`). The project must not be open in another Editor instance while these run.

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"

# Run EditMode tests (use -testPlatform PlayMode for play mode tests)
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/test-results.xml -logFile -

# Run a single test or fixture
& $unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter "Namespace.ClassName.MethodName" -logFile -

# Compile-check scripts without building
& $unity -batchmode -quit -projectPath . -logFile -
```

Builds need an editor method invoked with `-executeMethod` (e.g. a static method that calls `BuildPipeline.BuildPlayer` or builds from the build profile). None exists yet.
