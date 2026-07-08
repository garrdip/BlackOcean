using System.Collections.Generic;

/// <summary>
/// 현재 세션의 PlayerInterface 목록.
/// FindObjectsByType 전수 스캔(성능 + 씬 전환 타이밍 취약)을 대체한다.
/// PlayerInterface가 스폰/파괴 시점에 스스로 등록/해제한다 (서버·클라 공용).
/// </summary>
public static class PlayerRegistry
{
    static readonly List<PlayerInterface> players = new List<PlayerInterface>();

    public static IReadOnlyList<PlayerInterface> All
    {
        get
        {
            players.RemoveAll(p => p == null); // 파괴됐지만 해제가 누락된 항목 방어
            return players;
        }
    }

    public static void Register(PlayerInterface player)
    {
        if (player != null && !players.Contains(player))
            players.Add(player);
    }

    public static void Unregister(PlayerInterface player)
    {
        players.Remove(player);
    }
}
