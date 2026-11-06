# Block Jam — Rollic Game Developer Case 2026

Unity puzzle prototype (menu, gameplay, progression, level editor) built for the Rollic case brief.

## Requirements

- **Unity:** `2022.3.62f2` (LTS) with URP
- Open the project folder in Unity Hub, then open the project in the Editor

## How to run

1. Open the project in Unity `2022.3.62f2`.
2. Open `Assets/Scenes/Menu.unity`.
3. Press **Play**.
4. Tap the level button on the home screen to load Gameplay.
5. Drag blocks toward matching-colored doors. Clear the board before the timer ends to win; use **Next** to advance progress.

Progress (current level, coins) is stored in `PlayerPrefs`.

## Level Editor

1. In the Unity menu bar: **Block Jam > Level Editor**.
2. Use the left list to select, reorder, create, duplicate, or delete levels (`LevelCatalog`).
3. Set board size, timer, and coin reward.
4. Choose **Block** / **Door** / **Erase**, pick a color and shape, then click the board (or edge slots for doors).
5. **Validate** runs layout checks and a solvability search.
6. **Save** writes the working copy into the selected `LevelData` asset (blocked if there are validation errors).
7. **Play** saves context and opens the Gameplay scene with that level as a playtest override. Leaving Play Mode clears the override.

**Notes**

- Edits happen on a working copy; the asset only changes on Save. Closing the window or switching levels prompts if there are unsaved changes.
- Shrinking the board does **not** auto-delete blocks that fall outside; Validate reports them as errors.
- **Next** in playtest still advances player progress.

## Architecture

| Layer | Role | Why |
| --- | --- | --- |
| **Data** (`LevelData`, `BlockShape`, `LevelCatalog`) | ScriptableObject level format | Same assets for runtime and editor; designers edit without code changes |
| **Logic** (`BoardModel`, `LevelValidator`, `LevelSolver`) | Pure C# rules, no MonoBehaviour | One move API (`TryStep`) shared by gameplay and the solver; easy to reason about and test |
| **Presentation** (`BoardPresenter`, `GameplayPresenter`) | Input → model steps; HUD / win-fail flow | Keeps drag handling and UI state out of the board rules |
| **View** (`BoardView`, block/door visuals, UI screens) | Spawn tiles, animate moves/exits | Visuals can change without touching rules |
| **Installers** (`MenuInstaller`, `GameplayInstaller`) | Wire dependencies at scene start | Small composition roots instead of a heavy DI framework |
| **Editor** (`Assets/Scripts/Editor/LevelEditor/`) | IMGUI window + working-copy session | No extra packages; `SerializedObject` gives Undo/Redo; Editor folder stays out of player builds |

**Move model:** drags are converted into single-cell steps so blocks cannot skip over each other. Exit requires a matching door on every lane the piece covers, with a clear path to the edge.

**Optional (case 5):** Validate uses layout errors/warnings, then a **BFS** solvability check on `BoardModel` (~200k state limit → `Solvable` / `Unsolvable` / `TooComplex`). Save can still proceed after a solvability warning via a confirm dialog.

## Asset feedback

Wall and door sprites were hard to assemble cleanly for board edges. The supplied corner / multi-piece wall-door set did not line up well in the UI, so the board uses only the **1×1 wall** and **1×1 door** pieces. Corner variants were left unused. A clearer slice guide (or pre-authored edge strips) would make full wall kits easier to use.

## Known issues / incomplete

- Lives are a placeholder (always full); no life spend / refill economy.
- Home tab bar icons are mostly visual; only the main level flow is wired.
- Settings cover the toggles needed for the case; no full audio/settings pipeline beyond that.
- Very large layouts may hit `TooComplex` in the solver (not a hard proof of unsolvability).
- No automated play-mode test suite; validation is editor/manual oriented.

## LLM tools

Used **Cursor** (Composer) as an assistant for the heavier or error-prone pieces, not for the whole project:

- Level editor session / canvas interaction details and Undo-safe `SerializedObject` wiring
- BFS solvability search and tying it to the same `BoardModel` move rules
- Playtest override path (`SessionState` + domain reload)
- Targeted debugging when editor/playtest edge cases appeared

Core gameplay loop, board rules, UI wiring, and most level content were implemented and iterated in the Editor directly.

## Approximate work time

About **30 hours** over **5 days** (~6 hours/day): setup and core gameplay first, then progression/UI polish, then the level editor, validation/solver, and five playable levels.
