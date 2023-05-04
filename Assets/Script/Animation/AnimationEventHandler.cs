using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    public bool isAnimating;

    public void OnCompleteAnimation()
    {
        isAnimating = false;
    }
}
