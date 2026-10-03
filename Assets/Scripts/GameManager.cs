using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    // events
    public UnityEvent gameStart;
    public UnityEvent gameRestart;
    public UnityEvent<int> scoreChange;
    public UnityEvent<int> livesChange;
    public UnityEvent gameOver;
    public UnityEvent rewindStart;
    public UnityEvent rewindEnd;

    private int score = 0;

    public int maxRewinds = 2;
    private int livesLeft;

    void Start()
    {
        gameStart.Invoke();
        Time.timeScale = 1.0f;
        SetLives(maxRewinds);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GameRestart()
    {
        // reset score
        score = 0;
        SetScore(score);
        // reset rewind lives
        SetLives(maxRewinds);
        gameRestart.Invoke();
        Time.timeScale = 1.0f;

        //deselect UI button so that keyboard can be used without accidentally triggering it again
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void IncreaseScore(int increment)
    {
        score += increment;
        SetScore(score);
    }

    public void SetScore(int score)
    {
        scoreChange.Invoke(score);
    }

    // called by PlayerMovement when Mario gets hit, returns false if there are no lives left
    public bool UseLife()
    {
        if (livesLeft <= 0) return false;
        SetLives(livesLeft - 1);
        return true;
    }

    private void SetLives(int lives)
    {
        livesLeft = lives;
        livesChange.Invoke(livesLeft);
    }

    // called by EnemyMovement when the rewind animation starts and finishes
    public void RewindStart()
    {
        rewindStart.Invoke();
    }

    public void RewindEnd()
    {
        rewindEnd.Invoke();
    }

    public void GameOver()
    {
        Time.timeScale = 0.0f;
        gameOver.Invoke();
    }
}