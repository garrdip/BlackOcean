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
                Instance = FindObjectOfType<T>();
            }

            return Instance;
        }
    }

    // NetworkBehavior 클래스의 경우 Awake함수에서 DDOL을 호출 시 에디터와 빌드간의 차이로 인해 오류발생 이슈 있음.
    // : https://mirror-networking.gitbook.io/docs/manual/components/networkbehaviour - 공식문서 경고문구
    // : https://github.com/MirrorNetworking/Mirror/issues/1748 - 이슈 트래커
    protected virtual void Start()
    {
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