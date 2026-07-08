# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BlackOcean is a Unity **2021.3.18f1** project (open with that editor version): a co-op multiplayer deck-building roguelike card game for up to 3 players, distributed on Steam (app ID 2359700, see `steam_appid.txt`). Code comments, commit messages, and game data are primarily in **Korean** — keep commit messages in Korean to match the existing history.

There is no CLI build or test pipeline; development happens through the Unity Editor. For local multiplayer testing, **ParrelSync** (`Assets/Plugins/ParrelSync`) is used to run clone editor instances as additional clients. Running the game requires Steam to be running (Steamworks init failure shows `SteamFailUI`).

## Scene Flow

`MenuScene` → `RoomScene` → `GameScene` (in `Assets/Scenes/`). Matchmaking happens through Steam lobbies; the actual game state is synchronized with Mirror.

## Architecture

### Networking (Mirror + Steamworks)

- **Mirror** is vendored at `Assets/ExternalLibrary/Mirror`; Steamworks.NET at `Assets/ExternalLibrary/SteamWork`.
- `M_NetworkRoomManager` (extends Mirror's `NetworkRoomManager`) is the hub: it spawns `RoomPlayer` + `LobbyPlayer` pairs, assigns `PlayOrder`/colors/Steam IDs, and transfers room-player data onto `PlayerInterface` when transitioning to GameScene.
- `M_SteamManager` handles Steam lobby create/join/list via Steamworks callbacks; the host's address is stored as Steam lobby data (`HostAddress` key), and joining clients set `networkManager.networkAddress` from it before `StartClient()`.
- The game is **server-authoritative**: turn state, buffs, and card execution run on the host; clients receive state via `[SyncVar]`/`SyncList` and RPCs.

### Singleton Patterns (`Assets/Script/Common/`)

- `SingletonD<T>` — standard MonoBehaviour DDOL singleton.
- `NetworkSingletonD<T>` — NetworkBehaviour singleton. **Important:** DDOL is set in `Start()`, not `Awake()`, due to a Mirror editor/build discrepancy (see comments in the file). Scene-placed network singletons are registered in `M_NetworkRoomManager.persistentManagers` so the network manager controls their lifecycle across scene changes.
- `InstanceD<T>` — non-DDOL instance accessor.

### Managers (`Assets/Script/Mangers/` — note the folder name misspelling)

All prefixed `M_`: `M_TurnManager` (battle phase state machine via the `BattleTurn` enum SyncVar — phases like `PLAYER_DRAW`, `PLAYER_ACTIVE`, `MONSTER_ACTIVE`), `M_CardManager`, `M_MapManager`, `M_LobbyMananger`, `M_SaveManager`, plus UI managers under `Mangers/UIManager/` (`PopUpUIManager`, `GameUIManager`, `M_LanguageManager`, `M_SoundManager`, etc.).

### Data / DB Layer (CSV-driven)

- Game data lives as CSV files in `Assets/Resources/DB/` (`CardDB.csv`, `BuffDB.csv`, `MonsterDB.csv`, `MonsterGroupDB.csv`, `ArtifactDB.csv`, `LegacyDB.csv`, `Description.csv`, `CardCharacteristic.csv`), parsed at runtime by the classes in `Assets/Script/DB/`.
- **Card effects are bound by reflection**: `CardData` reads the method name from `CardDB.csv` and creates an `ExecuteCard` delegate (`delegate IEnumerator ExecuteCard(Card card, List<TargetObject> target)`, defined in `ProjectD.cs`) via `Delegate.CreateDelegate`. Adding a card means adding a CSV row **and** a coroutine method with the exact matching name.
- Card effect implementations live in `partial class CardData` files split per playable character: `Assets/Script/Card/CardData_Geork.cs`, `CardData_Eris.cs`, `CardData_DanHyang.cs` (characters: GEORK, ERIS, HONGDANHYANG).

### Shared Enums and Utilities

`Assets/Script/Common/ProjectD.cs` (namespace `ProjectD`) holds nearly every game enum: `Character`, `BattleTurn`, `CardType`, `BuffType`, `RoomType`, `ItemType`, `PlayOrder`, etc. Check here first when working with game-state values.

### Player Composition

Each player is a set of NetworkBehaviours: `PlayerInterface` (persistent identity/Steam data carried from room to game), `GamePlayer`, and split-responsibility components `GamePlayerDeck` (partial, with `GamePlayerDeck_IchiPart.cs`), `GamePlayerItem`, `GamePlayerMap`, `GamePlayerTarget`.

### Other Structure

- `Assets/Script/Monster/` — `Monster` base with subclasses under `Normal/`, `Elite/`, `Boss/`; monsters use Spine animations.
- `Assets/Script/Map/` — hexagonal room-based map progression (`HexagonMapRoom`, `Region`, `MapPlayer`).
- `Assets/Script/UI/PopUpComponent/` — popup windows managed by `PopUpUIManager`.
- Localization: key/value CSV at `Language/Korean.csv` (repo root), loaded by `M_LanguageManager`.

### Key Third-Party Libraries

DOTween (`Assets/Plugins/Demigiant`), Spine runtime (`Assets/ExternalLibrary/Spine`), AYellowpaper SerializedCollections (used heavily for inspector-editable dictionaries), GPM UI (`Gpm.Ui`, e.g. InfiniteScroll), UnlimitedScrollUI.
