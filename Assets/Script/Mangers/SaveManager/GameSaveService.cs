using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using ProjectD;

/// <summary>
/// RPG 영속 저장 서비스 (Phase 5) — 네트워크 오브젝트가 아닌 정적 서비스라 메뉴에서도 동작한다.
/// 구조: 호스트가 단일 파일(rpg_save.json)을 소유하고, 파티원 프로필은 SteamID 키로 전원분 저장한다.
/// - PlayerProfile: 레벨/EXP/스탯/스킬트리/장비/소모품/골드/HP/자원 (캐릭터별)
/// - WorldState: 맵 시드 + 방문 완료 타일 + 밝혀진 타일 + 현재 위치 (시드 결정적 생성이라 이것만으로 월드 복원)
/// 저장 시점: 이동 확정/전투 종료 (SphereMapNetwork.ScheduleSave). 로드: 메뉴 '이어서 하기' → pendingLoad.
/// 기존 M_SaveManager(카드 런 스냅샷)와는 독립 — 구 시스템은 카드 제거와 함께 정리 예정.
/// </summary>
public static class GameSaveService
{
    [System.Serializable]
    public class ProfileData
    {
        public string steamId;
        public Character character;
        public int level;
        public int exp;
        public int skillPoints;
        public int strength, agility, vitality, intelligence, defense, magicDefense;
        public int HP, MaxHP;
        public int currentResource, maxResource;
        public int gold;
        public List<string> learnedNodes = new List<string>();
        public List<string> equippedItems = new List<string>();
        public List<string> inventoryEquips = new List<string>();
        public List<string> inventoryConsumables = new List<string>();
    }

    [System.Serializable]
    public class RpgSaveData
    {
        public int mapSeed;
        public int currentTileIndex = -1;
        public List<int> completedTiles = new List<int>();
        public List<int> activeTiles = new List<int>();
        public List<ProfileData> profiles = new List<ProfileData>();
    }

    /// <summary>메뉴에서 '이어서 하기'로 시작 — 서버 시작(SphereMapNetwork/GenerateGamePlayer)이 소비한다</summary>
    public static bool pendingLoad;

    static RpgSaveData loaded;
    public static RpgSaveData Loaded => loaded;

    static string FilePath => Path.Combine(Application.persistentDataPath, "rpg_save.json");

    public static bool HasSaveFile() => File.Exists(FilePath);

    /// <summary>저장 파일 로드. 실패 시 false (일반 시작으로 진행)</summary>
    public static bool TryLoad()
    {
        loaded = null;
        try
        {
            if (!File.Exists(FilePath)) return false;
            loaded = JsonUtility.FromJson<RpgSaveData>(File.ReadAllText(FilePath));
            return loaded != null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 저장 파일 로드 실패 — {e.Message}");
            loaded = null;
            return false;
        }
    }

    public static ProfileData FindProfile(ulong steamId)
    {
        if (loaded == null) return null;
        string key = steamId.ToString();
        return loaded.profiles.Find(profile => profile.steamId == key);
    }

    /// <summary>현재 서버 상태를 파일로 저장 (호스트 전용). 이동 확정/전투 종료 시점에 호출된다</summary>
    public static void SaveGame()
    {
        if (!NetworkServer.active) return;
        if (SphereMapNetwork.instance == null || SphereMapNetwork.instance.system == null || !SphereMapNetwork.instance.system.HasState)
            return;

        var data = new RpgSaveData();
        SphereMapSystem system = SphereMapNetwork.instance.system;
        data.mapSeed = system.Seed;
        data.currentTileIndex = system.currentTileIndex;
        system.ExportProgress(data.completedTiles, data.activeTiles);

        foreach (PlayerInterface playerInterface in PlayerRegistry.All)
        {
            foreach (GamePlayer gamePlayer in playerInterface.ownedPlayers)
            {
                if (gamePlayer == null) continue;
                var profile = new ProfileData
                {
                    steamId = playerInterface.steamID.ToString(),
                    character = gamePlayer.character,
                    level = gamePlayer.level,
                    exp = gamePlayer.exp,
                    skillPoints = gamePlayer.skillPoints,
                    strength = gamePlayer.strength,
                    agility = gamePlayer.agility,
                    vitality = gamePlayer.vitality,
                    intelligence = gamePlayer.intelligence,
                    defense = gamePlayer.defense,
                    magicDefense = gamePlayer.magicDefense,
                    HP = gamePlayer.HP,
                    MaxHP = gamePlayer.MaxHP,
                    currentResource = gamePlayer.currentResource,
                    maxResource = gamePlayer.maxResource,
                    gold = gamePlayer.gold,
                    learnedNodes = new List<string>(gamePlayer.learnedNodes),
                    equippedItems = new List<string>(gamePlayer.equippedItems),
                    inventoryEquips = new List<string>(gamePlayer.inventoryEquips),
                    inventoryConsumables = new List<string>(gamePlayer.inventoryConsumables),
                };
                data.profiles.Add(profile);
            }
        }

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[GameSaveService] 저장 완료 — 타일 {data.currentTileIndex}, 프로필 {data.profiles.Count}명");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 저장 실패 — {e.Message}");
        }
    }

    /// <summary>저장된 프로필을 GamePlayer에 반영 (서버 — GenerateGamePlayer의 기본 초기화를 덮어쓴다)</summary>
    public static void ApplyProfile(GamePlayer gamePlayer, ProfileData profile)
    {
        gamePlayer.level = profile.level;
        gamePlayer.exp = profile.exp;
        gamePlayer.skillPoints = profile.skillPoints;
        gamePlayer.strength = profile.strength;
        gamePlayer.agility = profile.agility;
        gamePlayer.vitality = profile.vitality;
        gamePlayer.intelligence = profile.intelligence;
        gamePlayer.defense = profile.defense;
        gamePlayer.magicDefense = profile.magicDefense;
        gamePlayer.MaxHP = profile.MaxHP;
        gamePlayer.HP = Mathf.Clamp(profile.HP, 1, profile.MaxHP); // 빈사 저장이어도 1로 복원
        gamePlayer.currentResource = profile.currentResource;
        gamePlayer.maxResource = profile.maxResource;
        gamePlayer.gold = profile.gold;
        gamePlayer.learnedNodes.Clear();
        foreach (string nodeId in profile.learnedNodes) gamePlayer.learnedNodes.Add(nodeId);
        gamePlayer.equippedItems.Clear();
        foreach (string equipNo in profile.equippedItems) gamePlayer.equippedItems.Add(equipNo);
        gamePlayer.inventoryEquips.Clear();
        foreach (string equipNo in profile.inventoryEquips) gamePlayer.inventoryEquips.Add(equipNo);
        gamePlayer.inventoryConsumables.Clear();
        foreach (string potionNo in profile.inventoryConsumables) gamePlayer.inventoryConsumables.Add(potionNo);
        Debug.Log($"[GameSaveService] 프로필 복원 — {profile.character} Lv.{profile.level}");
    }

    public static void ClearPending()
    {
        pendingLoad = false;
        loaded = null;
    }
}
