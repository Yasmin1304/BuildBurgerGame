# Build Burger Game

Build Burger Game is a Unity project for a child-facing movement game used in a
research setting. The child stands in front of a camera, calibrates their body
position, and catches falling objects using body/hand movement.

The project currently uses YOLO pose detection through Unity Inference Engine,
plus a lightweight ByteTrack-style person tracker. The old tracking-plugin
README that originally came with this project is no longer applicable.

## Main Features

- Camera-based body and wrist tracking.
- Pre-game and between-level body-position calibration.
- Burger, Letters, and Numbers themes.
- English and Arabic localization.
- RTL-aware Arabic UI text.
- Localized instruction slides with Next/Back navigation.
- Optional recorded narration per theme and language.
- Static, calm `3, 2, 1, Go` countdown without zoom/pop animation.
- Falling catchable objects.
- Optional obstacle levels with spacing rules to reduce unavoidable catches.
- Pause menu with home-exit confirmation.
- Level-complete and game-complete screens.
- Supabase logging for participants, sessions, gameplay events, and summaries.

## Project Flow

1. The researcher/player selects a theme.
2. The participant code is entered.
3. Localized instruction slides are shown.
4. Calibration asks the child to stand where the camera can see them clearly.
5. The countdown appears calmly at a fixed scale.
6. The child catches falling items and avoids obstacles when enabled.
7. The game records hits, misses, sides, level results, and session summaries.
8. The level-complete UI allows moving forward, replaying a level, or replaying
   from level 1 at the end.

## Unity Version And Packages

This project is a Unity project and should be opened from the repository root.

Important runtime dependencies include:

- Unity Inference Engine for YOLO pose inference.
- TextMeshPro for UI text.
- RTLTMPro for Arabic text shaping/display.
- Supabase REST integration scripts for research data logging.

Check `Packages/manifest.json` in Unity for the exact package list used by the
current project.

## Important Scenes

Common scene files are under `Assets/Scenes`.

- `MainMenu.unity` contains the menu, theme selection, participant flow, and
  instruction panels.
- The gameplay scene contains calibration, countdown, tracking, catch targets,
  spawners, score/progress UI, pause UI, and level-complete UI.

Scene object wiring is done mainly through Inspector references, so after script
changes Unity should be allowed to recompile before testing.

## Script Overview

Most project scripts are under `Assets/Scripts`.

### Core

`Assets/Scripts/Core/GameManager.cs`

Controls game flow, level progression, level-complete transitions, replay
behavior, obstacle-instruction flow, pausing, and recalibration resume.

`Assets/Scripts/Core/LevelConfig.cs`

Defines level defaults such as item speed, spawn interval, max item count,
spawn padding, and obstacle settings.

`Assets/Scripts/Core/SessionData.cs`

Stores session-level runtime choices such as participant code, selected theme,
and requested start level.

### Tracking

`Assets/Scripts/Tracking/WebCamInputProvider.cs`

Owns webcam input and maps camera preview coordinates into Unity screen
coordinates.

`Assets/Scripts/Tracking/YoloBodyPoseProvider.cs`

Runs YOLO pose inference, extracts COCO pose keypoints, exposes calibrated body
landmarks, provides wrist positions for gameplay, and locks tracking to the
selected player.

`Assets/Scripts/Tracking/ByteTrackPersonTracker.cs`

Maintains person tracks across frames so the game can keep following the same
child when more than one person is visible.

### Calibration

`Assets/Scripts/Calibration/BodyPoseProvider.cs`

Defines the tracking abstraction consumed by calibration and gameplay.

`Assets/Scripts/Calibration/BodyPositionCalibrationManager.cs`

Handles calibration before gameplay and during runtime correction. It checks
whether the child is visible, centered, and at a usable distance from the camera.
It also shows English/Arabic instructions and supports recorded narration per
instruction, per theme, and per language.

### Gameplay

`Assets/Scripts/GamePlay/IngredientSpawner.cs`

Spawns catchable objects. In Burger mode, the bottom bun appears first and keeps
appearing until caught. After the bottom bun is caught, regular ingredients
spawn.

`Assets/Scripts/GamePlay/ObstacleSpawner.cs`

Spawns obstacles when enabled. It includes fairness checks so obstacles avoid
the center lane and avoid spawning too close to catchable items or other
obstacles.

`Assets/Scripts/GamePlay/HandCatch3D.cs`

Handles catching ingredients, catching obstacles, scoring, burger stacking, and
level-completion requests.

`Assets/Scripts/GamePlay/FreeDropReceiver.cs`

Handles free-drop style catching/placement for non-burger themes.

`Assets/Scripts/GamePlay/MissZoneLogger.cs`

Logs items that fall past the catch zone.

`Assets/Scripts/GamePlay/LevelItemResolutionTracker.cs`

Tracks whether spawned items have been caught or missed before a level can be
completed.

`Assets/Scripts/GamePlay/LevelSessionTracker.cs`

Tracks per-level hit/miss metrics and left/right distribution.

`Assets/Scripts/GamePlay/FollowNearestHandCluster.cs`

Moves gameplay receivers based on tracked wrist positions.

`Assets/Scripts/GamePlay/HandTrackingAvailabilityMonitor.cs`

Monitors wrist availability and can pause spawning when tracking is stale.

### UI

`Assets/Scripts/UI/MainMenuUI.cs`

Controls menu navigation, theme selection, participant entry, instruction slide
navigation, localized instruction images, and per-theme/per-language narration
clips.

`Assets/Scripts/UI/SettingsUI.cs`

Allows the researcher to configure level count, speed, max objects, and
obstacle settings.

`Assets/Scripts/UI/PauseController.cs`

Handles pause/resume, replay current level, home navigation, and the
confirmation popup before exiting gameplay.

`Assets/Scripts/UI/LevelCompleteUI.cs`

Shows level-complete and game-complete UI, including localized Arabic/English
text handling.

`Assets/Scripts/UI/GameStartCountdownImages.cs`

Shows the static countdown images without zooming or popping.

`Assets/Scripts/UI/BurgerProgressUI.cs`

Displays burger-building progress.

`Assets/Scripts/UI/BadCatchFeedbackUI.cs`

Shows feedback when an obstacle or invalid object is caught.

### Localization

`Assets/Scripts/Localization` contains helpers for:

- language switching
- localized text
- localized images
- theme-specific localized images
- RTL layout direction
- TMP font swapping
- Arabic input preview behavior

### Database

`Assets/Scripts/Database` contains Supabase integration:

- participant insert
- session insert
- gameplay event insert
- session summary update
- Supabase configuration

## Audio And Narration

The project is set up for recorded voice clips rather than live text-to-speech.

Instruction-slide narration is configured on `MainMenuUI`:

- Burger English clips
- Burger Arabic clips
- Letters English clips
- Letters Arabic clips
- Numbers English clips
- Numbers Arabic clips

Each audio array should match the order of the instruction image array.

Calibration narration is configured on `BodyPositionCalibrationManager`. Each
calibration instruction can have shared English/Arabic clips and optional
theme-specific overrides for Burger, Letters, and Numbers.

## Data Logging

Supabase scripts are responsible for research data logging. The current gameplay
flow can log:

- participant code
- session start
- level number
- hits
- misses
- object type
- catch side
- level summary

Supabase URL/key values should be configured in the appropriate Supabase config
asset or scene object and should not be committed if they are private.

## Working Notes

- Add new tracking behavior in the YOLO/ByteTrack tracking scripts, not in
  legacy plugin code.
- Keep calibration depending on `BodyPoseProvider` where possible.
- Keep child-facing UI calm and direct.
- Avoid strong motion effects for countdowns and instructions unless they are
  necessary for gameplay.
- After adding audio files, assign them in the Inspector before testing.
- After script changes, let Unity recompile and check the Console for missing
  references or serialization warnings.
