using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    public enum Character { NONE, GEORK, ERIS, HONGDANHYANG }
    public enum ObjectType {PLAYER, ENEMY, NPC}
    public enum ActionType {DEFENSE, ATTACK, ATTACKX2, ATTACKANDDEBUFF}
    public enum ActionTarget {UNDEFINED ,FRONT, MIDDLE, BACK, FRONT_MIDDLE, FRONT_BACK, MIDDLE_BACK, WHOLE, WHOLE_ALLY, FIXEDPLAYER, RANDOM, NONE, RANDOM_FRONT_MIDDLE, RANDOM_FRONT_BACK, RANDOM_MIDDLE_BACK, RANDOM_SINGLE, RANDOM_DOUBLE, ENEMY_SINGLE}
    public enum PlayOrder { FIRST = 0, SECOND = 1, THIRD = 2 }
    public enum GameLevel { EASY = 0, NORMAL = 1, HARD = 2 }
    public enum RoomType { MONSTER, ELITE, EVENT_POSITIIVE, EVENT_NEGATIVE, CAMP, ITEM_NPC, BOSS, EMPTY, EXIT } // 스테이지 방 종류. EMPTY = 빈 방(입구 포함), EXIT = 출구(비보스 스테이지의 종료 방). (CARD_NPC는 카드 시스템 제거로 폐기)
    /// <summary>공격 속성 (RPG 전환) — 참격/타격/관통/마법/공명/무속성. NONE은 약점 공격 불가 대신 속성 방어의 영향도 받지 않는다</summary>
    public enum AttackAttribute { NONE, SLASH, STRIKE, PIERCE, MAGIC, RESONANCE }
    /// <summary>캐릭터별 전투 자원 종류 (RPG 전환) — 게오르크: 분노(피해를 주고받으며 충전), 홍단향: MP, 에리스: HP 소모</summary>
    public enum BattleResourceType { NONE, RAGE, MP, HP }
    /// <summary>TP 턴제 전투의 플레이어 액션 (RPG 전환 — 1턴 1액션)</summary>
    public enum TpAction { ATTACK, DEFEND, SKILL, ITEM, MOVE }
    /// <summary>장비 슬롯 (RPG 전환) — 무기 + 갑옷·투구·신발 + 악세사리(2개까지). 착용 제한 없음 (아이템 개편 Q22)</summary>
    public enum EquipSlot { WEAPON, ARMOR, HELMET, BOOTS, ACCESSORY }
    /// <summary>소모품 효과 종류 (RPG 전환) — HP 회복 / 전투 자원(MP·분노) 회복</summary>
    public enum ConsumableType { HEAL_HP, RESTORE_RESOURCE }
    /// <summary>전투 상태 — NONE_BATTLE_SCENE(거점/미로) → BATTLE_INITIALIZE(시작 효과 후 TP 루프) → BATTLE_END(보상) → NONE_BATTLE_END(복귀). 구 카드 페이즈(드로우/액티브/몬스터 순서)는 폐기</summary>
    public enum BattleTurn { NONE_BATTLE_SCENE, NONE_BATTLE_END, BATTLE_INITIALIZE, BATTLE_END }
    public enum BuffType { NONE, IRONDEMON, DEFENSE, ICHI_ATTACK, ICHI_DEFENSE , MOMISPOWERFUL, FLOWERPOWDER, FLOWER, CARDCOSTONE, SOIRAK, APDO, THEREISNOJABI, BOONGGUI, BYEOLMURI, SUHOJA, BLADETRIMMING, IMANGRY, GROWTHSPURT, FURYOFFLOWER, FURYOFIRON,
                            FRAGRANT, GREATMAN , HERO,
                            GOHANG3, GOHANG2_DEBUFF ,GOHANG3_DEBUFF, BOOKBANG, GOTONG, BRILLIANTCURSE, UGLYKNIGHT, ABSOLUTEDOMINATOR, WRAPWINGS, CLOSEPOSE, WISDOMOFOLDSOLDIER, MELODYOFHERO,
                            ERIS_NORMAL, ERIS_2ND, ERIS_3RD, POWEROFDESTRUCTION, POWEROFCREATION, TEMPESTOSO, ECLIPSE, REPEATMARK, SIGNOFEND, ENDOFDISTORTION, ENHANCESKIN, DEATHTHROES, DICHOTOMY,
                            // 스탯 버프 (아이템 개편 Phase 1, ToDo_RPG_ITEM 0-13·Q15·Q45) — 양수 = 상승, 음수 = 하락. 출처(스킬·행동·물약)가 다르면 합산 (M_TurnManager.ApplyTimedStatBuff).
                            // STAT_STRENGTH(힘 스탯 — 힘 요구치 판정에 포함)와 STAT_ATTACK(공격력 가산)은 다르다. ICHI_ATTACK/ICHI_DEFENSE는 기존 스킬 호환용으로만 남는다 (버프 개편에서 정리)
                            STAT_ATTACK, STAT_STRENGTH, STAT_DEFENSE, STAT_MAGIC_DEFENSE, STAT_CRIT_RATE, STAT_EVADE, STAT_ACCURACY
                            }
    // (구 유물 시스템 enum ItemEffectTime/ItemType/ItemGrade는 아이템 개편 Phase 3-1에서 삭제 — Q2·Q21)
    /// <summary>장비 등급 (아이템 개편 Phase 3) — 등급별 옵션 개수·드랍 가중치는 EquipGradeDB.csv</summary>
    public enum EquipGrade { NORMAL, ADVANCED, RARE, LEGEND }
    /// <summary>장비 추가 옵션 종류 — 일반 8종 + 전설 14종 (기획 「장비 옵션」). 슬롯별 허용·수치 범위는 EquipOptionDB.csv</summary>
    public enum EquipOptionType
    {
        STRENGTH, INTELLIGENCE, AGILITY, CONTROL, MAX_HP, MAX_MP, DEFENSE, SHIELD,
        LIFE_STEAL, SKILL_DAMAGE, COMBO_DAMAGE, CRIT_DAMAGE, REFLECT_FLAT, REFLECT_PERCENT, HEAL_RECEIVED,
        WEAKNESS_NULLIFY, CRIT_RATE, ACCURACY, EVADE, FREE_MOVE, NO_ROW_PENALTY, MAX_RAGE
    }
    /// <summary>옵션 값 종류 — NUMBER 수치 / ATTRIBUTE 속성(값 = (int)AttackAttribute, 약점 상쇄) / FLAG 보유 여부(값 1)</summary>
    public enum EquipValueKind { NUMBER, ATTRIBUTE, FLAG }
    /// <summary>무기 종류 — 대검·장검·지팡이·책·수정구·핵 (착용 제한 없음, Q22)</summary>
    public enum WeaponType { NONE, GREATSWORD, LONGSWORD, STAFF, BOOK, ORB, CORE }
    /// <summary>방어구 종류 — 중갑·경갑·로브 (세트 약점은 ArmorTypeDB, Phase 4)</summary>
    public enum ArmorType { NONE, HEAVY, LIGHT, ROBE }
    public enum ValidTarget { NONE, ENEMY, ENEMY_ALL, MEMBER, TEAM , ALL}
    public enum ErisMode {NORMAL, ANGER, MAD}
    public enum MonsterGrade {NORMAL, ELITE, BOSS} // 처치 집계용 몬스터 등급
    public enum LOADING_STATE { ROOM_SCENE = 0, SCENE_LOADING, HUB_PREPARE, GAMEPLAYER_COMPONENT_GEN, UPLOAD_AVATAR, HUB_SCENE, LOADING_GAME_SCENE, GAME_SCENE }

    public static class StringUtils{
        public static string RemoveZWSP(string input)
        {
            return input.Replace("​", "");
        }
    }

    public static class ColorUtils{

        // Color값을 헥사 코드로 변환하는 함수
        public static string ColorToHex(Color color)
        {
            Color32 color32 = color;
            return $"#{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        }

        // 헥사 코드를 Color로 변환하는 함수
        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", ""); // '#' 문자 제거
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color32(r, g, b, 255); // 알파 값은 255로 설정 (불투명)
        }
    }

    /// <summary>스킬 실행 델리게이트 (RPG 전환) — SkillDB.csv의 SkillNo와 동명의 SkillData 정적 메서드에 리플렉션 바인딩된다.
    /// 인자는 SkillCast(정의·시전자·대상 + 콤보 배율 슬롯 — 아이템 개편 Phase 1-4, A9)</summary>
    public delegate IEnumerator ExecuteSkill(SkillData.SkillCast cast);

}
