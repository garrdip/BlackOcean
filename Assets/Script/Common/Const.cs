public static class Const 
{
    // Sorting Order
    public const int MAX_ORDER = 999;

    // Room Type
    public const string RoomType_StartLocation = "시작";
    public const string RoomType_Complete = "완료";
    public const string RoomType_Monster = "몬스터";
    public const string RoomType_Event = "이벤트";
    public const string RoomType_Elite = "앨리트";
    public const string RoomType_Camp = "캠프";
    public const string RoomType_ItemNpc = "아이템상인";
    public const string RoomType_CardNpc = "카드상인";
    public const string RoomType_Ruins = "폐허";
    public const string RoomType_Boss = "보스전";

    // 코스트 모자랄 때 텍스트
    public const string Georg_78 = "다시...";
    public const string Georg_79 = "불가능 하다..";
    public const string Georg_80 = "행할 수 없다...";
    public const string Eris_116 = "할 수 없어요.";
    public const string Eris_117 = "그건 좀 힘들어요.";
    public const string Eris_118 = "못한답니다.";
    public const string Eris_119 = "싫어요!";
    public const string Hong_66 = "불가하다.";
    public const string Hong_67 = "그럴 수 없구나.";
    public const string Hong_68 = "못 하겠다.";

    // CardData Sprite Dictionary Key
    public const string ATTACK_CARD_BG = "AttackCardBG";
    public const string BLESS_CARD_BG = "BlessCardBG";
    public const string STRATEGY_CARD_BG = "StrategyCardBG";
    public const string HERO_CARD_BG = "HeroCardBG";
    public const string ATTACK_IMAGE_FRAME = "AttackImageFrame";
    public const string BLESS_IMAGE_FRAME = "BlessImageFrame";
    public const string STRATEGY_IMAGE_FRAME = "StrategyImageFrame";
    public const string HERO_IMAGE_FRAME = "HeroImageFrame";
    public const string NORMAL_GRADE_FRAME = "NormalGradeFrame";
    public const string RARE_GRADE_FRAME = "RareGradeFrame";
    public const string LEGEND_GRADE_FRAME = "LegendGradeFrame";
    public const string ENHANCE_NORMAL_GRADE_FRAME = "EnhanceNormalGradeFrame";
    public const string ENHANCE_RARE_GRADE_FRAME = "EnhanceRareGradeFrame";
    public const string ENHANCE_LEGEND_GRADE_FRAME = "EnhanceLegendGradeFrame";
    public const string ATTACK_EMBLEM = "AttackEmblem";
    public const string BLESS_EMBLEM = "BlessEmblem";
    public const string STRATEGY_EMBLEM = "StrategyEmblem";
    public const string HERO_EMBLEM = "HeroEmblem";
    public const string EXP_BAR_ACTIVE = "ExpBarActive";
    public const string EXP_BAR_INACTIVE = "ExpBarInActive";
    public const string CURSE_CARD_BG = "CurseCardBG";
    public const string CURSE_IMAGE_FRAME = "CurseImageFrame";
    public const string CURSE_GRADE_FRAME = "CurseGradeFrame";
    public const string CURSE_EMBLEM = "CurseEmblem";

    public const string PREFARE_DECK = "뽑을 덱";
    public const string TRASH_DECK = "버린 덱";
    public const string FORGOTTEN_DECK = "잊혀진 덱";

    public const string DEFEND_TEXT = "Defend";


    // 오류 메시지
    public const string ERR_RECOVERY_COUNT_LIMITED = "체력 회복 제한 횟수를 초과하였습니다.";
    public const string ERR_DENIED_GIVE_GOLD_LOCAL_PLAYER = "본인에게는 골드를 전달할 수 없습니다.";
    public const string ERR_NOT_ENOUGH_GOLD = "보유한 골드가 부족합니다.";
    public const string ERR_NO_MORE_SELECTABLE_CARD = "더 이상 카드를 선택할 수 없습니다.";

    
    // MapTileBase 스프라이트 아틀라스
    public const string M_B_Monster_Default = "M_B_Monster_Default";
    public const string M_B_NormalMonster = "M_B_NormalMonster";
    public const string M_B_NormalMonster_Light = "M_B_NormalMonster_Light";
    public const string M_B_EliteMonster = "M_B_EliteMonster";
    public const string M_B_EliteMonster_Light = "M_B_EliteMonster_Light";

    public const string M_B_CardShop = "M_B_CardShop";
    public const string M_B_CardShop_Default = "M_B_CardShop_Default";
    public const string M_B_CardShop_Light = "M_B_CardShop_Light";

    public const string M_B_ItemShop = "M_B_ItemShop";
    public const string M_B_ItemShop_Default = "M_B_ItemShop_Default";
    public const string M_B_ItemShop_Light = "M_B_ItemShop_Light";

    public const string M_B_Event = "M_B_Event";
    public const string M_B_Event_Default = "M_B_Event_Default";
    public const string M_B_Event_Light = "M_B_Event_Light";

    public const string M_B_Camp = "M_B_Camp";
    public const string M_B_Camp_Default = "M_B_Camp_Default";
    public const string M_B_Camp_Light = "M_B_Camp_Light";

    public const string M_B_Complete = "M_B_Complete";
    public const string M_B_Complete_Default = "M_B_Complete_Default";
    public const string M_B_Complete_Light = "M_B_Complete_Light";

    public const string M_B_Current = "M_B_Current";
    public const string M_B_Current_Default = "M_B_Current_Default";
    public const string M_B_Current_Light = "M_B_Current_Light";

    
    // MapTileCap 스프라이트 아틀라스
    public const string M_C_Monster = "M_C_Monster";
    public const string M_C_Monster_Light = "M_C_Monster_Light";
    
    public const string M_C_CardShop = "M_C_CardShop";
    public const string M_C_CardShop_Light = "M_C_CardShop_Light";

    public const string M_C_ItemShop = "M_C_ItemShop";
    public const string M_C_ItemShop_Light = "M_C_ItemShop_Light";

    public const string M_C_Camp = "M_C_Camp";
    public const string M_C_Camp_Light = "M_C_Camp_Light";

    public const string M_C_Event = "M_C_Event";
    public const string M_C_Event_Light = "M_C_Event_Light";

    public const string M_C_Complete = "M_C_Complete";
    public const string M_C_Current = "M_C_Current";

    
    // MapTileIcon 스프라이트 아틀라스
    public const string M_I_NormalMonster = "M_I_NormalMonster";
    public const string M_I_MormalMonster_Light = "M_I_MormalMonster_Light";

    public const string M_I_EliteMonster = "M_I_EliteMonster";
    public const string M_I_EliteMonster_Light = "M_I_EliteMonster_Light";

    public const string M_I_CardShop = "M_I_CardShop";
    public const string M_I_CardShop_Light = "M_I_CardShop_Light";

    public const string M_I_ItemShop = "M_I_ItemShop";
    public const string M_I_ItemShop_Light = "M_I_ItemShop_Light";

    public const string M_I_Camp = "M_I_Camp";
    public const string M_I_Camp_Light = "M_I_Camp_Light";

    public const string M_I_Event = "M_I_Event";
    public const string M_I_Event_Light = "M_I_Event_Light";

    public const string M_I_Complete = "M_I_Complete";
    public const string M_I_Complete_Light= "M_I_Complete_Light";

    public const string M_I_Current = "M_I_Current";
    public const string M_I_Current_Light = "M_I_Current_Light";
}
