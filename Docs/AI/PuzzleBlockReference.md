# Puzzle Block Reference

## Common authoring rule

- Every persistent puzzle object needs one `PuzzleStateController`.
- Assign a project-wide unique `puzzleId`, for example `ch01.box_reveal`.
- Use `onCompletedStateApplied` for visual state that must be restored after load.
- Use `onFirstCompleted` for one-time rewards, sounds, or dialogue.
- Add `InventoryItemReward` to a completion event when a puzzle grants items.
- `PuzzleStateObjectBinding` can switch incomplete/completed GameObjects without
  custom code.

Sub-step IDs, pickup IDs, outcomes, morality, inventory contents, equipment,
and discoveries are all stored by the existing save system.

## Available blocks

| Component | Purpose |
| --- | --- |
| `NumericCodeLock` | Runtime keypad, keyboard input, code validation, wrong/correct events |
| `PushablePuzzleObject` | One or more persistent push movements and reveal events |
| `ItemPlacementPuzzle` + `ItemPlacementSocket` | Required equipped items, fixed sockets, ordered placement |
| `InventoryLayoutPuzzle` | Exact inventory position and rotation validation |
| `OverlayInspectionPuzzle` | Overlay two item sprites and reveal a clue or code |
| `MultiStepItemUsePuzzle` | Ordered machine, lever, card-key, or item-use steps |
| `ExclusivePuzzleChoice` + option | One irreversible branch, item reward, item consumption, morality |
| `InventoryWeightSensor` | Maximum item/cell count, forbidden items, and progress prerequisites |
| `SceneTransitionInteractable` | Conditional fade transition between build scenes |
| `ChapterFlowController` | Objective text, required puzzle list, chapter completion, next scene |
| `InventoryItemReward` | Transactional multi-item reward and discovery registration |
| `PuzzleStateObjectBinding` | Restore open/closed, visible/hidden scene state after load |

`PuzzleModalUI` and `SceneTransitionService` are created automatically at
runtime. They do not require scene objects or prefabs.

## Chapter mapping

### Chapter 1

- Box reveal: `PushablePuzzleObject`
- Hole clue and locked chest: `InvestigationPoint` + `NumericCodeLock`
- Doll table: `ItemPlacementPuzzle` with three sockets
- Book arrangement: `InventoryLayoutPuzzle`
- Ball-track machine: `MultiStepItemUsePuzzle`
- Exit door: `ChapterFlowController`

### Chapter 2

- Diary code `0416`: `NumericCodeLock`
- Loaded slingshot and window: one-step `MultiStepItemUsePuzzle`
- Virtue/sin storage choice: `ExclusivePuzzleChoice`
- Film plate and torn paper: `OverlayInspectionPuzzle`
- Door lock and exit: `NumericCodeLock` + `ChapterFlowController`

### Chapter 3

- Ink on pending document: one-step `MultiStepItemUsePuzzle`
- Card-key storage: one-step `MultiStepItemUsePuzzle`
- Hammer versus precision key: `ExclusivePuzzleChoice`
- Fireplace versus organized-document route: `ExclusivePuzzleChoice`
- Exit weight check: `InventoryWeightSensor`

## Persistence details

- World pickups derive a stable fallback ID from scene and hierarchy path.
  Assign `persistentPickupId` explicitly when hierarchy names may change.
- Push steps use `<puzzleId>.push.<number>`.
- Placement sockets use `<puzzleId>.socket.<socketId>`. Socket IDs must be
  unique inside the puzzle.
- Machine steps use `<puzzleId>.step.<stepId>`.
- Choice outcomes use `<choicePuzzleId>.choice.<optionId>` unless an explicit
  outcome ID is assigned.
- Completed-state events are reapplied after save loading. First-completion
  events are not replayed, preventing duplicate rewards.

## Build scenes

Both global Build Settings and the Windows build profile use:

1. `Mainmenu`
2. `FirstMap`
3. `BaseMap`

`InventoryTestScene` remains disabled and is available for manual testing.
