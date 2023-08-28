using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    public enum Character { NONE, GEORK, ERIS, HONGDANHYANG }
    public enum ObjectType {PLAYER, ENEMY, BOSS}
    public enum ActionType { SINGLEATTACK, FULLSCALEATTACK, MULTIPLEATTACK, BLEEDING_PER_DAMAGE, DEFENSE , DEBUFF_ATK, DEBUFF_DEF, BUFF_STR, DEBUFF_WEAKNESS, DEBUFF_BLEEDING, DEBUFF_DEFENSELESS }
    public enum ActionTarget {FRONT, MIDDLE, BACK, FRONT_MIDDLE, FRONT_BACK, MIDDLE_BACK, WHOLE, FIXEDPLAYER, RANDOM, NONE}
    public enum PlayOrder { FIRST = 0, SECOND = 1, THIRD = 2 , UNDEFINED = 3}
    public enum GameLevel { EASY = 0, NORMAL = 1, HARD = 2 }
    public enum RoomType { START_LOCATION, MONSTER, ELITE, EVENT, CAMP, ITEM_NPC, CARD_NPC, UNDEFINED }
    public enum BattleTurn { NONE_BATTLE_SCENE, NONE_BATTLE_END, BATTLE_STANDBY, PLAYER_ORDERSELECT, PLAYER_PREEFFECT, PLAYER_DRAW, PLAYER_ACTIVE, PLAYER_ACTIVE_DONE, PLAYER_END, MONSTER_ORDERSELECT, MONSTER_PREEFFECT, MONSTER_ACTIVE, BATTLE_END}
    public enum CardType { BLESS, ATTACK, STRATEGY, CURSE, WOUND }
    public enum CardCharacteristic { GOOWON, YOUNGWON, GEUNWON, CHALNA, HEBANG, JOONGREUK, SOOKREON, BOONGGUI, SOIRAK, FORWARD, BACKWARD ,GOHENG, GISADO, EUNHASOO }
    public enum BuffType { NONE, DEFENSE, ICHI_ATTACK, ICHI_DEFENSE , MOMISPOWERFUL, FLOWERPOWDER, FLOWER, CARDCOSTONE, SOIRAK, APDO, THEREISNOJABI, BOONGGUI, BYEOLMURI, SUHOJA, E2}
    public enum DeckListType { PREFARE_DECK, TRASH_DECK }
    public enum RegionGrade{ NONE, NORMAL, RARE, UNIQUE, LEGEND }
    public enum ItemEffectTime { STARTBATTLE, CHANGEPOSITION, DEAD, ENDBATTLE, KILLMONSTER, ALWAYS, MOVETOROOM, STARTTURN, GETCARD, HOOKHP, ONCEGET, GETICHI }
    public enum ItemType {ARTIFACT, LEGACY}
    public enum ItemGrade {NORMAL, RARE, UNIQUE, LEGEND}
    public enum ValidTarget { NONE, ENEMY, MEMBER, TEAM }

    public enum MoveDirection {FORWARD, BACKWARD}
    public static class StringUtils{
        public static string RemoveZWSP(string input)
        {
            return input.Replace("\u200B", "");
        }
    }

    public static class ColorUtils{
        public static string ToHex(Color color)
        {
            Color32 color32 = color;
            return $"#{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        }
    }

    public delegate IEnumerator ExecuteCard(Card card,List<TargetObject> target);
    public delegate void ItemEventHanddler(TargetObject sender);
}