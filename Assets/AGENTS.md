# Project Agent Rules

## Unity scene work isolation

These rules apply to any task that can change a Unity scene or its serialized presentation, including `.unity` and `.prefab` files, scene builders, Editor authoring utilities, and scripts or assets whose purpose is to rebuild or materially rearrange a scene.

1. Before making any scene-related change, inspect the current Git branch, working-tree status, the active Unity scene, and whether the scene has unsaved changes.
2. Never perform scene-related work directly on the user's current branch. Before the first mutation, create a dedicated Codex branch named `codex/scene-<short-task-name>` from the exact current branch `HEAD`.
3. The dedicated scene branch must start with the complete current working-tree state: all staged changes, unstaged changes, and untracked files present on the user's current branch must remain present after switching to the Codex branch. Do not start from a clean `HEAD`, another branch, or an older commit while omitting those changed files.
4. Record the source branch name, source `HEAD`, and the staged/unstaged/untracked file inventory before creating the branch. After switching, verify that the new branch has the same base commit and that every pre-existing changed file is still present with identical content. If checkout, conflicts, ignored files, submodules, or tooling make exact transfer uncertain, stop before scene mutation and ask the user.
5. Before running a scene builder, bulk authoring command, or any operation that replaces or deletes scene objects, save a uniquely timestamped snapshot of the current scene. Never reuse or conditionally skip an old backup.
6. Treat the currently open scene as authoritative. Inspect its hierarchy and visual state before deciding that a builder or older scene file represents the intended layout.
7. After changes, save and validate only on the dedicated branch. Report the source branch and `HEAD`, dedicated branch name, inherited changed-file verification, changed scene paths, backup path, and validation results to the user. Do not merge, rebase, or switch the user's original branch unless the user explicitly requests it.
8. A request to roll back scene work means restoring the exact pre-change snapshot and removing changes introduced by the scene task, while preserving unrelated user work.

## Unity raster image generation and editing

1. All generation or visual modification of raster images for this project must use the `Antigravity Image Bridge` plugin and its `gemini-image-bridge` skill. This includes creating new sprites, repainting or inpainting existing sprites, removing or replacing visual elements, compositing, recoloring, background removal, and producing visual variants.
2. Always call `gemini_image_bridge_status` before requesting an image. If the Antigravity bridge is not ready, report the exact setup problem and stop. Do not silently use Codex built-in ImageGen, GPT Image, another image model, or a manually substituted generated asset.
3. Antigravity output must remain in `Assets/.gemini-image-staging` until it has been visually inspected for the requested subject, style match, aspect ratio, transparency, edge quality, and explicit restrictions. Never import a failed or unreviewed result.
4. Only after the staged result passes inspection may it be copied to its requested Unity asset destination and imported through the connected Unity Editor. Antigravity must never modify the Unity project directly.
5. Deterministic post-processing such as resizing, cropping, format conversion, sprite-sheet packing, or alpha validation is allowed only after Antigravity has produced or edited the visual content, and it must not introduce or reinterpret visual content.
6. Using any non-Antigravity image-generation system requires the user's explicit approval for that specific task. A previous approval does not carry over to another image task.
