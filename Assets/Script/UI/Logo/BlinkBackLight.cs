using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlinkBackLight : MonoBehaviour
{
    private Image backLight;
    public float speed = 1f;
    int time = 0;
    public bool isDecrease = false;

    void Start()
    {
        backLight = GetComponent<Image>();
    }

    void FixedUpdate()
    {
        if(isDecrease)
        {
            time --;
            float alpha = time * speed * Time.deltaTime + 0.2f;
            if( alpha <= 0.5f )
                isDecrease = false;
            backLight.color = new Color(1,1,1,alpha);
        }
        else
        {
            time ++;
            float alpha = time * speed * Time.deltaTime + 0.2f;
            if( alpha >= 1f )
                isDecrease = true;
            backLight.color = new Color(1,1,1,alpha);
        }
    }
}
