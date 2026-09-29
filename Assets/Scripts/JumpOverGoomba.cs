using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class JumpOverGoomba : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    [System.NonSerialized]
    public int score = 0; // we don't want this to show up in the inspector

    // called by each goomba's EnemyMovement when Mario jumps over it
    public void AddScore()
    {
        if (!enabled) return; // disabled by PlayerMovement while Mario is dead
        score++;
        scoreText.text = "Score: " + score.ToString();
    }
}
