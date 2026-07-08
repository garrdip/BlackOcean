using Mirror;
using UnityEngine;

/// <summary>
/// NetworkServer/NetworkClient.spawned 딕셔너리 안전 조회 헬퍼.
/// 직접 인덱싱(spawned[netId])은 대상이 스폰 해제되었거나 아직 클라이언트에 도착하지 않은 경우
/// KeyNotFoundException으로 크래시하므로 반드시 이 헬퍼를 사용할 것.
/// 조회 실패 시 null을 반환하고 경고 로그를 남긴다 — 호출부에서 null 체크 필요.
/// </summary>
public static class NetLookup
{
    /// <summary>서버 spawned 딕셔너리에서 netId로 컴포넌트를 안전 조회. 실패 시 null.</summary>
    public static T Server<T>(uint netId) where T : Component
    {
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity) && identity != null)
            return identity.GetComponent<T>();

        Debug.LogWarning($"[NetLookup] NetworkServer.spawned에 netId({netId})가 없습니다. 요청 타입: {typeof(T).Name}");
        return null;
    }

    /// <summary>클라이언트 spawned 딕셔너리에서 netId로 컴포넌트를 안전 조회. 실패 시 null.</summary>
    public static T Client<T>(uint netId) where T : Component
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity) && identity != null)
            return identity.GetComponent<T>();

        Debug.LogWarning($"[NetLookup] NetworkClient.spawned에 netId({netId})가 없습니다. 요청 타입: {typeof(T).Name}");
        return null;
    }
}
