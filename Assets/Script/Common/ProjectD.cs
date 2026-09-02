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
    /// <summary>장비 슬롯 (RPG 전환) — 무기(캐릭터 고정) + 방어구 4종. ACCESSORY는 2개까지 장착 가능</summary>
    public enum EquipSlot { WEAPON, ARMOR, HELMET, BOOTS, ACCESSORY }
    /// <summary>소모품 효과 종류 (RPG 전환) — HP 회복 / 전투 자원(MP·분노) 회복</summary>
    public enum ConsumableType { HEAL_HP, RESTORE_RESOURCE }
    /// <summary>전투 상태 — NONE_BATTLE_SCENE(거점/미로) → BATTLE_INITIALIZE(시작 효과 후 TP 루프) → BATTLE_END(보상) → NONE_BATTLE_END(복귀). 구 카드 페이즈(드로우/액티브/몬스터 순서)는 폐기</summary>
    public enum BattleTurn { NONE_BATTLE_SCENE, NONE_BATTLE_END, BATTLE_INITIALIZE, BATTLE_END }
    public enum BuffType { NONE, IRONDEMON, DEFENSE, ICHI_ATTACK, ICHI_DEFENSE , MOMISPOWERFUL, FLOWERPOWDER, FLOWER, CARDCOSTONE, SOIRAK, APDO, THEREISNOJABI, BOONGGUI, BYEOLMURI, SUHOJA, BLADETRIMMING, IMANGRY, GROWTHSPURT, FURYOFFLOWER, FURYOFIRON,
                            FRAGRANT, GREATMAN , HERO,
                            GOHANG3, GOHANG2_DEBUFF ,GOHANG3_DEBUFF, BOOKBANG, GOTONG, BRILLIANTCURSE, UGLYKNIGHT, ABSOLUTEDOMINATOR, WRAPWINGS, CLOSEPOSE, WISDOMOFOLDSOLDIER, MELODYOFHERO,
                            ERIS_NORMAL, ERIS_2ND, ERIS_3RD, POWEROFDESTRUCTION, POWEROFCREATION, TEMPESTOSO, ECLIPSE, REPEATMARK, SIGNOFEND, ENDOFDISTORTION, ENHANCESKIN, DEATHTHROES, DICHOTOMY
                            }
    public enum ItemEffectTime { STARTBATTLE, CHANGEPOSITION, DEAD, ENDBATTLE, KILLMONSTER, ALWAYS, MOVETOROOM, STARTTURN, HOOKHP, ONCEGET }
    // ITEM: 개인 아이템(상인·이벤트 보상, 소유자에게만 효과) / ARTIFACT: 공용 아티팩트(지역거점 클리어 보상, 전원에게 효과)
    public enum ItemType {ITEM, ARTIFACT, LEGACY}
    public enum ItemGrade {NORMAL, RARE, UNIQUE, LEGEND}
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

    /// <summary>스킬 실행 델리게이트 (RPG 전환) — SkillDB.csv의 SkillNo와 동명의 SkillData 정적 메서드에 리플렉션 바인딩된다</summary>
    public delegate IEnumerator ExecuteSkill(SkillData.SkillDef skill, TargetObject user, List<TargetObject> targets);
    // owner: 아이템 소유 플레이어(전투 밖에서도 유효), sender: 전투 중 해당 플레이어의 타겟오브젝트(비전투 시점 발동이면 null)
    public delegate void ItemEventHanddler(GamePlayerItem owner, TargetObject sender, Item item);

}
