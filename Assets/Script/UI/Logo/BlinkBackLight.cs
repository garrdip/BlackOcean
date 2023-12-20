using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkBackLight : MonoBehaviour
{
    private SpriteRenderer backLight;
    public float speed = 1f;
    int time = 0;
    public bool isDecrease = false;
    // Start is called before the first frame update
    void Start()
    {
        backLight = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
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
