using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JumpOverGoomba : MonoBehaviour
{
    // link to OnIncrementScore's Raise (Dynamic int) in the Inspector
    public UnityEvent<int> incrementScore;

    public void AddScore()
    {
        if (!enabled) return;
        incrementScore.Invoke(1);
    }
}
