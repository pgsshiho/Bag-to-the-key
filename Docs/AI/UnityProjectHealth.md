# Unity Project Health Report

> Last analyzed: 2026-07-30
> Commit: `b89732a` with uncommitted equipment/save changes
> Scope: Standard read-only static audit
> Project root: `C:/Users/USER/Documents/GitHub/Bag-to-the-key`

## Overall Assessment

The project is small enough to remain understandable, and its inventory domain
has clear state ownership. It is not currently build-ready, however. The most
important risks are a broken build-scene entry, a runtime-created interaction
dispatcher whose lifetime does not cover scene transitions, and save files
being overwritten without atomic replacement or recovery.

## Coverage

### Checked

- Unity version, package manifest and lock file
- URP and input configuration
- first-party script structure and representative implementation
- build scenes and project-owned scene inventory
- scene/prefab references to first-party scripts
- inventory, combination, equipment, progress, and save architecture
- static lifecycle, event, allocation, and asset-loading indicators
- item/recipe ID uniqueness
- tests, CI, Git hygiene, tracked generated files, and large tracked assets

### Not Checked

- Unity Console and current compilation state
- Play Mode behavior and scene transitions
- scene/prefab missing references through the Unity serialization API
- EditMode or PlayMode test execution
- Windows or other target-platform builds
- profiler, memory, rendering, and target-device captures

## Priority Findings

| ID | Severity | Confidence | Domain | Finding |
|---|---|---|---|---|
| H-001 | High | Confirmed | Build configuration | The only enabled build scene does not exist |
| H-002 | High | Likely | Lifecycle / interaction | The global point-and-click interactor is lost after a scene transition |
| H-003 | High | Likely | Save integrity | Save files are overwritten in place without recovery or migration enforcement |
| H-004 | Medium | Confirmed | Testing | Critical inventory and persistence rules have no automated regression tests |
| H-005 | Medium | Confirmed | Equipment integration | No scene or prefab currently uses `EquippedItemUseTarget` |
| H-006 | Medium | Likely | Maintainability / UI | `InventoryUI` combines state interaction, input, view rebuilding, and runtime UI construction |
| H-007 | Low | Confirmed | Repository size | Two tracked font assets are approximately 32 MB each |

## Detailed Findings

### H-001: The Only Enabled Build Scene Does Not Exist

**Observation:** `EditorBuildSettings.asset` enables
`Assets/Scenes/SampleScene.unity`, but the file is absent. The project-owned
scenes are `Mainmenu`, `BaseMap`, `FirstMap`, and `InventoryTestScene`.

**Impact:** A normal player build has no valid startup scene and is blocked
until Build Settings are corrected.

**Recommendation:** Set the intended startup and gameplay scene order through
the Unity Editor, then perform an actual Windows build.

**Remediation size:** Small

**Evidence:** `ProjectSettings/EditorBuildSettings.asset:7`

### H-002: The Global Point-And-Click Interactor Is Lost After Scene Changes

**Observation:** `PointAndClickInteractor` is created by an
`AfterSceneLoad` runtime initializer, is not marked `DontDestroyOnLoad`, and
has no serialized scene or prefab usage. Its own `OnDestroy` removes the
scene-loaded subscription.

**Impact:** The first scene can receive point-and-click interaction, but a
subsequent non-additive scene load is likely to destroy the only dispatcher.
World interactions can then stop responding.

**Recommendation:** Choose one lifetime model: keep one validated dispatcher
alive across scenes, or place/bootstrap one in every gameplay scene. Add a
PlayMode transition test.

**Remediation size:** Small

**Validation needed:** Load from the menu into gameplay and between gameplay
scenes, then verify exactly one active dispatcher and successful interaction.

**Evidence:** `Assets/Scripts/ItemScript/PointAndClickInteractor.cs:11`,
`:16`, and `:26`

### H-003: Save Replacement And Migration Are Not Resilient

**Observation:** A save slot is written directly with `File.WriteAllText`.
There is no temporary-file replacement or backup. `formatVersion` is written,
but loading does not validate it or route through migrations. A failed load can
also restore camera/progress state before confirming that an inventory manager
exists.

**Impact:** Process interruption, disk failure, schema evolution, renamed
scenes, or changed grid constraints can produce an unrecoverable or partially
applied save.

**Recommendation:** Write and flush a temporary file, validate it, then replace
the slot while retaining one backup. Validate `formatVersion` and migrate
explicitly before applying any game state. Apply loaded data transactionally
after all references and scene prerequisites are confirmed.

**Remediation size:** Medium

**Validation needed:** Automated fixtures for current, legacy, truncated,
unknown-version, missing-item, invalid-position, and renamed-scene saves.

**Evidence:** `Assets/Scripts/SaveScript/SaveLoadManager.cs:166`, `:168`,
`:254`, and `:264`; `Assets/Scripts/SaveScript/SaveData.cs:8`

### H-004: Critical Domain Rules Lack Automated Regression Tests

**Observation:** Unity Test Framework 1.6.0 is installed, but no first-party
test scripts or test assemblies were found. `InventoryTestScene` is a manual
test surface only.

**Impact:** Changes to placement, rotation, recipe matching, disassembly,
equipment, and save migration can regress without a repeatable signal.

**Recommendation:** Start with EditMode tests for `InventoryGrid`, recipe
matching, combination rollback, equip/unequip capacity, and save-data
migration. Add focused PlayMode tests only for scene and UI lifecycles.

**Remediation size:** Medium

**Evidence:** `Packages/manifest.json`,
`Assets/Scenes/Editor/InventoryTestScene.unity`

### H-005: Equipped-Item World Use Is Not Integrated Into Serialized Content

**Observation:** The uncommitted `EquippedItemUseTarget` component exists, but
its script GUID is referenced by no scene or prefab.

**Impact:** Equipment can be stored and shown in the inventory UI, but no
serialized world object currently exercises the required-item, consume, or
success/failure event path.

**Recommendation:** Attach and configure the component on at least one intended
interaction target, or document that integration as remaining scene work.

**Remediation size:** Small

**Validation needed:** Equip, use, consume/non-consume, wrong-item, repeated-use,
save, and load scenarios in a gameplay scene.

**Evidence:** `Assets/Scripts/ItemScript/EquippedItemUseTarget.cs`

### H-006: Inventory UI Responsibilities Are Concentrated

**Observation:** `InventoryUI` is approximately 1,000 lines and handles input,
drag state, selection, slot creation, item-view destruction/recreation,
combination/disassembly overlays, catalog construction, and equipment UI
construction.

**Impact:** Inventory changes have a broad regression surface. Every refresh
destroys and recreates item views, which is acceptable at the current 60-cell
scale but couples domain changes to UI churn.

**Recommendation:** Do not rewrite it wholesale. Extract the catalog and
equipment views when those features next change, and preserve one controller
as the coordination point. Validate allocations with the profiler before
adding pooling.

**Remediation size:** Medium

**Evidence:** `Assets/Scripts/InventoryScript/InventoryUI.cs:260`, `:810`

### H-007: Duplicate Large Font Assets Increase Repository And Build Pressure

**Observation:** Two tracked TextMesh Pro font assets are approximately
32.1 MB each.

**Impact:** They increase clone/import size and may both enter a build if
referenced. Runtime memory impact is unknown without a build report.

**Recommendation:** Check which font/material variant is required and inspect
the build report before removing or reducing either asset.

**Remediation size:** Small

**Evidence:** `Assets/Font/GriunXHangeul_Equal-Rg SDF.asset`,
`Assets/Font/GriunXHangeul_Equal-Rg SDF Outline.asset`

## Healthy Areas

- Package versions are pinned and a package lock file is present.
- URP 17.4.0 and the 2D renderer are consistently configured in quality assets.
- Inventory state is owned by `InventoryManager`, with grid placement isolated
  in a plain C# class.
- Item and recipe IDs inspected were non-empty and unique.
- Sampled event subscriptions generally have matching unsubscriptions.
- No explicit null `m_Script` entries were found in project-owned scenes or
  prefabs by static text inspection.
- `.gitignore` covers normal Unity generated output, and no generated project
  directories or obvious credential-file types are tracked.
- Save text is explicitly written and read as UTF-8.

## Recommended Remediation Order

1. Correct Build Settings and establish the intended startup flow (H-001).
2. Fix and PlayMode-test the point-and-click dispatcher lifetime (H-002).
3. Complete scene integration for equipment use and verify the uncommitted
   equipment/save changes together (H-005).
4. Make save replacement and migration transactional, then add fixtures
   (H-003, H-004).
5. Add focused inventory-domain tests before further feature expansion (H-004).
6. Address UI separation and font footprint only when profiling or feature work
   justifies it (H-006, H-007).

## Validation Baseline

No Unity Editor, Console, tests, builds, or profiler captures were available.
This report is a repository-based static baseline, not a release-readiness
certification.

## Limitations

Unity MCP is not connected. Serialized references, runtime initialization
order, current Console state, platform-specific behavior, and actual memory
cost require Editor-based validation.

## Team Notes

Manual notes may be added here.
