using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using ProjectD;

/// <summary>
/// RPG 영속 저장 서비스 (Phase 5) — 네트워크 오브젝트가 아닌 정적 서비스라 메뉴에서도 동작한다.
/// 구조: 호스트가 세이브 파일을 소유하고, 파티원 프로필은 SteamID 키로 전원분 저장한다.
/// 슬롯(2026-08-31): 슬롯 3개(rpg_save_1~3.json). 메뉴/방 만들기에서 SaveSlotPanel로 슬롯을 고르면(BeginNewGame/BeginContinue) CurrentSlot이 정해지고,
/// 이후 모든 자동/수동 저장(SaveGame)이 그 슬롯에 쓴다. 구 단일 파일(rpg_save.json)은 최초 접근 시 슬롯 1로 이관.
/// - PlayerProfile: 레벨/EXP/스탯/스킬트리/장비/소모품/골드/HP/자원 (캐릭터별)
/// 맵 타일 시스템 제거(거점 전환)로 월드 상태 저장은 없다 — 거점은 항상 같은 화면이므로 프로필만 복원하면 된다.
/// 저장 시점: 전투 종료 후 거점 복귀(M_TurnManager.NoneBattleEnd). 로드: 메뉴 '이어서 하기' → pendingLoad → M_HubManager.OnStartServer.
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
        public int strength, agility, intelligence, defense, magicDefense; // (구 vitality 필드 폐기 — 최대 HP는 MaxHP로 직접 저장)
        public int control; // 제어 — MP 회복·분노 생성 스탯 (스킬트리 CTRL 노드). 구 세이브(필드 없음)는 0
        public int growthSeed; // 레벨업 성장치 분배 시드 (LevelGrowthTable). 구 세이브(필드 없음)는 0 — 0도 유효한 시드라 그대로 사용
        public int HP, MaxHP;
        public int currentResource, maxResource;
        public int gold;
        public List<string> learnedNodes = new List<string>();
        public List<string> equippedItems = new List<string>();   // (버전 0 — 장비 번호 문자열. 버전 1부터는 비어 있고 equippedInstances를 쓴다)
        public List<string> inventoryEquips = new List<string>(); // (버전 0)
        public List<EquipInstanceSave> equippedInstances = new List<EquipInstanceSave>();   // 버전 1 — 장비 인스턴스 (등급·옵션 포함, 아이템 개편 Phase 3-11)
        public List<EquipInstanceSave> inventoryInstances = new List<EquipInstanceSave>();  // 버전 1
        public List<string> inventoryConsumables = new List<string>();
    }

    /// <summary>현재 세이브 형식 버전 (R11). 0 = 필드 없는 구 세이브(문자열 장비·최대치에 장비 델타 포함) → 로드 시 1회 변환. 1 = 장비 인스턴스·고유 최대치 (2026-10-19)</summary>
    public const int CurrentSaveVersion = 1;

    [System.Serializable]
    public class RpgSaveData
    {
        public int saveVersion = 0;        // 세이브 형식 버전 — 구 세이브(필드 없음)는 0 (R11)
        public int unlockedStageCount = 1; // 해금된 스테이지 수 (StageDB 행 순서) — 파티 공용 진행도, 호스트 저장
        public int hazardLevel = 0; // 전역 위험도 (위험도 시스템) — 파티 공용, 호스트 저장. 구 세이브(필드 없음)는 0
        public string savedAt = ""; // 저장 시각 "yyyy-MM-dd HH:mm" — 슬롯 선택 UI 표시용. 구 세이브(필드 없음)는 빈 문자열
        public List<ProfileData> profiles = new List<ProfileData>();
    }

    /// <summary>메뉴에서 '이어서 하기'로 시작 — 서버 시작(M_HubManager.OnStartServer → TryLoad, PlayerInterface.GenerateGamePlayer → FindProfile)이 소비한다</summary>
    public static bool pendingLoad;

    static RpgSaveData loaded;
    public static RpgSaveData Loaded => loaded;

    // ------------------------------------------------------------ 슬롯 -------------------------------------------------------------- //

    /// <summary>세이브 슬롯 수 (SaveSlotPanel이 이 수만큼 카드를 만든다)</summary>
    public const int SlotCount = 3;
    const string SlotPrefsKey = "rpg_save_slot";
    static int currentSlot = -1;

    /// <summary>
    /// 현재 진행 중인 세이브 슬롯 (0 ~ SlotCount-1). 메뉴(싱글)/방 만들기(멀티)에서 선택되며 이후 모든 저장이 이 슬롯에 쓴다.
    /// 세션에서 아직 고르지 않았으면 마지막 사용 슬롯(PlayerPrefs), 그것도 없으면 슬롯 1.
    /// </summary>
    public static int CurrentSlot
    {
        get
        {
            if (currentSlot < 0) currentSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPrefsKey, 0), 0, SlotCount - 1);
            return currentSlot;
        }
        set
        {
            currentSlot = Mathf.Clamp(value, 0, SlotCount - 1);
            PlayerPrefs.SetInt(SlotPrefsKey, currentSlot);
            PlayerPrefs.Save();
        }
    }

    static string LegacyFilePath => Path.Combine(Application.persistentDataPath, "rpg_save.json");
    static string SlotFilePath(int slot) => Path.Combine(Application.persistentDataPath, $"rpg_save_{slot + 1}.json");
    static string FilePath => SlotFilePath(CurrentSlot);

    // 슬롯 도입 전 단일 파일(rpg_save.json)은 최초 1회 슬롯 1로 옮긴다 (슬롯 1이 이미 있으면 구 파일은 버림)
    static bool legacyChecked;
    static void MigrateLegacyFile()
    {
        if (legacyChecked) return;
        legacyChecked = true;
        try
        {
            if (!File.Exists(LegacyFilePath)) return;
            if (!File.Exists(SlotFilePath(0))) File.Move(LegacyFilePath, SlotFilePath(0));
            else File.Delete(LegacyFilePath);
            Debug.Log("[GameSaveService] 구 세이브(rpg_save.json) → 슬롯 1로 이관");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 구 세이브 이관 실패 — {e.Message}");
        }
    }

    public static bool HasSaveFile(int slot)
    {
        MigrateLegacyFile();
        return slot >= 0 && slot < SlotCount && File.Exists(SlotFilePath(slot));
    }

    /// <summary>현재 슬롯에 저장 파일이 있는지</summary>
    public static bool HasSaveFile() => HasSaveFile(CurrentSlot);

    /// <summary>슬롯 중 하나라도 저장 파일이 있는지 — 메뉴 '이어하기' 활성 판정</summary>
    public static bool HasAnySaveFile()
    {
        for (int i = 0; i < SlotCount; i++) if (HasSaveFile(i)) return true;
        return false;
    }

    /// <summary>슬롯 내용 미리보기 (메뉴 요약 표시용) — loaded 상태를 건드리지 않는다. 없거나 손상이면 null</summary>
    public static RpgSaveData Peek(int slot)
    {
        try
        {
            if (!HasSaveFile(slot)) return null;
            return JsonUtility.FromJson<RpgSaveData>(File.ReadAllText(SlotFilePath(slot)));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 슬롯 {slot + 1} 미리보기 실패 — {e.Message}");
            return null;
        }
    }

    /// <summary>슬롯 파일 삭제. 현재 슬롯이면 로드 캐시도 비운다</summary>
    public static bool DeleteSlot(int slot)
    {
        try
        {
            string path = SlotFilePath(slot);
            if (File.Exists(path)) File.Delete(path);
            if (slot == currentSlot) loaded = null;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 슬롯 {slot + 1} 삭제 실패 — {e.Message}");
            return false;
        }
    }

    /// <summary>새 게임 준비 — 슬롯 선택 + 기존 데이터 삭제(덮어쓰기는 UI에서 확인받음) + 로드 예약 해제. 호출 후 호스트 시작</summary>
    public static void BeginNewGame(int slot)
    {
        CurrentSlot = slot;
        DeleteSlot(slot);
        ClearPending();
    }

    /// <summary>이어하기 준비 — 슬롯 선택 + 로드 예약. 파일이 없으면 false. 호출 후 호스트 시작</summary>
    public static bool BeginContinue(int slot)
    {
        if (!HasSaveFile(slot)) return false;
        CurrentSlot = slot;
        loaded = null;
        pendingLoad = true;
        return true;
    }

    // ------------------------------------------------------------ 로드/저장 -------------------------------------------------------------- //

    /// <summary>현재 슬롯의 저장 파일 로드. 실패 시 false (일반 시작으로 진행)</summary>
    public static bool TryLoad()
    {
        loaded = null;
        try
        {
            MigrateLegacyFile();
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

    /// <summary>로드 보장 — 이미 로드된 데이터가 있으면 재사용, 없으면 TryLoad. 룸 씬(캐릭터 자동 선택)과 게임 씬(프로필 복원)이 공유</summary>
    public static bool EnsureLoaded()
    {
        return loaded != null || TryLoad();
    }

    public static ProfileData FindProfile(ulong steamId)
    {
        if (loaded == null) return null;
        string key = steamId.ToString();
        return loaded.profiles.Find(profile => profile.steamId == key);
    }

    /// <summary>SteamID + 캐릭터로 프로필 조회 — 싱글 플레이는 한 PlayerInterface(같은 SteamID)가 파티 3인을 모두 소유하므로 캐릭터까지 맞춰야 한다</summary>
    public static ProfileData FindProfile(ulong steamId, Character character)
    {
        if (loaded == null) return null;
        string key = steamId.ToString();
        return loaded.profiles.Find(profile => profile.steamId == key && profile.character == character);
    }

    /// <summary>현재 서버 상태를 파일로 저장 (호스트 전용). 전투 종료 후 거점 복귀 시점에 호출된다</summary>
    public static void SaveGame()
    {
        if (!NetworkServer.active) return;

        var data = new RpgSaveData();
        data.saveVersion = CurrentSaveVersion;
        data.unlockedStageCount = M_HubManager.instance != null ? M_HubManager.instance.unlockedStageCount : 1;
        data.hazardLevel = M_HubManager.instance != null ? M_HubManager.instance.hazardLevel : 0;
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
                    intelligence = gamePlayer.intelligence,
                    defense = gamePlayer.defense,
                    magicDefense = gamePlayer.magicDefense,
                    control = gamePlayer.control,
                    growthSeed = gamePlayer.growthSeed,
                    HP = gamePlayer.HP,
                    MaxHP = gamePlayer.MaxHP,
                    currentResource = gamePlayer.currentResource,
                    maxResource = gamePlayer.maxResource,
                    gold = gamePlayer.gold,
                    learnedNodes = new List<string>(gamePlayer.learnedNodes),
                    inventoryConsumables = new List<string>(gamePlayer.inventoryConsumables),
                };
                foreach (EquipInstance item in gamePlayer.equippedItems) profile.equippedInstances.Add(item.ToSave());
                foreach (EquipInstance item in gamePlayer.inventoryEquips) profile.inventoryInstances.Add(item.ToSave());
                data.profiles.Add(profile);
            }
        }

        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[GameSaveService] 저장 완료 — 슬롯 {CurrentSlot + 1}, 프로필 {data.profiles.Count}명");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSaveService] 저장 실패 — {e.Message}");
        }
    }

    /// <summary>저장된 프로필을 GamePlayer에 반영 (서버 — GenerateGamePlayer의 기본 초기화를 덮어쓴다). 구 세이브(버전 0)는 여기서 1회 변환한다 (3-11·9-1)</summary>
    public static void ApplyProfile(GamePlayer gamePlayer, ProfileData profile)
    {
        int version = loaded != null ? loaded.saveVersion : 0;
        gamePlayer.level = profile.level;
        gamePlayer.exp = profile.exp;
        gamePlayer.skillPoints = profile.skillPoints;
        gamePlayer.strength = profile.strength;
        gamePlayer.agility = profile.agility;
        gamePlayer.intelligence = profile.intelligence;
        gamePlayer.defense = profile.defense;
        gamePlayer.magicDefense = profile.magicDefense;
        gamePlayer.control = profile.control;
        gamePlayer.growthSeed = profile.growthSeed;
        int maxHP = profile.MaxHP;
        int maxResource = profile.maxResource;
        gamePlayer.learnedNodes.Clear();
        foreach (string nodeId in profile.learnedNodes) gamePlayer.learnedNodes.Add(nodeId);
        gamePlayer.equippedItems.Clear();
        gamePlayer.inventoryEquips.Clear();
        if (version < 1)
        {
            // 버전 0 → 1 (Q43·R13): 저장된 최대치에는 장착 중이던 구 장비의 MaxHP/MaxResource 델타가 더해져 있다 → 구 EquipDB 상수표로 1회 차감 (고유치만 남긴다).
            // 문자열 장비 → NORMAL 인스턴스 (삭제·번호 이동 행은 대체 매핑: AC3 → AC4, 구 AR2 → AR3, 구 HE2 → HE3)
            foreach (string equipNo in profile.equippedItems)
            {
                (int hp, int resource) delta = LegacyMaxDelta(equipNo);
                maxHP -= delta.hp;
                maxResource -= delta.resource;
                EquipInstance item = EquipGenerator.Create(MapLegacyEquipNo(equipNo), EquipGrade.NORMAL);
                if (item.IsValid) gamePlayer.equippedItems.Add(item);
            }
            foreach (string equipNo in profile.inventoryEquips)
            {
                EquipInstance item = EquipGenerator.Create(MapLegacyEquipNo(equipNo), EquipGrade.NORMAL);
                if (item.IsValid) gamePlayer.inventoryEquips.Add(item);
            }
            CharacterStatData.Entry stat = CharacterStatData.Get(profile.character);
            if (stat != null) { maxHP = Mathf.Max(stat.baseHP, maxHP); maxResource = Mathf.Max(stat.baseResource > 0 ? 1 : 0, maxResource); }
            int filled = gamePlayer.ServerFillMissingArmor(); // 방어구 빈 부위 기본 세트 보충 (A5)
            Debug.Log($"[GameSaveService] 구 세이브(버전 0) 변환 — {profile.character}: 장비 {profile.equippedItems.Count + profile.inventoryEquips.Count}개 인스턴스화, 최대 HP {profile.MaxHP} → {maxHP}, 자원 {profile.maxResource} → {maxResource}, 방어구 보충 {filled}");
        }
        else
        {
            foreach (EquipInstanceSave save in profile.equippedInstances)
                if (EquipData.Get(save.equipNo) != null) gamePlayer.equippedItems.Add(EquipInstance.FromSave(save));
            foreach (EquipInstanceSave save in profile.inventoryInstances)
                if (EquipData.Get(save.equipNo) != null) gamePlayer.inventoryEquips.Add(EquipInstance.FromSave(save));
        }
        gamePlayer.MaxHP = maxHP;
        gamePlayer.maxResource = maxResource;
        gamePlayer.HP = Mathf.Clamp(profile.HP, 1, gamePlayer.TotalMaxHP); // 빈사 저장이어도 1로 복원. 상한은 장비 포함 최대치
        gamePlayer.currentResource = Mathf.Clamp(profile.currentResource, 0, Mathf.Max(0, gamePlayer.TotalMaxResource));
        gamePlayer.gold = profile.gold;
        gamePlayer.inventoryConsumables.Clear();
        foreach (string potionNo in profile.inventoryConsumables) gamePlayer.inventoryConsumables.Add(potionNo);
        Debug.Log($"[GameSaveService] 프로필 복원 — {profile.character} Lv.{profile.level} (세이브 버전 {version})");
    }

    // 구 EquipDB(2026-10-19 이전 16행)의 MaxHP/MaxResource 델타 — 버전 0 세이브의 최대치에서 되돌리기 위한 상수표 (Q43)
    static (int hp, int resource) LegacyMaxDelta(string equipNo)
    {
        switch (equipNo)
        {
            case "AR1": return (5, 0);
            case "AR2": return (10, 0);
            case "HE1": return (3, 0);
            case "HE2": return (0, 10);
            case "HW2": return (0, 5);
            case "AC1": return (15, 0);
            case "AC2": return (0, 10);
            default: return (0, 0);
        }
    }

    // 구 장비 번호 → 새 EquipDB 번호 (R13). 삭제된 AC3(숙련의 부적)은 AC4로, 초기 세트가 들어온 자리의 구 AR2(철 갑옷 Lv4)·HE2(현자의 관)는 AR3·HE3으로
    static string MapLegacyEquipNo(string equipNo)
    {
        switch (equipNo)
        {
            case "AC3": return "AC4";
            case "AR2": return "AR3";
            case "HE2": return "HE3";
            default: return equipNo;
        }
    }

    public static void ClearPending()
    {
        pendingLoad = false;
        loaded = null;
    }
}
