using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// one per scene, score and lives live in IntVariable assets so they carry over between scenes
public class GameManager : MonoBehaviour, IPowerupApplicable
{
    public GameConstants gameConstants;
    public IntVariable gameScore;
    public IntVariable gameLives;
    // tick in the first level only, later levels keep the score and lives from the previous scene
    public bool newGameOnStart = true;

    // events, link each to the matching GameEvent's Raise in the Inspector
    public UnityEvent gameStart; // OnGameStart
    public UnityEvent<int> scoreChange; // OnUpdateScore (Dynamic int)
    public UnityEvent<int> livesChange; // OnUpdateLives (Dynamic int)

    void Start()
    {
        if (newGameOnStart)
        {
            gameScore.Value = 0;
            gameLives.Value = gameConstants.maxRewinds;
        }
        gameStart.Invoke();
        Time.timeScale = 1.0f;
        SetScore(gameScore.Value);
        SetLives(gameLives.Value);
    }

    // listens to OnGameRestart
    public void GameRestart()
    {
        // reset score
        gameScore.Value = 0;
        SetScore(gameScore.Value);
        // reset rewind lives
        gameLives.Value = gameConstants.maxRewinds;
        SetLives(gameLives.Value);
        Time.timeScale = 1.0f;

        //deselect UI button so that keyboard can be used without accidentally triggering it again
        EventSystem.current.SetSelectedGameObject(null);
    }

    // listens to OnIncrementScore
    public void IncreaseScore(int increment)
    {
        gameScore.ApplyChange(increment);
        SetScore(gameScore.Value);
    }

    public void SetScore(int score)
    {
        scoreChange.Invoke(score);
    }

    private void SetLives(int lives)
    {
        livesChange.Invoke(lives);
    }

    // listens to OnPowerupAffectsManager
    public void RequestPowerupEffect(IPowerup powerup)
    {
        powerup.ApplyPowerup(this);
    }

    // listens to OnGameOver
    public void GameOver()
    {
        Time.timeScale = 0.0f;
    }
}
