# Build Burger Game Scripts

This folder contains the runtime scripts for the Build Burger Game Unity project.
The current tracking stack uses a YOLO pose model through Unity Inference Engine,
with a lightweight ByteTrack-style tracker to keep gameplay focused on the
calibrated child. This README describes the current script layout and should not
be read as legacy tracking setup documentation.

## Current Architecture

The game flow is:

1. The child selects a theme: Burger, Letters, or Numbers.
2. The menu shows localized instruction slides, with optional recorded narration.
3. The calibration overlay checks that the child is visible, centered, and at a
   usable distance from the camera.
4. A calm static `3, 2, 1, Go` countdown appears.
5. Gameplay starts with falling catchable items and optional obstacles.
6. Per-level results are shown and session data can be logged to Supabase.

## Core

`Core/GameManager.cs`

Controls level progression, applies level settings, starts and pauses gameplay,
shows obstacle instructions when needed, handles level-complete transitions, and
supports replaying either the current level or the whole game from level 1.

`Core/LevelConfig.cs`

Defines per-level defaults such as spawn interval, fall speed, max item count,
spawn padding, obstacle toggle, and obstacle speed.

`Core/SessionData.cs`

Stores runtime session choices such as participant code, selected game mode, and
requested start level.

`GameMode.cs`

Defines the available themes:

- `Burger`
- `Letters`
- `Numbers`

## Tracking

`Tracking/WebCamInputProvider.cs`

Owns the webcam input and converts camera preview coordinates into screen
coordinates used by gameplay and calibration.

`Tracking/YoloBodyPoseProvider.cs`

Runs the YOLO pose model using Unity Inference Engine, extracts COCO pose
keypoints, exposes body landmarks, and provides wrist positions for gameplay.
It also locks gameplay to the calibrated child using the tracker.

`Tracking/ByteTrackPersonTracker.cs`

Maintains person tracks across frames so the game can keep following the same
child even when multiple people are visible.

## Calibration

`Calibration/BodyPoseProvider.cs`

Small abstraction between gameplay/calibration and the underlying pose provider.
This lets calibration use `BodyPoseProvider` without depending directly on the
YOLO implementation.

`Calibration/BodyPositionCalibrationManager.cs`

Runs the pre-game and between-level calibration checks. It verifies that the
child is visible, centered, and at an acceptable camera distance. It also:

- Shows child-friendly English/Arabic instructions.
- Shapes Arabic text for RTL display.
- Keeps instruction text centered and fitted to screen.
- Supports white text with black outline/shadow.
- Supports recorded narration per instruction, per language, and per theme.
- Can pause gameplay for a short recalibration if body anchors are lost.

## Gameplay

`GamePlay/IngredientSpawner.cs`

Spawns catchable items. For Burger mode, the bottom bun is shown first and keeps
appearing until caught; after that, regular ingredients spawn.

`GamePlay/ObstacleSpawner.cs`

Spawns obstacles when enabled for a level. It includes fairness checks so
obstacles avoid the center lane and avoid spawning too close to catchable items
or other obstacles.

`GamePlay/HandCatch3D.cs`

Handles catches and obstacle hits, stacks burger items when relevant, notifies
the spawner when the bottom bun is caught, updates score/progress, and requests
level completion when all required items are resolved.

`GamePlay/FreeDropReceiver.cs`

Handles the free-drop style catching/placement workflow for non-burger themes.

`GamePlay/FallingIngredient.cs`

Moves spawned items downward.

`GamePlay/ControlledFallVelocity.cs`

Applies controlled falling velocity to spawned objects.

`GamePlay/MissZoneLogger.cs`

Detects missed items and logs misses.

`GamePlay/LevelItemResolutionTracker.cs`

Tracks whether spawned items have been caught or missed so level completion only
happens after all required items are resolved.

`GamePlay/LevelSessionTracker.cs`

Tracks per-level performance metrics such as hits, misses, and left/right catch
distribution.

`GamePlay/FollowNearestHandCluster.cs`

Moves gameplay receivers based on the tracked wrist positions from
`YoloBodyPoseProvider`.

`GamePlay/HandTrackingAvailabilityMonitor.cs`

Monitors wrist availability and can pause spawning when hands are not detected
freshly enough.

`GamePlay/SpawnPositionUtility.cs`

Shared helper for spawn-position calculations.

`GamePlay/ScoreManager.cs`

Maintains the current gameplay score.

## UI

`UI/MainMenuUI.cs`

Controls main-menu navigation, theme selection, participant entry, localized
instruction slides, slide next/back buttons, and recorded narration for
instruction slides per theme and language.

`UI/SettingsUI.cs`

Allows the researcher to configure session/level settings such as level count,
item speed, obstacle speed, max item count, and whether obstacles are enabled.

`UI/PauseController.cs`

Controls pause/resume behavior, replay current level, home navigation, and the
exit-confirmation popup shown before leaving gameplay.

`UI/LevelCompleteUI.cs`

Displays level-complete and game-complete screens, supports localized text, and
routes Next Level / Play Again actions.

`UI/GameStartCountdownImages.cs`

Displays the static `3, 2, 1, Go` countdown images. The older zoom/pop animation
has been removed to keep the countdown calmer and less overstimulating.

`UI/BurgerProgressUI.cs`

Displays visual progress while building the burger.

`UI/BadCatchFeedbackUI.cs`

Shows feedback when the child catches an obstacle or invalid object.

`UI/ButtonPressAnim.cs`

Adds button press animation behavior.

`UI/ThemeSelector.cs`

Stores simple theme-selection UI behavior.

## Localization

The `Localization` folder contains helpers for English/Arabic UI:

- `LanguageManager.cs`
- `AppLanguage.cs`
- `LocalizationDatabase.cs`
- `LocalizedText.cs`
- `LocalizedImage.cs`
- `ThemeLocalizedImage.cs`
- `ThemeLocalizedSprite.cs`
- `LocalizedDropdown.cs`
- `LocalizedLayoutDirection.cs`
- `LocalizedTMPFont.cs`
- `LanguageSwitcher.cs`
- `LanguageToggleVisual.cs`
- `ArabicInputPreview.cs`

These scripts handle localized copy, localized images, RTL layout direction,
Arabic input preview behavior, and language switching.

## Settings

`Settings/SettingsData.cs`

Stores runtime settings for the current session.

`Settings/LevelSettings.cs`

Data model for one level's editable settings.

## Database

The `Database` folder contains Supabase integration scripts:

- `SupabaseConfig.cs`
- `SupabaseParticipantCodeInsert.cs`
- `SupabaseSessionInsert.cs`
- `SupabaseSessionEventInsert.cs`
- `SupabaseSessionUpdate.cs`

These scripts create participant/session records, log gameplay events, and
update session summaries after a level is complete.

## Archived

`Archived/LevelSettingsCardUI.cs`

Older dynamic settings-card UI. Kept for reference, but not part of the current
main settings workflow.

## Notes For Future Changes

- Add or change tracking behavior in `Tracking/YoloBodyPoseProvider.cs`, not in
  old tracking scripts.
- Keep calibration-facing code depending on `BodyPoseProvider` where possible.
- For new narrated instructions, import recorded audio clips into Unity and
  assign them in the Inspector fields on `MainMenuUI` or
  `BodyPositionCalibrationManager`.
- For child-facing UI, prefer calm transitions, clear spacing, and reduced
  motion unless the interaction truly needs movement.
