using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpOverGoomba : MonoBehaviour
{
    GameManager gameManager;

    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
    }

    // called by each goomba's EnemyMovement when Mario jumps over it
    public void AddScore()
    {
        if (!enabled) return; // disabled by PlayerMovement while Mario is dead
        gameManager.IncreaseScore(1);
    }
}
