using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// 공통 네트워크 싱글톤 클래스(게임 네트워크 내에 1개만 존재해야하는 오브젝트용 : ex-카드 컨트롤 화살표 인디케이터)
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

                if (Instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    Instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";

                    DontDestroyOnLoad(singletonObject);
                }
            }

            return Instance;
        }
    }

    protected virtual void Awake()
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
