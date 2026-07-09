using System.Collections.Generic;

/// <summary>
/// 현재 세션의 PlayerInterface 목록.
/// FindObjectsByType 전수 스캔(성능 + 씬 전환 타이밍 취약)을 대체한다.
/// PlayerInterface가 스폰/파괴 시점에 스스로 등록/해제한다 (서버·클라 공용).
/// </summary>
public static class PlayerRegistry
{
    static readonly List<PlayerInterface> players = new List<PlayerInterface>();
    static PlayerInterface local;

    /// <summary>
    /// 로컬 플레이어의 PlayerInterface 캐시.
    /// NetworkClient.localPlayer.GetComponent&lt;PlayerInterface&gt;() 매 호출 재조회를 대체한다.
    /// 캐시가 비어 있으면(씬 전환 직후 등) localPlayer에서 1회 재조회해 자가 복구한다.
    /// </summary>
    public static PlayerInterface Local
    {
        get
        {
            if (local == null && Mirror.NetworkClient.localPlayer != null)
                local = Mirror.NetworkClient.localPlayer.GetComponent<PlayerInterface>();
            return local;
        }
    }

    public static void SetLocal(PlayerInterface player)
    {
        local = player;
    }

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
        if (local == player)
            local = null;
    }
}
