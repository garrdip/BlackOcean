namespace ProjectD
{
    public enum Character { NONE, GEORK, ERIS, HONGDANHYANG }
    public enum ObjectType {PLAYER, ENEMY, BOSS}
    public enum ActionType { SINGLEATTACK, AREAOFEFFECT, DEFENSE , DEBUFF_ATK, DEBUFF_DEF, BUFF_STR}
    public enum CardGrade { NORMAL, RARE, UNIQUE, LEGEND }
    public enum CardEffect { SINGLEATTACK, MULTIATTACK, SELFDEFENSE, SELFHEAL }
    public enum CardAttribute { DESTROY, CREATION } 
    public enum CardCharacteristic { NONE }
    public enum PlayOrder { FIRST = 0, SECOND = 1, THIRD = 2 , UNDEFINED = 3}
    public enum GameLevel { EASY = 0, NORMAL = 1, HARD = 2 }
}