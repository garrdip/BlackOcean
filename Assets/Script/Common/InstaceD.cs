using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공통 싱글톤 클래스
public class InstanceD<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T Instance;

    public static T instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<T>();

                if (Instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    Instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";
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
        }
        else
        {
            Destroy(gameObject);
        }
    }
}