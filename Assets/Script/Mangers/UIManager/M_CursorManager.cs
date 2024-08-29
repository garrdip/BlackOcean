using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class M_CursorManager : SingletonD<M_CursorManager>
{
    public Texture2D[] cursorTextureArray;


    protected override void Awake()
    {
        Cursor.SetCursor(cursorTextureArray[0], Vector2.zero, CursorMode.ForceSoftware);
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0)){
            Cursor.SetCursor(cursorTextureArray[1], Vector2.zero, CursorMode.ForceSoftware);
        }
        if(Input.GetMouseButtonUp(0)){
            Cursor.SetCursor(cursorTextureArray[0], Vector2.zero, CursorMode.ForceSoftware);
        }  
        
    }
}
