using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    // events
    public GameConstants gameConstants;
    public IntVariable gameScore;
    public UnityEvent gameStart;
    public UnityEvent<int> scoreChange; // link to OnUpdateScore's Raise (Dynamic int) in the Inspector
    public UnityEvent<int> livesChange;

    private int livesLeft;

    void Start()
    {
        gameScore.Value = 0;
        gameStart.Invoke();
        Time.timeScale = 1.0f;
        SetLives(gameConstants.maxRewinds);
        SceneManager.activeSceneChanged += SceneSetup;
    }

    public void SceneSetup(Scene current, Scene next)
    {
        gameStart.Invoke();
        SetScore(gameScore.Value);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // listens to OnGameRestart
    public void GameRestart()
    {
        // reset score
        gameScore.Value = 0;
        SetScore(gameScore.Value);
        // reset rewind lives
        SetLives(gameConstants.maxRewinds);
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

    // listens to OnGameOver
    public void GameOver()
    {
        Time.timeScale = 0.0f;
    }
}
