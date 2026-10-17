using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public GameObject rewindPanel;
    public GameObject highscoreText;
    public IntVariable gameScore;

    private GameObject[] lifeSprites;

    void Awake()
    {
        lifeSprites = GameObject.FindGameObjectsWithTag("Life")
            .OrderBy(go => go.name)
            .ToArray();

        GameManager.instance.gameStart.AddListener(GameStart);
        GameManager.instance.gameRestart.AddListener(GameRestart);
        GameManager.instance.scoreChange.AddListener(SetScore);
        GameManager.instance.livesChange.AddListener(SetLives);
        GameManager.instance.gameOver.AddListener(GameOver);
    }

    public void GameStart()
    {
        // hide gameover panel
        gameOverPanel.SetActive(false);
        rewindPanel.SetActive(false); // only shown while the rewind animation plays
    }

    public void GameRestart()
    {
        gameOverPanel.SetActive(false);
        rewindPanel.SetActive(false);
    }

    public void SetScore(int score)
    {
        scoreText.text = "Score: " + score.ToString();
        finalScoreText.text = "Score: " + score.ToString();
    }

    public void SetLives(int lives)
    {
        // hide used lives from the front, so Life1 disappears first
        for (int i = 0; i < lifeSprites.Length; i++)
        {
            lifeSprites[i].SetActive(i >= lifeSprites.Length - lives);
        }
    }

    public void ShowRewindPanel()
    {
        rewindPanel.SetActive(true);
    }

    public void HideRewindPanel()
    {
        rewindPanel.SetActive(false);
    }

    public void GameOver()
    {
        gameOverPanel.SetActive(true);
        // set highscore
        highscoreText.GetComponent<TextMeshProUGUI>().text = "TOP- " + gameScore.previousHighestValue.ToString("D6");
        // show
        highscoreText.SetActive(true);
    }
}
