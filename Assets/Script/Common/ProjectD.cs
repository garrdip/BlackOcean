using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectD
{
    public enum Character { NONE, GEORK, ERIS, HONGDANHYANG }
    public enum ObjectType {PLAYER, ENEMY, BOSS}
    public enum ActionType {DEFENSE, ATTACK, ATTACKX2, ATTACKANDDEBUFF}
    public enum ActionTarget {UNDEFINED ,FRONT, MIDDLE, BACK, FRONT_MIDDLE, FRONT_BACK, MIDDLE_BACK, WHOLE, FIXEDPLAYER, RANDOM, NONE, RANDOM_FRONT_MIDDLE, RANDOM_FRONT_BACK, RANDOM_MIDDLE_BACK, RANDOM_SINGLE, RANDOM_DOUBLE, ENEMY_SINGLE}
    public enum PlayOrder { FIRST = 0, SECOND = 1, THIRD = 2 }
    public enum GameLevel { EASY = 0, NORMAL = 1, HARD = 2 }
    public enum RoomType { START_LOCATION, MONSTER, ELITE, EVENT, CAMP, ITEM_NPC, CARD_NPC, UNDEFINED, COMPLETE, RUINS, BOSS }
    public enum BattleTurn { NONE_BATTLE_SCENE, NONE_BATTLE_END,BATTLE_INITIALIZE ,BATTLE_STANDBY, PLAYER_ORDERSELECT, PLAYER_PREEFFECT, PLAYER_DRAW, PLAYER_ACTIVE, PLAYER_ACTIVE_DONE, PLAYER_END_TURN_EFFECT, PLAYER_END, MONSTER_ORDERSELECT, MONSTER_PREEFFECT, MONSTER_ACTIVE, BATTLE_END}
    public enum CardType { BLESS, ATTACK, STRATEGY, CURSE, WOUND, HERO }
    public enum CardCharacteristic { GOOWON, YOUNGWON, GEUNWON, CHALNA, HEBANG, JOONGREUK, SOOKREON, BOONGGUI, SOIRAK, FORWARD, BACKWARD ,GOHENG, GISADO, EUNHASOO, HWAHAP, NON_CHALNA, BYULMOORI }
    public enum BuffType { NONE, IRONDEMON, DEFENSE, ICHI_ATTACK, ICHI_DEFENSE , MOMISPOWERFUL, FLOWERPOWDER, FLOWER, CARDCOSTONE, SOIRAK, APDO, THEREISNOJABI, BOONGGUI, BYEOLMURI, SUHOJA, BLADETRIMMING, IMANGRY, E2, GROWTHSPURT, FURYOFFLOWER, FURYOFIRON, 
                            FRAGRANT, GOHANG3, GOHANG2_DEBUFF ,GOHANG3_DEBUFF}
    public enum GOHENGType {GOHENG1, GOHENG2, GOHENG3}
    public enum DeckListType { PREFARE_DECK, TRASH_DECK,FORGOTTEN_DECK }
    public enum RegionGrade{ NONE, NORMAL, RARE, UNIQUE, LEGEND }
    public enum ItemEffectTime { STARTBATTLE, CHANGEPOSITION, DEAD, ENDBATTLE, KILLMONSTER, ALWAYS, MOVETOROOM, STARTTURN, GETCARD, HOOKHP, ONCEGET, GETICHI }
    public enum ItemType {ARTIFACT, LEGACY}
    public enum ItemGrade {NORMAL, RARE, UNIQUE, LEGEND}
    public enum ValidTarget { NONE, ENEMY, MEMBER, TEAM , ALL}
    public enum ErisMode {NORMAL, ANGER, MAD}
    public enum LOADING_STATE { ROOM_SCENE = 0, SCENE_LOADING, MAP_GENERATE, GAMEPLAYER_COMPONENT_GEN, UPLOAD_AVATAR, MAP_SCENE, LOADING_GAME_SCENE, GAME_SCENE }

    public enum MoveDirection {FORWARD, BACKWARD}
    public static class StringUtils{
        public static string RemoveZWSP(string input)
        {
            return input.Replace("\u200B", "");
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

    public delegate IEnumerator ExecuteCard(Card card,List<TargetObject> target);
    public delegate void CurssEffect(TargetObject target);
    public delegate void ItemEventHanddler(TargetObject sender);
    public delegate void CardSelectCallBack(List<Card> cards);
    public delegate IEnumerator CardBlessEffect(TargetObject target,int index);
    public delegate IEnumerator GOHENGEffcet(TargetObject target);

}