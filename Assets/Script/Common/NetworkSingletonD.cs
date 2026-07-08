using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// 공통 싱글톤 클래스
public class NetworkSingletonD<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T Instance;

    public static T instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<T>();
            }

            return Instance;
        }
    }

    // NetworkBehavior 클래스의 경우 Awake함수에서 DDOL을 호출 시 에디터와 빌드간의 차이로 인해 오류발생 이슈 있음.
    // : https://mirror-networking.gitbook.io/docs/manual/components/networkbehaviour - 공식문서 경고문구
    // : https://github.com/MirrorNetworking/Mirror/issues/1748 - 이슈 트래커
    protected virtual void Start()
    {
        // 네트워크 싱글톤 2가지 방식
        // 1. 동적으로 생성되는 네트워크 싱글톤(Loading, Save 매니저등) : 동적으로 생성되므로 정상적으로 새로운 인스턴스가 할당되고 싱글톤 객체로 확정.
        // 2. Scene내에 이미 생성되어있는 네트워크 싱글톤(Lobby, Map, Card, Turn 매니저등) : Scene내에 이미 존재하기때문에 else조건에 의해 삭제됨. 
        // 따라서 삭제 방지를 위해 Start함수 오버라이딩 후 base를 호출하지않고 DDOL를 설정한 뒤 네트워크 매니저에 해당 매니저 관리 리스트에 추가하여 Scene전환에 따라 생성 및 삭제를 관리.
        if (Instance == null)
        {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}