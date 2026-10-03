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

    private GameObject[] lifeSprites;

    void Awake()
    {
        // order life sprites by name (Life1, Life2, ...) so they disappear in order
        lifeSprites = GameObject.FindGameObjectsWithTag("Life").OrderBy(go => go.name).ToArray();
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
    }
}
