// 디버그 전용 설정 모음 — 배포 빌드 전 반드시 전부 false 확인
public static class DebugSettings
{
    // 디버그 모드: 게임 시작 시 모든 캐릭터의 체력/최대체력을 DEBUG_PLAYER_HP로 오버라이드 (새 게임·세이브 로드 모두 적용)
    // 종료 방법: 이 값을 false로 바꾸면 기존 체력(BalanceDB.csv의 PLAYER_INIT_HP / 세이브 데이터)으로 복귀
    public static readonly bool USE_DEBUG_PLAYER_HP = true;

    public static readonly int DEBUG_PLAYER_HP = 999;
}
