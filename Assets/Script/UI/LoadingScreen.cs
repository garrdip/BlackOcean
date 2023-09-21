using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public List<Sprite> loadingAnimationFrames;

    int counter = 0;
    int number = 0;

    void FixedUpdate()
    {
        counter++;
        if(counter >= 20)
        {
            if(number >= 3)
                number = 0;
            else
                number++;
            counter = 0;
            GetComponent<Image>().sprite = loadingAnimationFrames[number];
        }
    }
}
