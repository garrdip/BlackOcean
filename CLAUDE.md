# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BlackOcean is a Unity project (the editor currently in use is **6000.3.x**; `ProjectSettings` may still say 2021.3 — open with the version the team is on): a co-op multiplayer **turn-based RPG** (TP gauge turn order, 3-character party) for up to 3 players, distributed on Steam (app ID 2359700 = `SteamManager.AppId`, injected as the `SteamAppId`/`SteamGameId` env vars before `SteamAPI.Init` so builds do not ship `steam_appid.txt`; the root `steam_appid.txt` is for the editor and `Assets/Editor/SteamAppIdBuildCheck` fails the build if the two differ). It was converted from a deck-building card game on the `turn-rpg` branch; **the card system was removed entirely on 2026-09-01** (card scripts, card DB/localization rows, card prefabs and scene UI). Code comments, commit messages, and game data are primarily in **Korean** — keep commit messages in Korean to match the existing history.

There is no CLI build or test pipeline; development happens through the Unity Editor (the `ai-game-developer` MCP / `unity-mcp-cli` can refresh assets, read the console and run editor scripts). For local multiplayer testing, **ParrelSync** (`Assets/Plugins/ParrelSync`) is used to run clone editor instances as additional clients. Running the game requires Steam to be running (Steamworks init failure shows `SteamFailUI`).

## Scene Flow

`MenuScene` → `RoomScene` → `GameScene` (in `Assets/Scenes/`). Matchmaking happens through Steam lobbies; the actual game state is synchronized with Mirror. Single play (`M_NetworkRoomManager.isSinglePlay`, maxConnections = 1) skips character selection and enters GameScene with the fixed 3-character party (`SinglePlayParty`: 전열 게오르크 / 중열 에리스 / 후열 홍단향) owned by the host's `PlayerInterface`.

## Architecture

### Networking (Mirror + Steamworks)

- **Mirror** is vendored at `Assets/ExternalLibrary/Mirror`; Steamworks.NET at `Assets/ExternalLibrary/SteamWork`.
- `M_NetworkRoomManager` (extends Mirror's `NetworkRoomManager`) is the hub: it spawns `RoomPlayer` + `LobbyPlayer` pairs, assigns `PlayOrder`/colors/Steam IDs, and transfers room-player data onto `PlayerInterface` when transitioning to GameScene.
- `M_SteamManager` handles Steam lobby create/join/list via Steamworks callbacks; the host's address is stored as Steam lobby data (`HostAddress` key), and joining clients set `networkManager.networkAddress` from it before `StartClient()`.
- The game is **server-authoritative**: turn state, buffs, skill execution and rewards run on the host; clients receive state via `[SyncVar]`/`SyncList` and RPCs. Mirror identifies RPCs by a 16-bit hash of the method name — a new RPC whose hash collides with an existing one silently breaks the other (watch for "have the same hash" in the console; rename the new one).

### Singleton Patterns (`Assets/Script/Common/`)

- `SingletonD<T>` — standard MonoBehaviour DDOL singleton.
- `NetworkSingletonD<T>` — NetworkBehaviour singleton. **Important:** DDOL is set in `Start()`, not `Awake()`, due to a Mirror editor/build discrepancy (see comments in the file). Scene-placed network singletons are registered in `M_NetworkRoomManager.persistentManagers` so the network manager controls their lifecycle across scene changes.
- `InstanceD<T>` — non-DDOL instance accessor.

### Managers (`Assets/Script/Mangers/` — note the folder name misspelling)

All prefixed `M_`: `M_TurnManager` (battle state owner — `BattleTurn` SyncVar with only `NONE_BATTLE_SCENE`(hub/maze) → `BATTLE_INITIALIZE` → `BATTLE_END` → `NONE_BATTLE_END`; the actual fight is the TP turn loop in `M_TurnManager.TpBattle.cs`; other partials: `.Spawner` (scene transitions), `.Presentation` (BGM/toast/animation RPCs), `.Reward`, `.IronDemon`), `M_HubManager`, `M_LobbyMananger`, `M_SaveManager`, `BattleSpawner`, `RewardService`, plus UI managers under `Mangers/UIManager/` (`PopUpUIManager`, `GameUIManager`, `M_LanguageManager`, `M_SoundManager`, etc.).

### Battle (TP turn-based, `M_TurnManager.TpBattle.cs` + `Assets/Script/Battle/`)

- Each unit's TP charges by `TP_GAIN_BASE + agility`; whoever reaches 100 first acts (overflow carries over). Players get one action per turn: attack / defend / skill / item / move (전열·중열·후열 = `playerOrder[2]/[1]/[0]`). Innate skills (`SkillDB Innate=1`) do not consume the turn (once per turn).
- Damage formulas live in `BattleActions` (stat × power% × row modifier × Eris transform modifier; `ICHI_ATTACK` buff is a flat attack modifier). Player HP/shield/buffs live on `TargetObject` (partials `.Damage`, `.Buff`, `.CharacterSpecific`, `.Voice`); `TargetObject.GainBuff(type, value, isDebuff, isInfinity, isDecrease, isSeparate, from)`.
- Eris transforms by HP ratio (`TargetObject.UpdateErisMode`, BalanceDB `ERIS_*`); 홍단향's 철귀 (iron demon) is `M_TurnManager.IronDemon.cs` + innate skill HS0.
- The temporary battle UI is `OnGUI` in `TpBattle.cs` (action bar, skill buttons, TP labels, turn-order text under MapInfo).

### Data / DB Layer (CSV-driven, `Assets/Resources/DB/`)

- `SkillDB.csv` / `SkillTreeDB.csv` (skills + 드퀘식 skill trees), `CharacterStatDB.csv`, `LevelDB.csv`, `EquipDB.csv`, `ConsumableDB.csv`, `ItemDB.csv`, `ArtifactDB.csv`, `BuffDB.csv`, `MonsterDB.csv` (HP + action patterns), `MonsterStatDB.csv` (weakness/attribute/agility/exp/hazard bonuses), `MonsterGroupDB.csv`, `StageDB.csv`, `BalanceDB.csv` (all tunables — `BalanceData.Get(key, fallback)` logs an error for missing keys; use `BalanceData.TryGet` for optional keys). Parsed by the classes in `Assets/Script/DB/`.
- **Skill effects are bound by reflection**: `SkillData` reads `SkillNo` from `SkillDB.csv` and binds it to a same-named static coroutine in `SkillData.Geork.cs` / `SkillData.Eris.cs` / `SkillData.DanHyang.cs` (`delegate IEnumerator ExecuteSkill(SkillDef, TargetObject user, List<TargetObject> targets)`). Adding a skill = CSV row **and** a method with the exact name. Item/artifact effects (`ItemMethods.cs` / `ArtifactMethods.cs`) follow the same pattern with `ItemDB`/`ArtifactDB` `Number`.

### Shared Enums and Utilities

`Assets/Script/Common/ProjectD.cs` (namespace `ProjectD`) holds nearly every game enum: `Character`, `BattleTurn`, `BuffType`, `RoomType`, `ItemType`, `PlayOrder`, `AttackAttribute`, `BattleResourceType`, `TpAction`, `ErisMode`, etc. Check here first when working with game-state values.

### Player Composition

Each player is a set of NetworkBehaviours: `PlayerInterface` (persistent identity/Steam data carried from room to game; owns one or more `GamePlayer`s via `ownedPlayers`, `currentGamePlayer` = the one UI acts on), `GamePlayer` (stats/level/skill tree/equipment/inventory — partials `.SkillTree`, `.Equipment`, `.DebugStats`), `GamePlayerDeck` (**now only the per-player battle reward list `rewards`** — the class name is kept for prefab binding), `GamePlayerItem`, `GamePlayerTarget`. `PlayerOrder` is the top-left party banner (click = select character / swap row in the hub).

### Save

`GameSaveService` (JSON slots `rpg_save_1~3.json`) stores per-character profiles (SteamID + character) and party progress (unlocked stages, hazard). `M_SaveManager`/`SaveData` is an older HP-only snapshot still spawned by `RoomPlayer`.

### Other Structure

- `Assets/Script/Monster/` — `SpawnedMonster` base with subclasses under `Normal/`, `Elite/`, `Boss/`; monsters use Spine animations. `APDO` (압도) and `ICHI_ATTACK/ICHI_DEFENSE` buffs are the live monster stun/buff system, not card leftovers.
- 거점(Hub): `Assets/Script/Mangers/M_HubManager.cs` — the GameScene has view roots `Hub` (background + `House_*` anchors, 4 NPCs under `Assets/Script/Npc/`), `Stage` (maze) and `Game` (battle); `SetView` switches them. Stage rooms come from `StageDB`; battle end returns via `M_TurnManager.NoneBattleEnd()` → `M_HubManager.OnBattleVictory()`.
- `Assets/Script/UI/PopUpComponent/` — popup windows managed by `PopUpUIManager` (battle result, camp, item shop, save slots, level-up...).
- Localization: key-based string tables at `Assets/Resources/Language/` (`Locales.csv` + one CSV per locale), loaded by `M_LanguageManager`. Korean is the base/fallback language; other locales override by key (`buff.<enum>.name`, `ui.*`). Adding a language = one CSV + one row in `Locales.csv`. See `Document/LOCALIZATION.md`.
- Design docs for the RPG conversion: `Document/RPG_CONVERSION_*.md`. Card-era documents are archived under `Document/legacy/`.

### Key Third-Party Libraries

DOTween (`Assets/Plugins/Demigiant`), Spine runtime (`Assets/ExternalLibrary/Spine`), AYellowpaper SerializedCollections (used heavily for inspector-editable dictionaries), GPM UI (`Gpm.Ui`), UnlimitedScrollUI.
