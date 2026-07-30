# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/USER/Documents/GitHub/Bag-to-the-key`
- Genre: inventory item placement, combination, disassembly, and puzzle-solving game
- Last analyzed: 2026-07-30
- Last analyzed commit: `b89732a` on `TestingBranch`
- Working tree: equipment and save-data changes are currently uncommitted

## Confirmed Environment

- Unity version: Unity 6.4, `6000.4.2f1` (`7a4c1aeef971`)
- Render pipeline: Universal Render Pipeline 17.4.0 with a 2D Renderer
- Input system: both; Input System 1.19.0 is configured, while first-party code also uses legacy `Input` APIs
- Target platforms: a Windows build profile exists; the active Editor build target is unverified

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.4.0 with `UniversalRP` and `Renderer2D` assets | Confirmed | `Packages/manifest.json`, `Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset`, `ProjectSettings/QualitySettings.asset` |
| Input | Input System package and action asset coexist with legacy `Input` calls | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions`, `ProjectSettings/ProjectSettings.asset`, `Assets/Scripts/` |
| UI | Unity UI 2.0.0 and TextMesh Pro assets | Confirmed | `Packages/manifest.json`, `Assets/TextMesh Pro/` |
| Animation | DOTween is imported as a project plugin and enabled with `DOTWEEN` defines | Confirmed | `Assets/Plugins/Demigiant/DOTween/`, `ProjectSettings/ProjectSettings.asset` |
| Camera/cinematics | Cinemachine 3.1.7 and Timeline 1.8.12 are installed | Confirmed | `Packages/manifest.json` |
| Networking | No runtime networking framework or first-party multiplayer usage found | Confirmed | `Packages/manifest.json`, `Assets/Scripts/` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Scripts/InventoryScript` | Grid inventory, UI, movement modes, combination, equipment | Confirmed | 8 first-party scripts |
| `Assets/Scripts/ItemScript` | Item data, recipes, discovery, and world interactions | Confirmed | 15 first-party scripts |
| `Assets/Scripts/SaveScript` | JSON save slots, load flow, and delayed auto-save | Confirmed | 3 first-party scripts |
| `Assets/Scripts/ProgressScript` | Puzzle outcomes, morality, and ending/BGM behavior | Confirmed | 5 first-party scripts |
| `Assets/Scripts/MainmenuScript` | Main-menu presentation and save-slot entry | Confirmed | 3 first-party scripts |
| `Assets/Inventory/Resources` | Runtime-loaded item and recipe ScriptableObjects | Confirmed | item and recipe `.asset` files |
| `Assets/Scenes` | Main menu, gameplay maps, and an inventory test scene | Confirmed | four project-owned `.unity` files |
| `Assets/Plugins` | Imported third-party code, currently DOTween | Confirmed | `Assets/Plugins/Demigiant/DOTween/` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Assembly-CSharp` | All first-party runtime systems | UnityEngine, UGUI, DOTween | No first-party `.asmdef` or `.asmref`; all 40 scripts compile into one default assembly |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/SampleScene.unity` is the only enabled entry, but that file is absent.
- Project-owned scenes: `Mainmenu`, `BaseMap`, `FirstMap`, and `InventoryTestScene`.
- Likely startup scene: `Mainmenu` by naming and menu behavior, but this is not represented in Build Settings and remains unverified.
- Scene loading flow: saved games load the scene recorded in JSON through `SaveLoadManager`; no other first-party scene transition call was found.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Scene composition | MonoBehaviour components wired through serialized scene references | Confirmed | representative scripts and scenes |
| Inventory domain | `InventoryManager` owns item state and delegates placement to a plain C# `InventoryGrid` | Confirmed | `InventoryManager.cs`, `InventoryGrid.cs` |
| Data model | Items and recipes are ScriptableObjects loaded from `Resources` | Confirmed | `ItemData.cs`, `ItemRecipe.cs`, database classes |
| UI synchronization | Inventory and equipment changes propagate through C# events | Confirmed | `InventoryManager.cs`, `InventoryUI.cs` |
| Persistence | Persistent singleton-style managers serialize JSON under `Application.persistentDataPath/Saves` | Confirmed | `SaveLoadManager.cs`, `SaveData.cs` |
| Progress | Static `GameProgressState` tracks puzzle completion, outcomes, and morality | Confirmed | `GameProgressState.cs` |
| World interaction | A shared `IWorldInteractable` contract is dispatched by a point-and-click interactor | Confirmed | `IWorldInteractable.cs`, `PointAndClickInteractor.cs` |

## Coding Conventions

- Namespace style: no namespaces in first-party scripts.
- Naming: PascalCase types/methods; camelCase fields; public Inspector fields and `[SerializeField] private` fields both occur.
- Formatting: Allman braces are predominant.
- Async: coroutines and `SceneManager.LoadSceneAsync`; no task-based async framework found.
- Comments/docs: short Korean comments and log messages; XML documentation is not used.
- File encoding: read and preserve project text as UTF-8.

## Testing And Validation

- Unity Test Framework 1.6.0 is installed.
- No first-party EditMode/PlayMode test scripts or test assemblies were found.
- `Assets/Scenes/Editor/InventoryTestScene.unity` provides a manual inventory test surface.
- No repository CI configuration or documented build command was found.
- Build readiness is currently blocked by the missing enabled build scene until Build Settings are corrected.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Repository inspection and editing | available | Codex workspace filesystem |
| Unity-specific implementation and validation workflows | available | Unity Essentials plugin |
| Unity Editor MCP connection | unavailable | no MCP package/configuration or Unity MCP tools detected |
| Console, scene, prefab, Play Mode, tests, and profiler through MCP | unavailable | no connected Unity MCP provider |

## Important Constraints

- Do not overwrite the user's uncommitted equipment/save changes.
- Treat `.unity`, `.prefab`, `.asset`, and project settings as serialized assets; edit through Unity/MCP when possible.
- Runtime item and recipe discovery depends on placement under a `Resources` folder.
- Several managers use `DontDestroyOnLoad`; duplicate instances and stale scene references require care.
- Save/load relies on stable item IDs and scene names.

## Unknowns And Confidence

- The intended startup scene and final Build Settings order are unknown because the configured scene is missing.
- The active build target and whether the Windows profile is production-ready are unverified without the Editor.
- Scene and prefab reference integrity, Console state, and Play Mode behavior are unverified without Unity MCP or an Editor validation run.
- No automated regression baseline exists for inventory, combination, equipment, or save/load behavior.

## Source Files Inspected

- `README.md`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/Settings/UniversalRP.asset`
- `Assets/Settings/Renderer2D.asset`
- representative files under `Assets/Scripts/InventoryScript`, `ItemScript`, `SaveScript`, `ProgressScript`, `MainmenuScript`, and `RoomScript`
- first-party scene, assembly-definition, input-action, test, package, and MCP configuration inventories

<!-- unity-onboarding:generated:end -->
