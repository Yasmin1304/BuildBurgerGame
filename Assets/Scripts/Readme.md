{\rtf1\ansi\ansicpg1252\cocoartf2868
\cocoatextscaling0\cocoaplatform0{\fonttbl\f0\fswiss\fcharset0 Helvetica;}
{\colortbl;\red255\green255\blue255;}
{\*\expandedcolortbl;;}
\paperw11900\paperh16840\margl1440\margr1440\vieww11520\viewh8400\viewkind0
\pard\tx720\tx1440\tx2160\tx2880\tx3600\tx4320\tx5040\tx5760\tx6480\tx7200\tx7920\tx8640\pardirnatural\partightenfactor0

\f0\fs24 \cf0 # \uc0\u55356 \u57172  Build-a-Burger Game \'97 Scripts Overview\
\
This folder contains all scripts used in the Build-a-Burger serious game.  \
The project is designed for research purposes to study motor coordination, interaction, and performance metrics.\
\
---\
\
# Folder Structure\
\
## Core\
Handles overall game flow and session state.\
\
- **GameManager.cs**\
  Controls level progression, applies level settings, manages transitions, and handles end-of-game logic.\
\
- **LevelConfig.cs**\
  Defines static configuration for levels (names, rules, constraints).\
\
- **SessionData.cs**\
  Stores runtime session data such as participant ID and session ID.\
\
---\
\
## Gameplay\
Handles all in-game mechanics and interactions.\
\
- **HandCatch3D.cs**\
  Detects collisions between hands and falling objects (ingredients/obstacles), applies game rules, and logs events.\
\
- **IngredientSpawner.cs**\
  Spawns ingredients based on interval, max count, and level configuration.\
\
- **ObstacleSpawner.cs**\
  Spawns obstacles when enabled in settings.\
\
- **FallingIngredient.cs**\
  Controls falling behavior of ingredients.\
\
- **MissZoneLogger.cs**\
  Detects when ingredients are missed (not caught) and logs them as "miss" events.\
\
- **FollowNearestHandCluster.cs**\
  Handles movement or tracking logic for hand-based interaction.\
\
- **LevelSessionTracker.cs**\
  Tracks gameplay metrics per level (hits, misses, left/right distribution).\
\
---\
\
## UI\
Handles all user interface elements.\
\
- **MainMenuUI.cs**\
  Controls navigation from the main menu.\
\
- **SettingsUI.cs**\
  Allows the user (researcher) to configure per-level settings:\
  - number of levels\
  - ingredient speed\
  - obstacle speed\
  - max ingredients\
  - obstacles toggle\
\
- **LevelCompleteUI.cs**\
  Displays level completion screen with score and next-level option.\
\
- **BurgerProgressUI.cs**\
  Visualizes progress of building the burger.\
\
- **GameStartCountdownImages.cs**\
  Displays countdown before gameplay starts.\
\
---\
\
## Settings\
Stores configurable parameters for each level.\
\
- **SettingsData.cs**\
  Central storage for all level settings (dynamic per level).\
\
- **LevelSettings.cs**\
  Data model representing a single level\'92s configuration.\
\
---\
\
## Database (Supabase Integration)\
Handles communication with Supabase backend.\
\
- **SupabaseConfig.cs**\
  Stores API URL and keys.\
\
- **SupabaseParticipantCodeInsert.cs**\
  Inserts participant into database using participant code.\
\
- **SupabaseSessionInsert.cs**\
  Creates a new session at the start of each level.\
\
- **SupabaseSessionEventInsert.cs**\
  Logs gameplay events (hit/miss, side, object type).\
\
- **SupabaseSessionUpdate.cs**\
  Updates session summary after level completion:\
  - total hits\
  - total misses\
  - left/right breakdown\
\
---\
\
## Archive / Unused\
- **LevelSettingsCardUI.cs**\
  Previously used for dynamic UI cards (replaced by simpler settings UI approach).\
\
---\
\
