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